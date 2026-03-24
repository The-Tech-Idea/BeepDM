using System;

namespace TheTechIdea.Beep.StreamandEvents
{
    /// <summary>Auth mode for broker connections.</summary>
    public enum BrokerAuthMode { None, UsernamePassword, Token, Certificate, Mtls }

    /// <summary>Broker authentication and authorization policy (model only).</summary>
    public sealed class BrokerAuthPolicy
    {
        public BrokerAuthMode AuthMode { get; init; } = BrokerAuthMode.None;

        /// <summary>Secret reference — never a literal value. Resolve via ISecretProvider.</summary>
        public string SecretReference { get; init; }

        /// <summary>Certificate thumbprint or path reference for mTLS.</summary>
        public string CertificateReference { get; init; }

        public bool RequireTlsTransport { get; init; } = true;

        /// <summary>Token audience (OIDC / OAuth2).</summary>
        public string Audience { get; init; }

        /// <summary>Token issuer endpoint.</summary>
        public string TokenEndpoint { get; init; }
    }

    /// <summary>PII / sensitive data classification tag for payload fields.</summary>
    public enum PayloadSensitivity { Public, Internal, Confidential, Restricted }

    /// <summary>Per-field payload encryption/masking policy.</summary>
    public sealed class PayloadFieldPolicy
    {
        public string FieldPath { get; init; }
        public PayloadSensitivity Sensitivity { get; init; } = PayloadSensitivity.Internal;
        public bool EncryptAtRest { get; init; }
        public bool MaskInLogs { get; init; } = true;

        /// <summary>"Mask", "Hash", "Encrypt", "Redact"</summary>
        public string TransformMode { get; init; } = "Mask";
    }

    /// <summary>Governance policy for a topic's payload content.</summary>
    public sealed class PayloadClassificationPolicy
    {
        public string TopicName { get; init; }
        public PayloadSensitivity MaxSensitivity { get; init; } = PayloadSensitivity.Internal;
        public PayloadFieldPolicy[] FieldPolicies { get; init; } = Array.Empty<PayloadFieldPolicy>();
        public bool AuditOnPublish { get; init; } = true;
        public bool AuditOnConsume { get; init; } = false;
    }

    /// <summary>Immutable audit record for publish/consume/admin actions.</summary>
    public sealed class StreamAuditRecord
    {
        public string AuditId { get; init; } = Guid.NewGuid().ToString();
        public string Action { get; init; } // Publish | Consume | AdminCreate | AdminDelete
        public string Topic { get; init; }
        public string EventId { get; init; }
        public string Actor { get; init; }
        public string ClientId { get; init; }
        public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
        public bool Success { get; init; }
        public string FailureReason { get; init; }
    }
}
