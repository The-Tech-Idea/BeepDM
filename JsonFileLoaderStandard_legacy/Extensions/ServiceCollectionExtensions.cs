using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Utilities.Extensions
{
    /// <summary>
    /// Extension methods for registering JsonLoader with dependency injection
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds JsonLoader services to the dependency injection container
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for method chaining</returns>
        public static IServiceCollection AddJsonLoader(this IServiceCollection services)
        {
            services.TryAddSingleton<JsonLoader>();
            services.TryAddSingleton<IJsonLoader>(provider => provider.GetRequiredService<JsonLoader>());
           // services.TryAddSingleton<IEnhancedJsonLoader>(provider => provider.GetRequiredService<JsonLoader>());
            
            return services;
        }

        /// <summary>
        /// Adds JsonLoader services as scoped to the dependency injection container
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for method chaining</returns>
        public static IServiceCollection AddJsonLoaderScoped(this IServiceCollection services)
        {
            services.TryAddScoped<JsonLoader>();
            services.TryAddScoped<IJsonLoader>(provider => provider.GetRequiredService<JsonLoader>());
          //  services.TryAddScoped<IEnhancedJsonLoader>(provider => provider.GetRequiredService<JsonLoader>());
            
            return services;
        }

        /// <summary>
        /// Adds JsonLoader services as transient to the dependency injection container
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for method chaining</returns>
        public static IServiceCollection AddJsonLoaderTransient(this IServiceCollection services)
        {
            services.TryAddTransient<JsonLoader>();
            services.TryAddTransient<IJsonLoader>(provider => provider.GetRequiredService<JsonLoader>());
           // services.TryAddTransient<IEnhancedJsonLoader>(provider => provider.GetRequiredService<JsonLoader>());
            
            return services;
        }
    }
}