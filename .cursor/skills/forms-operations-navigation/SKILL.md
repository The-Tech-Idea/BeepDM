---
name: forms-operations-navigation
description: Detailed guidance for FormsManager form lifecycle, navigation, and block-level movement. Use when implementing OpenFormAsync, CloseFormAsync, CommitFormAsync, RollbackFormAsync, record navigation, and block switching behavior.
---

# Forms Operations And Navigation

Use this skill for end-user form lifecycle behavior and record navigation patterns.

## Additional Resources
- End-to-end scenarios: [reference.md](reference.md)

## Form Lifecycle APIs
- `OpenFormAsync(string formName)`
- `CloseFormAsync()`
- `CommitFormAsync()`
- `RollbackFormAsync()`
- `ClearAllBlocksAsync()`
- `ClearBlockAsync(string blockName)`
- `ValidateForm()`

## Navigation APIs
- `FirstRecordAsync(string blockName)`
- `NextRecordAsync(string blockName)`
- `PreviousRecordAsync(string blockName)`
- `LastRecordAsync(string blockName)`
- `NavigateToRecordAsync(string blockName, int recordIndex)`
- `SwitchToBlockAsync(string blockName)`
- `GetCurrentRecordInfo(string blockName)`
- `GetAllNavigationInfo()`

## Event Hooks
- Form events: `OnFormOpen`, `OnFormClose`, `OnFormCommit`, `OnFormRollback`, `OnFormValidate`
- Navigation events: `OnNavigate`, `OnCurrentChanged`

## Safe Lifecycle Pattern
```csharp
if (!await forms.OpenFormAsync("CustomerForm"))
{
    return;
}

var commitResult = await forms.CommitFormAsync();
if (commitResult.Flag != Errors.Ok)
{
    await forms.RollbackFormAsync();
}

await forms.CloseFormAsync();
```

## Safe Navigation Pattern
```csharp
if (!await forms.SwitchToBlockAsync("CUSTOMERS"))
{
    return;
}

if (!await forms.NextRecordAsync("CUSTOMERS"))
{
    // no next row or navigation cancelled
}

var nav = forms.GetCurrentRecordInfo("CUSTOMERS");
```

## Rules For Correct Behavior
1. Always switch block explicitly before block-scoped actions.
2. Assume navigation can be cancelled by validation or unsaved-change logic.
3. Always check return values for form and navigation methods.
4. Commit/rollback should run through form-level APIs, not block-by-block manual calls.
5. Use `ValidateForm()` as a gate before high-impact operations when required.

## Unsaved Changes Handling
- Navigation and block switching call unsaved-change handling internally.
- Close flow can be cancelled by event handlers or unsaved-change policy.
- `IsDirty` should be treated as a global form-level signal.

## Common Pitfalls
- Attempting to close form without respecting unsaved-change decisions.
- Assuming navigation success means data save happened (it does not).
- Clearing blocks directly before running commit/rollback.
- Calling navigation against a non-current, non-registered block.

## Diagnostics
- Check `forms.Status` for last operation status string.
- Use `GetAllNavigationInfo()` to inspect per-block cursor positions.
- Wire `OnNavigate` and `OnCurrentChanged` for runtime traceability.

