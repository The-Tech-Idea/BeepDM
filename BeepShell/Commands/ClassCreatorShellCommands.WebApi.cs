using System;
using System.CommandLine;
using System.Collections.Generic;
using System.Linq;
using Spectre.Console;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;

namespace BeepShell.Commands
{
    /// <summary>
    /// Web API generation commands for ClassCreatorShellCommands
    /// </summary>
    public partial class ClassCreatorShellCommands
    {
        /// <summary>
        /// Builds Web API related commands
        /// </summary>
        private void AddWebApiCommands(Command classCommand)
        {
            // class webapi - Generate Web API controllers
            var webApiCommand = new Command("webapi", "Generate Web API controllers for entities");
            
            var dsArg = new Argument<string>("datasource", "Data source name");
            var outputOption = new Option<string>("--output", "Output directory path") { IsRequired = true };
            var namespaceOption = new Option<string>("--namespace", () => "TheTechIdea.ProjectControllers", "Namespace for controllers");
            var allTablesOption = new Option<bool>("--all", () => false, "Generate controllers for all tables");
            var tablesOption = new Option<string[]>("--tables", "Specific tables to generate controllers for");

            webApiCommand.AddArgument(dsArg);
            webApiCommand.AddOption(outputOption);
            webApiCommand.AddOption(namespaceOption);
            webApiCommand.AddOption(allTablesOption);
            webApiCommand.AddOption(tablesOption);

            webApiCommand.SetHandler((datasource, output, ns, all, tables) =>
            {
                GenerateWebApiControllers(datasource, output, ns, all, tables);
            }, dsArg, outputOption, namespaceOption, allTablesOption, tablesOption);

            classCommand.AddCommand(webApiCommand);

            // class minimal-api - Generate Minimal API
            var minimalApiCommand = new Command("minimal-api", "Generate .NET Minimal API");
            var minimalOutputOption = new Option<string>("--output", "Output file path") { IsRequired = true };
            var minimalNsOption = new Option<string>("--namespace", () => "TheTechIdea.ProjectMinimalAPI", "Namespace");

            minimalApiCommand.AddOption(minimalOutputOption);
            minimalApiCommand.AddOption(minimalNsOption);

            minimalApiCommand.SetHandler((output, ns) =>
            {
                GenerateMinimalApi(output, ns);
            }, minimalOutputOption, minimalNsOption);

            classCommand.AddCommand(minimalApiCommand);

            // class api-param - Generate parameterized API controller
            var apiParamCommand = new Command("api-param", "Generate Web API controller with datasource/entity parameters");
            var classNameArg = new Argument<string>("classname", "Controller class name");
            var paramOutputOption = new Option<string>("--output", "Output file path") { IsRequired = true };
            var paramNsOption = new Option<string>("--namespace", () => "TheTechIdea.ProjectControllers", "Namespace");

            apiParamCommand.AddArgument(classNameArg);
            apiParamCommand.AddOption(paramOutputOption);
            apiParamCommand.AddOption(paramNsOption);

            apiParamCommand.SetHandler((className, output, ns) =>
            {
                GenerateParameterizedApiController(className, output, ns);
            }, classNameArg, paramOutputOption, paramNsOption);

            classCommand.AddCommand(apiParamCommand);
        }

        /// <summary>
        /// Generates Web API controllers for entities
        /// </summary>
        private void GenerateWebApiControllers(string datasourceName, string outputDir, string namespaceName, bool generateAll, string[] tables)
        {
            try
            {
                var ds = _editor.GetDataSource(datasourceName);
                if (ds == null)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Data source '{datasourceName}' not found");
                    return;
                }

                if (ds.ConnectionStatus != System.Data.ConnectionState.Open)
                {
                    _editor.OpenDataSource(datasourceName);
                }

                var entities = new List<EntityStructure>();
                var entitiesToProcess = new List<string>();

                if (generateAll)
                {
                    var allEntities = ds.GetEntitesList()?.ToList();
                    if (allEntities != null)
                        entitiesToProcess.AddRange(allEntities);
                }
                else if (tables != null && tables.Length > 0)
                {
                    entitiesToProcess.AddRange(tables);
                }
                else
                {
                    AnsiConsole.MarkupLine("[yellow]Please specify --all or --tables option[/]");
                    return;
                }

                AnsiConsole.Status()
                    .Start("Loading entity structures...", ctx =>
                    {
                        foreach (var tableName in entitiesToProcess)
                        {
                            ctx.Status($"Loading {tableName}...");
                            var structure = ds.GetEntityStructure(tableName, false);
                            if (structure != null)
                            {
                                entities.Add(structure);
                            }
                        }
                    });

                if (entities.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No entities found to generate[/]");
                    return;
                }

                AnsiConsole.MarkupLine($"[cyan]Generating Web API controllers for {entities.Count} entities...[/]");

                var generatedFiles = _classCreator.GenerateWebApiControllers(datasourceName, entities, outputDir, namespaceName);

                if (generatedFiles != null && generatedFiles.Count > 0)
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] Generated {generatedFiles.Count} controller(s):");
                    foreach (var file in generatedFiles)
                    {
                        AnsiConsole.MarkupLine($"  [dim]→[/] {file}");
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine("[yellow]No controllers were generated[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
                if (_editor.ErrorObject.Flag == Errors.Failed)
                {
                    AnsiConsole.MarkupLine($"[dim]{_editor.ErrorObject.Message}[/]");
                }
            }
        }

        /// <summary>
        /// Generates a .NET Minimal API
        /// </summary>
        private void GenerateMinimalApi(string outputPath, string namespaceName)
        {
            try
            {
                AnsiConsole.Status()
                    .Start("Generating Minimal API...", ctx =>
                    {
                        var result = _classCreator.GenerateMinimalWebApi(outputPath, namespaceName);
                        
                        if (!string.IsNullOrEmpty(result))
                        {
                            AnsiConsole.MarkupLine($"[green]✓[/] Minimal API generated: {result}");
                        }
                        else
                        {
                            AnsiConsole.MarkupLine("[yellow]Minimal API generation returned no result[/]");
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates a parameterized Web API controller
        /// </summary>
        private void GenerateParameterizedApiController(string className, string outputPath, string namespaceName)
        {
            try
            {
                AnsiConsole.Status()
                    .Start($"Generating {className} controller...", ctx =>
                    {
                        var result = _classCreator.GenerateWebApiControllerForEntityWithParams(className, outputPath, namespaceName);
                        
                        if (!string.IsNullOrEmpty(result))
                        {
                            AnsiConsole.MarkupLine($"[green]✓[/] Controller generated: {result}");
                        }
                        else
                        {
                            AnsiConsole.MarkupLine("[yellow]Controller generation returned no result[/]");
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }
    }
}
