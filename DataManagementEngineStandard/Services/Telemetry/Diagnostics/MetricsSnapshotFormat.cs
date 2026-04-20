namespace TheTechIdea.Beep.Services.Telemetry.Diagnostics
{
    /// <summary>
    /// Output format used by
    /// <see cref="PeriodicMetricsSnapshotHostedService"/> when writing
    /// periodic metrics snapshots to disk.
    /// </summary>
    public enum MetricsSnapshotFormat
    {
        /// <summary>Plain-text key/value report (the default).</summary>
        Text = 0,

        /// <summary>Machine-readable JSON document.</summary>
        Json = 1
    }
}
