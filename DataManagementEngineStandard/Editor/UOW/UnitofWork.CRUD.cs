using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.UOW.Interfaces;
using TheTechIdea.Beep.Helpers.RDBMSHelpers;

using System.Collections.ObjectModel;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.UOW
{
    /// <summary>
    /// CRUD Operations partial class for UnitofWork with DefaultsManager integration
    /// Handles Create, Read, Update, Delete operations with enhanced default value support
    /// </summary>
    /// <typeparam name="T">The type of entity</typeparam>
    public partial class UnitofWork<T>
    {
        #region Read and Query Operations

        /// <summary>
        /// Reads an item from a collection based on its ID
        /// </summary>
        /// <param name="id">The ID of the item to read</param>
        /// <returns>The item with the specified ID, or the default value of the item type if the ID is not found or the collection is not valid</returns>
        public T Read(string id)
        {
            if (!Validateall())
            {
                return default;
            }
            var index = Getindex(id);
            if (index < 0 || index >= Units.Count)
            {
                return default;
            }
            return Units[index];
        }

        /// <summary>
        /// Reads an item using a predicate function
        /// </summary>
        /// <param name="predicate">The predicate function to find the item</param>
        /// <returns>The first item matching the predicate or default if not found</returns>
        public T Read(Func<T, bool> predicate)
        {
            if (!Validateall())
            {
                return default;
            }
            return Units.FirstOrDefault(predicate);
        }

        /// <summary>
        /// Reads multiple items using a predicate function
        /// </summary>
        /// <param name="predicate">The predicate function to find items</param>
        /// <returns>A task containing the collection of matching items</returns>
        public Task<ObservableBindingList<T>> MultiRead(Func<T, bool> predicate)
        {
            if (!Validateall())
            {
                return Task.FromResult<ObservableBindingList<T>>(null);
            }
            return Task.FromResult(new ObservableBindingList<T>(Units.Where(predicate)));
        }

        /// <summary>
        /// Retrieves a collection of entities asynchronously
        /// </summary>
        /// <returns>An observable binding list of entities</returns>
        public virtual async Task<ObservableBindingList<T>> Get()
        {
            UnitofWorkParams ps = new UnitofWorkParams() { Cancel = false, EventAction = EventAction.PreQuery };
            PreQuery?.Invoke(this, ps);
            if (ps.Cancel)
            {
                DMEEditor.ErrorObject.Message = "Query Cancelled";
                return await Task.FromResult(Units);
            }
            
            _suppressNotification = true;
            IsFilterOn = false;
            
            if (!IsInListMode)
            {
                var retval = DataSource.GetEntity(EntityName, null);
                try
                {
                    GetDataInUnits(retval);
                }
                catch (Exception ex)
                {
                    DMEEditor.AddLogMessage("Beep", $" Unit of Work Could not get Data in units {ex.Message} ", DateTime.Now, -1, null, Errors.Failed);
                }
            }
            
            _suppressNotification = false;
            PostQuery?.Invoke(Units, ps);
            if (ps.Cancel)
            {
                DMEEditor.ErrorObject.Message = "Post Query";
                return await Task.FromResult(Units);
            }
            return await Task.FromResult(Units);
        }

        /// <summary>
        /// Retrieves a list of items based on the specified filters
        /// </summary>
        /// <param name="filters">The list of filters to apply</param>
        /// <returns>A task that represents the asynchronous operation containing the filtered list</returns>
        public virtual async Task<ObservableBindingList<T>> Get(List<AppFilter> filters)
        {
            UnitofWorkParams ps = new UnitofWorkParams() { Cancel = false, EventAction = EventAction.PreQuery };
            PreQuery?.Invoke(this, ps);
            if (ps.Cancel)
            {
                DMEEditor.ErrorObject.Message = "Query Cancelled";
                return await Task.FromResult(Units);
            }
            
            if (filters == null)
            {
                IsFilterOn = false;
                return Units;
            }
            else
                IsFilterOn = true;

            if (!IsInListMode)
            {
                var retval = DataSource.GetEntity(EntityName, filters);
                GetDataInUnits(retval);
            }
            else
            {
                if (filters != null && _units != null)
                {
                    if (_units.Count > 0)
                    {
                        _suppressNotification = true;
                        FilteredUnits = FilterCollection(_units, filters);
                        _suppressNotification = false;
                        return await Task.FromResult(FilteredUnits);
                    }
                }
            }
            
            PostQuery?.Invoke(Units, ps);
            if (ps.Cancel)
            {
                DMEEditor.ErrorObject.Message = "Post Query";
                return await Task.FromResult(Units);
            }
            return await Task.FromResult(Units);
        }

        /// <summary>
        /// Retrieves items based on a query string
        /// </summary>
        /// <param name="query">The query string</param>
        /// <returns>A task containing the query results</returns>
        public virtual async Task<ObservableBindingList<T>> GetQuery(string query)
        {
            UnitofWorkParams ps = new UnitofWorkParams() { Cancel = false, EventAction = EventAction.PreQuery };
            PreQuery?.Invoke(this, ps);
            if (ps.Cancel)
            {
                DMEEditor.ErrorObject.Message = "Query Cancelled";
                return await Task.FromResult(Units);
            }
            
            if (query == null)
            {
                IsFilterOn = false;
                return Units;
            }
            else
                IsFilterOn = true;

            if (!IsInListMode)
            {
                var retval = DataSource.GetEntity(query, null);
                GetDataInUnits(retval);
            }
            else
            {
                if (query != null && _units != null)
                {
                    if (_units.Count > 0)
                    {
                        _suppressNotification = true;
                        FilteredUnits = FilterCollection(_units, null);
                        _suppressNotification = false;
                        return await Task.FromResult(FilteredUnits);
                    }
                }
            }
            
            ps = new UnitofWorkParams() { Cancel = false, EventAction = EventAction.PostQuery };
            PostQuery?.Invoke(Units, ps);
            if (ps.Cancel)
            {
                DMEEditor.ErrorObject.Message = "Post Query";
                return await Task.FromResult(Units);
            }
            return await Task.FromResult(Units);
        }

        /// <summary>
        /// Retrieves the value associated with the specified key
        /// </summary>
        /// <param name="key">The key of the value to retrieve</param>
        /// <returns>The value associated with the specified key</returns>
        public virtual T Get(int key)
        {
            if (Units == null || key < 0 || key >= Units.Count)
            {
                return default;
            }
            return Units[key];
        }

        /// <summary>
        /// Returns an object of type T based on the provided primary key
        /// </summary>
        /// <param name="PrimaryKeyid">The value of the primary key</param>
        /// <returns>An object of type T that matches the provided primary key, or null if no match is found</returns>
        public virtual T Get(string PrimaryKeyid)
        {
            if (Units == null || Units.Count == 0)
            {
                // Await the async Get to ensure data is loaded before querying
                Get(new List<AppFilter>() { new AppFilter() {FieldName = PrimaryKey, Operator = "=", FilterValue = PrimaryKeyid } }).GetAwaiter().GetResult();
            }

            if (string.IsNullOrEmpty(PrimaryKey))
            {
                return default;
            }

            var pkProperty = typeof(T).GetProperty(PrimaryKey, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (pkProperty == null)
            {
                return default;
            }

            var retval = Units.FirstOrDefault(p =>
            {
                var val = pkProperty.GetValue(p, null);
                return val != null && val.ToString() == PrimaryKeyid;
            });
            return retval;
        }

        #endregion

        #region Create Operations with DefaultsManager Integration

        /// <summary>
        /// Creates a new entity and adds it to the collection with default values applied
        /// </summary>
        /// <remarks>
        /// This method validates the collection, creates a new entity, applies default values,
        /// raises pre/post create events, and adds the entity to the collection.
        /// </remarks>
        public void New()
        {
            if (!Validateall())
            {
                return;
            }
            
            T entity = new T();
            
            // Raise pre-create event
            UnitofWorkParams ps = new UnitofWorkParams() { Cancel = false, EventAction = EventAction.PreCreate };
            PreCreate?.Invoke(entity, ps);
            if (ps.Cancel)
            {
                return;
            }

            // Apply default values for insert operations using DefaultsManager
            try
            {
                if (_defaultsHelper != null)
                {
                    _defaultsHelper.ApplyInsertDefaults(entity);
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("UnitofWork", 
                    $"Error applying insert defaults for new entity: {ex.Message}", 
                    DateTime.Now, -1, null, Errors.Failed);
            }

            // Generate primary key sequence if needed
            if (!IsInListMode && !IsIdentity)
            {
                GetPrimaryKeySequence(entity);
            }

            // Add to collection
            Units.Add(entity);
            
            // Subscribe to PropertyChanged event
            entity.PropertyChanged += ItemPropertyChangedHandler;

            // Raise post-create event
            ps = new UnitofWorkParams() { Cancel = false, EventAction = EventAction.PostCreate };
            PostCreate?.Invoke(entity, ps);
            if (ps.Cancel)
            {
                return;
            }
        }

        /// <summary>
        /// Adds a new entity to the collection with default values applied
        /// </summary>
        /// <param name="entity">The entity to be added</param>
        /// <remarks>
        /// This method validates the collection, applies default values, raises pre/post create events,
        /// and adds the entity to the collection with PropertyChanged event subscription.
        /// </remarks>
        public void Add(T entity)
        {
            if (!Validateall())
            {
                return;
            }
            
            // Raise pre-create event
            UnitofWorkParams ps = new UnitofWorkParams() { Cancel = false, EventAction = EventAction.PreCreate };
            PreCreate?.Invoke(entity, ps);
            if (ps.Cancel)
            {
                return;
            }

            // Apply default values for insert operations using DefaultsManager
            try
            {
                if (_defaultsHelper != null)
                {
                    _defaultsHelper.ApplyInsertDefaults(entity);
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("UnitofWork", 
                    $"Error applying insert defaults for entity: {ex.Message}", 
                    DateTime.Now, -1, null, Errors.Failed);
            }

            // Generate primary key sequence if needed
            if (!IsInListMode && !IsIdentity)
            {
                GetPrimaryKeySequence(entity);
            }

            // Add to collection
            Units.Add(entity);
            
            // Subscribe to PropertyChanged event
            entity.PropertyChanged += ItemPropertyChangedHandler;

            // Raise post-create event
            ps = new UnitofWorkParams() { Cancel = false, EventAction = EventAction.PostCreate };
            PostCreate?.Invoke(entity, ps);
            if (ps.Cancel)
            {
                return;
            }
        }

        #endregion

        #region Update Operations with DefaultsManager Integration

        /// <summary>
        /// Updates an entity using a predicate with default values applied
        /// </summary>
        /// <param name="predicate">Predicate to find the entity</param>
        /// <param name="updatedEntity">The updated entity</param>
        /// <returns>Result of the update operation</returns>
        public ErrorsInfo Update(Func<T, bool> predicate, T updatedEntity)
        {
            ErrorsInfo errorsInfo = new ErrorsInfo();
            if (!Validateall())
            {
                errorsInfo.Message = "Validation Failed";
                errorsInfo.Flag = Errors.Failed;
                return errorsInfo;
            }
            
            var entity = Units.FirstOrDefault(predicate);
            if (entity != null)
            {
                // Apply update defaults using DefaultsManager
                try
                {
                    if (_defaultsHelper != null)
                    {
                        _defaultsHelper.ApplyUpdateDefaults(updatedEntity);
                    }
                }
                catch (Exception ex)
                {
                    DMEEditor.AddLogMessage("UnitofWork", 
                        $"Error applying update defaults: {ex.Message}", 
                        DateTime.Now, -1, null, Errors.Failed);
                }

                var index = Units.IndexOf(entity);
                Units[index] = updatedEntity;
                
                errorsInfo.Message = "Update Done";
                errorsInfo.Flag = Errors.Ok;
                return errorsInfo;
            }
            else
            {
                errorsInfo.Message = "Object not found";
                errorsInfo.Flag = Errors.Failed;
                return errorsInfo;
            }
        }

        /// <summary>
        /// Updates an entity with default values applied
        /// </summary>
        /// <param name="entity">The entity to be updated</param>
        /// <returns>An ErrorsInfo object containing information about the update operation</returns>
        public IErrorsInfo Update(T entity)
        {
            ErrorsInfo errorsInfo = new ErrorsInfo();
            if (!Validateall())
            {
                errorsInfo.Message = "Validation Failed";
                errorsInfo.Flag = Errors.Failed;
                return errorsInfo;
            }
            
            UnitofWorkParams ps = new UnitofWorkParams() { Cancel = false, EventAction = EventAction.PreUpdate };
            PreUpdate?.Invoke(entity, ps);
            if (ps.Cancel)
            {
                return errorsInfo;
            }

            // Apply update defaults using DefaultsManager
            try
            {
                if (_defaultsHelper != null)
                {
                    _defaultsHelper.ApplyUpdateDefaults(entity);
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("UnitofWork", 
                    $"Error applying update defaults: {ex.Message}", 
                    DateTime.Now, -1, null, Errors.Failed);
            }

            var index = DocExistByKey(entity);
            if (index >= 0)
            {
                _units[index] = entity;
                if (_entityStates.Count > 0)
                {
                    if (_entityStates.ContainsKey(index))
                    {
                        if (_entityStates[index] != EntityState.Added)
                        {
                            _entityStates[index] = EntityState.Modified;
                        }
                        errorsInfo.Message = "Update Done";
                        errorsInfo.Flag = Errors.Ok;
                        return errorsInfo;
                    }
                }
                else
                {
                    _entityStates.Add(index, EntityState.Modified);
                }

                ps = new UnitofWorkParams() { Cancel = false, EventAction = EventAction.PostUpdate };
                PostUpdate?.Invoke(entity, ps);
                if (ps.Cancel)
                {
                    return errorsInfo;
                }
                errorsInfo.Message = "Update Done";
                errorsInfo.Flag = Errors.Ok;
                return errorsInfo;
            }
            else
            {
                errorsInfo.Message = "Object not found";
                errorsInfo.Flag = Errors.Failed;
                return errorsInfo;
            }
        }

        /// <summary>
        /// Updates an entity with the specified ID and applies default values
        /// </summary>
        /// <param name="id">The ID of the entity to update</param>
        /// <param name="entity">The updated entity</param>
        /// <returns>An ErrorsInfo object indicating the result of the update operation</returns>
        public IErrorsInfo Update(string id, T entity)
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
                UnitofWorkParams ps = new UnitofWorkParams() { Cancel = false, EventAction = EventAction.PreUpdate };
                PreUpdate?.Invoke(entity, ps);
                if (ps.Cancel)
                {
                    return errorsInfo;
                }

                // Apply update defaults using DefaultsManager
                try
                {
                    if (_defaultsHelper != null)
                    {
                        _defaultsHelper.ApplyUpdateDefaults(entity);
                    }
                }
                catch (Exception ex)
                {
                    DMEEditor.AddLogMessage("UnitofWork", 
                        $"Error applying update defaults: {ex.Message}", 
                        DateTime.Now, -1, null, Errors.Failed);
                }

                Units[index] = entity;
                
                if (_entityStates.Count > 0)
                {
                    if (_entityStates.ContainsKey(index))
                    {
                        if (_entityStates[index] != EntityState.Added)
                        {
                            _entityStates[index] = EntityState.Modified;
                        }
                        errorsInfo.Message = "Update Done";
                        errorsInfo.Flag = Errors.Ok;
                        return errorsInfo;
                    }
                }
                else
                {
                    _entityStates.Add(index, EntityState.Modified);
                }
                
                ps = new UnitofWorkParams() { Cancel = false, EventAction = EventAction.PostUpdate };
                PostUpdate?.Invoke(entity, ps);
                if (ps.Cancel)
                {
                    return errorsInfo;
                }
                errorsInfo.Message = "Update Done";
                errorsInfo.Flag = Errors.Ok;
                return errorsInfo;
            }
            else
            {
                errorsInfo.Message = "Object not found";
                errorsInfo.Flag = Errors.Failed;
                return errorsInfo;
            }
        }

        /// <summary>
        /// Updates a document asynchronously with default values applied
        /// </summary>
        /// <param name="doc">The document to be updated</param>
        /// <returns>An object containing information about any errors that occurred during the update</returns>
        public async Task<IErrorsInfo> UpdateAsync(T doc)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            if (!IsRequirmentsValidated())
            {
                return DMEEditor.ErrorObject;
            }
            
            UnitofWorkParams ps = new UnitofWorkParams() { Cancel = false, EventAction = EventAction.PreUpdate };
            PreUpdate?.Invoke(doc, ps);
            if (ps.Cancel)
            {
                return DMEEditor.ErrorObject;
            }
            
            if (doc == null)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = "Object is null";
                return DMEEditor.ErrorObject;
            }

            // Apply update defaults using DefaultsManager
            try
            {
                if (_defaultsHelper != null)
                {
                    _defaultsHelper.ApplyUpdateDefaults(doc);
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("UnitofWork", 
                    $"Error applying update defaults in UpdateAsync: {ex.Message}", 
                    DateTime.Now, -1, null, Errors.Failed);
            }

            IErrorsInfo retval = UpdateDoc(doc);
            return retval;
        }

        #endregion

        #region Insert Operations with DefaultsManager Integration

        /// <summary>
        /// Inserts a document asynchronously with default values applied
        /// </summary>
        /// <param name="doc">The document to be inserted</param>
        /// <returns>An object containing information about any errors that occurred during the insertion process</returns>
        public async Task<IErrorsInfo> InsertAsync(T doc)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            if (!IsRequirmentsValidated())
            {
                return DMEEditor.ErrorObject;
            }
            
            UnitofWorkParams ps = new UnitofWorkParams() { Cancel = false, EventAction = EventAction.PreInsert };
            PreInsert?.Invoke(doc, ps);
            if (ps.Cancel)
            {
                return DMEEditor.ErrorObject;
            }
            
            if (doc == null)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = "Object is null";
                return DMEEditor.ErrorObject;
            }

            // Apply insert defaults using DefaultsManager
            try
            {
                if (_defaultsHelper != null)
                {
                    _defaultsHelper.ApplyInsertDefaults(doc);
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("UnitofWork", 
                    $"Error applying insert defaults in InsertAsync: {ex.Message}", 
                    DateTime.Now, -1, null, Errors.Failed);
            }

            IErrorsInfo retval = InsertDoc(doc);
            return retval;
        }

        /// <summary>
        /// Inserts a document into the data source with default values applied
        /// </summary>
        /// <param name="doc">The document to insert</param>
        /// <returns>An object containing information about any errors that occurred during the insertion</returns>
        public IErrorsInfo InsertDoc(T doc)
        {
            string cname = typeof(T).Name;

            // Apply GUID key if specified
            if (!string.IsNullOrEmpty(GuidKey))
            {
                if (Guidproperty == null)
                {
                    Guidproperty = doc.GetType().GetProperty(GuidKey, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                }
                Guidproperty?.SetValue(doc, Guid.NewGuid().ToString());
            }

            // Apply insert defaults using DefaultsManager
            try
            {
                if (_defaultsHelper != null)
                {
                    _defaultsHelper.ApplyInsertDefaults(doc);
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("UnitofWork", 
                    $"Error applying insert defaults in InsertDoc: {ex.Message}", 
                    DateTime.Now, -1, null, Errors.Failed);
            }

            UnitofWorkParams ps = new UnitofWorkParams() { Cancel = false, EventAction = EventAction.PreInsert };
            PreInsert?.Invoke(doc, ps);
            if (ps.Cancel)
            {
                return DMEEditor.ErrorObject;
            }

            IErrorsInfo retval;
            if (!IsInListMode)
            {
                retval = DataSource.InsertEntity(cname, doc);
            }
            else
            {
                int idx = Getindex(doc);
                if (idx > -1)
                {
                    _units[idx] = doc;
                    retval = new ErrorsInfo { Flag = Errors.Ok, Message = "object already there, updated" };
                }
                else
                {
                    _units.Add(doc);
                    retval = new ErrorsInfo { Flag = Errors.Ok, Message = "Add object to list" };
                }
            }
            return retval;
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Updates a document and returns information about any errors that occurred
        /// </summary>
        /// <param name="doc">The document to update</param>
        /// <returns>An object containing information about any errors that occurred during the update</returns>
        public IErrorsInfo UpdateDoc(T doc)
        {
            IErrorsInfo retval;
            string cname = typeof(T).Name;
            
            UnitofWorkParams ps = new UnitofWorkParams() { Cancel = false, EventAction = EventAction.PreUpdate };
            PreUpdate?.Invoke(doc, ps);
            if (ps.Cancel)
            {
                return DMEEditor.ErrorObject;
            }
            
            if (!IsInListMode)
            {
                retval = DataSource.UpdateEntity(cname, doc);
            }
            else
            {
                int idx = Getindex(doc);
                if (idx > -1)
                {
                    _units[idx] = doc;
                    retval = new ErrorsInfo { Flag = Errors.Ok, Message = "Update Done" };
                }
                else
                {
                    retval = new ErrorsInfo { Flag = Errors.Failed, Message = "Object not found - Could not Update" };
                }
            }
            
            return retval;
        }

        /// <summary>
        /// Gets the primary key sequence for a document
        /// </summary>
        /// <param name="doc">The document for which to retrieve the primary key sequence</param>
        /// <returns>The primary key sequence value</returns>
        public virtual int GetPrimaryKeySequence(T doc)
        {
            int retval = -1;
            if (DataSource.Category == DatasourceCategory.RDBMS && !string.IsNullOrEmpty(Sequencer))
            {
                retval = GetSeq(Sequencer);
                if (retval > 0)
                {
                    SetIDValue(doc, retval);
                }
            }
            return retval;
        }

        /// <summary>
        /// Gets the next value of a sequence
        /// </summary>
        /// <param name="SeqName">The name of the sequence</param>
        /// <returns>The next value of the sequence</returns>
        public virtual int GetSeq(string SeqName)
        {
            int retval = -1;
            if (DataSource.Category == DatasourceCategory.RDBMS)
            {
                string str = RDBMSHelper.GenerateFetchNextSequenceValueQuery(DataSource.DatasourceType, SeqName);
                if (!string.IsNullOrEmpty(str))
                {
                    var r = DataSource.GetScalar(str);
                    if (DMEEditor.ErrorObject.Flag == Errors.Ok || r <= 0)
                    {
                        if (r != null)
                        {
                            retval = (int)r;
                        }
                    }
                }
            }
            return retval;
        }

        /// <summary>
        /// Returns the Last identity of the specified entity in the list of units
        /// </summary>
        /// <returns>The Identity of the entity in the list of units. Returns -1 if the list is not valid</returns>
        public double GetLastIdentity()
        {
            double identity = -1;

            if (IsIdentity)
            {
                identity = DataSource.GetScalar(RDBMSHelper.GenerateFetchLastIdentityQuery(DataSource.DatasourceType));
            }
            return identity;
        }

        /// <summary>
        /// Sets the value of the primary key property for the specified entity
        /// </summary>
        /// <param name="entity">The entity object</param>
        /// <param name="value">The value to set</param>
        /// <exception cref="ArgumentException">Thrown when the primary key property is not found on the entity</exception>
        public void SetIDValue(T entity, object value)
        {
            if (!Validateall())
            {
                return;
            }

            var propertyInfo = entity.GetType().GetProperty(PrimaryKey, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            if (propertyInfo == null)
            {
                throw new ArgumentException($"Property '{PrimaryKey}' not found on '{entity.GetType().Name}'");
            }

            propertyInfo.SetValue(entity, Convert.ChangeType(value, propertyInfo.PropertyType), null);
        }

        /// <summary>
        /// Retrieves the value of the primary key property for the specified entity
        /// </summary>
        /// <param name="entity">The entity object</param>
        /// <returns>The value of the primary key property</returns>
        public object GetIDValue(T entity)
        {
            if (!Validateall())
            {
                return null;
            }
            var idValue = entity.GetType().GetProperty(PrimaryKey, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)?.GetValue(entity, null);
            return idValue;
        }

        /// <summary>
        /// Checks if the requirements for a valid operation are validated
        /// </summary>
        /// <returns>True if the requirements are validated, false otherwise</returns>
        private bool IsRequirmentsValidated()
        {
            bool retval = true;
            if (EntityStructure == null)
            {
                retval = false;
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = "Missing Entity Structure";
            }
            else if (EntityStructure.PrimaryKeys.Count == 0)
            {
                retval = false;
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = "Missing Entity PrimaryKey";
            }
            if (DataSource == null)
            {
                retval = false;
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = "Missing Entity Datasource";
            }
            return retval;
        }

        #endregion
    }
}