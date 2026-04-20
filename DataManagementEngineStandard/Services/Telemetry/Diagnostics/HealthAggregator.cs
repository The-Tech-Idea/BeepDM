using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Services.Telemetry.Diagnostics
{
    /// <summary>
    /// Rolls a list of sinks up into a single health verdict plus a
    /// per-sink breakdown. Used by <see cref="PipelineMetrics"/> when
    /// composing a snapshot and by <see cref="PeriodicMetricsSnapshotHostedService"/>
    /// when writing the periodic file.
    /// </summary>
    /// <remarks>
    /// The aggregator is allocation-free for the common path of zero
    /// sinks; for non-empty inputs it returns a tiny POCO list. It does
    /// not cache results — the caller is expected to throttle invocation
    /// (the snapshot writer already runs on its own cadence).
    /// </remarks>
    public sealed class HealthAggregator
    {
        private readonly IReadOnlyList<ITelemetrySink> _sinks;

        /// <summary>Creates an aggregator over the supplied sink list.</summary>
        public HealthAggregator(IReadOnlyList<ITelemetrySink> sinks)
        {
            _sinks = sinks ?? Array.Empty<ITelemetrySink>();
        }

        /// <summary>True when every sink reports itself healthy.</summary>
        public bool IsHealthy
        {
            get
            {
                for (int i = 0; i < _sinks.Count; i++)
                {
                    ITelemetrySink sink = _sinks[i];
                    if (sink is null)
                    {
                        continue;
                    }
                    if (!sink.IsHealthy)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        /// <summary>Returns one <see cref="SinkHealth"/> entry per registered sink.</summary>
        public IReadOnlyList<SinkHealth> Probe()
        {
            if (_sinks.Count == 0)
            {
                return Array.Empty<SinkHealth>();
            }

            SinkHealth[] result = new SinkHealth[_sinks.Count];
            for (int i = 0; i < _sinks.Count; i++)
            {
                ITelemetrySink sink = _sinks[i];
                if (sink is ISinkHealthProbe probe)
                {
                    try
                    {
                        result[i] = probe.Probe() ?? SinkHealth.FromBareSink(sink);
                    }
                    catch
                    {
                        result[i] = SinkHealth.FromBareSink(sink);
                    }
                }
                else
                {
                    result[i] = SinkHealth.FromBareSink(sink);
                }
            }
            return result;
        }
    }
}
