using System;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.SetUp
{
    /// <summary>
    /// Creates pre-configured <see cref="ISetupWizard"/> instances so platform adapters
    /// do not duplicate wizard construction logic.
    /// </summary>
    public interface ISetupWizardFactory
    {
        /// <summary>
        /// Returns a wizard and its associated context built from the application's
        /// default steps (driver provision → connection config → schema setup → seeding).
        /// </summary>
        (ISetupWizard wizard, SetupContext context) CreateDefault(IDMEEditor editor);

        /// <summary>
        /// Returns a wizard and context built with explicit options and a builder callback
        /// for adding custom steps.
        /// </summary>
        (ISetupWizard wizard, SetupContext context) Create(
            IDMEEditor editor,
            SetupOptions options,
            Action<ISetupWizardBuilder> configure);
    }
}
