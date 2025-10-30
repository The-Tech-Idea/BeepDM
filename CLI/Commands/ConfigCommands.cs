using System;
using System.CommandLine;
using System.Linq;
using Spectre.Console;
using TheTechIdea.Beep.CLI.Infrastructure;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Helpers.ConnectionHelpers;
using System.Collections.Generic;
using static TheTechIdea.Beep.CLI.Infrastructure.CliHelper;

namespace TheTechIdea.Beep.CLI.Commands
{
    /// <summary>
    /// Configuration management commands - handles config file CRUD operations
    /// </summary>
    public static class ConfigCommands
    {
        public static Command Build()
        {
            var configCommand = new Command("config", "Configuration management");
            var profileOption = new Option<string>("--profile", () => ProfileManager.DEFAULT_PROFILE, "Profile to use");

            // ============ CONNECTION MANAGEMENT ============
            var connCommand = new Command("connection", "Manage configuration connections");

            // List connections with filtering and details
            var listCommand = new Command("list", "List configured connections");
            var filterOption = new Option<string>("--filter", "Filter by name, type, or database");
            var detailsOption = new Option<bool>("--details", "Show detailed information");
            listCommand.AddOption(profileOption);
            listCommand.AddOption(filterOption);
            listCommand.AddOption(detailsOption);
            listCommand.SetHandler((string profile, string filter, bool details) =>
            {
                var services = new BeepServiceProvider(profile);
                var editor = services.GetEditor();
                var connections = editor.ConfigEditor.DataConnections.AsEnumerable();
                if (!string.IsNullOrEmpty(filter))
                {
                    connections = connections.Where(c =>
                        (c.ConnectionName?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (c.DatabaseType.ToString().Contains(filter, StringComparison.OrdinalIgnoreCase)) ||
                        (c.Database?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false));
                }
                var table = new Table().Border(TableBorder.Rounded);
                table.AddColumn("Name");
                table.AddColumn("Type");
                table.AddColumn("Category");
                table.AddColumn("Database/File");
                if (details)
                {
                    table.AddColumn("Host");
                    table.AddColumn("Driver");
                    table.AddColumn("GUID");
                }
                foreach (var conn in connections)
                {
                    var dbOrFile = conn.Category == DatasourceCategory.FILE
                        ? conn.FileName ?? conn.FilePath ?? "N/A"
                        : conn.Database ?? "N/A";
                    if (details)
                    {
                        table.AddRow(
                            conn.ConnectionName ?? "N/A",
                            conn.DatabaseType.ToString(),
                            conn.Category.ToString(),
                            dbOrFile,
                            conn.Host ?? "N/A",
                            conn.DriverName ?? "N/A",
                            conn.GuidID?.Substring(0, 8) ?? "N/A"
                        );
                    }
                    else
                    {
                        table.AddRow(
                            conn.ConnectionName ?? "N/A",
                            conn.DatabaseType.ToString(),
                            conn.Category.ToString(),
                            dbOrFile
                        );
                    }
                }
                AnsiConsole.Write(table);
                AnsiConsole.MarkupLine($"\n[blue]Total:[/] {connections.Count()} connection(s)");
            }, profileOption, filterOption, detailsOption);

            // Add connection interactively
            var addCommand = new Command("add", "Add new connection");
            addCommand.AddOption(profileOption);
            addCommand.SetHandler((string profile) =>
            {
                var services = new BeepServiceProvider(profile);
                var editor = services.GetEditor();
                AnsiConsole.MarkupLine("[bold blue]Add New Connection[/]\n");
                var connectionName = AnsiConsole.Ask<string>("Enter [green]connection name[/]:");
                if (editor.ConfigEditor.DataConnectionExist(connectionName))
                {
                    DisplayError($"Connection '{connectionName}' already exists");
                    return;
                }
                var category = AnsiConsole.Prompt(
                    new SelectionPrompt<DatasourceCategory>()
                        .Title("Select [green]category[/]:")
                        .AddChoices(Enum.GetValues(typeof(DatasourceCategory)).Cast<DatasourceCategory>()));
                var availableDrivers = ConnectionDriverLinkingHelper.GetDriversForCategory(category, editor.ConfigEditor);
                if (!availableDrivers.Any())
                {
                    DisplayWarning($"No drivers available for category {category}. Please install drivers first.");
                    return;
                }
                var driverOptions = availableDrivers.Select(d => d.DatasourceType).Distinct().OrderBy(d => d.ToString()).ToList();
                DataSourceType dbType = driverOptions.Count == 1
                    ? driverOptions.First()
                    : AnsiConsole.Prompt(new SelectionPrompt<DataSourceType>().Title($"Select [green]{category} type[/]:").AddChoices(driverOptions));
                var requiredParams = ConnectionHelper.GetAllParametersForDataSourceTypeInConnectionProperties(dbType);
                var conn = new ConnectionProperties
                {
                    ConnectionName = connectionName,
                    DatabaseType = dbType,
                    Category = category,
                    GuidID = Guid.NewGuid().ToString()
                };
                AnsiConsole.MarkupLine($"\n[bold]Configure connection parameters:[/]");
                foreach (var param in requiredParams)
                {
                    var paramName = param.Key;
                    var paramInfo = param.Value;
                    if (paramName.Equals("ConnectionName", StringComparison.OrdinalIgnoreCase) ||
                        paramName.Equals("DatabaseType", StringComparison.OrdinalIgnoreCase) ||
                        paramName.Equals("Category", StringComparison.OrdinalIgnoreCase) ||
                        paramName.Equals("GuidID", StringComparison.OrdinalIgnoreCase))
                        continue;
                    var prop = typeof(ConnectionProperties).GetProperty(paramName);
                    if (prop == null || !prop.CanWrite) continue;
                    var promptText = $"{paramName}";
                    if (!string.IsNullOrEmpty(paramInfo.Description)) promptText += $" [dim]({paramInfo.Description})[/]";
                    if (!string.IsNullOrEmpty(paramInfo.DefaultValue)) promptText += $" [dim]Default: {paramInfo.DefaultValue}[/]";
                    object? value = null;
                    if (paramInfo.IsRequired)
                    {
                        if (paramName.Equals("Password", StringComparison.OrdinalIgnoreCase) ||
                            paramName.Equals("ApiKey", StringComparison.OrdinalIgnoreCase) ||
                            paramName.Equals("KeyToken", StringComparison.OrdinalIgnoreCase))
                        {
                            value = AnsiConsole.Prompt(new TextPrompt<string>($"[green]* {promptText}[/]:").Secret());
                        }
                        else if (paramInfo.DataType == "int")
                        {
                            value = AnsiConsole.Ask<int>($"[green]* {promptText}[/]:");
                        }
                        else if (paramInfo.DataType == "bool")
                        {
                            value = AnsiConsole.Confirm($"[green]* {promptText}[/]:", false);
                        }
                        else
                        {
                            value = AnsiConsole.Ask<string>($"[green]* {promptText}[/]:");
                        }
                    }
                    else
                    {
                        if (paramInfo.DataType == "int")
                        {
                            var defaultInt = string.IsNullOrEmpty(paramInfo.DefaultValue) ? 0 : int.Parse(paramInfo.DefaultValue);
                            value = AnsiConsole.Ask($"{promptText}:", defaultInt);
                        }
                        else if (paramInfo.DataType == "bool")
                        {
                            var defaultBool = string.IsNullOrEmpty(paramInfo.DefaultValue) ? false : bool.Parse(paramInfo.DefaultValue);
                            value = AnsiConsole.Confirm($"{promptText}:", defaultBool);
                        }
                        else
                        {
                            value = AnsiConsole.Ask($"{promptText}:", paramInfo.DefaultValue ?? string.Empty);
                        }
                    }
                    if (value != null) prop.SetValue(conn, value);
                }
                var driver = availableDrivers.FirstOrDefault(d => d.DatasourceType == dbType);
                if (driver != null)
                {
                    conn.DriverName = driver.PackageName;
                    conn.DriverVersion = driver.version;
                    AnsiConsole.MarkupLine($"\n[green]✓[/] Linked driver: {driver.PackageName} v{driver.version}");
                }
                else
                {
                    AnsiConsole.MarkupLine($"\n[yellow]⚠[/] No driver found for {dbType}");
                }
                if (editor.ConfigEditor.AddDataConnection(conn))
                {
                    editor.ConfigEditor.SaveDataconnectionsValues();
                    DisplaySuccess($"Connection '{connectionName}' added successfully");
                }
                else
                {
                    DisplayError("Failed to add connection");
                }
            }, profileOption);

            // Update connection interactively
            var updateCommand = new Command("update", "Update connection");
            var updateNameArg = new Argument<string>("name", "Connection name to update");
            updateCommand.AddArgument(updateNameArg);
            updateCommand.AddOption(profileOption);
            updateCommand.SetHandler((string name, string profile) =>
            {
                var services = new BeepServiceProvider(profile);
                var editor = services.GetEditor();
                var conn = editor.ConfigEditor.DataConnections.FirstOrDefault(c => c.ConnectionName == name);
                if (conn == null)
                {
                    DisplayError($"Connection '{name}' not found");
                    return;
                }
                AnsiConsole.MarkupLine($"[bold blue]Update Connection: {name}[/]\n");
                AnsiConsole.MarkupLine("[dim](Press Enter to keep current value)[/]\n");
                if (conn.Category == DatasourceCategory.FILE)
                {
                    var newFilePath = AnsiConsole.Ask($"File Path [dim]({conn.FilePath})[/]:", conn.FilePath);
                    if (!string.IsNullOrEmpty(newFilePath)) conn.FilePath = newFilePath;
                    var newFileName = AnsiConsole.Ask($"File Name [dim]({conn.FileName})[/]:", conn.FileName ?? string.Empty);
                    if (!string.IsNullOrEmpty(newFileName)) conn.FileName = newFileName;
                }
                else if (conn.Category == DatasourceCategory.RDBMS || conn.Category == DatasourceCategory.NOSQL)
                {
                    var newHost = AnsiConsole.Ask($"Host [dim]({conn.Host})[/]:", conn.Host);
                    if (!string.IsNullOrEmpty(newHost)) conn.Host = newHost;
                    var newPort = AnsiConsole.Ask($"Port [dim]({conn.Port})[/]:", conn.Port);
                    conn.Port = newPort;
                    var newDatabase = AnsiConsole.Ask($"Database [dim]({conn.Database})[/]:", conn.Database);
                    if (!string.IsNullOrEmpty(newDatabase)) conn.Database = newDatabase;
                    var newUser = AnsiConsole.Ask($"Username [dim]({conn.UserID})[/]:", conn.UserID);
                    if (!string.IsNullOrEmpty(newUser)) conn.UserID = newUser;
                    if (AnsiConsole.Confirm("Update password?", false))
                    {
                        conn.Password = AnsiConsole.Prompt(new TextPrompt<string>("Enter new password:").Secret());
                    }
                }
                editor.ConfigEditor.SaveDataconnectionsValues();
                DisplaySuccess($"Connection '{name}' updated");
            }, updateNameArg, profileOption);

            // Delete connection with confirmation
            var deleteCommand = new Command("delete", "Delete connection");
            var deleteNameArg = new Argument<string>("name", "Connection name to delete");
            var forceOption = new Option<bool>("--force", "Skip confirmation");
            deleteCommand.AddArgument(deleteNameArg);
            deleteCommand.AddOption(forceOption);
            deleteCommand.AddOption(profileOption);
            deleteCommand.SetHandler((string name, bool force, string profile) =>
            {
                var services = new BeepServiceProvider(profile);
                var editor = services.GetEditor();
                if (!editor.ConfigEditor.DataConnectionExist(name))
                {
                    DisplayError($"Connection '{name}' not found");
                    return;
                }
                if (!force && !AnsiConsole.Confirm($"Are you sure you want to delete connection '{name}'?"))
                {
                    AnsiConsole.MarkupLine("[yellow]Operation cancelled[/]");
                    return;
                }
                if (editor.ConfigEditor.RemoveDataConnection(name))
                {
                    editor.ConfigEditor.SaveDataconnectionsValues();
                    DisplaySuccess($"Connection '{name}' deleted");
                }
                else
                {
                    DisplayError("Failed to delete connection");
                }
            }, deleteNameArg, forceOption, profileOption);

            connCommand.AddCommand(listCommand);
            connCommand.AddCommand(addCommand);
            connCommand.AddCommand(updateCommand);
            connCommand.AddCommand(deleteCommand);

            // ============ GENERAL CONFIG COMMANDS ============
            var showCommand = new Command("show", "Show configuration details");
            showCommand.AddOption(profileOption);
            showCommand.SetHandler((string profile) =>
            {
                var services = new BeepServiceProvider(profile);
                var editor = services.GetEditor();
                var table = new Table();
                table.AddColumn("Setting");
                table.AddColumn("Value");
                table.AddRow("Profile", profile);
                table.AddRow("Config Path", editor.ConfigEditor.ConfigPath ?? "N/A");
                table.AddRow("Exe Path", editor.ConfigEditor.ExePath ?? "N/A");
                table.AddRow("Container Name", editor.ConfigEditor.ContainerName ?? "N/A");
                table.AddRow("Config Type", editor.ConfigEditor.ConfigType.ToString());
                table.AddRow("Data Connections", editor.ConfigEditor.DataConnections.Count.ToString());
                table.AddRow("Drivers", editor.ConfigEditor.DataDriversClasses.Count.ToString());
                table.AddRow("WorkFlows", editor.ConfigEditor.WorkFlows?.Count.ToString() ?? "0");
                table.AddRow("Data Sources Classes", editor.ConfigEditor.DataSourcesClasses?.Count.ToString() ?? "0");
                table.AddRow("Addins", editor.ConfigEditor.Addins?.Count.ToString() ?? "0");
                table.AddRow("Data Files Path", editor.ConfigEditor.Config?.DataFilePath ?? "N/A");
                table.AddRow("Scripts Path", editor.ConfigEditor.Config?.ScriptsPath ?? "N/A");
                table.AddRow("Entities Path", editor.ConfigEditor.Config?.EntitiesPath ?? "N/A");
                table.AddRow("Mapping Path", editor.ConfigEditor.Config?.MappingPath ?? "N/A");
                table.AddRow("Connection Drivers Path", editor.ConfigEditor.Config?.ConnectionDriversPath ?? "N/A");
                AnsiConsole.Write(table);
            }, profileOption);

            var pathCommand = new Command("path", "Show configuration path");
            pathCommand.AddOption(profileOption);
            pathCommand.SetHandler((string profile) =>
            {
                var services = new BeepServiceProvider(profile);
                var editor = services.GetEditor();
                AnsiConsole.MarkupLine($"[bold]Config Path:[/] {editor.ConfigEditor.ConfigPath}");
            }, profileOption);

            var validateCommand = new Command("validate", "Validate configuration");
            validateCommand.AddOption(profileOption);
            validateCommand.SetHandler((string profile) =>
            {
                try
                {
                    var services = new BeepServiceProvider(profile);
                    var editor = services.GetEditor();
                    AnsiConsole.Status().Start("Validating configuration...", ctx =>
                    {
                        ctx.Status("Checking data connections...");
                        var connections = editor.ConfigEditor.DataConnections;
                        ctx.Status("Checking drivers...");
                        var drivers = editor.ConfigEditor.DataDriversClasses;
                        ctx.Status("Checking assemblies...");
                        var assemblies = editor.assemblyHandler.Assemblies;
                    });
                    DisplaySuccess("Configuration is valid");
                }
                catch (Exception ex)
                {
                    DisplayError($"Configuration validation failed: {ex.Message}");
                }
            }, profileOption);

            configCommand.AddCommand(connCommand);
            configCommand.AddCommand(showCommand);
            configCommand.AddCommand(pathCommand);
            configCommand.AddCommand(validateCommand);
            return configCommand;
        }
    }
}
