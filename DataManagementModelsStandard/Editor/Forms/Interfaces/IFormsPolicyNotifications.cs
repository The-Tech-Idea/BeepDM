using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.Forms.Models
{
    /// <summary>Immutable policy publication evidence; carries no principal or record values.</summary>
    public sealed class SecurityPolicyChangedEventArgs : EventArgs
    {
        public long Revision { get; }
        public long ReadAuthorizationRevision { get; }
        public SecurityPolicyChangedEventArgs(long revision, long readAuthorizationRevision)
        { Revision = revision; ReadAuthorizationRevision = readAuthorizationRevision; }
    }
}

namespace TheTechIdea.Beep.Editor.UOWManager.Interfaces
{
    /// <summary>Optional helper notification. Publish outside policy monitors and isolate observer failures.</summary>
    public interface IObservableSecurityPolicy
    {
        event EventHandler<Forms.Models.SecurityPolicyChangedEventArgs> SecurityPolicyChanged;
        IReadOnlyList<Exception> PolicyNotificationFailures { get; }
    }

    /// <summary>Optional manager feed for host-owned, revision-checked UI reconciliation.</summary>
    public interface IFormsPolicyNotifications : IObservableSecurityPolicy
    {
        long SecurityPolicyRevision { get; }
    }
}
