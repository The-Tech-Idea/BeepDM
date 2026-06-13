using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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

        public SchemaSetupStep(
            SchemaSetupStepOptions opts,
            ILogger<SchemaSetupStep>? logger = null,
            ISchemaManager? schemaManager = null,
            IEntityDiscoveryService? discovery = null)
        {
            _opts = opts ?? throw new ArgumentNullException(nameof(opts));
            _logger = logger;
            _schemaManager = schemaManager;
            _discovery = discovery;
        }

        // ── ISetupStep ───────────────────────────────────────────────────────

        public string StepId => "schema-setup";
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
            if (_opts.EntityTypes == null || _opts.EntityTypes.Count == 0) return false;

            var currentHash = ComputeEntityListHash(_opts.EntityTypes);
            return context.State.SchemaHash == currentHash;
        }

        public IErrorsInfo Validate(SetupContext context)
        {
            if (context?.Editor == null)
                return Fail("SetupContext.Editor is required.");

            if (context.DataSource == null || context.DataSource.ConnectionStatus != ConnectionState.Open)
                return Fail("DataSource must be open before SchemaSetupStep. " +
                            "Ensure ConnectionConfigStep ran successfully.");

            if (_opts.EntityTypes == null || _opts.EntityTypes.Count == 0)
                return Fail("SchemaSetupStepOptions.EntityTypes must contain at least one type.");

            return Ok();
        }

        public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs> progress = null)
        {
            var editor = context.Editor;
            var ds = context.DataSource;
            bool strict = _opts.StrictPolicyMode ?? context.Options?.StrictPolicyMode ?? false;
            string environment = context.Options?.Environment ?? "Development";

            // Auto-discover entity types when none were explicitly provided
            if (_discovery != null && (_opts.EntityTypes == null || _opts.EntityTypes.Count == 0))
            {
                var discovered = _discovery.Discover(new EntityDiscoveryOptions
                {
                    Scope = DiscoveryScope.AllLoaded,
                    ExcludeAbstract = true,
                    ExcludeOpenGenerics = true
                });
                _opts.EntityTypes = discovered
                    .Select(e => Type.GetType(e.FullName, throwOnError: false))
                    .Where(t => t != null)
                    .Cast<Type>()
                    .ToList();
                _logger?.LogInformation("[SchemaSetupStep] Auto-discovered {Count} entity types", _opts.EntityTypes.Count);
            }

            var migration = new MigrationManager(editor, ds);

            // Register extra assemblies when provided
            if (_opts.ExtraAssemblies != null)
            {
                foreach (var asm in _opts.ExtraAssemblies)
                    migration.RegisterAssembly(asm);
            }

            // ── A. Build migration plan ──────────────────────────────────────
            StepErrorHelpers.Report(progress, 5, "Building migration plan…");
            var plan = migration.BuildMigrationPlanForTypes(
                _opts.EntityTypes,
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
                context.Properties["DryRunReportJson"] = JsonSerializer.Serialize(dryRun);

            // ── C2. Per-entity schema drift (.NET class vs live DB) ───
            try
            {
                var schema = _schemaManager ?? new SchemaManager(editor);
                var drift = schema.InspectManyAsync(_opts.EntityTypes, ds).GetAwaiter().GetResult();
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
                context.Properties["CompensationPlanJson"] = JsonSerializer.Serialize(compensationPlan);
            var rollbackReadiness = migration.CheckRollbackReadiness(
                plan,
                backupConfirmed: !strict,       // non-strict: optimistic; strict: require confirmation
                restoreTestEvidenceProvided: false);

            if (!rollbackReadiness.IsReady && strict)
                return Fail("Rollback readiness check failed in StrictPolicyMode. " +
                             "Confirm backup and restore-test evidence before proceeding.");

            // ── G. Approve plan ──────────────────────────────────────────────
            StepErrorHelpers.Report(progress, 65, "Approving migration plan…");
            plan = migration.ApproveMigrationPlan(
                plan,
                approvedBy: _opts.ApproverLabel,
                notes: $"Auto-approved by setup wizard. Environment={environment}");

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
            var hash = ComputeEntityListHash(_opts.EntityTypes);
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
