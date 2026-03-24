namespace TheTechIdea.Beep.Pipelines.Models
{
    /// <summary>
    /// Data sensitivity classification applied to a pipeline or individual fields.
    /// Drives masking, retention, and access-control behaviour in the security engine.
    /// </summary>
    public enum DataClassification
    {
        /// <summary>No restrictions — data may be logged and exposed freely.</summary>
        Public,
        /// <summary>Internal use only — may appear in logs but not external outputs.</summary>
        Internal,
        /// <summary>Contains business-sensitive data — mask in logs, restrict access.</summary>
        Confidential,
        /// <summary>Regulated PII / PCI / PHI — mandatory masking and audit trail.</summary>
        Restricted
    }

    /// <summary>
    /// Strategy used to redact or mask sensitive field values in logs and snapshots.
    /// </summary>
    public enum MaskingStrategy
    {
        /// <summary>No masking — value logged as-is.</summary>
        None,
        /// <summary>Replace value with "***REDACTED***".</summary>
        Redact,
        /// <summary>Replace value with a one-way SHA-256 hash.</summary>
        Hash,
        /// <summary>Show first and last character with asterisks in between.</summary>
        Partial
    }
}
