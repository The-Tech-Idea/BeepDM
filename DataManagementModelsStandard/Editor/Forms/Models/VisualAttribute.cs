using System;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// A named bundle of display properties applied to an item or block —
    /// the Oracle Forms Visual Attribute.
    /// </summary>
    /// <remarks>
    /// UI-agnostic on purpose. Colours are hex strings ("#RRGGBB"), not
    /// <c>System.Drawing.Color</c>, so the engine and the WPF host take no
    /// <c>System.Drawing</c> dependency (AGENTS.md §8). Each host parses the hex
    /// into its own colour type when it applies the attribute.
    ///
    /// The three *Override flags mark which record states this attribute varies
    /// for. A host asks <see cref="IVisualAttributeManager.ResolveForItem"/> for
    /// the attribute that applies given the current record state rather than
    /// re-implementing the precedence itself.
    /// </remarks>
    public sealed class VisualAttribute
    {
        /// <summary>The attribute's identity; registration and application key by this.</summary>
        public string Name { get; set; } = string.Empty;

        public string FontFamily { get; set; }
        public float? FontSize { get; set; }

        /// <summary>Bold/italic/underline bitmask; host-interpreted, null = inherit.</summary>
        public int? FontStyleMask { get; set; }

        /// <summary>Foreground colour as "#RRGGBB", or null to inherit.</summary>
        public string ForeColorHex { get; set; }

        /// <summary>Background colour as "#RRGGBB", or null to inherit.</summary>
        public string BackColorHex { get; set; }

        /// <summary>Border colour as "#RRGGBB", or null to inherit.</summary>
        public string BorderColorHex { get; set; }

        /// <summary>This attribute overrides the display of the CURRENT record.</summary>
        public bool CurrentRecordOverride { get; set; }

        /// <summary>This attribute overrides the display of a CHANGED (dirty) record.</summary>
        public bool ChangedRecordOverride { get; set; }

        /// <summary>This attribute overrides the display while the block is in query mode.</summary>
        public bool QueryModeOverride { get; set; }
    }
}
