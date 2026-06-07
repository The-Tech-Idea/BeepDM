using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.SetUp
{
    internal sealed class SetupWizardServices
    {
        public required ISetupWizard Wizard { get; init; }
        public required SetupContext Context { get; init; }
    }

    public static class SetupWizardServiceExtensions
    {
        public static IServiceCollection AddFirstRunDetector(this IServiceCollection services, string? markerFileName = null)
        {
            services.TryAddSingleton<IFirstRunDetector>(sp =>
            {
                var editor = sp.GetRequiredService<IDMEEditor>();
                return new FileBasedFirstRunDetector(editor, markerFileName);
            });
            return services;
        }

        public static IServiceCollection AddSetupWizard(this IServiceCollection services)
        {
            services.TryAddSingleton<ISetupWizardFactory, DefaultSetupWizardFactory>();
            services.TryAddSingleton(sp =>
            {
                var factory = sp.GetRequiredService<ISetupWizardFactory>();
                var editor = sp.GetRequiredService<IDMEEditor>();
                var (wizard, context) = factory.CreateDefault(editor);
                return new SetupWizardServices { Wizard = wizard, Context = context };
            });
            services.TryAddSingleton(sp => sp.GetRequiredService<SetupWizardServices>().Context);
            services.TryAddSingleton<ISetupWizard>(sp => sp.GetRequiredService<SetupWizardServices>().Wizard);
            return services;
        }

        public static IServiceCollection AddSetupWizardAdapter<T>(this IServiceCollection services)
            where T : class, ISetupWizardAdapter
        {
            services.TryAddSingleton<ISetupWizardAdapter, T>();
            return services;
        }

        public static IServiceCollection AddApplicationBootstrapper(this IServiceCollection services)
        {
            services.TryAddSingleton(sp =>
            {
                var firstRun = sp.GetRequiredService<IFirstRunDetector>();
                var wizard = sp.GetRequiredService<ISetupWizard>();
                var context = sp.GetRequiredService<SetupContext>();
                var adapter = sp.GetRequiredService<ISetupWizardAdapter>();
                return new ApplicationBootstrapper(firstRun, wizard, context, adapter);
            });
            return services;
        }
    }
}
