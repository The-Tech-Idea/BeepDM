using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Services;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Container
{
    /// <summary>
    /// Desktop-specific options for BeepService configuration.
    /// Optimized for WinForms, WPF, and other desktop applications with singleton lifetime.
    /// </summary>
    public class DesktopBeepOptions
    {
        /// <summary>
        /// Gets or sets the directory path for Beep data storage.
        /// </summary>
        public string DirectoryPath { get; set; } = AppContext.BaseDirectory;

        /// <summary>
        /// Gets or sets the application repository/container name.
        /// </summary>
        public string AppRepoName { get; set; } = "DesktopApp";

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
        /// Gets or sets whether to enable progress reporting UI elements.
        /// </summary>
        public bool EnableProgressReporting { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable design-time support for Visual Studio designers.
        /// </summary>
        public bool EnableDesignTimeSupport { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to auto-initialize forms and controls on startup.
        /// </summary>
        public bool AutoInitializeForms { get; set; } = false;

        /// <summary>
        /// Gets or sets the initialization timeout.
        /// </summary>
        public TimeSpan InitializationTimeout { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Converts DesktopBeepOptions to BeepServiceOptions.
        /// </summary>
        internal BeepServiceOptions ToBeepServiceOptions()
        {
            return new BeepServiceOptions
            {
                DirectoryPath = DirectoryPath,
                AppRepoName = AppRepoName,
                ConfigType = ConfigType,
                ServiceLifetime = ServiceLifetime.Singleton, // Always singleton for desktop
                EnableAutoMapping = EnableAutoMapping,
                EnableAssemblyLoading = EnableAssemblyLoading,
                InitializationTimeout = InitializationTimeout,
                EnableConfigurationValidation = true,
                AdditionalProperties = new System.Collections.Generic.Dictionary<string, object>
                {
                    ["EnableProgressReporting"] = EnableProgressReporting,
                    ["EnableDesignTimeSupport"] = EnableDesignTimeSupport,
                    ["AutoInitializeForms"] = AutoInitializeForms
                }
            };
        }
    }

    /// <summary>
    /// Desktop-specific extension methods for BeepService registration.
    /// Optimized for WinForms, WPF, and other desktop application patterns.
    /// </summary>
    public static class DesktopBeepServiceExtensions
    {
        /// <summary>
        /// Registers BeepService with desktop-optimized defaults (singleton, progress reporting, design-time support).
        /// Use this method for WinForms, WPF, or any desktop application.
        /// </summary>
        /// <param name="services">The service collection to extend.</param>
        /// <param name="configure">Configuration action for desktop-specific options.</param>
        /// <returns>The configured IBeepService instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when services or configure is null.</exception>
        /// <example>
        /// <code>
        /// services.AddBeepForDesktop(opts => 
        /// {
        ///     opts.DirectoryPath = AppContext.BaseDirectory;
        ///     opts.AppRepoName = "MyDesktopApp";
        ///     opts.EnableProgressReporting = true;
        /// });
        /// </code>
        /// </example>
        public static IBeepService AddBeepForDesktop(this IServiceCollection services, 
            Action<DesktopBeepOptions> configure = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            var desktopOptions = new DesktopBeepOptions();
            configure?.Invoke(desktopOptions);

            var beepOptions = desktopOptions.ToBeepServiceOptions();
            return BeepServiceRegistration.RegisterBeepServicesInternal(services, beepOptions);
        }

        /// <summary>
        /// Registers BeepService for desktop with fluent builder API.
        /// Returns a builder interface that supports method chaining.
        /// </summary>
        /// <param name="services">The service collection to extend.</param>
        /// <returns>A fluent builder interface pre-configured for desktop scenarios.</returns>
        /// <example>
        /// <code>
        /// services.AddBeepForDesktop()
        ///     .InDirectory(AppContext.BaseDirectory)
        ///     .WithAppRepo("MyDesktopApp")
        ///     .WithProgressUI()
        ///     .Build();
        /// </code>
        /// </example>
        public static IDesktopBeepServiceBuilder AddBeepForDesktop(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            return new DesktopBeepServiceBuilder(services);
        }

        /// <summary>
        /// Configures and initializes BeepService for desktop applications in an IHost.
        /// This method combines service registration with assembly loading and progress reporting.
        /// </summary>
        /// <param name="host">The configured host instance.</param>
        /// <param name="progress">Optional progress reporter for assembly loading.</param>
        /// <returns>The configured IBeepService instance.</returns>
        /// <example>
        /// <code>
        /// var host = Host.CreateDefaultBuilder(args)
        ///     .ConfigureServices((context, services) => 
        ///         services.AddBeepForDesktop(opts => opts.DirectoryPath = basePath))
        ///     .Build();
        /// 
        /// var beepService = host.UseBeepForDesktop();
        /// Application.Run(mainForm);
        /// </code>
        /// </example>
        public static IBeepService UseBeepForDesktop(this IHost host, Progress<PassedArgs> progress = null)
        {
            if (host == null)
                throw new ArgumentNullException(nameof(host));

            var beepService = host.Services.GetRequiredService<IBeepService>();

            // Load assemblies if enabled
            if (progress != null)
            {
                beepService.LoadAssemblies(progress);
            }
            else
            {
                beepService.LoadAssemblies();
            }

            return beepService;
        }
    }

    /// <summary>
    /// Fluent builder interface for desktop-specific BeepService configuration.
    /// </summary>
    public interface IDesktopBeepServiceBuilder
    {
        /// <summary>
        /// Sets the directory path for Beep data storage.
        /// </summary>
        IDesktopBeepServiceBuilder InDirectory(string directoryPath);

        /// <summary>
        /// Sets the application repository/container name.
        /// </summary>
        IDesktopBeepServiceBuilder WithAppRepo(string appRepoName);

        /// <summary>
        /// Enables progress reporting UI elements during long operations.
        /// </summary>
        IDesktopBeepServiceBuilder WithProgressUI(bool enable = true);

        /// <summary>
        /// Enables design-time support for Visual Studio designers.
        /// </summary>
        IDesktopBeepServiceBuilder WithDesignTimeSupport(bool enable = true);

        /// <summary>
        /// Enables automatic form and control initialization on startup.
        /// </summary>
        IDesktopBeepServiceBuilder WithAutoInitialize(bool enable = true);

        /// <summary>
        /// Enables automatic mapping creation during initialization.
        /// </summary>
        IDesktopBeepServiceBuilder WithMapping(bool enable = true);

        /// <summary>
        /// Enables automatic assembly loading during initialization.
        /// </summary>
        IDesktopBeepServiceBuilder WithAssemblyLoading(bool enable = true);

        /// <summary>
        /// Builds and registers the BeepService with desktop-optimized configuration.
        /// </summary>
        IBeepService Build();
    }

   /// <summary>
    /// Implementation of the fluent builder for desktop-specific BeepService configuration.
    /// </summary>
    internal class DesktopBeepServiceBuilder : IDesktopBeepServiceBuilder
    {
        private readonly IServiceCollection _services;
        private readonly DesktopBeepOptions _options;

        public DesktopBeepServiceBuilder(IServiceCollection services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _options = new DesktopBeepOptions();
        }

        public IDesktopBeepServiceBuilder InDirectory(string directoryPath)
        {
            _options.DirectoryPath = directoryPath;
            return this;
        }

        public IDesktopBeepServiceBuilder WithAppRepo(string appRepoName)
        {
            _options.AppRepoName = appRepoName;
            return this;
        }

        public IDesktopBeepServiceBuilder WithProgressUI(bool enable = true)
        {
            _options.EnableProgressReporting = enable;
            return this;
        }

        public IDesktopBeepServiceBuilder WithDesignTimeSupport(bool enable = true)
        {
            _options.EnableDesignTimeSupport = enable;
            return this;
        }

        public IDesktopBeepServiceBuilder WithAutoInitialize(bool enable = true)
        {
            _options.AutoInitializeForms = enable;
            return this;
        }

        public IDesktopBeepServiceBuilder WithMapping(bool enable = true)
        {
            _options.EnableAutoMapping = enable;
            return this;
        }

        public IDesktopBeepServiceBuilder WithAssemblyLoading(bool enable = true)
        {
            _options.EnableAssemblyLoading = enable;
            return this;
        }

        public IBeepService Build()
        {
            var beepOptions = _options.ToBeepServiceOptions();
            return BeepServiceRegistration.RegisterBeepServicesInternal(_services, beepOptions);
        }
    }
}
