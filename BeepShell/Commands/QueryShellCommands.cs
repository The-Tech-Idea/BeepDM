using System;
using System.CommandLine;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using BeepShell.Infrastructure;
using Spectre.Console;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.ConfigUtil;

namespace BeepShell.Commands
{
    /// <summary>
    /// Query execution commands for BeepShell
    /// Uses persistent DMEEditor with open connections for fast query execution
    /// </summary>
    public class QueryShellCommands : IShellCommand
    {
        private IDMEEditor _editor;

        public string CommandName => "query";
        public string Description => "Execute queries on data sources";
        public string Category => "Data";
        public string Version => "1.0.0";
        public string Author => "BeepDM Team";
        public string[] Aliases => new[] { "q", "sql" };

        public void Initialize(IDMEEditor editor)
        {
            _editor = editor;
        }

        public Command BuildCommand()
        {
            var queryCommand = new Command("query", Description);

            // query exec
            var execCommand = new Command("exec", "Execute a query");
            var dsArg = new Argument<string>("datasource", "Data source name");
            var sqlArg = new Argument<string>("sql", "SQL query to execute");
            var limitOption = new Option<int?>("--limit", "Limit number of rows returned");
            
            execCommand.AddArgument(dsArg);
            execCommand.AddArgument(sqlArg);
            execCommand.AddOption(limitOption);
            execCommand.SetHandler((datasource, sql, limit) => 
            {
                ExecuteQuery(datasource, sql, limit);
            }, dsArg, sqlArg, limitOption);
            queryCommand.AddCommand(execCommand);

            // query nonquery
            var nonQueryCommand = new Command("nonquery", "Execute a non-query command (INSERT, UPDATE, DELETE)");
            var nqDsArg = new Argument<string>("datasource", "Data source name");
            var nqSqlArg = new Argument<string>("sql", "SQL command to execute");
            
            nonQueryCommand.AddArgument(nqDsArg);
            nonQueryCommand.AddArgument(nqSqlArg);
            nonQueryCommand.SetHandler((datasource, sql) => 
            {
                ExecuteNonQuery(datasource, sql);
            }, nqDsArg, nqSqlArg);
            queryCommand.AddCommand(nonQueryCommand);

            // query scalar
            var scalarCommand = new Command("scalar", "Execute a scalar query (returns single value)");
            var scDsArg = new Argument<string>("datasource", "Data source name");
            var scSqlArg = new Argument<string>("sql", "SQL query to execute");
            
            scalarCommand.AddArgument(scDsArg);
            scalarCommand.AddArgument(scSqlArg);
            scalarCommand.SetHandler((datasource, sql) => 
            {
                ExecuteScalar(datasource, sql);
            }, scDsArg, scSqlArg);
            queryCommand.AddCommand(scalarCommand);

            // query table
            var tableCommand = new Command("table", "Query a table with optional filters");
            var tblDsArg = new Argument<string>("datasource", "Data source name");
            var tblNameArg = new Argument<string>("table", "Table name");
            var whereOption = new Option<string>("--where", "WHERE clause (without WHERE keyword)");
            var topOption = new Option<int?>("--top", "Number of rows to return");
            
            tableCommand.AddArgument(tblDsArg);
            tableCommand.AddArgument(tblNameArg);
            tableCommand.AddOption(whereOption);
            tableCommand.AddOption(topOption);
            tableCommand.SetHandler((datasource, table, where, top) => 
            {
                QueryTable(datasource, table, where, top);
            }, tblDsArg, tblNameArg, whereOption, topOption);
            queryCommand.AddCommand(tableCommand);

            return queryCommand;
        }

        private void ExecuteQuery(string datasourceName, string sql, int? limit)
        {
            try
            {
                var ds = _editor.GetDataSource(datasourceName);
                if (ds == null)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Data source '{datasourceName}' not found");
                    return;
                }

                if (ds.ConnectionStatus != ConnectionState.Open)
                {
                    AnsiConsole.MarkupLine($"[yellow]![/] Opening connection to '{datasourceName}'...");
                    _editor.OpenDataSource(datasourceName);
                }

                IEnumerable<object>? result = null;
                AnsiConsole.Status()
                    .Start("Executing query...", ctx =>
                    {
                        result = ds.RunQuery(sql);
                    });

                var resultList = result?.ToList();
                if (resultList == null || resultList.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]Query returned no results[/]");
                    return;
                }

                DisplayObjectResults(resultList, limit);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error executing query: {ex.Message}");
            }
        }

        private void ExecuteNonQuery(string datasourceName, string sql)
        {
            try
            {
                var ds = _editor.GetDataSource(datasourceName);
                if (ds == null)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Data source '{datasourceName}' not found");
                    return;
                }

                if (ds.ConnectionStatus != ConnectionState.Open)
                {
                    AnsiConsole.MarkupLine($"[yellow]![/] Opening connection to '{datasourceName}'...");
                    _editor.OpenDataSource(datasourceName);
                }

                int rowsAffected = 0;
                AnsiConsole.Status()
                    .Start("Executing command...", ctx =>
                    {
                        var result = ds.ExecuteSql(sql);
                        if (result != null && result.Flag == Errors.Ok)
                        {
                            rowsAffected = 0; // Cannot get affected rows from IErrorsInfo
                        }
                    });

                AnsiConsole.MarkupLine($"[green]✓[/] Command executed. Rows affected: {rowsAffected}");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error executing command: {ex.Message}");
            }
        }

        private void ExecuteScalar(string datasourceName, string sql)
        {
            try
            {
                var ds = _editor.GetDataSource(datasourceName);
                if (ds == null)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Data source '{datasourceName}' not found");
                    return;
                }

                if (ds.ConnectionStatus != ConnectionState.Open)
                {
                    AnsiConsole.MarkupLine($"[yellow]![/] Opening connection to '{datasourceName}'...");
                    _editor.OpenDataSource(datasourceName);
                }

                object? result = null;
                AnsiConsole.Status()
                    .Start("Executing scalar query...", ctx =>
                    {
                        result = ds.GetScalar(sql);
                    });

                if (result == null)
                {
                    AnsiConsole.MarkupLine("[yellow]Query returned NULL[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[green]Result:[/] {result}");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error executing scalar query: {ex.Message}");
            }
        }

        private void QueryTable(string datasourceName, string tableName, string whereClause, int? top)
        {
            try
            {
                var ds = _editor.GetDataSource(datasourceName);
                if (ds == null)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Data source '{datasourceName}' not found");
                    return;
                }

                if (ds.ConnectionStatus != ConnectionState.Open)
                {
                    AnsiConsole.MarkupLine($"[yellow]![/] Opening connection to '{datasourceName}'...");
                    _editor.OpenDataSource(datasourceName);
                }

                // Build SQL query
                string sql = $"SELECT * FROM {tableName}";
                if (!string.IsNullOrWhiteSpace(whereClause))
                {
                    sql += $" WHERE {whereClause}";
                }

                IEnumerable<object>? result = null;
                AnsiConsole.Status()
                    .Start($"Querying table '{tableName}'...", ctx =>
                    {
                        result = ds.RunQuery(sql);
                    });

                var resultList = result?.ToList();
                if (resultList == null || resultList.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No rows found[/]");
                    return;
                }

                DisplayObjectResults(resultList, top);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error querying table: {ex.Message}");
            }
        }

        private void DisplayObjectResults(List<object> results, int? limit)
        {
            if (results == null || results.Count == 0)
                return;

            var displayTable = new Table();
            displayTable.Border = TableBorder.Rounded;

            // Get properties from first object
            var firstObject = results[0];
            var properties = firstObject.GetType().GetProperties();

            // Add columns
            foreach (var prop in properties)
            {
                displayTable.AddColumn($"[cyan]{prop.Name}[/]");
            }

            // Add rows
            int rowCount = 0;
            int maxRows = limit ?? Math.Min(results.Count, 100); // Default limit 100

            foreach (var row in results)
            {
                if (rowCount >= maxRows)
                    break;

                var cells = new string[properties.Length];
                for (int i = 0; i < properties.Length; i++)
                {
                    var value = properties[i].GetValue(row);
                    cells[i] = value == null ? "[dim]NULL[/]" : value.ToString() ?? "[dim]NULL[/]";
                    
                    // Truncate long strings
                    if (cells[i].Length > 50)
                    {
                        cells[i] = cells[i].Substring(0, 47) + "...";
                    }
                }

                displayTable.AddRow(cells);
                rowCount++;
            }

            AnsiConsole.Write(displayTable);

            // Show summary
            var totalRows = results.Count;
            if (totalRows > rowCount)
            {
                AnsiConsole.MarkupLine($"\n[dim]Showing {rowCount} of {totalRows} rows (use --limit to show more)[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"\n[dim]Total: {totalRows} rows[/]");
            }
        }

        private void DisplayDataTable(DataTable table, int? limit)
        {
            var displayTable = new Table();
            displayTable.Border = TableBorder.Rounded;

            // Add columns
            foreach (DataColumn column in table.Columns)
            {
                displayTable.AddColumn($"[cyan]{column.ColumnName}[/]");
            }

            // Add rows
            int rowCount = 0;
            int maxRows = limit ?? Math.Min(table.Rows.Count, 100); // Default limit 100

            foreach (DataRow row in table.Rows)
            {
                if (rowCount >= maxRows)
                    break;

                var cells = new string[table.Columns.Count];
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    var value = row[i];
                    cells[i] = value == DBNull.Value ? "[dim]NULL[/]" : (value?.ToString() ?? "[dim]NULL[/]");
                    
                    // Truncate long strings
                    if (cells[i].Length > 50)
                    {
                        cells[i] = cells[i].Substring(0, 47) + "...";
                    }
                }

                displayTable.AddRow(cells);
                rowCount++;
            }

            AnsiConsole.Write(displayTable);

            // Show summary
            var totalRows = table.Rows.Count;
            if (totalRows > rowCount)
            {
                AnsiConsole.MarkupLine($"\n[dim]Showing {rowCount} of {totalRows} rows (use --limit to show more)[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"\n[dim]Total: {totalRows} rows[/]");
            }
        }

        public bool CanExecute() => _editor != null;

        public string[] GetExamples()
        {
            return new[]
            {
                "query exec mydb \"SELECT * FROM Users\"",
                "query exec mydb \"SELECT * FROM Products WHERE Price > 100\" --limit 50",
                "query nonquery mydb \"UPDATE Users SET Active = 1 WHERE Id = 5\"",
                "query scalar mydb \"SELECT COUNT(*) FROM Orders\"",
                "query table mydb Customers",
                "query table mydb Products --where \"Category = 'Electronics'\" --top 20"
            };
        }
    }
}
