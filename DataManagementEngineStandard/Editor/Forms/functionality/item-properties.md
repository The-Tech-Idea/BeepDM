# FormsManager — Item / Block Properties

This document covers `SET_ITEM_PROPERTY` / `GET_ITEM_PROPERTY` and the corresponding block-level properties. The engine implements these as a property bag with ~20 named properties; the host UI reads the bag to render the form.

## Concepts

- **`IItemPropertyManager`** — the engine-side property bag for items. ~20 properties.
- **`IBlockPropertyManager`** — the engine-side property bag for blocks. (Distinct from the `BlockProperty` enum on `FormsManager` which has its own typed shortcuts.)
- **`ItemPropertyEventArgs`** — the event args raised on property change.
- **Property names** — string keys (e.g. `"VISIBLE"`, `"ENABLED"`, `"REQUIRED"`). Case-insensitive.

## Access

The `FormsManager.ItemProperties` property exposes the `IItemPropertyManager`:

```csharp
// Set a property
manager.ItemProperties.SetItemProperty("ORDERS", "Status", "VISIBLE", false);

// Get a property
var visible = manager.ItemProperties.GetItemProperty("ORDERS", "Status", "VISIBLE");

// Bulk query — get all properties for an item
var all = manager.ItemProperties.GetAllItemProperties("ORDERS", "Status");
```

Returns `object` (boxed). Cast as needed:

```csharp
var isRequired = (bool)manager.ItemProperties.GetItemProperty("ORDERS", "CustomerId", "REQUIRED");
```

## The standard property names

The engine recognizes ~20 property names. They map to Oracle Forms' `SET_ITEM_PROPERTY` family:

| Property name | Type | Oracle Forms equivalent | Meaning |
| --- | --- | --- | --- |
| `VISIBLE` | bool | `VISIBLE` | The item is rendered. |
| `ENABLED` | bool | `ENABLED` | The item is editable. |
| `REQUIRED` | bool | `REQUIRED` | The item must have a value. Validated on commit. |
| `DEFAULT_VALUE` | object | `DEFAULT_VALUE` | The value to set on new records. Use `SetItemDefault` instead. |
| `FORMAT_MASK` | string | `FORMAT_MASK` | A format string for display. (Engine stores; host renders.) |
| `HINT` | string | `HINT` | Tooltip text. (Engine stores; host displays.) |
| `PROMPTText` | string | `PROMPT_TEXT` | The item's prompt / label. |
| `DATATYPE` | string | `DATATYPE` | "STRING" / "NUMBER" / "DATE" / etc. |
| `MAX_LENGTH` | int | `MAX_LENGTH` | String length cap. |
| `LOV_NAME` | string | `LOV_NAME` | The name of the LOV registered for this item. |
| `LOV_VALIDATION` | bool | `LOV_VALIDATION` | Whether to validate against the LOV on entry. |
| `UPDATE_ALLOWED` | bool | `UPDATE_ALLOWED` | Whether this item is updatable. |
| `INSERT_ALLOWED` | bool | `INSERT_ALLOWED` | Whether this item can be set on insert. |
| `QUERYABLE` | bool | `QUERYABLE` | Whether this item can be used in a query filter. |
| `CASE_RESTRICTION` | string | `CASE_RESTRICTION` | "UPPER" / "LOWER" / "MIXED". |
| `PROMPT_FONT_NAME` | string | n/a (UI) | Font for the prompt. |
| `PROMPT_FONT_SIZE` | int | n/a (UI) | Font size for the prompt. |
| `FONT_NAME` | string | n/a (UI) | Font for the value. |
| `FONT_SIZE` | int | n/a (UI) | Font size for the value. |
| `FOREGROUND_COLOR` | int | n/a (UI) | RGB color of the text. |
| `BACKGROUND_COLOR` | int | n/a (UI) | RGB color of the background. |
| `VISUAL_ATTRIBUTE` | string | `VISUAL_ATTRIBUTE` | The named visual attribute (font + color preset). |

UI-specific properties (font, color) are stored by the engine but rendered by the host. The engine does NOT validate that the values are reasonable — it just stores them.

## Block properties

`FormsManager.SetBlockProperty` and `GetBlockProperty` (in `BlockProperties.cs`) take a `BlockProperty` enum, not a string. This is the typed shortcut for the most common block-level properties. The enum is in `Models/BlockPropertyEnum.cs`:

| `BlockProperty` | Type | What it controls |
| --- | --- | --- |
| `INSERT_ALLOWED` | bool | Insert is permitted on this block. |
| `UPDATE_ALLOWED` | bool | Update is permitted. |
| `DELETE_ALLOWED` | bool | Delete is permitted. |
| `QUERY_ALLOWED` | bool | Query is permitted. |
| `DEFAULT_WHERE` | string | The default filter for every `EXECUTE_QUERY`. |
| `ORDER_BY` | string | The default order. |
| `NAVIGABLE` | bool | `GO_BLOCK` is permitted. |
| `RECORD_VISUAL_ATTRIBUTE` | string | The visual attribute for the current record. |
| `CURRENT_RECORD_VISUAL_ATTRIBUTE` | string | Alias of the above. |
| `INSERT_ALLOWED_FROM_QUERY` | bool | Insert is permitted even in Query mode. |
| `KEY_MODE` | string | "QUERY" / "NORMAL" — how KEY- triggers behave. |

For unmodeled block properties, use the lower-level `IBlockPropertyManager` (exposed via `manager.BlockProperties`) which is a string-keyed property bag like `IItemPropertyManager`.

## Events

`IItemPropertyManager` raises three events:

- `ItemPropertyChanged` — raised when any property of any item changes. Args include block, item, property name, new value.
- `ItemValueChanged` — raised when the item's *value* changes (different from a property change). Args include block, item, old value, new value.
- `ItemErrorChanged` — raised when the item's validation error state changes. Args include block, item, error message, severity.

The host UI subscribes to these to keep its display in sync with engine state.

## When properties take effect

Most properties take effect **immediately** (the next read returns the new value). The exceptions:

- `DEFAULT_VALUE` (via `SetItemDefault`) — takes effect on the next `CreateNewRecord` / `ApplyItemDefaults`. Not retroactive.
- `DEFAULT_WHERE` / `ORDER_BY` — takes effect on the next `EXECUTE_QUERY`. Not retroactive.
- `FORMAT_MASK` / `HINT` / font / color — take effect on the next render by the host. The engine stores them, the host reads them.

If a property change is "not yet effective", a `ItemPropertyChanged` event still fires. Hosts can use the event to re-render.

## Property values are not validated

The engine stores property values without validation. Setting `MAX_LENGTH` to a negative number, or `VISIBLE` to a non-bool, doesn't fail. The host UI is responsible for rendering reasonable values.

This matches Oracle Forms behavior — `SET_ITEM_PROPERTY` accepts whatever value you pass and the runtime tries to apply it. Garbage in, garbage out.

## How this differs from `FormsManager.SetBlockProperty`

`FormsManager.SetBlockProperty(name, BlockProperty, value)` is the **typed** block property API. The enum constrains the property name to known values. Internally, it stores the value in the same `IBlockPropertyManager` bag.

`manager.BlockProperties.SetBlockProperty(name, string, value)` is the **string-keyed** API. It accepts any property name. Use this for unmodeled properties.

Both APIs read from / write to the same backing store. There's no synchronization issue between them.

## `IBeepBuiltins.SetItemProperty` / `GetItemProperty`

The host-facing built-ins (in `IBeepBuiltins`) wrap this:

```csharp
// In the host's IBeepBuiltins impl
public bool SetItemProperty(string itemName, string property, object? value)
    => _manager.ItemProperties.SetItemProperty(_manager.CurrentBlockName, itemName, property, value);

public object? GetItemProperty(string itemName, string property)
    => _manager.ItemProperties.GetItemProperty(_manager.CurrentBlockName, itemName, property);
```

So the Oracle Forms `SET_ITEM_PROPERTY('block.item', 'property', value)` becomes the engine's `manager.ItemProperties.SetItemProperty(block, item, property, value)`. The host's IBeepBuiltins impl does the block-name lookup from `CurrentBlockName`.

## Notes for callers

- The `IItemPropertyManager` is **per-block**. The same property name on different blocks has different values. There's no "apply property to all blocks" method.
- The `VISUAL_ATTRIBUTE` property is a string that names a visual attribute preset (e.g. "Required", "Error", "Highlighted"). The host UI must define the visual attribute presets and render accordingly. The engine does not enforce.
- UI-specific properties (font, color) are stored as raw values (int for color RGB, string for font name, int for font size). The host interprets and renders. If the host is cross-platform (WinForms + Blazor), it should normalize the values to its platform's representation.
- The `IItemPropertyManager` is thread-safe for reads. Writes (e.g. from the host UI on a UI thread) are not synchronized with reads. If the host needs to write from a background thread, it should marshal to the UI thread first.

## See also

- [`security.md`](security.md) — per-field security uses a different mechanism (FieldSecurity, not a property bag).
- [`lov.md`](lov.md) — the `LOV_NAME` property ties an item to a registered LOV.
- [`validation.md`](validation.md) — the `REQUIRED` property is enforced by the validation system.
- [`ORACLE-FORMS-MAPPING.md`](../ORACLE-FORMS-MAPPING.md) sections 2, 10 — the block and item property mapping.
