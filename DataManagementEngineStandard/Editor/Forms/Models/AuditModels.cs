using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.Forms.Models
{
    /// <summary>A single field-level change captured during an audit.</summary>
    public class AuditFieldChange
    {
        public string FieldName { get; set; }
        public object OldValue  { get; set; }
        public object NewValue  { get; set; }
    }

    /// <summary>DML operations that the audit system can record.</summary>
    public enum AuditOperation
    {
        Insert  = 1,
        Update  = 2,
        Delete  = 3,
        Commit  = 4,
        Rollback = 5,
    }

    /// <summary>One audited action against a data block (commit or delete).</summary>
    public class AuditEntry
    {
        public Guid   Id          { get; set; } = Guid.NewGuid();
        public string FormName    { get; set; }
        public string BlockName   { get; set; }
        /// <summary>String representation of the record key / row index.</summary>
        public string RecordKey   { get; set; }
        public AuditOperation Operation { get; set; }
        public string UserName    { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        /// <summary>JSON snapshot of the record state before the change.</summary>
        public string BeforeImage { get; set; }
        /// <summary>JSON snapshot of the record state after the change.</summary>
        public string AfterImage  { get; set; }
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
        public string CreatedByField  { get; set; } = "CreatedBy";
        public string ModifiedByField { get; set; } = "ModifiedBy";
        public string CreatedAtField  { get; set; } = "CreatedAt";
        public string ModifiedAtField { get; set; } = "ModifiedAt";

        /// <summary>Auto-purge entries older than this many days (0 = no purge).</summary>
        public int MaxRetentionDays { get; set; } = 90;

        /// <summary>Maximum entries kept in the in-memory store (0 = unlimited).</summary>
        public int MaxEntries { get; set; } = 10_000;
    }
}
