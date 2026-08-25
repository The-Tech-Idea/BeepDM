using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    /// <summary>
    /// Named Editor object registry and EDIT_TEXTITEM built-in.
    /// The Editor object is a large-text popup an item's EDITOR_NAME property
    /// attaches to (Oracle Forms). Neither the definition nor the invocation
    /// existed anywhere before this — the only prior "Editor" hit in the model
    /// layer was BlockFieldDefinition/BlockEntityDefinition.EditorKey, an
    /// unrelated control-selection string for the platform field-presenter
    /// registry. Added 2026-08-25.
    /// </summary>
    public partial class FormsManager : IEditorRegistry
    {
        #region Named Editor Registry

        private readonly ConcurrentDictionary<string, EditorDefinition> _editors = new(StringComparer.OrdinalIgnoreCase);

        public EditorDefinition CreateEditor(
            string name, string title = "Edit Text",
            int width = 480, int height = 320,
            bool wrapText = true, bool showScrollBar = true)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            var editor = new EditorDefinition(name, title, width, height, wrapText, showScrollBar);
            _editors[name] = editor;
            return editor;
        }

        public EditorDefinition GetEditor(string name) =>
            !string.IsNullOrWhiteSpace(name) && _editors.TryGetValue(name, out var editor) ? editor : null;

        public IReadOnlyList<EditorDefinition> GetAllEditors() =>
            _editors.Values.ToList().AsReadOnly();

        public bool RemoveEditor(string name) =>
            !string.IsNullOrWhiteSpace(name) && _editors.TryRemove(name, out _);

        public void ClearAllEditors() =>
            _editors.Clear();

        public bool EditorExists(string name) =>
            !string.IsNullOrWhiteSpace(name) && _editors.ContainsKey(name);

        #endregion

        #region EDIT_TEXTITEM Built-in

        /// <summary>
        /// Shows the large-text editor popup for an item. On commit, writes
        /// the edited value onto the block's current record the same way
        /// ShowLOVAsync writes a selected LOV record's related fields —
        /// via SetFieldValue on blockInfo.UnitOfWork.CurrentItem — so the
        /// change flows through the same commit path a normal item edit
        /// would use. Does not itself fire WHEN-VALIDATE-ITEM: Oracle's
        /// EDIT_TEXTITEM doesn't either: validation fires on navigation away
        /// from the item, same as any other edit, once the new value is
        /// in place.
        /// </summary>
        public async Task<EditorResult> ShowEditorAsync(string blockName, string itemName, CancellationToken ct = default)
        {
            var blockInfo = GetBlock(blockName);
            if (blockInfo?.UnitOfWork == null)
            {
                LogError($"ShowEditorAsync: block '{blockName}' not found or has no unit of work", null, blockName);
                return EditorResult.Cancel();
            }

            var currentRecord = blockInfo.UnitOfWork.CurrentItem;
            if (currentRecord == null)
            {
                LogError($"ShowEditorAsync: block '{blockName}' has no current record", null, blockName);
                return EditorResult.Cancel();
            }

            var item = _itemPropertyManager.GetItem(blockName, itemName);
            var editor = !string.IsNullOrWhiteSpace(item?.EditorName)
                ? GetEditor(item.EditorName) ?? EditorDefinition.SystemDefault()
                : EditorDefinition.SystemDefault();

            var currentValue = GetFieldValue(currentRecord, itemName)?.ToString();

            var result = await _editorProvider.ShowEditorAsync(editor, currentValue, ct).ConfigureAwait(false);
            if (result.Committed)
            {
                SetFieldValue(currentRecord, itemName, result.Value);
            }

            return result;
        }

        #endregion
    }
}
