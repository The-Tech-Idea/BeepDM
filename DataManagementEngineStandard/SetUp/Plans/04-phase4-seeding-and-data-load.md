# Phase 4 — Data Seeding and Initial Load Step

## Objective

Implement the seeding subsystem that populates reference data, lookup tables, admin users, and any other initial records needed for a new installation.

The seeding system is:
- **Datasource-agnostic**: seeders use `IDataSource.InsertEntity` — no raw SQL.
- **Ordered**: seeders declare foreign-key–style dependencies; the engine topologically sorts them.
- **Idempotent**: each seeder checks whether it has already run before inserting data.
- **Defaults-aware**: `DefaultsManager` is initialized before seeding so audit/timestamp fields are filled.
- **Progress-reporting**: per-seeder and per-row progress events flow to the wizard's progress reporter.

---

## Scope

- `ISeeder` — single seed unit contract
- `ISeederRegistry` — registration, discovery, topological ordering
- `SeederRegistry` — default implementation
- `SeederBase` — abstract base with idempotency guard and defaults integration
- `SeedingStep : ISetupStep` — wizard step that drives the registry
- `ReferenceDataSeederBase` — pattern for enum/lookup table seeds
- `AdminUserSeeder` — example context-aware seeder (application admin record)
- `SeedingStepOptions` — which seeders to run, transaction mode

---

## Contracts

### `ISeeder`

```csharp
namespace TheTechIdea.Beep.SetUp.Seeding
{
    public interface ISeeder
    {
        /// <summary>Stable unique identifier for this seeder (e.g. "roles-seeder").</summary>
        string SeederId { get; }

        /// <summary>Human-readable display name.</summary>
        string SeederName { get; }

        /// <summary>
        /// SeedIds of other seeders that must run before this one.
        /// Declare foreign-key–style dependencies here.
        /// </summary>
        IReadOnlyList<string> DependsOn { get; }

        /// <summary>
        /// True if this seeder has already been applied to the target datasource.
        /// Checked before seed is executed.
        /// </summary>
        bool IsAlreadySeeded(IDataSource dataSource, IDMEEditor editor);

        /// <summary>
        /// Seed data into the datasource. Must not throw.
        /// Returns Errors.Ok on success.
        /// </summary>
        IErrorsInfo Seed(IDataSource dataSource, IDMEEditor editor,
            IProgress<PassedArgs> progress = null);
    }
}
```

### `ISeederRegistry`

```csharp
namespace TheTechIdea.Beep.SetUp.Seeding
{
    public interface ISeederRegistry
    {
        /// <summary>Register a seeder. Returns false if a seeder with the same ID already exists.</summary>
        bool Register(ISeeder seeder);

        /// <summary>Register all ISeeder implementations found in the given assemblies.</summary>
        void DiscoverFromAssemblies(IEnumerable<Assembly> assemblies);

        /// <summary>
        /// Returns all registered seeders in dependency-resolved execution order.
        /// Throws InvalidOperationException if a circular dependency is detected.
        /// </summary>
        IReadOnlyList<ISeeder> GetOrderedSeeders();

        /// <summary>Returns the registered seeder by ID, or null.</summary>
        ISeeder Get(string seederId);

        IReadOnlyCollection<ISeeder> All { get; }
    }
}
```

---

## Implementations

### `SeederRegistry` (Topological Sort)

```csharp
namespace TheTechIdea.Beep.SetUp.Seeding
{
    public class SeederRegistry : ISeederRegistry
    {
        private readonly Dictionary<string, ISeeder> _seeders = new();

        public IReadOnlyCollection<ISeeder> All => _seeders.Values;

        public bool Register(ISeeder seeder)
        {
            if (_seeders.ContainsKey(seeder.SeederId)) return false;
            _seeders[seeder.SeederId] = seeder;
            return true;
        }

        public void DiscoverFromAssemblies(IEnumerable<Assembly> assemblies)
        {
            foreach (var asm in assemblies)
            {
                var seederTypes = asm.GetTypes()
                    .Where(t => !t.IsAbstract && !t.IsInterface &&
                                typeof(ISeeder).IsAssignableFrom(t) &&
                                t.GetConstructor(Type.EmptyTypes) != null);
                foreach (var t in seederTypes)
                {
                    var instance = (ISeeder)Activator.CreateInstance(t);
                    Register(instance);
                }
            }
        }

        public ISeeder Get(string seederId) =>
            _seeders.TryGetValue(seederId, out var s) ? s : null;

        public IReadOnlyList<ISeeder> GetOrderedSeeders()
        {
            // Kahn's topological sort
            var inDegree = _seeders.Keys.ToDictionary(k => k, _ => 0);
            var adj = _seeders.Keys.ToDictionary(k => k, _ => new List<string>());

            foreach (var seeder in _seeders.Values)
            {
                foreach (var dep in seeder.DependsOn)
                {
                    if (!_seeders.ContainsKey(dep))
                        throw new InvalidOperationException(
                            $"Seeder '{seeder.SeederId}' depends on unknown seeder '{dep}'.");
                    adj[dep].Add(seeder.SeederId);
                    inDegree[seeder.SeederId]++;
                }
            }

            var queue = new Queue<string>(inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
            var ordered = new List<ISeeder>();

            while (queue.Count > 0)
            {
                var id = queue.Dequeue();
                ordered.Add(_seeders[id]);
                foreach (var next in adj[id])
                {
                    inDegree[next]--;
                    if (inDegree[next] == 0) queue.Enqueue(next);
                }
            }

            if (ordered.Count != _seeders.Count)
                throw new InvalidOperationException(
                    "Circular dependency detected in seeder registry.");

            return ordered.AsReadOnly();
        }
    }
}
```

### `SeederBase` (Abstract Base with Idempotency + Defaults)

```csharp
namespace TheTechIdea.Beep.SetUp.Seeding
{
    public abstract class SeederBase : ISeeder
    {
        public abstract string SeederId { get; }
        public abstract string SeederName { get; }
        public virtual IReadOnlyList<string> DependsOn => Array.Empty<string>();

        /// <summary>
        /// Default idempotency check: query row count for the target entity.
        /// Override for custom checks (e.g. check specific sentinel row).
        /// </summary>
        public virtual bool IsAlreadySeeded(IDataSource ds, IDMEEditor editor)
        {
            if (!ds.CheckEntityExist(TargetEntityName)) return false;
            var data = ds.GetEntity(TargetEntityName, null) as System.Collections.IEnumerable;
            return data?.Cast<object>().Any() ?? false;
        }

        /// <summary>Primary entity name written by this seeder (used by IsAlreadySeeded).</summary>
        protected abstract string TargetEntityName { get; }

        public IErrorsInfo Seed(IDataSource ds, IDMEEditor editor,
            IProgress<PassedArgs> progress = null)
        {
            // Ensure defaults are wired before inserting
            DefaultsManager.EnsureInitialized(editor);

            try
            {
                return SeedCore(ds, editor, progress);
            }
            catch (Exception ex)
            {
                return new ErrorsInfo
                {
                    Flag = Errors.Failed,
                    Message = $"Seeder '{SeederId}' threw: {ex.Message}",
                    Ex = ex
                };
            }
        }

        /// <summary>Implement actual insert logic here.</summary>
        protected abstract IErrorsInfo SeedCore(IDataSource ds, IDMEEditor editor,
            IProgress<PassedArgs> progress);

        protected static void Report(IProgress<PassedArgs> p, int pct, string msg) =>
            p?.Report(new PassedArgs { ParameterInt1 = pct, Messege = msg });

        protected static IErrorsInfo Ok(string msg = "Ok") =>
            new ErrorsInfo { Flag = Errors.Ok, Message = msg };

        protected static IErrorsInfo Fail(string msg, Exception ex = null) =>
            new ErrorsInfo { Flag = Errors.Failed, Message = msg, Ex = ex };
    }
}
```

### `ReferenceDataSeederBase` (Pattern for Lookup / Enum Tables)

```csharp
namespace TheTechIdea.Beep.SetUp.Seeding
{
    /// <summary>
    /// Base for seeders that insert a fixed list of reference records (status codes,
    /// role types, country codes, etc.) into a single entity/table.
    /// </summary>
    public abstract class ReferenceDataSeederBase<T> : SeederBase
        where T : class, new()
    {
        /// <summary>The fixed reference records to insert.</summary>
        protected abstract IReadOnlyList<T> GetRecords();

        protected override IErrorsInfo SeedCore(IDataSource ds, IDMEEditor editor,
            IProgress<PassedArgs> progress)
        {
            var records = GetRecords();
            int total = records.Count;
            for (int i = 0; i < total; i++)
            {
                var result = ds.InsertEntity(TargetEntityName, records[i]);
                if (result.Flag == Errors.Failed)
                    return result;
                Report(progress, (int)((i + 1) * 100.0 / total),
                    $"Seeding {SeederName}: {i + 1}/{total}");
            }
            return Ok($"Seeded {total} records into '{TargetEntityName}'.");
        }
    }
}
```

### Example: `AdminUserSeeder`

```csharp
namespace TheTechIdea.Beep.SetUp.Seeding.Examples
{
    public class AdminUserSeeder : SeederBase
    {
        public override string SeederId => "admin-user";
        public override string SeederName => "Admin User";
        public override IReadOnlyList<string> DependsOn => new[] { "roles-seeder" };
        protected override string TargetEntityName => "Users";

        public string DefaultAdminEmail { get; set; } = "admin@localhost";
        public string DefaultAdminRole { get; set; } = "Administrator";

        public override bool IsAlreadySeeded(IDataSource ds, IDMEEditor editor)
        {
            // Custom guard: check for admin email specifically
            if (!ds.CheckEntityExist("Users")) return false;
            var users = ds.GetEntity("Users", new List<AppFilter>
            {
                new AppFilter { FieldName = "Email", Operator = "=",
                               FilterValue = DefaultAdminEmail }
            }) as System.Collections.IEnumerable;
            return users?.Cast<object>().Any() ?? false;
        }

        protected override IErrorsInfo SeedCore(IDataSource ds, IDMEEditor editor,
            IProgress<PassedArgs> progress)
        {
            Report(progress, 20, "Creating admin user...");
            var admin = new
            {
                Email = DefaultAdminEmail,
                Role = DefaultAdminRole,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            var result = ds.InsertEntity("Users", admin);
            Report(progress, 100, "Admin user created.");
            return result;
        }
    }
}
```

---

## `SeedingStep : ISetupStep`

```csharp
namespace TheTechIdea.Beep.SetUp.Steps
{
    public class SeedingStep : ISetupStep
    {
        public string StepId => "seeding";
        public string StepName => "Seed Initial Data";
        public string Description => "Runs all registered seeders in dependency order.";
        public IReadOnlyList<string> DependsOn => new[] { "schema-setup" };

        private readonly SeedingStepOptions _opts;

        public SeedingStep(SeedingStepOptions options)
        {
            _opts = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>Skip if all seeders are already seeded.</summary>
        public bool CanSkip(SetupContext context)
        {
            if (context.Options.SkipSeeding) return true;
            var ds = context.DataSource;
            if (ds == null) return false;
            var ordered = _opts.Registry.GetOrderedSeeders();
            return ordered.All(s => s.IsAlreadySeeded(ds, context.Editor));
        }

        public IErrorsInfo Validate(SetupContext context)
        {
            if (context.DataSource == null || context.DataSource.ConnectionStatus != ConnectionState.Open)
                return Fail("DataSource must be open before SeedingStep.");
            return Ok();
        }

        public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs> progress = null)
        {
            if (context.Options.SkipSeeding)
                return Ok("Seeding skipped (SkipSeeding=true).");

            var ds = context.DataSource;
            var editor = context.Editor;
            var ordered = _opts.Registry.GetOrderedSeeders();
            int total = ordered.Count;
            var completedIds = new List<string>();

            for (int i = 0; i < total; i++)
            {
                var seeder = ordered[i];

                // Skip if in completed state from a previous partial run
                if (context.State?.CompletedSeederIds?.Contains(seeder.SeederId) == true)
                {
                    completedIds.Add(seeder.SeederId);
                    continue;
                }

                Report(progress, (int)(i * 100.0 / total),
                    $"[{i + 1}/{total}] Running seeder: {seeder.SeederName}...");

                if (seeder.IsAlreadySeeded(ds, editor))
                {
                    editor.Logger?.WriteLog(
                        $"[SeedingStep] Skipping '{seeder.SeederId}' — already seeded.");
                    completedIds.Add(seeder.SeederId);
                    continue;
                }

                var result = seeder.Seed(ds, editor,
                    new Progress<PassedArgs>(a =>
                        Report(progress,
                            (int)(i * 100.0 / total) + (a.ParameterInt1 / total),
                            a.Messege)));

                if (result.Flag != Errors.Ok)
                {
                    // Persist partial progress
                    if (context.State != null)
                        context.State.CompletedSeederIds =
                            new HashSet<string>(completedIds);
                    return Fail($"Seeder '{seeder.SeederId}' failed: {result.Message}",
                        result.Ex);
                }

                completedIds.Add(seeder.SeederId);
                if (context.State != null)
                    context.State.CompletedSeederIds =
                        new HashSet<string>(completedIds);
            }

            Report(progress, 100, $"Seeding complete. {completedIds.Count} seeders ran.");
            return Ok($"Seeding complete. {completedIds.Count} seeders applied.");
        }

        private static IErrorsInfo Ok(string msg = "Ok") =>
            new ErrorsInfo { Flag = Errors.Ok, Message = msg };
        private static IErrorsInfo Fail(string msg, Exception ex = null) =>
            new ErrorsInfo { Flag = Errors.Failed, Message = msg, Ex = ex };
        private static void Report(IProgress<PassedArgs> p, int pct, string msg) =>
            p?.Report(new PassedArgs { ParameterInt1 = pct, Messege = msg });
    }
}
```

### `SeedingStepOptions`

```csharp
namespace TheTechIdea.Beep.SetUp.Steps
{
    public class SeedingStepOptions
    {
        /// <summary>Registry containing all registered seeders.</summary>
        public ISeederRegistry Registry { get; set; }

        /// <summary>If set, only run seeders with these IDs (for partial seeding scenarios).</summary>
        public IReadOnlyList<string> SeederFilter { get; set; }

        /// <summary>
        /// If true, wrap all seeder executions in a transaction (requires datasource support).
        /// </summary>
        public bool UseTransaction { get; set; } = false;
    }
}
```

---

## Seeder Registration Patterns

### Manual Registration (Startup)
```csharp
var registry = new SeederRegistry();
registry.Register(new RolesSeeder());
registry.Register(new PermissionsSeeder());  // depends on "roles-seeder"
registry.Register(new AdminUserSeeder());    // depends on "roles-seeder"

var seedingStep = new SeedingStep(new SeedingStepOptions { Registry = registry });
```

### Assembly Discovery
```csharp
var registry = new SeederRegistry();
registry.DiscoverFromAssemblies(new[] { typeof(MyApp.Seeders.RolesSeeder).Assembly });
```

### DI-Registered Seeders (Web / Blazor)
```csharp
// Register all ISeeder implementations via DI, then resolve into registry
services.AddScoped<ISeeder, RolesSeeder>();
services.AddScoped<ISeeder, AdminUserSeeder>();
services.AddScoped<ISeederRegistry, SeederRegistry>();

// In wizard factory:
var registry = serviceProvider.GetRequiredService<ISeederRegistry>();
foreach (var seeder in serviceProvider.GetServices<ISeeder>())
    registry.Register(seeder);
```

---

## Defaults Integration

Before each seeder's `Seed()` call, `SeederBase` calls:
```csharp
DefaultsManager.EnsureInitialized(editor);
```

This ensures:
- Audit fields (`CreatedAt`, `UpdatedAt`, `CreatedBy`) are auto-filled.
- GUID primary keys are generated if not set.
- Any `:NOW`, `:USERNAME`, `:NEWGUID` rules are resolved.

To register entity-specific defaults before seeding:
```csharp
DefaultsManager.RegisterProfile(editor, "Users", new[]
{
    new FieldDefaultRule { FieldName = "IsActive", RuleString = "True" },
    new FieldDefaultRule { FieldName = "CreatedAt", RuleString = ":NOW" },
    new FieldDefaultRule { FieldName = "CreatedBy", RuleString = ":USERNAME" }
});
```

---

## Idempotency Rules

| Scenario | Behavior |
|---|---|
| Seeder row exists in target table | `IsAlreadySeeded` returns `true` → skip |
| Seeder already in `SetupState.CompletedSeederIds` | skip, no re-check against DB |
| `SkipSeeding = true` in `SetupOptions` | entire step skips |
| Seeder filter set in `SeedingStepOptions` | only listed seeders run |
| Partial run (seeder N failed, seeders 0..N-1 done) | resume reads `CompletedSeederIds` → only N..end run |

---

## File Layout

```
DataManagementEngineStandard/
  SetUp/
    Seeding/
      ISeeder.cs
      ISeederRegistry.cs
      SeederRegistry.cs
      SeederBase.cs
      ReferenceDataSeederBase.cs
    Steps/
      SeedingStep.cs
      SeedingStepOptions.cs
    Examples/
      AdminUserSeeder.cs
      RolesSeeder.cs          (reference implementation)
      PermissionsSeeder.cs    (depends on RolesSeeder)
```

---

## Testing Approach

| Test | Description |
|---|---|
| `SeederRegistry_TopologicalSort_CorrectOrder` | Roles → Permissions → AdminUser |
| `SeederRegistry_CircularDependency_Throws` | A → B → A → exception |
| `SeederRegistry_UnknownDependency_Throws` | Reference to unregistered ID → exception |
| `SeedingStep_AlreadySeeded_Skips` | IsAlreadySeeded=true → seeder not called |
| `SeedingStep_SeederFails_PersistsPartialState` | Fail at seeder 2 → seeders 0+1 in CompletedSeederIds |
| `SeedingStep_Resume_SkipsCompleted` | Resume after partial run → only seeder 2+ run |
| `SeedingStep_DefaultsInitialized_BeforeSeed` | DefaultsManager.EnsureInitialized called |
| `ReferenceDataSeeder_InsertsAllRecords_ReportsProgress` | All records inserted, progress events fired |

---

## Acceptance Criteria

- [ ] `ISeeder` and `ISeederRegistry` exist in `SetUp/Seeding/`.
- [ ] `SeederRegistry.GetOrderedSeeders()` uses topological sort; throws on circular dep.
- [ ] `SeederBase.Seed()` calls `DefaultsManager.EnsureInitialized` before `SeedCore`.
- [ ] `SeedingStep.CanSkip()` returns `true` when all seeders report `IsAlreadySeeded = true`.
- [ ] A failed seeder persists completed IDs into `SetupState.CompletedSeederIds`.
- [ ] Resuming a partial seeding run skips seeders already in `CompletedSeederIds`.
- [ ] `ReferenceDataSeederBase<T>` provides a working pattern for lookup table seeds.
- [ ] `AdminUserSeeder` example demonstrates context-aware idempotency check.
