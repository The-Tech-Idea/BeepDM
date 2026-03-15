using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.Forms.Models;
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
        private bool _suspended;
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
        public void RegisterTrigger(TriggerDefinition trigger)
        {
            if (trigger == null)
                throw new ArgumentNullException(nameof(trigger));
            
            bool wasReplacement = _triggers.ContainsKey(trigger.TriggerId);
            _triggers[trigger.TriggerId] = trigger;
            
            // Add to appropriate collection based on scope
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
            _triggers.Clear();
            _formTriggers.Clear();
            _blockTriggers.Clear();
            _itemTriggers.Clear();
            _globalTriggers.Clear();
            
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
                return typeTriggers.Values.SelectMany(t => t).ToList().AsReadOnly();
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
                return typeTriggers.Values.SelectMany(t => t).ToList().AsReadOnly();
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
                return typeTriggers.Values.SelectMany(t => t).ToList().AsReadOnly();
            }
            
            return Array.Empty<TriggerDefinition>();
        }
        
        /// <inheritdoc />
        public IReadOnlyList<TriggerDefinition> GetGlobalTriggers()
        {
            return _globalTriggers.Values.SelectMany(t => t).ToList().AsReadOnly();
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
