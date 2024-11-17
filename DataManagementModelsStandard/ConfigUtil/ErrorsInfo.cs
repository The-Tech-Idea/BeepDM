using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace TheTechIdea.Beep.ConfigUtil
{

    public class ErrorsInfo : INotifyPropertyChanged, IErrorsInfo
    {
     //   public IDMLogger logger { get; set; }
      
        public event PropertyChangedEventHandler PropertyChanged;
        private Errors er;
        public Errors Flag
        {
            get { return er; }
            set { er = value; OnPropertyChanged("ErrorStatus"); }
        }
        public System.Exception Ex { get; set; }
        private string pMessage;
        public string Message
        {
            get { return pMessage; }
            set { pMessage = value; OnPropertyChanged("Message");  }
        }
        public ErrorsInfo()
        {
            Errors = new List<IErrorsInfo>();
            //logger = plogger;
        }
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null) // if there is any subscribers 
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        public string Module { get; set; }
        public string Fucntion { get; set; }
        public List<IErrorsInfo> Errors { get; set; }

       
    }
    public enum Errors
    {
        Ok, Failed
    }

}
