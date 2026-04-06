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

        public int MessageDisplayDurationMs { get; set; } = 3000;
        public bool AutoAdvanceMessages { get; set; } = true;

        public event EventHandler<BlockMessageEventArgs> OnMessage;
        public event EventHandler<BlockMessageEventArgs> OnMessageCleared;

        #region Set / Clear

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

        public void ShowInfoMessage(string blockName, string text)
            => SetMessage(blockName, text, MessageLevel.Info);

        public void ShowSuccessMessage(string blockName, string text)
            => SetMessage(blockName, text, MessageLevel.Success);

        public void ShowWarningMessage(string blockName, string text)
            => SetMessage(blockName, text, MessageLevel.Warning);

        public void ShowErrorMessage(string blockName, string text)
            => SetMessage(blockName, text, MessageLevel.Error);

        #endregion

        #region Query

        public string GetCurrentMessage(string blockName)
            => _current.TryGetValue(blockName, out var m) ? m?.Text : null;

        public MessageLevel GetCurrentMessageLevel(string blockName)
            => _current.TryGetValue(blockName, out var m) && m != null
               ? m.Level
               : MessageLevel.Info;

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
