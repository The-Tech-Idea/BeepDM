using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Rules.BuiltinRules.Flow
{
    /// <summary>
    /// Implements the Circuit Breaker resilience pattern.
    /// States: Closed (normal) → Open (failing) → HalfOpen (probe) → Closed.
    /// Parameters:
    ///   <c>StateKey</c>          — unique string key per circuit.
    ///   <c>FailureThreshold</c>  — failures before opening (default 5).
    ///   <c>ResetSeconds</c>      — seconds before attempting half-open probe (default 30).
    ///   <c>CallSucceeded</c>     — bool string — report the outcome of the last protected call.
    /// Optional <c>IRuleStateStore</c>.
    /// Returns <c>true</c> when the circuit is Closed or HalfOpen (call is allowed).
    /// Returns <c>false</c> when Open (call should be rejected).
    /// </summary>
    [Rule(ruleKey: "Flow.CircuitBreaker", ParserKey = "RulesParser", RuleName = "CircuitBreaker")]
    public sealed class CircuitBreaker : IRule
    {
        private static readonly IRuleStateStore _defaultStore = new InMemoryRuleStateStore();

        private const string StateSuffix    = "__cb_state";
        private const string FailsSuffix    = "__cb_fails";
        private const string OpenedSuffix   = "__cb_openedAt";

        public string RuleText { get; set; } = "Flow.CircuitBreaker";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public (Dictionary<string, object> outputs, object result) SolveRule(
            Dictionary<string, object> parameters = null)
        {
            var output = new Dictionary<string, object>();
            if (parameters == null || !parameters.TryGetValue("StateKey", out var keyRaw))
            {
                output["Error"] = "Missing required parameter: StateKey";
                return (output, false);
            }

            int threshold   = 5;
            int resetSec    = 30;
            if (parameters.TryGetValue("FailureThreshold", out var ftRaw)) int.TryParse(ftRaw?.ToString(), out threshold);
            if (parameters.TryGetValue("ResetSeconds",     out var rsRaw)) int.TryParse(rsRaw?.ToString(), out resetSec);

            bool callSucceeded = true;
            if (parameters.TryGetValue("CallSucceeded", out var csRaw))
                bool.TryParse(csRaw?.ToString(), out callSucceeded);

            var   store    = parameters.TryGetValue("IRuleStateStore", out var storeRaw)
                                 && storeRaw is IRuleStateStore s ? s : _defaultStore;
            string baseKey = keyRaw?.ToString() ?? "cb";
            string state   = store.Get(baseKey + StateSuffix)?.ToString() ?? "Closed";

            if (state == "Open")
            {
                // Check if reset period has elapsed → try half-open
                long openedAt = store.Get(baseKey + OpenedSuffix) is long t ? t : 0L;
                if ((DateTime.UtcNow.Ticks - openedAt) > TimeSpan.FromSeconds(resetSec).Ticks)
                {
                    store.Set(baseKey + StateSuffix, "HalfOpen");
                    state = "HalfOpen";
                }
            }

            bool allowed;
            if (state == "Closed" || state == "HalfOpen")
            {
                allowed = true;
                if (!callSucceeded)
                {
                    int fails = store.Increment(baseKey + FailsSuffix);
                    if (fails >= threshold)
                    {
                        store.Set(baseKey + StateSuffix,  "Open");
                        store.Set(baseKey + OpenedSuffix, DateTime.UtcNow.Ticks);
                        store.Remove(baseKey + FailsSuffix);
                        state = "Open"; allowed = false;
                    }
                }
                else if (state == "HalfOpen")
                {
                    // Probe succeeded → close
                    store.Set(baseKey + StateSuffix, "Closed");
                    store.Remove(baseKey + FailsSuffix);
                    state = "Closed";
                }
            }
            else
            {
                allowed = false;
            }

            output["Result"]        = allowed;
            output["CircuitState"]  = state;
            output["Allowed"]       = allowed;
            return (output, allowed);
        }
    }
}
