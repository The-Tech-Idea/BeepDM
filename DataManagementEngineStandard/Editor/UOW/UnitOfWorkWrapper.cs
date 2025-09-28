using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;
using System.Dynamic;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;

namespace TheTechIdea.Beep.Editor.UOW
{
    /// <summary>
    /// Enhanced wrapper around dynamic UnitOfWork providing strongly-typed interface
    /// with improved error handling, validation, and Oracle Forms compatibility
    /// </summary>
    public class UnitOfWorkWrapper : IUnitOfWorkWrapper
    {
        private dynamic _unitOfWork;
        private bool _disposed = false;

        public UnitOfWorkWrapper(object unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        #region Properties with Enhanced Error Handling

        public bool IsInListMode
        {
            get => GetPropertySafely(() => _unitOfWork.IsInListMode, false);
            set => SetPropertySafely(() => _unitOfWork.IsInListMode = value);
        }

        public bool IsDirty => GetPropertySafely(() => _unitOfWork.IsDirty, false);

        public IDataSource DataSource
        {
            get => GetPropertySafely(() => _unitOfWork.DataSource, default(IDataSource));
            set => SetPropertySafely(() => _unitOfWork.DataSource = value);
        }

        public string DatasourceName
        {
            get => GetPropertySafely(() => _unitOfWork.DatasourceName, string.Empty);
            set => SetPropertySafely(() => _unitOfWork.DatasourceName = value);
        }

        public Dictionary<int, string> DeletedKeys
        {
            get => GetPropertySafely(() => _unitOfWork.DeletedKeys, new Dictionary<int, string>());
            set => SetPropertySafely(() => _unitOfWork.DeletedKeys = value);
        }

        public List<dynamic> DeletedUnits
        {
            get => GetPropertySafely(() => _unitOfWork.DeletedUnits, new List<dynamic>());
            set => SetPropertySafely(() => _unitOfWork.DeletedUnits = value);
        }

        public IDMEEditor DMEEditor => GetPropertySafely(() => _unitOfWork.DMEEditor, default(IDMEEditor));

        public string EntityName
        {
            get => GetPropertySafely(() => _unitOfWork.EntityName, string.Empty);
            set => SetPropertySafely(() => _unitOfWork.EntityName = value);
        }

        public EntityStructure EntityStructure
        {
            get => GetPropertySafely(() => _unitOfWork.EntityStructure, default(EntityStructure));
            set => SetPropertySafely(() => _unitOfWork.EntityStructure = value);
        }

        public Type EntityType
        {
            get => GetPropertySafely(() => _unitOfWork.EntityType, default(Type));
            set => SetPropertySafely(() => _unitOfWork.EntityType = value);
        }

        public string GuidKey
        {
            get => GetPropertySafely(() => _unitOfWork.GuidKey, string.Empty);
            set => SetPropertySafely(() => _unitOfWork.GuidKey = value);
        }

        public string Sequencer
        {
            get => GetPropertySafely(() => _unitOfWork.Sequencer, string.Empty);
            set => SetPropertySafely(() => _unitOfWork.Sequencer = value);
        }

        public Dictionary<int, string> InsertedKeys
        {
            get => GetPropertySafely(() => _unitOfWork.InsertedKeys, new Dictionary<int, string>());
            set => SetPropertySafely(() => _unitOfWork.InsertedKeys = value);
        }

        public string PrimaryKey
        {
            get => GetPropertySafely(() => _unitOfWork.PrimaryKey, string.Empty);
            set => SetPropertySafely(() => _unitOfWork.PrimaryKey = value);
        }

        public dynamic Units
        {
            get => GetPropertySafely(() => _unitOfWork.Units, null);
            set => SetPropertySafely(() => _unitOfWork.Units = value);
        }

        public Dictionary<int, string> UpdatedKeys
        {
            get => GetPropertySafely(() => _unitOfWork.UpdatedKeys, new Dictionary<int, string>());
            set => SetPropertySafely(() => _unitOfWork.UpdatedKeys = value);
        }

        public bool IsIdentity
        {
            get => GetPropertySafely(() => _unitOfWork.IsIdentity, false);
            set => SetPropertySafely(() => _unitOfWork.IsIdentity = value);
        }

        #endregion

        #region Enhanced Properties for Oracle Forms Compatibility

        /// <summary>
        /// Gets the current item (current record) - Oracle Forms equivalent
        /// </summary>
        public dynamic CurrentItem => GetPropertySafely(() => _unitOfWork.CurrentItem, null);

        /// <summary>
        /// Gets the current index (current record position) - Oracle Forms equivalent
        /// </summary>
        public int CurrentIndex => GetPropertySafely(() => _unitOfWork.CurrentIndex, -1);

        /// <summary>
        /// Gets the total count of records - Oracle Forms equivalent
        /// </summary>
        public int Count => GetPropertySafely(() => _unitOfWork.Count, 0);

        #endregion

        #region Core Operations with Enhanced Error Handling

        public void Clear() => ExecuteSafely(() => _unitOfWork.Clear());

        public double GetLastIdentity() => ExecuteSafely(() => _unitOfWork.GetLastIdentity(), 0.0);

        public IEnumerable<int> GetAddedEntities() => 
            ExecuteSafely(() => _unitOfWork.GetAddedEntities(), new List<int>());

        public async Task<dynamic> GetQuery(string query)
        {
            ValidateNotDisposed();
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Query cannot be null or empty", nameof(query));

            return await ExecuteSafelyAsync(async () => await _unitOfWork.GetQuery(query), null);
        }

        public async Task<dynamic> Get() => 
            await ExecuteSafelyAsync(async () => await _unitOfWork.Get(), null);

        public async Task<dynamic> Get(List<AppFilter> filters) => 
            await ExecuteSafelyAsync(async () => await _unitOfWork.Get(filters), null);

        public IEnumerable<dynamic> GetDeletedEntities() => 
            ExecuteSafely(() => _unitOfWork.GetDeletedEntities(), new List<dynamic>());

        public dynamic Get(int key) => ExecuteSafely(() => _unitOfWork.Get(key), null);

        public dynamic GetIDValue(dynamic entity) => 
            ExecuteSafely(() => _unitOfWork.GetIDValue(entity), null);

        public int Getindex(string id) => ExecuteSafely(() => _unitOfWork.Getindex(id), -1);

        public int Getindex(dynamic entity) => ExecuteSafely(() => _unitOfWork.Getindex(entity), -1);

        public IEnumerable<int> GetModifiedEntities() => 
            ExecuteSafely(() => _unitOfWork.GetModifiedEntities(), new List<int>());

        public int GetPrimaryKeySequence(dynamic doc) => 
            ExecuteSafely(() => _unitOfWork.GetPrimaryKeySequence(doc), -1);

        public int GetSeq(string SeqName) => 
            ExecuteSafely(() => _unitOfWork.GetSeq(SeqName), -1);

        public dynamic Read(string id) => ExecuteSafely(() => _unitOfWork.Read(id), null);

        public dynamic Get(string PrimaryKeyid) => 
            ExecuteSafely(() => _unitOfWork.Get(PrimaryKeyid), null);

        #endregion

        #region Transaction Operations with Enhanced Error Handling

        public async Task<IErrorsInfo> Commit(IProgress<PassedArgs> progress, CancellationToken token)
        {
            ValidateNotDisposed();
            return await ExecuteSafelyAsync(
                async () => await _unitOfWork.Commit(progress, token),
                new ErrorsInfo { Flag = Errors.Failed, Message = "Commit operation failed" }
            );
        }

        public async Task<IErrorsInfo> Commit()
        {
            ValidateNotDisposed();
            return await ExecuteSafelyAsync(
                async () => await _unitOfWork.Commit(),
                new ErrorsInfo { Flag = Errors.Failed, Message = "Commit operation failed" }
            );
        }

        public async Task<IErrorsInfo> Rollback()
        {
            ValidateNotDisposed();
            return await ExecuteSafelyAsync(
                async () => await _unitOfWork.Rollback(),
                new ErrorsInfo { Flag = Errors.Failed, Message = "Rollback operation failed" }
            );
        }

        #endregion

        #region CRUD Operations with Enhanced Error Handling

        // Async CRUD operations (preferred)
        public async Task<IErrorsInfo> UpdateAsync(dynamic doc)
        {
            ValidateNotDisposed();
            ValidateDocument(doc, nameof(doc));
            return await ExecuteSafelyAsync(
                async () => await _unitOfWork.UpdateAsync(doc),
                new ErrorsInfo { Flag = Errors.Failed, Message = "Update operation failed" }
            );
        }

        public async Task<IErrorsInfo> InsertAsync(dynamic doc)
        {
            ValidateNotDisposed();
            ValidateDocument(doc, nameof(doc));
            return await ExecuteSafelyAsync(
                async () => await _unitOfWork.InsertAsync(doc),
                new ErrorsInfo { Flag = Errors.Failed, Message = "Insert operation failed" }
            );
        }

        public async Task<IErrorsInfo> DeleteAsync(dynamic doc)
        {
            ValidateNotDisposed();
            ValidateDocument(doc, nameof(doc));
            return await ExecuteSafelyAsync(
                async () => await _unitOfWork.DeleteAsync(doc),
                new ErrorsInfo { Flag = Errors.Failed, Message = "Delete operation failed" }
            );
        }

        // Document operations
        public async Task<IErrorsInfo> InsertDoc(dynamic doc)
        {
            ValidateNotDisposed();
            ValidateDocument(doc, nameof(doc));
            return await ExecuteSafelyAsync(
                async () => await _unitOfWork.InsertDoc(doc),
                new ErrorsInfo { Flag = Errors.Failed, Message = "Insert document operation failed" }
            );
        }

        public async Task<IErrorsInfo> UpdateDoc(dynamic doc)
        {
            ValidateNotDisposed();
            ValidateDocument(doc, nameof(doc));
            return await ExecuteSafelyAsync(
                async () => await _unitOfWork.UpdateDoc(doc),
                new ErrorsInfo { Flag = Errors.Failed, Message = "Update document operation failed" }
            );
        }

        public async Task<IErrorsInfo> DeleteDoc(dynamic doc)
        {
            ValidateNotDisposed();
            ValidateDocument(doc, nameof(doc));
            return await ExecuteSafelyAsync(
                async () => await _unitOfWork.DeleteDoc(doc),
                new ErrorsInfo { Flag = Errors.Failed, Message = "Delete document operation failed" }
            );
        }

        public int DocExist(dynamic doc) => ExecuteSafely(() => _unitOfWork.DocExist(doc), -1);

        public int DocExistByKey(dynamic doc) => ExecuteSafely(() => _unitOfWork.DocExistByKey(doc), -1);

        public int FindDocIdx(dynamic doc) => ExecuteSafely(() => _unitOfWork.FindDocIdx(doc), -1);

        // Legacy sync CRUD operations (for backward compatibility)
        public void Create(dynamic entity)
        {
            ValidateNotDisposed();
            ValidateDocument(entity, nameof(entity));
            ExecuteSafely(() => _unitOfWork.Create(entity));
        }

        public ErrorsInfo Delete(string id)
        {
            ValidateNotDisposed();
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID cannot be null or empty", nameof(id));

            return ExecuteSafely(() => _unitOfWork.Delete(id), 
                new ErrorsInfo { Flag = Errors.Failed, Message = "Delete operation failed" });
        }

        public ErrorsInfo Delete(dynamic doc)
        {
            ValidateNotDisposed();
            ValidateDocument(doc, nameof(doc));
            return ExecuteSafely(() => _unitOfWork.Delete(doc),
                new ErrorsInfo { Flag = Errors.Failed, Message = "Delete operation failed" });
        }

        public ErrorsInfo Update(dynamic entity)
        {
            ValidateNotDisposed();
            ValidateDocument(entity, nameof(entity));
            return ExecuteSafely(() => _unitOfWork.Update(entity),
                new ErrorsInfo { Flag = Errors.Failed, Message = "Update operation failed" });
        }

        public ErrorsInfo Update(string id, dynamic entity)
        {
            ValidateNotDisposed();
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID cannot be null or empty", nameof(id));
            ValidateDocument(entity, nameof(entity));

            return ExecuteSafely(() => _unitOfWork.Update(id, entity),
                new ErrorsInfo { Flag = Errors.Failed, Message = "Update operation failed" });
        }

        #endregion

        #region Navigation Operations (Oracle Forms Compatible)

        /// <summary>
        /// Move to first record - Oracle Forms GO_RECORD(FIRST_RECORD) equivalent
        /// </summary>
        public void MoveFirst()
        {
            ValidateNotDisposed();
            ExecuteSafely(() => _unitOfWork.MoveFirst());
        }

        /// <summary>
        /// Move to next record - Oracle Forms NEXT_RECORD equivalent
        /// </summary>
        public void MoveNext()
        {
            ValidateNotDisposed();
            ExecuteSafely(() => _unitOfWork.MoveNext());
        }

        /// <summary>
        /// Move to previous record - Oracle Forms PREVIOUS_RECORD equivalent
        /// </summary>
        public void MovePrevious()
        {
            ValidateNotDisposed();
            ExecuteSafely(() => _unitOfWork.MovePrevious());
        }

        /// <summary>
        /// Move to last record - Oracle Forms GO_RECORD(LAST_RECORD) equivalent
        /// </summary>
        public void MoveLast()
        {
            ValidateNotDisposed();
            ExecuteSafely(() => _unitOfWork.MoveLast());
        }

        /// <summary>
        /// Move to specific record index - Oracle Forms GO_RECORD equivalent
        /// </summary>
        public void MoveTo(int index)
        {
            ValidateNotDisposed();
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), "Index cannot be negative");

            ExecuteSafely(() => _unitOfWork.MoveTo(index));
        }

        #endregion

        #region Enhanced Navigation Methods

        /// <summary>
        /// Checks if current position is at first record
        /// </summary>
        public bool IsAtFirst => CurrentIndex <= 0;

        /// <summary>
        /// Checks if current position is at last record
        /// </summary>
        public bool IsAtLast => CurrentIndex >= Count - 1;

        /// <summary>
        /// Safely move to next record, returns true if successful
        /// </summary>
        public bool TryMoveNext()
        {
            try
            {
                if (!IsAtLast)
                {
                    MoveNext();
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Safely move to previous record, returns true if successful
        /// </summary>
        public bool TryMovePrevious()
        {
            try
            {
                if (!IsAtFirst)
                {
                    MovePrevious();
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Helper Methods for Safe Dynamic Operations

        private T GetPropertySafely<T>(Func<T> getter, T defaultValue)
        {
            try
            {
                ValidateNotDisposed();
                return getter();
            }
            catch (Exception ex)
            {
                LogError($"Error getting property: {ex.Message}");
                return defaultValue;
            }
        }

        private void SetPropertySafely(Action setter)
        {
            try
            {
                ValidateNotDisposed();
                setter();
            }
            catch (Exception ex)
            {
                LogError($"Error setting property: {ex.Message}");
                throw;
            }
        }

        private T ExecuteSafely<T>(Func<T> operation, T defaultValue)
        {
            try
            {
                ValidateNotDisposed();
                return operation();
            }
            catch (Exception ex)
            {
                LogError($"Error executing operation: {ex.Message}");
                return defaultValue;
            }
        }

        private void ExecuteSafely(Action operation)
        {
            try
            {
                ValidateNotDisposed();
                operation();
            }
            catch (Exception ex)
            {
                LogError($"Error executing operation: {ex.Message}");
                throw;
            }
        }

        private async Task<T> ExecuteSafelyAsync<T>(Func<Task<T>> operation, T defaultValue)
        {
            try
            {
                ValidateNotDisposed();
                return await operation();
            }
            catch (Exception ex)
            {
                LogError($"Error executing async operation: {ex.Message}");
                return defaultValue;
            }
        }

        private void ValidateNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitOfWorkWrapper));
        }

        private void ValidateDocument(dynamic doc, string paramName)
        {
            if (doc == null)
                throw new ArgumentNullException(paramName, "Document cannot be null");
        }

        private void LogError(string message)
        {
            // Log error - in real implementation, use proper logging framework
            System.Diagnostics.Debug.WriteLine($"UnitOfWorkWrapper Error: {message}");
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                try
                {
                    // Dispose the wrapped unit of work if it implements IDisposable
                    if (_unitOfWork is IDisposable disposableUnitOfWork)
                    {
                        disposableUnitOfWork.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Error disposing wrapped unit of work: {ex.Message}");
                }
                finally
                {
                    _unitOfWork = null;
                    _disposed = true;
                }
            }
        }

        ~UnitOfWorkWrapper()
        {
            Dispose(false);
        }

        #endregion
    }
}