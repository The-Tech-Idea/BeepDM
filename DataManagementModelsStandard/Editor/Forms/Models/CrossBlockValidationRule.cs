using System;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// A validation rule that spans two registered blocks.
    /// </summary>
    public class CrossBlockValidationRule
    {
        /// <summary>Gets or sets the logical rule name.</summary>
        public string RuleName { get; set; }

        /// <summary>Gets or sets the first block participating in the rule.</summary>
        public string BlockA   { get; set; }

        /// <summary>Gets or sets the second block participating in the rule.</summary>
        public string BlockB   { get; set; }

        /// <summary>
        /// Receives (blockAUow, blockBUow); return null/empty string to pass, non-empty to fail.
        /// </summary>
        public Func<IUnitofWork, IUnitofWork, string> Validator { get; set; }

        /// <summary>Gets or sets the severity to report when the rule fails.</summary>
        public ValidationSeverity Severity { get; set; } = ValidationSeverity.Error;
    }
}
