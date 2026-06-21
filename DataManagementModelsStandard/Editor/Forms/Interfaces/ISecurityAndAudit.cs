using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Editor.UOWManager.Configuration;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Editor.Forms.Models;


namespace TheTechIdea.Beep.Editor.UOWManager.Interfaces
{

    // ── Phase 5: Audit Trail ────────────────────────────────────────────────

    #region IAuditStore

    /// <summary>Pluggable persistence back-end for audit entries.</summary>
    public interface IAuditStore
    {
        /// <summary>Persist a single audit entry.</summary>
        void Save(AuditEntry entry);

        /// <summary>Query audit entries with optional filters.</summary>
        IReadOnlyList<AuditEntry> Query(
            string blockName         = null,
            AuditOperation? operation = null,
            DateTime? from           = null,
            DateTime? to             = null);

        /// <summary>Remove entries older than <paramref name="olderThanDays"/> days.</summary>
        void Purge(int olderThanDays);

        /// <summary>Remove all entries.</summary>
        void Clear();
    }

    #endregion

    #region IAuditManager

    /// <summary>
    /// Manages field-level and commit-level audit recording for the forms engine.
    /// Accumulates pending field changes and flushes them as <see cref="AuditEntry"/>
    /// objects to the configured <see cref="IAuditStore"/> on each commit.
    /// </summary>
    public interface IAuditManager
    {
        /// <summary>Current audit configuration.</summary>
        AuditConfiguration Configuration { get; }

        /// <summary>Name of the currently logged-in user stamped on each audit entry.</summary>
        string CurrentUser { get; }

        /// <summary>The underlying store (injectable for testing or external persistence).</summary>
        IAuditStore Store { get; }

        // ── Configuration ────────────────────────────────────────────────────

        /// <summary>Sets the user name stamped on every subsequent audit entry.</summary>
        void SetAuditUser(string userName);

        /// <summary>Applies configuration changes via a delegate.</summary>
        void Configure(Action<AuditConfiguration> configure);

        // ── Accumulation ─────────────────────────────────────────────────────

        /// <summary>
        /// Records a single field change in the pending buffer.
        /// Called for every <see cref="BlockFieldChangedEventArgs"/> while audit is enabled.
        /// </summary>
        void RecordFieldChange(
            string blockName,
            string fieldName,
            object oldValue,
            object newValue,
            int recordIndex);

        /// <summary>
        /// Flushes all pending field changes as committed audit entries.
        /// Should be called after a successful <c>CommitFormAsync</c>.
        /// </summary>
        void FlushPendingToStore(string formName, AuditOperation operation);

        /// <summary>
        /// Discards all pending (uncommitted) field changes.
        /// Should be called after a rollback.
        /// </summary>
        void DiscardPending();

        // ── Query ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns audit entries from the store, with optional block/operation/date filters.
        /// </summary>
        IReadOnlyList<AuditEntry> GetAuditLog(
            string blockName         = null,
            AuditOperation? operation = null,
            DateTime? from           = null,
            DateTime? to             = null);

        /// <summary>
        /// Returns the history of changes to a specific field within a record.
        /// <paramref name="recordKey"/> is the string form of the row index or PK.
        /// </summary>
        IReadOnlyList<AuditFieldChange> GetFieldHistory(
            string blockName,
            string recordKey,
            string fieldName);

        // ── Export ────────────────────────────────────────────────────────────

        /// <summary>Writes all (or block-filtered) audit entries to a CSV file.</summary>
        System.Threading.Tasks.Task ExportToCsvAsync(string filePath, string blockName = null);

        /// <summary>Writes all (or block-filtered) audit entries to a JSON file.</summary>
        System.Threading.Tasks.Task ExportToJsonAsync(string filePath, string blockName = null);

        // ── Maintenance ───────────────────────────────────────────────────────

        /// <summary>Purges entries older than <paramref name="olderThanDays"/> days.</summary>
        void Purge(int olderThanDays);

        /// <summary>Clears all audit data from the store.</summary>
        void Clear();
    }

    #endregion

    // ── Phase 6: Security & Authorization ──────────────────────────────────

    #region IFieldMaskProvider

    /// <summary>Applies a mask pattern to a raw field value for display purposes.</summary>
    public interface IFieldMaskProvider
    {
        /// <summary>
        /// Returns a masked representation of <paramref name="rawValue"/> using
        /// <paramref name="pattern"/>.  The single character "*" means "hide everything".
        /// Other patterns use '#' as a digit placeholder and '*' for any char.
        /// </summary>
        string Mask(object rawValue, string pattern);
    }

    #endregion

    #region ISecurityManager

    /// <summary>
    /// Controls block- and field-level security for the forms engine.
    /// Integrates with <see cref="IBlockPropertyManager"/> and <see cref="IItemPropertyManager"/>
    /// to enforce permissions at runtime.
    /// </summary>
    public interface ISecurityManager
    {
        /// <summary>Raised whenever a security violation is detected.</summary>
        event EventHandler<SecurityViolationEventArgs> OnSecurityViolation;

        /// <summary>The currently active security context (user + roles).</summary>
        SecurityContext CurrentContext { get; }

        // ── Context ──────────────────────────────────────────────────────────

        /// <summary>Sets the current security context and re-evaluates all block/field permissions.</summary>
        void SetSecurityContext(SecurityContext context);

        // ── Block Security ───────────────────────────────────────────────────

        /// <summary>Registers or replaces block-level security for <paramref name="blockName"/>.</summary>
        void SetBlockSecurity(string blockName, BlockSecurity security);

        /// <summary>Returns the registered security rules for a block, or null if none.</summary>
        BlockSecurity GetBlockSecurity(string blockName);

        /// <summary>
        /// Returns true when the current user/roles may perform <paramref name="permission"/> on the block.
        /// Also applies to a specific permission flag when <c>ISecurityManager</c> is used standalone.
        /// </summary>
        bool IsBlockAllowed(string blockName, SecurityPermission permission);

        /// <summary>
        /// Evaluates all registered block securities against the current context and updates
        /// <c>DataBlockInfo.InsertAllowed</c> / <c>UpdateAllowed</c> / <c>DeleteAllowed</c> / <c>QueryAllowed</c>
        /// via the supplied <paramref name="applyBlockFlags"/> callback.
        /// </summary>
        void ApplyBlockSecurityFlags(Action<string, bool, bool, bool, bool> applyBlockFlags);

        /// <summary>Removes all security rules (block- and field-level) for the named block.</summary>
        void ClearBlockSecurity(string blockName);

        /// <summary>
        /// Returns the effective row-filter WHERE clause for a block (or empty string if none).
        /// </summary>
        string GetBlockRowFilter(string blockName);

        // ── Field Security ───────────────────────────────────────────────────

        /// <summary>Registers or replaces field-level security for a specific item.</summary>
        void SetFieldSecurity(string blockName, string fieldName, FieldSecurity security);

        /// <summary>Returns registered field security or null.</summary>
        FieldSecurity GetFieldSecurity(string blockName, string fieldName);

        /// <summary>
        /// Evaluates all registered field securities against the current context and applies
        /// Enabled / Visible flags via the supplied callbacks (delegates into ItemPropertyManager).
        /// </summary>
        void ApplyFieldSecurityFlags(
            Action<string, string, bool> setEnabled,
            Action<string, string, bool> setVisible);

        /// <summary>
        /// Returns a masked / display-safe value for a field, applying the registered mask pattern
        /// if field security has <c>Masked = true</c>.  Returns the raw value unchanged otherwise.
        /// </summary>
        object GetMaskedValue(string blockName, string fieldName, object rawValue);

        // ── Logging ──────────────────────────────────────────────────────────

        /// <summary>Records a security violation without throwing.</summary>
        void RaiseViolation(string blockName, string fieldName, SecurityPermission permission, string message);

        /// <summary>Returns all recorded violations for the current session.</summary>
        IReadOnlyList<SecurityViolationEventArgs> GetViolationLog();
    }

    #endregion

}
