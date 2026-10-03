using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Editor.UOWManager.Models;

namespace TheTechIdea.Beep.Editor.Forms.Models
{
    public enum FormLovState { Unattempted, Completed, Cancelled, Superseded, Failed }

    /// <summary>Captured manager result; helper cache/events and arbitrary setters are not transactional.</summary>
    public sealed class FormLovResult : LOVResult
    {
        public FormLovState State { get; internal set; }
        public Guid FormInstanceId { get; internal set; }
        public Guid RegistrationId { get; internal set; }
        public string BlockName { get; internal set; }
        public string FieldName { get; internal set; }
        public long RequestRevision { get; internal set; }
        public bool LoadAcknowledged { get; internal set; }
        public bool HelperEffectsPossible { get; internal set; }
        public bool SelectionEffectsPossible { get; internal set; }
        public bool SelectionApplied { get; internal set; }
        public IReadOnlyList<string> AppliedFields { get; internal set; } = Array.Empty<string>();
        public Exception Exception { get; internal set; }
    }
}
