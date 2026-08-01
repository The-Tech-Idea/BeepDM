using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Models;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    /// <summary>
    /// Message and alert built-ins partial class.
    /// Provides Oracle Forms MESSAGE / SHOW_ALERT / BELL equivalents.
    /// </summary>
    public partial class FormsManager
    {
        #region Status message state

        private StatusMessage _currentStatusMessage;

        /// <summary>Gets the current status area message (null if cleared)</summary>
        public StatusMessage CurrentMessage => _currentStatusMessage;

        #endregion

        #region Message Built-ins

        /// <summary>
        /// Display a message in the form status area.
        /// Corresponds to Oracle Forms MESSAGE built-in.
        /// </summary>
        public void SetMessage(string text, MessageLevel level = MessageLevel.Info)
        {
            _currentStatusMessage = new StatusMessage { Text = text, Level = level };
            Status = text;
            LogOperation($"Message [{level}]: {text}");

            // Also publish through the message queue, which is what raises
            // OnMessage.
            //
            // This method only recorded the message until 2026-08-01, while
            // IDataOperations.OnMessage — documented as "UI layers subscribe to
            // OnMessage/OnMessageCleared to display messages" — is raised solely
            // by MessageQueueManager. So the form-level MESSAGE built-in that
            // every host calls notified nobody, and a status line driven off the
            // documented event stayed blank however many messages the form set.
            // Two implementations of "set the message", and hosts reached the
            // one that told no one.
            // Replace rather than enqueue: MESSAGE overwrites the status line.
            _messageManager?.ReplaceMessage(MessageScope(), text, level);
        }

        /// <summary>
        /// Clear the current status message.
        /// </summary>
        public void ClearMessage()
        {
            _currentStatusMessage = null;
            Status = "Ready";
            _messageManager?.ClearMessage(MessageScope());
        }

        /// <summary>
        /// The queue key for a form-level message. The queue is keyed per block;
        /// the MESSAGE built-in is form-level, so it rides on whichever block is
        /// current, and on an empty key before any block has been entered.
        /// </summary>
        private string MessageScope() => _currentBlockName ?? string.Empty;

        #endregion

        #region Alert Built-ins

        /// <summary>
        /// Show a modal alert dialog.
        /// Corresponds to Oracle Forms SHOW_ALERT built-in.
        /// The result indicates which button the user pressed.
        /// </summary>
        public async Task<AlertResult> ShowAlertAsync(
            string title,
            string message,
            AlertStyle style = AlertStyle.None,
            string button1Text = "OK",
            string button2Text = null,
            string button3Text = null,
            CancellationToken ct = default)
        {
            try
            {
                return await _alertProvider.ShowAlertAsync(
                    title, message, style, button1Text, button2Text, button3Text, ct);
            }
            catch (Exception ex)
            {
                LogError($"Error showing alert '{title}'", ex);
                return AlertResult.None;
            }
        }

        /// <summary>
        /// Convenience overload: single-button information alert.
        /// </summary>
        public Task<AlertResult> ShowInfoAsync(string title, string message, CancellationToken ct = default)
            => ShowAlertAsync(title, message, AlertStyle.Information, "OK", null, null, ct);

        /// <summary>
        /// Convenience overload: two-button Yes/No question alert.
        /// Returns true if user pressed Button1 (Yes).
        /// </summary>
        public async Task<bool> ConfirmAsync(string title, string message, CancellationToken ct = default)
        {
            var result = await ShowAlertAsync(title, message, AlertStyle.Question, "Yes", "No", null, ct);
            return result == AlertResult.Button1;
        }

        #endregion
    }
}
