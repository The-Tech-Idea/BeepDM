using System;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.StreamandEvents
{
    // ── Health probe ──────────────────────────────────────────────────────────

    /// <summary>
    /// Reports the health of an underlying stream transport (broker, gRPC endpoint, etc.).
    /// Used by <c>GrpcStreamHealthProbe</c> and other transport-specific health checkers.
    /// </summary>
    public interface IStreamHealthProbe
    {
        /// <summary>
        /// Performs a health check for the named <paramref name="topic"/> / <paramref name="consumerGroup"/> pair.
        /// Returns a <see cref="StreamHealthReport"/> regardless of outcome (never throws for connectivity failures).
        /// </summary>
        Task<StreamHealthReport> CheckAsync(
            string topic,
            string consumerGroup,
            CancellationToken ct = default);
    }

    // ── Channel pool ──────────────────────────────────────────────────────────

    /// <summary>
    /// Manages a pool of gRPC channel instances keyed by endpoint address.
    /// The engine implementation (<c>DefaultGrpcChannelPool</c>) returns actual
    /// <c>GrpcChannel</c> objects; the models interface uses <c>object</c> to remain
    /// free of any gRPC SDK dependency.
    /// </summary>
    public interface IGrpcChannelPool
    {
        /// <summary>
        /// Returns (and lazily creates) a channel to <paramref name="address"/>.
        /// The returned object is a <c>Grpc.Net.Client.GrpcChannel</c> in the default implementation;
        /// cast it accordingly in the engine layer.
        /// </summary>
        object GetOrCreateChannel(Uri address);

        /// <summary>Marks the channel for <paramref name="address"/> as idle (returns it to the pool).</summary>
        void ReleaseChannel(Uri address);

        /// <summary>Gracefully shuts down all pooled channels.</summary>
        Task ShutdownAsync(CancellationToken ct = default);

        /// <summary>
        /// Performs a health check for <paramref name="address"/> using the gRPC health-checking protocol
        /// (grpc.health.v1.Health.Check).  Returns <c>true</c> when the service reports SERVING.
        /// </summary>
        Task<bool> HealthCheckAsync(Uri address, CancellationToken ct = default);
    }
}
