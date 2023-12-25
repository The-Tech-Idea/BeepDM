using Serilog;
using System;
using System.ComponentModel;

namespace TheTechIdea.Logger
{
   
    /// <summary>
    /// Provides logging functionalities for the application.
    /// </summary>
    public class DMLogger : IDMLogger
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
        private bool logstatus = true;

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
            if (logstatus) {
                logger.Information(info);
              
                 Onevent?.Invoke(this, info);
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
    }
}
