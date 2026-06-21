using System;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Event args for item property changes
    /// </summary>
    public class ItemPropertyChangedEventArgs : EventArgs
    {
        /// <summary>Block containing the item</summary>
        public string BlockName { get; set; }
        
        /// <summary>Item name</summary>
        public string ItemName { get; set; }
        
        /// <summary>Property name that changed</summary>
        public string PropertyName { get; set; }
        
        /// <summary>Previous property value</summary>
        public object OldValue { get; set; }
        
        /// <summary>New property value</summary>
        public object NewValue { get; set; }
        
        /// <summary>Timestamp of change</summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
        
        /// <summary>Whether the change should be cancelled (set by event handler)</summary>
        public bool Cancel { get; set; }
    }
    
    /// <summary>
    /// Event args for item value changes
    /// </summary>
    public class ItemValueChangedEventArgs : EventArgs
    {
        /// <summary>Block containing the item</summary>
        public string BlockName { get; set; }
        
        /// <summary>Item name</summary>
        public string ItemName { get; set; }
        
        /// <summary>Previous value</summary>
        public object OldValue { get; set; }
        
        /// <summary>New value</summary>
        public object NewValue { get; set; }
        
        /// <summary>Whether item is now dirty</summary>
        public bool IsDirty { get; set; }
        
        /// <summary>Record index (row number)</summary>
        public int RecordIndex { get; set; }
        
        /// <summary>Timestamp of change</summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
        
        /// <summary>Whether the change should be cancelled (set by event handler)</summary>
        public bool Cancel { get; set; }
    }
    
    /// <summary>
    /// Event args for item error state changes
    /// </summary>
    public class ItemErrorEventArgs : EventArgs
    {
        /// <summary>Block containing the item</summary>
        public string BlockName { get; set; }
        
        /// <summary>Item name</summary>
        public string ItemName { get; set; }
        
        /// <summary>Whether item has error</summary>
        public bool HasError { get; set; }
        
        /// <summary>Error message</summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>Validation rule that failed (if applicable)</summary>
        public string ValidationRuleName { get; set; }
        
        /// <summary>Timestamp of change</summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
    
    /// <summary>
    /// Event args for item navigation changes
    /// </summary>
    public class ItemNavigationEventArgs : EventArgs
    {
        /// <summary>Block name</summary>
        public string BlockName { get; set; }
        
        /// <summary>Item navigating from</summary>
        public string FromItem { get; set; }
        
        /// <summary>Item navigating to</summary>
        public string ToItem { get; set; }
        
        /// <summary>Navigation direction</summary>
        public NavigationDirection Direction { get; set; }
        
        /// <summary>Whether navigation should be cancelled</summary>
        public bool Cancel { get; set; }
    }
    
    /// <summary>
    /// Navigation direction
    /// </summary>
    public enum NavigationDirection
    {
        /// <summary>Moving forward (tab, enter)</summary>
        Forward,
        
        /// <summary>Moving backward (shift+tab)</summary>
        Backward,
        
        /// <summary>Moving up (arrow key, previous record)</summary>
        Up,
        
        /// <summary>Moving down (arrow key, next record)</summary>
        Down,
        
        /// <summary>Explicit navigation (GO_ITEM)</summary>
        Explicit
    }
}
