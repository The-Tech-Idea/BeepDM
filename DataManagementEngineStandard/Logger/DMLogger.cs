using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

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
    public class DMLogger : IDMLogger, IDisposable, Microsoft.Extensions.Logging.ILogger
    {
        /// <summary>
        /// Occurs when a log event is triggered.
        /// </summary>
        public event EventHandler<string> Onevent;

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        private Microsoft.Extensions.Logging.ILogger _logger;
        private ILoggerFactory _loggerFactory;
        private readonly object lockObject = new object();
        private bool disposedValue;
        private readonly List<Func<string, bool>> logFilters = new List<Func<string, bool>>();
        private LogState logstate = LogState.Active;
        public LogState LogState { get { return logstate; } set { logstate = value; } } // Default state is Active
        
        /// <summary>
        /// Initializes a new instance of the DMLogger class, setting up the logger configurations.
        /// </summary>
        public DMLogger()
        {
            InitializeLoggerFactory(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Debug);
                builder.AddProvider(new SimpleConsoleLoggerProvider());
                builder.AddProvider(new RollingFileLoggerProvider("log.txt", 7));
            });

            WriteLog("Logger Initialized");
        }

        private void InitializeLoggerFactory(Action<ILoggingBuilder> configure)
        {
            _loggerFactory?.Dispose();
            _loggerFactory = LoggerFactory.Create(configure);
            _logger = _loggerFactory.CreateLogger<DMLogger>();
        }

        /// <summary>
        /// Writes a general log message.
        /// </summary>
        public void WriteLog(string info)
        {
            LogMessage(() => _logger.LogInformation(info), info);
        }

        public void LogError(string error)
        {
            LogMessage(() => _logger.LogError(error), error);
        }

        public void LogWarning(string warning)
        {
            LogMessage(() => _logger.LogWarning(warning), warning);
        }

        public void LogInfo(string info)
        {
            LogMessage(() => _logger.LogInformation(info), info);
        }

        public void LogCritical(string error)
        {
            LogMessage(() => _logger.LogCritical(error), error);
        }

        public void LogDebug(string message)
        {
            LogMessage(() => _logger.LogDebug(message), message);
        }

        public void LogTrace(string message)
        {
            LogMessage(() => _logger.LogTrace(message), message);
        }

        public void LogWithContext(string message, object context)
        {
            string contextStr = context != null ? JsonConvert.SerializeObject(context) : "null";
            string fullMessage = $"{message} - Context: {contextStr}";
            LogMessage(() => _logger.LogInformation(fullMessage), fullMessage);
        }

        public void LogStructured(string message, object properties)
        {
            string propertiesStr = properties != null ? JsonConvert.SerializeObject(properties) : "null";
            string fullMessage = $"{message} - Properties: {propertiesStr}";
            LogMessage(() => _logger.LogInformation(fullMessage), fullMessage);
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
            // Flush is not natively supported in the same way by Microsoft.Extensions.Logging,
            // but disposing the factory ensures buffers are flushed.
            _loggerFactory?.Dispose();
            // Reinitialize to keep logger functional if used after flush
            InitializeLoggerFactory(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Debug);
                builder.AddProvider(new SimpleConsoleLoggerProvider());
                builder.AddProvider(new RollingFileLoggerProvider("log.txt", 7));
            });
        }

        public void ConfigureLogger(Action<object> configure)
        {
            lock (lockObject)
            {
                if (configure is Action<ILoggingBuilder> builderAction)
                {
                    InitializeLoggerFactory(builderAction);
                }
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

                // Apply log filters if any
                if (logFilters.Count > 0)
                {
                    foreach (var filter in logFilters)
                    {
                        if (!filter(message))
                            return; // Message filtered out
                    }
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
                    _loggerFactory?.Dispose();
                }

                _logger = null;
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
            return _logger?.BeginScope(state);
        }

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel)
        {
            if (logstate == LogState.Stopped)
                return false;

            return _logger?.IsEnabled(logLevel) ?? false;
        }

        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var message = formatter(state, exception);
            if (string.IsNullOrEmpty(message) && exception == null)
                return;

            // Handled via inner _logger to keep things dry, but also check filters locally
            if (logFilters.Count > 0)
            {
                foreach (var filter in logFilters)
                {
                    if (!filter(message))
                        return;
                }
            }

            _logger?.Log(logLevel, eventId, state, exception, formatter);
            Onevent?.Invoke(this, message); // Forward standard logger calls to the event
        }

        #endregion

        #region Custom Providers

        internal class SimpleConsoleLoggerProvider : ILoggerProvider
        {
            public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName) => new SimpleConsoleLogger();
            public void Dispose() { }

            private class SimpleConsoleLogger : Microsoft.Extensions.Logging.ILogger
            {
                public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
                public bool IsEnabled(LogLevel logLevel) => true;
                public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
                {
                    if (!IsEnabled(logLevel)) return;
                    var msg = formatter(state, exception);
                    if (string.IsNullOrEmpty(msg) && exception == null) return;
                    
                    var color = Console.ForegroundColor;
                    switch (logLevel)
                    {
                        case LogLevel.Critical:
                        case LogLevel.Error: Console.ForegroundColor = ConsoleColor.Red; break;
                        case LogLevel.Warning: Console.ForegroundColor = ConsoleColor.Yellow; break;
                        case LogLevel.Information: Console.ForegroundColor = ConsoleColor.White; break;
                        case LogLevel.Debug:
                        case LogLevel.Trace: Console.ForegroundColor = ConsoleColor.Gray; break;
                    }
                    
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{logLevel.ToString().Substring(0, 3).ToUpper()}] {msg}");
                    if (exception != null) Console.WriteLine(exception.ToString());
                    
                    Console.ForegroundColor = color;
                }
            }
        }

        internal class RollingFileLoggerProvider : ILoggerProvider
        {
            private readonly string _baseFileName;
            private readonly int _retainedFileCountLimit;
            private readonly object _lock = new object();

            public RollingFileLoggerProvider(string baseFileName, int retainedFileCountLimit = 7)
            {
                _baseFileName = baseFileName;
                _retainedFileCountLimit = retainedFileCountLimit;
            }

            public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName) => new RollingFileLogger(this);
            public void Dispose() { }

            internal void WriteLog(string message)
            {
                lock (_lock)
                {
                    try
                    {
                        string currentDate = DateTime.Now.ToString("yyyyMMdd");
                        string ext = Path.GetExtension(_baseFileName);
                        string name = Path.GetFileNameWithoutExtension(_baseFileName);
                        string dir = Path.GetDirectoryName(_baseFileName);
                        if (string.IsNullOrEmpty(dir)) dir = AppDomain.CurrentDomain.BaseDirectory;
                        
                        string currentFile = Path.Combine(dir, $"{name}{currentDate}{ext}");
                        
                        File.AppendAllText(currentFile, message + Environment.NewLine);
                        
                        CleanupOldFiles(dir, name, ext);
                    }
                    catch { } // Ignore file write errors
                }
            }

            private void CleanupOldFiles(string directory, string fileNameWithoutExt, string ext)
            {
                try
                {
                    var files = Directory.GetFiles(directory, $"{fileNameWithoutExt}*{ext}")
                                         .Select(f => new FileInfo(f))
                                         .OrderByDescending(f => f.CreationTime)
                                         .ToList();

                    if (files.Count > _retainedFileCountLimit)
                    {
                        for (int i = _retainedFileCountLimit; i < files.Count; i++)
                        {
                            files[i].Delete();
                        }
                    }
                }
                catch { }
            }

            private class RollingFileLogger : Microsoft.Extensions.Logging.ILogger
            {
                private readonly RollingFileLoggerProvider _provider;
                public RollingFileLogger(RollingFileLoggerProvider provider) { _provider = provider; }
                public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
                public bool IsEnabled(LogLevel logLevel) => true;
                public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
                {
                    if (!IsEnabled(logLevel)) return;
                    var msg = formatter(state, exception);
                    if (string.IsNullOrEmpty(msg) && exception == null) return;
                    
                    var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{logLevel.ToString().Substring(0, 3).ToUpper()}] {msg}";
                    if (exception != null) logEntry += Environment.NewLine + exception.ToString();
                    
                    _provider.WriteLog(logEntry);
                }
            }
        }

        #endregion
    }
}
