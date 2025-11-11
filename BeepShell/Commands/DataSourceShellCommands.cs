using System;
using System.CommandLine;
using System.Linq;
using BeepShell.Infrastructure;
using Spectre.Console;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.DataBase;

namespace BeepShell.Commands
{
    /// <summary>
    /// Data source management commands for BeepShell
    /// Uses persistent DMEEditor with open connections
    /// </summary>
    public class DataSourceShellCommands : IShellCommand
    {
        private IDMEEditor _editor;

        public string CommandName => "datasource";
        public string Description => "Manage data sources and connections";
        public string Category => "Data";
        public string Version => "1.0.0";
        public string Author => "BeepDM Team";
        public string[] Aliases => new[] { "ds", "source" };

        public void Initialize(IDMEEditor editor)
        {
            _editor = editor;
        }

        public Command BuildCommand()
        {
            var dsCommand = new Command("datasource", Description);
            dsCommand.AddAlias("ds");

            // datasource list
            var listCommand = new Command("list", "List all data sources");
            listCommand.SetHandler(() => ListDataSources());
            dsCommand.AddCommand(listCommand);

            // datasource open
            var openCommand = new Command("open", "Open a data source connection");
            var nameArg = new Argument<string>("name", "Data source name");
            openCommand.AddArgument(nameArg);
            openCommand.SetHandler((name) => OpenDataSource(name), nameArg);
            dsCommand.AddCommand(openCommand);

            // datasource close
            var closeCommand = new Command("close", "Close a data source connection");
            var closeNameArg = new Argument<string>("name", "Data source name");
            closeCommand.AddArgument(closeNameArg);
            closeCommand.SetHandler((name) => CloseDataSource(name), closeNameArg);
            dsCommand.AddCommand(closeCommand);

            // datasource info
            var infoCommand = new Command("info", "Show data source information");
            var infoNameArg = new Argument<string>("name", "Data source name");
            infoCommand.AddArgument(infoNameArg);
            infoCommand.SetHandler((name) => ShowDataSourceInfo(name), infoNameArg);
            dsCommand.AddCommand(infoCommand);

            // datasource entities
            var entitiesCommand = new Command("entities", "List entities in a data source");
            var entitiesNameArg = new Argument<string>("name", "Data source name");
            entitiesCommand.AddArgument(entitiesNameArg);
            entitiesCommand.SetHandler((name) => ListEntities(name), entitiesNameArg);
            dsCommand.AddCommand(entitiesCommand);

            // datasource test
            var testCommand = new Command("test", "Test connection to a data source");
            var testNameArg = new Argument<string>("name", "Data source name");
            testCommand.AddArgument(testNameArg);
            testCommand.SetHandler((name) => TestConnection(name), testNameArg);
            dsCommand.AddCommand(testCommand);

            return dsCommand;
        }

        private void ListDataSources()
        {
            try
            {
                if (_editor.DataSources == null || _editor.DataSources.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No data sources available[/]");
                    return;
                }

                var table = new Table();
                table.Border = TableBorder.Rounded;
                table.AddColumn("[cyan]Name[/]");
                table.AddColumn("[cyan]Type[/]");
                table.AddColumn("[cyan]Category[/]");
                table.AddColumn("[cyan]Status[/]");
                table.AddColumn("[cyan]Entities[/]");

                foreach (var ds in _editor.DataSources)
                {
                    var statusColor = ds.ConnectionStatus == System.Data.ConnectionState.Open ? "green" : "dim";
                    var status = ds.ConnectionStatus == System.Data.ConnectionState.Open ? "Open" : "Closed";
                    var entityCount = ds.Entities?.Count ?? 0;

                    table.AddRow(
                        ds.DatasourceName,
                        ds.DatasourceType.ToString(),
                        ds.Category.ToString(),
                        $"[{statusColor}]{status}[/]",
                        entityCount.ToString()
                    );
                }

                AnsiConsole.Write(table);
                AnsiConsole.MarkupLine($"\n[dim]Total: {_editor.DataSources.Count} data sources[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error listing data sources: {ex.Message}");
            }
        }

        private void OpenDataSource(string name)
        {
            try
            {
                var ds = _editor.GetDataSource(name);
                if (ds == null)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Data source '{name}' not found");
                    return;
                }

                if (ds.ConnectionStatus == System.Data.ConnectionState.Open)
                {
                    AnsiConsole.MarkupLine($"[yellow]![/] Data source '{name}' is already open");
                    return;
                }

                AnsiConsole.Status()
                    .Start($"Opening connection to '{name}'...", ctx =>
                    {
                        _editor.OpenDataSource(name);
                    });

                AnsiConsole.MarkupLine($"[green]✓[/] Data source '{name}' opened successfully");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error opening data source: {ex.Message}");
            }
        }

        private void CloseDataSource(string name)
        {
            try
            {
                var ds = _editor.GetDataSource(name);
                if (ds == null)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Data source '{name}' not found");
                    return;
                }

                _editor.CloseDataSource(name);
                AnsiConsole.MarkupLine($"[green]✓[/] Data source '{name}' closed");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error closing data source: {ex.Message}");
            }
        }

        private void ShowDataSourceInfo(string name)
        {
            try
            {
                var ds = _editor.GetDataSource(name);
                if (ds == null)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Data source '{name}' not found");
                    return;
                }

                var status = ds.ConnectionStatus == System.Data.ConnectionState.Open ? "[green]Open[/]" : "[dim]Closed[/]";
                var info = new Panel(new Markup(
                    $"[cyan]Name:[/] {ds.DatasourceName}\n" +
                    $"[cyan]Type:[/] {ds.DatasourceType}\n" +
                    $"[cyan]Category:[/] {ds.Category}\n" +
                    $"[cyan]Status:[/] {status}\n" +
                    $"[cyan]Entities:[/] {ds.Entities?.Count ?? 0}\n" +
                    $"[cyan]ID:[/] {ds.Id}"
                ));
                info.Header = new PanelHeader($"[green]Data Source: {name}[/]");
                info.Border = BoxBorder.Rounded;

                AnsiConsole.Write(info);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error showing data source info: {ex.Message}");
            }
        }

        private void ListEntities(string name)
        {
            try
            {
                var ds = _editor.GetDataSource(name);
                if (ds == null)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Data source '{name}' not found");
                    return;
                }

                if (ds.ConnectionStatus != System.Data.ConnectionState.Open)
                {
                    AnsiConsole.MarkupLine($"[yellow]![/] Opening connection to '{name}'...");
                    _editor.OpenDataSource(name);
                }

                var entities = ds.GetEntitesList()?.ToList();
                if (entities == null || entities.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No entities found[/]");
                    return;
                }

                var table = new Table();
                table.Border = TableBorder.Rounded;
                table.AddColumn("[cyan]Entity Name[/]");
                table.AddColumn("[cyan]Type[/]");

                foreach (var entityName in entities)
                {
                    table.AddRow(
                        entityName,
                        "Entity"
                    );
                }

                AnsiConsole.Write(table);
                AnsiConsole.MarkupLine($"\n[dim]Total: {entities.Count} entities[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error listing entities: {ex.Message}");
            }
        }

        private void TestConnection(string name)
        {
            try
            {
                var ds = _editor.GetDataSource(name);
                if (ds == null)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Data source '{name}' not found");
                    return;
                }

                bool success = false;
                AnsiConsole.Status()
                    .Start($"Testing connection to '{name}'...", ctx =>
                    {
                        try
                        {
                            var wasOpen = ds.ConnectionStatus == System.Data.ConnectionState.Open;
                            
                            if (!wasOpen)
                                _editor.OpenDataSource(name);

                            success = ds.ConnectionStatus == System.Data.ConnectionState.Open;

                            if (!wasOpen && success)
                                _editor.CloseDataSource(name);
                        }
                        catch
                        {
                            success = false;
                        }
                    });

                if (success)
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] Connection test successful");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Connection test failed");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error testing connection: {ex.Message}");
            }
        }

        public bool CanExecute() => _editor != null;

        public string[] GetExamples()
        {
            return new[]
            {
                "datasource list",
                "ds list",
                "datasource open mydb",
                "datasource info mydb",
                "datasource entities mydb",
                "datasource test mydb",
                "datasource close mydb"
            };
        }
    }
}
