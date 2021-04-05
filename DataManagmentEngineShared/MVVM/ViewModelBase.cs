using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace TheTechIdea.DataManagment_Engine.MVVM
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        // base class code
        public EnumRecordStatus RecordStatus { get; set; } = EnumRecordStatus.Unchanged;
        public event PropertyChangedEventHandler PropertyChanged;

       
       
        protected virtual void OnPropertyChanged(string propertyName)
        {
            System.ComponentModel.PropertyChangedEventHandler handler;
            handler = this.PropertyChanged;
            if ((null != handler))
            {
                handler(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
                if (RecordStatus == EnumRecordStatus.Unchanged)
                {
                    RecordStatus = EnumRecordStatus.Modified;
                }

            }
        }

    }
}
