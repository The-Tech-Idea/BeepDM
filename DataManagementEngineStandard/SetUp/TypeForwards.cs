using System.Runtime.CompilerServices;
using TheTechIdea.Beep.SetUp.Steps;

// These option types moved from DataManagementEngine to DataManagementModels so that a
// SetupDefinition's option shapes live with the contracts — a CLI or CI validator must be able to
// read a definition without referencing the engine.
//
// The namespace is unchanged, so source consumers are unaffected. These forwards keep consumers
// that were compiled against the older engine assembly resolving at runtime.
[assembly: TypeForwardedTo(typeof(DefaultsSetupStepOptions))]
[assembly: TypeForwardedTo(typeof(DataImportStepOptions))]
