using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Helpers;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;

namespace TheTechIdea.Beep.Editor.Forms.Helpers
{
    /// <summary>
    /// Manages trigger registration and execution for Oracle Forms emulation.
    /// Thread-safe implementation supporting sync and async triggers.
    /// </summary>
    public class TriggerManager : ITriggerManager
    {
        #region Private Fields
        
        /// <summary>All registered triggers by ID</summary>
        private readonly ConcurrentDictionary<string, TriggerDefinition> _triggers;
        
        /// <summary>Form triggers: formName -> type -> list of triggers</summary>
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<TriggerType, List<TriggerDefinition>>> _formTriggers;
        
        /// <summary>Block triggers: blockName -> type -> list of triggers</summary>
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<TriggerType, List<TriggerDefinition>>> _blockTriggers;
        
        /// <summary>Item triggers: blockName.itemName -> type -> list of triggers</summary>
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<TriggerType, List<TriggerDefinition>>> _itemTriggers;
        
        /// <summary>Global triggers: type -> list of triggers</summary>
        private readonly ConcurrentDictionary<TriggerType, List<TriggerDefinition>> _globalTriggers;
        
        private readonly IDMEEditor _editor;
        private readonly ConcurrentDictionary<string, DataBlockInfo> _blocks;
        private readonly object _lockObject = new object();
        // volatile: SuspendTriggers / ResumeTriggers may be called from one thread while
        // Fire*Trigger methods read _suspended on another. Without the volatile barrier,
        // a thread that just called SuspendTriggers could still see _suspended == false
        // on a weak memory model (ARM, older x86 with non-temporal stores) for a few
        // cycles, causing a trigger to fire after the suspend request landed. Marking
        // the field volatile forces an acquire/release fence on every read/write.
        private volatile bool _suspended;
        private bool _disposed;
        
        #endregion
        
        #region Events
        
        /// <inheritdoc />
        public event EventHandler<TriggerExecutingEventArgs> TriggerExecuting;
        
        /// <inheritdoc />
        public event EventHandler<TriggerExecutedEventArgs> TriggerExecuted;
        
        /// <inheritdoc />
        public event EventHandler<TriggerRegisteredEventArgs> TriggerRegistered;
        
        /// <inheritdoc />
        public event EventHandler<TriggerUnregisteredEventArgs> TriggerUnregistered;
        
        /// <inheritdoc />
        public event EventHandler<TriggerChainCompletedEventArgs> TriggerChainCompleted;
        
        #endregion
        
        #region Properties
        
        /// <inheritdoc />
        public int TriggerCount => _triggers.Count;
        
        /// <inheritdoc />
        public bool IsSuspended => _suspended;
        
        #endregion
        
        #region Constructors
        
        /// <summary>
        /// Create a new TriggerManager
        /// </summary>
        public TriggerManager() : this(null, null) { }
        
        /// <summary>
        /// Create a new TriggerManager with dependencies
        /// </summary>
        public TriggerManager(IDMEEditor editor, ConcurrentDictionary<string, DataBlockInfo> blocks = null)
        {
            _editor = editor;
            _blocks = blocks;
            _triggers = new ConcurrentDictionary<string, TriggerDefinition>(StringComparer.OrdinalIgnoreCase);
            _formTriggers = new ConcurrentDictionary<string, ConcurrentDictionary<TriggerType, List<TriggerDefinition>>>(StringComparer.OrdinalIgnoreCase);
            _blockTriggers = new ConcurrentDictionary<string, ConcurrentDictionary<TriggerType, List<TriggerDefinition>>>(StringComparer.OrdinalIgnoreCase);
            _itemTriggers = new ConcurrentDictionary<string, ConcurrentDictionary<TriggerType, List<TriggerDefinition>>>(StringComparer.OrdinalIgnoreCase);
            _globalTriggers = new ConcurrentDictionary<TriggerType, List<TriggerDefinition>>();
        }
        
        #endregion
        
        #region Trigger Registration
        
        /// <inheritdoc />
        public void RegisterFormTrigger(TriggerType type, string formName, Func<TriggerContext, TriggerResult> handler, TriggerPriority priority = TriggerPriority.Normal)
        {
            var trigger = TriggerDefinition.CreateFormTrigger(type, formName, handler);
            trigger.Priority = priority;
            RegisterTrigger(trigger);
        }
        
        /// <inheritdoc />
        public void RegisterFormTriggerAsync(TriggerType type, string formName, Func<TriggerContext, CancellationToken, Task<TriggerResult>> handler, TriggerPriority priority = TriggerPriority.Normal)
        {
            var trigger = new TriggerDefinition(type, TriggerScope.Form)
            {
                FormName = formName,
                AsyncHandler = handler,
                IsAsync = true,
                Priority = priority
            };
            RegisterTrigger(trigger);
        }
        
        /// <inheritdoc />
        public void RegisterBlockTrigger(TriggerType type, string blockName, Func<TriggerContext, TriggerResult> handler, TriggerPriority priority = TriggerPriority.Normal)
        {
            var trigger = TriggerDefinition.CreateBlockTrigger(type, blockName, handler);
            trigger.Priority = priority;
            RegisterTrigger(trigger);
        }
        
        /// <inheritdoc />
        public void RegisterBlockTriggerAsync(TriggerType type, string blockName, Func<TriggerContext, CancellationToken, Task<TriggerResult>> handler, TriggerPriority priority = TriggerPriority.Normal)
        {
            var trigger = TriggerDefinition.CreateAsyncBlockTrigger(type, blockName, handler);
            trigger.Priority = priority;
            RegisterTrigger(trigger);
        }
        
        /// <inheritdoc />
        public void RegisterItemTrigger(TriggerType type, string blockName, string itemName, Func<TriggerContext, TriggerResult> handler, TriggerPriority priority = TriggerPriority.Normal)
        {
            var trigger = TriggerDefinition.CreateItemTrigger(type, blockName, itemName, handler);
            trigger.Priority = priority;
            RegisterTrigger(trigger);
        }
        
        /// <inheritdoc />
        public void RegisterItemTriggerAsync(TriggerType type, string blockName, string itemName, Func<TriggerContext, CancellationToken, Task<TriggerResult>> handler, TriggerPriority priority = TriggerPriority.Normal)
        {
            var trigger = new TriggerDefinition(type, TriggerScope.Item)
            {
                BlockName = blockName,
                ItemName = itemName,
                AsyncHandler = handler,
                IsAsync = true,
                Priority = priority
            };
            RegisterTrigger(trigger);
        }
        
        /// <inheritdoc />
        public void RegisterGlobalTrigger(TriggerType type, Func<TriggerContext, TriggerResult> handler, TriggerPriority priority = TriggerPriority.Normal)
        {
            var trigger = TriggerDefinition.CreateGlobalTrigger(type, handler);
            trigger.Priority = priority;
            RegisterTrigger(trigger);
        }
        
        /// <inheritdoc />
        /// <remarks>
        /// Re-registering a trigger with the same <see cref="TriggerDefinition.TriggerId"/>
        /// is a supported idempotent operation. The previously-registered definition is
        /// removed from its per-scope list (Form / Block / Item / Global) and the new
        /// definition takes its place. Without this, the per-scope list would accumulate
        /// a stale entry for every re-register and the next <c>Fire*</c> call would
        /// execute BOTH the old and new handler — a silent duplicate-fire bug.
        /// </remarks>
        public void RegisterTrigger(TriggerDefinition trigger)
        {
            if (trigger == null)
                throw new ArgumentNullException(nameof(trigger));

            // B8 (audit pass 4, 2026-06): the previous version released
            // _lockObject BEFORE calling AddToFormTriggers /
            // AddToBlockTriggers / etc. A concurrent FireBlockTriggerAsync
            // that read _triggers[trigger.TriggerId] after the lock was
            // released would see the new trigger in the global dict,
            // but the per-scope list still contained the OLD list
            // (without the new entry). The fire call would walk the
            // per-scope list, not find the new trigger, and silently
            // miss it. The new behavior: hold the lock across both the
            // removal/insert into _triggers AND the per-scope append.
            // The per-scope Add* helpers take their own inner per-list
            // lock; we are now nested under _lockObject + per-list lock.
            // The order is consistent with the pass-1 ClearAllTriggers
            // fix (same pattern). The lock duration is still small
            // (the per-scope append is a dictionary insert + a sort).
            bool wasReplacement = false;
            TriggerDefinition previous = null;
            lock (_lockObject)
            {
                if (_triggers.TryGetValue(trigger.TriggerId, out previous) && !ReferenceEquals(previous, trigger))
                {
                    wasReplacement = true;
                    RemoveFromCollections(previous);
                }
                _triggers[trigger.TriggerId] = trigger;

                // Add to appropriate collection based on scope, INSIDE
                // the outer lock. The Add* helpers lock the inner list
                // (the per-scope List<TriggerDefinition>) internally,
                // so we are nested under _lockObject + inner lock.
                switch (trigger.Scope)
                {
                    case TriggerScope.Form:
                        AddToFormTriggers(trigger);
                        break;
                    case TriggerScope.Block:
                        AddToBlockTriggers(trigger);
                        break;
                    case TriggerScope.Item:
                        AddToItemTriggers(trigger);
                        break;
                    case TriggerScope.Global:
                        AddToGlobalTriggers(trigger);
                        break;
                }
            }

            _editor?.AddLogMessage($"TriggerManager: Registered trigger: {trigger.QualifiedName}");

            RaiseTriggerRegistered(trigger, wasReplacement);
        }
        
        #endregion
        
        #region Trigger Unregistration
        
        /// <inheritdoc />
        public bool UnregisterTrigger(string triggerId)
        {
            if (string.IsNullOrEmpty(triggerId))
                return false;
            
            if (_triggers.TryRemove(triggerId, out var trigger))
            {
                RemoveFromCollections(trigger);
                _editor?.AddLogMessage($"TriggerManager: Unregistered trigger: {trigger.QualifiedName}");
                RaiseTriggerUnregistered(trigger.TriggerType, trigger.BlockName, trigger.ItemName, 1);
                return true;
            }
            
            return false;
        }
        
        /// <inheritdoc />
        public int UnregisterBlockTriggers(TriggerType type, string blockName)
        {
            if (string.IsNullOrEmpty(blockName))
                return 0;
            
            int removed = 0;
            
            if (_blockTriggers.TryGetValue(blockName, out var typeTriggers))
            {
                if (typeTriggers.TryGetValue(type, out var triggers))
                {
                    lock (triggers)
                    {
                        foreach (var trigger in triggers.ToList())
                        {
                            _triggers.TryRemove(trigger.TriggerId, out _);
                            removed++;
                        }
                        triggers.Clear();
                    }
                }
            }
            
            if (removed > 0)
            {
                _editor?.AddLogMessage($"TriggerManager: Unregistered {removed} {type} triggers for block {blockName}");
                RaiseTriggerUnregistered(type, blockName, null, removed);
            }
            
            return removed;
        }
        
        /// <inheritdoc />
        public int UnregisterItemTriggers(TriggerType type, string blockName, string itemName)
        {
            if (string.IsNullOrEmpty(blockName) || string.IsNullOrEmpty(itemName))
                return 0;
            
            string key = GetItemKey(blockName, itemName);
            int removed = 0;
            
            if (_itemTriggers.TryGetValue(key, out var typeTriggers))
            {
                if (typeTriggers.TryGetValue(type, out var triggers))
                {
                    lock (triggers)
                    {
                        foreach (var trigger in triggers.ToList())
                        {
                            _triggers.TryRemove(trigger.TriggerId, out _);
                            removed++;
                        }
                        triggers.Clear();
                    }
                }
            }
            
            if (removed > 0)
            {
                _editor?.AddLogMessage($"TriggerManager: Unregistered {removed} {type} triggers for item {blockName}.{itemName}");
                RaiseTriggerUnregistered(type, blockName, itemName, removed);
            }
            
            return removed;
        }
        
        /// <inheritdoc />
        public int ClearBlockTriggers(string blockName)
        {
            if (string.IsNullOrEmpty(blockName))
                return 0;
            
            int removed = 0;
            
            if (_blockTriggers.TryRemove(blockName, out var typeTriggers))
            {
                foreach (var triggers in typeTriggers.Values)
                {
                    lock (triggers)
                    {
                        foreach (var trigger in triggers)
                        {
                            _triggers.TryRemove(trigger.TriggerId, out _);
                            removed++;
                        }
                    }
                }
            }
            
            // Also remove item triggers for this block
            var itemKeys = _itemTriggers.Keys.Where(k => k.StartsWith(blockName + ".", StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var key in itemKeys)
            {
                if (_itemTriggers.TryRemove(key, out var itemTypeTriggers))
                {
                    foreach (var triggers in itemTypeTriggers.Values)
                    {
                        lock (triggers)
                        {
                            foreach (var trigger in triggers)
                            {
                                _triggers.TryRemove(trigger.TriggerId, out _);
                                removed++;
                            }
                        }
                    }
                }
            }
            
            if (removed > 0)
            {
                _editor?.AddLogMessage($"TriggerManager: Cleared {removed} triggers for block {blockName}");
            }
            
            return removed;
        }
        
        /// <inheritdoc />
        public int ClearItemTriggers(string blockName, string itemName)
        {
            if (string.IsNullOrEmpty(blockName) || string.IsNullOrEmpty(itemName))
                return 0;
            
            string key = GetItemKey(blockName, itemName);
            int removed = 0;
            
            if (_itemTriggers.TryRemove(key, out var typeTriggers))
            {
                foreach (var triggers in typeTriggers.Values)
                {
                    lock (triggers)
                    {
                        foreach (var trigger in triggers)
                        {
                            _triggers.TryRemove(trigger.TriggerId, out _);
                            removed++;
                        }
                    }
                }
            }
            
            if (removed > 0)
            {
                _editor?.AddLogMessage($"TriggerManager: Cleared {removed} triggers for item {blockName}.{itemName}");
            }
            
            return removed;
        }
        
        /// <inheritdoc />
        public void ClearAllTriggers()
        {
            // Serialize the 5 dict clears under _lockObject so a concurrent
            // RegisterTrigger cannot land a new entry in one of the per-scope dicts
            // between the time we clear _triggers and the time we clear the per-scope
            // dict. Without the lock, the sequence
            //   thread A: _triggers.Clear(); _formTriggers.Clear();
            //   thread B: RegisterTrigger(...); [adds to _triggers then to _formTriggers]
            // can leave an orphan in _formTriggers that _triggers no longer knows
            // about. The lock is taken only for the 5 .Clear() calls (no per-list
            // locking needed here since we are emptying everything in one go).
            lock (_lockObject)
            {
                _triggers.Clear();
                _formTriggers.Clear();
                _blockTriggers.Clear();
                _itemTriggers.Clear();
                _globalTriggers.Clear();
            }

            _editor?.AddLogMessage("TriggerManager: Cleared all triggers");
        }
        
        #endregion
        
        #region Trigger Execution
        
        /// <inheritdoc />
        public TriggerResult FireFormTrigger(TriggerType type, string formName, TriggerContext context = null)
        {
            if (_suspended)
                return TriggerResult.Skipped;
            
            var triggers = GetFormTriggersForExecution(type, formName);
            return ExecuteTriggerChain(triggers, type, context ?? TriggerContext.ForForm(type, formName, _editor));
        }
        
        /// <inheritdoc />
        public async Task<TriggerResult> FireFormTriggerAsync(TriggerType type, string formName, TriggerContext context = null, CancellationToken cancellationToken = default)
        {
            if (_suspended)
                return TriggerResult.Skipped;
            
            var triggers = GetFormTriggersForExecution(type, formName);
            return await ExecuteTriggerChainAsync(triggers, type, context ?? TriggerContext.ForForm(type, formName, _editor), cancellationToken);
        }
        
        /// <inheritdoc />
        public TriggerResult FireBlockTrigger(TriggerType type, string blockName, TriggerContext context = null)
        {
            if (_suspended)
                return TriggerResult.Skipped;
            
            var triggers = GetBlockTriggersForExecution(type, blockName);
            context ??= TriggerContext.ForBlock(type, blockName, null, _editor);
            context.BlockName = blockName;
            
            return ExecuteTriggerChain(triggers, type, context);
        }
        
        /// <inheritdoc />
        public async Task<TriggerResult> FireBlockTriggerAsync(TriggerType type, string blockName, TriggerContext context = null, CancellationToken cancellationToken = default)
        {
            if (_suspended)
                return TriggerResult.Skipped;
            
            var triggers = GetBlockTriggersForExecution(type, blockName);
            context ??= TriggerContext.ForBlock(type, blockName, null, _editor);
            context.BlockName = blockName;
            
            return await ExecuteTriggerChainAsync(triggers, type, context, cancellationToken);
        }
        
        /// <inheritdoc />
        public TriggerResult FireItemTrigger(TriggerType type, string blockName, string itemName, TriggerContext context = null)
        {
            if (_suspended)
                return TriggerResult.Skipped;
            
            var triggers = GetItemTriggersForExecution(type, blockName, itemName);
            context ??= TriggerContext.ForItem(type, blockName, itemName, null, null, _editor);
            context.BlockName = blockName;
            context.ItemName = itemName;
            
            return ExecuteTriggerChain(triggers, type, context);
        }
        
        /// <inheritdoc />
        public async Task<TriggerResult> FireItemTriggerAsync(TriggerType type, string blockName, string itemName, TriggerContext context = null, CancellationToken cancellationToken = default)
        {
            if (_suspended)
                return TriggerResult.Skipped;
            
            var triggers = GetItemTriggersForExecution(type, blockName, itemName);
            context ??= TriggerContext.ForItem(type, blockName, itemName, null, null, _editor);
            context.BlockName = blockName;
            context.ItemName = itemName;
            
            return await ExecuteTriggerChainAsync(triggers, type, context, cancellationToken);
        }
        
        /// <inheritdoc />
        public TriggerResult FireGlobalTrigger(TriggerType type, TriggerContext context = null)
        {
            if (_suspended)
                return TriggerResult.Skipped;
            
            var triggers = GetGlobalTriggersForExecution(type);
            context ??= new TriggerContext { TriggerType = type, Scope = TriggerScope.Global, Editor = _editor };
            
            return ExecuteTriggerChain(triggers, type, context);
        }
        
        /// <inheritdoc />
        public async Task<TriggerResult> FireGlobalTriggerAsync(TriggerType type, TriggerContext context = null, CancellationToken cancellationToken = default)
        {
            if (_suspended)
                return TriggerResult.Skipped;
            
            var triggers = GetGlobalTriggersForExecution(type);
            context ??= new TriggerContext { TriggerType = type, Scope = TriggerScope.Global, Editor = _editor };
            
            return await ExecuteTriggerChainAsync(triggers, type, context, cancellationToken);
        }
        
        #endregion
        
        #region Trigger Query
        
        /// <inheritdoc />
        public TriggerDefinition GetTrigger(string triggerId)
        {
            if (string.IsNullOrEmpty(triggerId))
                return null;
            
            _triggers.TryGetValue(triggerId, out var trigger);
            return trigger;
        }
        
        /// <inheritdoc />
        public IReadOnlyList<TriggerDefinition> GetBlockTriggers(string blockName)
        {
            if (string.IsNullOrEmpty(blockName))
                return Array.Empty<TriggerDefinition>();

            if (_blockTriggers.TryGetValue(blockName, out var typeTriggers))
            {
                // typeTriggers is a ConcurrentDictionary, so .Values is a snapshot of
                // the value references. We then enumerate each inner List<TriggerDefinition>
                // under its own lock — without the lock, a concurrent
                // AddToBlockTriggers / RemoveFromBlockTriggers (which both lock the
                // inner list) could mutate the list while we are reading it, and
                // List<T> is not safe for concurrent enumeration. We lock each list
                // individually rather than taking a global lock so concurrent
                // registrations on OTHER trigger types for the SAME block are not
                // blocked.
                var result = new List<TriggerDefinition>();
                foreach (var triggers in typeTriggers.Values)
                {
                    lock (triggers)
                    {
                        result.AddRange(triggers);
                    }
                }
                return result.AsReadOnly();
            }

            return Array.Empty<TriggerDefinition>();
        }
        
        /// <inheritdoc />
        public IReadOnlyList<TriggerDefinition> GetBlockTriggers(TriggerType type, string blockName)
        {
            if (string.IsNullOrEmpty(blockName))
                return Array.Empty<TriggerDefinition>();
            
            if (_blockTriggers.TryGetValue(blockName, out var typeTriggers))
            {
                if (typeTriggers.TryGetValue(type, out var triggers))
                {
                    lock (triggers)
                    {
                        return triggers.ToList().AsReadOnly();
                    }
                }
            }
            
            return Array.Empty<TriggerDefinition>();
        }
        
        /// <inheritdoc />
        public IReadOnlyList<TriggerDefinition> GetItemTriggers(string blockName, string itemName)
        {
            if (string.IsNullOrEmpty(blockName) || string.IsNullOrEmpty(itemName))
                return Array.Empty<TriggerDefinition>();

            string key = GetItemKey(blockName, itemName);

            if (_itemTriggers.TryGetValue(key, out var typeTriggers))
            {
                // See GetBlockTriggers(string) for the locking rationale: each inner
                // List<TriggerDefinition> is locked individually so concurrent
                // registrations on other TriggerTypes for the same item do not
                // block this read.
                var result = new List<TriggerDefinition>();
                foreach (var triggers in typeTriggers.Values)
                {
                    lock (triggers)
                    {
                        result.AddRange(triggers);
                    }
                }
                return result.AsReadOnly();
            }

            return Array.Empty<TriggerDefinition>();
        }
        
        /// <inheritdoc />
        public IReadOnlyList<TriggerDefinition> GetFormTriggers(string formName)
        {
            if (string.IsNullOrEmpty(formName))
                return Array.Empty<TriggerDefinition>();

            if (_formTriggers.TryGetValue(formName, out var typeTriggers))
            {
                // See GetBlockTriggers(string) for the locking rationale.
                var result = new List<TriggerDefinition>();
                foreach (var triggers in typeTriggers.Values)
                {
                    lock (triggers)
                    {
                        result.AddRange(triggers);
                    }
                }
                return result.AsReadOnly();
            }

            return Array.Empty<TriggerDefinition>();
        }

        /// <inheritdoc />
        public IReadOnlyList<TriggerDefinition> GetGlobalTriggers()
        {
            // _globalTriggers is keyed by TriggerType directly; each value is the
            // same inner List<TriggerDefinition> pattern as the per-scope dicts.
            // Lock each list to be safe with concurrent AddToGlobalTriggers /
            // RemoveFromGlobalTriggers calls.
            var result = new List<TriggerDefinition>();
            foreach (var triggers in _globalTriggers.Values)
            {
                lock (triggers)
                {
                    result.AddRange(triggers);
                }
            }
            return result.AsReadOnly();
        }
        
        /// <inheritdoc />
        public IReadOnlyList<TriggerDefinition> GetTriggersByCategory(TriggerCategory category)
        {
            return _triggers.Values.Where(t => t.Category == category).ToList().AsReadOnly();
        }
        
        /// <inheritdoc />
        public bool HasBlockTrigger(TriggerType type, string blockName)
        {
            if (string.IsNullOrEmpty(blockName))
                return false;
            
            if (_blockTriggers.TryGetValue(blockName, out var typeTriggers))
            {
                if (typeTriggers.TryGetValue(type, out var triggers))
                {
                    lock (triggers)
                    {
                        return triggers.Any(t => t.IsEnabled);
                    }
                }
            }
            
            return false;
        }
        
        /// <inheritdoc />
        public bool HasItemTrigger(TriggerType type, string blockName, string itemName)
        {
            if (string.IsNullOrEmpty(blockName) || string.IsNullOrEmpty(itemName))
                return false;
            
            string key = GetItemKey(blockName, itemName);
            
            if (_itemTriggers.TryGetValue(key, out var typeTriggers))
            {
                if (typeTriggers.TryGetValue(type, out var triggers))
                {
                    lock (triggers)
                    {
                        return triggers.Any(t => t.IsEnabled);
                    }
                }
            }
            
            return false;
        }
        
        #endregion
        
        #region Trigger Enable/Disable
        
        /// <inheritdoc />
        public void EnableTrigger(string triggerId)
        {
            if (_triggers.TryGetValue(triggerId, out var trigger))
            {
                trigger.IsEnabled = true;
            }
        }
        
        /// <inheritdoc />
        public void DisableTrigger(string triggerId)
        {
            if (_triggers.TryGetValue(triggerId, out var trigger))
            {
                trigger.IsEnabled = false;
            }
        }
        
        /// <inheritdoc />
        public void EnableBlockTriggers(TriggerType type, string blockName)
        {
            foreach (var trigger in GetBlockTriggers(type, blockName))
            {
                trigger.IsEnabled = true;
            }
        }
        
        /// <inheritdoc />
        public void DisableBlockTriggers(TriggerType type, string blockName)
        {
            foreach (var trigger in GetBlockTriggers(type, blockName))
            {
                trigger.IsEnabled = false;
            }
        }
        
        /// <inheritdoc />
        public void EnableAllTriggers()
        {
            foreach (var trigger in _triggers.Values)
            {
                trigger.IsEnabled = true;
            }
        }
        
        /// <inheritdoc />
        public void DisableAllTriggers()
        {
            foreach (var trigger in _triggers.Values)
            {
                trigger.IsEnabled = false;
            }
        }
        
        /// <inheritdoc />
        public void SuspendTriggers()
        {
            _suspended = true;
            _editor?.AddLogMessage("TriggerManager: Triggers suspended");
        }
        
        /// <inheritdoc />
        public void ResumeTriggers()
        {
            _suspended = false;
            _editor?.AddLogMessage("TriggerManager: Triggers resumed");
        }
        
        #endregion
        
        #region Private Helper Methods
        
        private void AddToFormTriggers(TriggerDefinition trigger)
        {
            string key = trigger.FormName ?? "DEFAULT";
            var typeTriggers = _formTriggers.GetOrAdd(key, _ => new ConcurrentDictionary<TriggerType, List<TriggerDefinition>>());
            var triggers = typeTriggers.GetOrAdd(trigger.TriggerType, _ => new List<TriggerDefinition>());
            
            lock (triggers)
            {
                triggers.Add(trigger);
                triggers.Sort((a, b) => ((int)b.Priority).CompareTo((int)a.Priority));
            }
        }
        
        private void AddToBlockTriggers(TriggerDefinition trigger)
        {
            string key = trigger.BlockName ?? "DEFAULT";
            var typeTriggers = _blockTriggers.GetOrAdd(key, _ => new ConcurrentDictionary<TriggerType, List<TriggerDefinition>>());
            var triggers = typeTriggers.GetOrAdd(trigger.TriggerType, _ => new List<TriggerDefinition>());
            
            lock (triggers)
            {
                triggers.Add(trigger);
                triggers.Sort((a, b) => ((int)b.Priority).CompareTo((int)a.Priority));
            }
        }
        
        private void AddToItemTriggers(TriggerDefinition trigger)
        {
            string key = GetItemKey(trigger.BlockName, trigger.ItemName);
            var typeTriggers = _itemTriggers.GetOrAdd(key, _ => new ConcurrentDictionary<TriggerType, List<TriggerDefinition>>());
            var triggers = typeTriggers.GetOrAdd(trigger.TriggerType, _ => new List<TriggerDefinition>());
            
            lock (triggers)
            {
                triggers.Add(trigger);
                triggers.Sort((a, b) => ((int)b.Priority).CompareTo((int)a.Priority));
            }
        }
        
        private void AddToGlobalTriggers(TriggerDefinition trigger)
        {
            var triggers = _globalTriggers.GetOrAdd(trigger.TriggerType, _ => new List<TriggerDefinition>());
            
            lock (triggers)
            {
                triggers.Add(trigger);
                triggers.Sort((a, b) => ((int)b.Priority).CompareTo((int)a.Priority));
            }
        }
        
        private void RemoveFromCollections(TriggerDefinition trigger)
        {
            switch (trigger.Scope)
            {
                case TriggerScope.Form:
                    RemoveFromFormTriggers(trigger);
                    break;
                case TriggerScope.Block:
                    RemoveFromBlockTriggers(trigger);
                    break;
                case TriggerScope.Item:
                    RemoveFromItemTriggers(trigger);
                    break;
                case TriggerScope.Global:
                    RemoveFromGlobalTriggers(trigger);
                    break;
            }
        }
        
        private void RemoveFromFormTriggers(TriggerDefinition trigger)
        {
            string key = trigger.FormName ?? "DEFAULT";
            if (_formTriggers.TryGetValue(key, out var typeTriggers))
            {
                if (typeTriggers.TryGetValue(trigger.TriggerType, out var triggers))
                {
                    lock (triggers)
                    {
                        triggers.RemoveAll(t => t.TriggerId == trigger.TriggerId);
                    }
                }
            }
        }
        
        private void RemoveFromBlockTriggers(TriggerDefinition trigger)
        {
            string key = trigger.BlockName ?? "DEFAULT";
            if (_blockTriggers.TryGetValue(key, out var typeTriggers))
            {
                if (typeTriggers.TryGetValue(trigger.TriggerType, out var triggers))
                {
                    lock (triggers)
                    {
                        triggers.RemoveAll(t => t.TriggerId == trigger.TriggerId);
                    }
                }
            }
        }
        
        private void RemoveFromItemTriggers(TriggerDefinition trigger)
        {
            string key = GetItemKey(trigger.BlockName, trigger.ItemName);
            if (_itemTriggers.TryGetValue(key, out var typeTriggers))
            {
                if (typeTriggers.TryGetValue(trigger.TriggerType, out var triggers))
                {
                    lock (triggers)
                    {
                        triggers.RemoveAll(t => t.TriggerId == trigger.TriggerId);
                    }
                }
            }
        }
        
        private void RemoveFromGlobalTriggers(TriggerDefinition trigger)
        {
            if (_globalTriggers.TryGetValue(trigger.TriggerType, out var triggers))
            {
                lock (triggers)
                {
                    triggers.RemoveAll(t => t.TriggerId == trigger.TriggerId);
                }
            }
        }
        
        private string GetItemKey(string blockName, string itemName)
        {
            return $"{blockName ?? "BLOCK"}.{itemName ?? "ITEM"}";
        }
        
        private List<TriggerDefinition> GetFormTriggersForExecution(TriggerType type, string formName)
        {
            var result = new List<TriggerDefinition>();
            
            // Add global triggers first
            result.AddRange(GetGlobalTriggersForExecution(type));
            
            // Add form triggers
            if (_formTriggers.TryGetValue(formName ?? "DEFAULT", out var typeTriggers))
            {
                if (typeTriggers.TryGetValue(type, out var triggers))
                {
                    lock (triggers)
                    {
                        result.AddRange(triggers.Where(t => t.IsEnabled));
                    }
                }
            }
            
            return result.OrderByDescending(t => (int)t.Priority).ToList();
        }
        
        private List<TriggerDefinition> GetBlockTriggersForExecution(TriggerType type, string blockName)
        {
            var result = new List<TriggerDefinition>();
            
            // Add global triggers first
            result.AddRange(GetGlobalTriggersForExecution(type));
            
            // Add block triggers
            if (_blockTriggers.TryGetValue(blockName ?? "DEFAULT", out var typeTriggers))
            {
                if (typeTriggers.TryGetValue(type, out var triggers))
                {
                    lock (triggers)
                    {
                        result.AddRange(triggers.Where(t => t.IsEnabled));
                    }
                }
            }
            
            return result.OrderByDescending(t => (int)t.Priority).ToList();
        }
        
        private List<TriggerDefinition> GetItemTriggersForExecution(TriggerType type, string blockName, string itemName)
        {
            var result = new List<TriggerDefinition>();
            
            // Add global triggers first
            result.AddRange(GetGlobalTriggersForExecution(type));
            
            // Add block-level triggers
            result.AddRange(GetBlockTriggersForExecution(type, blockName).Where(t => !result.Any(r => r.TriggerId == t.TriggerId)));
            
            // Add item triggers
            string key = GetItemKey(blockName, itemName);
            if (_itemTriggers.TryGetValue(key, out var typeTriggers))
            {
                if (typeTriggers.TryGetValue(type, out var triggers))
                {
                    lock (triggers)
                    {
                        result.AddRange(triggers.Where(t => t.IsEnabled));
                    }
                }
            }
            
            return result.OrderByDescending(t => (int)t.Priority).ToList();
        }
        
        private List<TriggerDefinition> GetGlobalTriggersForExecution(TriggerType type)
        {
            if (_globalTriggers.TryGetValue(type, out var triggers))
            {
                lock (triggers)
                {
                    return triggers.Where(t => t.IsEnabled).ToList();
                }
            }
            return new List<TriggerDefinition>();
        }
        
        private TriggerResult ExecuteTriggerChain(List<TriggerDefinition> triggers, TriggerType type, TriggerContext context)
        {
            if (triggers == null || triggers.Count == 0)
                return TriggerResult.Skipped;
            
            var startTime = DateTime.UtcNow;
            int successCount = 0, failureCount = 0, skippedCount = 0;
            TriggerResult overallResult = TriggerResult.Success;
            bool cancelled = false;
            string cancelMessage = null;
            
            foreach (var trigger in triggers)
            {
                // Check for cancellation from previous trigger
                if (context.SkipRemainingTriggers || context.Cancel)
                {
                    skippedCount++;
                    continue;
                }
                
                // Fire executing event (allows cancellation)
                var executingArgs = new TriggerExecutingEventArgs { Trigger = trigger, Context = context };
                TriggerExecuting?.Invoke(this, executingArgs);
                
                if (executingArgs.Cancel)
                {
                    skippedCount++;
                    continue;
                }
                
                var triggerStart = DateTime.UtcNow;
                context.Trigger = trigger;
                
                TriggerResult result;
                Exception exception = null;
                
                try
                {
                    result = trigger.Execute(context);
                }
                catch (Exception ex)
                {
                    result = TriggerResult.Exception;
                    exception = ex;
                    _editor?.AddLogMessage($"TriggerManager: Trigger {trigger.QualifiedName} threw exception: {ex.Message}");
                }
                
                var triggerEnd = DateTime.UtcNow;
                
                // Fire executed event
                TriggerExecuted?.Invoke(this, new TriggerExecutedEventArgs
                {
                    Trigger = trigger,
                    Context = context,
                    Result = result,
                    DurationMs = (triggerEnd - triggerStart).TotalMilliseconds,
                    Exception = exception,
                    StartTime = triggerStart,
                    EndTime = triggerEnd
                });
                
                // Track results
                switch (result)
                {
                    case TriggerResult.Success:
                        successCount++;
                        break;
                    case TriggerResult.Skipped:
                        skippedCount++;
                        break;
                    default:
                        failureCount++;
                        if (result == TriggerResult.FormTriggerFailure || context.Cancel)
                        {
                            cancelled = true;
                            cancelMessage = context.CancelMessage;
                            overallResult = TriggerResult.FormTriggerFailure;
                        }
                        // B3 (audit pass 4, 2026-06): a trigger that
                        // returns Cancelled is NOT a FormTriggerFailure.
                        // The previous code fell into the `else if
                        // (overallResult == Success)` branch and set
                        // overallResult to Cancelled, but left the
                        // `cancelled` flag false — so the
                        // TriggerChainCompletedEventArgs reported
                        // WasCancelled = false even though the result
                        // was a cancellation. Now: set the cancelled
                        // flag, set the message, and keep
                        // overallResult as Cancelled.
                        else if (result == TriggerResult.Cancelled)
                        {
                            cancelled = true;
                            cancelMessage = context.CancelMessage ?? "Trigger returned Cancelled.";
                            overallResult = TriggerResult.Cancelled;
                        }
                        else if (overallResult == TriggerResult.Success)
                        {
                            overallResult = result;
                        }

                        // Stop chain if trigger raised failure and not set to continue
                        if (!trigger.ContinueOnFailure && result != TriggerResult.Skipped)
                        {
                            context.SkipRemainingTriggers = true;
                        }
                        break;
                }
            }

            // Fire chain completed event
            TriggerChainCompleted?.Invoke(this, new TriggerChainCompletedEventArgs
            {
                TriggerType = type,
                TriggerCount = triggers.Count,
                SuccessCount = successCount,
                FailureCount = failureCount,
                SkippedCount = skippedCount,
                TotalDurationMs = (DateTime.UtcNow - startTime).TotalMilliseconds,
                WasCancelled = cancelled,
                CancelMessage = cancelMessage,
                OverallResult = overallResult
            });

            return overallResult;
        }

        private async Task<TriggerResult> ExecuteTriggerChainAsync(List<TriggerDefinition> triggers, TriggerType type, TriggerContext context, CancellationToken cancellationToken)
        {
            if (triggers == null || triggers.Count == 0)
                return TriggerResult.Skipped;
            
            var startTime = DateTime.UtcNow;
            int successCount = 0, failureCount = 0, skippedCount = 0;
            TriggerResult overallResult = TriggerResult.Success;
            bool cancelled = false;
            string cancelMessage = null;
            
            foreach (var trigger in triggers)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    overallResult = TriggerResult.Cancelled;
                    break;
                }
                
                // Check for cancellation from previous trigger
                if (context.SkipRemainingTriggers || context.Cancel)
                {
                    skippedCount++;
                    continue;
                }
                
                // Fire executing event (allows cancellation)
                var executingArgs = new TriggerExecutingEventArgs { Trigger = trigger, Context = context };
                TriggerExecuting?.Invoke(this, executingArgs);
                
                if (executingArgs.Cancel)
                {
                    skippedCount++;
                    continue;
                }
                
                var triggerStart = DateTime.UtcNow;
                context.Trigger = trigger;
                
                TriggerResult result;
                Exception exception = null;
                
                try
                {
                    result = await trigger.ExecuteAsync(context, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    result = TriggerResult.Cancelled;
                }
                catch (Exception ex)
                {
                    result = TriggerResult.Exception;
                    exception = ex;
                    _editor?.AddLogMessage($"TriggerManager: Trigger {trigger.QualifiedName} threw exception: {ex.Message}");
                }
                
                var triggerEnd = DateTime.UtcNow;
                
                // Fire executed event
                TriggerExecuted?.Invoke(this, new TriggerExecutedEventArgs
                {
                    Trigger = trigger,
                    Context = context,
                    Result = result,
                    DurationMs = (triggerEnd - triggerStart).TotalMilliseconds,
                    Exception = exception,
                    StartTime = triggerStart,
                    EndTime = triggerEnd
                });
                
                // Track results
                switch (result)
                {
                    case TriggerResult.Success:
                        successCount++;
                        break;
                    case TriggerResult.Skipped:
                        skippedCount++;
                        break;
                    default:
                        failureCount++;
                        if (result == TriggerResult.FormTriggerFailure || context.Cancel)
                        {
                            cancelled = true;
                            cancelMessage = context.CancelMessage;
                            overallResult = TriggerResult.FormTriggerFailure;
                        }
                        // B3 (audit pass 4, 2026-06): see the sync
                        // path for the same fix. The async path had
                        // the same defect — a Cancelled result fell
                        // into the `else if (overallResult ==
                        // Success)` branch and was reported with
                        // WasCancelled = false.
                        else if (result == TriggerResult.Cancelled)
                        {
                            cancelled = true;
                            cancelMessage = context.CancelMessage ?? "Trigger returned Cancelled.";
                            overallResult = TriggerResult.Cancelled;
                        }
                        else if (overallResult == TriggerResult.Success)
                        {
                            overallResult = result;
                        }

                        // Stop chain if trigger raised failure and not set to continue
                        if (!trigger.ContinueOnFailure && result != TriggerResult.Skipped)
                        {
                            context.SkipRemainingTriggers = true;
                        }
                        break;
                }
            }

            // Fire chain completed event
            TriggerChainCompleted?.Invoke(this, new TriggerChainCompletedEventArgs
            {
                TriggerType = type,
                TriggerCount = triggers.Count,
                SuccessCount = successCount,
                FailureCount = failureCount,
                SkippedCount = skippedCount,
                TotalDurationMs = (DateTime.UtcNow - startTime).TotalMilliseconds,
                WasCancelled = cancelled,
                CancelMessage = cancelMessage,
                OverallResult = overallResult
            });
            
            return overallResult;
        }
        
        private void RaiseTriggerRegistered(TriggerDefinition trigger, bool wasReplacement)
        {
            TriggerRegistered?.Invoke(this, new TriggerRegisteredEventArgs
            {
                Trigger = trigger,
                BlockName = trigger.BlockName,
                ItemName = trigger.ItemName,
                WasReplacement = wasReplacement
            });
        }
        
        private void RaiseTriggerUnregistered(TriggerType type, string blockName, string itemName, int count)
        {
            TriggerUnregistered?.Invoke(this, new TriggerUnregisteredEventArgs
            {
                TriggerType = type,
                BlockName = blockName,
                ItemName = itemName,
                RemovedCount = count
            });
        }
        
        #endregion

        #region Statistics & Scope Helpers

        /// <inheritdoc />
        public TriggerStatisticsInfo GetTriggerStatistics(string blockName)
            => TriggerLibrary.GetTriggerStatistics(blockName, this);

        /// <inheritdoc />
        public IReadOnlyList<TriggerDefinition> GetFormLevelTriggers(string blockName)
            => TriggerLibrary.GetFormLevelTriggers(blockName, this);

        /// <inheritdoc />
        public IReadOnlyList<TriggerDefinition> GetBlockLevelTriggers(string blockName)
            => TriggerLibrary.GetBlockLevelTriggers(blockName, this);

        /// <inheritdoc />
        public IReadOnlyList<TriggerDefinition> GetRecordLevelTriggers(string blockName)
            => TriggerLibrary.GetRecordLevelTriggers(blockName, this);

        /// <inheritdoc />
        public IReadOnlyList<TriggerDefinition> GetItemLevelTriggers(string blockName)
            => TriggerLibrary.GetItemLevelTriggers(blockName, this);

        #endregion
        
        #region IDisposable
        
        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
                return;
            
            ClearAllTriggers();
            
            TriggerExecuting = null;
            TriggerExecuted = null;
            TriggerRegistered = null;
            TriggerUnregistered = null;
            TriggerChainCompleted = null;
            
            _disposed = true;
        }
        
        #endregion
    }
}
