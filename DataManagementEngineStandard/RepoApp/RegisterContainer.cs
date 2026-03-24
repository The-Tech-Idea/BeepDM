 using Microsoft.Extensions.DependencyInjection;

using TheTechIdea.Beep.Container.Services;
using TheTechIdea.Beep.Environments;

using TheTechIdea.Beep.Services;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.RepoApp
{
    public static class RegisterContainer
    {
        private static IAppRepoManager cantainerManager;
        private static IServiceCollection Services;
       

        public static IServiceCollection AddContainerManager(this IServiceCollection services)
        {
            Services = services;
            cantainerManager = new AppRepoManager(services);
            services.AddSingleton<IAppRepoManager>(cantainerManager);
       
            return services;
        }
        public static IAppRepoManager AddContainer(this IServiceCollection services, string directorypath, string containername, BeepConfigType configType, bool AddasSingleton = false)
        {
            Services = services;
            if (cantainerManager == null)
            {
                cantainerManager = new AppRepoManager(services);
                services.AddSingleton<IAppRepoManager>(cantainerManager);
            }

            BeepService beepService = new BeepService(services);
            beepService.Configure(directorypath, containername, configType, AddasSingleton);
            services.AddSingleton<IBeepService>(beepService);
            services.AddSingleton<IBeepService>(beepService);
            IBeepAppRepo container = new BeepAppRepo(containername, beepService);
            services.AddSingleton<IBeepAppRepo>(container);

            return cantainerManager;
        }
        public static IAppRepoManager AddScopedContainerManager(this IServiceCollection services)
        {
            Services = services;
            if (cantainerManager == null)
            {
                cantainerManager = new AppRepoManager(services);
                services.AddSingleton<IAppRepoManager>(cantainerManager);
            }
         
            services.AddScoped<IBeepService,BeepService>();
            return cantainerManager;
        }

    }
}
