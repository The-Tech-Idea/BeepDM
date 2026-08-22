namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// A named, reusable bundle of item property overrides — the Oracle Forms
    /// Property Class. Mirrors <see cref="VisualAttribute"/>, which is the same
    /// mechanism scoped to display-only properties; a Property Class covers the
    /// declarative item properties Visual Attribute does not.
    /// </summary>
    /// <remarks>
    /// Every field is nullable and defaults to "not part of this class" —
    /// applying a class never clobbers a value the inheriting field authored
    /// for itself. See <see cref="Interfaces.IPropertyClassManager.ApplyToItem"/>
    /// for the precedence: the field's own authored value wins, the class
    /// fills a gap the field left open, and anything neither says keeps the
    /// item's existing (entity-structure-derived) value.
    /// </remarks>
    public sealed class PropertyClass
    {
        /// <summary>The class's identity; registration and inheritance key by this.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Oracle Forms QUERY_ALLOWED. Null = not part of this class.</summary>
        public bool? QueryAllowed { get; set; }

        /// <summary>Oracle Forms INSERT_ALLOWED. Null = not part of this class.</summary>
        public bool? InsertAllowed { get; set; }

        /// <summary>Oracle Forms UPDATE_ALLOWED. Null = not part of this class.</summary>
        public bool? UpdateAllowed { get; set; }

        /// <summary>Oracle Forms FORMAT_MASK. Null = not part of this class.</summary>
        public string FormatMask { get; set; }

        /// <summary>
        /// Whether <see cref="DefaultValue"/> is part of this class. A default
        /// value of literal null is itself a meaningful override, so presence
        /// can't be inferred from <see cref="DefaultValue"/> being non-null.
        /// </summary>
        public bool HasDefaultValue { get; set; }

        /// <summary>Oracle Forms DEFAULT_VALUE. See <see cref="HasDefaultValue"/>.</summary>
        public object DefaultValue { get; set; }

        /// <summary>
        /// Oracle Forms "Copy Value from Item" — "BlockName.ItemName" of the
        /// item an inheriting field copies its value from on record creation.
        /// Null = not part of this class.
        /// </summary>
        public string CopyValueFromItem { get; set; }
    }
}
