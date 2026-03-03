namespace TheTechIdea.Beep.Editor.Importing.Staging
{
    /// <summary>
    /// Controls whether raw records are written to a staging entity before normalization.
    /// When staging is enabled the pipeline writes all source records to
    /// <c>&lt;EntityName&gt;&lt;StagingEntitySuffix&gt;</c> first, then normalizes and
    /// moves them to the final destination entity.
    /// </summary>
    public sealed class StagingOptions
    {
        /// <summary>
        /// Master switch.  When <c>false</c> (default) records go directly to the destination.
        /// </summary>
        public bool Enabled                  { get; set; } = false;

        /// <summary>
        /// Suffix appended to the entity name to form the staging entity name.
        /// Defaults to <c>"_raw"</c> (e.g. <c>Customers_raw</c>).
        /// </summary>
        public string StagingEntitySuffix    { get; set; } = "_raw";

        /// <summary>
        /// When <c>true</c> the staging entity is dropped after normalization succeeds.
        /// When <c>false</c> the staging entity is kept for auditing.
        /// </summary>
        public bool DropStagingAfterNormalize { get; set; } = false;

        /// <summary>
        /// Skip the normalization step entirely — records remain in the staging entity only.
        /// Useful for inspection runs without writing to the live destination table.
        /// </summary>
        public bool SkipNormalization        { get; set; } = false;
    }
}
