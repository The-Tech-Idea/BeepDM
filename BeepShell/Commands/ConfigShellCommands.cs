using System;
using System.CommandLine;
using System.Linq;
using BeepShell.Infrastructure;
using Spectre.Console;
using TheTechIdea.Beep.Editor;

namespace BeepShell.Commands
{
    /// <summary>
    /// Configuration management commands for BeepShell
    /// Uses persistent DMEEditor instead of creating new instances
    /// </summary>
    public class ConfigShellCommands : IShellCommand
    {
        private IDMEEditor _editor;

        public string CommandName => "config";
        public string Description => "Manage BeepDM configuration";
        public string Category => "Configuration";
        public string Version => "1.0.0";
        public string Author => "BeepDM Team";
        public string[] Aliases => new[] { "cfg", "configuration" };

        public void Initialize(IDMEEditor editor)
        {
            _editor = editor;
        }

        public Command BuildCommand()
        {
            var configCommand = new Command("config", Description);

            // config show
            var showCommand = new Command("show", "Show current configuration");
            showCommand.SetHandler(() => ShowConfig());
            configCommand.AddCommand(showCommand);

            // config connection list
            var connectionListCommand = new Command("list", "List all connections");
            connectionListCommand.SetHandler(() => ListConnections());
            
            var connectionCommand = new Command("connection", "Manage connections");
            connectionCommand.AddCommand(connectionListCommand);
            configCommand.AddCommand(connectionCommand);

            // config connection add
            var addCommand = new Command("add", "Add a new connection");
            var nameOption = new Option<string>("--name", "Connection name") { IsRequired = true };
            var driverOption = new Option<string>("--driver", "Driver type") { IsRequired = true };
            var hostOption = new Option<string>("--host", "Host/Server");
            var portOption = new Option<int?>("--port", "Port number");
            var databaseOption = new Option<string>("--database", "Database name");
            var userOption = new Option<string>("--user", "Username");
            var passwordOption = new Option<string>("--password", "Password");

            addCommand.AddOption(nameOption);
            addCommand.AddOption(driverOption);
            addCommand.AddOption(hostOption);
            addCommand.AddOption(portOption);
            addCommand.AddOption(databaseOption);
            addCommand.AddOption(userOption);
            addCommand.AddOption(passwordOption);

            addCommand.SetHandler((name, driver, host, port, database, user, password) =>
            {
                AddConnection(name, driver, host, port, database, user, password);
            }, nameOption, driverOption, hostOption, portOption, databaseOption, userOption, passwordOption);

            connectionCommand.AddCommand(addCommand);

            // config save
            var saveCommand = new Command("save", "Save configuration to disk");
            saveCommand.SetHandler(() => SaveConfig());
            configCommand.AddCommand(saveCommand);

            // config reload
            var reloadCommand = new Command("reload", "Reload configuration from disk");
            reloadCommand.SetHandler(() => ReloadConfig());
            configCommand.AddCommand(reloadCommand);

            return configCommand;
        }

        private void ShowConfig()
        {
            try
            {
                var config = _editor.ConfigEditor.Config;
                
                var panel = new Panel(new Markup(
                    $"[cyan]Profile:[/] {_editor.ConfigEditor.Config.ConfigPath}\n" +
                    $"[cyan]Connections:[/] {_editor.ConfigEditor.DataConnections?.Count ?? 0}\n" +
                    $"[cyan]Data Sources:[/] {_editor.DataSources.Count}\n" +
                    $"[cyan]Loaded Assemblies:[/] {_editor.ConfigEditor.LoadedAssemblies?.Count ?? 0}"
                ));
                panel.Header = new PanelHeader("[green]BeepDM Configuration[/]");
                panel.Border = BoxBorder.Rounded;

                AnsiConsole.Write(panel);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error showing config: {ex.Message}");
            }
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
                table.AddColumn("[cyan]Status[/]");

                foreach (var conn in connections)
                {
                    var status = _editor.DataSources.Any(ds => ds.DatasourceName == conn.ConnectionName && 
                                                               ds.ConnectionStatus == System.Data.ConnectionState.Open)
                        ? "[green]Open[/]"
                        : "[dim]Closed[/]";

                    table.AddRow(
                        conn.ConnectionName,
                        conn.DriverName ?? "-",
                        conn.Database ?? "-",
                        conn.Host ?? "-",
                        status
                    );
                }

                AnsiConsole.Write(table);
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
                var connection = new TheTechIdea.Beep.ConfigUtil.ConnectionProperties
                {
                    ConnectionName = name,
                    DriverName = driver,
                    Host = host,
                    Port = port ?? 0,
                    Database = database,
                    UserID = user,
                    Password = password
                };

                _editor.ConfigEditor.DataConnections.Add(connection);
                _editor.ConfigEditor.SaveDataconnectionsValues();

                AnsiConsole.MarkupLine($"[green]✓[/] Connection '{name}' added successfully");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error adding connection: {ex.Message}");
            }
        }

        private void SaveConfig()
        {
            try
            {
                _editor.ConfigEditor.SaveConfigValues();
                _editor.ConfigEditor.SaveDataconnectionsValues();
                AnsiConsole.MarkupLine("[green]✓[/] Configuration saved");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error saving config: {ex.Message}");
            }
        }

        private void ReloadConfig()
        {
            try
            {
                _editor.ConfigEditor.LoadConfigValues();
                AnsiConsole.MarkupLine("[green]✓[/] Configuration reloaded");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error reloading config: {ex.Message}");
            }
        }

        public bool CanExecute() => _editor != null;

        public string[] GetExamples()
        {
            return new[]
            {
                "config show",
                "config connection list",
                "config connection add --name mydb --driver SqlServer --host localhost --database mydb",
                "config save",
                "config reload"
            };
        }
    }
}
