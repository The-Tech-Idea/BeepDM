using Serilog;
using System;
using System.ComponentModel;

namespace TheTechIdea.Beep.Logger
{
   
    /// <summary>
    /// Provides logging functionalities for the application.
    /// </summary>
    public class DMLogger : IDMLogger,IDisposable
    {
        public delegate void LogEventHandler(string logMessage);

        /// <summary>
        /// Occurs when a log event is triggered.
        /// </summary>
        public event EventHandler<string> Onevent;

        /// <summary>
        /// Represents the method that will handle a log event.
        /// </summary>
        public static event LogEventHandler LogEvent;
        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        private Serilog.ILogger logger { get; set; }
        private readonly object lockObject = new object();
        private bool logstatus = true;
        private bool disposedValue;

        /// <summary>
        /// Initializes a new instance of the DMLogger class, setting up the logger configurations.
        /// </summary>
        public DMLogger()
        {
            logger = new LoggerConfiguration()
             .WriteTo.File("log.txt", outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.Console()
           
            .CreateLogger();
            WriteLog("Init Logger");  //File("log.txt"
           // Log.CloseAndFlush();
        }

        /// <summary>
        /// Writes a log message.
        /// </summary>
        /// <param name="info">The information to log.</param>
        public void WriteLog(string info)
        {
            // Check for null or empty strings to avoid logging unnecessary information.
            if (string.IsNullOrEmpty(info))
            {
                return;
            }

            // Ensure thread safety when checking and using logstatus and logger.
            lock (lockObject)
            {
                if (logstatus && logger != null)
                {
                    try
                    {
                        logger.Information(info);
                        Onevent?.Invoke(this, info);
                    }
                    catch (Exception ex)
                    {
                        // Consider how to handle logging exceptions.
                        // You might want to log to an alternative destination, show a message, etc.
                        Console.WriteLine($"Error logging message: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Invokes the PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null) // if there is any subscribers 
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        /// <summary>
        /// Starts logging.
        /// </summary>
        public void StartLog()
        {
            logstatus = true;
        }
        /// <summary>
        /// Stops logging.
        /// </summary>
        public void StopLog()
        {
            logstatus = false;
        }
        /// <summary>
        /// Pauses logging.
        /// </summary>
        public void PauseLog()
        {
            logstatus = false;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects).
                    // Since logger is a managed type but holds onto unmanaged resources, ensure it's disposed.
                    (logger as IDisposable)?.Dispose();
                }

                // Set large fields to null.
                logger = null;

                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~DMLogger()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
