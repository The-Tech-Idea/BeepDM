# FormsManager — Security

This document covers the security subsystem: per-block and per-field security, role-based access checks, and field masking.

## Concepts

- **`SecurityContext`** — the current user's identity and roles. Set on the manager.
- **`BlockSecurity`** — the per-block permissions: insert, update, delete, query allowed.
- **`FieldSecurity`** — the per-field permissions: visible, enabled, maskable.
- **`SecurityPermission`** — the enum of permissions: Query, Insert, Update, Delete, Execute, etc.
- **`SecurityManager`** — the orchestrator's helper that owns the checks.
- **`SecurityViolationEventArgs`** — the event args raised on a violation.

## Setting the security context

```csharp
manager.SetSecurityContext(new SecurityContext
{
    UserName = "fahad",
    Roles = new List<SecurityRole>
    {
        new SecurityRole
        {
            Name = "OrderClerk",
            Permissions = SecurityPermission.Query | SecurityPermission.Update
        },
        new SecurityRole
        {
            Name = "OrderAdmin",
            Permissions = SecurityPermission.Query | SecurityPermission.Insert |
                         SecurityPermission.Update | SecurityPermission.Delete
        }
    }
});
```

`SetSecurityContext` replaces the previous context. To add a role temporarily, modify the `SecurityContext.Roles` list directly.

A `SecurityContext` with no roles is **read-only** — every check returns `false` for write permissions but `true` for query.

## Per-block security

```csharp
manager.SetBlockSecurity("ORDERS", new BlockSecurity
{
    InsertAllowed = true,
    UpdateAllowed = true,
    DeleteAllowed = false,
    QueryAllowed  = true
});
```

Or use the boolean shortcuts:

```csharp
manager.SetInsertAllowed("ORDERS", true);
manager.SetUpdateAllowed("ORDERS", true);
manager.SetDeleteAllowed("ORDERS", false);
manager.SetQueryAllowed("ORDERS", true);
```

The per-block security is checked at every block operation:

- `InsertRecordAsync` checks `BlockSecurity.InsertAllowed`.
- `UpdateCurrentRecordAsync` checks `BlockSecurity.UpdateAllowed`.
- `DeleteCurrentRecordAsync` checks `BlockSecurity.DeleteAllowed`.
- `ExecuteQueryAsync` checks `BlockSecurity.QueryAllowed`.
- `SwitchToBlockAsync` checks `BlockSecurity.Navigable` (if set).

A failed check raises `OnSecurityViolation` and the operation returns `false` / `Errors.Failed`.

## Per-field security

```csharp
manager.SetFieldSecurity("ORDERS", "Salary", new FieldSecurity
{
    Visible  = true,
    Enabled  = false,    // can be displayed but not edited
    Maskable = true
});
```

Per-field security is applied to the `IItemPropertyManager`. When a field is `Enabled = false`, the `IItemPropertyManager.SetItemProperty` returns `false` for write attempts. When a field is `Maskable = true`, the value is masked on read (see below).

## Field masking

```csharp
var masked = manager.GetMaskedFieldValue("ORDERS", "Salary", 75000.00m);
// masked = "*****" (or similar)
```

The masking is a **format-string substitution** — the engine doesn't do real encryption or hashing. The actual format is a `FieldMaskProvider` (pluggable via `IFieldMaskProvider`). The default implementation replaces all characters with `*`.

A common pattern: use masking for display in a list view, unmask in a detail view. The host UI decides which view to show.

## `IsBlockAllowed` (programmatic check)

```csharp
if (manager.IsBlockAllowed("ORDERS", SecurityPermission.Delete))
{
    // Show the delete button
    deleteButton.Visible = true;
}
else
{
    deleteButton.Visible = false;
}
```

Use this for UI gating: only show buttons / menu items that the current user is allowed to use. The check is the same one the engine uses internally for the operation.

## `GetSecurityViolations`

```csharp
var violations = manager.GetSecurityViolations();
foreach (var v in violations)
{
    Console.WriteLine($"{v.UserName} tried {v.Operation} on {v.BlockName}.{v.FieldName}: {v.Reason}");
}
```

Returns the in-memory log of all security violations since the last `ClearSecurityViolations` (which is not yet a public method — see [gaps](../gaps.md)).

## Security violation event

`SecurityManager.OnSecurityViolation` raises when a check fails. Args include:

- The user name.
- The block (or null if not block-scoped).
- The field (or null if not field-scoped).
- The permission that was denied.
- The reason (human-readable).
- Whether the operation was allowed to continue (typically `false`).

A host UI can subscribe to this event to log security violations to its own audit / monitoring system.

## Security flow (worked example: `InsertRecordAsync`)

1. **Block security check** — `IsBlockAllowed(blockName, SecurityPermission.Insert)`.
2. **If denied** — raise `OnSecurityViolation`, add to violations list, return `false`.
3. **If allowed** — proceed with the insert.
4. **Per-field security check** — for each field in the record, `_itemPropertyManager` checks `FieldSecurity.Enabled` when the UoW applies the value. (Currently the per-field check is on write; reads are unrestricted unless the field is masked.)
5. **Field masking** — if the field is masked, the displayed value is the masked form.

## The `SecurityPermission` enum

Currently ~10 permission values. Common ones:

| Permission | Meaning |
| --- | --- |
| `Query` | Can read / execute_query. |
| `Insert` | Can insert new records. |
| `Update` | Can update existing records. |
| `Delete` | Can delete records. |
| `Execute` | Can call stored procedures / engine-level execute methods. |
| `Navigate` | Can `GO_BLOCK` to this block. |
| `Commit` | Can call `COMMIT_FORM`. |
| `Rollback` | Can call `ROLLBACK_FORM`. |
| `Administer` | Can configure the block (security, properties, etc.). |

The full enum is in `Models/SecurityModels.cs`. Some values are reserved for future use.

## Authentication is the host's job

The engine does **not** authenticate users. It assumes the host has already authenticated the user and provides a `SecurityContext` to the manager. The manager trusts the context.

A common pattern:

```csharp
// Host authenticates the user (e.g. via Windows auth, OAuth, or its own auth)
var user = await authService.GetCurrentUserAsync();

manager.SetSecurityContext(new SecurityContext
{
    UserName = user.Name,
    Roles    = user.Roles.Select(r => new SecurityRole
    {
        Name = r.Name,
        Permissions = r.Permissions
    }).ToList()
});
```

The engine then enforces the role-based permissions.

## Notes for callers

- `SecurityContext` is **per-instance** — it lives in the `FormsManager` instance. Two `FormsManager` instances in the same process have independent contexts.
- The `OnSecurityViolation` event is **synchronous** and fires on the caller's thread.
- Field masking is a **display concern** — the engine masks for `GetMaskedFieldValue`, but the actual data in the record is unchanged. The host UI is responsible for calling `GetMaskedFieldValue` instead of the raw record property when displaying a masked field.
- The engine does **not** enforce row-level security (e.g. "user X can only see rows where CreatedBy = X"). This is typically implemented as a `DefaultWhere` filter on the block, set during block registration.
- The engine does **not** enforce column-level encryption. The masking is a display-time substitution only.

## See also

- [`architecture.md`](../architecture.md) — where `SecurityManager` sits in the helper layer.
- [`audit.md`](audit.md) — the audit log (different concern; security violations vs data change tracking).
- [`ORACLE-FORMS-MAPPING.md`](../ORACLE-FORMS-MAPPING.md) section 15 — the security mapping.
