using System.Collections.Generic;
using System.Reflection;

namespace TheTechIdea.Beep.SetUp.Seeding
{
    /// <summary>
    /// Manages a collection of <see cref="ISeeder"/> instances and resolves their
    /// execution order using topological sorting.
    /// </summary>
    public interface ISeederRegistry
    {
        /// <summary>All registered seeders (unordered).</summary>
        IReadOnlyCollection<ISeeder> All { get; }

        /// <summary>
        /// Registers a seeder.
        /// Returns <c>false</c> (no-op) if a seeder with the same <see cref="ISeeder.SeederId"/> is already registered.
        /// </summary>
        bool Register(ISeeder seeder);

        /// <summary>
        /// Scans the provided assemblies for concrete <see cref="ISeeder"/> types
        /// with a public parameterless constructor and registers them.
        /// </summary>
        void DiscoverFromAssemblies(IEnumerable<Assembly> assemblies);

        /// <summary>
        /// Returns all registered seeders sorted so that each seeder's dependencies
        /// appear before it in the list.
        /// Throws <see cref="System.InvalidOperationException"/> on circular dependency.
        /// </summary>
        IReadOnlyList<ISeeder> GetOrderedSeeders();

        /// <summary>Returns the seeder registered with <paramref name="seederId"/>, or <c>null</c>.</summary>
        ISeeder Get(string seederId);
    }
}
