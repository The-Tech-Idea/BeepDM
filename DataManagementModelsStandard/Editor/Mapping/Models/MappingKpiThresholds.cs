namespace TheTechIdea.Beep.Editor.Mapping.Models
{
    /// <summary>
    /// Thresholds that gate promotion to the next <see cref="MappingRolloutWave"/>.
    /// Defaults match the existing quality bands from
    /// <c>MappingQualityBand</c> so existing mappings can promote without
    /// reconfiguration.
    /// </summary>
    public sealed class MappingKpiThresholds
    {
        public int  MinAutoMatchCoverage { get; init; } = 80;     // 80 %
        public int  MinQualityScore      { get; init; } = 60;     // 60/100 = "Good"
        public int  MaxDriftEntries      { get; init; } = 5;
        public string MinBand            { get; init; } = "Good"; // "Good" | "Excellent"
    }
}
