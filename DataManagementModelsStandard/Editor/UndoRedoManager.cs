using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor
{
    /// <summary>
    /// Describes the type of undoable action.
    /// </summary>
    public enum UndoActionType
    {
        /// <summary>A property value was changed.</summary>
        PropertyChange,
        /// <summary>An item was inserted.</summary>
        Insert,
        /// <summary>An item was removed.</summary>
        Remove
    }

    /// <summary>
    /// Represents a single undoable/redoable action.
    /// </summary>
    public class UndoAction<T> where T : class
    {
        /// <summary>The type of action.</summary>
        public UndoActionType ActionType { get; set; }
        /// <summary>The affected item.</summary>
        public T Item { get; set; }
        /// <summary>The property name (for PropertyChange actions).</summary>
        public string PropertyName { get; set; }
        /// <summary>The old value before the change.</summary>
        public object OldValue { get; set; }
        /// <summary>The new value after the change.</summary>
        public object NewValue { get; set; }
        /// <summary>The index position of the action.</summary>
        public int Index { get; set; }
        /// <summary>When the action occurred.</summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Manages undo/redo stacks with configurable depth.
    /// Opt-in: zero overhead when not enabled.
    /// </summary>
    public class UndoRedoManager<T> where T : class
    {
        private readonly Stack<UndoAction<T>> _undoStack = new Stack<UndoAction<T>>();
        private readonly Stack<UndoAction<T>> _redoStack = new Stack<UndoAction<T>>();

        /// <summary>
        /// Maximum number of undo actions retained. Default: 50.
        /// </summary>
        public int MaxUndoDepth { get; set; } = 50;

        /// <summary>True if there are actions to undo.</summary>
        public bool CanUndo => _undoStack.Count > 0;

        /// <summary>True if there are actions to redo.</summary>
        public bool CanRedo => _redoStack.Count > 0;

        /// <summary>Number of actions in the undo stack.</summary>
        public int UndoCount => _undoStack.Count;

        /// <summary>Number of actions in the redo stack.</summary>
        public int RedoCount => _redoStack.Count;

        /// <summary>
        /// Records an action on the undo stack.
        /// Clears the redo stack (new action invalidates redo history).
        /// </summary>
        public void RecordAction(UndoAction<T> action)
        {
            if (action == null) return;

            _undoStack.Push(action);
            _redoStack.Clear();

            // Trim to max depth
            if (_undoStack.Count > MaxUndoDepth)
            {
                TrimStack(_undoStack, MaxUndoDepth);
            }
        }

        /// <summary>
        /// Pops and returns the most recent undo action.
        /// Pushes it onto the redo stack.
        /// </summary>
        public UndoAction<T> Undo()
        {
            if (_undoStack.Count == 0) return null;
            var action = _undoStack.Pop();
            _redoStack.Push(action);
            return action;
        }

        /// <summary>
        /// Pops and returns the most recent redo action.
        /// Pushes it back onto the undo stack.
        /// </summary>
        public UndoAction<T> Redo()
        {
            if (_redoStack.Count == 0) return null;
            var action = _redoStack.Pop();
            _undoStack.Push(action);
            return action;
        }

        /// <summary>Clears both undo and redo stacks.</summary>
        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
        }

        /// <summary>
        /// Trims a stack to the specified max size by rebuilding it.
        /// </summary>
        private void TrimStack(Stack<UndoAction<T>> stack, int maxSize)
        {
            if (stack.Count <= maxSize) return;

            var temp = new List<UndoAction<T>>();
            while (stack.Count > 0 && temp.Count < maxSize)
                temp.Add(stack.Pop());

            stack.Clear();
            // Re-push in reverse order to maintain chronological order
            for (int i = temp.Count - 1; i >= 0; i--)
                stack.Push(temp[i]);
        }
    }
}
