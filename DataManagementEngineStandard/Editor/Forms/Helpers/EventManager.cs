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
    /// Event management helper for UnitofWorksManager
    /// </summary>
    public class EventManager : IEventManager
    {
        #region Fields
        private readonly IDMEEditor _dmeEditor;
        private readonly Dictionary<string, List<WeakReference>> _eventSubscriptions = new();
        private readonly object _lockObject = new object();
        #endregion

        #region Events
    #pragma warning disable CS0067
        // Block-level triggers
        /// <summary>
        /// Raised when a block is entered.
        /// </summary>
        public event EventHandler<BlockTriggerEventArgs> OnBlockEnter;
        /// <summary>
        /// Raised when a block is left.
        /// </summary>
        public event EventHandler<BlockTriggerEventArgs> OnBlockLeave;
        /// <summary>
        /// Raised when a block clear operation is triggered.
        /// </summary>
        public event EventHandler<BlockTriggerEventArgs> OnBlockClear;
        /// <summary>
        /// Raised when block-level validation is triggered.
        /// </summary>
        public event EventHandler<BlockTriggerEventArgs> OnBlockValidate;

        // Record-level triggers  
        /// <summary>
        /// Raised when a record is entered.
        /// </summary>
        public event EventHandler<RecordTriggerEventArgs> OnRecordEnter;
        /// <summary>
        /// Raised when a record is left.
        /// </summary>
        public event EventHandler<RecordTriggerEventArgs> OnRecordLeave;
        /// <summary>
        /// Raised when record-level validation is triggered.
        /// </summary>
        public event EventHandler<RecordTriggerEventArgs> OnRecordValidate;

        // DML triggers
        /// <summary>
        /// Raised before a query operation executes.
        /// </summary>
        public event EventHandler<DMLTriggerEventArgs> OnPreQuery;
        /// <summary>
        /// Raised after a query operation completes.
        /// </summary>
        public event EventHandler<DMLTriggerEventArgs> OnPostQuery;
        /// <summary>
        /// Raised before an insert operation executes.
        /// </summary>
        public event EventHandler<DMLTriggerEventArgs> OnPreInsert;
        /// <summary>
        /// Raised after an insert operation completes.
        /// </summary>
        public event EventHandler<DMLTriggerEventArgs> OnPostInsert;
        /// <summary>
        /// Raised before an update operation executes.
        /// </summary>
        public event EventHandler<DMLTriggerEventArgs> OnPreUpdate;
        /// <summary>
        /// Raised after an update operation completes.
        /// </summary>
        public event EventHandler<DMLTriggerEventArgs> OnPostUpdate;
        /// <summary>
        /// Raised before a delete operation executes.
        /// </summary>
        public event EventHandler<DMLTriggerEventArgs> OnPreDelete;
        /// <summary>
        /// Raised after a delete operation completes.
        /// </summary>
        public event EventHandler<DMLTriggerEventArgs> OnPostDelete;
        /// <summary>
        /// Raised before a commit operation executes.
        /// </summary>
        public event EventHandler<DMLTriggerEventArgs> OnPreCommit;
        /// <summary>
        /// Raised after a commit operation completes.
        /// </summary>
        public event EventHandler<DMLTriggerEventArgs> OnPostCommit;

        // Validation triggers
        /// <summary>
        /// Raised when field-level validation is requested.
        /// </summary>
        public event EventHandler<ValidationTriggerEventArgs> OnValidateField;
        /// <summary>
        /// Raised when record-level validation is requested.
        /// </summary>
        public event EventHandler<ValidationTriggerEventArgs> OnValidateRecord;
        /// <summary>
        /// Raised when form-level validation is requested.
        /// </summary>
        public event EventHandler<ValidationTriggerEventArgs> OnValidateForm;

        // Error handling
        /// <summary>
        /// Raised when an error is surfaced through the Forms event pipeline.
        /// </summary>
        public event EventHandler<ErrorTriggerEventArgs> OnError;
    #pragma warning restore CS0067
        #endregion

        #region Constructor
        /// <summary>
        /// Creates an event manager that records activity through the supplied editor.
        /// </summary>
        /// <param name="dmeEditor">Editor used for logging and integration callbacks.</param>
        public EventManager(IDMEEditor dmeEditor)
        {
            _dmeEditor = dmeEditor ?? throw new ArgumentNullException(nameof(dmeEditor));
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Subscribes to unit of work events
        /// </summary>
        public void SubscribeToUnitOfWorkEvents(IUnitofWork unitOfWork, string blockName)
        {
            if (unitOfWork == null || string.IsNullOrWhiteSpace(blockName))
                return;

            try
            {
                // Store subscription reference for cleanup
                StoreSubscription(blockName, unitOfWork);

                // Subscribe to DML events
                unitOfWork.PreInsert += (sender, e) => HandlePreInsert(blockName, sender, e);
                unitOfWork.PostInsert += (sender, e) => HandlePostInsert(blockName, sender, e);
                unitOfWork.PreUpdate += (sender, e) => HandlePreUpdate(blockName, sender, e);
                unitOfWork.PostUpdate += (sender, e) => HandlePostUpdate(blockName, sender, e);
                unitOfWork.PreDelete += (sender, e) => HandlePreDelete(blockName, sender, e);
                unitOfWork.PostDelete += (sender, e) => HandlePostDelete(blockName, sender, e);

                    // Subscribe to navigation events at the UoW boundary.
                    unitOfWork.CurrentChanged += (sender, e) => HandleCurrentChanged(blockName, sender, e);

                LogOperation($"Subscribed to events for block '{blockName}'");
            }
            catch (Exception ex)
            {
                LogError($"Error subscribing to events for block '{blockName}'", ex);
            }
        }

        /// <summary>
        /// Unsubscribes from unit of work events
        /// </summary>
        public void UnsubscribeFromUnitOfWorkEvents(IUnitofWork unitOfWork, string blockName)
        {
            if (unitOfWork == null || string.IsNullOrWhiteSpace(blockName))
                return;

            try
            {
                // Remove subscription reference
                RemoveSubscription(blockName);

                // Note: In a real implementation, you would need to store event handler references
                // to properly unsubscribe. This is a simplified version.
                
                LogOperation($"Unsubscribed from events for block '{blockName}'");
            }
            catch (Exception ex)
            {
                LogError($"Error unsubscribing from events for block '{blockName}'", ex);
            }
        }

        /// <summary>
        /// Triggers block enter event
        /// </summary>
        public void TriggerBlockEnter(string blockName)
        {
            try
            {
                var args = new BlockTriggerEventArgs(blockName, "Block entered");
                OnBlockEnter?.Invoke(this, args);
                LogOperation($"Block enter event triggered for '{blockName}'");
            }
            catch (Exception ex)
            {
                LogError($"Error triggering block enter event for '{blockName}'", ex);
            }
        }

        /// <summary>
        /// Triggers block leave event
        /// </summary>
        public void TriggerBlockLeave(string blockName)
        {
            try
            {
                var args = new BlockTriggerEventArgs(blockName, "Block leaving");
                OnBlockLeave?.Invoke(this, args);
                LogOperation($"Block leave event triggered for '{blockName}'");
            }
            catch (Exception ex)
            {
                LogError($"Error triggering block leave event for '{blockName}'", ex);
            }
        }

        /// <summary>
        /// Triggers error event
        /// </summary>
        public void TriggerError(string blockName, Exception ex)
        {
            try
            {
                var args = new ErrorTriggerEventArgs(blockName, ex.Message, ex);
                OnError?.Invoke(this, args);
                LogError($"Error event triggered for block '{blockName}': {ex.Message}", ex);
            }
            catch (Exception triggerEx)
            {
                LogError($"Error triggering error event for block '{blockName}'", triggerEx);
            }
        }

        /// <summary>
        /// Triggers field validation event
        /// </summary>
        public bool TriggerFieldValidation(string blockName, string FieldName, object value)
        {
            try
            {
                var args = new ValidationTriggerEventArgs(blockName, FieldName, value);
                OnValidateField?.Invoke(this, args);
                return args.IsValid;
            }
            catch (Exception ex)
            {
                LogError($"Error triggering field validation for '{FieldName}' in block '{blockName}'", ex);
                return false;
            }
        }

        /// <summary>
        /// Triggers record validation event
        /// </summary>
        public bool TriggerRecordValidation(string blockName, object record)
        {
            try
            {
                var args = new ValidationTriggerEventArgs(blockName, null, record);
                OnValidateRecord?.Invoke(this, args);
                return args.IsValid;
            }
            catch (Exception ex)
            {
                LogError($"Error triggering record validation in block '{blockName}'", ex);
                return false;
            }
        }

        #endregion

        #region Private Event Handlers

        private void HandlePreInsert(string blockName, object sender, UnitofWorkParams e)
        {
            try
            {
                var args = new DMLTriggerEventArgs(blockName, DMLOperation.Insert, e)
                {
                    CurrentRecord = sender
                };
                OnPreInsert?.Invoke(this, args);
                
                // Apply any changes back to the UnitofWorkParams
                if (args.CurrentRecord != null)
                {
                    e.Record = args.CurrentRecord;
                }
                e.Cancel = args.Cancel;
            }
            catch (Exception ex)
            {
                LogError($"Error in pre-insert handler for block '{blockName}'", ex);
            }
        }

        private void HandlePostInsert(string blockName, object sender, UnitofWorkParams e)
        {
            try
            {
                var args = new DMLTriggerEventArgs(blockName, DMLOperation.Insert, e)
                {
                    CurrentRecord = sender
                };
                OnPostInsert?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                LogError($"Error in post-insert handler for block '{blockName}'", ex);
            }
        }

        private void HandlePreUpdate(string blockName, object sender, UnitofWorkParams e)
        {
            try
            {
                var args = new DMLTriggerEventArgs(blockName, DMLOperation.Update, e)
                {
                    CurrentRecord = sender
                };
                OnPreUpdate?.Invoke(this, args);
                
                // Apply any changes back to the UnitofWorkParams
                if (args.CurrentRecord != null)
                {
                    e.Record = args.CurrentRecord;
                }
                e.Cancel = args.Cancel;
            }
            catch (Exception ex)
            {
                LogError($"Error in pre-update handler for block '{blockName}'", ex);
            }
        }

        private void HandlePostUpdate(string blockName, object sender, UnitofWorkParams e)
        {
            try
            {
                var args = new DMLTriggerEventArgs(blockName, DMLOperation.Update, e)
                {
                    CurrentRecord = sender
                };
                OnPostUpdate?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                LogError($"Error in post-update handler for block '{blockName}'", ex);
            }
        }

        private void HandlePreDelete(string blockName, object sender, UnitofWorkParams e)
        {
            try
            {
                var args = new DMLTriggerEventArgs(blockName, DMLOperation.Delete, e)
                {
                    CurrentRecord = sender
                };
                OnPreDelete?.Invoke(this, args);
                
                // For delete, we typically don't modify the record, but we allow cancellation
                e.Cancel = args.Cancel;
            }
            catch (Exception ex)
            {
                LogError($"Error in pre-delete handler for block '{blockName}'", ex);
            }
        }

        private void HandlePostDelete(string blockName, object sender, UnitofWorkParams e)
        {
            try
            {
                var args = new DMLTriggerEventArgs(blockName, DMLOperation.Delete, e)
                {
                    CurrentRecord = sender
                };
                OnPostDelete?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                LogError($"Error in post-delete handler for block '{blockName}'", ex);
            }
        }

        private void HandleCurrentChanged(string blockName, object sender, EventArgs e)
        {
            try
            {
                var args = new RecordTriggerEventArgs(blockName, sender, "Current record changed");
                OnRecordEnter?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                LogError($"Error in current changed handler for block '{blockName}'", ex);
            }
        }

        #endregion

        #region Private Helper Methods

        private void StoreSubscription(string blockName, IUnitofWork unitOfWork)
        {
            lock (_lockObject)
            {
                if (!_eventSubscriptions.ContainsKey(blockName))
                {
                    _eventSubscriptions[blockName] = new List<WeakReference>();
                }
                _eventSubscriptions[blockName].Add(new WeakReference(unitOfWork));
            }
        }

        private void RemoveSubscription(string blockName)
        {
            lock (_lockObject)
            {
                _eventSubscriptions.Remove(blockName);
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