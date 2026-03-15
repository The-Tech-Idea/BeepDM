# BeepService Quick Reference

Quick reference for initializing IDMEEditor and BeepService patterns.

## Simple Pattern

```csharp
// 1. Create DMEEditor
var editor = new DMEEditor();

// 2. Create connection properties
var connectionProps = AppDbContext.CreateSqliteConnectionProps(editor);

// 3. Add to ConfigEditor
editor.ConfigEditor.DataConnections.Add(connectionProps);

// 4. Open datasource
var state = editor.OpenDataSource(connectionProps.ConnectionName);
var dataSource = editor.GetDataSource(connectionProps.ConnectionName);

// 5. Run migrations
var migrationManager = new MigrationManager(editor, dataSource);
migrationManager.EnsureDatabaseCreated("YourApp.Common.Entities", true, null);
```

## Service Pattern

```csharp
// 1. Register services
var builder = Host.CreateApplicationBuilder();
BeepDesktopServices.RegisterServices(builder);
var host = builder.Build();
BeepDesktopServices.ConfigureServices(host);

// 2. Access editor
var editor = BeepDesktopServices.AppManager.DMEEditor;

// 3. Start loading
BeepDesktopServices.StartLoading(new[] { "Beep" }, showWaitForm: true);
```

## Key Methods

- `new DMEEditor()` - Create editor instance
- `editor.OpenDataSource(name)` - Open connection (returns ConnectionState)
- `editor.GetDataSource(name)` - Get IDataSource instance
- `new MigrationManager(editor, dataSource)` - Create migration manager
- `BeepDesktopServices.AppManager.DMEEditor` - Access editor in service pattern

## Common Namespaces

```csharp
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Migration;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Container.Services;
using System.Data;
```
