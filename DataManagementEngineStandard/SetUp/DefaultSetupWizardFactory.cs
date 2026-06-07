using System;
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
    /// </summary>
    public class DefaultSetupWizardFactory : ISetupWizardFactory
    {
        public (ISetupWizard wizard, SetupContext context) CreateDefault(IDMEEditor editor)
        {
            var options = new SetupOptions();
            var context = new SetupContext { Editor = editor, Options = options };

            var wizard = new SetupWizardBuilder()
                .WithId("standard-setup")
                .WithOptions(options)
                .AddStep(new DriverProvisionStep(new DriverProvisionStepOptions()))
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
            Action<SetupWizardBuilder> configure)
        {
            if (editor == null) throw new ArgumentNullException(nameof(editor));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            options ??= new SetupOptions();
            var context = new SetupContext { Editor = editor, Options = options };

            var builder = new SetupWizardBuilder().WithOptions(options);
            configure(builder);
            var wizard = builder.Build();

            return (wizard, context);
        }
    }
}
