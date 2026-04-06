using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.UOW;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.Forms.Models;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    public partial class FormsManager
    {
        #region Oracle Forms Inter-Form Communication Built-ins

        // ── Global variables (:GLOBAL.*) ────────────────────────────────────

        /// <summary>
        /// Set a global variable visible to all forms sharing this registry.
        /// Equivalent to Oracle Forms :GLOBAL.variableName := value.
        /// </summary>
        public void SetGlobalVariable(string name, object value)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            _formRegistry?.SetGlobal(name, value);
        }

        /// <summary>
        /// Get a global variable. Returns null if not set.
        /// Equivalent to Oracle Forms :GLOBAL.variableName.
        /// </summary>
        public object GetGlobalVariable(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            return _formRegistry?.GetGlobal(name);
        }

        /// <summary>Get a typed global variable.</summary>
        public T GetGlobalVariable<T>(string name)
        {
            var v = GetGlobalVariable(name);
            return v is T t ? t : default;
        }

        // ── Parameter passing ────────────────────────────────────────────────

        /// <summary>
        /// Send a named parameter directly to another registered form's parameter dictionary.
        /// </summary>
        public bool SendParameterToForm(string targetFormName, string paramName, object value)
        {
            if (string.IsNullOrWhiteSpace(targetFormName) || string.IsNullOrWhiteSpace(paramName))
                return false;
            var target = _formRegistry?.GetForm(targetFormName);
            if (target is FormsManager targetFm)
            {
                targetFm._formParameters[paramName] = value;
                return true;
            }
            return false;
        }

        // ── Message bus ──────────────────────────────────────────────────────

        /// <summary>
        /// Event raised on this form when the shared message bus delivers a message
        /// addressed to or broadcast to it.
        /// </summary>
        public event EventHandler<FormMessageEventArgs> OnFormMessage;

        /// <summary>
        /// Post a typed message to a specific form.
        /// Equivalent to Oracle Forms DO_KEY or a custom inter-form RPC.
        /// </summary>
        public void PostMessage(string targetForm, string messageType, object payload = null)
        {
            if (string.IsNullOrWhiteSpace(targetForm) || string.IsNullOrWhiteSpace(messageType))
                return;
            _messageBus?.PostMessage(targetForm, messageType, payload, _currentFormName);
        }

        /// <summary>
        /// Broadcast a message to all forms subscribed to the given message type.
        /// </summary>
        public void BroadcastMessage(string messageType, object payload = null)
        {
            if (string.IsNullOrWhiteSpace(messageType)) return;
            _messageBus?.Broadcast(messageType, payload, _currentFormName);
        }

        /// <summary>
        /// Subscribe this form to receive messages of a given type from the shared bus.
        /// The handler is called synchronously on the posting thread.
        /// </summary>
        public void SubscribeToMessage(string messageType, Action<FormMessage> handler)
        {
            if (string.IsNullOrWhiteSpace(messageType) || handler == null) return;
            _messageBus?.Subscribe(_currentFormName ?? string.Empty, messageType, handler);
        }

        /// <summary>Unsubscribe this form from a specific message type.</summary>
        public void UnsubscribeFromMessage(string messageType)
        {
            _messageBus?.Unsubscribe(_currentFormName ?? string.Empty, messageType);
        }

        // ── Shared block access ──────────────────────────────────────────────

        /// <summary>
        /// Publish this form's block UoW as a cross-form shared block.
        /// Returns false if the name is already taken.
        /// </summary>
        public bool CreateSharedBlock(string blockName, IUnitofWork uow)
            => _sharedBlockManager?.CreateSharedBlock(blockName, uow) == true;

        /// <summary>
        /// Retrieve a shared block UoW published by another form.
        /// Returns null when the block doesn't exist.
        /// </summary>
        public IUnitofWork GetSharedBlock(string blockName)
            => _sharedBlockManager?.GetSharedBlock(blockName);

        /// <summary>
        /// Attempt to acquire a write-lock on a shared block within the given timeout.
        /// </summary>
        public bool TryLockSharedBlock(string blockName, TimeSpan timeout)
            => _sharedBlockManager?.TryLockSharedBlock(blockName, _currentFormName ?? "anonymous", timeout) == true;

        /// <summary>Release the write-lock this form holds on the shared block.</summary>
        public void ReleaseSharedBlockLock(string blockName)
            => _sharedBlockManager?.ReleaseSharedBlockLock(blockName, _currentFormName ?? "anonymous");

        #endregion
    }
}
