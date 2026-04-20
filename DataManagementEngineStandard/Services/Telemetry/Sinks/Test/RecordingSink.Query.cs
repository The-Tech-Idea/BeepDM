using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Services.Audit.Models;
using TheTechIdea.Beep.Services.Logging;

namespace TheTechIdea.Beep.Services.Telemetry.Sinks.Test
{
    /// <summary>
    /// Typed query helpers for <see cref="RecordingSink"/>. Tests use these
    /// instead of filtering the raw <see cref="RecordingSink.All"/> list so
    /// assertions stay readable and the kind-discriminator logic lives in
    /// one place.
    /// </summary>
    public sealed partial class RecordingSink
    {
        /// <summary>
        /// Returns every recorded log envelope (excludes audit and
        /// self-observability events).
        /// </summary>
        public IReadOnlyList<TelemetryEnvelope> Logs()
        {
            TelemetryEnvelope[] all = _buffer.ToArray();
            List<TelemetryEnvelope> result = new List<TelemetryEnvelope>(all.Length);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].Kind == TelemetryKind.Log) { result.Add(all[i]); }
            }
            return result;
        }

        /// <summary>
        /// Returns every recorded log envelope at or above
        /// <paramref name="minLevel"/>.
        /// </summary>
        public IReadOnlyList<TelemetryEnvelope> Logs(BeepLogLevel minLevel)
        {
            TelemetryEnvelope[] all = _buffer.ToArray();
            List<TelemetryEnvelope> result = new List<TelemetryEnvelope>();
            for (int i = 0; i < all.Length; i++)
            {
                TelemetryEnvelope env = all[i];
                if (env.Kind == TelemetryKind.Log && env.Level >= minLevel)
                {
                    result.Add(env);
                }
            }
            return result;
        }

        /// <summary>
        /// Returns every recorded log envelope whose
        /// <see cref="TelemetryEnvelope.Category"/> matches
        /// <paramref name="category"/> (case-insensitive).
        /// </summary>
        public IReadOnlyList<TelemetryEnvelope> WithCategory(string category)
        {
            if (string.IsNullOrEmpty(category))
            {
                return Array.Empty<TelemetryEnvelope>();
            }

            TelemetryEnvelope[] all = _buffer.ToArray();
            List<TelemetryEnvelope> result = new List<TelemetryEnvelope>();
            for (int i = 0; i < all.Length; i++)
            {
                TelemetryEnvelope env = all[i];
                if (env.Kind == TelemetryKind.Log
                    && string.Equals(env.Category, category, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(env);
                }
            }
            return result;
        }

        /// <summary>
        /// Returns every recorded audit event. Pass <paramref name="source"/>
        /// to filter by <see cref="AuditEvent.Source"/> (case-insensitive).
        /// </summary>
        public IReadOnlyList<AuditEvent> Audit(string source = null)
        {
            TelemetryEnvelope[] all = _buffer.ToArray();
            List<AuditEvent> result = new List<AuditEvent>();
            for (int i = 0; i < all.Length; i++)
            {
                TelemetryEnvelope env = all[i];
                if (env.Kind != TelemetryKind.Audit || env.Audit is null) { continue; }
                if (source is null
                    || string.Equals(env.Audit.Source, source, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(env.Audit);
                }
            }
            return result;
        }

        /// <summary>
        /// Returns every recorded self-observability envelope. Pass
        /// <paramref name="categoryPrefix"/> to filter by
        /// <see cref="TelemetryEnvelope.Category"/> prefix (e.g.
        /// <c>BeepTelemetry.Self.Sink</c>).
        /// </summary>
        public IReadOnlyList<TelemetryEnvelope> SelfEvents(string categoryPrefix = null)
        {
            TelemetryEnvelope[] all = _buffer.ToArray();
            List<TelemetryEnvelope> result = new List<TelemetryEnvelope>();
            for (int i = 0; i < all.Length; i++)
            {
                TelemetryEnvelope env = all[i];
                if (env.Kind != TelemetryKind.Self) { continue; }
                if (categoryPrefix is null
                    || (env.Category is not null
                        && env.Category.StartsWith(categoryPrefix, StringComparison.OrdinalIgnoreCase)))
                {
                    result.Add(env);
                }
            }
            return result;
        }

        /// <summary>
        /// Returns the first log envelope whose message contains
        /// <paramref name="needle"/> (case-insensitive), or <c>null</c>.
        /// Useful for tests that just need to assert that a single
        /// expected event surfaced.
        /// </summary>
        public TelemetryEnvelope FirstLog(string needle)
        {
            if (string.IsNullOrEmpty(needle)) { return null; }
            TelemetryEnvelope[] all = _buffer.ToArray();
            for (int i = 0; i < all.Length; i++)
            {
                TelemetryEnvelope env = all[i];
                if (env.Kind != TelemetryKind.Log || env.Message is null) { continue; }
                if (env.Message.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return env;
                }
            }
            return null;
        }
    }
}
