# Forms Enhanced Data Operations Reference

This reference demonstrates robust end-to-end CRUD using enhanced operations in `FormsManager`.

## Scenario A: Insert + Update + Query Roundtrip

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.UOW;
using TheTechIdea.Beep.Editor.UOWManager;
using TheTechIdea.Beep.Utilities;

public static class FormsEnhancedDataOpsExamples
{
    public static async Task RunCrudRoundtripAsync(IDMEEditor editor)
    {
        var forms = new FormsManager(editor);

        using var orderUow = new UnitofWork<Order>(editor, "MyDb", "Orders", "Id");
        var orderStructure = editor.GetDataSource("MyDb").GetEntityStructure("Orders", true);
        forms.RegisterBlock("ORDERS", orderUow, orderStructure, "MyDb", isMasterBlock: true);

        await forms.OpenFormAsync("OrderForm");

        // 1) Enter CRUD-ready state for new record
        var enterCrud = await forms.EnterCrudModeForNewRecordAsync("ORDERS");
        if (enterCrud.Flag != Errors.Ok)
        {
            throw new InvalidOperationException(enterCrud.Message);
        }

        // 2) Let manager create record, then fill fields
        var newRecord = forms.CreateNewRecord("ORDERS");
        if (newRecord == null)
        {
            throw new InvalidOperationException("CreateNewRecord returned null");
        }

        forms.ApplyAuditDefaults(newRecord, Environment.UserName);
        // Use your record type or helper to set values
        // Example: ((Order)newRecord).Status = "NEW";

        var insert = await forms.InsertRecordEnhancedAsync("ORDERS", newRecord);
        if (insert.Flag != Errors.Ok)
        {
            throw new InvalidOperationException(insert.Message);
        }

        // 3) Update current record
        var update = await forms.UpdateCurrentRecordAsync("ORDERS");
        if (update.Flag != Errors.Ok)
        {
            throw new InvalidOperationException(update.Message);
        }

        // 4) Query with filters via enhanced path
        var filters = new List<AppFilter>
        {
            new AppFilter { FieldName = "Status", Operator = "=", FilterValue = "NEW" }
        };
        var query = await forms.ExecuteQueryEnhancedAsync("ORDERS", filters);
        if (query.Flag != Errors.Ok && query.Flag != Errors.Warning)
        {
            throw new InvalidOperationException(query.Message);
        }

        Console.WriteLine($"Records in ORDERS: {forms.GetRecordCount("ORDERS")}");

        await forms.CommitFormAsync();
        await forms.CloseFormAsync();
    }
}
```

## Scenario B: Copy Fields Between Records

```csharp
public static object CloneOrderWithSelectedFields(FormsManager forms, string blockName)
{
    var source = forms.GetCurrentRecord(blockName);
    if (source == null)
    {
        return null;
    }

    var target = forms.CreateNewRecord(blockName);
    if (target == null)
    {
        return null;
    }

    var copied = forms.CopyFields(
        source,
        target,
        "CustomerId",
        "OrderDate",
        "CurrencyCode",
        "Notes");

    if (copied)
    {
        forms.ApplyAuditDefaults(target, Environment.UserName);
    }

    return target;
}
```

## Notes
- Keep enhanced operations as the single CRUD entry point for consistency.
- Always inspect `IErrorsInfo` and preserve warning/error context.
- Use explicit field lists for safe copy behavior.

