using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace TheTechIdea.Beep.Editor
{
    public partial class ObservableBindingList<T>
    {
        #region "Filter"
        /// <summary>Indicates that filtering is supported.</summary>
        public bool SupportsFiltering => true;

        #region "Pipeline Helper Methods"

        /// <summary>
        /// After filtering from originalList, re-applies the active sort if one is set.
        /// Called by all ApplyFilter methods before ResetItems.
        /// </summary>
        private List<T> ApplyActiveSortIfNeeded(List<T> items)
        {
            if (isSorted && _activeSortProperty != null)
            {
                var comparer = new PropertyComparer<T>(_activeSortProperty, _activeSortDirection);
                items.Sort(comparer);
            }
            return items;
        }

        /// <summary>
        /// Re-derives the view from originalList by calling existing filter/sort/page methods
        /// in sequence based on which state fields are set. Called by InsertItem, RemoveItem,
        /// AddRange, RemoveRange after data mutations to keep the view consistent.
        /// Does NOT replace existing methods — just calls them.
        /// </summary>
        private void ReapplyActiveTransformations()
        {
            SuppressNotification = true;
            RaiseListChangedEvents = false;

            // Start from originalList
            List<T> workingSet = new List<T>(originalList);

            // Re-apply filter if active
            if (_activeFilterPredicate != null)
            {
                workingSet = workingSet.Where(_activeFilterPredicate).ToList();
            }
            else if (!string.IsNullOrEmpty(filterString))
            {
                var fil = ParseFilter(filterString);
                if (fil != null)
                    workingSet = originalList.AsQueryable().Where(fil).ToList();
            }

            _filteredCount = workingSet.Count;

            // Re-apply sort if active
            workingSet = ApplyActiveSortIfNeeded(workingSet);

            // Store for pagination
            _currentWorkingSet = workingSet;

            // Re-apply pagination if active
            if (_isPagingActive && PageSize > 0)
            {
                int totalPages = (int)Math.Ceiling((double)workingSet.Count / PageSize);
                if (CurrentPage > totalPages) CurrentPage = Math.Max(1, totalPages);
                workingSet = workingSet.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
            }

            ResetItems(workingSet);

            ResetBindings();
            SuppressNotification = false;
            RaiseListChangedEvents = true;
        }

        #endregion

        // New ApplyFilter method using predicate
        /// <summary>Applies or removes a filter using a predicate function.</summary>
        public void ApplyFilter(Func<T, bool> predicate)
        {
            SuppressNotification = true;
            RaiseListChangedEvents = false;

            if (predicate == null)
            {
                // Remove filter by resetting items to the original list
                _activeFilterPredicate = null;
                _filteredCount = originalList.Count;
                _currentWorkingSet = null;
                ResetItems(originalList);
                FilterRemoved?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                // Apply the predicate to filter the items
                _activeFilterPredicate = predicate;
                var filteredItems = originalList.Where(predicate).ToList();
                filteredItems = ApplyActiveSortIfNeeded(filteredItems);  // BUG 1 fix
                _filteredCount = filteredItems.Count;
                _currentWorkingSet = filteredItems;
                ResetItems(filteredItems);
                FilterApplied?.Invoke(this, EventArgs.Empty);
            }

            ResetBindings();
            SuppressNotification = false;
            RaiseListChangedEvents = true;
        }
        /// <summary>Applies a filter by comparing a property value using the specified operator.</summary>
        public void ApplyFilter(string propertyName, object value, string comparisonOperator = "==")
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(parameter, propertyName);
            var constant = Expression.Constant(value);

            BinaryExpression comparison;
            switch (comparisonOperator)
            {
                case "==":
                    comparison = Expression.Equal(property, constant);
                    break;
                case "!=":
                    comparison = Expression.NotEqual(property, constant);
                    break;
                case ">":
                    comparison = Expression.GreaterThan(property, constant);
                    break;
                case "<":
                    comparison = Expression.LessThan(property, constant);
                    break;
                case ">=":
                    comparison = Expression.GreaterThanOrEqual(property, constant);
                    break;
                case "<=":
                    comparison = Expression.LessThanOrEqual(property, constant);
                    break;
                default:
                    throw new ArgumentException("Invalid comparison operator");
            }

            var lambda = Expression.Lambda<Func<T, bool>>(comparison, parameter);
            ApplyFilter(lambda.Compile());
        }

        /// <summary>Gets or sets the string-based filter expression (IBindingListView).</summary>
        public string Filter
        {
            get => filterString;
            set
            {
                if (filterString != value)
                {
                    filterString = value;
                    ApplyFilter();
                }
            }
        }
        /// <summary>Removes any active filter and restores the full list.</summary>
        public void RemoveFilter()
        {
            _activeFilterPredicate = null;
            _filteredCount = originalList.Count;
            _currentWorkingSet = null;
            Filter = null;
            FilterRemoved?.Invoke(this, EventArgs.Empty);
        }
        private void ApplyFilter()
        {
            SuppressNotification = true;
            RaiseListChangedEvents = false;
            if (string.IsNullOrWhiteSpace(filterString))
            {
                _filteredCount = originalList.Count;
                _currentWorkingSet = null;
                ResetItems(originalList);
                FilterRemoved?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                var fil = ParseFilter(filterString);
                if(fil == null)
                {
                    return;
                }
                var filteredItems = originalList.AsQueryable().Where(fil).ToList();
                filteredItems = ApplyActiveSortIfNeeded(filteredItems);  // BUG 1 fix
                _filteredCount = filteredItems.Count;
                _currentWorkingSet = filteredItems;
                ResetItems(filteredItems);
                FilterApplied?.Invoke(this, EventArgs.Empty);
            }
            ResetBindings();
            SuppressNotification = false;
            RaiseListChangedEvents = true;
           
        }
        private Expression<Func<T, bool>> ParseFilter(string filter)
        {
            var parameter = Expression.Parameter(typeof(T), "x");

            // Split by OR first, then each OR-group by AND
            // Example: "Name LIKE '%John%' AND Age > 30 OR City = 'NY'"
            var orGroups = filter.Split(new string[] { " OR " }, StringSplitOptions.RemoveEmptyEntries);
            Expression orExpression = null;

            foreach (var orGroup in orGroups)
            {
                var andFilters = orGroup.Split(new string[] { " AND " }, StringSplitOptions.RemoveEmptyEntries);
                Expression andExpression = null;

                foreach (var f in andFilters)
                {
                    var comparison = ParseSingleCondition(f.Trim(), parameter);
                    if (comparison != null)
                    {
                        andExpression = andExpression == null ? comparison : Expression.AndAlso(andExpression, comparison);
                    }
                }

                if (andExpression != null)
                {
                    orExpression = orExpression == null ? andExpression : Expression.OrElse(orExpression, andExpression);
                }
            }

            return orExpression != null ? Expression.Lambda<Func<T, bool>>(orExpression, parameter) : null;
        }

        private Expression ParseSingleCondition(string condition, ParameterExpression parameter)
        {
            var parts = condition.Split(new[] { ' ' }, 3);
            if (parts.Length < 3)
                return null;

            string propName = parts[0];
            string op = parts[1];
            string value = parts[2].Trim('\'');

            var property = Expression.Property(parameter, propName);
            var propertyType = Nullable.GetUnderlyingType(property.Type) ?? property.Type;

            Expression comparison = null;
            bool treatAsString = value.Contains("%");

            if ((op.ToUpper() == "LIKE") && (propertyType == typeof(string) || treatAsString))
            {
                var nullCheck = Expression.NotEqual(property, Expression.Constant(null, property.Type));
                var propertyAsString = Expression.Call(property, typeof(object).GetMethod("ToString", Type.EmptyTypes));

                Expression containsExpression = null;
                if (value.StartsWith("%") && value.EndsWith("%"))
                {
                    value = value.Trim('%');
                    containsExpression = Expression.Call(propertyAsString, typeof(string).GetMethod("Contains", new[] { typeof(string) }), Expression.Constant(value));
                }
                else if (value.StartsWith("%"))
                {
                    value = value.TrimStart('%');
                    containsExpression = Expression.Call(propertyAsString, typeof(string).GetMethod("EndsWith", new[] { typeof(string) }), Expression.Constant(value));
                }
                else if (value.EndsWith("%"))
                {
                    value = value.TrimEnd('%');
                    containsExpression = Expression.Call(propertyAsString, typeof(string).GetMethod("StartsWith", new[] { typeof(string) }), Expression.Constant(value));
                }

                comparison = containsExpression != null ? Expression.AndAlso(nullCheck, containsExpression) : null;
            }
            else
            {
                // Ensure the value is converted to the correct type
                object convertedValue = null;
                try
                {
                    if (propertyType == typeof(string) || treatAsString)
                    {
                        convertedValue = value;
                    }
                    else if (propertyType.IsEnum)
                    {
                        convertedValue = Enum.Parse(propertyType, value);
                    }
                    else if (propertyType == typeof(DateTime))
                    {
                        convertedValue = DateTime.Parse(value);
                    }
                    else if (IsNumericType(propertyType))
                    {
                        convertedValue = Convert.ChangeType(value, propertyType);
                    }
                    else
                    {
                        convertedValue = Convert.ChangeType(value, propertyType);
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidCastException($"Failed to convert value '{value}' to type '{propertyType}'", ex);
                }

                var valueExpression = Expression.Constant(convertedValue, treatAsString ? typeof(string) : property.Type);

                switch (op.ToUpper())
                {
                    case "=":
                        comparison = Expression.Equal(property, valueExpression);
                        break;
                    case "!=":
                        comparison = Expression.NotEqual(property, valueExpression);
                        break;
                    case ">":
                        comparison = Expression.GreaterThan(property, valueExpression);
                        break;
                    case "<":
                        comparison = Expression.LessThan(property, valueExpression);
                        break;
                    case ">=":
                        comparison = Expression.GreaterThanOrEqual(property, valueExpression);
                        break;
                    case "<=":
                        comparison = Expression.LessThanOrEqual(property, valueExpression);
                        break;
                }
            }

            return comparison;
        }
        private void ResetItems(List<T> items)
        {
            // Create a copy of the list for safe iteration
            var itemsCopy = new List<T>(items);

            // Bypass InsertItem override by using Items directly.
            // This avoids re-adding to originalList and corrupting Trackings.
            // Unhook PropertyChanged from current items first
            foreach (var item in Items)
            {
                if (item is INotifyPropertyChanged npc)
                    npc.PropertyChanged -= Item_PropertyChanged;
            }
            Items.Clear();

            // Add items directly to the underlying list (no InsertItem override)
            foreach (var item in itemsCopy)
            {
                Items.Add(item);
                if (item is INotifyPropertyChanged npc)
                    npc.PropertyChanged += Item_PropertyChanged;
            }

            UpdateIndexTrackingAfterFilterorSort(); // Update index mapping after resetting items

            // BUG 4 fix: Clamp _currentIndex to valid range after filter/sort/page
            if (Items.Count == 0)
                _currentIndex = -1;
            else if (_currentIndex >= Items.Count)
                _currentIndex = 0;
            else if (_currentIndex < 0 && Items.Count > 0)
                _currentIndex = 0;

            // Phase 6B: invalidate bookmarks after filter/sort/page resets
            InvalidateBookmarks();
        }
        /// <summary>Resets bindings by firing a ListChanged Reset event.</summary>
        public new void ResetBindings()
        {
            OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
        }
        #endregion
    }
}
