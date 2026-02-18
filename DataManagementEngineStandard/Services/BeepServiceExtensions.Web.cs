using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using TheTechIdea.Beep.Services;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Container
{
    /// <summary>
    /// Web-specific options for BeepService configuration.
    /// Optimized for ASP.NET Core Web API and MVC applications with scoped lifetime.
    /// </summary>
    public class WebBeepOptions
    {
        /// <summary>
        /// Gets or sets the directory path for Beep data storage.
        /// </summary>
        public string DirectoryPath { get; set; } = System.IO.Path.Combine(AppContext.BaseDirectory, "Beep");

        /// <summary>
        /// Gets or sets the application repository/container name.
        /// </summary>
        public string AppRepoName { get; set; } = "WebApp";

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
        /// Gets or sets whether to enable request-level isolation (recommended for web apps).
        /// </summary>
        public bool EnableRequestIsolation { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable connection pooling for better performance.
        /// </summary>
        public bool EnableConnectionPooling { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable API endpoint discovery for BeepService operations.
        /// </summary>
        public bool EnableApiDiscovery { get; set; } = false;

        /// <summary>
        /// Gets or sets the initialization timeout.
        /// </summary>
        public TimeSpan InitializationTimeout { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Converts WebBeepOptions to BeepServiceOptions.
        /// </summary>
        internal BeepServiceOptions ToBeepServiceOptions()
        {
            return new BeepServiceOptions
            {
                DirectoryPath = DirectoryPath,
                AppRepoName = AppRepoName,
                ConfigType = ConfigType,
                ServiceLifetime = ServiceLifetime.Scoped, // Always scoped for web
                EnableAutoMapping = EnableAutoMapping,
                EnableAssemblyLoading = EnableAssemblyLoading,
                InitializationTimeout = InitializationTimeout,
                EnableConfigurationValidation = true,
                AdditionalProperties = new System.Collections.Generic.Dictionary<string, object>
                {
                    ["EnableRequestIsolation"] = EnableRequestIsolation,
                    ["EnableConnectionPooling"] = EnableConnectionPooling,
                    ["EnableApiDiscovery"] = EnableApiDiscovery
                }
            };
        }
    }

    /// <summary>
    /// Web-specific extension methods for BeepService registration.
    /// Optimized for ASP.NET Core Web API and MVC application patterns.
    /// </summary>
    public static class WebBeepServiceExtensions
    {
        /// <summary>
        /// Registers BeepService with web-optimized defaults (scoped lifetime, request isolation, connection pooling).
        /// Use this method for ASP.NET Core Web API or MVC applications.
        /// </summary>
        /// <param name="services">The service collection to extend.</param>
        /// <param name="configure">Configuration action for web-specific options.</param>
        /// <returns>The service collection for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when services is null.</exception>
        /// <example>
        /// <code>
        /// services.AddBeepForWeb(opts => 
        /// {
        ///     opts.DirectoryPath = Path.Combine(basePath, "Beep");
        ///     opts.AppRepoName = "MyWebApi";
        ///     opts.EnableConnectionPooling = true;
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddBeepForWeb(this IServiceCollection services, 
            Action<WebBeepOptions> configure = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            var webOptions = new WebBeepOptions();
            configure?.Invoke(webOptions);

            var beepOptions = webOptions.ToBeepServiceOptions();
            BeepServiceRegistration.RegisterBeepServicesInternal(services, beepOptions);

            return services;
        }

        /// <summary>
        /// Registers BeepService for web applications with fluent builder API.
        /// Returns a builder interface that supports method chaining.
        /// </summary>
        /// <param name="services">The service collection to extend.</param>
        /// <returns>A fluent builder interface pre-configured for web scenarios.</returns>
        /// <example>
        /// <code>
        /// services.AddBeepForWeb()
        ///     .InDirectory(Path.Combine(basePath, "Beep"))
        ///     .WithAppRepo("MyWebApi")
        ///     .WithConnectionPooling()
        ///     .Build();
        /// </code>
        /// </example>
        public static IWebBeepServiceBuilder AddBeepForWeb(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            return new WebBeepServiceBuilder(services);
        }

        /// <summary>
        /// Configures the middleware pipeline to use BeepService features.
        /// This method adds middleware for connection cleanup, health checks, and other web-specific features.
        /// </summary>
        /// <param name="app">The application builder to extend.</param>
        /// <returns>The application builder for method chaining.</returns>
        /// <example>
        /// <code>
        /// app.UseBeepForWeb();
        /// app.UseRouting();
        /// app.UseEndpoints(endpoints => endpoints.MapControllers());
        /// </code>
        /// </example>
        public static IApplicationBuilder UseBeepForWeb(this IApplicationBuilder app)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));

            // Add middleware for connection cleanup at end of request
            app.Use(async (context, next) =>
            {
                try
                {
                    await next();
                }
                finally
                {
                    // Cleanup scoped BeepService connections
                    var beepService = context.RequestServices.GetService<IBeepService>();
                    if (beepService != null && beepService.DMEEditor != null)
                    {
                        // Close any open connections at end of request
                        foreach (var ds in beepService.DMEEditor.DataSources)
                        {
                            try
                            {
                                ds.Closeconnection();
                            }
                            catch
                            {
                                // Ignore cleanup errors
                            }
                        }
                    }
                }
            });

            return app;
        }
    }

    /// <summary>
    /// Fluent builder interface for web-specific BeepService configuration.
    /// </summary>
    public interface IWebBeepServiceBuilder
    {
        /// <summary>
        /// Sets the directory path for Beep data storage.
        /// </summary>
        IWebBeepServiceBuilder InDirectory(string directoryPath);

        /// <summary>
        /// Sets the application repository/container name.
        /// </summary>
        IWebBeepServiceBuilder WithAppRepo(string appRepoName);

        /// <summary>
        /// Enables request-level isolation (recommended for thread safety).
        /// </summary>
        IWebBeepServiceBuilder WithRequestIsolation(bool enable = true);

        /// <summary>
        /// Enables connection pooling for improved performance.
        /// </summary>
        IWebBeepServiceBuilder WithConnectionPooling(bool enable = true);

        /// <summary>
        /// Enables API endpoint discovery for BeepService operations.
        /// </summary>
        IWebBeepServiceBuilder WithApiDiscovery(bool enable = true);

        /// <summary>
        /// Enables automatic mapping creation during initialization.
        /// </summary>
        IWebBeepServiceBuilder WithMapping(bool enable = true);

        /// <summary>
        /// Enables automatic assembly loading during initialization.
        /// </summary>
        IWebBeepServiceBuilder WithAssemblyLoading(bool enable = true);

        /// <summary>
        /// Builds and registers the BeepService with web-optimized configuration.
        /// </summary>
        IServiceCollection Build();
    }

    /// <summary>
    /// Implementation of the fluent builder for web-specific BeepService configuration.
    /// </summary>
    internal class WebBeepServiceBuilder : IWebBeepServiceBuilder
    {
        private readonly IServiceCollection _services;
        private readonly WebBeepOptions _options;

        public WebBeepServiceBuilder(IServiceCollection services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _options = new WebBeepOptions();
        }

        public IWebBeepServiceBuilder InDirectory(string directoryPath)
        {
            _options.DirectoryPath = directoryPath;
            return this;
        }

        public IWebBeepServiceBuilder WithAppRepo(string appRepoName)
        {
            _options.AppRepoName = appRepoName;
            return this;
        }

        public IWebBeepServiceBuilder WithRequestIsolation(bool enable = true)
        {
            _options.EnableRequestIsolation = enable;
            return this;
        }

        public IWebBeepServiceBuilder WithConnectionPooling(bool enable = true)
        {
            _options.EnableConnectionPooling = enable;
            return this;
        }

        public IWebBeepServiceBuilder WithApiDiscovery(bool enable = true)
        {
            _options.EnableApiDiscovery = enable;
            return this;
        }

        public IWebBeepServiceBuilder WithMapping(bool enable = true)
        {
            _options.EnableAutoMapping = enable;
            return this;
        }

        public IWebBeepServiceBuilder WithAssemblyLoading(bool enable = true)
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
