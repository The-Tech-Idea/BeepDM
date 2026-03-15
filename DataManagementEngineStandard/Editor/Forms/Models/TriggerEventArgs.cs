using System;

namespace TheTechIdea.Beep.Editor.Forms.Models
{
    /// <summary>
    /// Event arguments for trigger execution events
    /// </summary>
    public class TriggerExecutedEventArgs : EventArgs
    {
        /// <summary>The trigger that was executed</summary>
        public TriggerDefinition Trigger { get; set; }
        
        /// <summary>The execution context</summary>
        public TriggerContext Context { get; set; }
        
        /// <summary>Result of the execution</summary>
        public TriggerResult Result { get; set; }
        
        /// <summary>Execution duration in milliseconds</summary>
        public double DurationMs { get; set; }
        
        /// <summary>Exception if execution failed</summary>
        public Exception Exception { get; set; }
        
        /// <summary>When execution started</summary>
        public DateTime StartTime { get; set; }
        
        /// <summary>When execution ended</summary>
        public DateTime EndTime { get; set; }
        
        /// <summary>Whether execution was successful</summary>
        public bool IsSuccess => Result == TriggerResult.Success;
    }
    
    /// <summary>
    /// Event arguments for before trigger execution (allows cancellation)
    /// </summary>
    public class TriggerExecutingEventArgs : EventArgs
    {
        /// <summary>The trigger about to execute</summary>
        public TriggerDefinition Trigger { get; set; }
        
        /// <summary>The execution context</summary>
        public TriggerContext Context { get; set; }
        
        /// <summary>Set to true to cancel execution</summary>
        public bool Cancel { get; set; }
        
        /// <summary>Reason for cancellation</summary>
        public string CancelReason { get; set; }
    }
    
    /// <summary>
    /// Event arguments for trigger registration events
    /// </summary>
    public class TriggerRegisteredEventArgs : EventArgs
    {
        /// <summary>The trigger that was registered</summary>
        public TriggerDefinition Trigger { get; set; }
        
        /// <summary>Block name (if block-level)</summary>
        public string BlockName { get; set; }
        
        /// <summary>Item name (if item-level)</summary>
        public string ItemName { get; set; }
        
        /// <summary>Whether this replaced an existing trigger</summary>
        public bool WasReplacement { get; set; }
        
        /// <summary>Timestamp of registration</summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
    
    /// <summary>
    /// Event arguments for trigger unregistration events
    /// </summary>
    public class TriggerUnregisteredEventArgs : EventArgs
    {
        /// <summary>Type of trigger that was removed</summary>
        public TriggerType TriggerType { get; set; }
        
        /// <summary>Block name (if block-level)</summary>
        public string BlockName { get; set; }
        
        /// <summary>Item name (if item-level)</summary>
        public string ItemName { get; set; }
        
        /// <summary>Number of triggers removed</summary>
        public int RemovedCount { get; set; }
        
        /// <summary>Timestamp of unregistration</summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
    
    /// <summary>
    /// Event arguments for trigger chain execution
    /// </summary>
    public class TriggerChainCompletedEventArgs : EventArgs
    {
        /// <summary>Type of trigger that was fired</summary>
        public TriggerType TriggerType { get; set; }
        
        /// <summary>Number of triggers in the chain</summary>
        public int TriggerCount { get; set; }
        
        /// <summary>Number of successful executions</summary>
        public int SuccessCount { get; set; }
        
        /// <summary>Number of failed executions</summary>
        public int FailureCount { get; set; }
        
        /// <summary>Number of skipped triggers</summary>
        public int SkippedCount { get; set; }
        
        /// <summary>Total duration in milliseconds</summary>
        public double TotalDurationMs { get; set; }
        
        /// <summary>Whether any trigger cancelled the action</summary>
        public bool WasCancelled { get; set; }
        
        /// <summary>Cancellation message if cancelled</summary>
        public string CancelMessage { get; set; }
        
        /// <summary>Overall chain result</summary>
        public TriggerResult OverallResult { get; set; }
    }
}
