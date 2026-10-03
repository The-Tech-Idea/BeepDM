using System;

namespace TheTechIdea.Beep.Editor.UOWManager.Interfaces
{
    /// <summary>Optional revision gate for publishing an already prepared query's owned state.</summary>
    public interface IQuerySecurityPublication : IQuerySecuritySnapshotProvider
    {
        /// <summary>
        /// Reject a changed revision without invoking the action. The action must only publish owned
        /// memory; user triggers, providers, metadata getters and notifications belong outside the gate.
        /// </summary>
        bool TryPublishQuery(long revision, Action publishOwnedState);
    }
}
