//using Microsoft.Extensions.DependencyInjection;

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using TheTechIdea.Beep;
//using TheTechIdea.Beep.ConfigUtil;
//using TheTechIdea.Beep.Editor;
//using TheTechIdea.Beep.Logger;
//using TheTechIdea.Beep.Tools;
//using TheTechIdea.Beep.Utilities;
//using TheTechIdea.Beep.Utils;
//using TheTechIdea.Beep.DataBase;
//using TheTechIdea.Beep.Editor.ETL;
//using TheTechIdea.Beep.Helpers;

//using TheTechIdea.Beep.Editor.UOW;

//using TheTechIdea.Beep.Tools.Helpers;
//using TheTechIdea.Beep.Editor.UOWManager;
//using TheTechIdea.Beep.Editor.Mapping;
//using TheTechIdea.Beep.Editor.Mapping.Interfaces;

//namespace TheTechIdea.Beep.Exensions
//{
//    /// <summary>
//    /// Extension methods for registering Beep services with the dependency injection container
//    /// </summary>
//    public static class BeepRegisterServices
//    {
//        /// <summary>
//        /// Registers all core Beep services as singletons for dependency injection.
//        /// This is the main registration method that configures the entire Beep framework.
//        /// </summary>
//        /// <param name="services">The service collection to register services with</param>
//        /// <param name="configPath">Optional configuration path. If null, uses default location</param>
//        /// <param name="containerName">Optional container name for multi-tenant scenarios</param>
//        /// <param name="configType">The type of configuration (Application, DataConnector, etc.)</param>
//        /// <returns>The service collection for method chaining</returns>
//        public static IServiceCollection RegisterBeepServices(
//            this IServiceCollection services,
//            string configPath = null,
//            string containerName = null,
//            BeepConfigType configType = BeepConfigType.Application)
//        {
//            // Register Microsoft Logging first
//            services.AddLogging(builder =>
//            {
//                builder.AddConsole();
//                builder.AddDebug();
//                builder.SetMinimumLevel(LogLevel.Information);
//            });

//            // Register core infrastructure services
//            RegisterInfrastructureServices(services, configPath, containerName, configType);

//            // Register utility services
//            RegisterUtilityServices(services);

//            // Register editor and data management services
//            RegisterEditorServices(services);

//            // Register tool services
//            RegisterToolServices(services);

//            // Register helper services
//            RegisterHelperServices(services);

//            // Register forms and UI services (if applicable)
//            RegisterFormsServices(services);

//            // Register Unit of Work services
//            RegisterUnitOfWorkServices(services);

//            // Register mapping services
//            RegisterMappingServices(services);

//            return services;
//        }

//        /// <summary>
//        /// Registers just the core editor service as singleton for minimal dependency injection setup.
//        /// This method provides a lightweight registration for scenarios that only need the editor.
//        /// </summary>
//        /// <param name="services">The service collection to register the editor with</param>
//        /// <param name="configPath">Optional configuration path</param>
//        /// <param name="containerName">Optional container name</param>
//        /// <param name="configType">The type of configuration</param>
//        /// <returns>The service collection for method chaining</returns>
//        public static IServiceCollection RegisterEditorService(
//            this IServiceCollection services,
//            string configPath = null,
//            string containerName = null,
//            BeepConfigType configType = BeepConfigType.Application)
//        {
//            // Register only essential services needed for the editor
//            RegisterInfrastructureServices(services, configPath, containerName, configType);
//            RegisterUtilityServices(services);

//            // Register the main editor
//            services.AddSingleton<IDMEEditor, DMEEditor>(provider =>
//            {
//                var logger = provider.GetRequiredService<IDMLogger>();
//                var util = provider.GetRequiredService<IUtil>();
//                var errorObject = provider.GetRequiredService<IErrorsInfo>();
//                var configEditor = provider.GetRequiredService<IConfigEditor>();
//                var assemblyHandler = provider.GetRequiredService<IAssemblyHandler>();

//                return new DMEEditor(logger, util, errorObject, configEditor, assemblyHandler);
//            });

//            return services;
//        }

//        /// <summary>
//        /// Registers infrastructure services including logging, configuration, and assembly handling
//        /// </summary>
//        private static void RegisterInfrastructureServices(
//            IServiceCollection services,
//            string configPath,
//            string containerName,
//            BeepConfigType configType)
//        {
//            // Register error handling
//            services.AddSingleton<IErrorsInfo, ErrorsInfo>();

//            // Register Beep logger (wraps Microsoft.Extensions.Logging and Serilog)
//            services.AddSingleton<IDMLogger, DMLogger>();

//            // Register JSON loader with a factory pattern
//            services.AddSingleton<IJsonLoader>(provider =>
//            {
//                var logger = provider.GetRequiredService<IDMLogger>();
//                var errorObject = provider.GetRequiredService<IErrorsInfo>();
//                return new JsonLoader(logger, errorObject);
//            });

//            // Register configuration editor with proper initialization
//            services.AddSingleton<IConfigEditor>(provider =>
//            {
//                var logger = provider.GetRequiredService<IDMLogger>();
//                var errorObject = provider.GetRequiredService<IErrorsInfo>();
//                var jsonLoader = provider.GetRequiredService<IJsonLoader>();

//                return new ConfigEditor(logger, errorObject, jsonLoader, configPath, containerName, configType);
//            });

//            // Register assembly handler
//            services.AddSingleton<IAssemblyHandler>(provider =>
//            {
//                var configEditor = provider.GetRequiredService<IConfigEditor>();
//                var errorObject = provider.GetRequiredService<IErrorsInfo>();
//                var logger = provider.GetRequiredService<IDMLogger>();
//                var util = provider.GetRequiredService<IUtil>();

//                return new AssemblyHandler(configEditor, errorObject, logger, util);
//            });
//        }

//        /// <summary>
//        /// Registers utility services including data type helpers and general utilities
//        /// </summary>
//        private static void RegisterUtilityServices(IServiceCollection services)
//        {
//            // Register utility functions
//            services.AddSingleton<IUtil>(provider =>
//            {
//                var logger = provider.GetRequiredService<IDMLogger>();
//                var errorObject = provider.GetRequiredService<IErrorsInfo>();
//                var configEditor = provider.GetRequiredService<IConfigEditor>();

//                return new Util(logger, errorObject, configEditor);
//            });

//            // Register data types helper
//            services.AddSingleton<IDataTypesHelper>(provider =>
//            {
//                var dmeEditor = provider.GetRequiredService<IDMEEditor>();
//                return new DataTypesHelper(dmeEditor);
//            });
//        }

//        /// <summary>
//        /// Registers editor and data management services
//        /// </summary>
//        private static void RegisterEditorServices(IServiceCollection services)
//        {
//            // Register the main DME Editor
//            services.AddSingleton<IDMEEditor, DMEEditor>(provider =>
//            {
//                var logger = provider.GetRequiredService<IDMLogger>();
//                var util = provider.GetRequiredService<IUtil>();
//                var errorObject = provider.GetRequiredService<IErrorsInfo>();
//                var configEditor = provider.GetRequiredService<IConfigEditor>();
//                var assemblyHandler = provider.GetRequiredService<IAssemblyHandler>();

//                return new DMEEditor(logger, util, errorObject, configEditor, assemblyHandler);
//            });

//            // Register alternative lightweight editor
//            services.AddSingleton<DMEEditorHelpers>();

//            // Register ETL services
//            services.AddSingleton<IETL>(provider =>
//            {
//                var dmeEditor = provider.GetRequiredService<IDMEEditor>();
//                return new ETLEditor(dmeEditor);
//            });

//            // Register connection helper
//            services.AddSingleton<IConnectionHelper>(provider =>
//            {
//                var dmeEditor = provider.GetRequiredService<IDMEEditor>();
//                return new ConnectionHelper(dmeEditor);
//            });

//            // Register data source lifecycle helper
//            services.AddSingleton<IDataSourceLifecycleHelper, DataSourceLifecycleHelper>();

//            // Register validation helper
//            services.AddSingleton<IValidationHelper, ValidationHelper>();

//            // Register error handling helper
//            services.AddSingleton<IErrorHandlingHelper, ErrorHandlingHelper>();
//        }

//        /// <summary>
//        /// Registers tool services including class creators and generators
//        /// </summary>
//        private static void RegisterToolServices(IServiceCollection services)
//        {
//            // Register class creator
//            services.AddSingleton<IClassCreator>(provider =>
//            {
//                var dmeEditor = provider.GetRequiredService<IDMEEditor>();
//                return new ClassCreator(dmeEditor);
//            });

//            // Register class generation helpers
//            services.AddSingleton<ClassGenerationHelper>(provider =>
//            {
//                var dmeEditor = provider.GetRequiredService<IDMEEditor>();
//                return new ClassGenerationHelper(dmeEditor);
//            });

//            services.AddSingleton<PocoClassGeneratorHelper>(provider =>
//            {
//                var dmeEditor = provider.GetRequiredService<IDMEEditor>();
//                return new PocoClassGeneratorHelper(dmeEditor);
//            });

//            services.AddSingleton<ModernClassGeneratorHelper>(provider =>
//            {
//                var dmeEditor = provider.GetRequiredService<IDMEEditor>();
//                return new ModernClassGeneratorHelper(dmeEditor);
//            });

//            services.AddSingleton<DatabaseClassGeneratorHelper>(provider =>
//            {
//                var dmeEditor = provider.GetRequiredService<IDMEEditor>();
//                return new DatabaseClassGeneratorHelper(dmeEditor);
//            });

//            services.AddSingleton<WebApiGeneratorHelper>(provider =>
//            {
//                var dmeEditor = provider.GetRequiredService<IDMEEditor>();
//                return new WebApiGeneratorHelper(dmeEditor);
//            });

//            // Register web API generator
//            services.AddSingleton<WebApiGenerator>(provider =>
//            {
//                var dmeEditor = provider.GetRequiredService<IDMEEditor>();
//                return new WebApiGenerator(dmeEditor);
//            });
//        }

//        /// <summary>
//        /// Registers helper services for various operations
//        /// </summary>
//        private static void RegisterHelperServices(IServiceCollection services)
//        {
//            // Register file and folder helpers
//            services.AddSingleton<FileDataSourceHelper>();
//            services.AddSingleton<ReflectionHelper>();

//            // Register batch extensions
//            services.AddSingleton<BatchExtensions>();

//            // Register data management helpers
//            services.AddSingleton<DataSourceLifecycleHelper>();
//            services.AddSingleton<ValidationHelper>();
//            services.AddSingleton<ErrorHandlingHelper>();
//            services.AddSingleton<CacheManager>();
//        }

//        /// <summary>
//        /// Registers forms and UI management services
//        /// </summary>
//        private static void RegisterFormsServices(IServiceCollection services)
//        {
//            // Register forms manager and its extensions
//            services.AddSingleton<FormsManager>(provider =>
//            {
//                var dmeEditor = provider.GetRequiredService<IDMEEditor>();
//                return new FormsManager(dmeEditor);
//            });

//            // Register forms navigation and operations
//            services.AddSingleton<IFormsManager>(provider =>
//                provider.GetRequiredService<FormsManager>());
//        }

//        /// <summary>
//        /// Registers Unit of Work pattern services
//        /// </summary>
//        private static void RegisterUnitOfWorkServices(IServiceCollection services)
//        {
//            // Register UnitOfWork factory
//            services.AddSingleton<UnitOfWorkFactory>(provider =>
//            {
//                var dmeEditor = provider.GetRequiredService<IDMEEditor>();
//                return new UnitOfWorkFactory(dmeEditor);
//            });

//            // Register UnitOfWork wrapper factory
//            services.AddSingleton<IUnitOfWorkWrapperFactory>(provider =>
//            {
//                var dmeEditor = provider.GetRequiredService<IDMEEditor>();
//                return new UnitOfWorkWrapperFactory(dmeEditor);
//            });

//            // Register scoped UnitOfWork instances (these should be scoped, not singleton)
//            services.AddScoped<IUnitOfWork>(provider =>
//            {
//                var factory = provider.GetRequiredService<UnitOfWorkFactory>();
//                return factory.CreateUnitOfWork();
//            });

//            services.AddScoped<IUnitOfWorkWrapper>(provider =>
//            {
//                var factory = provider.GetRequiredService<IUnitOfWorkWrapperFactory>();
//                return factory.CreateWrapper();
//            });
//        }

//        /// <summary>
//        /// Registers mapping services for object and data mapping
//        /// </summary>
//        private static void RegisterMappingServices(IServiceCollection services)
//        {
//            // Register auto object mapper
//            services.AddSingleton<AutoObjMapper>(provider =>
//            {
//                var dmeEditor = provider.GetRequiredService<IDMEEditor>();
//                return new AutoObjMapper(dmeEditor);
//            });

//            // Register mapping services as needed
//            services.AddSingleton<IAutoObjMapper>(provider =>
//                provider.GetRequiredService<AutoObjMapper>());
//        }

//        /// <summary>
//        /// Registers Beep services with custom configuration options
//        /// </summary>
//        /// <param name="services">The service collection</param>
//        /// <param name="configure">Configuration action for advanced setup</param>
//        /// <returns>The service collection for method chaining</returns>
//        public static IServiceCollection RegisterBeepServices(
//            this IServiceCollection services,
//            Action<BeepServiceOptions> configure)
//        {
//            var options = new BeepServiceOptions();
//            configure?.Invoke(options);

//            return RegisterBeepServices(
//                services,
//                options.ConfigPath,
//                options.ContainerName,
//                options.ConfigType);
//        }

//        /// <summary>
//        /// Registers only data source and connection services for lightweight scenarios
//        /// </summary>
//        /// <param name="services">The service collection</param>
//        /// <param name="configPath">Configuration path</param>
//        /// <returns>The service collection for method chaining</returns>
//        public static IServiceCollection RegisterDataSourceServices(
//            this IServiceCollection services,
//            string configPath = null)
//        {
//            RegisterInfrastructureServices(services, configPath, null, BeepConfigType.DataConnector);
//            RegisterUtilityServices(services);

//            // Register only data source related services
//            services.AddSingleton<IDataSourceLifecycleHelper, DataSourceLifecycleHelper>();
//            services.AddSingleton<IConnectionHelper>(provider =>
//            {
//                var dmeEditor = provider.GetRequiredService<IDMEEditor>();
//                return new ConnectionHelper(dmeEditor);
//            });

//            return services;
//        }
//    }

//    /// <summary>
//    /// Configuration options for Beep service registration
//    /// </summary>
//    public class BeepServiceOptions
//    {
//        /// <summary>
//        /// Gets or sets the configuration path. If null, uses default location.
//        /// </summary>
//        public string ConfigPath { get; set; }

//        /// <summary>
//        /// Gets or sets the container name for multi-tenant scenarios.
//        /// </summary>
//        public string ContainerName { get; set; }

//        /// <summary>
//        /// Gets or sets the configuration type.
//        /// </summary>
//        public BeepConfigType ConfigType { get; set; } = BeepConfigType.Application;

//        /// <summary>
//        /// Gets or sets whether to enable advanced logging features.
//        /// </summary>
//        public bool EnableAdvancedLogging { get; set; } = true;

//        /// <summary>
//        /// Gets or sets whether to preload assemblies during initialization.
//        /// </summary>
//        public bool PreloadAssemblies { get; set; } = true;

//        /// <summary>
//        /// Gets or sets the assembly loading timeout in milliseconds.
//        /// </summary>
//        public int AssemblyLoadTimeoutMs { get; set; } = 30000;
//    }
//}
