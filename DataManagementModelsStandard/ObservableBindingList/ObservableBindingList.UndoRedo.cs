using System;
using System.ComponentModel;
using System.Reflection;

namespace TheTechIdea.Beep.Editor
{
    public partial class ObservableBindingList<T>
    {
        #region "Undo / Redo — Phase 7A"

        /// <summary>
        /// The undo/redo manager instance. Created lazily when undo is enabled.
        /// </summary>
        private UndoRedoManager<T> _undoRedoManager;

        /// <summary>
        /// When true, property changes and CRUD operations are recorded for undo/redo.
        /// Default: false (zero overhead when off).
        /// </summary>
        private bool _isUndoEnabled;

        /// <summary>
        /// Gets or sets whether undo/redo tracking is enabled.
        /// Enabling creates the UndoRedoManager; disabling does NOT clear history.
        /// </summary>
        public bool IsUndoEnabled
        {
            get => _isUndoEnabled;
            set
            {
                _isUndoEnabled = value;
                if (value && _undoRedoManager == null)
                    _undoRedoManager = new UndoRedoManager<T>();
            }
        }

        /// <summary>
        /// Gets or sets the maximum undo depth. Default: 50.
        /// </summary>
        public int MaxUndoDepth
        {
            get => _undoRedoManager?.MaxUndoDepth ?? 50;
            set
            {
                if (_undoRedoManager == null)
                    _undoRedoManager = new UndoRedoManager<T>();
                _undoRedoManager.MaxUndoDepth = value;
            }
        }

        /// <summary>True if there are actions to undo.</summary>
        public bool CanUndo => _undoRedoManager?.CanUndo ?? false;

        /// <summary>True if there are actions to redo.</summary>
        public bool CanRedo => _undoRedoManager?.CanRedo ?? false;

        /// <summary>
        /// Records a property-change action for undo.
        /// Called from Item_PropertyChanged when undo is enabled.
        /// </summary>
        internal void RecordPropertyChangeForUndo(T item, string propertyName, object oldValue, object newValue)
        {
            if (!_isUndoEnabled || _undoRedoManager == null || _isUndoing) return;

            _undoRedoManager.RecordAction(new UndoAction<T>
            {
                ActionType = UndoActionType.PropertyChange,
                Item = item,
                PropertyName = propertyName,
                OldValue = oldValue,
                NewValue = newValue,
                Index = IndexOf(item)
            });
        }

        /// <summary>
        /// Records an insertion for undo.
        /// </summary>
        internal void RecordInsertForUndo(T item, int index)
        {
            if (!_isUndoEnabled || _undoRedoManager == null || _isUndoing) return;

            _undoRedoManager.RecordAction(new UndoAction<T>
            {
                ActionType = UndoActionType.Insert,
                Item = item,
                Index = index
            });
        }

        /// <summary>
        /// Records a removal for undo.
        /// </summary>
        internal void RecordRemoveForUndo(T item, int index)
        {
            if (!_isUndoEnabled || _undoRedoManager == null || _isUndoing) return;

            _undoRedoManager.RecordAction(new UndoAction<T>
            {
                ActionType = UndoActionType.Remove,
                Item = item,
                Index = index
            });
        }

        /// <summary>
        /// Flag to prevent re-recording during undo/redo execution.
        /// </summary>
        private bool _isUndoing;

        /// <summary>
        /// Undoes the most recent action.
        /// Returns true if an action was undone.
        /// </summary>
        public bool Undo()
        {
            if (_undoRedoManager == null || !_undoRedoManager.CanUndo) return false;

            var action = _undoRedoManager.Undo();
            if (action == null) return false;

            _isUndoing = true;
            try
            {
                ApplyUndoAction(action, isRedo: false);
            }
            finally
            {
                _isUndoing = false;
            }
            return true;
        }

        /// <summary>
        /// Redoes the most recently undone action.
        /// Returns true if an action was redone.
        /// </summary>
        public bool Redo()
        {
            if (_undoRedoManager == null || !_undoRedoManager.CanRedo) return false;

            var action = _undoRedoManager.Redo();
            if (action == null) return false;

            _isUndoing = true;
            try
            {
                ApplyUndoAction(action, isRedo: true);
            }
            finally
            {
                _isUndoing = false;
            }
            return true;
        }

        /// <summary>
        /// Clears all undo/redo history.
        /// </summary>
        public void ClearUndoHistory()
        {
            _undoRedoManager?.Clear();
        }

        /// <summary>
        /// Applies an undo or redo action.
        /// </summary>
        private void ApplyUndoAction(UndoAction<T> action, bool isRedo)
        {
            switch (action.ActionType)
            {
                case UndoActionType.PropertyChange:
                    var prop = typeof(T).GetProperty(action.PropertyName, BindingFlags.Public | BindingFlags.Instance);
                    if (prop != null && prop.CanWrite)
                    {
                        // Undo: restore old value. Redo: re-apply new value.
                        var valueToSet = isRedo ? action.NewValue : action.OldValue;
                        prop.SetValue(action.Item, valueToSet);
                        // prop.SetValue re-raises Item_PropertyChanged (a genuine
                        // property set on an INotifyPropertyChanged item), but that
                        // handler only ever transitions Unchanged -> Modified -- it
                        // never checks whether the new value brought the item back
                        // to its original snapshot, so an Undo that reverts a value
                        // never clears the dirty flag RejectChanges/AcceptChanges
                        // would clear for the same values. Reconcile explicitly.
                        ReconcileTrackingAfterUndoRedo(action.Item);
                    }
                    break;

                case UndoActionType.Insert:
                    if (isRedo)
                    {
                        // Redo insertion = re-insert
                        int insertIdx = Math.Min(action.Index, Items.Count);
                        InsertItem(insertIdx, action.Item);
                    }
                    else
                    {
                        // Undo insertion = remove
                        int idx = IndexOf(action.Item);
                        if (idx >= 0) RemoveItem(idx);
                    }
                    break;

                case UndoActionType.Remove:
                    if (isRedo)
                    {
                        // Redo removal = re-remove
                        int idx = IndexOf(action.Item);
                        if (idx >= 0) RemoveItem(idx);
                    }
                    else
                    {
                        // Undo removal = re-insert
                        int insertIdx = Math.Min(action.Index, Items.Count);
                        InsertItem(insertIdx, action.Item);
                    }
                    break;
            }
        }

        /// <summary>
        /// After an Undo/Redo property change, checks whether the item's
        /// current values now match its <see cref="Tracking.OriginalValues"/>
        /// snapshot for every property that snapshot recorded. If every one
        /// matches, the item is genuinely clean again, so its tracking state
        /// is reset to <see cref="EntityState.Unchanged"/> — the same
        /// cleanup <see cref="RejectChanges(T)"/> already performs after a
        /// full revert. If any still differs (a multi-property edit with
        /// only one property undone), the item is correctly left <see
        /// cref="EntityState.Modified"/> — <c>Item_PropertyChanged</c>'s own
        /// Phase 1B already set that when <c>ApplyUndoAction</c>'s
        /// <c>prop.SetValue</c> re-raised the property-changed event, so
        /// there is nothing for this method to do in that case.
        /// </summary>
        /// <remarks>
        /// Only ever narrows <see cref="EntityState.Modified"/> back to
        /// <see cref="EntityState.Unchanged"/> — it does not widen the other
        /// way. Re-mutating an item that is already <see cref="EntityState.Unchanged"/>
        /// (a Redo landing on an item an Undo just fully reconciled) is a
        /// separate, pre-existing gap in <c>Item_PropertyChanged</c>'s own
        /// lazy <c>OriginalValues ??= SnapshotValues(item)</c> snapshot
        /// timing — by the time that line runs the value has already
        /// changed, so it captures the wrong "original". Not fixed here;
        /// see <c>ObservableBindingListUndoRedoTests.cs</c>'s class doc
        /// comment for the full account. <see cref="EntityState.Added"/>/
        /// <see cref="EntityState.Deleted"/> items are unaffected — they are
        /// handled entirely by <see cref="UndoActionType.Insert"/>/
        /// <see cref="UndoActionType.Remove"/> above, not this
        /// property-level path.
        /// </remarks>
        private void ReconcileTrackingAfterUndoRedo(T item)
        {
            var tracking = GetTrackingItem(item);
            if (tracking == null || tracking.OriginalValues == null || tracking.EntityState != EntityState.Modified)
                return;

            foreach (var prop in GetCachedProperties())
            {
                if (!prop.CanRead) continue;
                if (!tracking.OriginalValues.TryGetValue(prop.Name, out var originalValue)) continue;
                if (!Equals(originalValue, prop.GetValue(item)))
                    return;
            }

            tracking.EntityState = EntityState.Unchanged;
            tracking.OriginalValues = null;
            tracking.ModifiedProperties.Clear();
            tracking.ModifiedAt = null;
            tracking.ModifiedBy = null;
            tracking.Version = 0;
        }

        #endregion
    }
}
