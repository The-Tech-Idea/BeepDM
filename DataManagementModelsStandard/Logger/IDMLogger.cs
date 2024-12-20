using System;
using System.ComponentModel;

namespace TheTechIdea.Beep.Logger
{
    /// <summary>
    /// Provides a comprehensive logging interface for the application.
    /// </summary>
    public interface IDMLogger
    {
        /// <summary>
        /// Occurs when a log event is triggered.
        /// </summary>
        event EventHandler<string> Onevent;

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Writes a general log message.
        /// </summary>
        /// <param name="info">The information to log.</param>
        void WriteLog(string info);

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="error">The error message to log.</param>
        void LogError(string error);

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="warning">The warning message to log.</param>
        void LogWarning(string warning);

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="info">The informational message to log.</param>
        void LogInfo(string info);

        /// <summary>
        /// Logs a critical error message.
        /// </summary>
        /// <param name="error">The critical error message to log.</param>
        void LogCritical(string error);

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="message">The debug message to log.</param>
        void LogDebug(string message);

        /// <summary>
        /// Logs a trace message for fine-grained details.
        /// </summary>
        /// <param name="message">The trace message to log.</param>
        void LogTrace(string message);

        /// <summary>
        /// Logs a message with additional context information.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="context">The additional context information.</param>
        void LogWithContext(string message, object context);

        /// <summary>
        /// Logs a message with structured properties.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="properties">The structured properties to include.</param>
        void LogStructured(string message, object properties);

        /// <summary>
        /// Starts logging.
        /// </summary>
        void StartLog();

        /// <summary>
        /// Stops logging.
        /// </summary>
        void StopLog();

        /// <summary>
        /// Pauses logging.
        /// </summary>
        void PauseLog();

        /// <summary>
        /// Flushes all pending log messages to the output.
        /// </summary>
        void Flush();

        /// <summary>
        /// Configures the logger dynamically at runtime.
        /// </summary>
        /// <param name="configure">The configuration action.</param>
        void ConfigureLogger(Action<object> configure);

        /// <summary>
        /// Adds a filter to selectively log messages based on custom conditions.
        /// </summary>
        /// <param name="filter">The filter function.</param>
        void AddLogFilter(Func<string, bool> filter);
    }
}
