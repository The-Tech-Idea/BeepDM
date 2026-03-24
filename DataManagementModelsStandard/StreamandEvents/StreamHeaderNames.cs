namespace TheTechIdea.Beep.StreamandEvents
{
    /// <summary>
    /// Well-known streaming header constant names.
    /// These keys are used across producer interceptors, consumer interceptors, broker adapters,
    /// and the W3C TraceContext propagator.
    /// All values are lowercase to match HTTP/2 and CloudEvents canonical casing.
    /// </summary>
    public static class StreamHeaderNames
    {
        // ── W3C Trace Context ─────────────────────────────────────────────────
        /// <summary>W3C Trace Context — carries the active span: version-traceId-parentId-flags.</summary>
        public const string TraceParent = "traceparent";

        /// <summary>W3C Trace Context — vendor-specific trace state.</summary>
        public const string TraceState = "tracestate";

        // ── Beep correlation ─────────────────────────────────────────────────
        /// <summary>Business-level correlation ID linking related events across services.</summary>
        public const string CorrelationId = "x-correlation-id";

        /// <summary>ID of the event that causally triggered this event.</summary>
        public const string CausationId = "x-causation-id";

        // ── Schema & typing ───────────────────────────────────────────────────
        /// <summary>Schema registry ID used to resolve the payload schema.</summary>
        public const string SchemaId = "x-schema-id";

        /// <summary>Fully-qualified event type name (e.g. <c>orders.OrderPlaced.v1</c>).</summary>
        public const string EventType = "x-event-type";

        /// <summary>Unique event ID (UUID or deterministic from aggregate + sequence).</summary>
        public const string EventId = "x-event-id";

        /// <summary>MIME content type of the payload bytes (e.g. <c>application/json</c>).</summary>
        public const string ContentType = "content-type";

        // ── Publisher metadata ────────────────────────────────────────────────
        /// <summary>Service name or identifier that produced the event.</summary>
        public const string PublisherService = "x-publisher-service";

        // ── Security ─────────────────────────────────────────────────────────
        /// <summary>Bearer token injected by a security interceptor (value from <c>ISecretProvider</c>).</summary>
        public const string SecurityToken = "x-security-token";

        // ── Reliability ───────────────────────────────────────────────────────
        /// <summary>Idempotency key — duplicate publishes with the same key must be deduplicated.</summary>
        public const string IdempotencyKey = "x-idempotency-key";

        /// <summary>Number of delivery attempts (0 = first attempt).</summary>
        public const string RetryCount = "x-retry-count";

        /// <summary>Reason the event was routed to the dead-letter topic.</summary>
        public const string DeadLetterReason = "x-deadletter-reason";

        // ── Multi-tenant (Phase M) ────────────────────────────────────────────
        /// <summary>Tenant identifier for multi-tenant stream isolation.</summary>
        public const string TenantId = "x-tenant-id";

        /// <summary>Tenant-scoped topic prefix applied by <c>TenantAwareBrokerAdapter</c>.</summary>
        public const string TenantTopicPrefix = "x-tenant-topic-prefix";

        // ── Claim-check (Phase N) ────────────────────────────────────────────
        /// <summary>JSON-serialised <c>ClaimTicket</c> replacing an oversized payload.</summary>
        public const string ClaimTicket = "x-claim-ticket";

        /// <summary>Original payload size in bytes, set by the claim-check producer interceptor.</summary>
        public const string PayloadSizeBytes = "x-payload-size-bytes";
    }

    // ── StreamContext (parsed W3C + Beep correlation) ─────────────────────────

    /// <summary>
    /// Parsed correlation and tracing identifiers extracted from event headers.
    /// Populated by <see cref="IStreamContextPropagator.Extract"/>.
    /// </summary>
    public sealed class StreamContext
    {
        /// <summary>W3C traceId (32 hex chars).</summary>
        public string TraceId { get; init; }

        /// <summary>W3C parentId / spanId (16 hex chars).</summary>
        public string SpanId { get; init; }

        /// <summary>W3C trace-flags byte (e.g. "01" = sampled).</summary>
        public string TraceFlags { get; init; }

        /// <summary>Beep-level correlation ID.</summary>
        public string CorrelationId { get; init; }

        /// <summary>Causation event ID.</summary>
        public string CausationId { get; init; }

        /// <summary>Unique event ID.</summary>
        public string EventId { get; init; }

        /// <summary>True when the <c>traceparent</c> header was present and well-formed.</summary>
        public bool HasTraceContext => TraceId != null;
    }

    // ── Propagator interface ──────────────────────────────────────────────────

    /// <summary>
    /// Reads and writes W3C TraceContext headers plus Beep correlation headers.
    /// Implementations can delegate to System.Diagnostics.Activity or OpenTelemetry's
    /// <c>TextMapPropagator</c>.
    /// </summary>
    public interface IStreamContextPropagator
    {
        /// <summary>
        /// Writes W3C traceparent/tracestate and Beep correlation headers into <paramref name="carrier"/>.
        /// </summary>
        void Inject<T>(EventEnvelope<T> envelope, System.Collections.Generic.IDictionary<string, string> carrier);

        /// <summary>
        /// Reads headers from <paramref name="carrier"/> and returns a parsed <see cref="StreamContext"/>.
        /// </summary>
        StreamContext Extract(System.Collections.Generic.IReadOnlyDictionary<string, string> carrier);
    }
}
