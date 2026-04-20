using System;
using System.Threading;

namespace TheTechIdea.Beep.Services.Telemetry.Sinks
{
    /// <summary>
    /// Rotation half of <see cref="FileRollingSink"/>. Decides when to
    /// roll, performs the swap, and surfaces the closed file via the
    /// <see cref="FileRollingSink.Rolled"/> event so Phase 04 can compress
    /// it and enforce retention.
    /// </summary>
    public sealed partial class FileRollingSink
    {
        /// <summary>
        /// Forces an immediate rotation. Safe to call any time; if no
        /// file is currently open it is a no-op.
        /// </summary>
        public void RollNow(string reason = "manual")
        {
            lock (_writeGate)
            {
                if (_stream is null)
                {
                    return;
                }
                RollUnderLock(string.IsNullOrWhiteSpace(reason) ? "manual" : reason);
            }
        }

        private bool ShouldRollUnderLock()
        {
            if (_stream is null)
            {
                return false;
            }

            if (_currentBytes >= _maxFileBytes)
            {
                return true;
            }

            if (_rollInterval > TimeSpan.Zero
                && DateTime.UtcNow - _currentOpenedUtc >= _rollInterval)
            {
                return true;
            }

            return false;
        }

        private void RollUnderLock(string reasonSuffix)
        {
            CloseUnderLock(reasonSuffix);
            // Next write triggers a fresh open via EnsureOpenUnderLock.
        }

        private void CloseUnderLock(string reasonSuffix)
        {
            if (_stream is null)
            {
                return;
            }

            string closedPath = _currentPath;
            DateTime openedUtc = _currentOpenedUtc;
            long bytes = _currentBytes;

            try
            {
                _stream.Flush(flushToDisk: true);
            }
            catch
            {
                // Best-effort flush before close.
            }

            try
            {
                _stream.Dispose();
            }
            catch
            {
                // Best-effort close; we still raise Rolled below so Phase 04
                // can attempt to consume the file even if the handle close
                // surfaces a transient error.
            }

            _stream = null;
            _currentPath = null;
            _currentBytes = 0;
            Interlocked.Increment(ref _rolledCount);

            try
            {
                Rolled?.Invoke(new RolledFile(
                    sinkName: Name,
                    path: closedPath,
                    openedUtc: openedUtc,
                    closedUtc: DateTime.UtcNow,
                    bytes: bytes,
                    reason: reasonSuffix));
            }
            catch (Exception ex)
            {
                Volatile.Write(ref _lastError, ex.Message);
            }
        }
    }
}
