using System.Runtime.CompilerServices;
using TheTechIdea.Beep.ConfigUtil;

// MigrationHistory / MigrationRecord / MigrationStep used to be defined in this
// assembly (DataManagementEngine) AND identically in DataManagementModels under the
// same namespace. Two shipped public types with the same fully-qualified name meant
// any consumer referencing both packages hit CS0433 ("type exists in both assemblies").
// The canonical copy now lives only in DataManagementModels; these forwarders keep
// binary compatibility for consumers already bound to this assembly's copy.
[assembly: TypeForwardedTo(typeof(MigrationHistory))]
[assembly: TypeForwardedTo(typeof(MigrationRecord))]
[assembly: TypeForwardedTo(typeof(MigrationStep))]
