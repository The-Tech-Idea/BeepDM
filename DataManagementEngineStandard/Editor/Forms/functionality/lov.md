# FormsManager — LOV (List of Values)

This document covers the LOV system: how to define a LOV, register it on a block, show it, and select from it.

## Overview

A "LOV" in Oracle Forms is a popup that shows a list of valid values for a field. The user picks one, the chosen value is written back to the bound field, and optionally some related fields are populated from the selected record.

FormsManager implements:
- `LOVDefinition` — the LOV's metadata (datasource, entity, key column, display column, field mappings).
- `LOVManager` — the engine-side orchestration (load, cache, validate).
- `ShowLOVAsync` — the orchestrator-level entry point.
- `IBeepBuiltins.ShowLov` / `PopupLov` / `ListValues` — the host-facing built-ins.
- `LOVResult` — the return value of `ShowLOVAsync`.

## Defining a LOV

```csharp
var customerLov = LOVDefinition
    .CreateLookup(
        name: "CUSTOMER_LOV",
        dataSourceName: "Northwind",
        entityName: "Customers",
        keyField: "CustomerId",
        displayField: "CompanyName")
    .MapField(sourceField: "CompanyName", targetField: "CustomerName")
    .MapField(sourceField: "ContactName",  targetField: "Contact")
    .MapField(sourceField: "Phone",        targetField: "Phone")
    .WithCache(TimeSpan.FromMinutes(5))
    .WithMaxRecords(500);

manager.LOV.RegisterLOV("ORDERS", "CustomerId", customerLov);
```

`LOVDefinition.CreateLookup` is a fluent builder. The chain:

1. **`CreateLookup(name, ds, entity, key, display)`** — the minimum: a datasource, an entity, a key field, and a display field.
2. **`.MapField(source, target)`** — when the user picks a row, this maps the `source` column in the LOV to the `target` field in the bound record. Repeat for each related field you want populated.
3. **`.WithCache(ttl)`** — enable caching. The LOV data is loaded once per TTL.
4. **`.WithMaxRecords(n)`** — cap the LOV at n rows (Oracle's `LOV_MAX_RECORDS`).

`MapField` is **additive** — every mapped field is populated on selection. The key field is always written back to the bound field (the "return value"), and any `MapField` calls add to that.

## Showing a LOV

### `ShowLOVAsync(string blockName, string fieldName, string? searchText, object? selectedRecord)`

The orchestrator-level entry point. Used when the user clicks the LOV button (or types F9) on a field.

Two modes:

1. **No selectedRecord** — opens the LOV dialog. The host UI renders the dialog. The user picks a row, the result is returned.
2. **With selectedRecord** — applies the LOV's field mappings to the current record using the given record as the source. No dialog is shown. This is the "I have the data, just write it back" path.

```csharp
// User clicked the LOV button
var result = await manager.ShowLOVAsync("ORDERS", "CustomerId", searchText: "alf");
if (result.IsSuccess && result.SelectedRecord != null)
{
    // The orchestrator already wrote the value back; no further action needed.
    // result.SelectedRecord has the chosen row.
}

// Programmatic (no dialog) — used by host UI when a row is selected from a different source
await manager.ShowLOVAsync("ORDERS", "CustomerId", selectedRecord: customerRecord);
```

### `IBeepBuiltins.ShowLov(blockName, fieldName, out selectedValue)` / `IBeepBuiltins.PopupLov(...)`

The host-facing built-ins. The host calls these, which route back to `ShowLOVAsync` (for `ShowLov`) or to a higher-level orchestration (for `PopupLov`, which returns the full record).

`ListValues(blockName, fieldName)` returns the LOV's records without a selection UI — the host uses this to render the LOV in a non-modal way (e.g. an autocomplete dropdown).

## How the orchestrator normalizes the return

`LOVManager` historically returned its value in an internal sentinel (`__RETURN_VALUE__`). The orchestrator now normalizes this to the actual bound field name. So when a host listens to `LOVDataLoaded` and reads the return value, the field name is the real one (e.g. `CustomerId`), not the sentinel.

This was the bug that the existing test slice ("LOV integration slice") exposed — see the README's test coverage section.

## Caching

`LOVDefinition.CacheEnabled` + `LOVDefinition.CacheTtl` controls the cache. The `LOVManager` keeps a per-LOV cache keyed by `(lovName, searchText)`. Cache invalidation:

- Automatic on TTL expiry.
- Manual via `LOVManager.InvalidateCache(lovName)`.
- On commit (the cache is invalidated for any block that has a dirty state, to prevent stale LOV data after a commit).

The cache is **in-memory** — it does not survive process restarts. There is no persistent LOV cache.

## Validation on entry

When a field is set programmatically (e.g. via `CopyFieldValue` or a type-in), the LOV validates the new value against the LOV's key field. If the value is not in the LOV, the `LOVValidationFailed` event fires with the offending value.

This is automatic for fields with a registered LOV — no opt-in needed. The validation is async (non-blocking) and runs in the background.

## Multi-column LOVs

A LOV can have **multiple display columns** in addition to the key:

```csharp
var productLov = LOVDefinition
    .CreateLookup("PRODUCT_LOV", "Northwind", "Products", "ProductId", "ProductName")
    .AddColumn("CategoryName", 150)   // display width
    .AddColumn("UnitPrice",     80)
    .AddColumn("Discontinued",  60);
```

The user sees multiple columns in the LOV dialog. The chosen row's `ProductId` is the return value; the `MapField` calls populate related fields.

Column widths are a hint for the host UI's dialog rendering. The engine does NOT enforce them — the host decides how to render.

## Filter (search text)

When the LOV dialog opens, the host typically shows a search field. The user types a fragment, the host passes it to `ShowLOVAsync(blockName, fieldName, searchText: "...")`. The engine forwards the search text to the datasource query.

The exact search semantics depend on the datasource. SQL datasources typically do a `LIKE '%searchText%'` on the display column. The engine does not enforce a specific search implementation; the datasource adapter decides.

## LOV events

| Event | Args | Raised when |
| --- | --- | --- |
| `LOVDataLoaded` | `LOVDataLoadedEventArgs` | The LOV's data was loaded from the datasource. |
| `LOVValidationFailed` | `LOVValidationEventArgs` | A value entered into the LOV-bound field is not in the LOV. |

## Host UI rendering

The engine does NOT render the LOV dialog — the host UI does. The host's `IBuiltinHost.ShowLovAsync` is called by the engine to render the dialog. The host returns the chosen record (or null if the user cancelled).

The engine handles:
- Loading the data (with caching).
- Validating the bound value on entry.
- Writing the chosen value back to the bound record (and any mapped fields).
- Raising the appropriate events.

The host handles:
- The dialog UI (WinForms `LookupDialog`, Blazor `<LovDialog>`, etc.).
- User interaction (typing, clicking, keyboard navigation).
- Returning the chosen record.

This split is why the LOV's display columns, widths, and styling are host concerns.

## Notes for callers

- Use `LOVDefinition.CreateLookup` (not `new LOVDefinition { ... }`) — the builder enforces required fields and gives you the fluent mapping API.
- `MapField` is per-LOV. If you register the same LOV on two fields, the mappings are independent.
- The cache invalidates on commit — this is **per-block**, not per-LOV. If multiple blocks share a LOV and one of them commits, the cache for that LOV is invalidated globally.
- The engine does NOT support `LOV`-column **reordering** or **hiding** at the engine level. The host can render the dialog however it wants; the engine just provides the data.
- The `LOVValidationFailed` event is for **type-in validation** — the value the user typed is not in the LOV. It does NOT fire when the user picks from the LOV dialog.

## See also

- [`architecture.md`](../architecture.md) — where `LOVManager` sits in the helper layer.
- [`item-properties.md`](item-properties.md) — the `LOV` property of an item, which references the registered LOV.
- [`ORACLE-FORMS-MAPPING.md`](../ORACLE-FORMS-MAPPING.md) section 9 — the LOV mapping.
