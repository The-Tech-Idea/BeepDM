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

    /// <summary>
    /// Fired BEFORE the current position changes. Set Cancel = true to prevent navigation.
    /// </summary>
    public class CurrentChangingEventArgs : EventArgs
    {
        /// <summary>Index before the move.</summary>
        public int OldIndex { get; }
        /// <summary>Proposed new index.</summary>
        public int NewIndex { get; }
        /// <summary>Item at OldIndex (may be null if list is empty).</summary>
        public object OldItem { get; }
        /// <summary>Item at NewIndex (may be null if list is empty).</summary>
        public object NewItem { get; }
        /// <summary>Set to true to cancel the navigation.</summary>
        public bool Cancel { get; set; }

        public CurrentChangingEventArgs(int oldIndex, int newIndex, object oldItem, object newItem)
        {
            OldIndex = oldIndex;
            NewIndex = newIndex;
            OldItem = oldItem;
            NewItem = newItem;
        }
    }

    /// <summary>
    /// Raised around AddRange / RemoveRange batch operations.
    /// </summary>
    public class BatchOperationEventArgs : EventArgs
    {
        /// <summary>Type of batch operation: "AddRange", "RemoveRange", "RemoveAll".</summary>
        public string OperationType { get; }
        /// <summary>Number of items in the batch.</summary>
        public int ItemCount { get; }

        public BatchOperationEventArgs(string operationType, int itemCount)
        {
            OperationType = operationType;
            ItemCount = itemCount;
        }
    }

    /// <summary>
    /// Raised around commit/save operations with cancel support.
    /// </summary>
    public class CommitEventArgs<T> : EventArgs
    {
        /// <summary>The item being committed.</summary>
        public T Item { get; }
        /// <summary>The entity state at time of commit.</summary>
        public EntityState EntityState { get; }
        /// <summary>Set to true to cancel the commit for this item.</summary>
        public bool Cancel { get; set; }

        public CommitEventArgs(T item, EntityState entityState)
        {
            Item = item;
            EntityState = entityState;
        }
    }
}
