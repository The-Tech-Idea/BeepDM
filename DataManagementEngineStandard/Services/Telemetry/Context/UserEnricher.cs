using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Services.Telemetry.Context
{
    /// <summary>
    /// Stamps <c>userId</c> and <c>userName</c> onto every envelope using
    /// caller-supplied resolvers. Either resolver may be omitted; the
    /// enricher writes only the fields it has values for.
    /// </summary>
    /// <remarks>
    /// Resolvers run on the producer thread. To stay cross-platform the
    /// enricher does not touch <see cref="System.Security.Principal.WindowsIdentity"/>
    /// or any other OS-specific principal — those bindings live in the
    /// Phase 09 bridges.
    /// </remarks>
    public sealed class UserEnricher : IEnricher
    {
        private readonly Func<string> _userIdResolver;
        private readonly Func<string> _userNameResolver;

        /// <summary>Creates a user enricher with optional id and name resolvers.</summary>
        public UserEnricher(Func<string> userIdResolver = null, Func<string> userNameResolver = null)
        {
            _userIdResolver = userIdResolver;
            _userNameResolver = userNameResolver;
        }

        /// <inheritdoc/>
        public string Name => "user";

        /// <inheritdoc/>
        public void Enrich(TelemetryEnvelope envelope)
        {
            if (envelope is null)
            {
                return;
            }
            string userId = SafeInvoke(_userIdResolver);
            string userName = SafeInvoke(_userNameResolver);
            if (string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(userName))
            {
                return;
            }

            if (envelope.Properties is null)
            {
                envelope.Properties = new Dictionary<string, object>();
            }

            if (!string.IsNullOrEmpty(userId) && !envelope.Properties.ContainsKey(EnrichmentProperties.UserId))
            {
                envelope.Properties[EnrichmentProperties.UserId] = userId;
            }
            if (!string.IsNullOrEmpty(userName) && !envelope.Properties.ContainsKey(EnrichmentProperties.UserName))
            {
                envelope.Properties[EnrichmentProperties.UserName] = userName;
            }
        }

        private static string SafeInvoke(Func<string> resolver)
        {
            if (resolver is null)
            {
                return null;
            }
            try
            {
                return resolver();
            }
            catch
            {
                return null;
            }
        }
    }
}
