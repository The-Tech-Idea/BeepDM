using System;
using System.Data;
using System.Linq;
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
        public static void ShowHelp()
        {
            var table = new Table();
            table.Border = TableBorder.Rounded;
            table.Title = new TableTitle("[bold cyan]BeepShell Commands[/]");
            table.AddColumn("[bold]Command[/]");
            table.AddColumn("[bold]Description[/]");

            // Shell-specific commands
            table.AddRow("[cyan]help, ?[/]", "Show this help message");
            table.AddRow("[cyan]clear, cls[/]", "Clear the screen");
            table.AddRow("[cyan]exit, quit, q[/]", "Exit the shell");
            table.AddRow("[cyan]status[/]", "Show session status and statistics");
            table.AddRow("[cyan]connections[/]", "Show all open connections");
            table.AddRow("[cyan]datasources[/]", "Show all active data sources");
            table.AddRow("[cyan]history[/]", "Show command history");
            table.AddRow("[cyan]extensions[/]", "Show loaded shell extensions");
            table.AddRow("[cyan]workflows[/]", "Show available workflows");
            table.AddRow("[cyan]plugin <cmd>[/]", "Plugin management (list, load, unload, reload, health)");
            table.AddRow("[cyan]events[/]", "Show event bus subscribers");
            table.AddRow("[cyan]alias [name cmd][/]", "Show or create command aliases");
            table.AddRow("[cyan]profile[/]", "Show current profile");
            table.AddRow("[cyan]profile switch <name>[/]", "Switch to different profile");
            table.AddRow("[cyan]reload[/]", "Reload configuration from disk");
            table.AddRow("[cyan]close <datasource>[/]", "Close a specific data source connection");
            
            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();

            AnsiConsole.MarkupLine("[bold]Assembly Management Commands:[/]");
            AnsiConsole.MarkupLine("  [cyan]assembly list [--verbose][/]        - List loaded assemblies");
            AnsiConsole.MarkupLine("  [cyan]assembly load <path>[/]             - Load assembly from path");
            AnsiConsole.MarkupLine("  [cyan]assembly unload <name>[/]           - Unload assembly");
            AnsiConsole.MarkupLine("  [cyan]assembly scan [--all][/]            - Scan assemblies for types");
            AnsiConsole.MarkupLine("  [cyan]assembly types [--assembly][/]      - List types from assemblies");
            AnsiConsole.MarkupLine("  [cyan]assembly drivers[/]                 - List data drivers");
            AnsiConsole.MarkupLine("  [cyan]assembly extensions[/]              - List loader extensions");
            AnsiConsole.MarkupLine("  [cyan]assembly create <typename>[/]       - Create instance from type");
            AnsiConsole.MarkupLine("  [cyan]assembly nugget load <path>[/]      - Load NuGet package");
            AnsiConsole.MarkupLine("  [dim]Alias: 'asm' for 'assembly'[/]");
            AnsiConsole.WriteLine();

            AnsiConsole.MarkupLine("[bold]Standard Commands:[/]");
            AnsiConsole.MarkupLine("  [dim]All CLI commands are available (config, ds, etl, class, etc.)[/]");
            AnsiConsole.MarkupLine("  [dim]Type 'config --help' or 'ds --help' for specific command help[/]");
            AnsiConsole.WriteLine();

            AnsiConsole.MarkupLine("[bold]Extension Commands:[/]");
            AnsiConsole.MarkupLine("  [dim]Custom commands loaded from extensions are also available[/]");
            AnsiConsole.MarkupLine("  [dim]Type 'extensions' to see loaded extensions[/]");
            AnsiConsole.WriteLine();

            AnsiConsole.MarkupLine("[bold]Plugin Management:[/]");
            AnsiConsole.MarkupLine("  [cyan]plugin list[/]                        - Show loaded plugins");
            AnsiConsole.MarkupLine("  [cyan]plugin load <path>[/]                 - Load a plugin (hot-reload)");
            AnsiConsole.MarkupLine("  [cyan]plugin unload <id>[/]                 - Unload a plugin");
            AnsiConsole.MarkupLine("  [cyan]plugin reload <id>[/]                 - Reload a plugin");
            AnsiConsole.MarkupLine("  [cyan]plugin health [id][/]                 - Check plugin health");
            AnsiConsole.WriteLine();

            AnsiConsole.MarkupLine("[bold]Examples:[/]");
            AnsiConsole.MarkupLine("  [cyan]asm list --verbose[/]                 - List all assemblies with details");
            AnsiConsole.MarkupLine("  [cyan]asm load MyExtension.dll[/]           - Load an assembly");
            AnsiConsole.MarkupLine("  [cyan]asm types --interface IDataSource[/]  - Find all IDataSource types");
            AnsiConsole.MarkupLine("  [cyan]asm drivers[/]                        - List all data drivers");
            AnsiConsole.MarkupLine("  [cyan]config connection list[/]             - List all connections");
            AnsiConsole.MarkupLine("  [cyan]ds test MyDatabase[/]                 - Test a connection");
            AnsiConsole.MarkupLine("  [cyan]class generate-poco MyDB Users[/]     - Generate POCO class");
            AnsiConsole.MarkupLine("  [cyan]status[/]                             - Show session statistics");
            AnsiConsole.MarkupLine("  [cyan]alias ls datasources[/]               - Create 'ls' alias");
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
