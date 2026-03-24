using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TheTechIdea.Beep.Rules
{
    /// <summary>
    /// Rule engine — parses, registers, and evaluates rules.
    /// Supports execution policies (Phase 5), audit events (Phase 5),
    /// and cycle detection (Phase 3).
    /// </summary>
    public partial class RuleEngine : IRuleEngine
    {
        private readonly IRuleParser _parser;
        private readonly Dictionary<string, IRule> _rules = new Dictionary<string, IRule>(StringComparer.OrdinalIgnoreCase);

        public RuleEngine(IRuleParser parser)
        {
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        }

        // ── IRuleEngine: audit ────────────────────────────────────────────────────
        public event EventHandler<RuleAuditEventArgs> RuleEvaluated;

        // ── IRuleEngine: registration ─────────────────────────────────────────────
        public void RegisterRule(IRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            if (string.IsNullOrWhiteSpace(rule.RuleText))
                throw new ArgumentException("Rule must have non-empty RuleText.", nameof(rule));

            string key = rule.RuleText;
            if (_rules.ContainsKey(key))
                throw new RuleCatalogException(DiagnosticCode.DuplicateRuleRegistration,
                    $"A rule with key '{key}' is already registered.");

            _rules[key] = rule;
            EmitAudit(key, true, TimeSpan.Zero, null, null);
        }

        public bool UnregisterRule(string ruleKey)
        {
            if (string.IsNullOrWhiteSpace(ruleKey)) return false;
            return _rules.Remove(ruleKey);
        }

        public bool HasRule(string ruleKey) =>
            !string.IsNullOrWhiteSpace(ruleKey) && _rules.ContainsKey(ruleKey);

        public IEnumerable<string> GetRegisteredKeys() => _rules.Keys.ToList();

        // ── IRuleEngine: solve ────────────────────────────────────────────────────
        public (Dictionary<string, object> outputs, object result) SolveRule(
            string ruleKey, Dictionary<string, object> parameters)
            => SolveRule(ruleKey, parameters, RuleExecutionPolicy.Default);

        public (Dictionary<string, object> outputs, object result) SolveRule(
            string ruleKey, Dictionary<string, object> parameters, RuleExecutionPolicy policy)
        {
            var sw = Stopwatch.StartNew();
            var diags = new List<ParseDiagnostic>();
            try
            {
                var result = SolveRuleInternal(ruleKey, parameters, policy,
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase), 0);
                sw.Stop();
                EmitAudit(ruleKey, true, sw.Elapsed, result.result, diags);
                return result;
            }
            catch
            {
                sw.Stop();
                EmitAudit(ruleKey, false, sw.Elapsed, null, diags);
                throw;
            }
        }

        // ── IRuleEngine: expression evaluation ───────────────────────────────────
        public object EvaluateExpression(IList<Token> tokens, Dictionary<string, object> parameters)
            => EvaluateExpression(tokens, parameters, RuleExecutionPolicy.Default);

        public object EvaluateExpression(IList<Token> tokens, Dictionary<string, object> parameters,
                                         RuleExecutionPolicy policy)
        {
            ValidatePolicyTokens(tokens, policy);
            var rpn = ConvertToRpn(tokens);
            return EvaluateRpn(rpn, parameters, policy,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase), 0);
        }

        // ── Internal solve with recursion guard ───────────────────────────────────
        internal (Dictionary<string, object> outputs, object result) SolveRuleInternal(
            string ruleKey, Dictionary<string, object> parameters,
            RuleExecutionPolicy policy, HashSet<string> callChain, int depth)
        {
            if (depth > policy.MaxDepth)
                throw new RuleEvaluationException(DiagnosticCode.MaxDepthExceeded,
                    $"Rule recursion depth exceeded the policy limit of {policy.MaxDepth}.");

            if (callChain.Contains(ruleKey))
                throw new RuleEvaluationException(DiagnosticCode.CircularRuleReference,
                    $"Circular rule reference detected involving '{ruleKey}'.");

            if (!_rules.TryGetValue(ruleKey, out var rule))
                throw new RuleEvaluationException(DiagnosticCode.RuleNotFound,
                    $"Rule '{ruleKey}' not found.");

            EnforceLifecyclePolicy(rule, policy);

            // Ensure the rule is parsed
            if (rule.Structure == null || !(rule.Structure.Tokens?.Any() == true))
            {
                var pr = _parser.ParseRule(rule.RuleText);
                if (!pr.Success)
                    throw new RuleParseException(pr.Diagnostics,
                        $"Parse failed for rule '{ruleKey}'.");
                rule.Structure = pr.Structure;
            }

            callChain.Add(ruleKey);
            try
            {
                ValidatePolicyTokens(rule.Structure.Tokens, policy);
                var rpn = ConvertToRpn(rule.Structure.Tokens);
                object evalResult = EvaluateRpn(rpn, parameters, policy, callChain, depth + 1);
                return (parameters ?? new Dictionary<string, object>(), evalResult);
            }
            finally
            {
                callChain.Remove(ruleKey);
            }
        }

        // ── Policy enforcement ────────────────────────────────────────────────────
        private static void EnforceLifecyclePolicy(IRule rule, RuleExecutionPolicy policy)
        {
            if (rule.Structure == null) return;

            var state = rule.Structure.LifecycleState;
            if (state == RuleLifecycleState.Deprecated && !policy.AllowDeprecatedExecution)
                throw new RuleEvaluationException(DiagnosticCode.LifecycleStateViolation,
                    $"Rule '{rule.RuleText}' is Deprecated and the policy does not permit deprecated execution.");

            if ((int)state < (int)policy.MinimumLifecycleState)
                throw new RuleEvaluationException(DiagnosticCode.LifecycleStateViolation,
                    $"Rule '{rule.RuleText}' is in state '{state}' which is below the policy minimum '{policy.MinimumLifecycleState}'.");
        }

        private static void ValidatePolicyTokens(IEnumerable<Token> tokens, RuleExecutionPolicy policy)
        {
            if (policy.AllowedTokenTypes == null || tokens == null) return;
            foreach (var t in tokens)
            {
                if (!policy.IsTokenTypeAllowed(t.Type))
                    throw new RuleEvaluationException(DiagnosticCode.OperatorNotAllowed,
                        $"Token type '{t.Type}' ('{t.Value}') is not permitted by the current execution policy.");
            }
        }

        // ── Audit ─────────────────────────────────────────────────────────────────
        private void EmitAudit(string key, bool success, TimeSpan elapsed, object result,
            List<ParseDiagnostic> diags)
        {
            RuleEvaluated?.Invoke(this, new RuleAuditEventArgs
            {
                RuleKey     = key,
                Success     = success,
                Elapsed     = elapsed,
                Result      = result,
                Diagnostics = diags ?? new List<ParseDiagnostic>()
            });
        }
    }
}
