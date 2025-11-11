using System;
using System.CommandLine;
using System.Linq;
using System.Collections.Generic;
using BeepShell.Infrastructure;
using Spectre.Console;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.ConfigUtil;

namespace BeepShell.Commands
{
    /// <summary>
    /// ETL (Extract, Transform, Load) commands for BeepShell
    /// Uses persistent DMEEditor for data transformations
    /// </summary>
    public class ETLShellCommands : IShellCommand
    {
        private IDMEEditor _editor;

        public string CommandName => "etl";
        public string Description => "Extract, Transform, Load operations";
        public string Category => "Data";
        public string Version => "1.0.0";
        public string Author => "BeepDM Team";
        public string[] Aliases => new[] { "transform", "load" };

        public void Initialize(IDMEEditor editor)
        {
            _editor = editor;
        }

        public Command BuildCommand()
        {
            var etlCommand = new Command("etl", Description);

            // etl copy
            var copyCommand = new Command("copy", "Copy data from source to destination");
            var sourceOption = new Option<string>("--source", "Source data source name") { IsRequired = true };
            var sourceTableOption = new Option<string>("--source-table", "Source table/entity name") { IsRequired = true };
            var destOption = new Option<string>("--dest", "Destination data source name") { IsRequired = true };
            var destTableOption = new Option<string>("--dest-table", "Destination table/entity name") { IsRequired = true };
            var createOption = new Option<bool>("--create", "Create destination table if it doesn't exist");

            copyCommand.AddOption(sourceOption);
            copyCommand.AddOption(sourceTableOption);
            copyCommand.AddOption(destOption);
            copyCommand.AddOption(destTableOption);
            copyCommand.AddOption(createOption);

            copyCommand.SetHandler((source, sourceTable, dest, destTable, create) =>
            {
                CopyData(source, sourceTable, dest, destTable, create);
            }, sourceOption, sourceTableOption, destOption, destTableOption, createOption);

            etlCommand.AddCommand(copyCommand);

            // etl transform
            var transformCommand = new Command("transform", "Apply transformations to data");
            var transformSourceOption = new Option<string>("--source", "Source data source") { IsRequired = true };
            var transformTableOption = new Option<string>("--table", "Table name") { IsRequired = true };
            var scriptOption = new Option<string>("--script", "Transformation script/mapping");

            transformCommand.AddOption(transformSourceOption);
            transformCommand.AddOption(transformTableOption);
            transformCommand.AddOption(scriptOption);

            transformCommand.SetHandler((source, table, script) =>
            {
                TransformData(source, table, script);
            }, transformSourceOption, transformTableOption, scriptOption);

            etlCommand.AddCommand(transformCommand);

            return etlCommand;
        }

        private void CopyData(string sourceName, string sourceTable, string destName, string destTable, bool createTable)
        {
            try
            {
                var sourceDs = _editor.GetDataSource(sourceName);
                var destDs = _editor.GetDataSource(destName);

                if (sourceDs == null)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Source data source '{sourceName}' not found");
                    return;
                }

                if (destDs == null)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Destination data source '{destName}' not found");
                    return;
                }

                // Ensure connections are open
                if (sourceDs.ConnectionStatus != System.Data.ConnectionState.Open)
                {
                    _editor.OpenDataSource(sourceName);
                }

                if (destDs.ConnectionStatus != System.Data.ConnectionState.Open)
                {
                    _editor.OpenDataSource(destName);
                }

                AnsiConsole.Status()
                    .Start($"Copying data from {sourceName}.{sourceTable} to {destName}.{destTable}...", ctx =>
                    {
                        // Get source data
                        ctx.Status($"Reading from {sourceName}.{sourceTable}...");
                        var data = sourceDs.GetEntity(sourceTable, null)?.ToList();

                        if (data == null || data.Count == 0)
                        {
                            AnsiConsole.MarkupLine("[yellow]No data to copy[/]");
                            return;
                        }

                        ctx.Status($"Read {data.Count} rows");

                        // Create destination table if needed
                        if (createTable)
                        {
                            ctx.Status($"Creating table {destTable}...");
                            var entityStructure = sourceDs.GetEntityStructure(sourceTable, false);
                            destDs.CreateEntityAs(entityStructure);
                        }

                        // Write to destination
                        ctx.Status($"Writing to {destName}.{destTable}...");
                        var result = destDs.UpdateEntity(destTable, data);

                        if (result != null && result.Flag == Errors.Ok)
                        {
                            AnsiConsole.MarkupLine($"[green]✓[/] Copied {data.Count} rows successfully");
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] Error copying data: {result?.Message}");
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }

        private void TransformData(string sourceName, string tableName, string script)
        {
            try
            {
                var ds = _editor.GetDataSource(sourceName);
                if (ds == null)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Data source '{sourceName}' not found");
                    return;
                }

                if (ds.ConnectionStatus != System.Data.ConnectionState.Open)
                {
                    _editor.OpenDataSource(sourceName);
                }

                AnsiConsole.MarkupLine($"[yellow]Transform feature requires mapping configuration[/]");
                AnsiConsole.MarkupLine($"[dim]Use 'mapping' commands to configure transformations[/]");
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
                "etl copy --source sourceDb --source-table Users --dest destDb --dest-table Users_Copy",
                "etl copy --source db1 --source-table Products --dest db2 --dest-table Products --create",
                "etl transform --source mydb --table Orders --script transform.json"
            };
        }
    }
}
