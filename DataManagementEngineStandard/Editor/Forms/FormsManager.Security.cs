using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
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
    public partial class FormsManager : IFormsPolicyNotifications
    {
        private IObservableSecurityPolicy _observedSecurityPolicy;
        private readonly ConcurrentQueue<Exception> _policyNotificationFailures = new();
        public event EventHandler<SecurityPolicyChangedEventArgs> SecurityPolicyChanged;
        public IReadOnlyList<Exception> PolicyNotificationFailures => _policyNotificationFailures.ToArray();
        public long SecurityPolicyRevision => (_securityManager as IQuerySecuritySnapshotProvider)?.SecurityRevision ?? -1;

        private void RecordPolicyNotificationFailure(Exception error)
        {
            _policyNotificationFailures.Enqueue(error);
            while (_policyNotificationFailures.Count > 128) _policyNotificationFailures.TryDequeue(out _);
        }

        private void OnSecurityPolicyChanged(object sender, SecurityPolicyChangedEventArgs change)
        {
            using var admission = TryEnterCallback();
            if (admission == null || change == null || !ReferenceEquals(sender, _observedSecurityPolicy)) return;
            try { ApplyAllSecurityFlags(); }
            catch (Exception ex) { RecordPolicyNotificationFailure(ex); }
            PublishSecurityPolicyChange(change);
        }

        private void PublishSecurityPolicyChange(SecurityPolicyChangedEventArgs change)
        {
            if (_disposed || change.Revision != SecurityPolicyRevision) return;
            foreach (EventHandler<SecurityPolicyChangedEventArgs> observer in
                SecurityPolicyChanged?.GetInvocationList() ?? Array.Empty<Delegate>())
            {
                if (_disposed || change.Revision != SecurityPolicyRevision) break;
                try { observer(this, change); }
                catch (Exception ex) { RecordPolicyNotificationFailure(ex); }
            }
        }

        private void ApplyLegacySecurityChange()
        {
            if (_disposed || _observedSecurityPolicy != null) return;
            try { ApplyAllSecurityFlags(); }
            finally
            {
                var snapshot = CaptureQuerySecurity(string.Empty);
                PublishSecurityPolicyChange(new SecurityPolicyChangedEventArgs(snapshot.Revision, snapshot.ReadAuthorizationRevision));
            }
        }
        #region Initialization

        private void InitializeSecurity()
        {
            if (_securityManager == null) return;
            _observedSecurityPolicy = _securityManager as IObservableSecurityPolicy;
            try
            {
                _securityManager.OnSecurityViolation += OnSecurityViolationHandler;
                if (_observedSecurityPolicy != null) _observedSecurityPolicy.SecurityPolicyChanged += OnSecurityPolicyChanged;
            }
            catch
            {
                if (_observedSecurityPolicy != null)
                    CleanupAction("Failed policy notification attachment", () => _observedSecurityPolicy.SecurityPolicyChanged -= OnSecurityPolicyChanged);
                CleanupAction("Failed security notification attachment", () => _securityManager.OnSecurityViolation -= OnSecurityViolationHandler);
                throw;
            }
        }

        private void OnSecurityViolationHandler(object sender, SecurityViolationEventArgs e)
        {
            if (_disposed) return;
            _errorLog?.LogError(e.BlockName, null, e.Message);
        }

        #endregion

        #region Security Context

        /// <summary>
        /// Sets the active security context (user + roles) and immediately
        /// re-applies all block and field security flags.
        /// </summary>
        public void SetSecurityContext(SecurityContext context)
        {
            using var admission = TryEnterCallback() ?? throw new ObjectDisposedException(nameof(FormsManager));
            _securityManager?.SetSecurityContext(context);
            ApplyLegacySecurityChange();
        }

        /// <summary>Returns the current security context.</summary>
        public SecurityContext SecurityContext => _securityManager?.CurrentContext;

        #endregion

        #region Block-Level Security

        /// <summary>Registers or replaces block-level security rules.</summary>
        public void SetBlockSecurity(string blockName, BlockSecurity security)
        {
            using var admission = TryEnterCallback() ?? throw new ObjectDisposedException(nameof(FormsManager));
            _securityManager?.SetBlockSecurity(blockName, security);
            ApplyLegacySecurityChange();
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
            using var admission = TryEnterCallback() ?? throw new ObjectDisposedException(nameof(FormsManager));
            _securityManager?.SetFieldSecurity(blockName, fieldName, security);
            ApplyLegacySecurityChange();
        }

        /// <summary>Returns current field security settings, or null if none registered.</summary>
        public FieldSecurity GetFieldSecurity(string blockName, string fieldName)
            => _securityManager?.GetFieldSecurity(blockName, fieldName);

        /// <summary>
        /// Returns the display/UI-safe (possibly masked) value for a field.
        /// Returns <paramref name="rawValue"/> unchanged when no masking is configured.
        /// </summary>
        public object GetMaskedFieldValue(string blockName, string fieldName, object rawValue)
            => _securityManager == null ? rawValue : _securityManager.GetMaskedValue(blockName, fieldName, rawValue);

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
            RegistrationLease[] registrations;
            lock (_registrationGate) registrations = _registrations.Values.Where(r => r.Published && !r.Retired).ToArray();
            foreach (var registration in registrations)
                try { ApplyRegistrationSecurityFlags(registration); }
                catch (Exception ex) { RecordPolicyNotificationFailure(ex); }
        }

        private void ApplyRegistrationSecurityFlags(RegistrationLease registration)
        {
            if (!CanDispatchRegistration(registration)) return;
            var revision = SecurityPolicyRevision;
            var name = registration.Name;
            var query = _securityManager?.IsBlockAllowed(name, SecurityPermission.Query) ?? true;
            var insert = _securityManager?.IsBlockAllowed(name, SecurityPermission.Insert) ?? true;
            var update = _securityManager?.IsBlockAllowed(name, SecurityPermission.Update) ?? true;
            var delete = _securityManager?.IsBlockAllowed(name, SecurityPermission.Delete) ?? true;
            var admin = _securityManager?.CurrentContext?.IsAdmin == true;
            var flags = (_itemPropertyManager?.GetAllItems(name) ?? Array.Empty<ItemInfo>()).Select(item =>
            {
                var policy = _securityManager?.GetFieldSecurity(name, item.ItemName);
                return new ItemSecurityProjection(item.ItemName, item, policy == null || admin || policy.Editable,
                    policy == null || admin || policy.Visible);
            }).ToArray();
            void Authorize(Action publishItems)
            {
                void Publish()
                {
                    lock (_registrationGate)
                    {
                        if (!CanDispatchRegistration(registration)) throw new InvalidOperationException("Security projection registration was retired.");
                        registration.Block.PublishSecurityPermissions(registration.Identity, revision, query, insert, update, delete);
                        publishItems();
                    }
                }
                if (_securityManager is IQuerySecurityPublication gate)
                {
                    if (!gate.TryPublishQuery(revision, Publish)) throw new InvalidOperationException("Security projection policy changed before publication.");
                }
                else
                {
                    if (revision != SecurityPolicyRevision) throw new InvalidOperationException("Security projection policy changed.");
                    Publish();
                }
            }
            if (_itemPropertyManager is IItemSecurityProjection projection)
            {
                foreach (var failure in projection.PublishSecurityFlags(name, registration.Identity, revision, flags, Authorize,
                    () => CanDispatchRegistration(registration) && revision == SecurityPolicyRevision))
                    RecordPolicyNotificationFailure(failure);
            }
            else
            {
                // Legacy helpers lack registry gating. Do not overwrite their authored
                // permissions or call setters inside an ownership monitor.
                Authorize(() =>
                {
                    foreach (var flag in flags) flag.Item.PublishSecurityPermissions(registration.Identity, revision, flag.Enabled, flag.Visible);
                });
            }
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

        /// <summary>
        /// Removes all security rules (block- and field-level) for the named block.
        /// Removes policy restrictions, preserving authored flags. Cached rows still require read authorization.
        /// </summary>
        public void ClearBlockSecurity(string blockName)
        {
            using var admission = TryEnterCallback() ?? throw new ObjectDisposedException(nameof(FormsManager));
            if (string.IsNullOrWhiteSpace(blockName)) return;
            _securityManager?.ClearBlockSecurity(blockName);
            ApplyLegacySecurityChange();
        }
    }
}
