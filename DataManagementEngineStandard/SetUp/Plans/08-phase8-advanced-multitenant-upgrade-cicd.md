# Phase 8 — Advanced Features: Multi-Tenant, Upgrade Wizard, and CI/CD

## Objective

Extend the setup wizard system with three enterprise-grade capabilities:

1. **Multi-Tenant Setup** — run the same wizard against many tenant datasources in parallel or sequentially, isolating context per tenant and collecting aggregated reports.
2. **Upgrade Wizard** — detect schema drift since the last run (new entities, added columns) and apply only the delta without re-seeding unchanged data.
3. **CI/CD Headless Mode** — run the full wizard in a pipeline without interactive input; output JSON/Markdown artifacts for approval gates and deployment evidence.

---

## Part A — Multi-Tenant Setup

### Design Goals

- One wizard definition, N tenant executions.
- Each tenant has its own `SetupContext` (separate `IDataSource`, `ConnectionProperties`, `SetupState`).
- Tenants can share the same entity types and seeders.
- Reports are aggregated across all tenants.
- Partial failures (some tenants fail) do not block other tenants from completing.

---

### `TenantSetupContext`

```csharp
namespace TheTechIdea.Beep.SetUp.MultiTenant
{
    /// <summary>Per-tenant specialization of SetupContext.</summary>
    public class TenantSetupContext : SetupContext
    {
        /// <summary>Unique identifier for this tenant (e.g. GUID, slug).</summary>
        public string TenantId { get; set; }

        /// <summary>Human-readable tenant name for reporting.</summary>
        public string TenantName { get; set; }

        /// <summary>Optional schema prefix (e.g. "tenant_abc_" for SQL Server schema isolation).</summary>
        public string SchemaPrefix { get; set; }
    }
}
```

### `ITenantResolver`

```csharp
namespace TheTechIdea.Beep.SetUp.MultiTenant
{
    public interface ITenantResolver
    {
        /// <summary>
        /// Returns a TenantSetupContext for each tenant that needs setup.
        /// Implementation decides which tenants are active/pending.
        /// </summary>
        IReadOnlyList<TenantSetupContext> ResolveTenants(IDMEEditor editor);
    }
}
```

### `MultiTenantSetupOrchestrator`

```csharp
namespace TheTechIdea.Beep.SetUp.MultiTenant
{
    public class MultiTenantSetupOrchestrator
    {
        private readonly IDMEEditor _editor;
        private readonly ISetupWizard _wizardTemplate;
        private readonly MultiTenantSetupOptions _opts;

        public MultiTenantSetupOrchestrator(IDMEEditor editor,
            ISetupWizard wizardTemplate, MultiTenantSetupOptions opts = null)
        {
            _editor = editor;
            _wizardTemplate = wizardTemplate;
            _opts = opts ?? new MultiTenantSetupOptions();
        }

        public MultiTenantSetupReport RunAll(ITenantResolver resolver,
            IProgress<PassedArgs> progress = null)
        {
            var tenants = resolver.ResolveTenants(_editor);
            var tenantResults = new List<TenantSetupResult>();
            int total = tenants.Count;

            for (int i = 0; i < total; i++)
            {
                var tenant = tenants[i];
                Report(progress, (int)(i * 100.0 / total),
                    $"[{i + 1}/{total}] Setting up tenant: {tenant.TenantName}...");

                // Clone wizard for this tenant (reset state)
                var tenantWizard = CloneWizard(_wizardTemplate, tenant);

                IErrorsInfo result;
                if (_opts.RunParallel)
                {
                    // Parallel mode: fire-and-forget, collect later
                    result = new ErrorsInfo { Flag = Errors.Ok };
                    // Note: parallel execution requires thread-safe datasource instances
                    // Use Task.WhenAll pattern in production
                }
                else
                {
                    result = tenantWizard.Run(tenant, progress);
                }

                tenantResults.Add(new TenantSetupResult
                {
                    TenantId = tenant.TenantId,
                    TenantName = tenant.TenantName,
                    Succeeded = result.Flag == Errors.Ok,
                    Message = result.Message,
                    Report = tenantWizard.GetReport()
                });
            }

            return new MultiTenantSetupReport
            {
                TenantResults = tenantResults.AsReadOnly(),
                TotalTenants = total,
                SucceededCount = tenantResults.Count(r => r.Succeeded),
                FailedCount = tenantResults.Count(r => !r.Succeeded)
            };
        }

        private ISetupWizard CloneWizard(ISetupWizard template, TenantSetupContext tenant)
        {
            // Create a fresh wizard with the same step definitions but tenant context
            var builder = new SetupWizardBuilder().WithId($"setup-{tenant.TenantId}");
            foreach (var step in template.Steps)
                builder.AddStep(step);
            builder.WithOptions(template.Options);
            return builder.Build();
        }

        private static void Report(IProgress<PassedArgs> p, int pct, string msg) =>
            p?.Report(new PassedArgs { ParameterInt1 = pct, Messege = msg });
    }

    public class MultiTenantSetupOptions
    {
        /// <summary>Run tenant setups in parallel (requires thread-safe datasource instances).</summary>
        public bool RunParallel { get; set; } = false;

        /// <summary>Maximum number of parallel tenant setups. Ignored when RunParallel=false.</summary>
        public int MaxDegreeOfParallelism { get; set; } = 4;

        /// <summary>If true, continue with remaining tenants when one fails.</summary>
        public bool ContinueOnTenantFailure { get; set; } = true;
    }

    public class TenantSetupResult
    {
        public string TenantId { get; set; }
        public string TenantName { get; set; }
        public bool Succeeded { get; set; }
        public string Message { get; set; }
        public SetupReport Report { get; set; }
    }

    public class MultiTenantSetupReport
    {
        public IReadOnlyList<TenantSetupResult> TenantResults { get; set; }
        public int TotalTenants { get; set; }
        public int SucceededCount { get; set; }
        public int FailedCount { get; set; }
        public bool AllSucceeded => FailedCount == 0;
    }
}
```

---

## Part B — Upgrade Wizard

### Design Goals

- Detect schema drift: new entity types added, new columns on existing entities.
- Apply only the delta migration (additive only in the default safe mode).
- Re-seed only seeders for affected entities.
- Record the "upgrade baseline" hash so subsequent upgrade checks are incremental.

---

### `SchemaVersionTracker`

```csharp
namespace TheTechIdea.Beep.SetUp.Upgrade
{
    public class SchemaVersionTracker
    {
        private const string TrackingEntity = "__BeepSetupVersion";

        /// <summary>
        /// Record the current entity list hash as the "installed version" in the datasource.
        /// Creates the tracking entity if it does not exist.
        /// </summary>
        public IErrorsInfo RecordVersion(IDataSource ds, IDMEEditor editor,
            string versionHash, string wizardId)
        {
            if (!ds.CheckEntityExist(TrackingEntity))
                EnsureVersionTable(ds, editor);

            var record = new
            {
                WizardId = wizardId,
                SchemaHash = versionHash,
                InstalledAt = DateTime.UtcNow
            };
            return ds.InsertEntity(TrackingEntity, record);
        }

        /// <summary>
        /// Returns the most recently recorded schema hash, or null if no version is tracked.
        /// </summary>
        public string GetLastVersion(IDataSource ds, string wizardId)
        {
            if (!ds.CheckEntityExist(TrackingEntity)) return null;
            var data = ds.GetEntity(TrackingEntity,
                new List<AppFilter> { new AppFilter
                    { FieldName = "WizardId", Operator = "=", FilterValue = wizardId } })
                as System.Collections.IEnumerable;
            return data?.Cast<dynamic>()
                .OrderByDescending(r => (DateTime)r.InstalledAt)
                .Select(r => (string)r.SchemaHash)
                .FirstOrDefault();
        }

        private static void EnsureVersionTable(IDataSource ds, IDMEEditor editor)
        {
            var entity = new EntityStructure
            {
                EntityName = TrackingEntity,
                Fields = new List<EntityField>
                {
                    new EntityField { fieldname = "Id", fieldtype = "System.Guid", IsKey = true },
                    new EntityField { fieldname = "WizardId", fieldtype = "System.String" },
                    new EntityField { fieldname = "SchemaHash", fieldtype = "System.String" },
                    new EntityField { fieldname = "InstalledAt", fieldtype = "System.DateTime" }
                }
            };
            ds.CreateEntityAs(entity);
        }
    }
}
```

### `UpgradeWizard`

```csharp
namespace TheTechIdea.Beep.SetUp.Upgrade
{
    /// <summary>
    /// Detects schema and seed drift and applies only the delta.
    /// Wraps ISetupWizard with upgrade-aware CanSkip logic.
    /// </summary>
    public class UpgradeWizard : ISetupWizard
    {
        private readonly ISetupWizard _innerWizard;
        private readonly SchemaVersionTracker _tracker;
        private readonly IReadOnlyList<Type> _entityTypes;

        public IReadOnlyList<ISetupStep> Steps => _innerWizard.Steps;
        public SetupState State => _innerWizard.State;
        public SetupOptions Options => _innerWizard.Options;

        public UpgradeWizard(ISetupWizard innerWizard,
            IReadOnlyList<Type> entityTypes,
            SchemaVersionTracker tracker = null)
        {
            _innerWizard = innerWizard;
            _entityTypes = entityTypes;
            _tracker = tracker ?? new SchemaVersionTracker();
        }

        public IErrorsInfo Run(SetupContext context, IProgress<PassedArgs> progress = null)
        {
            // Inject upgrade-aware schema hash into state
            if (context.DataSource != null)
            {
                var lastHash = _tracker.GetLastVersion(
                    context.DataSource, _innerWizard.Options.ToString());
                if (lastHash != null && context.State != null)
                    context.State.SchemaHash = lastHash;
                // SchemaSetupStep.CanSkip will compare against current hash
                // If unchanged → skip; if changed → run (additive migration)
            }

            var result = _innerWizard.Run(context, progress);

            // Record new version hash on success
            if (result.Flag == Errors.Ok && context.DataSource != null)
            {
                var newHash = ComputeEntityListHash(_entityTypes);
                _tracker.RecordVersion(context.DataSource, context.Editor,
                    newHash, "upgrade-wizard");
            }

            return result;
        }

        public IErrorsInfo Resume(SetupContext context, IProgress<PassedArgs> progress = null) =>
            _innerWizard.Resume(context, progress);

        public SetupReport GetReport() => _innerWizard.GetReport();

        private static string ComputeEntityListHash(IEnumerable<Type> types)
        {
            var names = string.Join(",", types.Select(t => t.FullName).OrderBy(n => n));
            return Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(names)));
        }
    }
}
```

---

## Part C — CI/CD Headless Mode

### Design Goals

- `wizard.Run()` executes without any interactive prompts.
- Dry-run mode produces DDL preview + policy report without applying changes.
- Apply mode produces full artifacts including migration evidence.
- Exit code reflects success (0) or failure (non-zero) for pipeline gates.
- All output written to `SetupOptions.ReportOutputPath`.

---

### `CiSetupRunner`

```csharp
namespace TheTechIdea.Beep.SetUp.Ci
{
    public class CiSetupRunner
    {
        private readonly ISetupWizard _wizard;
        private readonly SetupContext _context;

        public CiSetupRunner(ISetupWizard wizard, SetupContext context)
        {
            _wizard = wizard;
            _context = context;
        }

        /// <summary>
        /// Run in CI mode. Returns 0 on success, 1 on failure.
        /// Writes JSON and Markdown report to ReportOutputPath.
        /// </summary>
        public int Run(TextWriter output = null)
        {
            output ??= Console.Out;
            output.WriteLine($"[CI] BeepDM Setup Wizard starting. " +
                $"DryRun={_context.Options.DryRun} " +
                $"Environment={_context.Options.Environment}");

            var progress = new Progress<PassedArgs>(args =>
                output.WriteLine($"[CI] {args.ParameterInt1,3}% {args.Messege}"));

            var result = _wizard.Run(_context, progress);
            var report = _wizard.GetReport();

            // Export artifacts
            if (!string.IsNullOrEmpty(_context.Options.ReportOutputPath))
            {
                SetupReportExporter.WriteToDirectory(report, _context.Options.ReportOutputPath);
                output.WriteLine($"[CI] Report written to: {_context.Options.ReportOutputPath}");
            }

            // CI summary output (GitHub Actions / Azure Pipelines style)
            output.WriteLine();
            output.WriteLine($"## BeepDM Setup Report");
            output.WriteLine(SetupReportExporter.ToMarkdown(report));

            output.WriteLine(result.Flag == Errors.Ok
                ? "[CI] Setup completed successfully."
                : $"[CI] Setup FAILED: {result.Message}");

            return result.Flag == Errors.Ok ? 0 : 1;
        }

        /// <summary>
        /// Validate the CI plan without applying changes.
        /// Uses MigrationManager.ValidatePlanForCi for migration gates.
        /// </summary>
        public CiValidationResult ValidatePlan(IDMEEditor editor, IDataSource ds,
            IReadOnlyList<Type> entityTypes)
        {
            var migration = new MigrationManager(editor, ds);
            var plan = migration.BuildMigrationPlanForTypes(entityTypes);
            if (plan == null)
                return new CiValidationResult { Passed = false, Message = "Plan build failed." };

            var ciReport = migration.ValidatePlanForCi(plan);
            return new CiValidationResult
            {
                Passed = ciReport.OverallPassed,
                Message = ciReport.Summary,
                CiReportJson = System.Text.Json.JsonSerializer.Serialize(ciReport)
            };
        }
    }

    public class CiValidationResult
    {
        public bool Passed { get; set; }
        public string Message { get; set; }
        public string CiReportJson { get; set; }
    }
}
```

### CLI Usage (BeepShell)

```csharp
// beep setup run --env Production --dry-run --output ./ci-artifacts
var command = new Command("setup");
var runCmd = new Command("run", "Run the setup wizard");
runCmd.Options.Add(new Option<string>("--env", getDefaultValue: () => "Development"));
runCmd.Options.Add(new Option<bool>("--dry-run"));
runCmd.Options.Add(new Option<string>("--output", getDefaultValue: () => "./setup-output"));
runCmd.SetAction(parseResult =>
{
    var env = parseResult.GetValue<string>("--env");
    var dryRun = parseResult.GetValue<bool>("--dry-run");
    var output = parseResult.GetValue<string>("--output");

    var opts = new SetupOptions
    {
        Environment = env,
        DryRun = dryRun,
        ReportOutputPath = output
    };

    var wizard = BuildDefaultWizard(_editor, opts);
    var context = new SetupContext { Editor = _editor, Options = opts };
    var runner = new CiSetupRunner(wizard, context);

    return runner.Run();
});
command.Subcommands.Add(runCmd);
```

### GitHub Actions Integration

```yaml
# .github/workflows/setup-validate.yml
- name: Validate BeepDM Setup Plan
  run: |
    dotnet run --project Beep.Shell setup run \
      --env Staging \
      --dry-run \
      --output ./setup-artifacts

- name: Upload Setup Report
  uses: actions/upload-artifact@v4
  with:
    name: setup-report
    path: ./setup-artifacts/

- name: Gate on Setup Success
  run: |
    # Exit code from CiSetupRunner.Run() gates the pipeline
    exit $?
```

---

## File Layout

```
DataManagementEngineStandard/
  SetUp/
    MultiTenant/
      TenantSetupContext.cs
      ITenantResolver.cs
      MultiTenantSetupOrchestrator.cs
      MultiTenantSetupOptions.cs
      MultiTenantSetupReport.cs
      TenantSetupResult.cs
    Upgrade/
      SchemaVersionTracker.cs
      UpgradeWizard.cs
    Ci/
      CiSetupRunner.cs
      CiValidationResult.cs
```

---

## Testing Approach

### Multi-Tenant
| Test | Description |
|---|---|
| `MultiTenantOrchestrator_RunAll_ProducesOneReportPerTenant` | N tenants → N TenantSetupResult |
| `MultiTenantOrchestrator_OneTenantFails_OthersContinue` | ContinueOnTenantFailure=true |
| `MultiTenantOrchestrator_AggregateReport_CorrectCounts` | SucceededCount + FailedCount = TotalTenants |

### Upgrade Wizard
| Test | Description |
|---|---|
| `UpgradeWizard_NoSchemaChange_SkipsSchemaStep` | Hash unchanged → schema step skipped |
| `UpgradeWizard_NewEntity_RunsMigration` | New entity type → schema step runs |
| `SchemaVersionTracker_RecordAndRetrieve` | Hash written + read from tracking table |
| `SchemaVersionTracker_NoTrackingTable_ReturnsNull` | First run → GetLastVersion returns null |

### CI/CD
| Test | Description |
|---|---|
| `CiSetupRunner_DryRun_WritesReportNoSchemaChanges` | DryRun=true → JSON file created, no tables |
| `CiSetupRunner_SuccessfulRun_Returns0` | Successful run → exit code 0 |
| `CiSetupRunner_FailedRun_Returns1` | Failed run → exit code 1 |
| `CiSetupRunner_ValidatePlan_FailsOnDestructiveChange` | Destructive DDL → Passed=false |

---

## Acceptance Criteria

### Multi-Tenant
- [ ] `MultiTenantSetupOrchestrator.RunAll` produces one `TenantSetupResult` per tenant.
- [ ] `ContinueOnTenantFailure = true` runs all tenants even when one fails.
- [ ] `MultiTenantSetupReport.AllSucceeded` is `true` only when all tenants succeed.

### Upgrade Wizard
- [ ] `UpgradeWizard.Run` calls `SchemaVersionTracker.GetLastVersion` before running.
- [ ] If entity hash unchanged, `SchemaSetupStep.CanSkip` returns `true`.
- [ ] If new entity added, `SchemaSetupStep` runs only an additive migration.
- [ ] `SchemaVersionTracker.RecordVersion` writes to `__BeepSetupVersion` table.

### CI/CD
- [ ] `CiSetupRunner.Run` exits 0 on success, 1 on failure.
- [ ] `CiSetupRunner.Run` writes JSON + MD to `ReportOutputPath` when set.
- [ ] `CiSetupRunner.ValidatePlan` calls `MigrationManager.ValidatePlanForCi`.
- [ ] `DryRun = true` produces report artifacts without modifying the schema.
