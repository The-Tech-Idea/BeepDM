using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Schema;

namespace TheTechIdea.Beep.Services
{
    /// <summary>
    /// DI registration for the unified schema manager and its dependencies.
    /// </summary>
    public static class BeepServiceSchemaExtensions
    {
        /// <summary>
        /// Registers <see cref="ISchemaManager"/> and its dependencies as singletons.
        /// </summary>
        public static IServiceCollection AddBeepSchemaManager(this IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            services.TryAddSingleton<ISchemaComparator, SchemaComparator>();
            services.TryAddSingleton<ISchemaFingerprinter, SchemaFingerprinter>();
            services.TryAddSingleton<ISchemaSnapshotStore, FileSchemaSnapshotStore>();

            services.AddSingleton<ISchemaManager>(sp =>
            {
                var editor = sp.GetRequiredService<IDMEEditor>();
                var store = sp.GetRequiredService<ISchemaSnapshotStore>();
                var comparator = sp.GetRequiredService<ISchemaComparator>();
                var fingerprinter = sp.GetRequiredService<ISchemaFingerprinter>();
                return new SchemaManager(editor, store, comparator, fingerprinter);
            });
            return services;
        }
    }
}

