using System;
using System.Collections.Generic;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.UOWManager.Helpers
{
    /// <summary>
    /// Event management helper for UnitofWorksManager.
    /// Subscribes to all 17 IUnitofWork DML/lifecycle events and translates
    /// them into FormsManager's event pipeline (DMLTriggerEventArgs,
    /// RecordTriggerEventArgs, ValidationTriggerEventArgs).
    /// Handler delegates are stored so Unsubscribe can remove every one.
    /// </summary>
    public class EventManager : IEventManager
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
            if (unitOfWork == null || string.IsNullOrWhiteSpace(blockName))
                return;

            try
            {
                var handlers = new StoredHandlers();

                handlers.PreInsert = (s, e) => HandlePreInsert(blockName, s, e);
                handlers.PostInsert = (s, e) => HandlePostInsert(blockName, s, e);
                handlers.PreUpdate = (s, e) => HandlePreUpdate(blockName, s, e);
                handlers.PostUpdate = (s, e) => HandlePostUpdate(blockName, s, e);
                handlers.PreDelete = (s, e) => HandlePreDelete(blockName, s, e);
                handlers.PostDelete = (s, e) => HandlePostDelete(blockName, s, e);
                handlers.PreCreate = (s, e) => HandlePreCreate(blockName, s, e);
                handlers.PostCreate = (s, e) => HandlePostCreate(blockName, s, e);
                handlers.PreQuery = (s, e) => HandlePreQuery(blockName, s, e);
                handlers.PostQuery = (s, e) => HandlePostQuery(blockName, s, e);
                handlers.PreCommit = (s, e) => HandlePreCommit(blockName, s, e);
                handlers.PostCommit = (s, e) => HandlePostCommit(blockName, s, e);
                handlers.PostEdit = (s, e) => HandlePostEdit(blockName, s, e);
                handlers.OnItemReverted = (s, e) => HandleItemReverted(blockName, s, e);

                handlers.PreBatchInsert = (s, e) => HandlePreBatchInsert(blockName, s, e);
                handlers.PostBatchInsert = (s, e) => HandlePostBatchInsert(blockName, s, e);
                handlers.PreBatchUpdate = (s, e) => HandlePreBatchUpdate(blockName, s, e);
                handlers.PostBatchUpdate = (s, e) => HandlePostBatchUpdate(blockName, s, e);
                handlers.PreBatchDelete = (s, e) => HandlePreBatchDelete(blockName, s, e);
                handlers.PostBatchDelete = (s, e) => HandlePostBatchDelete(blockName, s, e);
                handlers.PreRollback = (s, e) => HandlePreRollback(blockName, s, e);
                handlers.PostRollback = (s, e) => HandlePostRollback(blockName, s, e);

                handlers.CurrentChanged = (s, e) => HandleCurrentChanged(blockName, s, e);

                unitOfWork.PreInsert += handlers.PreInsert;
                unitOfWork.PostInsert += handlers.PostInsert;
                unitOfWork.PreUpdate += handlers.PreUpdate;
                unitOfWork.PostUpdate += handlers.PostUpdate;
                unitOfWork.PreDelete += handlers.PreDelete;
                unitOfWork.PostDelete += handlers.PostDelete;
                unitOfWork.PreCreate += handlers.PreCreate;
                unitOfWork.PostCreate += handlers.PostCreate;
                unitOfWork.PreQuery += handlers.PreQuery;
                unitOfWork.PostQuery += handlers.PostQuery;
                unitOfWork.PreCommit += handlers.PreCommit;
                unitOfWork.PostCommit += handlers.PostCommit;
                unitOfWork.PostEdit += handlers.PostEdit;

                // Optional events only on IUnitofWork<T>, not on non-generic base.
                // Use string names to avoid compile-time binding to the missing member.
                TrySubscribe(unitOfWork, "OnItemReverted",
                    () => ((dynamic)unitOfWork).OnItemReverted += handlers.OnItemReverted,
                    () => ((dynamic)unitOfWork).OnItemReverted -= handlers.OnItemReverted);
                TrySubscribe(unitOfWork, "PreBatchInsert",
                    () => ((dynamic)unitOfWork).PreBatchInsert += handlers.PreBatchInsert,
                    () => ((dynamic)unitOfWork).PreBatchInsert -= handlers.PreBatchInsert);
                TrySubscribe(unitOfWork, "PostBatchInsert",
                    () => ((dynamic)unitOfWork).PostBatchInsert += handlers.PostBatchInsert,
                    () => ((dynamic)unitOfWork).PostBatchInsert -= handlers.PostBatchInsert);
                TrySubscribe(unitOfWork, "PreBatchUpdate",
                    () => ((dynamic)unitOfWork).PreBatchUpdate += handlers.PreBatchUpdate,
                    () => ((dynamic)unitOfWork).PreBatchUpdate -= handlers.PreBatchUpdate);
                TrySubscribe(unitOfWork, "PostBatchUpdate",
                    () => ((dynamic)unitOfWork).PostBatchUpdate += handlers.PostBatchUpdate,
                    () => ((dynamic)unitOfWork).PostBatchUpdate -= handlers.PostBatchUpdate);
                TrySubscribe(unitOfWork, "PreBatchDelete",
                    () => ((dynamic)unitOfWork).PreBatchDelete += handlers.PreBatchDelete,
                    () => ((dynamic)unitOfWork).PreBatchDelete -= handlers.PreBatchDelete);
                TrySubscribe(unitOfWork, "PostBatchDelete",
                    () => ((dynamic)unitOfWork).PostBatchDelete += handlers.PostBatchDelete,
                    () => ((dynamic)unitOfWork).PostBatchDelete -= handlers.PostBatchDelete);
                TrySubscribe(unitOfWork, "PreRollback",
                    () => ((dynamic)unitOfWork).PreRollback += handlers.PreRollback,
                    () => ((dynamic)unitOfWork).PreRollback -= handlers.PreRollback);
                TrySubscribe(unitOfWork, "PostRollback",
                    () => ((dynamic)unitOfWork).PostRollback += handlers.PostRollback,
                    () => ((dynamic)unitOfWork).PostRollback -= handlers.PostRollback);

                unitOfWork.CurrentChanged += handlers.CurrentChanged;

                lock (_lockObject)
                {
                    _subscriptions[blockName] = handlers;
                }

                LogOperation($"Subscribed to all events for block '{blockName}'");
            }
            catch (Exception ex)
            {
                LogError($"Error subscribing to events for block '{blockName}'", ex);
            }
        }

        /// <summary>
        /// Removes every event handler previously subscribed for the block.
        /// Safe to call on already-unsubscribed blocks (no-op).
        /// </summary>
        public void UnsubscribeFromUnitOfWorkEvents(IUnitofWork unitOfWork, string blockName)
        {
            if (unitOfWork == null || string.IsNullOrWhiteSpace(blockName))
                return;

            StoredHandlers? handlers;
            lock (_lockObject)
            {
                if (!_subscriptions.Remove(blockName, out handlers))
                    return;
            }

            try
            {
                unitOfWork.PreInsert -= handlers.PreInsert;
                unitOfWork.PostInsert -= handlers.PostInsert;
                unitOfWork.PreUpdate -= handlers.PreUpdate;
                unitOfWork.PostUpdate -= handlers.PostUpdate;
                unitOfWork.PreDelete -= handlers.PreDelete;
                unitOfWork.PostDelete -= handlers.PostDelete;
                unitOfWork.PreCreate -= handlers.PreCreate;
                unitOfWork.PostCreate -= handlers.PostCreate;
                unitOfWork.PreQuery -= handlers.PreQuery;
                unitOfWork.PostQuery -= handlers.PostQuery;
                unitOfWork.PreCommit -= handlers.PreCommit;
                unitOfWork.PostCommit -= handlers.PostCommit;
                unitOfWork.PostEdit -= handlers.PostEdit;

                unitOfWork.CurrentChanged -= handlers.CurrentChanged;

                LogOperation($"Unsubscribed from events for block '{blockName}'");
            }
            catch (Exception ex)
            {
                LogError($"Error unsubscribing from events for block '{blockName}'", ex);
            }
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
        private sealed class StoredHandlers
        {
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
        /// Subscribes to an optional event that may not exist on the non-generic
        /// IUnitofWork interface. Stores the cleanup action for unsubscribe.
        /// </summary>
        private static void TrySubscribe(IUnitofWork unitOfWork, string eventName, Action subscribe, Action unsubscribe)
        {
            try { subscribe(); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EventManager] Optional event '{eventName}' not available on this IUnitofWork instance: {ex.Message}");
            }
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
