using System;

namespace TheTechIdea.Beep.Services.Telemetry.Sinks
{
    /// <summary>
    /// Payload raised by <see cref="FileRollingSink.Rolled"/> when the
    /// active log file is closed and replaced. Phase 04 consumes this to
    /// schedule compression and to enforce the per-directory storage
    /// budget.
    /// </summary>
    public sealed class RolledFile
    {
        /// <summary>Sink name that produced the rotation.</summary>
        public string SinkName { get; }

        /// <summary>Absolute path to the file that was just closed.</summary>
        public string Path { get; }

        /// <summary>UTC timestamp the closed file was opened.</summary>
        public DateTime OpenedUtc { get; }

        /// <summary>UTC timestamp at the moment of rotation.</summary>
        public DateTime ClosedUtc { get; }

        /// <summary>Total bytes written to the file before rotation.</summary>
        public long Bytes { get; }

        /// <summary>Reason suffix (size, time, dispose, manual).</summary>
        public string Reason { get; }

        /// <summary>Creates a rotation event payload.</summary>
        public RolledFile(string sinkName, string path, DateTime openedUtc, DateTime closedUtc, long bytes, string reason)
        {
            SinkName = sinkName;
            Path = path;
            OpenedUtc = openedUtc;
            ClosedUtc = closedUtc;
            Bytes = bytes;
            Reason = reason;
        }
    }
}
