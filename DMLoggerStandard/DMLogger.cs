
using Serilog;
using Serilog.Sinks.TextWriter;
using System;
using System.ComponentModel;

namespace TheTechIdea.Logger
{
    public delegate void LogEventHandler(string logMessage);
    public class DMLogger : IDMLogger
    {

        public event EventHandler<string> Onevent;
      
        public static event LogEventHandler LogEvent;
        public event PropertyChangedEventHandler PropertyChanged;
        private bool logstatus = true;
        public DMLogger()
        {
            logger = new LoggerConfiguration()
             .WriteTo.File("log.txt", outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.Console()
           
            .CreateLogger();
            WriteLog("Init Logger");  //File("log.txt"
           // Log.CloseAndFlush();
        }
        private Serilog.ILogger logger { get; set; }
        public void WriteLog(string info)
        {
            if (logstatus) {
                logger.Information(info);
              
                 Onevent?.Invoke(this, info);
            }
            
           
        }
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null) // if there is any subscribers 
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public void StartLog()
        {
            logstatus = true;
        }

        public void StopLog()
        {
            logstatus = false;
        }

        public void PauseLog()
        {
            logstatus = false;
        }
    }
}
