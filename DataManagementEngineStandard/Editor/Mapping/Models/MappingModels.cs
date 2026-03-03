using System.Collections.Generic;
using TheTechIdea.Beep.Workflow;

namespace TheTechIdea.Beep.Editor.Mapping.Models
{
    /// <summary>Controls how field names are compared when auto-mapping by convention.</summary>
    public enum NameMatchMode
    {
        /// <summary>Names must match character-for-character (ordinal).</summary>
        Exact,
        /// <summary>Names match regardless of casing.</summary>
        CaseInsensitive,
        /// <summary>Names match when one is a prefix of the other (case-insensitive).</summary>
        FuzzyPrefix
    }

    /// <summary>Outcome of a mapping validation pass.</summary>
    public class MappingValidationResult
    {
        public bool IsValid => Errors.Count == 0;
        public int  MappedFieldCount { get; set; }
        public List<string> Errors   { get; } = new();
        public List<string> Warnings { get; } = new();
    }

    /// <summary>Before/after record for a single changed field mapping.</summary>
    public class MappingFieldChange
    {
        public Mapping_rep_fields Before { get; set; } = new();
        public Mapping_rep_fields After  { get; set; } = new();
    }

    /// <summary>Describes field-level differences between two mapping versions.</summary>
    public class MappingDiff
    {
        public List<Mapping_rep_fields> Added   { get; } = new();
        public List<Mapping_rep_fields> Removed { get; } = new();
        public List<MappingFieldChange> Changed { get; } = new();
        public bool HasChanges => Added.Count > 0 || Removed.Count > 0 || Changed.Count > 0;
    }
}
