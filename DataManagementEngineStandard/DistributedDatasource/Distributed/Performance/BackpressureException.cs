using System;
using System.Runtime.Serialization;

namespace TheTechIdea.Beep.Distributed.Performance
{
    /// <summary>
    /// Thrown when a Phase 14 capacity control (concurrency gate,
    /// rate limiter, or shard-permit) rejects a caller instead of
    /// letting the request queue unboundedly. Carries a
    /// <see cref="RetryAfter"/> hint so callers can back off without
    /// guessing a delay.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Backpressure is explicit rather than silent: the distribution
    /// tier would rather fast-fail with a structured exception than
    /// let a slow shard pull the whole datasource down. Higher-level
    /// frameworks (queues, schedulers) can catch this and re-submit
    /// after <see cref="RetryAfter"/>.
    /// </para>
    /// <para>
    /// The exception is serializable so it survives crossing AppDomain
    /// or process boundaries when routed through Beep's proxy /
    /// remoting layers.
    /// </para>
    /// </remarks>
    [Serializable]
    public sealed class BackpressureException : Exception
    {
        /// <summary>Creates a backpressure error with a diagnostic hint.</summary>
        /// <param name="gateName">Name of the gate that rejected the call (e.g. <c>"DistributedCall"</c> or <c>"Shard:orders-01"</c>).</param>
        /// <param name="retryAfter">Suggested wait before retrying; may be <see cref="TimeSpan.Zero"/> when unknown.</param>
        /// <param name="message">Optional custom message (defaults to an informative one).</param>
        /// <param name="inner">Optional inner exception.</param>
        public BackpressureException(
            string    gateName,
            TimeSpan  retryAfter,
            string    message = null,
            Exception inner   = null)
            : base(message ?? $"Distribution-tier backpressure: gate '{gateName}' rejected the call; retry after {retryAfter.TotalMilliseconds:F0} ms.", inner)
        {
            GateName   = gateName   ?? string.Empty;
            RetryAfter = retryAfter < TimeSpan.Zero ? TimeSpan.Zero : retryAfter;
        }

        private BackpressureException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            GateName   = info.GetString(nameof(GateName)) ?? string.Empty;
            RetryAfter = TimeSpan.FromTicks(info.GetInt64(nameof(RetryAfter)));
        }

        /// <summary>Name of the gate that rejected the call.</summary>
        public string GateName { get; }

        /// <summary>Suggested wait before retrying.</summary>
        public TimeSpan RetryAfter { get; }

        /// <inheritdoc/>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            base.GetObjectData(info, context);
            info.AddValue(nameof(GateName),   GateName);
            info.AddValue(nameof(RetryAfter), RetryAfter.Ticks);
        }
    }
}
