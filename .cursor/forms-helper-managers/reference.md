# Forms Helper Managers Reference

This reference demonstrates end-to-end helper manager usage with events, dirty state, and relationship synchronization.

## Scenario: Build A Triggered Master-Detail Form

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

public static class FormsHelperManagerExamples
{
    public static async Task ConfigureHelpersAndRunAsync(IDMEEditor editor)
    {
        var forms = new FormsManager(editor);

        // Register blocks
        using var customerUow = new UnitofWork<Customer>(editor, "MyDb", "Customers", "Id");
        using var orderUow = new UnitofWork<Order>(editor, "MyDb", "Orders", "Id");
        var customerStructure = editor.GetDataSource("MyDb").GetEntityStructure("Customers", true);
        var orderStructure = editor.GetDataSource("MyDb").GetEntityStructure("Orders", true);
        forms.RegisterBlock("CUSTOMERS", customerUow, customerStructure, "MyDb", isMasterBlock: true);
        forms.RegisterBlock("ORDERS", orderUow, orderStructure, "MyDb", isMasterBlock: false);

        // RelationshipManager path
        forms.CreateMasterDetailRelation("CUSTOMERS", "ORDERS", "Id", "CustomerId");

        // DirtyStateManager event path
        forms.OnUnsavedChanges += async (s, e) =>
        {
            // Example policy: auto-save on navigation pressure
            e.UserChoice = UnsavedChangesAction.Save;
            await Task.CompletedTask;
        };

        // EventManager validation path
        forms.OnValidateField += (s, e) =>
        {
            if (e.FieldName == "Email" && string.IsNullOrWhiteSpace(e.Value?.ToString()))
            {
                e.IsValid = false;
                e.ErrorMessage = "Email is required";
            }
        };

        forms.OnError += (s, e) =>
        {
            Console.WriteLine($"[{e.BlockName}] {e.ErrorMessage}");
        };

        // Form flow
        await forms.OpenFormAsync("CustomerOrderForm");
        await forms.ExecuteQueryAndEnterCrudModeAsync("CUSTOMERS");

        // Force detail sync on master context change
        await forms.SynchronizeDetailBlocksAsync("CUSTOMERS");

        // Example of dirty-state guard before a transition
        var canProceed = await forms.CheckAndHandleUnsavedChangesAsync("CUSTOMERS");
        if (!canProceed)
        {
            return;
        }

        await forms.CloseFormAsync();
    }
}
```

## Scenario: Using Simulation Helpers During Record Preparation

```csharp
public static void ApplyRecordDefaults(FormsManager forms, object record)
{
    // Via FormsManager helper path
    forms.ApplyAuditDefaults(record, Environment.UserName);

    // Explicit field copy example for derived record
    var clone = forms.CreateNewRecord("ORDERS");
    if (clone != null)
    {
        forms.CopyFields(record, clone, "CustomerId", "OrderDate", "Status");
    }
}
```

## Notes
- Ensure block registration happens before relationship creation.
- Keep event subscriptions centralized and remove them when UI/form closes.
- Prefer manager APIs over direct helper invocation from scattered call sites.

