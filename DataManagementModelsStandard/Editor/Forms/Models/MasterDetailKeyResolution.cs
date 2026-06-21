using System.Collections.Generic;
using System.Linq;

namespace TheTechIdea.Beep.Editor.Forms.Models
{
    /// <summary>Identifies how a master-detail key mapping was resolved.</summary>
    public enum MasterDetailKeyResolutionSource
    {
        /// <summary>Resolved from explicit configuration values.</summary>
        ExplicitConfiguration,

        /// <summary>Resolved from entity relation metadata.</summary>
        EntityRelations,

        /// <summary>Resolved from datasource foreign-key metadata.</summary>
        DataSourceForeignKeys,

        /// <summary>Resolved by matching primary-key style field names.</summary>
        MatchingPrimaryKeyNames,

        /// <summary>No resolution could be determined.</summary>
        Unresolved
    }

    /// <summary>Represents a single master-field to detail-field mapping.</summary>
    public sealed class DataBlockFieldMapping
    {
        /// <summary>Gets or sets the master field name.</summary>
        public string MasterField { get; set; }

        /// <summary>Gets or sets the detail field name.</summary>
        public string DetailField { get; set; }
    }

    /// <summary>Contains the outcome of resolving a master-detail key mapping.</summary>
    public sealed class MasterDetailKeyResolution
    {
        /// <summary>Gets or sets whether the mapping was resolved.</summary>
        public bool IsResolved { get; set; }

        /// <summary>Gets or sets the source used to resolve the mapping.</summary>
        public MasterDetailKeyResolutionSource Source { get; set; } = MasterDetailKeyResolutionSource.Unresolved;

        /// <summary>Gets or sets the resolved field mappings.</summary>
        public List<DataBlockFieldMapping> Mappings { get; set; } = new();

        /// <summary>Gets the non-fatal warnings generated during resolution.</summary>
        public List<string> Warnings { get; } = new();

        /// <summary>Gets or sets the fatal resolution error message.</summary>
        public string ErrorMessage { get; set; }

        /// <summary>Gets the resolved master key field list as a comma-separated string.</summary>
        public string MasterKeyField => string.Join(", ", Mappings.Select(mapping => mapping.MasterField));

        /// <summary>Gets the resolved detail foreign-key field list as a comma-separated string.</summary>
        public string DetailForeignKeyField => string.Join(", ", Mappings.Select(mapping => mapping.DetailField));
    }
}