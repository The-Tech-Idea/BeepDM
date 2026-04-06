using System;

namespace TheTechIdea.Beep.Editor.Forms.Models
{
    #region Trigger Type Enumeration
    
    /// <summary>
    /// Comprehensive trigger types covering all Oracle Forms trigger equivalents.
    /// Organized by category for easier management.
    /// </summary>
    public enum TriggerType
    {
        #region Form Level Triggers (0-19)
        
        /// <summary>When form opens (PRE-FORM)</summary>
        PreForm = 0,
        
        /// <summary>When new form instance created (WHEN-NEW-FORM-INSTANCE)</summary>
        WhenNewFormInstance = 1,
        
        /// <summary>When form closes (POST-FORM)</summary>
        PostForm = 2,
        
        /// <summary>When window activated (WHEN-WINDOW-ACTIVATED)</summary>
        WhenWindowActivated = 3,
        
        /// <summary>When window closed (WHEN-WINDOW-CLOSED)</summary>
        WhenWindowClosed = 4,
        
        /// <summary>When window deactivated (WHEN-WINDOW-DEACTIVATED)</summary>
        WhenWindowDeactivated = 5,
        
        /// <summary>When window resized (WHEN-WINDOW-RESIZED)</summary>
        WhenWindowResized = 6,
        
        /// <summary>On error (ON-ERROR)</summary>
        OnError = 7,
        
        /// <summary>On message (ON-MESSAGE)</summary>
        OnMessage = 8,
        
        // Reserved 9-19 for future form-level triggers
        
        #endregion
        
        #region Block Level Triggers (20-49)
        
        /// <summary>Before entering block (PRE-BLOCK)</summary>
        PreBlock = 20,
        
        /// <summary>When entering block (WHEN-NEW-BLOCK-INSTANCE)</summary>
        WhenNewBlockInstance = 21,
        
        /// <summary>After leaving block (POST-BLOCK)</summary>
        PostBlock = 22,
        
        /// <summary>When creating a new record (WHEN-CREATE-RECORD)</summary>
        WhenCreateRecord = 23,
        
        /// <summary>When removing a record (WHEN-REMOVE-RECORD)</summary>
        WhenRemoveRecord = 24,
        
        /// <summary>Before query execution (PRE-QUERY)</summary>
        PreQuery = 25,
        
        /// <summary>After query execution (POST-QUERY)</summary>
        PostQuery = 26,
        
        /// <summary>Before delete operation (PRE-DELETE)</summary>
        PreDelete = 27,
        
        /// <summary>After delete operation (POST-DELETE)</summary>
        PostDelete = 28,
        
        /// <summary>Before insert operation (PRE-INSERT)</summary>
        PreInsert = 29,
        
        /// <summary>After insert operation (POST-INSERT)</summary>
        PostInsert = 30,
        
        /// <summary>Before update operation (PRE-UPDATE)</summary>
        PreUpdate = 31,
        
        /// <summary>After update operation (POST-UPDATE)</summary>
        PostUpdate = 32,
        
        /// <summary>Before commit operation (PRE-COMMIT)</summary>
        PreCommit = 33,
        
        /// <summary>After commit operation (POST-COMMIT)</summary>
        PostCommit = 34,
        
        /// <summary>On block populate (ON-POPULATE-DETAILS)</summary>
        OnPopulateDetails = 35,
        
        /// <summary>On check delete master (ON-CHECK-DELETE-MASTER)</summary>
        OnCheckDeleteMaster = 36,
        
        /// <summary>On clear details (ON-CLEAR-DETAILS)</summary>
        OnClearDetails = 37,
        
        /// <summary>On lock (ON-LOCK)</summary>
        OnLock = 38,
        
        /// <summary>On rollback (ON-ROLLBACK)</summary>
        OnRollback = 39,
        
        /// <summary>On insert — replaces default DB insert (ON-INSERT)</summary>
        OnInsert = 40,
        
        /// <summary>On update — replaces default DB update (ON-UPDATE)</summary>
        OnUpdate = 41,
        
        /// <summary>On delete — replaces default DB delete (ON-DELETE)</summary>
        OnDelete = 42,
        
        // Reserved 43-49 for future block-level triggers
        
        #endregion
        
        #region Record Level Triggers (50-69)
        
        /// <summary>Before entering record (PRE-RECORD)</summary>
        PreRecord = 50,
        
        /// <summary>When entering record (WHEN-NEW-RECORD-INSTANCE)</summary>
        WhenNewRecordInstance = 51,
        
        /// <summary>After leaving record (POST-RECORD)</summary>
        PostRecord = 52,
        
        /// <summary>When record modified (WHEN-RECORD-MODIFIED)</summary>
        WhenRecordModified = 53,
        
        /// <summary>Post Log Record Change</summary>
        PostLogRecordChange = 54,
        
        /// <summary>When validating a record (WHEN-VALIDATE-RECORD)</summary>
        WhenValidateRecord = 55,
        
        // Reserved 55-69 for future record-level triggers
        
        #endregion
        
        #region Item/Field Level Triggers (70-99)
        
        /// <summary>Before entering item (PRE-TEXT-ITEM)</summary>
        PreTextItem = 70,
        
        /// <summary>When entering item (WHEN-NEW-ITEM-INSTANCE)</summary>
        WhenNewItemInstance = 71,
        
        /// <summary>After leaving item (POST-TEXT-ITEM)</summary>
        PostTextItem = 72,
        
        /// <summary>When item value changes (WHEN-VALIDATE-ITEM)</summary>
        WhenValidateItem = 73,
        
        /// <summary>List of values validation (WHEN-LOV-VALIDATE)</summary>
        WhenLOVValidate = 74,
        
        /// <summary>Post change trigger (POST-CHANGE)</summary>
        PostChange = 75,
        
        /// <summary>When checkbox changed (WHEN-CHECKBOX-CHANGED)</summary>
        WhenCheckboxChanged = 76,
        
        /// <summary>When radio changed (WHEN-RADIO-CHANGED)</summary>
        WhenRadioChanged = 77,
        
        /// <summary>When list changed (WHEN-LIST-CHANGED)</summary>
        WhenListChanged = 78,
        
        /// <summary>When list activated (WHEN-LIST-ACTIVATED)</summary>
        WhenListActivated = 79,
        
        /// <summary>When image activated (WHEN-IMAGE-ACTIVATED)</summary>
        WhenImageActivated = 80,
        
        /// <summary>When image pressed (WHEN-IMAGE-PRESSED)</summary>
        WhenImagePressed = 81,
        
        // Reserved 82-99 for future item-level triggers
        
        #endregion
        
        #region Navigation Triggers (100-119)
        
        /// <summary>Key next item (KEY-NEXT-ITEM)</summary>
        KeyNextItem = 100,
        
        /// <summary>Key previous item (KEY-PREV-ITEM)</summary>
        KeyPreviousItem = 101,
        
        /// <summary>Key up (KEY-UP)</summary>
        KeyUp = 102,
        
        /// <summary>Key down (KEY-DOWN)</summary>
        KeyDown = 103,
        
        /// <summary>Key next record (KEY-NXTREC)</summary>
        KeyNextRecord = 104,
        
        /// <summary>Key previous record (KEY-PRVREC)</summary>
        KeyPreviousRecord = 105,
        
        /// <summary>Key next block (KEY-NXTBLK)</summary>
        KeyNextBlock = 106,
        
        /// <summary>Key previous block (KEY-PRVBLK)</summary>
        KeyPreviousBlock = 107,
        
        /// <summary>Key scroll up (KEY-SCRDN)</summary>
        KeyScrollUp = 108,
        
        /// <summary>Key scroll down (KEY-SCRUP)</summary>
        KeyScrollDown = 109,
        
        /// <summary>Key enter (KEY-ENTER)</summary>
        KeyEnter = 110,
        
        /// <summary>Key exit (KEY-EXIT)</summary>
        KeyExit = 111,
        
        // Reserved 112-119 for future navigation triggers
        
        #endregion
        
        #region Action Triggers (120-149)
        
        /// <summary>Key execute query (KEY-EXEQRY)</summary>
        KeyExecuteQuery = 120,
        
        /// <summary>Key count records (KEY-CNTQRY)</summary>
        KeyCountRecords = 121,
        
        /// <summary>Key commit (KEY-COMMIT)</summary>
        KeyCommit = 122,
        
        /// <summary>Key rollback (KEY-ROLLBACK)</summary>
        KeyRollback = 123,
        
        /// <summary>Key create record (KEY-CREREC)</summary>
        KeyCreateRecord = 124,
        
        /// <summary>Key delete record (KEY-DELREC)</summary>
        KeyDeleteRecord = 125,
        
        /// <summary>Key duplicate record (KEY-DUPREC)</summary>
        KeyDuplicateRecord = 126,
        
        /// <summary>Key duplicate item (KEY-DUPITM)</summary>
        KeyDuplicateItem = 127,
        
        /// <summary>Key clear block (KEY-CLRBLK)</summary>
        KeyClearBlock = 128,
        
        /// <summary>Key clear form (KEY-CLRFRM)</summary>
        KeyClearForm = 129,
        
        /// <summary>Key clear record (KEY-CLRREC)</summary>
        KeyClearRecord = 130,
        
        /// <summary>Key clear item (KEY-CLRITM)</summary>
        KeyClearItem = 131,
        
        /// <summary>Key list values (KEY-LISTVAL)</summary>
        KeyListValues = 132,
        
        /// <summary>Key help (KEY-HELP)</summary>
        KeyHelp = 133,
        
        /// <summary>Key print (KEY-PRINT)</summary>
        KeyPrint = 134,
        
        /// <summary>Key edit (KEY-EDIT)</summary>
        KeyEdit = 135,
        
        /// <summary>Key menu (KEY-MENU)</summary>
        KeyMenu = 136,
        
        // Reserved 137-149 for future action triggers
        
        #endregion
        
        #region Function Key Triggers (150-169)
        
        /// <summary>Key F1</summary>
        KeyF1 = 150,
        /// <summary>Key F2</summary>
        KeyF2 = 151,
        /// <summary>Key F3</summary>
        KeyF3 = 152,
        /// <summary>Key F4</summary>
        KeyF4 = 153,
        /// <summary>Key F5</summary>
        KeyF5 = 154,
        /// <summary>Key F6</summary>
        KeyF6 = 155,
        /// <summary>Key F7</summary>
        KeyF7 = 156,
        /// <summary>Key F8</summary>
        KeyF8 = 157,
        /// <summary>Key F9</summary>
        KeyF9 = 158,
        /// <summary>Key F10</summary>
        KeyF10 = 159,
        /// <summary>Key F11</summary>
        KeyF11 = 160,
        /// <summary>Key F12</summary>
        KeyF12 = 161,
        
        // Reserved 162-169 for additional key triggers
        
        #endregion
        
        #region Mouse Triggers (170-189)
        
        /// <summary>When mouse down (WHEN-MOUSE-DOWN)</summary>
        WhenMouseDown = 170,
        
        /// <summary>When mouse up (WHEN-MOUSE-UP)</summary>
        WhenMouseUp = 171,
        
        /// <summary>When mouse click (WHEN-MOUSE-CLICK)</summary>
        WhenMouseClick = 172,
        
        /// <summary>When mouse double click (WHEN-MOUSE-DOUBLECLICK)</summary>
        WhenMouseDoubleClick = 173,
        
        /// <summary>When mouse move (WHEN-MOUSE-MOVE)</summary>
        WhenMouseMove = 174,
        
        /// <summary>When mouse enter (WHEN-MOUSE-ENTER)</summary>
        WhenMouseEnter = 175,
        
        /// <summary>When mouse leave (WHEN-MOUSE-LEAVE)</summary>
        WhenMouseLeave = 176,
        
        /// <summary>When button pressed (WHEN-BUTTON-PRESSED)</summary>
        WhenButtonPressed = 177,
        
        // Reserved 178-189 for future mouse triggers
        
        #endregion
        
        #region Timer Triggers (190-199)
        
        /// <summary>When timer expired (WHEN-TIMER-EXPIRED)</summary>
        WhenTimerExpired = 190,
        
        // Reserved 191-199 for future timer triggers
        
        #endregion
        
        #region Custom/Application Triggers (200-255)
        
        /// <summary>Custom application-defined trigger 1</summary>
        Custom1 = 200,
        /// <summary>Custom application-defined trigger 2</summary>
        Custom2 = 201,
        /// <summary>Custom application-defined trigger 3</summary>
        Custom3 = 202,
        /// <summary>Custom application-defined trigger 4</summary>
        Custom4 = 203,
        /// <summary>Custom application-defined trigger 5</summary>
        Custom5 = 204,
        
        /// <summary>User named trigger</summary>
        UserNamed = 250
        
        #endregion
    }
    
    #endregion

    #region TriggerChainMode

    /// <summary>
    /// Controls behaviour when a trigger in a dependency chain fails.
    /// </summary>
    public enum TriggerChainMode
    {
        /// <summary>Stop execution of remaining triggers in the chain (default).</summary>
        StopOnFailure,
        /// <summary>Continue executing remaining triggers even after a failure.</summary>
        Continue,
        /// <summary>Roll back the entire form transaction if any trigger fails.</summary>
        Rollback
    }

    #endregion

    #region KeyTriggerType

    /// <summary>
    /// Convenience subset enum covering all Oracle Forms KEY-* trigger types.
    /// Maps 1-to-1 to <see cref="TriggerType"/> values in the 100-169 range.
    /// </summary>
    public enum KeyTriggerType
    {
        NextItem       = 100,  // KEY-NEXT-ITEM
        PreviousItem   = 101,  // KEY-PREV-ITEM
        Up             = 102,  // KEY-UP
        Down           = 103,  // KEY-DOWN
        NextRecord     = 104,  // KEY-NXTREC
        PreviousRecord = 105,  // KEY-PRVREC
        NextBlock      = 106,  // KEY-NXTBLK
        PreviousBlock  = 107,  // KEY-PRVBLK
        ScrollUp       = 108,  // KEY-SCRDN
        ScrollDown     = 109,  // KEY-SCRUP
        Enter          = 110,  // KEY-ENTER
        Exit           = 111,  // KEY-EXIT
        ExecuteQuery   = 120,  // KEY-EXEQRY
        CountRecords   = 121,  // KEY-CNTQRY
        Commit         = 122,  // KEY-COMMIT
        Rollback       = 123,  // KEY-ROLLBACK
        CreateRecord   = 124,  // KEY-CREREC
        DeleteRecord   = 125,  // KEY-DELREC
        DuplicateRecord= 126,  // KEY-DUPREC
        DuplicateItem  = 127,  // KEY-DUPITM
        ClearBlock     = 128,  // KEY-CLRBLK
        ClearForm      = 129,  // KEY-CLRFRM
        ClearRecord    = 130,  // KEY-CLRREC
        ClearItem      = 131,  // KEY-CLRITM
        ListValues     = 132,  // KEY-LISTVAL
        Help           = 133,  // KEY-HELP
        Print          = 134,  // KEY-PRINT
        Edit           = 135,  // KEY-EDIT
        Menu           = 136,  // KEY-MENU
        F1  = 150, F2  = 151, F3  = 152, F4  = 153,
        F5  = 154, F6  = 155, F7  = 156, F8  = 157,
        F9  = 158, F10 = 159, F11 = 160, F12 = 161
    }

    #endregion
    
    #region Trigger Scope Enumeration
    
    /// <summary>
    /// Defines the scope at which a trigger operates
    /// </summary>
    public enum TriggerScope
    {
        /// <summary>Form level - applies to entire form</summary>
        Form = 0,
        
        /// <summary>Block level - applies to specific block</summary>
        Block = 1,
        
        /// <summary>Record level - applies to record operations</summary>
        Record = 2,
        
        /// <summary>Item level - applies to specific field/item</summary>
        Item = 3,
        
        /// <summary>Global - applies across all forms</summary>
        Global = 4
    }
    
    #endregion
    
    #region Trigger Timing Enumeration
    
    /// <summary>
    /// When the trigger fires relative to the action
    /// </summary>
    public enum TriggerTiming
    {
        /// <summary>Before the action occurs</summary>
        Before = 0,
        
        /// <summary>When/During the action</summary>
        When = 1,
        
        /// <summary>After the action completes</summary>
        After = 2,
        
        /// <summary>On/In response to</summary>
        On = 3,
        
        /// <summary>Key press (user initiated)</summary>
        Key = 4
    }
    
    #endregion
    
    #region Trigger Execution Result Enumeration
    
    /// <summary>
    /// Result of trigger execution
    /// </summary>
    public enum TriggerResult
    {
        /// <summary>Trigger executed successfully</summary>
        Success = 0,
        
        /// <summary>Trigger failed</summary>
        Failure = 1,
        
        /// <summary>Trigger raised an exception</summary>
        Exception = 2,
        
        /// <summary>Trigger was cancelled</summary>
        Cancelled = 3,
        
        /// <summary>Trigger was skipped (not applicable)</summary>
        Skipped = 4,
        
        /// <summary>Trigger timed out</summary>
        Timeout = 5,
        
        /// <summary>Trigger caused form navigation failure (RAISE FORM_TRIGGER_FAILURE)</summary>
        FormTriggerFailure = 6
    }
    
    #endregion
    
    #region Trigger Priority Enumeration
    
    /// <summary>
    /// Priority for trigger execution order
    /// </summary>
    public enum TriggerPriority
    {
        /// <summary>Lowest priority - runs last</summary>
        Lowest = 0,
        
        /// <summary>Low priority</summary>
        Low = 25,
        
        /// <summary>Normal/Default priority</summary>
        Normal = 50,
        
        /// <summary>High priority</summary>
        High = 75,
        
        /// <summary>Highest priority - runs first</summary>
        Highest = 100,
        
        /// <summary>System level - reserved for framework</summary>
        System = 255
    }
    
    #endregion
    
    #region Trigger Category Enumeration
    
    /// <summary>
    /// Logical category grouping for triggers
    /// </summary>
    public enum TriggerCategory
    {
        /// <summary>Form lifecycle triggers</summary>
        FormLifecycle = 0,
        
        /// <summary>Block lifecycle triggers</summary>
        BlockLifecycle = 1,
        
        /// <summary>Record lifecycle triggers</summary>
        RecordLifecycle = 2,
        
        /// <summary>Item/field lifecycle triggers</summary>
        ItemLifecycle = 3,
        
        /// <summary>Data manipulation triggers</summary>
        DataManipulation = 4,
        
        /// <summary>Query triggers</summary>
        Query = 5,
        
        /// <summary>Validation triggers</summary>
        Validation = 6,
        
        /// <summary>Navigation triggers</summary>
        Navigation = 7,
        
        /// <summary>Key/Action triggers</summary>
        KeyAction = 8,
        
        /// <summary>Mouse event triggers</summary>
        MouseEvent = 9,
        
        /// <summary>Timer triggers</summary>
        Timer = 10,
        
        /// <summary>Error handling triggers</summary>
        ErrorHandling = 11,
        
        /// <summary>Master-detail relationship triggers</summary>
        MasterDetail = 12,
        
        /// <summary>Custom/user-defined triggers</summary>
        Custom = 99
    }
    
    #endregion
}
