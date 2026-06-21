using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
namespace TheTechIdea.Beep.Editor.UOWManager.Helpers
{
    /// <summary>
    /// Fluent builder for ValidationRule.
    /// Usage: manager.ForField("CustomerBlock", "Email").Required().Pattern(emailRegex).Register();
    /// Ported from BeepDataBlock.Validation.cs ValidationRuleBuilder.
    /// </summary>
    public class ValidationRuleBuilder
    {
        private readonly IValidationManager _manager;
        private readonly string _blockName;
        private readonly string _fieldName;
        private readonly ValidationRule _rule;

        /// <summary>
        /// Creates a fluent builder for a field-level validation rule.
        /// </summary>
        /// <param name="manager">Validation manager that will receive the rule on registration.</param>
        /// <param name="blockName">Logical block name for the rule.</param>
        /// <param name="fieldName">Field name the rule applies to.</param>
        public ValidationRuleBuilder(IValidationManager manager, string blockName, string fieldName)
        {
            _manager = manager;
            _blockName = blockName;
            _fieldName = fieldName;
            _rule = new ValidationRule
            {
                BlockName = blockName,
                ItemName = fieldName,
                RuleName = $"{blockName}_{fieldName}_rule"
            };
        }

        /// <summary>
        /// Configures the rule as a required-value check.
        /// </summary>
        public ValidationRuleBuilder Required()
        {
            _rule.ValidationType = ValidationType.Required;
            _rule.ErrorMessage ??= $"{_fieldName} is required";
            return this;
        }

        /// <summary>
        /// Configures the rule as a minimum string-length check.
        /// </summary>
        public ValidationRuleBuilder MinLength(int min)
        {
            _rule.ValidationType = ValidationType.MinLength;
            _rule.MinValue = min;
            _rule.ErrorMessage ??= $"{_fieldName} must be at least {min} characters";
            return this;
        }

        /// <summary>
        /// Configures the rule as a maximum string-length check.
        /// </summary>
        public ValidationRuleBuilder MaxLength(int max)
        {
            _rule.ValidationType = ValidationType.MaxLength;
            _rule.MaxValue = max;
            _rule.ErrorMessage ??= $"{_fieldName} must be at most {max} characters";
            return this;
        }

        /// <summary>
        /// Configures the rule as a minimum/maximum range check.
        /// </summary>
        public ValidationRuleBuilder Range(object min, object max)
        {
            _rule.ValidationType = ValidationType.Range;
            _rule.MinValue = min;
            _rule.MaxValue = max;
            _rule.ErrorMessage ??= $"{_fieldName} must be between {min} and {max}";
            return this;
        }

        /// <summary>
        /// Configures the rule as a regex-pattern check.
        /// </summary>
        public ValidationRuleBuilder Pattern(string regex)
        {
            _rule.ValidationType = ValidationType.Pattern;
            _rule.Pattern = regex;
            _rule.ErrorMessage ??= $"{_fieldName} has invalid format";
            return this;
        }

        /// <summary>
        /// Configures the rule with a custom predicate that must return a valid result.
        /// </summary>
        public ValidationRuleBuilder MustBe(Func<object, object, Dictionary<string, object>, Task<(bool isValid, string errorMessage)>> predicate)
        {
            _rule.ValidationType = ValidationType.Custom;
            _rule.CustomValidator = predicate;
            return this;
        }

        /// <summary>
        /// Configures the rule to reject a single forbidden value.
        /// </summary>
        public ValidationRuleBuilder CannotBe(object forbiddenValue)
        {
            _rule.ValidationType = ValidationType.Custom;
            _rule.CustomValidator = (value, record, ctx) =>
                Task.FromResult((!Equals(value, forbiddenValue), $"{_fieldName} cannot be {forbiddenValue}"));
            _rule.ErrorMessage ??= $"{_fieldName} cannot be {forbiddenValue}";
            return this;
        }

        /// <summary>
        /// Configures the rule with a custom validation callback.
        /// </summary>
        public ValidationRuleBuilder Custom(Func<object, object, Dictionary<string, object>, Task<(bool isValid, string errorMessage)>> predicate)
        {
            _rule.ValidationType = ValidationType.Custom;
            _rule.CustomValidator = predicate;
            return this;
        }

        /// <summary>
        /// Overrides the validation error message.
        /// </summary>
        public ValidationRuleBuilder WithMessage(string message)
        {
            _rule.ErrorMessage = message;
            return this;
        }

        /// <summary>
        /// Overrides the generated rule name.
        /// </summary>
        public ValidationRuleBuilder WithName(string ruleName)
        {
            _rule.RuleName = ruleName;
            return this;
        }

        /// <summary>
        /// Sets the execution order used by the validation manager.
        /// </summary>
        public ValidationRuleBuilder WithOrder(int order)
        {
            _rule.ExecutionOrder = order;
            return this;
        }

        /// <summary>
        /// Sets the severity reported when the rule fails.
        /// </summary>
        public ValidationRuleBuilder WithSeverity(ValidationSeverity severity)
        {
            _rule.Severity = severity;
            return this;
        }

        /// <summary>
        /// Sets the validation timing at which the rule should execute.
        /// </summary>
        public ValidationRuleBuilder WithTiming(ValidationTiming timing)
        {
            _rule.Timing = timing;
            return this;
        }

        /// <summary>
        /// Controls whether later rules should stop executing after this rule fails.
        /// </summary>
        public ValidationRuleBuilder StopOnFailure(bool stop = true)
        {
            _rule.StopOnFailure = stop;
            return this;
        }

        /// <summary>Registers the built rule with the ValidationManager and returns it.</summary>
        public ValidationRule Register()
        {
            _manager.RegisterRule(_rule);
            return _rule;
        }
    }
}
