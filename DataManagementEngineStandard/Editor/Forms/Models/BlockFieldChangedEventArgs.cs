using System;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>Event args raised when any tracked field in any registered block changes.</summary>
    public class BlockFieldChangedEventArgs : EventArgs
    {
        public string BlockName   { get; set; }
        public string FieldName   { get; set; }
        public object OldValue    { get; set; }
        public object NewValue    { get; set; }
        public int    RecordIndex { get; set; }
    }
}
