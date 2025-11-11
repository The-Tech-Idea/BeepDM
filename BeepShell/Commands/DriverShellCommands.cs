using System;
using System.CommandLine;
using System.Linq;
using BeepShell.Infrastructure;
using Spectre.Console;
using TheTechIdea.Beep.Editor;

namespace BeepShell.Commands
{
    /// <summary>
    /// Driver management commands for BeepShell
    /// Uses persistent DMEEditor for driver operations
    /// </summary>
    public class DriverShellCommands : IShellCommand
    {
        private IDMEEditor _editor;

        public string CommandName => "driver";
        public string Description => "Manage database drivers";
        public string Category => "Configuration";
        public string Version => "1.0.0";
        public string Author => "BeepDM Team";
        public string[] Aliases => new[] { "drv", "drivers" };

        public void Initialize(IDMEEditor editor)
        {
            _editor = editor;
        }

        public Command BuildCommand()
        {
            var driverCommand = new Command("driver", Description);

            // driver list
            var listCommand = new Command("list", "List all available drivers");
            listCommand.SetHandler(() => ListDrivers());
            driverCommand.AddCommand(listCommand);

            // driver info
            var infoCommand = new Command("info", "Show driver information");
            var nameArg = new Argument<string>("name", "Driver name");
            infoCommand.AddArgument(nameArg);
            infoCommand.SetHandler((name) => ShowDriverInfo(name), nameArg);
            driverCommand.AddCommand(infoCommand);

            // driver test
            var testCommand = new Command("test", "Test driver availability");
            var testNameArg = new Argument<string>("name", "Driver name");
            testCommand.AddArgument(testNameArg);
            testCommand.SetHandler((name) => TestDriver(name), testNameArg);
            driverCommand.AddCommand(testCommand);

            return driverCommand;
        }

        private void ListDrivers()
        {
            try
            {
                var drivers = _editor.ConfigEditor.DataDriversClasses;

                if (drivers == null || drivers.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No drivers configured[/]");
                    return;
                }

                var table = new Table();
                table.Border = TableBorder.Rounded;
                table.AddColumn("[cyan]Driver Name[/]");
                table.AddColumn("[cyan]Class Name[/]");
                table.AddColumn("[cyan]Category[/]");
                table.AddColumn("[cyan]Version[/]");
                table.AddColumn("[cyan]Package[/]");

                foreach (var driver in drivers)
                {
                    table.AddRow(
                        driver.DriverClass ?? "-",
                        driver.classHandler ?? "-",
                        driver.DatasourceCategory.ToString(),
                        driver.version ?? "-",
                        driver.PackageName ?? "-"
                    );
                }

                AnsiConsole.Write(table);
                AnsiConsole.MarkupLine($"\n[dim]Total: {drivers.Count} drivers[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error listing drivers: {ex.Message}");
            }
        }

        private void ShowDriverInfo(string driverName)
        {
            try
            {
                var driver = _editor.ConfigEditor.DataDriversClasses
                    ?.FirstOrDefault(d => d.DriverClass.Equals(driverName, StringComparison.OrdinalIgnoreCase));

                if (driver == null)
                {
                    AnsiConsole.MarkupLine($"[yellow]Driver '{driverName}' not found[/]");
                    return;
                }

                var panel = new Panel(new Markup(
                    $"[cyan]Driver Name:[/] {driver.DriverClass}\n" +
                    $"[cyan]Class Handler:[/] {driver.classHandler}\n" +
                    $"[cyan]Category:[/] {driver.DatasourceCategory}\n" +
                    $"[cyan]Version:[/] {driver.version ?? "N/A"}\n" +
                    $"[cyan]Package:[/] {driver.PackageName ?? "N/A"}\n" +
                    $"[cyan]ADO Type:[/] {driver.ADOType}\n" +
                    $"[cyan]Extension:[/] {driver.extensionstoHandle ?? "N/A"}\n" +
                    $"[cyan]Create Local DB:[/] {driver.CreateLocal}"
                ));
                panel.Header = new PanelHeader($"[green]Driver: {driverName}[/]");
                panel.Border = BoxBorder.Rounded;

                AnsiConsole.Write(panel);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }

        private void TestDriver(string driverName)
        {
            try
            {
                var driver = _editor.ConfigEditor.DataDriversClasses
                    ?.FirstOrDefault(d => d.DriverClass.Equals(driverName, StringComparison.OrdinalIgnoreCase));

                if (driver == null)
                {
                    AnsiConsole.MarkupLine($"[yellow]Driver '{driverName}' not found[/]");
                    return;
                }

                AnsiConsole.Status()
                    .Start($"Testing driver '{driverName}'...", ctx =>
                    {
                        // Check if driver class is loaded in assemblies
                        var loadedAssemblies = _editor.ConfigEditor.LoadedAssemblies;
                        var driverFound = false;

                        foreach (var assembly in loadedAssemblies)
                        {
                            try
                            {
                                var type = assembly.GetType(driver.classHandler);
                                if (type != null)
                                {
                                    driverFound = true;
                                    break;
                                }
                            }
                            catch { }
                        }

                        if (driverFound)
                        {
                            AnsiConsole.MarkupLine($"[green]✓[/] Driver '{driverName}' is available");
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[yellow]![/] Driver '{driverName}' configured but class not found in loaded assemblies");
                        }
                    });
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
                "driver list",
                "driver info SqlServer",
                "driver test PostgreSQL"
            };
        }
    }
}
