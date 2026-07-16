using System.Text.Json.Serialization;
using TheTechIdea.Beep.SetUp.Seeding;

namespace TheTechIdea.Beep.SetUp.Steps
{
    /// <summary>
    /// Configuration options for <see cref="SeedingStep"/>.
    /// </summary>
    public class SeedingStepOptions
    {
        /// <summary>
        /// Registry containing all seeders to run. Required.
        /// </summary>
        /// <remarks>
        /// Never serialized into a <c>SetupDefinition</c>: it is a live object graph, and
        /// deserializing seeders named in a file would be an arbitrary-code-execution vector once
        /// definitions can arrive from a shared store. <c>ISetupStepFactory</c> injects it from DI.
        /// </remarks>
        [JsonIgnore]
        public ISeederRegistry Registry { get; set; }

        /// <summary>
        /// When set, only seeders whose <see cref="ISeeder.SeederId"/> appears in this list
        /// will be executed. Useful for partial / incremental seeding scenarios.
        /// When <c>null</c> (default), all registered seeders run.
        /// </summary>
        public System.Collections.Generic.IReadOnlyList<string> SeederFilter { get; set; }
    }
}
