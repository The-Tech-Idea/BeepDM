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
    /// <see cref="FireOnDeleteAsync"/> helpers that fire the new
    /// <see cref="TriggerType.OnInsert"/> / <see cref="TriggerType.OnUpdate"/> /
    /// <see cref="TriggerType.OnDelete"/> triggers added in Phase 4.
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
            var result = await _triggerManager.FireBlockTriggerAsync(TriggerType.OnInsert, blockName, ctx);

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
            var result = await _triggerManager.FireBlockTriggerAsync(TriggerType.OnUpdate, blockName, ctx);

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
            var result = await _triggerManager.FireBlockTriggerAsync(TriggerType.OnDelete, blockName, ctx);

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
                return await _triggerManager.FireBlockTriggerAsync(type, block, ctx);
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
