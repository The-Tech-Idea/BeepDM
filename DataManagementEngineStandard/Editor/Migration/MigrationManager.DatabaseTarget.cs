using TheTechIdea.Beep.Editor.SchemaMigration;

namespace TheTechIdea.Beep.Editor.Migration
{
    public partial class MigrationManager
    {
        /// <summary>Bind native database creation to the framework's guarded pre-open lifecycle.
        /// A catalog must exist before a datasource can open and migrate its entity schema.</summary>
        public static DatabaseTargetMigration BindDatabaseTarget(IDatabaseTargetMigrationProvider provider, string claimPath,
            DatabaseTargetLegacyClaimBinding legacyClaimBinding = null)
            => new DatabaseTargetMigration(provider, claimPath, legacyClaimBinding);
    }
}
