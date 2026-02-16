using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace TheTechIdea.Beep.Editor
{
    public partial class ObservableBindingList<T>
    {
        #region "Validation Framework"

        #region "Cached Annotation Metadata"

        /// <summary>
        /// Static cache of validation attributes per property for type T.
        /// Key: PropertyName, Value: list of ValidationAttribute instances.
        /// Built once per generic type instantiation.
        /// </summary>
        private static readonly Lazy<Dictionary<string, List<System.ComponentModel.DataAnnotations.ValidationAttribute>>> _annotationCache
            = new Lazy<Dictionary<string, List<System.ComponentModel.DataAnnotations.ValidationAttribute>>>(() =>
            {
                var cache = new Dictionary<string, List<System.ComponentModel.DataAnnotations.ValidationAttribute>>();
                foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    var attrs = prop.GetCustomAttributes<System.ComponentModel.DataAnnotations.ValidationAttribute>(true).ToList();
                    if (attrs.Count > 0)
                        cache[prop.Name] = attrs;
                }
                return cache;
            });

        #endregion

        #region "Per-Item Validation Cache"

        /// <summary>
        /// Cached validation results, keyed by item reference.
        /// Invalidated on property change for the affected item.
        /// </summary>
        private readonly Dictionary<T, Editor.ValidationResult> _validationCache
            = new Dictionary<T, Editor.ValidationResult>();

        #endregion

        #region "Configuration"

        /// <summary>
        /// When true, each property change triggers validation of the changed item.
        /// Default: false (opt-in).
        /// </summary>
        public bool IsAutoValidateEnabled { get; set; } = false;

        /// <summary>
        /// When true, CommitItemAsync blocks items that have Error-severity validation failures.
        /// Default: true.
        /// </summary>
        public bool BlockCommitOnValidationError { get; set; } = true;

        /// <summary>
        /// Optional custom validator. Called after Data Annotations.
        /// Return a ValidationResult with any additional errors.
        /// </summary>
        public Func<T, Editor.ValidationResult> CustomValidator { get; set; }

        #endregion

        #region "Events"

        /// <summary>
        /// Raised when validation fails for an item (auto-validate or explicit call).
        /// </summary>
        public event EventHandler<ValidationEventArgs<T>> ValidationFailed;

        #endregion

        #region "Validate Single Item"

        /// <summary>
        /// Validates a single item using Data Annotations and the CustomValidator.
        /// Caches the result and raises ValidationFailed if errors exist.
        /// </summary>
        public Editor.ValidationResult Validate(T item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            var result = new Editor.ValidationResult();

            // 1. Run Data Annotations
            RunAnnotationValidation(item, result, propertyFilter: null);

            // 2. Run custom validator
            if (CustomValidator != null)
            {
                var customResult = CustomValidator(item);
                if (customResult != null)
                    result.Merge(customResult);
            }

            // 3. Cache
            _validationCache[item] = result;

            // 4. Raise event if errors
            if (!result.IsValid)
                ValidationFailed?.Invoke(this, new ValidationEventArgs<T>(item, result));

            return result;
        }

        /// <summary>
        /// Validates a single property on an item.
        /// Updates the cached result for the item (merges with existing non-property errors).
        /// </summary>
        public Editor.ValidationResult ValidateProperty(T item, string propertyName)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (string.IsNullOrEmpty(propertyName)) return Validate(item);

            // Get or create cached result
            if (!_validationCache.TryGetValue(item, out var cachedResult))
            {
                cachedResult = new Editor.ValidationResult();
                _validationCache[item] = cachedResult;
            }

            // Remove old errors for this property
            cachedResult.Errors.RemoveAll(e => e.PropertyName == propertyName);

            // Re-validate just this property
            RunAnnotationValidation(item, cachedResult, propertyFilter: propertyName);

            // Run custom validator (full item) — merge only new property errors
            if (CustomValidator != null)
            {
                var customResult = CustomValidator(item);
                if (customResult != null)
                {
                    var propErrors = customResult.Errors
                        .Where(e => e.PropertyName == propertyName)
                        .ToList();
                    cachedResult.Errors.AddRange(propErrors);
                }
            }

            // Raise event if errors on this property
            if (cachedResult.Errors.Any(e => e.PropertyName == propertyName && e.Severity == ValidationSeverity.Error))
                ValidationFailed?.Invoke(this, new ValidationEventArgs<T>(item, cachedResult, propertyName));

            return cachedResult;
        }

        #endregion

        #region "Validate All"

        /// <summary>
        /// Validates every item in the current list.
        /// Returns a merged ValidationResult.
        /// </summary>
        public Editor.ValidationResult ValidateAll()
        {
            var merged = new Editor.ValidationResult();
            foreach (var item in Items)
            {
                var itemResult = Validate(item);
                merged.Merge(itemResult);
            }
            return merged;
        }

        /// <summary>
        /// True when all items in the current list pass validation (no Error-severity issues).
        /// Forces a full validation if the cache is empty.
        /// </summary>
        public bool IsAllValid
        {
            get
            {
                foreach (var item in Items)
                {
                    if (!_validationCache.TryGetValue(item, out var cached))
                        cached = Validate(item);
                    if (!cached.IsValid)
                        return false;
                }
                return true;
            }
        }

        #endregion

        #region "Error Query"

        /// <summary>
        /// Gets all validation errors for a specific item.
        /// Returns cached result or runs validation if not cached.
        /// </summary>
        public List<ValidationError> GetErrors(T item)
        {
            if (item == null) return new List<ValidationError>();
            if (!_validationCache.TryGetValue(item, out var cached))
                cached = Validate(item);
            return cached.Errors;
        }

        /// <summary>
        /// Gets validation errors for a specific property on a specific item.
        /// </summary>
        public List<ValidationError> GetErrors(T item, string propertyName)
        {
            if (item == null) return new List<ValidationError>();
            if (!_validationCache.TryGetValue(item, out var cached))
                cached = Validate(item);
            return cached.GetPropertyErrors(propertyName);
        }

        /// <summary>
        /// Returns all items that currently have validation errors.
        /// </summary>
        public List<T> GetInvalidItems()
        {
            var invalid = new List<T>();
            foreach (var item in Items)
            {
                if (!_validationCache.TryGetValue(item, out var cached))
                    cached = Validate(item);
                if (!cached.IsValid)
                    invalid.Add(item);
            }
            return invalid;
        }

        /// <summary>
        /// Clears the validation cache for all items.
        /// </summary>
        public void ClearValidationCache()
        {
            _validationCache.Clear();
        }

        /// <summary>
        /// Clears the validation cache for a specific item.
        /// </summary>
        public void ClearValidationCache(T item)
        {
            if (item != null)
                _validationCache.Remove(item);
        }

        #endregion

        #region "Internal — Data Annotations Runner"

        /// <summary>
        /// Runs Data Annotation validation attributes against an item.
        /// If propertyFilter is non-null, only validates that property.
        /// Appends errors to the provided result.
        /// </summary>
        private void RunAnnotationValidation(T item, Editor.ValidationResult result, string propertyFilter)
        {
            var annotations = _annotationCache.Value;

            IEnumerable<KeyValuePair<string, List<System.ComponentModel.DataAnnotations.ValidationAttribute>>> props;
            if (propertyFilter != null)
            {
                if (!annotations.ContainsKey(propertyFilter))
                    return; // no annotations on this property
                props = new[] { new KeyValuePair<string, List<System.ComponentModel.DataAnnotations.ValidationAttribute>>(propertyFilter, annotations[propertyFilter]) };
            }
            else
            {
                props = annotations;
            }

            foreach (var kvp in props)
            {
                string propName = kvp.Key;
                var propInfo = GetCachedProperty(propName);
                if (propInfo == null) continue;

                object value = propInfo.GetValue(item);

                foreach (var attr in kvp.Value)
                {
                    var context = new System.ComponentModel.DataAnnotations.ValidationContext(item)
                    {
                        MemberName = propName
                    };

                    var validationResult = attr.GetValidationResult(value, context);
                    if (validationResult != null && validationResult != System.ComponentModel.DataAnnotations.ValidationResult.Success)
                    {
                        result.Errors.Add(new ValidationError(
                            propName,
                            validationResult.ErrorMessage ?? $"Validation failed for {propName}",
                            ValidationSeverity.Error
                        ));
                    }
                }
            }
        }

        /// <summary>
        /// Called from Item_PropertyChanged when auto-validation is enabled.
        /// Validates only the changed property for efficiency.
        /// </summary>
        internal void AutoValidateProperty(T item, string propertyName)
        {
            if (!IsAutoValidateEnabled) return;
            ValidateProperty(item, propertyName);
        }

        #endregion

        #endregion
    }
}
