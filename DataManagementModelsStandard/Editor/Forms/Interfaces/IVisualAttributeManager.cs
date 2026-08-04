using System.Collections.Generic;
using TheTechIdea.Beep.Editor.UOWManager.Models;

namespace TheTechIdea.Beep.Editor.UOWManager.Interfaces
{
    /// <summary>
    /// Registers named <see cref="VisualAttribute"/>s and applies them to items
    /// and blocks — the engine side of the Oracle Forms Visual Attribute. Mirrors
    /// <c>ILOVManager</c>: the IDE authors registrations, the host asks
    /// <see cref="ResolveForItem"/> what to render.
    /// </summary>
    public interface IVisualAttributeManager
    {
        /// <summary>Registers or replaces a named visual attribute (keyed by <see cref="VisualAttribute.Name"/>).</summary>
        void RegisterVisualAttribute(VisualAttribute visualAttribute);

        /// <summary>The named attribute, or null when none is registered under that name.</summary>
        VisualAttribute GetVisualAttribute(string name);

        /// <summary>Every registered attribute.</summary>
        IReadOnlyList<VisualAttribute> GetVisualAttributes();

        /// <summary>Applies a registered attribute to an item.</summary>
        void ApplyToItem(string blockName, string itemName, string visualAttributeName);

        /// <summary>Applies a registered attribute to a whole block (its default for every item).</summary>
        void ApplyToBlock(string blockName, string visualAttributeName);

        /// <summary>The name of the attribute applied to an item, or null.</summary>
        string GetItemVisualAttribute(string blockName, string itemName);

        /// <summary>The name of the attribute applied to a block, or null.</summary>
        string GetBlockVisualAttribute(string blockName);

        /// <summary>Removes the attribute applied to an item.</summary>
        void ClearItem(string blockName, string itemName);

        /// <summary>
        /// The attribute a host should render for an item given its record state.
        /// The manager owns the precedence: the item's own attribute wins over the
        /// block default, and a state-override attribute applies only when that
        /// state is active. Returns null when nothing applies — the host then
        /// leaves the control at its theme default.
        /// </summary>
        VisualAttribute ResolveForItem(
            string blockName, string itemName,
            bool isCurrentRecord, bool isChangedRecord, bool inQueryMode);
    }
}
