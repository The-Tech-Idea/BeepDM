namespace TheTechIdea.Beep.Rules
{
    /// <summary>
    /// Structured diagnostic entry emitted during tokenization, parsing, or evaluation.
    /// </summary>
    public class ParseDiagnostic
    {
        /// <summary>Machine-readable diagnostic code.</summary>
        public DiagnosticCode Code { get; set; }

        /// <summary>Severity of the diagnostic.</summary>
        public DiagnosticSeverity Severity { get; set; }

        /// <summary>Human-readable description of the problem.</summary>
        public string Message { get; set; }

        /// <summary>Zero-based character offset in the source expression where the problem starts.</summary>
        public int Start { get; set; }

        /// <summary>Number of characters in the problem span.  Zero means point location.</summary>
        public int Length { get; set; }

        /// <summary>Optional suggested fix or action for the caller.</summary>
        public string Suggestion { get; set; }

        public override string ToString() =>
            $"[{Severity}] {Code} at {Start}+{Length}: {Message}" +
            (string.IsNullOrEmpty(Suggestion) ? string.Empty : $" → {Suggestion}");
    }
}
