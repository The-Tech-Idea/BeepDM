# FormsManager Migration Guide

This guide is for consumers moving from older `UnitofWorksManager`-style usage or custom form orchestration glue to the current `FormsManager` surface.

## 1. Update the naming model

- The concrete runtime class is `FormsManager`.
- The compatibility interface remains `IUnitofWorksManager`.
- Older docs may still say `UnitofWorksManager`; use `FormsManager` in new code and documentation.

## 2. Keep orchestration in FormsManager and persistence in IUnitofWork

Move form behavior into FormsManager and keep datasource work inside the UoW.

Before:

```csharp
customerUow.Commit();
orderUow.Commit();
relationshipHelper.SynchronizeDetailBlocksAsync("CUSTOMERS");
```

After:

```csharp
await manager.CommitFormAsync();
manager.CreateMasterDetailRelation("CUSTOMERS", "ORDERS", "CustomerId", "CustomerId");
```

FormsManager now owns:

- block registration and current-block state
- master/detail registration and synchronization
- mode transitions and dirty-state prompts
- form-level commit, rollback, and navigation coordination

IUnitofWork remains responsible for:

- actual insert, update, delete, and query execution
- dirty-state tracking
- datasource-generated identities and commit behavior

## 3. Adopt typed block registration

If you previously registered blocks without preserving CLR type, switch to the generic overload so new-record flows and typed accessors stay correct.

Before:

```csharp
manager.RegisterBlock("CUSTOMERS", customerUow, customerEntity, "Northwind", true);
```

After:

```csharp
manager.RegisterBlock<CustomerDto>("CUSTOMERS", customerUow, customerEntity, "Northwind", true);

var customerBlock = manager.GetBlock<CustomerDto>("CUSTOMERS");
await manager.InsertRecordAsync<CustomerDto>("CUSTOMERS", new CustomerDto());
```

If `customerUow.EntityStructure` is already populated, you can omit the explicit entity metadata:

```csharp
manager.RegisterBlock<CustomerDto>("CUSTOMERS", customerUow, "Northwind", true);
```

## 4. Move LOV registration to `manager.LOV` and selection to `ShowLOVAsync`

LOV registration lives on the helper-manager surface, but record population belongs to FormsManager.

Before:

```csharp
lovManager.RegisterLOV("ORDERS", "CustomerId", lov);
var values = lovManager.GetRelatedFieldValues(lov, selectedRecord);
```

After:

```csharp
manager.LOV.RegisterLOV("ORDERS", "CustomerId", lov);

var lovResult = await manager.ShowLOVAsync("ORDERS", "CustomerId", searchText: "alf");
var selected = lovResult.Records.FirstOrDefault();

if (selected != null)
{
    await manager.ShowLOVAsync("ORDERS", "CustomerId", selectedRecord: selected);
}
```

Behavioral note: `ShowLOVAsync` now writes the selected return value back to the requested field name and then applies related-field mappings. This closes a gap that appeared when callers relied on `LOVManager`'s internal `__RETURN_VALUE__` sentinel directly.

## 5. Prefer built-in APIs over custom glue

Older integrations often hand-rolled these behaviors. Migrate them to the built-in FormsManager surface when possible.

- Block flags and default query clauses: `SetBlockProperty`, `SetDefaultWhere`, `SetOrderBy`
- Alerts and confirmations: `ShowAlertAsync`, `ConfirmAsync`, `ShowInfoAsync`
- Sequences and defaults: `GetNextSequence`, `SetItemDefault`, `CopyFieldValue`
- Timers: `CreateTimer`, `DeleteTimer`, `GetTimer`
- Undo and export/import: `SetBlockUndoEnabled`, `UndoBlock`, `ExportBlockToJsonAsync`, `ImportBlockFromCsvAsync`
- Paging and cache: `SetBlockPageSize`, `LoadPageAsync`, `SetFetchAheadDepth`, `InvalidateBlockCache`

## 6. Migrate multi-form behavior to the registry and message bus

If you previously passed state through static variables or UI-only callbacks, move to FormsManager's multi-form support.

- Modal child form: `CallFormAsync`
- Modeless form: `OpenFormAsync(string formName, Dictionary<string, object> parameters)`
- Replace current form: `NewFormAsync`
- Return data to caller: `ReturnToCallerAsync`
- Shared globals: `SetGlobalVariable`, `GetGlobalVariable`
- Inter-form messaging: `PostMessage`, `SubscribeToMessage`, `UnsubscribeFromMessage`
- Shared data blocks: `CreateSharedBlock`

## 7. Review key-generation behavior

If older code assumed FormsManager would always allocate a key, align with the current precedence rules.

1. Preserve explicit caller or trigger keys.
2. Leave datasource-managed identities unset until insert or commit refresh completes.
3. Prefer datasource or UoW sequence generation over the in-memory sequence provider.
4. Use FormsManager sequences for Oracle-style built-ins or test-only flows.
5. Handle composite keys explicitly.

## 8. Review master/detail ownership

`IRelationshipManager` is deprecated as a standalone public orchestration model. Current master/detail rules live in FormsManager and its block metadata.

- Register master/detail relationships through `CreateMasterDetailRelation(...)`.
- Let FormsManager resolve keys and synchronize detail blocks.
- Keep UoWs focused on entity persistence, not relationship registration.

## 9. Add audit, security, and paging deliberately

These surfaces are now first-class and should replace ad-hoc wrappers when you need the behavior.

- Audit: `SetAuditUser`, `ConfigureAudit`, `GetAuditLog`, `ExportAuditToCsvAsync`
- Security: `SetSecurityContext`, `SetBlockSecurity`, `SetFieldSecurity`, `GetMaskedFieldValue`
- Paging: `SetBlockPageSize`, `SetTotalRecordCount`, `LoadPageAsync`, `Paging`

## Migration checklist

1. Replace stale `UnitofWorksManager` references in code comments and wrappers with `FormsManager`.
2. Switch block registration to `RegisterBlock<T>` where a real CLR type exists.
3. Move master/detail registration and synchronization into FormsManager.
4. Register LOVs via `manager.LOV` and apply selections through `ShowLOVAsync`.
5. Replace custom alert, timer, sequence, and paging wrappers with FormsManager built-ins.
6. Move modal or modeless form communication to the registry, globals, or message bus.
7. Re-test identity, sequence, and composite-key flows end to end after migration.