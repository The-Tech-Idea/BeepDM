# Forms Mode Transitions Reference

This reference shows larger end-to-end transition flows for `FormsManager` with master-detail behavior.

## Scenario A: Query -> CRUD -> New Detail Record

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.UOW;
using TheTechIdea.Beep.Editor.UOWManager;
using TheTechIdea.Beep.Utilities;

public static class FormsModeTransitionExamples
{
    public static async Task RunQueryToCrudFlowAsync(IDMEEditor editor)
    {
        var forms = new FormsManager(editor);

        // Register master block
        using var customerUow = new UnitofWork<Customer>(editor, "MyDb", "Customers", "Id");
        var customerStructure = editor.GetDataSource("MyDb").GetEntityStructure("Customers", true);
        forms.RegisterBlock("CUSTOMERS", customerUow, customerStructure, "MyDb", isMasterBlock: true);

        // Register detail block
        using var orderUow = new UnitofWork<Order>(editor, "MyDb", "Orders", "Id");
        var orderStructure = editor.GetDataSource("MyDb").GetEntityStructure("Orders", true);
        forms.RegisterBlock("ORDERS", orderUow, orderStructure, "MyDb", isMasterBlock: false);

        // Link blocks
        forms.CreateMasterDetailRelation("CUSTOMERS", "ORDERS", "Id", "CustomerId");

        // 1) Enter query mode on master
        var enterQuery = await forms.EnterQueryModeAsync("CUSTOMERS");
        if (enterQuery.Flag != Errors.Ok)
        {
            throw new InvalidOperationException(enterQuery.Message);
        }

        // 2) Execute query and transition back to CRUD
        var filters = new List<AppFilter>
        {
            new AppFilter { FieldName = "IsActive", Operator = "=", FilterValue = true }
        };
        var executeQuery = await forms.ExecuteQueryAndEnterCrudModeAsync("CUSTOMERS", filters);
        if (executeQuery.Flag != Errors.Ok && executeQuery.Flag != Errors.Warning)
        {
            throw new InvalidOperationException(executeQuery.Message);
        }

        // Optional readiness check before another mode-sensitive operation
        var ready = await forms.IsFormReadyForModeTransitionAsync();
        if (!ready)
        {
            throw new InvalidOperationException("Form is not ready for mode transition.");
        }

        // 3) Create detail record in CRUD context
        var detailCrud = await forms.EnterCrudModeForNewRecordAsync("ORDERS");
        if (detailCrud.Flag != Errors.Ok)
        {
            throw new InvalidOperationException(detailCrud.Message);
        }

        // Status/diagnostics
        var blockMode = forms.GetBlockMode("ORDERS");
        var allModes = forms.GetAllBlockModeInfo();
        Console.WriteLine($"ORDERS mode: {blockMode}; tracked blocks: {allModes.Count}");
    }
}
```

## Scenario B: Transition Guardrails Before Entering Query

```csharp
public static async Task<bool> SafeEnterQueryAsync(FormsManager forms, string blockName)
{
    // Ensure dirty state is resolved first (save/discard/cancel path from events/config)
    var canProceed = await forms.CheckAndHandleUnsavedChangesAsync(blockName);
    if (!canProceed)
    {
        return false;
    }

    var result = await forms.EnterQueryModeAsync(blockName);
    if (result.Flag != Errors.Ok)
    {
        // Log or show result.Message in UI
        return false;
    }

    return true;
}
```

## Notes
- Always treat `Errors.Warning` as non-fatal but visible.
- For detail insertion, ensure a valid master current row exists.
- Prefer transition APIs over direct mode mutation.

