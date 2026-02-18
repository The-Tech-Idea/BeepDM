using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Tools;

namespace TheTechIdea.Beep.Tools.PluginSystem
{
    /// <summary>
    /// Plugin context implementation with dependency injection
    /// </summary>
    public class PluginContext : IPluginContext
    {
        public IServiceProvider Services { get; }
        public IConfigEditor Configuration { get; }
        public IDMLogger Logger { get; }
        public IAssemblyHandler AssemblyHandler { get; }
        public Dictionary<string, object> Properties { get; } = new();

        // New unified context properties
        public IUnifiedPluginManager PluginManager { get; }
        public string CurrentPluginId { get; }
        public UnifiedPluginType CurrentPluginType { get; }

        public PluginContext(IServiceProvider services, IConfigEditor configuration, 
                           IDMLogger logger, IAssemblyHandler assemblyHandler, IUnifiedPluginManager pluginManager = null, string currentPluginId = null, UnifiedPluginType currentPluginType = UnifiedPluginType.SharedAssembly)
        {
            Services = services;
            Configuration = configuration;
            Logger = logger;
            AssemblyHandler = assemblyHandler;
            PluginManager = pluginManager;
            CurrentPluginId = currentPluginId;
            CurrentPluginType = currentPluginType;
        }
    }

    /// <summary>
    /// Manages plugin services and dependency injection
    /// </summary>
    public class PluginServiceManager : IDisposable
    {
        private readonly ConcurrentDictionary<string, object> _services = new();
        private readonly ConcurrentDictionary<string, IServiceScope> _pluginServiceScopes = new();
        private readonly IServiceProvider _globalServiceProvider;
        private readonly IDMLogger _logger;
        private bool _disposed = false;

        public PluginServiceManager(IServiceProvider globalServiceProvider, IDMLogger logger)
        {
            _globalServiceProvider = globalServiceProvider;
            _logger = logger;
        }

        /// <summary>
        /// Registers a service for plugins
        /// </summary>
        public void RegisterPluginService<T>(T service) where T : class
        {
            if (service == null)
                return;

            try
            {
                var serviceKey = typeof(T).FullName;
                _services[serviceKey] = service;

                _logger?.LogWithContext($"Plugin service registered: {serviceKey}", null);
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to register plugin service: {typeof(T).FullName}", ex);
            }
        }

        /// <summary>
        /// Gets a service for plugins
        /// </summary>
        public T GetPluginService<T>() where T : class
        {
            try
            {
                var serviceKey = typeof(T).FullName;
                
                // Try to get from plugin services first
                if (_services.TryGetValue(serviceKey, out var service) && service is T typedService)
                {
                    return typedService;
                }

                // Fallback to global service provider
                try
                {
                    return _globalServiceProvider?.GetService<T>();
                }
                catch
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to get plugin service: {typeof(T).FullName}", ex);
                return null;
            }
        }

        /// <summary>
        /// Creates plugin context with dependency injection
        /// </summary>
        public IPluginContext CreatePluginContext(string pluginId, IServiceProvider services = null)
        {
            try
            {
                // Use provided services or create scoped services
                var contextServices = services ?? CreateScopedServices(pluginId);
                
                var configuration = GetPluginService<IConfigEditor>() ?? 
                                  contextServices?.GetService<IConfigEditor>();
                
                var logger = GetPluginService<IDMLogger>() ?? 
                           contextServices?.GetService<IDMLogger>();
                
                var assemblyHandler = GetPluginService<IAssemblyHandler>() ?? 
                                    contextServices?.GetService<IAssemblyHandler>();

                var context = new PluginContext(contextServices, configuration, logger, assemblyHandler);

                _logger?.LogWithContext($"Plugin context created: {pluginId}", null);
                return context;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to create plugin context: {pluginId}", ex);
                return null;
            }
        }

        /// <summary>
        /// Creates scoped services for a plugin
        /// </summary>
        public IServiceProvider CreateScopedServices(string pluginId)
        {
            try
            {
                if (_globalServiceProvider is IServiceScopeFactory scopeFactory)
                {
                    var scope = scopeFactory.CreateScope();
                    _pluginServiceScopes[pluginId] = scope;
                    return scope.ServiceProvider;
                }

                return _globalServiceProvider;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to create scoped services for plugin: {pluginId}", ex);
                return _globalServiceProvider;
            }
        }

        /// <summary>
        /// Disposes scoped services for a plugin
        /// </summary>
        public void DisposeScopedServices(string pluginId)
        {
            try
            {
                if (_pluginServiceScopes.TryRemove(pluginId, out var scope))
                {
                    scope.Dispose();
                    _logger?.LogWithContext($"Scoped services disposed for plugin: {pluginId}", null);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to dispose scoped services for plugin: {pluginId}", ex);
            }
        }

        /// <summary>
        /// Registers plugin dependency
        /// </summary>
        public void RegisterPluginDependency(string pluginId, string dependencyId)
        {
            try
            {
                var dependencyKey = $"{pluginId}:dependency:{dependencyId}";
                _services[dependencyKey] = dependencyId;

                _logger?.LogWithContext($"Plugin dependency registered: {pluginId} -> {dependencyId}", null);
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to register plugin dependency: {pluginId} -> {dependencyId}", ex);
            }
        }

        /// <summary>
        /// Gets plugin dependencies
        /// </summary>
        public List<string> GetPluginDependencies(string pluginId)
        {
            var dependencies = new List<string>();
            
            try
            {
                var dependencyPrefix = $"{pluginId}:dependency:";
                
                foreach (var kvp in _services)
                {
                    if (kvp.Key.StartsWith(dependencyPrefix) && kvp.Value is string dependency)
                    {
                        dependencies.Add(dependency);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to get plugin dependencies: {pluginId}", ex);
            }

            return dependencies;
        }

        /// <summary>
        /// Validates plugin dependencies
        /// </summary>
        public bool ValidatePluginDependencies(string pluginId)
        {
            try
            {
                var dependencies = GetPluginDependencies(pluginId);
                
                foreach (var dependency in dependencies)
                {
                    // Check if dependency service is available
                    var dependencyKey = dependency;
                    if (!_services.ContainsKey(dependencyKey))
                    {
                        _logger?.LogWithContext($"Missing dependency for plugin {pluginId}: {dependency}", null);
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to validate plugin dependencies: {pluginId}", ex);
                return false;
            }
        }

        /// <summary>
        /// Gets service registry information
        /// </summary>
        public Dictionary<string, object> GetServiceRegistry()
        {
            return new Dictionary<string, object>(_services);
        }

        /// <summary>
        /// Creates a service collection builder for plugins
        /// </summary>
        public IServiceCollection CreatePluginServiceCollection()
        {
            var services = new ServiceCollection();

            // Copy registered plugin services
            foreach (var kvp in _services)
            {
                if (kvp.Value != null)
                {
                    services.AddSingleton(kvp.Value.GetType(), kvp.Value);
                }
            }

            return services;
        }

        /// <summary>
        /// Registers common plugin services
        /// </summary>
        public void RegisterCommonPluginServices(IConfigEditor configEditor, IDMLogger logger, IAssemblyHandler assemblyHandler)
        {
            try
            {
                RegisterPluginService<IConfigEditor>(configEditor);
                RegisterPluginService<IDMLogger>(logger);
                RegisterPluginService<IAssemblyHandler>(assemblyHandler);

                _logger?.LogWithContext("Common plugin services registered", null);
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext("Failed to register common plugin services", ex);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // Dispose all scoped services
                foreach (var scope in _pluginServiceScopes.Values)
                {
                    try
                    {
                        scope.Dispose();
                    }
                    catch { } // Ignore disposal errors
                }

                _pluginServiceScopes.Clear();
                _services.Clear();
                _disposed = true;
            }
        }
    }
}