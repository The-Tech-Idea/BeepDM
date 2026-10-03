using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Defaults.Interfaces;
using TheTechIdea.Beep.Editor.Defaults.RuleParsing;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Editor.Importing;

namespace TheTechIdea.Beep.Editor.Defaults.Resolvers
{
    /// <summary>
    /// Manager for default value resolvers with extensible resolver registration.
    /// Enhanced to support Dictionary parameters, better resolver management,
    /// and Phase-1 rule normalization (dot-style DSL + legacy function-style).
    /// </summary>
    public class DefaultValueResolverManager : IDefaultValueResolverManager
    {
        private readonly Dictionary<string, IDefaultValueResolver> _resolvers;
        private readonly Dictionary<string, int>                    _priorities;
        private readonly Dictionary<string, ParsedRule>             _ruleCache;

        // ── Phase 6: Value result cache ──────────────────────────────────────
        // Key: "<normalizedRule>\0<contextHash>"  —  only populated for deterministic/cacheable resolvers.
        // Size is bounded by _valueCacheMaxSize; entries are evicted by insertion order (FIFO).
        private readonly Dictionary<string, object>    _valueCache;
        private readonly Queue<string>                 _valueCacheKeys;
        private const    int                           _valueCacheMaxSize = 512;

        private readonly IDMEEditor _editor;
        private readonly object _resolverGate = new();

        public DefaultValueResolverManager(IDMEEditor editor) : this(editor, requiredInitialization: false) { }

        internal DefaultValueResolverManager(IDMEEditor editor, bool requiredInitialization)
        {
            _editor     = editor ?? throw new ArgumentNullException(nameof(editor));
            _resolvers   = new Dictionary<string, IDefaultValueResolver>(StringComparer.OrdinalIgnoreCase);
            _priorities  = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            _ruleCache   = new Dictionary<string, ParsedRule>(StringComparer.OrdinalIgnoreCase);
            _valueCache  = new Dictionary<string, object>(StringComparer.Ordinal);
            _valueCacheKeys = new Queue<string>();

            // Register built-in resolvers
            RegisterBuiltInResolvers(requiredInitialization);
        }

        public void RegisterResolver(IDefaultValueResolver resolver)
        {
            // Honour self-declared priority from IResolverCapabilities, fall back to 100.
            int selfPriority = resolver is IResolverCapabilities caps ? caps.Priority : 100;
            RegisterResolver(resolver, selfPriority);
        }

        /// <summary>[Phase 4] Register with explicit priority — lower values win.</summary>
        public void RegisterResolver(IDefaultValueResolver resolver, int priority)
        {
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));

            var name = resolver.ResolverName;
            RegisterResolverCore(name, resolver, priority);
            _editor.AddLogMessage("DefaultValueResolverManager",
                $"Registered resolver '{resolver.ResolverName}' at priority {priority}", DateTime.Now, -1, "", Errors.Ok);
        }

        private void RegisterResolverCore(string name, IDefaultValueResolver resolver, int priority)
        {
            lock (_resolverGate)
            {
                _resolvers[name] = resolver;
                _priorities[name] = priority;
            }
        }

        public void UnregisterResolver(string resolverName)
        {
            bool removed;
            lock (_resolverGate)
            {
                removed = _resolvers.Remove(resolverName);
                _priorities.Remove(resolverName);
            }
            if (removed)
            {
                _editor.AddLogMessage("DefaultValueResolverManager", 
                    $"Unregistered resolver '{resolverName}'", DateTime.Now, -1, "", Errors.Ok);
            }
        }

        public object ResolveValue(string rule, IPassedArgs parameters)
        {
            if (RequiredDefaultResolution.Current is { } required)
                return DefaultsManager.GetRequiredResolverRegistry(_editor).Resolve(rule, parameters, required.Token);
            if (string.IsNullOrWhiteSpace(rule))
                return null;

            // Short-circuit: plain literals (no ':' prefix) are returned as-is.
            var quickParse = RuleNormalizer.Normalize(rule);
            if (quickParse.IsLiteral)
                return quickParse.NormalizedRule;

            // Phase-1: normalize through the rule-parsing pipeline.
            // Dot-style rules are converted to legacy function style before routing.
            var routedRule = RuleNormalizer.GetNormalizedRule(rule, out var diagMsg);
            if (diagMsg != null)
            {
                _editor.AddLogMessage("DefaultValueResolverManager",
                    $"Rule normalization warnings for '{rule}': {diagMsg}", DateTime.Now, -1, "", Errors.Warning);
            }

            var resolver = GetResolverForRule(routedRule);
            if (resolver == null)
            {
                _editor.AddLogMessage("DefaultValueResolverManager",
                    $"No resolver found for rule '{rule}' (normalized: '{routedRule}')", DateTime.Now, -1, "", Errors.Failed);
                return null;
            }

            // ── Phase 6: value result cache ───────────────────────────────────
            bool cacheable = resolver is IResolverCapabilities rc && rc.SupportsCaching;
            string cacheKey = cacheable ? BuildCacheKey(routedRule, parameters) : null;

            if (cacheKey != null && _valueCache.TryGetValue(cacheKey, out var cached))
                return cached;

            try
            {
                var value = resolver.ResolveValue(routedRule, parameters);

                if (cacheKey != null)
                    StoreInValueCache(cacheKey, value);

                return value;
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("DefaultValueResolverManager",
                    $"Error resolving rule '{rule}': {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                return null;
            }
        }

        /// <summary>
        /// Resolves a value after normalization. Implements <see cref="IDefaultValueResolverManagerV2"/>.
        /// Identical to <see cref="ResolveValue(string, IPassedArgs)"/> — exposed explicitly
        /// so callers that hold the V2 interface can call it by name.
        /// </summary>
        public object ResolveValueWithNormalization(string rule, IPassedArgs parameters)
            => ResolveValue(rule, parameters);

        /// <summary>
        /// Returns the <see cref="ParsedRule"/> produced by the normalization pipeline
        /// without executing resolution (useful for validators and tooling).
        /// Implements <see cref="IDefaultValueResolverManagerV2"/>.
        /// </summary>
        public ParsedRule ParseRule(string rule)
            => RuleNormalizer.Normalize(rule);

        /// <summary>
        /// Enhanced ResolveValue method that accepts Dictionary parameters for better flexibility.
        /// Rule normalization (dot-style DSL→legacy) is applied before routing.
        /// </summary>
        public object ResolveValue(string rule, Dictionary<string, object> parameters)
        {
            if (RequiredDefaultResolution.Current is { } required)
                return DefaultsManager.GetRequiredResolverRegistry(_editor).Resolve(rule, ConvertToPassedArgs(parameters), required.Token);
            if (string.IsNullOrWhiteSpace(rule))
                return null;

            var routedRule = RuleNormalizer.GetNormalizedRule(rule, out var diagMsg);
            if (diagMsg != null)
            {
                _editor.AddLogMessage("DefaultValueResolverManager",
                    $"Rule normalization warnings for '{rule}': {diagMsg}", DateTime.Now, -1, "", Errors.Warning);
            }

            var resolver = GetResolverForRule(routedRule);
            if (resolver == null)
            {
                _editor.AddLogMessage("DefaultValueResolverManager",
                    $"No resolver found for rule '{rule}' (normalized: '{routedRule}')", DateTime.Now, -1, "", Errors.Failed);
                return null;
            }

            var passedArgs = ConvertToPassedArgs(parameters);

            // ── Phase 6: value result cache ─────────────────────────────────────────
            bool cacheable = resolver is IResolverCapabilities rc2 && rc2.SupportsCaching;
            string cacheKey2 = cacheable ? BuildCacheKey(routedRule, passedArgs) : null;

            if (cacheKey2 != null && _valueCache.TryGetValue(cacheKey2, out var cached2))
                return cached2;

            try
            {
                var value = resolver.ResolveValue(routedRule, passedArgs);

                if (cacheKey2 != null)
                    StoreInValueCache(cacheKey2, value);

                return value;
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("DefaultValueResolverManager",
                    $"Error resolving rule '{rule}': {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                return null;
            }
        }

        public IReadOnlyDictionary<string, IDefaultValueResolver> GetResolvers()
        {
            lock (_resolverGate) return new Dictionary<string, IDefaultValueResolver>(_resolvers, StringComparer.OrdinalIgnoreCase);
        }

        public IDefaultValueResolver GetResolverForRule(string rule)
        {
            if (RequiredDefaultResolution.Current != null)
                return DefaultsManager.GetRequiredResolverRegistry(_editor).Select(rule);
            return CaptureRequiredRegistry().Select(rule);
        }

        internal RequiredResolverRegistry CaptureRequiredRegistry()
        {
            (IDefaultValueResolver Resolver, int Priority)[] snapshot;
            lock (_resolverGate)
                snapshot = _resolvers.Select(kvp => (kvp.Value, _priorities.TryGetValue(kvp.Key, out var p) ? p : 100)).ToArray();
            // Custom callbacks run outside registry locks.
            return new RequiredResolverRegistry(_editor, this,
                snapshot.OrderBy(item => item.Priority).Select(item => item.Resolver).ToArray());
        }

        internal object ResolveRequired(string rule, IPassedArgs parameters, CancellationToken token, RequiredResolverRegistry registry)
        {
            using var scope = new RequiredDefaultResolution(token, registry);
            try
            {
                token.ThrowIfCancellationRequested();
                if (scope.Depth > 32 || string.IsNullOrWhiteSpace(rule) || rule.Length > 16384)
                    throw new ImportTransformationException(ImportTransformationStage.Defaults);
                var parsed = RuleNormalizer.NormalizeRequired(rule);
                if (!parsed.IsValid || string.IsNullOrWhiteSpace(parsed.NormalizedRule))
                    throw new ImportTransformationException(ImportTransformationStage.Defaults);
                if (parsed.IsLiteral) return parsed.NormalizedRule;
                if (!RequiredDefaultResolution.HasValidEnvelope(parsed.NormalizedRule))
                    throw new ImportTransformationException(ImportTransformationStage.Defaults);
                var resolver = registry.Select(parsed.NormalizedRule);
                if (resolver == null || scope.Failed)
                    throw new ImportTransformationException(ImportTransformationStage.Defaults);
                RequiredBuiltInRule.Validate(resolver, parsed.NormalizedRule);
                // Required rows cannot share the legacy metadata-only result cache.
                var value = resolver.ResolveValue(parsed.NormalizedRule, parameters);
                token.ThrowIfCancellationRequested();
                if (value == null || scope.Failed || value is double d && !double.IsFinite(d) || value is float f && !float.IsFinite(f))
                    throw new ImportTransformationException(ImportTransformationStage.Defaults);
                return value;
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                RequiredDefaultResolution.Report(failure: true);
                token.ThrowIfCancellationRequested();
                throw new ImportTransformationException(ImportTransformationStage.Defaults, ex.GetType().Name);
            }
        }

        /// <summary>[Phase 5] Resolve with full telemetry — timing, resolver name, fingerprint.</summary>
        public ResolverExecutionResult ResolveWithTelemetry(string rule, IPassedArgs parameters)
        {
            var sw      = Stopwatch.StartNew();
            if (RequiredDefaultResolution.Current is { } required)
            {
                try
                {
                    var value = DefaultsManager.GetRequiredResolverRegistry(_editor).Resolve(rule, parameters, required.Token);
                    return new ResolverExecutionResult { Succeeded = true, ResolvedValue = value, Duration = sw.Elapsed };
                }
                catch (OperationCanceledException) when (required.Token.IsCancellationRequested) { throw; }
                catch (Exception)
                {
                    RequiredDefaultResolution.Report(failure: true);
                    return new ResolverExecutionResult { Succeeded = false, ErrorMessage = "Required defaults resolution failed.", Duration = sw.Elapsed };
                }
            }
            var normalized = RuleNormalizer.GetNormalizedRule(rule, out _);
            var fingerprint = ResolverExecutionResult.ComputeFingerprint(normalized);

            try
            {
                var resolver = GetResolverForRule(normalized);
                if (resolver == null)
                {
                    return new ResolverExecutionResult
                    {
                        ResolverName   = "none",
                        OriginalRule   = rule,
                        NormalizedRule = normalized,
                        Succeeded      = false,
                        FallbackUsed   = false,
                        Duration       = sw.Elapsed,
                        ErrorMessage   = $"No resolver found for rule '{rule}'",
                        RuleFingerprint = fingerprint
                    };
                }

                var value = resolver.ResolveValue(normalized, parameters);
                return new ResolverExecutionResult
                {
                    ResolverName    = resolver.ResolverName,
                    OriginalRule    = rule,
                    NormalizedRule  = normalized,
                    ResolvedValue   = value,
                    Succeeded       = true,
                    FallbackUsed    = false,
                    Duration        = sw.Elapsed,
                    RuleFingerprint = fingerprint
                };
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("DefaultValueResolverManager",
                    $"ResolveWithTelemetry error for rule '{rule}': {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                return new ResolverExecutionResult
                {
                    ResolverName    = "error",
                    OriginalRule    = rule,
                    NormalizedRule  = normalized,
                    Succeeded       = false,
                    Duration        = sw.Elapsed,
                    ErrorMessage    = ex.Message,
                    RuleFingerprint = fingerprint
                };
            }
        }

        /// <summary>[Phase 6] Pre-parse and cache a rule to eliminate parse overhead on first use.</summary>
        public void CompileRule(string rule)
        {
            if (string.IsNullOrWhiteSpace(rule)) return;
            if (_ruleCache.ContainsKey(rule)) return;
            try
            {
                var parsed = RuleNormalizer.Normalize(rule);
                _ruleCache[rule] = parsed;
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("DefaultValueResolverManager",
                    $"CompileRule error for '{rule}': {ex.Message}", DateTime.Now, -1, "", Errors.Warning);
            }
        }

        /// <summary>
        /// [Phase 6] Clears the value result cache entirely.  Call when datasource connections
        /// or configuration change to ensure stale cached values are not returned.
        /// </summary>
        public void InvalidateValueCache()
        {
            _valueCache.Clear();
            _valueCacheKeys.Clear();
            _editor.AddLogMessage("DefaultValueResolverManager",
                "Value result cache invalidated.", DateTime.Now, -1, "", Errors.Ok);
        }

        // ── Phase 6 private cache helpers ─────────────────────────────────────

        /// <summary>
        /// Builds a stable, short cache key from the normalized rule and a lightweight
        /// context hash derived from datasource name + entity name + field name.
        /// Using only metadata — not record values — keeps the key stable across rows.
        /// </summary>
        private static string BuildCacheKey(string normalizedRule, IPassedArgs parameters)
        {
            // FNV-1a over the context identifiers that affect deterministic resolution.
            uint h = 2166136261u;
            void Mix(string s)
            {
                if (s == null) return;
                foreach (char c in s) { h ^= (uint)c; h *= 16777619u; }
                h ^= (uint)'|'; h *= 16777619u;
            }
            Mix(parameters?.DatasourceName);
            Mix(parameters?.CurrentEntity);
            Mix(parameters?.ParameterString1);
            return normalizedRule + "\0" + h.ToString("x8");
        }

        private void StoreInValueCache(string key, object value)
        {
            if (_valueCacheKeys.Count >= _valueCacheMaxSize)
            {
                // FIFO eviction — remove oldest entry.
                var oldest = _valueCacheKeys.Dequeue();
                _valueCache.Remove(oldest);
            }
            _valueCache[key] = value;
            _valueCacheKeys.Enqueue(key);
        }

        private void RegisterBuiltInResolvers(bool requiredInitialization)
        {
            try
            {
                var builtIns = new IDefaultValueResolver[]
                {
                    new DateTimeResolver(_editor), new UserContextResolver(_editor), new SystemInfoResolver(_editor),
                    new GuidResolver(_editor), new FormulaResolver(_editor), new DataSourceResolver(_editor),
                    new ObjectPropertyResolver(_editor), new ConfigurationResolver(_editor),
                    new EnvironmentResolver(_editor), new ExpressionResolver(_editor)
                };
                foreach (var resolver in builtIns)
                {
                    if (requiredInitialization)
                        RegisterResolverCore(resolver.ResolverName, resolver, resolver is IResolverCapabilities caps ? caps.Priority : 100);
                    else RegisterResolver(resolver);
                }

                if (requiredInitialization) return;

                _editor.AddLogMessage("DefaultValueResolverManager", 
                    $"Registered {_resolvers.Count} built-in resolvers", DateTime.Now, -1, "", Errors.Ok);
            }
            catch (Exception ex)
            {
                if (requiredInitialization) throw new DefaultCatalogReadException();
                _editor.AddLogMessage("DefaultValueResolverManager", 
                    $"Error registering built-in resolvers: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
            }
        }

        /// <summary>
        /// Converts Dictionary parameters to IPassedArgs for backward compatibility
        /// </summary>
        /// <param name="parameters">Dictionary parameters</param>
        /// <returns>IPassedArgs instance</returns>
        private IPassedArgs ConvertToPassedArgs(Dictionary<string, object> parameters)
        {
            if (parameters == null)
                return null;

            var passedArgs = new PassedArgs();

            // Map common parameters
            if (parameters.TryGetValue("DataSource", out var dataSource) && dataSource is IDataSource ds)
                passedArgs.DataSource = ds;

            if (parameters.TryGetValue("CurrentEntity", out var entity) && entity is string entityName)
                passedArgs.CurrentEntity = entityName;

            if (parameters.TryGetValue("DatasourceName", out var dsName) && dsName is string dataSourceName)
                passedArgs.DatasourceName = dataSourceName;

            if (parameters.TryGetValue("Record", out var record))
            {
                if (record != null)
                {
                    // Convert to List<ObjectItem> as expected by IPassedArgs
                    passedArgs.Objects = new List<ObjectItem>
                    {
                        new ObjectItem { obj = record, Name = "Record" }
                    };
                }
                else
                {
                    passedArgs.Objects = new List<ObjectItem>();
                }
            }

            if (parameters.TryGetValue("FieldName", out var FieldName) && FieldName is string field)
                passedArgs.ParameterString1 = field;

            if (parameters.TryGetValue("Parameters", out var additionalParams) && additionalParams is Dictionary<string, object> additionalDict)
            {
                // Store additional parameters in available fields
                var keys = additionalDict.Keys.ToArray();
                if (keys.Length > 0 && additionalDict.TryGetValue(keys[0], out var param1))
                {
                    passedArgs.ParameterString2 = param1?.ToString();
                }
                if (keys.Length > 1 && additionalDict.TryGetValue(keys[1], out var param2))
                {
                    passedArgs.ParameterString3 = param2?.ToString();
                }
            }

            return passedArgs;
        }

        /// <summary>
        /// Gets resolver statistics and health information
        /// </summary>
        /// <returns>Dictionary with resolver statistics</returns>
        public Dictionary<string, object> GetResolverStatistics()
        {
            var stats = new Dictionary<string, object>
            {
                ["TotalResolvers"] = _resolvers.Count,
                ["ResolverNames"] = _resolvers.Keys.ToList(),
                ["RegisteredAt"] = DateTime.Now
            };

            // Group resolvers by type
            var resolverTypes = new Dictionary<string, int>();
            foreach (var resolver in _resolvers.Values)
            {
                var type = resolver.GetType().Name.Replace("Resolver", "");
                resolverTypes[type] = resolverTypes.ContainsKey(type) ? resolverTypes[type] + 1 : 1;
            }
            stats["ResolverTypes"] = resolverTypes;

            return stats;
        }

        /// <summary>
        /// Tests if a rule can be resolved without actually resolving it
        /// </summary>
        /// <param name="rule">Rule to test</param>
        /// <returns>True if the rule can be handled</returns>
        public bool CanResolveRule(string rule)
        {
            if (string.IsNullOrWhiteSpace(rule))
                return false;

            return GetResolverForRule(rule) != null;
        }

        /// <summary>
        /// Gets all examples from all registered resolvers
        /// </summary>
        /// <returns>Dictionary mapping resolver names to their examples</returns>
        public Dictionary<string, IEnumerable<string>> GetAllExamples()
        {
            var examples = new Dictionary<string, IEnumerable<string>>();
            
            foreach (var kvp in _resolvers)
            {
                try
                {
                    examples[kvp.Key] = kvp.Value.GetExamples();
                }
                catch (Exception ex)
                {
                    _editor.AddLogMessage("DefaultValueResolverManager", 
                        $"Error getting examples from resolver '{kvp.Key}': {ex.Message}", DateTime.Now, -1, "", Errors.Warning);
                    examples[kvp.Key] = new[] { $"Error getting examples: {ex.Message}" };
                }
            }

            return examples;
        }

        /// <summary>
        /// Validates that all registered resolvers are functioning correctly
        /// </summary>
        /// <returns>Validation results</returns>
        public Dictionary<string, bool> ValidateResolvers()
        {
            var results = new Dictionary<string, bool>();

            foreach (var kvp in _resolvers)
            {
                try
                {
                    var resolver = kvp.Value;
                    
                    // Basic validation checks
                    var isValid = !string.IsNullOrWhiteSpace(resolver.ResolverName) &&
                                  resolver.SupportedRuleTypes != null &&
                                  resolver.GetExamples() != null;

                    results[kvp.Key] = isValid;

                    if (!isValid)
                    {
                        _editor.AddLogMessage("DefaultValueResolverManager", 
                            $"Resolver '{kvp.Key}' failed validation", DateTime.Now, -1, "", Errors.Warning);
                    }
                }
                catch (Exception ex)
                {
                    results[kvp.Key] = false;
                    _editor.AddLogMessage("DefaultValueResolverManager", 
                        $"Error validating resolver '{kvp.Key}': {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                }
            }

            return results;
        }
    }
}
