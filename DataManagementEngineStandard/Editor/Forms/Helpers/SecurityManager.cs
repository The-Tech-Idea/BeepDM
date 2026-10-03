using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;

namespace TheTechIdea.Beep.Editor.Forms.Helpers
{
    /// <summary>
    /// Default field-mask provider.  Pattern rules:
    /// <list type="bullet">
    ///   <item><c>*</c> (single asterisk) — replaces the whole value with "*****".</item>
    ///   <item>Any other pattern — '#' matches a digit, '*' matches any character; unmatched
    ///   positions in the raw value are replaced with the next pattern character.</item>
    /// </list>
    /// </summary>
    public class DefaultFieldMaskProvider : IFieldMaskProvider
    {
        /// <summary>Masks a raw field value using the supplied pattern.</summary>
        public string Mask(object rawValue, string pattern)
        {
            if (rawValue == null) return string.Empty;
            if (string.IsNullOrEmpty(pattern)) return rawValue.ToString();

            var raw = rawValue.ToString();

            // "*" alone = fully hide
            if (pattern == "*")
                return new string('*', Math.Max(raw.Length, 5));

            // Pattern-based masking: copy literal characters, replace '#' with digit placeholder
            var sb = new StringBuilder();
            int ri = 0;
            foreach (char pc in pattern)
            {
                if (pc == '#')
                {
                    sb.Append(ri < raw.Length && char.IsDigit(raw[ri]) ? raw[ri] : '#');
                    ri++;
                }
                else if (pc == '*')
                {
                    sb.Append(ri < raw.Length ? raw[ri] : '*');
                    ri++;
                }
                else
                {
                    sb.Append(pc);
                    if (ri < raw.Length && raw[ri] == pc) ri++;
                }
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// Manages block- and field-level authorization for the forms engine.
    /// Evaluates the current <see cref="SecurityContext"/> against registered
    /// <see cref="BlockSecurity"/> and <see cref="FieldSecurity"/> rules.
    /// </summary>
    public class SecurityManager : ISecurityManager, IQuerySecurityPublication, IObservableSecurityPolicy
    {
        #region Fields
        private readonly ConcurrentDictionary<string, BlockSecurity> _blockSecurities
            = new ConcurrentDictionary<string, BlockSecurity>(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, FieldSecurity> _fieldSecurities
            = new ConcurrentDictionary<string, FieldSecurity>(StringComparer.OrdinalIgnoreCase);

        private readonly List<SecurityViolationEventArgs> _violations = new List<SecurityViolationEventArgs>();
        private readonly object _violationLock = new object();

        private readonly IFieldMaskProvider _maskProvider;
        private readonly object _policyLock = new object();
        private SecurityContext _context = new SecurityContext();
        private long _securityRevision;
        private long _readAuthorizationRevision;
        public long SecurityRevision { get { lock (_policyLock) return _securityRevision; } }
        private readonly ConcurrentQueue<Exception> _policyNotificationFailures = new();
        public IReadOnlyList<Exception> PolicyNotificationFailures => _policyNotificationFailures.ToArray();
        public event EventHandler<SecurityPolicyChangedEventArgs> SecurityPolicyChanged;

        private SecurityPolicyChangedEventArgs CurrentPolicyChange() => new(_securityRevision, _readAuthorizationRevision);

        private void NotifyPolicyChange(SecurityPolicyChangedEventArgs change)
        {
            foreach (EventHandler<SecurityPolicyChangedEventArgs> observer in
                SecurityPolicyChanged?.GetInvocationList() ?? Array.Empty<Delegate>())
                try { observer(this, change); }
                catch (Exception ex)
                {
                    _policyNotificationFailures.Enqueue(ex);
                    while (_policyNotificationFailures.Count > 128) _policyNotificationFailures.TryDequeue(out _);
                }
        }

        public bool TryPublishQuery(long revision, Action publishOwnedState)
        {
            ArgumentNullException.ThrowIfNull(publishOwnedState);
            lock (_policyLock)
            {
                if (revision != _securityRevision) return false;
                publishOwnedState();
                return true;
            }
        }
        #endregion

        #region Events
        /// <summary>Raised when a security violation is recorded.</summary>
        public event EventHandler<SecurityViolationEventArgs> OnSecurityViolation;
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a security manager with the supplied mask provider.
        /// </summary>
        /// <param name="maskProvider">Optional field-mask provider override.</param>
        public SecurityManager(IFieldMaskProvider maskProvider = null)
        {
            _maskProvider = maskProvider ?? new DefaultFieldMaskProvider();
        }
        #endregion

        #region ISecurityManager

        /// <summary>Gets the current security context.</summary>
        public SecurityContext CurrentContext { get { lock (_policyLock) return CloneContext(_context); } }

        // ── Context ──────────────────────────────────────────────────────────

        /// <summary>Sets the active security context.</summary>
        public void SetSecurityContext(SecurityContext context)
        {
            var copy = CloneContext(context ?? new SecurityContext());
            SecurityPolicyChangedEventArgs change;
            lock (_policyLock) { _context = copy; _securityRevision++; _readAuthorizationRevision++; change = CurrentPolicyChange(); }
            NotifyPolicyChange(change);
        }

        // ── Block Security ───────────────────────────────────────────────────

        /// <summary>Registers block-level security rules for a block.</summary>
        public void SetBlockSecurity(string blockName, BlockSecurity security)
        {
            if (string.IsNullOrEmpty(blockName) || security == null) return;
            var copy = CloneBlockSecurity(security);
            copy.BlockName = blockName;
            SecurityPolicyChangedEventArgs change;
            lock (_policyLock) { _blockSecurities[blockName] = copy; _securityRevision++; _readAuthorizationRevision++; change = CurrentPolicyChange(); }
            NotifyPolicyChange(change);
        }

        /// <summary>Returns block-level security rules for a block.</summary>
        public BlockSecurity GetBlockSecurity(string blockName)
        {
            lock (_policyLock)
                return _blockSecurities.TryGetValue(blockName ?? string.Empty, out var bs) ? CloneBlockSecurity(bs) : null;
        }

        /// <summary>Removes all block-level and field-level security rules for a block.</summary>
        public void ClearBlockSecurity(string blockName)
        {
            if (string.IsNullOrWhiteSpace(blockName)) return;
            SecurityPolicyChangedEventArgs change;
            lock (_policyLock)
            {
                _blockSecurities.TryRemove(blockName, out _);
                foreach (var key in _fieldSecurities.Keys.Where(k => k.StartsWith(blockName.ToUpperInvariant() + "|", StringComparison.Ordinal)))
                    _fieldSecurities.TryRemove(key, out _);
                _securityRevision++;
                _readAuthorizationRevision++;
                change = CurrentPolicyChange();
            }
            NotifyPolicyChange(change);
        }

        /// <summary>Returns whether a block operation is allowed in the current context.</summary>
        public bool IsBlockAllowed(string blockName, SecurityPermission permission)
        {
            lock (_policyLock)
                return EvaluatePermission(blockName, permission);
        }

        private bool EvaluatePermission(string blockName, SecurityPermission permission)
        {
            if (_context.IsAdmin) return true;
            if (!_blockSecurities.TryGetValue(blockName ?? string.Empty, out var policy)) return true;
            var matches = _context.Roles.Where(role => role != null && policy.RolePermissions.ContainsKey(role)).ToList();
            if (matches.Count > 0)
                return matches.Any(role => (policy.RolePermissions[role] & permission) == permission);
            return permission switch
            {
                SecurityPermission.Query => policy.AllowQuery,
                SecurityPermission.Insert => policy.AllowInsert,
                SecurityPermission.Update => policy.AllowUpdate,
                SecurityPermission.Delete => policy.AllowDelete,
                _ => true
            };
        }

        public QuerySecuritySnapshot CaptureQuerySecurity(string blockName)
        {
            lock (_policyLock)
            {
                _blockSecurities.TryGetValue(blockName ?? string.Empty, out var policy);
                return new QuerySecuritySnapshot(_securityRevision, EvaluatePermission(blockName, SecurityPermission.Query),
                    policy?.RowFilterClause ?? string.Empty,
                    new ReadOnlyDictionary<string, object>(new Dictionary<string, object>(
                        policy?.RowFilterValues ?? new Dictionary<string, object>(), StringComparer.OrdinalIgnoreCase)),
                    _readAuthorizationRevision);
            }
        }

        private static SecurityContext CloneContext(SecurityContext context) => new SecurityContext
        {
            UserName = context.UserName, IsAdmin = context.IsAdmin,
            Roles = new List<string>(context.Roles ?? new List<string>()),
            Claims = new Dictionary<string, string>(context.Claims ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase)
        };

        private static BlockSecurity CloneBlockSecurity(BlockSecurity policy) => new BlockSecurity
        {
            BlockName = policy.BlockName, AllowQuery = policy.AllowQuery, AllowInsert = policy.AllowInsert,
            AllowUpdate = policy.AllowUpdate, AllowDelete = policy.AllowDelete, RowFilterClause = policy.RowFilterClause,
            RolePermissions = new Dictionary<string, SecurityPermission>(policy.RolePermissions ?? new Dictionary<string, SecurityPermission>(), StringComparer.OrdinalIgnoreCase),
            RowFilterValues = new Dictionary<string, object>(policy.RowFilterValues ?? new Dictionary<string, object>(), StringComparer.OrdinalIgnoreCase)
        };

        /// <summary>Applies evaluated block security flags through the supplied callback.</summary>
        public void ApplyBlockSecurityFlags(Action<string, bool, bool, bool, bool> applyBlockFlags)
        {
            if (applyBlockFlags == null) return;

            foreach (var kvp in _blockSecurities)
            {
                var blockName = kvp.Key;
                bool q = IsBlockAllowed(blockName, SecurityPermission.Query);
                bool i = IsBlockAllowed(blockName, SecurityPermission.Insert);
                bool u = IsBlockAllowed(blockName, SecurityPermission.Update);
                bool d = IsBlockAllowed(blockName, SecurityPermission.Delete);
                applyBlockFlags(blockName, q, i, u, d);
            }
        }

        /// <summary>Returns the row filter clause associated with a block.</summary>
        public string GetBlockRowFilter(string blockName)
        {
            if (_blockSecurities.TryGetValue(blockName ?? string.Empty, out var bs))
                return bs.RowFilterClause ?? string.Empty;
            return string.Empty;
        }

        // ── Field Security ───────────────────────────────────────────────────

        /// <summary>Registers field-level security rules for a block field.</summary>
        public void SetFieldSecurity(string blockName, string fieldName, FieldSecurity security)
        {
            if (string.IsNullOrEmpty(blockName) || string.IsNullOrEmpty(fieldName) || security == null) return;
            var key = MakeFieldKey(blockName, fieldName);
            var copy = CloneFieldSecurity(security);
            copy.BlockName = blockName;
            copy.FieldName = fieldName;
            SecurityPolicyChangedEventArgs change;
            lock (_policyLock) { _fieldSecurities[key] = copy; _securityRevision++; change = CurrentPolicyChange(); }
            NotifyPolicyChange(change);
        }

        private static FieldSecurity CloneFieldSecurity(FieldSecurity policy) => new()
        {
            BlockName = policy.BlockName, FieldName = policy.FieldName, Visible = policy.Visible,
            Editable = policy.Editable, Masked = policy.Masked, MaskPattern = policy.MaskPattern, UiHint = policy.UiHint
        };

        /// <summary>Returns an owned field-policy copy; use SetFieldSecurity to publish changes.</summary>
        public FieldSecurity GetFieldSecurity(string blockName, string fieldName)
        {
            var key = MakeFieldKey(blockName, fieldName);
            lock (_policyLock) return _fieldSecurities.TryGetValue(key, out var fs) ? CloneFieldSecurity(fs) : null;
        }

        /// <summary>Applies evaluated field security flags through the supplied callbacks.</summary>
        public void ApplyFieldSecurityFlags(
            Action<string, string, bool> setEnabled,
            Action<string, string, bool> setVisible)
        {
            if (setEnabled == null && setVisible == null) return;

            bool isAdmin = CurrentContext.IsAdmin;

            foreach (var kvp in _fieldSecurities)
            {
                var fs = kvp.Value;
                bool enabled = isAdmin || fs.Editable;
                bool visible = isAdmin || fs.Visible;

                setEnabled?.Invoke(fs.BlockName, fs.FieldName, enabled);
                setVisible?.Invoke(fs.BlockName, fs.FieldName, visible);
            }
        }

        /// <summary>Returns a masked field value when masking is enabled for the field.</summary>
        public object GetMaskedValue(string blockName, string fieldName, object rawValue)
        {
            var key = MakeFieldKey(blockName, fieldName);
            if (!_fieldSecurities.TryGetValue(key, out var fs) || !fs.Masked)
                return rawValue;

            return _maskProvider.Mask(rawValue, fs.MaskPattern ?? "*");
        }

        // ── Logging ──────────────────────────────────────────────────────────

        /// <summary>Records and raises a security violation.</summary>
        public void RaiseViolation(
            string blockName, string fieldName,
            SecurityPermission permission, string message)
        {
            var ev = new SecurityViolationEventArgs
            {
                UserName   = CurrentContext?.UserName ?? string.Empty,
                BlockName  = blockName,
                FieldName  = fieldName,
                Permission = permission,
                Message    = message
            };

            lock (_violationLock)
                _violations.Add(ev);

            OnSecurityViolation?.Invoke(this, ev);
        }

        /// <summary>Returns the recorded security-violation log.</summary>
        public IReadOnlyList<SecurityViolationEventArgs> GetViolationLog()
        {
            lock (_violationLock)
                return _violations.ToArray();
        }

        #endregion

        #region Private
        private static string MakeFieldKey(string blockName, string fieldName)
            => $"{blockName?.ToUpperInvariant()}|{fieldName?.ToUpperInvariant()}";
        #endregion
    }
}
