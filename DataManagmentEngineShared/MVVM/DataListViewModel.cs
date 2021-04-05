using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;

namespace TheTechIdea.DataManagment_Engine.MVVM
{
   public class DataListViewModel 
    {
        
        public event PropertyChangedEventHandler PropertyChanged;
        public DataListViewModel(ObservableCollection<ViewModelBase> EntityModel)
        {
            Rows.CollectionChanged += Rows_CollectionChanged;
         //   EntityModel.PropertyChanged += EntityModel_PropertyChanged;
            
        }
       
        private void Rows_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    break;
                
                  

            }
            
        }
        private ObservableCollection<ViewModelBase> _rows;
        public ObservableCollection<ViewModelBase> Rows
        {
            get
            {
                return _rows;
            }
            set
            {
                _rows = value;
                RaisePropertyChanged("Rows");
            }
        }
        private void RaisePropertyChanged(string v)
        {
            System.ComponentModel.PropertyChangedEventHandler handler;
            handler = this.PropertyChanged;
            if ((null != handler))
            {
                handler(this, new System.ComponentModel.PropertyChangedEventArgs(v));
            }
        }
        private void EntityModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            System.ComponentModel.PropertyChangedEventHandler handler;
            
            handler = this.PropertyChanged;
            if ((null != handler))
            {
                handler(this, new System.ComponentModel.PropertyChangedEventArgs(e.PropertyName));
            }
        }
    }
   
    public class RecordeventArgs:EventArgs
    {
        //
       // public virtual 
        public virtual string PropertyName { get; }
    }
    public enum EnumRecordStatus
    {
        Added,Modified,Deleted,Unchanged
    }
}
//If Action is NotifyCollectionChangedAction.Add, then NewItems contains the items that were added. In addition, if NewStartingIndex is not -1, then it contains the index where the new items were added.

//If Action is NotifyCollectionChangedAction.Remove, then OldItems contains the items that were removed. In addition, if OldStartingIndex is not -1, then it contains the index where the old items were removed.

//If Action is NotifyCollectionChangedAction.Replace, then OldItems contains the replaced items and NewItems contains the replacement items. In addition, NewStartingIndex and OldStartingIndex are equal, and if they are not -1, then they contain the index where the items were replaced.

//If Action is NotifyCollectionChangedAction.Move, then NewItems and OldItems are logically equivalent (i.e., they are SequenceEqual, even if they are different instances), and they contain the items that moved. In addition, OldStartingIndex contains the index where the items were moved from, and NewStartingIndex contains the index where the items were moved to. A Move operation is logically treated as a Remove followed by an Add, so NewStartingIndex is interpreted as though the items had already been removed.

//If Action is NotifyCollectionChangedAction.Reset, then no other properties are valid.