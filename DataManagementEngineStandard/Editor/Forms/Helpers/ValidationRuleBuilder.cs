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

        public ValidationRuleBuilder Required()
        {
            _rule.ValidationType = ValidationType.Required;
            _rule.ErrorMessage ??= $"{_fieldName} is required";
            return this;
        }

        public ValidationRuleBuilder MinLength(int min)
        {
            _rule.ValidationType = ValidationType.MinLength;
            _rule.MinValue = min;
            _rule.ErrorMessage ??= $"{_fieldName} must be at least {min} characters";
            return this;
        }

        public ValidationRuleBuilder MaxLength(int max)
        {
            _rule.ValidationType = ValidationType.MaxLength;
            _rule.MaxValue = max;
            _rule.ErrorMessage ??= $"{_fieldName} must be at most {max} characters";
            return this;
        }

        public ValidationRuleBuilder Range(object min, object max)
        {
            _rule.ValidationType = ValidationType.Range;
            _rule.MinValue = min;
            _rule.MaxValue = max;
            _rule.ErrorMessage ??= $"{_fieldName} must be between {min} and {max}";
            return this;
        }

        public ValidationRuleBuilder Pattern(string regex)
        {
            _rule.ValidationType = ValidationType.Pattern;
            _rule.Pattern = regex;
            _rule.ErrorMessage ??= $"{_fieldName} has invalid format";
            return this;
        }

        public ValidationRuleBuilder MustBe(Func<object, object, Dictionary<string, object>, Task<(bool isValid, string errorMessage)>> predicate)
        {
            _rule.ValidationType = ValidationType.Custom;
            _rule.CustomValidator = predicate;
            return this;
        }

        public ValidationRuleBuilder CannotBe(object forbiddenValue)
        {
            _rule.ValidationType = ValidationType.Custom;
            _rule.CustomValidator = (value, record, ctx) =>
                Task.FromResult((!Equals(value, forbiddenValue), $"{_fieldName} cannot be {forbiddenValue}"));
            _rule.ErrorMessage ??= $"{_fieldName} cannot be {forbiddenValue}";
            return this;
        }

        public ValidationRuleBuilder Custom(Func<object, object, Dictionary<string, object>, Task<(bool isValid, string errorMessage)>> predicate)
        {
            _rule.ValidationType = ValidationType.Custom;
            _rule.CustomValidator = predicate;
            return this;
        }

        public ValidationRuleBuilder WithMessage(string message)
        {
            _rule.ErrorMessage = message;
            return this;
        }

        public ValidationRuleBuilder WithName(string ruleName)
        {
            _rule.RuleName = ruleName;
            return this;
        }

        public ValidationRuleBuilder WithOrder(int order)
        {
            _rule.ExecutionOrder = order;
            return this;
        }

        public ValidationRuleBuilder WithSeverity(ValidationSeverity severity)
        {
            _rule.Severity = severity;
            return this;
        }

        public ValidationRuleBuilder WithTiming(ValidationTiming timing)
        {
            _rule.Timing = timing;
            return this;
        }

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
