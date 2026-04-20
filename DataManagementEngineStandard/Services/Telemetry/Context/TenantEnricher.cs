using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Services.Telemetry.Context
{
    /// <summary>
    /// Stamps a <c>tenant</c> identifier onto every envelope. The value is
    /// resolved through a caller-supplied delegate so the enricher itself
    /// has no dependency on <see cref="IBeepService"/>; the
    /// Phase 09 bridges wire concrete resolvers (e.g. one that reads
    /// <c>BeepService.AppRepoName</c>).
    /// </summary>
    /// <remarks>
    /// The resolver is invoked on the producer thread and must therefore be
    /// cheap and side-effect free. Throwing resolvers are swallowed; the
    /// enricher never fails the envelope.
    /// </remarks>
    public sealed class TenantEnricher : IEnricher
    {
        private readonly Func<string> _resolver;

        /// <summary>Creates a tenant enricher with the supplied resolver.</summary>
        public TenantEnricher(Func<string> tenantResolver)
        {
            _resolver = tenantResolver ?? throw new ArgumentNullException(nameof(tenantResolver));
        }

        /// <inheritdoc/>
        public string Name => "tenant";

        /// <inheritdoc/>
        public void Enrich(TelemetryEnvelope envelope)
        {
            if (envelope is null)
            {
                return;
            }

            string tenant;
            try
            {
                tenant = _resolver();
            }
            catch
            {
                return;
            }
            if (string.IsNullOrEmpty(tenant))
            {
                return;
            }

            if (envelope.Properties is null)
            {
                envelope.Properties = new Dictionary<string, object>();
            }
            if (!envelope.Properties.ContainsKey(EnrichmentProperties.Tenant))
            {
                envelope.Properties[EnrichmentProperties.Tenant] = tenant;
            }
        }
    }
}
