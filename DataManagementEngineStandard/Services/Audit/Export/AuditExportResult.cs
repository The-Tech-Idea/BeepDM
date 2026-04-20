using System;

namespace TheTechIdea.Beep.Services.Audit.Export
{
    /// <summary>
    /// Container returned by <see cref="AuditExporter.ExportAsync"/>.
    /// Bundles the rendered payload bytes with the signed
    /// <see cref="ExportManifest"/> so callers can persist or stream
    /// both atomically.
    /// </summary>
    public sealed class AuditExportResult
    {
        /// <summary>Creates a new immutable result.</summary>
        public AuditExportResult(byte[] payload, ExportManifest manifest)
        {
            Payload = payload ?? throw new ArgumentNullException(nameof(payload));
            Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
        }

        /// <summary>Rendered payload bytes (NDJSON, JSON, or CSV).</summary>
        public byte[] Payload { get; }

        /// <summary>Signed manifest describing the payload.</summary>
        public ExportManifest Manifest { get; }
    }
}
