using System;
using System.CommandLine;
using System.Linq;
using Spectre.Console;
using TheTechIdea.Beep.CLI.Infrastructure;
using TheTechIdea.Beep.Editor.Mapping;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Workflow.Mapping;

namespace TheTechIdea.Beep.CLI.Commands
{
    /// <summary>
    /// Field mapping management commands - create and manage entity mappings
    /// </summary>
    public static class MappingCommands
    {
        public static Command Build()
        {
            var mappingCommand = new Command("mapping", "Field mapping management between entities");

            var profileOption = new Option<string>("--profile", () => ProfileManager.DEFAULT_PROFILE, "Profile to use");

            // ============ CREATE MAPPING ============
            
            // mapping create
            var createCommand = new Command("create", "Create field mapping between source and destination entities");
            var srcDsArg = new Argument<string>("source-datasource", "Source data source name");
            var srcEntityArg = new Argument<string>("source-entity", "Source entity name");
            var destDsArg = new Argument<string>("dest-datasource", "Destination data source name");
            var destEntityArg = new Argument<string>("dest-entity", "Destination entity name");
            var autoMapOption = new Option<bool>("--auto-map", () => true, "Automatically map matching field names");
            
            createCommand.AddArgument(srcDsArg);
            createCommand.AddArgument(srcEntityArg);
            createCommand.AddArgument(destDsArg);
            createCommand.AddArgument(destEntityArg);
            createCommand.AddOption(autoMapOption);
            createCommand.AddOption(profileOption);
            
            createCommand.SetHandler((string srcDs, string srcEntity, string destDs, string destEntity, bool autoMap, string profile) =>
            {
                var services = new BeepServiceProvider(profile);
                var editor = services.GetEditor();
                
                try
                {
                    AnsiConsole.Status()
                        .Start($"Creating mapping from {srcDs}.{srcEntity} to {destDs}.{destEntity}...", ctx =>
                        {
                            var result = MappingManager.CreateEntityMap(
                                editor, 
                                srcEntity, srcDs,
                                destEntity, destDs
                            );
                            
                            if (result.Item1.Flag == Errors.Ok)
                            {
                                var mapping = result.Item2;
                                
                                AnsiConsole.MarkupLine($"[green]✓[/] Mapping created successfully");
                                AnsiConsole.MarkupLine($"[cyan]Mapping Name:[/] {mapping.MappingName}");
                                
                                if (mapping.MappedEntities != null && mapping.MappedEntities.Any())
                                {
                                    var mappedEntity = mapping.MappedEntities.First();
                                    int mappedFieldCount = mappedEntity.SelectedDestFields?.Count ?? 0;
                                    
                                    AnsiConsole.MarkupLine($"[cyan]Mapped Fields:[/] {mappedFieldCount}");
                                    
                                    if (autoMap && mappedFieldCount > 0)
                                    {
                                        AnsiConsole.WriteLine();
                                        var table = new Table();
                                        table.Border = TableBorder.Rounded;
                                        table.Title = new TableTitle("[bold cyan]Field Mappings[/]");
                                        table.AddColumn("Source Field");
                                        table.AddColumn("Destination Field");
                                        table.AddColumn("Type");
                                        
                                        for (int i = 0; i < Math.Min(10, mappedFieldCount); i++)
                                        {
                                            var srcField = mappedEntity.EntityFields[i];
                                            var destField = mappedEntity.SelectedDestFields[i];
                                            table.AddRow(
                                                srcField.fieldname,
                                                destField.fieldname,
                                                srcField.fieldtype
                                            );
                                        }
                                        
                                        if (mappedFieldCount > 10)
                                        {
                                            table.AddRow("[dim]...[/]", "[dim]...[/]", "[dim]...[/]");
                                        }
                                        
                                        AnsiConsole.Write(table);
                                    }
                                }
                            }
                            else
                            {
                                AnsiConsole.MarkupLine($"[red]✗[/] Failed: {result.Item1.Message}");
                            }
                        });
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
                }
            }, srcDsArg, srcEntityArg, destDsArg, destEntityArg, autoMapOption, profileOption);

            // ============ LIST MAPPINGS ============
            
            // mapping list
            var listCommand = new Command("list", "List saved mappings");
            var datasourceOption = new Option<string?>("--datasource", "Filter by data source");
            
            listCommand.AddOption(datasourceOption);
            listCommand.AddOption(profileOption);
            
            listCommand.SetHandler((string? datasource, string profile) =>
            {
                var services = new BeepServiceProvider(profile);
                var editor = services.GetEditor();
                
                try
                {
                    var mappingPath = editor.ConfigEditor.Config.MappingPath;
                    
                    if (!System.IO.Directory.Exists(mappingPath))
                    {
                        AnsiConsole.MarkupLine("[yellow]No mappings found - mapping directory doesn't exist[/]");
                        return;
                    }
                    
                    var mappingFiles = System.IO.Directory.GetFiles(mappingPath, "*_Mapping.json", System.IO.SearchOption.AllDirectories);
                    
                    if (!mappingFiles.Any())
                    {
                        AnsiConsole.MarkupLine("[yellow]No mapping files found[/]");
                        return;
                    }
                    
                    var table = new Table();
                    table.Border = TableBorder.Rounded;
                    table.Title = new TableTitle($"[bold cyan]Saved Mappings ({mappingFiles.Length})[/]");
                    table.AddColumn("Entity");
                    table.AddColumn("Data Source");
                    table.AddColumn("Path");
                    
                    foreach (var file in mappingFiles)
                    {
                        var fileName = System.IO.Path.GetFileNameWithoutExtension(file);
                        var dirName = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(file));
                        
                        if (datasource != null && !dirName.Equals(datasource, StringComparison.InvariantCultureIgnoreCase))
                            continue;
                        
                        var entityName = fileName.Replace("_Mapping", "");
                        table.AddRow(entityName, dirName ?? "N/A", file);
                    }
                    
                    AnsiConsole.Write(table);
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
                }
            }, datasourceOption, profileOption);

            // ============ SHOW MAPPING ============
            
            // mapping show
            var showCommand = new Command("show", "Show mapping details");
            var entityNameArg = new Argument<string>("entity", "Entity name");
            var dsNameArg = new Argument<string>("datasource", "Data source name");
            
            showCommand.AddArgument(entityNameArg);
            showCommand.AddArgument(dsNameArg);
            showCommand.AddOption(profileOption);
            
            showCommand.SetHandler((string entityName, string dsName, string profile) =>
            {
                var services = new BeepServiceProvider(profile);
                var editor = services.GetEditor();
                
                try
                {
                    var mapping = editor.ConfigEditor.LoadMappingValues(entityName, dsName);
                    
                    if (mapping == null || string.IsNullOrEmpty(mapping.EntityName))
                    {
                        AnsiConsole.MarkupLine($"[red]✗[/] Mapping not found for {entityName} in {dsName}");
                        return;
                    }
                    
                    AnsiConsole.MarkupLine($"[bold cyan]Mapping Details[/]");
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine($"[cyan]Entity Name:[/] {mapping.EntityName}");
                    AnsiConsole.MarkupLine($"[cyan]Data Source:[/] {mapping.EntityDataSource}");
                    AnsiConsole.MarkupLine($"[cyan]Mapping Name:[/] {mapping.MappingName}");
                    
                    if (mapping.MappedEntities != null && mapping.MappedEntities.Any())
                    {
                        AnsiConsole.WriteLine();
                        AnsiConsole.MarkupLine($"[bold]Mapped Entities: {mapping.MappedEntities.Count}[/]");
                        
                        foreach (var mappedEntity in mapping.MappedEntities)
                        {
                            AnsiConsole.WriteLine();
                            AnsiConsole.MarkupLine($"[yellow]Source:[/] {mappedEntity.EntityDataSource}.{mappedEntity.EntityName}");
                            
                            if (mappedEntity.SelectedDestFields != null && mappedEntity.SelectedDestFields.Any())
                            {
                                var table = new Table();
                                table.Border = TableBorder.Rounded;
                                table.AddColumn("Source Field");
                                table.AddColumn("Dest Field");
                                table.AddColumn("Source Type");
                                table.AddColumn("Dest Type");
                                
                                for (int i = 0; i < mappedEntity.SelectedDestFields.Count; i++)
                                {
                                    var srcField = mappedEntity.EntityFields[i];
                                    var destField = mappedEntity.SelectedDestFields[i];
                                    
                                    table.AddRow(
                                        srcField.fieldname,
                                        destField.fieldname,
                                        srcField.fieldtype ?? "N/A",
                                        destField.fieldtype ?? "N/A"
                                    );
                                }
                                
                                AnsiConsole.Write(table);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
                }
            }, entityNameArg, dsNameArg, profileOption);

            // ============ DELETE MAPPING ============
            
            // mapping delete
            var deleteCommand = new Command("delete", "Delete a mapping");
            var delEntityArg = new Argument<string>("entity", "Entity name");
            var delDsArg = new Argument<string>("datasource", "Data source name");
            
            deleteCommand.AddArgument(delEntityArg);
            deleteCommand.AddArgument(delDsArg);
            deleteCommand.AddOption(profileOption);
            
            deleteCommand.SetHandler((string entityName, string dsName, string profile) =>
            {
                var services = new BeepServiceProvider(profile);
                var editor = services.GetEditor();
                
                try
                {
                    var mappingPath = editor.ConfigEditor.Config.MappingPath;
                    var filePath = System.IO.Path.Combine(mappingPath, dsName, $"{entityName}_Mapping.json");
                    
                    if (!System.IO.File.Exists(filePath))
                    {
                        AnsiConsole.MarkupLine($"[red]✗[/] Mapping file not found: {filePath}");
                        return;
                    }
                    
                    System.IO.File.Delete(filePath);
                    AnsiConsole.MarkupLine($"[green]✓[/] Mapping deleted: {entityName} from {dsName}");
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
                }
            }, delEntityArg, delDsArg, profileOption);

            mappingCommand.AddCommand(createCommand);
            mappingCommand.AddCommand(listCommand);
            mappingCommand.AddCommand(showCommand);
            mappingCommand.AddCommand(deleteCommand);

            return mappingCommand;
        }
    }
}
