using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace TheTechIdea.Beep.Services.Telemetry.Context
{
    /// <summary>
    /// Stamps machine, process and thread identity onto every envelope.
    /// Reads <see cref="Environment.MachineName"/> and the current process
    /// metadata once at construction time so the hot path is just dictionary
    /// writes — no <see cref="Process.GetCurrentProcess"/> call per event.
    /// </summary>
    /// <remarks>
    /// Cached values intentionally do not refresh; in long-lived hosts the
    /// machine name and process id are immutable for the lifetime of the
    /// process. Thread id is captured per call because telemetry crosses
    /// thread-pool worker boundaries freely.
    /// </remarks>
    public sealed class MachineProcessEnricher : IEnricher
    {
        private readonly string _machine;
        private readonly int _processId;
        private readonly string _processName;

        /// <summary>Creates a machine/process enricher with cached identity.</summary>
        public MachineProcessEnricher()
        {
            _machine = Environment.MachineName ?? string.Empty;

            int processId = 0;
            string processName = string.Empty;
            try
            {
                using (Process current = Process.GetCurrentProcess())
                {
                    processId = current.Id;
                    processName = current.ProcessName ?? string.Empty;
                }
            }
            catch
            {
                // Some sandboxed hosts (e.g. browser-WASM) deny process access.
                // Fall back to env when available.
                processId = Environment.ProcessId;
                processName = string.Empty;
            }

            _processId = processId;
            _processName = processName;
        }

        /// <inheritdoc/>
        public string Name => "machine-process";

        /// <inheritdoc/>
        public void Enrich(TelemetryEnvelope envelope)
        {
            if (envelope is null)
            {
                return;
            }

            if (envelope.Properties is null)
            {
                envelope.Properties = new Dictionary<string, object>();
            }

            WriteIfMissing(envelope.Properties, EnrichmentProperties.Machine, _machine);
            if (!envelope.Properties.ContainsKey(EnrichmentProperties.ProcessId))
            {
                envelope.Properties[EnrichmentProperties.ProcessId] = _processId;
            }
            WriteIfMissing(envelope.Properties, EnrichmentProperties.ProcessName, _processName);
            if (!envelope.Properties.ContainsKey(EnrichmentProperties.ThreadId))
            {
                envelope.Properties[EnrichmentProperties.ThreadId] = Thread.CurrentThread.ManagedThreadId;
            }
        }

        private static void WriteIfMissing(IDictionary<string, object> bag, string key, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }
            if (!bag.ContainsKey(key))
            {
                bag[key] = value;
            }
        }
    }
}
