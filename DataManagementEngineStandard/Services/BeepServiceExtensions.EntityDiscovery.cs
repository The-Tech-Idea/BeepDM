using System;
using Microsoft.Extensions.DependencyInjection;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.EntityDiscovery;

namespace TheTechIdea.Beep.Services
{
    public static class BeepServiceEntityDiscoveryExtensions
    {
        public static IServiceCollection AddBeepEntityDiscovery(this IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            services.AddSingleton<IEntityDiscoveryService>(sp =>
            {
                var editor = sp.GetRequiredService<IDMEEditor>();
                return new EntityDiscoveryService(editor);
            });

            return services;
        }
    }
}
