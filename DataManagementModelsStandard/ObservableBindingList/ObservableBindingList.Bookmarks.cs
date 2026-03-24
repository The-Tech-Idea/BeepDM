using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace TheTechIdea.Beep.Editor
{
    public partial class ObservableBindingList<T>
    {
        #region "Bookmarks / Named Positions — Phase 6B"

        /// <summary>
        /// Named bookmarks mapping name → index position.
        /// </summary>
        private readonly Dictionary<string, int> _bookmarks
            = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets all saved bookmarks as a read-only dictionary.
        /// </summary>
        public IReadOnlyDictionary<string, int> Bookmarks
            => new ReadOnlyDictionary<string, int>(_bookmarks);

        /// <summary>
        /// Saves the current cursor position under the given name.
        /// Overwrites an existing bookmark with the same name.
        /// </summary>
        public void SetBookmark(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            _bookmarks[name] = _currentIndex;
        }

        /// <summary>
        /// Saves a specific index position under the given name.
        /// </summary>
        public void SetBookmark(string name, int index)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            _bookmarks[name] = index;
        }

        /// <summary>
        /// Navigates to a previously saved bookmark.
        /// Returns false if the bookmark doesn't exist or the saved position is no longer valid.
        /// </summary>
        public bool GoToBookmark(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (!_bookmarks.TryGetValue(name, out var index)) return false;

            // Check if the saved position is still valid
            if (index < 0 || index >= Items.Count)
            {
                // Bookmark is stale — remove it
                _bookmarks.Remove(name);
                return false;
            }

            return MoveTo(index);
        }

        /// <summary>
        /// Removes a specific bookmark.
        /// </summary>
        public void RemoveBookmark(string name)
        {
            if (!string.IsNullOrEmpty(name))
                _bookmarks.Remove(name);
        }

        /// <summary>
        /// Removes all bookmarks.
        /// </summary>
        public void ClearBookmarks()
        {
            _bookmarks.Clear();
        }

        /// <summary>
        /// Invalidates all bookmarks (called after filter/sort/page changes).
        /// Marks all saved positions as potentially stale by removing those
        /// that are now out of range.
        /// </summary>
        internal void InvalidateBookmarks()
        {
            if (_bookmarks.Count == 0) return;

            var stale = new List<string>();
            foreach (var kvp in _bookmarks)
            {
                if (kvp.Value < 0 || kvp.Value >= Items.Count)
                    stale.Add(kvp.Key);
            }
            foreach (var key in stale)
                _bookmarks.Remove(key);
        }

        #endregion
    }
}
