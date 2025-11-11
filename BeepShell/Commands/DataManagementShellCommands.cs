using System;
using System.CommandLine;
using System.Linq;
using BeepShell.Infrastructure;
using Spectre.Console;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;

namespace BeepShell.Commands
{
    /// <summary>
    /// Data management commands for BeepShell
    /// Uses persistent DMEEditor for data operations
    /// </summary>
    public class DataManagementShellCommands : IShellCommand
    {
        private IDMEEditor _editor;

        public string CommandName => "data";
        public string Description => "Data management operations";
        public string Category => "Data";
        public string Version => "1.0.0";
        public string Author => "BeepDM Team";
        public string[] Aliases => new[] { "dm", "manage" };

        public void Initialize(IDMEEditor editor)
        {
            _editor = editor;
        }

        public Command BuildCommand()
        {
            var dataCommand = new Command("data", Description);

            // data schema
            var schemaCommand = new Command("schema", "Show table schema/structure");
            var dsArg = new Argument<string>("datasource", "Data source name");
            var tableArg = new Argument<string>("table", "Table name");

            schemaCommand.AddArgument(dsArg);
            schemaCommand.AddArgument(tableArg);

            schemaCommand.SetHandler((datasource, table) =>
            {
                ShowSchema(datasource, table);
            }, dsArg, tableArg);

            dataCommand.AddCommand(schemaCommand);

            // data count
            var countCommand = new Command("count", "Count rows in a table");
            var countDsArg = new Argument<string>("datasource", "Data source name");
            var countTableArg = new Argument<string>("table", "Table name");
            var whereOption = new Option<string>("--where", "WHERE clause filter");

            countCommand.AddArgument(countDsArg);
            countCommand.AddArgument(countTableArg);
            countCommand.AddOption(whereOption);

            countCommand.SetHandler((datasource, table, where) =>
            {
                CountRows(datasource, table, where);
            }, countDsArg, countTableArg, whereOption);

            dataCommand.AddCommand(countCommand);

            // data validate
            var validateCommand = new Command("validate", "Validate data source connection and structure");
            var validateDsArg = new Argument<string>("datasource", "Data source name");
            validateCommand.AddArgument(validateDsArg);
            validateCommand.SetHandler((datasource) => ValidateDataSource(datasource), validateDsArg);
            dataCommand.AddCommand(validateCommand);

            // data clear
            var clearCommand = new Command("clear", "Clear/truncate table data");
            var clearDsArg = new Argument<string>("datasource", "Data source name");
            var clearTableArg = new Argument<string>("table", "Table name");
            var confirmOption = new Option<bool>("--confirm", "Confirm the operation");

            clearCommand.AddArgument(clearDsArg);
            clearCommand.AddArgument(clearTableArg);
            clearCommand.AddOption(confirmOption);

            clearCommand.SetHandler((datasource, table, confirm) =>
            {
                ClearTable(datasource, table, confirm);
            }, clearDsArg, clearTableArg, confirmOption);

            dataCommand.AddCommand(clearCommand);

            return dataCommand;
        }

        private void ShowSchema(string datasourceName, string tableName)
        {
            try
            {
                var ds = _editor.GetDataSource(datasourceName);
                if (ds == null)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Data source '{datasourceName}' not found");
                    return;
                }

                if (ds.ConnectionStatus != System.Data.ConnectionState.Open)
                {
                    _editor.OpenDataSource(datasourceName);
                }

                var structure = ds.GetEntityStructure(tableName, false);
                if (structure == null)
                {
                    AnsiConsole.MarkupLine($"[yellow]Could not retrieve structure for '{tableName}'[/]");
                    return;
                }

                var panel = new Panel(new Markup(
                    $"[cyan]Entity:[/] {structure.EntityName}\n" +
                    $"[cyan]DataSource:[/] {structure.DataSourceID ?? "N/A"}\n" +
                    $"[cyan]Schema:[/] {structure.SchemaOrOwnerOrDatabase ?? "N/A"}\n" +
                    $"[cyan]Type:[/] {structure.Viewtype}\n" +
                    $"[cyan]Columns:[/] {structure.Fields?.Count ?? 0}"
                ));
                panel.Header = new PanelHeader($"[green]{tableName} Schema[/]");
                panel.Border = BoxBorder.Rounded;

                AnsiConsole.Write(panel);

                if (structure.Fields != null && structure.Fields.Count > 0)
                {
                    AnsiConsole.WriteLine();
                    var table = new Table();
                    table.Border = TableBorder.Rounded;
                    table.AddColumn("[cyan]Column Name[/]");
                    table.AddColumn("[cyan]Data Type[/]");
                    table.AddColumn("[cyan]Size[/]");
                    table.AddColumn("[cyan]Nullable[/]");
                    table.AddColumn("[cyan]Key[/]");

                    foreach (var field in structure.Fields)
                    {
                        var nullable = field.AllowDBNull ? "[green]Yes[/]" : "[dim]No[/]";
                        var key = field.IsKey ? "[yellow]PK[/]" : (field.IsUnique ? "[cyan]UQ[/]" : "");

                        table.AddRow(
                            field.fieldname,
                            field.fieldtype ?? "N/A",
                            field.Size1.ToString(),
                            nullable,
                            key
                        );
                    }

                    AnsiConsole.Write(table);
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }

        private void CountRows(string datasourceName, string tableName, string whereClause)
        {
            try
            {
                var ds = _editor.GetDataSource(datasourceName);
                if (ds == null)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Data source '{datasourceName}' not found");
                    return;
                }

                if (ds.ConnectionStatus != System.Data.ConnectionState.Open)
                {
                    _editor.OpenDataSource(datasourceName);
                }

                string sql = $"SELECT COUNT(*) FROM {tableName}";
                if (!string.IsNullOrWhiteSpace(whereClause))
                {
                    sql += $" WHERE {whereClause}";
                }

                var count = ds.GetScalar(sql);
                
                if (count > 0)
                {
                    AnsiConsole.MarkupLine($"[green]Row count:[/] {count:N0}");
                }
                else
                {
                    AnsiConsole.MarkupLine("[yellow]Could not retrieve count[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }

        private void ValidateDataSource(string datasourceName)
        {
            try
            {
                var ds = _editor.GetDataSource(datasourceName);
                if (ds == null)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Data source '{datasourceName}' not found");
                    return;
                }

                AnsiConsole.Status()
                    .Start($"Validating '{datasourceName}'...", ctx =>
                    {
                        var results = new System.Collections.Generic.List<(string check, bool passed, string message)>();

                        // Check 1: Connection
                        ctx.Status("Testing connection...");
                        bool wasOpen = ds.ConnectionStatus == System.Data.ConnectionState.Open;
                        bool canConnect = false;
                        try
                        {
                            if (!wasOpen)
                                _editor.OpenDataSource(datasourceName);
                            
                            canConnect = ds.ConnectionStatus == System.Data.ConnectionState.Open;
                            results.Add(("Connection", canConnect, canConnect ? "Connected successfully" : "Failed to connect"));

                            if (!wasOpen && canConnect)
                                _editor.CloseDataSource(datasourceName);
                        }
                        catch (Exception ex)
                        {
                            results.Add(("Connection", false, ex.Message));
                        }

                        // Check 2: Entity List
                        ctx.Status("Retrieving entities...");
                        try
                        {
                            if (canConnect)
                            {
                                if (ds.ConnectionStatus != System.Data.ConnectionState.Open)
                                    _editor.OpenDataSource(datasourceName);

                                var entities = ds.GetEntitesList()?.ToList();
                                var entityCount = entities?.Count ?? 0;
                                results.Add(("Entities", entityCount > 0, $"Found {entityCount} entities"));
                            }
                        }
                        catch (Exception ex)
                        {
                            results.Add(("Entities", false, ex.Message));
                        }

                        // Display results
                        AnsiConsole.WriteLine();
                        var table = new Table();
                        table.Border = TableBorder.Rounded;
                        table.AddColumn("[cyan]Check[/]");
                        table.AddColumn("[cyan]Status[/]");
                        table.AddColumn("[cyan]Details[/]");

                        foreach (var (check, passed, message) in results)
                        {
                            var status = passed ? "[green]✓ Pass[/]" : "[red]✗ Fail[/]";
                            table.AddRow(check, status, message);
                        }

                        AnsiConsole.Write(table);
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }

        private void ClearTable(string datasourceName, string tableName, bool confirm)
        {
            try
            {
                if (!confirm)
                {
                    AnsiConsole.MarkupLine("[yellow]This operation will delete all data from the table![/]");
                    AnsiConsole.MarkupLine("[yellow]Add --confirm flag to proceed[/]");
                    return;
                }

                var ds = _editor.GetDataSource(datasourceName);
                if (ds == null)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Data source '{datasourceName}' not found");
                    return;
                }

                if (ds.ConnectionStatus != System.Data.ConnectionState.Open)
                {
                    _editor.OpenDataSource(datasourceName);
                }

                AnsiConsole.Status()
                    .Start($"Clearing table '{tableName}'...", ctx =>
                    {
                        var sql = $"DELETE FROM {tableName}";
                        var result = ds.ExecuteSql(sql);

                        if (result != null && result.Flag == Errors.Failed)
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] Error: {result.Message}");
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[green]✓[/] Table '{tableName}' cleared");
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }

        public bool CanExecute() => _editor != null;

        public string[] GetExamples()
        {
            return new[]
            {
                "data schema mydb Users",
                "data count mydb Products",
                "data count mydb Orders --where \"Status = 'Pending'\"",
                "data validate mydb",
                "data clear mydb TempTable --confirm"
            };
        }
    }
}
