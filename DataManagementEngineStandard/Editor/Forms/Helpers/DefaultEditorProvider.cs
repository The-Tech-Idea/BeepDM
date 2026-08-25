using System;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.Forms.Models;

namespace TheTechIdea.Beep.Editor.Forms.Helpers
{
    /// <summary>
    /// Default no-op implementation of IEditorProvider used when no UI layer is present.
    /// Unlike DefaultAlertProvider's auto-accept, there is no text to hand back without
    /// a real UI, so this always returns a cancelled result rather than fabricate an edit.
    /// Replace by injecting a real implementation from the UI project.
    /// </summary>
    public class DefaultEditorProvider : IEditorProvider
    {
        /// <summary>Reports that no editor UI is available, without fabricating a result.</summary>
        public Task<EditorResult> ShowEditorAsync(
            EditorDefinition editor,
            string currentValue,
            CancellationToken ct = default)
        {
            Console.WriteLine($"[EDITOR {editor?.Title ?? "Edit Text"}] no UI provider available — edit discarded");
            return Task.FromResult(EditorResult.Cancel());
        }
    }
}
