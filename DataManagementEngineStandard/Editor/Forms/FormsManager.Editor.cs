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
    public partial class FormsManager : IEditorRegistry, IFormsEditorOutcomes
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
            var result = await ShowEditorWithOutcomeAsync(blockName, itemName, ct).ConfigureAwait(false);
            return result.Committed ? EditorResult.Ok(result.Value) : EditorResult.Cancel();
        }

        private readonly AsyncLocal<int> _editorOperationDepth = new();

        public async Task<FormEditorResult> ShowEditorWithOutcomeAsync(string blockName, string itemName,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var lifetime = TryEnterCallback() ?? throw new ObjectDisposedException(nameof(FormsManager));
            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _operationLifetime.Token);
            var ct = cancellation.Token;
            var result = new FormEditorResult { FormInstanceId = _commitFormInstanceId, BlockName = blockName, FieldName = itemName };
            if (_editorOperationDepth.Value > 0)
            {
                result.State = FormEditorState.Failed;
                result.ErrorMessage = "Awaited nested editor operations from their own providers are not supported.";
                return result;
            }
            _editorOperationDepth.Value++;
            try
            {
                if (string.IsNullOrWhiteSpace(itemName)) throw new ArgumentException("Editor field name is required.", nameof(itemName));
                var target = CaptureRecordTarget(blockName, itemName, "Editor");
                blockName = target.Registration.Name;
                result.BlockName = blockName;
                result.RegistrationId = target.Registration.Identity;
                result.RequestRevision = target.Request;
                if (target.Record == null) throw new InvalidOperationException("Editor has no captured current record.");
                var item = _itemPropertyManager.GetItem(blockName, itemName);
                var editorName = item?.EditorName;
                var definition = GetEditor(editorName);
                var snapshot = definition == null ? EditorDefinition.SystemDefault() : new EditorDefinition(
                    definition.Name, definition.Title, definition.Width, definition.Height, definition.WrapText, definition.ShowScrollBar)
                    { CreatedAt = definition.CreatedAt };
                // Keep a separate comparison copy: the borrowed provider may mutate its input.
                var expected = new EditorDefinition(snapshot.Name, snapshot.Title, snapshot.Width, snapshot.Height,
                    snapshot.WrapText, snapshot.ShowScrollBar) { CreatedAt = snapshot.CreatedAt };
                void Verify()
                {
                    VerifyRecordTarget(target, ct);
                    if (item?.EditorName != editorName || !ReferenceEquals(GetEditor(editorName), definition) ||
                        definition != null && (definition.Name != expected.Name || definition.Title != expected.Title ||
                            definition.Width != expected.Width || definition.Height != expected.Height ||
                            definition.WrapText != expected.WrapText || definition.ShowScrollBar != expected.ShowScrollBar ||
                            definition.CreatedAt != expected.CreatedAt)) throw new SupersededRecordOperationException();
                    VerifyEditorEditable(target);
                    VerifyRecordTarget(target, ct);
                }
                Verify();
                var currentValue = target.Values[itemName]?.ToString();
                Verify();
                result.ProviderInvoked = true;
                var providerResult = await _editorProvider.ShowEditorAsync(snapshot, currentValue, ct).ConfigureAwait(false);
                result.ProviderAcknowledged = providerResult != null;
                result.ProviderCommitted = providerResult?.Committed == true;
                var value = providerResult?.Value;
                Verify();
                if (providerResult == null) throw new InvalidOperationException("Editor provider returned no result.");
                if (!result.ProviderCommitted) { result.State = FormEditorState.Cancelled; return result; }
                result.WriteEffectsPossible = true;
                if (!WriteCapturedRecordField(target, itemName, value, ct, () => result.WriteAcknowledged = true))
                    throw new InvalidOperationException("Editor setter did not acknowledge the write.");
                Verify();
                result.Value = target.Values[itemName]?.ToString();
                Verify();
                result.State = FormEditorState.Completed;
                return result;
            }
            catch (Exception ex)
            {
                result.Value = null;
                result.State = ct.IsCancellationRequested ? FormEditorState.Cancelled :
                    ex is SupersededRecordOperationException ? FormEditorState.Superseded :
                    ex is EditorDeniedException ? FormEditorState.Denied : FormEditorState.Failed;
                result.ErrorMessage = ex.Message;
                result.Exception = ex;
                return result;
            }
            finally { _editorOperationDepth.Value--; }
        }

        private sealed class EditorDeniedException : InvalidOperationException
        {
            internal EditorDeniedException() : base("Editor target is not editable or cannot disclose raw text.") { }
        }

        private void VerifyEditorEditable(RecordTarget target)
        {
            var block = target.Registration.Block;
            var item = _itemPropertyManager.GetItem(target.Registration.Name, target.Field);
            var tracking = target.Registration.Source.GetTrackingItem(target.Record);
            var inserting = block.Mode == Models.DataBlockMode.Insert || tracking?.IsNew == true || tracking?.EntityState == EntityState.Added;
            var permission = inserting ? SecurityPermission.Insert : SecurityPermission.Update;
            var fieldSecurity = _securityManager?.GetFieldSecurity(target.Registration.Name, target.Field);
            var admin = _securityManager?.CurrentContext?.IsAdmin == true;
            if (block.Mode == Models.DataBlockMode.EnterQuery || block.Mode == Models.DataBlockMode.ReadOnly ||
                !(inserting ? block.InsertAllowed : block.UpdateAllowed) ||
                item != null && (!item.Enabled || !item.Visible || !(inserting ? item.InsertAllowed : item.UpdateAllowed)) ||
                !IsBlockAllowed(target.Registration.Name, permission) ||
                fieldSecurity != null && (fieldSecurity.Masked || !admin && (!fieldSecurity.Editable || !fieldSecurity.Visible)))
                throw new EditorDeniedException();
        }

        #endregion
    }
}
