using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Services.Telemetry.Context
{
    /// <summary>
    /// Lightweight activity record pushed onto <see cref="BeepActivityScope"/>.
    /// Mirrors the W3C trace context shape so we can hand off to a real
    /// <see cref="System.Diagnostics.Activity"/> (and any OTel exporter) later
    /// without translating identifiers.
    /// </summary>
    /// <remarks>
    /// Identifiers are stored as lowercase hex strings — 32 chars for
    /// <see cref="TraceId"/> and 16 chars for <see cref="SpanId"/>/<see cref="ParentSpanId"/>
    /// — to match the W3C TraceContext format. Tags are an optional bag of
    /// scope-local properties surfaced by <see cref="ActivityScopeEnricher"/>.
    /// </remarks>
    public sealed class BeepActivity
    {
        /// <summary>Caller-supplied scope name (e.g. <c>"Order.Submit"</c>).</summary>
        public string Name { get; set; }

        /// <summary>Lowercase hex 16-byte trace id (W3C TraceContext).</summary>
        public string TraceId { get; set; }

        /// <summary>Lowercase hex 8-byte span id (W3C TraceContext).</summary>
        public string SpanId { get; set; }

        /// <summary>Span id of the parent activity, when nested.</summary>
        public string ParentSpanId { get; set; }

        /// <summary>UTC start timestamp captured when the scope opened.</summary>
        public DateTime StartUtc { get; set; }

        /// <summary>Optional scope-local tags surfaced by enrichers.</summary>
        public IDictionary<string, object> Tags { get; set; }
    }
}
