using System;
using System.Collections.Generic;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Editor.Forms.Models
{
    public enum FormQueryState { Unattempted, Completed, BlockedDirty, Cancelled, Superseded, Denied, Failed, PageOutOfRange }

    /// <summary>Read publication evidence, not database durability or permission to replay a write.</summary>
    public sealed class FormQueryResult : ErrorsInfo
    {
        public Guid FormInstanceId { get; internal set; }
        public Guid RegistrationId { get; internal set; }
        public string BlockName { get; internal set; }
        /// <summary>Monotonic within RegistrationId, not across replacements or independent forms.</summary>
        public long RequestRevision { get; internal set; }
        public FormQueryState State { get; internal set; }
        public bool UsedStaging { get; internal set; }
        public bool RecordsPublished { get; internal set; }
        /// <summary>Validated provider observation only. Check RecordsPublished before displaying an accepted page.</summary>
        public ProviderPageInfo ProviderPage { get; internal set; }
        /// <summary>Legacy Get was invoked and can have published before late rejection.</summary>
        public bool LegacyPublicationPossible { get; internal set; }
        public bool ReadAcknowledged => State == FormQueryState.Completed;
        public IReadOnlyList<Exception> NotificationFailures { get; internal set; } = Array.Empty<Exception>();
    }
}
