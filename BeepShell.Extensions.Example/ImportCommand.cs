using System.CommandLine;
using BeepShell.Infrastructure;
using Spectre.Console;
using TheTechIdea.Beep.Editor;

namespace BeepShell.Extensions.Example
{
    /// <summary>
    /// Example import command - counterpart to ExportCommand
    /// </summary>
    public class ImportCommand : IShellCommand
    {
        private IDMEEditor _editor;

        public string CommandName => "import";
        public string Description => "Import data from CSV to a data source";
        public string Category => "Data";
        public string Version => "1.0.0";
        public string Author => "BeepDM Team";

        public void Initialize(IDMEEditor editor)
        {
            _editor = editor;
        }

        public Command BuildCommand()
        {
            var command = new Command("import", Description);

            var fileOption = new Option<string>(
                aliases: new[] { "--file", "-f" },
                description: "CSV file path to import"
            ) { IsRequired = true };

            var targetOption = new Option<string>(
                aliases: new[] { "--target", "-t" },
                description: "Target data source name"
            ) { IsRequired = true };

            var tableOption = new Option<string>(
                aliases: new[] { "--table", "-b" },
                description: "Target table name"
            ) { IsRequired = true };

            var truncateOption = new Option<bool>(
                aliases: new[] { "--truncate" },
                description: "Truncate table before import",
                getDefaultValue: () => false
            );

            command.AddOption(fileOption);
            command.AddOption(targetOption);
            command.AddOption(tableOption);
            command.AddOption(truncateOption);

            command.SetHandler(async (file, target, table, truncate) =>
            {
                await ExecuteImport(file, target, table, truncate);
            }, fileOption, targetOption, tableOption, truncateOption);

            return command;
        }

        private async Task ExecuteImport(string filePath, string targetName, string tableName, bool truncate)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new Exception($"File not found: {filePath}");
                }

                await AnsiConsole.Status()
                    .StartAsync("Importing data...", async ctx =>
                    {
                        // Get target data source
                        var ds = _editor.GetDataSource(targetName);
                        if (ds == null)
                        {
                            throw new Exception($"Data source '{targetName}' not found");
                        }

                        // Open connection
                        if (ds.ConnectionStatus != System.Data.ConnectionState.Open)
                        {
                            ctx.Status("Opening connection...");
                            ds.Openconnection();
                        }

                        // Read CSV
                        ctx.Status($"Reading '{filePath}'...");
                        var data = await ReadCsv(filePath);

                        AnsiConsole.MarkupLine($"[dim]Loaded {data.Rows.Count} rows with {data.Columns.Count} columns[/]");

                        // Truncate if requested
                        if (truncate)
                        {
                            ctx.Status($"Truncating '{tableName}'...");
                            // Implement truncate logic here
                        }

                        // Import data
                        ctx.Status($"Writing to '{tableName}'...");
                        ds.UpdateEntity(tableName, data);

                        AnsiConsole.MarkupLine($"[green]✓[/] Imported {data.Rows.Count} rows to '{tableName}'");
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Import failed: {ex.Message}");
            }
        }

        private async Task<System.Data.DataTable> ReadCsv(string path)
        {
            var table = new System.Data.DataTable();
            var lines = await File.ReadAllLinesAsync(path);

            if (lines.Length == 0)
                return table;

            // Parse header
            var headers = ParseCsvLine(lines[0]);
            foreach (var header in headers)
            {
                table.Columns.Add(header);
            }

            // Parse rows
            for (int i = 1; i < lines.Length; i++)
            {
                var values = ParseCsvLine(lines[i]);
                table.Rows.Add(values);
            }

            return table;
        }

        private string[] ParseCsvLine(string line)
        {
            var values = new List<string>();
            var current = "";
            var inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                var c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current += '"';
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    values.Add(current);
                    current = "";
                }
                else
                {
                    current += c;
                }
            }

            values.Add(current);
            return values.ToArray();
        }

        public bool CanExecute()
        {
            return _editor != null;
        }

        public string[] GetExamples()
        {
            return new[]
            {
                "import --file customers.csv --target mydb --table customers",
                "import -f orders.csv -t mydb -b orders --truncate",
                "import -f users.csv -t api -b users"
            };
        }
    }
}
