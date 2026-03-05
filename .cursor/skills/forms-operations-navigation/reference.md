# Forms Operations And Navigation Reference

This reference demonstrates full lifecycle and navigation flows with `FormsManager`.

## Scenario A: Full Form Lifecycle With Navigation

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.UOW;
using TheTechIdea.Beep.Editor.UOWManager;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Utilities;

public static class FormsOperationsNavigationExamples
{
    public static async Task RunLifecycleAsync(IDMEEditor editor)
    {
        var forms = new FormsManager(editor);

        // Wire optional events for operational visibility
        forms.OnFormOpen += (s, e) => Console.WriteLine($"Opening {e.FormName}");
        forms.OnFormCommit += (s, e) => Console.WriteLine($"Commit event for {e.FormName}");
        forms.OnNavigate += (s, e) => Console.WriteLine($"Navigate {e.NavigationType} in {e.BlockName}");
        forms.OnCurrentChanged += (s, e) => Console.WriteLine($"Current changed in {e.BlockName}");

        using var customerUow = new UnitofWork<Customer>(editor, "MyDb", "Customers", "Id");
        using var orderUow = new UnitofWork<Order>(editor, "MyDb", "Orders", "Id");

        var customerStructure = editor.GetDataSource("MyDb").GetEntityStructure("Customers", true);
        var orderStructure = editor.GetDataSource("MyDb").GetEntityStructure("Orders", true);

        forms.RegisterBlock("CUSTOMERS", customerUow, customerStructure, "MyDb", isMasterBlock: true);
        forms.RegisterBlock("ORDERS", orderUow, orderStructure, "MyDb", isMasterBlock: false);
        forms.CreateMasterDetailRelation("CUSTOMERS", "ORDERS", "Id", "CustomerId");

        if (!await forms.OpenFormAsync("CustomerOrderForm"))
        {
            throw new InvalidOperationException(forms.Status);
        }

        // Query + navigate master block
        var queryResult = await forms.ExecuteQueryAndEnterCrudModeAsync("CUSTOMERS");
        if (queryResult.Flag != Errors.Ok && queryResult.Flag != Errors.Warning)
        {
            throw new InvalidOperationException(queryResult.Message);
        }

        await forms.SwitchToBlockAsync("CUSTOMERS");
        await forms.FirstRecordAsync("CUSTOMERS");
        await forms.NextRecordAsync("CUSTOMERS");
        await forms.LastRecordAsync("CUSTOMERS");
        await forms.NavigateToRecordAsync("CUSTOMERS", 0);

        // Inspect navigation state
        NavigationInfo current = forms.GetCurrentRecordInfo("CUSTOMERS");
        Dictionary<string, NavigationInfo> all = forms.GetAllNavigationInfo();
        Console.WriteLine($"Current index={current?.CurrentIndex}, total={current?.TotalRecords}, blocks={all.Count}");

        // Commit and close
        var commit = await forms.CommitFormAsync();
        if (commit.Flag != Errors.Ok)
        {
            await forms.RollbackFormAsync();
        }

        await forms.CloseFormAsync();
    }
}
```

## Scenario B: Safe Close Pattern

```csharp
public static async Task<bool> SafeCloseAsync(FormsManager forms)
{
    // Let FormsManager apply its own unsaved-change handling and event cancellation logic
    var closed = await forms.CloseFormAsync();
    if (!closed)
    {
        // UI can surface forms.Status here
        return false;
    }

    return true;
}
```

## Notes
- Always check `bool`/`IErrorsInfo` from lifecycle calls.
- Navigation does not imply persistence; commit explicitly.
- Keep block switching explicit before block-scoped actions.

