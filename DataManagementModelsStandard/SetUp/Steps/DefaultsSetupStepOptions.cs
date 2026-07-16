namespace TheTechIdea.Beep.SetUp.Steps
{
    /// <summary>
    /// Configuration options for <c>DefaultsSetupStep</c>.
    /// </summary>
    /// <remarks>
    /// Lives in the Models project alongside the other step options: a <c>SetupDefinition</c>
    /// serializes these shapes, and consumers of a definition (CLI, CI validator) must be able to
    /// read them without referencing the engine.
    /// </remarks>
    public class DefaultsSetupStepOptions
    {
        /// <summary>When false the step reports success without writing any defaults.</summary>
        public bool ApplyDefaults { get; set; } = true;
    }
}
