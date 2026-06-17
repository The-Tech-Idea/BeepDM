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

            // B3 (audit pass 3, 2026-06): TryRemove returns false when the
            // block was already unregistered. The previous code ignored the
            // return value and unconditionally iterated `blockRules.Values`
            // — but when the block is missing, `blockRules` is the default
            // (null) reference, and `blockRules.Values` throws NRE. A
            // double-unregister is a realistic scenario (host tears down a
            // block, then the form manager's teardown also runs) so this
            // is not a defensive-only fix.
            if (!_rulesByBlockItem.TryRemove(blockName, out var blockRules) || blockRules == null)
                return;

            // Remove all rules from name lookup
            foreach (var itemRules in blockRules.Values)
            {
                foreach (var rule in itemRules)
                {
                    _rulesByName.TryRemove(rule.RuleName, out _);
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
                // B4 (audit pass 3, 2026-06): TryRemove returns false when
                // the item was already unregistered. The previous code
                // iterated `itemRules` even when it was null, throwing NRE.
                // Same scenario as B3.
                if (!blockRules.TryRemove(itemKey, out var itemRules) || itemRules == null)
                    return;

                foreach (var rule in itemRules)
                {
                    _rulesByName.TryRemove(rule.RuleName, out _);
                }
            }
        }
        
        /// <inheritdoc />
        public void ClearAllRules()
        {
            // B5 (audit pass 3, 2026-06): serialize the three Clear() calls
            // under _lockObject. A concurrent RegisterRule could land an
            // entry in _rulesByName after its Clear() and in
            // _rulesByBlockItem before its Clear(), leaving an orphan in
            // _rulesByBlockItem that _rulesByName no longer knows about.
            // Same fix pattern as TriggerManager.ClearAllTriggers.
            lock (_lockObject)
            {
                _rulesByName.Clear();
                _rulesByBlockItem.Clear();
                _blockValidationEnabled.Clear();
            }
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

            // B11 (audit pass 3, 2026-06): track the record index
            // and set it on each RecordValidationResult. The
            // previous version did not, leaving RecordIndex at 0
            // for every result. Also stamp the record dictionary
            // onto the Record property so the caller can correlate
            // the result with the input (the public API only sees
            // the dictionary, so Record is the dictionary, not the
            // original record object).
            int index = 0;
            foreach (var record in records)
            {
                var recordResult = ValidateRecord(blockName, record, timing);
                recordResult.RecordIndex = index;
                recordResult.Record = record;
                result.RecordResults.Add(recordResult);
                index++;
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

        /// <summary>
        /// Async wrapper around <see cref="ValidateItem"/>. The work is
        /// synchronous; this method offloads it to the thread pool.
        /// </summary>
        /// <remarks>
        /// B10 (audit pass 3, 2026-06): the previous "async" path
        /// wrapped the sync <c>ValidateItem</c> in
        /// <c>Task.Run</c>. This is sync-over-async with extra
        /// steps — the work itself is not parallel, just
        /// off-thread. True parallel validation would require
        /// per-rule async execution, which is out of scope for
        /// this pass. The benefit of this wrapper is only that
        /// the caller can <c>await</c> without blocking the UI
        /// thread, which is a real (if narrow) win.
        /// </remarks>
        public async Task<ItemValidationResult> ValidateItemAsync(string blockName, string itemName, object value, ValidationTiming timing = ValidationTiming.Manual, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => ValidateItem(blockName, itemName, value, timing), cancellationToken);
        }

        /// <summary>
        /// Async wrapper around <see cref="ValidateRecord"/>. The
        /// work is synchronous; this method offloads it to the
        /// thread pool. See <see cref="ValidateItemAsync"/> for the
        /// sync-over-async caveat.
        /// </summary>
        public async Task<RecordValidationResult> ValidateRecordAsync(string blockName, IDictionary<string, object> record, ValidationTiming timing = ValidationTiming.Manual, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => ValidateRecord(blockName, record, timing), cancellationToken);
        }

        /// <summary>
        /// Validates a block of records in parallel. The per-record
        /// validation is itself synchronous; this method fans out
        /// the records to the thread pool via
        /// <see cref="ValidateRecordAsync"/>.
        /// </summary>
        /// <remarks>
        /// B10: the parallelism here is real (records are
        /// validated concurrently on the thread pool), but the
        /// per-record work is still sync-over-async on any
        /// async resources it touches. This is useful when
        /// records are I/O-light and CPU-bound, but a wasted
        /// fan-out for records that are themselves I/O-bound
        /// (e.g. ValidateLookup / ValidateUnique hit a
        /// database). For I/O-heavy records, prefer the sync
        /// entry point <see cref="ValidateBlock"/> in a
        /// background thread, or pre-load the data before
        /// calling.
        /// </remarks>
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

        /// <summary>
        /// Validates a form (collection of blocks) in parallel. The
        /// per-block validation runs concurrently. See
        /// <see cref="ValidateBlockAsync"/> for the
        /// sync-over-async caveat.
        /// </summary>
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

            // Get block-level rules. RegisterRule stores them under the
            // key "*" (via `rule.ItemName ?? "*"`), so the single call
            // below retrieves them. The previous version called this
            // twice (once with null, once with "*") but both keys
            // resolve to the same underlying list, making the second
            // call a no-op that was subsequently de-duped by
            // Distinct(). Removed the duplicate (B7, audit pass 3,
            // 2026-06).
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
                    // B31 (audit pass 3, 2026-06): if a validator set a
                    // diagnostic (e.g. ValidateCustom on a UI thread),
                    // use it as the error message instead of the
                    // rule's default. The diagnostic explains WHY the
                    // validator returned false — without it, a
                    // false-return on a UI thread looks identical to
                    // "the field is actually invalid" and the user
                    // gets a confusing "field is invalid" error.
                    result.ErrorMessage = !string.IsNullOrEmpty(_lastValidationDiagnostic)
                        ? _lastValidationDiagnostic
                        : rule.GetFormattedMessage(rule.ItemName, value);
                    _lastValidationDiagnostic = null;
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
                    // B3 (audit pass 3, 2026-06): avoid
                    // .GetAwaiter().GetResult() deadlock on UI
                    // threads. The custom validator signature
                    // returns Task<...>, so we have no sync
                    // alternative here. Instead, detect a captured
                    // SynchronizationContext and treat a deadlock
                    // as "validation not yet runnable from this
                    // thread"; the caller can retry via
                    // ValidateFormAsync if they need async
                    // semantics.
                    if (SynchronizationContext.Current != null)
                    {
                        result.IsValid = false;
                        result.ErrorMessage =
                            "Custom cross-field validator is async but " +
                            "ExecuteCrossFieldValidation is sync. Call " +
                            "ValidateFormAsync to run async validators.";
                        return result;
                    }
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
                // B26 (audit pass 3, 2026-06): use double.NegativeInfinity /
                // double.PositiveInfinity for "no lower / no upper bound".
                // The previous code used double.MinValue / double.MaxValue,
                // which works today (every double is in [MinValue, MaxValue])
                // but is the wrong constant — double.MinValue is the most
                // NEGATIVE double, not the smallest. A future maintainer
                // reading "double.MinValue" and asking "wait, what if I have
                // a subnormal value?" would have a hard time convincing
                // themselves the range is correctly open at the bottom.
                var min = minValue != null ? Convert.ToDouble(minValue) : double.NegativeInfinity;
                var max = maxValue != null ? Convert.ToDouble(maxValue) : double.PositiveInfinity;

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

            // B29 (audit pass 3, 2026-06): lookups are informational, not
            // security-critical. Keep the fail-open behavior — if the DB
            // is down, we let the value pass so the user can keep
            // working, with a Debug.WriteLine to surface the issue to
            // operators. This is a deliberate departure from the
            // ValidateUnique behavior (B30), which fails closed.
            if (TryQueryRowsEx(entityName, CreateEqualityFilters(fieldName, value), out var rows) == QueryOutcome.Error)
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

            // B30 (audit pass 3, 2026-06): uniqueness is a security-
            // critical check. The previous version returned true
            // (validation passed) on DB error, which silently allowed
            // duplicates when the database was unavailable. The fix
            // fails CLOSED: if the query errored, treat the value as
            // not-unique. The caller will see the validation fail and
            // the user can retry once the DB is back. This is a
            // behavior change from "silently allow duplicate" to
            // "block the save when DB is unavailable" — intentional.
            var outcome = TryQueryRowsEx(entityName, CreateEqualityFilters(fieldName, value), out var rows);
            if (outcome == QueryOutcome.Error)
                return false;
            if (outcome == QueryOutcome.Empty)
                return true;

            // outcome == Ok and rows.Count > 0: check identity fields
            var identityFields = GetCompareFieldNames(rule, record);
            if (identityFields.Count == 0)
                return false;

            return rows.All(row => RecordMatchesIdentityFields(row, identityFields) );
        }
        
        // B31 (audit pass 3, 2026-06): per-call diagnostic that the
        // dispatcher can pick up after a false return. Without this
        // side-channel, ValidateCustom returning false on a UI thread
        // looks identical to "the field is actually invalid" — the
        // user sees the same error message and can't tell that the
        // engine refused to run the validator. ExecuteValidation reads
        // this after ValidateByType and overrides ErrorMessage if it
        // is set. The value is intentionally per-instance (a field,
        // not a static) so concurrent validations don't clobber each
        // other — the lock in ValidateItemCore ensures
        // ValidateByType and the read in ExecuteValidation run on the
        // same thread.
        private string _lastValidationDiagnostic;

        private bool ValidateCustom(object value, ValidationRule rule)
        {
            if (rule.CustomValidator == null)
                return true;

            // B3 (audit pass 3, 2026-06): if we're on a UI thread
            // (captured SynchronizationContext), refuse to block on
            // the async validator. The caller can retry via
            // ValidateItemAsync for true async semantics.
            if (SynchronizationContext.Current != null)
            {
                _lastValidationDiagnostic =
                    "Custom validator was not executed because the form is on a UI thread. " +
                    "Use ValidateItemAsync (or the Form's async validate path) to run async validators.";
                return false;
            }

            try
            {
                _lastValidationDiagnostic = null;
                var record = new Dictionary<string, object>
                {
                    { rule.ItemName ?? "value", value }
                };

                Task<ValidationResult> task;
                if (SynchronizationContext.Current != null)
                    task = Task.Run(() => rule.CustomValidator(value, null, record));
                else
                    task = rule.CustomValidator(value, null, record);
                var result = task.GetAwaiter().GetResult();

                return result?.isValid ?? false;
            }
            catch (Exception ex)
            {
                _lastValidationDiagnostic = $"Custom validator threw: {ex.Message}";
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

        /// <summary>
        /// Outcome of a single validation query against the data source.
        /// Lets the caller distinguish "no rows found" (Empty, a real
        /// result) from "the DB call threw" (Error, an undefined
        /// result). The previous bool out-parameter collapsed both
        /// into a single false, which forced callers like
        /// <see cref="ValidateUnique"/> to silently pass on DB error —
        /// a security bypass for a uniqueness check.
        /// </summary>
        private enum QueryOutcome
        {
            /// <summary>Query ran successfully (rows is populated, possibly empty).</summary>
            Ok,
            /// <summary>Query ran successfully and returned zero rows.</summary>
            Empty,
            /// <summary>Data source is null, entity name is empty, or the query threw.</summary>
            Error
        }

        private bool TryQueryRows(string entityName, IEnumerable<AppFilter> filters, out List<object> rows)
        {
            // Back-compat shim: collapse the tri-state outcome into a bool.
            // Existing callers that just want "did the query return
            // anything, including empty" get true for Ok+Empty. Callers
            // that need to distinguish Error from Empty use
            /// <see cref="TryQueryRowsEx"/> directly.
            var outcome = TryQueryRowsEx(entityName, filters, out rows);
            return outcome != QueryOutcome.Error;
        }

        private QueryOutcome TryQueryRowsEx(string entityName, IEnumerable<AppFilter> filters, out List<object> rows)
        {
            rows = new List<object>();

            if (_dataSource == null || string.IsNullOrWhiteSpace(entityName))
                return QueryOutcome.Error;

            // B3 (audit pass 3, 2026-06): sync-first, async-only-on-failure.
            // The previous version did sync-first, but the fallback to
            // async used .GetAwaiter().GetResult() which is the
            // classic sync-over-async deadlock pattern when called
            // from a UI thread (the FormsManager runs on a UI thread
            // per form). The deadlock happens when the awaited task
            // tries to resume on the captured SynchronizationContext
            // and the context is blocked waiting for the task to
            // complete.
            //
            // Fix: keep the sync path as the primary call (it's the
            // path most data sources support), and if it throws,
            // surface Error rather than block. The async API is still
            // available via ValidateBlockAsync / ValidateFormAsync.
            try
            {
                var data = _dataSource.GetEntity(entityName, filters?.ToList() ?? new List<AppFilter>());
                rows = MaterializeRows(data);
                return rows.Count == 0 ? QueryOutcome.Empty : QueryOutcome.Ok;
            }
            catch (Exception ex)
            {
                // Sync path failed. Distinguish from "no rows" so
                // security-critical callers (ValidateUnique) can fail
                // closed. The previous implementation silently treated
                // this as a pass — see B29/B30.
                Debug.WriteLine(
                    $"[ValidationManager] TryQueryRows failed for entity '{entityName}': {ex.Message}");
                return QueryOutcome.Error;
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

            // B6 (audit pass 3, 2026-06): the previous version silently
            // dropped fields whose value was null in the record. That
            // could cause a "match any single field" false negative on
            // a unique-constraint check (e.g. a composite key with
            // (OrderId=1, LineNumber=null) would have its LineNumber
            // dropped, then the check would compare OrderId only,
            // finding a false match and rejecting the save).
            //
            // New behavior: include the field with a null value in
            // the identity set. The downstream RecordMatchesIdentityFields
            // already handles null values (AreEquivalentValues treats
            // null == null as true), so this is consistent.
            foreach (var fieldName in fieldNames)
            {
                if (record.TryGetValue(fieldName, out var value))
                {
                    identityFields[fieldName] = value; // value may be null
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

            // B5 (audit pass 3, 2026-06): use RecordPropertyAccessor
            // instead of the inline Type.GetProperty reflection. The
            // accessor provides a process-wide PropertyInfo cache
            // (so repeated reads of the same field on rows of the
            // same type are O(1)) and emits a throttled diagnostic
            // on miss, which previously was a silent null return.
            //
            // Note: this method is static and doesn't have access to
            // _dmeEditor for the diagnostic, so we pass null. The
            // diagnostic is still useful (Debug.WriteLine fires) and
            // the caller can correlate via the log type.
            return RecordPropertyAccessor.TryGetValue(source, fieldName, out value);
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
