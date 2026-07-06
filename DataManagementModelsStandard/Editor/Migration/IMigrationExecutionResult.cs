namespace TheTechIdea.Beep.Editor.Migration
{
    /// <summary>
    /// Contract for the migration execution result stored on
    /// <c>SetupContext.MigrationResult</c>. The full implementation
    /// <c>MigrationExecutionResult</c> lives in the engine project and implements this
    /// marker interface so the models project can hold a typed reference without
    /// taking a dependency on engine-only types.
    /// </summary>
    public interface IMigrationExecutionResult
    {
        bool Success { get; }
        string Message { get; }
        string ExecutionToken { get; }
    }
}