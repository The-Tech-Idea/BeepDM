using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using BeepShell.Infrastructure;
using Spectre.Console;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Utilities;

namespace BeepShell.Commands
{
    /// <summary>
    /// Comprehensive data connection management shell commands
    /// Combines interactive selection from DriverShell and NuGetShell patterns
    /// </summary>
    public class ConnectionShellCommands : IShellCommand
    {
        private IDMEEditor _editor = null!;

        public string CommandName => "connection";
        public string Description => "Manage data connections with interactive selection";
        public string Category => "Connection Management";
        public string Version => "1.0.0";
        public string Author => "BeepDM Team";
        public string[] Aliases => new[] { "conn", "connections" };

        public void Initialize(IDMEEditor editor)
        {
            _editor = editor;
        }

        public Command BuildCommand()
        {
            var cmd = new Command("connection", Description);

            // connection list
            var listCmd = new Command("list", "List all configured connections");
            var showDetailsOpt = new Option<bool>(new[] { "--details", "-d" }, "Show detailed connection information");
            listCmd.AddOption(showDetailsOpt);
            listCmd.SetHandler((showDetails) => ListConnections(showDetails), showDetailsOpt);
            cmd.AddCommand(listCmd);

            // connection create (interactive)
            var createCmd = new Command("create", "Create a new connection interactively");
            createCmd.SetHandler(() => CreateConnectionInteractive());
            cmd.AddCommand(createCmd);

            // connection create-manual
            var createManualCmd = new Command("create-manual", "Create a new connection with command-line parameters");
            var nameOpt = new Option<string>(new[] { "--name", "-n" }, "Connection name") { IsRequired = true };
            var dsTypeOpt = new Option<string>(new[] { "--type", "-t" }, "DataSourceType (e.g., SqlServer, PostgreSQL, MongoDB)") { IsRequired = true };
            var hostOpt = new Option<string>(new[] { "--host", "-h" }, "Host/Server address");
            var portOpt = new Option<int?>(new[] { "--port", "-p" }, "Port number");
            var databaseOpt = new Option<string>(new[] { "--database", "-db" }, "Database name");
            var userOpt = new Option<string>(new[] { "--user", "-u" }, "Username");
            var passwordOpt = new Option<string>(new[] { "--password", "-pw" }, "Password");
            var connStringOpt = new Option<string>(new[] { "--connection-string", "-cs" }, "Full connection string");

            createManualCmd.AddOption(nameOpt);
            createManualCmd.AddOption(dsTypeOpt);
            createManualCmd.AddOption(hostOpt);
            createManualCmd.AddOption(portOpt);
            createManualCmd.AddOption(databaseOpt);
            createManualCmd.AddOption(userOpt);
            createManualCmd.AddOption(passwordOpt);
            createManualCmd.AddOption(connStringOpt);

            createManualCmd.SetHandler((name, dsType, host, port, database, user, password, connString) =>
            {
                CreateConnectionManual(name, dsType, host, port, database, user, password, connString);
            }, nameOpt, dsTypeOpt, hostOpt, portOpt, databaseOpt, userOpt, passwordOpt, connStringOpt);
            cmd.AddCommand(createManualCmd);

            // connection update
            var updateCmd = new Command("update", "Update an existing connection interactively");
            var updateNameOpt = new Option<string>(new[] { "--name", "-n" }, "Connection name (if not provided, will show selection list)");
            updateCmd.AddOption(updateNameOpt);
            updateCmd.SetHandler((name) => UpdateConnectionInteractive(name), updateNameOpt);
            cmd.AddCommand(updateCmd);

            // connection delete
            var deleteCmd = new Command("delete", "Delete a connection");
            var deleteNameOpt = new Option<string>(new[] { "--name", "-n" }, "Connection name (if not provided, will show selection list)");
            deleteCmd.AddOption(deleteNameOpt);
            deleteCmd.SetHandler((name) => DeleteConnectionInteractive(name), deleteNameOpt);
            cmd.AddCommand(deleteCmd);

            // connection test
            var testCmd = new Command("test", "Test a connection");
            var testNameOpt = new Option<string>(new[] { "--name", "-n" }, "Connection name (if not provided, will show selection list)");
            testCmd.AddOption(testNameOpt);
            testCmd.SetHandler((name) => TestConnectionInteractive(name), testNameOpt);
            cmd.AddCommand(testCmd);

            // connection info
            var infoCmd = new Command("info", "Show detailed information about a connection");
            var infoNameOpt = new Option<string>(new[] { "--name", "-n" }, "Connection name (if not provided, will show selection list)") { IsRequired = true };
            infoCmd.AddOption(infoNameOpt);
            infoCmd.SetHandler((name) => ShowConnectionInfo(name), infoNameOpt);
            cmd.AddCommand(infoCmd);

            // connection check-drivers
            var checkDriversCmd = new Command("check-drivers", "Check if required drivers are available");
            checkDriversCmd.SetHandler(() => CheckAvailableDrivers());
            cmd.AddCommand(checkDriversCmd);

            return cmd;
        }

        private void ListConnections(bool showDetails)
        {
            try
            {
                var connections = _editor.ConfigEditor.DataConnections;

                if (connections == null || connections.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]⚠[/] No connections configured");
                    AnsiConsole.MarkupLine("[dim]Use 'connection create' to add a new connection[/]");
                    return;
                }

                var table = new Table();
                table.Border = TableBorder.Rounded;
                table.Title = new TableTitle($"[bold cyan]Data Connections ({connections.Count})[/]");
                
                table.AddColumn("[cyan]#[/]");
                table.AddColumn("[cyan]Name[/]");
                table.AddColumn("[cyan]Type[/]");
                table.AddColumn("[cyan]Database[/]");
                table.AddColumn("[cyan]Host[/]");
                
                if (showDetails)
                {
                    table.AddColumn("[cyan]Port[/]");
                    table.AddColumn("[cyan]User[/]");
                    table.AddColumn("[cyan]Driver Available[/]");
                }
                
                table.AddColumn("[cyan]Status[/]");

                var index = 1;
                foreach (var conn in connections)
                {
                    var status = _editor.DataSources.Any(ds => 
                        ds.DatasourceName == conn.ConnectionName && 
                        ds.ConnectionStatus == System.Data.ConnectionState.Open)
                        ? "[green]●[/]"
                        : "[dim]○[/]";

                    var driverAvailable = string.Empty;
                    if (showDetails)
                    {
                        var driver = _editor.ConfigEditor.DataDriversClasses
                            .FirstOrDefault(d => d.DatasourceType == conn.DatabaseType);
                        driverAvailable = driver != null && !driver.NuggetMissing ? "[green]Yes[/]" : "[red]No[/]";
                    }

                    if (showDetails)
                    {
                        table.AddRow(
                            index.ToString(),
                            $"[cyan]{conn.ConnectionName}[/]",
                            conn.DatabaseType.ToString(),
                            conn.Database ?? "-",
                            conn.Host ?? "-",
                            conn.Port > 0 ? conn.Port.ToString() : "-",
                            conn.UserID ?? "-",
                            driverAvailable,
                            status
                        );
                    }
                    else
                    {
                        table.AddRow(
                            index.ToString(),
                            $"[cyan]{conn.ConnectionName}[/]",
                            conn.DatabaseType.ToString(),
                            conn.Database ?? "-",
                            conn.Host ?? "-",
                            status
                        );
                    }
                    index++;
                }

                AnsiConsole.Write(table);
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[dim]Use 'connection info --name <name>' for detailed information[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error listing connections: {ex.Message}");
            }
        }

        private void CreateConnectionInteractive()
        {
            try
            {
                AnsiConsole.MarkupLine("[bold cyan]Create New Connection[/]");
                AnsiConsole.WriteLine();

                // Step 1: Select DataSourceType
                var dataSourceTypes = Enum.GetValues(typeof(DataSourceType))
                    .Cast<DataSourceType>()
                    .OrderBy(t => t.ToString())
                    .ToList();

                var selectedType = AnsiConsole.Prompt(
                    new SelectionPrompt<DataSourceType>()
                        .Title("[cyan]Select [green]DataSource Type[/]:[/]")
                        .PageSize(15)
                        .MoreChoicesText("[grey](Move up and down to see more types)[/]")
                        .AddChoices(dataSourceTypes)
                        .UseConverter(t => t.ToString()));

                // Check if driver is available
                var driver = _editor.ConfigEditor.DataDriversClasses
                    .FirstOrDefault(d => d.DatasourceType == selectedType);

                if (driver == null || driver.NuggetMissing)
                {
                    AnsiConsole.MarkupLine($"[yellow]⚠[/] Driver for {selectedType} is not available or missing");
                    
                    if (AnsiConsole.Confirm("Would you like to check available drivers and install if needed?"))
                    {
                        CheckAndInstallDriver(selectedType);
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[yellow]Connection creation cancelled[/]");
                        return;
                    }
                }

                // Step 2: Gather connection details
                var name = AnsiConsole.Ask<string>("[cyan]Connection [green]Name[/]:[/]");
                
                // Check if name already exists
                if (_editor.ConfigEditor.DataConnections.Any(c => c.ConnectionName.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Connection '{name}' already exists");
                    return;
                }

                var host = AnsiConsole.Ask<string>("[cyan]Host/Server:[/]", "localhost");
                var port = AnsiConsole.Ask<int>("[cyan]Port:[/]", GetDefaultPort(selectedType));
                var database = AnsiConsole.Ask<string>("[cyan]Database:[/]");
                var userId = AnsiConsole.Ask<string>("[cyan]User ID:[/]", string.Empty);
                
                string password = string.Empty;
                if (!string.IsNullOrEmpty(userId))
                {
                    password = AnsiConsole.Prompt(
                        new TextPrompt<string>("[cyan]Password:[/]")
                            .Secret()
                            .AllowEmpty());
                }

                // Create connection
                var connection = new ConnectionProperties
                {
                    ConnectionName = name,
                    DatabaseType = selectedType,
                    DriverName = driver?.DriverClass ?? selectedType.ToString(),
                    Host = host,
                    Port = port,
                    Database = database,
                    UserID = userId,
                    Password = password
                };

                _editor.ConfigEditor.DataConnections.Add(connection);
                _editor.ConfigEditor.SaveDataconnectionsValues();

                AnsiConsole.MarkupLine($"[green]✓[/] Connection '{name}' created successfully");
                
                if (AnsiConsole.Confirm("Would you like to test the connection?"))
                {
                    TestConnection(connection);
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error creating connection: {ex.Message}");
            }
        }

        private void CreateConnectionManual(string name, string dsType, string host, int? port, string database, string user, string password, string connectionString)
        {
            try
            {
                // Parse DataSourceType
                if (!Enum.TryParse<DataSourceType>(dsType, true, out var dataSourceType))
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Invalid DataSourceType: {dsType}");
                    AnsiConsole.MarkupLine("[dim]Use 'connection check-drivers' to see available types[/]");
                    return;
                }

                // Check if name already exists
                if (_editor.ConfigEditor.DataConnections.Any(c => c.ConnectionName.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Connection '{name}' already exists");
                    return;
                }

                var driver = _editor.ConfigEditor.DataDriversClasses
                    .FirstOrDefault(d => d.DatasourceType == dataSourceType);

                var connection = new ConnectionProperties
                {
                    ConnectionName = name,
                    DatabaseType = dataSourceType,
                    DriverName = driver?.DriverClass ?? dataSourceType.ToString(),
                    Host = host ?? "localhost",
                    Port = port ?? GetDefaultPort(dataSourceType),
                    Database = database ?? string.Empty,
                    UserID = user ?? string.Empty,
                    Password = password ?? string.Empty,
                    ConnectionString = connectionString ?? string.Empty
                };

                _editor.ConfigEditor.DataConnections.Add(connection);
                _editor.ConfigEditor.SaveDataconnectionsValues();

                AnsiConsole.MarkupLine($"[green]✓[/] Connection '{name}' created successfully");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error creating connection: {ex.Message}");
            }
        }

        private void UpdateConnectionInteractive(string connectionName)
        {
            try
            {
                ConnectionProperties connection;

                if (string.IsNullOrWhiteSpace(connectionName))
                {
                    // Show selection list
                    connection = SelectConnection("Select connection to update");
                    if (connection == null) return;
                }
                else
                {
                    connection = _editor.ConfigEditor.DataConnections
                        .FirstOrDefault(c => c.ConnectionName.Equals(connectionName, StringComparison.OrdinalIgnoreCase));
                    
                    if (connection == null)
                    {
                        AnsiConsole.MarkupLine($"[red]✗[/] Connection '{connectionName}' not found");
                        return;
                    }
                }

                AnsiConsole.MarkupLine($"[bold cyan]Update Connection: {connection.ConnectionName}[/]");
                AnsiConsole.WriteLine();

                // Select what to update
                var updateOptions = new[] 
                { 
                    "Host", 
                    "Port", 
                    "Database", 
                    "User ID", 
                    "Password", 
                    "Connection String",
                    "All Properties" 
                };

                var selectedOption = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[cyan]What would you like to update?[/]")
                        .AddChoices(updateOptions));

                switch (selectedOption)
                {
                    case "Host":
                        connection.Host = AnsiConsole.Ask("[cyan]New Host:[/]", connection.Host ?? "localhost");
                        break;
                    case "Port":
                        connection.Port = AnsiConsole.Ask("[cyan]New Port:[/]", connection.Port);
                        break;
                    case "Database":
                        connection.Database = AnsiConsole.Ask("[cyan]New Database:[/]", connection.Database ?? string.Empty);
                        break;
                    case "User ID":
                        connection.UserID = AnsiConsole.Ask("[cyan]New User ID:[/]", connection.UserID ?? string.Empty);
                        break;
                    case "Password":
                        connection.Password = AnsiConsole.Prompt(
                            new TextPrompt<string>("[cyan]New Password:[/]")
                                .Secret()
                                .AllowEmpty());
                        break;
                    case "Connection String":
                        connection.ConnectionString = AnsiConsole.Ask("[cyan]New Connection String:[/]", connection.ConnectionString ?? string.Empty);
                        break;
                    case "All Properties":
                        connection.Host = AnsiConsole.Ask("[cyan]Host:[/]", connection.Host ?? "localhost");
                        connection.Port = AnsiConsole.Ask("[cyan]Port:[/]", connection.Port);
                        connection.Database = AnsiConsole.Ask("[cyan]Database:[/]", connection.Database ?? string.Empty);
                        connection.UserID = AnsiConsole.Ask("[cyan]User ID:[/]", connection.UserID ?? string.Empty);
                        connection.Password = AnsiConsole.Prompt(
                            new TextPrompt<string>("[cyan]Password:[/]")
                                .Secret()
                                .AllowEmpty());
                        break;
                }

                _editor.ConfigEditor.SaveDataconnectionsValues();
                AnsiConsole.MarkupLine($"[green]✓[/] Connection '{connection.ConnectionName}' updated successfully");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error updating connection: {ex.Message}");
            }
        }

        private void DeleteConnectionInteractive(string connectionName)
        {
            try
            {
                ConnectionProperties connection;

                if (string.IsNullOrWhiteSpace(connectionName))
                {
                    // Show selection list
                    connection = SelectConnection("Select connection to delete");
                    if (connection == null) return;
                }
                else
                {
                    connection = _editor.ConfigEditor.DataConnections
                        .FirstOrDefault(c => c.ConnectionName.Equals(connectionName, StringComparison.OrdinalIgnoreCase));
                    
                    if (connection == null)
                    {
                        AnsiConsole.MarkupLine($"[red]✗[/] Connection '{connectionName}' not found");
                        return;
                    }
                }

                if (!AnsiConsole.Confirm($"[yellow]Are you sure you want to delete connection '{connection.ConnectionName}'?[/]"))
                {
                    AnsiConsole.MarkupLine("[yellow]Deletion cancelled[/]");
                    return;
                }

                _editor.ConfigEditor.DataConnections.Remove(connection);
                _editor.ConfigEditor.SaveDataconnectionsValues();

                AnsiConsole.MarkupLine($"[green]✓[/] Connection '{connection.ConnectionName}' deleted successfully");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error deleting connection: {ex.Message}");
            }
        }

        private void TestConnectionInteractive(string connectionName)
        {
            try
            {
                ConnectionProperties connection;

                if (string.IsNullOrWhiteSpace(connectionName))
                {
                    // Show selection list
                    connection = SelectConnection("Select connection to test");
                    if (connection == null) return;
                }
                else
                {
                    connection = _editor.ConfigEditor.DataConnections
                        .FirstOrDefault(c => c.ConnectionName.Equals(connectionName, StringComparison.OrdinalIgnoreCase));
                    
                    if (connection == null)
                    {
                        AnsiConsole.MarkupLine($"[red]✗[/] Connection '{connectionName}' not found");
                        return;
                    }
                }

                TestConnection(connection);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error testing connection: {ex.Message}");
            }
        }

        private void TestConnection(ConnectionProperties connection)
        {
            AnsiConsole.Status()
                .Start($"Testing connection to [cyan]{connection.ConnectionName}[/]...", ctx =>
                {
                    try
                    {
                        var dataSource = _editor.GetDataSource(connection.ConnectionName);
                        
                        if (dataSource == null)
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] Failed to create data source");
                            return;
                        }

                        var result = dataSource.Openconnection();
                        
                        if (result == System.Data.ConnectionState.Open)
                        {
                            AnsiConsole.MarkupLine($"[green]✓[/] Connection successful!");
                            dataSource.Closeconnection();
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] Connection failed");
                        }
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"[red]✗[/] Connection error: {ex.Message}");
                    }
                });
        }

        private void ShowConnectionInfo(string connectionName)
        {
            try
            {
                var connection = _editor.ConfigEditor.DataConnections
                    .FirstOrDefault(c => c.ConnectionName.Equals(connectionName, StringComparison.OrdinalIgnoreCase));

                if (connection == null)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Connection '{connectionName}' not found");
                    return;
                }

                var table = new Table();
                table.Border = TableBorder.Rounded;
                table.Title = new TableTitle($"[bold cyan]Connection: {connection.ConnectionName}[/]");
                table.HideHeaders();
                table.AddColumn(new TableColumn("").Width(20));
                table.AddColumn(new TableColumn(""));

                table.AddRow("[bold]Name:[/]", connection.ConnectionName);
                table.AddRow("[bold]Type:[/]", connection.DatabaseType.ToString());
                table.AddRow("[bold]Driver:[/]", connection.DriverName ?? "-");
                table.AddRow("[bold]Host:[/]", connection.Host ?? "-");
                table.AddRow("[bold]Port:[/]", connection.Port > 0 ? connection.Port.ToString() : "-");
                table.AddRow("[bold]Database:[/]", connection.Database ?? "-");
                table.AddRow("[bold]User ID:[/]", connection.UserID ?? "-");
                table.AddRow("[bold]Password:[/]", !string.IsNullOrEmpty(connection.Password) ? "[dim]********[/]" : "[dim]Not set[/]");
                
                if (!string.IsNullOrEmpty(connection.ConnectionString))
                {
                    table.AddRow("[bold]Conn String:[/]", $"[dim]{connection.ConnectionString.Substring(0, Math.Min(50, connection.ConnectionString.Length))}...[/]");
                }

                // Check driver availability
                var driver = _editor.ConfigEditor.DataDriversClasses
                    .FirstOrDefault(d => d.DatasourceType == connection.DatabaseType);
                
                var driverStatus = driver != null && !driver.NuggetMissing 
                    ? "[green]Available[/]" 
                    : "[red]Missing[/]";
                
                table.AddRow("[bold]Driver Status:[/]", driverStatus);

                AnsiConsole.Write(table);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error showing connection info: {ex.Message}");
            }
        }

        private void CheckAvailableDrivers()
        {
            try
            {
                var drivers = _editor.ConfigEditor.DataDriversClasses
                    .OrderBy(d => d.DatasourceType.ToString())
                    .ToList();

                var table = new Table();
                table.Border = TableBorder.Rounded;
                table.Title = new TableTitle($"[bold cyan]Available Drivers ({drivers.Count})[/]");
                table.AddColumn("[cyan]DataSource Type[/]");
                table.AddColumn("[cyan]Driver Class[/]");
                table.AddColumn("[cyan]Status[/]");
                table.AddColumn("[cyan]NuGet Package[/]");

                foreach (var driver in drivers)
                {
                    var status = !driver.NuggetMissing ? "[green]Installed[/]" : "[red]Missing[/]";
                    var packageName = driver.PackageName ?? "[dim]Unknown[/]";

                    table.AddRow(
                        driver.DatasourceType.ToString(),
                        driver.DriverClass ?? "-",
                        status,
                        packageName
                    );
                }

                AnsiConsole.Write(table);
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[dim]Use 'driver-nuget install-missing' to install missing drivers[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error checking drivers: {ex.Message}");
            }
        }

        private void CheckAndInstallDriver(DataSourceType dataSourceType)
        {
            try
            {
                var driver = _editor.ConfigEditor.DataDriversClasses
                    .FirstOrDefault(d => d.DatasourceType == dataSourceType);

                if (driver == null)
                {
                    AnsiConsole.MarkupLine($"[yellow]⚠[/] No driver configuration found for {dataSourceType}");
                    return;
                }

                if (!driver.NuggetMissing)
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] Driver for {dataSourceType} is already installed");
                    return;
                }

                AnsiConsole.MarkupLine($"[yellow]Driver package needed: {driver.PackageName ?? "Unknown"}[/]");
                
                if (!string.IsNullOrWhiteSpace(driver.NuggetSource))
                {
                    AnsiConsole.MarkupLine($"[dim]Source: {driver.NuggetSource}[/]");
                    AnsiConsole.MarkupLine("[dim]Use 'driver-nuget install' command to install this driver[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine("[yellow]⚠[/] No NuGet source configured for this driver");
                    AnsiConsole.MarkupLine("[dim]Use 'driver-nuget set-source' to configure the driver source[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error checking driver: {ex.Message}");
            }
        }

        private ConnectionProperties? SelectConnection(string title)
        {
            var connections = _editor.ConfigEditor.DataConnections;
            
            if (connections == null || connections.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]⚠[/] No connections available");
                return null;
            }

            var selectedConnection = AnsiConsole.Prompt(
                new SelectionPrompt<ConnectionProperties>()
                    .Title($"[cyan]{title}:[/]")
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to see more connections)[/]")
                    .AddChoices(connections)
                    .UseConverter(c => $"{c.ConnectionName} ({c.DatabaseType})"));

            return selectedConnection;
        }

        private int GetDefaultPort(DataSourceType dataSourceType)
        {
            return dataSourceType switch
            {
                DataSourceType.SqlServer => 1433,
                DataSourceType.Postgre => 5432,
                DataSourceType.Mysql => 3306,
                DataSourceType.MongoDB => 27017,
                DataSourceType.Redis => 6379,
                DataSourceType.Oracle => 1521,
                _ => 0
            };
        }

        public bool CanExecute() => _editor != null;

        public string[] GetExamples()
        {
            return new[]
            {
                "connection list",
                "connection list --details",
                "connection create",
                "connection create-manual --name mydb --type SqlServer --host localhost --database mydb",
                "connection update --name mydb",
                "connection delete",
                "connection test --name mydb",
                "connection info --name mydb",
                "connection check-drivers"
            };
        }
    }
}
