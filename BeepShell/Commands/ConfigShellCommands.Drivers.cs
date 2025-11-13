using System;
using System.CommandLine;
using System.Linq;
using Spectre.Console;

namespace BeepShell.Commands
{
    /// <summary>
    /// Driver management commands for ConfigShellCommands
    /// Displays information about installed data drivers using ComponentConfigManager
    /// </summary>
    public partial class ConfigShellCommands
    {
        private void AddDriverCommands(Command parent)
        {
            var driverCommand = new Command("driver", "Manage data drivers");

            // List drivers
            var listCommand = new Command("list", "List all installed drivers");
            listCommand.SetHandler(ListDrivers);
            driverCommand.AddCommand(listCommand);

            // Show driver info
            var infoCommand = new Command("info", "Show detailed driver information");
            var packageArg = new Argument<string>("package", "Driver package name");
            infoCommand.AddArgument(packageArg);
            infoCommand.SetHandler(ShowDriverInfo, packageArg);
            driverCommand.AddCommand(infoCommand);

            parent.AddCommand(driverCommand);
        }

        private void ListDrivers()
        {
            try
            {
                var drivers = _editor.ConfigEditor.DataDriversClasses;
                
                if (drivers == null || drivers.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No drivers installed[/]");
                    return;
                }

                var table = new Table();
                table.Border = TableBorder.Rounded;
                table.AddColumn("[cyan]Package Name[/]");
                table.AddColumn("[cyan]Driver Class[/]");
                table.AddColumn("[cyan]Version[/]");
                table.AddColumn("[cyan]DLL[/]");
                table.AddColumn("[cyan]Category[/]");
                table.AddColumn("[cyan]Status[/]");

                foreach (var driver in drivers.OrderBy(d => d.PackageName))
                {
                    var status = driver.IsMissing ? "[red]Missing[/]" : "[green]Available[/]";
                    
                    table.AddRow(
                        driver.PackageName ?? "-",
                        driver.DriverClass ?? "-",
                        driver.version ?? "-",
                        driver.dllname ?? "-",
                        driver.DatasourceCategory.ToString() ?? "-",
                        status
                    );
                }

                AnsiConsole.Write(table);
                AnsiConsole.MarkupLine($"\n[dim]Total drivers: {drivers.Count}[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error listing drivers: {ex.Message}");
            }
        }

        private void ShowDriverInfo(string packageName)
        {
            try
            {
                var driver = _editor.ConfigEditor.DataDriversClasses
                    .FirstOrDefault(d => d.PackageName.Equals(packageName, StringComparison.OrdinalIgnoreCase));

                if (driver == null)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Driver '{packageName}' not found[/]");
                    return;
                }

                var panel = new Panel(new Markup(
                    $"[cyan]Package Name:[/] {driver.PackageName}\n" +
                    $"[cyan]Driver Class:[/] {driver.DriverClass ?? "N/A"}\n" +
                    $"[cyan]Version:[/] {driver.version ?? "N/A"}\n" +
                    $"[cyan]DLL Name:[/] {driver.dllname ?? "N/A"}\n" +
                    $"[cyan]Category:[/] {driver.DatasourceCategory.ToString() ?? "N/A"}\n" +
                    $"[cyan]Type:[/] {driver.DatasourceType.ToString() ?? "N/A"}\n" +
                    $"[cyan]ADO Type:[/] {(driver.ADOType ? "Yes" : "No")}\n" +
                    $"[cyan]Adapter Type:[/] {driver.AdapterType ?? "N/A"}\n" +
                    $"[cyan]Connection Type:[/] {driver.DbConnectionType ?? "N/A"}\n" +
                    $"[cyan]Extensions:[/] {driver.extensionstoHandle ?? "N/A"}\n" +
                    $"[cyan]Icon:[/] {driver.iconname ?? "N/A"}\n" +
                    $"[cyan]Create Local:[/] {(driver.CreateLocal ? "Yes" : "No")}\n" +
                    $"[cyan]In Memory:[/] {(driver.InMemory ? "Yes" : "No")}\n" +
                    $"[cyan]Favourite:[/] {(driver.Favourite ? "Yes" : "No")}\n" +
                    $"[cyan]Status:[/] {(driver.IsMissing ? "[red]Missing[/]" : "[green]Available[/]")}"
                ));
                panel.Header = new PanelHeader($"[green]Driver: {packageName}[/]");
                panel.Border = BoxBorder.Rounded;

                AnsiConsole.Write(panel);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error showing driver info: {ex.Message}");
            }
        }
    }
}
