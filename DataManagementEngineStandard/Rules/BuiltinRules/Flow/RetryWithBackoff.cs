using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Rules.BuiltinRules.Flow
{
    /// <summary>
    /// Retries a downstream rule/action up to <c>MaxRetries</c> times with exponential backoff.
    /// In this implementation the rule records retry state and returns <c>false</c> when the
    /// maximum retry count is exceeded — the caller is responsible for re-invoking.
    /// Parameters:
    ///   <c>StateKey</c>     — unique key scoping retry state.
    ///   <c>MaxRetries</c>   — int (default 3).
    ///   <c>DelayMs</c>      — initial delay in ms (default 200); doubles each retry.
    ///   <c>LastResult</c>   — bool string — pass "true" when the downstream call succeeded.
    /// Optional <c>IRuleStateStore</c> for persistent counter.
    /// Returns <c>true</c> when retries are not yet exhausted and more attempts may be made.
    /// </summary>
    [Rule(ruleKey: "Flow.RetryWithBackoff", ParserKey = "RulesParser", RuleName = "RetryWithBackoff")]
    public sealed class RetryWithBackoff : IRule
    {
        private static readonly IRuleStateStore _defaultStore = new InMemoryRuleStateStore();

        public string RuleText { get; set; } = "Flow.RetryWithBackoff";
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

            int maxRetries = 3;
            int delayMs    = 200;
            if (parameters.TryGetValue("MaxRetries", out var mrRaw)) int.TryParse(mrRaw?.ToString(), out maxRetries);
            if (parameters.TryGetValue("DelayMs",    out var dlRaw)) int.TryParse(dlRaw?.ToString(), out delayMs);

            bool lastResult = false;
            if (parameters.TryGetValue("LastResult", out var lrRaw))
                bool.TryParse(lrRaw?.ToString(), out lastResult);

            var store    = parameters.TryGetValue("IRuleStateStore", out var storeRaw)
                               && storeRaw is IRuleStateStore s ? s : _defaultStore;
            string stateKey = keyRaw?.ToString() ?? "retry";

            // If the downstream call succeeded, clear retry counter.
            if (lastResult)
            {
                store.Remove(stateKey);
                output["Result"]     = true;
                output["ShouldRetry"] = false;
                output["RetryCount"] = 0;
                return (output, true);
            }

            int count = store.Increment(stateKey);
            bool canRetry = count <= maxRetries;
            int  waitMs   = canRetry ? delayMs * (int)Math.Pow(2, count - 1) : 0;

            if (!canRetry) store.Remove(stateKey); // reset for next use

            output["Result"]      = canRetry;
            output["ShouldRetry"] = canRetry;
            output["RetryCount"]  = count;
            output["WaitMs"]      = waitMs;
            return (output, canRetry);
        }
    }
}
