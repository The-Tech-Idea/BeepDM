using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

                    // Remove from collection — OBL handles tracking automatically:
                    // - Sets entity state to Deleted
                    // - Adds to DeletedList
                    // - Re-keys internal tracking indices
                    if (index >= 0)
                    {
                        Units.RemoveAt(index);
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
                        var oblCommitResult = await CommitChangesToDataSource(progress, token);
                        
                        if (!oblCommitResult.AllSucceeded)
                        {
                            // Some items failed — rollback transaction
                            DataSource.EndTransaction(new PassedArgs());
                            result.Flag = Errors.Failed;
                            result.Message = $"Commit partially failed: {oblCommitResult.FailedCount} of {oblCommitResult.TotalCount} items failed";
                            return result;
                        }

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
                
                // OBL's CommitAllAsync already called AcceptChanges per-item for succeeded items.
                // Clear auxiliary key tracking dictionaries.
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

                // Delegate to OBL's RejectChanges which restores original values,
                // re-adds deleted items, removes added items, and resets states
                if (Units != null)
                {
                    Units.RejectChanges();
                }

                // Clear auxiliary tracking
                DeletedUnits.Clear();
                InsertedKeys.Clear();
                UpdatedKeys.Clear();
                DeletedKeys.Clear();

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
        /// Commits individual changes to the data source using OBL's CommitAllAsync.
        /// OBL handles ordering, BeforeSave/AfterSave events, validation blocking, and per-item tracking cleanup.
        /// </summary>
        private async Task<CommitResult> CommitChangesToDataSource(IProgress<PassedArgs> progress, CancellationToken token)
        {
            var commitResult = await Units.CommitAllAsync(
                insertAsync: async (item) =>
                {
                    token.ThrowIfCancellationRequested();
                    _defaultsHelper?.ApplyInsertDefaults(item);
                    return InsertDoc(item);
                },
                updateAsync: async (item) =>
                {
                    token.ThrowIfCancellationRequested();
                    _defaultsHelper?.ApplyUpdateDefaults(item);
                    return UpdateDoc(item);
                },
                deleteAsync: async (item) =>
                {
                    token.ThrowIfCancellationRequested();
                    string cname = typeof(T).Name;
                    if (!IsInListMode && DataSource != null)
                    {
                        return DataSource.DeleteEntity(cname, item);
                    }
                    return new ErrorsInfo { Flag = Errors.Ok, Message = "Deleted from collection" };
                },
                CommitOrder
            );

            // Report progress
            if (progress != null)
            {
                progress.Report(new PassedArgs
                {
                    ParameterInt1 = commitResult.SuccessCount,
                    ParameterInt2 = commitResult.TotalCount,
                    ParameterString1 = $"Committed {commitResult.SuccessCount} of {commitResult.TotalCount} entities ({commitResult.FailedCount} failed)"
                });
            }

            return commitResult;
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

        #region "Phase 2 — Item Utilities (2-C, 2-H, 2-I, 2-J)"

        // ── Revert (2-C) ──────────────────────────────────────────────────────────

        /// <summary>
        /// Reverts <paramref name="item"/> to its original values, undoing any pending
        /// inserts, modifications or deletes for that item.
        /// </summary>
        /// <returns><c>true</c> if the item was reverted; <c>false</c> if the item was not tracked.</returns>
        public bool RevertItem(T item)
        {
            if (Units == null || item == null) return false;

            var tracking = Units.GetTrackingItem(item);
            if (tracking == null) return false;

            Units.RejectChanges(item);

            OnItemReverted?.Invoke(this, new UnitofWorkParams
            {
                EventAction = EventAction.PostEdit,
                Record      = item,
                EntityName  = EntityName
            });

            return true;
        }

        /// <summary>Asynchronous wrapper for <see cref="RevertItem"/>.</summary>
        public Task<bool> RevertItemAsync(T item, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(RevertItem(item));
        }

        // ── Find (2-H) ────────────────────────────────────────────────────────────────

        /// <summary>Returns the first item matching <paramref name="predicate"/>, or <c>default</c>.</summary>
        public Task<T> FindAsync(Func<T, bool> predicate, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (Units == null || predicate == null) return Task.FromResult(default(T));
            return Task.FromResult(Units.FirstOrDefault(predicate));
        }

        /// <summary>Returns all items matching <paramref name="predicate"/>.</summary>
        public Task<List<T>> FindManyAsync(Func<T, bool> predicate, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (Units == null || predicate == null) return Task.FromResult(new List<T>());
            return Task.FromResult(Units.Where(predicate).ToList());
        }

        // ── Clone (2-H) ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a copy of <paramref name="item"/>.
        /// When <paramref name="deepCopy"/> is <c>true</c>, a JSON round-trip is used to produce
        /// a fully independent clone; otherwise a shallow property copy is performed.
        /// </summary>
        public T CloneItem(T item, bool deepCopy = false)
        {
            if (item == null) return default;

            if (deepCopy)
            {
                var json   = System.Text.Json.JsonSerializer.Serialize(item);
                return System.Text.Json.JsonSerializer.Deserialize<T>(json);
            }

            // Shallow copy via reflection
            var clone = new T();
            foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.CanWrite)
                    prop.SetValue(clone, prop.GetValue(item));
            }
            return clone;
        }

        // ── Count predicate (2-I) ─────────────────────────────────────────────────────

        /// <summary>Returns the number of items matching <paramref name="predicate"/>.</summary>
        public int Count(Func<T, bool> predicate)
            => Units == null ? 0 : Units.Count(predicate);

        // ── Undo helpers (2-J) ───────────────────────────────────────────────────────

        /// <summary>
        /// Configures the undo/redo system.
        /// </summary>
        /// <param name="enable">When <c>true</c>, undo recording is enabled.</param>
        /// <param name="maxDepth">Maximum number of undo steps to retain. Default is 100.</param>
        public void EnableUndo(bool enable, int maxDepth = 100)
        {
            IsUndoEnabled = enable;
            MaxUndoDepth  = maxDepth;
        }

        /// <summary>Performs an undo operation. Alias for <see cref="Undo"/>.</summary>
        public bool UndoLastAction() => Undo();

        /// <summary>Performs a redo operation. Alias for <see cref="Redo"/>.</summary>
        public bool RedoLastAction() => Redo();

        #endregion

        #region Aggregates (IAggregatable)

        /// <summary>Count items matching an untyped predicate (IAggregatable explicit implementation).</summary>
        int IAggregatable.Count(Func<object, bool> predicate)
        {
            if (Units == null) return 0;
            if (predicate == null) return Units.Count;
            return Units.Cast<object>().Count(predicate);
        }

        #endregion
    }

    // EntityUpdateInsertLog class is defined in ObservableBindingList.cs (canonical location)
    // Duplicate definition was removed from here to avoid compilation conflicts.
}