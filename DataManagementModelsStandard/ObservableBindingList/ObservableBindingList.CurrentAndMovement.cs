using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace TheTechIdea.Beep.Editor
{
    public partial class ObservableBindingList<T>
    {
        #region "Current and Movement"

        #region "3A — Current/Index (Index is single source of truth)"

        /// <summary>
        /// Gets or sets the current position. Setting calls MoveTo(value).
        /// </summary>
        public int CurrentIndex
        {
            get => _currentIndex;
            set => MoveTo(value);
        }

        /// <summary>
        /// Gets the item at the current position. Read-only — change via CurrentIndex or Move methods.
        /// </summary>
        public T Current
        {
            get
            {
                if (_currentIndex >= 0 && _currentIndex < Items.Count)
                {
                    return Items[_currentIndex];
                }
                else
                {
                    return default;
                }
            }
        }

        /// <summary>
        /// Navigates to the position of the specified item.
        /// Returns true if the item was found and position changed.
        /// </summary>
        public bool MoveToItem(T item)
        {
            if (item == null) return false;
            int idx = Items.IndexOf(item);
            if (idx >= 0)
                return MoveTo(idx);
            return false;
        }

        #endregion

        #region "3B — BOF/EOF Cursor Semantics"

        /// <summary>True when cursor is at the first item or the list is empty.</summary>
        public bool IsAtBOF => _currentIndex <= 0 || Count == 0;

        /// <summary>True when cursor is at the last item or the list is empty.</summary>
        public bool IsAtEOF => _currentIndex >= Count - 1 || Count == 0;

        /// <summary>True when the list contains no items.</summary>
        public bool IsEmpty => Count == 0;

        /// <summary>True when _currentIndex points to a valid item.</summary>
        public bool IsPositionValid => _currentIndex >= 0 && _currentIndex < Count;

        #endregion

        #region "3C — CurrentChanging Event"

        /// <summary>
        /// Fires BEFORE the current position changes. Set Cancel = true to prevent navigation.
        /// </summary>
        public event EventHandler<CurrentChangingEventArgs> CurrentChanging;

        /// <summary>
        /// Fires AFTER the current position has changed.
        /// </summary>
        public event EventHandler CurrentChanged;

        /// <summary>
        /// Raises CurrentChanging. Returns true if the navigation was NOT cancelled.
        /// </summary>
        private bool RaiseCurrentChanging(int newIndex)
        {
            if (CurrentChanging == null) return true; // no subscribers, allow

            object oldItem = IsPositionValid ? (object)Items[_currentIndex] : null;
            object newItem = (newIndex >= 0 && newIndex < Items.Count) ? (object)Items[newIndex] : null;

            var args = new CurrentChangingEventArgs(_currentIndex, newIndex, oldItem, newItem);
            CurrentChanging.Invoke(this, args);
            return !args.Cancel;
        }

        /// <summary>
        /// Fires CurrentChanged and PropertyChanged("Current").
        /// </summary>
        protected virtual void OnCurrentChanged()
        {
            SuppressNotification = true;
            _isPositionChanging = true;
            CurrentChanged?.Invoke(this, EventArgs.Empty);
            _isPositionChanging = false;
            SuppressNotification = false;
        }

        #endregion

        #region "Navigation Methods"

        /// <summary>Moves to the next item. Returns false if already at EOF.</summary>
        public bool MoveNext()
        {
            if (_currentIndex < Items.Count - 1)
            {
                int proposed = _currentIndex + 1;
                if (!RaiseCurrentChanging(proposed)) return false;

                _currentIndex = proposed;
                OnCurrentChanged();
                OnPropertyChanged("Current");
                OnPropertyChanged("CurrentIndex");
                OnPropertyChanged("IsAtBOF");
                OnPropertyChanged("IsAtEOF");
                return true;
            }
            return false;
        }

        /// <summary>Moves to the previous item. Returns false if already at BOF.</summary>
        public bool MovePrevious()
        {
            if (_currentIndex > 0)
            {
                int proposed = _currentIndex - 1;
                if (!RaiseCurrentChanging(proposed)) return false;

                _currentIndex = proposed;
                OnCurrentChanged();
                OnPropertyChanged("Current");
                OnPropertyChanged("CurrentIndex");
                OnPropertyChanged("IsAtBOF");
                OnPropertyChanged("IsAtEOF");
                return true;
            }
            return false;
        }

        /// <summary>Moves to the first item. Returns false if list is empty.</summary>
        public bool MoveFirst()
        {
            if (Items.Count > 0)
            {
                if (!RaiseCurrentChanging(0)) return false;

                _currentIndex = 0;
                OnCurrentChanged();
                OnPropertyChanged("Current");
                OnPropertyChanged("CurrentIndex");
                OnPropertyChanged("IsAtBOF");
                OnPropertyChanged("IsAtEOF");
                return true;
            }
            return false;
        }

        /// <summary>Moves to the last item. Returns false if list is empty.</summary>
        public bool MoveLast()
        {
            if (Items.Count > 0)
            {
                int proposed = Items.Count - 1;
                if (!RaiseCurrentChanging(proposed)) return false;

                _currentIndex = proposed;
                OnCurrentChanged();
                OnPropertyChanged("Current");
                OnPropertyChanged("CurrentIndex");
                OnPropertyChanged("IsAtBOF");
                OnPropertyChanged("IsAtEOF");
                return true;
            }
            return false;
        }

        /// <summary>Moves to the specified index. Returns false if index is out of range or cancelled.</summary>
        public bool MoveTo(int index)
        {
            if (index >= 0 && index < Items.Count)
            {
                if (index == _currentIndex) return true; // already there

                if (!RaiseCurrentChanging(index)) return false;

                _currentIndex = index;
                OnCurrentChanged();
                OnPropertyChanged("Current");
                OnPropertyChanged("CurrentIndex");
                OnPropertyChanged("IsAtBOF");
                OnPropertyChanged("IsAtEOF");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Sets position without raising CurrentChanging (internal/programmatic use).
        /// Still fires CurrentChanged.
        /// </summary>
        public void SetPosition(int newPosition)
        {
            if (newPosition >= 0 && newPosition < Count)
            {
                SuppressNotification = true;
                _isPositionChanging = true;
                try
                {
                    _currentIndex = newPosition;
                    OnCurrentChanged();
                    OnPropertyChanged("Current");
                    OnPropertyChanged("CurrentIndex");
                    OnPropertyChanged("IsAtBOF");
                    OnPropertyChanged("IsAtEOF");
                }
                finally
                {
                    SuppressNotification = false;
                    _isPositionChanging = false;
                }
            }
        }

        #endregion

        #endregion
    }
}
