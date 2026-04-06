namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Platform-neutral field visibility and ordering metadata for a block.
    /// Mirrors the serializable subset of BeepDataBlockFieldSelection without
    /// any WinForms or AppDomain references.
    /// </summary>
    public class BlockFieldMetadata
    {
        /// <summary>Field name as it appears in the EntityStructure.</summary>
        public string FieldName { get; set; } = string.Empty;

        /// <summary>Whether this field should be included when building queries or UI.</summary>
        public bool IncludeInView { get; set; } = true;

        /// <summary>Display order (ascending).</summary>
        public int DisplayOrder { get; set; } = 0;

        /// <summary>Human-readable label. Defaults to FieldName if empty.</summary>
        public string LabelText { get; set; } = string.Empty;

        /// <summary>Whether the field is required for record validation.</summary>
        public bool IsRequired { get; set; } = false;

        /// <summary>Whether the field participates in query-by-example mode.</summary>
        public bool IsQueryable { get; set; } = true;

        /// <summary>Whether the field is read-only during insert/update.</summary>
        public bool IsReadOnly { get; set; } = false;

        /// <summary>Template or editor identifier string (platform resolves to control type).</summary>
        public string TemplateId { get; set; } = string.Empty;

        /// <summary>Optional inline JSON settings forwarded to the platform renderer.</summary>
        public string InlineSettingsJson { get; set; } = string.Empty;

        public override string ToString() =>
            $"{FieldName} ({(IncludeInView ? "Shown" : "Hidden")}, order={DisplayOrder})";
    }
}
