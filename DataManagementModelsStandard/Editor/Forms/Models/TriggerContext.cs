using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;

namespace TheTechIdea.Beep.Editor.Forms.Models
{
    /// <summary>
    /// Provides context information to trigger handlers during execution.
    /// Oracle Forms equivalent: System variables and trigger context available at runtime.
    /// </summary>
    public class TriggerContext
    {
        #region Trigger Information
        
        /// <summary>The trigger being executed</summary>
        public TriggerDefinition Trigger { get; set; }
        
        /// <summary>Type of trigger being fired</summary>
        public TriggerType TriggerType { get; set; }
        
        /// <summary>Scope of the trigger</summary>
        public TriggerScope Scope { get; set; }
        
        #endregion
        
        #region Location Context
        
        /// <summary>Current form name</summary>
        public string FormName { get; set; }
        
        /// <summary>Current block name</summary>
        public string BlockName { get; set; }
        
        /// <summary>Current item/field name</summary>
        public string ItemName { get; set; }
        
        /// <summary>Current record index (0-based)</summary>
        public int RecordIndex { get; set; } = -1;
        
        #endregion
        
        #region Record Data
        
        /// <summary>Current record object</summary>
        public object CurrentRecord { get; set; }
        
        /// <summary>Previous record (for navigation triggers)</summary>
        public object PreviousRecord { get; set; }
        
        /// <summary>New record being created (for insert triggers)</summary>
        public object NewRecord { get; set; }
        
        /// <summary>Whether current record is new (not yet saved)</summary>
        public bool IsNewRecord { get; set; }
        
        /// <summary>Whether current record is modified</summary>
        public bool IsModified { get; set; }
        
        #endregion
        
        #region Field/Item Data
        
        /// <summary>Old value (for validation/change triggers)</summary>
        public object OldValue { get; set; }
        
        /// <summary>New/current value</summary>
        public object NewValue { get; set; }
        
        /// <summary>Data type of the field</summary>
        public Type FieldType { get; set; }
        
        #endregion
        
        #region Navigation Context
        
        /// <summary>Source block (for navigation triggers)</summary>
        public string SourceBlock { get; set; }
        
        /// <summary>Target block (for navigation triggers)</summary>
        public string TargetBlock { get; set; }
        
        /// <summary>Source item (for navigation triggers)</summary>
        public string SourceItem { get; set; }
        
        /// <summary>Target item (for navigation triggers)</summary>
        public string TargetItem { get; set; }
        
        /// <summary>Navigation direction</summary>
        public NavigationDirection NavigationDirection { get; set; }
        
        #endregion
        
        #region Query Context
        
        /// <summary>Query filters being applied</summary>
        public List<AppFilter> QueryFilters { get; set; }
        
        /// <summary>Number of records fetched</summary>
        public int RecordsFetched { get; set; }
        
        /// <summary>Total records available</summary>
        public int TotalRecords { get; set; }
        
        #endregion
        
        #region Error Context
        
        /// <summary>Error message (for error triggers)</summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>Error code (for error triggers)</summary>
        public int ErrorCode { get; set; }
        
        /// <summary>Exception that occurred</summary>
        public Exception Exception { get; set; }
        
        #endregion
        
        #region Key/Mouse Context
        
        /// <summary>Key code (for key triggers)</summary>
        public int KeyCode { get; set; }
        
        /// <summary>Key modifiers (Ctrl, Alt, Shift)</summary>
        public KeyModifiers Modifiers { get; set; }
        
        /// <summary>Mouse X position</summary>
        public int MouseX { get; set; }
        
        /// <summary>Mouse Y position</summary>
        public int MouseY { get; set; }
        
        /// <summary>Mouse button clicked</summary>
        public MouseButton MouseButton { get; set; }
        
        #endregion
        
        #region Timer Context
        
        /// <summary>Timer name (for timer triggers)</summary>
        public string TimerName { get; set; }
        
        /// <summary>Timer elapsed time</summary>
        public TimeSpan TimerElapsed { get; set; }
        
        #endregion
        
        #region Custom Context
        
        /// <summary>Custom parameters passed to trigger</summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        
        /// <summary>Custom user data</summary>
        public object Tag { get; set; }
        
        #endregion
        
        #region Execution Control
        
        /// <summary>
        /// Whether to cancel the triggering action.
        /// Setting to true is equivalent to RAISE FORM_TRIGGER_FAILURE in Oracle Forms.
        /// </summary>
        public bool Cancel { get; set; }
        
        /// <summary>
        /// Message to display if cancelled
        /// </summary>
        public string CancelMessage { get; set; }
        
        /// <summary>
        /// Output message from trigger execution
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// Whether to skip remaining triggers in chain
        /// </summary>
        public bool SkipRemainingTriggers { get; set; }
        
        #endregion
        
        #region Services
        
        /// <summary>Reference to DME Editor for data operations</summary>
        public IDMEEditor Editor { get; set; }
        
        /// <summary>Reference to FormsManager</summary>
        public IUnitofWorksManager FormsManager { get; set; }
        
        /// <summary>Reference to System Variables</summary>
        public ISystemVariablesManager SystemVariables { get; set; }
        
        /// <summary>Reference to Validation Manager</summary>
        public IValidationManager ValidationManager { get; set; }
        
        /// <summary>Reference to LOV Manager</summary>
        public ILOVManager LOVManager { get; set; }
        
        /// <summary>Reference to Item Property Manager</summary>
        public IItemPropertyManager ItemPropertyManager { get; set; }
        
        #endregion
        
        #region Timestamp
        
        /// <summary>When the trigger was fired</summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
        
        #endregion
        
        #region Methods
        
        /// <summary>
        /// Get a parameter value with type conversion
        /// </summary>
        public T GetParameter<T>(string name, T defaultValue = default)
        {
            if (Parameters != null && Parameters.TryGetValue(name, out var value))
            {
                if (value is T typed)
                    return typed;
                
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }
        
        /// <summary>
        /// Set a parameter value
        /// </summary>
        public void SetParameter(string name, object value)
        {
            Parameters ??= new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            Parameters[name] = value;
        }
        
        /// <summary>
        /// Raise form trigger failure (cancel with message)
        /// </summary>
        public void RaiseFormTriggerFailure(string message = null)
        {
            Cancel = true;
            CancelMessage = message ?? "Form trigger failure";
        }
        
        /// <summary>
        /// Clone this context
        /// </summary>
        public TriggerContext Clone()
        {
            return new TriggerContext
            {
                Trigger = Trigger,
                TriggerType = TriggerType,
                Scope = Scope,
                FormName = FormName,
                BlockName = BlockName,
                ItemName = ItemName,
                RecordIndex = RecordIndex,
                CurrentRecord = CurrentRecord,
                PreviousRecord = PreviousRecord,
                NewRecord = NewRecord,
                IsNewRecord = IsNewRecord,
                IsModified = IsModified,
                OldValue = OldValue,
                NewValue = NewValue,
                FieldType = FieldType,
                SourceBlock = SourceBlock,
                TargetBlock = TargetBlock,
                SourceItem = SourceItem,
                TargetItem = TargetItem,
                NavigationDirection = NavigationDirection,
                QueryFilters = QueryFilters != null ? new List<AppFilter>(QueryFilters) : null,
                RecordsFetched = RecordsFetched,
                TotalRecords = TotalRecords,
                ErrorMessage = ErrorMessage,
                ErrorCode = ErrorCode,
                Exception = Exception,
                KeyCode = KeyCode,
                Modifiers = Modifiers,
                MouseX = MouseX,
                MouseY = MouseY,
                MouseButton = MouseButton,
                TimerName = TimerName,
                TimerElapsed = TimerElapsed,
                Parameters = Parameters != null ? new Dictionary<string, object>(Parameters, StringComparer.OrdinalIgnoreCase) : null,
                Tag = Tag,
                Cancel = Cancel,
                CancelMessage = CancelMessage,
                Message = Message,
                SkipRemainingTriggers = SkipRemainingTriggers,
                Editor = Editor,
                FormsManager = FormsManager,
                SystemVariables = SystemVariables,
                ValidationManager = ValidationManager,
                LOVManager = LOVManager,
                ItemPropertyManager = ItemPropertyManager,
                Timestamp = Timestamp
            };
        }
        
        #endregion
        
        #region Factory Methods
        
        /// <summary>
        /// Create context for form-level trigger
        /// </summary>
        public static TriggerContext ForForm(TriggerType type, string formName, IDMEEditor editor = null)
        {
            return new TriggerContext
            {
                TriggerType = type,
                Scope = TriggerScope.Form,
                FormName = formName,
                Editor = editor
            };
        }
        
        /// <summary>
        /// Create context for block-level trigger
        /// </summary>
        public static TriggerContext ForBlock(TriggerType type, string blockName, object currentRecord = null, IDMEEditor editor = null)
        {
            return new TriggerContext
            {
                TriggerType = type,
                Scope = TriggerScope.Block,
                BlockName = blockName,
                CurrentRecord = currentRecord,
                Editor = editor
            };
        }
        
        /// <summary>
        /// Create context for record-level trigger
        /// </summary>
        public static TriggerContext ForRecord(TriggerType type, string blockName, object record, int recordIndex, IDMEEditor editor = null)
        {
            return new TriggerContext
            {
                TriggerType = type,
                Scope = TriggerScope.Record,
                BlockName = blockName,
                CurrentRecord = record,
                RecordIndex = recordIndex,
                Editor = editor
            };
        }
        
        /// <summary>
        /// Create context for item-level trigger
        /// </summary>
        public static TriggerContext ForItem(TriggerType type, string blockName, string itemName, object oldValue, object newValue, IDMEEditor editor = null)
        {
            return new TriggerContext
            {
                TriggerType = type,
                Scope = TriggerScope.Item,
                BlockName = blockName,
                ItemName = itemName,
                OldValue = oldValue,
                NewValue = newValue,
                Editor = editor
            };
        }
        
        /// <summary>
        /// Create context for validation trigger
        /// </summary>
        public static TriggerContext ForValidation(string blockName, string itemName, object value, IDMEEditor editor = null)
        {
            return new TriggerContext
            {
                TriggerType = TriggerType.WhenValidateItem,
                Scope = TriggerScope.Item,
                BlockName = blockName,
                ItemName = itemName,
                NewValue = value,
                Editor = editor
            };
        }
        
        /// <summary>
        /// Create context for navigation trigger
        /// </summary>
        public static TriggerContext ForNavigation(TriggerType type, string sourceBlock, string targetBlock, NavigationDirection direction, IDMEEditor editor = null)
        {
            return new TriggerContext
            {
                TriggerType = type,
                Scope = TriggerScope.Block,
                SourceBlock = sourceBlock,
                TargetBlock = targetBlock,
                BlockName = targetBlock,
                NavigationDirection = direction,
                Editor = editor
            };
        }
        
        /// <summary>
        /// Create context for query trigger
        /// </summary>
        public static TriggerContext ForQuery(TriggerType type, string blockName, List<AppFilter> filters, IDMEEditor editor = null)
        {
            return new TriggerContext
            {
                TriggerType = type,
                Scope = TriggerScope.Block,
                BlockName = blockName,
                QueryFilters = filters,
                Editor = editor
            };
        }
        
        /// <summary>
        /// Create context for error trigger
        /// </summary>
        public static TriggerContext ForError(string errorMessage, int errorCode, Exception ex = null, IDMEEditor editor = null)
        {
            return new TriggerContext
            {
                TriggerType = TriggerType.OnError,
                Scope = TriggerScope.Form,
                ErrorMessage = errorMessage,
                ErrorCode = errorCode,
                Exception = ex,
                Editor = editor
            };
        }
        
        /// <summary>
        /// Create context for key trigger
        /// </summary>
        public static TriggerContext ForKey(TriggerType keyTrigger, int keyCode, KeyModifiers modifiers, IDMEEditor editor = null)
        {
            return new TriggerContext
            {
                TriggerType = keyTrigger,
                Scope = TriggerScope.Form,
                KeyCode = keyCode,
                Modifiers = modifiers,
                Editor = editor
            };
        }
        
        /// <summary>
        /// Create context for mouse trigger
        /// </summary>
        public static TriggerContext ForMouse(TriggerType mouseType, int x, int y, MouseButton button, IDMEEditor editor = null)
        {
            return new TriggerContext
            {
                TriggerType = mouseType,
                Scope = TriggerScope.Item,
                MouseX = x,
                MouseY = y,
                MouseButton = button,
                Editor = editor
            };
        }
        
        /// <summary>
        /// Create context for timer trigger
        /// </summary>
        public static TriggerContext ForTimer(string timerName, TimeSpan elapsed, IDMEEditor editor = null)
        {
            return new TriggerContext
            {
                TriggerType = TriggerType.WhenTimerExpired,
                Scope = TriggerScope.Form,
                TimerName = timerName,
                TimerElapsed = elapsed,
                Editor = editor
            };
        }
        
        #endregion
    }
    
    #region Supporting Enums
    
    /// <summary>
    /// Key modifier flags
    /// </summary>
    [Flags]
    public enum KeyModifiers
    {
        /// <summary>No modifier keys.</summary>
        None = 0,

        /// <summary>Shift key.</summary>
        Shift = 1,

        /// <summary>Control key.</summary>
        Control = 2,

        /// <summary>Alt key.</summary>
        Alt = 4
    }
    
    /// <summary>
    /// Mouse button
    /// </summary>
    public enum MouseButton
    {
        /// <summary>No mouse button.</summary>
        None = 0,

        /// <summary>Left mouse button.</summary>
        Left = 1,

        /// <summary>Right mouse button.</summary>
        Right = 2,

        /// <summary>Middle mouse button.</summary>
        Middle = 3
    }
    
    #endregion
}
