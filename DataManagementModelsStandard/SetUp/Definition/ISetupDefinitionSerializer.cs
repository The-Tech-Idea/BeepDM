namespace TheTechIdea.Beep.SetUp.Definition
{
    /// <summary>
    /// Round-trips a <see cref="SetupDefinition"/> to and from JSON.
    /// </summary>
    /// <remarks>
    /// Output must be <b>diff-stable</b>: the artifact is meant to be reviewed in a pull request,
    /// so serializing the same definition twice must produce byte-identical text, and a cosmetic
    /// change must not churn the diff.
    /// </remarks>
    public interface ISetupDefinitionSerializer
    {
        string Serialize(SetupDefinition definition);

        SetupDefinition Deserialize(string json);

        /// <summary>
        /// Stable SHA-256 over the canonical form, excluding <see cref="SetupDefinition.ContentHash"/>
        /// itself. Audit binds "what was applied" to this value.
        /// </summary>
        string ComputeContentHash(SetupDefinition definition);
    }
}
