using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TheTechIdea.Beep.SetUp.Seeding
{
    /// <summary>
    /// Default <see cref="ISeederRegistry"/> implementation.
    /// Uses Kahn's algorithm for topological sorting.
    /// </summary>
    public class SeederRegistry : ISeederRegistry
    {
        private readonly Dictionary<string, ISeeder> _seeders =
            new Dictionary<string, ISeeder>(StringComparer.Ordinal);

        /// <inheritdoc/>
        public IReadOnlyCollection<ISeeder> All => _seeders.Values;

        /// <inheritdoc/>
        public bool Register(ISeeder seeder)
        {
            if (seeder == null) throw new ArgumentNullException(nameof(seeder));
            if (_seeders.ContainsKey(seeder.SeederId)) return false;
            _seeders[seeder.SeederId] = seeder;
            return true;
        }

        /// <inheritdoc/>
        public void DiscoverFromAssemblies(IEnumerable<Assembly> assemblies)
        {
            if (assemblies == null) return;
            foreach (var asm in assemblies)
            {
                IEnumerable<Type> seederTypes;
                try { seederTypes = asm.GetTypes(); }
                catch (ReflectionTypeLoadException ex)
                { seederTypes = ex.Types.Where(t => t != null); }

                foreach (var type in seederTypes)
                {
                    if (type.IsAbstract || type.IsInterface) continue;
                    if (!typeof(ISeeder).IsAssignableFrom(type)) continue;
                    if (type.GetConstructor(Type.EmptyTypes) == null) continue;

                    var instance = (ISeeder)Activator.CreateInstance(type);
                    Register(instance);
                }
            }
        }

        /// <inheritdoc/>
        public ISeeder Get(string seederId) =>
            _seeders.TryGetValue(seederId, out var s) ? s : null;

        /// <inheritdoc/>
        public IReadOnlyList<ISeeder> GetOrderedSeeders()
        {
            // Kahn's topological sort
            var inDegree = _seeders.Keys.ToDictionary(k => k, _ => 0, StringComparer.Ordinal);
            var adj = _seeders.Keys.ToDictionary(
                k => k, _ => new List<string>(), StringComparer.Ordinal);

            foreach (var seeder in _seeders.Values)
            {
                foreach (var dep in seeder.DependsOn)
                {
                    if (!_seeders.ContainsKey(dep))
                        throw new InvalidOperationException(
                            $"Seeder '{seeder.SeederId}' declares dependency on " +
                            $"unknown seeder '{dep}'. Register the dependency first.");

                    adj[dep].Add(seeder.SeederId);
                    inDegree[seeder.SeederId]++;
                }
            }

            var queue = new Queue<string>(
                inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
            var ordered = new List<ISeeder>(_seeders.Count);

            while (queue.Count > 0)
            {
                var id = queue.Dequeue();
                ordered.Add(_seeders[id]);
                foreach (var next in adj[id])
                {
                    if (--inDegree[next] == 0)
                        queue.Enqueue(next);
                }
            }

            if (ordered.Count != _seeders.Count)
                throw new InvalidOperationException(
                    "Circular dependency detected among registered seeders. " +
                    "Check DependsOn declarations.");

            return ordered.AsReadOnly();
        }
    }
}
