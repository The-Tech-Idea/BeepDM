using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Services.Audit.Models;
using TheTechIdea.Beep.Services.Audit.Query;

namespace TheTechIdea.Beep.Services.Audit.Export
{
    /// <summary>
    /// Materializes the result of an <see cref="AuditQuery"/> as an
    /// export bundle (payload file + signed manifest). Sealed and
    /// partial — the format-specific writers live in
    /// <c>.Ndjson</c>, <c>.Json</c>, and <c>.Csv</c>.
    /// </summary>
    /// <remarks>
    /// The exporter never writes directly to operator storage — it
    /// hands the payload back as a <see cref="byte"/> array plus a
    /// matching <see cref="ExportManifest"/>. Callers persist the
    /// pair atomically so the manifest cannot drift away from the
    /// payload it certifies.
    /// </remarks>
    public sealed partial class AuditExporter
    {
        private readonly IAuditQueryEngine _queryEngine;
        private readonly ManifestSigner _manifestSigner;

        /// <summary>
        /// Creates an exporter over the supplied query engine and
        /// manifest signer.
        /// </summary>
        public AuditExporter(IAuditQueryEngine queryEngine, ManifestSigner manifestSigner)
        {
            _queryEngine = queryEngine ?? throw new ArgumentNullException(nameof(queryEngine));
            _manifestSigner = manifestSigner ?? throw new ArgumentNullException(nameof(manifestSigner));
        }

        /// <summary>
        /// Runs <paramref name="query"/> against the configured engine,
        /// renders the result in the requested <paramref name="format"/>,
        /// stamps and signs the manifest.
        /// </summary>
        public async Task<AuditExportResult> ExportAsync(
            AuditQuery query,
            ExportFormat format,
            string operatorId,
            string notes,
            CancellationToken cancellationToken = default)
        {
            if (query is null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            IReadOnlyList<AuditEvent> events =
                await _queryEngine.ExecuteAsync(query, cancellationToken).ConfigureAwait(false);

            byte[] payload;
            switch (format)
            {
                case ExportFormat.Ndjson:
                    payload = WriteNdjson(events);
                    break;
                case ExportFormat.Json:
                    payload = WriteJson(events);
                    break;
                case ExportFormat.Csv:
                    payload = WriteCsv(events);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, null);
            }

            ExportManifest manifest = BuildManifest(events, format, operatorId, notes, payload);
            _manifestSigner.Sign(manifest);
            return new AuditExportResult(payload, manifest);
        }

        /// <summary>
        /// Writes <paramref name="result"/> atomically to
        /// <paramref name="payloadPath"/> + <c>.manifest.json</c>. The
        /// payload is written to a <c>.tmp</c> first, then renamed so
        /// crashes never leave a partial file behind.
        /// </summary>
        public static void WriteToFiles(AuditExportResult result, string payloadPath)
        {
            if (result is null)
            {
                throw new ArgumentNullException(nameof(result));
            }
            if (string.IsNullOrEmpty(payloadPath))
            {
                throw new ArgumentNullException(nameof(payloadPath));
            }

            string dir = System.IO.Path.GetDirectoryName(payloadPath);
            if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }

            string tmp = payloadPath + ".tmp";
            File.WriteAllBytes(tmp, result.Payload);
            if (File.Exists(payloadPath))
            {
                File.Delete(payloadPath);
            }
            File.Move(tmp, payloadPath);

            string manifestPath = payloadPath + ".manifest.json";
            byte[] manifestBytes = WriteManifestJson(result.Manifest);
            string manifestTmp = manifestPath + ".tmp";
            File.WriteAllBytes(manifestTmp, manifestBytes);
            if (File.Exists(manifestPath))
            {
                File.Delete(manifestPath);
            }
            File.Move(manifestTmp, manifestPath);
        }

        private static ExportManifest BuildManifest(
            IReadOnlyList<AuditEvent> events,
            ExportFormat format,
            string operatorId,
            string notes,
            byte[] payload)
        {
            DateTime? fromUtc = null;
            DateTime? toUtc = null;
            var chains = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < events.Count; i++)
            {
                AuditEvent ev = events[i];
                if (ev is null) { continue; }
                if (!fromUtc.HasValue || ev.TimestampUtc < fromUtc.Value)
                {
                    fromUtc = ev.TimestampUtc;
                }
                if (!toUtc.HasValue || ev.TimestampUtc > toUtc.Value)
                {
                    toUtc = ev.TimestampUtc;
                }
                chains.Add(string.IsNullOrEmpty(ev.ChainId) ? AuditEvent.DefaultChainId : ev.ChainId);
            }

            var manifest = new ExportManifest
            {
                Version = 1,
                CreatedUtc = DateTime.UtcNow,
                OperatorId = string.IsNullOrEmpty(operatorId) ? "unknown" : operatorId,
                Format = format.ToString().ToLowerInvariant(),
                EventCount = events.Count,
                FromUtc = fromUtc,
                ToUtc = toUtc,
                Notes = notes,
                PayloadSha256 = ManifestSigner.ComputePayloadSha256(payload)
            };
            foreach (string chain in chains)
            {
                manifest.ChainIds.Add(chain);
            }
            return manifest;
        }
    }
}
