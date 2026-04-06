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
        #region "Enhanced Search Functionality"

        /// <summary>
        /// Searches for items matching multiple property conditions with a specific operator between conditions.
        /// </summary>
        /// <param name="propertyConditions">Dictionary of property names and their values to match</param>
        /// <param name="matchOperator">Logical operator to apply between conditions ("AND" or "OR")</param>
        /// <returns>List of matching items</returns>
        public List<T> SearchByProperties(Dictionary<string, object> propertyConditions, string matchOperator = "AND")
        {
            if (propertyConditions == null || propertyConditions.Count == 0)
                return originalList.ToList();

            var parameter = Expression.Parameter(typeof(T), "x");
            Expression expression = null;

            foreach (var condition in propertyConditions)
            {
                var property = Expression.Property(parameter, condition.Key);
                var constant = Expression.Constant(condition.Value);
                var comparison = Expression.Equal(property, constant);

                if (expression == null)
                {
                    expression = comparison;
                }
                else
                {
                    expression = matchOperator.ToUpper() == "AND"
                        ? Expression.AndAlso(expression, comparison)
                        : Expression.OrElse(expression, comparison);
                }
            }

            if (expression == null)
                return originalList.ToList();

            var lambda = Expression.Lambda<Func<T, bool>>(expression, parameter);
            return originalList.Where(lambda.Compile()).ToList();
        }

        /// <summary>
        /// Finds all items with the specified property value matching the search string using 
        /// contains, starts with, or ends with matching.
        /// </summary>
        /// <param name="propertyName">Name of the property to search</param>
        /// <param name="searchText">Text to search for</param>
        /// <param name="matchType">Type of match: "Contains", "StartsWith", "EndsWith", or "Exact"</param>
        /// <param name="ignoreCase">Whether to ignore case when matching</param>
        /// <returns>List of matching items</returns>
        public List<T> SearchByText(string propertyName, string searchText,
                                   string matchType = "Contains", bool ignoreCase = true)
        {
            if (string.IsNullOrEmpty(propertyName) || string.IsNullOrEmpty(searchText))
                return originalList.ToList();

            var property = GetCachedProperty(propertyName);
            if (property == null)
                throw new ArgumentException($"Property '{propertyName}' not found on type {typeof(T).Name}");

            if (property.PropertyType != typeof(string))
                throw new ArgumentException($"Property '{propertyName}' is not a string property");

            StringComparison comparison = ignoreCase
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            return originalList.Where(item =>
            {
                string propertyValue = property.GetValue(item) as string;
                if (propertyValue == null)
                    return false;

                switch (matchType.ToLower())
                {
                    case "startswith":
                        return propertyValue.StartsWith(searchText, comparison);
                    case "endswith":
                        return propertyValue.EndsWith(searchText, comparison);
                    case "exact":
                        return string.Equals(propertyValue, searchText, comparison);
                    case "contains":
                    default:
                        return propertyValue.IndexOf(searchText, comparison) >= 0;
                }
            }).ToList();
        }

        /// <summary>
        /// Searches across multiple string properties for a search term
        /// </summary>
        /// <param name="searchText">Text to search for</param>
        /// <param name="propertyNames">Optional specific properties to search (searches all string properties if null)</param>
        /// <param name="ignoreCase">Whether to ignore case when matching</param>
        /// <returns>List of matching items</returns>
        public List<T> SearchAllProperties(string searchText, IEnumerable<string> propertyNames = null, bool ignoreCase = true)
        {
            if (string.IsNullOrEmpty(searchText))
                return originalList.ToList();

            StringComparison comparison = ignoreCase
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            // Get all string properties if propertyNames is null (using cached reflection)
            var allProps = GetCachedProperties();
            var properties = propertyNames == null
                ? allProps.Where(p => p.PropertyType == typeof(string)).ToList()
                : allProps.Where(p =>
                    propertyNames.Contains(p.Name) && p.PropertyType == typeof(string)).ToList();

            if (!properties.Any())
                return originalList.ToList();

            return originalList.Where(item =>
            {
                foreach (var prop in properties)
                {
                    string value = prop.GetValue(item) as string;
                    if (value != null && value.IndexOf(searchText, comparison) >= 0)
                        return true;
                }
                return false;
            }).ToList();
        }

        /// <summary>
        /// Finds items based on a complex predicate that can include nested property paths
        /// </summary>
        /// <param name="filter">Filter string in format "Property[.NestedProperty] Operator Value"</param>
        /// <param name="separator">Character separating multiple filter conditions</param>
        /// <param name="logicalOperator">Logical operator to use between conditions ("AND" or "OR")</param>
        /// <returns>List of items matching the complex filter</returns>
        public List<T> AdvancedSearch(string filter, char separator = ';', string logicalOperator = "AND")
        {
            if (string.IsNullOrWhiteSpace(filter))
                return originalList.ToList();

            var filters = filter.Split(separator);
            Func<T, bool> combinedPredicate = null;

            foreach (var filterPart in filters)
            {
                var parts = filterPart.Trim().Split(new[] { ' ' }, 3);
                if (parts.Length < 3)
                    continue;

                string propPath = parts[0];
                string op = parts[1];
                string value = parts[2].Trim('\'');

                // Create predicate for this filter part
                Func<T, bool> partPredicate = item => EvaluateCondition(item, propPath, op, value);

                // Combine with existing predicate
                if (combinedPredicate == null)
                    combinedPredicate = partPredicate;
                else if (logicalOperator.ToUpper() == "AND")
                    combinedPredicate = x => combinedPredicate(x) && partPredicate(x);
                else
                    combinedPredicate = x => combinedPredicate(x) || partPredicate(x);
            }

            return combinedPredicate == null
                ? originalList.ToList()
                : originalList.Where(combinedPredicate).ToList();
        }

        private bool EvaluateCondition(T item, string propertyPath, string op, string value)
        {
            // Split property path for nested properties
            var pathParts = propertyPath.Split('.');
            object propValue = item;

            // Navigate the property path
            foreach (var part in pathParts)
            {
                if (propValue == null)
                    return false;

                var propInfo = propValue.GetType().GetProperty(part);
                if (propInfo == null)
                    return false;

                propValue = propInfo.GetValue(propValue);
            }

            if (propValue == null)
                return op.ToUpper() == "IS" && value.ToUpper() == "NULL";

            // Handle different operators
            switch (op.ToUpper())
            {
                case "=":
                case "==":
                    return CompareValues(propValue, value, (a, b) => a.Equals(b));
                case "!=":
                    return CompareValues(propValue, value, (a, b) => !a.Equals(b));
                case ">":
                    return CompareValues(propValue, value, (a, b) => Comparer<IComparable>.Default.Compare((IComparable)a, (IComparable)b) > 0);
                case "<":
                    return CompareValues(propValue, value, (a, b) => Comparer<IComparable>.Default.Compare((IComparable)a, (IComparable)b) < 0);
                case ">=":
                    return CompareValues(propValue, value, (a, b) => Comparer<IComparable>.Default.Compare((IComparable)a, (IComparable)b) >= 0);
                case "<=":
                    return CompareValues(propValue, value, (a, b) => Comparer<IComparable>.Default.Compare((IComparable)a, (IComparable)b) <= 0);
                case "LIKE":
                    return propValue.ToString().Contains(value.Replace("%", ""));
                case "IN":
                    var values = value.Split(',').Select(v => v.Trim());
                    return values.Contains(propValue.ToString());
                default:
                    return false;
            }
        }

        private bool CompareValues(object propValue, string stringValue, Func<object, object, bool> comparison)
        {
            try
            {
                var propType = propValue.GetType();
                object convertedValue;

                if (propType == typeof(string))
                {
                    convertedValue = stringValue;
                }
                else if (propType.IsEnum)
                {
                    convertedValue = Enum.Parse(propType, stringValue);
                }
                else if (propType == typeof(DateTime))
                {
                    convertedValue = DateTime.Parse(stringValue);
                }
                else if (IsNumericType(propType))
                {
                    convertedValue = Convert.ChangeType(stringValue, propType);
                }
                else
                {
                    convertedValue = Convert.ChangeType(stringValue, propType);
                }

                return comparison(propValue, convertedValue);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Applies the search based on a predicate and updates the current view
        /// </summary>
        /// <param name="predicate">Search predicate function</param>
        /// <returns>Number of items found</returns>
        public int FindAndFilter(Func<T, bool> predicate)
        {
            if (predicate == null)
            {
                ResetItems(originalList);
                return Count;
            }

            var results = originalList.Where(predicate).ToList();
            ResetItems(results);
            ResetBindings();
            return results.Count;
        }

        #region "Async Search (1-D)"

        /// <summary>
        /// Searches the list asynchronously on a background thread — safe to call from UI.
        /// Returns all items matching <paramref name="predicate"/>.
        /// </summary>
        public System.Threading.Tasks.Task<IReadOnlyList<T>> SearchAsync(
            Func<T, bool> predicate,
            System.Threading.CancellationToken ct = default)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            // Capture snapshot to avoid cross-thread list mutation
            var snapshot = originalList.ToList();
            return System.Threading.Tasks.Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                var results = new List<T>();
                foreach (var item in snapshot)
                {
                    ct.ThrowIfCancellationRequested();
                    if (predicate(item)) results.Add(item);
                }
                return (IReadOnlyList<T>)results;
            }, ct);
        }

        /// <summary>
        /// Streams matching items asynchronously, yielding each chunk back to the caller.
        /// Useful for large lists where results should appear incrementally.
        /// </summary>
        public async IAsyncEnumerable<T> SearchStreamAsync(
            Func<T, bool> predicate,
            [System.Runtime.CompilerServices.EnumeratorCancellation]
            System.Threading.CancellationToken ct = default)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            var snapshot = originalList.ToList();
            const int chunkSize = 100;

            for (int i = 0; i < snapshot.Count; i += chunkSize)
            {
                ct.ThrowIfCancellationRequested();
                int end = Math.Min(i + chunkSize, snapshot.Count);
                for (int j = i; j < end; j++)
                {
                    if (predicate(snapshot[j]))
                        yield return snapshot[j];
                }
                await System.Threading.Tasks.Task.Yield();
            }
        }

        #endregion "Async Search (1-D)"

        #endregion
    }
}
