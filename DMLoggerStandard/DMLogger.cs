using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace TheTechIdea.Beep.Logger
{
    // Define an enum for the logging state
    public enum LogState
    {
        Active,
        Paused,
        Stopped
    }
    /// <summary>
    /// Provides logging functionalities for the application.
    /// </summary>
    public class DMLogger : IDMLogger, IDisposable, Serilog.ILogger, Microsoft.Extensions.Logging.ILogger
    {
        /// <summary>
        /// Occurs when a log event is triggered.
        /// </summary>
        public event EventHandler<string> Onevent;

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        private Serilog.ILogger logger;
        private readonly object lockObject = new object();
        private bool logstatus = true;
        private bool disposedValue;
        private readonly List<Func<string, bool>> logFilters = new List<Func<string, bool>>();
        private LogState logstate = LogState.Active;
        public LogState LogState { get { return logstate; } set { logstate = value; } } // Default state is Active
        
        /// <summary>
        /// Initializes a new instance of the DMLogger class, setting up the logger configurations.
        /// </summary>
        public DMLogger()
        {
            logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
                .CreateLogger();

            WriteLog("Logger Initialized");
        }

        /// <summary>
        /// Writes a general log message.
        /// </summary>
        public void WriteLog(string info)
        {
            LogMessage(() => logger.Information(info), info);
        }

        public void LogError(string error)
        {
            LogMessage(() => logger.Error(error), error);
        }

        public void LogWarning(string warning)
        {
            LogMessage(() => logger.Warning(warning), warning);
        }

        public void LogInfo(string info)
        {
            LogMessage(() => logger.Information(info), info);
        }

        public void LogCritical(string error)
        {
            LogMessage(() => logger.Fatal(error), error);
        }

        public void LogDebug(string message)
        {
            LogMessage(() => logger.Debug(message), message);
        }

        public void LogTrace(string message)
        {
            LogMessage(() => logger.Verbose(message), message);
        }

        public void LogWithContext(string message, object context)
        {
            LogMessage(() => logger.ForContext("Context", context, destructureObjects: true).Information(message), message);
        }

        public void LogStructured(string message, object properties)
        {
            LogMessage(() => logger.Information("{@Message} {@Properties}", message, properties), message);
        }


        public void StartLog()
        {
            logstate = LogState.Active;
            WriteLog("Logging resumed.");
        }

        public void StopLog()
        {
            logstate = LogState.Stopped;
            WriteLog("Logging stopped.");
        }

        public void PauseLog()
        {
            logstate = LogState.Paused;
            WriteLog("Logging paused.");
        }

        public void Flush()
        {
            Serilog.Log.CloseAndFlush();
        }

        public void ConfigureLogger(Action<object> configure)
        {

            lock (lockObject)
            {
                Action<LoggerConfiguration> x = (Action<LoggerConfiguration>)configure;
                var config = new LoggerConfiguration();
                x?.Invoke(config);
                logger = config.CreateLogger();
            }
        }

        public void AddLogFilter(Func<string, bool> filter)
        {
            logFilters.Add(filter);
        }

      

        private void LogMessage(Action logAction, string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return; // Do not log empty messages
            }

            lock (lockObject)
            {
                if (logstate == LogState.Stopped)
                {
                    return; // Ignore logs when stopped
                }

                if (logstate == LogState.Paused)
                {
                    // Optionally buffer messages or just discard
                    Console.WriteLine("Log paused; message discarded: " + message);
                    return;
                }

                try
                {
                    logAction(); // Execute the log action
                    Onevent?.Invoke(this, message); // Raise the event
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error logging message: {ex.Message}");
                }
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    (logger as IDisposable)?.Dispose();
                }

                logger = null;
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #region Microsoft.Extensions.Logging.ILogger Implementation

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            // Return a scope that tracks the state
            return new LoggerScope(state);
        }

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel)
        {
            if (logstate == LogState.Stopped)
                return false;

            // Map Microsoft.Extensions.Logging.LogLevel to Serilog LogEventLevel
            var serilogLevel = MapToSerilogLevel(logLevel);
            return logger?.IsEnabled(serilogLevel) ?? false;
        }

        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var message = formatter(state, exception);
            if (string.IsNullOrEmpty(message) && exception == null)
                return;

            // Apply log filters if any
            if (logFilters.Count > 0)
            {
                foreach (var filter in logFilters)
                {
                    if (!filter(message))
                        return; // Message filtered out
                }
            }

            // Map to appropriate internal logging method
            switch (logLevel)
            {
                case Microsoft.Extensions.Logging.LogLevel.Trace:
                    LogTrace(message);
                    if (exception != null)
                        LogTrace($"Exception: {exception}");
                    break;
                case Microsoft.Extensions.Logging.LogLevel.Debug:
                    LogDebug(message);
                    if (exception != null)
                        LogDebug($"Exception: {exception}");
                    break;
                case Microsoft.Extensions.Logging.LogLevel.Information:
                    LogInfo(message);
                    if (exception != null)
                        LogInfo($"Exception: {exception}");
                    break;
                case Microsoft.Extensions.Logging.LogLevel.Warning:
                    LogWarning(message);
                    if (exception != null)
                        LogWarning($"Exception: {exception}");
                    break;
                case Microsoft.Extensions.Logging.LogLevel.Error:
                    if (exception != null)
                        LogError($"{message} - Exception: {exception}");
                    else
                        LogError(message);
                    break;
                case Microsoft.Extensions.Logging.LogLevel.Critical:
                    if (exception != null)
                        LogCritical($"{message} - Exception: {exception}");
                    else
                        LogCritical(message);
                    break;
                case Microsoft.Extensions.Logging.LogLevel.None:
                    // Do nothing
                    break;
            }
        }

        private LogEventLevel MapToSerilogLevel(Microsoft.Extensions.Logging.LogLevel logLevel)
        {
            return logLevel switch
            {
                Microsoft.Extensions.Logging.LogLevel.Trace => LogEventLevel.Verbose,
                Microsoft.Extensions.Logging.LogLevel.Debug => LogEventLevel.Debug,
                Microsoft.Extensions.Logging.LogLevel.Information => LogEventLevel.Information,
                Microsoft.Extensions.Logging.LogLevel.Warning => LogEventLevel.Warning,
                Microsoft.Extensions.Logging.LogLevel.Error => LogEventLevel.Error,
                Microsoft.Extensions.Logging.LogLevel.Critical => LogEventLevel.Fatal,
                Microsoft.Extensions.Logging.LogLevel.None => LogEventLevel.Information,
                _ => LogEventLevel.Information
            };
        }

        #endregion

        #region Serilog.ILogger Implementation

        public void Write(LogEvent logEvent)
        {
            if (logEvent == null)
                return;

            // Apply log filters if any
            var message = logEvent.RenderMessage();
            if (logFilters.Count > 0)
            {
                foreach (var filter in logFilters)
                {
                    if (!filter(message))
                        return; // Message filtered out
                }
            }

            switch (logEvent.Level)
            {
                case LogEventLevel.Verbose:
                    LogTrace(message);
                    break;
                case LogEventLevel.Debug:
                    LogDebug(message);
                    break;
                case LogEventLevel.Information:
                    LogInfo(message);
                    break;
                case LogEventLevel.Warning:
                    LogWarning(message);
                    break;
                case LogEventLevel.Error:
                    LogError(message);
                    break;
                case LogEventLevel.Fatal:
                    LogCritical(message);
                    break;
                default:
                    LogInfo(message);
                    break;
            }
        }

        public bool IsEnabled(LogEventLevel level)
        {
            // Check if logging is active and the level is enabled
            if (logstate == LogState.Stopped)
                return false;

            // Delegate to the underlying Serilog logger for level checking
            return logger?.IsEnabled(level) ?? false;
        }

        #endregion

        // ...rest of existing methods remain the same...
    }

    /// <summary>
    /// A simple implementation of a logging scope for Microsoft.Extensions.Logging
    /// </summary>
    internal class LoggerScope : IDisposable
    {
        private readonly object _state;
        private bool _disposed = false;

        public LoggerScope(object state)
        {
            _state = state;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // Clean up scope if needed
                _disposed = true;
            }
        }
    }
}
