using System;
using Microsoft.Extensions.DependencyInjection;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Schema;

namespace TheTechIdea.Beep.Services
{
    /// <summary>
    /// DI registration for the unified schema manager.
    /// Singleton per <see cref="IDMEEditor"/> — holds only a reference to the editor + a snapshot store.
    /// </summary>
    public static class BeepServiceSchemaExtensions
    {
        /// <summary>
        /// Registers <see cref="ISchemaManager"/> as a singleton in the service collection.
        /// </summary>
        public static IServiceCollection AddBeepSchemaManager(this IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            services.AddSingleton<ISchemaManager>(sp =>
            {
                var editor = sp.GetRequiredService<IDMEEditor>();
                return new SchemaManager(editor);
            });
            return services;
        }
    }
}
