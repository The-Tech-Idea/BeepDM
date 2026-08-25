using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;

namespace TheTechIdea.Beep.Editor.UOWManager.Helpers
{
    /// <summary>
    /// Engine-side Property Class store. Holds named property bundles and
    /// applies one to an item's <see cref="ItemInfo"/>, honoring whatever the
    /// field itself already authored. Mirrors <c>VisualAttributeManager</c>.
    /// </summary>
    public class PropertyClassManager : IPropertyClassManager
    {
        private readonly ConcurrentDictionary<string, PropertyClass> _registered =
            new(StringComparer.OrdinalIgnoreCase);

        public void RegisterPropertyClass(PropertyClass propertyClass)
        {
            if (propertyClass == null || string.IsNullOrWhiteSpace(propertyClass.Name))
                return;
            _registered[propertyClass.Name] = propertyClass;
        }

        public PropertyClass GetPropertyClass(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            return _registered.TryGetValue(name, out var propertyClass) ? propertyClass : null;
        }

        public IReadOnlyList<PropertyClass> GetPropertyClasses() => _registered.Values.ToList();

        public void RemovePropertyClass(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            _registered.TryRemove(name, out _);
        }

        public void ApplyToItem(ItemInfo item, BlockFieldDefinition fieldDefinition)
        {
            if (item == null || fieldDefinition == null) return;

            var propertyClass = string.IsNullOrWhiteSpace(fieldDefinition.PropertyClassName)
                ? null
                : GetPropertyClass(fieldDefinition.PropertyClassName);

            // Precedence: the field's own authored value wins; the class
            // fills a gap the field left open; anything neither says keeps
            // whatever the item already had (its entity-structure-derived
            // default from RegisterItemsFromEntityStructure).
            item.QueryAllowed = fieldDefinition.QueryAllowed ?? propertyClass?.QueryAllowed ?? item.QueryAllowed;
            item.InsertAllowed = fieldDefinition.InsertAllowed ?? propertyClass?.InsertAllowed ?? item.InsertAllowed;
            item.UpdateAllowed = fieldDefinition.UpdateAllowed ?? propertyClass?.UpdateAllowed ?? item.UpdateAllowed;

            var formatMask = fieldDefinition.FormatMask ?? propertyClass?.FormatMask;
            if (formatMask != null) item.FormatMask = formatMask;

            if (fieldDefinition.HasDefaultValue)
                item.DefaultValue = fieldDefinition.DefaultValue;
            else if (propertyClass != null && propertyClass.HasDefaultValue)
                item.DefaultValue = propertyClass.DefaultValue;

            var copyFrom = fieldDefinition.CopyValueFromItem ?? propertyClass?.CopyValueFromItem;
            if (copyFrom != null) item.CopyValueFromItem = copyFrom;

            // ItemInfo.Create defaults PromptText to the raw field name
            // (ItemInfo.cs:318) — both hosts already read PromptText as the
            // visible field label (WinFormBlockHost.cs/BeepWpfBlock.cs
            // presenter.Label / label.Text) and grid column caption
            // (*.GridMode.cs ColumnCaption). The IDE has always emitted an
            // authored Label onto BlockFieldDefinition
            // (DesignerBlockGenerator.cs), but nothing carried it across to
            // ItemInfo: every authored caption ("Order ID") was silently
            // discarded and every field showed its raw column name
            // ("OrderId") instead. PropertyClass has no Label member, so
            // this is a direct field-only override, the same shape as
            // Enabled/Visible below.
            if (!string.IsNullOrWhiteSpace(fieldDefinition.Label))
                item.PromptText = fieldDefinition.Label;

            // IsEnabled/IsVisible are plain per-field flags, not part of the
            // Property Class inheritance model (PropertyClass has no
            // Enabled/Visible member, unlike the nullable cluster above), so
            // they apply directly rather than through the field-then-class-
            // then-existing fallback chain. Both default to true on
            // BlockFieldDefinition, matching ItemInfo's own defaults, so a
            // field that authors neither is a no-op.
            item.Enabled = fieldDefinition.IsEnabled;
            item.Visible = fieldDefinition.IsVisible;
        }
    }
}
