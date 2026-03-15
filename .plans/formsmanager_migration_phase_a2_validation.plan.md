# Phase A2: Validation Manager Migration

## Overview
Migrate the validation rule engine from `BeepDataBlock` to `FormsManager` in BeepDM.

## Current State

### Source Files (Beep.Winform)
- `TheTechIdea.Beep.Winform.Controls.Integrated/DataBlocks/BeepDataBlock.Validation.cs`
- `TheTechIdea.Beep.Winform.Controls.Integrated/DataBlocks/Models/ValidationRule.cs`
- `TheTechIdea.Beep.Winform.Controls.Integrated/DataBlocks/Helpers/ValidationRuleHelpers.cs`

### Features to Migrate

#### ValidationType Enum
| Value | Description |
|-------|-------------|
| `Required` | Required field validation |
| `Format` | Pattern/regex validation |
| `Range` | Min/max numeric validation |
| `Length` | String length validation |
| `CrossField` | Cross-field validation |
| `BusinessRule` | Custom business rule |
| `Lookup` | Database lookup validation |
| `Expression` | Expression-based validation |
| `Computed` | Computed field validation |

#### ValidationRule Properties
| Property | Type | Description |
|----------|------|-------------|
| `RuleName` | string | Unique rule identifier |
| `Description` | string | Rule description |
| `FieldName` | string | Target field (null = record-level) |
| `ValidationType` | ValidationType | Type of validation |
| `ErrorMessage` | string | Error message on failure |
| `WarningMessage` | string | Warning (doesn't block) |
| `ExecutionOrder` | int | Execution priority |
| `IsEnabled` | bool | Whether rule is active |
| `ValidationFunction` | Func<> | Custom validation logic |
| `ValidationExpression` | string | Expression for serialization |
| `IsRequired` | bool | Required field flag |
| `MinLength` | int? | Minimum string length |
| `MaxLength` | int? | Maximum string length |
| `MinValue` | object | Minimum value (IComparable) |
| `MaxValue` | object | Maximum value (IComparable) |
| `Pattern` | string | Regex pattern |
| `ValidValues` | List<object> | Whitelist values |
| `InvalidValues` | List<object> | Blacklist values |
| `DependentFields` | List<string> | Cross-field dependencies |
| `ConditionalExpression` | string | Conditional validation |
| `ComputationExpression` | string | Computed validation |

#### ValidationContext Properties
| Property | Type | Description |
|----------|------|-------------|
| `BlockName` | string | Block being validated |
| `RecordValues` | Dictionary | Current record field values |
| `FieldName` | string | Field being validated |
| `OldValue` | object | Previous value |
| `NewValue` | object | Value being validated |
| `IsNewRecord` | bool | Whether new record |
| `Mode` | BlockMode | Current block mode |
| `CustomData` | Dictionary | Custom validation data |

#### Methods to Migrate
| Method | Description |
|--------|-------------|
| `RegisterValidationRule()` | Add rule for field |
| `RegisterRecordValidationRule()` | Add record-level rule |
| `UnregisterValidationRules()` | Remove rules for field |
| `GetValidationRules()` | Get rules for field |
| `ValidateField()` | Validate single field |
| `ValidateCurrentRecord()` | Validate entire record |
| `ClearValidationErrors()` | Clear all errors |
| `GetFieldsWithErrors()` | Get fields with errors |
| `ForField()` | Fluent API entry point |

---

## Target Files (BeepDM)

### File 1: ValidationType Enum
**Path**: `DataManagementModelsStandard/Editor/UOWManager/Models/ValidationType.cs`

```csharp
namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Validation type enumeration for Oracle Forms-compatible validation
    /// </summary>
    public enum ValidationType
    {
        /// <summary>Required field validation</summary>
        Required,
        
        /// <summary>Format/pattern validation</summary>
        Format,
        
        /// <summary>Range validation (min/max)</summary>
        Range,
        
        /// <summary>Length validation (strings)</summary>
        Length,
        
        /// <summary>Cross-field validation</summary>
        CrossField,
        
        /// <summary>Custom business rule</summary>
        BusinessRule,
        
        /// <summary>Database lookup validation</summary>
        Lookup,
        
        /// <summary>Expression-based validation</summary>
        Expression,
        
        /// <summary>Computed field validation</summary>
        Computed
    }
}
```

### File 2: ValidationRule Model
**Path**: `DataManagementModelsStandard/Editor/UOWManager/Models/ValidationRule.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Represents a validation rule for a field or record
    /// Oracle Forms-compatible validation with modern C# enhancements
    /// </summary>
    public class ValidationRule
    {
        #region Properties
        
        /// <summary>Unique rule name</summary>
        public string RuleName { get; set; }
        
        /// <summary>Rule description</summary>
        public string Description { get; set; }
        
        /// <summary>Field name (null = record-level)</summary>
        public string FieldName { get; set; }
        
        /// <summary>Validation type</summary>
        public ValidationType ValidationType { get; set; }
        
        /// <summary>Error message on failure</summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>Warning message (doesn't block)</summary>
        public string WarningMessage { get; set; }
        
        /// <summary>Execution order (lower = earlier)</summary>
        public int ExecutionOrder { get; set; }
        
        /// <summary>Whether rule is enabled</summary>
        public bool IsEnabled { get; set; } = true;
        
        #endregion
        
        #region Validation Logic
        
        /// <summary>Custom validation function</summary>
        public Func<object, ValidationContext, bool> ValidationFunction { get; set; }
        
        /// <summary>Expression-based validation (serializable)</summary>
        public string ValidationExpression { get; set; }
        
        #endregion
        
        #region Rule Conditions
        
        /// <summary>Field is required</summary>
        public bool IsRequired { get; set; }
        
        /// <summary>Min length (strings)</summary>
        public int? MinLength { get; set; }
        
        /// <summary>Max length (strings)</summary>
        public int? MaxLength { get; set; }
        
        /// <summary>Min value (numbers/dates)</summary>
        public object MinValue { get; set; }
        
        /// <summary>Max value (numbers/dates)</summary>
        public object MaxValue { get; set; }
        
        /// <summary>Regex pattern</summary>
        public string Pattern { get; set; }
        
        /// <summary>Valid values (whitelist)</summary>
        public List<object> ValidValues { get; set; }
        
        /// <summary>Invalid values (blacklist)</summary>
        public List<object> InvalidValues { get; set; }
        
        #endregion
        
        #region Business Rules
        
        /// <summary>Cross-field dependencies</summary>
        public List<string> DependentFields { get; set; } = new List<string>();
        
        /// <summary>Conditional expression</summary>
        public string ConditionalExpression { get; set; }
        
        /// <summary>Computation expression</summary>
        public string ComputationExpression { get; set; }
        
        #endregion
        
        #region Statistics
        
        /// <summary>Execution count</summary>
        public int ExecutionCount { get; set; }
        
        /// <summary>Failure count</summary>
        public int FailureCount { get; set; }
        
        /// <summary>Last execution time</summary>
        public DateTime? LastExecutionTime { get; set; }
        
        #endregion
        
        #region Validation Method
        
        /// <summary>
        /// Validate a value against this rule
        /// </summary>
        public IErrorsInfo Validate(object value, ValidationContext context)
        {
            ExecutionCount++;
            LastExecutionTime = DateTime.Now;
            
            var errors = new ErrorsInfo { Flag = Errors.Ok };
            
            if (!IsEnabled)
                return errors;
                
            try
            {
                // Custom validation function
                if (ValidationFunction != null)
                {
                    if (!ValidationFunction(value, context))
                    {
                        FailureCount++;
                        errors.Flag = Errors.Failed;
                        errors.Message = ErrorMessage ?? $"Validation failed for {FieldName}";
                        return errors;
                    }
                }
                
                // Required validation
                if (IsRequired && (value == null || string.IsNullOrEmpty(value.ToString())))
                {
                    FailureCount++;
                    errors.Flag = Errors.Failed;
                    errors.Message = ErrorMessage ?? $"{FieldName} is required";
                    return errors;
                }
                
                // String validations
                if (value is string strValue && !string.IsNullOrEmpty(strValue))
                {
                    if (MinLength.HasValue && strValue.Length < MinLength.Value)
                    {
                        FailureCount++;
                        errors.Flag = Errors.Failed;
                        errors.Message = ErrorMessage ?? $"{FieldName} must be at least {MinLength} characters";
                        return errors;
                    }
                    
                    if (MaxLength.HasValue && strValue.Length > MaxLength.Value)
                    {
                        FailureCount++;
                        errors.Flag = Errors.Failed;
                        errors.Message = ErrorMessage ?? $"{FieldName} must be at most {MaxLength} characters";
                        return errors;
                    }
                    
                    if (!string.IsNullOrEmpty(Pattern))
                    {
                        var regex = new Regex(Pattern);
                        if (!regex.IsMatch(strValue))
                        {
                            FailureCount++;
                            errors.Flag = Errors.Failed;
                            errors.Message = ErrorMessage ?? $"{FieldName} format is invalid";
                            return errors;
                        }
                    }
                }
                
                // Range validation
                if (value != null && MinValue != null && value is IComparable minComp)
                {
                    if (minComp.CompareTo(Convert.ChangeType(MinValue, value.GetType())) < 0)
                    {
                        FailureCount++;
                        errors.Flag = Errors.Failed;
                        errors.Message = ErrorMessage ?? $"{FieldName} must be at least {MinValue}";
                        return errors;
                    }
                }
                
                if (value != null && MaxValue != null && value is IComparable maxComp)
                {
                    if (maxComp.CompareTo(Convert.ChangeType(MaxValue, value.GetType())) > 0)
                    {
                        FailureCount++;
                        errors.Flag = Errors.Failed;
                        errors.Message = ErrorMessage ?? $"{FieldName} must be at most {MaxValue}";
                        return errors;
                    }
                }
                
                // Valid values whitelist
                if (ValidValues != null && ValidValues.Count > 0 && value != null)
                {
                    var valueStr = value.ToString();
                    if (!ValidValues.Any(v => v?.ToString() == valueStr))
                    {
                        FailureCount++;
                        errors.Flag = Errors.Failed;
                        errors.Message = ErrorMessage ?? $"{FieldName} must be one of: {string.Join(", ", ValidValues)}";
                        return errors;
                    }
                }
                
                // Invalid values blacklist
                if (InvalidValues != null && InvalidValues.Count > 0 && value != null)
                {
                    var valueStr = value.ToString();
                    if (InvalidValues.Any(v => v?.ToString() == valueStr))
                    {
                        FailureCount++;
                        errors.Flag = Errors.Failed;
                        errors.Message = ErrorMessage ?? $"{FieldName} cannot be: {value}";
                        return errors;
                    }
                }
            }
            catch (Exception ex)
            {
                FailureCount++;
                errors.Flag = Errors.Failed;
                errors.Message = $"Validation error: {ex.Message}";
                errors.Ex = ex;
            }
            
            return errors;
        }
        
        #endregion
    }
}
```

### File 3: ValidationContext Model
**Path**: `DataManagementModelsStandard/Editor/UOWManager/Models/ValidationContext.cs`

```csharp
using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Validation context - provides info for validation logic
    /// UI-agnostic version (no BeepDataBlock reference)
    /// </summary>
    public class ValidationContext
    {
        /// <summary>Block name being validated</summary>
        public string BlockName { get; set; }
        
        /// <summary>Current record values</summary>
        public Dictionary<string, object> RecordValues { get; set; } = new Dictionary<string, object>();
        
        /// <summary>Field being validated</summary>
        public string FieldName { get; set; }
        
        /// <summary>Old value (before change)</summary>
        public object OldValue { get; set; }
        
        /// <summary>New value (being validated)</summary>
        public object NewValue { get; set; }
        
        /// <summary>Whether this is a new record</summary>
        public bool IsNewRecord { get; set; }
        
        /// <summary>Block mode</summary>
        public BlockMode Mode { get; set; }
        
        /// <summary>Custom data for validation logic</summary>
        public Dictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Get a field value from the current record
        /// </summary>
        public object GetFieldValue(string fieldName)
        {
            return RecordValues.TryGetValue(fieldName, out var value) ? value : null;
        }
        
        /// <summary>
        /// Check if a field has a value
        /// </summary>
        public bool HasValue(string fieldName)
        {
            return RecordValues.TryGetValue(fieldName, out var value) && 
                   value != null && 
                   !string.IsNullOrEmpty(value.ToString());
        }
    }
}
```

### File 4: ValidationResult Model
**Path**: `DataManagementModelsStandard/Editor/UOWManager/Models/ValidationResult.cs`

```csharp
using System.Collections.Generic;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Result of validation operation
    /// </summary>
    public class ValidationResult
    {
        /// <summary>Overall success/failure</summary>
        public bool IsValid { get; set; } = true;
        
        /// <summary>Errors info for backward compatibility</summary>
        public IErrorsInfo ErrorsInfo { get; set; } = new ErrorsInfo { Flag = Errors.Ok };
        
        /// <summary>Field-specific errors</summary>
        public Dictionary<string, List<string>> FieldErrors { get; set; } = new Dictionary<string, List<string>>();
        
        /// <summary>Field-specific warnings</summary>
        public Dictionary<string, List<string>> FieldWarnings { get; set; } = new Dictionary<string, List<string>>();
        
        /// <summary>Record-level errors</summary>
        public List<string> RecordErrors { get; set; } = new List<string>();
        
        /// <summary>Record-level warnings</summary>
        public List<string> RecordWarnings { get; set; } = new List<string>();
        
        /// <summary>Add a field error</summary>
        public void AddFieldError(string fieldName, string message)
        {
            IsValid = false;
            ErrorsInfo.Flag = Errors.Failed;
            
            if (!FieldErrors.ContainsKey(fieldName))
                FieldErrors[fieldName] = new List<string>();
                
            FieldErrors[fieldName].Add(message);
        }
        
        /// <summary>Add a field warning</summary>
        public void AddFieldWarning(string fieldName, string message)
        {
            if (!FieldWarnings.ContainsKey(fieldName))
                FieldWarnings[fieldName] = new List<string>();
                
            FieldWarnings[fieldName].Add(message);
        }
        
        /// <summary>Add a record-level error</summary>
        public void AddRecordError(string message)
        {
            IsValid = false;
            ErrorsInfo.Flag = Errors.Failed;
            RecordErrors.Add(message);
        }
        
        /// <summary>Add a record-level warning</summary>
        public void AddRecordWarning(string message)
        {
            RecordWarnings.Add(message);
        }
        
        /// <summary>Has any errors</summary>
        public bool HasErrors => !IsValid || FieldErrors.Count > 0 || RecordErrors.Count > 0;
        
        /// <summary>Has any warnings</summary>
        public bool HasWarnings => FieldWarnings.Count > 0 || RecordWarnings.Count > 0;
    }
}
```

### File 5: IValidationManager Interface
**Path**: `DataManagementModelsStandard/Editor/UOWManager/Interfaces/IValidationManager.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor.UOWManager.Interfaces
{
    /// <summary>
    /// Manages validation rules for blocks
    /// Oracle Forms-compatible validation engine
    /// </summary>
    public interface IValidationManager
    {
        #region Rule Registration
        
        /// <summary>Register a validation rule for a field</summary>
        void RegisterValidationRule(string blockName, string fieldName, ValidationRule rule);
        
        /// <summary>Register a record-level validation rule</summary>
        void RegisterRecordValidationRule(string blockName, ValidationRule rule);
        
        /// <summary>Unregister all rules for a field</summary>
        void UnregisterValidationRules(string blockName, string fieldName);
        
        /// <summary>Unregister all rules for a block</summary>
        void UnregisterBlockValidationRules(string blockName);
        
        /// <summary>Get all rules for a field</summary>
        List<ValidationRule> GetValidationRules(string blockName, string fieldName);
        
        /// <summary>Get all rules for a block</summary>
        List<ValidationRule> GetBlockValidationRules(string blockName);
        
        #endregion
        
        #region Validation Methods
        
        /// <summary>Validate a specific field</summary>
        Task<ValidationResult> ValidateFieldAsync(string blockName, string fieldName, object value, Dictionary<string, object> recordValues = null);
        
        /// <summary>Validate an entire record</summary>
        Task<ValidationResult> ValidateRecordAsync(string blockName, Dictionary<string, object> recordValues, bool isNewRecord = false);
        
        /// <summary>Validate before commit</summary>
        Task<ValidationResult> ValidateBeforeCommitAsync(string blockName);
        
        #endregion
        
        #region Fluent API
        
        /// <summary>Fluent API entry point</summary>
        IValidationRuleBuilder ForField(string blockName, string fieldName);
        
        #endregion
        
        #region Error State
        
        /// <summary>Get fields with errors</summary>
        List<string> GetFieldsWithErrors(string blockName);
        
        /// <summary>Clear all validation errors for a block</summary>
        void ClearValidationErrors(string blockName);
        
        /// <summary>Set field error state</summary>
        void SetFieldError(string blockName, string fieldName, string errorMessage);
        
        /// <summary>Clear field error state</summary>
        void ClearFieldError(string blockName, string fieldName);
        
        #endregion
    }
    
    /// <summary>
    /// Fluent API for building validation rules
    /// </summary>
    public interface IValidationRuleBuilder
    {
        IValidationRuleBuilder Required(string errorMessage = null);
        IValidationRuleBuilder MinLength(int length, string errorMessage = null);
        IValidationRuleBuilder MaxLength(int length, string errorMessage = null);
        IValidationRuleBuilder Range(object min, object max, string errorMessage = null);
        IValidationRuleBuilder Pattern(string pattern, string errorMessage = null);
        IValidationRuleBuilder MustBe(params object[] validValues);
        IValidationRuleBuilder CannotBe(params object[] invalidValues);
        IValidationRuleBuilder Custom(Func<object, ValidationContext, bool> validationFunc, string errorMessage);
        IValidationRuleBuilder WithOrder(int order);
        void Apply();
    }
}
```

### File 6: ValidationManager Implementation
**Path**: `DataManagementEngineStandard/Editor/Forms/Helpers/ValidationManager.cs`

```csharp
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor.UOWManager.Helpers
{
    /// <summary>
    /// Manages validation rules for Oracle Forms-compatible validation
    /// </summary>
    public class ValidationManager : IValidationManager
    {
        #region Fields
        
        private readonly IDMEEditor _dmeEditor;
        private readonly ConcurrentDictionary<string, DataBlockInfo> _blocks;
        
        // Key: "blockName:fieldName", Value: List of rules
        private readonly ConcurrentDictionary<string, List<ValidationRule>> _validationRules = new();
        
        // Error state: Key: "blockName:fieldName"
        private readonly ConcurrentDictionary<string, string> _fieldErrors = new();
        
        #endregion
        
        #region Constructor
        
        public ValidationManager(
            IDMEEditor dmeEditor,
            ConcurrentDictionary<string, DataBlockInfo> blocks)
        {
            _dmeEditor = dmeEditor ?? throw new ArgumentNullException(nameof(dmeEditor));
            _blocks = blocks ?? throw new ArgumentNullException(nameof(blocks));
        }
        
        #endregion
        
        #region Rule Registration
        
        public void RegisterValidationRule(string blockName, string fieldName, ValidationRule rule)
        {
            if (string.IsNullOrEmpty(blockName))
                throw new ArgumentException("Block name required", nameof(blockName));
            if (rule == null)
                throw new ArgumentNullException(nameof(rule));
                
            var key = GetRuleKey(blockName, fieldName ?? "*");
            
            var rules = _validationRules.GetOrAdd(key, _ => new List<ValidationRule>());
            
            lock (rules)
            {
                rules.Add(rule);
                rules.Sort((a, b) => a.ExecutionOrder.CompareTo(b.ExecutionOrder));
            }
        }
        
        public void RegisterRecordValidationRule(string blockName, ValidationRule rule)
        {
            RegisterValidationRule(blockName, "*", rule);
        }
        
        public void UnregisterValidationRules(string blockName, string fieldName)
        {
            var key = GetRuleKey(blockName, fieldName);
            _validationRules.TryRemove(key, out _);
        }
        
        public void UnregisterBlockValidationRules(string blockName)
        {
            var keysToRemove = _validationRules.Keys
                .Where(k => k.StartsWith($"{blockName}:"))
                .ToList();
                
            foreach (var key in keysToRemove)
            {
                _validationRules.TryRemove(key, out _);
            }
        }
        
        public List<ValidationRule> GetValidationRules(string blockName, string fieldName)
        {
            var key = GetRuleKey(blockName, fieldName);
            return _validationRules.TryGetValue(key, out var rules)
                ? new List<ValidationRule>(rules)
                : new List<ValidationRule>();
        }
        
        public List<ValidationRule> GetBlockValidationRules(string blockName)
        {
            return _validationRules
                .Where(kvp => kvp.Key.StartsWith($"{blockName}:"))
                .SelectMany(kvp => kvp.Value)
                .ToList();
        }
        
        #endregion
        
        #region Validation Methods
        
        public async Task<ValidationResult> ValidateFieldAsync(
            string blockName, 
            string fieldName, 
            object value, 
            Dictionary<string, object> recordValues = null)
        {
            var result = new ValidationResult();
            
            var key = GetRuleKey(blockName, fieldName);
            if (!_validationRules.TryGetValue(key, out var rules))
                return result;  // No rules = valid
                
            var context = new ValidationContext
            {
                BlockName = blockName,
                FieldName = fieldName,
                NewValue = value,
                RecordValues = recordValues ?? new Dictionary<string, object>(),
                Mode = _blocks.TryGetValue(blockName, out var block) ? block.Status : BlockMode.CRUD
            };
            
            foreach (var rule in rules.Where(r => r.IsEnabled))
            {
                var ruleResult = rule.Validate(value, context);
                
                if (ruleResult.Flag != Errors.Ok)
                {
                    result.AddFieldError(fieldName, ruleResult.Message);
                    SetFieldError(blockName, fieldName, ruleResult.Message);
                    return result;  // Stop on first error
                }
            }
            
            // Clear error if validation passed
            ClearFieldError(blockName, fieldName);
            
            return result;
        }
        
        public async Task<ValidationResult> ValidateRecordAsync(
            string blockName, 
            Dictionary<string, object> recordValues, 
            bool isNewRecord = false)
        {
            var result = new ValidationResult();
            
            // Validate each field with rules
            var fieldKeys = _validationRules.Keys
                .Where(k => k.StartsWith($"{blockName}:") && !k.EndsWith(":*"))
                .ToList();
                
            foreach (var key in fieldKeys)
            {
                var fieldName = key.Split(':')[1];
                var value = recordValues.TryGetValue(fieldName, out var v) ? v : null;
                
                var fieldResult = await ValidateFieldAsync(blockName, fieldName, value, recordValues);
                
                if (!fieldResult.IsValid)
                {
                    foreach (var error in fieldResult.FieldErrors.SelectMany(kvp => kvp.Value))
                    {
                        result.AddFieldError(fieldName, error);
                    }
                }
            }
            
            // Validate record-level rules
            var recordKey = GetRuleKey(blockName, "*");
            if (_validationRules.TryGetValue(recordKey, out var recordRules))
            {
                var context = new ValidationContext
                {
                    BlockName = blockName,
                    RecordValues = recordValues,
                    IsNewRecord = isNewRecord,
                    Mode = _blocks.TryGetValue(blockName, out var block) ? block.Status : BlockMode.CRUD
                };
                
                foreach (var rule in recordRules.Where(r => r.IsEnabled))
                {
                    var ruleResult = rule.Validate(null, context);
                    
                    if (ruleResult.Flag != Errors.Ok)
                    {
                        result.AddRecordError(ruleResult.Message);
                    }
                }
            }
            
            return result;
        }
        
        public async Task<ValidationResult> ValidateBeforeCommitAsync(string blockName)
        {
            if (!_blocks.TryGetValue(blockName, out var blockInfo))
                return new ValidationResult();
                
            // Get current record values from UnitOfWork
            var recordValues = new Dictionary<string, object>();
            
            if (blockInfo.UnitOfWork?.CurrentItem != null)
            {
                var currentItem = blockInfo.UnitOfWork.CurrentItem;
                var properties = currentItem.GetType().GetProperties();
                
                foreach (var prop in properties)
                {
                    try
                    {
                        recordValues[prop.Name] = prop.GetValue(currentItem);
                    }
                    catch { }
                }
            }
            
            var isNewRecord = blockInfo.Status == BlockMode.Insert;
            
            return await ValidateRecordAsync(blockName, recordValues, isNewRecord);
        }
        
        #endregion
        
        #region Fluent API
        
        public IValidationRuleBuilder ForField(string blockName, string fieldName)
        {
            return new ValidationRuleBuilder(this, blockName, fieldName);
        }
        
        #endregion
        
        #region Error State
        
        public List<string> GetFieldsWithErrors(string blockName)
        {
            return _fieldErrors.Keys
                .Where(k => k.StartsWith($"{blockName}:"))
                .Select(k => k.Split(':')[1])
                .ToList();
        }
        
        public void ClearValidationErrors(string blockName)
        {
            var keysToRemove = _fieldErrors.Keys
                .Where(k => k.StartsWith($"{blockName}:"))
                .ToList();
                
            foreach (var key in keysToRemove)
            {
                _fieldErrors.TryRemove(key, out _);
            }
        }
        
        public void SetFieldError(string blockName, string fieldName, string errorMessage)
        {
            var key = GetRuleKey(blockName, fieldName);
            _fieldErrors[key] = errorMessage;
        }
        
        public void ClearFieldError(string blockName, string fieldName)
        {
            var key = GetRuleKey(blockName, fieldName);
            _fieldErrors.TryRemove(key, out _);
        }
        
        /// <summary>Get field error message</summary>
        public string GetFieldError(string blockName, string fieldName)
        {
            var key = GetRuleKey(blockName, fieldName);
            return _fieldErrors.TryGetValue(key, out var error) ? error : null;
        }
        
        #endregion
        
        #region Private Methods
        
        private static string GetRuleKey(string blockName, string fieldName)
        {
            return $"{blockName}:{fieldName}";
        }
        
        #endregion
    }
    
    /// <summary>
    /// Fluent API builder implementation
    /// </summary>
    internal class ValidationRuleBuilder : IValidationRuleBuilder
    {
        private readonly ValidationManager _manager;
        private readonly string _blockName;
        private readonly string _fieldName;
        private readonly ValidationRule _rule;
        
        public ValidationRuleBuilder(ValidationManager manager, string blockName, string fieldName)
        {
            _manager = manager;
            _blockName = blockName;
            _fieldName = fieldName;
            _rule = new ValidationRule { FieldName = fieldName };
        }
        
        public IValidationRuleBuilder Required(string errorMessage = null)
        {
            _rule.IsRequired = true;
            _rule.ErrorMessage = errorMessage ?? $"{_fieldName} is required";
            _rule.ValidationType = ValidationType.Required;
            return this;
        }
        
        public IValidationRuleBuilder MinLength(int length, string errorMessage = null)
        {
            _rule.MinLength = length;
            _rule.ErrorMessage = errorMessage ?? $"{_fieldName} must be at least {length} characters";
            _rule.ValidationType = ValidationType.Length;
            return this;
        }
        
        public IValidationRuleBuilder MaxLength(int length, string errorMessage = null)
        {
            _rule.MaxLength = length;
            _rule.ErrorMessage = errorMessage ?? $"{_fieldName} must be at most {length} characters";
            _rule.ValidationType = ValidationType.Length;
            return this;
        }
        
        public IValidationRuleBuilder Range(object min, object max, string errorMessage = null)
        {
            _rule.MinValue = min;
            _rule.MaxValue = max;
            _rule.ErrorMessage = errorMessage ?? $"{_fieldName} must be between {min} and {max}";
            _rule.ValidationType = ValidationType.Range;
            return this;
        }
        
        public IValidationRuleBuilder Pattern(string pattern, string errorMessage = null)
        {
            _rule.Pattern = pattern;
            _rule.ErrorMessage = errorMessage ?? $"{_fieldName} format is invalid";
            _rule.ValidationType = ValidationType.Format;
            return this;
        }
        
        public IValidationRuleBuilder MustBe(params object[] validValues)
        {
            _rule.ValidValues = validValues.ToList();
            _rule.ErrorMessage = $"{_fieldName} must be one of: {string.Join(", ", validValues)}";
            _rule.ValidationType = ValidationType.BusinessRule;
            return this;
        }
        
        public IValidationRuleBuilder CannotBe(params object[] invalidValues)
        {
            _rule.InvalidValues = invalidValues.ToList();
            _rule.ErrorMessage = $"{_fieldName} cannot be: {string.Join(", ", invalidValues)}";
            _rule.ValidationType = ValidationType.BusinessRule;
            return this;
        }
        
        public IValidationRuleBuilder Custom(Func<object, ValidationContext, bool> validationFunc, string errorMessage)
        {
            _rule.ValidationFunction = validationFunc;
            _rule.ErrorMessage = errorMessage;
            _rule.ValidationType = ValidationType.BusinessRule;
            return this;
        }
        
        public IValidationRuleBuilder WithOrder(int order)
        {
            _rule.ExecutionOrder = order;
            return this;
        }
        
        public void Apply()
        {
            _manager.RegisterValidationRule(_blockName, _fieldName, _rule);
        }
    }
}
```

### File 7: FormsManager.Validation.cs Partial
**Path**: `DataManagementEngineStandard/Editor/Forms/FormsManager.Validation.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.UOWManager.Helpers;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    /// <summary>
    /// FormsManager partial - Validation support
    /// </summary>
    public partial class FormsManager
    {
        #region Fields
        
        private IValidationManager _validationManager;
        
        #endregion
        
        #region Properties
        
        /// <summary>Validation manager</summary>
        public IValidationManager ValidationManager => _validationManager;
        
        #endregion
        
        #region Validation Methods
        
        /// <summary>Register a validation rule for current block</summary>
        public void RegisterValidationRule(string fieldName, ValidationRule rule)
        {
            _validationManager?.RegisterValidationRule(CurrentBlockName, fieldName, rule);
        }
        
        /// <summary>Register a validation rule for specific block</summary>
        public void RegisterValidationRule(string blockName, string fieldName, ValidationRule rule)
        {
            _validationManager?.RegisterValidationRule(blockName, fieldName, rule);
        }
        
        /// <summary>Register a record-level validation rule</summary>
        public void RegisterRecordValidationRule(ValidationRule rule)
        {
            _validationManager?.RegisterRecordValidationRule(CurrentBlockName, rule);
        }
        
        /// <summary>Validate a field in current block</summary>
        public async Task<ValidationResult> ValidateFieldAsync(string fieldName, object value)
        {
            return await (_validationManager?.ValidateFieldAsync(CurrentBlockName, fieldName, value) 
                ?? Task.FromResult(new ValidationResult()));
        }
        
        /// <summary>Validate current record in current block</summary>
        public async Task<ValidationResult> ValidateCurrentRecordAsync()
        {
            return await (_validationManager?.ValidateBeforeCommitAsync(CurrentBlockName)
                ?? Task.FromResult(new ValidationResult()));
        }
        
        /// <summary>Fluent API for validation rules</summary>
        public IValidationRuleBuilder ForField(string fieldName)
        {
            return _validationManager?.ForField(CurrentBlockName, fieldName);
        }
        
        /// <summary>Fluent API for validation rules on specific block</summary>
        public IValidationRuleBuilder ForField(string blockName, string fieldName)
        {
            return _validationManager?.ForField(blockName, fieldName);
        }
        
        /// <summary>Get fields with validation errors</summary>
        public List<string> GetFieldsWithErrors()
        {
            return _validationManager?.GetFieldsWithErrors(CurrentBlockName) ?? new List<string>();
        }
        
        /// <summary>Clear all validation errors</summary>
        public void ClearValidationErrors()
        {
            _validationManager?.ClearValidationErrors(CurrentBlockName);
        }
        
        #endregion
        
        #region Initialization
        
        /// <summary>Initialize validation manager</summary>
        private void InitializeValidationManager()
        {
            _validationManager = new ValidationManager(_dmeEditor, _blocks);
        }
        
        #endregion
    }
}
```

---

## Modifications to Existing Files

### File 8: Update FormsManager.cs Constructor
**Path**: `DataManagementEngineStandard/Editor/Forms/FormsManager.cs`

**Add to constructor**:
```csharp
// Initialize validation manager
InitializeValidationManager();
```

---

## BeepDataBlock Refactoring (Beep.Winform)

After migration, update `BeepDataBlock.Validation.cs` to delegate to FormsManager:

### Updated BeepDataBlock.Validation.cs

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Winform.Controls
{
    /// <summary>
    /// BeepDataBlock partial - Validation (thin UI wrapper)
    /// Business logic delegated to FormsManager.ValidationManager
    /// </summary>
    public partial class BeepDataBlock
    {
        #region Delegation to FormsManager
        
        /// <summary>Register a validation rule (delegates to FormsManager)</summary>
        public void RegisterValidationRule(string fieldName, ValidationRule rule)
        {
            _formsManager?.ValidationManager?.RegisterValidationRule(Name, fieldName, rule);
        }
        
        /// <summary>Validate a field (delegates to FormsManager)</summary>
        public async Task<IErrorsInfo> ValidateField(string fieldName, object value)
        {
            var result = await (_formsManager?.ValidationManager?.ValidateFieldAsync(Name, fieldName, value, GetCurrentRecordValues())
                ?? Task.FromResult(new ValidationResult()));
            
            // UI-specific: Update visual error state
            UpdateFieldErrorState(fieldName, result);
            
            return result.ErrorsInfo;
        }
        
        /// <summary>Validate current record (delegates to FormsManager)</summary>
        public async Task<IErrorsInfo> ValidateCurrentRecord()
        {
            var result = await (_formsManager?.ValidationManager?.ValidateRecordAsync(Name, GetCurrentRecordValues())
                ?? Task.FromResult(new ValidationResult()));
            
            // UI-specific: Update visual error states
            UpdateRecordErrorStates(result);
            
            return result.ErrorsInfo;
        }
        
        /// <summary>Fluent API (delegates to FormsManager)</summary>
        public IValidationRuleBuilder ForField(string fieldName)
        {
            return _formsManager?.ValidationManager?.ForField(Name, fieldName);
        }
        
        #endregion
        
        #region UI-Specific Methods (Keep in BeepDataBlock)
        
        /// <summary>Update visual error state for a field</summary>
        private void UpdateFieldErrorState(string fieldName, ValidationResult result)
        {
            if (TryResolveItem(fieldName, out var item, out _))
            {
                item.HasError = result.FieldErrors.ContainsKey(fieldName);
                item.ErrorMessage = item.HasError 
                    ? string.Join(", ", result.FieldErrors[fieldName])
                    : null;
                    
                // Update control visual (border color, etc.)
                UpdateControlVisualState(item);
            }
        }
        
        /// <summary>Update visual error states for all fields</summary>
        private void UpdateRecordErrorStates(ValidationResult result)
        {
            foreach (var fieldName in GetAllFieldNames())
            {
                UpdateFieldErrorState(fieldName, result);
            }
        }
        
        /// <summary>Update control visual state based on error</summary>
        private void UpdateControlVisualState(BeepDataBlockItem item)
        {
            if (item.Control == null)
                return;
                
            // UI-specific styling for errors
            // This stays in BeepDataBlock as it's WinForms-specific
            if (item.HasError)
            {
                // Apply error styling
                item.Control.BackColor = System.Drawing.Color.MistyRose;
            }
            else
            {
                // Clear error styling
                item.Control.BackColor = System.Drawing.SystemColors.Window;
            }
        }
        
        /// <summary>Clear all validation errors (UI state)</summary>
        public void ClearValidationErrors()
        {
            _formsManager?.ValidationManager?.ClearValidationErrors(Name);
            
            // Clear UI visual states
            foreach (var item in _items.Values)
            {
                item.HasError = false;
                item.ErrorMessage = null;
                UpdateControlVisualState(item);
            }
        }
        
        /// <summary>Get fields with errors from FormsManager</summary>
        public List<string> GetFieldsWithErrors()
        {
            return _formsManager?.ValidationManager?.GetFieldsWithErrors(Name) ?? new List<string>();
        }
        
        #endregion
    }
}
```

---

## Verification Steps

1. **Build BeepDM** - Verify no compile errors
2. **Check interfaces** - Ensure `IValidationManager`, `IValidationRuleBuilder` accessible
3. **Check models** - Ensure `ValidationRule`, `ValidationType`, `ValidationContext`, `ValidationResult` accessible
4. **Update BeepDataBlock** - Reference new models, delegate to FormsManager
5. **Test validation** - Verify rules work correctly through delegation

---

## Dependencies

- **Phase A2 depends on**: None
- **Phase A4 depends on Phase A2**: Item's REQUIRED property uses validation

---

## Files Summary

| File | Action | Location |
|------|--------|----------|
| `ValidationType.cs` | CREATE | `DataManagementModelsStandard/Editor/UOWManager/Models/` |
| `ValidationRule.cs` | CREATE | `DataManagementModelsStandard/Editor/UOWManager/Models/` |
| `ValidationContext.cs` | CREATE | `DataManagementModelsStandard/Editor/UOWManager/Models/` |
| `ValidationResult.cs` | CREATE | `DataManagementModelsStandard/Editor/UOWManager/Models/` |
| `IValidationManager.cs` | CREATE | `DataManagementModelsStandard/Editor/UOWManager/Interfaces/` |
| `ValidationManager.cs` | CREATE | `DataManagementEngineStandard/Editor/Forms/Helpers/` |
| `FormsManager.Validation.cs` | CREATE | `DataManagementEngineStandard/Editor/Forms/` |
| `FormsManager.cs` | MODIFY | Add initialization call |
| `BeepDataBlock.Validation.cs` | MODIFY | Thin wrapper delegation (Beep.Winform) |
