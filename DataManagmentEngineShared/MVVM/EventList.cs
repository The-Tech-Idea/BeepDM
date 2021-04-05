using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;

namespace TheTechIdea.DataManagment_Engine.MVVM
{
    public class EventList<T> : ObservableCollection<T>  where T : INotifyPropertyChanged
    {
        //private readonly ObservableCollection<T> _list;

        public EventList()
        {
            _list = new ObservableCollection<T>();
            init();
        }

        public EventList(IEnumerable<T> collection)
        {
            _list = new ObservableCollection<T>(collection);
            init();
        }
        private void init()
        {
            
            _list.CollectionChanged += _list_CollectionChanged;
           
        }
        private void _list_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            //publisher.SomeEvent += target.SomeHandler;
            //then "publisher" will keep "target" alive, but "target" will not keep "publisher" alive.
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:

                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    break;



            }
            if (e.OldItems != null)
                foreach (var item in e.OldItems)
                    if (item != null && item is INotifyPropertyChanged i)
                        i.PropertyChanged -= Element_PropertyChanged;


            if (e.NewItems != null)
                foreach (var item in e.NewItems)
                    if (item != null && item is INotifyPropertyChanged i)
                    {
                        i.PropertyChanged -= Element_PropertyChanged;
                        i.PropertyChanged += Element_PropertyChanged;
                    }
                
            base.OnCollectionChanged(e);
        }
        public delegate void ListedItemPropertyChangedEventHandler(IList SourceList, object Item, PropertyChangedEventArgs e);
        public ListedItemPropertyChangedEventHandler ItemPropertyChanged;
        private void Element_PropertyChanged(object sender, PropertyChangedEventArgs e) => ItemPropertyChanged?.Invoke(this, sender, e);

        private ObservableCollection<T> _list;
    

        public event EventHandler<EventListArgs<T>> ItemAdded;
        public event EventHandler<EventListArgs<T>> ItemRemoved;

        private void RaiseEvent(EventHandler<EventListArgs<T>> eventHandler, T item, int index)
        {
            var eh = eventHandler;
            eh?.Invoke(this, new EventListArgs<T>(item, index));
        }

        public new IEnumerator<T> GetEnumerator()
        {
            
            return _list.GetEnumerator();
        }

       

        public new void Add(T item)
        {
            var index = _list.Count;
            _list.Add(item);
            RaiseEvent(ItemAdded, item, index);
        }

        public new void Clear()
        {
            for (var index = 0; index < _list.Count; index++)
            {
                var item = _list[index];
                RaiseEvent(ItemRemoved, item, index);
            }

            _list.Clear();
        }

        public new bool Contains(T item)
        {
            return _list.Contains(item);
        }

        public new void CopyTo(T[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public new bool Remove(T item)
        {
            var index = _list.IndexOf(item);

            if (index < 0)
                return false;

            RemoveAt(index);
            return true;
        }

        public new int Count => _list.Count;
        public bool IsReadOnly => false;

        public new int IndexOf(T item)
        {
            return _list.IndexOf(item);
        }

        public new void Insert(int index, T item)
        {
            _list.Insert(index, item);
            RaiseEvent(ItemAdded, item, index);
        }

        public new void RemoveAt(int index)
        {
            var item = _list[index];
            _list.RemoveAt(index);
            RaiseEvent(ItemRemoved, item, index);
        }

        public new T this[int index]
        {
            get { return _list[index]; }
            set { _list[index] = value; }
        }
    }

    public class EventListArgs<T> : EventArgs
    {
        public EventListArgs(T item, int index)
        {
            Item = item;
            Index = index;
        }

        public T Item { get; }
        public int Index { get; }
    }
    public class EventListBaseModel<ViewModelBase>: ObservableCollection<ViewModelBase> 
    {
        //private readonly ObservableCollection<T> _list;

        public EventListBaseModel()
        {
            _list = new ObservableCollection<ViewModelBase>();
        }

        public EventListBaseModel(IEnumerable<ViewModelBase> collection)
        {
            _list = new ObservableCollection<ViewModelBase>(collection);
            
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

                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    break;



            }

        }
        private ObservableCollection<ViewModelBase> _list;
       
        public event EventHandler<EventListArgs<ViewModelBase>> ItemAdded;
        public event EventHandler<EventListArgs<ViewModelBase>> ItemRemoved;

        private void RaiseEvent(EventHandler<EventListArgs<ViewModelBase>> eventHandler, ViewModelBase item, int index)
        {
            var eh = eventHandler;
            eh?.Invoke(this, new EventListArgs<ViewModelBase>(item, index));
        }

        public new IEnumerator<ViewModelBase> GetEnumerator()
        {

            return _list.GetEnumerator();
        }


        
        public new void Add(ViewModelBase item)
        {
            var index = _list.Count;
            _list.Add(item);
            RaiseEvent(ItemAdded, item, index);
        }

        public new void Clear()
        {
            for (var index = 0; index < _list.Count; index++)
            {
                var item = _list[index];
                RaiseEvent(ItemRemoved, item, index);
            }

            _list.Clear();
        }

        public new bool Contains(ViewModelBase item)
        {
            return _list.Contains(item);
        }

        public new void CopyTo(ViewModelBase[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public new bool Remove(ViewModelBase item)
        {
            var index = _list.IndexOf(item);

            if (index < 0)
                return false;

            RemoveAt(index);
            return true;
        }

        public int Count => _list.Count;
        public bool IsReadOnly => false;

        public int IndexOf(ViewModelBase item)
        {
            return _list.IndexOf(item);
        }

        public new void Insert(int index, ViewModelBase item)
        {
            _list.Insert(index, item);
            RaiseEvent(ItemAdded, item, index);
        }

        public new void RemoveAt(int index)
        {
            var item = _list[index];
            _list.RemoveAt(index);
            RaiseEvent(ItemRemoved, item, index);
        }

        public new ViewModelBase this[int index]
        {
            get { return _list[index]; }
            set { _list[index] = value; }
        }
    }

    //public class MyClass
    //    {
    //        public MyClass()
    //        {
    //            MyItems = new EventList<string>();

    //            MyItems.ItemAdded += (sender, args) =>
    //            {
    //                // do something when items are added
    //            };

    //            MyItems.ItemRemoved += (sender, args) =>
    //            {
    //                // do something when items are removed
    //            };
    //        }

    //        public EventList<string> MyItems { get; }
    //    }
}
