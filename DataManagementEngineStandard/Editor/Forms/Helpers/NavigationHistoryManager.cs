using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Editor.UOWManager.Models;

namespace TheTechIdea.Beep.Editor.UOWManager.Helpers
{
    /// <summary>
    /// Per-block back/forward navigation history stacks.
    /// </summary>
    public class NavigationHistoryManager
    {
        private readonly ConcurrentDictionary<string, Stack<int>> _backStacks  = new();
        private readonly ConcurrentDictionary<string, Stack<int>> _fwdStacks   = new();
        private readonly ConcurrentDictionary<string, List<NavigationHistoryEntry>> _histories = new();

        private Stack<int> BackStack(string block)  => _backStacks.GetOrAdd(block, _ => new Stack<int>());
        private Stack<int> FwdStack(string block)   => _fwdStacks.GetOrAdd(block,  _ => new Stack<int>());
        private List<NavigationHistoryEntry> History(string block)
            => _histories.GetOrAdd(block, _ => new List<NavigationHistoryEntry>());

        /// <summary>
        /// Call whenever a block navigates from <paramref name="fromIndex"/> to a new position.
        /// Clears the forward stack.
        /// </summary>
        public void Push(string blockName, int fromIndex)
        {
            BackStack(blockName).Push(fromIndex);
            FwdStack(blockName).Clear();
            History(blockName).Add(new NavigationHistoryEntry { RecordIndex = fromIndex });
        }

        /// <summary>
        /// Pop back-stack and return the index to navigate to.
        /// Caller must supply <paramref name="currentIndex"/> so we can push it onto forward.
        /// Returns -1 if stack is empty.
        /// </summary>
        public int Back(string blockName, int currentIndex)
        {
            var back = BackStack(blockName);
            if (back.Count == 0) return -1;
            FwdStack(blockName).Push(currentIndex);
            return back.Pop();
        }

        /// <summary>
        /// Pop forward-stack and return index to navigate to.
        /// Returns -1 if stack is empty.
        /// </summary>
        public int Forward(string blockName, int currentIndex)
        {
            var fwd = FwdStack(blockName);
            if (fwd.Count == 0) return -1;
            BackStack(blockName).Push(currentIndex);
            return fwd.Pop();
        }

        public bool CanGoBack(string blockName)    => BackStack(blockName).Count > 0;
        public bool CanGoForward(string blockName) => FwdStack(blockName).Count  > 0;

        public IReadOnlyList<NavigationHistoryEntry> GetHistory(string blockName)
            => History(blockName).AsReadOnly();

        public void Clear(string blockName)
        {
            BackStack(blockName).Clear();
            FwdStack(blockName).Clear();
            History(blockName).Clear();
        }

        public void RemoveBlock(string blockName)
        {
            _backStacks.TryRemove(blockName, out _);
            _fwdStacks.TryRemove(blockName, out _);
            _histories.TryRemove(blockName, out _);
        }
    }
}
