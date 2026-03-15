using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Logger.Providers;

namespace TheTechIdea.Beep.Logger.Extensions
{
    /// <summary>
    /// Extension methods for configuring DMLogger with dependency injection
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds DMLogger as the logging provider to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for method chaining</returns>
        public static IServiceCollection AddDMLogger(this IServiceCollection services)
        {
            // Register DMLogger as singleton
            services.AddSingleton<DMLogger>();
            
            // Register as IDMLogger interface
            services.AddSingleton<IDMLogger>(provider => provider.GetRequiredService<DMLogger>());
            
            // Register as Microsoft.Extensions.Logging.ILogger
            services.AddSingleton<ILogger>(provider => provider.GetRequiredService<DMLogger>());
            
            // Register as Serilog.ILogger
            services.AddSingleton<Serilog.ILogger>(provider => provider.GetRequiredService<DMLogger>());

            return services;
        }

        /// <summary>
        /// Adds DMLogger as the logging provider with custom configuration
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configureLogger">Action to configure the logger</param>
        /// <returns>The service collection for method chaining</returns>
        public static IServiceCollection AddDMLogger(this IServiceCollection services, Action<DMLogger> configureLogger)
        {
            services.AddSingleton<DMLogger>(provider =>
            {
                var logger = new DMLogger();
                configureLogger?.Invoke(logger);
                return logger;
            });
            
            // Register as IDMLogger interface
            services.AddSingleton<IDMLogger>(provider => provider.GetRequiredService<DMLogger>());
            
            // Register as Microsoft.Extensions.Logging.ILogger
            services.AddSingleton<ILogger>(provider => provider.GetRequiredService<DMLogger>());
            
            // Register as Serilog.ILogger
            services.AddSingleton<Serilog.ILogger>(provider => provider.GetRequiredService<DMLogger>());

            return services;
        }

        /// <summary>
        /// Adds DMLogger as a logging provider to the ASP.NET Core logging system
        /// </summary>
        /// <param name="builder">The logging builder</param>
        /// <returns>The logging builder for method chaining</returns>
        public static ILoggingBuilder AddDMLogger(this ILoggingBuilder builder)
        {
            builder.Services.AddSingleton<ILoggerProvider, DMLoggerProvider>();
            return builder;
        }

        /// <summary>
        /// Adds DMLogger as a logging provider with custom configuration to the ASP.NET Core logging system
        /// </summary>
        /// <param name="builder">The logging builder</param>
        /// <param name="configureLogger">Action to configure the logger</param>
        /// <returns>The logging builder for method chaining</returns>
        public static ILoggingBuilder AddDMLogger(this ILoggingBuilder builder, Action<DMLogger> configureLogger)
        {
            builder.Services.AddSingleton<ILoggerProvider>(provider =>
            {
                var logger = new DMLogger();
                configureLogger?.Invoke(logger);
                return new DMLoggerProvider(logger);
            });
            return builder;
        }
    }
}