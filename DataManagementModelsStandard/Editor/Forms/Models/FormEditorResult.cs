using System;

namespace TheTechIdea.Beep.Editor.Forms.Models
{
    public enum FormEditorState { Unattempted, Completed, Cancelled, Superseded, Denied, Failed }

    /// <summary>Popup acknowledgement and setter evidence are distinct from accepted completion.</summary>
    public sealed class FormEditorResult
    {
        public FormEditorState State { get; internal set; }
        public Guid FormInstanceId { get; internal set; }
        public Guid RegistrationId { get; internal set; }
        public string BlockName { get; internal set; }
        public string FieldName { get; internal set; }
        public long RequestRevision { get; internal set; }
        public bool ProviderInvoked { get; internal set; }
        public bool ProviderAcknowledged { get; internal set; }
        public bool ProviderCommitted { get; internal set; }
        public bool WriteEffectsPossible { get; internal set; }
        public bool WriteAcknowledged { get; internal set; }
        public bool Committed => State == FormEditorState.Completed;
        /// <summary>Accepted text; withheld for rejected completion, even after possible effects.</summary>
        public string Value { get; internal set; }
        public string ErrorMessage { get; internal set; }
        public Exception Exception { get; internal set; }
    }
}
