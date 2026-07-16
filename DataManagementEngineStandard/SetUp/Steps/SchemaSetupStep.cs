using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor.EntityDiscovery;
using TheTechIdea.Beep.Editor.Migration;
using TheTechIdea.Beep.Editor.Schema;
using static TheTechIdea.Beep.SetUp.StepErrorHelpers;

namespace TheTechIdea.Beep.SetUp.Steps
{
    /// <summary>
    /// Creates the full database schema for a set of .NET entity types by driving the
    /// <c>MigrationManager</c> plan → policy → dry-run → preflight → compensation →
    /// approve → execute pipeline.
    ///
    /// This step is datasource-agnostic: no raw SQL is generated here.
    /// All DDL is produced by <c>MigrationManager</c> based on the connected <c>IDataSource</c>.
    ///
    /// Idempotency: the entity-list SHA-256 hash is stored in <see cref="SetupState.SchemaHash"/>.
    /// <see cref="CanSkip"/> returns <c>true</c> when the hash is unchanged, meaning the schema
    /// was already applied and no entity types have been added, removed, or had their property
    /// signatures changed. The hash includes type names and all public instance property
    /// name+type pairs to detect structual schema drift.
    /// </summary>
    public class SchemaSetupStep : ISchemaSetupStep
    {
        private readonly SchemaSetupStepOptions _opts;
        private readonly ILogger<SchemaSetupStep>? _logger;
        private readonly ISchemaManager? _schemaManager;
        private readonly IEntityDiscoveryService? _discovery;

        /// <summary>
        /// Resolved entity types, cached for the life of the step so CanSkip's hash and Execute's
        /// plan are computed from the same list.
        /// </summary>
        private IReadOnlyList<Type> _resolvedTypes;

        private readonly Rollback.IBackupConfirmationProvider? _backupConfirmation;
        private readonly Security.ISetupApprovalProvider? _approvalProvider;
        private readonly Security.ISetupPrincipal? _principal;

        public SchemaSetupStep(
            SchemaSetupStepOptions opts,
            ILogger<SchemaSetupStep>? logger = null,
            ISchemaManager? schemaManager = null,
            IEntityDiscoveryService? discovery = null,
            Rollback.IBackupConfirmationProvider? backupConfirmation = null,
            Security.ISetupApprovalProvider? approvalProvider = null,
            Security.ISetupPrincipal? principal = null)
        {
            _opts = opts ?? throw new ArgumentNullException(nameof(opts));
            _logger = logger;
            _schemaManager = schemaManager;
            _discovery = discovery;
            _backupConfirmation = backupConfirmation;
            _approvalProvider = approvalProvider;
            _principal = principal;
        }

        /// <summary>
        /// Public accessor for the typed options. UI shells use this to read and write
        /// EntityTypes, ExtraAssemblies, DetectRelationships, StrictPolicyMode, ApproverLabel.
        /// </summary>
        public SchemaSetupStepOptions Options => _opts;

        // ── ISetupStep ───────────────────────────────────────────────────────

        public string StepId => "schema-setup";

        /// <inheritdoc/>
        public System.Text.Json.JsonElement? SerializeOptions()
            => System.Text.Json.JsonSerializer.SerializeToElement(_opts, Definition.SetupJson.Options);

        /// <inheritdoc/>
        public bool SupportsRollback => true;

        /// <inheritdoc/>
        public Security.SetupPermission RequiredPermission => Security.SetupPermission.ApplySchema;

        /// <summary>
        /// Rolls back the applied migration using the execution token recorded on the state.
        /// </summary>
        /// <remarks>
        /// Uses <c>MigrationManager.RollbackFailedExecution(token)</c> — which undoes what was
        /// actually executed — rather than re-deriving a plan (the live schema may have drifted, and
        /// re-planning could compute a different, wrong compensation).
        /// </remarks>
        public Task<IErrorsInfo> RollbackAsync(SetupContext context,
            IProgress<PassedArgs> progress = null, CancellationToken token = default)
        {
            if (context?.Editor == null || context.DataSource == null)
                return Task.FromResult<IErrorsInfo>(Fail("Cannot roll back schema: editor or datasource missing."));

            var executionToken = context.State?.Metadata != null
                && context.State.Metadata.TryGetValue("ExecutionToken", out var t) ? t : null;

            if (string.IsNullOrWhiteSpace(executionToken))
                // Loud, not silent: no token means the migration either never executed or its token
                // wasn't recorded. Don't pretend the schema was undone.
                return Task.FromResult<IErrorsInfo>(
                    Fail("Cannot roll back schema: no migration execution token was recorded."));

            try
            {
                var migration = new MigrationManager(context.Editor, context.DataSource);
                StepErrorHelpers.Report(progress, 0, "Rolling back schema migration…");

                var result = migration.RollbackFailedExecution(executionToken, dryRun: false);
                if (result == null || !result.Success)
                    return Task.FromResult<IErrorsInfo>(
                        Fail($"Schema rollback failed: {result?.Message ?? "no result"}."));

                context.State.SchemaHash = null; // schema no longer matches the applied set
                StepErrorHelpers.Report(progress, 100, "Schema migration rolled back.");
                return Task.FromResult<IErrorsInfo>(Ok($"Schema rolled back (token={executionToken})."));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[SchemaSetupStep] Rollback threw.");
                return Task.FromResult<IErrorsInfo>(Fail($"Schema rollback threw: {ex.Message}", ex));
            }
        }
        public string StepName => "Create Database Schema";
        public string Description =>
            "Plans, validates, and applies schema creation for all registered entity types.";
        public IReadOnlyList<string> DependsOn => new[] { "connection-config" };

        public bool CanSkip(SetupContext context)
        {
            // Explicit skip flag takes priority
            if (context?.Options?.SkipSchema == true) return true;

            if (context?.DataSource == null) return false;
            if (context.State == null) return false;

            // Never skip on an unresolved list: an empty list would hash to a value that
            // could be mistaken for "schema already current". Let Validate report the problem.
            if (!TryResolveEntityTypes(context, allowDiscovery: false, out var types, out _)
                || types.Count == 0)
                return false;

            var currentHash = ComputeEntityListHash(types);
            return context.State.SchemaHash == currentHash;
        }

        public IErrorsInfo Validate(SetupContext context)
        {
            if (context?.Editor == null)
                return Fail("SetupContext.Editor is required.");

            if (context.DataSource == null || context.DataSource.ConnectionStatus != ConnectionState.Open)
                return Fail("DataSource must be open before SchemaSetupStep. " +
                            "Ensure ConnectionConfigStep ran successfully.");

            // Discovery runs in Execute, so a discovery-backed step legitimately has no types yet.
            if (_discovery != null && !HasExplicitEntityTypes())
                return Ok();

            if (!TryResolveEntityTypes(context, allowDiscovery: false, out var types, out var error))
                return Fail(error);

            if (types.Count == 0)
                return Fail("SchemaSetupStepOptions.EntityTypeNames must contain at least one type.");

            return Ok();
        }

        private bool HasExplicitEntityTypes()
        {
#pragma warning disable CS0618 // legacy path is still supported
            if (_opts.EntityTypes?.Count > 0) return true;
#pragma warning restore CS0618
            return _opts.EntityTypeNames?.Count > 0;
        }

        /// <summary>
        /// Resolves the entity types for this run, in priority order:
        /// explicit <c>EntityTypes</c> (legacy) → <c>EntityTypeNames</c> → auto-discovery.
        /// Cached, so the hash in <see cref="CanSkip"/> and the plan in <c>Execute</c> agree.
        /// </summary>
        private bool TryResolveEntityTypes(SetupContext context, bool allowDiscovery,
            out IReadOnlyList<Type> types, out string error)
        {
            error = null;

            if (_resolvedTypes != null)
            {
                types = _resolvedTypes;
                return true;
            }

#pragma warning disable CS0618 // legacy path is still supported
            if (_opts.EntityTypes?.Count > 0)
            {
                types = _resolvedTypes = _opts.EntityTypes;
                return true;
            }
#pragma warning restore CS0618

            if (_opts.EntityTypeNames?.Count > 0)
            {
                var resolved = new List<Type>(_opts.EntityTypeNames.Count);
                var missing = new List<string>();

                foreach (var name in _opts.EntityTypeNames)
                {
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    var t = ResolveTypeName(context, name);
                    if (t != null) resolved.Add(t); else missing.Add(name);
                }

                if (missing.Count > 0)
                {
                    // Loud on purpose: silently dropping a type would create no schema for it and
                    // still report success, and would poison SetupState.SchemaHash for later runs.
                    error = $"Could not resolve entity type(s): {string.Join(", ", missing)}. " +
                            "Ensure the declaring assembly is loaded (LoadAllAssembly) or listed in " +
                            "SchemaSetupStepOptions.ExtraAssemblyNames.";
                    types = Array.Empty<Type>();
                    return false;
                }

                types = _resolvedTypes = resolved;
                return true;
            }

            if (allowDiscovery && _discovery != null)
            {
                var discovered = _discovery.Discover(new EntityDiscoveryOptions
                {
                    Scope = DiscoveryScope.AllLoaded,
                    ExcludeAbstract = true,
                    ExcludeOpenGenerics = true
                });
                types = _resolvedTypes = discovered
                    .Select(e => ResolveTypeName(context, e.FullName))
                    .Where(t => t != null)
                    .ToList();
                _logger?.LogInformation("[SchemaSetupStep] Auto-discovered {Count} entity types", types.Count);
                return true;
            }

            types = Array.Empty<Type>();
            return true;
        }

        /// <summary>
        /// Resolves a type name through the assembly handler's cache first (it knows about
        /// plugin-loaded assemblies that <see cref="Type.GetType(string)"/> cannot see), then
        /// falls back to the CLR and finally to a scan of the configured extra assemblies.
        /// </summary>
        private Type ResolveTypeName(SetupContext context, string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;

            var viaHandler = context?.Editor?.assemblyHandler?.GetType(name);
            if (viaHandler != null) return viaHandler;

            var viaClr = Type.GetType(name, throwOnError: false);
            if (viaClr != null) return viaClr;

            foreach (var asm in EnumerateProbeAssemblies())
            {
                var t = asm?.GetType(name, throwOnError: false);
                if (t != null) return t;

                // Simple-name fallback: definitions written by hand often omit the namespace.
                t = SafeGetTypes(asm).FirstOrDefault(x =>
                    string.Equals(x.Name, name, StringComparison.Ordinal) ||
                    string.Equals(x.FullName, name, StringComparison.Ordinal));
                if (t != null) return t;
            }

            return null;
        }

        private IEnumerable<Assembly> EnumerateProbeAssemblies()
        {
            if (_opts.ExtraAssemblies != null)
                foreach (var a in _opts.ExtraAssemblies) yield return a;

            if (_opts.ExtraAssemblyNames != null)
            {
                foreach (var n in _opts.ExtraAssemblyNames)
                {
                    Assembly a = null;
                    try { a = Assembly.Load(n); }
                    catch (Exception ex) { _logger?.LogWarning("Could not load assembly '{Name}': {Msg}", n, ex.Message); }
                    if (a != null) yield return a;
                }
            }
        }

        private static IEnumerable<Type> SafeGetTypes(Assembly asm)
        {
            if (asm == null) return Array.Empty<Type>();
            try { return asm.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t != null); }
        }

        public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs> progress = null)
        {
            var editor = context.Editor;
            var ds = context.DataSource;
            bool strict = _opts.StrictPolicyMode ?? context.Options?.StrictPolicyMode ?? false;
            string environment = context.Options?.Environment ?? "Development";

            // Resolve entity types: explicit types → names → auto-discovery.
            if (!TryResolveEntityTypes(context, allowDiscovery: true, out var entityTypes, out var resolveError))
                return Fail(resolveError);

            if (entityTypes.Count == 0)
                return Fail("No entity types to create. Set SchemaSetupStepOptions.EntityTypeNames.");

            var migration = new MigrationManager(editor, ds);

            // Register extra assemblies when provided
            foreach (var asm in EnumerateProbeAssemblies())
                migration.RegisterAssembly(asm);

            // ── A. Build migration plan ──────────────────────────────────────
            StepErrorHelpers.Report(progress, 5, "Building migration plan…");
            var plan = migration.BuildMigrationPlanForTypes(
                entityTypes,
                _opts.DetectRelationships);

            if (plan == null)
                return Fail("BuildMigrationPlanForTypes returned null.");

            context.MigrationPlan = plan;

            // ── B. Evaluate policy ───────────────────────────────────────────
            StepErrorHelpers.Report(progress, 15, "Evaluating migration policy…");
            var policyOpts = BuildPolicyOptions(environment, strict);
            var policy = migration.EvaluateMigrationPlanPolicy(plan, policyOpts);

            if (policy.HasBlockingFindings)
            {
                var reasons = string.Join("; ",
                    policy.Findings
                        .Where(f => f.Decision == MigrationPolicyDecision.Block)
                        .Select(f => f.Message));
                return Fail($"Migration plan blocked by policy: {reasons}");
            }

            // ── C. Dry-run DDL preview ───────────────────────────────────────
            StepErrorHelpers.Report(progress, 25, "Generating dry-run DDL preview…");
            var dryRun = migration.GenerateDryRunReport(plan);
            if (dryRun != null)
                context.SetDryRunReport(JsonSerializer.Serialize(dryRun));

            // ── C2. Per-entity schema drift (.NET class vs live DB) ───
            try
            {
                var schema = _schemaManager ?? new SchemaManager(editor);
                // Task.Run keeps the inspection's awaits off the caller's SynchronizationContext,
                // so a UI caller blocked here in GetResult() cannot deadlock against them.
                var drift = Task.Run(() => schema.InspectManyAsync(entityTypes, ds)).GetAwaiter().GetResult();
                if (drift != null && drift.Count > 0)
                {
                    context.Properties["SchemaDrift"] = drift;
                    StepErrorHelpers.Report(progress, 26, $"Captured {drift.Count} schema drift report(s).");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "[SchemaSetupStep] Schema drift capture failed");
                editor.Logger?.WriteLog(
                    $"[SchemaSetupStep] Schema drift capture failed: {ex.Message}");
            }

            // In DryRun mode stop here — do not modify the datasource
            if (context.Options?.DryRun == true)
                return Ok("Dry-run complete. Schema not modified (DryRun=true).");

            // ── D. Preflight checks ──────────────────────────────────────────
            StepErrorHelpers.Report(progress, 35, "Running preflight checks…");
            var preflight = migration.RunPreflightChecks(plan, policyOpts);
            if (!preflight.CanApply)
            {
                var failures = string.Join("; ",
                    preflight.Checks
                        .Where(c => c.Decision == MigrationPolicyDecision.Block)
                        .Select(c => c.Message));
                return Fail($"Preflight failed: {failures}");
            }

            // ── E. Impact report (advisory) ──────────────────────────────────
            StepErrorHelpers.Report(progress, 45, "Building impact report…");
            var impact = migration.BuildImpactReport(plan);
            var highImpact = impact?.Entries?
                .Count(e => e.Sensitivity == MigrationImpactSensitivity.High) ?? 0;
            if (highImpact > 0)
                editor.Logger?.WriteLog(
                    $"[SchemaSetupStep] {highImpact} high-sensitivity operations detected. " +
                    "Review the impact report before applying in production.");

            // ── F. Compensation / rollback plan ──────────────────────────────
            StepErrorHelpers.Report(progress, 55, "Building compensation plan…");
            var compensationPlan = migration.BuildCompensationPlan(plan);
            if (compensationPlan != null)
                context.SetCompensationPlan(JsonSerializer.Serialize(compensationPlan));
            // Ask a provider whether a backup is really confirmed. The old code passed
            // `backupConfirmed: !strict`, which asserted a backup existed precisely when nobody had
            // checked. Default provider returns false and warns — never claim an unverified backup.
            bool backupConfirmed = _backupConfirmation != null
                && Task.Run(() => _backupConfirmation.IsBackupConfirmedAsync(context))
                       .GetAwaiter().GetResult();

            if (!backupConfirmed)
                _logger?.LogWarning("[SchemaSetupStep] No backup confirmed before schema change on '{Ds}'.",
                    ds?.DatasourceName);

            var rollbackReadiness = migration.CheckRollbackReadiness(
                plan,
                backupConfirmed: backupConfirmed,
                restoreTestEvidenceProvided: false);

            if (!rollbackReadiness.IsReady && strict)
                return Fail("Rollback readiness check failed in StrictPolicyMode. " +
                             "Confirm backup and restore-test evidence before proceeding.");

            // ── G. Approve plan ──────────────────────────────────────────────
            StepErrorHelpers.Report(progress, 65, "Approving migration plan…");

            string approvedBy = _opts.ApproverLabel;
            string approvalNotes = $"Auto-approved by setup wizard. Environment={environment}";

            // When an approval provider is configured, get a real decision bound to the plan id —
            // rather than the self-granted "SetupWizard" label. An enterprise provider rejects
            // self-approval; the solo default approves but records IsSelfApproved honestly.
            if (_approvalProvider != null)
            {
                var approval = Task.Run(() =>
                    _approvalProvider.RequestApprovalAsync(context, _principal, plan.PlanId))
                    .GetAwaiter().GetResult();

                if (approval == null || !approval.Granted)
                    return Fail($"Migration plan approval was not granted: {approval?.Note ?? "no approval"}.");

                approvedBy = approval.ApproverLabel ?? approval.ApproverId ?? approvedBy;
                approvalNotes = $"{approval.Note} SelfApproved={approval.IsSelfApproved}. Environment={environment}";
            }

            plan = migration.ApproveMigrationPlan(plan, approvedBy: approvedBy, notes: approvalNotes);

            // ── H. Execute ───────────────────────────────────────────────────
            StepErrorHelpers.Report(progress, 70, "Executing migration plan…");
            var execPolicy = new MigrationExecutionPolicy();
            var checkpoint = migration.CreateExecutionCheckpoint(plan);

            var execResult = migration.ExecuteMigrationPlan(
                plan,
                execPolicy,
                checkpoint.ExecutionToken,
                new Progress<PassedArgs>(a =>
                    StepErrorHelpers.Report(progress, 70 + (a.ParameterInt1 / 4), a.Messege)));

            if (!execResult.Success)
            {
                if (context.State != null)
                    context.State.Metadata["LastCheckpointId"] =
                        execResult.Checkpoint?.ExecutionToken ?? string.Empty;
                return Fail("Migration execution failed: " + execResult.Message);
            }

            context.MigrationResult = execResult;

            // ── I. Record schema hash & refresh metadata ─────────────────────
            // Must hash the RESOLVED list — hashing _opts.EntityTypes would hash null whenever the
            // step was driven by EntityTypeNames, so CanSkip would never match and the schema would
            // be re-planned on every run.
            var hash = ComputeEntityListHash(entityTypes);
            if (context.State != null)
            {
                context.State.SchemaHash = hash;
                context.State.Metadata["MigrationPlanId"] = plan.PlanId ?? string.Empty;
                context.State.Metadata["ExecutionToken"] =
                    execResult.Checkpoint?.ExecutionToken ?? string.Empty;
            }

            ds.GetEntitesList();

            StepErrorHelpers.Report(progress, 100, "Schema creation complete.");
            return Ok($"Schema applied via MigrationManager. Token={execResult.ExecutionToken}");
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static MigrationPolicyOptions BuildPolicyOptions(string environment, bool strict)
        {
            var tier = environment switch
            {
                "Production" => MigrationEnvironmentTier.Production,
                "Staging"    => MigrationEnvironmentTier.Staging,
                "Test"       => MigrationEnvironmentTier.Test,
                _            => MigrationEnvironmentTier.Development
            };

            return new MigrationPolicyOptions
            {
                EnvironmentTier = tier,
                RequireApprovalForHighRisk = strict || tier >= MigrationEnvironmentTier.Staging,
                RequireApprovalForCriticalRisk = true,
                BlockDestructiveInProtectedEnvironments =
                    strict || tier >= MigrationEnvironmentTier.Production
            };
        }

        private static string ComputeEntityListHash(IEnumerable<Type> types)
        {
            var sb = new StringBuilder();
            foreach (var t in types.OrderBy(t => t.FullName))
            {
                sb.Append(t.FullName);
                foreach (var prop in t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .OrderBy(p => p.Name))
                {
                    sb.Append('|');
                    sb.Append(prop.Name);
                    sb.Append(':');
                    sb.Append(prop.PropertyType.FullName);
                }
            }
            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return Convert.ToHexString(SHA256.HashData(bytes));
        }
    }
}
