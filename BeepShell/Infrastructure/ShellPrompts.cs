using Spectre.Console;
using TheTechIdea.Beep.Editor;

namespace BeepShell.Infrastructure
{
    /// <summary>
    /// Interactive prompt utilities for shell extensions
    /// </summary>
    public static class ShellPrompts
    {
        /// <summary>
        /// Prompt for text input with validation
        /// </summary>
        public static string PromptText(string prompt, string defaultValue = null, bool allowEmpty = false)
        {
            var textPrompt = new TextPrompt<string>($"[cyan]{prompt}[/]")
                .PromptStyle("green");

            if (!string.IsNullOrEmpty(defaultValue))
            {
                textPrompt.DefaultValue(defaultValue);
            }

            if (allowEmpty)
            {
                textPrompt.AllowEmpty();
            }

            return AnsiConsole.Prompt(textPrompt);
        }

        /// <summary>
        /// Prompt for password/secret input
        /// </summary>
        public static string PromptSecret(string prompt)
        {
            return AnsiConsole.Prompt(
                new TextPrompt<string>($"[cyan]{prompt}[/]")
                    .PromptStyle("green")
                    .Secret()
            );
        }

        /// <summary>
        /// Prompt for yes/no confirmation
        /// </summary>
        public static bool PromptConfirm(string prompt, bool defaultValue = false)
        {
            return AnsiConsole.Confirm($"[yellow]{prompt}[/]", defaultValue);
        }

        /// <summary>
        /// Prompt for selection from list
        /// </summary>
        public static T PromptSelection<T>(string prompt, IEnumerable<T> choices, Func<T, string> displaySelector = null)
        {
            displaySelector ??= (item => item?.ToString());

            return AnsiConsole.Prompt(
                new SelectionPrompt<T>()
                    .Title($"[cyan]{prompt}[/]")
                    .AddChoices(choices)
                    .UseConverter(displaySelector)
            );
        }

        /// <summary>
        /// Prompt for multiple selections
        /// </summary>
        public static List<T> PromptMultiSelection<T>(string prompt, IEnumerable<T> choices, Func<T, string> displaySelector = null)
        {
            displaySelector ??= (item => item?.ToString());

            return AnsiConsole.Prompt(
                new MultiSelectionPrompt<T>()
                    .Title($"[cyan]{prompt}[/]")
                    .AddChoices(choices)
                    .UseConverter(displaySelector)
            );
        }

        /// <summary>
        /// Prompt for data source selection
        /// </summary>
        public static string PromptDataSource(IDMEEditor editor, string prompt = "Select a data source")
        {
            var dataSources = editor.ConfigEditor.DataConnections
                .Select(c => c.ConnectionName)
                .ToList();

            if (!dataSources.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No data sources configured[/]");
                return null;
            }

            return PromptSelection(prompt, dataSources);
        }

        /// <summary>
        /// Prompt for entity/table selection from a data source
        /// </summary>
        public static string PromptEntity(IDMEEditor editor, string dataSourceName, string prompt = "Select a table/entity")
        {
            var ds = editor.GetDataSource(dataSourceName);
            if (ds == null)
            {
                AnsiConsole.MarkupLine($"[red]Data source '{dataSourceName}' not found[/]");
                return null;
            }

            if (ds.Entities == null || !ds.Entities.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No entities found in data source[/]");
                return null;
            }

            return PromptSelection(prompt, ds.Entities.Select(e => e.EntityName));
        }

        /// <summary>
        /// Prompt for file path with validation
        /// </summary>
        public static string PromptFilePath(string prompt, bool mustExist = false, string defaultPath = null)
        {
            var textPrompt = new TextPrompt<string>($"[cyan]{prompt}[/]")
                .PromptStyle("green");

            if (!string.IsNullOrEmpty(defaultPath))
            {
                textPrompt.DefaultValue(defaultPath);
            }

            if (mustExist)
            {
                textPrompt.Validate(path =>
                {
                    if (File.Exists(path))
                        return ValidationResult.Success();
                    return ValidationResult.Error($"[red]File not found: {path}[/]");
                });
            }

            return AnsiConsole.Prompt(textPrompt);
        }

        /// <summary>
        /// Prompt for directory path with validation
        /// </summary>
        public static string PromptDirectoryPath(string prompt, bool mustExist = false, string defaultPath = null)
        {
            var textPrompt = new TextPrompt<string>($"[cyan]{prompt}[/]")
                .PromptStyle("green");

            if (!string.IsNullOrEmpty(defaultPath))
            {
                textPrompt.DefaultValue(defaultPath);
            }

            if (mustExist)
            {
                textPrompt.Validate(path =>
                {
                    if (Directory.Exists(path))
                        return ValidationResult.Success();
                    return ValidationResult.Error($"[red]Directory not found: {path}[/]");
                });
            }

            return AnsiConsole.Prompt(textPrompt);
        }

        /// <summary>
        /// Display progress bar while executing action
        /// </summary>
        public static T WithProgress<T>(string description, Func<ProgressContext, T> action)
        {
            return AnsiConsole.Progress()
                .Columns(
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new SpinnerColumn()
                )
                .Start(ctx => action(ctx));
        }

        /// <summary>
        /// Display progress bar for async action
        /// </summary>
        public static async Task<T> WithProgressAsync<T>(string description, Func<ProgressContext, Task<T>> action)
        {
            return await AnsiConsole.Progress()
                .Columns(
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new SpinnerColumn()
                )
                .StartAsync(async ctx => await action(ctx));
        }

        /// <summary>
        /// Display status spinner while executing action
        /// </summary>
        public static T WithStatus<T>(string status, Func<StatusContext, T> action)
        {
            return AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .Start(status, action);
        }

        /// <summary>
        /// Display status spinner for async action
        /// </summary>
        public static async Task<T> WithStatusAsync<T>(string status, Func<StatusContext, Task<T>> action)
        {
            return await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync(status, action);
        }

        /// <summary>
        /// Display a table of data
        /// </summary>
        public static void DisplayTable(string title, IEnumerable<string> columns, IEnumerable<IEnumerable<string>> rows)
        {
            var table = new Table()
                .Border(TableBorder.Rounded)
                .Title($"[bold]{title}[/]");

            foreach (var column in columns)
            {
                table.AddColumn($"[cyan]{column}[/]");
            }

            foreach (var row in rows)
            {
                table.AddRow(row.ToArray());
            }

            AnsiConsole.Write(table);
        }

        /// <summary>
        /// Display a panel with content
        /// </summary>
        public static void DisplayPanel(string title, string content, Color? borderColor = null)
        {
            var panel = new Panel(new Markup(content))
            {
                Header = new PanelHeader($"[bold]{title}[/]"),
                Border = BoxBorder.Rounded,
                Padding = new Padding(1, 0)
            };

            if (borderColor.HasValue)
            {
                panel.BorderStyle(new Style(borderColor.Value));
            }

            AnsiConsole.Write(panel);
        }

        /// <summary>
        /// Display a rule separator
        /// </summary>
        public static void DisplayRule(string title = null)
        {
            if (string.IsNullOrEmpty(title))
            {
                AnsiConsole.Write(new Rule());
            }
            else
            {
                AnsiConsole.Write(new Rule($"[cyan]{title}[/]"));
            }
        }
    }
}
