using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
    public class SecurityManager : ISecurityManager
    {
        #region Fields
        private readonly ConcurrentDictionary<string, BlockSecurity> _blockSecurities
            = new ConcurrentDictionary<string, BlockSecurity>(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, FieldSecurity> _fieldSecurities
            = new ConcurrentDictionary<string, FieldSecurity>(StringComparer.OrdinalIgnoreCase);

        private readonly List<SecurityViolationEventArgs> _violations = new List<SecurityViolationEventArgs>();
        private readonly object _violationLock = new object();

        private readonly IFieldMaskProvider _maskProvider;
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
            CurrentContext = new SecurityContext();
        }
        #endregion

        #region ISecurityManager

        /// <summary>Gets the current security context.</summary>
        public SecurityContext CurrentContext { get; private set; }

        // ── Context ──────────────────────────────────────────────────────────

        /// <summary>Sets the active security context.</summary>
        public void SetSecurityContext(SecurityContext context)
        {
            CurrentContext = context ?? new SecurityContext();
        }

        // ── Block Security ───────────────────────────────────────────────────

        /// <summary>Registers block-level security rules for a block.</summary>
        public void SetBlockSecurity(string blockName, BlockSecurity security)
        {
            if (string.IsNullOrEmpty(blockName) || security == null) return;
            security.BlockName = blockName;
            _blockSecurities[blockName] = security;
        }

        /// <summary>Returns block-level security rules for a block.</summary>
        public BlockSecurity GetBlockSecurity(string blockName)
            => _blockSecurities.TryGetValue(blockName ?? string.Empty, out var bs) ? bs : null;

        /// <summary>Returns whether a block operation is allowed in the current context.</summary>
        public bool IsBlockAllowed(string blockName, SecurityPermission permission)
        {
            if (CurrentContext.IsAdmin) return true;

            if (!_blockSecurities.TryGetValue(blockName ?? string.Empty, out var bs))
                return true;  // no security rule = allow

            // Check role-specific overrides first
            foreach (var role in CurrentContext.Roles)
            {
                if (bs.RolePermissions.TryGetValue(role, out var rp))
                    return (rp & permission) != 0;
            }

            // Fall back to block defaults
            return permission switch
            {
                SecurityPermission.Query  => bs.AllowQuery,
                SecurityPermission.Insert => bs.AllowInsert,
                SecurityPermission.Update => bs.AllowUpdate,
                SecurityPermission.Delete => bs.AllowDelete,
                _                         => true
            };
        }

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
            security.BlockName = blockName;
            security.FieldName = fieldName;
            _fieldSecurities[key] = security;
        }

        /// <summary>Returns field-level security rules for a block field.</summary>
        public FieldSecurity GetFieldSecurity(string blockName, string fieldName)
        {
            var key = MakeFieldKey(blockName, fieldName);
            return _fieldSecurities.TryGetValue(key, out var fs) ? fs : null;
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
