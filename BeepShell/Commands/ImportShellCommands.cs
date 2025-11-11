using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using BeepShell.Infrastructure;
using Spectre.Console;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;

namespace BeepShell.Commands
{
    /// <summary>
    /// Import/Export commands for BeepShell
    /// Uses persistent DMEEditor for data import operations
    /// </summary>
    public class ImportShellCommands : IShellCommand
    {
        private IDMEEditor _editor;

        public string CommandName => "import";
        public string Description => "Import data from files";
        public string Category => "Data";
        public string Version => "1.0.0";
        public string Author => "BeepDM Team";
        public string[] Aliases => new[] { "imp", "load-file" };

        public void Initialize(IDMEEditor editor)
        {
            _editor = editor;
        }

        public Command BuildCommand()
        {
            var importCommand = new Command("import", Description);

            // import csv
            var csvCommand = new Command("csv", "Import data from CSV file");
            var fileArg = new Argument<string>("file", "CSV file path");
            var destOption = new Option<string>("--dest", "Destination data source") { IsRequired = true };
            var tableOption = new Option<string>("--table", "Destination table name") { IsRequired = true };
            var hasHeaderOption = new Option<bool>("--header", () => true, "First row contains headers");
            var delimiterOption = new Option<string>("--delimiter", () => ",", "Field delimiter");
            var createOption = new Option<bool>("--create", "Create table if it doesn't exist");

            csvCommand.AddArgument(fileArg);
            csvCommand.AddOption(destOption);
            csvCommand.AddOption(tableOption);
            csvCommand.AddOption(hasHeaderOption);
            csvCommand.AddOption(delimiterOption);
            csvCommand.AddOption(createOption);

            csvCommand.SetHandler((file, dest, table, hasHeader, delimiter, create) =>
            {
                ImportCsv(file, dest, table, hasHeader, delimiter, create);
            }, fileArg, destOption, tableOption, hasHeaderOption, delimiterOption, createOption);

            importCommand.AddCommand(csvCommand);

            // import json
            var jsonCommand = new Command("json", "Import data from JSON file");
            var jsonFileArg = new Argument<string>("file", "JSON file path");
            var jsonDestOption = new Option<string>("--dest", "Destination data source") { IsRequired = true };
            var jsonTableOption = new Option<string>("--table", "Destination table name") { IsRequired = true };
            var jsonCreateOption = new Option<bool>("--create", "Create table if it doesn't exist");

            jsonCommand.AddArgument(jsonFileArg);
            jsonCommand.AddOption(jsonDestOption);
            jsonCommand.AddOption(jsonTableOption);
            jsonCommand.AddOption(jsonCreateOption);

            jsonCommand.SetHandler((file, dest, table, create) =>
            {
                ImportJson(file, dest, table, create);
            }, jsonFileArg, jsonDestOption, jsonTableOption, jsonCreateOption);

            importCommand.AddCommand(jsonCommand);

            return importCommand;
        }

        private void ImportCsv(string filePath, string destName, string tableName, bool hasHeader, string delimiter, bool createTable)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] File not found: {filePath}");
                    return;
                }

                var destDs = _editor.GetDataSource(destName);
                if (destDs == null)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Data source '{destName}' not found");
                    return;
                }

                if (destDs.ConnectionStatus != System.Data.ConnectionState.Open)
                {
                    _editor.OpenDataSource(destName);
                }

                AnsiConsole.Status()
                    .Start($"Importing CSV from {Path.GetFileName(filePath)}...", ctx =>
                    {
                        // Read CSV file
                        var lines = File.ReadAllLines(filePath);
                        if (lines.Length == 0)
                        {
                            AnsiConsole.MarkupLine("[yellow]File is empty[/]");
                            return;
                        }

                        ctx.Status($"Processing {lines.Length} lines...");

                        // Parse CSV (basic implementation)
                        var data = new System.Data.DataTable(tableName);
                        
                        // Parse header
                        var firstLine = lines[0].Split(delimiter);
                        var startRow = 0;

                        if (hasHeader)
                        {
                            foreach (var header in firstLine)
                            {
                                data.Columns.Add(header.Trim());
                            }
                            startRow = 1;
                        }
                        else
                        {
                            for (int i = 0; i < firstLine.Length; i++)
                            {
                                data.Columns.Add($"Column{i + 1}");
                            }
                        }

                        // Parse data rows
                        for (int i = startRow; i < lines.Length; i++)
                        {
                            var values = lines[i].Split(delimiter);
                            if (values.Length == data.Columns.Count)
                            {
                                data.Rows.Add(values);
                            }
                        }

                        ctx.Status($"Parsed {data.Rows.Count} rows");

                        // Create table if needed
                        if (createTable)
                        {
                            ctx.Status($"Creating table {tableName}...");
                            // Would need entity structure creation here
                        }

                        // Import data
                        ctx.Status($"Writing to {tableName}...");
                        var result = destDs.UpdateEntity(tableName, data);

                        if (result != null && result.Flag == Errors.Ok)
                        {
                            AnsiConsole.MarkupLine($"[green]✓[/] Imported {data.Rows.Count} rows");
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] Error importing: {result?.Message}");
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }

        private void ImportJson(string filePath, string destName, string tableName, bool createTable)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] File not found: {filePath}");
                    return;
                }

                var destDs = _editor.GetDataSource(destName);
                if (destDs == null)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Data source '{destName}' not found");
                    return;
                }

                if (destDs.ConnectionStatus != System.Data.ConnectionState.Open)
                {
                    _editor.OpenDataSource(destName);
                }

                AnsiConsole.MarkupLine("[yellow]JSON import requires JsonLoader configuration[/]");
                AnsiConsole.MarkupLine($"[dim]Use JsonLoader data source for JSON files[/]");
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
                "import csv data.csv --dest mydb --table Customers --header",
                "import csv export.csv --dest db --table Products --delimiter \";\" --create",
                "import json data.json --dest mydb --table Orders --create"
            };
        }
    }
}
