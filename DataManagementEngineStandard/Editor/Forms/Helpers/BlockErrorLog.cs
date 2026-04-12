using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;

namespace TheTechIdea.Beep.Editor.UOWManager.Helpers
{
    /// <summary>
    /// Per-block error and warning log with FIFO eviction and event propagation.
    /// Platform-agnostic — no MessageBox. Subscribers handle display.
    /// </summary>
    public class BlockErrorLog : IBlockErrorLog
    {
        private readonly Dictionary<string, List<BlockErrorInfo>> _logs
            = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets or sets whether logging should avoid raising error and warning events.
        /// </summary>
        public bool SuppressErrorEvents { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of entries retained per block before FIFO eviction occurs.
        /// </summary>
        public int MaxLogSize { get; set; } = 100;

        /// <summary>
        /// Raised when an error entry is logged.
        /// </summary>
        public event EventHandler<BlockErrorEventArgs> OnError;

        /// <summary>
        /// Raised when a warning entry is logged.
        /// </summary>
        public event EventHandler<BlockErrorEventArgs> OnWarning;

        #region Logging

        /// <summary>
        /// Logs an error or warning entry for a block.
        /// </summary>
        public void LogError(
            string blockName,
            Exception ex,
            string context,
            ErrorSeverity severity = ErrorSeverity.Error)
        {
            var info = new BlockErrorInfo
            {
                BlockName = blockName,
                Context = context,
                Severity = severity,
                Message = ex?.Message ?? string.Empty,
                Exception = ex,
                Timestamp = DateTime.UtcNow
            };

            AddToLog(blockName, info);

            if (SuppressErrorEvents) return;

            var args = new BlockErrorEventArgs { ErrorInfo = info };

            if (severity == ErrorSeverity.Warning)
                OnWarning?.Invoke(this, args);
            else
                OnError?.Invoke(this, args);
        }

        /// <summary>
        /// Logs a warning entry for a block.
        /// </summary>
        public void LogWarning(string blockName, string message, string context)
        {
            LogError(blockName, new InvalidOperationException(message), context, ErrorSeverity.Warning);
        }

        #endregion

        #region Query

        /// <summary>
        /// Clears the stored log for a single block.
        /// </summary>
        public void ClearErrorLog(string blockName)
        {
            if (_logs.ContainsKey(blockName))
                _logs[blockName].Clear();
        }

        /// <summary>
        /// Clears the stored logs for all blocks.
        /// </summary>
        public void ClearAllLogs()
        {
            _logs.Clear();
        }

        /// <summary>
        /// Returns the complete retained log for a block.
        /// </summary>
        public IReadOnlyList<BlockErrorInfo> GetErrorLog(string blockName)
        {
            return _logs.TryGetValue(blockName, out var log)
                ? log.AsReadOnly()
                : Array.Empty<BlockErrorInfo>();
        }

        /// <summary>
        /// Returns the retained log entries for a block that match a specific context string.
        /// </summary>
        public IReadOnlyList<BlockErrorInfo> GetErrorsForContext(string blockName, string context)
        {
            return GetErrorLog(blockName)
                .Where(e => string.Equals(e.Context, context, StringComparison.OrdinalIgnoreCase))
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Returns the retained log entries for a block that match a specific severity.
        /// </summary>
        public IReadOnlyList<BlockErrorInfo> GetErrorsBySeverity(string blockName, ErrorSeverity severity)
        {
            return GetErrorLog(blockName)
                .Where(e => e.Severity == severity)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Returns the number of retained log entries for a block.
        /// </summary>
        public int GetErrorCount(string blockName)
            => GetErrorLog(blockName).Count;

        /// <summary>
        /// Returns whether the retained log for a block contains at least one error-severity entry.
        /// </summary>
        public bool HasErrors(string blockName)
            => GetErrorLog(blockName).Any(e => e.Severity >= ErrorSeverity.Error);

        #endregion

        #region Private

        private void AddToLog(string blockName, BlockErrorInfo info)
        {
            if (!_logs.ContainsKey(blockName))
                _logs[blockName] = new List<BlockErrorInfo>();

            _logs[blockName].Add(info);

            // FIFO eviction
            while (_logs[blockName].Count > MaxLogSize)
                _logs[blockName].RemoveAt(0);
        }

        #endregion
    }
}
