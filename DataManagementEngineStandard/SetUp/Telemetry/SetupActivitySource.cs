using System.Diagnostics;

namespace TheTechIdea.Beep.SetUp.Telemetry
{
    /// <summary>
    /// The setup framework's <see cref="ActivitySource"/>. Zero-cost when nothing listens, so there
    /// is no opt-out for solo. Subscribe with an <see cref="ActivityListener"/> for the source name
    /// <c>TheTechIdea.Beep.SetUp</c>, or via OpenTelemetry's <c>AddSource(...)</c>.
    /// </summary>
    public static class SetupActivitySource
    {
        public const string Name = "TheTechIdea.Beep.SetUp";

        public static readonly ActivitySource Source = new(Name, "1.0.0");

        // Tag keys — stable so dashboards can rely on them.
        public const string TagWizardId = "beep.setup.wizard_id";
        public const string TagStepId = "beep.setup.step_id";
        public const string TagEnvironment = "beep.setup.environment";
        public const string TagDefinitionHash = "beep.setup.definition_hash";
        public const string TagActorId = "beep.setup.actor_id";
        public const string TagOutcome = "beep.setup.outcome";
    }
}
