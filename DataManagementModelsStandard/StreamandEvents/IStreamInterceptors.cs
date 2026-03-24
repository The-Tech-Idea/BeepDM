using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.StreamandEvents
{
    // ── Context ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Mutable context passed through every interceptor in a publish or consume pipeline.
    /// Headers modified here are applied back to the envelope/request before it reaches the broker.
    /// </summary>
    public interface IStreamInterceptorContext
    {
        /// <summary>Mutable headers carrier — interceptors read and write well-known keys here.</summary>
        IDictionary<string, string> Headers { get; }

        /// <summary>Arbitrary per-call bag for interceptor-to-interceptor communication.</summary>
        IReadOnlyDictionary<string, object> Metadata { get; }

        /// <summary>W3C <c>traceparent</c> value extracted from <see cref="Headers"/>, or null.</summary>
        string TraceId { get; }

        /// <summary>Stores a value in the mutable metadata bag.</summary>
        void SetMetadata(string key, object value);
    }

    // ── Producer interceptor ─────────────────────────────────────────────────

    /// <summary>
    /// Cross-cutting concern hook on the publish path.
    /// Interceptors are ordered by <see cref="Order"/> (ascending — lower runs first).
    /// </summary>
    public interface IProducerInterceptor
    {
        /// <summary>Execution order — lower values run first. Negative values run before built-ins.</summary>
        int Order { get; }

        /// <summary>
        /// Called before the envelope is forwarded to the broker.
        /// May mutate <paramref name="context"/>.Headers (e.g. inject traceparent, auth token).
        /// Throwing here aborts the publish.
        /// </summary>
        Task OnBeforePublishAsync(IStreamInterceptorContext context, CancellationToken cancellationToken);

        /// <summary>
        /// Called after the broker responds (success or failure).
        /// Suitable for audit logging and publish metrics.
        /// </summary>
        Task OnAfterPublishAsync(IStreamInterceptorContext context, PublishResult result, CancellationToken cancellationToken);
    }

    // ── Consumer interceptor ─────────────────────────────────────────────────

    /// <summary>
    /// Cross-cutting concern hook on the consume path.
    /// Interceptors are ordered by <see cref="Order"/> (ascending — lower runs first).
    /// </summary>
    public interface IConsumerInterceptor
    {
        /// <summary>Execution order — lower values run first. Negative values run before built-ins.</summary>
        int Order { get; }

        /// <summary>
        /// Called before the business handler is invoked.
        /// May enrich <paramref name="context"/> with span IDs, correlation IDs, or tenant context.
        /// Throwing here aborts handling and routes to retry/DLQ.
        /// </summary>
        Task OnBeforeHandleAsync(IStreamInterceptorContext context, CancellationToken cancellationToken);

        /// <summary>
        /// Called after the handler completes (regardless of success or failure).
        /// Suitable for metrics recording, tracing span completion, and audit writes.
        /// </summary>
        Task OnAfterHandleAsync(IStreamInterceptorContext context, EventProcessingResult result, CancellationToken cancellationToken);
    }

    // ── Context implementation ────────────────────────────────────────────────

    /// <summary>Concrete <see cref="IStreamInterceptorContext"/> populated from an envelope or broker message.</summary>
    public sealed class InterceptorPipelineContext : IStreamInterceptorContext
    {
        private readonly Dictionary<string, string> _headers;
        private readonly Dictionary<string, object> _metadata = new();

        public IDictionary<string, string> Headers => _headers;
        public IReadOnlyDictionary<string, object> Metadata => _metadata;

        public string TraceId =>
            _headers.TryGetValue(StreamHeaderNames.TraceParent, out var v) ? v : null;

        public InterceptorPipelineContext(Dictionary<string, string> headers)
        {
            _headers = headers ?? new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
        }

        public void SetMetadata(string key, object value) => _metadata[key] = value;

        /// <summary>Creates a context pre-populated from <paramref name="envelope"/> headers and metadata.</summary>
        public static InterceptorPipelineContext FromEnvelope<T>(EventEnvelope<T> envelope)
        {
            var headers = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
            if (envelope.Headers != null)
                foreach (var kv in envelope.Headers.All)
                    headers[kv.Key] = kv.Value;

            var ctx = new InterceptorPipelineContext(headers);
            ctx.SetMetadata("eventId",       envelope.EventId);
            ctx.SetMetadata("eventType",     envelope.EventType);
            ctx.SetMetadata("topic",         envelope.Topic);
            ctx.SetMetadata("schemaId",      envelope.SchemaId);
            ctx.SetMetadata("correlationId", envelope.CorrelationId);
            ctx.SetMetadata("causationId",   envelope.CausationId);
            ctx.SetMetadata("source",        envelope.Source);
            return ctx;
        }

        /// <summary>Creates a context pre-populated from a raw <see cref="BrokerMessage"/>.</summary>
        public static InterceptorPipelineContext FromBrokerMessage(BrokerMessage msg)
        {
            var headers = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
            if (msg.Headers != null)
                foreach (var kv in msg.Headers)
                    headers[kv.Key] = kv.Value;

            var ctx = new InterceptorPipelineContext(headers);
            ctx.SetMetadata("eventId", msg.EventId);
            ctx.SetMetadata("topic",   msg.Topic);
            return ctx;
        }

        /// <summary>
        /// Writes all headers from this context back into the envelope's mutable <see cref="EventHeaders"/> bag.
        /// Call this after interceptors have run to propagate modifications to the outgoing envelope.
        /// </summary>
        public void ApplyHeadersTo(EventHeaders target)
        {
            if (target == null) return;
            foreach (var kv in _headers)
                target.Set(kv.Key, kv.Value);
        }
    }
}
