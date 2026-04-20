using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Services.Telemetry.Sinks;

namespace TheTechIdea.Beep.Services.Logging.Query
{
    /// <summary>
    /// Streams NDJSON log files (raw and gzipped) and applies the
    /// supplied <see cref="LogQuery"/> in-process. Mirrors
    /// <c>FileScanAuditQueryEngine</c> but skips audit envelopes so the
    /// log query path stays free of audit data.
    /// </summary>
    public sealed class FileScanLogQueryEngine : ILogQueryEngine
    {
        private readonly string _directory;
        private readonly string _searchPattern;

        /// <summary>Creates an engine scoped to <paramref name="sink"/>.</summary>
        public FileScanLogQueryEngine(FileRollingSink sink)
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
        /// <paramref name="directory"/> and file
        /// <paramref name="searchPattern"/>.
        /// </summary>
        public FileScanLogQueryEngine(string directory, string searchPattern)
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
        public Task<IReadOnlyList<LogRecord>> ExecuteAsync(LogQuery query, CancellationToken cancellationToken = default)
        {
            return Task.Run(() => ExecuteCore(query ?? new LogQuery(), cancellationToken), cancellationToken);
        }

        private IReadOnlyList<LogRecord> ExecuteCore(LogQuery query, CancellationToken cancellationToken)
        {
            if (!System.IO.Directory.Exists(_directory))
            {
                return Array.Empty<LogRecord>();
            }

            string[] files;
            try
            {
                files = System.IO.Directory.GetFiles(_directory, _searchPattern);
            }
            catch
            {
                return Array.Empty<LogRecord>();
            }
            Array.Sort(files, (a, b) => File.GetLastWriteTimeUtc(b).CompareTo(File.GetLastWriteTimeUtc(a)));

            var results = new List<LogRecord>(Math.Max(16, query.Take));
            foreach (string file in files)
            {
                if (cancellationToken.IsCancellationRequested) { break; }
                if (!ScanFile(file, query, results, cancellationToken))
                {
                    break;
                }
            }

            results.Sort((a, b) =>
            {
                int cmp = a.TimestampUtc.CompareTo(b.TimestampUtc);
                return query.OrderDescending ? -cmp : cmp;
            });
            if (query.Take > 0 && results.Count > query.Take)
            {
                results.RemoveRange(query.Take, results.Count - query.Take);
            }
            return results;
        }

        private static bool ScanFile(string path, LogQuery query, List<LogRecord> results, CancellationToken cancellationToken)
        {
            try
            {
                using Stream stream = OpenRead(path);
                using var reader = new StreamReader(stream);
                string line;
                while ((line = reader.ReadLine()) is not null)
                {
                    if (cancellationToken.IsCancellationRequested) { return false; }
                    LogRecord record = NdjsonLogDeserializer.TryParse(line);
                    if (record is null || !query.Matches(record)) { continue; }
                    results.Add(record);
                    if (query.Take > 0 && results.Count >= query.Take * 2)
                    {
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
    }
}
