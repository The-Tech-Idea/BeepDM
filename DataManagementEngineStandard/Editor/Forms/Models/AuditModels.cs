using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.Forms.Models
{
    /// <summary>A single field-level change captured during an audit.</summary>
    public class AuditFieldChange
    {
        /// <summary>Gets or sets the field name that changed.</summary>
        public string FieldName { get; set; }

        /// <summary>Gets or sets the previous field value.</summary>
        public object OldValue  { get; set; }

        /// <summary>Gets or sets the new field value.</summary>
        public object NewValue  { get; set; }
    }

    /// <summary>DML operations that the audit system can record.</summary>
    public enum AuditOperation
    {
        /// <summary>Insert operation.</summary>
        Insert  = 1,

        /// <summary>Update operation.</summary>
        Update  = 2,

        /// <summary>Delete operation.</summary>
        Delete  = 3,

        /// <summary>Commit operation.</summary>
        Commit  = 4,

        /// <summary>Rollback operation.</summary>
        Rollback = 5,
    }

    /// <summary>One audited action against a data block (commit or delete).</summary>
    public class AuditEntry
    {
        /// <summary>Gets or sets the audit entry identifier.</summary>
        public Guid   Id          { get; set; } = Guid.NewGuid();

        /// <summary>Gets or sets the form name associated with the entry.</summary>
        public string FormName    { get; set; }

        /// <summary>Gets or sets the block name associated with the entry.</summary>
        public string BlockName   { get; set; }
        /// <summary>String representation of the record key / row index.</summary>
        public string RecordKey   { get; set; }
        /// <summary>Gets or sets the audited operation.</summary>
        public AuditOperation Operation { get; set; }

        /// <summary>Gets or sets the user who performed the operation.</summary>
        public string UserName    { get; set; }

        /// <summary>Gets or sets when the operation occurred.</summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        /// <summary>JSON snapshot of the record state before the change.</summary>
        public string BeforeImage { get; set; }
        /// <summary>JSON snapshot of the record state after the change.</summary>
        public string AfterImage  { get; set; }
        /// <summary>Gets or sets the field-level changes captured for the entry.</summary>
        public List<AuditFieldChange> FieldChanges { get; set; } = new List<AuditFieldChange>();
    }

    /// <summary>Controls which blocks/fields are audited and how entries are retained.</summary>
    public class AuditConfiguration
    {
        /// <summary>Master switch. When false the audit manager is a no-op.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Capture individual field changes; otherwise only entry-level records.</summary>
        public bool CaptureFieldLevel { get; set; } = true;

        /// <summary>Include full before/after JSON snapshots (heavier storage).</summary>
        public bool CaptureBeforeAfterImage { get; set; } = false;

        /// <summary>Blocks to audit. When empty, all registered blocks are audited.</summary>
        public HashSet<string> AuditedBlocks  { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Blocks to exclude from audit regardless of AuditedBlocks.</summary>
        public HashSet<string> ExcludedBlocks { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Field names to suppress from field-level capture (e.g. password hashes).</summary>
        public HashSet<string> ExcludedFields { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Audit-stamp field names written to the entity on insert/update
        /// <summary>Gets or sets the created-by field name.</summary>
        public string CreatedByField  { get; set; } = "CreatedBy";

        /// <summary>Gets or sets the modified-by field name.</summary>
        public string ModifiedByField { get; set; } = "ModifiedBy";

        /// <summary>Gets or sets the created-at field name.</summary>
        public string CreatedAtField  { get; set; } = "CreatedAt";

        /// <summary>Gets or sets the modified-at field name.</summary>
        public string ModifiedAtField { get; set; } = "ModifiedAt";

        /// <summary>Auto-purge entries older than this many days (0 = no purge).</summary>
        public int MaxRetentionDays { get; set; } = 90;

        /// <summary>Maximum entries kept in the in-memory store (0 = unlimited).</summary>
        public int MaxEntries { get; set; } = 10_000;
    }
}
