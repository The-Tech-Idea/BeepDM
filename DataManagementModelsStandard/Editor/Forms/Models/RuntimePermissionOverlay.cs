using System;
using System.Threading;

namespace TheTechIdea.Beep.Editor.UOWManager.Models;

// Publish only owned memory. Application configuration remains in the model's setters.
internal sealed class RuntimePermissionOverlay
{
    internal readonly Guid Owner;
    internal readonly long Revision;
    internal readonly bool Query, Insert, Update, Delete, Enabled, Visible;

    internal RuntimePermissionOverlay(Guid owner, long revision, bool query, bool insert, bool update,
        bool delete, bool enabled, bool visible)
    { Owner = owner; Revision = revision; Query = query; Insert = insert; Update = update;
        Delete = delete; Enabled = enabled; Visible = visible; }

    internal static void Publish(ref RuntimePermissionOverlay slot, RuntimePermissionOverlay next)
    {
        while (true)
        {
            var prior = Volatile.Read(ref slot);
            if (prior != null && (prior.Owner != next.Owner || prior.Revision > next.Revision))
                throw new InvalidOperationException("Permission overlay belongs to another registration or newer policy.");
            if (ReferenceEquals(Interlocked.CompareExchange(ref slot, next, prior), prior)) return;
        }
    }
}
