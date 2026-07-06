namespace TheTechIdea.Beep.Editor.Migration
{
    /// <summary>
    /// Contract for the migration plan artifact stored on <c>SetupContext.MigrationPlan</c>.
    /// The full implementation <c>MigrationPlanArtifact</c> lives in the engine project
    /// (<c>TheTechIdea.Beep.Editor.Migration.MigrationPlanArtifact</c>) and implements
    /// this marker interface so the models project can hold a typed reference without
    /// taking a dependency on engine-only types.
    /// </summary>
    /// <remarks>
    /// All members are opaque read-only handles from the model side. Engine code reads
    /// or computes everything; consumers such as the Setup wizard only need to know that
    /// a plan exists and survives across steps.
    /// </remarks>
    public interface IMigrationPlanArtifact
    {
        string PlanId { get; }
    }
}