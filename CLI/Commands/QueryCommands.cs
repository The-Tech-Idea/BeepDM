using System;
using System.CommandLine;
using System.Linq;
using TheTechIdea.Beep.Report;
using System.Data;
using System.Reflection;
using TheTechIdea.Beep.DataBase;
using Spectre.Console;
using TheTechIdea.Beep.CLI.Infrastructure;
using System.Collections.Generic;

namespace TheTechIdea.Beep.CLI.Commands
{
    /// <summary>
    /// Query execution commands
    /// </summary>
    public static class QueryCommands
    {
        public static Command Build()
        {
            var queryCommand = new Command("query", "Execute queries and inspect entities in data sources");

            var profileOption = new Option<string>("--profile", () => ProfileManager.DEFAULT_PROFILE, "Profile to use");

            // query exec
            var execCommand = new Command("exec", "Execute a SQL query");
            var dsArg = new Argument<string>("datasource", "Data source name");
            var sqlArg = new Argument<string>("sql", "SQL query to execute");
            var limitOption = new Option<int>("--limit", () => 100, "Maximum number of rows to return");
            var outOption = new Option<string?>("--out", "Output file (CSV or JSON)");
            execCommand.AddArgument(dsArg);
            execCommand.AddArgument(sqlArg);
            execCommand.AddOption(limitOption);
            execCommand.AddOption(outOption);
            execCommand.AddOption(profileOption);

            execCommand.SetHandler((string datasource, string sql, int limit, string? outFile, string profile) =>
            {
                var services = new BeepServiceProvider(profile);
                var editor = services.GetEditor();
                var sw = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    AnsiConsole.Status()
                        .Start($"Executing query on '{datasource}'...", ctx =>
                        {
                            var ds = editor.GetDataSource(datasource);
                            if (ds == null)
                            {
                                AnsiConsole.MarkupLine($"[red]✗[/] Data source '{datasource}' not found");
                                return;
                            }
                            if (ds.ConnectionStatus != System.Data.ConnectionState.Open)
                                editor.OpenDataSource(datasource);

                            var result = ds.RunQuery(sql);
                            sw.Stop();
                            if (result == null)
                            {
                                AnsiConsole.MarkupLine("[yellow]Query returned no results[/]");
                                return;
                            }

                            AnsiConsole.MarkupLine($"[green]✓[/] Query executed in [bold]{sw.ElapsedMilliseconds} ms[/]");
                            FormatAndDisplayResult(result, limit, outFile);
                        });
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Query execution failed: {ex.Message}");
                }
            }, dsArg, sqlArg, limitOption, outOption, profileOption);

            // query entity
            var entityCommand = new Command("entity", "Query an entity from a data source");
            var entityDsArg = new Argument<string>("datasource", "Data source name");
            var entityNameArg = new Argument<string>("entity", "Entity name");
            var filterOption = new Option<string?>("--filter", "Filter expression (e.g. field=value)");
            var entityLimitOption = new Option<int>("--limit", () => 100, "Maximum number of rows");
            var outEntityOption = new Option<string?>("--out", "Output file (CSV or JSON)");
            var showSchemaOption = new Option<bool>("--schema", () => false, "Show entity schema");
            entityCommand.AddArgument(entityDsArg);
            entityCommand.AddArgument(entityNameArg);
            entityCommand.AddOption(filterOption);
            entityCommand.AddOption(entityLimitOption);
            entityCommand.AddOption(outEntityOption);
            entityCommand.AddOption(showSchemaOption);
            entityCommand.AddOption(profileOption);

            entityCommand.SetHandler((string datasource, string entity, string? filter, int limit, string? outFile, bool showSchema, string profile) =>
            {
                var services = new BeepServiceProvider(profile);
                var editor = services.GetEditor();
                var sw = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    var ds = editor.GetDataSource(datasource);
                    if (ds == null)
                    {
                        AnsiConsole.MarkupLine($"[red]✗[/] Data source '{datasource}' not found");
                        return;
                    }
                    if (ds.ConnectionStatus != System.Data.ConnectionState.Open)
                        editor.OpenDataSource(datasource);

                    if (showSchema)
                    {
                        var schema = ds.GetEntityStructure(entity, false);
                        if (schema == null)
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] No schema found for entity '{entity}'");
                            return;
                        }
                        var table = new Table().Title($"[bold]Schema for {entity}[/]");
                        table.AddColumn("Field");
                        table.AddColumn("Type");
                        foreach (var f in schema.Fields)
                            table.AddRow(f.fieldname, f.fieldtype ?? "");
                        AnsiConsole.Write(table);
                        return;
                    }

                    // Parse filter (simple field=value)
                    List<AppFilter>? filters = null;
                    if (!string.IsNullOrWhiteSpace(filter))
                    {
                        filters = new List<AppFilter>();
                        var parts = filter.Split('=', 2);
                        if (parts.Length == 2)
                        {
                            filters.Add(new     AppFilter { FieldName = parts[0].Trim(), FilterValue = parts[1].Trim() });
                        }
                    }

                    var result = ds.GetEntity(entity, filters);
                    sw.Stop();
                    if (result == null)
                    {
                        AnsiConsole.MarkupLine("[yellow]No data found[/]");
                        return;
                    }
                    AnsiConsole.MarkupLine($"[green]✓[/] Entity '{entity}' queried in [bold]{sw.ElapsedMilliseconds} ms[/]");
                    FormatAndDisplayResult(result, limit, outFile);
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Query failed: {ex.Message}");
                }
            }, entityDsArg, entityNameArg, filterOption, entityLimitOption, outEntityOption, showSchemaOption, profileOption);

            queryCommand.AddCommand(execCommand);
            queryCommand.AddCommand(entityCommand);

            return queryCommand;
        }

        // Helper: Format and display results
        private static void FormatAndDisplayResult(object result, int limit, string? outFile)
        {
            if (result == null) return;
            // DataTable
            if (result is DataTable dt)
            {
                var rowCount = dt.Rows.Count;
                var table = new Table().Title($"[bold]Results ({Math.Min(rowCount, limit)} of {rowCount})[/]");
                foreach (System.Data.DataColumn col in dt.Columns)
                    table.AddColumn(col.ColumnName);
                for (int i = 0; i < Math.Min(rowCount, limit); i++)
                {
                    var row = dt.Rows[i];
                    table.AddRow(((object[])row.ItemArray).Select(v => v?.ToString() ?? "").ToArray());
                }
                AnsiConsole.Write(table);
                if (!string.IsNullOrEmpty(outFile))
                {
                    if (outFile.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                        WriteDataTableToCsv(dt, outFile);
                    else if (outFile.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                        WriteDataTableToJson(dt, outFile);
                }
            }
            // IEnumerable<object>
            else if (result is System.Collections.IEnumerable enumerable && !(result is string))
            {
                var list = enumerable.Cast<object>().Take(limit).ToList();
                if (list.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No results[/]");
                    return;
                }
                var first = list[0];
                var props = first.GetType().GetProperties();
                var table = new Table().Title($"[bold]Results ({list.Count})[/]");
                foreach (var p in props)
                    table.AddColumn(p.Name);
                foreach (var item in list)
                {
                    table.AddRow(props.Select(p => p.GetValue(item)?.ToString() ?? "").ToArray());
                }
                AnsiConsole.Write(table);
                if (!string.IsNullOrEmpty(outFile))
                {
                    if (outFile.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                        WriteObjectsToCsv(list, props, outFile);
                    else if (outFile.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                        WriteObjectsToJson(list, outFile);
                }
            }
            else
            {
                AnsiConsole.WriteLine(result?.ToString() ?? string.Empty);
            }
        }

        private static void WriteDataTableToCsv(System.Data.DataTable dt, string path)
        {
            using var sw = new System.IO.StreamWriter(path);
            sw.WriteLine(string.Join(",", dt.Columns.OfType<DataColumn>().Select(c => c.ColumnName)));
            foreach (DataRow row in dt.Rows)
                sw.WriteLine(string.Join(",", ((object[])row.ItemArray).Select(v => v?.ToString()?.Replace(",", " ") ?? "")));
        }
        private static void WriteDataTableToJson(System.Data.DataTable dt, string path)
        {
            var rows = dt.Rows.OfType<DataRow>().Select(r => dt.Columns.OfType<DataColumn>().ToDictionary(c => c.ColumnName, c => r[c]));
            var json = System.Text.Json.JsonSerializer.Serialize(rows, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(path, json);
        }
        private static void WriteObjectsToCsv(System.Collections.Generic.List<object> list, System.Reflection.PropertyInfo[] props, string path)
        {
            using var sw = new System.IO.StreamWriter(path);
            sw.WriteLine(string.Join(",", props.Select(p => p.Name)));
            foreach (var item in list)
                sw.WriteLine(string.Join(",", props.Select(p => p.GetValue(item)?.ToString()?.Replace(",", " ") ?? "")));
        }
        private static void WriteObjectsToJson(System.Collections.Generic.List<object> list, string path)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(list, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(path, json);
        }
    }
}
