using System;
using System.CommandLine;
using System.Linq;
using BeepShell.Infrastructure;
using Spectre.Console;
using TheTechIdea.Beep.Editor;

namespace BeepShell.Commands
{
    /// <summary>
    /// Mapping management commands for BeepShell
    /// Uses persistent DMEEditor for mapping configurations
    /// </summary>
    public class MappingShellCommands : IShellCommand
    {
        private IDMEEditor _editor;

        public string CommandName => "mapping";
        public string Description => "Manage data mappings";
        public string Category => "Configuration";
        public string Version => "1.0.0";
        public string Author => "BeepDM Team";
        public string[] Aliases => new[] { "map", "mappings" };

        public void Initialize(IDMEEditor editor)
        {
            _editor = editor;
        }

        public Command BuildCommand()
        {
            var mappingCommand = new Command("mapping", Description);

            // mapping list
            var listCommand = new Command("list", "List all mappings");
            listCommand.SetHandler(() => ListMappings());
            mappingCommand.AddCommand(listCommand);

            // mapping create
            var createCommand = new Command("create", "Create a new mapping");
            var nameOption = new Option<string>("--name", "Mapping name") { IsRequired = true };
            var sourceOption = new Option<string>("--source", "Source entity") { IsRequired = true };
            var destOption = new Option<string>("--dest", "Destination entity") { IsRequired = true };

            createCommand.AddOption(nameOption);
            createCommand.AddOption(sourceOption);
            createCommand.AddOption(destOption);

            createCommand.SetHandler((name, source, dest) =>
            {
                CreateMapping(name, source, dest);
            }, nameOption, sourceOption, destOption);

            mappingCommand.AddCommand(createCommand);

            // mapping show
            var showCommand = new Command("show", "Show mapping details");
            var showNameArg = new Argument<string>("name", "Mapping name");
            showCommand.AddArgument(showNameArg);
            showCommand.SetHandler((name) => ShowMapping(name), showNameArg);
            mappingCommand.AddCommand(showCommand);

            // mapping delete
            var deleteCommand = new Command("delete", "Delete a mapping");
            var deleteNameArg = new Argument<string>("name", "Mapping name");
            deleteCommand.AddArgument(deleteNameArg);
            deleteCommand.SetHandler((name) => DeleteMapping(name), deleteNameArg);
            mappingCommand.AddCommand(deleteCommand);

            return mappingCommand;
        }

        private void ListMappings()
        {
            try
            {
                AnsiConsole.MarkupLine("[yellow]Listing all mappings is not currently supported via shell.[/]");
                AnsiConsole.MarkupLine("[dim]Mappings are stored per entity. Use 'mapping show <datasource> <entity>' to view specific mappings.[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error listing mappings: {ex.Message}");
            }
        }

        private void CreateMapping(string name, string source, string dest)
        {
            try
            {
                AnsiConsole.MarkupLine($"[yellow]Creating mapping '{name}'...[/]");
                AnsiConsole.MarkupLine($"[cyan]Source:[/] {source}");
                AnsiConsole.MarkupLine($"[cyan]Destination:[/] {dest}");
                AnsiConsole.MarkupLine("[dim]Use mapping editor or config files to configure field mappings[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }

        private void ShowMapping(string name)
        {
            try
            {
                AnsiConsole.MarkupLine("[yellow]Showing specific mappings by name is not currently supported via shell.[/]");
                AnsiConsole.MarkupLine($"[dim]To view a mapping, provide datasource and entity: 'mapping show <datasource> <entity>'[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }

        private void DeleteMapping(string name)
        {
            try
            {
                AnsiConsole.MarkupLine("[yellow]Deleting mappings by name is not currently supported via shell.[/]");
                AnsiConsole.MarkupLine($"[dim]Mappings are managed through configuration files.[/]");
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
                "mapping list",
                "mapping create --name UserSync --source db1.Users --dest db2.Users",
                "mapping show UserSync",
                "mapping delete OldMapping"
            };
        }
    }
}
