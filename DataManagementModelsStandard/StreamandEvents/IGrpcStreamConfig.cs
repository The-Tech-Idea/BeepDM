using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.StreamandEvents
{
    // ── Compression ───────────────────────────────────────────────────────────

    /// <summary>Compression algorithm applied at the gRPC channel level.</summary>
    public enum GrpcCompressionAlgorithm
    {
        None    = 0,
        Gzip    = 1,
        Deflate = 2
    }

    // ── Retry policy ──────────────────────────────────────────────────────────

    /// <summary>Parameters for automatic call retries on transient gRPC failures.</summary>
    public sealed class GrpcRetryPolicy
    {
        /// <summary>Maximum number of retry attempts before propagating the failure. Default: 3.</summary>
        public int MaxRetries { get; init; } = 3;

        /// <summary>Initial delay before the first retry. Default: 100 ms.</summary>
        public TimeSpan InitialBackoff { get; init; } = TimeSpan.FromMilliseconds(100);

        /// <summary>Maximum delay between retries. Default: 5 s.</summary>
        public TimeSpan MaxBackoff { get; init; } = TimeSpan.FromSeconds(5);

        /// <summary>Multiplier applied to the backoff on each retry. Default: 2.0 (exponential).</summary>
        public double BackoffMultiplier { get; init; } = 2.0;

        /// <summary>
        /// gRPC status codes that should trigger a retry.
        /// Stored as ints to keep the models assembly free of gRPC SDK references.
        /// Defaults: 14 (Unavailable), 4 (DeadlineExceeded).
        /// </summary>
        public IReadOnlyList<int> RetryableStatusCodes { get; init; } = new[] { 14, 4 };
    }

    // ── Channel options ───────────────────────────────────────────────────────

    /// <summary>Connection parameters for a single gRPC endpoint.</summary>
    public sealed class GrpcChannelOptions
    {
        /// <summary>gRPC service endpoint URI.</summary>
        public Uri Address { get; init; }

        /// <summary>Whether to use TLS. Default: <c>true</c>.</summary>
        public bool UseTls { get; init; } = true;

        /// <summary>Client certificate for mutual TLS authentication. <c>null</c> = one-way TLS.</summary>
        public X509Certificate2? ClientCertificate { get; init; }

        /// <summary>Payload compression applied at the channel level.</summary>
        public GrpcCompressionAlgorithm Compression { get; init; } = GrpcCompressionAlgorithm.None;

        /// <summary>Maximum inbound message size in bytes. <c>null</c> = use SDK default (4 MB).</summary>
        public int? MaxReceiveMessageSizeBytes { get; init; } = 4 * 1024 * 1024;

        /// <summary>Maximum outbound message size in bytes. <c>null</c> = use SDK default (4 MB).</summary>
        public int? MaxSendMessageSizeBytes { get; init; } = 4 * 1024 * 1024;

        /// <summary>HTTP/2 keep-alive interval. Default: 30 s.</summary>
        public TimeSpan KeepAliveInterval { get; init; } = TimeSpan.FromSeconds(30);

        /// <summary>HTTP/2 keep-alive timeout. Default: 5 s.</summary>
        public TimeSpan KeepAliveTimeout { get; init; } = TimeSpan.FromSeconds(5);

        /// <summary>Per-call deadline. <c>null</c> = no deadline.</summary>
        public TimeSpan? Deadline { get; init; }
    }

    // ── Config interface ──────────────────────────────────────────────────────

    /// <summary>
    /// Provides connection and retry configuration to <c>GrpcStreamAdapter</c> and
    /// <c>DefaultGrpcChannelPool</c>.  Implementations are free to read from environment
    /// variables, appsettings.json, or a central config service.
    /// </summary>
    public interface IGrpcStreamConfig
    {
        /// <summary>Returns channel options for the gRPC service at <paramref name="serviceAddress"/>.</summary>
        GrpcChannelOptions GetChannelOptions(Uri serviceAddress);

        /// <summary>Returns the retry policy to apply to streaming calls.</summary>
        GrpcRetryPolicy GetRetryPolicy();

        /// <summary>Default consumer group ID used when no group is specified by the caller.</summary>
        string GetConsumerGroup();
    }
}
