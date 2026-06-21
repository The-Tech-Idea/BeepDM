using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Editor.UOWManager.Configuration;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Editor.Forms.Models;


namespace TheTechIdea.Beep.Editor.UOWManager.Interfaces
{

    #region Trigger Manager Interface
    
    /// <summary>
    /// Interface for managing triggers (Oracle Forms trigger equivalents).
    /// Provides registration, execution, and lifecycle management for all trigger types.
    /// </summary>
    public interface ITriggerManager : IDisposable
    {
        #region Trigger Registration
        
        /// <summary>
        /// Register a form-level trigger
        /// </summary>
        void RegisterFormTrigger(TriggerType type, string formName, Func<TriggerContext, TriggerResult> handler, TriggerPriority priority = TriggerPriority.Normal);
        
        /// <summary>
        /// Register a form-level async trigger
        /// </summary>
        void RegisterFormTriggerAsync(TriggerType type, string formName, Func<TriggerContext, CancellationToken, Task<TriggerResult>> handler, TriggerPriority priority = TriggerPriority.Normal);
        
        /// <summary>
        /// Register a block-level trigger
        /// </summary>
        void RegisterBlockTrigger(TriggerType type, string blockName, Func<TriggerContext, TriggerResult> handler, TriggerPriority priority = TriggerPriority.Normal);
        
        /// <summary>
        /// Register a block-level async trigger
        /// </summary>
        void RegisterBlockTriggerAsync(TriggerType type, string blockName, Func<TriggerContext, CancellationToken, Task<TriggerResult>> handler, TriggerPriority priority = TriggerPriority.Normal);
        
        /// <summary>
        /// Register an item-level trigger
        /// </summary>
        void RegisterItemTrigger(TriggerType type, string blockName, string itemName, Func<TriggerContext, TriggerResult> handler, TriggerPriority priority = TriggerPriority.Normal);
        
        /// <summary>
        /// Register an item-level async trigger
        /// </summary>
        void RegisterItemTriggerAsync(TriggerType type, string blockName, string itemName, Func<TriggerContext, CancellationToken, Task<TriggerResult>> handler, TriggerPriority priority = TriggerPriority.Normal);
        
        /// <summary>
        /// Register a global trigger (applies to all forms)
        /// </summary>
        void RegisterGlobalTrigger(TriggerType type, Func<TriggerContext, TriggerResult> handler, TriggerPriority priority = TriggerPriority.Normal);
        
        /// <summary>
        /// Register a trigger definition directly
        /// </summary>
        void RegisterTrigger(TriggerDefinition trigger);
        
        #endregion
        
        #region Trigger Unregistration
        
        /// <summary>
        /// Unregister a specific trigger by ID
        /// </summary>
        bool UnregisterTrigger(string triggerId);
        
        /// <summary>
        /// Unregister all triggers of a type for a block
        /// </summary>
        int UnregisterBlockTriggers(TriggerType type, string blockName);
        
        /// <summary>
        /// Unregister all triggers of a type for an item
        /// </summary>
        int UnregisterItemTriggers(TriggerType type, string blockName, string itemName);
        
        /// <summary>
        /// Unregister all triggers for a block
        /// </summary>
        int ClearBlockTriggers(string blockName);
        
        /// <summary>
        /// Unregister all triggers for an item
        /// </summary>
        int ClearItemTriggers(string blockName, string itemName);
        
        /// <summary>
        /// Unregister all triggers
        /// </summary>
        void ClearAllTriggers();
        
        #endregion
        
        #region Trigger Execution
        
        /// <summary>
        /// Fire a form-level trigger
        /// </summary>
        TriggerResult FireFormTrigger(TriggerType type, string formName, TriggerContext context = null);
        
        /// <summary>
        /// Fire a form-level trigger asynchronously
        /// </summary>
        Task<TriggerResult> FireFormTriggerAsync(TriggerType type, string formName, TriggerContext context = null, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Fire a block-level trigger
        /// </summary>
        TriggerResult FireBlockTrigger(TriggerType type, string blockName, TriggerContext context = null);
        
        /// <summary>
        /// Fire a block-level trigger asynchronously
        /// </summary>
        Task<TriggerResult> FireBlockTriggerAsync(TriggerType type, string blockName, TriggerContext context = null, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Fire an item-level trigger
        /// </summary>
        TriggerResult FireItemTrigger(TriggerType type, string blockName, string itemName, TriggerContext context = null);
        
        /// <summary>
        /// Fire an item-level trigger asynchronously
        /// </summary>
        Task<TriggerResult> FireItemTriggerAsync(TriggerType type, string blockName, string itemName, TriggerContext context = null, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Fire a global trigger
        /// </summary>
        TriggerResult FireGlobalTrigger(TriggerType type, TriggerContext context = null);
        
        /// <summary>
        /// Fire a global trigger asynchronously
        /// </summary>
        Task<TriggerResult> FireGlobalTriggerAsync(TriggerType type, TriggerContext context = null, CancellationToken cancellationToken = default);
        
        #endregion
        
        #region Trigger Query
        
        /// <summary>
        /// Get a trigger by ID
        /// </summary>
        TriggerDefinition GetTrigger(string triggerId);
        
        /// <summary>
        /// Get all triggers for a block
        /// </summary>
        IReadOnlyList<TriggerDefinition> GetBlockTriggers(string blockName);
        
        /// <summary>
        /// Get all triggers of a specific type for a block
        /// </summary>
        IReadOnlyList<TriggerDefinition> GetBlockTriggers(TriggerType type, string blockName);
        
        /// <summary>
        /// Get all triggers for an item
        /// </summary>
        IReadOnlyList<TriggerDefinition> GetItemTriggers(string blockName, string itemName);
        
        /// <summary>
        /// Get all form-level triggers
        /// </summary>
        IReadOnlyList<TriggerDefinition> GetFormTriggers(string formName);
        
        /// <summary>
        /// Get all global triggers
        /// </summary>
        IReadOnlyList<TriggerDefinition> GetGlobalTriggers();
        
        /// <summary>
        /// Get all triggers by category
        /// </summary>
        IReadOnlyList<TriggerDefinition> GetTriggersByCategory(TriggerCategory category);
        
        /// <summary>
        /// Check if a trigger exists for a block
        /// </summary>
        bool HasBlockTrigger(TriggerType type, string blockName);
        
        /// <summary>
        /// Check if a trigger exists for an item
        /// </summary>
        bool HasItemTrigger(TriggerType type, string blockName, string itemName);
        
        /// <summary>
        /// Get total trigger count
        /// </summary>
        int TriggerCount { get; }
        
        #endregion
        
        #region Trigger Enable/Disable
        
        /// <summary>
        /// Enable a trigger by ID
        /// </summary>
        void EnableTrigger(string triggerId);
        
        /// <summary>
        /// Disable a trigger by ID
        /// </summary>
        void DisableTrigger(string triggerId);
        
        /// <summary>
        /// Enable all triggers of a type for a block
        /// </summary>
        void EnableBlockTriggers(TriggerType type, string blockName);
        
        /// <summary>
        /// Disable all triggers of a type for a block
        /// </summary>
        void DisableBlockTriggers(TriggerType type, string blockName);
        
        /// <summary>
        /// Enable all triggers
        /// </summary>
        void EnableAllTriggers();
        
        /// <summary>
        /// Disable all triggers
        /// </summary>
        void DisableAllTriggers();
        
        /// <summary>
        /// Check if trigger execution is globally suspended
        /// </summary>
        bool IsSuspended { get; }
        
        /// <summary>
        /// Suspend all trigger execution temporarily
        /// </summary>
        void SuspendTriggers();
        
        /// <summary>
        /// Resume trigger execution
        /// </summary>
        void ResumeTriggers();
        
        #endregion
        
        #region Events
        
        /// <summary>Event raised before a trigger executes</summary>
        event EventHandler<TriggerExecutingEventArgs> TriggerExecuting;
        
        /// <summary>Event raised after a trigger executes</summary>
        event EventHandler<TriggerExecutedEventArgs> TriggerExecuted;
        
        /// <summary>Event raised when a trigger is registered</summary>
        event EventHandler<TriggerRegisteredEventArgs> TriggerRegistered;
        
        /// <summary>Event raised when a trigger is unregistered</summary>
        event EventHandler<TriggerUnregisteredEventArgs> TriggerUnregistered;
        
        /// <summary>Event raised when a trigger chain completes</summary>
        event EventHandler<TriggerChainCompletedEventArgs> TriggerChainCompleted;
        
        #endregion

        #region Statistics & Scope Helpers

        /// <summary>Get aggregate execution statistics for a block's triggers</summary>
        TriggerStatisticsInfo GetTriggerStatistics(string blockName);

        /// <summary>Get only form-scope triggers registered for a block</summary>
        IReadOnlyList<TriggerDefinition> GetFormLevelTriggers(string blockName);

        /// <summary>Get only block-scope triggers registered for a block</summary>
        IReadOnlyList<TriggerDefinition> GetBlockLevelTriggers(string blockName);

        /// <summary>Get only record-scope triggers registered for a block</summary>
        IReadOnlyList<TriggerDefinition> GetRecordLevelTriggers(string blockName);

        /// <summary>Get only item-scope triggers registered for a block</summary>
        IReadOnlyList<TriggerDefinition> GetItemLevelTriggers(string blockName);

        #endregion
    }
    
    #endregion
    // ──────────────────────────────────────────────────────────────────────────
    // Phase 4 — Advanced Trigger System
    // ──────────────────────────────────────────────────────────────────────────

    #region TriggerExecutionLogEntry

    /// <summary>
    /// One recorded execution of a trigger (timing + outcome).
    /// Stored by <see cref="ITriggerExecutionLog"/>.
    /// </summary>
    public class TriggerExecutionLogEntry
    {
        /// <summary>Gets or sets the unique trigger identifier.</summary>
        public string TriggerId    { get; set; }

        /// <summary>Gets or sets the trigger display name.</summary>
        public string TriggerName  { get; set; }

        /// <summary>Gets or sets the trigger type that executed.</summary>
        public TriggerType TriggerType { get; set; }

        /// <summary>Gets or sets the block involved in the execution.</summary>
        public string BlockName    { get; set; }

        /// <summary>Gets or sets the item involved in the execution.</summary>
        public string ItemName     { get; set; }

        /// <summary>Gets or sets the trigger execution result.</summary>
        public TriggerResult Result { get; set; }

        /// <summary>Gets or sets the execution time in milliseconds.</summary>
        public long ElapsedMs      { get; set; }

        /// <summary>Gets or sets when the trigger executed.</summary>
        public DateTime ExecutedAt { get; set; } = DateTime.Now;

        /// <summary>Gets or sets the error message for failed trigger executions.</summary>
        public string ErrorMessage { get; set; }
    }

    #endregion

    #region ITriggerExecutionLog

    /// <summary>
    /// In-memory ring-buffer log of recent trigger executions with timing.
    /// </summary>
    public interface ITriggerExecutionLog
    {
        /// <summary>Maximum number of entries to retain (oldest are dropped).</summary>
        int Capacity { get; set; }

        /// <summary>Append an entry.</summary>
        void Record(TriggerExecutionLogEntry entry);

        /// <summary>All retained entries, newest last.</summary>
        IReadOnlyList<TriggerExecutionLogEntry> GetAll();

        /// <summary>Entries for a specific block.</summary>
        IReadOnlyList<TriggerExecutionLogEntry> GetByBlock(string blockName);

        /// <summary>Entries for a specific trigger type.</summary>
        IReadOnlyList<TriggerExecutionLogEntry> GetByType(TriggerType type);

        /// <summary>Remove all retained entries.</summary>
        void Clear();
    }

    #endregion

    #region ITriggerDependencyManager

    /// <summary>
    /// Builds a dependency graph over <see cref="TriggerDefinition.DependsOn"/> lists,
    /// detects cycles, and returns an execution-ordered list for a set of triggers.
    /// </summary>
    public interface ITriggerDependencyManager
    {
        /// <summary>
        /// Returns the triggers in dependency order (topological sort).
        /// Throws <see cref="InvalidOperationException"/> if a cycle is detected.
        /// </summary>
        IReadOnlyList<TriggerDefinition> OrderByDependency(IReadOnlyList<TriggerDefinition> triggers);

        /// <summary>
        /// Returns true when the supplied list contains a circular dependency.
        /// </summary>
        bool HasCircularDependency(IReadOnlyList<TriggerDefinition> triggers);

        /// <summary>
        /// Returns the names of all triggers involved in a cycle, or empty list when no cycle exists.
        /// </summary>
        IReadOnlyList<string> FindCycle(IReadOnlyList<TriggerDefinition> triggers);
    }

    #endregion

}
