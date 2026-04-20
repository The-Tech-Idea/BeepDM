namespace TheTechIdea.Beep.Services.Telemetry.Diagnostics
{
    /// <summary>
    /// Reserved category names used by the self-observability stage so
    /// downstream filters can isolate diagnostic noise from application
    /// telemetry.
    /// </summary>
    /// <remarks>
    /// All self events flow through the same <c>TelemetryPipeline</c> as
    /// regular logs, but they bypass the deduper / rate-limiter / sampler
    /// stages because the <see cref="SelfEventEmitter"/> already enforces
    /// its own per-(eventType, key) dedupe window. Operators can route
    /// these events to a separate sink (or filter them out entirely) by
    /// matching on <see cref="Root"/>.
    /// </remarks>
    public static class SelfEventCategory
    {
        /// <summary>Root prefix shared by every self event (<c>BeepTelemetry.Self</c>).</summary>
        public const string Root = "BeepTelemetry.Self";

        /// <summary>Sink reported a write failure (<c>BeepTelemetry.Self.Sink</c>).</summary>
        public const string Sink = Root + ".Sink";

        /// <summary>Pipeline queue dropped envelopes (<c>BeepTelemetry.Self.Queue</c>).</summary>
        public const string Queue = Root + ".Queue";

        /// <summary>Retention sweep produced a result (<c>BeepTelemetry.Self.Retention</c>).</summary>
        public const string Retention = Root + ".Retention";

        /// <summary>Storage budget breach detected (<c>BeepTelemetry.Self.Budget</c>).</summary>
        public const string Budget = Root + ".Budget";

        /// <summary>Hash chain divergence detected (<c>BeepTelemetry.Self.Chain</c>).</summary>
        public const string Chain = Root + ".Chain";

        /// <summary>Metrics snapshot writer state change (<c>BeepTelemetry.Self.Snapshot</c>).</summary>
        public const string Snapshot = Root + ".Snapshot";
    }
}
