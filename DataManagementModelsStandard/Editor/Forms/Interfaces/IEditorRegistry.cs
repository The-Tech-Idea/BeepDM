using System.Collections.Generic;
using TheTechIdea.Beep.Editor.Forms.Models;

namespace TheTechIdea.Beep.Editor.UOWManager.Interfaces
{
    /// <summary>
    /// Manages named, reusable Editor objects (Oracle Forms EDITOR) — the
    /// large-text popup an item's EDITOR_NAME property attaches, invoked via
    /// EDIT_TEXTITEM. Distinct from <see cref="IEditorProvider"/>, which
    /// renders the popup; this registry only holds the named definitions.
    /// </summary>
    public interface IEditorRegistry
    {
        EditorDefinition CreateEditor(
            string name, string title = "Edit Text",
            int width = 480, int height = 320,
            bool wrapText = true, bool showScrollBar = true);

        EditorDefinition GetEditor(string name);

        IReadOnlyList<EditorDefinition> GetAllEditors();

        bool RemoveEditor(string name);

        void ClearAllEditors();

        bool EditorExists(string name);
    }
}
