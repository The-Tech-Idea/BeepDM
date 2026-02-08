using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.UOW
{
    /// <summary>
    /// Utility methods partial class for UnitofWork - Contains utility methods that were missing from the refactored version
    /// </summary>
    /// <typeparam name="T">The type of entity.</typeparam>
    public partial class UnitofWork<T>
    {
        #region Missing Core Methods

        /// <summary>
        /// Clears all data from the collection and resets state
        /// </summary>
        public void Clear()
        {
            try
            {
                // Detach event handlers before clearing
                DetachHandlers(_units);
                DetachHandlers(_filteredunits);

                // Ensure _units is always initialized (not just cleared)
                if (_units == null)
                {
                    _units = new ObservableBindingList<T>();
                }
                else
                {
                    _units.Clear();
                }

                _filteredunits?.Clear();
                DeletedUnits?.Clear();

                // Clear tracking dictionaries
                _entityStates?.Clear();
                _deletedentities?.Clear();
                InsertedKeys?.Clear();
                UpdatedKeys?.Clear();
                DeletedKeys?.Clear();

                // Reset backup collection
                Tempunits?.Clear();

                // Reset flags
                IsFilterOn = false;
                keysidx = 0;

                OnPropertyChanged(nameof(Units));
                OnPropertyChanged(nameof(IsDirty));
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("UnitofWork",
                    $"Error clearing UnitofWork: {ex.Message}",
                    DateTime.Now, -1, null, Errors.Failed);
            }
        }

        /// <summary>
        /// Gets whether the unit of work has uncommitted changes
        /// </summary>
        /// <returns>True if there are uncommitted changes</returns>
        public bool GetIsDirty()
        {
            if (_entityStates == null || _entityStates.Count == 0)
            {
                return false;
            }

            // Check if any entity has been added, modified, or deleted
            return _entityStates.Values.Any(state =>
                state == EntityState.Added ||
                state == EntityState.Modified ||
                state == EntityState.Deleted) ||
                DeletedUnits?.Count > 0 ||
                InsertedKeys?.Count > 0 ||
                UpdatedKeys?.Count > 0 ||
                DeletedKeys?.Count > 0;
        }

        /// <summary>
        /// Checks if a document exists in the collection
        /// </summary>
        /// <param name="doc">The document to check</param>
        /// <returns>Index of the document, or -1 if not found</returns>
        public int DocExist(T doc)
        {
            if (doc == null || Units == null)
                return -1;

            return Units.IndexOf(doc);
        }

        /// <summary>
        /// Checks if a document exists by its primary key value
        /// </summary>
        /// <param name="doc">The document to check</param>
        /// <returns>Index of the document, or -1 if not found</returns>
        public int DocExistByKey(T doc)
        {
            if (doc == null || Units == null || string.IsNullOrEmpty(PrimaryKey))
                return -1;

            try
            {
                var docKeyValue = GetIDValue(doc);
                if (docKeyValue == null)
                    return -1;

                for (int i = 0; i < Units.Count; i++)
                {
                    var currentKeyValue = GetIDValue(Units[i]);
                    if (currentKeyValue != null && currentKeyValue.Equals(docKeyValue))
                    {
                        return i;
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("UnitofWork",
                    $"Error checking document existence by key: {ex.Message}",
                    DateTime.Now, -1, null, Errors.Failed);
            }

            return -1;
        }

        /// <summary>
        /// Finds the index of a document in the collection
        /// </summary>
        /// <param name="doc">The document to find</param>
        /// <returns>Index of the document, or -1 if not found</returns>
        public int FindDocIdx(T doc)
        {
            return DocExist(doc);
        }

        /// <summary>
        /// Gets the index of an entity by its primary key value
        /// </summary>
        /// <param name="id">The primary key value</param>
        /// <returns>Index of the entity, or -1 if not found</returns>
        public int Getindex(string id)
        {
            if (string.IsNullOrEmpty(id) || Units == null || string.IsNullOrEmpty(PrimaryKey))
                return -1;

            try
            {
                for (int i = 0; i < Units.Count; i++)
                {
                    var keyValue = GetIDValue(Units[i]);
                    if (keyValue != null && keyValue.ToString() == id)
                    {
                        return i;
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("UnitofWork",
                    $"Error getting index by ID: {ex.Message}",
                    DateTime.Now, -1, null, Errors.Failed);
            }

            return -1;
        }

        /// <summary>
        /// Gets the index of an entity in the collection
        /// </summary>
        /// <param name="entity">The entity to find</param>
        /// <returns>Index of the entity, or -1 if not found</returns>
        public int Getindex(T entity)
        {
            return DocExist(entity);
        }

        /// <summary>
        /// Gets an entity from the current list by index
        /// </summary>
        /// <param name="index">The index</param>
        /// <returns>The entity at the specified index</returns>
        public T GetItemFromCurrentList(int index)
        {
            if (Units == null || index < 0 || index >= Units.Count)
                return default(T);

            return Units[index];
        }

        /// <summary>
        /// Undoes the last change made to the collection
        /// </summary>
        public void UndoLastChange()
        {
            try
            {
                if (Tempunits != null && Tempunits.Count > 0)
                {
                    // Restore from backup
                    var restoredItems = new List<T>();
                    foreach (var item in Tempunits)
                    {
                        if (_dataHelper != null)
                        {
                            var clonedItem = _dataHelper.CloneEntity(item);
                            if (clonedItem != null)
                            {
                                restoredItems.Add(clonedItem);
                            }
                        }
                    }

                    if (restoredItems.Count > 0)
                    {
                        SetUnits(new ObservableBindingList<T>(restoredItems));

                        // Clear change tracking
                        _entityStates?.Clear();
                        DeletedUnits?.Clear();

                        OnPropertyChanged(nameof(Units));
                        OnPropertyChanged(nameof(IsDirty));
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("UnitofWork",
                    $"Error undoing last change: {ex.Message}",
                    DateTime.Now, -1, null, Errors.Failed);
            }
        }

        #endregion

        #region Change Tracking Methods

        /// <summary>
        /// Gets the indices of entities that have been added
        /// </summary>
        /// <returns>Enumerable of indices</returns>
        public IEnumerable<int> GetAddedEntities()
        {
            if (_entityStates == null)
                return Enumerable.Empty<int>();

            return _entityStates.Where(kvp => kvp.Value == EntityState.Added).Select(kvp => kvp.Key);
        }

        /// <summary>
        /// Gets the indices of entities that have been modified
        /// </summary>
        /// <returns>Enumerable of indices</returns>
        public IEnumerable<int> GetModifiedEntities()
        {
            if (_entityStates == null)
                return Enumerable.Empty<int>();

            return _entityStates.Where(kvp => kvp.Value == EntityState.Modified).Select(kvp => kvp.Key);
        }

        /// <summary>
        /// Gets the entities that have been deleted
        /// </summary>
        /// <returns>Enumerable of deleted entities</returns>
        public IEnumerable<T> GetDeletedEntities()
        {
            if (DeletedUnits == null)
                return Enumerable.Empty<T>();

            return DeletedUnits;
        }

        #endregion

        #region Navigation Methods

        /// <summary>
        /// Moves to the first entity in the collection
        /// </summary>
        public void MoveFirst()
        {
            if (Units != null && Units.Count > 0)
            {
                Units.MoveFirst();
                OnPropertyChanged(nameof(CurrentItem));
            }
        }

        /// <summary>
        /// Moves to the next entity in the collection
        /// </summary>
        public void MoveNext()
        {
            if (Units != null)
            {
                Units.MoveNext();
                OnPropertyChanged(nameof(CurrentItem));
            }
        }

        /// <summary>
        /// Moves to the previous entity in the collection
        /// </summary>
        public void MovePrevious()
        {
            if (Units != null)
            {
                Units.MovePrevious();
                OnPropertyChanged(nameof(CurrentItem));
            }
        }

        /// <summary>
        /// Moves to the last entity in the collection
        /// </summary>
        public void MoveLast()
        {
            if (Units != null)
            {
                Units.MoveLast();
                OnPropertyChanged(nameof(CurrentItem));
            }
        }

        /// <summary>
        /// Moves to the entity at the specified index
        /// </summary>
        /// <param name="index">The index to move to</param>
        public void MoveTo(int index)
        {
            if (Units != null)
            {
                Units.MoveTo(index);
                OnPropertyChanged(nameof(CurrentItem));
            }
        }

        #endregion

        #region Event Handlers and Data Loading

        /// <summary>
        /// Handles property changes on individual entities
        /// </summary>
        protected void ItemPropertyChangedHandler(object sender, PropertyChangedEventArgs e)
        {
            if (_suppressNotification) return;

            try
            {
                var item = sender as T;
                if (item != null)
                {
                    var index = Units.IndexOf(item);
                    if (index >= 0)
                    {
                        // Mark as modified if not already added
                        if (_entityStates.ContainsKey(index))
                        {
                            if (_entityStates[index] != EntityState.Added)
                            {
                                _entityStates[index] = EntityState.Modified;
                            }
                        }
                        else
                        {
                            _entityStates[index] = EntityState.Modified;
                        }

                        // Record change for audit trail
                        var newValue = GetPropertyValue(item, e.PropertyName);
                        RecordChange(item, e.PropertyName, null, newValue, EntityState.Modified);

                        // Fire PostEdit event
                        var eventArgs = new UnitofWorkParams
                        {
                            EventAction = EventAction.PostEdit,
                            PropertyName = e.PropertyName,
                            PropertyValue = newValue?.ToString(),
                            Record = item
                        };

                        PostEdit?.Invoke(this, eventArgs);
                        OnPropertyChanged(nameof(IsDirty));
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("UnitofWork",
                    $"Error handling property change: {ex.Message}",
                    DateTime.Now, -1, null, Errors.Failed);
            }
        }

        /// <summary>
        /// Records a change to the audit trail
        /// </summary>
        private void RecordChange(T entity, string propertyName, object oldValue, object newValue, EntityState action)
        {
            try
            {
                _changeLog.Add(new ChangeRecord
                {
                    Entity = entity,
                    PropertyName = propertyName,
                    OldValue = oldValue,
                    NewValue = newValue,
                    Timestamp = DateTime.UtcNow,
                    Action = action,
                    EntityName = EntityName
                });
            }
            catch
            {
                // Non-critical - don't let audit logging break the operation
            }
        }

        /// <summary>
        /// Handles collection change events
        /// </summary>
        protected void Units_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_suppressNotification) return;

            try
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        if (e.NewItems != null)
                        {
                            foreach (T item in e.NewItems)
                            {
                                var index = Units.IndexOf(item);
                                _entityStates[index] = EntityState.Added;
                                item.PropertyChanged += ItemPropertyChangedHandler;
                            }
                        }
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        if (e.OldItems != null)
                        {
                            foreach (T item in e.OldItems)
                            {
                                item.PropertyChanged -= ItemPropertyChangedHandler;
                                DeletedUnits.Add(item);
                                var existingIndex = _entityStates.FirstOrDefault(kvp => Units.ElementAtOrDefault(kvp.Key) == item).Key;
                                if (_entityStates.ContainsKey(existingIndex))
                                {
                                    _entityStates.Remove(existingIndex);
                                }
                            }
                        }
                        break;

                    case NotifyCollectionChangedAction.Reset:
                        _entityStates.Clear();
                        break;
                }

                OnPropertyChanged(nameof(IsDirty));
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("UnitofWork",
                    $"Error handling collection change: {ex.Message}",
                    DateTime.Now, -1, null, Errors.Failed);
            }
        }

        /// <summary>
        /// Gets data from the enumerable and puts it into the Units collection
        /// </summary>
        /// <param name="retval">The enumerable data source</param>
        protected void GetDataInUnits(IEnumerable<object> retval)
        {
            try
            {
                _suppressNotification = true;

                var items = new List<T>();
                if (retval != null)
                {
                    foreach (var item in retval)
                    {
                        if (item is T typedItem)
                        {
                            items.Add(typedItem);
                        }
                        else if (_dataHelper != null)
                        {
                            var convertedItem = _dataHelper.ConvertToEntity(item);
                            if (convertedItem != null)
                            {
                                items.Add(convertedItem);
                            }
                        }
                    }
                }

                SetUnits(new ObservableBindingList<T>(items));

                _suppressNotification = false;
                OnPropertyChanged(nameof(Units));
            }
            catch (Exception ex)
            {
                _suppressNotification = false;
                DMEEditor.AddLogMessage("UnitofWork",
                    $"Error loading data into units: {ex.Message}",
                    DateTime.Now, -1, null, Errors.Failed);
            }
        }

        /// <summary>
        /// Filters the collection based on the provided filters
        /// </summary>
        /// <param name="collection">The collection to filter</param>
        /// <param name="filters">The filters to apply</param>
        /// <returns>Filtered collection</returns>
        protected ObservableBindingList<T> FilterCollection(ObservableBindingList<T> collection, List<AppFilter> filters)
        {
            if (collection == null || filters == null || filters.Count == 0)
                return collection;

            try
            {
                var filteredItems = collection.AsEnumerable();

                foreach (var filter in filters)
                {
                    filteredItems = ApplyFilter(filteredItems, filter);
                }

                return new ObservableBindingList<T>(filteredItems.ToList());
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("UnitofWork",
                    $"Error filtering collection: {ex.Message}",
                    DateTime.Now, -1, null, Errors.Failed);
                return collection;
            }
        }

        /// <summary>
        /// Applies a single filter to the enumerable
        /// </summary>
        /// <param name="items">The items to filter</param>
        /// <param name="filter">The filter to apply</param>
        /// <returns>Filtered enumerable</returns>
        private IEnumerable<T> ApplyFilter(IEnumerable<T> items, AppFilter filter)
        {
            if (string.IsNullOrEmpty(filter.FieldName) || filter.FilterValue == null)
                return items;

            return items.Where(item =>
            {
                try
                {
                    var propertyValue = GetPropertyValue(item, filter.FieldName);
                    if (propertyValue == null) return false;

                    return CompareValues(propertyValue, filter.FilterValue, filter.Operator);
                }
                catch
                {
                    return false;
                }
            });
        }

        /// <summary>
        /// Compares two values based on the operator
        /// </summary>
        private bool CompareValues(object propertyValue, object filterValue, string op)
        {
            if (propertyValue == null && filterValue == null) return op == "=";
            if (propertyValue == null || filterValue == null) return op == "!=";

            var comparison = Comparer<IComparable>.Default.Compare(propertyValue as IComparable, filterValue as IComparable);

            return op switch
            {
                "=" => comparison == 0,
                "!=" => comparison != 0,
                ">" => comparison > 0,
                "<" => comparison < 0,
                ">=" => comparison >= 0,
                "<=" => comparison <= 0,
                _ => false,
            };
        }

        /// <summary>
        /// Gets a property value using reflection
        /// </summary>
        private object GetPropertyValue(T item, string propertyName)
        {
            try
            {
                var property = typeof(T).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                return property?.GetValue(item);
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Missing Interface Implementation

        /// <summary>
        /// Gets whether logging is enabled
        /// </summary>
        public bool IsLogging { get; set; } = false;

        /// <summary>
        /// Deletes an entity by its string ID
        /// </summary>
        /// <param name="id">The string ID of the entity to delete</param>
        /// <returns>Error information</returns>
        public IErrorsInfo Delete(string id)
        {
            ErrorsInfo errorsInfo = new ErrorsInfo();
            if (!Validateall())
            {
                errorsInfo.Message = "Validation Failed";
                errorsInfo.Flag = Errors.Failed;
                return errorsInfo;
            }

            var index = Getindex(id);
            if (index >= 0)
            {
                var entity = Units[index];
                return Delete(entity);
            }
            else
            {
                errorsInfo.Message = "Object not found";
                errorsInfo.Flag = Errors.Failed;
                return errorsInfo;
            }
        }

        /// <summary>
        /// Gets the tracking item for an entity - Fixed return type to match interface
        /// </summary>
        /// <param name="item">The entity</param>
        /// <returns>Tracking information</returns>
        public Tracking GetTrackingItem(T item)
        {
            if (Units == null || item == null)
                return null;

            return Units.GetTrackingItem(item);
        }

        #endregion
    }
}