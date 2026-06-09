using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Migration;

namespace TheTechIdea.Beep.Editor.EntityDiscovery
{
    public class EntityDiscoveryService
    {
        private readonly IDMEEditor _editor;

        public EntityDiscoveryService(IDMEEditor editor)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        }

        public void RegisterAssembly(Assembly assembly)
        {
            if (assembly == null) return;
            var migration = new MigrationManager(_editor);
            migration.RegisterAssembly(assembly);
            _editor.AddLogMessage("EntityDiscovery",
                $"Registered assembly '{assembly.GetName().Name}' for entity discovery",
                DateTime.Now, 0, null, Errors.Ok);
        }

        public void UnregisterAssembly(Assembly assembly)
        {
            if (assembly == null) return;
            var migration = new MigrationManager(_editor);
            migration.UnregisterAssembly(assembly);
            _editor.AddLogMessage("EntityDiscovery",
                $"Unregistered assembly '{assembly.GetName().Name}' from entity discovery",
                DateTime.Now, 0, null, Errors.Ok);
        }

        public IReadOnlyList<Assembly> GetRegisteredAssemblies()
        {
            var migration = new MigrationManager(_editor);
            return migration.GetRegisteredAssemblies();
        }

        public List<DiscoveredEntity> DiscoverEntities(
            string namespaceName = null,
            Assembly assembly = null,
            bool includeSubNamespaces = true)
        {
            var migration = new MigrationManager(_editor);
            var entityTypes = migration.DiscoverEntityTypes(namespaceName, assembly, includeSubNamespaces);
            return MapToDiscoveredEntities(entityTypes);
        }

        public List<DiscoveredEntity> DiscoverAllEntities(bool includeSubNamespaces = true)
        {
            return DiscoverEntities(null, null, includeSubNamespaces);
        }

        public List<DiscoveredEntity> ScanAssemblyForEntities(Assembly assembly, bool includeSubNamespaces = true)
        {
            if (assembly == null) return new List<DiscoveredEntity>();

            var migration = new MigrationManager(_editor);
            var entityTypes = migration.DiscoverEntityTypes(null, assembly, includeSubNamespaces);
            var entities = MapToDiscoveredEntities(entityTypes);

            var migrationTypes = new HashSet<Type>(entityTypes);
            var additionalPocos = new List<DiscoveredEntity>();

            try
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type == null || migrationTypes.Contains(type)) continue;
                    if (type.IsNotPublic) continue;
                    if (type.IsInterface || type.IsAbstract || type.IsGenericTypeDefinition) continue;
                    if (type.IsNested && !type.IsNestedPublic) continue;
                    if (!type.IsClass) continue;

                    var classCreator = _editor.classCreator;
                    if (classCreator != null && classCreator.IsDiscoverablePoco(type))
                    {
                        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        var category = classCreator.IsEfDecoratedType(type)
                            ? EntityCategory.EfCore
                            : EntityCategory.Poco;

                        additionalPocos.Add(new DiscoveredEntity
                        {
                            Name = type.Name,
                            FullName = type.FullName ?? type.Name,
                            AssemblyName = assembly.GetName().Name ?? string.Empty,
                            PropertyCount = properties.Length,
                            Category = category,
                            Namespace = type.Namespace ?? string.Empty,
                            HasParameterlessConstructor = type.GetConstructor(Type.EmptyTypes) != null
                        });
                    }
                }
            }
            catch
            {
            }

            var result = new List<DiscoveredEntity>();
            result.AddRange(entities);
            result.AddRange(additionalPocos);
            return result.DistinctBy(e => e.FullName).ToList();
        }

        public Dictionary<string, List<DiscoveredEntity>> GroupEntitiesByAssembly(
            string namespaceName = null,
            Assembly assembly = null,
            bool includeSubNamespaces = true)
        {
            var entities = DiscoverEntities(namespaceName, assembly, includeSubNamespaces);
            return entities
                .GroupBy(e => e.AssemblyName)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        private List<DiscoveredEntity> MapToDiscoveredEntities(List<Type> entityTypes)
        {
            var result = new List<DiscoveredEntity>();

            foreach (var type in entityTypes)
            {
                if (type == null) continue;

                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                EntityCategory category;

                if (typeof(IEntity).IsAssignableFrom(type) ||
                    (type.BaseType != null && type.BaseType.Name == "Entity"))
                {
                    category = EntityCategory.Entity;
                }
                else if (_editor.classCreator?.IsEfDecoratedType(type) == true)
                {
                    category = EntityCategory.EfCore;
                }
                else
                {
                    category = EntityCategory.Poco;
                }

                result.Add(new DiscoveredEntity
                {
                    Name = type.Name,
                    FullName = type.FullName ?? type.Name,
                    AssemblyName = type.Assembly.GetName().Name ?? string.Empty,
                    PropertyCount = properties.Length,
                    Category = category,
                    Namespace = type.Namespace ?? string.Empty,
                    HasParameterlessConstructor = type.GetConstructor(Type.EmptyTypes) != null
                });
            }

            return result;
        }
    }
}
