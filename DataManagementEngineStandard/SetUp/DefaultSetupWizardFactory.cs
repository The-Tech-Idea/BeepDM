using System;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.SetUp
{
    /// <summary>
    /// Default <see cref="ISetupWizardFactory"/> implementation.
    ///
    /// <see cref="CreateDefault"/> returns an empty wizard (no steps).
    /// Applications should register a custom factory that adds their domain steps,
    /// or use <see cref="Create"/> with a configuration callback.
    /// </summary>
    public class DefaultSetupWizardFactory : ISetupWizardFactory
    {
        /// <inheritdoc/>
        /// <remarks>
        /// Returns a wizard with no steps by default. Applications that need a standard
        /// wizard should override this class or register a custom <see cref="ISetupWizardFactory"/>.
        /// </remarks>
        public (ISetupWizard wizard, SetupContext context) CreateDefault(IDMEEditor editor)
        {
            var options = new SetupOptions();
            var context = new SetupContext { Editor = editor, Options = options };

            var wizard = new SetupWizardBuilder()
                .WithId("default-setup")
                .WithOptions(options)
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
