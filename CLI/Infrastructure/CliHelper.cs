using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Spectre.Console;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.CLI.Infrastructure
{
    /// <summary>
    /// Helper utilities for CLI operations
    /// </summary>
    public static class CliHelper
    {
        /// <summary>
        /// Display entity field information in a formatted table
        /// </summary>
        public static void DisplayEntityFields(EntityStructure entity)
        {
            if (entity == null || entity.Fields == null || !entity.Fields.Any())
            {
                AnsiConsole.MarkupLine("[yellow]⚠[/] No fields found");
                return;
            }

            var table = new Table();
            table.Border = TableBorder.Rounded;
            table.Title = new TableTitle($"[bold cyan]{entity.EntityName} - Fields[/]");
            table.AddColumn("Field Name");
            table.AddColumn("Type");
            table.AddColumn("Nullable");
            table.AddColumn("Key");
            table.AddColumn("Auto Inc");
            table.AddColumn("Size");

            foreach (var field in entity.Fields)
            {
                table.AddRow(
                    $"[cyan]{field.fieldname}[/]",
                    field.fieldtype ?? "-",
                    field.AllowDBNull ? "[yellow]Yes[/]" : "[dim]No[/]",
                    field.IsKey ? "[green]✓[/]" : "",
                    field.IsAutoIncrement ? "[green]✓[/]" : "",
                    field.Size1 > 0 ? field.Size1.ToString() : "-"
                );
            }

            AnsiConsole.Write(table);
        }

        /// <summary>
        /// Display a list of data sources
        /// </summary>
        public static void DisplayDataSources(IDMEEditor editor)
        {
            var connections = editor.ConfigEditor.DataConnections;
            if (connections == null || !connections.Any())
            {
                AnsiConsole.MarkupLine("[yellow]⚠[/] No data sources configured");
                return;
            }

            var table = new Table();
            table.Border = TableBorder.Rounded;
            table.Title = new TableTitle("[bold]Configured Data Sources[/]");
            table.AddColumn("#");
            table.AddColumn("Name");
            table.AddColumn("Category");
            table.AddColumn("Database Type");

            int index = 1;
            foreach (var conn in connections)
            {
                table.AddRow(
                    index.ToString(),
                    $"[cyan]{conn.ConnectionName}[/]",
                    conn.Category.ToString(),
                    conn.DatabaseType.ToString()
                );
                index++;
            }

            AnsiConsole.Write(table);
        }

        /// <summary>
        /// Display progress with a progress bar
        /// </summary>
        public static void DisplayProgress(string description, Action action)
        {
            AnsiConsole.Progress()
                .Start(ctx =>
                {
                    var task = ctx.AddTask(description);
                    task.IsIndeterminate = true;
                    action();
                    task.Value = 100;
                    task.StopTask();
                });
        }

        /// <summary>
        /// Confirm an action with the user
        /// </summary>
        public static bool Confirm(string message, bool defaultValue = false)
        {
            return AnsiConsole.Confirm(message, defaultValue);
        }

        /// <summary>
        /// Select from a list of options
        /// </summary>
        public static T SelectFromList<T>(string title, IEnumerable<T> items) where T : notnull
        {
            return AnsiConsole.Prompt(
                new SelectionPrompt<T>()
                    .Title(title)
                    .PageSize(10)
                    .AddChoices(items)
            );
        }

        /// <summary>
        /// Multi-select from a list of options
        /// </summary>
        public static List<T> MultiSelectFromList<T>(string title, IEnumerable<T> items) where T : notnull
        {
            return AnsiConsole.Prompt(
                new MultiSelectionPrompt<T>()
                    .Title(title)
                    .PageSize(10)
                    .AddChoices(items)
            );
        }

        /// <summary>
        /// Display error message
        /// </summary>
        public static void DisplayError(string message)
        {
            AnsiConsole.MarkupLine($"[red]✗ Error:[/] {Markup.Escape(message)}");
        }

        /// <summary>
        /// Display success message
        /// </summary>
        public static void DisplaySuccess(string message)
        {
            AnsiConsole.MarkupLine($"[green]✓[/] {Markup.Escape(message)}");
        }

        /// <summary>
        /// Display warning message
        /// </summary>
        public static void DisplayWarning(string message)
        {
            AnsiConsole.MarkupLine($"[yellow]⚠[/] {Markup.Escape(message)}");
        }

        /// <summary>
        /// Display info message
        /// </summary>
        public static void DisplayInfo(string message)
        {
            AnsiConsole.MarkupLine($"[cyan]ℹ[/] {Markup.Escape(message)}");
        }

        /// <summary>
        /// Ensure output directory exists
        /// </summary>
        public static string EnsureOutputDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                path = Environment.CurrentDirectory;

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                DisplayInfo($"Created directory: {path}");
            }

            return path;
        }

        /// <summary>
        /// Format file size in human-readable format
        /// </summary>
        public static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        /// <summary>
        /// Create a panel with title and content
        /// </summary>
        public static Panel CreatePanel(string title, string content, BoxBorder? border = null)
        {
            return new Panel(new Markup(content))
            {
                Header = new PanelHeader(title),
                Border = border ?? BoxBorder.Rounded,
                Padding = new Padding(1, 0)
            };
        }

        /// <summary>
        /// Display a rule/separator
        /// </summary>
        public static void DisplayRule(string title = "")
        {
            if (string.IsNullOrWhiteSpace(title))
                AnsiConsole.Write(new Rule());
            else
                AnsiConsole.Write(new Rule($"[cyan]{title}[/]"));
        }

        /// <summary>
        /// Validate data source exists and open connection
        /// </summary>
        public static IDataSource? ValidateAndGetDataSource(IDMEEditor editor, string dataSourceName, bool openConnection = true)
        {
            var ds = editor.GetDataSource(dataSourceName);
            if (ds == null)
            {
                DisplayError($"Data source '{dataSourceName}' not found");
                return null;
            }

            if (openConnection)
            {
                if (ds.ConnectionStatus != System.Data.ConnectionState.Open)
                {
                    ds.Openconnection();
                }

                if (ds.ConnectionStatus != System.Data.ConnectionState.Open)
                {
                    DisplayError($"Failed to open connection to '{dataSourceName}'");
                    return null;
                }
            }

            return ds;
        }

        /// <summary>
        /// Validate entity exists
        /// </summary>
        public static EntityStructure? ValidateAndGetEntity(IDataSource dataSource, string entityName, bool refresh = true)
        {
            var entity = dataSource.GetEntityStructure(entityName, refresh);
            if (entity == null)
            {
                DisplayError($"Entity '{entityName}' not found in data source '{dataSource.DatasourceName}'");
                return null;
            }
            return entity;
        }

        /// <summary>
        /// Display a tree structure
        /// </summary>
        public static void DisplayTree(string rootName, Dictionary<string, List<string>> structure)
        {
            var tree = new Tree(rootName);
            
            foreach (var kvp in structure)
            {
                var node = tree.AddNode($"[cyan]{kvp.Key}[/]");
                foreach (var item in kvp.Value)
                {
                    node.AddNode($"[dim]{item}[/]");
                }
            }
            
            AnsiConsole.Write(tree);
        }

        /// <summary>
        /// Execute with status spinner
        /// </summary>
        public static T ExecuteWithStatus<T>(string statusMessage, Func<T> action)
        {
            T result = default!;
            AnsiConsole.Status()
                .Start(statusMessage, ctx =>
                {
                    result = action();
                });
            return result;
        }

        /// <summary>
        /// Execute with status spinner (async)
        /// </summary>
        public static async System.Threading.Tasks.Task<T> ExecuteWithStatusAsync<T>(string statusMessage, Func<System.Threading.Tasks.Task<T>> action)
        {
            T result = default!;
            await AnsiConsole.Status()
                .StartAsync(statusMessage, async ctx =>
                {
                    result = await action();
                });
            return result;
        }
    }
}

