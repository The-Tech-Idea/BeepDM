using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor.UOWManager.Helpers
{
    /// <summary>
    /// Manages validation rules and executes validation for Oracle Forms-style validation.
    /// Implements WHEN-VALIDATE-ITEM, WHEN-VALIDATE-RECORD, and PRE-/POST- trigger validation patterns.
    /// Thread-safe implementation using ConcurrentDictionary.
    /// </summary>
    public class ValidationManager : IValidationManager
    {
        #region Fields
        
        private readonly ConcurrentDictionary<string, ValidationRule> _rulesByName;
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, List<ValidationRule>>> _rulesByBlockItem;
        private readonly ConcurrentDictionary<string, bool> _blockValidationEnabled;
        private IDataSource _dataSource;
        private readonly object _lockObject = new object();
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Initializes a new instance of ValidationManager
        /// </summary>
        public ValidationManager()
        {
            _rulesByName = new ConcurrentDictionary<string, ValidationRule>(StringComparer.OrdinalIgnoreCase);
            _rulesByBlockItem = new ConcurrentDictionary<string, ConcurrentDictionary<string, List<ValidationRule>>>(StringComparer.OrdinalIgnoreCase);
            _blockValidationEnabled = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            IsValidationEnabled = true;
        }
        
        #endregion
        
        #region Properties
        
        /// <inheritdoc />
        public bool IsValidationEnabled { get; set; }
        
        #endregion
        
        #region Events
        
        /// <inheritdoc />
        public event EventHandler<ValidationFailedEventArgs> ValidationFailed;
        
        /// <inheritdoc />
        public event EventHandler<ValidationStartingEventArgs> ValidationStarting;
        
        /// <inheritdoc />
        public event EventHandler<ValidationCompletedEventArgs> ValidationCompleted;
        
        #endregion
        
        #region Rule Registration
        
        /// <inheritdoc />
        public void RegisterRule(ValidationRule rule)
        {
            if (rule == null)
                throw new ArgumentNullException(nameof(rule));
            if (string.IsNullOrWhiteSpace(rule.RuleName))
                throw new ArgumentException("Rule name cannot be empty", nameof(rule));
            if (string.IsNullOrWhiteSpace(rule.BlockName))
                throw new ArgumentException("Block name cannot be empty", nameof(rule));
            
            // Add to name lookup
            _rulesByName[rule.RuleName] = rule;
            
            // Add to block/item lookup
            var blockRules = _rulesByBlockItem.GetOrAdd(rule.BlockName, 
                _ => new ConcurrentDictionary<string, List<ValidationRule>>(StringComparer.OrdinalIgnoreCase));
            
            var itemKey = rule.ItemName ?? "*"; // Use "*" for block-level rules
            
            lock (_lockObject)
            {
                if (!blockRules.TryGetValue(itemKey, out var itemRules))
                {
                    itemRules = new List<ValidationRule>();
                    blockRules[itemKey] = itemRules;
                }
                
                // Remove existing rule with same name if exists
                itemRules.RemoveAll(r => r.RuleName.Equals(rule.RuleName, StringComparison.OrdinalIgnoreCase));
                itemRules.Add(rule);
            }
        }
        
        /// <inheritdoc />
        public void RegisterRules(IEnumerable<ValidationRule> rules)
        {
            if (rules == null)
                throw new ArgumentNullException(nameof(rules));
            
            foreach (var rule in rules)
            {
                RegisterRule(rule);
            }
        }

        /// <inheritdoc />
        public ValidationRuleBuilder ForField(string blockName, string fieldName)
            => new ValidationRuleBuilder(this, blockName, fieldName);
        
        /// <inheritdoc />
        public bool UnregisterRule(string ruleName)
        {
            if (string.IsNullOrWhiteSpace(ruleName))
                return false;
            
            if (_rulesByName.TryRemove(ruleName, out var rule))
            {
                // Remove from block/item lookup
                if (_rulesByBlockItem.TryGetValue(rule.BlockName, out var blockRules))
                {
                    var itemKey = rule.ItemName ?? "*";
                    if (blockRules.TryGetValue(itemKey, out var itemRules))
                    {
                        lock (_lockObject)
                        {
                            itemRules.RemoveAll(r => r.RuleName.Equals(ruleName, StringComparison.OrdinalIgnoreCase));
                        }
                    }
                }
                return true;
            }
            return false;
        }
        
        /// <inheritdoc />
        public void UnregisterBlockRules(string blockName)
        {
            if (string.IsNullOrWhiteSpace(blockName))
                return;
            
            if (_rulesByBlockItem.TryRemove(blockName, out var blockRules))
            {
                // Remove all rules from name lookup
                foreach (var itemRules in blockRules.Values)
                {
                    foreach (var rule in itemRules)
                    {
                        _rulesByName.TryRemove(rule.RuleName, out _);
                    }
                }
            }
        }
        
        /// <inheritdoc />
        public void UnregisterItemRules(string blockName, string itemName)
        {
            if (string.IsNullOrWhiteSpace(blockName))
                return;
            
            if (_rulesByBlockItem.TryGetValue(blockName, out var blockRules))
            {
                var itemKey = itemName ?? "*";
                if (blockRules.TryRemove(itemKey, out var itemRules))
                {
                    foreach (var rule in itemRules)
                    {
                        _rulesByName.TryRemove(rule.RuleName, out _);
                    }
                }
            }
        }
        
        /// <inheritdoc />
        public void ClearAllRules()
        {
            _rulesByName.Clear();
            _rulesByBlockItem.Clear();
            _blockValidationEnabled.Clear();
        }
        
        #endregion
        
        #region Rule Retrieval
        
        /// <inheritdoc />
        public ValidationRule GetRule(string ruleName)
        {
            if (string.IsNullOrWhiteSpace(ruleName))
                return null;
            
            _rulesByName.TryGetValue(ruleName, out var rule);
            return rule;
        }
        
        /// <inheritdoc />
        public IReadOnlyList<ValidationRule> GetRulesForItem(string blockName, string itemName)
        {
            var result = new List<ValidationRule>();
            
            if (string.IsNullOrWhiteSpace(blockName))
                return result;
            
            if (_rulesByBlockItem.TryGetValue(blockName, out var blockRules))
            {
                var itemKey = itemName ?? "*";
                if (blockRules.TryGetValue(itemKey, out var itemRules))
                {
                    lock (_lockObject)
                    {
                        result.AddRange(itemRules);
                    }
                }
            }
            
            return result;
        }
        
        /// <inheritdoc />
        public IReadOnlyList<ValidationRule> GetRulesForBlock(string blockName)
        {
            var result = new List<ValidationRule>();
            
            if (string.IsNullOrWhiteSpace(blockName))
                return result;
            
            if (_rulesByBlockItem.TryGetValue(blockName, out var blockRules))
            {
                lock (_lockObject)
                {
                    foreach (var itemRules in blockRules.Values)
                    {
                        result.AddRange(itemRules);
                    }
                }
            }
            
            return result;
        }
        
        /// <inheritdoc />
        public IReadOnlyList<ValidationRule> GetAllRules()
        {
            return _rulesByName.Values.ToList();
        }
        
        /// <inheritdoc />
        public IReadOnlyList<ValidationRule> GetRulesByTiming(string blockName, string itemName, ValidationTiming timing)
        {
            var rules = string.IsNullOrWhiteSpace(itemName) 
                ? GetRulesForBlock(blockName) 
                : GetRulesForItem(blockName, itemName);
            
            return rules.Where(r => r.Timing == timing).ToList();
        }
        
        #endregion
        
        #region Synchronous Validation
        
        /// <inheritdoc />
        public ItemValidationResult ValidateItem(string blockName, string itemName, object value, ValidationTiming timing = ValidationTiming.Manual)
            => ValidateItemCore(blockName, itemName, value, timing, null);

        private ItemValidationResult ValidateItemCore(string blockName, string itemName, object value, ValidationTiming timing, IDictionary<string, object> record)
        {
            var result = new ItemValidationResult
            {
                BlockName = blockName,
                ItemName = itemName,
                Value = value
            };
            
            if (!IsValidationEnabled || !IsBlockValidationEnabled(blockName))
            {
                return result;
            }
            
            var rules = GetApplicableRules(blockName, itemName, timing);
            if (!rules.Any())
            {
                return result;
            }
            
            // Raise starting event
            var startingArgs = new ValidationStartingEventArgs
            {
                BlockName = blockName,
                ItemName = itemName,
                Value = value,
                Rules = rules
            };
            ValidationStarting?.Invoke(this, startingArgs);
            
            if (startingArgs.Cancel)
            {
                return result;
            }
            
            var stopwatch = Stopwatch.StartNew();
            
            foreach (var rule in rules.Where(r => r.IsEnabled))
            {
                var ruleResult = ExecuteValidation(rule, value, record);
                result.RuleResults.Add(ruleResult);
                
                if (!ruleResult.IsValid)
                {
                    var failedArgs = new ValidationFailedEventArgs
                    {
                        BlockName = blockName,
                        ItemName = itemName,
                        Value = value,
                        FailedRule = rule,
                        Result = ruleResult
                    };
                    ValidationFailed?.Invoke(this, failedArgs);
                    
                    if (failedArgs.Cancel)
                        break;
                }
            }
            
            stopwatch.Stop();
            
            // Raise completed event
            var completedArgs = new ValidationCompletedEventArgs
            {
                BlockName = blockName,
                ItemName = itemName,
                IsValid = result.IsValid,
                RulesEvaluated = result.RuleResults.Count,
                RulesFailed = result.RuleResults.Count(r => !r.IsValid),
                Duration = stopwatch.Elapsed
            };
            ValidationCompleted?.Invoke(this, completedArgs);
            
            return result;
        }

        /// <inheritdoc />
        public RecordValidationResult ValidateRecord(string blockName, IDictionary<string, object> record, ValidationTiming timing = ValidationTiming.Manual)
        {
            var result = new RecordValidationResult
            {
                BlockName = blockName
            };
            
            if (record == null || !IsValidationEnabled || !IsBlockValidationEnabled(blockName))
            {
                return result;
            }
            
            foreach (var field in record)
            {
                var itemResult = ValidateItemCore(blockName, field.Key, field.Value, timing, record);
                if (itemResult.RuleResults.Any())
                {
                    result.ItemResults[field.Key] = itemResult;
                }
            }
            
            // Validate cross-field rules (block-level rules)
            var blockLevelRules = GetApplicableRules(blockName, null, timing)
                .Where(r => r.ValidationType == ValidationType.CrossField);
            
            foreach (var rule in blockLevelRules.Where(r => r.IsEnabled))
            {
                // For cross-field validation, we pass the entire record
                var ruleResult = ExecuteCrossFieldValidation(rule, record);
                
                // Add to item result for the primary field
                if (!string.IsNullOrWhiteSpace(rule.ItemName))
                {
                    if (!result.ItemResults.TryGetValue(rule.ItemName, out var itemResult))
                    {
                        itemResult = new ItemValidationResult
                        {
                            BlockName = blockName,
                            ItemName = rule.ItemName,
                            Value = record.TryGetValue(rule.ItemName, out var val) ? val : null
                        };
                        result.ItemResults[rule.ItemName] = itemResult;
                    }
                    itemResult.RuleResults.Add(ruleResult);
                }
            }
            
            return result;
        }
        
        /// <inheritdoc />
        public BlockValidationResult ValidateBlock(string blockName, IEnumerable<IDictionary<string, object>> records, ValidationTiming timing = ValidationTiming.Manual)
        {
            var result = new BlockValidationResult
            {
                BlockName = blockName
            };
            
            if (records == null)
                return result;
            
            foreach (var record in records)
            {
                var recordResult = ValidateRecord(blockName, record, timing);
                result.RecordResults.Add(recordResult);
            }
            
            return result;
        }
        
        /// <inheritdoc />
        public FormValidationResult ValidateForm(IDictionary<string, IEnumerable<IDictionary<string, object>>> formData, ValidationTiming timing = ValidationTiming.Manual)
        {
            var result = new FormValidationResult();
            
            if (formData == null)
                return result;
            
            foreach (var block in formData)
            {
                var blockResult = ValidateBlock(block.Key, block.Value, timing);
                result.BlockResults[block.Key] = blockResult;
            }
            
            return result;
        }
        
        #endregion
        
        #region Asynchronous Validation
        
        /// <inheritdoc />
        public async Task<ItemValidationResult> ValidateItemAsync(string blockName, string itemName, object value, ValidationTiming timing = ValidationTiming.Manual, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => ValidateItem(blockName, itemName, value, timing), cancellationToken);
        }
        
        /// <inheritdoc />
        public async Task<RecordValidationResult> ValidateRecordAsync(string blockName, IDictionary<string, object> record, ValidationTiming timing = ValidationTiming.Manual, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => ValidateRecord(blockName, record, timing), cancellationToken);
        }
        
        /// <inheritdoc />
        public async Task<BlockValidationResult> ValidateBlockAsync(string blockName, IEnumerable<IDictionary<string, object>> records, ValidationTiming timing = ValidationTiming.Manual, CancellationToken cancellationToken = default)
        {
            var result = new BlockValidationResult
            {
                BlockName = blockName
            };
            
            if (records == null)
                return result;
            
            var recordList = records.ToList();
            var tasks = recordList.Select(record => 
                ValidateRecordAsync(blockName, record, timing, cancellationToken));
            
            var recordResults = await Task.WhenAll(tasks);
            result.RecordResults.AddRange(recordResults);
            
            return result;
        }
        
        /// <inheritdoc />
        public async Task<FormValidationResult> ValidateFormAsync(IDictionary<string, IEnumerable<IDictionary<string, object>>> formData, ValidationTiming timing = ValidationTiming.Manual, CancellationToken cancellationToken = default)
        {
            var result = new FormValidationResult();
            
            if (formData == null)
                return result;
            
            var tasks = formData.Select(async block =>
            {
                var blockResult = await ValidateBlockAsync(block.Key, block.Value, timing, cancellationToken);
                return (block.Key, blockResult);
            });
            
            var blockResults = await Task.WhenAll(tasks);
            
            foreach (var (blockName, blockResult) in blockResults)
            {
                result.BlockResults[blockName] = blockResult;
            }
            
            return result;
        }
        
        #endregion
        
        #region Validation Context
        
        /// <inheritdoc />
        public void SetDataSource(IDataSource dataSource)
        {
            _dataSource = dataSource;
        }
        
        /// <inheritdoc />
        public void SetRuleEnabled(string ruleName, bool enabled)
        {
            if (_rulesByName.TryGetValue(ruleName, out var rule))
            {
                rule.IsEnabled = enabled;
            }
        }
        
        /// <inheritdoc />
        public void SetBlockValidationEnabled(string blockName, bool enabled)
        {
            _blockValidationEnabled[blockName] = enabled;
        }
        
        #endregion
        
        #region Private Methods
        
        private bool IsBlockValidationEnabled(string blockName)
        {
            if (_blockValidationEnabled.TryGetValue(blockName, out var enabled))
            {
                return enabled;
            }
            return true; // Enabled by default
        }
        
        private IReadOnlyList<ValidationRule> GetApplicableRules(string blockName, string itemName, ValidationTiming timing)
        {
            var rules = new List<ValidationRule>();
            
            // Get item-specific rules
            if (!string.IsNullOrWhiteSpace(itemName))
            {
                rules.AddRange(GetRulesForItem(blockName, itemName));
            }
            
            // Get block-level rules (itemName is null or "*")
            rules.AddRange(GetRulesForItem(blockName, null));
            rules.AddRange(GetRulesForItem(blockName, "*"));
            
            // Filter by timing (Manual timing matches all)
            if (timing != ValidationTiming.Manual)
            {
                rules = rules.Where(r => r.Timing == timing || r.Timing == ValidationTiming.Manual).ToList();
            }
            
            return rules.Distinct().ToList();
        }
        
        private ValidationRuleResult ExecuteValidation(ValidationRule rule, object value, IDictionary<string, object> record)
        {
            var result = new ValidationRuleResult
            {
                RuleName = rule.RuleName,
                Severity = rule.Severity
            };
            
            try
            {
                // Check condition first
                if (rule.ConditionFunction != null)
                {
                    bool conditionMet = record != null 
                        ? rule.ConditionFunction(value, record) 
                        : rule.ConditionFunction(value, new Dictionary<string, object> { { rule.ItemName ?? "value", value } });
                    
                    if (!conditionMet)
                    {
                        result.IsValid = true;
                        return result;
                    }
                }
                
                result.IsValid = ValidateByType(rule, value, record);
                
                if (!result.IsValid)
                {
                    result.ErrorMessage = rule.GetFormattedMessage(rule.ItemName, value);
                }
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ErrorMessage = $"Validation error: {ex.Message}";
                result.Exception = ex;
            }
            
            return result;
        }
        
        private ValidationRuleResult ExecuteCrossFieldValidation(ValidationRule rule, IDictionary<string, object> record)
        {
            var result = new ValidationRuleResult
            {
                RuleName = rule.RuleName,
                Severity = rule.Severity
            };
            
            try
            {
                if (rule.CustomValidator != null)
                {
                    var validationResult = rule.CustomValidator(null, record, record as Dictionary<string, object> ?? new Dictionary<string, object>(record)).GetAwaiter().GetResult();
                    result.IsValid = validationResult.isValid;
                    if (!result.IsValid && !string.IsNullOrEmpty(validationResult.errorMessage))
                        result.ErrorMessage = validationResult.errorMessage;
                }
                else if (!string.IsNullOrWhiteSpace(rule.CompareFieldName))
                {
                    // Compare two fields
                    object value1 = record.TryGetValue(rule.ItemName, out var v1) ? v1 : null;
                    object value2 = record.TryGetValue(rule.CompareFieldName, out var v2) ? v2 : null;
                    
                    result.IsValid = CompareValues(value1, value2, rule.ValidationType);
                }
                else
                {
                    result.IsValid = true;
                }
                
                if (!result.IsValid && string.IsNullOrEmpty(result.ErrorMessage))
                {
                    result.ErrorMessage = rule.GetFormattedMessage(rule.ItemName, null);
                }
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ErrorMessage = $"Cross-field validation error: {ex.Message}";
                result.Exception = ex;
            }
            
            return result;
        }
        
        private bool ValidateByType(ValidationRule rule, object value, IDictionary<string, object> record)
        {
            switch (rule.ValidationType)
            {
                case ValidationType.Required:
                    return ValidateRequired(value);
                    
                case ValidationType.Range:
                    return ValidateRange(value, rule.MinValue, rule.MaxValue);
                    
                case ValidationType.Pattern:
                    return ValidatePattern(value, rule.Pattern);
                    
                case ValidationType.MaxLength:
                    return ValidateMaxLength(value, rule.MaxValue);
                    
                case ValidationType.MinLength:
                    return ValidateMinLength(value, rule.MinValue);
                    
                case ValidationType.Email:
                    return ValidateEmail(value);
                    
                case ValidationType.Url:
                    return ValidateUrl(value);
                    
                case ValidationType.Date:
                    return ValidateDate(value);
                    
                case ValidationType.Numeric:
                    return ValidateNumeric(value);
                    
                case ValidationType.GreaterThan:
                    return ValidateGreaterThan(value, rule.MinValue);
                    
                case ValidationType.LessThan:
                    return ValidateLessThan(value, rule.MaxValue);
                    
                case ValidationType.EqualTo:
                    return ValidateEqualTo(value, rule.MinValue ?? rule.MaxValue);
                    
                case ValidationType.Lookup:
                    return ValidateLookup(value, rule, record);
                    
                case ValidationType.Unique:
                    return ValidateUnique(value, rule, record);
                    
                case ValidationType.Custom:
                    return ValidateCustom(value, rule);
                    
                case ValidationType.Database:
                    return ValidateDatabase(value, rule, record);
                    
                default:
                    return true;
            }
        }
        
        private bool ValidateRequired(object value)
        {
            if (value == null)
                return false;
            if (value is string str && string.IsNullOrWhiteSpace(str))
                return false;
            return true;
        }
        
        private bool ValidateRange(object value, object minValue, object maxValue)
        {
            if (value == null)
                return true; // Use Required for null check
            
            try
            {
                var comparableValue = Convert.ToDouble(value);
                var min = minValue != null ? Convert.ToDouble(minValue) : double.MinValue;
                var max = maxValue != null ? Convert.ToDouble(maxValue) : double.MaxValue;
                
                return comparableValue >= min && comparableValue <= max;
            }
            catch
            {
                return false;
            }
        }
        
        private bool ValidatePattern(object value, string pattern)
        {
            if (value == null || string.IsNullOrWhiteSpace(pattern))
                return true;
            
            var strValue = value.ToString();
            return Regex.IsMatch(strValue, pattern, RegexOptions.IgnoreCase);
        }
        
        private bool ValidateMaxLength(object value, object maxLength)
        {
            if (value == null)
                return true;
            
            var strValue = value.ToString();
            var max = Convert.ToInt32(maxLength ?? int.MaxValue);
            return strValue.Length <= max;
        }
        
        private bool ValidateMinLength(object value, object minLength)
        {
            if (value == null)
                return true;
            
            var strValue = value.ToString();
            var min = Convert.ToInt32(minLength ?? 0);
            return strValue.Length >= min;
        }
        
        private bool ValidateEmail(object value)
        {
            if (value == null)
                return true;
            
            var strValue = value.ToString();
            if (string.IsNullOrWhiteSpace(strValue))
                return true;
            
            // Simple email pattern
            var emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(strValue, emailPattern);
        }
        
        private bool ValidateUrl(object value)
        {
            if (value == null)
                return true;
            
            var strValue = value.ToString();
            if (string.IsNullOrWhiteSpace(strValue))
                return true;
            
            return Uri.TryCreate(strValue, UriKind.Absolute, out var uri) &&
                   (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }
        
        private bool ValidateDate(object value)
        {
            if (value == null)
                return true;
            
            if (value is DateTime)
                return true;
            
            return DateTime.TryParse(value.ToString(), out _);
        }
        
        private bool ValidateNumeric(object value)
        {
            if (value == null)
                return true;
            
            if (value is int || value is long || value is float || value is double || value is decimal)
                return true;
            
            return double.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out _);
        }
        
        private bool ValidateGreaterThan(object value, object compareValue)
        {
            if (value == null || compareValue == null)
                return true;
            
            try
            {
                var val = Convert.ToDouble(value);
                var compare = Convert.ToDouble(compareValue);
                return val > compare;
            }
            catch
            {
                return false;
            }
        }
        
        private bool ValidateLessThan(object value, object compareValue)
        {
            if (value == null || compareValue == null)
                return true;
            
            try
            {
                var val = Convert.ToDouble(value);
                var compare = Convert.ToDouble(compareValue);
                return val < compare;
            }
            catch
            {
                return false;
            }
        }
        
        private bool ValidateEqualTo(object value, object compareValue)
        {
            if (value == null && compareValue == null)
                return true;
            if (value == null || compareValue == null)
                return false;
            
            return value.Equals(compareValue);
        }
        
        private bool ValidateLookup(object value, ValidationRule rule, IDictionary<string, object> record)
        {
            if (value == null)
                return true;
            
            if (_dataSource == null)
                return true;

            if (!TryResolveValidationTarget(rule, rule.ItemName, out var entityName, out var fieldName))
                return true;

            if (!TryQueryRows(entityName, CreateEqualityFilters(fieldName, value), out var rows))
                return true;

            return rows.Count > 0;
        }
        
        private bool ValidateUnique(object value, ValidationRule rule, IDictionary<string, object> record)
        {
            if (value == null)
                return true;
            
            if (_dataSource == null)
                return true;

            var entityName = !string.IsNullOrWhiteSpace(rule.LookupSource)
                ? ResolveEntityName(rule)
                : rule.BlockName;
            var fieldName = rule.ItemName;

            if (string.IsNullOrWhiteSpace(entityName) || string.IsNullOrWhiteSpace(fieldName))
                return true;

            if (!TryQueryRows(entityName, CreateEqualityFilters(fieldName, value), out var rows))
                return true;

            if (rows.Count == 0)
                return true;

            var identityFields = GetCompareFieldNames(rule, record);
            if (identityFields.Count == 0)
                return false;

            return rows.All(row => RecordMatchesIdentityFields(row, identityFields) );
        }
        
        private bool ValidateCustom(object value, ValidationRule rule)
        {
            if (rule.CustomValidator == null)
                return true;
            
            try
            {
                var record = new Dictionary<string, object>
                {
                    { rule.ItemName ?? "value", value }
                };
                var result = rule.CustomValidator(value, null, record).GetAwaiter().GetResult();
                return result.isValid;
            }
            catch
            {
                return false;
            }
        }
        
        private bool ValidateDatabase(object value, ValidationRule rule, IDictionary<string, object> record)
        {
            if (value == null)
                return true;
            
            if (_dataSource == null)
                return true;

            return ValidateLookup(value, rule, record);
        }

        private bool TryResolveValidationTarget(ValidationRule rule, string defaultFieldName, out string entityName, out string fieldName)
        {
            entityName = null;
            fieldName = null;

            var source = rule.LookupSource?.Trim();
            var fallbackField = !string.IsNullOrWhiteSpace(rule.CompareFieldName)
                ? rule.CompareFieldName.Trim()
                : defaultFieldName?.Trim();

            if (string.IsNullOrWhiteSpace(source))
            {
                entityName = rule.BlockName;
                fieldName = fallbackField;
                return !string.IsNullOrWhiteSpace(entityName) && !string.IsNullOrWhiteSpace(fieldName);
            }

            var separatorIndex = source.IndexOfAny(new[] { '|', ':' });
            if (separatorIndex > 0 && separatorIndex < source.Length - 1)
            {
                entityName = source.Substring(0, separatorIndex).Trim();
                fieldName = source.Substring(separatorIndex + 1).Trim();
                return !string.IsNullOrWhiteSpace(entityName) && !string.IsNullOrWhiteSpace(fieldName);
            }

            var lastDotIndex = source.LastIndexOf('.');
            if (lastDotIndex > 0 && lastDotIndex < source.Length - 1)
            {
                var possibleFieldName = source.Substring(lastDotIndex + 1).Trim();
                if (string.Equals(possibleFieldName, fallbackField, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(possibleFieldName, rule.ItemName, StringComparison.OrdinalIgnoreCase))
                {
                    entityName = source.Substring(0, lastDotIndex).Trim();
                    fieldName = possibleFieldName;
                    return !string.IsNullOrWhiteSpace(entityName) && !string.IsNullOrWhiteSpace(fieldName);
                }
            }

            entityName = source;
            fieldName = fallbackField;
            return !string.IsNullOrWhiteSpace(entityName) && !string.IsNullOrWhiteSpace(fieldName);
        }

        private string ResolveEntityName(ValidationRule rule)
        {
            if (TryResolveValidationTarget(rule, rule.ItemName, out var entityName, out _))
                return entityName;

            return rule.BlockName;
        }

        private static List<AppFilter> CreateEqualityFilters(string fieldName, object value)
        {
            return new List<AppFilter>
            {
                new AppFilter
                {
                    FieldName = fieldName,
                    Operator = "=",
                    FilterValue = value?.ToString(),
                    FieldType = value?.GetType(),
                    valueType = value?.GetType().FullName
                }
            };
        }

        private bool TryQueryRows(string entityName, IEnumerable<AppFilter> filters, out List<object> rows)
        {
            rows = new List<object>();

            if (_dataSource == null || string.IsNullOrWhiteSpace(entityName))
                return false;

            try
            {
                var data = _dataSource.GetEntity(entityName, filters?.ToList() ?? new List<AppFilter>());
                rows = MaterializeRows(data);
                return true;
            }
            catch
            {
                try
                {
                    var data = _dataSource.GetEntityAsync(entityName, filters?.ToList() ?? new List<AppFilter>()).GetAwaiter().GetResult();
                    rows = MaterializeRows(data);
                    return true;
                }
                catch
                {
                    rows = new List<object>();
                    return false;
                }
            }
        }

        private static List<object> MaterializeRows(object data)
        {
            if (data == null)
                return new List<object>();

            if (data is DataTable dataTable)
                return dataTable.Rows.Cast<DataRow>().Select(row => (object)row).ToList();

            if (data is IEnumerable<object> objectEnumerable)
                return objectEnumerable.ToList();

            if (data is IEnumerable enumerable && data is not string)
            {
                var rows = new List<object>();
                foreach (var item in enumerable)
                {
                    rows.Add(item);
                }
                return rows;
            }

            return new List<object> { data };
        }

        private static Dictionary<string, object> GetCompareFieldNames(ValidationRule rule, IDictionary<string, object> record)
        {
            var identityFields = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (record == null || string.IsNullOrWhiteSpace(rule.CompareFieldName))
                return identityFields;

            var fieldNames = rule.CompareFieldName
                .Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var fieldName in fieldNames)
            {
                if (record.TryGetValue(fieldName, out var value) && value != null)
                {
                    identityFields[fieldName] = value;
                }
            }

            return identityFields;
        }

        private static bool RecordMatchesIdentityFields(object row, IReadOnlyDictionary<string, object> identityFields)
        {
            foreach (var identityField in identityFields)
            {
                if (!TryGetFieldValue(row, identityField.Key, out var rowValue) || !AreEquivalentValues(rowValue, identityField.Value))
                    return false;
            }

            return true;
        }

        private static bool TryGetFieldValue(object source, string fieldName, out object value)
        {
            value = null;
            if (source == null || string.IsNullOrWhiteSpace(fieldName))
                return false;

            if (source is IDictionary<string, object> dict)
                return dict.TryGetValue(fieldName, out value);

            if (source is DataRow dataRow)
            {
                if (!dataRow.Table.Columns.Contains(fieldName))
                    return false;

                value = dataRow[fieldName];
                return true;
            }

            var property = source.GetType().GetProperty(fieldName,
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.IgnoreCase);

            if (property == null)
                return false;

            value = property.GetValue(source);
            return true;
        }

        private static bool AreEquivalentValues(object left, object right)
        {
            if (left == DBNull.Value)
                left = null;
            if (right == DBNull.Value)
                right = null;

            if (left == null && right == null)
                return true;
            if (left == null || right == null)
                return false;

            if (left.Equals(right))
                return true;

            return string.Equals(left.ToString(), right.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        
        private bool CompareValues(object value1, object value2, ValidationType comparisonType)
        {
            if (value1 == null && value2 == null)
                return true;
            if (value1 == null || value2 == null)
                return false;
            
            try
            {
                var double1 = Convert.ToDouble(value1);
                var double2 = Convert.ToDouble(value2);
                
                switch (comparisonType)
                {
                    case ValidationType.GreaterThan:
                        return double1 > double2;
                    case ValidationType.LessThan:
                        return double1 < double2;
                    case ValidationType.EqualTo:
                        return Math.Abs(double1 - double2) < 0.0001;
                    default:
                        return value1.Equals(value2);
                }
            }
            catch
            {
                return value1.Equals(value2);
            }
        }
        
        #endregion
    }
}
