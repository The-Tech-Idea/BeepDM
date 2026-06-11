using System;
using System.Collections.Generic;

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

        /// <summary>Gets or sets the key field in the master block</summary>
        public string MasterKeyField { get; set; }

        /// <summary>Gets or sets the foreign key field in the detail block</summary>
        public string DetailForeignKeyField { get; set; }

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
    }
}