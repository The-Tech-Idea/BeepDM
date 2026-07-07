namespace TheTechIdea.Beep.Editor.Mapping.Models
{
    /// <summary>
    /// Snapshot of the mapping's measured KPIs at the time of the latest
    /// promotion check. The mapping manager compares the values in this
    /// snapshot against the thresholds in
    /// <c>MappingKpiThresholds</c> to decide whether to advance the wave.
    /// </summary>
    public sealed class MappingKpiSnapshot
    {
        public int    AutoMatchCoveragePct   { get; init; }   // 0–100
        public int    QualityScoreAverage    { get; init; }   // 0–100
        public int    DriftEntryCount       { get; init; }
        public string TopIssueCode          { get; init; } = string.Empty;
    }
}
