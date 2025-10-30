using System;
using System.CommandLine;
using System.Linq;
using Spectre.Console;
using TheTechIdea.Beep.CLI.Infrastructure;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Helpers.ConnectionHelpers;
using TheTechIdea.Beep.DriversConfigurations;
using System.Collections.Generic;
using static TheTechIdea.Beep.CLI.Infrastructure.CliHelper;

namespace TheTechIdea.Beep.CLI.Commands
{
    /// <summary>
    /// Driver management commands
    /// </summary>
    public static class DriverCommands
    {
        public static Command Build()
        {
            var driverCommand = new Command("driver", "Driver management");

            var profileOption = new Option<string>("--profile", () => ProfileManager.DEFAULT_PROFILE, "Profile to use");

            // driver list
            var listCommand = new Command("list", "List all installed drivers");
            var categoryOption = new Option<string?>("--category", "Filter by category (RDBMS, FILE, NOSQL, WEBAPI, CLOUD, etc.)");
            var typeOption = new Option<string?>("--type", "Filter by DataSourceType");
            var detailsOption = new Option<bool>("--details", () => false, "Show detailed information");
            
            listCommand.AddOption(profileOption);
            listCommand.AddOption(categoryOption);
            listCommand.AddOption(typeOption);
            listCommand.AddOption(detailsOption);
            
            listCommand.SetHandler((string profile, string? category, string? type, bool details) =>
            {
                var services = new BeepServiceProvider(profile);
                var editor = services.GetEditor();
                
                var drivers = editor.ConfigEditor.DataDriversClasses.AsEnumerable();
                
                // Filter by category
                if (!string.IsNullOrEmpty(category))
                {
                    if (Enum.TryParse<DatasourceCategory>(category, true, out var cat))
                    {
                        drivers = ConnectionDriverLinkingHelper.GetDriversForCategory(cat, editor.ConfigEditor);
                        AnsiConsole.MarkupLine($"[cyan]Filtering by category:[/] {cat}");
                    }
                    else
                    {
                        DisplayError($"Invalid category: {category}");
                        return;
                    }
                }
                
                // Filter by DataSourceType
                if (!string.IsNullOrEmpty(type))
                {
                    if (Enum.TryParse<DataSourceType>(type, true, out var dsType))
                    {
                        drivers = ConnectionDriverLinkingHelper.GetDriversForDataSourceType(dsType, editor.ConfigEditor);
                        AnsiConsole.MarkupLine($"[cyan]Filtering by type:[/] {dsType}");
                    }
                    else
                    {
                        DisplayError($"Invalid type: {type}");
                        return;
                    }
                }
                
                var driversList = drivers.ToList();
                
                if (!driversList.Any())
                {
                    DisplayWarning("No drivers found matching the criteria");
                    return;
                }
                
                var table = new Table();
                table.Border = TableBorder.Rounded;
                table.Title = new TableTitle($"[bold cyan]Installed Drivers ({driversList.Count})[/]");
                
                if (details)
                {
                    table.AddColumn("Package Name");
                    table.AddColumn("Driver Class");
                    table.AddColumn("Version");
                    table.AddColumn("Category");
                    table.AddColumn("DB Type");
                    table.AddColumn("Extensions");
                    table.AddColumn("DLL");
                    table.AddColumn("ADO");
                    table.AddColumn("Status");
                    
                    foreach (var driver in driversList)
                    {
                        string status = driver.IsMissing ? "[red]Missing[/]" : "[green]OK[/]";
                        table.AddRow(
                            driver.PackageName ?? "N/A",
                            driver.DriverClass ?? "N/A",
                            driver.version ?? "N/A",
                            driver.DatasourceCategory.ToString(),
                            driver.DatasourceType.ToString(),
                            driver.extensionstoHandle ?? "N/A",
                            driver.dllname ?? "N/A",
                            driver.ADOType ? "[green]Yes[/]" : "No",
                            status
                        );
                    }
                }
                else
                {
                    table.AddColumn("Package Name");
                    table.AddColumn("Version");
                    table.AddColumn("Category");
                    table.AddColumn("DB Type");
                    table.AddColumn("Extensions");
                    
                    foreach (var driver in driversList)
                    {
                        table.AddRow(
                            driver.PackageName ?? "N/A",
                            driver.version ?? "N/A",
                            driver.DatasourceCategory.ToString(),
                            driver.DatasourceType.ToString(),
                            driver.extensionstoHandle ?? "N/A"
                        );
                    }
                }
                
                AnsiConsole.Write(table);
            }, profileOption, categoryOption, typeOption, detailsOption);

            // driver scan
            var scanCommand = new Command("scan", "Scan and register drivers from a path");
            var pathArg = new Argument<string>("path", "Path to scan for drivers");
            scanCommand.AddArgument(pathArg);
            scanCommand.AddOption(profileOption);
            scanCommand.SetHandler((string path, string profile) =>
            {
                var services = new BeepServiceProvider(profile);
                var editor = services.GetEditor();
                
                try
                {
                    AnsiConsole.Status()
                        .Start("Scanning for drivers...", ctx =>
                        {
                            editor.assemblyHandler.LoadAssembly(path, FolderFileTypes.ConnectionDriver);
                            editor.ConfigEditor.SaveConnectionDriversConfigValues();
                        });
                    
                    DisplaySuccess($"Drivers scanned and registered from '{path}'");
                }
                catch (Exception ex)
                {
                    DisplayError($"Failed to scan drivers: {ex.Message}");
                }
            }, pathArg, profileOption);

            // driver info
            var infoCommand = new Command("info", "Show driver details");
            var nameArg = new Argument<string>("name", "Driver class name");
            infoCommand.AddArgument(nameArg);
            infoCommand.AddOption(profileOption);
            infoCommand.SetHandler((string name, string profile) =>
            {
                var services = new BeepServiceProvider(profile);
                var editor = services.GetEditor();
                
                var driver = editor.ConfigEditor.DataDriversClasses
                    .FirstOrDefault(d => d.DriverClass == name || d.PackageName == name);
                
                if (driver == null)
                {
                    DisplayError($"Driver '{name}' not found");
                    return;
                }
                
                var table = new Table();
                table.AddColumn("Property");
                table.AddColumn("Value");
                
                table.AddRow("Package Name", driver.PackageName ?? "N/A");
                table.AddRow("Driver Class", driver.DriverClass ?? "N/A");
                table.AddRow("Class Handler", driver.classHandler ?? "N/A");
                table.AddRow("DLL Name", driver.dllname ?? "N/A");
                table.AddRow("Database Type", driver.DatasourceType.ToString());
                table.AddRow("Category", driver.DatasourceCategory.ToString());
                table.AddRow("Version", driver.version ?? "N/A");
                table.AddRow("Create Local", driver.CreateLocal.ToString());
                table.AddRow("In Memory", driver.InMemory.ToString());
                table.AddRow("ADO Type", driver.ADOType.ToString());
                table.AddRow("Extensions", driver.extensionstoHandle ?? "N/A");
                table.AddRow("Connection String Template", driver.ConnectionString ?? "N/A");
                table.AddRow("Adapter Type", driver.AdapterType ?? "N/A");
                table.AddRow("GUID", driver.GuidID ?? "N/A");
                
                AnsiConsole.Write(table);
            }, nameArg, profileOption);

            driverCommand.AddCommand(listCommand);
            driverCommand.AddCommand(scanCommand);
            driverCommand.AddCommand(infoCommand);
            
            // driver validate
            var validateCommand = new Command("validate", "Validate driver compatibility with a connection");
            var connNameArg = new Argument<string>("connection", "Connection name to validate");
            validateCommand.AddArgument(connNameArg);
            validateCommand.AddOption(profileOption);
            validateCommand.SetHandler((string connectionName, string profile) =>
            {
                var services = new BeepServiceProvider(profile);
                var editor = services.GetEditor();
                
                var connection = editor.ConfigEditor.DataConnections
                    .FirstOrDefault(c => c.ConnectionName.Equals(connectionName, StringComparison.InvariantCultureIgnoreCase));
                
                if (connection == null)
                {
                    DisplayError($"Connection '{connectionName}' not found");
                    return;
                }
                
                AnsiConsole.MarkupLine($"[cyan]Validating connection:[/] {connectionName}");
                AnsiConsole.MarkupLine($"[cyan]Type:[/] {connection.DatabaseType}");
                AnsiConsole.MarkupLine($"[cyan]Category:[/] {connection.Category}");
                AnsiConsole.WriteLine();
                
                // Find compatible drivers
                var compatibleDrivers = editor.ConfigEditor.DataDriversClasses
                    .Where(d => ConnectionDriverLinkingHelper.IsDriverCompatible(d, connection))
                    .ToList();
                
                if (!compatibleDrivers.Any())
                {
                    DisplayError("No compatible drivers found!");
                    DisplayWarning($"Suggestion: Install a driver for {connection.DatabaseType} ({connection.Category})");
                    return;
                }
                
                AnsiConsole.MarkupLine($"[green]✓[/] Found {compatibleDrivers.Count} compatible driver(s)");
                AnsiConsole.WriteLine();
                
                // Find best matching driver
                var bestDriver = ConnectionDriverLinkingHelper.GetBestMatchingDriver(connection, editor.ConfigEditor);
                
                if (bestDriver != null)
                {
                    AnsiConsole.MarkupLine("[bold cyan]Best Match:[/]");
                    var matchTable = new Table();
                    matchTable.Border = TableBorder.Rounded;
                    matchTable.AddColumn("Property");
                    matchTable.AddColumn("Value");
                    
                    matchTable.AddRow("Package Name", bestDriver.PackageName ?? "N/A");
                    matchTable.AddRow("Driver Class", bestDriver.DriverClass ?? "N/A");
                    matchTable.AddRow("Version", bestDriver.version ?? "N/A");
                    matchTable.AddRow("Category", bestDriver.DatasourceCategory.ToString());
                    matchTable.AddRow("DB Type", bestDriver.DatasourceType.ToString());
                    matchTable.AddRow("DLL", bestDriver.dllname ?? "N/A");
                    matchTable.AddRow("Status", bestDriver.IsMissing ? "[red]Missing[/]" : "[green]Available[/]");
                    
                    AnsiConsole.Write(matchTable);
                    
                    if (bestDriver.IsMissing)
                    {
                        AnsiConsole.MarkupLine("[yellow]Warning:[/] Driver files are missing!");
                    }
                }
                
                if (compatibleDrivers.Count > 1)
                {
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine("[cyan]Other Compatible Drivers:[/]");
                    var otherTable = new Table();
                    otherTable.Border = TableBorder.Rounded;
                    otherTable.AddColumn("Package Name");
                    otherTable.AddColumn("Version");
                    otherTable.AddColumn("Status");
                    
                    foreach (var driver in compatibleDrivers.Where(d => d != bestDriver))
                    {
                        otherTable.AddRow(
                            driver.PackageName ?? "N/A",
                            driver.version ?? "N/A",
                            driver.IsMissing ? "[red]Missing[/]" : "[green]Available[/]"
                        );
                    }
                    
                    AnsiConsole.Write(otherTable);
                }
            }, connNameArg, profileOption);
            
            driverCommand.AddCommand(validateCommand);
            
            // driver for-extension
            var forExtCommand = new Command("for-extension", "Find drivers for a file extension");
            var extArg = new Argument<string>("extension", "File extension (e.g., csv, json, xlsx)");
            forExtCommand.AddArgument(extArg);
            forExtCommand.AddOption(profileOption);
            forExtCommand.SetHandler((string extension, string profile) =>
            {
                var services = new BeepServiceProvider(profile);
                var editor = services.GetEditor();
                
                var drivers = ConnectionDriverLinkingHelper.GetDriversForFileExtension(extension, editor.ConfigEditor);
                
                if (!drivers.Any())
                {
                    DisplayWarning($"No drivers found for extension: .{extension}");
                    return;
                }
                
                AnsiConsole.MarkupLine($"[green]✓[/] Found {drivers.Count} driver(s) for extension: [cyan].{extension}[/]");
                AnsiConsole.WriteLine();
                
                var table = new Table();
                table.Border = TableBorder.Rounded;
                table.AddColumn("Package Name");
                table.AddColumn("Version");
                table.AddColumn("DB Type");
                table.AddColumn("Supported Extensions");
                table.AddColumn("Status");
                
                foreach (var driver in drivers)
                {
                    table.AddRow(
                        driver.PackageName ?? "N/A",
                        driver.version ?? "N/A",
                        driver.DatasourceType.ToString(),
                        driver.extensionstoHandle ?? "N/A",
                        driver.IsMissing ? "[red]Missing[/]" : "[green]Available[/]"
                    );
                }
                
                AnsiConsole.Write(table);
            }, extArg, profileOption);
            
            driverCommand.AddCommand(forExtCommand);

            return driverCommand;
        }
    }
}
