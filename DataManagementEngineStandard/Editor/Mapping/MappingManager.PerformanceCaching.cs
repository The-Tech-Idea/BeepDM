using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Core;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Workflow.Mapping;

namespace TheTechIdea.Beep.Editor.Mapping
{
    public static partial class MappingManager
    {
        private const int MaxCompiledPlanCacheEntries = 512;
        private const int MaxAccessorCacheEntries = 2048;

        private static readonly ConcurrentDictionary<string, CompiledMappingPlan> CompiledPlanCache = new(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<string, Func<object, object>> SourcePathAccessorCache = new(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<string, Action<object, object>> DestinationSetterCache = new(StringComparer.OrdinalIgnoreCase);

        public static void InvalidateMappingCaches(string destinationDataSource, string destinationEntity)
        {
            var prefix = BuildCachePrefix(destinationDataSource, destinationEntity);
            RemoveByPrefix(CompiledPlanCache, prefix);
            RemoveByPrefix(DestinationSetterCache, prefix);
        }

        private static bool ExecuteCompiledPlan(
            IDMEEditor editor,
            object source,
            object destination,
            string destinationDataSource,
            string destinationEntity,
            EntityDataMap_DTL selectedMapping)
        {
            var plan = GetOrBuildCompiledPlan(destination.GetType(), destinationDataSource, destinationEntity, selectedMapping);
            if (plan == null || plan.Steps.Count == 0)
                return false;

            plan.Hits++;
            foreach (var step in plan.Steps)
            {
                try
                {
                    var trace = ResolveRuleExecutionTrace(source, step.Mapping, out var rule);
                    if (trace.Decision == MappingRuleDecision.Skip)
                        continue;

                    object sourceValue = null;
                    if (trace.Decision == MappingRuleDecision.UseDefault)
                    {
                        sourceValue = rule.NullDefaultSpecified ? rule.NullDefaultValue : null;
                    }
                    else
                    {
                        sourceValue = ResolveSourceValueFast(source, rule.SourcePath);
                        if (sourceValue == null && rule.NullDefaultSpecified)
                            sourceValue = rule.NullDefaultValue;
                    }

                    var policy = GetConversionPolicy(destinationDataSource, destinationEntity);
                    var configuredChain = GetFieldTransformChain(destinationDataSource, destinationEntity, step.Mapping.ToFieldName);
                    var effectiveChain = MergeTransformChains(step.Mapping.ToFieldName, rule.ExplicitTransforms, configuredChain);
                    var conversion = ApplyConversionPipeline(sourceValue, step.TargetType, policy, effectiveChain);

                    if (conversion.Skipped)
                        continue;
                    if (!conversion.Success && policy.FailureMode == MappingConversionFailureMode.Reject)
                        throw new InvalidOperationException(conversion.Message);

                    step.Setter(destination, conversion.Value);
                }
                catch (Exception ex)
                {
                    editor.AddLogMessage("MappingError", $"Compiled mapping failed for '{step.Mapping?.ToFieldName}': {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                }
            }

            return true;
        }

        private static object ResolveSourceValueFast(object source, string sourcePath)
        {
            if (source == null || string.IsNullOrWhiteSpace(sourcePath))
                return null;

            var normalizedPath = sourcePath.StartsWith("Source.", StringComparison.OrdinalIgnoreCase)
                ? sourcePath.Substring("Source.".Length)
                : sourcePath;
            var key = $"{source.GetType().AssemblyQualifiedName}::{normalizedPath}";
            var accessor = SourcePathAccessorCache.GetOrAdd(key, _ => BuildSourcePathAccessor(source.GetType(), normalizedPath));
            EnsureAccessorCacheBounded();
            return accessor(source);
        }

        private static Func<object, object> BuildSourcePathAccessor(Type sourceType, string path)
        {
            var members = new List<MemberInfo>();
            var currentType = sourceType;
            foreach (var segment in SplitPath(path))
            {
                var member = GetMember(currentType, segment);
                if (member == null)
                    return _ => null;
                members.Add(member);
                currentType = GetMemberType(member);
            }

            return instance =>
            {
                var current = instance;
                foreach (var member in members)
                {
                    if (current == null)
                        return null;
                    current = GetMemberValue(current, member);
                }
                return current;
            };
        }

        private static CompiledMappingPlan GetOrBuildCompiledPlan(
            Type destinationType,
            string destinationDataSource,
            string destinationEntity,
            EntityDataMap_DTL selectedMapping)
        {
            if (selectedMapping == null)
                return null;

            var key = BuildPlanCacheKey(destinationType, destinationDataSource, destinationEntity, selectedMapping);
            var signature = ComputeMappingSignature(selectedMapping);

            if (CompiledPlanCache.TryGetValue(key, out var existing) && string.Equals(existing.MappingSignature, signature, StringComparison.Ordinal))
                return existing;

            var steps = new List<CompiledMappingStep>();
            foreach (var mapping in selectedMapping.FieldMapping ?? new List<Mapping_rep_fields>())
            {
                if (mapping == null || string.IsNullOrWhiteSpace(mapping.ToFieldName))
                    continue;

                var targetProperty = destinationType.GetProperty(mapping.ToFieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                if (targetProperty == null || !targetProperty.CanWrite)
                    continue;

                var setter = GetDestinationSetter(destinationType, destinationDataSource, destinationEntity, targetProperty);
                if (setter == null)
                    continue;

                steps.Add(new CompiledMappingStep
                {
                    Mapping = mapping,
                    TargetType = targetProperty.PropertyType,
                    Setter = setter
                });
            }

            if (steps.Count == 0)
                return null;

            var plan = new CompiledMappingPlan
            {
                CacheKey = key,
                MappingSignature = signature,
                DestinationEntity = destinationEntity ?? string.Empty,
                DestinationDataSource = destinationDataSource ?? string.Empty,
                DestinationTypeName = destinationType.AssemblyQualifiedName ?? destinationType.FullName ?? string.Empty,
                CreatedOnUtc = DateTime.UtcNow,
                Steps = steps
            };

            CompiledPlanCache[key] = plan;
            EnsurePlanCacheBounded();
            return plan;
        }

        private static Action<object, object> GetDestinationSetter(
            Type destinationType,
            string destinationDataSource,
            string destinationEntity,
            PropertyInfo property)
        {
            var key = $"{BuildCachePrefix(destinationDataSource, destinationEntity)}::{destinationType.AssemblyQualifiedName}::{property.Name}";
            return DestinationSetterCache.GetOrAdd(key, _ =>
            {
                return (target, value) =>
                {
                    property.SetValue(target, value);
                };
            });
        }

        private static string BuildPlanCacheKey(
            Type destinationType,
            string destinationDataSource,
            string destinationEntity,
            EntityDataMap_DTL selectedMapping)
        {
            return $"{BuildCachePrefix(destinationDataSource, destinationEntity)}::{destinationType.AssemblyQualifiedName}::{selectedMapping.EntityDataSource}::{selectedMapping.EntityName}";
        }

        private static string BuildCachePrefix(string destinationDataSource, string destinationEntity)
        {
            return $"{destinationDataSource ?? string.Empty}::{destinationEntity ?? string.Empty}";
        }

        private static string ComputeMappingSignature(EntityDataMap_DTL selectedMapping)
        {
            var fields = selectedMapping?.FieldMapping ?? new List<Mapping_rep_fields>();
            return string.Join("|", fields.Select(item =>
                $"{item?.FromFieldName}>{item?.ToFieldName}:{item?.FromFieldType}>{item?.ToFieldType}:{item?.Rules}"));
        }

        private static void EnsurePlanCacheBounded()
        {
            if (CompiledPlanCache.Count <= MaxCompiledPlanCacheEntries)
                return;

            var removeCount = Math.Max(1, MaxCompiledPlanCacheEntries / 4);
            foreach (var key in CompiledPlanCache.Values
                         .OrderBy(item => item.CreatedOnUtc)
                         .ThenBy(item => item.Hits)
                         .Take(removeCount)
                         .Select(item => item.CacheKey)
                         .ToList())
            {
                CompiledPlanCache.TryRemove(key, out _);
            }
        }

        private static void EnsureAccessorCacheBounded()
        {
            if (SourcePathAccessorCache.Count <= MaxAccessorCacheEntries && DestinationSetterCache.Count <= MaxAccessorCacheEntries)
                return;

            var removeCount = Math.Max(1, MaxAccessorCacheEntries / 4);
            foreach (var key in SourcePathAccessorCache.Keys.Take(removeCount).ToList())
                SourcePathAccessorCache.TryRemove(key, out _);
            foreach (var key in DestinationSetterCache.Keys.Take(removeCount).ToList())
                DestinationSetterCache.TryRemove(key, out _);
        }

        private static void RemoveByPrefix<T>(ConcurrentDictionary<string, T> cache, string prefix)
        {
            foreach (var key in cache.Keys.Where(key => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList())
                cache.TryRemove(key, out _);
        }

        private sealed class CompiledMappingPlan
        {
            public string CacheKey { get; set; } = string.Empty;
            public string MappingSignature { get; set; } = string.Empty;
            public string DestinationDataSource { get; set; } = string.Empty;
            public string DestinationEntity { get; set; } = string.Empty;
            public string DestinationTypeName { get; set; } = string.Empty;
            public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;
            public long Hits { get; set; }
            public List<CompiledMappingStep> Steps { get; set; } = new List<CompiledMappingStep>();
        }

        private sealed class CompiledMappingStep
        {
            public Mapping_rep_fields Mapping { get; set; } = new Mapping_rep_fields();
            public Type TargetType { get; set; } = typeof(object);
            public Action<object, object> Setter { get; set; } = (_, _) => { };
        }
    }
}
