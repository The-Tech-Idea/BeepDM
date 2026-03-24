using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Rules
{
    public interface IRuleEngine
    {
        // ── Evaluation ──────────────────────────────────────────────────────────────
        /// <summary>Evaluates an already-tokenized expression with the given parameters.</summary>
        object EvaluateExpression(IList<Token> tokens, Dictionary<string, object> parameters);

        /// <summary>Evaluates with an explicit execution policy.</summary>
        object EvaluateExpression(IList<Token> tokens, Dictionary<string, object> parameters,
                                  RuleExecutionPolicy policy);

        // ── Registration ─────────────────────────────────────────────────────────────
        /// <summary>
        /// Registers a rule with the engine.
        /// Throws <see cref="ArgumentException"/> on duplicate key.
        /// </summary>
        void RegisterRule(IRule rule);

        /// <summary>
        /// Removes a rule by its registered key.
        /// Returns true if the rule was found and removed.
        /// </summary>
        bool UnregisterRule(string ruleKey);

        // ── Solve ────────────────────────────────────────────────────────────────────
        /// <summary>Evaluates a registered rule by key.</summary>
        (Dictionary<string, object> outputs, object result) SolveRule(
            string ruleKey, Dictionary<string, object> parameters);

        /// <summary>Evaluates a registered rule with an explicit execution policy.</summary>
        (Dictionary<string, object> outputs, object result) SolveRule(
            string ruleKey, Dictionary<string, object> parameters, RuleExecutionPolicy policy);

        // ── Introspection ─────────────────────────────────────────────────────────────
        /// <summary>Returns true if a rule with the given key is registered.</summary>
        bool HasRule(string ruleKey);

        /// <summary>Returns all currently registered rule keys.</summary>
        IEnumerable<string> GetRegisteredKeys();

        // ── Audit events ──────────────────────────────────────────────────────────────
        /// <summary>
        /// Raised after every successful or failed rule evaluation.
        /// Subscribers can use this for logging, metrics, or policy auditing.
        /// </summary>
        event EventHandler<RuleAuditEventArgs> RuleEvaluated;
    }

    /// <summary>Event data emitted by <see cref="IRuleEngine.RuleEvaluated"/>.</summary>
    public class RuleAuditEventArgs : EventArgs
    {
        public string RuleKey { get; set; }
        public bool Success { get; set; }
        public TimeSpan Elapsed { get; set; }
        public List<ParseDiagnostic> Diagnostics { get; set; } = new List<ParseDiagnostic>();
        public object Result { get; set; }
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    }
}