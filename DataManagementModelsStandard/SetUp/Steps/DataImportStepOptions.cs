using System.Collections.Generic;

namespace TheTechIdea.Beep.SetUp.Steps
{
    /// <summary>
    /// Configuration options for <c>DataImportStep</c>.
    /// </summary>
    /// <remarks>
    /// Lives in the Models project alongside the other step options: a <c>SetupDefinition</c>
    /// serializes these shapes, and consumers of a definition (CLI, CI validator) must be able to
    /// read them without referencing the engine.
    /// </remarks>
    public class DataImportStepOptions
    {
        /// <summary>Entities to verify. When empty the step skips.</summary>
        public List<string> EntityNames { get; set; } = new();

        public bool SkipIfTargetHasData { get; set; } = true;

        /// <summary>
        /// Step ids this step depends on. When null, defaults to
        /// <c>{ "defaults-setup", "seeding" }</c>.
        /// <para>
        /// A wizard built without a seeding step must not name <c>"seeding"</c> — the builder
        /// rejects a dependency on an unregistered step.
        /// </para>
        /// </summary>
        public IReadOnlyList<string> DependsOnStepIds { get; set; }
    }
}
