
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using System;
using System.ComponentModel;

namespace TheTechIdea.Logger
{
    public class DMLogger : IDMLogger
    {

        public event EventHandler<string> Onevent;
        public event PropertyChangedEventHandler PropertyChanged;
       
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
           
            logger.Information(info);
          //  OnPropertyChanged("Log");
           // Onevent?.Invoke(this, info);
           // Log.CloseAndFlush();
        }
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null) // if there is any subscribers 
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
