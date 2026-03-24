using System;
using System.Collections.Generic;
using System.Linq;

namespace TheTechIdea.Beep.Rules.BuiltinRules.Route
{
    /// <summary>
    /// Evaluates a list of condition→route mappings and returns the first matching route label.
    /// Parameters: <c>Routes</c> (semicolon-separated list of "expression=&gt;routeLabel" pairs),
    /// <c>DefaultRoute</c> (string, returned when no condition matches).
    /// Plus any context values referenced by the condition expressions as named parameters.
    ///
    /// Example Routes value: "Status==Active=&gt;ActiveQueue;Status==Pending=&gt;WaitQueue"
    /// Conditions support literal equality comparison only in this implementation.
    /// For advanced expression evaluation integrate with <see cref="RulesEngine"/>.
    /// </summary>
    [Rule(ruleKey: "Route.RouteOnCondition", ParserKey = "RulesParser", RuleName = "RouteOnCondition")]
    public sealed class RouteOnCondition : IRule
    {
        public string RuleText { get; set; } = "Route.RouteOnCondition";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public (Dictionary<string, object> outputs, object result) SolveRule(
            Dictionary<string, object> parameters = null)
        {
            var output = new Dictionary<string, object>();
            if (parameters == null || !parameters.TryGetValue("Routes", out var routesRaw))
            {
                output["Error"] = "Missing required parameter: Routes";
                return (output, null);
            }

            string defaultRoute = string.Empty;
            if (parameters.TryGetValue("DefaultRoute", out var defRaw))
                defaultRoute = defRaw?.ToString() ?? string.Empty;

            string? matched = null;
            foreach (string entry in (routesRaw?.ToString() ?? string.Empty).Split(';',
                         StringSplitOptions.RemoveEmptyEntries))
            {
                int arrow = entry.IndexOf("=>", StringComparison.Ordinal);
                if (arrow < 0) continue;

                string condition = entry[..arrow].Trim();
                string route     = entry[(arrow + 2)..].Trim();

                if (EvaluateSimpleEquality(condition, parameters))
                {
                    matched = route;
                    break;
                }
            }

            string res = matched ?? defaultRoute;
            output["Result"]   = res;
            output["Matched"]  = matched != null;
            output["Route"]    = res;
            return (output, res);
        }

        // Supports "FieldName==Value" or "FieldName!=Value" simple equality checks.
        private static bool EvaluateSimpleEquality(string condition, Dictionary<string, object> ctx)
        {
            bool negate = false;
            string sep;
            if (condition.Contains("!=", StringComparison.Ordinal))
            { sep = "!="; negate = true; }
            else if (condition.Contains("==", StringComparison.Ordinal))
            { sep = "=="; }
            else
            { return false; }

            int idx = condition.IndexOf(sep, StringComparison.Ordinal);
            string field = condition[..idx].Trim();
            string expected = condition[(idx + sep.Length)..].Trim().Trim('\'', '"');

            if (!ctx.TryGetValue(field, out var actual)) return false;
            bool eq = string.Equals(actual?.ToString(), expected, StringComparison.OrdinalIgnoreCase);
            return negate ? !eq : eq;
        }
    }
}
