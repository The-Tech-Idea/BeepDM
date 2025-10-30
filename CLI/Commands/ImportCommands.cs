using System;
using System.CommandLine;
using System.Linq;
using System.Collections.Generic;
using Spectre.Console;
using TheTechIdea.Beep.CLI.Infrastructure;
using TheTechIdea.Beep.Editor.Importing;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.CLI.Commands
{
    /// <summary>
    /// Bulk data import commands - import data from files into data sources
    /// </summary>
    public static class ImportCommands
    {
        public static Command Build()
        {
            var importCommand = new Command("import", "Bulk data import from files");

            var profileOption = new Option<string>("--profile", () => ProfileManager.DEFAULT_PROFILE, "Profile to use");

            // ============ IMPORT FILE ============
            
            // import file
            var fileCommand = new Command("file", "Import data from file to data source");
            var sourceFileArg = new Argument<string>("source-file", "Path to source file");
            var destDsArg = new Argument<string>("dest-datasource", "Destination data source name");
            var destEntityArg = new Argument<string>("dest-entity", "Destination entity name");
            var mappingOption = new Option<string?>("--mapping-datasource", "Use saved mapping from data source");
            var createTableOption = new Option<bool>("--create", () => false, "Create destination table if it doesn't exist");
            var batchSizeOption = new Option<int>("--batch-size", () => 1000, "Batch size for inserts");
            var skipHeaderOption = new Option<bool>("--skip-header", () => true, "Skip header row for CSV/Excel files");
            
            fileCommand.AddArgument(sourceFileArg);
            fileCommand.AddArgument(destDsArg);
            fileCommand.AddArgument(destEntityArg);
            fileCommand.AddOption(mappingOption);
            fileCommand.AddOption(createTableOption);
            fileCommand.AddOption(batchSizeOption);
            fileCommand.AddOption(skipHeaderOption);
            fileCommand.AddOption(profileOption);
            
            fileCommand.SetHandler((string sourceFile, string destDs, string destEntity, string? mappingDs, bool createTable, int batchSize, bool skipHeader, string profile) =>
            {
                var services = new BeepServiceProvider(profile);
                var editor = services.GetEditor();
                
                try
                {
                    if (!System.IO.File.Exists(sourceFile))
                    {
                        AnsiConsole.MarkupLine($"[red]✗[/] Source file not found: {sourceFile}");
                        return;
                    }
                    
                    var destConnection = editor.DataSources.FirstOrDefault(x => x.DatasourceName.Equals(destDs, StringComparison.InvariantCultureIgnoreCase));
                    if (destConnection == null)
                    {
                        AnsiConsole.MarkupLine($"[red]✗[/] Destination data source '{destDs}' not found in configuration");
                        return;
                    }
                    
                    AnsiConsole.Status()
                        .Start($"Importing data from {System.IO.Path.GetFileName(sourceFile)}...", ctx =>
                        {
                            ctx.Status($"Opening connections...");
                            
                            // Get or create data source for the file
                            var fileExtension = System.IO.Path.GetExtension(sourceFile).TrimStart('.');
                            var sourceDs = editor.GetDataSource($"TempImport_{fileExtension}");
                            
                            if (sourceDs == null)
                            {
                                // Create temporary data source for the file
                                var fileConn = new ConnectionProperties
                                {
                                    ConnectionName = $"TempImport_{fileExtension}",
                                    FilePath = sourceFile,
                                    FileName = System.IO.Path.GetFileName(sourceFile),
                                    Ext = fileExtension
                                };
                                
                                // Get the appropriate handler for the file extension
                                var driver = editor.ConfigEditor.DataDriversClasses
                                    .FirstOrDefault(d => d.extensionstoHandle != null && 
                                                       d.extensionstoHandle.Split(',').Any(e => e.Trim().Equals(fileExtension, StringComparison.InvariantCultureIgnoreCase)));
                                
                                if (driver == null)
                                {
                                    AnsiConsole.MarkupLine($"[red]✗[/] No handler found for file extension: {fileExtension}[/]");
                                    return;
                                }
                                
                                sourceDs = editor.CreateLocalDataSourceConnection(fileConn, $"TempImport_{fileExtension}", driver.classHandler);
                                if (sourceDs == null)
                                {
                                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to create data source for file");
                                    return;
                                }
                            }
                            
                            var destDataSource = CliHelper.ValidateAndGetDataSource(editor, destDs);
                            if (destDataSource == null) return;
                            
                            ctx.Status($"Reading source structure...");
                            
                            // Get source entities
                            var sourceEntities = sourceDs.GetEntitesList();
                            if (sourceEntities == null || !sourceEntities.Any())
                            {
                                AnsiConsole.MarkupLine($"[red]✗[/] No entities found in source file");
                                return;
                            }
                            
                            var sourceEntity = sourceEntities.First();
                            var sourceStructure = sourceDs.GetEntityStructure(sourceEntity, false);
                            
                            ctx.Status($"Preparing destination...");
                            
                            // Create table if requested
                            if (createTable)
                            {
                                var createResult = destDataSource.CreateEntityAs(sourceStructure);
                                if (!createResult)
                                {
                                    AnsiConsole.MarkupLine($"[yellow]![/] Warning: Failed to create destination table (may already exist)");
                                }
                            }
                            
                            var destStructure = destDataSource.GetEntityStructure(destEntity, true);
                            if (destStructure == null)
                            {
                                AnsiConsole.MarkupLine($"[red]✗[/] Destination entity '{destEntity}' not found");
                                return;
                            }
                            
                            ctx.Status($"Reading source data...");
                            
                            // Read all data
                            var sourceData = sourceDs.GetEntity(sourceEntity, null);
                            if (sourceData == null)
                            {
                                AnsiConsole.MarkupLine($"[red]✗[/] Failed to read source data");
                                return;
                            }
                            
                            var dataRows = sourceData.ToList();
                            int totalRows = dataRows.Count;
                            
                            if (totalRows == 0)
                            {
                                AnsiConsole.MarkupLine($"[yellow]![/] No data rows found in source file");
                                return;
                            }
                            
                            AnsiConsole.MarkupLine($"[cyan]Total rows to import:[/] {totalRows}");
                            
                            ctx.Status($"Importing data...");
                            
                            // Import with progress
                            int imported = 0;
                            int failed = 0;
                            var errors = new List<string>();
                            
                            AnsiConsole.Progress()
                                .Start(progressCtx =>
                                {
                                    var task = progressCtx.AddTask("[cyan]Importing records[/]", maxValue: totalRows);
                                    
                                    for (int i = 0; i < totalRows; i += batchSize)
                                    {
                                        var batch = dataRows.Skip(i).Take(batchSize).ToList();
                                        
                                        foreach (var row in batch)
                                        {
                                            try
                                            {
                                                // Create entity object - map from source to destination
                                                var entity = new Dictionary<string, object>();
                                                
                                                var rowType = row.GetType();
                                                foreach (var field in destStructure.Fields)
                                                {
                                                    try
                                                    {
                                                        // Try to get property value from the row object
                                                        var prop = rowType.GetProperty(field.fieldname, 
                                                            System.Reflection.BindingFlags.Public | 
                                                            System.Reflection.BindingFlags.Instance | 
                                                            System.Reflection.BindingFlags.IgnoreCase);
                                                        
                                                        if (prop != null)
                                                        {
                                                            entity[field.fieldname] = prop.GetValue(row);
                                                        }
                                                    }
                                                    catch
                                                    {
                                                        // Skip fields that can't be mapped
                                                    }
                                                }
                                                
                                                var result = destDataSource.InsertEntity(destEntity, entity);
                                                
                                                if (result != null)
                                                {
                                                    imported++;
                                                }
                                                else
                                                {
                                                    failed++;
                                                    if (errors.Count < 10)
                                                    {
                                                        errors.Add($"Row {i + imported + failed}: Insert failed");
                                                    }
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                failed++;
                                                if (errors.Count < 10)
                                                {
                                                    errors.Add($"Row {i + imported + failed}: {ex.Message}");
                                                }
                                            }
                                            
                                            task.Increment(1);
                                        }
                                    }
                                    
                                    task.StopTask();
                                });
                            
                            AnsiConsole.WriteLine();
                            AnsiConsole.MarkupLine($"[green]✓[/] Import completed");
                            AnsiConsole.MarkupLine($"[cyan]Imported:[/] {imported} rows");
                            
                            if (failed > 0)
                            {
                                AnsiConsole.MarkupLine($"[red]Failed:[/] {failed} rows");
                                
                                if (errors.Any())
                                {
                                    AnsiConsole.WriteLine();
                                    AnsiConsole.MarkupLine("[yellow]Sample errors:[/]");
                                    foreach (var error in errors)
                                    {
                                        AnsiConsole.MarkupLine($"  [dim]{error}[/]");
                                    }
                                }
                            }
                        });
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        AnsiConsole.MarkupLine($"[dim]{ex.InnerException.Message}[/]");
                    }
                }
            }, sourceFileArg, destDsArg, destEntityArg, mappingOption, createTableOption, batchSizeOption, skipHeaderOption, profileOption);

            // ============ VALIDATE FILE ============
            
            // import validate
            var validateCommand = new Command("validate", "Validate file structure against destination");
            var valFileArg = new Argument<string>("source-file", "Path to source file");
            var valDsArg = new Argument<string>("dest-datasource", "Destination data source name");
            var valEntityArg = new Argument<string>("dest-entity", "Destination entity name");
            
            validateCommand.AddArgument(valFileArg);
            validateCommand.AddArgument(valDsArg);
            validateCommand.AddArgument(valEntityArg);
            validateCommand.AddOption(profileOption);
            
            validateCommand.SetHandler((string sourceFile, string destDs, string destEntity, string profile) =>
            {
                var services = new BeepServiceProvider(profile);
                var editor = services.GetEditor();
                
                try
                {
                    if (!System.IO.File.Exists(sourceFile))
                    {
                        AnsiConsole.MarkupLine($"[red]✗[/] Source file not found: {sourceFile}");
                        return;
                    }
                    
                    AnsiConsole.Status()
                        .Start("Validating file structure...", ctx =>
                        {
                            // Get or create source data source
                            var fileExtension = System.IO.Path.GetExtension(sourceFile).TrimStart('.');
                            var sourceDs = editor.GetDataSource($"TempValidate_{fileExtension}");
                            
                            if (sourceDs == null)
                            {
                                var fileConn = new ConnectionProperties
                                {
                                    ConnectionName = $"TempValidate_{fileExtension}",
                                    FilePath = sourceFile,
                                    FileName = System.IO.Path.GetFileName(sourceFile),
                                    Ext = fileExtension
                                };
                                
                                // Get the appropriate handler for the file extension
                                var driver = editor.ConfigEditor.DataDriversClasses
                                    .FirstOrDefault(d => d.extensionstoHandle != null && 
                                                       d.extensionstoHandle.Split(',').Any(e => e.Trim().Equals(fileExtension, StringComparison.InvariantCultureIgnoreCase)));
                                
                                if (driver != null)
                                {
                                    sourceDs = editor.CreateLocalDataSourceConnection(fileConn, $"TempValidate_{fileExtension}", driver.classHandler);
                                }
                            }
                            
                            if (sourceDs == null)
                            {
                                AnsiConsole.MarkupLine($"[red]✗[/] Failed to read source file");
                                return;
                            }
                            
                            var destDataSource = CliHelper.ValidateAndGetDataSource(editor, destDs);
                            if (destDataSource == null) return;
                            
                            // Get structures
                            var sourceEntities = sourceDs.GetEntitesList();
                            if (sourceEntities == null || !sourceEntities.Any())
                            {
                                AnsiConsole.MarkupLine($"[red]✗[/] No entities found in source file");
                                return;
                            }
                            
                            var sourceEntity = sourceEntities.First();
                            var sourceStructure = sourceDs.GetEntityStructure(sourceEntity, false);
                            var destStructure = destDataSource.GetEntityStructure(destEntity, false);
                            
                            if (destStructure == null)
                            {
                                AnsiConsole.MarkupLine($"[red]✗[/] Destination entity '{destEntity}' not found");
                                return;
                            }
                            
                            // Compare structures
                            var table = new Table();
                            table.Border = TableBorder.Rounded;
                            table.Title = new TableTitle("[bold cyan]Structure Validation[/]");
                            table.AddColumn("Field Name");
                            table.AddColumn("Source Type");
                            table.AddColumn("Dest Type");
                            table.AddColumn("Status");
                            
                            var matchCount = 0;
                            var mismatchCount = 0;
                            var missingCount = 0;
                            
                            foreach (var sourceField in sourceStructure.Fields)
                            {
                                var destField = destStructure.Fields.FirstOrDefault(f => f.fieldname.Equals(sourceField.fieldname, StringComparison.InvariantCultureIgnoreCase));
                                
                                if (destField == null)
                                {
                                    table.AddRow(
                                        sourceField.fieldname,
                                        sourceField.fieldtype ?? "N/A",
                                        "[dim]N/A[/]",
                                        "[red]Missing[/]"
                                    );
                                    missingCount++;
                                }
                                else if (sourceField.fieldtype != destField.fieldtype)
                                {
                                    table.AddRow(
                                        sourceField.fieldname,
                                        sourceField.fieldtype ?? "N/A",
                                        destField.fieldtype ?? "N/A",
                                        "[yellow]Type Diff[/]"
                                    );
                                    mismatchCount++;
                                }
                                else
                                {
                                    table.AddRow(
                                        sourceField.fieldname,
                                        sourceField.fieldtype ?? "N/A",
                                        destField.fieldtype ?? "N/A",
                                        "[green]Match[/]"
                                    );
                                    matchCount++;
                                }
                            }
                            
                            AnsiConsole.Write(table);
                            
                            AnsiConsole.WriteLine();
                            AnsiConsole.MarkupLine($"[green]Matched:[/] {matchCount}");
                            AnsiConsole.MarkupLine($"[yellow]Type Differences:[/] {mismatchCount}");
                            AnsiConsole.MarkupLine($"[red]Missing in Destination:[/] {missingCount}");
                            
                            if (missingCount == 0 && mismatchCount == 0)
                            {
                                AnsiConsole.MarkupLine($"[green]✓[/] Structure is compatible - ready to import");
                            }
                            else if (missingCount > 0)
                            {
                                AnsiConsole.MarkupLine($"[yellow]![/] Warning: Missing fields will be skipped during import");
                            }
                        });
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
                }
            }, valFileArg, valDsArg, valEntityArg, profileOption);

            importCommand.AddCommand(fileCommand);
            importCommand.AddCommand(validateCommand);

            return importCommand;
        }
    }
}
