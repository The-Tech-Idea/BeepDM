using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;

namespace TheTechIdea.Beep.Editor.UOWManager.Helpers
{
    /// <summary>
    /// Manages cross-block validation rules and executes them against registered UOW pairs.
    /// </summary>
    public class CrossBlockValidationManager
    {
        private readonly ConcurrentDictionary<string, CrossBlockValidationRule> _rules = new();
        private readonly Func<string, IUnitofWork> _uowResolver;

        public CrossBlockValidationManager(Func<string, IUnitofWork> uowResolver)
        {
            _uowResolver = uowResolver ?? throw new ArgumentNullException(nameof(uowResolver));
        }

        public void Register(CrossBlockValidationRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            if (string.IsNullOrEmpty(rule.RuleName))
                throw new ArgumentException("RuleName must not be empty.", nameof(rule));
            _rules[rule.RuleName] = rule;
        }

        public bool Unregister(string ruleName) => _rules.TryRemove(ruleName, out _);

        /// <summary>
        /// Execute all rules. Returns a list of failure messages (empty = all passed).
        /// Error-severity failures indicate the form should not commit.
        /// </summary>
        public IReadOnlyList<string> Validate()
        {
            var failures = new List<string>();
            foreach (var rule in _rules.Values)
            {
                var uowA = _uowResolver(rule.BlockA);
                var uowB = _uowResolver(rule.BlockB);
                if (uowA == null || uowB == null) continue;
                try
                {
                    var msg = rule.Validator?.Invoke(uowA, uowB);
                    if (!string.IsNullOrEmpty(msg))
                        failures.Add($"[{rule.RuleName}] {msg}");
                }
                catch (Exception ex)
                {
                    failures.Add($"[{rule.RuleName}] Validator threw: {ex.Message}");
                }
            }
            return failures;
        }

        public bool HasErrorSeverityFailures(IReadOnlyList<string> failures) =>
            failures != null && failures.Count > 0 &&
            _rules.Values
                  .Any(r => r.Severity == ValidationSeverity.Error ||
                             r.Severity == ValidationSeverity.Critical);
    }
}
