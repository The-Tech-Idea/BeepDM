namespace TheTechIdea.Beep.Editor
{
    /// <summary>
    /// Optional canonical revision: each observed cursor/item/structural change advances once,
    /// before callbacks. Repeated audit delivery of that same item change does not advance it.
    /// A supported but retired source returns false, never downgrades its support identity.
    /// This is not a mutation lock
    /// or a guarantee that arbitrary external edits produce notifications.
    /// </summary>
    public interface IUnitofWorkRecordRevision
    {
        bool SupportsRecordRevision { get; }
        bool TryGetRecordRevision(out long revision);
    }
}
