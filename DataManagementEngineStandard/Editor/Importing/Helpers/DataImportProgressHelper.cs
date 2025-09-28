using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Importing.Interfaces;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.Importing.Helpers
{
    /// <summary>
    /// Helper class for progress monitoring and logging in data import operations
    /// </summary>
    public class DataImportProgressHelper : IDataImportProgressHelper
    {
        private readonly IDMEEditor _editor;
        private readonly List<Importlogdata> _importLogData;

        public DataImportProgressHelper(IDMEEditor editor)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            _importLogData = new List<Importlogdata>();
        }

        /// <summary>
        /// Gets the import log data
        /// </summary>
        public List<Importlogdata> ImportLogData => new List<Importlogdata>(_importLogData); // Return copy for safety

        /// <summary>
        /// Logs an import operation
        /// </summary>
        public void LogImport(string message, int recordNumber)
        {
            if (string.IsNullOrEmpty(message))
                return;

            try
            {
                var logEntry = new Importlogdata
                {
                    Timestamp = DateTime.Now,
                    Message = message,
                    RecordNumber = recordNumber,
                    Level = DetermineLogLevel(message),
                    Category = "Import"
                };

                _importLogData.Add(logEntry);

                // Also log to the main DME logger
                var logLevel = logEntry.Level == ImportLogLevel.Error ? Errors.Failed : Errors.Ok;
                _editor?.AddLogMessage("DataImport", message, DateTime.Now, recordNumber, null, logLevel);
            }
            catch (Exception ex)
            {
                // Fallback logging to avoid infinite loops
                _editor?.Logger?.WriteLog($"Error in LogImport: {ex.Message}");
            }
        }

        /// <summary>
        /// Logs an error
        /// </summary>
        public void LogError(string message, Exception exception)
        {
            try
            {
                var fullMessage = exception != null 
                    ? $"{message}: {exception.Message}"
                    : message;

                var logEntry = new Importlogdata
                {
                    Timestamp = DateTime.Now,
                    Message = fullMessage,
                    RecordNumber = 0,
                    Level = ImportLogLevel.Error,
                    Category = "ImportError"
                };

                _importLogData.Add(logEntry);

                // Log to main DME logger with full exception details
                _editor?.AddLogMessage("DataImport", fullMessage, DateTime.Now, 0, null, Errors.Failed);
                
                // Also log full exception to detailed logger if available
                if (exception != null)
                {
                    _editor?.Logger?.WriteLog($"Import Error Details: {exception}");
                }
            }
            catch (Exception ex)
            {
                // Fallback logging
                _editor?.Logger?.WriteLog($"Error in LogError: {ex.Message}");
            }
        }

        /// <summary>
        /// Reports progress to the progress reporter
        /// </summary>
        public void ReportProgress(IProgress<PassedArgs> progress, string message, int recordsProcessed, int? totalRecords = null)
        {
            if (progress == null)
                return;

            try
            {
                var args = new PassedArgs
                {
                    Messege = message ?? string.Empty,
                    ParameterInt1 = recordsProcessed
                };

                if (totalRecords.HasValue)
                {
                    args.ParameterInt2 = totalRecords.Value;
                    
                    // Calculate percentage if total is known
                    if (totalRecords.Value > 0)
                    {
                        var percentage = (recordsProcessed * 100.0) / totalRecords.Value;
                        args.Parameterdouble1 = percentage;
                    }
                }

                progress.Report(args);

                // Also log the progress
                LogImport(message, recordsProcessed);
            }
            catch (Exception ex)
            {
                LogError("Error reporting progress", ex);
            }
        }

        /// <summary>
        /// Calculates and reports performance metrics
        /// </summary>
        public ImportPerformanceMetrics CalculatePerformanceMetrics(DateTime startTime, int recordsProcessed, int totalRecords)
        {
            try
            {
                var now = DateTime.Now;
                var elapsed = now - startTime;
                var recordsPerSecond = elapsed.TotalSeconds > 0 ? recordsProcessed / elapsed.TotalSeconds : 0;
                var percentageComplete = totalRecords > 0 ? (recordsProcessed * 100.0) / totalRecords : 0;
                
                var remainingRecords = Math.Max(0, totalRecords - recordsProcessed);
                var estimatedTimeRemaining = recordsPerSecond > 0 
                    ? TimeSpan.FromSeconds(remainingRecords / recordsPerSecond)
                    : TimeSpan.Zero;

                var metrics = new ImportPerformanceMetrics
                {
                    ElapsedTime = elapsed,
                    RecordsPerSecond = recordsPerSecond,
                    EstimatedTimeRemaining = estimatedTimeRemaining,
                    PercentageComplete = percentageComplete,
                    RecordsProcessed = recordsProcessed,
                    TotalRecords = totalRecords,
                    LastUpdated = now
                };

                // Log performance metrics
                LogImport($"Performance: {recordsPerSecond:F1} records/sec, " +
                         $"{percentageComplete:F1}% complete, " +
                         $"ETA: {estimatedTimeRemaining:hh\\:mm\\:ss}", recordsProcessed);

                return metrics;
            }
            catch (Exception ex)
            {
                LogError("Error calculating performance metrics", ex);
                
                // Return basic metrics on error
                return new ImportPerformanceMetrics
                {
                    ElapsedTime = DateTime.Now - startTime,
                    RecordsProcessed = recordsProcessed,
                    TotalRecords = totalRecords,
                    LastUpdated = DateTime.Now
                };
            }
        }

        /// <summary>
        /// Clears the import log
        /// </summary>
        public void ClearLog()
        {
            try
            {
                _importLogData.Clear();
                LogImport("Import log cleared", 0);
            }
            catch (Exception ex)
            {
                _editor?.Logger?.WriteLog($"Error clearing import log: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets log entries by level
        /// </summary>
        public List<Importlogdata> GetLogEntriesByLevel(ImportLogLevel level)
        {
            try
            {
                var filteredEntries = new List<Importlogdata>();
                
                foreach (var entry in _importLogData)
                {
                    if (entry.Level == level)
                    {
                        filteredEntries.Add(entry);
                    }
                }

                return filteredEntries;
            }
            catch (Exception ex)
            {
                LogError("Error filtering log entries", ex);
                return new List<Importlogdata>();
            }
        }

        /// <summary>
        /// Gets log entries within a time range
        /// </summary>
        public List<Importlogdata> GetLogEntriesByTimeRange(DateTime startTime, DateTime endTime)
        {
            try
            {
                var filteredEntries = new List<Importlogdata>();
                
                foreach (var entry in _importLogData)
                {
                    if (entry.Timestamp >= startTime && entry.Timestamp <= endTime)
                    {
                        filteredEntries.Add(entry);
                    }
                }

                return filteredEntries;
            }
            catch (Exception ex)
            {
                LogError("Error filtering log entries by time range", ex);
                return new List<Importlogdata>();
            }
        }

        /// <summary>
        /// Exports log to text format
        /// </summary>
        public string ExportLogToText()
        {
            try
            {
                var lines = new List<string>
                {
                    "=== Data Import Log ===",
                    $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    $"Total Entries: {_importLogData.Count}",
                    ""
                };

                foreach (var entry in _importLogData)
                {
                    lines.Add($"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] [{entry.Level}] [{entry.Category}] " +
                             $"Record #{entry.RecordNumber}: {entry.Message}");
                }

                return string.Join(Environment.NewLine, lines);
            }
            catch (Exception ex)
            {
                LogError("Error exporting log to text", ex);
                return $"Error exporting log: {ex.Message}";
            }
        }

        /// <summary>
        /// Gets summary statistics for the import log
        /// </summary>
        public ImportLogSummary GetLogSummary()
        {
            try
            {
                var summary = new ImportLogSummary();
                
                foreach (var entry in _importLogData)
                {
                    summary.TotalEntries++;
                    
                    switch (entry.Level)
                    {
                        case ImportLogLevel.Info:
                            summary.InfoCount++;
                            break;
                        case ImportLogLevel.Warning:
                            summary.WarningCount++;
                            break;
                        case ImportLogLevel.Error:
                            summary.ErrorCount++;
                            break;
                        case ImportLogLevel.Debug:
                            summary.DebugCount++;
                            break;
                        case ImportLogLevel.Success:
                            summary.SuccessCount++;
                            break;
                    }
                }

                if (_importLogData.Count > 0)
                {
                    summary.FirstEntryTime = _importLogData[0].Timestamp;
                    summary.LastEntryTime = _importLogData[_importLogData.Count - 1].Timestamp;
                    summary.Duration = summary.LastEntryTime - summary.FirstEntryTime;
                }

                return summary;
            }
            catch (Exception ex)
            {
                LogError("Error calculating log summary", ex);
                return new ImportLogSummary();
            }
        }

        /// <summary>
        /// Determines the log level based on message content
        /// </summary>
        private ImportLogLevel DetermineLogLevel(string message)
        {
            if (string.IsNullOrEmpty(message))
                return ImportLogLevel.Info;

            var lowerMessage = message.ToLowerInvariant();

            if (lowerMessage.Contains("error") || lowerMessage.Contains("failed") || lowerMessage.Contains("exception"))
                return ImportLogLevel.Error;

            if (lowerMessage.Contains("warning") || lowerMessage.Contains("warn"))
                return ImportLogLevel.Warning;

            if (lowerMessage.Contains("completed") || lowerMessage.Contains("success") || lowerMessage.Contains("finished"))
                return ImportLogLevel.Success;

            if (lowerMessage.Contains("debug") || lowerMessage.Contains("trace"))
                return ImportLogLevel.Debug;

            return ImportLogLevel.Info;
        }

    }

    /// <summary>
    /// Summary statistics for import logs
    /// </summary>
    public class ImportLogSummary
    {
        public int TotalEntries { get; set; }
        public int InfoCount { get; set; }
        public int WarningCount { get; set; }
        public int ErrorCount { get; set; }
        public int DebugCount { get; set; }
        public int SuccessCount { get; set; }
        public DateTime FirstEntryTime { get; set; }
        public DateTime LastEntryTime { get; set; }
        public TimeSpan Duration { get; set; }

        public bool HasErrors => ErrorCount > 0;
        public bool HasWarnings => WarningCount > 0;
        public double ErrorRate => TotalEntries > 0 ? (ErrorCount * 100.0) / TotalEntries : 0;
    }
}