using System;
using System.CommandLine;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using TheTechIdea.Beep.CLI.Infrastructure;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.ETL;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using System.Collections.Generic;

namespace TheTechIdea.Beep.CLI.Commands
{
    /// <summary>
    /// ETL operations commands - depends on DataSourceCommands
    /// Provides Extract, Transform, Load functionality for data migration
    /// </summary>
    public static class ETLCommands
    {
        public static Command Build()
        {
            var etlCommand = new Command("etl", "ETL (Extract, Transform, Load) operations");

            var profileOption = new Option<string>("--profile", () => ProfileManager.DEFAULT_PROFILE, "Profile to use");

            // ============ COPY ENTITY STRUCTURE ============
            
            // etl copy-structure
            var copyStructureCommand = new Command("copy-structure", "Copy entity structure from source to destination");
            var sourceArg = new Argument<string>("source", "Source data source name");
            var destArg = new Argument<string>("destination", "Destination data source name");
            var entityArg = new Argument<string>("entity", "Entity name to copy");
            var createOption = new Option<bool>("--create", () => true, "Create entity if missing");
            
            copyStructureCommand.AddArgument(sourceArg);
            copyStructureCommand.AddArgument(destArg);
            copyStructureCommand.AddArgument(entityArg);
            copyStructureCommand.AddOption(createOption);
            copyStructureCommand.AddOption(profileOption);
            
            copyStructureCommand.SetHandler(async (string source, string dest, string entity, bool create, string profile) =>
            {
                var services = new BeepServiceProvider(profile);
                var editor = services.GetEditor();
                
                await AnsiConsole.Status()
                    .StartAsync($"Copying structure of '{entity}' from {source} to {dest}...", async ctx =>
                    {
                        try
                        {
                            var sourceDs = CliHelper.ValidateAndGetDataSource(editor, source);
                            var destDs = CliHelper.ValidateAndGetDataSource(editor, dest);
                            
                            if (sourceDs == null || destDs == null) return;
                            
                            var etl = new ETLEditor(editor);
                            var progress = new Progress<PassedArgs>(args => 
                            {
                                if (!string.IsNullOrEmpty(args.Messege))
                                    ctx.Status($"{args.Messege}");
                            });
                            
                            var result = etl.CopyEntityStructure(sourceDs, destDs, entity, entity, progress, CancellationToken.None, create);
                            
                            if (result.Flag == Errors.Ok)
                            {
                                AnsiConsole.MarkupLine($"[green]✓[/] Entity structure copied successfully");
                            }
                            else
                            {
                                AnsiConsole.MarkupLine($"[red]✗[/] Failed: {result.Message}");
                            }
                        }
                        catch (Exception ex)
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
                        }
                    });
            }, sourceArg, destArg, entityArg, createOption, profileOption);

            // ============ COPY ENTITY DATA ============
            
            // etl copy-data
            var copyDataCommand = new Command("copy-data", "Copy entity data from source to destination");
            var copySourceArg = new Argument<string>("source", "Source data source name");
            var copyDestArg = new Argument<string>("destination", "Destination data source name");
            var copyEntityArg = new Argument<string>("entity", "Entity name to copy");
            var copyCreateOption = new Option<bool>("--create", () => true, "Create entity if missing");
            
            copyDataCommand.AddArgument(copySourceArg);
            copyDataCommand.AddArgument(copyDestArg);
            copyDataCommand.AddArgument(copyEntityArg);
            copyDataCommand.AddOption(copyCreateOption);
            copyDataCommand.AddOption(profileOption);
            
            copyDataCommand.SetHandler(async (string source, string dest, string entity, bool create, string profile) =>
            {
                var services = new BeepServiceProvider(profile);
                var editor = services.GetEditor();
                
                await AnsiConsole.Status()
                    .StartAsync($"Copying data of '{entity}' from {source} to {dest}...", async ctx =>
                    {
                        try
                        {
                            var sourceDs = CliHelper.ValidateAndGetDataSource(editor, source);
                            var destDs = CliHelper.ValidateAndGetDataSource(editor, dest);
                            
                            if (sourceDs == null || destDs == null) return;
                            
                            var etl = new ETLEditor(editor);
                            int recordCount = 0;
                            
                            var progress = new Progress<PassedArgs>(args => 
                            {
                                if (!string.IsNullOrEmpty(args.Messege))
                                {
                                    ctx.Status($"{args.Messege}");
                                    recordCount = etl.ScriptCount;
                                }
                            });
                            
                            var result = etl.CopyEntityData(sourceDs, destDs, entity, entity, progress, CancellationToken.None, create);
                            
                            if (result.Flag == Errors.Ok)
                            {
                                AnsiConsole.MarkupLine($"[green]✓[/] {recordCount} records copied successfully");
                            }
                            else
                            {
                                AnsiConsole.MarkupLine($"[red]✗[/] Failed: {result.Message}");
                            }
                        }
                        catch (Exception ex)
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
                        }
                    });
            }, copySourceArg, copyDestArg, copyEntityArg, copyCreateOption, profileOption);

            // ============ COPY ALL DATA ============
            
            // etl copy-all
            var copyAllCommand = new Command("copy-all", "Copy all entities and data from source to destination");
            var allSourceArg = new Argument<string>("source", "Source data source name");
            var allDestArg = new Argument<string>("destination", "Destination data source name");
            var allCreateOption = new Option<bool>("--create", () => true, "Create entities if missing");
            
            copyAllCommand.AddArgument(allSourceArg);
            copyAllCommand.AddArgument(allDestArg);
            copyAllCommand.AddOption(allCreateOption);
            copyAllCommand.AddOption(profileOption);
            
            copyAllCommand.SetHandler(async (string source, string dest, bool create, string profile) =>
            {
                var services = new BeepServiceProvider(profile);
                var editor = services.GetEditor();
                
                await AnsiConsole.Progress()
                    .StartAsync(async ctx =>
                    {
                        try
                        {
                            var sourceDs = CliHelper.ValidateAndGetDataSource(editor, source);
                            var destDs = CliHelper.ValidateAndGetDataSource(editor, dest);
                            
                            if (sourceDs == null || destDs == null) return;
                            
                            var etl = new ETLEditor(editor);
                            var progress = new Progress<PassedArgs>(args => 
                            {
                                if (!string.IsNullOrEmpty(args.Messege))
                                    AnsiConsole.MarkupLine($"[dim]{args.Messege}[/]");
                            });
                            
                            var result = etl.CopyDatasourceData(sourceDs, destDs, progress, CancellationToken.None, create);
                            
                            if (result.Flag == Errors.Ok)
                            {
                                AnsiConsole.MarkupLine($"[green]✓[/] All data copied successfully");
                            }
                            else
                            {
                                AnsiConsole.MarkupLine($"[red]✗[/] Failed: {result.Message}");
                            }
                        }
                        catch (Exception ex)
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
                        }
                    });
            }, allSourceArg, allDestArg, allCreateOption, profileOption);

            // ============ VALIDATE ENTITIES ============
            
            // etl validate
            var validateCommand = new Command("validate", "Validate entity consistency between source and destination");
            var valSourceArg = new Argument<string>("source", "Source data source name");
            var valDestArg = new Argument<string>("destination", "Destination data source name");
            var valEntityArg = new Argument<string>("entity", "Entity name to validate");
            
            validateCommand.AddArgument(valSourceArg);
            validateCommand.AddArgument(valDestArg);
            validateCommand.AddArgument(valEntityArg);
            validateCommand.AddOption(profileOption);
            
            validateCommand.SetHandler((string source, string dest, string entity, string profile) =>
            {
                var services = new BeepServiceProvider(profile);
                var editor = services.GetEditor();
                
                try
                {
                    var sourceDs = CliHelper.ValidateAndGetDataSource(editor, source);
                    var destDs = CliHelper.ValidateAndGetDataSource(editor, dest);
                    
                    if (sourceDs == null || destDs == null) return;
                    
                    var validator = new ETLValidator(editor);
                    var result = validator.ValidateEntityConsistency(sourceDs, destDs, entity, entity);
                    
                    if (result.Flag == Errors.Ok)
                    {
                        AnsiConsole.MarkupLine($"[green]✓[/] Entity structures are consistent");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[yellow]Validation issues found:[/]");
                        var errors = result as ErrorsInfo;
                        if (errors?.Errors != null)
                        {
                            foreach (var err in errors.Errors)
                            {
                                AnsiConsole.MarkupLine($"  [red]•[/] {err.Message}");
                            }
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"  [red]•[/] {result.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
                }
            }, valSourceArg, valDestArg, valEntityArg, profileOption);

            // ============ COMPARE ENTITIES ============
            
            // etl compare
            var compareCommand = new Command("compare", "Compare entity structures between source and destination");
            var cmpSourceArg = new Argument<string>("source", "Source data source name");
            var cmpDestArg = new Argument<string>("destination", "Destination data source name");
            var cmpEntityArg = new Argument<string>("entity", "Entity name to compare");
            
            compareCommand.AddArgument(cmpSourceArg);
            compareCommand.AddArgument(cmpDestArg);
            compareCommand.AddArgument(cmpEntityArg);
            compareCommand.AddOption(profileOption);
            
            compareCommand.SetHandler((string source, string dest, string entity, string profile) =>
            {
                var services = new BeepServiceProvider(profile);
                var editor = services.GetEditor();
                
                try
                {
                    var sourceDs = CliHelper.ValidateAndGetDataSource(editor, source);
                    var destDs = CliHelper.ValidateAndGetDataSource(editor, dest);
                    
                    if (sourceDs == null || destDs == null) return;
                    
                    var srcStructure = sourceDs.GetEntityStructure(entity, true);
                    var destStructure = destDs.GetEntityStructure(entity, true);
                    
                    if (srcStructure == null || destStructure == null)
                    {
                        AnsiConsole.MarkupLine($"[red]✗[/] Entity not found in one or both data sources");
                        return;
                    }
                    
                    var table = new Table();
                    table.Border = TableBorder.Rounded;
                    table.Title = new TableTitle($"[bold cyan]Comparing: {entity}[/]");
                    table.AddColumn("Field Name");
                    table.AddColumn("Source Type");
                    table.AddColumn("Dest Type");
                    table.AddColumn("Status");
                    
                    foreach (var srcField in srcStructure.Fields)
                    {
                        var destField = destStructure.Fields.FirstOrDefault(f => 
                            f.fieldname.Equals(srcField.fieldname, StringComparison.InvariantCultureIgnoreCase));
                        
                        if (destField != null)
                        {
                            bool match = srcField.fieldtype.Equals(destField.fieldtype, StringComparison.InvariantCultureIgnoreCase);
                            table.AddRow(
                                srcField.fieldname,
                                srcField.fieldtype,
                                destField.fieldtype,
                                match ? "[green]✓ Match[/]" : "[yellow]⚠ Type Diff[/]"
                            );
                        }
                        else
                        {
                            table.AddRow(
                                srcField.fieldname,
                                srcField.fieldtype,
                                "[dim]N/A[/]",
                                "[red]✗ Missing[/]"
                            );
                        }
                    }
                    
                    // Check for fields in dest that don't exist in source
                    foreach (var destField in destStructure.Fields)
                    {
                        var srcField = srcStructure.Fields.FirstOrDefault(f => 
                            f.fieldname.Equals(destField.fieldname, StringComparison.InvariantCultureIgnoreCase));
                        
                        if (srcField == null)
                        {
                            table.AddRow(
                                destField.fieldname,
                                "[dim]N/A[/]",
                                destField.fieldtype,
                                "[cyan]+ Extra[/]"
                            );
                        }
                    }
                    
                    AnsiConsole.Write(table);
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
                }
            }, cmpSourceArg, cmpDestArg, cmpEntityArg, profileOption);

            etlCommand.AddCommand(copyStructureCommand);
            etlCommand.AddCommand(copyDataCommand);
            etlCommand.AddCommand(copyAllCommand);
            etlCommand.AddCommand(validateCommand);
            etlCommand.AddCommand(compareCommand);

            return etlCommand;
        }
    }
}
