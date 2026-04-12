using System;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>Event args raised when any tracked field in any registered block changes.</summary>
    public class BlockFieldChangedEventArgs : EventArgs
    {
        /// <summary>Gets or sets the block where the change occurred.</summary>
        public string BlockName   { get; set; }

        /// <summary>Gets or sets the field that changed.</summary>
        public string FieldName   { get; set; }

        /// <summary>Gets or sets the previous field value.</summary>
        public object OldValue    { get; set; }

        /// <summary>Gets or sets the new field value.</summary>
        public object NewValue    { get; set; }

        /// <summary>Gets or sets the record index affected by the change.</summary>
        public int    RecordIndex { get; set; }
    }
}
