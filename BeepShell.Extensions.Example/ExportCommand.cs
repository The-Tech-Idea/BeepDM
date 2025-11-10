using System.CommandLine;
using BeepShell.Infrastructure;
using Spectre.Console;
using TheTechIdea.Beep.Editor;

namespace BeepShell.Extensions.Example
{
    /// <summary>
    /// Example shell command that exports data from a data source to CSV
    /// </summary>
    public class ExportCommand : IShellCommand
    {
        private IDMEEditor _editor;

        public string CommandName => "export";
        public string Description => "Export data from a data source to CSV";
        public string Category => "Data";
        public string Version => "1.0.0";
        public string Author => "BeepDM Team";

        public void Initialize(IDMEEditor editor)
        {
            _editor = editor;
        }

        public Command BuildCommand()
        {
            var command = new Command("export", Description);

            // Add options
            var sourceOption = new Option<string>(
                aliases: new[] { "--source", "-s" },
                description: "Source data source name"
            ) { IsRequired = true };

            var tableOption = new Option<string>(
                aliases: new[] { "--table", "-t" },
                description: "Table or entity name to export"
            ) { IsRequired = true };

            var outputOption = new Option<string>(
                aliases: new[] { "--output", "-o" },
                description: "Output CSV file path"
            ) { IsRequired = true };

            var limitOption = new Option<int?>(
                aliases: new[] { "--limit", "-l" },
                description: "Maximum number of rows to export (optional)"
            );

            command.AddOption(sourceOption);
            command.AddOption(tableOption);
            command.AddOption(outputOption);
            command.AddOption(limitOption);

            // Set handler
            command.SetHandler(async (source, table, output, limit) =>
            {
                await ExecuteExport(source, table, output, limit);
            }, sourceOption, tableOption, outputOption, limitOption);

            return command;
        }

        private async Task ExecuteExport(string sourceName, string tableName, string outputPath, int? limit)
        {
            try
            {
                await AnsiConsole.Status()
                    .StartAsync("Exporting data...", async ctx =>
                    {
                        // Get data source
                        var ds = _editor.GetDataSource(sourceName);
                        if (ds == null)
                        {
                            throw new Exception($"Data source '{sourceName}' not found");
                        }

                        // Open connection if needed
                        if (ds.ConnectionStatus != System.Data.ConnectionState.Open)
                        {
                            ctx.Status("Opening connection...");
                            ds.Openconnection();
                        }

                        // Get data
                        ctx.Status($"Reading data from '{tableName}'...");
                        var data = ds.GetEntity(tableName, null);
                        
                        if (data == null)
                        {
                            throw new Exception($"Failed to read data from '{tableName}'");
                        }

                        // Apply limit if specified
                        var rowCount = data.Rows.Count;
                        if (limit.HasValue && limit.Value < rowCount)
                        {
                            // Create limited view
                            var limitedTable = data.Clone();
                            for (int i = 0; i < limit.Value; i++)
                            {
                                limitedTable.ImportRow(data.Rows[i]);
                            }
                            data = limitedTable;
                        }

                        // Export to CSV
                        ctx.Status($"Writing to '{outputPath}'...");
                        await WriteToCsv(data, outputPath);

                        AnsiConsole.MarkupLine($"[green]✓[/] Exported {data.Rows.Count} rows to '{outputPath}'");
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Export failed: {ex.Message}");
            }
        }

        private async Task WriteToCsv(System.Data.DataTable data, string path)
        {
            using var writer = new StreamWriter(path);

            // Write headers
            var headers = string.Join(",", data.Columns.Cast<System.Data.DataColumn>()
                .Select(c => EscapeCsvField(c.ColumnName)));
            await writer.WriteLineAsync(headers);

            // Write rows
            foreach (System.Data.DataRow row in data.Rows)
            {
                var values = string.Join(",", row.ItemArray.Select(v => 
                    EscapeCsvField(v?.ToString() ?? "")));
                await writer.WriteLineAsync(values);
            }
        }

        private string EscapeCsvField(string field)
        {
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }
            return field;
        }

        public bool CanExecute()
        {
            return _editor != null && _editor.DataSources.Count > 0;
        }

        public string[] GetExamples()
        {
            return new[]
            {
                "export --source mydb --table customers --output customers.csv",
                "export -s mydb -t orders -o orders.csv --limit 1000",
                "export -s api -t users -o users.csv"
            };
        }
    }
}
