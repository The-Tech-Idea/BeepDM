using System;
using System.Text;

namespace TheTechIdea.Beep.Services.Audit.Integrity
{
    /// <summary>
    /// Reads the HMAC secret from an environment variable
    /// (default <c>BEEP_AUDIT_HMAC_SECRET</c>). Returns a documented
    /// fallback secret only when the variable is missing — the fallback
    /// is intentionally weak so misconfiguration is visible without
    /// breaking development workflows.
    /// </summary>
    /// <remarks>
    /// Production hosts should set the environment variable to at least
    /// 32 bytes of random material (e.g. <c>openssl rand -hex 32</c>)
    /// and rotate per environment. The fallback secret is logged once
    /// at startup by Phase 11 self-observability.
    /// </remarks>
    public sealed class EnvironmentKeyMaterialProvider : IKeyMaterialProvider
    {
        /// <summary>Default environment variable consulted by the provider.</summary>
        public const string DefaultVariableName = "BEEP_AUDIT_HMAC_SECRET";

        /// <summary>
        /// Fallback secret used only when the environment variable is
        /// missing. Documented so operators can recognise it in tests.
        /// </summary>
        public const string DevelopmentFallbackSecret = "beep-dev-audit-secret-do-not-use-in-prod";

        private readonly string _variableName;

        /// <summary>Creates a provider reading from the supplied variable.</summary>
        public EnvironmentKeyMaterialProvider(string variableName = DefaultVariableName)
        {
            _variableName = string.IsNullOrWhiteSpace(variableName) ? DefaultVariableName : variableName;
        }

        /// <summary>Configured environment variable name.</summary>
        public string VariableName => _variableName;

        /// <summary>
        /// Returns <c>true</c> if the environment variable is set and
        /// non-empty (i.e. the production secret is in use). Useful for
        /// the Phase 11 startup probe.
        /// </summary>
        public bool UsingProductionSecret
        {
            get
            {
                string value = Environment.GetEnvironmentVariable(_variableName);
                return !string.IsNullOrEmpty(value);
            }
        }

        /// <inheritdoc/>
        public byte[] GetHmacKey()
        {
            string value = Environment.GetEnvironmentVariable(_variableName);
            if (string.IsNullOrEmpty(value))
            {
                value = DevelopmentFallbackSecret;
            }
            return Encoding.UTF8.GetBytes(value);
        }
    }
}
