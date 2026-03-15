---
name: beepservice
description: Guidance for initializing IDMEEditor and Beep services in desktop apps, including simple and hosted patterns.
---

# BeepService Initialization Guide

Use this skill when bootstrapping BeepDM in a WinForms or desktop app and wiring IDMEEditor, connections, and migrations.

## Scope
- Simple pattern: direct `DMEEditor` initialization
- Service pattern: `BeepDesktopServices` host + AppManager
- Connection creation and migrations

## Core Steps
1. Create editor or use `BeepDesktopServices.AppManager.DMEEditor`.
2. Build `ConnectionProperties` and add/update in `ConfigEditor`.
3. Open datasource and verify `ConnectionState.Open`.
4. Run `MigrationManager` to ensure schema.

## Validation
- Always check `ConnectionState.Open` from `OpenDataSource`.
- Check `IErrorsInfo.Flag == Errors.Ok` from migrations.

## Pitfalls
- Do not call `GetDataSource` before `OpenDataSource`.
- Do not re-create the editor per form; use a shared instance.
- Avoid swallowing migration errors; surface messages to logs/UI.

## File Locations
- Beep.Desktop/TheTechIdea.Beep.Desktop.Common/AppManager.cs
- Beep.Desktop/TheTechIdea.Beep.Desktop.Common/BeepServices.cs
- DataManagementEngineStandard/Editor/DM/DMEEditor.cs
- DataManagementEngineStandard/Migration/MigrationManager.cs

## Examples

### Simple Pattern
```csharp
var editor = new DMEEditor();
var props = AppDbContext.CreateSqliteConnectionProps(editor);

editor.ConfigEditor.AddDataConnection(props);
var state = editor.OpenDataSource(props.ConnectionName);
if (state != ConnectionState.Open)
{
    throw new InvalidOperationException("OpenDataSource failed");
}

var ds = editor.GetDataSource(props.ConnectionName);
var migration = new MigrationManager(editor, ds);
var result = migration.EnsureDatabaseCreated("YourApp.Entities", true, null);
if (result.Flag != Errors.Ok)
{
    throw new InvalidOperationException(result.Message);
}
```

### Service Pattern
```csharp
var builder = Host.CreateApplicationBuilder();
BeepDesktopServices.RegisterServices(builder);
var host = builder.Build();
BeepDesktopServices.ConfigureServices(host);

BeepDesktopServices.StartLoading(new[] { "Beep" }, showWaitForm: true);
var editor = BeepDesktopServices.AppManager.DMEEditor;
```

## Task-Specific Examples

### Update Or Add Connection Before Open
```csharp
var props = AppDbContext.CreateSqliteConnectionProps(editor);
var existing = editor.ConfigEditor.DataConnections
    .FirstOrDefault(c => c.ConnectionName == props.ConnectionName);

if (existing == null)
{
    editor.ConfigEditor.AddDataConnection(props);
}
else
{
    existing.ConnectionString = props.ConnectionString;
}

editor.ConfigEditor.SaveDataconnectionsValues();
editor.OpenDataSource(props.ConnectionName);
```

### Run Migrations After Load
```csharp
var ds = editor.GetDataSource(props.ConnectionName);
var migration = new MigrationManager(editor, ds);
var result = migration.EnsureDatabaseCreated("YourApp.Entities", true, null);
if (result.Flag != Errors.Ok)
{
    throw new InvalidOperationException(result.Message);
}
```