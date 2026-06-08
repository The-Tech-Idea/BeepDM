using System;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Mirrors Oracle Forms <c>:SYSTEM.RECORD_STATUS</c>. Distinct from
    /// <see cref="DataBlockMode"/> which only describes the overall block mode
    /// (Normal / Enter-Query / Query).
    /// </summary>
    public enum BeepRecordStatus
    {
        /// <summary>
        /// Default state of a fetched record that has not been changed.
        /// Maps to <c>:SYSTEM.RECORD_STATUS = 'QUERY'</c>.
        /// </summary>
        Query = 0,

        /// <summary>
        /// Brand new record that has been created with <c>CREATE_RECORD</c> and
        /// not yet committed. Maps to <c>:SYSTEM.RECORD_STATUS = 'NEW'</c>.
        /// </summary>
        New = 1,

        /// <summary>
        /// New record that was successfully inserted into the database. Maps to
        /// <c>:SYSTEM.RECORD_STATUS = 'INSERT'</c>. Beep stores this state so
        /// trigger handlers can distinguish "uncommitted new" from
        /// "newly inserted" the way Oracle Forms does.
        /// </summary>
        Insert = 2,

        /// <summary>
        /// Fetched record that has at least one field updated and not yet
        /// committed. Maps to <c>:SYSTEM.RECORD_STATUS = 'CHANGED'</c>.
        /// </summary>
        Changed = 3,

        /// <summary>
        /// Record in the query criteria area. Maps to
        /// <c>:SYSTEM.RECORD_STATUS = 'QUERY'</c> when the block is in
        /// <see cref="DataBlockMode.EnterQuery"/>.
        /// </summary>
        QueryCriteria = 4
    }

    /// <summary>
    /// Helpers to translate between the various status enums the editor exposes
    /// and the string form Oracle Forms queries use.
    /// </summary>
    public static class BeepRecordStatusExtensions
    {
        public static string ToFormsString(this BeepRecordStatus status) => status switch
        {
            BeepRecordStatus.New => "NEW",
            BeepRecordStatus.Insert => "INSERT",
            BeepRecordStatus.Changed => "CHANGED",
            BeepRecordStatus.QueryCriteria => "QUERY",
            _ => "QUERY"
        };

        public static BeepRecordStatus FromString(string? value) => value?.ToUpperInvariant() switch
        {
            "NEW" => BeepRecordStatus.New,
            "INSERT" => BeepRecordStatus.Insert,
            "CHANGED" => BeepRecordStatus.Changed,
            "QUERY" => BeepRecordStatus.QueryCriteria,
            _ => BeepRecordStatus.Query
        };

        /// <summary>
        /// Returns true when the record has unsaved changes (NEW, INSERT, or
        /// CHANGED) the way Oracle Forms would surface "the block has changes".
        /// </summary>
        public static bool IsUnsaved(this BeepRecordStatus status) =>
            status != BeepRecordStatus.Query && status != BeepRecordStatus.QueryCriteria;

        /// <summary>
        /// Returns true when the record was created by a <c>CREATE_RECORD</c>
        /// call (NEW or INSERT) and therefore should be reported as a new row
        /// by visual indicators (asterisk in the row header, etc.).
        /// </summary>
        public static bool IsNewRow(this BeepRecordStatus status) =>
            status == BeepRecordStatus.New || status == BeepRecordStatus.Insert;
    }
}
