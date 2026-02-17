


namespace TheTechIdea.Beep.Editor
{
    /// <summary>
    /// Represents the state of an entity within an ObservableBindingList's change tracker.
    /// </summary>
    public enum EntityState
    {
        /// <summary>The entity has been added but not yet persisted.</summary>
        Added,
        /// <summary>One or more properties have been modified since the last accept.</summary>
        Modified,
        /// <summary>The entity has been marked for deletion.</summary>
        Deleted,
        /// <summary>The entity has no pending changes.</summary>
        Unchanged,
        /// <summary>The entity has been removed from tracking without being persisted.</summary>
        Detached
    }
}
