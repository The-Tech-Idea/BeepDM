namespace TheTechIdea.Beep.SetUp
{
    /// <summary>
    /// Contract for the fluent wizard builder passed to
    /// <see cref="ISetupWizardFactory.Create"/> as <c>Action&lt;ISetupWizardBuilder&gt;</c>.
    /// The full implementation <c>SetupWizardBuilder</c> lives in the engine project
    /// and implements this marker interface so the models project can carry the type
    /// without taking a dependency on engine-only code.
    /// </summary>
    /// <remarks>
    /// The fluent builder API (<c>WithId</c>, <c>WithOptions</c>, <c>AddStep</c>, …) is
    /// only consumed inside the engine and host projects. The models project only needs
    /// the type itself for the factory callback signature.
    /// </remarks>
    public interface ISetupWizardBuilder
    {
        ISetupWizard Build();
    }
}