using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;

namespace TheTechIdea.Beep.Editor.UOWManager.Helpers
{
    /// <summary>
    /// Platform-agnostic message queue manager.
    /// Replaces the WinForms Timer-based BeepDataBlock.Messages.cs.
    /// Display timing is the responsibility of the platform layer,
    /// which subscribes to OnMessage and OnMessageCleared.
    /// </summary>
    public class MessageQueueManager : IMessageQueueManager
    {
        private readonly Dictionary<string, BlockMessage> _current
            = new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, Queue<BlockMessage>> _queues
            = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets or sets the intended display duration for a message before platform code advances it.
        /// </summary>
        public int MessageDisplayDurationMs { get; set; } = 3000;

        /// <summary>
        /// Gets or sets whether the manager should automatically advance to the next queued message after a clear.
        /// </summary>
        public bool AutoAdvanceMessages { get; set; } = true;

        /// <summary>
        /// Raised when a message becomes current for a block.
        /// </summary>
        public event EventHandler<BlockMessageEventArgs> OnMessage;

        /// <summary>
        /// Raised when the current message for a block is cleared.
        /// </summary>
        public event EventHandler<BlockMessageEventArgs> OnMessageCleared;

        #region Set / Clear

        /// <summary>
        /// Sets the current message for a block or queues it if another message is already being shown.
        /// </summary>
        public void SetMessage(string blockName, string text, MessageLevel level = MessageLevel.Info)
        {
            var message = new BlockMessage
            {
                BlockName = blockName,
                Text = text,
                Level = level,
                Timestamp = DateTime.UtcNow
            };

            if (!_current.TryGetValue(blockName, out var existing) || existing == null)
            {
                _current[blockName] = message;
                OnMessage?.Invoke(this, new BlockMessageEventArgs { Message = message });
            }
            else
            {
                EnsureQueue(blockName);
                _queues[blockName].Enqueue(message);
            }
        }

        /// <summary>
        /// Clears the current message for a block and optionally advances to the next queued message.
        /// </summary>
        public void ClearMessage(string blockName)
        {
            _current[blockName] = null;
            OnMessageCleared?.Invoke(this, new BlockMessageEventArgs
            {
                Message = new BlockMessage { BlockName = blockName },
                IsClear = true
            });
            AdvanceMessage(blockName);
        }

        /// <summary>
        /// Advances to the next queued message for a block, if any exist.
        /// </summary>
        public void AdvanceMessage(string blockName)
        {
            if (_queues.TryGetValue(blockName, out var queue) && queue.Count > 0)
            {
                var next = queue.Dequeue();
                _current[blockName] = next;
                OnMessage?.Invoke(this, new BlockMessageEventArgs { Message = next });
            }
        }

        #endregion

        #region Convenience

        /// <summary>
        /// Queues an informational message for a block.
        /// </summary>
        public void ShowInfoMessage(string blockName, string text)
            => SetMessage(blockName, text, MessageLevel.Info);

        /// <summary>
        /// Queues a success message for a block.
        /// </summary>
        public void ShowSuccessMessage(string blockName, string text)
            => SetMessage(blockName, text, MessageLevel.Success);

        /// <summary>
        /// Queues a warning message for a block.
        /// </summary>
        public void ShowWarningMessage(string blockName, string text)
            => SetMessage(blockName, text, MessageLevel.Warning);

        /// <summary>
        /// Queues an error message for a block.
        /// </summary>
        public void ShowErrorMessage(string blockName, string text)
            => SetMessage(blockName, text, MessageLevel.Error);

        #endregion

        #region Query

        /// <summary>
        /// Returns the current message text for a block.
        /// </summary>
        public string GetCurrentMessage(string blockName)
            => _current.TryGetValue(blockName, out var m) ? m?.Text : null;

        /// <summary>
        /// Returns the level of the current message for a block.
        /// </summary>
        public MessageLevel GetCurrentMessageLevel(string blockName)
            => _current.TryGetValue(blockName, out var m) && m != null
               ? m.Level
               : MessageLevel.Info;

        /// <summary>
        /// Returns the number of queued messages waiting behind the current message for a block.
        /// </summary>
        public int GetQueuedMessageCount(string blockName)
            => _queues.TryGetValue(blockName, out var q) ? q.Count : 0;

        #endregion

        private void EnsureQueue(string blockName)
        {
            if (!_queues.ContainsKey(blockName))
                _queues[blockName] = new Queue<BlockMessage>();
        }
    }
}
