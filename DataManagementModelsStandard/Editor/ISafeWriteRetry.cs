namespace TheTechIdea.Beep.Editor
{
    /// <summary>
    /// Optional provider/UoW failure guarantee: no durable effects, unresolved transaction
    /// or accepted tracking. Text messages alone do not establish safe replay.
    /// </summary>
    public interface ISafeWriteRetry
    {
        bool IsSafeToRetry { get; }
    }
}
