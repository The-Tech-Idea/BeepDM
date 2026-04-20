using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Services.Telemetry.Sinks
{
    /// <summary>
    /// Cross-platform NDJSON file sink that rolls by size and / or wall
    /// clock. Phase 03 owns write + roll only; compression and retention
    /// run from Phase 04 by subscribing to <see cref="Rolled"/>.
    /// </summary>
    /// <remarks>
    /// Split into three partial files:
    /// <list type="bullet">
    ///   <item><c>.Core</c> — fields, ctor, dispose, public state.</item>
    ///   <item><c>.Write</c> — batch append + flush to disk.</item>
    ///   <item><c>.Roll</c> — rotation policy + <see cref="Rolled"/> event.</item>
    /// </list>
    /// Only one open file handle is held at a time per sink instance, so
    /// the sink is safe for hosts with strict file-descriptor budgets
    /// (mobile / containers).
    /// </remarks>
    public sealed partial class FileRollingSink : ITelemetrySink
    {
        /// <summary>Default rotation cap (10 MB) when none is supplied.</summary>
        public const long DefaultMaxFileBytes = 10L * 1024 * 1024;

        /// <summary>Default rotation interval (60 minutes) when none is supplied.</summary>
        public static readonly TimeSpan DefaultRollInterval = TimeSpan.FromMinutes(60);

        private readonly object _writeGate = new();
        private readonly string _directory;
        private readonly string _prefix;
        private readonly string _extension;
        private readonly long _maxFileBytes;
        private readonly TimeSpan _rollInterval;

        private FileStream _stream;
        private string _currentPath;
        private DateTime _currentOpenedUtc;
        private long _currentBytes;
        private long _writtenCount;
        private long _rolledCount;
        private bool _healthy = true;
        private string _lastError;
        private int _disposed;

        /// <summary>
        /// Creates a sink that writes to <paramref name="directory"/> using
        /// the supplied filename <paramref name="prefix"/>.
        /// </summary>
        public FileRollingSink(
            string directory,
            string prefix = "beep",
            string extension = ".ndjson",
            long maxFileBytes = DefaultMaxFileBytes,
            TimeSpan? rollInterval = null,
            string name = "file")
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new ArgumentException("Directory must be supplied.", nameof(directory));
            }
            if (maxFileBytes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxFileBytes), "maxFileBytes must be positive.");
            }

            Name = string.IsNullOrWhiteSpace(name) ? "file" : name;
            _directory = directory;
            _prefix = PlatformPaths.SanitizeFileName(prefix, "beep");
            _extension = string.IsNullOrWhiteSpace(extension) ? ".ndjson" : extension;
            _maxFileBytes = maxFileBytes;
            _rollInterval = rollInterval ?? DefaultRollInterval;

            try
            {
                if (!System.IO.Directory.Exists(_directory))
                {
                    System.IO.Directory.CreateDirectory(_directory);
                }
            }
            catch (Exception ex)
            {
                _healthy = false;
                Volatile.Write(ref _lastError, ex.Message);
            }
        }

        /// <inheritdoc />
        public string Name { get; }

        /// <summary>Directory the sink writes files into.</summary>
        public string Directory => _directory;

        /// <summary>Filename prefix used when opening a fresh file.</summary>
        public string Prefix => _prefix;

        /// <summary>File-extension suffix (default <c>.ndjson</c>).</summary>
        public string Extension => _extension;

        /// <summary>
        /// File-name search pattern that matches both raw and gzipped
        /// siblings produced by this sink. Suitable for use as the
        /// <c>FilePattern</c> on a Phase 04 enforcer scope.
        /// </summary>
        public string FilePattern => string.Concat(_prefix, "-*", _extension, "*");

        /// <summary>Configured size cap before rotation.</summary>
        public long MaxFileBytes => _maxFileBytes;

        /// <summary>Configured wall-clock interval before rotation.</summary>
        public TimeSpan RollInterval => _rollInterval;

        /// <inheritdoc />
        public bool IsHealthy => Volatile.Read(ref _healthy) ? true : false;

        /// <summary>Total envelopes written since startup.</summary>
        public long WrittenCount => Interlocked.Read(ref _writtenCount);

        /// <summary>Total file rotations performed since startup.</summary>
        public long RolledCount => Interlocked.Read(ref _rolledCount);

        /// <summary>
        /// Path of the file currently being appended to (may be <c>null</c>
        /// before the first write or immediately after a rotation).
        /// </summary>
        public string CurrentPath
        {
            get
            {
                lock (_writeGate)
                {
                    return _currentPath;
                }
            }
        }

        /// <summary>Most recent IO error message, or <c>null</c>.</summary>
        public string LastError => Volatile.Read(ref _lastError);

        /// <summary>
        /// Raised after a file is closed and rotated. Phase 04
        /// (compression / retention) subscribes here to compress and to
        /// enforce the directory budget.
        /// </summary>
        public event Action<RolledFile> Rolled;

        /// <inheritdoc />
        public Task FlushAsync(CancellationToken cancellationToken)
        {
            lock (_writeGate)
            {
                FlushUnderLock();
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return default;
            }

            lock (_writeGate)
            {
                CloseUnderLock(reasonSuffix: "dispose");
            }
            return default;
        }
    }
}
