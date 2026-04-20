using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Services.Audit.Models;
using TheTechIdea.Beep.Services.Telemetry.Sinks;

namespace TheTechIdea.Beep.Services.Audit.Query
{
    /// <summary>
    /// Streams NDJSON audit files (raw and gzipped) and applies the
    /// supplied <see cref="AuditQuery"/> in-process. Used as the v1
    /// fallback when no <see cref="SqliteSink"/> is configured.
    /// </summary>
    /// <remarks>
    /// Performance is bounded by IO; the engine reads files newest-first
    /// (mtime descending) and stops as soon as <see cref="AuditQuery.Take"/>
    /// matches accumulate. For production deployments operators are
    /// strongly advised to add a <see cref="SqliteSink"/> alongside the
    /// file sink so the SQLite engine handles ad-hoc queries.
    /// </remarks>
    public sealed class FileScanAuditQueryEngine : IAuditQueryEngine
    {
        private readonly string _directory;
        private readonly string _searchPattern;

        /// <summary>Creates an engine scoped to <paramref name="sink"/>.</summary>
        public FileScanAuditQueryEngine(FileRollingSink sink)
        {
            if (sink is null)
            {
                throw new ArgumentNullException(nameof(sink));
            }
            _directory = sink.Directory;
            _searchPattern = sink.FilePattern;
        }

        /// <summary>
        /// Creates an engine scoped to an explicit
        /// <paramref name="directory"/> and file <paramref name="searchPattern"/>.
        /// </summary>
        public FileScanAuditQueryEngine(string directory, string searchPattern)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new ArgumentException("Directory must be supplied.", nameof(directory));
            }
            _directory = directory;
            _searchPattern = string.IsNullOrWhiteSpace(searchPattern) ? "*.ndjson*" : searchPattern;
        }

        /// <summary>Directory the engine scans.</summary>
        public string Directory => _directory;

        /// <summary>Filename search pattern (matches raw and gzipped siblings).</summary>
        public string SearchPattern => _searchPattern;

        /// <inheritdoc />
        public Task<IReadOnlyList<AuditEvent>> ExecuteAsync(
            AuditQuery query,
            CancellationToken cancellationToken = default)
        {
            return Task.Run(() => ExecuteCore(query ?? new AuditQuery(), cancellationToken), cancellationToken);
        }

        private IReadOnlyList<AuditEvent> ExecuteCore(AuditQuery query, CancellationToken cancellationToken)
        {
            if (!System.IO.Directory.Exists(_directory))
            {
                return Array.Empty<AuditEvent>();
            }

            string[] files;
            try
            {
                files = System.IO.Directory.GetFiles(_directory, _searchPattern);
            }
            catch
            {
                return Array.Empty<AuditEvent>();
            }
            Array.Sort(files, (a, b) => File.GetLastWriteTimeUtc(b).CompareTo(File.GetLastWriteTimeUtc(a)));

            var results = new List<AuditEvent>(Math.Max(16, query.Take));
            foreach (string file in files)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                if (!ScanFile(file, query, results, cancellationToken))
                {
                    break;
                }
            }

            results.Sort((a, b) => CompareForOrder(a, b, query));
            if (query.Take > 0 && results.Count > query.Take)
            {
                results.RemoveRange(query.Take, results.Count - query.Take);
            }
            return results;
        }

        private static bool ScanFile(string path, AuditQuery query, List<AuditEvent> results, CancellationToken cancellationToken)
        {
            try
            {
                using Stream stream = OpenRead(path);
                using var reader = new StreamReader(stream);
                string line;
                while ((line = reader.ReadLine()) is not null)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return false;
                    }
                    AuditEvent ev = NdjsonAuditDeserializer.TryParse(line);
                    if (ev is null || !query.Matches(ev))
                    {
                        continue;
                    }
                    results.Add(ev);
                    if (query.Take > 0 && results.Count >= query.Take * 2)
                    {
                        // Read enough to give the post-sort pass room; keep
                        // bounded so we never stream a multi-GB file twice.
                        return true;
                    }
                }
            }
            catch
            {
                // Skip broken / locked files; the next file may still match.
            }
            return true;
        }

        private static Stream OpenRead(string path)
        {
            FileStream raw = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            if (path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
            {
                return new GZipStream(raw, CompressionMode.Decompress, leaveOpen: false);
            }
            return raw;
        }

        private static int CompareForOrder(AuditEvent a, AuditEvent b, AuditQuery query)
        {
            int cmp;
            switch ((query.OrderByField ?? "ts").ToLowerInvariant())
            {
                case "sequence":
                case "seq":
                    cmp = a.Sequence.CompareTo(b.Sequence);
                    break;
                case "user":
                case "user_id":
                    cmp = string.CompareOrdinal(a.UserId ?? string.Empty, b.UserId ?? string.Empty);
                    break;
                case "entity":
                case "entity_name":
                    cmp = string.CompareOrdinal(a.EntityName ?? string.Empty, b.EntityName ?? string.Empty);
                    break;
                default:
                    cmp = a.TimestampUtc.CompareTo(b.TimestampUtc);
                    break;
            }
            return query.OrderDescending ? -cmp : cmp;
        }
    }
}
