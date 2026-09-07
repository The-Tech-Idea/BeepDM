using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    /// <summary>
    /// Form menu invocation. The menu itself lives on <c>Menu</c>
    /// (<see cref="Helpers.FormMenuManager"/>); dispatch lives here because a
    /// built-in item runs the very form operations this class already exposes.
    /// </summary>
    public partial class FormsManager
    {
        /// <summary>
        /// Invokes a form menu item by id. A built-in runs the matching form
        /// operation; a MenuItemTrigger fires <see cref="TriggerType.WhenMenuItem"/>
        /// with the item id in context so one handler can dispatch on it; a
        /// CallForm calls the named form. A separator or submenu is a no-op.
        /// </summary>
        public async Task<IErrorsInfo> InvokeMenuItemAsync(string itemId)
        {
            var result = new ErrorsInfo { Flag = Errors.Ok };

            var item = _formMenuManager?.FindItem(itemId);
            if (item == null)
            {
                result.Flag = Errors.Failed;
                result.Message = $"No menu item '{itemId}' is registered.";
                return result;
            }

            try
            {
                switch (item.Kind)
                {
                    case FormMenuItemKind.Separator:
                    case FormMenuItemKind.Submenu:
                        return result;   // nothing to invoke

                    case FormMenuItemKind.CallForm:
                        if (string.IsNullOrWhiteSpace(item.CommandName))
                            return Fail(result, $"Menu item '{itemId}' is a CallForm with no target form.");
                        await CallFormAsync(item.CommandName).ConfigureAwait(false);
                        return result;

                    case FormMenuItemKind.MenuItemTrigger:
                    {
                        var ctx = TriggerContext.ForForm(
                            TriggerType.WhenMenuItem, _currentFormName ?? string.Empty, _dmeEditor);
                        ctx.Parameters["MenuItemId"] = itemId;
                        ctx.Parameters["MenuItemCommand"] = item.CommandName ?? string.Empty;
                        await _triggerManager.FireFormTriggerAsync(
                            TriggerType.WhenMenuItem, _currentFormName ?? string.Empty, ctx)
                            .ConfigureAwait(false);
                        return result;
                    }

                    case FormMenuItemKind.BuiltIn:
                    default:
                        return await InvokeBuiltInAsync(item, result).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                LogError($"Menu item '{itemId}' failed", ex);
                return Fail(result, $"Menu item '{itemId}' failed: {ex.Message}");
            }
        }

        private async Task<IErrorsInfo> InvokeBuiltInAsync(FormMenuItem item, ErrorsInfo result)
        {
            var block = string.IsNullOrWhiteSpace(item.BlockName) ? _currentBlockName : item.BlockName;
            var command = (item.CommandName ?? string.Empty).Trim();

            // The record-scoped built-ins need a block; the form-scoped ones do not.
            switch (command.ToLowerInvariant())
            {
                case "executequery":
                    await ExecuteQueryAsync(block).ConfigureAwait(false); return result;
                case "enterquery":
                    await EnterQueryAsync(block).ConfigureAwait(false); return result;
                case "commit":
                case "commitform":
                    return await CommitFormAsync().ConfigureAwait(false);
                case "rollback":
                case "rollbackform":
                    return await RollbackFormAsync().ConfigureAwait(false);
                case "clearform":
                    await CloseFormAsync().ConfigureAwait(false); return result;
                case "nextrecord":
                    await NextRecordAsync(block).ConfigureAwait(false); return result;
                case "previousrecord":
                    await PreviousRecordAsync(block).ConfigureAwait(false); return result;
                case "firstrecord":
                    await FirstRecordAsync(block).ConfigureAwait(false); return result;
                case "lastrecord":
                    await LastRecordAsync(block).ConfigureAwait(false); return result;
                case "createrecord":
                case "insertrecord":
                    await InsertRecordAsync(block).ConfigureAwait(false); return result;
                case "deleterecord":
                    await DeleteCurrentRecordAsync(block).ConfigureAwait(false); return result;
                case "exitform":
                    await CloseFormAsync().ConfigureAwait(false); return result;
                default:
                    return Fail(result,
                        $"Menu item command '{item.CommandName}' is not a known built-in.");
            }
        }

        private static ErrorsInfo Fail(ErrorsInfo result, string message)
        {
            result.Flag = Errors.Failed;
            result.Message = message;
            return result;
        }
    }
}
