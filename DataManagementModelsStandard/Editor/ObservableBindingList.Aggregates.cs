using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace TheTechIdea.Beep.Editor
{
    public partial class ObservableBindingList<T>
    {
        #region "Aggregates / Summary — Phase 7D"

        /// <summary>
        /// Sums a numeric property across all items in the current view.
        /// </summary>
        public decimal Sum(string propertyName)
        {
            var prop = GetPropertyOrThrow(propertyName);
            return Items.Sum(item => ConvertToDecimal(prop.GetValue(item)));
        }

        /// <summary>
        /// Sums a numeric property across items matching a predicate.
        /// </summary>
        public decimal SumWhere(string propertyName, Func<T, bool> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            var prop = GetPropertyOrThrow(propertyName);
            return Items.Where(predicate).Sum(item => ConvertToDecimal(prop.GetValue(item)));
        }

        /// <summary>
        /// Averages a numeric property across all items in the current view.
        /// Returns 0 if no items.
        /// </summary>
        public decimal Average(string propertyName)
        {
            if (Items.Count == 0) return 0m;
            var prop = GetPropertyOrThrow(propertyName);
            return Items.Average(item => ConvertToDecimal(prop.GetValue(item)));
        }

        /// <summary>
        /// Averages a numeric property across items matching a predicate.
        /// Returns 0 if no matching items.
        /// </summary>
        public decimal AverageWhere(string propertyName, Func<T, bool> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            var prop = GetPropertyOrThrow(propertyName);
            var matching = Items.Where(predicate).ToList();
            if (matching.Count == 0) return 0m;
            return matching.Average(item => ConvertToDecimal(prop.GetValue(item)));
        }

        /// <summary>
        /// Gets the minimum value of a property across all items in the current view.
        /// Returns null if no items.
        /// </summary>
        public object Min(string propertyName)
        {
            if (Items.Count == 0) return null;
            var prop = GetPropertyOrThrow(propertyName);
            return Items.Min(item => prop.GetValue(item));
        }

        /// <summary>
        /// Gets the maximum value of a property across all items in the current view.
        /// Returns null if no items.
        /// </summary>
        public object Max(string propertyName)
        {
            if (Items.Count == 0) return null;
            var prop = GetPropertyOrThrow(propertyName);
            return Items.Max(item => prop.GetValue(item));
        }

        /// <summary>
        /// Counts items matching a predicate in the current view.
        /// </summary>
        public int CountWhere(Func<T, bool> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            return Items.Count(predicate);
        }

        /// <summary>
        /// Groups items by a property value.
        /// </summary>
        public Dictionary<object, List<T>> GroupBy(string propertyName)
        {
            var prop = GetPropertyOrThrow(propertyName);
            return Items
                .GroupBy(item => prop.GetValue(item))
                .ToDictionary(g => g.Key ?? (object)"(null)", g => g.ToList());
        }

        /// <summary>
        /// Groups items by a property, but only items matching a predicate.
        /// </summary>
        public Dictionary<object, List<T>> GroupByWhere(string propertyName, Func<T, bool> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            var prop = GetPropertyOrThrow(propertyName);
            return Items.Where(predicate)
                .GroupBy(item => prop.GetValue(item))
                .ToDictionary(g => g.Key ?? (object)"(null)", g => g.ToList());
        }

        /// <summary>
        /// Returns distinct values for a property in the current view.
        /// </summary>
        public List<object> DistinctValues(string propertyName)
        {
            var prop = GetPropertyOrThrow(propertyName);
            return Items.Select(item => prop.GetValue(item)).Distinct().ToList();
        }

        #region "Helpers"

        /// <summary>
        /// Gets a PropertyInfo for the given property name, throwing if not found.
        /// </summary>
        private PropertyInfo GetPropertyOrThrow(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentNullException(nameof(propertyName));

            var prop = typeof(T).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (prop == null)
                throw new ArgumentException($"Property '{propertyName}' not found on type '{typeof(T).Name}'.");
            return prop;
        }

        /// <summary>
        /// Safely converts a value to decimal for aggregation.
        /// </summary>
        private decimal ConvertToDecimal(object value)
        {
            if (value == null || value == DBNull.Value) return 0m;
            try
            {
                return Convert.ToDecimal(value);
            }
            catch
            {
                return 0m;
            }
        }

        #endregion

        #endregion
    }
}
