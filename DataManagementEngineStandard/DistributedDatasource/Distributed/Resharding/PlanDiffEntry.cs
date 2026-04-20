using System;
using TheTechIdea.Beep.Distributed.Plan;

namespace TheTechIdea.Beep.Distributed.Resharding
{
    /// <summary>
    /// Single migration step produced by <see cref="PlanDiff.Compute"/>.
    /// Captures the before/after placements plus a classification so
    /// <see cref="ReshardingService"/> can dispatch to the right
    /// primitive.
    /// </summary>
    public sealed class PlanDiffEntry
    {
        /// <summary>Initialises a new diff entry.</summary>
        public PlanDiffEntry(
            string           entityName,
            PlanDiffKind     kind,
            EntityPlacement  oldPlacement,
            EntityPlacement  newPlacement)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                throw new ArgumentException("Entity name required.", nameof(entityName));

            EntityName   = entityName;
            Kind         = kind;
            OldPlacement = oldPlacement;
            NewPlacement = newPlacement;
        }

        /// <summary>Entity the step applies to.</summary>
        public string EntityName { get; }

        /// <summary>Classification of the step.</summary>
        public PlanDiffKind Kind { get; }

        /// <summary>Placement from the source plan; <c>null</c> for <see cref="PlanDiffKind.AddEntity"/>.</summary>
        public EntityPlacement OldPlacement { get; }

        /// <summary>Placement from the target plan; <c>null</c> for <see cref="PlanDiffKind.RemoveEntity"/>.</summary>
        public EntityPlacement NewPlacement { get; }

        /// <inheritdoc/>
        public override string ToString()
            => $"PlanDiffEntry({EntityName}, {Kind}, old={OldPlacement?.ToString() ?? "-"}, new={NewPlacement?.ToString() ?? "-"})";
    }
}
