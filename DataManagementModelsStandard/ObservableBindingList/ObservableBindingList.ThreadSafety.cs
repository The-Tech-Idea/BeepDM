using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace TheTechIdea.Beep.Editor
{
    public partial class ObservableBindingList<T>
    {
        #region "Thread Safety — Phase 7B"

        /// <summary>
        /// Reader-writer lock for thread-safe operations.
        /// Created lazily when thread safety is enabled.
        /// </summary>
        private ReaderWriterLockSlim _rwLock;

        /// <summary>
        /// When true, all public reads and mutations are wrapped in reader/writer locks.
        /// Default: false (zero overhead).
        /// </summary>
        private bool _isThreadSafe;

        /// <summary>
        /// Gets or sets whether thread-safe mode is enabled.
        /// </summary>
        public bool IsThreadSafe
        {
            get => _isThreadSafe;
            set
            {
                _isThreadSafe = value;
                if (value && _rwLock == null)
                    _rwLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
            }
        }

        /// <summary>
        /// Enters a read lock if thread safety is enabled.
        /// Returns an IDisposable that releases the lock.
        /// </summary>
        public IDisposable EnterReadLock()
        {
            if (!_isThreadSafe || _rwLock == null) return NullDisposable.Instance;
            _rwLock.EnterReadLock();
            return new LockReleaser(_rwLock, isWrite: false);
        }

        /// <summary>
        /// Enters a write lock if thread safety is enabled.
        /// Returns an IDisposable that releases the lock.
        /// </summary>
        public IDisposable EnterWriteLock()
        {
            if (!_isThreadSafe || _rwLock == null) return NullDisposable.Instance;
            _rwLock.EnterWriteLock();
            return new LockReleaser(_rwLock, isWrite: true);
        }

        /// <summary>
        /// Executes an action under a read lock (if thread safety is enabled).
        /// </summary>
        public TResult ReadLocked<TResult>(Func<TResult> func)
        {
            if (!_isThreadSafe || _rwLock == null) return func();

            _rwLock.EnterReadLock();
            try
            {
                return func();
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Executes an action under a write lock (if thread safety is enabled).
        /// </summary>
        public void WriteLocked(Action action)
        {
            if (!_isThreadSafe || _rwLock == null)
            {
                action();
                return;
            }

            _rwLock.EnterWriteLock();
            try
            {
                action();
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        #endregion

        #region "Freeze / Read-Only Mode — Phase 7C"

        /// <summary>
        /// When true, all mutations throw InvalidOperationException.
        /// </summary>
        private bool _isFrozen;

        /// <summary>
        /// Gets whether the list is currently frozen (read-only).
        /// </summary>
        public bool IsFrozen => _isFrozen;

        /// <summary>
        /// Freezes the list, preventing all mutations.
        /// </summary>
        public void Freeze()
        {
            _isFrozen = true;
        }

        /// <summary>
        /// Unfreezes the list, allowing mutations again.
        /// </summary>
        public void Unfreeze()
        {
            _isFrozen = false;
        }

        /// <summary>
        /// Throws if the list is frozen. Call at the start of mutation operations.
        /// </summary>
        internal void ThrowIfFrozen()
        {
            if (_isFrozen)
                throw new InvalidOperationException("Cannot modify a frozen ObservableBindingList.");
        }

        #endregion

        #region "Batch Update — Phase 7C"

        /// <summary>
        /// Saved state for nested batch updates.
        /// </summary>
        private int _batchUpdateDepth;

        /// <summary>
        /// Begins a batch update. Suppresses all notifications until the returned
        /// IDisposable is disposed. Supports nesting — only the outermost batch
        /// fires a Reset notification.
        /// </summary>
        public IDisposable BeginBatchUpdate()
        {
            _batchUpdateDepth++;
            if (_batchUpdateDepth == 1)
            {
                SuppressNotification = true;
                RaiseListChangedEvents = false;
            }
            return new BatchUpdateScope(this);
        }

        /// <summary>
        /// Ends a batch update. Only the outermost batch fires a reset.
        /// </summary>
        private void EndBatchUpdate()
        {
            if (_batchUpdateDepth <= 0) return;
            _batchUpdateDepth--;
            if (_batchUpdateDepth == 0)
            {
                SuppressNotification = false;
                RaiseListChangedEvents = true;
                ResetBindings();
            }
        }

        /// <summary>
        /// Disposable scope for batch updates.
        /// </summary>
        private class BatchUpdateScope : IDisposable
        {
            private readonly ObservableBindingList<T> _list;
            private bool _disposed;

            public BatchUpdateScope(ObservableBindingList<T> list) => _list = list;

            public void Dispose()
            {
                if (!_disposed)
                {
                    _disposed = true;
                    _list.EndBatchUpdate();
                }
            }
        }

        #endregion

        #region "Internal helpers"

        /// <summary>
        /// No-op disposable returned when thread safety is disabled.
        /// </summary>
        private class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new NullDisposable();
            public void Dispose() { }
        }

        /// <summary>
        /// Releases a reader or writer lock on dispose.
        /// </summary>
        private class LockReleaser : IDisposable
        {
            private readonly ReaderWriterLockSlim _lock;
            private readonly bool _isWrite;
            private bool _disposed;

            public LockReleaser(ReaderWriterLockSlim rwLock, bool isWrite)
            {
                _lock = rwLock;
                _isWrite = isWrite;
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _disposed = true;
                    if (_isWrite)
                        _lock.ExitWriteLock();
                    else
                        _lock.ExitReadLock();
                }
            }
        }

        #endregion
    }
}
