using System;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    /// <summary>
    /// Phase 4.1 — Key Trigger wrappers.
    /// Provides <see cref="RegisterKeyTrigger"/> / <see cref="FireKeyTriggerAsync"/> helpers
    /// that map <see cref="KeyTriggerType"/> values to matching <see cref="TriggerType"/>
    /// enum values on the underlying <see cref="ITriggerManager"/>.
    /// Also wires default keyboard actions: KEY-ENTER → commit, KEY-EXIT → close, etc.
    /// </summary>
    public partial class FormsManager
    {
        // ─────────────────────────────────────────────────────────────────────
        // Registration helpers
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Register a synchronous handler for a KEY-* trigger on the specified block.
        /// </summary>
        /// <param name="key">Logical key to intercept.</param>
        /// <param name="blockName">Block name (or null for form-level).</param>
        /// <param name="handler">Handler; return <see cref="TriggerResult.Cancelled"/> to suppress the default action.</param>
        public void RegisterKeyTrigger(
            KeyTriggerType key,
            string blockName,
            Func<TriggerContext, TriggerResult> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var type = (TriggerType)(int)key;

            if (string.IsNullOrEmpty(blockName))
                _triggerManager.RegisterFormTrigger(type, _currentFormName ?? string.Empty, handler);
            else
                _triggerManager.RegisterBlockTrigger(type, blockName, handler);
        }

        /// <summary>
        /// Register an async handler for a KEY-* trigger on the specified block.
        /// </summary>
        public void RegisterKeyTriggerAsync(
            KeyTriggerType key,
            string blockName,
            Func<TriggerContext, CancellationToken, Task<TriggerResult>> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var type = (TriggerType)(int)key;

            if (string.IsNullOrEmpty(blockName))
                _triggerManager.RegisterFormTriggerAsync(type, _currentFormName ?? string.Empty, handler);
            else
                _triggerManager.RegisterBlockTriggerAsync(type, blockName, handler);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Fire helpers
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Fire a KEY-* trigger on the current block and — if not cancelled —
        /// execute the default built-in action.
        /// </summary>
        /// <param name="key">The key trigger to fire.</param>
        /// <param name="blockName">Override block name; defaults to <see cref="CurrentBlockName"/>.</param>
        /// <returns>True when the action completed without cancellation.</returns>
        public async Task<bool> FireKeyTriggerAsync(
            KeyTriggerType key,
            string blockName = null)
        {
            var block = blockName ?? _currentBlockName;
            var type  = (TriggerType)(int)key;
            var ctx   = TriggerContext.ForBlock(type, block ?? string.Empty, null, _dmeEditor);
            ctx.KeyCode = (int)key;

            TriggerResult result;
            if (string.IsNullOrEmpty(block))
                result = await _triggerManager.FireFormTriggerAsync(type, _currentFormName ?? string.Empty, ctx);
            else
                result = await _triggerManager.FireBlockTriggerAsync(type, block, ctx);

            if (result == TriggerResult.Cancelled)
                return false;

            // Default built-in actions (replicate Oracle Forms KEY-* defaults)
            return await ExecuteKeyDefaultActionAsync(key, block);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Default built-in key actions
        // ─────────────────────────────────────────────────────────────────────

        private async Task<bool> ExecuteKeyDefaultActionAsync(KeyTriggerType key, string blockName)
        {
            switch (key)
            {
                case KeyTriggerType.Enter:
                case KeyTriggerType.Commit:
                    var commitResult = await CommitFormAsync();
                    return commitResult?.Flag == Errors.Ok;

                case KeyTriggerType.Exit:
                    return await CloseFormAsync();

                case KeyTriggerType.ExecuteQuery:
                    if (!string.IsNullOrEmpty(blockName))
                        return await ExecuteQueryAsync(blockName);
                    return false;

                case KeyTriggerType.NextRecord:
                    if (!string.IsNullOrEmpty(blockName))
                        return await NextRecordAsync(blockName);
                    return false;

                case KeyTriggerType.PreviousRecord:
                    if (!string.IsNullOrEmpty(blockName))
                        return await PreviousRecordAsync(blockName);
                    return false;

                case KeyTriggerType.NextBlock:
                    return await NextBlockAsync();

                case KeyTriggerType.PreviousBlock:
                    return await PreviousBlockAsync();

                // Keys with no default action — consumed silently
                default:
                    return true;
            }
        }
    }
}
