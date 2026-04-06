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
        public event EventHandler<SecurityViolationEventArgs> OnSecurityViolation;
        #endregion

        #region Constructor
        public SecurityManager(IFieldMaskProvider maskProvider = null)
        {
            _maskProvider = maskProvider ?? new DefaultFieldMaskProvider();
            CurrentContext = new SecurityContext();
        }
        #endregion

        #region ISecurityManager

        public SecurityContext CurrentContext { get; private set; }

        // ── Context ──────────────────────────────────────────────────────────

        public void SetSecurityContext(SecurityContext context)
        {
            CurrentContext = context ?? new SecurityContext();
        }

        // ── Block Security ───────────────────────────────────────────────────

        public void SetBlockSecurity(string blockName, BlockSecurity security)
        {
            if (string.IsNullOrEmpty(blockName) || security == null) return;
            security.BlockName = blockName;
            _blockSecurities[blockName] = security;
        }

        public BlockSecurity GetBlockSecurity(string blockName)
            => _blockSecurities.TryGetValue(blockName ?? string.Empty, out var bs) ? bs : null;

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

        public string GetBlockRowFilter(string blockName)
        {
            if (_blockSecurities.TryGetValue(blockName ?? string.Empty, out var bs))
                return bs.RowFilterClause ?? string.Empty;
            return string.Empty;
        }

        // ── Field Security ───────────────────────────────────────────────────

        public void SetFieldSecurity(string blockName, string fieldName, FieldSecurity security)
        {
            if (string.IsNullOrEmpty(blockName) || string.IsNullOrEmpty(fieldName) || security == null) return;
            var key = MakeFieldKey(blockName, fieldName);
            security.BlockName = blockName;
            security.FieldName = fieldName;
            _fieldSecurities[key] = security;
        }

        public FieldSecurity GetFieldSecurity(string blockName, string fieldName)
        {
            var key = MakeFieldKey(blockName, fieldName);
            return _fieldSecurities.TryGetValue(key, out var fs) ? fs : null;
        }

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

        public object GetMaskedValue(string blockName, string fieldName, object rawValue)
        {
            var key = MakeFieldKey(blockName, fieldName);
            if (!_fieldSecurities.TryGetValue(key, out var fs) || !fs.Masked)
                return rawValue;

            return _maskProvider.Mask(rawValue, fs.MaskPattern ?? "*");
        }

        // ── Logging ──────────────────────────────────────────────────────────

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
