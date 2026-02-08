using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.UOW
{
    /// <summary>
    /// Additional CRUD operations and async methods partial class for UnitofWork
    /// </summary>
    /// <typeparam name="T">The type of entity</typeparam>
    public partial class UnitofWork<T>
    {
        #region Additional Delete Operations

        /// <summary>
        /// Deletes an entity using a predicate
        /// </summary>
        /// <param name="predicate">Predicate to find the entity to delete</param>
        /// <returns>Result of the delete operation</returns>
        public ErrorsInfo Delete(Func<T, bool> predicate)
        {
            var errorsInfo = new ErrorsInfo();
            
            if (!Validateall())
            {
                errorsInfo.Message = "Validation Failed";
                errorsInfo.Flag = Errors.Failed;
                return errorsInfo;
            }

            try
            {
                var entity = Units.FirstOrDefault(predicate);
                if (entity != null)
                {
                    // Raise pre-delete event
                    var eventArgs = new UnitofWorkParams 
                    { 
                        Cancel = false, 
                        EventAction = EventAction.PreDelete,
                        Record = entity
                    };
                    PreDelete?.Invoke(this, eventArgs);
                    
                    if (eventArgs.Cancel)
                    {
                        errorsInfo.Message = eventArgs.Messege ?? "Delete operation cancelled";
                        errorsInfo.Flag = Errors.Failed;
                        return errorsInfo;
                    }

                    var index = Units.IndexOf(entity);
                    
                    // Determine whether entity was newly added (not yet persisted)
                    bool wasAdded = index >= 0 && _entityStates.ContainsKey(index) && _entityStates[index] == EntityState.Added;

                    // Remove from collection - this shifts all indices above 'index' down by 1
                    if (index >= 0)
                    {
                        Units.RemoveAt(index);
                    }
                    
                    // Re-key entity states to account for index shift after removal
                    var newStates = new Dictionary<int, EntityState>();
                    foreach (var kvp in _entityStates)
                    {
                        if (kvp.Key < index)
                        {
                            newStates[kvp.Key] = kvp.Value;
                        }
                        else if (kvp.Key > index)
                        {
                            // Shift down by 1
                            newStates[kvp.Key - 1] = kvp.Value;
                        }
                        // kvp.Key == index is intentionally dropped (the deleted item)
                    }
                    _entityStates = newStates;

                    // Track as deleted only if it was previously persisted
                    if (!wasAdded)
                    {
                        DeletedUnits.Add(entity);
                    }

                    // Raise post-delete event
                    PostDelete?.Invoke(this, new UnitofWorkParams 
                    { 
                        EventAction = EventAction.PostDelete,
                        Record = entity
                    });

                    errorsInfo.Message = "Delete Done";
                    errorsInfo.Flag = Errors.Ok;
                }
                else
                {
                    errorsInfo.Message = "Entity not found";
                    errorsInfo.Flag = Errors.Failed;
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("UnitofWork", 
                    $"Error deleting entity: {ex.Message}", 
                    DateTime.Now, -1, null, Errors.Failed);
                
                errorsInfo.Message = $"Delete failed: {ex.Message}";
                errorsInfo.Flag = Errors.Failed;
            }

            return errorsInfo;
        }

        /// <summary>
        /// Deletes the specified entity
        /// </summary>
        /// <param name="entity">Entity to delete</param>
        /// <returns>Result of the delete operation</returns>
        public IErrorsInfo Delete(T entity)
        {
            if (entity == null)
            {
                return new ErrorsInfo { Flag = Errors.Failed, Message = "Entity is null" };
            }
            var result = Delete(e => ReferenceEquals(e, entity) || Equals(e, entity));
            return result;
        }

        /// <summary>
        /// Deletes the current entity
        /// </summary>
        /// <returns>Result of the delete operation</returns>
        public IErrorsInfo Delete()
        {
            var errorsInfo = new ErrorsInfo();
            
            if (CurrentItem == null)
            {
                errorsInfo.Message = "No current item selected";
                errorsInfo.Flag = Errors.Failed;
                return errorsInfo;
            }

            var deleteResult = Delete(entity => entity.Equals(CurrentItem));
            errorsInfo.Message = deleteResult.Message;
            errorsInfo.Flag = deleteResult.Flag;
            
            return errorsInfo;
        }

        /// <summary>
        /// Asynchronously deletes an entity from the data source
        /// </summary>
        /// <param name="doc">The entity to delete</param>
        /// <returns>Result of the delete operation</returns>
        public async Task<IErrorsInfo> DeleteAsync(T doc)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            
            if (!IsRequirmentsValidated())
            {
                return DMEEditor.ErrorObject;
            }
            
            // Raise pre-delete event
            var eventArgs = new UnitofWorkParams 
            { 
                Cancel = false, 
                EventAction = EventAction.PreDelete,
                Record = doc
            };
            PreDelete?.Invoke(this, eventArgs);
            
            if (eventArgs.Cancel)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = eventArgs.Messege ?? "Delete operation cancelled";
                return DMEEditor.ErrorObject;
            }
            
            if (doc == null)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = "Entity is null";
                return DMEEditor.ErrorObject;
            }

            try
            {
                IErrorsInfo retval = DeleteDoc(doc);
                
                if (retval.Flag == Errors.Ok)
                {
                    // Raise post-delete event
                    PostDelete?.Invoke(this, new UnitofWorkParams 
                    { 
                        EventAction = EventAction.PostDelete,
                        Record = doc
                    });
                }
                
                return retval;
            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = $"Delete failed: {ex.Message}";
                DMEEditor.ErrorObject.Ex = ex;
                return DMEEditor.ErrorObject;
            }
        }

        /// <summary>
        /// Deletes a document from the data source
        /// </summary>
        /// <param name="doc">The document to delete</param>
        /// <returns>Result of the delete operation</returns>
        public IErrorsInfo DeleteDoc(T doc)
        {
            IErrorsInfo retval;
            
            try
            {
                if (!IsInListMode)
                {
                    string cname = typeof(T).Name;
                    
                    retval = DataSource.DeleteEntity(cname, doc);
                }
                else
                {
                    var index = Units.IndexOf(doc);
                    if (index > -1)
                    {
                        Units.RemoveAt(index);
                        retval = new ErrorsInfo { Flag = Errors.Ok, Message = "Entity deleted from collection" };
                    }
                    else
                    {
                        retval = new ErrorsInfo { Flag = Errors.Failed, Message = "Entity not found in collection" };
                    }
                }
            }
            catch (Exception ex)
            {
                retval = new ErrorsInfo 
                { 
                    Flag = Errors.Failed, 
                    Message = $"Delete failed: {ex.Message}",
                    Ex = ex
                };
            }
            
            return retval;
        }

        #endregion

        #region Read Operations

     

        #endregion

        #region Transaction Management Extensions

        /// <summary>
        /// Commits all changes to the data source with progress reporting and cancellation support
        /// </summary>
        /// <param name="progress">Progress reporter</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Result of the commit operation</returns>
        public async Task<IErrorsInfo> Commit(IProgress<PassedArgs> progress, CancellationToken token)
        {
            var result = new ErrorsInfo { Flag = Errors.Ok };
            
            if (!IsInListMode && !GetIsDirty())
            {
                result.Message = "No changes to commit";
                return result;
            }

            try
            {
                // Raise pre-commit event
                var preCommitArgs = new UnitofWorkParams 
                { 
                    Cancel = false, 
                    EventAction = EventAction.PreCommit 
                };
                PreCommit?.Invoke(this, preCommitArgs);
                
                if (preCommitArgs.Cancel)
                {
                    result.Flag = Errors.Failed;
                    result.Message = preCommitArgs.Messege ?? "Commit operation cancelled";
                    return result;
                }

                if (!IsInListMode && DataSource != null)
                {
                    // Begin transaction if supported
                    var beginTransactionResult = DataSource.BeginTransaction(new PassedArgs());
                    if (beginTransactionResult.Flag != Errors.Ok)
                    {
                        result.Flag = Errors.Failed;
                        result.Message = $"Failed to begin transaction: {beginTransactionResult.Message}";
                        return result;
                    }

                    try
                    {
                        await CommitChangesToDataSource(progress, token);
                        
                        // Commit transaction
                        var commitResult = DataSource.Commit(new PassedArgs());
                        if (commitResult.Flag != Errors.Ok)
                        {
                            result.Flag = Errors.Failed;
                            result.Message = $"Transaction commit failed: {commitResult.Message}";
                            return result;
                        }
                    }
                    catch (Exception)
                    {
                        // Rollback on error
                        DataSource.EndTransaction(new PassedArgs());
                        throw;
                    }
                }
                
                // Clear change tracking
                _entityStates.Clear();
                DeletedUnits.Clear();
                InsertedKeys.Clear();
                UpdatedKeys.Clear();
                DeletedKeys.Clear();

                // Raise post-commit event
                PostCommit?.Invoke(this, new UnitofWorkParams 
                { 
                    EventAction = EventAction.PostCommit 
                });

                result.Message = "Changes committed successfully";
                OnPropertyChanged(nameof(IsDirty));
            }
            catch (OperationCanceledException)
            {
                result.Flag = Errors.Failed;
                result.Message = "Commit operation was cancelled";
            }
            catch (Exception ex)
            {
                result.Flag = Errors.Failed;
                result.Message = $"Commit failed: {ex.Message}";
                result.Ex = ex;
                
                DMEEditor.AddLogMessage("UnitofWork", 
                    $"Commit error: {ex.Message}", 
                    DateTime.Now, -1, null, Errors.Failed);
            }

            return result;
        }

        /// <summary>
        /// Commits all changes to the data source
        /// </summary>
        /// <returns>Result of the commit operation</returns>
        public async Task<IErrorsInfo> Commit()
        {
            return await Commit(null, CancellationToken.None);
        }

        /// <summary>
        /// Rolls back all uncommitted changes
        /// </summary>
        /// <returns>Result of the rollback operation</returns>
        public async Task<IErrorsInfo> Rollback()
        {
            var result = new ErrorsInfo { Flag = Errors.Ok };
            
            try
            {
                if (!IsInListMode && DataSource != null)
                {
                    var rollbackResult = DataSource.EndTransaction(new PassedArgs());
                    if (rollbackResult.Flag != Errors.Ok)
                    {
                        result.Flag = Errors.Failed;
                        result.Message = $"Rollback failed: {rollbackResult.Message}";
                        return result;
                    }
                }

                // Restore from backup if available
                if (Tempunits != null)
                {
                    UndoLastChange();
                }
                else
                {
                    // Clear all changes
                    _entityStates.Clear();
                    DeletedUnits.Clear();
                    InsertedKeys.Clear();
                    UpdatedKeys.Clear();
                    DeletedKeys.Clear();
                }

                result.Message = "Changes rolled back successfully";
                OnPropertyChanged(nameof(IsDirty));
            }
            catch (Exception ex)
            {
                result.Flag = Errors.Failed;
                result.Message = $"Rollback failed: {ex.Message}";
                result.Ex = ex;
                
                DMEEditor.AddLogMessage("UnitofWork", 
                    $"Rollback error: {ex.Message}", 
                    DateTime.Now, -1, null, Errors.Failed);
            }

            return result;
        }

        /// <summary>
        /// Commits individual changes to the data source with progress reporting
        /// </summary>
        private async Task CommitChangesToDataSource(IProgress<PassedArgs> progress, CancellationToken token)
        {
            var totalChanges = GetAddedEntities().Count() + GetModifiedEntities().Count() + GetDeletedEntities().Count();
            var processedChanges = 0;

            // Process added entities
            foreach (var index in GetAddedEntities())
            {
                token.ThrowIfCancellationRequested();
                
                if (index < Units.Count)
                {
                    var entity = Units[index];
                    await InsertAsync(entity);
                    
                    processedChanges++;
                    progress?.Report(new PassedArgs 
                    { 
                        ParameterInt1 = processedChanges,
                        ParameterInt2 = totalChanges,
                        ParameterString1 = $"Inserted entity {processedChanges} of {totalChanges}"
                    });
                }
            }

            // Process modified entities
            foreach (var index in GetModifiedEntities())
            {
                token.ThrowIfCancellationRequested();
                
                if (index < Units.Count)
                {
                    var entity = Units[index];
                    await UpdateAsync(entity);
                    
                    processedChanges++;
                    progress?.Report(new PassedArgs 
                    { 
                        ParameterInt1 = processedChanges,
                        ParameterInt2 = totalChanges,
                        ParameterString1 = $"Updated entity {processedChanges} of {totalChanges}"
                    });
                }
            }

            // Process deleted entities
            foreach (var entity in GetDeletedEntities())
            {
                token.ThrowIfCancellationRequested();
                
                await DeleteAsync(entity);
                
                processedChanges++;
                progress?.Report(new PassedArgs 
                { 
                    ParameterInt1 = processedChanges,
                    ParameterInt2 = totalChanges,
                    ParameterString1 = $"Deleted entity {processedChanges} of {totalChanges}"
                });
            }
        }

        #endregion

        #region Batch Operations

        /// <summary>
        /// Adds a range of entities to the collection
        /// </summary>
        /// <param name="entities">Entities to add</param>
        /// <returns>Result of the operation</returns>
        public async Task<IErrorsInfo> AddRange(IEnumerable<T> entities)
        {
            var result = new ErrorsInfo { Flag = Errors.Ok };
            if (entities == null)
            {
                result.Flag = Errors.Failed;
                result.Message = "Entities collection is null";
                return result;
            }

            try
            {
                _suppressNotification = true;
                int count = 0;
                foreach (var entity in entities)
                {
                    Add(entity);
                    count++;
                }
                _suppressNotification = false;
                OnPropertyChanged(nameof(Units));

                result.Message = $"Added {count} entities";
            }
            catch (Exception ex)
            {
                _suppressNotification = false;
                result.Flag = Errors.Failed;
                result.Message = $"AddRange failed: {ex.Message}";
                result.Ex = ex;
            }

            return result;
        }

        /// <summary>
        /// Updates a range of entities
        /// </summary>
        /// <param name="entities">Entities to update</param>
        /// <returns>Result of the operation</returns>
        public async Task<IErrorsInfo> UpdateRange(IEnumerable<T> entities)
        {
            var result = new ErrorsInfo { Flag = Errors.Ok };
            if (entities == null)
            {
                result.Flag = Errors.Failed;
                result.Message = "Entities collection is null";
                return result;
            }

            try
            {
                int successCount = 0;
                int failCount = 0;
                foreach (var entity in entities)
                {
                    var updateResult = Update(entity);
                    if (updateResult.Flag == Errors.Ok)
                        successCount++;
                    else
                        failCount++;
                }

                result.Message = $"Updated {successCount} entities, {failCount} failed";
                if (failCount > 0 && successCount == 0)
                    result.Flag = Errors.Failed;
            }
            catch (Exception ex)
            {
                result.Flag = Errors.Failed;
                result.Message = $"UpdateRange failed: {ex.Message}";
                result.Ex = ex;
            }

            return result;
        }

        /// <summary>
        /// Deletes a range of entities
        /// </summary>
        /// <param name="entities">Entities to delete</param>
        /// <returns>Result of the operation</returns>
        public async Task<IErrorsInfo> DeleteRange(IEnumerable<T> entities)
        {
            var result = new ErrorsInfo { Flag = Errors.Ok };
            if (entities == null)
            {
                result.Flag = Errors.Failed;
                result.Message = "Entities collection is null";
                return result;
            }

            try
            {
                // Materialize to avoid modifying collection during iteration
                var entityList = new List<T>(entities);
                int successCount = 0;
                int failCount = 0;

                foreach (var entity in entityList)
                {
                    var deleteResult = Delete(entity);
                    if (deleteResult.Flag == Errors.Ok)
                        successCount++;
                    else
                        failCount++;
                }

                result.Message = $"Deleted {successCount} entities, {failCount} failed";
                if (failCount > 0 && successCount == 0)
                    result.Flag = Errors.Failed;
            }
            catch (Exception ex)
            {
                result.Flag = Errors.Failed;
                result.Message = $"DeleteRange failed: {ex.Message}";
                result.Ex = ex;
            }

            return result;
        }

        #endregion

        #region Change Audit

        /// <summary>
        /// Change log for tracking modifications
        /// </summary>
        private readonly List<ChangeRecord> _changeLog = new List<ChangeRecord>();

        /// <summary>
        /// Gets the change log containing all tracked changes
        /// </summary>
        /// <returns>List of change records</returns>
        public List<ChangeRecord> GetChangeLog()
        {
            return new List<ChangeRecord>(_changeLog);
        }

        #endregion

        #region Logging Support

        /// <summary>
        /// Update log for tracking changes (placeholder - would need full implementation)
        /// </summary>
        public Dictionary<DateTime, EntityUpdateInsertLog> UpdateLog { get; set; } = new Dictionary<DateTime, EntityUpdateInsertLog>();

        /// <summary>
        /// Saves the change log to a file
        /// </summary>
        /// <param name="pathandname">Path and filename for the log</param>
        /// <returns>True if successful</returns>
        public bool SaveLog(string pathandname)
        {
            try
            {
                if (UpdateLog == null || UpdateLog.Count == 0)
                    return true;

                var json = System.Text.Json.JsonSerializer.Serialize(UpdateLog, new System.Text.Json.JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                
                System.IO.File.WriteAllText(pathandname, json);
                return true;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("UnitofWork", 
                    $"Error saving log: {ex.Message}", 
                    DateTime.Now, -1, null, Errors.Failed);
                return false;
            }
        }

        #endregion

        #region Missing Event Declarations

        /// <summary>Event fired before creating a new entity</summary>
        public event EventHandler<UnitofWorkParams> PreCreate;
        
        /// <summary>Event fired after creating a new entity</summary>
        public event EventHandler<UnitofWorkParams> PostCreate;
        
        /// <summary>Event fired before inserting to data source</summary>
        public event EventHandler<UnitofWorkParams> PreInsert;
        
        /// <summary>Event fired after inserting to data source</summary>
        public event EventHandler<UnitofWorkParams> PostInsert;
        
        /// <summary>Event fired before updating in data source</summary>
        public event EventHandler<UnitofWorkParams> PreUpdate;
        
        /// <summary>Event fired after updating in data source</summary>
        public event EventHandler<UnitofWorkParams> PostUpdate;
        
        /// <summary>Event fired before deleting from data source</summary>
        public event EventHandler<UnitofWorkParams> PreDelete;
        
        /// <summary>Event fired after deleting from data source</summary>
        public event EventHandler<UnitofWorkParams> PostDelete;
        
        /// <summary>Event fired before querying data</summary>
        public event EventHandler<UnitofWorkParams> PreQuery;
        
        /// <summary>Event fired after querying data</summary>
        public event EventHandler<UnitofWorkParams> PostQuery;
        
        /// <summary>Event fired before committing changes</summary>
        public event EventHandler<UnitofWorkParams> PreCommit;
        
        /// <summary>Event fired after committing changes</summary>
        public event EventHandler<UnitofWorkParams> PostCommit;
        
        /// <summary>Event fired after property changes</summary>
        public event EventHandler<UnitofWorkParams> PostEdit;

        #endregion
    }

    // EntityUpdateInsertLog class is defined in ObservableBindingList.cs (canonical location)
    // Duplicate definition was removed from here to avoid compilation conflicts.
}