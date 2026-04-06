using System;
using System.Collections.Generic;
using System.Linq;

namespace TheTechIdea.Beep.Editor
{
    public partial class ObservableBindingList<T>
    {
        #region "Field-Level Change Introspection (1-A)"

        /// <summary>
        /// Returns the list of field names that have changed since the original snapshot was taken.
        /// Returns an empty list when the item is not tracked or has no snapshot.
        /// </summary>
        public IReadOnlyList<string> GetChangedFields(T item)
        {
            var tracking = GetTrackingItem(item);
            if (tracking?.OriginalValues == null)
                return Array.Empty<string>();

            var changed = new List<string>();
            foreach (var prop in GetCachedProperties())
            {
                if (!prop.CanRead) continue;
                if (!tracking.OriginalValues.TryGetValue(prop.Name, out var original)) continue;
                var current = prop.GetValue(item);
                if (!Equals(original, current))
                    changed.Add(prop.Name);
            }
            return changed;
        }

        /// <summary>
        /// Returns the (Original, Current) value pair for a specific field.
        /// Returns (null, null) when no snapshot exists or field is not tracked.
        /// </summary>
        public (object Original, object Current) GetFieldDelta(T item, string fieldName)
        {
            var tracking = GetTrackingItem(item);
            if (tracking?.OriginalValues == null)
                return (null, null);

            tracking.OriginalValues.TryGetValue(fieldName, out var original);
            var prop = GetCachedProperty(fieldName);
            var current = prop?.GetValue(item);
            return (original, current);
        }

        /// <summary>
        /// Returns true when at least one field on the item differs from its original snapshot.
        /// Returns false when the item is not tracked or has no snapshot.
        /// </summary>
        public bool HasFieldChanges(T item)
        {
            var tracking = GetTrackingItem(item);
            if (tracking?.OriginalValues == null)
                return false;

            foreach (var prop in GetCachedProperties())
            {
                if (!prop.CanRead) continue;
                if (!tracking.OriginalValues.TryGetValue(prop.Name, out var original)) continue;
                var current = prop.GetValue(item);
                if (!Equals(original, current))
                    return true;
            }
            return false;
        }

        #endregion "Field-Level Change Introspection (1-A)"

        #region "Change-Set Export (1-B)"

        /// <summary>Returns all items that have been inserted (state == Added).</summary>
        public IReadOnlyList<T> GetInserted()
        {
            var result = new List<T>();
            foreach (var item in Items)
            {
                var tr = GetTrackingItem(item);
                if (tr != null && tr.EntityState == EntityState.Added)
                    result.Add(item);
            }
            return result;
        }

        /// <summary>Returns all items that have been modified (state == Modified).</summary>
        public IReadOnlyList<T> GetUpdated()
        {
            var result = new List<T>();
            foreach (var item in Items)
            {
                var tr = GetTrackingItem(item);
                if (tr != null && tr.EntityState == EntityState.Modified)
                    result.Add(item);
            }
            return result;
        }

        /// <summary>Returns all items that have been deleted (in DeletedList).</summary>
        public IReadOnlyList<T> GetDeleted() => DeletedList.AsReadOnly();

        /// <summary>Returns all dirty items — union of Inserted and Updated.</summary>
        public IReadOnlyList<T> GetDirty()
        {
            var result = new List<T>();
            foreach (var item in Items)
            {
                var tr = GetTrackingItem(item);
                if (tr != null && (tr.EntityState == EntityState.Added || tr.EntityState == EntityState.Modified))
                    result.Add(item);
            }
            return result;
        }

        /// <summary>
        /// Returns a lightweight summary of pending changes without allocating item lists.
        /// </summary>
        public ChangeSetSummary GetChangeSetSummary()
        {
            int inserted = 0, updated = 0;
            foreach (var tr in _trackingsByGuid.Values)
            {
                if (tr.EntityState == EntityState.Added) inserted++;
                else if (tr.EntityState == EntityState.Modified) updated++;
            }
            return new ChangeSetSummary
            {
                InsertCount = inserted,
                UpdateCount = updated,
                DeleteCount = DeletedList.Count
            };
        }

        #endregion "Change-Set Export (1-B)"
    }
}
