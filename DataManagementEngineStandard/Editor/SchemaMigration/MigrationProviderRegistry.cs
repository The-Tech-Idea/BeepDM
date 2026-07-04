using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.SchemaMigration
{
    /// <summary>
    /// Default <see cref="IMigrationProviderRegistry"/>. Resolves providers 3-tier:
    /// exact <see cref="DataSourceType"/> (Tier 1, from <see cref="SchemaMigrationProviderAttribute"/>
    /// scan or explicit <see cref="Register"/>) → <see cref="DatasourceCategory"/> fallback
    /// (Tier 2, from <see cref="RegisterCategoryFallback"/>) → <see cref="NullMigrationProvider"/> (Tier 3).
    /// </summary>
    public class MigrationProviderRegistry : IMigrationProviderRegistry
    {
        private readonly Dictionary<DataSourceType, Func<IDataSource, ISchemaMigrationProvider>> _byType = new();
        private readonly Dictionary<DatasourceCategory, Func<IDataSource, ISchemaMigrationProvider>> _byCategory = new();
        private readonly IDMLogger _logger;
        private readonly object _scanLock = new object();
        private bool _scanned;

        public MigrationProviderRegistry(IDMLogger logger = null)
        {
            _logger = logger;
        }

        public IReadOnlyCollection<DataSourceType> RegisteredTypes => _byType.Keys.ToList();

        /// <summary>Registers a Tier-1 factory for an exact data-source type.</summary>
        public void Register(DataSourceType dataSourceType, Func<IDataSource, ISchemaMigrationProvider> factory)
        {
            if (factory == null) return;
            _byType[dataSourceType] = factory;
        }

        /// <summary>Registers a Tier-2 fallback factory for a data-source category.</summary>
        public void RegisterCategoryFallback(DatasourceCategory category, Func<IDataSource, ISchemaMigrationProvider> factory)
        {
            if (factory == null) return;
            _byCategory[category] = factory;
        }

        /// <summary>
        /// Resolves a provider for <paramref name="dataSource"/>. Tier 1 → Tier 2 → Tier 3.
        /// Never returns null.
        /// </summary>
        public ISchemaMigrationProvider Resolve(IDataSource dataSource)
        {
            if (dataSource == null) return new NullMigrationProvider(null);

            EnsureScanned();

            // Tier 1 — exact type override
            if (_byType.TryGetValue(dataSource.DatasourceType, out var typeFactory))
            {
                return SafeCreate(typeFactory, dataSource)
                    ?? new NullMigrationProvider(dataSource);
            }

            // Tier 2 — category fallback
            if (_byCategory.TryGetValue(dataSource.Category, out var catFactory))
            {
                return SafeCreate(catFactory, dataSource)
                    ?? new NullMigrationProvider(dataSource);
            }

            // Tier 3 — null
            return new NullMigrationProvider(dataSource);
        }

        /// <summary>
        /// Scans all loaded assemblies exactly once for classes decorated with
        /// <see cref="SchemaMigrationProviderAttribute"/> and registers a reflective factory
        /// for each (constructor must accept a single <see cref="IDataSource"/>).
        /// </summary>
        private void EnsureScanned()
        {
            if (_scanned) return;
            lock (_scanLock)
            {
                if (_scanned) return;
                try
                {
                    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        Type[] types;
                        try { types = asm.GetTypes(); }
                        catch (ReflectionTypeLoadException) { continue; }

                        foreach (var t in types)
                        {
                            var attr = t.GetCustomAttribute<SchemaMigrationProviderAttribute>();
                            if (attr == null) continue;
                            if (!typeof(ISchemaMigrationProvider).IsAssignableFrom(t)) continue;

                            var typeCaptured = t;
                            Register(attr.DataSourceType, owner => CreateReflective(typeCaptured, owner));
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.WriteLog($"SchemaMigrationProvider scan error: {ex.Message}");
                }
                finally
                {
                    _scanned = true;
                }
            }
        }

        private static ISchemaMigrationProvider CreateReflective(Type providerType, IDataSource owner)
        {
            // Require a public ctor taking a single IDataSource.
            var ctor = providerType.GetConstructor(new[] { typeof(IDataSource) })
                       ?? providerType.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null,
                            new[] { typeof(IDataSource) }, null);
            if (ctor == null)
                throw new InvalidOperationException(
                    $"{providerType.FullName} has no public constructor taking IDataSource.");
            return (ISchemaMigrationProvider)ctor.Invoke(new object[] { owner });
        }

        private ISchemaMigrationProvider SafeCreate(Func<IDataSource, ISchemaMigrationProvider> factory, IDataSource owner)
        {
            try { return factory(owner); }
            catch (Exception ex)
            {
                _logger?.WriteLog($"SchemaMigrationProvider factory failed for {owner.DatasourceType}: {ex.Message}");
                return null;
            }
        }
    }
}
