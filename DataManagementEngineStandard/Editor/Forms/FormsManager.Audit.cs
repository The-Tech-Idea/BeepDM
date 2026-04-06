using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    /// <summary>
    /// FormsManager partial — Phase 5 Audit Trail &amp; Change Tracking.
    /// Subscribes to <see cref="OnBlockFieldChanged"/> and exposes audit query / export / maintenance APIs.
    /// </summary>
    public partial class FormsManager
    {
        #region Fields (declared in FormsManager.cs)
        // _auditManager is declared in FormsManager.cs
        #endregion

        #region Initialization (called from FormsManager.cs constructor)

        private void InitializeAudit()
        {
            // Subscribe to the block field change feed for field-level capture
            OnBlockFieldChanged += HandleBlockFieldChangedForAudit;
        }

        private void HandleBlockFieldChangedForAudit(object sender, BlockFieldChangedEventArgs e)
        {
            if (_auditManager == null || e == null) return;
            _auditManager.RecordFieldChange(
                e.BlockName,
                e.FieldName,
                e.OldValue,
                e.NewValue,
                e.RecordIndex);
        }

        #endregion

        #region Public Audit API

        /// <summary>
        /// Sets the user name that will be stamped on all subsequent audit entries.
        /// Equivalent to setting a session-level audit context.
        /// </summary>
        public void SetAuditUser(string userName)
            => _auditManager?.SetAuditUser(userName);

        /// <summary>Configures the audit system via a delegate.</summary>
        public void ConfigureAudit(Action<AuditConfiguration> configure)
            => _auditManager?.Configure(configure);

        /// <summary>
        /// Returns audit log entries from the store with optional filters.
        /// </summary>
        public IReadOnlyList<AuditEntry> GetAuditLog(
            string blockName         = null,
            AuditOperation? operation = null,
            DateTime? from           = null,
            DateTime? to             = null)
            => _auditManager?.GetAuditLog(blockName, operation, from, to)
               ?? new List<AuditEntry>();

        /// <summary>
        /// Returns the change history for a specific field/record combination.
        /// </summary>
        public IReadOnlyList<AuditFieldChange> GetFieldHistory(
            string blockName, string recordKey, string fieldName)
            => _auditManager?.GetFieldHistory(blockName, recordKey, fieldName)
               ?? new List<AuditFieldChange>();

        /// <summary>Exports audit log to a CSV file.</summary>
        public Task ExportAuditToCsvAsync(string filePath, string blockName = null)
            => _auditManager?.ExportToCsvAsync(filePath, blockName) ?? Task.CompletedTask;

        /// <summary>Exports audit log to a JSON file.</summary>
        public Task ExportAuditToJsonAsync(string filePath, string blockName = null)
            => _auditManager?.ExportToJsonAsync(filePath, blockName) ?? Task.CompletedTask;

        /// <summary>Purges audit entries older than <paramref name="olderThanDays"/> days.</summary>
        public void PurgeAudit(int olderThanDays)
            => _auditManager?.Purge(olderThanDays);

        /// <summary>Clears all audit data from the store.</summary>
        public void ClearAudit()
            => _auditManager?.Clear();

        /// <summary>Exposes the underlying <see cref="IAuditManager"/> for advanced usage.</summary>
        public IAuditManager AuditManager => _auditManager;

        #endregion
    }
}
