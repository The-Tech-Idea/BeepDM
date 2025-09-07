using System;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor.BeepSync.Interfaces;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor.BeepSync.Helpers
{
    /// <summary>
    /// Helper class for sync progress reporting and logging operations
    /// Based on progress patterns from DataSyncManager.SendMessage and LogSyncRun
    /// </summary>
    public class SyncProgressHelper : ISyncProgressHelper
    {
        private readonly IDMEEditor _editor;
        private const string LoggerName = "BeepSync";

        /// <summary>
        /// Initializes a new instance of the SyncProgressHelper class
        /// </summary>
        /// <param name="editor">The DME editor instance</param>
        public SyncProgressHelper(IDMEEditor editor)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        }

        /// <summary>
        /// Report progress with message and optional progress counters
        /// Based on DataSyncManager.SendMessage pattern
        /// </summary>
        /// <param name="progress">Progress reporter</param>
        /// <param name="message">Progress message</param>
        /// <param name="current">Current progress count</param>
        /// <param name="total">Total progress count</param>
        public void ReportProgress(IProgress<PassedArgs> progress, string message, int current = 0, int total = 0)
        {
            if (progress == null)
                return;

            try
            {
                var args = new PassedArgs
                {
                    EventType = "Update",
                    Messege = message,
                    ParameterInt1 = current,
                    ParameterInt2 = total
                };

                progress.Report(args);

                // Also log the progress message
                LogMessage(message, Errors.Ok);
            }
            catch (Exception ex)
            {
                LogMessage($"Error reporting progress: {ex.Message}", Errors.Failed);
            }
        }

        /// <summary>
        /// Log sync operation message with specified error level
        /// </summary>
        /// <param name="message">Message to log</param>
        /// <param name="errorLevel">Error level (Ok, Failed, etc.)</param>
        public void LogMessage(string message, Errors errorLevel = Errors.Ok)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            try
            {
                _editor.AddLogMessage(LoggerName, message, DateTime.Now, -1, "", errorLevel);
            }
            catch (Exception ex)
            {
                // Fallback logging if main logging fails
                try
                {
                    _editor.AddLogMessage("System", $"Logging error in {LoggerName}: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                }
                catch
                {
                    // Silent fail if even fallback logging fails
                }
            }
        }

        /// <summary>
        /// Log sync run details and update schema status
        /// Based on DataSyncManager.LogSyncRun method
        /// </summary>
        /// <param name="schema">The sync schema to log</param>
        public void LogSyncRun(DataSyncSchema schema)
        {
            if (schema == null)
            {
                LogMessage("Cannot log sync run: schema is null", Errors.Failed);
                return;
            }

            try
            {
                // Create sync run data entry
                var syncRunData = new SyncRunData
                {
                    ID = Guid.NewGuid().ToString(),
                    SyncSchemaID = schema.ID,
                    SyncDate = schema.LastSyncDate != default ? schema.LastSyncDate : DateTime.Now,
                    SyncStatus = schema.SyncStatus,
                    SyncStatusMessage = schema.SyncStatusMessage
                };

                // Add to schema's sync runs collection
                if (schema.SyncRuns == null)
                    schema.SyncRuns = new ObservableBindingList<SyncRunData>();

                schema.SyncRuns.Add(syncRunData);
                schema.LastSyncRunData = syncRunData;

                // Log the sync run
                var logLevel = schema.SyncStatus == "Success" ? Errors.Ok : Errors.Failed;
                LogMessage($"Sync run logged for schema '{schema.ID}': {schema.SyncStatus} - {schema.SyncStatusMessage}", logLevel);
            }
            catch (Exception ex)
            {
                LogMessage($"Error logging sync run for schema '{schema?.ID}': {ex.Message}", Errors.Failed);
            }
        }

        /// <summary>
        /// Handle and log sync errors with detailed information
        /// </summary>
        /// <param name="schema">The sync schema where error occurred</param>
        /// <param name="message">Error message</param>
        /// <param name="ex">Optional exception for detailed error information</param>
        public void LogError(DataSyncSchema schema, string message, Exception ex = null)
        {
            if (schema == null)
            {
                LogMessage("Cannot log sync error: schema is null", Errors.Failed);
                return;
            }

            try
            {
                // Update schema status
                schema.SyncStatus = "Failed";
                schema.LastSyncDate = DateTime.Now;

                // Build comprehensive error message
                var fullMessage = message;
                if (ex != null)
                {
                    fullMessage = $"{message}. Exception: {ex.Message}";
                    if (ex.InnerException != null)
                        fullMessage += $" Inner Exception: {ex.InnerException.Message}";
                }

                schema.SyncStatusMessage = fullMessage;

                // Log the error
                LogMessage($"Sync error for schema '{schema.ID}' ({schema.SourceEntityName} -> {schema.DestinationEntityName}): {fullMessage}", Errors.Failed);

                // Log the sync run with error details
                LogSyncRun(schema);
            }
            catch (Exception logEx)
            {
                // Fallback error logging
                LogMessage($"Error while logging sync error for schema '{schema?.ID}': {logEx.Message}", Errors.Failed);
            }
        }

        /// <summary>
        /// Report successful sync completion
        /// </summary>
        /// <param name="schema">The sync schema that completed successfully</param>
        /// <param name="recordsProcessed">Number of records processed</param>
        /// <param name="progress">Optional progress reporter</param>
        public void LogSuccess(DataSyncSchema schema, int recordsProcessed, IProgress<PassedArgs> progress = null)
        {
            if (schema == null)
            {
                LogMessage("Cannot log sync success: schema is null", Errors.Failed);
                return;
            }

            try
            {
                // Update schema status
                schema.SyncStatus = "Success";
                schema.LastSyncDate = DateTime.Now;
                schema.SyncStatusMessage = $"Synchronization completed successfully. Processed {recordsProcessed} record(s).";

                // Log success message
                LogMessage($"Sync completed for schema '{schema.ID}' ({schema.SourceEntityName} -> {schema.DestinationEntityName}): {recordsProcessed} record(s) processed", Errors.Ok);

                // Report progress if available
                ReportProgress(progress, schema.SyncStatusMessage);

                // Log the sync run
                LogSyncRun(schema);
            }
            catch (Exception ex)
            {
                LogMessage($"Error while logging sync success for schema '{schema?.ID}': {ex.Message}", Errors.Failed);
            }
        }

        /// <summary>
        /// Report sync cancellation
        /// </summary>
        /// <param name="schema">The sync schema that was cancelled</param>
        /// <param name="progress">Optional progress reporter</param>
        public void LogCancellation(DataSyncSchema schema, IProgress<PassedArgs> progress = null)
        {
            if (schema == null)
            {
                LogMessage("Cannot log sync cancellation: schema is null", Errors.Failed);
                return;
            }

            try
            {
                // Update schema status
                schema.SyncStatus = "Cancelled";
                schema.LastSyncDate = DateTime.Now;
                schema.SyncStatusMessage = "Synchronization was cancelled by user.";

                // Log cancellation message
                LogMessage($"Sync cancelled for schema '{schema.ID}' ({schema.SourceEntityName} -> {schema.DestinationEntityName})", Errors.Ok);

                // Report progress if available
                if (progress != null)
                {
                    var args = new PassedArgs
                    {
                        EventType = "Cancel",
                        Messege = schema.SyncStatusMessage
                    };
                    progress.Report(args);
                }

                // Log the sync run
                LogSyncRun(schema);
            }
            catch (Exception ex)
            {
                LogMessage($"Error while logging sync cancellation for schema '{schema?.ID}': {ex.Message}", Errors.Failed);
            }
        }
    }
}
