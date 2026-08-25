using System;

namespace TheTechIdea.Beep.Editor.Forms.Models
{
    /// <summary>
    /// A named, reusable Editor object (Oracle Forms EDITOR) — a larger popup
    /// window for editing a text item's value, invoked via EDIT_TEXTITEM.
    /// Attached to an item by name (<c>ItemInfo.EditorName</c>) or used as the
    /// system default when an item has none attached — Oracle Forms' own
    /// EDIT_TEXTITEM works either way.
    /// </summary>
    public class EditorDefinition
    {
        /// <summary>Gets or sets the editor's unique name, or null for the system default.</summary>
        public string Name { get; set; }

        /// <summary>Gets or sets the popup's title bar text.</summary>
        public string Title { get; set; } = "Edit Text";

        /// <summary>Gets or sets the popup's width in device-independent pixels.</summary>
        public int Width { get; set; } = 480;

        /// <summary>Gets or sets the popup's height in device-independent pixels.</summary>
        public int Height { get; set; } = 320;

        /// <summary>Gets or sets whether text wraps at the edit area's width (Oracle Forms WRAP_STYLE = Word) rather than only at explicit line breaks.</summary>
        public bool WrapText { get; set; } = true;

        /// <summary>Gets or sets whether a scroll bar is shown for content taller than the edit area.</summary>
        public bool ShowScrollBar { get; set; } = true;

        /// <summary>Gets or sets when this editor definition was created.</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public EditorDefinition() { }

        /// <summary>
        /// Null <paramref name="name"/> is valid and intentional here (unlike
        /// e.g. AlertDefinition, every EditorDefinition is not necessarily a
        /// named registry entry) — it's what <see cref="SystemDefault"/> uses
        /// for an item with no Editor object attached. The registry
        /// (FormsManager.CreateEditor) is what rejects a null/empty name for
        /// an entry actually being stored.
        /// </summary>
        public EditorDefinition(
            string name, string title = "Edit Text",
            int width = 480, int height = 320,
            bool wrapText = true, bool showScrollBar = true)
        {
            Name = name;
            Title = title;
            Width = width;
            Height = height;
            WrapText = wrapText;
            ShowScrollBar = showScrollBar;
        }

        /// <summary>
        /// The definition EDIT_TEXTITEM uses for an item with no Editor
        /// object explicitly attached — a fresh instance each call so no
        /// caller can mutate the shared "system default" by editing what it
        /// got back.
        /// </summary>
        public static EditorDefinition SystemDefault() => new(name: null);
    }

    /// <summary>
    /// The outcome of showing an Editor popup — whether the user committed
    /// (OK) or discarded (Cancel) their edit, and the resulting text.
    /// </summary>
    public class EditorResult
    {
        /// <summary>Gets or sets whether the user committed the edit (OK) rather than cancelling.</summary>
        public bool Committed { get; set; }

        /// <summary>Gets or sets the edited text. Only meaningful when <see cref="Committed"/> is true.</summary>
        public string Value { get; set; }

        public static EditorResult Cancel() => new() { Committed = false };

        public static EditorResult Ok(string value) => new() { Committed = true, Value = value };
    }
}
