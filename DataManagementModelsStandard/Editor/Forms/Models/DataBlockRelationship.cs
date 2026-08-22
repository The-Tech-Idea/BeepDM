using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Editor.Forms.Models;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Extended data block relationship with additional metadata.
    /// </summary>
    /// <remarks>
    /// B36/D6 (audit pass 3, 2026-06): the previous version carried a
    /// number of fields that were never read by the master/detail
    /// engine (<c>CascadeDelete</c>, <c>CascadeUpdate</c>, <c>Strength</c>,
    /// <c>CustomSyncLogic</c>, <c>Metrics</c>, <c>ExtendedProperties</c>).
    /// The cascade / strength / custom-sync fields were placeholders for
    /// a feature that was never built; the metrics were never updated
    /// anywhere. The supporting <c>RelationshipStrength</c> enum and
    /// <c>RelationshipMetrics</c> class are also removed. External hosts
    /// that depended on the removed fields must migrate — the engine
    /// does not read them, so removing the fields has no runtime
    /// behavior change beyond compile-time breakage.
    /// </remarks>
    public class DataBlockRelationship
    {
        /// <summary>Gets or sets the name of the master block</summary>
        public string MasterBlockName { get; set; }

        /// <summary>Gets or sets the name of the detail block</summary>
        public string DetailBlockName { get; set; }

        /// <summary>Gets or sets the key field in the master block (first key for composite)</summary>
        public string MasterKeyField { get; set; }

        /// <summary>Gets or sets the foreign key field in the detail block (first key for composite)</summary>
        public string DetailForeignKeyField { get; set; }

        /// <summary>Gets or sets the resolved field mappings for composite-key relationships</summary>
        public List<DataBlockFieldMapping> KeyFieldMappings { get; set; } = new();

        /// <summary>Gets the master key fields as a read-only list (from Mappings or the single key)</summary>
        public IReadOnlyList<string> MasterKeyFields =>
            KeyFieldMappings.Count > 0
                ? KeyFieldMappings.Select(m => m.MasterField).ToList().AsReadOnly()
                : (MasterKeyField != null ? new List<string> { MasterKeyField }.AsReadOnly() : Array.Empty<string>());

        /// <summary>Gets the detail foreign-key fields as a read-only list</summary>
        public IReadOnlyList<string> DetailForeignKeyFields =>
            KeyFieldMappings.Count > 0
                ? KeyFieldMappings.Select(m => m.DetailField).ToList().AsReadOnly()
                : (DetailForeignKeyField != null ? new List<string> { DetailForeignKeyField }.AsReadOnly() : Array.Empty<string>());

        /// <summary>Gets or sets the type of relationship</summary>
        public RelationshipType RelationshipType { get; set; } = RelationshipType.OneToMany;

        /// <summary>Gets or sets whether the relationship is active</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Gets or sets when the relationship was created</summary>
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /// <summary>Gets or sets when the relationship was last modified</summary>
        public DateTime? ModifiedDate { get; set; }

        /// <summary>Gets or sets a description of the relationship</summary>
        public string Description { get; set; }

        /// <summary>
        /// Oracle Forms master-detail delete behavior, added 2026-08-22. Default
        /// matches Oracle's own default (<see cref="MasterDeleteBehavior.NonIsolated"/>).
        /// A previous attempt at this (a bare <c>CascadeDelete</c> bool) was
        /// removed 2026-06 as an unwired placeholder — see the class remarks.
        /// This one is read by <c>FormsManager.DeleteCurrentRecordAsync</c>.
        /// </summary>
        public MasterDeleteBehavior DeleteBehavior { get; set; } = MasterDeleteBehavior.NonIsolated;

        /// <summary>
        /// Whether the detail block re-queries the instant the master's current
        /// record changes (<see cref="DetailCoordination.Immediate"/>, Oracle's
        /// default and this engine's only behavior until 2026-08-22) or only
        /// when something explicitly asks for it
        /// (<see cref="DetailCoordination.Deferred"/>). Read by
        /// <c>FormsManager</c>'s master-current-changed handler.
        /// </summary>
        public DetailCoordination Coordination { get; set; } = DetailCoordination.Immediate;
    }

    /// <summary>
    /// Oracle Forms master-detail delete behavior — what happens when a master
    /// record with existing detail records is deleted.
    /// </summary>
    public enum MasterDeleteBehavior
    {
        /// <summary>
        /// Oracle's default. The delete is blocked while detail records exist
        /// for the current master record.
        /// </summary>
        NonIsolated = 0,

        /// <summary>
        /// The master can be deleted regardless of existing detail records —
        /// orphaned detail rows are left as-is (their foreign key now points
        /// at a master record that no longer exists).
        /// </summary>
        Isolated = 1,

        /// <summary>
        /// Deleting the master first deletes every detail record, through the
        /// detail block's own delete pipeline (so the detail's own triggers —
        /// including a further Cascading relationship on IT — still fire),
        /// then deletes the master.
        /// </summary>
        Cascading = 2,
    }

    /// <summary>
    /// Oracle Forms master-detail re-query timing.
    /// </summary>
    public enum DetailCoordination
    {
        /// <summary>Oracle's default — the detail re-queries the instant the master's current record changes.</summary>
        Immediate = 0,

        /// <summary>The detail does not re-query until something explicitly asks for it.</summary>
        Deferred = 1,
    }
}