using System;
using System.Collections.Generic;
using System.Threading;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.UOWManager.Helpers
{
    /// <summary>
    /// Event management helper for UnitofWorksManager.
    /// Subscribes to available IUnitofWork DML/lifecycle events and translates
    /// them into FormsManager's event pipeline (DMLTriggerEventArgs,
    /// RecordTriggerEventArgs, ValidationTriggerEventArgs).
    /// Handler delegates and captured sources are owned by disposable leases.
    /// </summary>
    public class EventManager : IEventManager, IGatedUnitOfWorkEventSubscriptions
    {
        #region Fields
        private readonly IDMEEditor _dmeEditor;
        private readonly Dictionary<string, StoredHandlers> _subscriptions = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _lockObject = new object();
        #endregion

        #region Events
#pragma warning disable CS0067
        public event EventHandler<BlockTriggerEventArgs> OnBlockEnter;
        public event EventHandler<BlockTriggerEventArgs> OnBlockLeave;
        public event EventHandler<BlockTriggerEventArgs> OnBlockClear;
        public event EventHandler<BlockTriggerEventArgs> OnBlockValidate;

        public event EventHandler<RecordTriggerEventArgs> OnRecordEnter;
        public event EventHandler<RecordTriggerEventArgs> OnRecordLeave;
        public event EventHandler<RecordTriggerEventArgs> OnRecordValidate;

        public event EventHandler<DMLTriggerEventArgs> OnPreQuery;
        public event EventHandler<DMLTriggerEventArgs> OnPostQuery;
        public event EventHandler<DMLTriggerEventArgs> OnPreInsert;
        public event EventHandler<DMLTriggerEventArgs> OnPostInsert;
        public event EventHandler<DMLTriggerEventArgs> OnPreUpdate;
        public event EventHandler<DMLTriggerEventArgs> OnPostUpdate;
        public event EventHandler<DMLTriggerEventArgs> OnPreDelete;
        public event EventHandler<DMLTriggerEventArgs> OnPostDelete;
        public event EventHandler<DMLTriggerEventArgs> OnPreCommit;
        public event EventHandler<DMLTriggerEventArgs> OnPostCommit;

        public event EventHandler<ValidationTriggerEventArgs> OnValidateField;
        public event EventHandler<ValidationTriggerEventArgs> OnValidateRecord;
        public event EventHandler<ValidationTriggerEventArgs> OnValidateForm;

        public event EventHandler<ErrorTriggerEventArgs> OnError;
        public event EventHandler<CustomItemEventArgs> OnCustomItemEvent;
#pragma warning restore CS0067
        #endregion

        #region Constructor
        public EventManager(IDMEEditor dmeEditor)
        {
            _dmeEditor = dmeEditor ?? throw new ArgumentNullException(nameof(dmeEditor));
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Subscribes to all DML, lifecycle, navigation, and batch events on the
        /// unit of work and stores every delegate so Unsubscribe can remove them.
        /// </summary>
        public void SubscribeToUnitOfWorkEvents(IUnitofWork unitOfWork, string blockName)
        {
            if (unitOfWork == null || string.IsNullOrWhiteSpace(blockName)) return;
            var next = PrepareSubscription(unitOfWork, blockName);
            StoredHandlers previous;
            lock (_lockObject)
            {
                _subscriptions.TryGetValue(blockName, out previous);
                if (previous != null) previous.Active = false;
                _subscriptions[blockName] = next;
            }
            previous?.Dispose();
        }

        /// <summary>Creates an independent lease, without changing legacy block-name subscriptions.</summary>
        public IDisposable SubscribeOwned(IUnitofWork unitOfWork, string blockName) =>
            PrepareSubscription(unitOfWork, blockName);

        public IDisposable SubscribeOwned(IUnitofWork unitOfWork, string blockName, Func<bool> canDispatch) =>
            PrepareSubscription(unitOfWork, blockName, canDispatch ?? throw new ArgumentNullException(nameof(canDispatch)));

        private StoredHandlers PrepareSubscription(IUnitofWork unitOfWork, string blockName, Func<bool> canDispatch = null)
        {
            if (unitOfWork == null) throw new ArgumentNullException(nameof(unitOfWork));
            if (string.IsNullOrWhiteSpace(blockName)) throw new ArgumentNullException(nameof(blockName));
            var handlers = new StoredHandlers(blockName, canDispatch);
            try
            {
                handlers.PreInsert = (s, e) => { if (handlers.CanDispatch) HandlePreInsert(blockName, s, e); };
                handlers.PostInsert = (s, e) => { if (handlers.CanDispatch) HandlePostInsert(blockName, s, e); };
                handlers.PreUpdate = (s, e) => { if (handlers.CanDispatch) HandlePreUpdate(blockName, s, e); };
                handlers.PostUpdate = (s, e) => { if (handlers.CanDispatch) HandlePostUpdate(blockName, s, e); };
                handlers.PreDelete = (s, e) => { if (handlers.CanDispatch) HandlePreDelete(blockName, s, e); };
                handlers.PostDelete = (s, e) => { if (handlers.CanDispatch) HandlePostDelete(blockName, s, e); };
                handlers.PreCreate = (s, e) => { if (handlers.CanDispatch) HandlePreCreate(blockName, s, e); };
                handlers.PostCreate = (s, e) => { if (handlers.CanDispatch) HandlePostCreate(blockName, s, e); };
                handlers.PreQuery = (s, e) => { if (handlers.CanDispatch) HandlePreQuery(blockName, s, e); };
                handlers.PostQuery = (s, e) => { if (handlers.CanDispatch) HandlePostQuery(blockName, s, e); };
                handlers.PreCommit = (s, e) => { if (handlers.CanDispatch) HandlePreCommit(blockName, s, e); };
                handlers.PostCommit = (s, e) => { if (handlers.CanDispatch) HandlePostCommit(blockName, s, e); };
                handlers.PostEdit = (s, e) => { if (handlers.CanDispatch) HandlePostEdit(blockName, s, e); };
                handlers.OnItemReverted = (s, e) => { if (handlers.CanDispatch) HandleItemReverted(blockName, s, e); };

                handlers.PreBatchInsert = (s, e) => { if (handlers.CanDispatch) HandlePreBatchInsert(blockName, s, e); };
                handlers.PostBatchInsert = (s, e) => { if (handlers.CanDispatch) HandlePostBatchInsert(blockName, s, e); };
                handlers.PreBatchUpdate = (s, e) => { if (handlers.CanDispatch) HandlePreBatchUpdate(blockName, s, e); };
                handlers.PostBatchUpdate = (s, e) => { if (handlers.CanDispatch) HandlePostBatchUpdate(blockName, s, e); };
                handlers.PreBatchDelete = (s, e) => { if (handlers.CanDispatch) HandlePreBatchDelete(blockName, s, e); };
                handlers.PostBatchDelete = (s, e) => { if (handlers.CanDispatch) HandlePostBatchDelete(blockName, s, e); };
                handlers.PreRollback = (s, e) => { if (handlers.CanDispatch) HandlePreRollback(blockName, s, e); };
                handlers.PostRollback = (s, e) => { if (handlers.CanDispatch) HandlePostRollback(blockName, s, e); };

                handlers.CurrentChanged = (s, e) => { if (handlers.CanDispatch) HandleCurrentChanged(blockName, s, e); };


                Attach(handlers, () => unitOfWork.PreInsert += handlers.PreInsert, () => unitOfWork.PreInsert -= handlers.PreInsert);
                Attach(handlers, () => unitOfWork.PostInsert += handlers.PostInsert, () => unitOfWork.PostInsert -= handlers.PostInsert);
                Attach(handlers, () => unitOfWork.PreUpdate += handlers.PreUpdate, () => unitOfWork.PreUpdate -= handlers.PreUpdate);
                Attach(handlers, () => unitOfWork.PostUpdate += handlers.PostUpdate, () => unitOfWork.PostUpdate -= handlers.PostUpdate);
                Attach(handlers, () => unitOfWork.PreDelete += handlers.PreDelete, () => unitOfWork.PreDelete -= handlers.PreDelete);
                Attach(handlers, () => unitOfWork.PostDelete += handlers.PostDelete, () => unitOfWork.PostDelete -= handlers.PostDelete);
                Attach(handlers, () => unitOfWork.PreCreate += handlers.PreCreate, () => unitOfWork.PreCreate -= handlers.PreCreate);
                Attach(handlers, () => unitOfWork.PostCreate += handlers.PostCreate, () => unitOfWork.PostCreate -= handlers.PostCreate);
                Attach(handlers, () => unitOfWork.PreQuery += handlers.PreQuery, () => unitOfWork.PreQuery -= handlers.PreQuery);
                Attach(handlers, () => unitOfWork.PostQuery += handlers.PostQuery, () => unitOfWork.PostQuery -= handlers.PostQuery);
                Attach(handlers, () => unitOfWork.PreCommit += handlers.PreCommit, () => unitOfWork.PreCommit -= handlers.PreCommit);
                Attach(handlers, () => unitOfWork.PostCommit += handlers.PostCommit, () => unitOfWork.PostCommit -= handlers.PostCommit);
                Attach(handlers, () => unitOfWork.PostEdit += handlers.PostEdit, () => unitOfWork.PostEdit -= handlers.PostEdit);
                TrySubscribe(handlers, unitOfWork, "OnItemReverted",
                    () => ((dynamic)unitOfWork).OnItemReverted += handlers.OnItemReverted,
                    () => ((dynamic)unitOfWork).OnItemReverted -= handlers.OnItemReverted);
                TrySubscribe(handlers, unitOfWork, "PreBatchInsert",
                    () => ((dynamic)unitOfWork).PreBatchInsert += handlers.PreBatchInsert,
                    () => ((dynamic)unitOfWork).PreBatchInsert -= handlers.PreBatchInsert);
                TrySubscribe(handlers, unitOfWork, "PostBatchInsert",
                    () => ((dynamic)unitOfWork).PostBatchInsert += handlers.PostBatchInsert,
                    () => ((dynamic)unitOfWork).PostBatchInsert -= handlers.PostBatchInsert);
                TrySubscribe(handlers, unitOfWork, "PreBatchUpdate",
                    () => ((dynamic)unitOfWork).PreBatchUpdate += handlers.PreBatchUpdate,
                    () => ((dynamic)unitOfWork).PreBatchUpdate -= handlers.PreBatchUpdate);
                TrySubscribe(handlers, unitOfWork, "PostBatchUpdate",
                    () => ((dynamic)unitOfWork).PostBatchUpdate += handlers.PostBatchUpdate,
                    () => ((dynamic)unitOfWork).PostBatchUpdate -= handlers.PostBatchUpdate);
                TrySubscribe(handlers, unitOfWork, "PreBatchDelete",
                    () => ((dynamic)unitOfWork).PreBatchDelete += handlers.PreBatchDelete,
                    () => ((dynamic)unitOfWork).PreBatchDelete -= handlers.PreBatchDelete);
                TrySubscribe(handlers, unitOfWork, "PostBatchDelete",
                    () => ((dynamic)unitOfWork).PostBatchDelete += handlers.PostBatchDelete,
                    () => ((dynamic)unitOfWork).PostBatchDelete -= handlers.PostBatchDelete);
                TrySubscribe(handlers, unitOfWork, "PreRollback",
                    () => ((dynamic)unitOfWork).PreRollback += handlers.PreRollback,
                    () => ((dynamic)unitOfWork).PreRollback -= handlers.PreRollback);
                TrySubscribe(handlers, unitOfWork, "PostRollback",
                    () => ((dynamic)unitOfWork).PostRollback += handlers.PostRollback,
                    () => ((dynamic)unitOfWork).PostRollback -= handlers.PostRollback);
                Attach(handlers, () => unitOfWork.CurrentChanged += handlers.CurrentChanged,
                    () => unitOfWork.CurrentChanged -= handlers.CurrentChanged);
                LogOperation($"Subscribed to all events for block '{blockName}'");
                handlers.Active = true;
                return handlers;
            }
            catch (Exception failure)
            {
                try { handlers.Dispose(); }
                catch (Exception cleanup)
                {
                    throw new AggregateException($"Subscription and rollback failed for block '{blockName}'.", failure, cleanup);
                }
                throw;
            }
        }

        /// <summary>Retires the legacy name-keyed subscription using its captured source.</summary>
        public void UnsubscribeFromUnitOfWorkEvents(IUnitofWork unitOfWork, string blockName)
        {
            if (unitOfWork == null || string.IsNullOrWhiteSpace(blockName)) return;
            StoredHandlers handlers;
            lock (_lockObject)
            {
                if (!_subscriptions.Remove(blockName, out handlers)) return;
                handlers.Active = false;
            }
            handlers.Dispose();
            LogOperation($"Unsubscribed from events for block '{blockName}'");
        }

        public void TriggerBlockEnter(string blockName)
        {
            try { OnBlockEnter?.Invoke(this, new BlockTriggerEventArgs(blockName, "Block entered")); }
            catch (Exception ex) { LogError($"Block enter error for '{blockName}'", ex); }
        }

        public void TriggerBlockLeave(string blockName)
        {
            try { OnBlockLeave?.Invoke(this, new BlockTriggerEventArgs(blockName, "Block leaving")); }
            catch (Exception ex) { LogError($"Block leave error for '{blockName}'", ex); }
        }

        public void TriggerError(string blockName, Exception ex)
        {
            try { OnError?.Invoke(this, new ErrorTriggerEventArgs(blockName, ex.Message, ex)); }
            catch (Exception triggerEx) { LogError($"Error event error for '{blockName}'", triggerEx); }
        }

        public bool TriggerFieldValidation(string blockName, string fieldName, object value)
        {
            try
            {
                var args = new ValidationTriggerEventArgs(blockName, fieldName, value);
                OnValidateField?.Invoke(this, args);
                return args.IsValid;
            }
            catch (Exception ex) { LogError($"Field validation error for '{fieldName}' in '{blockName}'", ex); return false; }
        }

        public bool TriggerRecordValidation(string blockName, object record)
        {
            try
            {
                var args = new ValidationTriggerEventArgs(blockName, null, record);
                OnValidateRecord?.Invoke(this, args);
                return args.IsValid;
            }
            catch (Exception ex) { LogError($"Record validation error in '{blockName}'", ex); return false; }
        }

        public bool TriggerCustomItemEvent(string eventType, string blockName, string itemName, object payload = null)
        {
            try
            {
                var args = new CustomItemEventArgs(eventType, blockName, itemName, payload);
                OnCustomItemEvent?.Invoke(this, args);
                return !args.Cancel;
            }
            catch (Exception ex) { LogError($"CustomItemEvent '{eventType}' error for '{itemName}' in '{blockName}'", ex); return false; }
        }

        #endregion

        #region DML Handlers

        private void HandlePreInsert(string blockName, object sender, UnitofWorkParams e)
        {
            try
            {
                var args = new DMLTriggerEventArgs(blockName, DMLOperation.Insert, e) { CurrentRecord = sender };
                OnPreInsert?.Invoke(this, args);
                if (args.CurrentRecord != null) e.Record = args.CurrentRecord;
                e.Cancel = args.Cancel;
            }
            catch (Exception ex) { LogError($"PreInsert handler error for '{blockName}'", ex); }
        }

        private void HandlePostInsert(string blockName, object sender, UnitofWorkParams e)
        {
            try { OnPostInsert?.Invoke(this, new DMLTriggerEventArgs(blockName, DMLOperation.Insert, e) { CurrentRecord = sender }); }
            catch (Exception ex) { LogError($"PostInsert handler error for '{blockName}'", ex); }
        }

        private void HandlePreUpdate(string blockName, object sender, UnitofWorkParams e)
        {
            try
            {
                var args = new DMLTriggerEventArgs(blockName, DMLOperation.Update, e) { CurrentRecord = sender };
                OnPreUpdate?.Invoke(this, args);
                if (args.CurrentRecord != null) e.Record = args.CurrentRecord;
                e.Cancel = args.Cancel;
            }
            catch (Exception ex) { LogError($"PreUpdate handler error for '{blockName}'", ex); }
        }

        private void HandlePostUpdate(string blockName, object sender, UnitofWorkParams e)
        {
            try { OnPostUpdate?.Invoke(this, new DMLTriggerEventArgs(blockName, DMLOperation.Update, e) { CurrentRecord = sender }); }
            catch (Exception ex) { LogError($"PostUpdate handler error for '{blockName}'", ex); }
        }

        private void HandlePreDelete(string blockName, object sender, UnitofWorkParams e)
        {
            try
            {
                var args = new DMLTriggerEventArgs(blockName, DMLOperation.Delete, e) { CurrentRecord = sender };
                OnPreDelete?.Invoke(this, args);
                e.Cancel = args.Cancel;
            }
            catch (Exception ex) { LogError($"PreDelete handler error for '{blockName}'", ex); }
        }

        private void HandlePostDelete(string blockName, object sender, UnitofWorkParams e)
        {
            try { OnPostDelete?.Invoke(this, new DMLTriggerEventArgs(blockName, DMLOperation.Delete, e) { CurrentRecord = sender }); }
            catch (Exception ex) { LogError($"PostDelete handler error for '{blockName}'", ex); }
        }

        private void HandlePreCreate(string blockName, object sender, UnitofWorkParams e)
        {
            try
            {
                var args = new DMLTriggerEventArgs(blockName, DMLOperation.Insert, e) { CurrentRecord = sender };
                OnPreInsert?.Invoke(this, args);
                if (args.CurrentRecord != null) e.Record = args.CurrentRecord;
                e.Cancel = args.Cancel;
            }
            catch (Exception ex) { LogError($"PreCreate handler error for '{blockName}'", ex); }
        }

        private void HandlePostCreate(string blockName, object sender, UnitofWorkParams e)
        {
            try { OnPostInsert?.Invoke(this, new DMLTriggerEventArgs(blockName, DMLOperation.Insert, e) { CurrentRecord = sender }); }
            catch (Exception ex) { LogError($"PostCreate handler error for '{blockName}'", ex); }
        }

        private void HandlePreQuery(string blockName, object sender, UnitofWorkParams e)
        {
            try
            {
                var args = new DMLTriggerEventArgs(blockName, DMLOperation.Query, e) { CurrentRecord = sender };
                OnPreQuery?.Invoke(this, args);
                e.Cancel = args.Cancel;
            }
            catch (Exception ex) { LogError($"PreQuery handler error for '{blockName}'", ex); }
        }

        private void HandlePostQuery(string blockName, object sender, UnitofWorkParams e)
        {
            try { OnPostQuery?.Invoke(this, new DMLTriggerEventArgs(blockName, DMLOperation.Query, e) { CurrentRecord = sender }); }
            catch (Exception ex) { LogError($"PostQuery handler error for '{blockName}'", ex); }
        }

        private void HandlePreCommit(string blockName, object sender, UnitofWorkParams e)
        {
            try
            {
                var args = new DMLTriggerEventArgs(blockName, DMLOperation.Commit, e) { CurrentRecord = sender };
                OnPreCommit?.Invoke(this, args);
                e.Cancel = args.Cancel;
            }
            catch (Exception ex) { LogError($"PreCommit handler error for '{blockName}'", ex); }
        }

        private void HandlePostCommit(string blockName, object sender, UnitofWorkParams e)
        {
            try { OnPostCommit?.Invoke(this, new DMLTriggerEventArgs(blockName, DMLOperation.Commit, e) { CurrentRecord = sender }); }
            catch (Exception ex) { LogError($"PostCommit handler error for '{blockName}'", ex); }
        }

        private void HandlePostEdit(string blockName, object sender, UnitofWorkParams e)
        {
            try
            {
                var args = new DMLTriggerEventArgs(blockName, DMLOperation.Update, e) { CurrentRecord = sender };
                OnPostUpdate?.Invoke(this, args);
            }
            catch (Exception ex) { LogError($"PostEdit handler error for '{blockName}'", ex); }
        }

        private void HandleItemReverted(string blockName, object sender, UnitofWorkParams e)
        {
            try { LogOperation($"Item reverted in block '{blockName}'"); }
            catch (Exception ex) { LogError($"ItemReverted handler error for '{blockName}'", ex); }
        }

        private void HandlePreBatchInsert(string blockName, object sender, UnitofWorkParams e)
        {
            try { LogOperation($"PreBatchInsert for block '{blockName}'"); }
            catch (Exception ex) { LogError($"PreBatchInsert handler error for '{blockName}'", ex); }
        }

        private void HandlePostBatchInsert(string blockName, object sender, UnitofWorkParams e)
        {
            try { LogOperation($"PostBatchInsert for block '{blockName}'"); }
            catch (Exception ex) { LogError($"PostBatchInsert handler error for '{blockName}'", ex); }
        }

        private void HandlePreBatchUpdate(string blockName, object sender, UnitofWorkParams e)
        {
            try { LogOperation($"PreBatchUpdate for block '{blockName}'"); }
            catch (Exception ex) { LogError($"PreBatchUpdate handler error for '{blockName}'", ex); }
        }

        private void HandlePostBatchUpdate(string blockName, object sender, UnitofWorkParams e)
        {
            try { LogOperation($"PostBatchUpdate for block '{blockName}'"); }
            catch (Exception ex) { LogError($"PostBatchUpdate handler error for '{blockName}'", ex); }
        }

        private void HandlePreBatchDelete(string blockName, object sender, UnitofWorkParams e)
        {
            try { LogOperation($"PreBatchDelete for block '{blockName}'"); }
            catch (Exception ex) { LogError($"PreBatchDelete handler error for '{blockName}'", ex); }
        }

        private void HandlePostBatchDelete(string blockName, object sender, UnitofWorkParams e)
        {
            try { LogOperation($"PostBatchDelete for block '{blockName}'"); }
            catch (Exception ex) { LogError($"PostBatchDelete handler error for '{blockName}'", ex); }
        }

        private void HandlePreRollback(string blockName, object sender, UnitofWorkParams e)
        {
            try { LogOperation($"PreRollback for block '{blockName}'"); }
            catch (Exception ex) { LogError($"PreRollback handler error for '{blockName}'", ex); }
        }

        private void HandlePostRollback(string blockName, object sender, UnitofWorkParams e)
        {
            try { LogOperation($"PostRollback for block '{blockName}'"); }
            catch (Exception ex) { LogError($"PostRollback handler error for '{blockName}'", ex); }
        }

        #endregion

        #region Navigation

        private void HandleCurrentChanged(string blockName, object sender, EventArgs e)
        {
            try { OnRecordEnter?.Invoke(this, new RecordTriggerEventArgs(blockName, sender, "Current record changed")); }
            catch (Exception ex) { LogError($"CurrentChanged handler error for '{blockName}'", ex); }
        }

        #endregion

        #region Storage

        /// <summary>
        /// Holds all handler delegates for a single block so Unsubscribe can
        /// remove every one. Each delegate is a named field so -= works.
        /// Also stores optional cleanup actions for events not on the base interface.
        /// </summary>
        private sealed class StoredHandlers : IDisposable
        {
            private readonly string _blockName;
            private readonly Func<bool> _canDispatch;
            private int _retired;
            public volatile bool Active;
            public readonly List<Action> Cleanup = new();
            public StoredHandlers(string blockName, Func<bool> canDispatch)
            { _blockName = blockName; _canDispatch = canDispatch; }

            public bool CanDispatch => Active && (_canDispatch?.Invoke() ?? true);

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _retired, 1) != 0) return;
                Active = false;
                var failures = new List<Exception>();
                foreach (var cleanup in Cleanup)
                {
                    try { cleanup(); }
                    catch (Exception ex) { failures.Add(ex); }
                }
                Cleanup.Clear();
                if (failures.Count > 0)
                    throw new AggregateException($"Event teardown failed for block '{_blockName}'.", failures);
            }
            public EventHandler<UnitofWorkParams> PreInsert = null!;
            public EventHandler<UnitofWorkParams> PostInsert = null!;
            public EventHandler<UnitofWorkParams> PreUpdate = null!;
            public EventHandler<UnitofWorkParams> PostUpdate = null!;
            public EventHandler<UnitofWorkParams> PreDelete = null!;
            public EventHandler<UnitofWorkParams> PostDelete = null!;
            public EventHandler<UnitofWorkParams> PreCreate = null!;
            public EventHandler<UnitofWorkParams> PostCreate = null!;
            public EventHandler<UnitofWorkParams> PreQuery = null!;
            public EventHandler<UnitofWorkParams> PostQuery = null!;
            public EventHandler<UnitofWorkParams> PreCommit = null!;
            public EventHandler<UnitofWorkParams> PostCommit = null!;
            public EventHandler<UnitofWorkParams> PostEdit = null!;
            public EventHandler<UnitofWorkParams> OnItemReverted = null!;
            public EventHandler<UnitofWorkParams> PreBatchInsert = null!;
            public EventHandler<UnitofWorkParams> PostBatchInsert = null!;
            public EventHandler<UnitofWorkParams> PreBatchUpdate = null!;
            public EventHandler<UnitofWorkParams> PostBatchUpdate = null!;
            public EventHandler<UnitofWorkParams> PreBatchDelete = null!;
            public EventHandler<UnitofWorkParams> PostBatchDelete = null!;
            public EventHandler<UnitofWorkParams> PreRollback = null!;
            public EventHandler<UnitofWorkParams> PostRollback = null!;
            public EventHandler CurrentChanged = null!;
        }

        /// <summary>
        /// Captures cleanup even if the add accessor mutates its source and then throws.
        /// </summary>
        private static void Attach(StoredHandlers handlers, Action subscribe, Action unsubscribe)
        {
            // Record before invoking: an accessor can attach and then throw.
            handlers.Cleanup.Add(unsubscribe);
            subscribe();
        }

        private static void TrySubscribe(StoredHandlers handlers, IUnitofWork unitOfWork,
            string eventName, Action subscribe, Action unsubscribe)
        {
            // Missing optional events are expected; present but failing accessors are not.
            if (unitOfWork.GetType().GetEvent(eventName) == null) return;
            Attach(handlers, subscribe, unsubscribe);
        }

        private void LogOperation(string message)
        {
            _dmeEditor?.AddLogMessage("EventManager", message, DateTime.Now, 0, null, Errors.Ok);
        }

        private void LogError(string message, Exception ex)
        {
            _dmeEditor?.AddLogMessage("EventManager", $"{message}: {ex?.Message}", DateTime.Now, -1, null, Errors.Failed);
        }

        #endregion
    }
}
