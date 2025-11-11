using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using BeepShell.Infrastructure;
using Spectre.Console;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Shell.Infrastructure;

namespace TheTechIdea.Beep.Shell.Commands
{
    /// <summary>
    /// Shell-specific commands (not part of standard CLI)
    /// </summary>
    public static class ShellCommands
    {
        public static void ShowHelp(IEnumerable<IShellCommand>? loadedCommands = null)
        {
            var table = new Table();
            table.Border = TableBorder.Rounded;
            table.Title = new TableTitle("[bold cyan]BeepShell Commands[/]");
            table.AddColumn("[bold]Command[/]");
            table.AddColumn("[bold]Description[/]");
            table.AddColumn("[bold]Category[/]");

            // Add dynamically loaded commands
            if (loadedCommands != null && loadedCommands.Any())
            {
                var groupedCommands = loadedCommands
                    .GroupBy(cmd => cmd.Category ?? "Other")
                    .OrderBy(g => g.Key);

                foreach (var group in groupedCommands)
                {
                    foreach (var cmd in group.OrderBy(c => c.CommandName))
                    {
                        var aliases = cmd.Aliases != null && cmd.Aliases.Any() 
                            ? $", {string.Join(", ", cmd.Aliases)}" 
                            : "";
                        
                        var commandName = $"[cyan]{cmd.CommandName}{aliases}[/]";
                        var description = cmd.Description ?? "[dim]No description[/]";
                        var category = $"[yellow]{group.Key}[/]";

                        table.AddRow(commandName, description, category);
                    }
                }
            }

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();

            AnsiConsole.MarkupLine("[bold]Built-in Shell Commands:[/]");
            AnsiConsole.MarkupLine("  [cyan]help, ?[/]       - Show this help message");
            AnsiConsole.MarkupLine("  [cyan]clear, cls[/]    - Clear the screen");
            AnsiConsole.MarkupLine("  [cyan]exit, quit, q[/] - Exit the shell");
            AnsiConsole.MarkupLine("  [cyan]status[/]        - Show session status and statistics");
            AnsiConsole.MarkupLine("  [cyan]connections[/]   - Show all open connections");
            AnsiConsole.MarkupLine("  [cyan]datasources[/]   - Show all active data sources");
            AnsiConsole.MarkupLine("  [cyan]history[/]       - Show command history");
            AnsiConsole.MarkupLine("  [cyan]extensions[/]    - Show loaded shell extensions");
            AnsiConsole.MarkupLine("  [cyan]workflows[/]     - Show available workflows");
            AnsiConsole.MarkupLine("  [cyan]profile[/]       - Show current profile");
            AnsiConsole.MarkupLine("  [cyan]reload[/]        - Reload configuration from disk");
            AnsiConsole.WriteLine();

            AnsiConsole.MarkupLine("[bold]Usage:[/]");
            AnsiConsole.MarkupLine("  [dim]Type '[cyan]<command> --help[/]' for detailed help on any command[/]");
            AnsiConsole.MarkupLine("  [dim]Example: '[cyan]nuget --help[/]' or '[cyan]driver-nuget --help[/]'[/]");
            AnsiConsole.WriteLine();

            if (loadedCommands != null && loadedCommands.Any())
            {
                var commandCount = loadedCommands.Count();
                AnsiConsole.MarkupLine($"[green]✓[/] [dim]{commandCount} commands loaded[/]");
            }
        }

        public static void ShowStatus(IDMEEditor editor, SessionState state)
        {
            var panel = new Panel(new Markup("[bold cyan]Session Status[/]"))
            {
                Border = BoxBorder.Rounded,
                Padding = new Padding(1, 0)
            };
            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();

            var table = new Table();
            table.Border = TableBorder.None;
            table.HideHeaders();
            table.AddColumn(new TableColumn("").Width(20));
            table.AddColumn(new TableColumn(""));

            // Session info
            table.AddRow("[bold]Profile:[/]", state.ProfileName);
            table.AddRow("[bold]Session Time:[/]", FormatDuration(state.SessionDuration));
            table.AddRow("[bold]Start Time:[/]", state.StartTime.ToString("yyyy-MM-dd HH:mm:ss"));

            // Statistics
            table.AddRow("", "");
            table.AddRow("[bold cyan]Command Statistics[/]", "");
            table.AddRow("  Total Commands:", state.TotalCommands.ToString());
            table.AddRow("  Successful:", $"[green]{state.SuccessfulCommands}[/]");
            table.AddRow("  Failed:", state.FailedCommands > 0 ? $"[red]{state.FailedCommands}[/]" : "0");
            table.AddRow("  Execution Time:", FormatDuration(state.TotalExecutionTime));

            // Connection info
            table.AddRow("", "");
            table.AddRow("[bold cyan]Connections[/]", "");
            var openConnections = editor.DataSources.Count(ds => ds.ConnectionStatus == ConnectionState.Open);
            var totalDataSources = editor.DataSources.Count;
            table.AddRow("  Active Data Sources:", totalDataSources.ToString());
            table.AddRow("  Open Connections:", openConnections > 0 ? $"[green]{openConnections}[/]" : "0");
            table.AddRow("  Configured:", editor.ConfigEditor.DataConnections.Count.ToString());

            // Configuration
            table.AddRow("", "");
            table.AddRow("[bold cyan]Configuration[/]", "");
            table.AddRow("  Drivers Loaded:", editor.ConfigEditor.DataDriversClasses.Count.ToString());
            table.AddRow("  Assemblies:", editor.assemblyHandler.Assemblies.Count.ToString());

            AnsiConsole.Write(table);
        }

        public static void ShowConnections(IDMEEditor editor)
        {
            var openConnections = editor.DataSources
                .Where(ds => ds.ConnectionStatus == ConnectionState.Open)
                .ToList();

            if (!openConnections.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No open connections[/]");
                return;
            }

            var table = new Table();
            table.Border = TableBorder.Rounded;
            table.Title = new TableTitle($"[bold]Open Connections ({openConnections.Count})[/]");
            table.AddColumn("Name");
            table.AddColumn("Type");
            table.AddColumn("Status");
            table.AddColumn("Entities");

            foreach (var ds in openConnections)
            {
                var status = ds.ConnectionStatus == ConnectionState.Open 
                    ? "[green]Open[/]" 
                    : "[yellow]Closed[/]";

                table.AddRow(
                    $"[cyan]{ds.DatasourceName}[/]",
                    ds.DatasourceType.ToString(),
                    status,
                    ds.Entities?.Count.ToString() ?? "0"
                );
            }

            AnsiConsole.Write(table);
        }

        public static void ShowDataSources(IDMEEditor editor)
        {
            if (!editor.DataSources.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No active data sources[/]");
                AnsiConsole.MarkupLine("[dim]Use 'ds test <name>' to activate a connection[/]");
                return;
            }

            var table = new Table();
            table.Border = TableBorder.Rounded;
            table.Title = new TableTitle($"[bold]Active Data Sources ({editor.DataSources.Count})[/]");
            table.AddColumn("#");
            table.AddColumn("Name");
            table.AddColumn("Type");
            table.AddColumn("Status");
            table.AddColumn("Category");

            int index = 1;
            foreach (var ds in editor.DataSources)
            {
                var status = ds.ConnectionStatus == ConnectionState.Open
                    ? "[green]●[/]"
                    : "[dim]○[/]";

                table.AddRow(
                    index.ToString(),
                    $"[cyan]{ds.DatasourceName}[/]",
                    ds.DatasourceType.ToString(),
                    status,
                    ds.Category.ToString()
                );
                index++;
            }

            AnsiConsole.Write(table);
        }

        public static void ShowHistory(SessionState state)
        {
            if (!state.CommandHistory.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No command history[/]");
                return;
            }

            var table = new Table();
            table.Border = TableBorder.Rounded;
            table.Title = new TableTitle($"[bold]Command History (Last {state.CommandHistory.Count})[/]");
            table.AddColumn("#");
            table.AddColumn("Command");

            int index = 1;
            foreach (var cmd in state.CommandHistory)
            {
                table.AddRow(
                    $"[dim]{index}[/]",
                    Markup.Escape(cmd)
                );
                index++;
            }

            AnsiConsole.Write(table);
        }

        private static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalSeconds < 1)
                return $"{duration.TotalMilliseconds:F0} ms";
            if (duration.TotalMinutes < 1)
                return $"{duration.TotalSeconds:F1} sec";
            if (duration.TotalHours < 1)
                return $"{duration.TotalMinutes:F0} min {duration.Seconds} sec";
            return $"{duration.TotalHours:F1} hrs";
        }
    }
}
