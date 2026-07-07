// Example 01 — Plan and Policy
//
// Use explicit entity types when schema ownership is known.

using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Migration;

namespace TheTechIdea.Beep.Editor.Migration.Examples
{
    /// <summary>
    /// Phase 1 — build a plan from a known set of CLR types, then run the policy
    /// evaluator to detect destructive changes.
    /// </summary>
    public static class Example01_PlanAndPolicy
    {
        public static MigrationPolicyEvaluation Run(IDMEEditor editor, IDataSource dataSource, IEnumerable<Type> entityTypes)
        {
            if (editor == null) throw new ArgumentNullException(nameof(editor));
            if (dataSource == null) throw new ArgumentNullException(nameof(dataSource));
            if (entityTypes == null) throw new ArgumentNullException(nameof(entityTypes));

            var migrationManager = new MigrationManager(editor, dataSource);

            // ── 1. Build the plan from the entity types ──────────────────────────
            MigrationPlanArtifact plan = migrationManager.BuildMigrationPlanForTypes(
                entityTypes,
                detectRelationships: true);

            Console.WriteLine(
                $"Plan {plan.PlanId}: {plan.Operations.Count} operations, " +
                $"entityCount={plan.EntityTypeCount}");

            // ── 2. Run the policy evaluator against a Staging policy ────────────
            var policyOptions = new MigrationPolicyOptions
            {
                EnvironmentTier = MigrationEnvironmentTier.Staging,
                RequireApprovalForHighRisk = true,
                RequireApprovalForCriticalRisk = true,
                BlockDestructiveInProtectedEnvironments = true
            };

            MigrationPolicyEvaluation policy = migrationManager.EvaluateMigrationPlanPolicy(plan, policyOptions);

            if (policy.Decision == MigrationPolicyDecision.Block)
            {
                foreach (var finding in policy.Findings)
                {
                    Console.WriteLine($"{finding.RuleId}: {finding.Decision} - {finding.Message}");
                }
                throw new InvalidOperationException("Migration plan blocked by policy.");
            }

            return policy;
        }
    }
}
