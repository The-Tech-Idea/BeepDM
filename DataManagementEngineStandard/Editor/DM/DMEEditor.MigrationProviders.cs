using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.SchemaMigration;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep
{
    /// <summary>
    /// DMEEditor partial class for the Phase 10 schema-migration-provider framework.
    /// Owns the <see cref="IMigrationProviderRegistry"/> and exposes
    /// <see cref="GetMigrationProvider"/> required by <see cref="IDMEEditor"/>.
    /// </summary>
    public partial class DMEEditor
    {
        private IMigrationProviderRegistry _migrationProviders;
        private readonly object _migrationProvidersLock = new object();

        /// <summary>
        /// The migration-provider registry. Lazily created and configured with the built-in
        /// Tier-2 category fallbacks. Tier-1 overrides are discovered automatically via
        /// <c>[SchemaMigrationProvider]</c> attribute scan on first resolution.
        /// </summary>
        public IMigrationProviderRegistry MigrationProviders
        {
            get
            {
                if (_migrationProviders == null)
                {
                    lock (_migrationProvidersLock)
                    {
                        if (_migrationProviders == null)
                        {
                            var registry = new MigrationProviderRegistry(Logger);

                            // Tier-2 category fallbacks. Only categories with a meaningful default
                            // are registered here; everything else falls through to the Tier-3
                            // NullMigrationProvider (clean "unsupported" outcome).
                            registry.RegisterCategoryFallback(
                                DatasourceCategory.RDBMS,
                                owner => new RdbmsSqlMigrationProvider(owner));

                            registry.RegisterCategoryFallback(
                                DatasourceCategory.FILE,
                                owner => new FileMutationMigrationProvider(owner));

                            // External / vendor-owned schema: SaaS connectors, messaging, queues,
                            // streams, web APIs. Schema cannot be mutated from BeepDM.
                            var externalCategories = new[]
                            {
                                DatasourceCategory.Connector,
                                DatasourceCategory.STREAM,
                                DatasourceCategory.QUEUE,
                                DatasourceCategory.WEBAPI
                            };
                            foreach (var cat in externalCategories)
                            {
                                registry.RegisterCategoryFallback(
                                    cat,
                                    owner => new ExternalReadOnlyMigrationProvider(owner));
                            }

                            _migrationProviders = registry;
                        }
                    }
                }
                return _migrationProviders;
            }
        }

        /// <summary>
        /// Resolves the <see cref="ISchemaMigrationProvider"/> for the given data source
        /// (3-tier: exact type → category fallback → null). Never returns null.
        /// </summary>
        public ISchemaMigrationProvider GetMigrationProvider(IDataSource dataSource)
        {
            try
            {
                var provider = MigrationProviders.Resolve(dataSource);
                if (provider == null)
                {
                    Logger?.WriteLog($"Migration provider resolve returned null for {dataSource?.DatasourceType}; using NullMigrationProvider.");
                    return new NullMigrationProvider(dataSource);
                }
                return provider;
            }
            catch (System.Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
                ErrorObject.Message = $"Failed to resolve migration provider for {dataSource?.DatasourceType}: {ex.Message}";
                Logger?.WriteLog($"ERROR: {ErrorObject.Message}");
                return new NullMigrationProvider(dataSource);
            }
        }
    }
}
