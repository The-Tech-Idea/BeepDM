using TheTechIdea.Beep.Proxy;

namespace TheTechIdea.Beep.Services.Telemetry.Redaction
{
    /// <summary>
    /// Bridge that lets the unified telemetry pipeline reuse the legacy
    /// <see cref="ProxyLogRedactor"/> (internal to the engine) without
    /// duplicating its compiled regex set. New code should prefer the
    /// dedicated <see cref="ConnectionStringRedactor"/>,
    /// <see cref="EmailRedactor"/>, <see cref="CreditCardRedactor"/> family;
    /// this adapter exists so existing proxy callers can opt into the
    /// pipeline incrementally.
    /// </summary>
    /// <remarks>
    /// The underlying <see cref="ProxyLogRedactor"/> always replaces matches
    /// with the literal token <c>[REDACTED]</c>; this adapter ignores the
    /// supplied <see cref="RedactionContext"/> beyond surfacing it for
    /// diagnostics. Use one of the typed redactors when you need
    /// <see cref="RedactionMode.Hash"/> or <see cref="RedactionMode.Drop"/>.
    /// </remarks>
    public sealed class ProxyRedactorAdapter : IRedactor
    {
        /// <summary>Creates an adapter around <see cref="ProxyLogRedactor"/>.</summary>
        public ProxyRedactorAdapter()
        {
        }

        /// <inheritdoc/>
        public string Name => "proxy-bridge";

        /// <inheritdoc/>
        public void Redact(TelemetryEnvelope envelope)
        {
            if (envelope is null)
            {
                return;
            }
            if (!string.IsNullOrEmpty(envelope.Message))
            {
                envelope.Message = ProxyLogRedactor.Redact(envelope.Message);
            }
            if (envelope.Exception != null)
            {
                string redactedException = ProxyLogRedactor.RedactException(envelope.Exception);
                if (!string.IsNullOrEmpty(redactedException))
                {
                    if (envelope.Properties is null)
                    {
                        envelope.Properties = new System.Collections.Generic.Dictionary<string, object>();
                    }
                    envelope.Properties["ExceptionMessageRedacted"] = redactedException;
                }
            }
        }
    }
}
