using System.Collections.Generic;

namespace TheTechIdea.Beep.Rules
{
    /// <summary>
    /// Result of a parse operation — contains success flag, partial or full AST structure,
    /// and any structured diagnostics emitted during tokenization and parsing.
    /// </summary>
    public class ParseResult
    {
        /// <summary>True when parsing completed without any Error-severity diagnostics.</summary>
        public bool Success { get; set; }

        /// <summary>
        /// The parsed <see cref="IRuleStructure"/> — may be partial when <see cref="Success"/> is false
        /// but the parser was able to recover.
        /// </summary>
        public IRuleStructure Structure { get; set; }

        /// <summary>Diagnostics emitted during tokenization and parsing.</summary>
        public List<ParseDiagnostic> Diagnostics { get; set; } = new List<ParseDiagnostic>();

        /// <summary>Shorthand: returns true when Diagnostics contains at least one Error entry.</summary>
        public bool HasErrors
        {
            get
            {
                foreach (var d in Diagnostics)
                    if (d.Severity == DiagnosticSeverity.Error) return true;
                return false;
            }
        }
    }
}
