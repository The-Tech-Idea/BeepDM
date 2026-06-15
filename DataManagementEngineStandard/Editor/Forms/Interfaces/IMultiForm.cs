using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Editor.UOWManager.Configuration;
using TheTechIdea.Beep.Editor.UOWManager.Helpers;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Editor.Forms.Models;


namespace TheTechIdea.Beep.Editor.UOWManager.Interfaces
{

    // ──────────────────────────────────────────────────────────────────────────
    // Phase 3 — Multi-Form & Cross-Form Communication
    // ──────────────────────────────────────────────────────────────────────────

    #region IFormRegistry

    /// <summary>
    /// Shared registry of all active form managers.
    /// Pass a single instance to every FormsManager so forms can discover each other.
    /// Equivalent to Oracle Forms' :GLOBAL scope and CALL_FORM / OPEN_FORM / NEW_FORM built-ins.
    /// </summary>
    public interface IFormRegistry
    {
        /// <summary>Name of the currently active (focused) form, or null.</summary>
        string ActiveFormName { get; }

        /// <summary>Register a form manager under a logical form name.</summary>
        void RegisterForm(string formName, IUnitofWorksManager form);

        /// <summary>Remove a form from the registry. Returns false if not found.</summary>
        bool UnregisterForm(string formName);

        /// <summary>Retrieve a registered form manager by name, or null.</summary>
        IUnitofWorksManager GetForm(string formName);

        /// <summary>Get all currently registered form names.</summary>
        IReadOnlyList<string> GetActiveFormNames();

        /// <summary>Returns true when a form with the given name is registered.</summary>
        bool FormExists(string formName);

        /// <summary>Mark a form as the currently active/focused form.</summary>
        void SetActiveForm(string formName);

        /// <summary>Set or overwrite a global variable (:GLOBAL.name).</summary>
        void SetGlobal(string name, object value);

        /// <summary>Read a global variable. Returns null if not set.</summary>
        object GetGlobal(string name);

        /// <summary>Returns true if a global variable with the given name exists.</summary>
        bool GlobalExists(string name);

        /// <summary>Raised whenever a form is registered, unregistered, activated or deactivated.</summary>
        event EventHandler<FormLifecycleEventArgs> FormLifecycleChanged;
    }

    #endregion

    #region IFormMessageBus

    /// <summary>
    /// Pub/sub message bus for inter-form communication.
    /// Equivalent to Oracle Forms' DO_KEY / SYNCHRONIZE and custom messaging patterns.
    /// </summary>
    public interface IFormMessageBus
    {
        /// <summary>
        /// Send a typed message payload to a specific form.
        /// Any subscribers registered for (targetForm, messageType) are invoked synchronously.
        /// </summary>
        void PostMessage(string targetForm, string messageType, object payload, string senderForm = null);

        /// <summary>Broadcast a message to all forms subscribed to the given messageType.</summary>
        void Broadcast(string messageType, object payload, string senderForm = null);

        /// <summary>Subscribe a form to receive messages of a given type.</summary>
        void Subscribe(string formName, string messageType, Action<FormMessage> handler);

        /// <summary>Unsubscribe a form from a specific message type.</summary>
        void Unsubscribe(string formName, string messageType);

        /// <summary>Remove all subscriptions registered by a form (call during cleanup).</summary>
        void UnsubscribeAll(string formName);

        /// <summary>Raised for every message posted or broadcast (global observer hook).</summary>
        event EventHandler<FormMessageEventArgs> OnFormMessage;
    }

    #endregion

    #region ISharedBlockManager

    /// <summary>
    /// Manages IUnitofWork data blocks that are shared across multiple form managers.
    /// Provides optimistic lock coordination so only one form modifies a shared block at a time.
    /// </summary>
    public interface ISharedBlockManager
    {
        /// <summary>Publish a block UoW so other forms can access it. Returns false if already exists.</summary>
        bool CreateSharedBlock(string blockName, IUnitofWork uow);

        /// <summary>Retrieve a shared block UoW by name, or null.</summary>
        IUnitofWork GetSharedBlock(string blockName);

        /// <summary>Returns true when the named shared block exists.</summary>
        bool SharedBlockExists(string blockName);

        /// <summary>Remove a shared block (releases any outstanding lock).</summary>
        bool RemoveSharedBlock(string blockName);

        /// <summary>
        /// Attempt to acquire an exclusive write lock on a shared block.
        /// Returns true when the lock is obtained within the timeout.
        /// </summary>
        bool TryLockSharedBlock(string blockName, string lockedBy, TimeSpan timeout);

        /// <summary>Release a write lock held by the named caller. No-op if not locked by that caller.</summary>
        void ReleaseSharedBlockLock(string blockName, string lockedBy);

        /// <summary>Raised when any caller notifies that a shared block's data has changed.</summary>
        event EventHandler<SharedBlockChangedEventArgs> SharedBlockChanged;
    }

    #endregion

}
