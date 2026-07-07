namespace TheTechIdea.Beep.Editor.Mapping.Models
{
    /// <summary>
    /// Rollout wave for a mapping. The mapping manager reads this to gate
    /// promotion to higher waves (Canary → EarlyAdopter → GA) based on
    /// measured KPIs and threshold policy.
    /// </summary>
    public enum MappingRolloutWave
    {
        /// <summary>Plan is in the queue but not yet deployed.</summary>
        Draft = 0,
        /// <summary>1% production traffic — diagnostic only.</summary>
        Canary = 1,
        /// <summary>10% production traffic — close monitoring.</summary>
        EarlyAdopter = 2,
        /// <summary>100% production traffic — fully promoted.</summary>
        GeneralAvailability = 3,
        /// <summary>Mapping is deprecated; new mappings should be authored.</summary>
        Deprecation = 4
    }
}
