using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;

namespace TheTechIdea.Beep.Editor.UOWManager.Helpers
{
    /// <summary>
    /// Engine-side Visual Attribute store. Holds named attributes and the
    /// item/block they are applied to, and resolves the attribute a host should
    /// render for an item given its record state. Mirrors <c>LOVManager</c>.
    /// </summary>
    public class VisualAttributeManager : IVisualAttributeManager
    {
        private readonly ConcurrentDictionary<string, VisualAttribute> _registered =
            new(StringComparer.OrdinalIgnoreCase);

        // block.item -> attribute name, and block -> attribute name.
        private readonly ConcurrentDictionary<string, string> _itemApplied =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, string> _blockApplied =
            new(StringComparer.OrdinalIgnoreCase);

        private static string ItemKey(string block, string item) => $"{block}{item}";

        public void RegisterVisualAttribute(VisualAttribute visualAttribute)
        {
            if (visualAttribute == null || string.IsNullOrWhiteSpace(visualAttribute.Name))
                return;
            _registered[visualAttribute.Name] = visualAttribute;
        }

        public VisualAttribute GetVisualAttribute(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            return _registered.TryGetValue(name, out var va) ? va : null;
        }

        public IReadOnlyList<VisualAttribute> GetVisualAttributes() => _registered.Values.ToList();

        public void ApplyToItem(string blockName, string itemName, string visualAttributeName)
        {
            if (string.IsNullOrWhiteSpace(blockName) || string.IsNullOrWhiteSpace(itemName)) return;
            if (string.IsNullOrWhiteSpace(visualAttributeName))
            {
                ClearItem(blockName, itemName);
                return;
            }
            _itemApplied[ItemKey(blockName, itemName)] = visualAttributeName;
        }

        public void ApplyToBlock(string blockName, string visualAttributeName)
        {
            if (string.IsNullOrWhiteSpace(blockName)) return;
            if (string.IsNullOrWhiteSpace(visualAttributeName))
            {
                _blockApplied.TryRemove(blockName, out _);
                return;
            }
            _blockApplied[blockName] = visualAttributeName;
        }

        public string GetItemVisualAttribute(string blockName, string itemName)
        {
            if (string.IsNullOrWhiteSpace(blockName) || string.IsNullOrWhiteSpace(itemName)) return null;
            return _itemApplied.TryGetValue(ItemKey(blockName, itemName), out var name) ? name : null;
        }

        public string GetBlockVisualAttribute(string blockName)
        {
            if (string.IsNullOrWhiteSpace(blockName)) return null;
            return _blockApplied.TryGetValue(blockName, out var name) ? name : null;
        }

        public void ClearItem(string blockName, string itemName)
        {
            if (string.IsNullOrWhiteSpace(blockName) || string.IsNullOrWhiteSpace(itemName)) return;
            _itemApplied.TryRemove(ItemKey(blockName, itemName), out _);
        }

        public VisualAttribute ResolveForItem(
            string blockName, string itemName,
            bool isCurrentRecord, bool isChangedRecord, bool inQueryMode)
        {
            // Item's own attribute wins over the block default.
            var name = GetItemVisualAttribute(blockName, itemName)
                       ?? GetBlockVisualAttribute(blockName);
            if (name == null) return null;

            var va = GetVisualAttribute(name);
            if (va == null) return null;

            // The manager owns the state precedence so a host asks one question.
            // An attribute with NO override flags is the item's base styling and
            // always applies. An attribute that overrides a specific state applies
            // only while that state is active; otherwise the item falls back to
            // the theme default (null).
            var hasStateOverride =
                va.CurrentRecordOverride || va.ChangedRecordOverride || va.QueryModeOverride;
            if (!hasStateOverride) return va;

            if (va.CurrentRecordOverride && isCurrentRecord) return va;
            if (va.ChangedRecordOverride && isChangedRecord) return va;
            if (va.QueryModeOverride && inQueryMode) return va;

            return null;
        }
    }
}
