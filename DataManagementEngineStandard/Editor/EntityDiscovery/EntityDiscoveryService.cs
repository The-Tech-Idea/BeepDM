using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.EntityDiscovery
{
    public class EntityDiscoveryService : IEntityDiscoveryService
    {
        private readonly IDMEEditor _editor;
        private readonly IDiscoveryCache? _cache;
        private readonly IProjectScopeStrategy _scopeStrategy;
        private readonly List<Assembly> _registeredAssemblies = new();
        private readonly object _registeredLock = new();

        public EntityDiscoveryService(
            IDMEEditor editor,
            IDiscoveryCache? cache = null,
            IProjectScopeStrategy? scopeStrategy = null)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            _cache = cache;
            _scopeStrategy = scopeStrategy ?? ProjectScopeResolver.Default;
        }

        public void RegisterAssembly(Assembly assembly)
        {
            if (assembly == null) return;
            lock (_registeredLock)
            {
                if (!_registeredAssemblies.Contains(assembly))
                    _registeredAssemblies.Add(assembly);
            }
            _editor.AddLogMessage("EntityDiscovery",
                $"Registered assembly '{assembly.GetName().Name}' for entity discovery",
                DateTime.Now, 0, null, Errors.Ok);
        }

        public IReadOnlyList<Assembly> GetRegisteredAssemblies()
        {
            lock (_registeredLock)
                return _registeredAssemblies.ToList();
        }

        public List<DiscoveredEntity> Discover(EntityDiscoveryOptions options)
        {
            // Task.Run, not ConfigureAwait(false): ConfigureAwait only helps if EVERY await down
            // the chain uses it — one that does not posts its continuation back to the caller's
            // context, and a UI thread blocked here in GetResult() can never run it. Task.Run
            // removes the context entirely, so the whole chain resumes on the thread pool.
            return Task.Run(() => DiscoverAsync(options, null, CancellationToken.None)).GetAwaiter().GetResult();
        }

        public async Task<List<DiscoveredEntity>> DiscoverAsync(
            EntityDiscoveryOptions options,
            IProgress<int>? progress = null,
            CancellationToken token = default)
        {
            if (options == null) options = new EntityDiscoveryOptions();

            var cacheKey = BuildCacheKey(options);

            if (_cache != null)
            {
                var cached = _cache.GetOrAdd(cacheKey, () =>
                {
                    return Task.Run(() => DiscoverCoreAsync(options, progress, token)).GetAwaiter().GetResult()
                        .ToList().AsReadOnly();
                });
                return cached.ToList();
            }

            return await DiscoverCoreAsync(options, progress, token).ConfigureAwait(false);
        }

        private static string BuildCacheKey(EntityDiscoveryOptions options)
        {
            var scope = options.Scope.ToString();
            var ns = options.Namespace ?? "*";
            var subNs = options.IncludeSubNamespaces ? "1" : "0";
            var asm = options.Assembly?.GetName().Name ?? "*";
            var nameFilter = options.NameFilter ?? "*";
            var categories = (int)options.Categories;
            var excludeAbs = options.ExcludeAbstract ? "1" : "0";
            var excludeGen = options.ExcludeOpenGenerics ? "1" : "0";
            var requireCtor = options.RequireParameterlessConstructor ? "1" : "0";
            return $"ED:{scope}|{ns}|{subNs}|{asm}|{nameFilter}|{categories}|{excludeAbs}|{excludeGen}|{requireCtor}";
        }

        private async Task<List<DiscoveredEntity>> DiscoverCoreAsync(
            EntityDiscoveryOptions options,
            IProgress<int>? progress,
            CancellationToken token)
        {
            var assemblies = ResolveScopeAssemblies(options);
            var total = assemblies.Count;
            var processed = 0;

            var results = new ConcurrentBag<DiscoveredEntity>();

            Parallel.ForEach(assemblies, new ParallelOptions
            {
                CancellationToken = token,
                MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount)
            }, asm =>
            {
                token.ThrowIfCancellationRequested();
                var discovered = ScanAssembly(asm, options);
                foreach (var entity in discovered)
                    results.Add(entity);

                var count = Interlocked.Increment(ref processed);
                progress?.Report((count * 100) / total);
            });

            return results
                .GroupBy(e => e.FullName)
                .Select(g => g.First())
                .Where(e => options.IncludesCategory(e.Category))
                .Where(e => options.PassesNamespace(e))
                .Where(e => options.PassesFreeText(e))
                .OrderBy(e => e.Namespace, StringComparer.Ordinal)
                .ThenBy(e => e.Name, StringComparer.Ordinal)
                .ToList();
        }

        public IReadOnlyList<Assembly> ResolveScopeAssemblies(EntityDiscoveryOptions options)
        {
            if (options == null) options = new EntityDiscoveryOptions();

            switch (options.Scope)
            {
                case DiscoveryScope.Explicit:
                    return options.Assemblies
                        ?? (IReadOnlyList<Assembly>)Array.Empty<Assembly>();

                case DiscoveryScope.AllLoaded:
                    return EnumerateSearchableAssemblies().ToList();

                case DiscoveryScope.Project:
                default:
                    return _scopeStrategy.ResolveProjectScope();
            }
        }

        public List<DiscoveredEntity> DiscoverEntities(
            string namespaceName = null,
            Assembly assembly = null,
            bool includeSubNamespaces = true)
        {
            return Discover(new EntityDiscoveryOptions
            {
                Namespace = namespaceName,
                IncludeSubNamespaces = includeSubNamespaces,
                Assembly = assembly
            });
        }

        public List<DiscoveredEntity> DiscoverAllEntities(bool includeSubNamespaces = true)
        {
            return Discover(new EntityDiscoveryOptions
            {
                IncludeSubNamespaces = includeSubNamespaces
            });
        }

        public List<DiscoveredEntity> ScanAssemblyForEntities(
            Assembly assembly,
            bool includeSubNamespaces = true)
        {
            if (assembly == null) return new List<DiscoveredEntity>();
            return Discover(new EntityDiscoveryOptions
            {
                Assembly = assembly,
                IncludeSubNamespaces = includeSubNamespaces
            });
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

        private IEnumerable<Assembly> EnumerateSearchableAssemblies()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.IsDynamic) continue;
                var name = asm.GetName().Name;
                if (string.IsNullOrEmpty(name)) continue;
                if (_scopeStrategy.FrameworkPrefixes.Any(p => name.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                    continue;
                if (!seen.Add(name)) continue;
                yield return asm;
            }
        }

        private List<DiscoveredEntity> ScanAssembly(Assembly assembly, EntityDiscoveryOptions options)
        {
            if (assembly == null || assembly.IsDynamic) return new List<DiscoveredEntity>();

            Type[] types;
            try { types = assembly.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t != null).ToArray(); }
            catch { return new List<DiscoveredEntity>(); }

            var result = new List<DiscoveredEntity>();

            foreach (var type in types)
            {
                if (type == null) continue;
                if (!PassesBasicFilters(type, options)) continue;

                var category = ClassifyEntity(type);
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                result.Add(BuildEntity(type, properties, category));
            }

            return result;
        }

        private static DiscoveredEntity BuildEntity(
            Type type,
            PropertyInfo[] properties,
            EntityCategory category)
        {
            var scalarCount = 0;
            var navCount = 0;
            foreach (var p in properties)
            {
                if (IsNavigationProperty(p)) navCount++;
                else scalarCount++;
            }

            return new DiscoveredEntity
            {
                Name = type.Name,
                FullName = type.FullName ?? type.Name,
                AssemblyName = type.Assembly?.GetName().Name ?? string.Empty,
                PropertyCount = properties.Length,
                ScalarPropertyCount = scalarCount,
                NavigationPropertyCount = navCount,
                Category = category,
                Namespace = type.Namespace ?? string.Empty,
                HasParameterlessConstructor = type.GetConstructor(Type.EmptyTypes) != null,
                IsEfDecorated = category == EntityCategory.EfCore,
                IsBeepEntity = category == EntityCategory.Entity,
                PrimaryKeyNames = DetectPrimaryKeyNames(type, properties),
                IsAbstract = type.IsAbstract,
                IsGeneric = type.IsGenericType
            };
        }

        private EntityCategory ClassifyEntity(Type type)
        {
            if (IsBeepEntity(type)) return EntityCategory.Entity;
            if (_editor.classCreator?.IsEfDecoratedType(type) == true) return EntityCategory.EfCore;
            if (_editor.classCreator?.IsDiscoverablePoco(type) == true) return EntityCategory.Poco;
            return EntityCategory.Poco;
        }

        private static bool IsBeepEntity(Type type)
        {
            if (type == null) return false;
            if (typeof(IEntity).IsAssignableFrom(type)) return true;
            for (var t = type.BaseType; t != null; t = t.BaseType)
            {
                if (t.Name == "Entity") return true;
            }
            return false;
        }

        private static bool PassesBasicFilters(Type type, EntityDiscoveryOptions options)
        {
            if (type == null) return false;
            if (!type.IsClass) return false;
            if (type.IsInterface) return false;
            if (type.IsNotPublic && !type.IsNestedPublic) return false;
            if (type.IsNested && !type.IsNestedPublic) return false;
            if (options.ExcludeAbstract && (type.IsAbstract || type.IsInterface)) return false;
            if (options.ExcludeOpenGenerics && type.IsGenericTypeDefinition) return false;
            if (IsCompilerGenerated(type)) return false;
            if (options.RequireParameterlessConstructor && type.GetConstructor(Type.EmptyTypes) == null) return false;
            return true;
        }

        private static bool IsCompilerGenerated(Type type)
        {
            if (type == null) return false;
            if (type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))
                return true;
            var name = type.Name ?? string.Empty;
            if (name.StartsWith("<", StringComparison.Ordinal)) return true;
            if (name.Contains("<") && name.EndsWith(">")) return true;
            return false;
        }

        private static bool IsNavigationProperty(PropertyInfo p)
        {
            if (p == null) return false;
            var t = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
            if (!t.IsClass && !t.IsInterface) return false;
            if (t == typeof(string)) return false;
            if (t == typeof(object)) return false;
            return true;
        }

        private static string DetectPrimaryKeyNames(Type type, PropertyInfo[] properties)
        {
            var keyProps = properties
                .Where(p => p.GetCustomAttribute<KeyAttribute>(inherit: false) != null)
                .Select(p => p.Name)
                .ToList();
            if (keyProps.Count > 0) return string.Join(", ", keyProps);

            var convention = properties
                .Where(p => p.Name == "Id" || p.Name == type.Name + "Id")
                .Select(p => p.Name)
                .ToList();
            if (convention.Count > 0) return string.Join(", ", convention);

            if (typeof(IEntity).IsAssignableFrom(type)
                && !type.IsAbstract
                && type.GetConstructor(Type.EmptyTypes) != null)
            {
                var pkProp = type.GetProperty("PrimaryKeys",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (pkProp != null && pkProp.CanRead && !pkProp.GetMethod.IsAbstract)
                {
                    try
                    {
                        var instance = Activator.CreateInstance(type);
                        if (instance != null)
                        {
                            var val = pkProp.GetValue(instance);
                            if (val is System.Collections.IEnumerable list)
                            {
                                var names = new List<string>();
                                foreach (var item in list)
                                {
                                    if (item is EntityField f && !string.IsNullOrWhiteSpace(f.FieldName))
                                        names.Add(f.FieldName);
                                    else if (item is string s && !string.IsNullOrWhiteSpace(s))
                                        names.Add(s);
                                }
                                if (names.Count > 0) return string.Join(", ", names);
                            }
                        }
                    }
                    catch { }
                }
            }

            return string.Empty;
        }
    }
}
