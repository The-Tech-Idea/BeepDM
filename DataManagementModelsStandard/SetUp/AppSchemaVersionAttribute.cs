using System;

namespace TheTechIdea.Beep.SetUp
{
    /// <summary>
    /// Declares the schema version an application expects its datasource to satisfy. A developer
    /// bumps this when the entity model changes; the next bootstrap upgrade pass compares it against
    /// the version recorded in the database and migrates when the app has moved ahead.
    /// </summary>
    /// <remarks>
    /// Place on the assembly that owns the entity types, e.g.
    /// <c>[assembly: AppSchemaVersion("2.3.0")]</c>. This is the coarse "version gate" trigger; the
    /// entity-model diff (<c>MigrationManager.BuildMigrationPlanForTypes</c>) remains the authority on
    /// what actually gets applied. When no attribute (and no explicit
    /// <see cref="SetupOptions.DeclaredSchemaVersion"/>) is present, the gate falls back to diff-only
    /// and never blocks a needed migration.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public sealed class AppSchemaVersionAttribute : Attribute
    {
        public AppSchemaVersionAttribute(string version)
        {
            Version = string.IsNullOrWhiteSpace(version) ? "0.0.0" : version.Trim();
        }

        /// <summary>Semantic version the app declares, e.g. "2.3.0".</summary>
        public string Version { get; }

        /// <summary>
        /// Optional minimum database version this build refuses to run below. When set and the
        /// recorded DB version is older, the gate treats the datasource as needing migration.
        /// </summary>
        public string? MinDatabaseVersion { get; init; }
    }
}
