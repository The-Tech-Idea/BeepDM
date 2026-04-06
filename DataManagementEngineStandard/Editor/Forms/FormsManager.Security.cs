using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    /// <summary>
    /// FormsManager partial — Phase 6 Security &amp; Authorization.
    /// Exposes security context management, block/field security registration,
    /// field masking and violation logging.
    /// </summary>
    public partial class FormsManager
    {
        #region Initialization

        private void InitializeSecurity()
        {
            if (_securityManager == null) return;

            // Subscribe to violations so they flow through the error log
            _securityManager.OnSecurityViolation += (s, e) =>
            {
                _errorLog?.LogError(e.BlockName, null, e.Message);
            };
        }

        #endregion

        #region Security Context

        /// <summary>
        /// Sets the active security context (user + roles) and immediately
        /// re-applies all block and field security flags.
        /// </summary>
        public void SetSecurityContext(SecurityContext context)
        {
            _securityManager?.SetSecurityContext(context);
            ApplyAllSecurityFlags();
        }

        /// <summary>Returns the current security context.</summary>
        public SecurityContext SecurityContext => _securityManager?.CurrentContext;

        #endregion

        #region Block-Level Security

        /// <summary>Registers or replaces block-level security rules.</summary>
        public void SetBlockSecurity(string blockName, BlockSecurity security)
        {
            _securityManager?.SetBlockSecurity(blockName, security);
            ApplyAllSecurityFlags();
        }

        /// <summary>Returns current block security rules, or null if none registered.</summary>
        public BlockSecurity GetBlockSecurity(string blockName)
            => _securityManager?.GetBlockSecurity(blockName);

        /// <summary>Returns true when the current user may perform <paramref name="permission"/> on the block.</summary>
        public bool IsBlockAllowed(string blockName, SecurityPermission permission)
            => _securityManager?.IsBlockAllowed(blockName, permission) ?? true;

        #endregion

        #region Field-Level Security

        /// <summary>Registers or replaces field-level security (visibility, editability, masking).</summary>
        public void SetFieldSecurity(string blockName, string fieldName, FieldSecurity security)
        {
            _securityManager?.SetFieldSecurity(blockName, fieldName, security);
            ApplyAllSecurityFlags();
        }

        /// <summary>Returns current field security settings, or null if none registered.</summary>
        public FieldSecurity GetFieldSecurity(string blockName, string fieldName)
            => _securityManager?.GetFieldSecurity(blockName, fieldName);

        /// <summary>
        /// Returns the display/UI-safe (possibly masked) value for a field.
        /// Returns <paramref name="rawValue"/> unchanged when no masking is configured.
        /// </summary>
        public object GetMaskedFieldValue(string blockName, string fieldName, object rawValue)
            => _securityManager?.GetMaskedValue(blockName, fieldName, rawValue) ?? rawValue;

        #endregion

        #region Violation Log

        /// <summary>Returns all security violations recorded this session.</summary>
        public IReadOnlyList<SecurityViolationEventArgs> GetSecurityViolations()
            => _securityManager?.GetViolationLog() ?? new List<SecurityViolationEventArgs>();

        /// <summary>Exposes the underlying security manager for advanced usage.</summary>
        public ISecurityManager Security => _securityManager;

        #endregion

        #region Internal Enforcement Helpers

        /// <summary>
        /// Called after security context or any security rule changes to push
        /// current flags out to all DataBlockInfo and ItemPropertyManager items.
        /// </summary>
        internal void ApplyAllSecurityFlags()
        {
            if (_securityManager == null) return;

            // Block flags: update DataBlockInfo.InsertAllowed etc.
            _securityManager.ApplyBlockSecurityFlags((blockName, q, ins, upd, del) =>
            {
                if (_blocks.TryGetValue(blockName, out var info))
                {
                    info.QueryAllowed  = q;
                    info.InsertAllowed = ins;
                    info.UpdateAllowed = upd;
                    info.DeleteAllowed = del;
                }
            });

            // Field flags: update ItemPropertyManager Enabled/Visible
            _securityManager.ApplyFieldSecurityFlags(
                setEnabled: (blockName, fieldName, enabled) =>
                    _itemPropertyManager?.SetItemEnabled(blockName, fieldName, enabled),
                setVisible: (blockName, fieldName, visible) =>
                    _itemPropertyManager?.SetItemVisible(blockName, fieldName, visible));
        }

        /// <summary>
        /// Returns true if the current security context permits the operation;
        /// raises a violation event and logs to error log if not.
        /// </summary>
        internal bool EnforceBlockSecurity(string blockName, SecurityPermission permission)
        {
            if (_securityManager == null) return true;
            if (_securityManager.IsBlockAllowed(blockName, permission)) return true;

            var msg = $"Security: user '{_securityManager.CurrentContext?.UserName}' " +
                      $"is not allowed to perform '{permission}' on block '{blockName}'.";
            _securityManager.RaiseViolation(blockName, null, permission, msg);
            return false;
        }

        #endregion
    }
}
