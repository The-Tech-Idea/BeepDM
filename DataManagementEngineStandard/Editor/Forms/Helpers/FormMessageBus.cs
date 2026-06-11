using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.Forms.Models;

namespace TheTechIdea.Beep.Editor.Forms.Helpers
{
    /// <summary>
    /// Pub/sub message bus for inter-form communication.
    /// Pass a single shared instance to all FormsManager constructors so each form
    /// can post and receive typed messages from any other form.
    /// </summary>
    public class FormMessageBus : IFormMessageBus
    {
        // key = "formName|messageType"
        private readonly ConcurrentDictionary<string, List<Action<FormMessage>>> _subscriptions
            = new ConcurrentDictionary<string, List<Action<FormMessage>>>(StringComparer.OrdinalIgnoreCase);

        private readonly object _subLock = new object();

        /// <inheritdoc/>
        public event EventHandler<FormMessageEventArgs> OnFormMessage;

        /// <inheritdoc/>
        public void PostMessage(string targetForm, string messageType, object payload, string senderForm = null)
        {
            var msg = new FormMessage
            {
                TargetForm = targetForm,
                MessageType = messageType,
                Payload = payload,
                SenderForm = senderForm
            };
            OnFormMessage?.Invoke(this, new FormMessageEventArgs { Message = msg });
            DispatchToSubscribers(targetForm, messageType, msg);
        }

        /// <inheritdoc/>
        public void Broadcast(string messageType, object payload, string senderForm = null)
        {
            var msg = new FormMessage
            {
                TargetForm = "*",
                MessageType = messageType,
                Payload = payload,
                SenderForm = senderForm
            };
            OnFormMessage?.Invoke(this, new FormMessageEventArgs { Message = msg });

            // Dispatch to all subscribers for this messageType across all forms
            var snapshot = new List<string>(_subscriptions.Keys);
            foreach (var key in snapshot)
            {
                var parts = key.Split('|');
                if (parts.Length == 2 && string.Equals(parts[1], messageType, StringComparison.OrdinalIgnoreCase))
                    DispatchToSubscribers(parts[0], messageType, msg);
            }
        }

        /// <inheritdoc/>
        public void Subscribe(string formName, string messageType, Action<FormMessage> handler)
        {
            if (string.IsNullOrWhiteSpace(formName) || string.IsNullOrWhiteSpace(messageType) || handler == null)
                return;
            var key = BuildKey(formName, messageType);
            lock (_subLock)
            {
                if (!_subscriptions.TryGetValue(key, out var list))
                {
                    list = new List<Action<FormMessage>>();
                    _subscriptions[key] = list;
                }
                list.Add(handler);
            }
        }

        /// <inheritdoc/>
        public void Unsubscribe(string formName, string messageType)
            => _subscriptions.TryRemove(BuildKey(formName, messageType), out _);

        /// <inheritdoc/>
        public void UnsubscribeAll(string formName)
        {
            if (string.IsNullOrWhiteSpace(formName)) return;
            var prefix = formName.ToUpperInvariant() + "|";
            foreach (var key in new List<string>(_subscriptions.Keys))
                if (key.ToUpperInvariant().StartsWith(prefix))
                    _subscriptions.TryRemove(key, out _);
        }

        private static string BuildKey(string formName, string messageType)
            => $"{formName}|{messageType}";

        private void DispatchToSubscribers(string formName, string messageType, FormMessage msg)
        {
            var key = BuildKey(formName, messageType);
            if (!_subscriptions.TryGetValue(key, out var list)) return;
            List<Action<FormMessage>> snapshot;
            lock (_subLock) { snapshot = new List<Action<FormMessage>>(list); }
            foreach (var h in snapshot)
            {
                // We intentionally swallow handler exceptions here so one bad
                // subscriber does not abort the dispatch to the rest of the
                // subscribers on the same form/type. The bus stays up; the
                // exception is logged so the operator can spot the bad
                // subscriber. Without the log, a subscriber whose handler
                // throws on every dispatch would silently drop all of its
                // messages with no signal at all.
                try
                {
                    h(msg);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(
                        $"[FormMessageBus] Subscriber handler threw for '{key}' " +
                        $"(message type '{messageType}', payload type " +
                        $"'{msg.Payload?.GetType().Name ?? "<null>"}'): {ex.Message}");
                }
            }
        }
    }
}
