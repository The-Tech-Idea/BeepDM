using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.Forms.Models
{
    /// <summary>Permissions flags controlling what a principal can do.</summary>
    [Flags]
    public enum SecurityPermission
    {
        None    = 0,
        Query   = 1,
        Insert  = 2,
        Update  = 4,
        Delete  = 8,
        Execute = 16,
        All     = Query | Insert | Update | Delete | Execute,
    }

    /// <summary>A named role with an associated set of permissions.</summary>
    public class SecurityRole
    {
        public string Name        { get; set; }
        public SecurityPermission Permissions { get; set; } = SecurityPermission.All;
    }

    /// <summary>The active user identity and role membership.</summary>
    public class SecurityContext
    {
        public string       UserName { get; set; } = string.Empty;
        public List<string> Roles    { get; set; } = new List<string>();
        public bool         IsAdmin  { get; set; }

        /// <summary>Additional key-value claims (e.g. TenantId, Department).</summary>
        public Dictionary<string, string> Claims { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Block-level security configuration: which operations a principal may perform.</summary>
    public class BlockSecurity
    {
        public string BlockName { get; set; }

        // Explicit allow/deny per DML operation
        public bool AllowQuery  { get; set; } = true;
        public bool AllowInsert { get; set; } = true;
        public bool AllowUpdate { get; set; } = true;
        public bool AllowDelete { get; set; } = true;

        /// <summary>Per-role overrides. An allow-any-matching-role wins over block defaults.</summary>
        public Dictionary<string, SecurityPermission> RolePermissions { get; set; }
            = new Dictionary<string, SecurityPermission>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Optional WHERE clause fragment added to every query (e.g. "TenantId = :TenantId").
        /// Placeholder values should be supplied via <see cref="RowFilterValues"/>.
        /// </summary>
        public string RowFilterClause { get; set; } = string.Empty;

        /// <summary>Parameter values merged into the query WHERE clause filter.</summary>
        public Dictionary<string, object> RowFilterValues { get; set; }
            = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Field-level security: visibility, editability, and masking.</summary>
    public class FieldSecurity
    {
        public string BlockName { get; set; }
        public string FieldName { get; set; }

        public bool Visible  { get; set; } = true;
        public bool Editable { get; set; } = true;

        /// <summary>When true the field value is replaced with <see cref="MaskPattern"/> on display.</summary>
        public bool   Masked      { get; set; }

        /// <summary>Mask pattern, e.g. "***-**-####" for SSN or "*" to fully hide the value.</summary>
        public string MaskPattern { get; set; } = "*";

        /// <summary>Optional UI hint shown in tooltips or header for secured fields.</summary>
        public string UiHint { get; set; } = string.Empty;
    }

    /// <summary>Arguments carried when a security violation is detected.</summary>
    public class SecurityViolationEventArgs : EventArgs
    {
        public string           UserName   { get; set; }
        public string           BlockName  { get; set; }
        public string           FieldName  { get; set; }
        public SecurityPermission Permission { get; set; }
        public string           Message    { get; set; }
        public DateTime         Timestamp  { get; set; } = DateTime.UtcNow;
    }
}
