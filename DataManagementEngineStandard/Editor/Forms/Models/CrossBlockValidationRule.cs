using System;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// A validation rule that spans two registered blocks.
    /// </summary>
    public class CrossBlockValidationRule
    {
        public string RuleName { get; set; }
        public string BlockA   { get; set; }
        public string BlockB   { get; set; }

        /// <summary>
        /// Receives (blockAUow, blockBUow); return null/empty string to pass, non-empty to fail.
        /// </summary>
        public Func<IUnitofWork, IUnitofWork, string> Validator { get; set; }

        public ValidationSeverity Severity { get; set; } = ValidationSeverity.Error;
    }
}
