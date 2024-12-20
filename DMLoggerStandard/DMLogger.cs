using System;
using System.Collections.Generic;
using System.ComponentModel;
using Serilog;

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
    public class DMLogger : IDMLogger, IDisposable
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
            Log.CloseAndFlush();
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
    }
}
