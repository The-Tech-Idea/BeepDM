using System;

namespace TheTechIdea.Beep.Editor.Defaults.RuleParsing
{
    /// <summary>
    /// Entry-point for rule normalization.
    ///
    /// Given any rule string (legacy function-style OR new dot-style DSL),
    /// <see cref="Normalize"/> returns a <see cref="ParsedRule"/> whose
    /// <see cref="ParsedRule.NormalizedRule"/> is always a legacy function-style
    /// string that existing resolvers can consume unchanged.
    ///
    /// Usage:
    /// <code>
    ///   var parsed = RuleNormalizer.Normalize(rule);
    ///   if (!parsed.IsValid) { /* handle diagnostics */ }
    ///   string routedRule = parsed.NormalizedRule; // feed to resolver
    /// </code>
    /// </summary>
    public static class RuleNormalizer
    {
        private static readonly DotStyleRuleParser _dotParser = new DotStyleRuleParser();

        /// <summary>
        /// Parses <paramref name="rule"/>, detects the syntax version, and returns
        /// a canonical <see cref="ParsedRule"/> with a resolver-ready
        /// <see cref="ParsedRule.NormalizedRule"/>.
        /// </summary>
        /// <param name="rule">Raw rule string from <see cref="TheTechIdea.Beep.ConfigUtil.DefaultValue.Rule"/>.</param>
        /// <returns>
        /// A <see cref="ParsedRule"/> whose <see cref="ParsedRule.IsValid"/> flag indicates
        /// whether normalisation succeeded.  Diagnostics are available on the returned object.
        /// </returns>
        // Known bare-operator tokens that are treated as expressions even without the `:` prefix,
        // for backward compatibility with persisted DefaultValue.Rule entries written before the
        // `:` convention was introduced.  A deprecation warning is logged when these are used.
        private static readonly System.Collections.Generic.HashSet<string> _legacyExpressionTokens =
            new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
            {
                "NOW","TODAY","YESTERDAY","TOMORROW","CURRENTDATE","CURRENTTIME","CURRENTDATETIME",
                "USERNAME","CURRENTUSER","USERID","USEREMAIL","USERLOGIN","USERDOMAIN",
                "NEWGUID","GUID","MACHINENAME","SEQUENCE","INCREMENT","RANDOM",
                "ENV","CONFIG","PROPERTY","RECORD","FIELD"
            };

        public static ParsedRule Normalize(string rule)
        {
            if (string.IsNullOrWhiteSpace(rule))
            {
                var emptyDiag = new[]
                {
                    new RuleDiagnostic(RuleDiagnosticSeverity.Warning, "NRM001", "Rule string is null or empty — nothing to normalize.")
                };
                return new ParsedRule(string.Empty, Array.Empty<string>(), rule ?? string.Empty,
                    string.Empty, RuleSyntaxVersion.Unknown, emptyDiag);
            }

            var trimmed = rule.Trim();

            // ── Leading `:` identifier — this string is an expression to be resolved. ──
            if (trimmed.StartsWith(":"))
            {
                var expressionBody = trimmed.Substring(1).TrimStart();
                // Re-enter normalization with the bare expression body.
                return NormalizeExpression(expressionBody, rule);
            }

            // ── No `:` prefix — check if it is a known bare operator for backward compat. ──
            var dotIdx  = trimmed.IndexOf('.');
            var parenIdx = trimmed.IndexOf('(');
            var bareToken = dotIdx > 0  ? trimmed.Substring(0, dotIdx).Trim()
                          : parenIdx > 0 ? trimmed.Substring(0, parenIdx).Trim()
                          : trimmed;

            if (_legacyExpressionTokens.Contains(bareToken))
            {
                // Legacy rule — treat as expression with deprecation warning.
                var legacyParsed = NormalizeExpression(trimmed, rule);
                var warnDiag = new RuleDiagnostic(RuleDiagnosticSeverity.Warning, "NRM002",
                    $"Rule '{rule}' is missing the ':' prefix. Consider updating it to ':{rule}' to silence this warning.");
                return new ParsedRule(
                    legacyParsed.Operator, legacyParsed.Args, rule,
                    legacyParsed.NormalizedRule, legacyParsed.SyntaxVersion,
                    new[] { warnDiag });
            }

            // ── No `:` and not a known operator → plain literal. ──
            return new ParsedRule(
                string.Empty,
                Array.Empty<string>(),
                rule,
                trimmed,              // NormalizedRule holds the literal value
                RuleSyntaxVersion.Literal,
                Array.Empty<RuleDiagnostic>());
        }

        /// <summary>
        /// Internal helper: normalizes an expression string (already stripped of the `:` prefix)
        /// using the dot-style parser or legacy function-style path.
        /// </summary>
        private static ParsedRule NormalizeExpression(string expressionBody, string originalRule)
        {
            // --- Dot-style detection ---
            if (DotStyleRuleParser.IsDotStyleRule(expressionBody))
                return _dotParser.Parse(expressionBody);

            // --- Legacy function-style path ---
            var parenPos = expressionBody.IndexOf('(');
            string op;
            if (parenPos > 0)
            {
                op = expressionBody.Substring(0, parenPos).Trim().ToUpperInvariant();
            }
            else
            {
                // No parentheses and no dot segments: bare operator token.
                op = expressionBody.ToUpperInvariant();
            }

            return new ParsedRule(
                op,
                Array.Empty<string>(),
                originalRule,
                expressionBody,          // normalized == expression body (without `:`)
                RuleSyntaxVersion.V1Legacy,
                Array.Empty<RuleDiagnostic>());
        }

        /// <summary>
        /// Convenience: normalizes the rule and returns the normalized string to route to resolvers.
        /// Returns the original rule unchanged when normalization fails.
        /// </summary>
        /// <param name="rule">Rule to normalize.</param>
        /// <param name="diagnosticsMessage">Receives a diagnostics summary string on failure, or null on success.</param>
        public static string GetNormalizedRule(string rule, out string diagnosticsMessage)
        {
            var parsed = Normalize(rule);
            if (!parsed.IsValid)
            {
                diagnosticsMessage = string.Join("; ", parsed.Diagnostics);
                return rule; // fall back to original so resolution still has a chance
            }

            diagnosticsMessage = null;
            return string.IsNullOrWhiteSpace(parsed.NormalizedRule) ? rule : parsed.NormalizedRule;
        }
    }
}
