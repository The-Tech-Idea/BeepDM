using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Editor.Forms.Models
{
    /// <summary>
    /// Defines a trigger and its configuration.
    /// Oracle Forms equivalent: Trigger definition in Form Builder.
    /// </summary>
    public class TriggerDefinition
    {
        #region Identification
        
        /// <summary>Unique identifier for this trigger instance</summary>
        public string TriggerId { get; set; } = Guid.NewGuid().ToString("N");
        
        /// <summary>Trigger name (e.g., "WHEN-NEW-RECORD-INSTANCE")</summary>
        public string TriggerName { get; set; }
        
        /// <summary>Type of trigger</summary>
        public TriggerType TriggerType { get; set; }
        
        /// <summary>Display name for UI</summary>
        public string DisplayName { get; set; }
        
        /// <summary>Description of what this trigger does</summary>
        public string Description { get; set; }
        
        #endregion
        
        #region Scope
        
        /// <summary>Scope at which trigger operates</summary>
        public TriggerScope Scope { get; set; } = TriggerScope.Block;
        
        /// <summary>Form name (if form-level scope)</summary>
        public string FormName { get; set; }
        
        /// <summary>Block name (if block-level scope)</summary>
        public string BlockName { get; set; }
        
        /// <summary>Item name (if item-level scope)</summary>
        public string ItemName { get; set; }
        
        /// <summary>Fully qualified name (e.g., "BlockName.ItemName.TriggerType")</summary>
        public string QualifiedName => BuildQualifiedName();
        
        #endregion
        
        #region Timing and Category
        
        /// <summary>When trigger fires relative to action</summary>
        public TriggerTiming Timing { get; set; }
        
        /// <summary>Logical category for organization</summary>
        public TriggerCategory Category { get; set; }
        
        #endregion
        
        #region Execution Configuration
        
        /// <summary>Execution priority (higher runs first)</summary>
        public TriggerPriority Priority { get; set; } = TriggerPriority.Normal;
        
        // B4 (audit pass 4, 2026-06): IsEnabled is mutated from
        // outside the trigger chain (EnableTrigger / DisableTrigger /
        // EnableBlockTriggers / DisableBlockTriggers / EnableAllTriggers
        // / DisableAllTriggers) and read from inside the chain (the
        // per-rule `rules.Where(r => r.IsEnabled)` filter, and the
        // `HasBlockTrigger` / `HasItemTrigger` checks). Without a
        // volatile barrier, a thread that just disabled a trigger may
        // see a stale IsEnabled = true on a weak memory model and
        // continue to execute the trigger. The C# compiler rejects
        // `volatile` on a public auto-property (volatile on auto-props
        // is restricted to certain access patterns), so we use a
        // private volatile backing field with explicit accessors. The
        // public setter is kept for API compat; reads/writes are
        // routed through the volatile field.
        private volatile bool _isEnabled = true;
        /// <summary>Whether trigger is enabled</summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }
        
        /// <summary>Maximum execution time in milliseconds (0 = no limit)</summary>
        public int TimeoutMs { get; set; } = 0;
        
        /// <summary>Whether to execute asynchronously</summary>
        public bool IsAsync { get; set; } = false;
        
        /// <summary>Whether to continue on failure (false = RAISE FORM_TRIGGER_FAILURE)</summary>
        public bool ContinueOnFailure { get; set; } = false;
        
        /// <summary>Whether this trigger can be overridden by block/item level</summary>
        public bool AllowOverride { get; set; } = true;
        
        /// <summary>Whether execution should be logged</summary>
        public bool LogExecution { get; set; } = false;

        /// <summary>
        /// IDs of triggers that must complete (with Success/Continue result) before this trigger fires.
        /// Used by the trigger dependency manager for ordered chain execution.
        /// </summary>
        public List<string> DependsOn { get; set; } = new List<string>();

        /// <summary>
        /// How the trigger chain behaves when this trigger fails.
        /// Defaults to <see cref="TriggerChainMode.StopOnFailure"/>.
        /// </summary>
        public TriggerChainMode ChainMode { get; set; } = TriggerChainMode.StopOnFailure;
        
        #endregion
        
        #region Handler
        
        /// <summary>
        /// Synchronous trigger handler delegate
        /// </summary>
        public Func<TriggerContext, TriggerResult> Handler { get; set; }
        
        /// <summary>
        /// Asynchronous trigger handler delegate
        /// </summary>
        public Func<TriggerContext, CancellationToken, Task<TriggerResult>> AsyncHandler { get; set; }
        
        /// <summary>Whether a handler is registered</summary>
        public bool HasHandler => Handler != null || AsyncHandler != null;
        
        #endregion
        
        #region Metadata
        
        /// <summary>When trigger was created</summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        /// <summary>When trigger was last modified</summary>
        public DateTime? ModifiedAt { get; set; }
        
        /// <summary>Who created this trigger</summary>
        public string CreatedBy { get; set; }
        
        /// <summary>Custom user data</summary>
        public object Tag { get; set; }
        
        #endregion
        
        #region Statistics
        
        /// <summary>Number of times trigger has been executed</summary>
        public long ExecutionCount { get; private set; }
        
        /// <summary>Number of successful executions</summary>
        public long SuccessCount { get; private set; }
        
        /// <summary>Number of failed executions</summary>
        public long FailureCount { get; private set; }
        
        /// <summary>Last execution time</summary>
        public DateTime? LastExecutedAt { get; private set; }
        
        /// <summary>Last execution result</summary>
        public TriggerResult? LastResult { get; private set; }
        
        /// <summary>Average execution time in milliseconds</summary>
        public double AverageExecutionTimeMs { get; private set; }
        
        /// <summary>Total execution time in milliseconds</summary>
        private long _totalExecutionTimeMs;
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Create a new TriggerDefinition
        /// </summary>
        public TriggerDefinition() { }
        
        /// <summary>
        /// Create a new TriggerDefinition with type and scope
        /// </summary>
        public TriggerDefinition(TriggerType type, TriggerScope scope)
        {
            TriggerType = type;
            Scope = scope;
            TriggerName = type.ToString();
            Category = DetermineCategory(type);
            Timing = DetermineTiming(type);
        }
        
        #endregion
        
        #region Methods
        
        /// <summary>
        /// Execute the trigger synchronously.
        /// </summary>
        /// <remarks>
        /// B1 (audit pass 4, 2026-06): the AsyncHandler-only path
        /// (<c>Handler is null &amp;&amp; AsyncHandler is not null</c>)
        /// calls <c>.GetAwaiter().GetResult()</c> on the async handler.
        /// This is the classic sync-over-async pattern and DEADLOCKS on
        /// a UI thread with a captured
        /// <see cref="SynchronizationContext"/>: the awaited task tries
        /// to resume on the captured context, but the context is blocked
        /// waiting for the task to complete. Callers that may be on a UI
        /// thread (Forms, WPF, Blazor) MUST use
        /// <see cref="ExecuteAsync(TriggerContext, CancellationToken)"/>
        /// instead. The TriggerManager's sync <c>FireXxxTrigger</c>
        /// overloads are safe to call from any thread because they reach
        /// this method from the engine's worker thread (or a thread
        /// without a captured sync context), but external callers that
        /// invoke <c>Execute</c> directly from a UI event handler WILL
        /// deadlock.
        ///
        /// The detection in the AsyncHandler-only branch is a defensive
        /// belt-and-braces: if we are on a captured sync context, refuse
        /// the call and throw with a clear message. This catches the
        /// common case (UI thread) early. Callers without a sync context
        /// (thread pool, console, test runners) will not see the throw
        /// and will get the same .GetAwaiter().GetResult() behavior as
        /// before.
        /// </remarks>
        public TriggerResult Execute(TriggerContext context)
        {
            if (!IsEnabled || !HasHandler)
                return TriggerResult.Skipped;

            var startTime = DateTime.UtcNow;
            TriggerResult result;

            try
            {
                if (Handler != null)
                {
                    result = Handler(context);
                }
                else if (AsyncHandler != null)
                {
                    if (SynchronizationContext.Current != null)
                    {
                        // Captured sync context — the .GetAwaiter().GetResult()
                        // below would deadlock. Refuse the call with a
                        // clear message pointing the caller at ExecuteAsync.
                        throw new InvalidOperationException(
                            "TriggerDefinition.Execute was called from a thread with a captured " +
                            "SynchronizationContext (typically a UI thread), but the trigger only " +
                            "has an AsyncHandler. Calling .GetAwaiter().GetResult() here would deadlock. " +
                            "Use TriggerDefinition.ExecuteAsync(...) from UI threads, or call " +
                            "TriggerManager.Fire*TriggerAsync(...) which dispatches via the thread pool.");
                    }
                    // Run async handler synchronously
                    result = AsyncHandler(context, CancellationToken.None).GetAwaiter().GetResult();
                }
                else
                {
                    return TriggerResult.Skipped;
                }
            }
            catch (Exception)
            {
                result = TriggerResult.Exception;
            }

            RecordExecution(result, startTime);
            return result;
        }
        
        /// <summary>
        /// Execute the trigger asynchronously
        /// </summary>
        public async Task<TriggerResult> ExecuteAsync(TriggerContext context, CancellationToken cancellationToken = default)
        {
            if (!IsEnabled || !HasHandler)
                return TriggerResult.Skipped;
            
            var startTime = DateTime.UtcNow;
            TriggerResult result;
            
            try
            {
                if (TimeoutMs > 0)
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(TimeoutMs);
                    
                    try
                    {
                        result = await ExecuteHandlerAsync(context, cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        result = cancellationToken.IsCancellationRequested
                            ? TriggerResult.Cancelled
                            : TriggerResult.Timeout;
                    }
                }
                else
                {
                    result = await ExecuteHandlerAsync(context, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                result = TriggerResult.Cancelled;
            }
            catch (Exception)
            {
                result = TriggerResult.Exception;
            }
            
            RecordExecution(result, startTime);
            return result;
        }
        
        /// <summary>
        /// Reset execution statistics
        /// </summary>
        public void ResetStatistics()
        {
            ExecutionCount = 0;
            SuccessCount = 0;
            FailureCount = 0;
            LastExecutedAt = null;
            LastResult = null;
            AverageExecutionTimeMs = 0;
            _totalExecutionTimeMs = 0;
        }
        
        /// <summary>
        /// Clone this trigger definition.
        /// </summary>
        /// <remarks>
        /// B2 (audit pass 4, 2026-06): the previous version did not copy
        /// <see cref="DependsOn"/> (a <see cref="List{String}"/>) or
        /// <see cref="ChainMode"/>. A clone of a trigger that had
        /// <c>DependsOn = ["x"]</c> and <c>ChainMode = Continue</c> came
        /// out with an empty dependency list and the default
        /// <c>StopOnFailure</c> chain mode — silently broken. The
        /// dependencies are now deep-copied (new list) and the chain
        /// mode is copied. The new <c>TriggerId</c> still differs from
        /// the source (by design — clones are independent registrations).
        /// </remarks>
        public TriggerDefinition Clone()
        {
            return new TriggerDefinition
            {
                TriggerId = Guid.NewGuid().ToString("N"),
                TriggerName = TriggerName,
                TriggerType = TriggerType,
                DisplayName = DisplayName,
                Description = Description,
                Scope = Scope,
                FormName = FormName,
                BlockName = BlockName,
                ItemName = ItemName,
                Timing = Timing,
                Category = Category,
                Priority = Priority,
                IsEnabled = IsEnabled,
                TimeoutMs = TimeoutMs,
                IsAsync = IsAsync,
                ContinueOnFailure = ContinueOnFailure,
                AllowOverride = AllowOverride,
                LogExecution = LogExecution,
                // Deep-copy the dependency list so the clone and the
                // source do not share the same backing array. The
                // TriggerId is intentionally different on the clone
                // (assigned above), so a downstream OrderByDependency
                // will treat them as distinct nodes.
                DependsOn = DependsOn != null
                    ? new List<string>(DependsOn)
                    : new List<string>(),
                ChainMode = ChainMode,
                Handler = Handler,
                AsyncHandler = AsyncHandler,
                CreatedAt = DateTime.Now,
                CreatedBy = CreatedBy,
                Tag = Tag
            };
        }
        
        #endregion
        
        #region Private Methods
        
        private async Task<TriggerResult> ExecuteHandlerAsync(TriggerContext context, CancellationToken cancellationToken)
        {
            if (AsyncHandler != null)
            {
                return await AsyncHandler(context, cancellationToken);
            }
            else if (Handler != null)
            {
                return await Task.Run(() => Handler(context), cancellationToken);
            }
            return TriggerResult.Skipped;
        }
        
        private void RecordExecution(TriggerResult result, DateTime startTime)
        {
            var elapsed = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;

            ExecutionCount++;
            // B6 (audit pass 4, 2026-06): the previous version used
            // DateTime.Now (local time) for LastExecutedAt while
            // startTime and the elapsed calculation are in UTC.
            // Mixing the two in the same record was confusing — the
            // wall-clock time of execution was reported in local
            // time, the duration was measured in UTC. Use UtcNow for
            // both so the timestamps are internally consistent. (The
            // consumer of LastExecutedAt can convert to local time for
            // display.)
            LastExecutedAt = DateTime.UtcNow;
            LastResult = result;

            if (result == TriggerResult.Success)
                SuccessCount++;
            else if (result != TriggerResult.Skipped)
                FailureCount++;

            _totalExecutionTimeMs += elapsed;
            AverageExecutionTimeMs = ExecutionCount > 0 ? (double)_totalExecutionTimeMs / ExecutionCount : 0;
        }
        
        private string BuildQualifiedName()
        {
            switch (Scope)
            {
                case TriggerScope.Form:
                    return $"{FormName ?? "FORM"}.{TriggerType}";
                case TriggerScope.Block:
                    return $"{BlockName ?? "BLOCK"}.{TriggerType}";
                case TriggerScope.Item:
                    return $"{BlockName ?? "BLOCK"}.{ItemName ?? "ITEM"}.{TriggerType}";
                case TriggerScope.Global:
                    return $"GLOBAL.{TriggerType}";
                default:
                    return TriggerType.ToString();
            }
        }
        
        private static TriggerCategory DetermineCategory(TriggerType type)
        {
            int value = (int)type;
            
            if (value < 20) return TriggerCategory.FormLifecycle;
            if (value < 50) return TriggerCategory.BlockLifecycle;
            if (value < 70) return TriggerCategory.RecordLifecycle;
            if (value < 100) return TriggerCategory.ItemLifecycle;
            if (value < 120) return TriggerCategory.Navigation;
            if (value < 150) return TriggerCategory.KeyAction;
            if (value < 170) return TriggerCategory.KeyAction;
            if (value < 190) return TriggerCategory.MouseEvent;
            if (value < 200) return TriggerCategory.Timer;
            return TriggerCategory.Custom;
        }
        
        private static TriggerTiming DetermineTiming(TriggerType type)
        {
            var name = type.ToString();
            
            if (name.StartsWith("Pre")) return TriggerTiming.Before;
            if (name.StartsWith("Post")) return TriggerTiming.After;
            if (name.StartsWith("When")) return TriggerTiming.When;
            if (name.StartsWith("On")) return TriggerTiming.On;
            if (name.StartsWith("Key")) return TriggerTiming.Key;
            
            return TriggerTiming.When;
        }
        
        #endregion
        
        #region Factory Methods
        
        /// <summary>
        /// Create a form-level trigger
        /// </summary>
        public static TriggerDefinition CreateFormTrigger(TriggerType type, string formName, Func<TriggerContext, TriggerResult> handler)
        {
            return new TriggerDefinition(type, TriggerScope.Form)
            {
                FormName = formName,
                Handler = handler
            };
        }
        
        /// <summary>
        /// Create a block-level trigger
        /// </summary>
        public static TriggerDefinition CreateBlockTrigger(TriggerType type, string blockName, Func<TriggerContext, TriggerResult> handler)
        {
            return new TriggerDefinition(type, TriggerScope.Block)
            {
                BlockName = blockName,
                Handler = handler
            };
        }
        
        /// <summary>
        /// Create an item-level trigger
        /// </summary>
        public static TriggerDefinition CreateItemTrigger(TriggerType type, string blockName, string itemName, Func<TriggerContext, TriggerResult> handler)
        {
            return new TriggerDefinition(type, TriggerScope.Item)
            {
                BlockName = blockName,
                ItemName = itemName,
                Handler = handler
            };
        }
        
        /// <summary>
        /// Create an async block-level trigger
        /// </summary>
        public static TriggerDefinition CreateAsyncBlockTrigger(TriggerType type, string blockName, Func<TriggerContext, CancellationToken, Task<TriggerResult>> asyncHandler)
        {
            return new TriggerDefinition(type, TriggerScope.Block)
            {
                BlockName = blockName,
                AsyncHandler = asyncHandler,
                IsAsync = true
            };
        }
        
        /// <summary>
        /// Create a global trigger
        /// </summary>
        public static TriggerDefinition CreateGlobalTrigger(TriggerType type, Func<TriggerContext, TriggerResult> handler)
        {
            return new TriggerDefinition(type, TriggerScope.Global)
            {
                Handler = handler
            };
        }
        
        #endregion
        
        #region Override
        
        /// <summary>Returns a compact display string for the trigger definition.</summary>
        public override string ToString()
        {
            return $"{QualifiedName} (Enabled={IsEnabled}, Priority={Priority})";
        }
        
        #endregion
    }
}
