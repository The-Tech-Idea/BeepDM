using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Services.Telemetry.Redaction
{
    /// <summary>
    /// Bundles several <see cref="IRedactor"/>s into a single ordered chain.
    /// Lets operators pre-build a named preset (e.g. <c>"AuditStrict"</c>)
    /// and pass it in any place a single redactor is expected.
    /// </summary>
    /// <remarks>
    /// Inner redactors run in registration order. Exceptions thrown by one
    /// redactor never short-circuit the chain — they are swallowed so a
    /// faulty plugin cannot disable the entire scrubbing layer. The
    /// pipeline-level guard in <see cref="TelemetryPipeline"/> still wraps
    /// each call, so the only state that crosses the boundary is the
    /// envelope itself.
    /// </remarks>
    public sealed class CompositeRedactor : IRedactor
    {
        private readonly IReadOnlyList<IRedactor> _inner;

        /// <summary>Creates a composite redactor with the supplied chain.</summary>
        public CompositeRedactor(string name, IEnumerable<IRedactor> inner)
        {
            if (inner is null)
            {
                throw new ArgumentNullException(nameof(inner));
            }
            Name = string.IsNullOrEmpty(name) ? "composite" : name;
            var list = new List<IRedactor>();
            foreach (var redactor in inner)
            {
                if (redactor != null)
                {
                    list.Add(redactor);
                }
            }
            _inner = list;
        }

        /// <inheritdoc/>
        public string Name { get; }

        /// <summary>Inner redactors, in execution order.</summary>
        public IReadOnlyList<IRedactor> Inner => _inner;

        /// <inheritdoc/>
        public void Redact(TelemetryEnvelope envelope)
        {
            if (envelope is null)
            {
                return;
            }
            for (int i = 0; i < _inner.Count; i++)
            {
                try
                {
                    _inner[i].Redact(envelope);
                }
                catch
                {
                    // A single bad redactor must not disable the chain.
                }
            }
        }
    }
}
