using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.UOW.Interfaces;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.UOW.Helpers
{
    /// <summary>
    /// Helper class for collection management in UnitofWork.
    /// Provides filtering, paging, sorting, and synchronization of collections.
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public class UnitofWorkCollectionHelper<T> : IUnitofWorkCollectionHelper<T> where T : Entity, new()
    {
        private readonly IDMEEditor _editor;

        /// <summary>
        /// Initializes a new instance of UnitofWorkCollectionHelper
        /// </summary>
        /// <param name="editor">DME Editor instance</param>
        public UnitofWorkCollectionHelper(IDMEEditor editor)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        }

        /// <summary>
        /// Synchronizes target collection to match source collection
        /// </summary>
        /// <param name="source">Source collection</param>
        /// <param name="target">Target collection to synchronize</param>
        public void SynchronizeCollections(ObservableBindingList<T> source, ObservableBindingList<T> target)
        {
            if (source == null || target == null) return;

            try
            {
                target.Clear();
                foreach (var item in source)
                {
                    target.Add(item);
                }
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("UnitofWorkCollectionHelper",
                    $"Error synchronizing collections: {ex.Message}",
                    DateTime.Now, -1, null, Errors.Failed);
            }
        }

        /// <summary>
        /// Filters collection based on AppFilter criteria
        /// </summary>
        /// <param name="collection">Collection to filter</param>
        /// <param name="filters">Filter criteria</param>
        /// <returns>Filtered collection</returns>
        public ObservableBindingList<T> FilterCollection(ObservableBindingList<T> collection, List<AppFilter> filters)
        {
            if (collection == null || filters == null || filters.Count == 0)
                return collection;

            try
            {
                IEnumerable<T> result = collection;

                foreach (var filter in filters)
                {
                    if (string.IsNullOrEmpty(filter.FieldName) || filter.FilterValue == null)
                        continue;

                    var property = UnitofWorkDataHelper<T>.GetCachedProperties(typeof(T))
                        .FirstOrDefault(p => p.Name.Equals(filter.FieldName, StringComparison.OrdinalIgnoreCase));

                    if (property == null) continue;

                    result = result.Where(item =>
                    {
                        try
                        {
                            var value = property.GetValue(item);
                            return CompareValues(value, filter.FilterValue, filter.Operator);
                        }
                        catch
                        {
                            return false;
                        }
                    });
                }

                return new ObservableBindingList<T>(result.ToList());
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("UnitofWorkCollectionHelper",
                    $"Error filtering collection: {ex.Message}",
                    DateTime.Now, -1, null, Errors.Failed);
                return collection;
            }
        }

        /// <summary>
        /// Applies paging to a collection
        /// </summary>
        /// <param name="collection">Collection to page</param>
        /// <param name="pageIndex">Zero-based page index</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <returns>Paged collection</returns>
        public ObservableBindingList<T> ApplyPaging(ObservableBindingList<T> collection, int pageIndex, int pageSize)
        {
            if (collection == null) return null;
            if (pageSize <= 0) return collection;
            if (pageIndex < 0) pageIndex = 0;

            try
            {
                var paged = collection.Skip(pageIndex * pageSize).Take(pageSize).ToList();
                return new ObservableBindingList<T>(paged);
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("UnitofWorkCollectionHelper",
                    $"Error applying paging: {ex.Message}",
                    DateTime.Now, -1, null, Errors.Failed);
                return collection;
            }
        }

        /// <summary>
        /// Sorts collection by a field name
        /// </summary>
        /// <param name="collection">Collection to sort</param>
        /// <param name="sortField">Property name to sort by</param>
        /// <param name="ascending">True for ascending, false for descending</param>
        /// <returns>Sorted collection</returns>
        public ObservableBindingList<T> SortCollection(ObservableBindingList<T> collection, string sortField, bool ascending)
        {
            if (collection == null || string.IsNullOrEmpty(sortField))
                return collection;

            try
            {
                var property = UnitofWorkDataHelper<T>.GetCachedProperties(typeof(T))
                    .FirstOrDefault(p => p.Name.Equals(sortField, StringComparison.OrdinalIgnoreCase));

                if (property == null) return collection;

                IEnumerable<T> sorted;
                if (ascending)
                {
                    sorted = collection.OrderBy(item => property.GetValue(item));
                }
                else
                {
                    sorted = collection.OrderByDescending(item => property.GetValue(item));
                }

                return new ObservableBindingList<T>(sorted.ToList());
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("UnitofWorkCollectionHelper",
                    $"Error sorting collection: {ex.Message}",
                    DateTime.Now, -1, null, Errors.Failed);
                return collection;
            }
        }

        #region Private Methods

        private bool CompareValues(object propertyValue, object filterValue, string op)
        {
            if (propertyValue == null && filterValue == null) return op == "=";
            if (propertyValue == null || filterValue == null) return op == "!=";

            try
            {
                var comparison = Comparer<IComparable>.Default.Compare(
                    propertyValue as IComparable, filterValue as IComparable);

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
            catch
            {
                // Fall back to string comparison
                return propertyValue?.ToString() == filterValue?.ToString();
            }
        }

        #endregion
    }
}
