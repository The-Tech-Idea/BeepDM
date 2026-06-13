using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Editor.EntityDiscovery
{
    /// <summary>
    /// Contract for discovering candidate Entity / POCO / EF Core types from
    /// registered and loaded assemblies.
    /// </summary>
    public interface IEntityDiscoveryService
    {
        void RegisterAssembly(Assembly assembly);
        IReadOnlyList<Assembly> GetRegisteredAssemblies();

        List<DiscoveredEntity> Discover(EntityDiscoveryOptions options);

        Task<List<DiscoveredEntity>> DiscoverAsync(
            EntityDiscoveryOptions options,
            IProgress<int>? progress = null,
            CancellationToken token = default);

        IReadOnlyList<Assembly> ResolveScopeAssemblies(EntityDiscoveryOptions options);

        List<DiscoveredEntity> DiscoverEntities(
            string namespaceName = null,
            Assembly assembly = null,
            bool includeSubNamespaces = true);

        List<DiscoveredEntity> DiscoverAllEntities(bool includeSubNamespaces = true);

        List<DiscoveredEntity> ScanAssemblyForEntities(
            Assembly assembly,
            bool includeSubNamespaces = true);

        Dictionary<string, List<DiscoveredEntity>> GroupEntitiesByAssembly(
            string namespaceName = null,
            Assembly assembly = null,
            bool includeSubNamespaces = true);
    }
}
