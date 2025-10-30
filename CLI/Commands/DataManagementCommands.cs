using System;
using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;
using Spectre.Console;
using TheTechIdea.Beep.CLI.Infrastructure;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.CLI.Commands
{
    /// <summary>
    /// Enhanced data management commands
    /// Provides comprehensive data operations and schema management
    /// </summary>
    public static class DataManagementCommands
    {
        public static Command Build()
        {
            var dmCommand = new Command("dm", "Enhanced data management operations");

            var profileOption = new Option<string>("--profile", () => ProfileManager.DEFAULT_PROFILE, "Profile to use");

            // ============ SCHEMA INFO ============
            
            var schemaInfoCommand = new Command("schema", "Display detailed schema information for an entity");
            var dsArg = new Argument<string>("datasource", "Data source name");
            var entityArg = new Argument<string>("entity", "Entity name");
            var showIndexesOpt = new Option<bool>("--indexes", () => true, "Show indexes");
            var showRelationsOpt = new Option<bool>("--relations", () => true, "Show relations");
            
            schemaInfoCommand.AddArgument(dsArg);
            schemaInfoCommand.AddArgument(entityArg);
            schemaInfoCommand.AddOption(showIndexesOpt);
            schemaInfoCommand.AddOption(showRelationsOpt);
            schemaInfoCommand.AddOption(profileOption);
            
            schemaInfoCommand.SetHandler((string ds, string entity, bool showIndexes, bool showRelations, string profile) =>
            {
                try
                {
                    var services = new BeepServiceProvider(profile);
                    var editor = services.GetEditor();
                    
                    var dataSource = CliHelper.ValidateAndGetDataSource(editor, ds);
                    if (dataSource == null) return;

                    var entityStructure = CliHelper.ValidateAndGetEntity(dataSource, entity);
                    if (entityStructure == null) return;

                    // Display entity header
                    var panel = new Panel(new Markup($"[bold cyan]{entity}[/]"))
                    {
                        Border = BoxBorder.Double,
                        Padding = new Padding(1, 0)
                    };
                    AnsiConsole.Write(panel);

                    // Display fields table
                    var table = new Table();
                    table.Border = TableBorder.Rounded;
                    table.Title = new TableTitle("[bold]Fields[/]");
                    table.AddColumn("Field Name");
                    table.AddColumn("Data Type");
                    table.AddColumn("Nullable");
                    table.AddColumn("Key");
                    table.AddColumn("Size");
                    
                    foreach (var field in entityStructure.Fields)
                    {
                        var isKey = field.IsKey ? "[green]✓[/]" : "";
                        var isNullable = field.AllowDBNull ? "[yellow]Yes[/]" : "[dim]No[/]";
                        var size = field.Size1 > 0 ? field.Size1.ToString() : "-";
                        
                        table.AddRow(
                            $"[cyan]{field.fieldname}[/]",
                            field.fieldtype,
                            isNullable,
                            isKey,
                            size
                        );
                    }
                    
                    AnsiConsole.Write(table);

                    // Show primary keys
                    var pkFields = entityStructure.Fields.Where(f => f.IsKey).ToList();
                    if (pkFields.Any())
                    {
                        AnsiConsole.MarkupLine($"\n[bold]Primary Keys:[/] [cyan]{string.Join(", ", pkFields.Select(f => f.fieldname))}[/]");
                    }

                    // Show statistics
                    AnsiConsole.MarkupLine($"\n[bold]Statistics:[/]");
                    AnsiConsole.MarkupLine($"  Total Fields: [cyan]{entityStructure.Fields.Count}[/]");
                    AnsiConsole.MarkupLine($"  Nullable Fields: [yellow]{entityStructure.Fields.Count(f => f.AllowDBNull)}[/]");
                    AnsiConsole.MarkupLine($"  Key Fields: [green]{pkFields.Count}[/]");

                    // Show relations if requested
                    if (showRelations && entityStructure.Relations != null && entityStructure.Relations.Count > 0)
                    {
                        var relTable = new Table();
                        relTable.Border = TableBorder.Rounded;
                        relTable.Title = new TableTitle("\n[bold]Relations[/]");
                        relTable.AddColumn("Relation Name");
                        relTable.AddColumn("Related Entity");
                        relTable.AddColumn("Relation Type");
                        
                        foreach (var rel in entityStructure.Relations)
                        {
                            relTable.AddRow(
                                rel.RalationName ?? "-",
                                rel.RelatedEntityID ?? "-",
                                rel.EntityColumnID ?? "-"
                            );
                        }
                        
                        AnsiConsole.Write(relTable);
                    }
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
                }
            }, dsArg, entityArg, showIndexesOpt, showRelationsOpt, profileOption);

            // ============ LIST ENTITIES WITH DETAILS ============
            
            var listEntitiesCommand = new Command("list-entities", "List all entities with details");
            var listDsArg = new Argument<string>("datasource", "Data source name");
            var showCountOpt = new Option<bool>("--count", () => false, "Show record count for each entity");
            
            listEntitiesCommand.AddArgument(listDsArg);
            listEntitiesCommand.AddOption(showCountOpt);
            listEntitiesCommand.AddOption(profileOption);
            
            listEntitiesCommand.SetHandler(async (string ds, bool showCount, string profile) =>
            {
                await AnsiConsole.Status()
                    .StartAsync($"Loading entities from '{ds}'...", async ctx =>
                    {
                        try
                        {
                            var services = new BeepServiceProvider(profile);
                            var editor = services.GetEditor();
                            
                            var dataSource = CliHelper.ValidateAndGetDataSource(editor, ds);
                            if (dataSource == null) return;

                            var entities = dataSource.GetEntitesList();
                            if (entities == null || entities.Count() == 0)
                            {
                                AnsiConsole.MarkupLine($"[yellow]⚠[/] No entities found in '{ds}'");
                                return;
                            }

                            var table = new Table();
                            table.Border = TableBorder.Rounded;
                            table.Title = new TableTitle($"[bold cyan]Entities in {ds}[/]");
                            table.AddColumn("#");
                            table.AddColumn("Entity Name");
                            table.AddColumn("Fields");
                            
                            if (showCount)
                                table.AddColumn("Record Count");

                            int index = 1;
                            foreach (var entityName in entities)
                            {
                                ctx.Status($"Loading {entityName}...");
                                
                                var entity = dataSource.GetEntityStructure(entityName, false);
                                var fieldCount = entity?.Fields?.Count ?? 0;
                                
                                if (showCount)
                                {
                                    try
                                    {
                                        // Get record count using GetEntity
                                        var entityData = dataSource.GetEntity(entityName, null);
                                        var count = entityData?.Count() ?? 0;
                                        table.AddRow(
                                            index.ToString(),
                                            $"[cyan]{entityName}[/]",
                                            fieldCount.ToString(),
                                            $"[green]{count:N0}[/]"
                                        );
                                    }
                                    catch
                                    {
                                        table.AddRow(
                                            index.ToString(),
                                            $"[cyan]{entityName}[/]",
                                            fieldCount.ToString(),
                                            "[dim]N/A[/]"
                                        );
                                    }
                                }
                                else
                                {
                                    table.AddRow(
                                        index.ToString(),
                                        $"[cyan]{entityName}[/]",
                                        fieldCount.ToString()
                                    );
                                }
                                
                                index++;
                            }
                            
                            AnsiConsole.Write(table);
                            AnsiConsole.MarkupLine($"\n[bold]Total Entities:[/] [cyan]{entities.Count()}[/]");
                        }
                        catch (Exception ex)
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
                        }
                    });
            }, listDsArg, showCountOpt, profileOption);

            // ============ EXPORT SCHEMA ============
            
            var exportSchemaCommand = new Command("export-schema", "Export schema definition to JSON");
            var expDsArg = new Argument<string>("datasource", "Data source name");
            var expOutputArg = new Argument<string>("output", "Output file path");
            var expEntitiesOpt = new Option<string[]>("--entities", "Specific entities (default: all)");
            
            exportSchemaCommand.AddArgument(expDsArg);
            exportSchemaCommand.AddArgument(expOutputArg);
            exportSchemaCommand.AddOption(expEntitiesOpt);
            exportSchemaCommand.AddOption(profileOption);
            
            exportSchemaCommand.SetHandler(async (string ds, string output, string[] entities, string profile) =>
            {
                await AnsiConsole.Status()
                    .StartAsync($"Exporting schema from '{ds}'...", async ctx =>
                    {
                        try
                        {
                            var services = new BeepServiceProvider(profile);
                            var editor = services.GetEditor();
                            
                            var dataSource = CliHelper.ValidateAndGetDataSource(editor, ds);
                            if (dataSource == null) return;

                            var entitiesToExport = entities?.ToList() ?? dataSource.GetEntitesList();
                            var schemaInfo = new System.Collections.Generic.Dictionary<string, object>();

                            foreach (var entityName in entitiesToExport)
                            {
                                ctx.Status($"Processing {entityName}...");
                                var entity = dataSource.GetEntityStructure(entityName, true);
                                if (entity != null)
                                {
                                    schemaInfo[entityName] = new
                                    {
                                        EntityName = entity.EntityName,
                                        Fields = entity.Fields.Select(f => new
                                        {
                                            f.fieldname,
                                            f.fieldtype,
                                            f.Size1,
                                            f.AllowDBNull,
                                            f.IsKey,
                                            f.IsAutoIncrement
                                        }).ToList(),
                                        Relations = entity.Relations?.Select(r => new
                                        {
                                            r.RalationName,
                                            r.RelatedEntityID,
                                            r.EntityColumnID
                                        }).ToList()
                                    };
                                }
                            }

                            var json = Newtonsoft.Json.JsonConvert.SerializeObject(schemaInfo, Newtonsoft.Json.Formatting.Indented);
                            System.IO.File.WriteAllText(output, json);

                            AnsiConsole.MarkupLine($"[green]✓[/] Schema exported to: [cyan]{output}[/]");
                            AnsiConsole.MarkupLine($"  Entities exported: [cyan]{schemaInfo.Count}[/]");
                        }
                        catch (Exception ex)
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
                        }
                    });
            }, expDsArg, expOutputArg, expEntitiesOpt, profileOption);

            // ============ COMPARE SCHEMAS ============
            
            var compareSchemaCommand = new Command("compare-schemas", "Compare schemas between two data sources");
            var cmpDs1Arg = new Argument<string>("datasource1", "First data source");
            var cmpDs2Arg = new Argument<string>("datasource2", "Second data source");
            
            compareSchemaCommand.AddArgument(cmpDs1Arg);
            compareSchemaCommand.AddArgument(cmpDs2Arg);
            compareSchemaCommand.AddOption(profileOption);
            
            compareSchemaCommand.SetHandler(async (string ds1, string ds2, string profile) =>
            {
                await AnsiConsole.Status()
                    .StartAsync($"Comparing schemas...", async ctx =>
                    {
                        try
                        {
                            var services = new BeepServiceProvider(profile);
                            var editor = services.GetEditor();
                            
                            var dataSource1 = CliHelper.ValidateAndGetDataSource(editor, ds1);
                            var dataSource2 = CliHelper.ValidateAndGetDataSource(editor, ds2);
                            
                            if (dataSource1 == null || dataSource2 == null) return;

                            var entities1 = dataSource1.GetEntitesList().ToHashSet();
                            var entities2 = dataSource2.GetEntitesList().ToHashSet();

                            var common = entities1.Intersect(entities2).ToList();
                            var onlyIn1 = entities1.Except(entities2).ToList();
                            var onlyIn2 = entities2.Except(entities1).ToList();

                            // Summary
                            var summaryTable = new Table();
                            summaryTable.Border = TableBorder.Rounded;
                            summaryTable.Title = new TableTitle("[bold]Schema Comparison Summary[/]");
                            summaryTable.AddColumn("Category");
                            summaryTable.AddColumn("Count");
                            
                            summaryTable.AddRow("[cyan]Common Entities[/]", $"[green]{common.Count}[/]");
                            summaryTable.AddRow($"[yellow]Only in {ds1}[/]", $"[yellow]{onlyIn1.Count}[/]");
                            summaryTable.AddRow($"[yellow]Only in {ds2}[/]", $"[yellow]{onlyIn2.Count}[/]");
                            
                            AnsiConsole.Write(summaryTable);

                            if (onlyIn1.Any())
                            {
                                AnsiConsole.MarkupLine($"\n[bold yellow]Entities only in {ds1}:[/]");
                                foreach (var entity in onlyIn1)
                                    AnsiConsole.MarkupLine($"  [yellow]•[/] {entity}");
                            }

                            if (onlyIn2.Any())
                            {
                                AnsiConsole.MarkupLine($"\n[bold yellow]Entities only in {ds2}:[/]");
                                foreach (var entity in onlyIn2)
                                    AnsiConsole.MarkupLine($"  [yellow]•[/] {entity}");
                            }
                        }
                        catch (Exception ex)
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
                        }
                    });
            }, cmpDs1Arg, cmpDs2Arg, profileOption);

            // ============ STATISTICS ============
            
            var statsCommand = new Command("stats", "Show statistics for a data source");
            var statsDsArg = new Argument<string>("datasource", "Data source name");
            
            statsCommand.AddArgument(statsDsArg);
            statsCommand.AddOption(profileOption);
            
            statsCommand.SetHandler(async (string ds, string profile) =>
            {
                await AnsiConsole.Status()
                    .StartAsync($"Collecting statistics for '{ds}'...", async ctx =>
                    {
                        try
                        {
                            var services = new BeepServiceProvider(profile);
                            var editor = services.GetEditor();
                            
                            var dataSource = CliHelper.ValidateAndGetDataSource(editor, ds);
                            if (dataSource == null) return;

                            var entities = dataSource.GetEntitesList();
                            int totalFields = 0;
                            int totalRelations = 0;

                            foreach (var entityName in entities)
                            {
                                ctx.Status($"Analyzing {entityName}...");
                                var entity = dataSource.GetEntityStructure(entityName, true);
                                if (entity != null)
                                {
                                    totalFields += entity.Fields?.Count ?? 0;
                                    totalRelations += entity.Relations?.Count ?? 0;
                                }
                            }

                            var statsTable = new Table();
                            statsTable.Border = TableBorder.Rounded;
                            statsTable.Title = new TableTitle($"[bold cyan]Statistics for {ds}[/]");
                            statsTable.AddColumn("Metric");
                            statsTable.AddColumn("Value");
                            
                            statsTable.AddRow("[cyan]Total Entities[/]", $"[green]{entities.Count()}[/]");
                            statsTable.AddRow("[cyan]Total Fields[/]", $"[green]{totalFields}[/]");
                            statsTable.AddRow("[cyan]Total Relations[/]", $"[green]{totalRelations}[/]");
                            statsTable.AddRow("[cyan]Avg Fields per Entity[/]", $"[green]{(entities.Count() > 0 ? totalFields / entities.Count() : 0)}[/]");
                            
                            AnsiConsole.Write(statsTable);
                        }
                        catch (Exception ex)
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
                        }
                    });
            }, statsDsArg, profileOption);

            // Add all subcommands
            dmCommand.AddCommand(schemaInfoCommand);
            dmCommand.AddCommand(listEntitiesCommand);
            dmCommand.AddCommand(exportSchemaCommand);
            dmCommand.AddCommand(compareSchemaCommand);
            dmCommand.AddCommand(statsCommand);

            return dmCommand;
        }
    }
}

