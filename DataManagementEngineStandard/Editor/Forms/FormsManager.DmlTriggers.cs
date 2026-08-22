using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    /// <summary>
    /// Phase 4.2 — DML Trigger wrappers.
    /// Provides <see cref="FireOnInsertAsync"/>, <see cref="FireOnUpdateAsync"/>,
    /// <see cref="FireOnDeleteAsync"/>, <see cref="FireOnLockAsync"/> helpers
    /// that fire the <see cref="TriggerType.OnInsert"/> /
    /// <see cref="TriggerType.OnUpdate"/> / <see cref="TriggerType.OnDelete"/> /
    /// <see cref="TriggerType.OnLock"/> triggers added in Phase 4.
    /// Also provides the Oracle Forms RAISE_FORM_TRIGGER built-in via
    /// <see cref="RaiseFormTriggerAsync"/>.
    /// </summary>
    public partial class FormsManager
    {
        // ─────────────────────────────────────────────────────────────────────
        // ON-INSERT / ON-UPDATE / ON-DELETE helpers
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Fire the ON-INSERT trigger for a block.
        /// When a handler is registered it replaces the default UoW insert.
        /// Returns true when the insert was handled (trigger ran successfully).
        /// Returns false when no trigger is registered (fall through to UoW default).
        /// Returns null on cancellation or error.
        /// </summary>
        public async Task<bool?> FireOnInsertAsync(string blockName, object record)
        {
            if (string.IsNullOrEmpty(blockName)) return false;

            if (!(_triggerManager.GetBlockTriggers(TriggerType.OnInsert, blockName)?.Count > 0))
                return false;

            var ctx = TriggerContext.ForBlock(TriggerType.OnInsert, blockName, record, _dmeEditor);
            var result = await _triggerManager.FireBlockTriggerAsync(TriggerType.OnInsert, blockName, ctx).ConfigureAwait(false);

            if (result == TriggerResult.Cancelled || result == TriggerResult.Failure)
                return null;

            return true;
        }

        /// <summary>
        /// Fire the ON-UPDATE trigger for a block.
        /// Returns true when handled, false when no trigger registered, null on error/cancel.
        /// </summary>
        public async Task<bool?> FireOnUpdateAsync(string blockName, object record)
        {
            if (string.IsNullOrEmpty(blockName)) return false;

            if (!(_triggerManager.GetBlockTriggers(TriggerType.OnUpdate, blockName)?.Count > 0))
                return false;

            var ctx = TriggerContext.ForBlock(TriggerType.OnUpdate, blockName, record, _dmeEditor);
            var result = await _triggerManager.FireBlockTriggerAsync(TriggerType.OnUpdate, blockName, ctx).ConfigureAwait(false);

            if (result == TriggerResult.Cancelled || result == TriggerResult.Failure)
                return null;

            return true;
        }

        /// <summary>
        /// Fire the ON-DELETE trigger for a block.
        /// Returns true when handled, false when no trigger registered, null on error/cancel.
        /// </summary>
        public async Task<bool?> FireOnDeleteAsync(string blockName, object record)
        {
            if (string.IsNullOrEmpty(blockName)) return false;

            if (!(_triggerManager.GetBlockTriggers(TriggerType.OnDelete, blockName)?.Count > 0))
                return false;

            var ctx = TriggerContext.ForBlock(TriggerType.OnDelete, blockName, record, _dmeEditor);
            var result = await _triggerManager.FireBlockTriggerAsync(TriggerType.OnDelete, blockName, ctx).ConfigureAwait(false);

            if (result == TriggerResult.Cancelled || result == TriggerResult.Failure)
                return null;

            return true;
        }

        /// <summary>
        /// Fire the ON-LOCK trigger for a block.
        /// When a handler is registered it replaces the default client-side
        /// record lock (<c>LockManager.LockCurrentRecordAsync</c>) — e.g. a
        /// custom SELECT FOR UPDATE against a different locking scheme.
        /// Returns true when handled, false when no trigger registered
        /// (fall through to the default lock), null on cancellation or error.
        /// Added 2026-08-22 — the <see cref="TriggerType.OnLock"/> member
        /// existed with no firing code anywhere.
        /// </summary>
        public async Task<bool?> FireOnLockAsync(string blockName, object record)
        {
            if (string.IsNullOrEmpty(blockName)) return false;

            if (!(_triggerManager.GetBlockTriggers(TriggerType.OnLock, blockName)?.Count > 0))
                return false;

            var ctx = TriggerContext.ForBlock(TriggerType.OnLock, blockName, record, _dmeEditor);
            var result = await _triggerManager.FireBlockTriggerAsync(TriggerType.OnLock, blockName, ctx).ConfigureAwait(false);

            if (result == TriggerResult.Cancelled || result == TriggerResult.Failure)
                return null;

            return true;
        }

        /// <summary>
        /// Fire the ON-ROLLBACK trigger for a block.
        /// When a handler is registered it replaces the default block rollback
        /// (<c>DirtyStateManager.RollbackDirtyBlocksAsync</c> for this block) —
        /// e.g. custom statement-level rollback processing. Returns true when
        /// handled, false when no trigger registered (fall through to the
        /// default rollback), null on cancellation or error. Added
        /// 2026-08-22 — the <see cref="TriggerType.OnRollback"/> member existed
        /// with no firing code anywhere.
        /// </summary>
        public async Task<bool?> FireOnRollbackAsync(string blockName)
        {
            if (string.IsNullOrEmpty(blockName)) return false;

            if (!(_triggerManager.GetBlockTriggers(TriggerType.OnRollback, blockName)?.Count > 0))
                return false;

            var ctx = TriggerContext.ForBlock(TriggerType.OnRollback, blockName, null, _dmeEditor);
            var result = await _triggerManager.FireBlockTriggerAsync(TriggerType.OnRollback, blockName, ctx).ConfigureAwait(false);

            if (result == TriggerResult.Cancelled || result == TriggerResult.Failure)
                return null;

            return true;
        }

        /// <summary>
        /// Fires ON-INSERT for every record about to be written during a
        /// commit of the given blocks, giving a registered handler a chance to
        /// run custom logic and to cancel the commit. Returns false when a
        /// handler cancels or errors; true otherwise (including "no handler
        /// registered").
        /// </summary>
        /// <remarks>
        /// Known, deliberate limitation added 2026-08-22: this does NOT
        /// exclude a "handled" record from the default insert that runs
        /// afterwards in <c>DirtyStateManager.SaveDirtyBlocksAsync</c> — full
        /// Oracle ON-INSERT semantics ("the trigger's own write replaces the
        /// default write, so there is exactly one write") would require
        /// intercepting individual records inside OBL's
        /// <c>CommitAllAsync</c> batch, which is out of scope for this change.
        /// A registered ON-INSERT handler today runs (and can cancel the
        /// commit) alongside the default insert, not instead of it. Before
        /// this change it did not run at all — see
        /// <c>DataManagementEngineStandard/Editor/Forms/gaps.md</c>. Do not
        /// build an ON-INSERT handler that assumes the default write is
        /// skipped until that follow-up lands.
        /// </remarks>
        private async Task<bool> FireOnInsertForDirtyBlocksAsync(IEnumerable<string> blockNames)
        {
            foreach (var blockName in blockNames)
            {
                var blockInfo = GetBlock(blockName);
                if (blockInfo?.UnitOfWork == null) continue;

                if (!(_triggerManager.GetBlockTriggers(TriggerType.OnInsert, blockName)?.Count > 0))
                    continue;

                System.Collections.IList insertedItems = null;
                try
                {
                    dynamic dynUoW = blockInfo.UnitOfWork;
                    insertedItems = (System.Collections.IList)dynUoW.GetInsertedItems();
                }
                catch
                {
                    // Optional — some IUnitofWork implementations don't expose
                    // GetInsertedItems. Same tolerance DirtyStateManager's own
                    // master-key propagation already applies to this call.
                }

                if (insertedItems == null) continue;

                foreach (var record in insertedItems)
                {
                    var outcome = await FireOnInsertAsync(blockName, record).ConfigureAwait(false);
                    if (outcome == null)
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Fire the ON-CHECK-DELETE-MASTER trigger for a (master, detail) pair.
        /// When a handler is registered it replaces the default
        /// isolated/non-isolated/cascading check
        /// (<see cref="DataBlockRelationship.DeleteBehavior"/>) for that
        /// relationship — the handler decides whether the master delete may
        /// proceed. Returns true when handled (skip the default check
        /// entirely), false when no trigger registered (apply
        /// <see cref="DataBlockRelationship.DeleteBehavior"/>), null on
        /// cancellation or error (block the delete). Added 2026-08-22 — the
        /// <see cref="TriggerType.OnCheckDeleteMaster"/> member existed with
        /// no firing code, and no isolated/non-isolated/cascading distinction
        /// existed anywhere in the engine to fall back to.
        /// </summary>
        public async Task<bool?> FireOnCheckDeleteMasterAsync(string masterBlockName, string detailBlockName, object masterRecord)
        {
            if (string.IsNullOrEmpty(masterBlockName)) return false;

            if (!(_triggerManager.GetBlockTriggers(TriggerType.OnCheckDeleteMaster, masterBlockName)?.Count > 0))
                return false;

            var ctx = TriggerContext.ForBlock(TriggerType.OnCheckDeleteMaster, masterBlockName, masterRecord, _dmeEditor);
            ctx.SourceBlock = masterBlockName;
            ctx.TargetBlock = detailBlockName;
            var result = await _triggerManager.FireBlockTriggerAsync(TriggerType.OnCheckDeleteMaster, masterBlockName, ctx).ConfigureAwait(false);

            if (result == TriggerResult.Cancelled || result == TriggerResult.Failure)
                return null;

            return true;
        }

        // ─────────────────────────────────────────────────────────────────────
        // RAISE_FORM_TRIGGER built-in
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Programmatically fire a trigger by name on the current form.
        /// Equivalent to Oracle Forms RAISE_FORM_TRIGGER('TRIGGER_NAME').
        /// Parses the name against <see cref="TriggerType"/> case-insensitively.
        /// </summary>
        /// <param name="triggerName">Oracle trigger name, e.g. "WHEN-BUTTON-PRESSED".</param>
        /// <param name="blockName">Block scope (null → form scope).</param>
        /// <returns>The <see cref="TriggerResult"/> returned by the handler.</returns>
        /// <exception cref="ArgumentException">When <paramref name="triggerName"/> does not map to a known TriggerType.</exception>
        public async Task<TriggerResult> RaiseFormTriggerAsync(
            string triggerName,
            string blockName = null)
        {
            if (string.IsNullOrWhiteSpace(triggerName))
                throw new ArgumentException("triggerName is required", nameof(triggerName));

            // Normalise Oracle Forms style "WHEN-BUTTON-PRESSED" → "WhenButtonPressed"
            var normalised = NormaliseTriggerName(triggerName);

            if (!Enum.TryParse<TriggerType>(normalised, ignoreCase: true, out var type))
            {
                // Try the raw name as-is
                if (!Enum.TryParse<TriggerType>(triggerName, ignoreCase: true, out type))
                    throw new ArgumentException(
                        $"'{triggerName}' does not map to a known TriggerType.", nameof(triggerName));
            }

            var block = blockName ?? _currentBlockName;

            if (string.IsNullOrEmpty(block))
            {
                var ctx = TriggerContext.ForForm(type, _currentFormName ?? string.Empty, _dmeEditor);
                return await _triggerManager.FireFormTriggerAsync(type, _currentFormName ?? string.Empty, ctx);
            }
            else
            {
                var ctx = TriggerContext.ForBlock(type, block, null, _dmeEditor);
                return await _triggerManager.FireBlockTriggerAsync(type, block, ctx).ConfigureAwait(false);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Private helpers
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Converts Oracle Forms trigger names like "WHEN-BUTTON-PRESSED" to
        /// the PascalCase equivalent "WhenButtonPressed" used in the TriggerType enum.
        /// </summary>
        private static string NormaliseTriggerName(string name)
        {
            var parts = name.Split('-');
            var result = new System.Text.StringBuilder();
            foreach (var part in parts)
            {
                if (part.Length == 0) continue;
                result.Append(char.ToUpperInvariant(part[0]));
                if (part.Length > 1)
                    result.Append(part.Substring(1).ToLowerInvariant());
            }
            return result.ToString();
        }
    }
}
