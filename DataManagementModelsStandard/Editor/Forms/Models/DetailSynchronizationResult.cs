using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Editor.Forms.Models
{
    public enum DetailSynchronizationState
    {
        Unattempted, Refreshed, Cleared, Deferred, BlockedDirty, Superseded, Missing, Failed
    }

    public sealed class DetailBlockSynchronizationResult
    {
        public Guid FormInstanceId { get; internal set; }
        public string BlockName { get; internal set; }
        public DetailSynchronizationState State { get; internal set; }
        public string Message { get; internal set; }
        /// <summary>Legacy Get was invoked, or a staged read published. Not proof of database writes.</summary>
        public bool ProviderMayHavePublished { get; internal set; }
        /// <summary>True only when the prepared read confirmed its live-record publication.</summary>
        public bool RecordsPublished { get; internal set; }
        public IReadOnlyList<Exception> NotificationFailures { get; internal set; } = Array.Empty<Exception>();
    }

    /// <summary>Captured detail outcomes with capability-dependent publication evidence, not a database transaction.</summary>
    public sealed class DetailSynchronizationResult : ErrorsInfo
    {
        public IReadOnlyList<DetailBlockSynchronizationResult> Details { get; internal set; } =
            Array.Empty<DetailBlockSynchronizationResult>();
        public bool AllDetailsCurrent => Details.Count > 0 && Details.All(d =>
            d.State == DetailSynchronizationState.Refreshed || d.State == DetailSynchronizationState.Cleared);
    }
}
