using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Rules.BuiltinRules.Route
{
    /// <summary>
    /// Limits the rate of rule activation per <c>WindowSeconds</c> sliding window.
    /// When the call count exceeds <c>MaxCalls</c> within the window, returns <c>false</c>.
    /// Requires an <see cref="IRuleStateStore"/> for persistent counter state.
    /// Parameters: <c>StateKey</c> (string scope key), <c>MaxCalls</c> (int),
    /// <c>WindowSeconds</c> (int).
    /// Optional <c>IRuleStateStore</c> (injected) or uses the static default store.
    /// </summary>
    [Rule(ruleKey: "Route.ThrottleRule", ParserKey = "RulesParser", RuleName = "ThrottleRule")]
    public sealed class ThrottleRule : IRule
    {
        private static readonly IRuleStateStore _defaultStore = new InMemoryRuleStateStore();

        public string RuleText { get; set; } = "Route.ThrottleRule";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public (Dictionary<string, object> outputs, object result) SolveRule(
            Dictionary<string, object> parameters = null)
        {
            var output = new Dictionary<string, object>();
            if (parameters == null ||
                !parameters.TryGetValue("StateKey",      out var keyRaw)  ||
                !parameters.TryGetValue("MaxCalls",      out var maxRaw)  ||
                !parameters.TryGetValue("WindowSeconds", out var winRaw))
            {
                output["Error"] = "Missing required parameters: StateKey, MaxCalls, WindowSeconds";
                return (output, false);
            }

            if (!int.TryParse(maxRaw?.ToString(), out int maxCalls) ||
                !int.TryParse(winRaw?.ToString(), out int windowSec))
            {
                output["Error"] = "MaxCalls and WindowSeconds must be valid integers";
                return (output, false);
            }

            var store = parameters.TryGetValue("IRuleStateStore", out var storeRaw)
                            && storeRaw is IRuleStateStore s ? s : _defaultStore;

            string   stateKey   = keyRaw?.ToString() ?? "throttle";
            string   tsKey      = stateKey + "__ts";
            long     nowTicks   = DateTime.UtcNow.Ticks;
            long     windowTicks = TimeSpan.FromSeconds(windowSec).Ticks;

            // Reset window if expired
            var stored = store.Get(tsKey);
            if (stored is long lastTs && (nowTicks - lastTs) > windowTicks)
            {
                store.Remove(stateKey);
                store.Set(tsKey, nowTicks);
            }
            else if (stored == null)
            {
                store.Set(tsKey, nowTicks);
            }

            int count   = store.Increment(stateKey);
            bool allowed = count <= maxCalls;

            output["Result"]    = allowed;
            output["CallCount"] = count;
            output["Allowed"]   = allowed;
            return (output, allowed);
        }
    }
}
