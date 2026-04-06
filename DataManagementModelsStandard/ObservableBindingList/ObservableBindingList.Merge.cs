using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Editor
{
    /// <summary>
    /// Determines how conflicts are resolved when local edits overlap with server updates.
    /// </summary>
    public enum ConflictMode
    {
        /// <summary>Keep local edits; discard conflicting server values.</summary>
        ClientWins,
        /// <summary>Overwrite local edits with incoming server values.</summary>
        ServerWins,
        /// <summary>Mark the item as Conflicted and let the application resolve manually.</summary>
        KeepBoth,
        /// <summary>Throw a <see cref="MergeConflictException"/> listing the conflicting fields.</summary>
        ThrowOnConflict
    }

    /// <summary>
    /// Summary of a merge operation result.
    /// </summary>
    public class MergeResult<T>
    {
        public int Added { get; set; }
        public int Updated { get; set; }
        public int Conflicted { get; set; }
        public int Unchanged { get; set; }
        public IReadOnlyList<T> ConflictedItems { get; set; } = Array.Empty<T>();
    }

    /// <summary>
    /// Thrown by <see cref="ObservableBindingList{T}.Merge"/> when mode is
    /// <see cref="ConflictMode.ThrowOnConflict"/> and at least one conflict is found.
    /// </summary>
    public class MergeConflictException : Exception
    {
        public IReadOnlyList<string> ConflictingFields { get; }
        public MergeConflictException(IReadOnlyList<string> fields)
            : base($"Merge conflict on fields: {string.Join(", ", fields)}")
            => ConflictingFields = fields;
    }

    public partial class ObservableBindingList<T>
    {
        #region "Server Merge / Conflict Resolution (1-E)"

        /// <summary>
        /// Merges <paramref name="serverItems"/> into the local list.
        /// Items are matched by <paramref name="primaryKeyField"/> (reflection).
        /// </summary>
        public MergeResult<T> Merge(
            IEnumerable<T> serverItems,
            ConflictMode mode = ConflictMode.ServerWins,
            string primaryKeyField = null)
        {
            return MergeCore(serverItems?.ToList() ?? new List<T>(), mode, primaryKeyField);
        }

        /// <summary>
        /// Async overload — performs the merge on a Task so it doesn't block the UI thread.
        /// </summary>
        public Task<MergeResult<T>> MergeAsync(
            IEnumerable<T> serverItems,
            ConflictMode mode = ConflictMode.ServerWins,
            string primaryKeyField = null,
            CancellationToken ct = default)
        {
            var list = serverItems?.ToList() ?? new List<T>();
            return Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                return MergeCore(list, mode, primaryKeyField);
            }, ct);
        }

        // ── internals ─────────────────────────────────────────────────────────────────

        private MergeResult<T> MergeCore(
            List<T> serverList,
            ConflictMode mode,
            string pkField)
        {
            var result = new MergeResult<T>();
            var conflictedItems = new List<T>();

            // Resolve PK property once
            PropertyInfo pkProp = string.IsNullOrEmpty(pkField)
                ? null
                : GetCachedProperty(pkField);

            // Build a lookup: pkValue → local item
            var localMap = new Dictionary<object, T>();
            if (pkProp != null)
            {
                foreach (var item in Items)
                {
                    var key = pkProp.GetValue(item);
                    if (key != null)
                        localMap[key] = item;
                }
            }

            // Track which local items were seen in server data
            var serverKeys = new HashSet<object>();

            foreach (var serverItem in serverList)
            {
                object serverKey = pkProp?.GetValue(serverItem);

                if (pkProp != null && serverKey != null && localMap.TryGetValue(serverKey, out var localItem))
                {
                    serverKeys.Add(serverKey);
                    var localTracking = GetTrackingItem(localItem);
                    bool localHasDirtyFields = localTracking?.EntityState == EntityState.Modified
                                              || localTracking?.EntityState == EntityState.Added;

                    if (!localHasDirtyFields)
                    {
                        // Item is clean — apply server values silently
                        ApplyValues(localItem, serverItem);
                        result.Updated++;
                    }
                    else
                    {
                        // Conflict: local item is modified and server also has a new version
                        var conflictFields = new List<string>();
                        foreach (var prop in GetCachedProperties())
                        {
                            if (!prop.CanRead || !prop.CanWrite) continue;
                            var sv = prop.GetValue(serverItem);
                            var lv = prop.GetValue(localItem);
                            if (!Equals(sv, lv)) conflictFields.Add(prop.Name);
                        }

                        switch (mode)
                        {
                            case ConflictMode.ServerWins:
                                ApplyValues(localItem, serverItem);
                                result.Updated++;
                                break;

                            case ConflictMode.ClientWins:
                                // Do nothing — keep local values
                                result.Unchanged++;
                                break;

                            case ConflictMode.KeepBoth:
                                conflictedItems.Add(localItem);
                                result.Conflicted++;
                                break;

                            case ConflictMode.ThrowOnConflict:
                                throw new MergeConflictException(conflictFields);
                        }
                    }
                }
                else
                {
                    // New item from server — add to local list
                    LoadBatch(new[] { serverItem });
                    result.Added++;
                }
            }

            result.ConflictedItems = conflictedItems;
            return result;
        }

        /// <summary>Copies all readable+writable property values from <paramref name="source"/> into <paramref name="target"/>.</summary>
        private void ApplyValues(T target, T source)
        {
            foreach (var prop in GetCachedProperties())
            {
                if (prop.CanRead && prop.CanWrite)
                    prop.SetValue(target, prop.GetValue(source));
            }
        }

        #endregion "Server Merge / Conflict Resolution (1-E)"
    }
}
