namespace TheTechIdea.Beep.Editor.BeepSync
{
    /// <summary>
    /// Rollout wave for a sync plan promotion. Determines how many production
    /// traffic-shares a plan is allowed to serve before being promoted to the
    /// next wave.
    /// </summary>
    public enum SyncRolloutWave
    {
        /// <summary>Plan is in the queue but not yet deployed.</summary>
        Draft = 0,
        /// <summary>1% production traffic — diagnostic only.</summary>
        Canary = 1,
        /// <summary>10% production traffic — close monitoring.</summary>
        EarlyAdopter = 2,
        /// <summary>100% production traffic — fully promoted.</summary>
        GeneralAvailability = 3,
        /// <summary>Plan is deprecated; new runs should pick a newer plan.</summary>
        Deprecation = 4
    }
}
