---
name: beepservice
description: Guide for initializing IDMEEditor and creating BeepService patterns for WinForms applications. Use when setting up BeepDM in a WinForms application, initializing IDMEEditor, configuring database connections, or creating service initialization patterns.
---

# BeepService Initialization Guide

Expert guidance for initializing `IDMEEditor` and creating service patterns for BeepDM in WinForms applications.

## Overview

There are two main patterns for initializing BeepDM in WinForms applications:

1. **Simple Pattern**: Direct `DMEEditor` initialization (for standalone apps)
2. **Service Pattern**: Using `BeepDesktopServices` (for full-featured desktop apps with routing, themes, etc.)

## Pattern 1: Simple DMEEditor Initialization

### Basic Setup

```csharp
using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Migration;
using TheTechIdea.Beep.Winform.Controls.ThemeManagement;

namespace YourApp
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.SystemAware);

            try
            {
                // Initialize Beep Data Management Editor
                var editor = new DMEEditor();
                
                // Initialize database connection
                InitializeDatabase(editor);

                // Initialize theme manager (optional)
                InitializeThemes();

                // Show login form or main form
                using (var loginForm = new LoginForm(editor))
                {
                    if (loginForm.ShowDialog() == DialogResult.OK)
                    {
                        Application.Run(new MainForm(editor, loginForm.LoggedInUser));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to start application: {ex.Message}\n\n{ex.StackTrace}",
                    "Startup Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Initialize database connection and ensure schema exists
        /// </summary>
        private static void InitializeDatabase(IDMEEditor editor)
        {
            try
            {
                // Create connection properties for SQLite (default)
                var connectionProps = AppDbContext.CreateSqliteConnectionProps(editor);

                // Add or update connection in ConfigEditor
                var existingConnection = editor.ConfigEditor.DataConnections
                    .FirstOrDefault(c => c.ConnectionName == AppDbContext.DataSourceName);

                if (existingConnection == null)
                {
                    editor.ConfigEditor.DataConnections.Add(connectionProps);
                }
                else
                {
                    // Update existing connection
                    existingConnection.ConnectionString = connectionProps.ConnectionString;
                    existingConnection.DriverName = connectionProps.DriverName;
                    existingConnection.DriverVersion = connectionProps.DriverVersion;
                }

                // Open connection
                var connectionState = editor.OpenDataSource(AppDbContext.DataSourceName);
                if (connectionState != ConnectionState.Open)
                {
                    throw new Exception($"Failed to open connection: {AppDbContext.DataSourceName}. Status: {connectionState}");
                }

                // Get the datasource
                var dataSource = editor.GetDataSource(AppDbContext.DataSourceName);
                if (dataSource == null)
                {
                    throw new Exception($"Failed to get datasource: {AppDbContext.DataSourceName}");
                }

                // Run migrations to ensure schema exists
                var migrationManager = new MigrationManager(editor, dataSource);
                var migrationResult = migrationManager.EnsureDatabaseCreated(
                    namespaceName: "YourApp.Common.Entities",
                    detectRelationships: true,
                    progress: null);
                
                if (migrationResult.Flag == Errors.Failed)
                {
                    throw new Exception($"Migration failed: {migrationResult.Message}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Database initialization failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Initialize Beep theme system
        /// </summary>
        private static void InitializeThemes()
        {
            try
            {
                // Themes are automatically loaded via static constructor
                // You can set a default theme here if needed
                // BeepThemesManager.SetCurrentTheme("Material3Dark");
            }
            catch (Exception ex)
            {
                // Log but don't fail - themes are optional
                System.Diagnostics.Debug.WriteLine($"Theme initialization warning: {ex.Message}");
            }
        }
    }
}
```

### Key Points

- **DMEEditor**: Create instance with `new DMEEditor()`
- **OpenDataSource**: Returns `ConnectionState`, not `IDataSource`
- **GetDataSource**: Use after opening to get the `IDataSource` instance
- **MigrationManager**: Must be instantiated with `editor` and `dataSource`
- **BeepThemesManager**: Static class, no instance needed

## Pattern 2: BeepDesktopServices (Full-Featured)

### Advanced Setup with Routing and Services

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TheTechIdea.Beep.Container.Services;
using TheTechIdea.Beep.Desktop.Common;
using TheTechIdea.Beep.Winform.Controls.ThemeManagement;

namespace YourApp
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Initialize configuration system early
            InitializeConfiguration(args);

            StartApplication();
        }

        private static void StartApplication()
        {
            // Create HostApplicationBuilder
            var builder = Host.CreateApplicationBuilder();

            // Register Beep Services
            BeepDesktopServices.RegisterServices(builder);

            // Build the host
            var host = builder.Build();
           
            // Configure services
            BeepDesktopServices.ConfigureServices(host);

            // Configure controls and menus based on dynamic loaded modules
            BeepDesktopServices.ConfigureControlsandMenus(BeepDesktopServices.AppManager);

            // Configure AppManager
            BeepDesktopServices.AppManager.DialogManager = new BeepDialogManager();
            BeepDesktopServices.AppManager.Title = "Your Application Title";
            BeepDesktopServices.AppManager.Theme = "TerminalTheme";
            BeepDesktopServices.AppManager.Style = FormStyle.Terminal;
            BeepDesktopServices.AppManager.WaitFormType = typeof(BeepWait);
            BeepDesktopServices.AppManager.HomePageName = "MainFrm";

            // Set theme and style before loading fonts
            BeepThemesManager.CurrentStyle = FormStyle.Terminal;
            FontListHelper.EnsureFontsLoaded();

            // Subscribe to events for custom routes and resources
            SubscribeToBeepEvents();

            // Start loading Beep framework
            var result = BeepDesktopServices.StartLoading(
                new string[] { "BeepEnterprize", "TheTechIdea", "Beep" }, 
                showWaitForm: true);

            if (result.Flag == Errors.Ok)
            {
                // Access IDMEEditor via AppManager
                var editor = BeepDesktopServices.AppManager.DMEEditor;
                
                // Initialize database
                InitializeDatabase(editor);

                // Show home page
                BeepDesktopServices.AppManager.ShowHome();
            }
            else
            {
                MessageBox.Show($"Loading failed: {result.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // Cleanup on exit
            BeepDesktopServices.DisposeServices();
            host.Dispose();
            Application.Exit();
        }

        private static void SubscribeToBeepEvents()
        {
            // Register custom routes
            BeepDesktopServices.OnRegisterRoutes += (routingManager) =>
            {
                routingManager.RegisterRouteByName("MainForm", "MainForm");
                routingManager.RegisterRouteByName("LoginForm", "LoginForm");
            };

            // Add custom graphics paths
            BeepDesktopServices.OnLoadGraphics += (graphicsLocations) =>
            {
                graphicsLocations.Add(@".\Resources\Images");
            };

            // Add custom font paths
            BeepDesktopServices.OnLoadFonts += (fontLocations) =>
            {
                fontLocations.Add(@".\Resources\Fonts");
            };
        }
    }
}
```

### Key Points for BeepDesktopServices

- **Host Builder**: Uses Microsoft.Extensions.Hosting
- **Service Registration**: `BeepDesktopServices.RegisterServices(builder)`
- **AppManager**: Central access point for `DMEEditor` and other services
- **Routing**: Built-in routing system for forms/views
- **Themes**: Full theme management system
- **Dynamic Modules**: Support for plugins and extensions

## Common Initialization Steps

### 1. Create Connection Properties

```csharp
// SQLite (default)
var connectionProps = AppDbContext.CreateSqliteConnectionProps(editor);

// SQL Server
var connectionProps = AppDbContext.CreateSqlServerConnectionProps(
    editor, 
    server: "localhost", 
    database: "MyDB", 
    integratedSecurity: true);

// MySQL
var connectionProps = AppDbContext.CreateMySqlConnectionProps(
    editor, 
    server: "localhost", 
    database: "MyDB", 
    userId: "root", 
    password: "password");

// PostgreSQL
var connectionProps = AppDbContext.CreatePostgreSqlConnectionProps(
    editor, 
    server: "localhost", 
    database: "MyDB", 
    userId: "postgres", 
    password: "password");
```

### 2. Add Connection to ConfigEditor

```csharp
// Check if connection exists
var existingConnection = editor.ConfigEditor.DataConnections
    .FirstOrDefault(c => c.ConnectionName == connectionProps.ConnectionName);

if (existingConnection == null)
{
    editor.ConfigEditor.DataConnections.Add(connectionProps);
}
else
{
    // Update existing connection
    existingConnection.ConnectionString = connectionProps.ConnectionString;
    existingConnection.DriverName = connectionProps.DriverName;
    existingConnection.DriverVersion = connectionProps.DriverVersion;
}
```

### 3. Open DataSource

```csharp
// Open connection (returns ConnectionState)
var connectionState = editor.OpenDataSource(connectionProps.ConnectionName);

if (connectionState != ConnectionState.Open)
{
    throw new Exception($"Failed to open connection. Status: {connectionState}");
}

// Get the datasource instance
var dataSource = editor.GetDataSource(connectionProps.ConnectionName);
```

### 4. Run Migrations

```csharp
// Create MigrationManager
var migrationManager = new MigrationManager(editor, dataSource);

// Ensure database schema exists from Entity classes
var migrationResult = migrationManager.EnsureDatabaseCreated(
    namespaceName: "YourApp.Common.Entities",  // Namespace containing Entity classes
    detectRelationships: true,                  // Auto-detect foreign keys
    progress: null);                            // Optional progress callback

if (migrationResult.Flag == Errors.Failed)
{
    throw new Exception($"Migration failed: {migrationResult.Message}");
}
```

## Accessing IDMEEditor in Forms

### Simple Pattern

```csharp
public partial class MainForm : Form
{
    private readonly IDMEEditor _editor;

    public MainForm(IDMEEditor editor)
    {
        InitializeComponent();
        _editor = editor;
    }

    private void LoadData()
    {
        var dataSource = _editor.GetDataSource("MyDataSource");
        // Use datasource...
    }
}
```

### Service Pattern

```csharp
public partial class MainForm : Form
{
    private IDMEEditor Editor => BeepDesktopServices.AppManager.DMEEditor;

    public MainForm()
    {
        InitializeComponent();
    }

    private void LoadData()
    {
        var dataSource = Editor.GetDataSource("MyDataSource");
        // Use datasource...
    }
}
```

## Error Handling

Always wrap initialization in try-catch:

```csharp
try
{
    var editor = new DMEEditor();
    InitializeDatabase(editor);
    // Continue...
}
catch (Exception ex)
{
    MessageBox.Show(
        $"Failed to start application: {ex.Message}\n\n{ex.StackTrace}",
        "Startup Error",
        MessageBoxButtons.OK,
        MessageBoxIcon.Error);
    
    // Log to file or event log
    System.Diagnostics.Debug.WriteLine($"Startup error: {ex}");
}
```

## Best Practices

1. **Initialize Early**: Create `DMEEditor` as early as possible in `Main()`
2. **Check Connection State**: Always verify `ConnectionState.Open` before using datasource
3. **Handle Errors Gracefully**: Don't let initialization failures crash the app
4. **Dispose Properly**: Ensure `DMEEditor` is disposed when app exits (if using `using` pattern)
5. **Use MigrationManager**: Always use migrations to ensure schema consistency
6. **Namespace Discovery**: Use namespace-based entity discovery for migrations
7. **Theme Initialization**: Initialize themes before creating forms

## Common Issues

### Issue: `DMEEditor.Instance` doesn't exist
**Solution**: Use `new DMEEditor()` - it's not a singleton

### Issue: `OpenConnection()` doesn't exist
**Solution**: Use `OpenDataSource()` which returns `ConnectionState`, then `GetDataSource()` to get `IDataSource`

### Issue: `editor.MigrationManager` doesn't exist
**Solution**: Create instance: `new MigrationManager(editor, dataSource)`

### Issue: `BeepThemesManager.Instance` doesn't exist
**Solution**: `BeepThemesManager` is static - use methods directly without instance

### Issue: Connection fails to open
**Solution**: 
- Check connection string is correct
- Verify driver is loaded
- Check connection exists in `ConfigEditor.DataConnections`
- Verify database file exists (for SQLite)

## Related Skills

- `beepdm` - Core BeepDM development patterns
- `connection` - Connection management
- `connectionproperties` - Connection properties configuration
- `unitofwork` - Data access patterns


## Repo Documentation Anchors

- DataManagementEngineStandard/Editor/README.md
- DataManagementEngineStandard/ConfigUtil/README.md
- DataManagementEngineStandard/Docs/registerbeep.html

