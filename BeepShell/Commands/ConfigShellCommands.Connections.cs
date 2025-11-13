using System;
using System.CommandLine;
using System.Linq;
using Spectre.Console;
using TheTechIdea.Beep.ConfigUtil;

namespace BeepShell.Commands
{
    /// <summary>
    /// Connection management commands for ConfigShellCommands
    /// Handles CRUD operations for data connections using DataConnectionManager
    /// </summary>
    public partial class ConfigShellCommands
    {
        private void AddConnectionCommands(Command parent)
        {
            var connCommand = new Command("connection", "Manage data connections");

            // List connections
            var listCommand = new Command("list", "List all data connections");
            listCommand.SetHandler(ListConnections);
            connCommand.AddCommand(listCommand);

            // Add connection
            var addCommand = new Command("add", "Add a new data connection");
            var nameOpt = new Option<string>("--name", "Connection name") { IsRequired = true };
            var driverOpt = new Option<string>("--driver", "Driver name") { IsRequired = true };
            var hostOpt = new Option<string>("--host", "Host address") { IsRequired = true };
            var portOpt = new Option<int?>("--port", "Port number");
            var databaseOpt = new Option<string>("--database", "Database name") { IsRequired = true };
            var userOpt = new Option<string>("--user", "User name") { IsRequired = true };
            var passwordOpt = new Option<string>("--password", "Password") { IsRequired = true };
            
            addCommand.AddOption(nameOpt);
            addCommand.AddOption(driverOpt);
            addCommand.AddOption(hostOpt);
            addCommand.AddOption(portOpt);
            addCommand.AddOption(databaseOpt);
            addCommand.AddOption(userOpt);
            addCommand.AddOption(passwordOpt);
            addCommand.SetHandler(AddConnection, nameOpt, driverOpt, hostOpt, portOpt, databaseOpt, userOpt, passwordOpt);
            connCommand.AddCommand(addCommand);

            // Remove connection
            var removeCommand = new Command("remove", "Remove a data connection");
            var removeArg = new Argument<string>("name", "Connection name to remove");
            removeCommand.AddArgument(removeArg);
            removeCommand.SetHandler(RemoveConnection, removeArg);
            connCommand.AddCommand(removeCommand);

            // Test connection
            var testCommand = new Command("test", "Test a data connection");
            var testArg = new Argument<string>("name", "Connection name to test");
            testCommand.AddArgument(testArg);
            testCommand.SetHandler(TestConnection, testArg);
            connCommand.AddCommand(testCommand);

            parent.AddCommand(connCommand);
        }

        private void ListConnections()
        {
            try
            {
                var connections = _editor.ConfigEditor.DataConnections;
                
                if (connections == null || connections.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No connections configured[/]");
                    return;
                }

                var table = new Table();
                table.Border = TableBorder.Rounded;
                table.AddColumn("[cyan]Name[/]");
                table.AddColumn("[cyan]Driver[/]");
                table.AddColumn("[cyan]Database[/]");
                table.AddColumn("[cyan]Host[/]");
                table.AddColumn("[cyan]Port[/]");
                table.AddColumn("[cyan]Status[/]");

                foreach (var conn in connections)
                {
                    var status = _editor.DataSources.Any(ds => ds.DatasourceName == conn.ConnectionName && 
                                                               ds.ConnectionStatus == System.Data.ConnectionState.Open)
                        ? "[green]Open[/]"
                        : "[dim]Closed[/]";

                    table.AddRow(
                        conn.ConnectionName ?? "-",
                        conn.DriverName ?? "-",
                        conn.Database ?? "-",
                        conn.Host ?? "-",
                        conn.Port > 0 ? conn.Port.ToString() : "-",
                        status
                    );
                }

                AnsiConsole.Write(table);
                AnsiConsole.MarkupLine($"\n[dim]Total connections: {connections.Count}[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error listing connections: {ex.Message}");
            }
        }

        private void AddConnection(string name, string driver, string host, int? port, string database, string user, string password)
        {
            try
            {
                if (_editor.ConfigEditor.DataConnectionExist(name))
                {
                    AnsiConsole.MarkupLine($"[yellow]Connection '{name}' already exists[/]");
                    return;
                }

                var connection = new ConnectionProperties
                {
                    ConnectionName = name,
                    DriverName = driver,
                    Host = host,
                    Port = port ?? 0,
                    Database = database,
                    UserID = user,
                    Password = password,
                    GuidID = Guid.NewGuid().ToString()
                };

                bool added = _editor.ConfigEditor.AddDataConnection(connection);
                
                if (added)
                {
                    _editor.ConfigEditor.SaveDataconnectionsValues();
                    AnsiConsole.MarkupLine($"[green]✓[/] Connection '{name}' added successfully");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to add connection '{name}'[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error adding connection: {ex.Message}");
            }
        }

        private void RemoveConnection(string name)
        {
            try
            {
                if (!_editor.ConfigEditor.DataConnectionExist(name))
                {
                    AnsiConsole.MarkupLine($"[yellow]Connection '{name}' not found[/]");
                    return;
                }

                var confirm = AnsiConsole.Confirm($"Are you sure you want to remove connection '{name}'?");
                
                if (confirm)
                {
                    bool removed = _editor.ConfigEditor.RemoveDataConnection(name);
                    
                    if (removed)
                    {
                        _editor.ConfigEditor.SaveDataconnectionsValues();
                        AnsiConsole.MarkupLine($"[green]✓[/] Connection '{name}' removed successfully[/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[red]✗[/] Failed to remove connection '{name}'[/]");
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine("[dim]Remove cancelled[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error removing connection: {ex.Message}");
            }
        }

        private void TestConnection(string name)
        {
            try
            {
                var connection = _editor.ConfigEditor.DataConnections
                    .FirstOrDefault(c => c.ConnectionName.Equals(name, StringComparison.OrdinalIgnoreCase));

                if (connection == null)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Connection '{name}' not found[/]");
                    return;
                }

                AnsiConsole.Status()
                    .Start($"Testing connection to '{name}'...", ctx =>
                    {
                        try
                        {
                            // Try to open the connection
                            var ds = _editor.GetDataSource(name);
                            if (ds == null)
                            {
                                AnsiConsole.MarkupLine($"[yellow]Connection not loaded, attempting to open...[/]");
                                _editor.OpenDataSource(name);
                                ds = _editor.GetDataSource(name);
                            }

                            if (ds != null && ds.ConnectionStatus == System.Data.ConnectionState.Open)
                            {
                                AnsiConsole.MarkupLine($"[green]✓[/] Connection '{name}' is working");
                                AnsiConsole.MarkupLine($"[dim]Driver: {connection.DriverName}[/]");
                                AnsiConsole.MarkupLine($"[dim]Database: {connection.Database}[/]");
                            }
                            else
                            {
                                AnsiConsole.MarkupLine($"[red]✗[/] Failed to connect to '{name}'[/]");
                            }
                        }
                        catch (Exception testEx)
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] Connection test failed: {testEx.Message}");
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error testing connection: {ex.Message}");
            }
        }
    }
}
