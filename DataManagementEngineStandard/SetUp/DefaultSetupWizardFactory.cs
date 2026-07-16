using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.SetUp.Seeding;
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
    /// <see cref="ConnectionConfigStepOptions.ConnectionProperties"/>, and
    /// <see cref="SchemaSetupStepOptions.EntityTypes"/> before running.
    /// Use <see cref="Create"/> with a configure callback for production use.
    /// </summary>
    public class DefaultSetupWizardFactory : ISetupWizardFactory
    {
        private readonly ILogger<SetupWizard>? _logger;
        private readonly ISeederRegistry? _seeders;

        public DefaultSetupWizardFactory(ILogger<SetupWizard>? logger = null, ISeederRegistry? seeders = null)
        {
            _logger = logger;
            _seeders = seeders;
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

            // Each driver step carries a per-package id, so the connection step must name
            // them all. Falls back to the bare id when no drivers were discovered.
            var driverStepIds = driverSteps.Count > 0
                ? driverSteps.Select(s => s.StepId).ToArray()
                : new[] { DriverProvisionStep.BaseStepId };

            builder
                .AddStep(new ConnectionConfigStep(new ConnectionConfigStepOptions
                {
                    DependsOnStepIds = driverStepIds
                }))
                .AddStep(new SchemaSetupStep(new SchemaSetupStepOptions()))
                .AddStep(new DefaultsSetupStep(new DefaultsSetupStepOptions()));

            // Only add SeedingStep when a registry is available. SeedingStep.Validate hard-fails
            // without one, so adding it unconditionally would ship a wizard that can never run.
            if (_seeders != null)
                builder.AddStep(new SeedingStep(new SeedingStepOptions { Registry = _seeders }));

            var wizard = builder
                .AddStep(new DataImportStep(new DataImportStepOptions
                {
                    DependsOnStepIds = BuildDataImportDependencies(_seeders != null)
                }))
                .Build();

            return (wizard, context);
        }

        /// <summary>
        /// DataImport depends on seeding only when a SeedingStep was actually added —
        /// naming an absent step would fail the builder's unknown-dependency check.
        /// </summary>
        private static string[] BuildDataImportDependencies(bool hasSeeding)
            => hasSeeding
                ? new[] { "defaults-setup", "seeding" }
                : new[] { "defaults-setup" };

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
            var distinct = new List<ConnectionDriversConfig>();
            foreach (var d in selected)
            {
                if (!seen.Add(d.PackageName)) continue;
                distinct.Add(d);
            }

            // Qualify step ids only when there is more than one driver. Step ids must be unique,
            // but a single-driver wizard keeps the bare "driver-provision" id so existing
            // DependsOn references and callers are unaffected.
            bool qualify = distinct.Count > 1;

            var result = new List<DriverProvisionStep>(distinct.Count);
            foreach (var d in distinct)
            {
                result.Add(new DriverProvisionStep(new DriverProvisionStepOptions
                {
                    StepId = qualify ? DriverProvisionStep.BuildStepId(d.PackageName) : null,
                    PackageName = d.PackageName,
                    Version = d.NuggetVersion
                }));
            }
            return result;
        }
    }
}
