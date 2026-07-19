using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TheTechIdea.Beep.Editor.Migration
{
    /// <summary>
    /// Row shape of the in-database schema-version marker table
    /// (<see cref="DbSchemaVersionStore.MarkerEntityName"/>). One current-version row is kept per
    /// datasource; full history lives in the JSON audit mirror (<c>IVersionManagementService</c>).
    /// Property names map 1:1 to the marker columns, so a datasource can insert/update an instance
    /// directly.
    /// </summary>
    [Table(DbSchemaVersionStore.MarkerEntityName)]
    public sealed class BeepSchemaVersionRecord
    {
        /// <summary>Single-row upsert key. Always 1 — the marker holds the current version, not history.</summary>
        [Key]
        public int Id { get; set; } = 1;

        /// <summary>Last-applied semantic version string (e.g. "2.3.1").</summary>
        public string Version { get; set; } = "0.0.0";

        public int Major { get; set; }
        public int Minor { get; set; }
        public int Patch { get; set; }

        /// <summary>SHA-256 of the entity list at this version (matches SetupState.SchemaHash).</summary>
        public string SchemaHash { get; set; } = string.Empty;

        /// <summary>Hash of the migration plan that produced this version.</summary>
        public string MigrationPlanHash { get; set; } = string.Empty;

        public DateTime AppliedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>Actor that applied this version.</summary>
        public string AppliedBy { get; set; } = string.Empty;

        /// <summary>Lossless JSON of the full <c>DatabaseVersion</c>, so a reader reconstructs it exactly.</summary>
        public string PayloadJson { get; set; } = string.Empty;
    }
}
