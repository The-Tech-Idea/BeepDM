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

        public bool SuppressErrorEvents { get; set; }
        public int MaxLogSize { get; set; } = 100;

        public event EventHandler<BlockErrorEventArgs> OnError;
        public event EventHandler<BlockErrorEventArgs> OnWarning;

        #region Logging

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

        public void LogWarning(string blockName, string message, string context)
        {
            LogError(blockName, new InvalidOperationException(message), context, ErrorSeverity.Warning);
        }

        #endregion

        #region Query

        public void ClearErrorLog(string blockName)
        {
            if (_logs.ContainsKey(blockName))
                _logs[blockName].Clear();
        }

        public void ClearAllLogs()
        {
            _logs.Clear();
        }

        public IReadOnlyList<BlockErrorInfo> GetErrorLog(string blockName)
        {
            return _logs.TryGetValue(blockName, out var log)
                ? log.AsReadOnly()
                : Array.Empty<BlockErrorInfo>();
        }

        public IReadOnlyList<BlockErrorInfo> GetErrorsForContext(string blockName, string context)
        {
            return GetErrorLog(blockName)
                .Where(e => string.Equals(e.Context, context, StringComparison.OrdinalIgnoreCase))
                .ToList()
                .AsReadOnly();
        }

        public IReadOnlyList<BlockErrorInfo> GetErrorsBySeverity(string blockName, ErrorSeverity severity)
        {
            return GetErrorLog(blockName)
                .Where(e => e.Severity == severity)
                .ToList()
                .AsReadOnly();
        }

        public int GetErrorCount(string blockName)
            => GetErrorLog(blockName).Count;

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
