using Microsoft.Extensions.DependencyInjection;
using System;
using TheTechIdea.Beep.Services;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Container
{
    /// <summary>
    /// Blazor-specific options for BeepService configuration.
    /// Optimized for Blazor Server and Blazor WebAssembly applications.
    /// </summary>
    public class BlazorBeepOptions
    {
        /// <summary>
        /// Gets or sets the directory path for Beep data storage.
        /// </summary>
        public string DirectoryPath { get; set; } = System.IO.Path.Combine(AppContext.BaseDirectory, "Beep");

        /// <summary>
        /// Gets or sets the application repository/container name.
        /// </summary>
        public string AppRepoName { get; set; } = "BlazorApp";

        /// <summary>
        /// Gets or sets the configuration type.
        /// </summary>
        public BeepConfigType ConfigType { get; set; } = BeepConfigType.Application;

        /// <summary>
        /// Gets or sets whether to enable automatic mapping creation.
        /// </summary>
        public bool EnableAutoMapping { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable automatic assembly loading.
        /// </summary>
        public bool EnableAssemblyLoading { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable SignalR for real-time progress reporting.
        /// </summary>
        public bool EnableSignalRProgress { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to use browser storage for configuration (WebAssembly only).
        /// </summary>
        public bool UseBrowserStorage { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to enable circuit handlers (Blazor Server only).
        /// </summary>
        public bool EnableCircuitHandlers { get; set; } = false;

        /// <summary>
        /// Gets or sets the initialization timeout.
        /// </summary>
        public TimeSpan InitializationTimeout { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets the service lifetime (Scoped for Server, Singleton for WebAssembly).
        /// </summary>
        public ServiceLifetime ServiceLifetime { get; set; } = ServiceLifetime.Scoped;

        /// <summary>
        /// Converts BlazorBeepOptions to BeepServiceOptions.
        /// </summary>
        internal BeepServiceOptions ToBeepServiceOptions()
        {
            return new BeepServiceOptions
            {
                DirectoryPath = DirectoryPath,
                AppRepoName = AppRepoName,
                ConfigType = ConfigType,
                ServiceLifetime = ServiceLifetime,
                EnableAutoMapping = EnableAutoMapping,
                EnableAssemblyLoading = EnableAssemblyLoading,
                InitializationTimeout = InitializationTimeout,
                EnableConfigurationValidation = true,
                AdditionalProperties = new System.Collections.Generic.Dictionary<string, object>
                {
                    ["EnableSignalRProgress"] = EnableSignalRProgress,
                    ["UseBrowserStorage"] = UseBrowserStorage,
                    ["EnableCircuitHandlers"] = EnableCircuitHandlers
                }
            };
        }
    }

    /// <summary>
    /// Blazor-specific extension methods for BeepService registration.
    /// Optimized for Blazor Server and Blazor WebAssembly application patterns.
    /// </summary>
    public static class BlazorBeepServiceExtensions
    {
        /// <summary>
        /// Registers BeepService with Blazor Server optimized defaults (scoped lifetime, SignalR progress, circuit handlers).
        /// Use this method for Blazor Server applications.
        /// </summary>
        /// <param name="services">The service collection to extend.</param>
        /// <param name="configure">Configuration action for Blazor-specific options.</param>
        /// <returns>The service collection for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when services is null.</exception>
        /// <example>
        /// <code>
        /// services.AddBeepForBlazorServer(opts => 
        /// {
        ///     opts.DirectoryPath = Path.Combine(basePath, "Beep");
        ///     opts.AppRepoName = "MyBlazorApp";
        ///     opts.EnableSignalRProgress = true;
        ///     opts.EnableCircuitHandlers = true;
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddBeepForBlazorServer(this IServiceCollection services, 
            Action<BlazorBeepOptions> configure = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            var blazorOptions = new BlazorBeepOptions 
            { 
                ServiceLifetime = ServiceLifetime.Scoped, // Scoped for Blazor Server
                EnableSignalRProgress = true, // Enable by default for Server
                EnableCircuitHandlers = true  // Enable circuit handlers
            };
            configure?.Invoke(blazorOptions);

            var beepOptions = blazorOptions.ToBeepServiceOptions();
            BeepServiceRegistration.RegisterBeepServicesInternal(services, beepOptions);

            return services;
        }

        /// <summary>
        /// Registers BeepService with Blazor WebAssembly optimized defaults (singleton lifetime, browser storage).
        /// Use this method for Blazor WebAssembly applications.
        /// </summary>
        /// <param name="services">The service collection to extend.</param>
        /// <param name="configure">Configuration action for Blazor-specific options.</param>
        /// <returns>The service collection for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when services is null.</exception>
        /// <example>
        /// <code>
        /// services.AddBeepForBlazorWasm(opts => 
        /// {
        ///     opts.DirectoryPath = "Beep"; // Browser path
        ///     opts.AppRepoName = "MyBlazorWasmApp";
        ///     opts.UseBrowserStorage = true;
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddBeepForBlazorWasm(this IServiceCollection services, 
            Action<BlazorBeepOptions> configure = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            var blazorOptions = new BlazorBeepOptions 
            { 
                ServiceLifetime = ServiceLifetime.Singleton, // Singleton for Blazor WASM
                UseBrowserStorage = true, // Use browser storage by default
                EnableSignalRProgress = false // Can't use SignalR in WASM
            };
            configure?.Invoke(blazorOptions);

            var beepOptions = blazorOptions.ToBeepServiceOptions();
            BeepServiceRegistration.RegisterBeepServicesInternal(services, beepOptions);

            return services;
        }

        /// <summary>
        /// Registers BeepService for Blazor Server with fluent builder API.
        /// Returns a builder interface that supports method chaining.
        /// </summary>
        /// <param name="services">The service collection to extend.</param>
        /// <returns>A fluent builder interface pre-configured for Blazor Server scenarios.</returns>
        /// <example>
        /// <code>
        /// services.AddBeepForBlazorServer()
        ///     .InDirectory(basePath)
        ///     .WithAppRepo("MyBlazorApp")
        ///     .WithSignalR()
        ///     .WithCircuitHandlers()
        ///     .Build();
        /// </code>
        /// </example>
        public static IBlazorBeepServiceBuilder AddBeepForBlazorServer(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            return new BlazorBeepServiceBuilder(services, BlazorHostingModel.Server);
        }

        /// <summary>
        /// Registers BeepService for Blazor WebAssembly with fluent builder API.
        /// Returns a builder interface that supports method chaining.
        /// </summary>
        /// <param name="services">The service collection to extend.</param>
        /// <returns>A fluent builder interface pre-configured for Blazor WASM scenarios.</returns>
        /// <example>
        /// <code>
        /// services.AddBeepForBlazorWasm()
        ///     .InDirectory("Beep")
        ///     .WithAppRepo("MyBlazorWasmApp")
        ///     .WithBrowserStorage()
        ///     .Build();
        /// </code>
        /// </example>
        public static IBlazorBeepServiceBuilder AddBeepForBlazorWasm(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            return new BlazorBeepServiceBuilder(services, BlazorHostingModel.WebAssembly);
        }
    }

    /// <summary>
    /// Blazor hosting model enumeration.
    /// </summary>
    public enum BlazorHostingModel
    {
        /// <summary>
        /// Blazor Server (runs on server with SignalR).
        /// </summary>
        Server,

        /// <summary>
        /// Blazor WebAssembly (runs in browser).
        /// </summary>
        WebAssembly
    }

    /// <summary>
    /// Fluent builder interface for Blazor-specific BeepService configuration.
    /// </summary>
    public interface IBlazorBeepServiceBuilder
    {
        /// <summary>
        /// Sets the directory path for Beep data storage.
        /// </summary>
        IBlazorBeepServiceBuilder InDirectory(string directoryPath);

        /// <summary>
        /// Sets the application repository/container name.
        /// </summary>
        IBlazorBeepServiceBuilder WithAppRepo(string appRepoName);

        /// <summary>
        /// Enables SignalR for real-time progress reporting (Blazor Server only).
        /// </summary>
        IBlazorBeepServiceBuilder WithSignalR(bool enable = true);

        /// <summary>
        /// Enables browser storage for configuration (Blazor WebAssembly only).
        /// </summary>
        IBlazorBeepServiceBuilder WithBrowserStorage(bool enable = true);

        /// <summary>
        /// Enables circuit handlers (Blazor Server only).
        /// </summary>
        IBlazorBeepServiceBuilder WithCircuitHandlers(bool enable = true);

        /// <summary>
        /// Enables automatic mapping creation during initialization.
        /// </summary>
        IBlazorBeepServiceBuilder WithMapping(bool enable = true);

        /// <summary>
        /// Enables automatic assembly loading during initialization.
        /// </summary>
        IBlazorBeepServiceBuilder WithAssemblyLoading(bool enable = true);

        /// <summary>
        /// Builds and registers the BeepService with Blazor-optimized configuration.
        /// </summary>
        IServiceCollection Build();
    }

    /// <summary>
    /// Implementation of the fluent builder for Blazor-specific BeepService configuration.
    /// </summary>
    internal class BlazorBeepServiceBuilder : IBlazorBeepServiceBuilder
    {
        private readonly IServiceCollection _services;
        private readonly BlazorBeepOptions _options;
        private readonly BlazorHostingModel _hostingModel;

        public BlazorBeepServiceBuilder(IServiceCollection services, BlazorHostingModel hostingModel)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _hostingModel = hostingModel;
            _options = new BlazorBeepOptions
            {
                ServiceLifetime = hostingModel == BlazorHostingModel.Server 
                    ? ServiceLifetime.Scoped 
                    : ServiceLifetime.Singleton
            };
        }

        public IBlazorBeepServiceBuilder InDirectory(string directoryPath)
        {
            _options.DirectoryPath = directoryPath;
            return this;
        }

        public IBlazorBeepServiceBuilder WithAppRepo(string appRepoName)
        {
            _options.AppRepoName = appRepoName;
            return this;
        }

        public IBlazorBeepServiceBuilder WithSignalR(bool enable = true)
        {
            if (_hostingModel == BlazorHostingModel.WebAssembly && enable)
            {
                throw new InvalidOperationException("SignalR is not supported in Blazor WebAssembly. Use Blazor Server for SignalR support.");
            }
            _options.EnableSignalRProgress = enable;
            return this;
        }

        public IBlazorBeepServiceBuilder WithBrowserStorage(bool enable = true)
        {
            _options.UseBrowserStorage = enable;
            return this;
        }

        public IBlazorBeepServiceBuilder WithCircuitHandlers(bool enable = true)
        {
            if (_hostingModel == BlazorHostingModel.WebAssembly && enable)
            {
                throw new InvalidOperationException("Circuit handlers are not supported in Blazor WebAssembly. Use Blazor Server for circuit handler support.");
            }
            _options.EnableCircuitHandlers = enable;
            return this;
        }

        public IBlazorBeepServiceBuilder WithMapping(bool enable = true)
        {
            _options.EnableAutoMapping = enable;
            return this;
        }

        public IBlazorBeepServiceBuilder WithAssemblyLoading(bool enable = true)
        {
            _options.EnableAssemblyLoading = enable;
            return this;
        }

        public IServiceCollection Build()
        {
            var beepOptions = _options.ToBeepServiceOptions();
            BeepServiceRegistration.RegisterBeepServicesInternal(_services, beepOptions);
            return _services;
        }
    }
}
