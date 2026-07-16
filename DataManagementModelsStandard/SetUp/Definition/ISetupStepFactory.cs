using System;
using System.Collections.Generic;
using System.Text.Json;

namespace TheTechIdea.Beep.SetUp.Definition
{
    /// <summary>
    /// Builds an <see cref="ISetupStep"/> from a <see cref="SetupStepDefinition"/>.
    /// </summary>
    /// <remarks>
    /// This is the <b>allow-list</b>. A definition names a registered type key, never an
    /// assembly-qualified type, so a definition arriving from a shared store (Phase 3) cannot cause
    /// arbitrary type instantiation. Unknown keys are rejected, not probed.
    /// </remarks>
    public interface ISetupStepFactory
    {
        /// <summary>Registers how to build a step type from its serialized options payload.</summary>
        void Register(string typeKey, Func<JsonElement?, ISetupStep> factory);

        bool CanCreate(string typeKey);

        /// <summary>Builds the step. Throws <see cref="InvalidOperationException"/> on an unknown key.</summary>
        ISetupStep Create(SetupStepDefinition definition);

        IReadOnlyCollection<string> RegisteredTypes { get; }
    }
}
