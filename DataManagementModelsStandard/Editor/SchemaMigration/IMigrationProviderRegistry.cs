using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.SchemaMigration
{
    /// <summary>
    /// Resolves an <see cref="ISchemaMigrationProvider"/> for a given data source using a
    /// 3-tier strategy:
    /// <list type="number">
        ///   <item><description><b>Tier 1 — exact type override</b>: a provider registered for the
        ///     exact <see cref="DataSourceType"/> (typically discovered via
        ///     <c>SchemaMigrationProviderAttribute</c>).</description></item>
        ///   <item><description><b>Tier 2 — category fallback</b>: a provider registered as the
        ///     default for the <see cref="DatasourceCategory"/> (e.g. RDBMS → SQL provider,
        ///     Connector → read-only provider).</description></item>
    ///   <item><description><b>Tier 3 — null</b>: the null provider, which
    ///     reports every operation as unsupported.</description></item>
    /// </list>
    /// <see cref="Resolve"/> never returns null.
    /// </summary>
    public interface IMigrationProviderRegistry
    {
        /// <summary>
        /// Registers a Tier-1 factory for an exact <see cref="DataSourceType"/>.
        /// The factory receives the live <see cref="IDataSource"/> being migrated.
        /// </summary>
        void Register(DataSourceType dataSourceType, Func<IDataSource, ISchemaMigrationProvider> factory);

        /// <summary>
        /// Registers a Tier-2 fallback factory for a <see cref="DatasourceCategory"/>.
        /// Used when no exact-type override is registered.
        /// </summary>
        void RegisterCategoryFallback(DatasourceCategory category, Func<IDataSource, ISchemaMigrationProvider> factory);

        /// <summary>
        /// Resolves a provider for the given data source. Never returns null.
        /// Order: exact type → category fallback → <see cref="NullMigrationProvider"/>.
        /// </summary>
        ISchemaMigrationProvider Resolve(IDataSource dataSource);

        /// <summary>Snapshot of every Tier-1 type currently registered (for diagnostics/tests).</summary>
        IReadOnlyCollection<DataSourceType> RegisteredTypes { get; }
    }
}
