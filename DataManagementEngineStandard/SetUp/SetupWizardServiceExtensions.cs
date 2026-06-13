using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
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
                var logger = sp.GetService<ILogger<FileBasedFirstRunDetector>>();
                return new FileBasedFirstRunDetector(editor, markerFileName, logger);
            });
            return services;
        }

        public static IServiceCollection AddSetupWizard(this IServiceCollection services)
        {
            services.TryAddSingleton<ISetupWizardFactory>(sp =>
            {
                var logger = sp.GetService<ILogger<SetupWizard>>();
                return new DefaultSetupWizardFactory(logger);
            });
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

        public static IServiceCollection AddBeepBootstrapper(this IServiceCollection services)
        {
            services.TryAddSingleton(sp =>
            {
                var firstRun = sp.GetRequiredService<IFirstRunDetector>();
                var factory  = sp.GetRequiredService<ISetupWizardFactory>();
                var adapter  = sp.GetRequiredService<ISetupWizardAdapter>();
                var logger   = sp.GetService<ILogger<BeepBootstrapper>>();
                return new BeepBootstrapper(
                    firstRun, factory,
                    () => sp.GetService<IDMEEditor>()
                           ?? throw new InvalidOperationException(
                                "BeepBootstrapper could not resolve IDMEEditor from DI."),
                    adapter,
                    logger);
            });
            return services;
        }

        public static IServiceCollection AddBeepBootstrapper(
            this IServiceCollection services,
            Func<IServiceProvider, IDMEEditor> editorAccessor)
        {
            services.TryAddSingleton(sp =>
            {
                var firstRun = sp.GetRequiredService<IFirstRunDetector>();
                var factory  = sp.GetRequiredService<ISetupWizardFactory>();
                var adapter  = sp.GetRequiredService<ISetupWizardAdapter>();
                var logger   = sp.GetService<ILogger<BeepBootstrapper>>();
                return new BeepBootstrapper(firstRun, factory, () => editorAccessor(sp), adapter, logger);
            });
            return services;
        }
    }
}
