using System;
using System.Linq;
using TheTechIdea.Beep.Workflow;

namespace TheTechIdea.Beep.Workflows.Engine
{
    /// <summary>
    /// Converts existing <see cref="IWorkFlow"/> / <see cref="WorkFlow"/> instances
    /// to <see cref="WorkFlowDefinition"/> so they can be executed by
    /// <see cref="WorkFlowEngine"/> without modifying the legacy model.
    /// </summary>
    public static class WorkFlowMigration
    {
        /// <summary>
        /// Converts a legacy <see cref="IWorkFlow"/> to a <see cref="WorkFlowDefinition"/>.
        /// Each legacy step becomes a <see cref="WorkFlowStepDef"/> of kind
        /// <see cref="StepActionKind.Script"/> (the most general equivalent);
        /// a caller can adjust per step after the fact.
        /// Sequential <see cref="StepConnection"/> edges are created between consecutive steps.
        /// </summary>
        public static WorkFlowDefinition FromLegacy(IWorkFlow wf)
        {
            ArgumentNullException.ThrowIfNull(wf);

            var def = new WorkFlowDefinition
            {
                Id          = (wf as WorkFlow)?.GuidID ?? Guid.NewGuid().ToString(),
                Name        = wf.DataWorkFlowName ?? "Migrated Workflow",
                Description = wf.Description ?? string.Empty
            };

            int seq = 0;
            WorkFlowStepDef? prev = null;

            foreach (var step in wf.Datasteps ?? Enumerable.Empty<IWorkFlowStep>())
            {
                var stepDef = new WorkFlowStepDef
                {
                    ID          = !string.IsNullOrWhiteSpace(step.ID) ? step.ID : Guid.NewGuid().ToString(),
                    Name        = step.Name        ?? $"Step_{seq}",
                    Description = step.Description ?? string.Empty,
                    Seq         = seq++,
                    Kind        = StepActionKind.Script,
                    ScriptBody  = step.Code,
                    StepType    = step.StepType
                };

                def.Steps.Add(stepDef);

                if (prev != null)
                    def.Connections.Add(new StepConnection
                    {
                        FromStepId = prev.ID,
                        ToStepId   = stepDef.ID
                    });

                prev = stepDef;
            }

            return def;
        }
    }
}
