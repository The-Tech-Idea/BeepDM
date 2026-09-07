using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Editor.Forms.Helpers;
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

            // IsReadOnly, like IsRequired below, is a plain bool with no
            // PropertyClass member and no "not authored" state distinct from
            // false -- so an authored `true` can only ADD the restriction on
            // top of whatever InsertAllowed/UpdateAllowed/the class already
            // computed above, never relax it. This was fully round-tripped
            // by the IDE's Block Fields editor (BlockFieldsEditorDialogData,
            // both load and save) and emitted into generated code
            // (DesignerBlockGenerator: "Ord.Fields[...].IsReadOnly = true;")
            // but never read by anything at runtime: both hosts'
            // WinFormBlockHost.cs/BeepWpfBlock.cs already compute
            // presenter.IsReadOnly from item.InsertAllowed/item.UpdateAllowed
            // depending on the current block mode, so an author who marked a
            // field Read Only in the IDE saw it round-trip perfectly and stay
            // fully editable at runtime. Deliberately does not also touch
            // item.Enabled -- that is IsEnabled's independent, already-wired
            // concept (a fully disabled/greyed-out control), and Oracle Forms
            // itself keeps "Enabled" and "Insert/Update Allowed" as separate
            // item properties; conflating them here would remove an author's
            // ability to set them independently, not fix a gap. QueryAllowed
            // is untouched for the same reason a read-only field must still
            // work as an Enter-Query search criterion.
            if (fieldDefinition.IsReadOnly)
            {
                item.InsertAllowed = false;
                item.UpdateAllowed = false;
            }

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

            // Width, like Label, has no PropertyClass member and no
            // meaningful "unauthored" value to fall back to other than 0 (=
            // the host's own default sizing) -- BlockFieldDefinition.Width
            // already defaults to 0 for an unauthored field, matching
            // ItemInfo.Width's own default, so this is a direct, always-safe
            // overlay rather than a conditional one.
            if (fieldDefinition.Width > 0)
                item.Width = fieldDefinition.Width;

            // One-directional, deliberately: BlockFieldDefinition.IsRequired
            // is a plain bool (no null = "not authored" state, unlike the
            // QueryAllowed/InsertAllowed/UpdateAllowed cluster above), and its
            // default (false) does not coincide with ItemInfo.Required's own
            // meaningful default -- RegisterItemsFromEntityStructure sets
            // item.Required from the live datasource's NOT NULL/nullability
            // metadata before this method ever runs. Unconditionally
            // overlaying, the way Enabled/Visible do (safe there because both
            // sides default to true), would silently force every
            // schema-required field optional the moment its author leaves
            // this field untouched -- the exact defect class this file
            // exists to catch, self-inflicted. So an authored `true` can
            // *add* required-ness a business rule calls for; an unauthored
            // (default) field always keeps whatever the schema already
            // determined. An author cannot use this to force a NOT NULL
            // column optional -- a known, accepted limitation of a
            // non-nullable authoring field, not an oversight.
            if (fieldDefinition.IsRequired)
                item.Required = true;

            // EditorKey, like Label and Width, has no PropertyClass member --
            // a direct field-only override. Unlike them, the "unauthored"
            // check is deliberately not just IsNullOrWhiteSpace: the field
            // editor's EditorKey box is free text, not a constrained
            // dropdown, so an author can type anything, including a value
            // outside the canonical set the runtime registries actually
            // switch on (a typo, or a WinForms-only control class name the
            // IDE's own designer-file scanner separately understands). Only
            // a value that normalizes to one of those canonical categories
            // is carried across; anything else leaves item.EditorKey null,
            // which the registry treats identically to "not authored" --
            // never a guess at what an unrecognised value might have meant.
            if (FieldTypeMapper.TryNormalizeEditorKey(fieldDefinition.EditorKey, out var editorKey))
                item.EditorKey = editorKey;
        }
    }
}
