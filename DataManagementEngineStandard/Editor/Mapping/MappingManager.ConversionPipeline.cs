using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Workflow.Mapping;

namespace TheTechIdea.Beep.Editor.Mapping
{
    public enum MappingNullHandlingMode
    {
        PreserveNull,
        SubstituteDefault,
        SkipAssignment
    }

    public enum MappingConversionFailureMode
    {
        WarnAndContinue,
        Reject,
        UseFallback
    }

    public enum MappingStringCaseMode
    {
        None,
        Upper,
        Lower
    }

    public sealed class MappingConversionPolicy
    {
        public string CultureName { get; set; } = CultureInfo.InvariantCulture.Name;
        public MappingNullHandlingMode NullHandlingMode { get; set; } = MappingNullHandlingMode.PreserveNull;
        public object NullSubstituteValue { get; set; }
        public MappingConversionFailureMode FailureMode { get; set; } = MappingConversionFailureMode.WarnAndContinue;
        public object FallbackValue { get; set; }
        public bool TrimStrings { get; set; } = false;
        public MappingStringCaseMode StringCaseMode { get; set; } = MappingStringCaseMode.None;
    }

    public sealed class FieldTransformStep
    {
        public string Name { get; set; } = string.Empty;
        public string Argument { get; set; } = string.Empty;
    }

    public sealed class MappingFieldTransformChain
    {
        public string DestinationFieldName { get; set; } = string.Empty;
        public List<FieldTransformStep> Steps { get; set; } = new List<FieldTransformStep>();
        public Func<object, object> CustomResolver { get; set; }
    }

    public sealed class MappingConversionResult
    {
        public bool Success { get; set; }
        public bool Skipped { get; set; }
        public object Value { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public static partial class MappingManager
    {
        private static readonly ConcurrentDictionary<string, MappingConversionPolicy> ConversionPolicies = new(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<string, MappingFieldTransformChain> FieldTransformChains = new(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<string, Func<object, string, object>> TransformRegistry = CreateDefaultTransformRegistry();

        public static void SetConversionPolicy(string destinationDataSource, string destinationEntity, MappingConversionPolicy policy)
        {
            var key = BuildEntityKey(destinationDataSource, destinationEntity);
            if (policy == null)
            {
                ConversionPolicies.TryRemove(key, out _);
                return;
            }

            ConversionPolicies[key] = policy;
        }

        public static MappingConversionPolicy GetConversionPolicy(string destinationDataSource, string destinationEntity)
        {
            var key = BuildEntityKey(destinationDataSource, destinationEntity);
            return ConversionPolicies.TryGetValue(key, out var policy) ? policy : new MappingConversionPolicy();
        }

        public static void SetFieldTransformChain(
            string destinationDataSource,
            string destinationEntity,
            string destinationField,
            MappingFieldTransformChain chain)
        {
            var key = BuildFieldKey(destinationDataSource, destinationEntity, destinationField);
            if (chain == null)
            {
                FieldTransformChains.TryRemove(key, out _);
                return;
            }

            chain.DestinationFieldName = destinationField ?? chain.DestinationFieldName;
            FieldTransformChains[key] = chain;
        }

        public static MappingFieldTransformChain GetFieldTransformChain(string destinationDataSource, string destinationEntity, string destinationField)
        {
            var key = BuildFieldKey(destinationDataSource, destinationEntity, destinationField);
            return FieldTransformChains.TryGetValue(key, out var chain) ? chain : null;
        }

        public static void RegisterTransform(string name, Func<object, string, object> transformer)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Transform name cannot be empty.", nameof(name));
            if (transformer == null)
                throw new ArgumentNullException(nameof(transformer));

            TransformRegistry[name.Trim()] = transformer;
        }

        private static void MapProperty(
            object source,
            object destination,
            Mapping_rep_fields mapping,
            string destinationDataSource,
            string destinationEntity)
        {
            if (mapping == null)
                throw new ArgumentNullException(nameof(mapping));

            var destinationProperty = destination.GetType().GetProperty(mapping.ToFieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (destinationProperty == null)
                throw new InvalidOperationException($"Destination property '{mapping.ToFieldName}' not found on object of type '{destination.GetType().Name}'.");

            var trace = ResolveRuleExecutionTrace(source, mapping, out var rule);
            if (trace.Decision == MappingRuleDecision.Skip)
                return;

            object sourceValue = null;
            if (trace.Decision == MappingRuleDecision.UseDefault)
            {
                sourceValue = rule.NullDefaultSpecified ? rule.NullDefaultValue : null;
            }
            else
            {
                sourceValue = ResolveSourceValueFromPath(source, rule.SourcePath);
                if (sourceValue == null && rule.NullDefaultSpecified)
                    sourceValue = rule.NullDefaultValue;
            }

            var policy = GetConversionPolicy(destinationDataSource, destinationEntity);
            var transformChain = GetFieldTransformChain(destinationDataSource, destinationEntity, mapping.ToFieldName);
            var effectiveChain = MergeTransformChains(mapping.ToFieldName, rule.ExplicitTransforms, transformChain);
            var conversion = ApplyConversionPipeline(sourceValue, destinationProperty.PropertyType, policy, effectiveChain);

            if (conversion.Skipped)
                return;
            if (!conversion.Success && policy.FailureMode == MappingConversionFailureMode.Reject)
                throw new InvalidOperationException(conversion.Message);

            destinationProperty.SetValue(destination, conversion.Value, null);
        }

        private static MappingFieldTransformChain MergeTransformChains(
            string destinationField,
            List<FieldTransformStep> ruleTransforms,
            MappingFieldTransformChain configuredChain)
        {
            var steps = new List<FieldTransformStep>();
            if (ruleTransforms != null && ruleTransforms.Count > 0)
                steps.AddRange(ruleTransforms.Where(step => step != null));
            if (configuredChain?.Steps != null && configuredChain.Steps.Count > 0)
                steps.AddRange(configuredChain.Steps.Where(step => step != null));

            if (steps.Count == 0 && configuredChain == null)
                return null;

            return new MappingFieldTransformChain
            {
                DestinationFieldName = destinationField ?? configuredChain?.DestinationFieldName ?? string.Empty,
                Steps = steps,
                CustomResolver = configuredChain?.CustomResolver
            };
        }

        private static MappingConversionResult ApplyConversionPipeline(
            object sourceValue,
            Type targetType,
            MappingConversionPolicy policy,
            MappingFieldTransformChain transformChain)
        {
            var result = new MappingConversionResult { Success = true, Value = sourceValue };
            policy ??= new MappingConversionPolicy();

            if (sourceValue == null || sourceValue == DBNull.Value)
            {
                switch (policy.NullHandlingMode)
                {
                    case MappingNullHandlingMode.SkipAssignment:
                        result.Skipped = true;
                        result.Message = "Skipped null assignment by policy.";
                        return result;
                    case MappingNullHandlingMode.SubstituteDefault:
                        result.Value = policy.NullSubstituteValue;
                        break;
                    case MappingNullHandlingMode.PreserveNull:
                    default:
                        result.Value = null;
                        break;
                }
            }

            if (result.Value is string stringValue)
            {
                if (policy.TrimStrings)
                    stringValue = stringValue.Trim();

                switch (policy.StringCaseMode)
                {
                    case MappingStringCaseMode.Upper:
                        stringValue = stringValue.ToUpperInvariant();
                        break;
                    case MappingStringCaseMode.Lower:
                        stringValue = stringValue.ToLowerInvariant();
                        break;
                }

                result.Value = stringValue;
            }

            if (transformChain != null)
            {
                foreach (var step in transformChain.Steps ?? new List<FieldTransformStep>())
                {
                    if (step == null || string.IsNullOrWhiteSpace(step.Name))
                        continue;

                    if (!TransformRegistry.TryGetValue(step.Name.Trim(), out var transformer))
                        continue;

                    result.Value = transformer(result.Value, step.Argument);
                }

                if (transformChain.CustomResolver != null)
                    result.Value = transformChain.CustomResolver(result.Value);
            }

            try
            {
                result.Value = ConvertValue(result.Value, targetType, policy);
                result.Success = true;
            }
            catch (Exception ex)
            {
                if (policy.FailureMode == MappingConversionFailureMode.UseFallback)
                {
                    result.Value = policy.FallbackValue;
                    result.Success = true;
                    result.Message = $"Conversion failed; fallback value used. {ex.Message}";
                }
                else if (policy.FailureMode == MappingConversionFailureMode.WarnAndContinue)
                {
                    result.Value = null;
                    result.Success = true;
                    result.Message = $"Conversion failed; null assigned. {ex.Message}";
                }
                else
                {
                    result.Success = false;
                    result.Message = ex.Message;
                }
            }

            return result;
        }

        private static object ConvertValue(object value, Type targetType, MappingConversionPolicy policy)
        {
            if (value == null)
                return null;

            var sourceType = value.GetType();
            if (targetType.IsAssignableFrom(sourceType))
                return value;

            var nonNullableType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            var culture = ResolveCulture(policy?.CultureName);

            if (nonNullableType == typeof(Guid))
                return Guid.Parse(value.ToString());
            if (nonNullableType.IsEnum)
                return Enum.Parse(nonNullableType, value.ToString(), ignoreCase: true);
            if (nonNullableType == typeof(DateTime))
                return DateTime.Parse(value.ToString(), culture, DateTimeStyles.RoundtripKind);
            if (nonNullableType == typeof(DateTimeOffset))
                return DateTimeOffset.Parse(value.ToString(), culture, DateTimeStyles.RoundtripKind);
            if (nonNullableType == typeof(TimeSpan))
                return TimeSpan.Parse(value.ToString(), culture);

            return Convert.ChangeType(value, nonNullableType, culture);
        }

        private static CultureInfo ResolveCulture(string cultureName)
        {
            if (string.IsNullOrWhiteSpace(cultureName))
                return CultureInfo.InvariantCulture;
            try
            {
                return CultureInfo.GetCultureInfo(cultureName);
            }
            catch
            {
                return CultureInfo.InvariantCulture;
            }
        }

        private static string BuildEntityKey(string destinationDataSource, string destinationEntity)
        {
            return $"{destinationDataSource ?? string.Empty}::{destinationEntity ?? string.Empty}";
        }

        private static string BuildFieldKey(string destinationDataSource, string destinationEntity, string destinationField)
        {
            return $"{BuildEntityKey(destinationDataSource, destinationEntity)}::{destinationField ?? string.Empty}";
        }

        private static ConcurrentDictionary<string, Func<object, string, object>> CreateDefaultTransformRegistry()
        {
            var registry = new ConcurrentDictionary<string, Func<object, string, object>>(StringComparer.OrdinalIgnoreCase);

            registry["trim"] = (value, _) => value is string text ? text.Trim() : value;
            registry["upper"] = (value, _) => value is string text ? text.ToUpperInvariant() : value;
            registry["lower"] = (value, _) => value is string text ? text.ToLowerInvariant() : value;
            registry["regex_replace"] = (value, argument) =>
            {
                if (value is not string text || string.IsNullOrWhiteSpace(argument))
                    return value;

                var args = argument.Split(new[] { "=>" }, StringSplitOptions.None);
                if (args.Length != 2)
                    return value;

                return Regex.Replace(text, args[0], args[1]);
            };

            return registry;
        }
    }
}
