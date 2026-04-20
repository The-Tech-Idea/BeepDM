using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace TheTechIdea.Beep.Services.Telemetry.Context
{
    /// <summary>
    /// Ambient scope stack used by enrichers to attach trace/correlation ids
    /// to every <see cref="TelemetryEnvelope"/> produced inside a
    /// <c>using (BeepActivityScope.Begin(...))</c> block. Backed by an
    /// <see cref="System.Threading.AsyncLocal{T}"/> stack so the context flows
    /// across <c>await</c> boundaries.
    /// </summary>
    /// <remarks>
    /// When <see cref="Activity.Current"/> exists at <see cref="Begin"/> time
    /// the new scope inherits its <see cref="Activity.TraceId"/> and treats
    /// the activity's <see cref="Activity.SpanId"/> as its parent. This keeps
    /// Beep telemetry correlated with any framework already producing OTel
    /// spans (ASP.NET Core, gRPC, HttpClient, etc.).
    /// </remarks>
    public static partial class BeepActivityScope
    {
        /// <summary>
        /// Opens a new scope. Dispose the returned handle to pop the scope.
        /// Returns a no-op handle when <paramref name="name"/> is null/empty
        /// so callers can safely guard with <c>using</c> regardless of whether
        /// the operation actually deserves a scope.
        /// </summary>
        /// <param name="name">Operation/scope name (e.g. <c>"Order.Submit"</c>).</param>
        /// <param name="tags">Optional scope-local tags.</param>
        public static IDisposable Begin(string name, IDictionary<string, object> tags = null)
        {
            if (string.IsNullOrEmpty(name))
            {
                return NoopScope.Instance;
            }

            BeepActivity parent = Current;
            BeepActivity activity = CreateActivity(name, parent, tags);
            Push(activity);
            return new ScopeHandle(activity);
        }

        /// <summary>
        /// Returns the current activity, or <c>null</c> if no scope is open
        /// on this async-local context.
        /// </summary>
        public static BeepActivity Current
        {
            get { return Peek(); }
        }

        /// <summary>True when no scope is currently open on this context.</summary>
        public static bool IsEmpty
        {
            get { return Peek() is null; }
        }

        private static BeepActivity CreateActivity(string name, BeepActivity parent, IDictionary<string, object> tags)
        {
            string traceId;
            string parentSpanId;

            Activity ambient = Activity.Current;
            if (ambient != null && ambient.IdFormat == ActivityIdFormat.W3C)
            {
                traceId = ambient.TraceId.ToHexString();
                parentSpanId = ambient.SpanId.ToHexString();
            }
            else if (parent != null)
            {
                traceId = parent.TraceId;
                parentSpanId = parent.SpanId;
            }
            else
            {
                traceId = IdGenerators.NewTraceId();
                parentSpanId = null;
            }

            return new BeepActivity
            {
                Name = name,
                TraceId = traceId,
                SpanId = IdGenerators.NewSpanId(),
                ParentSpanId = parentSpanId,
                StartUtc = DateTime.UtcNow,
                Tags = tags
            };
        }

        private sealed class NoopScope : IDisposable
        {
            public static readonly NoopScope Instance = new NoopScope();
            public void Dispose() { }
        }

        private sealed class ScopeHandle : IDisposable
        {
            private readonly BeepActivity _activity;
            private int _disposed;

            public ScopeHandle(BeepActivity activity)
            {
                _activity = activity;
            }

            public void Dispose()
            {
                if (System.Threading.Interlocked.Exchange(ref _disposed, 1) != 0)
                {
                    return;
                }
                Pop(_activity);
            }
        }
    }
}
