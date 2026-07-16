using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Schema;

namespace TheTechIdea.Beep.Services
{
    /// <summary>
    /// DI registration for the shared schema primitives (comparison + fingerprinting).
    /// Schema-change/drift is owned by <c>MigrationManager</c>; cross-datasource sync
    /// preflight/draft is the stateless <c>SyncSchemaPreflight</c> helper — neither needs DI.
    /// </summary>
    public static class BeepServiceSchemaExtensions
    {
        /// <summary>Registers the shared schema comparison + fingerprint primitives as singletons.</summary>
        public static IServiceCollection AddBeepSchemaManager(this IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            services.TryAddSingleton<ISchemaComparator, SchemaComparator>();
            services.TryAddSingleton<ISchemaFingerprinter, SchemaFingerprinter>();
            return services;
        }
    }
}

