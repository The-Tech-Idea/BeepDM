using System;
using System.Collections.Generic;
using System.Linq;

namespace TheTechIdea.Beep.Editor.Defaults.RuleParsing
{
    /// <summary>
    /// Identifies which DSL syntax was used to write the rule.
    /// </summary>
    public enum RuleSyntaxVersion
    {
        /// <summary>Legacy function-call style: IF(cond, trueVal, falseVal)</summary>
        V1Legacy = 0,

        /// <summary>Dot-segment style (DSL v1): IF.cond.trueVal.falseVal</summary>
        V1Dot = 1,

        /// <summary>Unknown or unparseable syntax.</summary>
        Unknown = -1,

        /// <summary>
        /// The string has no leading `:` identifier and is treated as a plain literal value.
        /// No resolver will be invoked; the value is used as-is.
        /// </summary>
        Literal = 2
    }

    /// <summary>
    /// Severity level of a rule parse diagnostic message.
    /// </summary>
    public enum RuleDiagnosticSeverity
    {
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// A diagnostic message produced when parsing or validating a rule string.
    /// </summary>
    public sealed class RuleDiagnostic
    {
        /// <summary>Severity of the issue.</summary>
        public RuleDiagnosticSeverity Severity { get; }

        /// <summary>Short error code, e.g. "DSL001".</summary>
        public string Code { get; }

        /// <summary>Human-readable description of the issue.</summary>
        public string Message { get; }

        /// <summary>Index in the original rule string where the problem was detected (-1 = unknown).</summary>
        public int Position { get; }

        public RuleDiagnostic(RuleDiagnosticSeverity severity, string code, string message, int position = -1)
        {
            Severity = severity;
            Code = code ?? string.Empty;
            Message = message ?? string.Empty;
            Position = position;
        }

        public override string ToString() => $"[{Severity}] {Code}: {Message}" + (Position >= 0 ? $" (pos {Position})" : "");
    }

    /// <summary>
    /// Canonical internal representation of a parsed default-value rule.
    /// Both legacy function-style rules and dot-style DSL rules are normalised
    /// into this structure so that resolvers and validators can work uniformly.
    /// </summary>
    public sealed class ParsedRule
    {
        /// <summary>
        /// Uppercase operator name extracted from the rule, e.g. "IF", "QUERY", "NOW", "ADD".
        /// Empty string when the rule could not be parsed.
        /// </summary>
        public string Operator { get; }

        /// <summary>
        /// Ordered list of argument strings extracted from the rule.
        /// For dot-style rules these are the dot-separated segments after the operator.
        /// For legacy rules these are the comma-separated parameters inside parentheses.
        /// </summary>
        public IReadOnlyList<string> Args { get; }

        /// <summary>Verbatim original rule string supplied by the caller.</summary>
        public string OriginalRule { get; }

        /// <summary>
        /// Equivalent legacy function-style representation produced by the normalizer,
        /// suitable for routing to existing resolvers.
        /// e.g. dot-style "IF.Age>=18.Adult.Minor" → "IF(Age>=18,Adult,Minor)"
        /// </summary>
        public string NormalizedRule { get; }

        /// <summary>Which DSL syntax was detected.</summary>
        public RuleSyntaxVersion SyntaxVersion { get; }

        /// <summary>True when no Error-severity diagnostics were produced.</summary>
        public bool IsValid => !Diagnostics.Any(d => d.Severity == RuleDiagnosticSeverity.Error);

        /// <summary>
        /// True when this rule is a plain literal value (no leading `:` identifier).
        /// When true, <see cref="NormalizedRule"/> holds the literal value and no resolver
        /// should be invoked.
        /// </summary>
        public bool IsLiteral => SyntaxVersion == RuleSyntaxVersion.Literal;

        /// <summary>Parse and validation diagnostics (may include Info, Warning, or Error entries).</summary>
        public IReadOnlyList<RuleDiagnostic> Diagnostics { get; }
        public object Arguments { get; internal set; }

        public ParsedRule(
            string @operator,
            IReadOnlyList<string> args,
            string originalRule,
            string normalizedRule,
            RuleSyntaxVersion syntaxVersion,
            IReadOnlyList<RuleDiagnostic> diagnostics = null)
        {
            Operator = (@operator ?? string.Empty).ToUpperInvariant();
            Args = args ?? Array.Empty<string>();
            OriginalRule = originalRule ?? string.Empty;
            NormalizedRule = normalizedRule ?? string.Empty;
            SyntaxVersion = syntaxVersion;
            Diagnostics = diagnostics ?? Array.Empty<RuleDiagnostic>();
        }

        /// <summary>Returns the normalized rule string.</summary>
        public override string ToString() => NormalizedRule;
    }
}
