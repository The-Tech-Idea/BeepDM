using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.SetUp.Steps;

namespace TheTechIdea.Beep.SetUp
{
    /// <summary>
    /// Default <see cref="ISetupWizardFactory"/> implementation.
    ///
    /// <see cref="CreateDefault"/> returns a wizard pre-wired with the 6 standard steps:
    /// DriverProvision → ConnectionConfig → SchemaSetup → Defaults → Seeding → DataImport.
    /// Applications can override by calling <see cref="Create"/> with a configuration callback.
    ///
    /// NOTE: <see cref="CreateDefault"/> creates steps with EMPTY options objects.
    /// The returned wizard will fail validation unless the caller populates
    /// <see cref="DriverProvisionStepOptions.PackageName"/>,
    /// <see cref="ConnectionConfigStepOptions.ConnectionProperties"/>,
    /// <see cref="SchemaSetupStepOptions.EntityTypes"/>, and
    /// <see cref="SeedingStepOptions.Registry"/> before running.
    /// Use <see cref="Create"/> with a configure callback for production use.
    /// </summary>
    public class DefaultSetupWizardFactory : ISetupWizardFactory
    {
        private readonly ILogger<SetupWizard>? _logger;

        public DefaultSetupWizardFactory(ILogger<SetupWizard>? logger = null)
        {
            _logger = logger;
        }

        public (ISetupWizard wizard, SetupContext context) CreateDefault(IDMEEditor editor)
        {
            var options = new SetupOptions();
            var context = new SetupContext { Editor = editor, Options = options };

            // Read driver package names directly from ConfigEditor — same pattern as
            // Beep.Razor.Components BeepSetupWizardRunner.StageExistingConnectionDriver.
            // One DriverProvisionStep per AutoLoad driver so each can be verified
            // independently and CanSkip returns true for the ones already loaded.
            var driverSteps = BuildDriverSteps(editor);

            var builder = new SetupWizardBuilder()
                .WithId("standard-setup")
                .WithOptions(options)
                .WithLogger(_logger);

            foreach (var step in driverSteps)
                builder.AddStep(step);

            var wizard = builder
                .AddStep(new ConnectionConfigStep(new ConnectionConfigStepOptions()))
                .AddStep(new SchemaSetupStep(new SchemaSetupStepOptions()))
                .AddStep(new DefaultsSetupStep(new DefaultsSetupStepOptions()))
                .AddStep(new SeedingStep(new SeedingStepOptions()))
                .AddStep(new DataImportStep(new DataImportStepOptions()))
                .Build();

            return (wizard, context);
        }

        /// <inheritdoc/>
        public (ISetupWizard wizard, SetupContext context) Create(
            IDMEEditor editor,
            SetupOptions options,
            Action<ISetupWizardBuilder> configure)
        {
            if (editor == null) throw new ArgumentNullException(nameof(editor));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            options ??= new SetupOptions();
            var context = new SetupContext { Editor = editor, Options = options };

            var builder = new SetupWizardBuilder().WithOptions(options).WithLogger(_logger);
            configure(builder);
            var wizard = builder.Build();

            return (wizard, context);
        }

        /// <summary>
        /// Reads <c>ConfigEditor.DataDriversClasses</c> and returns one
        /// <see cref="DriverProvisionStep"/> per distinct <c>PackageName</c>.
        /// Drivers with <c>AutoLoad == true</c> are preferred; if none are
        /// present, every distinct package is included so a first-run install
        /// still stages the auto-loadable drivers from the bundled config.
        /// </summary>
        private static IReadOnlyList<DriverProvisionStep> BuildDriverSteps(IDMEEditor editor)
        {
            var drivers = editor?.ConfigEditor?.DataDriversClasses?
                .Where(d => d != null && !string.IsNullOrWhiteSpace(d.PackageName))
                .ToList() ?? new List<ConnectionDriversConfig>();

            var selected = drivers.Where(d => d.AutoLoad).ToList();
            if (selected.Count == 0)
                selected = drivers;

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new List<DriverProvisionStep>();
            foreach (var d in selected)
            {
                if (!seen.Add(d.PackageName)) continue;
                result.Add(new DriverProvisionStep(new DriverProvisionStepOptions
                {
                    PackageName = d.PackageName,
                    Version = d.NuggetVersion
                }));
            }
            return result;
        }
    }
}
