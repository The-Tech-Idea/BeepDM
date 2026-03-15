namespace TheTechIdea.Beep.Pipelines.Interfaces
{
    /// <summary>
    /// Outcome of a single record validation rule.
    /// </summary>
    public enum ValidationOutcome
    {
        /// <summary>Record passed all rules — continue to main sink.</summary>
        Pass,

        /// <summary>Record has an issue but should still be written — flag it in metadata.</summary>
        Warn,

        /// <summary>Record failed a rule — route to the error sink.</summary>
        Reject
    }
}
