using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor.Migration;
using static TheTechIdea.Beep.SetUp.StepErrorHelpers;

namespace TheTechIdea.Beep.SetUp.Steps
{
    /// <summary>Options for <see cref="VersionGateStep"/>.</summary>
    public sealed class VersionGateStepOptions
    {
        /// <summary>Explicit entity types to gate on. Highest priority; else resolved from options.</summary>
        public IReadOnlyList<Type> EntityTypes { get; set; }

        /// <summary>Datasource to migrate. Falls back to <c>SetupContext.DataSource</c> when empty.</summary>
        public string DatasourceName { get; set; }

        /// <summary>Explicit declared version. Falls back to <c>SetupOptions.DeclaredSchemaVersion</c>, then the assembly attribute.</summary>
        public string DeclaredVersion { get; set; }

        public bool DetectRelationships { get; set; } = true;
    }

    /// <summary>
    /// The startup version gate: compares the declared schema version and the entity model against the
    /// version recorded <em>in the target database</em>, and applies pending migrations when the app has
    /// moved ahead or the model has drifted. Composes as an ordinary <see cref="ISetupStep"/> so it
    /// honours DryRun, RBAC, audit, and progress like every other step.
    /// </summary>
    /// <remarks>
    /// Trigger is <b>both</b>: the entity-model diff (<c>BuildMigrationPlanForTypes</c>) decides <i>what</i>
    /// to apply; the declared-vs-recorded semver compare is the coarse gate deciding <i>whether</i> to look.
    /// Migration and version-stamping are delegated to <see cref="MigrationTrackingService"/>, so the DB
    /// marker and JSON mirror advance together. Intended to run in a dedicated upgrade-pass wizard whose
    /// state starts empty each launch, so the wizard's skip-completed-steps guard never suppresses it.
    /// </remarks>
    public sealed class VersionGateStep : ISetupStep
    {
        /// <summary>Context property key holding the version the datasource was at before this run.</summary>
        public const string MigratedFromKey = "VersionGate.From";
        /// <summary>Context property key holding the version the datasource is at after this run.</summary>
        public const string MigratedToKey = "VersionGate.To";

        private readonly VersionGateStepOptions _opts;
        private readonly ILogger<VersionGateStep> _logger;

        public VersionGateStep(VersionGateStepOptions opts, ILogger<VersionGateStep> logger = null)
        {
            _opts = opts ?? new VersionGateStepOptions();
            _logger = logger;
        }

        public string StepId => "version-gate";
        public string StepName => "Check Version & Migrate";
        public string Description =>
            "Compares the app's declared/entity version against the database and applies pending migrations.";
        public IReadOnlyList<string> DependsOn => Array.Empty<string>();
        public Security.SetupPermission RequiredPermission => Security.SetupPermission.ApplySchema;

        public bool CanSkip(SetupContext context)
        {
            // Explicit opt-out or turned off entirely.
            if (context?.Options?.MigrateOnStartup == false) return true;
            if (context?.Options?.SkipSchema == true) return true;
            return false;
        }

        public IErrorsInfo Validate(SetupContext context)
        {
            if (context?.Editor == null)
                return Fail("SetupContext.Editor is required.");

            var dsName = ResolveDatasourceName(context);
            if (string.IsNullOrWhiteSpace(dsName))
                return Fail("VersionGateStep needs a datasource: set VersionGateStepOptions.DatasourceName or SetupContext.DataSource.");

            if (ResolveEntityTypes(context).Count == 0)
                return Fail("VersionGateStep found no entity types. Set VersionGateStepOptions.EntityTypes, " +
                            "SetupOptions.EntityTypeNames, or SetupOptions.EntityAssemblies.");
            return Ok();
        }

        public IErrorsInfo Execute(SetupContext context, System.IProgress<PassedArgs> progress = null)
        {
            var editor = context.Editor;
            var dsName = ResolveDatasourceName(context);
            var types = ResolveEntityTypes(context);
            if (types.Count == 0)
                return Fail("No entity types to gate on.");

            var ds = context.DataSource ?? editor.GetDataSource(dsName);
            if (ds == null)
                return Fail($"Datasource '{dsName}' could not be resolved.");

            string declared = ResolveDeclaredVersion(context, types);

            var store = new DbSchemaVersionStore(editor);
            var recorded = store.Read(dsName);

            StepErrorHelpers.Report(progress, 10, "Building migration plan…");
            var plan = new MigrationManager(editor, ds).BuildMigrationPlanForTypes(types, _opts.DetectRelationships);
            int pending = plan?.PendingOperationCount ?? 0;

            bool versionAhead = declared != null && recorded != null && SemVer.Compare(declared, recorded.VersionString) > 0;
            bool versionedButUnversionedDb = declared != null && recorded == null;
            bool needMigrate = pending > 0 || versionAhead || versionedButUnversionedDb;

            var fromLabel = recorded?.VersionString ?? "(unversioned)";
            context.Properties[MigratedFromKey] = fromLabel;

            if (!needMigrate)
            {
                context.Properties[MigratedToKey] = fromLabel;
                StepErrorHelpers.Report(progress, 100, "Database is up to date.");
                return Ok($"Up to date at v{fromLabel} on '{dsName}' — no migration needed.");
            }

            if (context.Options?.DryRun == true)
            {
                context.Properties[MigratedToKey] = fromLabel;
                var why = pending > 0 ? $"{pending} pending operation(s)" : "declared version ahead of database";
                StepErrorHelpers.Report(progress, 100, $"Dry-run: would migrate '{dsName}' ({why}).");
                return Ok($"Dry-run: '{dsName}' needs migration ({why}); no changes applied.");
            }

            StepErrorHelpers.Report(progress, 40, $"Migrating '{dsName}'…");
            var tracking = new MigrationTrackingService(editor);
            var result = tracking.ExecuteTrackedMigration(
                dsName, types, detectRelationships: _opts.DetectRelationships,
                progress: progress, declaredVersion: declared);

            if (result == null || result.Flag == Errors.Failed)
                return Fail($"Migration failed for '{dsName}': {result?.Message}");

            var after = store.Read(dsName);
            var toLabel = after?.VersionString ?? declared ?? fromLabel;
            context.Properties[MigratedToKey] = toLabel;

            StepErrorHelpers.Report(progress, 100, $"Migrated '{dsName}' {fromLabel} → {toLabel}.");
            return Ok($"Migrated '{dsName}' {fromLabel} → {toLabel}.");
        }

        // ── Resolution helpers ──────────────────────────────────────────────

        private string ResolveDatasourceName(SetupContext context)
        {
            if (!string.IsNullOrWhiteSpace(_opts.DatasourceName)) return _opts.DatasourceName;
            return context?.DataSource?.DatasourceName;
        }

        private IReadOnlyList<Type> ResolveEntityTypes(SetupContext context)
        {
            if (_opts.EntityTypes?.Count > 0) return _opts.EntityTypes;

            var handler = context?.Editor?.assemblyHandler;

            var names = context?.Options?.EntityTypeNames;
            if (names != null && names.Count > 0)
            {
                var resolved = new List<Type>();
                foreach (var n in names)
                {
                    if (string.IsNullOrWhiteSpace(n)) continue;
                    var t = handler?.GetType(n) ?? Type.GetType(n, throwOnError: false);
                    if (t != null) resolved.Add(t);
                }
                if (resolved.Count > 0) return resolved;
            }

            var assemblies = context?.Options?.EntityAssemblies;
            if (assemblies != null && assemblies.Count > 0)
            {
                var resolved = new List<Type>();
                foreach (var an in assemblies)
                {
                    var asm = LoadAssembly(an);
                    if (asm == null) continue;
                    resolved.AddRange(SafeExportedTypes(asm).Where(t => t.IsClass && !t.IsAbstract));
                }
                if (resolved.Count > 0) return resolved;
            }

            return Array.Empty<Type>();
        }

        private string ResolveDeclaredVersion(SetupContext context, IReadOnlyList<Type> types)
        {
            if (!string.IsNullOrWhiteSpace(_opts.DeclaredVersion)) return _opts.DeclaredVersion;
            if (!string.IsNullOrWhiteSpace(context?.Options?.DeclaredSchemaVersion))
                return context.Options.DeclaredSchemaVersion;

            // Fall back to an [AppSchemaVersion] attribute on the entity assembly.
            foreach (var asm in types.Select(t => t.Assembly).Distinct())
            {
                var attr = asm.GetCustomAttribute<AppSchemaVersionAttribute>();
                if (attr != null) return attr.Version;
            }
            return null; // diff-only mode — never blocks a needed migration
        }

        private Assembly LoadAssembly(string nameOrPath)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(nameOrPath) && nameOrPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                    && System.IO.File.Exists(nameOrPath))
                    return Assembly.LoadFrom(nameOrPath);
                return Assembly.Load(nameOrPath);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning("VersionGateStep could not load assembly '{Name}': {Msg}", nameOrPath, ex.Message);
                return null;
            }
        }

        private static IEnumerable<Type> SafeExportedTypes(Assembly asm)
        {
            try { return asm.GetExportedTypes(); }
            catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t != null); }
            catch { return Array.Empty<Type>(); }
        }
    }
}
