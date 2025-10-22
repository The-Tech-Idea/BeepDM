using System;
using System.CommandLine;
using System.Linq;
using System.Collections.Generic;
using Spectre.Console;
using TheTechIdea.Beep.CLI.Infrastructure;
using TheTechIdea.Beep.Editor.BeepSync;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.CLI.Commands
{
    /// <summary>
    /// Data synchronization commands - bi-directional sync between data sources
    /// </summary>
    public static class SyncCommands
    {
        public static Command Build()
        {
            var syncCommand = new Command("sync", "Bi-directional data synchronization");

            var profileOption = new Option<string>("--profile", () => ProfileManager.DEFAULT_PROFILE, "Profile to use");

            // ============ CREATE SYNC SCHEMA ============
            
            // sync create
            var createCommand = new Command("create", "Create synchronization schema");
            var schemaIdArg = new Argument<string>("schema-id", "Unique identifier for sync schema");
            var srcDsArg = new Argument<string>("source-datasource", "Source data source name");
            var srcEntityArg = new Argument<string>("source-entity", "Source entity name");
            var destDsArg = new Argument<string>("dest-datasource", "Destination data source name");
            var destEntityArg = new Argument<string>("dest-entity", "Destination entity name");
            var bidirectionalOption = new Option<bool>("--bidirectional", () => false, "Enable bi-directional synchronization");
            
            createCommand.AddArgument(schemaIdArg);
            createCommand.AddArgument(srcDsArg);
            createCommand.AddArgument(srcEntityArg);
            createCommand.AddArgument(destDsArg);
            createCommand.AddArgument(destEntityArg);
            createCommand.AddOption(bidirectionalOption);
            createCommand.AddOption(profileOption);
            
            createCommand.SetHandler((string schemaId, string srcDs, string srcEntity, string destDs, string destEntity, bool bidirectional, string profile) =>
            {
                var services = new BeepServiceProvider(profile);
                var editor = services.GetEditor();
                
                try
                {
                    AnsiConsole.Status()
                        .Start("Creating sync schema...", ctx =>
                        {
                            var syncManager = new BeepSyncManager(editor);
                            
                            // Create schema
                            var schema = new DataSyncSchema
                            {
                                ID = schemaId,
                                SourceDataSourceName = srcDs,
                                SourceEntityName = srcEntity,
                                DestinationDataSourceName = destDs,
                                DestinationEntityName = destEntity,
                                SyncDirection = bidirectional ? "BiDirectional" : "OneWay",
                                SyncType = "Full",
                                SyncStatus = "Active"
                            };
                            
                            // Validate schema
                            var validation = syncManager.ValidateSchema(schema);
                            if (validation.Flag == Errors.Ok)
                            {
                                syncManager.AddSyncSchema(schema);
                                syncManager.SaveSchemas();
                                
                                AnsiConsole.MarkupLine($"[green]✓[/] Sync schema created successfully");
                                AnsiConsole.MarkupLine($"[cyan]Schema ID:[/] {schemaId}");
                                AnsiConsole.MarkupLine($"[cyan]Source:[/] {srcDs}.{srcEntity}");
                                AnsiConsole.MarkupLine($"[cyan]Destination:[/] {destDs}.{destEntity}");
                                AnsiConsole.MarkupLine($"[cyan]Direction:[/] {schema.SyncDirection}");
                            }
                            else
                            {
                                AnsiConsole.MarkupLine($"[red]✗[/] Validation failed: {validation.Message}");
                            }
                        });
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
                }
            }, schemaIdArg, srcDsArg, srcEntityArg, destDsArg, destEntityArg, bidirectionalOption, profileOption);

            // ============ RUN SYNC ============
            
            // sync run
            var runCommand = new Command("run", "Execute synchronization");
            var runSchemaArg = new Argument<string>("schema-id", "Schema ID to execute");
            var dryRunOption = new Option<bool>("--dry-run", () => false, "Validate without executing");
            
            runCommand.AddArgument(runSchemaArg);
            runCommand.AddOption(dryRunOption);
            runCommand.AddOption(profileOption);
            
            runCommand.SetHandler((string schemaId, bool dryRun, string profile) =>
            {
                var services = new BeepServiceProvider(profile);
                var editor = services.GetEditor();
                
                try
                {
                    var syncManager = new BeepSyncManager(editor);
                    var schema = syncManager.GetSchema(schemaId);
                    
                    if (schema == null)
                    {
                        AnsiConsole.MarkupLine($"[red]✗[/] Schema '{schemaId}' not found");
                        return;
                    }
                    
                    if (dryRun)
                    {
                        AnsiConsole.MarkupLine("[yellow]DRY RUN MODE - Validating only[/]");
                        var validation = syncManager.ValidateSchema(schema);
                        
                        if (validation.Flag == Errors.Ok)
                        {
                            AnsiConsole.MarkupLine("[green]✓[/] Schema is valid and ready to sync");
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] Validation failed: {validation.Message}");
                        }
                        return;
                    }
                    
                    AnsiConsole.Status()
                        .Start($"Synchronizing {schema.SourceDataSourceName}.{schema.SourceEntityName} → {schema.DestinationDataSourceName}.{schema.DestinationEntityName}...", ctx =>
                        {
                            var result = syncManager.SyncData(schema);
                            
                            if (result.Flag == Errors.Ok)
                            {
                                AnsiConsole.MarkupLine($"[green]✓[/] Synchronization completed successfully");
                            }
                            else
                            {
                                AnsiConsole.MarkupLine($"[red]✗[/] Synchronization failed: {result.Message}");
                            }
                        });
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
                }
            }, runSchemaArg, dryRunOption, profileOption);

            // ============ LIST SCHEMAS ============
            
            // sync list
            var listCommand = new Command("list", "List sync schemas");
            listCommand.AddOption(profileOption);
            
            listCommand.SetHandler((string profile) =>
            {
                var services = new BeepServiceProvider(profile);
                var editor = services.GetEditor();
                
                try
                {
                    var syncManager = new BeepSyncManager(editor);
                    var schemas = syncManager.SyncSchemas;
                    
                    if (schemas == null || !schemas.Any())
                    {
                        AnsiConsole.MarkupLine("[yellow]No sync schemas found[/]");
                        return;
                    }
                    
                    var table = new Table();
                    table.Border = TableBorder.Rounded;
                    table.Title = new TableTitle($"[bold cyan]Sync Schemas ({schemas.Count})[/]");
                    table.AddColumn("Schema ID");
                    table.AddColumn("Source");
                    table.AddColumn("Destination");
                    table.AddColumn("Direction");
                    table.AddColumn("Status");
                    
                    foreach (var schema in schemas)
                    {
                        var source = $"{schema.SourceDataSourceName}.{schema.SourceEntityName}";
                        var dest = $"{schema.DestinationDataSourceName}.{schema.DestinationEntityName}";
                        var direction = schema.SyncDirection ?? "OneWay";
                        var status = schema.SyncStatus ?? "Unknown";
                        
                        table.AddRow(
                            schema.ID ?? "N/A",
                            source,
                            dest,
                            direction,
                            status
                        );
                    }
                    
                    AnsiConsole.Write(table);
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
                }
            }, profileOption);

            // ============ SHOW SCHEMA ============
            
            // sync show
            var showCommand = new Command("show", "Show schema details");
            var showSchemaArg = new Argument<string>("schema-id", "Schema ID to display");
            
            showCommand.AddArgument(showSchemaArg);
            showCommand.AddOption(profileOption);
            
            showCommand.SetHandler((string schemaId, string profile) =>
            {
                var services = new BeepServiceProvider(profile);
                var editor = services.GetEditor();
                
                try
                {
                    var syncManager = new BeepSyncManager(editor);
                    var schema = syncManager.GetSchema(schemaId);
                    
                    if (schema == null)
                    {
                        AnsiConsole.MarkupLine($"[red]✗[/] Schema '{schemaId}' not found");
                        return;
                    }
                    
                    AnsiConsole.MarkupLine($"[bold cyan]Sync Schema Details[/]");
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine($"[cyan]Schema ID:[/] {schema.ID}");
                    AnsiConsole.MarkupLine($"[cyan]Source DS:[/] {schema.SourceDataSourceName}");
                    AnsiConsole.MarkupLine($"[cyan]Source Entity:[/] {schema.SourceEntityName}");
                    AnsiConsole.MarkupLine($"[cyan]Destination DS:[/] {schema.DestinationDataSourceName}");
                    AnsiConsole.MarkupLine($"[cyan]Destination Entity:[/] {schema.DestinationEntityName}");
                    AnsiConsole.MarkupLine($"[cyan]Sync Direction:[/] {schema.SyncDirection ?? "OneWay"}");
                    AnsiConsole.MarkupLine($"[cyan]Sync Type:[/] {schema.SyncType ?? "Full"}");
                    AnsiConsole.MarkupLine($"[cyan]Status:[/] {schema.SyncStatus ?? "Unknown"}");
                    
                    if (schema.LastSyncDate != DateTime.MinValue)
                    {
                        AnsiConsole.MarkupLine($"[cyan]Last Sync:[/] {schema.LastSyncDate}");
                    }
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
                }
            }, showSchemaArg, profileOption);

            // ============ DELETE SCHEMA ============
            
            // sync delete
            var deleteCommand = new Command("delete", "Delete sync schema");
            var delSchemaArg = new Argument<string>("schema-id", "Schema ID to delete");
            
            deleteCommand.AddArgument(delSchemaArg);
            deleteCommand.AddOption(profileOption);
            
            deleteCommand.SetHandler((string schemaId, string profile) =>
            {
                var services = new BeepServiceProvider(profile);
                var editor = services.GetEditor();
                
                try
                {
                    var syncManager = new BeepSyncManager(editor);
                    syncManager.RemoveSyncSchema(schemaId);
                    syncManager.SaveSchemas();
                    
                    AnsiConsole.MarkupLine($"[green]✓[/] Schema '{schemaId}' deleted successfully");
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
                }
            }, delSchemaArg, profileOption);

            syncCommand.AddCommand(createCommand);
            syncCommand.AddCommand(runCommand);
            syncCommand.AddCommand(listCommand);
            syncCommand.AddCommand(showCommand);
            syncCommand.AddCommand(deleteCommand);

            return syncCommand;
        }
    }
}
