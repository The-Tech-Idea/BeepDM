namespace TheTechIdea.Beep.SetUp
{
    /// <summary>
    /// Contract for the fluent wizard builder passed to
    /// <see cref="ISetupWizardFactory.Create"/> as <c>Action&lt;ISetupWizardBuilder&gt;</c>.
    /// The full implementation <c>SetupWizardBuilder</c> lives in the engine project
    /// and implements this marker interface so the models project can carry the type
    /// without taking a dependency on engine-only code.
    /// </summary>
    public interface ISetupWizardBuilder
    {
        /// <summary>Sets the wizard identifier.</summary>
        ISetupWizardBuilder WithId(string wizardId);

        /// <summary>Sets the target environment label.</summary>
        ISetupWizardBuilder WithEnvironment(string env);

        /// <summary>Adds a setup step to the pipeline.</summary>
        ISetupWizardBuilder AddStep(ISetupStep step);

        /// <summary>Builds and returns the configured wizard.</summary>
        ISetupWizard Build();
    }
}