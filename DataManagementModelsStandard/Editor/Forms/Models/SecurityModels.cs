using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.Forms.Models
{
    /// <summary>Permissions flags controlling what a principal can do.</summary>
    [Flags]
    public enum SecurityPermission
    {
        /// <summary>No permissions granted.</summary>
        None    = 0,

        /// <summary>Query permission.</summary>
        Query   = 1,

        /// <summary>Insert permission.</summary>
        Insert  = 2,

        /// <summary>Update permission.</summary>
        Update  = 4,

        /// <summary>Delete permission.</summary>
        Delete  = 8,

        /// <summary>Execute permission.</summary>
        Execute = 16,

        /// <summary>All permissions combined.</summary>
        All     = Query | Insert | Update | Delete | Execute,
    }

    /// <summary>A named role with an associated set of permissions.</summary>
    public class SecurityRole
    {
        /// <summary>Gets or sets the role name.</summary>
        public string Name        { get; set; }

        /// <summary>Gets or sets the permissions granted to the role.</summary>
        public SecurityPermission Permissions { get; set; } = SecurityPermission.All;
    }

    /// <summary>The active user identity and role membership.</summary>
    public class SecurityContext
    {
        /// <summary>Gets or sets the current user name.</summary>
        public string       UserName { get; set; } = string.Empty;

        /// <summary>Gets or sets the current role memberships.</summary>
        public List<string> Roles    { get; set; } = new List<string>();

        /// <summary>Gets or sets whether the current principal is an administrator.</summary>
        public bool         IsAdmin  { get; set; }

        /// <summary>Additional key-value claims (e.g. TenantId, Department).</summary>
        public Dictionary<string, string> Claims { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Block-level security configuration: which operations a principal may perform.</summary>
    public class BlockSecurity
    {
        /// <summary>Gets or sets the block name the policy applies to.</summary>
        public string BlockName { get; set; }

        // Explicit allow/deny per DML operation
        /// <summary>Gets or sets whether querying is allowed.</summary>
        public bool AllowQuery  { get; set; } = true;

        /// <summary>Gets or sets whether inserts are allowed.</summary>
        public bool AllowInsert { get; set; } = true;

        /// <summary>Gets or sets whether updates are allowed.</summary>
        public bool AllowUpdate { get; set; } = true;

        /// <summary>Gets or sets whether deletes are allowed.</summary>
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
        /// <summary>Gets or sets the block name the policy applies to.</summary>
        public string BlockName { get; set; }

        /// <summary>Gets or sets the field name the policy applies to.</summary>
        public string FieldName { get; set; }

        /// <summary>Gets or sets whether the field is visible.</summary>
        public bool Visible  { get; set; } = true;

        /// <summary>Gets or sets whether the field is editable.</summary>
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
        /// <summary>Gets or sets the user who triggered the violation.</summary>
        public string           UserName   { get; set; }

        /// <summary>Gets or sets the affected block name.</summary>
        public string           BlockName  { get; set; }

        /// <summary>Gets or sets the affected field name.</summary>
        public string           FieldName  { get; set; }

        /// <summary>Gets or sets the denied permission.</summary>
        public SecurityPermission Permission { get; set; }

        /// <summary>Gets or sets the violation message.</summary>
        public string           Message    { get; set; }

        /// <summary>Gets or sets when the violation was recorded.</summary>
        public DateTime         Timestamp  { get; set; } = DateTime.UtcNow;
    }
}
