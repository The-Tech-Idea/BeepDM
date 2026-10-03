using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Container.Services;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Services;
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Container
{
    #region Fluent Builder API

    /// <summary>
    /// Fluent builder interface for configuring and registering BeepService with a discoverable API.
    /// Use this interface to chain configuration methods for better IntelliSense support.
    /// </summary>
    public interface IBeepServiceBuilder
    {
        /// <summary>
        /// Sets the directory path for Beep data storage.
        /// </summary>
        IBeepServiceBuilder WithDirectory(string directoryPath);

        /// <summary>
        /// Sets the application repository/container name.
        /// </summary>
        IBeepServiceBuilder WithAppRepo(string appRepoName);

        /// <summary>
        /// Sets the configuration type.
        /// </summary>
        IBeepServiceBuilder WithConfigType(BeepConfigType configType);

        /// <summary>
        /// Enables automatic mapping creation during initialization.
        /// </summary>
        IBeepServiceBuilder WithMapping(bool enable = true);

        /// <summary>
        /// Enables automatic assembly loading during initialization.
        /// </summary>
        IBeepServiceBuilder WithAssemblyLoading(bool enable = true);

        /// <summary>
        /// Sets the initialization timeout.
        /// </summary>
        IBeepServiceBuilder WithTimeout(TimeSpan timeout);

        /// <summary>
        /// Enables configuration validation during startup.
        /// </summary>
        IBeepServiceBuilder WithValidation(bool enable = true);

        /// <summary>
        /// Adds a custom configuration property.
        /// </summary>
        IBeepServiceBuilder WithProperty(string key, object value);

        /// <summary>
        /// Sets the assembly handler type to use.
        /// </summary>
        IBeepServiceBuilder WithAssemblyHandler(AssemblyHandlerType handlerType);

        /// <summary>
        /// Enables reflection-based discovery of <see cref="IDM_Addin"/> views and
        /// <see cref="IBeepViewModel"/> view models during <see cref="Build"/>.
        /// Discovered types are registered as keyed services keyed on their type name.
        /// Defaults to <c>true</c> when called with no argument.
        /// </summary>
        IBeepServiceBuilder WithViewDiscovery(bool enable = true);

        /// <summary>
        /// Constrains view / view-model discovery (see <see cref="WithViewDiscovery"/>)
        /// to the supplied set of assemblies. When not set, discovery inspects every
        /// relevant loaded assembly.
        /// </summary>
        IBeepServiceBuilder WithAssembliesToScan(IEnumerable<Assembly> assemblies);

        /// <summary>
        /// Registers BeepService as a singleton (recommended for desktop applications).
        /// </summary>
        IBeepServiceBuilder AsSingleton();

        /// <summary>
        /// Registers BeepService as scoped (recommended for web applications).
        /// </summary>
        IBeepServiceBuilder AsScoped();

        /// <summary>
        /// Registers BeepService as transient.
        /// </summary>
        IBeepServiceBuilder AsTransient();

        /// <summary>
        /// Builds and registers the BeepService with the configured options.
        /// </summary>
        /// <returns>The configured IBeepService instance.</returns>
        IBeepService Build();
    }

    /// <summary>
    /// Implementation of the fluent builder for BeepService configuration.
    /// </summary>
    internal class BeepServiceBuilder : IBeepServiceBuilder
    {
        private readonly IServiceCollection _services;
        private readonly BeepServiceOptions _options;

        public BeepServiceBuilder(IServiceCollection services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _options = new BeepServiceOptions();
        }

        // Fluent state for the add-in / view-model discovery hooks. Default to "enabled"
        // so the common case (Desktop host calling AddBeepServices().Build()) discovers
        // views/VMs without an extra call - matching the legacy RegisterServicesInternal
        // behaviour where EnableViewDiscovery defaulted to true.
        private bool _enableViewDiscovery = true;
        private IEnumerable<Assembly> _assembliesToScan;

        public IBeepServiceBuilder WithDirectory(string directoryPath)
        {
            _options.DirectoryPath = directoryPath;
            return this;
        }

        public IBeepServiceBuilder WithAppRepo(string appRepoName)
        {
            _options.AppRepoName = appRepoName;
            return this;
        }

        public IBeepServiceBuilder WithConfigType(BeepConfigType configType)
        {
            _options.ConfigType = configType;
            return this;
        }

        public IBeepServiceBuilder WithMapping(bool enable = true)
        {
            _options.EnableAutoMapping = enable;
            return this;
        }

        public IBeepServiceBuilder WithAssemblyLoading(bool enable = true)
        {
            _options.EnableAssemblyLoading = enable;
            return this;
        }

        public IBeepServiceBuilder WithTimeout(TimeSpan timeout)
        {
            _options.InitializationTimeout = timeout;
            return this;
        }

        public IBeepServiceBuilder WithValidation(bool enable = true)
        {
            _options.EnableConfigurationValidation = enable;
            return this;
        }

        public IBeepServiceBuilder WithProperty(string key, object value)
        {
            _options.AdditionalProperties[key] = value;
            return this;
        }

        public IBeepServiceBuilder WithAssemblyHandler(AssemblyHandlerType handlerType)
        {
            _options.AssemblyHandlerType = handlerType;
            return this;
        }

        public IBeepServiceBuilder WithViewDiscovery(bool enable = true)
        {
            _enableViewDiscovery = enable;
            return this;
        }

        public IBeepServiceBuilder WithAssembliesToScan(IEnumerable<Assembly> assemblies)
        {
            _assembliesToScan = assemblies;
            return this;
        }

        public IBeepServiceBuilder AsSingleton()
        {
            _options.ServiceLifetime = ServiceLifetime.Singleton;
            return this;
        }

        public IBeepServiceBuilder AsScoped()
        {
            _options.ServiceLifetime = ServiceLifetime.Scoped;
            return this;
        }

        public IBeepServiceBuilder AsTransient()
        {
            _options.ServiceLifetime = ServiceLifetime.Transient;
            return this;
        }

        public IBeepService Build()
        {
            var beepService = BeepServiceRegistration.RegisterBeepServicesInternal(_services, _options);

            // Load plugin assemblies BEFORE discovery so the reflection scan can see them.
            // Activates the previously-dormant EnableAssemblyLoading option. LoadAssemblies is
            // idempotent (guarded by BeepService.isassembliesloaded), so a later call from
            // BeepDesktopServices.StartLoading is a safe no-op.
            if (_options.EnableAssemblyLoading)
            {
                try
                {
                    beepService.LoadAssemblies();
                }
                catch (Exception ex)
                {
                    // Non-fatal: discovery will still scan whatever is already in the AppDomain.
                    System.Diagnostics.Debug.WriteLine($"[BeepServiceBuilder] Assembly loading failed: {ex.Message}");
                }
            }

            // Optional add-in / view-model discovery. Runs after the load above so folder-loaded
            // plugin add-ins (Addin / DataSources / ConnectionDriver) are discoverable here.
            if (_enableViewDiscovery)
            {
                // Prefer the AssemblyHandler's LoadedAssemblies when available - that is the
                // authoritative list of what just got loaded and is robust against isolated
                // AssemblyLoadContexts. Falls back to a full AppDomain scan otherwise.
                if (_assembliesToScan != null)
                {
                    _services.AddBeepViewModels(_assembliesToScan);
                    _services.AddBeepViews(_assembliesToScan);
                }
                else
                {
                    _services.AddBeepViewModels(beepService.LLoader);
                    _services.AddBeepViews(beepService.LLoader);
                }
            }

            return beepService;
        }
    }

    #endregion

    #region Validation Exceptions

    /// <summary>
    /// Exception thrown when BeepService configuration validation fails.
    /// </summary>
    public class BeepServiceValidationException : Exception
    {
        public string PropertyName { get; }
        public object InvalidValue { get; }

        public BeepServiceValidationException(string message) : base(message) { }

        public BeepServiceValidationException(string message, string propertyName, object invalidValue)
            : base(message)
        {
            PropertyName = propertyName;
            InvalidValue = invalidValue;
        }

        public BeepServiceValidationException(string message, Exception innerException)
            : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when BeepService runtime state is invalid.
    /// </summary>
    public class BeepServiceStateException : Exception
    {
        public string ComponentName { get; }

        public BeepServiceStateException(string message) : base(message) { }

        public BeepServiceStateException(string message, string componentName)
            : base(message)
        {
            ComponentName = componentName;
        }

        public BeepServiceStateException(string message, Exception innerException)
            : base(message, innerException) { }
    }

    #endregion

    /// <summary>
    /// Modern, thread-safe, and robust service registration extensions for Beep framework.
    /// Implements .NET 8/9 best practices with comprehensive error handling and validation.
    /// </summary>
    public static class BeepServiceRegistration
    {
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<IDMEEditor, RuntimeOwner> Owners = new();

        private sealed class RuntimeOwner
        {
            internal RuntimeOwner(IBeepService service, IServiceCollection services)
            {
                Service = new WeakReference<IBeepService>(service);
                Services = services;
            }
            internal WeakReference<IBeepService> Service { get; }
            internal IServiceCollection Services { get; }
        }

        private sealed class RuntimeRegistration
        {
            internal RuntimeRegistration(BeepServiceOptions options) => Options = options;
            internal BeepServiceOptions Options { get; }
        }

        public static IBeepServiceBuilder AddBeepServices(this IServiceCollection services) =>
            new BeepServiceBuilder(services ?? throw new ArgumentNullException(nameof(services)));

        /// <summary>
        /// Registers provider-owned runtimes without creating an eager startup instance.
        /// Each provider/scope owns the graph according to the captured lifetime.
        /// </summary>
        public static IServiceCollection AddBeepRuntime(this IServiceCollection services, Action<BeepServiceOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configure);
            var options = new BeepServiceOptions();
            configure(options);
            RegisterRuntime(services, options);
            return services;
        }

        /// <summary>
        /// Legacy eager entry point. The returned standalone runtime is caller-owned,
        /// independent of runtimes later resolved from DI. Prefer AddBeepRuntime for hosting.
        /// </summary>
        public static IBeepService AddBeepServices(this IServiceCollection services, Action<BeepServiceOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configure);
            var options = new BeepServiceOptions();
            configure(options);
            return RegisterBeepServicesInternal(services, options);
        }

        public static IBeepService Register(this IServiceCollection services, string directoryPath,
            string containerName, BeepConfigType configType, bool addAsSingleton = true) =>
            RegisterBeepServicesInternal(services, new BeepServiceOptions
            {
                DirectoryPath = directoryPath, AppRepoName = containerName, ConfigType = configType,
                ServiceLifetime = addAsSingleton ? ServiceLifetime.Singleton : ServiceLifetime.Scoped
            });

        public static IServiceCollection RegisterScoped(this IServiceCollection services) =>
            AddBeepRuntime(services, options =>
            {
                options.DirectoryPath = AppContext.BaseDirectory;
                options.ServiceLifetime = ServiceLifetime.Scoped;
                options.EnableAssemblyLoading = false;
            });

        public static IServiceCollection CreateMapping(this IBeepService beepService)
        {
            ArgumentNullException.ThrowIfNull(beepService);
            ArgumentNullException.ThrowIfNull(beepService.DMEEditor);
            EnvironmentService.AddAllConnectionConfigurations(beepService.DMEEditor);
            EnvironmentService.AddAllDataSourceMappings(beepService.DMEEditor);
            EnvironmentService.AddAllDataSourceQueryConfigurations(beepService.DMEEditor);
            return Owners.TryGetValue(beepService.DMEEditor, out var owner) ? owner.Services :
                (beepService as BeepService)?.Services ?? new ServiceCollection();
        }

        public static async Task CreateMappingAsync(this IBeepService beepService, IProgress<PassedArgs> progress = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(beepService);
            await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report(new PassedArgs { Messege = "Creating connection configurations...", EventType = "Progress" });
                EnvironmentService.AddAllConnectionConfigurations(beepService.DMEEditor);
                cancellationToken.ThrowIfCancellationRequested();
                EnvironmentService.AddAllDataSourceMappings(beepService.DMEEditor);
                cancellationToken.ThrowIfCancellationRequested();
                EnvironmentService.AddAllDataSourceQueryConfigurations(beepService.DMEEditor);
                progress?.Report(new PassedArgs { Messege = "Mapping creation completed successfully.", EventType = "Completed" });
            }, cancellationToken).ConfigureAwait(false);
        }

        public static string GetMainFolder() => EnvironmentService.CreateMainFolder();

        public static IBeepService GetBeepService(this IDMEEditor dmeEditor)
        {
            ArgumentNullException.ThrowIfNull(dmeEditor);
            if (Owners.TryGetValue(dmeEditor, out var owner) && owner.Service.TryGetTarget(out var service) &&
                ReferenceEquals(service.DMEEditor, dmeEditor))
                return service;
            throw new InvalidOperationException("This editor is not attached to a live Beep runtime. Resolve IBeepService from its provider.");
        }

        public static IBeepService GetBeepService(this IServiceProvider serviceProvider)
        {
            ArgumentNullException.ThrowIfNull(serviceProvider);
            return serviceProvider.GetRequiredService<IBeepService>();
        }

        [Obsolete("There is no process-wide runtime. Resolve IBeepService and use its ValidateConfiguration extension.")]
        public static bool ValidateConfiguration() => false;

        [Obsolete("There is no process-wide runtime. Resolve IBeepService and use its GetConfigurationSummary extension.")]
        public static string GetConfigurationSummary() => "No process-wide Beep runtime. Resolve IBeepService from its owning provider.";

        /// <summary>Compatibility no-op: registration state is now owned by each service collection.</summary>
        [Obsolete("Registration is collection-local. Create a new service collection instead of resetting process state.")]
        public static void ResetRegistrationState(bool force = false) { }

        internal static IBeepService RegisterBeepServicesInternal(IServiceCollection services, BeepServiceOptions options)
        {
            ArgumentNullException.ThrowIfNull(services);
            var snapshot = Snapshot(options);
            lock (services)
            {
                EnsureUnregistered(services);
                // The public legacy return signature requires an eager usable instance.
                // Never capture it in DI factories: its lifetime belongs to the caller.
                var standalone = CreateRuntime(services, snapshot);
                try { RegisterRuntime(services, snapshot); }
                catch { standalone.Dispose(); throw; }
                return standalone;
            }
        }

        internal static bool HasRuntimeRegistration(IServiceCollection services) =>
            services.Any(descriptor => descriptor.ServiceType == typeof(RuntimeRegistration));

        private static void EnsureUnregistered(IServiceCollection services)
        {
            if (services.IsReadOnly)
                throw new InvalidOperationException("Beep must be registered before building the service provider.");
            if (services.Any(descriptor => descriptor.ServiceType == typeof(IBeepService)))
                throw new InvalidOperationException("This collection already contains IBeepService. Use a separate collection for another runtime configuration.");
        }

        private static void RegisterRuntime(IServiceCollection services, BeepServiceOptions options)
        {
            var snapshot = Snapshot(options);
            lock (services)
            {
                EnsureUnregistered(services);
                services.AddSingleton(new RuntimeRegistration(snapshot));
                services.Add(new ServiceDescriptor(typeof(IBeepService),
                    _ => CreateRuntime(services, snapshot), snapshot.ServiceLifetime));
                AddComponent(services, snapshot.ServiceLifetime, "Editor", runtime => runtime.DMEEditor);
                AddComponent(services, snapshot.ServiceLifetime, "ConfigEditor", runtime => runtime.Config_editor);
                AddComponent(services, snapshot.ServiceLifetime, "Logger", runtime => runtime.lg);
                AddComponent(services, snapshot.ServiceLifetime, "Util", runtime => runtime.util);
                AddComponent(services, snapshot.ServiceLifetime, "JsonLoader", runtime => runtime.jsonLoader);
                AddComponent(services, snapshot.ServiceLifetime, "AssemblyHandler", runtime => runtime.LLoader);
                AddComponent(services, snapshot.ServiceLifetime, "Errors", runtime => runtime.Erinfo);
            }
        }

        private static void AddComponent<T>(IServiceCollection services, ServiceLifetime lifetime,
            string key, Func<IBeepService, T> select) where T : class
        {
            if (!services.Any(descriptor => !descriptor.IsKeyedService && descriptor.ServiceType == typeof(T)))
                services.Add(new ServiceDescriptor(typeof(T), provider => select(provider.GetRequiredService<IBeepService>()), lifetime));
            if (!services.Any(descriptor => descriptor.IsKeyedService && descriptor.ServiceType == typeof(T) && Equals(descriptor.ServiceKey, key)))
                services.Add(new ServiceDescriptor(typeof(T), key, (provider, _) => select(provider.GetRequiredService<IBeepService>()), lifetime));
        }

        private static BeepServiceOptions Snapshot(BeepServiceOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            options.Validate();
            return new BeepServiceOptions
            {
                DirectoryPath = Path.GetFullPath(options.DirectoryPath), AppRepoName = options.AppRepoName,
                ConfigType = options.ConfigType, ServiceLifetime = options.ServiceLifetime,
                EnableAutoMapping = options.EnableAutoMapping, EnableAssemblyLoading = options.EnableAssemblyLoading,
                EnableConfigurationValidation = options.EnableConfigurationValidation,
                InitializationTimeout = options.InitializationTimeout, AssemblyHandlerType = options.AssemblyHandlerType,
                ConnectionSecretProtector = options.ConnectionSecretProtector,
                AdditionalProperties = new Dictionary<string, object>(options.AdditionalProperties ?? new Dictionary<string, object>())
            };
        }

        private static BeepService CreateRuntime(IServiceCollection services, BeepServiceOptions options)
        {
            var runtime = new BeepService(services)
            {
                RegisterComponentsOnConfigure = false, LoadDefaultMappings = options.EnableAutoMapping,
                AssemblyHandlerType = options.AssemblyHandlerType,
                ConnectionSecretProtector = options.ConnectionSecretProtector ?? TheTechIdea.Beep.Security.ConnectionCredentialProtection.Default
            };
            try
            {
                runtime.Configure(options.DirectoryPath, options.AppRepoName, options.ConfigType,
                    options.ServiceLifetime == ServiceLifetime.Singleton);
                if (options.EnableAssemblyLoading) runtime.LoadAssemblies();
                if (options.EnableConfigurationValidation && !runtime.ValidateConfiguration())
                    throw new BeepServiceStateException("Runtime configuration is incomplete.");
                Owners.Add(runtime.DMEEditor, new RuntimeOwner(runtime, services));
                return runtime;
            }
            catch
            {
                runtime.Dispose();
                throw;
            }
        }
    }

    #region Configuration Classes

    /// <summary>
    /// Configuration options for Beep services with comprehensive validation.
    /// </summary>
    public class BeepServiceOptions
    {
        /// <summary>Optional host-owned credential policy captured by this runtime, not a process-global setting.</summary>
        public IConnectionSecretProtector ConnectionSecretProtector { get; set; }
        /// <summary>
        /// Gets or sets the directory path for Beep data storage.
        /// </summary>
        public string DirectoryPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the container.
        /// </summary>
        public string AppRepoName { get; set; } = "DefaultContainer";

        /// <summary>
        /// Gets or sets the configuration type.
        /// </summary>
        public BeepConfigType ConfigType { get; set; } = BeepConfigType.Application;

        /// <summary>
        /// Gets or sets the service lifetime for dependency injection.
        /// </summary>
        public ServiceLifetime ServiceLifetime { get; set; } = ServiceLifetime.Singleton;

        /// <summary>
        /// Gets or sets whether to automatically create mappings during initialization.
        /// </summary>
        public bool EnableAutoMapping { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to automatically load assemblies during initialization.
        /// </summary>
        public bool EnableAssemblyLoading { get; set; } = true;

        /// <summary>
        /// Gets or sets the timeout for initialization operations.
        /// </summary>
        public TimeSpan InitializationTimeout { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets whether to validate configuration during startup.
        /// </summary>
        public bool EnableConfigurationValidation { get; set; } = true;

        /// <summary>
        /// Gets or sets additional configuration properties for extensibility.
        /// </summary>
        public Dictionary<string, object> AdditionalProperties { get; set; } = new();

        /// <summary>
        /// Gets or sets the type of assembly handler to use.
        /// Default is the standard AssemblyHandler. SharedContext enables the plugin system with enhanced isolation.
        /// </summary>
        public AssemblyHandlerType AssemblyHandlerType { get; set; } = AssemblyHandlerType.Default;

        /// <summary>
        /// Validates the configuration options and throws descriptive exceptions for invalid configurations.
        /// </summary>
        /// <exception cref="BeepServiceValidationException">Thrown when configuration is invalid.</exception>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(DirectoryPath))
                throw new BeepServiceValidationException(
                    "DirectoryPath cannot be null or empty. Please specify a valid directory path for Beep data storage.",
                    nameof(DirectoryPath),
                    DirectoryPath);

            if (string.IsNullOrWhiteSpace(AppRepoName))
                throw new BeepServiceValidationException(
                    "AppRepoName cannot be null or empty. Please specify a name for the application repository/container.",
                    nameof(AppRepoName),
                    AppRepoName);

            if (AppRepoName is "." or ".." || Path.IsPathRooted(AppRepoName) ||
                AppRepoName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                AppRepoName.Contains('/') || AppRepoName.Contains('\\'))
                throw new BeepServiceValidationException("AppRepoName must be a single valid folder name.", nameof(AppRepoName), AppRepoName);

            if (!Enum.IsDefined(typeof(AssemblyHandlerType), AssemblyHandlerType))
                throw new BeepServiceValidationException("Invalid assembly handler type.", nameof(AssemblyHandlerType), AssemblyHandlerType);

            if (InitializationTimeout <= TimeSpan.Zero)
                throw new BeepServiceValidationException(
                    $"InitializationTimeout must be positive. Current value: {InitializationTimeout}",
                    nameof(InitializationTimeout),
                    InitializationTimeout);

            if (!Enum.IsDefined(typeof(BeepConfigType), ConfigType))
                throw new BeepServiceValidationException(
                    $"Invalid ConfigType value: {ConfigType}. Valid values are: {string.Join(", ", Enum.GetNames(typeof(BeepConfigType)))}",
                    nameof(ConfigType),
                    ConfigType);

            if (!Enum.IsDefined(typeof(ServiceLifetime), ServiceLifetime))
                throw new BeepServiceValidationException(
                    $"Invalid ServiceLifetime value: {ServiceLifetime}. Valid values are: Singleton, Scoped, Transient",
                    nameof(ServiceLifetime),
                    ServiceLifetime);

            // Validate directory path is accessible (if it exists) or can be created
            try
            {
                var fullPath = Path.GetFullPath(DirectoryPath);
                if (!Directory.Exists(fullPath))
                {
                    // Test if we can create it - just validate the path format
                    var testDir = Path.Combine(fullPath, ".beep_test");
                }
            }
            catch (Exception ex)
            {
                throw new BeepServiceValidationException(
                    $"DirectoryPath '{DirectoryPath}' is invalid or inaccessible: {ex.Message}",
                    nameof(DirectoryPath),
                    DirectoryPath);
            }
        }
    }

    #endregion

    #region Extension Methods

    /// <summary>
    /// Extension methods for IBeepService to provide additional functionality.
    /// </summary>
    public static class BeepServiceExtensions
    {
        /// <summary>
        /// Validates the BeepService configuration.
        /// </summary>
        /// <param name="beepService">The BeepService instance.</param>
        /// <returns>True if configuration is valid; otherwise, false.</returns>
        public static bool ValidateConfiguration(this IBeepService beepService)
        {
            if (beepService == null) return false;

            return !string.IsNullOrWhiteSpace(beepService.AppRepoName) &&
                   !string.IsNullOrWhiteSpace(beepService.BeepDirectory) &&
                   beepService.Config_editor != null;
        }

        /// <summary>
        /// Gets configuration summary for the BeepService.
        /// </summary>
        /// <param name="beepService">The BeepService instance.</param>
        /// <returns>Configuration summary as a formatted string.</returns>
        public static string GetConfigurationSummary(this IBeepService beepService)
        {
            if (beepService == null) return "BeepService is null";

            return $"Container: {beepService.AppRepoName}, " +
                   $"Type: {beepService.ConfigureationType}, " +
                   $"Directory: {beepService.BeepDirectory}";
        }
    }

    #endregion

    #region Legacy Compatibility

    /// <summary>
    /// Legacy RegisterBeep class for backward compatibility.
    /// This class maintains the exact same interface as the original but delegates to the new implementation.
    /// </summary>
    public static class RegisterBeep
    {
        private static IServiceCollection _services;

        /// <summary>
        /// Gets the current service collection for backward compatibility.
        /// </summary>
        public static IServiceCollection Services 
        { 
            get => _services ?? new ServiceCollection();
            private set => _services = value;
        }

        /// <summary>
        /// Registers Beep services with the original interface for backward compatibility.
        /// </summary>
        public static IBeepService Register(this IServiceCollection services, string directorypath, string containername, BeepConfigType configType, bool AddasSingleton = true)
        {
            Services = services;
            var beepService = BeepServiceRegistration.Register(services, directorypath, containername, configType, AddasSingleton);
            BeepServiceRegistration.CreateMapping(beepService);
            return beepService;
        }

        /// <summary>
        /// Registers scoped services for backward compatibility.
        /// </summary>
        public static IServiceCollection RegisterScoped(this IServiceCollection services)
        {
            Services = services;
            return BeepServiceRegistration.RegisterScoped(services);
        }

        /// <summary>
        /// Creates mappings for backward compatibility.
        /// </summary>
        public static IServiceCollection CreateMapping(this IBeepService beepService)
        {
            return BeepServiceRegistration.CreateMapping(beepService);
        }

        /// <summary>
        /// Gets the main folder for backward compatibility.
        /// </summary>
        public static string GetMainFolder()
        {
            return BeepServiceRegistration.GetMainFolder();
        }

        /// <summary>
        /// Gets the Beep service for backward compatibility.
        /// </summary>
        public static IBeepService GetBeepService(this IDMEEditor dmeEditor)
        {
            return BeepServiceRegistration.GetBeepService(dmeEditor);
        }
    }

    #endregion
}
