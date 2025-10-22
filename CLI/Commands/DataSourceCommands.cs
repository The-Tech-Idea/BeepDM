using System;
using System.CommandLine;
using System.Data;
using System.Linq;
using Spectre.Console;
using TheTechIdea.Beep.CLI.Infrastructure;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace TheTechIdea.Beep.CLI.Commands
{
    /// <summary>
    /// Data source runtime operations - works with live IDataSource instances for data operations
    /// </summary>
    public static class DataSourceCommands
    {
        public static Command Build()
        {
            var dsCommand = new Command("datasource", "Data source runtime operations");
            dsCommand.AddAlias("ds");

            var profileOption = new Option<string>("--profile", () => ProfileManager.DEFAULT_PROFILE, "Profile to use");

            // ============ CONNECTION TESTING & INFO ============
            
            // datasource test
            var testCommand = new Command("test", "Test data source connection");
            var testNameArg = new Argument<string>("name", "Data source name");
            testCommand.AddArgument(testNameArg);
            testCommand.AddOption(profileOption);
            testCommand.SetHandler((string name, string profile) =>
            {
                var services = new BeepServiceProvider(profile);
                var editor = services.GetEditor();
                
                AnsiConsole.Status()
                    .Start($"Testing connection to '{name}'...", ctx =>
                    {
                        try
                        {
                            var ds = editor.GetDataSource(name);
                            if (ds == null)
                            {
                                AnsiConsole.MarkupLine($"[red]✗[/] Data source '{name}' not found in configuration");
                                return;
                            }
                            
                            var state = ds.Openconnection();
                            if (state == ConnectionState.Open)
                            {
                                AnsiConsole.MarkupLine($"[green]✓[/] Connection successful");
                                AnsiConsole.MarkupLine($"[dim]Type:[/] {ds.DatasourceType}");
                                AnsiConsole.MarkupLine($"[dim]Category:[/] {ds.Category}");
                                ds.Closeconnection();
                            }
                            else
                            {
                                AnsiConsole.MarkupLine($"[red]✗[/] Connection failed: {state}");
                            }
                        }
                        catch (Exception ex)
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
                        }
                    });
            }, testNameArg, profileOption);

            // datasource info
            var infoCommand = new Command("info", "Show detailed data source information");
            var infoNameArg = new Argument<string>("name", "Data source name");
            infoCommand.AddArgument(infoNameArg);
            infoCommand.AddOption(profileOption);
            infoCommand.SetHandler((string name, string profile) =>
            {
                var services = new BeepServiceProvider(profile);
                var editor = services.GetEditor();
                
                var conn = editor.ConfigEditor.DataConnections
                    .FirstOrDefault(c => c.ConnectionName == name);
                
                if (conn == null)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Data source '{name}' not found");
                    return;
                }
                
                var table = new Table();
                table.Border = TableBorder.Rounded;
                table.Title = new TableTitle($"[bold cyan]{conn.ConnectionName}[/]");
                table.AddColumn("Property");
                table.AddColumn("Value");
                
                // Common properties
                table.AddRow("[bold]Connection Name[/]", conn.ConnectionName ?? "N/A");
                table.AddRow("[bold]Database Type[/]", conn.DatabaseType.ToString());
                table.AddRow("[bold]Category[/]", conn.Category.ToString());
                table.AddRow("Driver Name", conn.DriverName ?? "N/A");
                table.AddRow("Driver Version", conn.DriverVersion ?? "N/A");
                
                // Category-specific properties
                switch (conn.Category)
                {
                    case DatasourceCategory.FILE:
                        table.AddRow("[cyan]File Path[/]", conn.FilePath ?? "N/A");
                        table.AddRow("[cyan]File Name[/]", conn.FileName ?? "N/A");
                        table.AddRow("[cyan]Extension[/]", conn.Ext ?? "N/A");
                        if (conn.Delimiter != '\0')
                            table.AddRow("[cyan]Delimiter[/]", conn.Delimiter.ToString());
                        break;
                        
                    case DatasourceCategory.WEBAPI:
                        table.AddRow("[cyan]URL[/]", conn.Url ?? "N/A");
                        table.AddRow("[cyan]HTTP Method[/]", conn.HttpMethod ?? "GET");
                        if (!string.IsNullOrEmpty(conn.ApiKey))
                            table.AddRow("[cyan]API Key[/]", "[masked]");
                        if (!string.IsNullOrEmpty(conn.KeyToken))
                            table.AddRow("[cyan]Token[/]", "[masked]");
                        if (conn.UseOAuth)
                            table.AddRow("[cyan]OAuth[/]", "Enabled");
                        if (conn.Timeout > 0)
                            table.AddRow("[cyan]Timeout[/]", $"{conn.Timeout}s");
                        break;
                        
                    case DatasourceCategory.CLOUD:
                        if (!string.IsNullOrEmpty(conn.Url))
                            table.AddRow("[cyan]Endpoint URL[/]", conn.Url);
                        if (!string.IsNullOrEmpty(conn.Host))
                            table.AddRow("[cyan]Host[/]", conn.Host);
                        if (!string.IsNullOrEmpty(conn.Database))
                            table.AddRow("[cyan]Database/Bucket[/]", conn.Database);
                        if (!string.IsNullOrEmpty(conn.ApiKey))
                            table.AddRow("[cyan]API Key[/]", "[masked]");
                        if (conn.UseSSL)
                            table.AddRow("[cyan]SSL[/]", "Enabled");
                        break;
                        
                    case DatasourceCategory.NOSQL:
                        if (!string.IsNullOrEmpty(conn.Host))
                            table.AddRow("[cyan]Host[/]", conn.Host);
                        if (conn.Port > 0)
                            table.AddRow("[cyan]Port[/]", conn.Port.ToString());
                        if (!string.IsNullOrEmpty(conn.Database))
                            table.AddRow("[cyan]Database[/]", conn.Database);
                        if (!string.IsNullOrEmpty(conn.UserID))
                            table.AddRow("[cyan]User ID[/]", conn.UserID);
                        if (!string.IsNullOrEmpty(conn.Url))
                            table.AddRow("[cyan]Connection URL[/]", conn.Url);
                        break;
                        
                    case DatasourceCategory.RDBMS:
                        table.AddRow("[cyan]Database[/]", conn.Database ?? "N/A");
                        table.AddRow("[cyan]Host[/]", conn.Host ?? "N/A");
                        if (conn.Port > 0)
                            table.AddRow("[cyan]Port[/]", conn.Port.ToString());
                        table.AddRow("[cyan]User ID[/]", conn.UserID ?? "N/A");
                        if (!string.IsNullOrEmpty(conn.SchemaName))
                            table.AddRow("[cyan]Schema[/]", conn.SchemaName);
                        if (conn.IntegratedSecurity)
                            table.AddRow("[cyan]Integrated Security[/]", "Yes");
                        if (conn.UseSSL)
                            table.AddRow("[cyan]SSL[/]", "Enabled");
                        table.AddRow("[cyan]Connection String[/]", string.IsNullOrEmpty(conn.ConnectionString) ? "N/A" : "[masked]");
                        break;
                        
                    case DatasourceCategory.STREAM:
                        if (!string.IsNullOrEmpty(conn.Url))
                            table.AddRow("[cyan]Stream URL[/]", conn.Url);
                        if (!string.IsNullOrEmpty(conn.Host))
                            table.AddRow("[cyan]Host[/]", conn.Host);
                        if (conn.Port > 0)
                            table.AddRow("[cyan]Port[/]", conn.Port.ToString());
                        break;
                        
                    case DatasourceCategory.Connector:
                        if (!string.IsNullOrEmpty(conn.Url))
                            table.AddRow("[cyan]Connector URL[/]", conn.Url);
                        if (!string.IsNullOrEmpty(conn.ApiKey))
                            table.AddRow("[cyan]API Key[/]", "[masked]");
                        if (!string.IsNullOrEmpty(conn.KeyToken))
                            table.AddRow("[cyan]Token[/]", "[masked]");
                        break;
                        
                    case DatasourceCategory.INMEMORY:
                        table.AddRow("[cyan]In-Memory[/]", "Yes");
                        if (!string.IsNullOrEmpty(conn.Database))
                            table.AddRow("[cyan]Database Name[/]", conn.Database);
                        break;
                        
                    case DatasourceCategory.VectorDB:
                        if (!string.IsNullOrEmpty(conn.Host))
                            table.AddRow("[cyan]Host[/]", conn.Host);
                        if (conn.Port > 0)
                            table.AddRow("[cyan]Port[/]", conn.Port.ToString());
                        if (!string.IsNullOrEmpty(conn.Url))
                            table.AddRow("[cyan]URL[/]", conn.Url);
                        if (!string.IsNullOrEmpty(conn.ApiKey))
                            table.AddRow("[cyan]API Key[/]", "[masked]");
                        if (!string.IsNullOrEmpty(conn.Database))
                            table.AddRow("[cyan]Collection/Index[/]", conn.Database);
                        break;
                        
                    case DatasourceCategory.Blockchain:
                        if (!string.IsNullOrEmpty(conn.Url))
                            table.AddRow("[cyan]Node URL[/]", conn.Url);
                        if (!string.IsNullOrEmpty(conn.ApiKey))
                            table.AddRow("[cyan]API Key[/]", "[masked]");
                        break;
                }
                
                // Additional common properties
                table.AddRow("GUID", conn.GuidID ?? "N/A");
                table.AddRow("In Memory", conn.IsInMemory.ToString());
                table.AddRow("Read Only", conn.ReadOnly.ToString());
                
                // Additional parameters
                if (conn.ParameterList != null && conn.ParameterList.Any())
                {
                    table.AddRow("[dim]Additional Parameters[/]", $"{conn.ParameterList.Count} parameter(s)");
                }
                
                AnsiConsole.Write(table);
            }, infoNameArg, profileOption);

            // datasource entities (list tables/files/collections)
            var entitiesCommand = new Command("entities", "List entities (tables/files/collections) in data source");
            var entitiesNameArg = new Argument<string>("name", "Data source name");
            entitiesCommand.AddArgument(entitiesNameArg);
            entitiesCommand.AddOption(profileOption);
            entitiesCommand.SetHandler((string name, string profile) =>
            {
                var services = new BeepServiceProvider(profile);
                var editor = services.GetEditor();
                
                AnsiConsole.Status()
                    .Start($"Loading entities from '{name}'...", ctx =>
                    {
                        try
                        {
                            var ds = editor.GetDataSource(name);
                            if (ds == null)
                            {
                                AnsiConsole.MarkupLine($"[red]✗[/] Data source '{name}' not found");
                                return;
                            }
                            
                            if (ds.ConnectionStatus != ConnectionState.Open)
                            {
                                ds.Openconnection();
                            }
                            
                            var entities = ds.GetEntitesList().ToList();
                            
                            var table = new Table();
                            table.Border = TableBorder.Rounded;
                            table.AddColumn("Entity Name");
                            table.AddColumn("Type");
                            
                            foreach (var entityName in entities)
                            {
                                var structure = ds.GetEntityStructure(entityName, false);
                                table.AddRow(
                                    entityName,
                                    structure?.Viewtype.ToString() ?? "Unknown"
                                );
                            }
                            
                            AnsiConsole.Write(table);
                            AnsiConsole.MarkupLine($"\n[blue]Total:[/] {entities.Count} entity/entities");
                            
                            ds.Closeconnection();
                        }
                        catch (Exception ex)
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
                        }
                    });
            }, entitiesNameArg, profileOption);

            dsCommand.AddCommand(testCommand);
            dsCommand.AddCommand(infoCommand);
            dsCommand.AddCommand(entitiesCommand);

            return dsCommand;
        }
    }
}
