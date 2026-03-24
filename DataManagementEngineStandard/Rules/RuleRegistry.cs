using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Rules
{
    /// <summary>
    /// Discovers <see cref="IRule"/> and <see cref="IRuleParser"/> implementations from
    /// assemblies already scanned by <c>AssemblyHandler</c> and wires them into a
    /// <see cref="IRuleEngine"/> / <see cref="IRuleParserFactory"/> pair.
    ///
    /// Mirrors the pattern used by <c>PipelinePluginRegistry</c> — all extension loading
    /// flows through <c>IDMEEditor.assemblyHandler.ConfigEditor</c> so external plug-in
    /// assemblies are automatically picked up with zero extra wiring.
    ///
    /// Typical usage:
    /// <code>
    ///   var registry = new RuleRegistry(editor);
    ///   registry.Discover();                    // scan already-loaded assemblies
    ///   var engine  = registry.Engine;
    ///   var factory = registry.ParserFactory;
    /// </code>
    /// </summary>
    public sealed class RuleRegistry
    {
        private readonly IDMEEditor _editor;

        // ── public entry-points ──────────────────────────────────────────────────
        /// <summary>The engine populated by <see cref="Discover"/>.</summary>
        public IRuleEngine Engine { get; }

        /// <summary>The parser factory populated by <see cref="Discover"/>.</summary>
        public IRuleParserFactory ParserFactory { get; }

        // ── descriptor caches ────────────────────────────────────────────────────
        private readonly Dictionary<string, RuleDescriptor>       _rules   = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, RuleParserDescriptor> _parsers = new(StringComparer.OrdinalIgnoreCase);

        public RuleRegistry(IDMEEditor editor)
            : this(editor, null, null) { }

        public RuleRegistry(IDMEEditor editor, IRuleEngine engine, IRuleParserFactory parserFactory)
        {
            _editor      = editor      ?? throw new ArgumentNullException(nameof(editor));
            ParserFactory = parserFactory ?? new RuleParserFactory();
            Engine        = engine        ?? new RuleEngine(ParserFactory.HasParser("RulesParser")
                                                ? ParserFactory.GetParser("RulesParser")
                                                : new RuleParser());
        }

        // ── discovery ────────────────────────────────────────────────────────────

        /// <summary>
        /// Scans both <c>ConfigEditor.RuleParserClasses</c> and <c>ConfigEditor.Rules</c>,
        /// registering every discovered parser into <see cref="ParserFactory"/> and every
        /// discovered rule into <see cref="Engine"/>.
        ///
        /// Safe to call multiple times — already-registered keys are skipped (not replaced).
        /// </summary>
        public void Discover()
        {
            _editor.ErrorObject.Flag = Errors.Ok;
            try
            {
                DiscoverParsers();
                DiscoverRules();
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage(nameof(Discover),
                    $"Rule registry discovery failed: {ex.Message}",
                    DateTime.Now, -1, null, Errors.Failed);
            }
        }

        // ── parsers ───────────────────────────────────────────────────────────────

        private void DiscoverParsers()
        {
            foreach (var cls in _editor.assemblyHandler.ConfigEditor.RuleParserClasses)
            {
                if (cls.type == null || !cls.IsRuleParser) continue;

                string key = cls.RuleParserKey;
                if (string.IsNullOrWhiteSpace(key)) continue;
                if (_parsers.ContainsKey(key)) continue;            // already discovered

                try
                {
                    if (!(Activator.CreateInstance(cls.type) is IRuleParser instance))
                    {
                        _editor.AddLogMessage(nameof(DiscoverParsers),
                            $"Type '{cls.type.FullName}' does not implement IRuleParser.",
                            DateTime.Now, -1, null, Errors.Failed);
                        continue;
                    }

                    _parsers[key] = new RuleParserDescriptor(
                        key,
                        cls.RootName ?? cls.className,
                        cls.Version  ?? "1.0.0",
                        cls.type,
                        instance);

                    if (!ParserFactory.HasParser(key))
                        ParserFactory.RegisterParser(key, instance);
                }
                catch (Exception ex)
                {
                    _editor.AddLogMessage(nameof(DiscoverParsers),
                        $"Failed to instantiate parser '{cls.type.FullName}': {ex.Message}",
                        DateTime.Now, -1, null, Errors.Failed);
                }
            }
        }

        // ── rules ─────────────────────────────────────────────────────────────────

        private void DiscoverRules()
        {
            foreach (var cls in _editor.assemblyHandler.ConfigEditor.Rules)
            {
                if (cls.type == null || !cls.IsRule) continue;

                string key = cls.RuleKey;
                if (string.IsNullOrWhiteSpace(key)) continue;
                if (_rules.ContainsKey(key)) continue;              // already discovered

                try
                {
                    // Determine which parser to use (from [RuleAttribute] or fall back to default)
                    var ruleAttr = cls.type.GetCustomAttribute<RuleAttribute>(false);
                    string parserKey = ruleAttr?.ParserKey ?? "RulesParser";

                    // Instantiate — rules can optionally accept IDMEEditor
                    IRule instance = TryCreateRule(cls.type);
                    if (instance == null)
                    {
                        _editor.AddLogMessage(nameof(DiscoverRules),
                            $"Type '{cls.type.FullName}' does not implement IRule or could not be instantiated.",
                            DateTime.Now, -1, null, Errors.Failed);
                        continue;
                    }

                    _rules[key] = new RuleDescriptor(key, cls.RootName ?? cls.className,
                        cls.Version ?? "1.0.0", parserKey, cls.type, instance);

                    if (!Engine.HasRule(key))
                        Engine.RegisterRule(instance);
                }
                catch (Exception ex)
                {
                    _editor.AddLogMessage(nameof(DiscoverRules),
                        $"Failed to register rule '{key}' ({cls.type?.FullName}): {ex.Message}",
                        DateTime.Now, -1, null, Errors.Failed);
                }
            }
        }

        private IRule TryCreateRule(Type type)
        {
            // 1. Try (IDMEEditor) constructor
            var editorCtor = type.GetConstructor(new[] { typeof(IDMEEditor) });
            if (editorCtor != null)
                return editorCtor.Invoke(new object[] { _editor }) as IRule;

            // 2. Try parameterless constructor
            var defaultCtor = type.GetConstructor(Type.EmptyTypes);
            if (defaultCtor != null)
                return defaultCtor.Invoke(null) as IRule;

            return null;
        }

        // ── query ─────────────────────────────────────────────────────────────────

        /// <summary>Snapshot of all discovered rule descriptors.</summary>
        public IReadOnlyList<RuleDescriptor> GetAllRules() =>
            _rules.Values.ToList().AsReadOnly();

        /// <summary>Snapshot of all discovered parser descriptors.</summary>
        public IReadOnlyList<RuleParserDescriptor> GetAllParsers() =>
            _parsers.Values.ToList().AsReadOnly();

        /// <summary>Returns true if a rule with <paramref name="ruleKey"/> was discovered.</summary>
        public bool ContainsRule(string ruleKey) =>
            !string.IsNullOrWhiteSpace(ruleKey) && _rules.ContainsKey(ruleKey);

        /// <summary>Returns true if a parser with <paramref name="parserKey"/> was discovered.</summary>
        public bool ContainsParser(string parserKey) =>
            !string.IsNullOrWhiteSpace(parserKey) && _parsers.ContainsKey(parserKey);

        /// <summary>Number of discovered rules.</summary>
        public int RuleCount => _rules.Count;

        /// <summary>Number of discovered parsers.</summary>
        public int ParserCount => _parsers.Count;
    }

    // ── descriptor records ────────────────────────────────────────────────────────

    /// <summary>Describes a discovered <see cref="IRule"/> plug-in.</summary>
    public sealed record RuleDescriptor(
        string   RuleKey,
        string   DisplayName,
        string   Version,
        string   ParserKey,
        Type     ImplementationType,
        IRule    Instance);

    /// <summary>Describes a discovered <see cref="IRuleParser"/> plug-in.</summary>
    public sealed record RuleParserDescriptor(
        string      ParserKey,
        string      DisplayName,
        string      Version,
        Type        ImplementationType,
        IRuleParser Instance);
}
