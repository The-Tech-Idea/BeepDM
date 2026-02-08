using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor
{
    public class ItemAddedEventArgs<T> : EventArgs
    {
        public T Item { get; }
        public ItemAddedEventArgs(T item) => Item = item;
    }

    public class ItemRemovedEventArgs<T> : EventArgs
    {
        public T Item { get; }
        public ItemRemovedEventArgs(T item) => Item = item;
    }

    public class ItemChangedEventArgs<T> : EventArgs
    {
        public T Item { get; }
        public string PropertyName { get; }
        public ItemChangedEventArgs(T item, string propertyName)
        {
            Item = item;
            PropertyName = propertyName;
        }
    }

    public class ItemValidatingEventArgs<T> : EventArgs
    {
        public T Item { get; }
        public bool Cancel { get; set; } = false;
        public string ErrorMessage { get; set; }

        public ItemValidatingEventArgs(T item)
        {
            Item = item;
        }
    }
}
