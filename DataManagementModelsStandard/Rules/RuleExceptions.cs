using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Rules
{
    /// <summary>Base for all rule engine exceptions. Carries a <see cref="DiagnosticCode"/>.</summary>
    public class RuleException : Exception
    {
        public DiagnosticCode Code { get; }
        public RuleException(DiagnosticCode code, string message) : base(message) => Code = code;
        public RuleException(DiagnosticCode code, string message, Exception inner)
            : base(message, inner) => Code = code;
    }

    /// <summary>Thrown when a rule expression cannot be successfully parsed.</summary>
    public sealed class RuleParseException : RuleException
    {
        public IReadOnlyList<ParseDiagnostic> Diagnostics { get; }
        public RuleParseException(IEnumerable<ParseDiagnostic> diagnostics, string message)
            : base(DiagnosticCode.UnexpectedToken, message)
        {
            Diagnostics = new List<ParseDiagnostic>(diagnostics ?? Array.Empty<ParseDiagnostic>());
        }
    }

    /// <summary>Thrown during rule evaluation (type errors, cycles, depth exceeded, policy violations).</summary>
    public sealed class RuleEvaluationException : RuleException
    {
        public RuleEvaluationException(DiagnosticCode code, string message) : base(code, message) { }
        public RuleEvaluationException(DiagnosticCode code, string message, Exception inner)
            : base(code, message, inner) { }
    }

    /// <summary>Thrown for catalog registration/lookup failures.</summary>
    public sealed class RuleCatalogException : RuleException
    {
        public RuleCatalogException(DiagnosticCode code, string message) : base(code, message) { }
    }
}
