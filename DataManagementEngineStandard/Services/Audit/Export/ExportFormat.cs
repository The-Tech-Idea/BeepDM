namespace TheTechIdea.Beep.Services.Audit.Export
{
    /// <summary>
    /// Output format requested from <see cref="AuditExporter"/>.
    /// CSV is intended for spreadsheet operators; NDJSON is intended
    /// for downstream pipelines (SIEM, Elastic, etc.); JSON wraps the
    /// payload + manifest in a single object suitable for archival.
    /// </summary>
    public enum ExportFormat
    {
        /// <summary>Newline-delimited JSON; one canonical event per line.</summary>
        Ndjson = 0,

        /// <summary>Comma-separated values with a flat header row.</summary>
        Csv = 1,

        /// <summary>Single JSON document <c>{ "events": [...], "manifest": {...} }</c>.</summary>
        Json = 2
    }
}
