namespace TheTechIdea.Beep.Services.Telemetry.Diagnostics
{
    /// <summary>
    /// Optional richer health surface for sinks that track their own
    /// success / error timestamps and counters. Sinks that only expose
    /// the bare <see cref="ITelemetrySink.IsHealthy"/> flag are still
    /// supported: <see cref="HealthAggregator"/> falls back to
    /// <see cref="SinkHealth.FromBareSink"/> for them.
    /// </summary>
    public interface ISinkHealthProbe
    {
        /// <summary>Stable sink identifier (mirrors <see cref="ITelemetrySink.Name"/>).</summary>
        string Name { get; }

        /// <summary>Returns the latest health snapshot.</summary>
        SinkHealth Probe();
    }
}
