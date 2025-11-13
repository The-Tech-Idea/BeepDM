using System;
using System.CommandLine;
using System.Collections.Generic;
using System.Linq;
using Spectre.Console;

namespace BeepShell.Commands
{
    /// <summary>
    /// Advanced generation commands (Documentation, UI, Serverless) for ClassCreatorShellCommands
    /// </summary>
    public partial class ClassCreatorShellCommands
    {
        /// <summary>
        /// Builds advanced generation commands
        /// </summary>
        private void AddAdvancedCommands(Command classCommand)
        {
            // class docs - Generate documentation
            var docsCommand = new Command("docs", "Generate XML documentation for entity");
            var docsDsArg = new Argument<string>("datasource", "Data source name");
            var docsTableArg = new Argument<string>("table", "Table name");
            var docsOutputOption = new Option<string>("--output", "Output file path") { IsRequired = true };

            docsCommand.AddArgument(docsDsArg);
            docsCommand.AddArgument(docsTableArg);
            docsCommand.AddOption(docsOutputOption);

            docsCommand.SetHandler((datasource, table, output) =>
            {
                GenerateDocumentation(datasource, table, output);
            }, docsDsArg, docsTableArg, docsOutputOption);

            classCommand.AddCommand(docsCommand);

            // class blazor - Generate Blazor component
            var blazorCommand = new Command("blazor", "Generate Blazor component for entity");
            var blazorDsArg = new Argument<string>("datasource", "Data source name");
            var blazorTableArg = new Argument<string>("table", "Table name");
            var blazorOutputOption = new Option<string>("--output", "Output directory path") { IsRequired = true };
            var blazorNsOption = new Option<string>("--namespace", () => "TheTechIdea.ProjectComponents", "Namespace");

            blazorCommand.AddArgument(blazorDsArg);
            blazorCommand.AddArgument(blazorTableArg);
            blazorCommand.AddOption(blazorOutputOption);
            blazorCommand.AddOption(blazorNsOption);

            blazorCommand.SetHandler((datasource, table, output, ns) =>
            {
                GenerateBlazorComponent(datasource, table, output, ns);
            }, blazorDsArg, blazorTableArg, blazorOutputOption, blazorNsOption);

            classCommand.AddCommand(blazorCommand);

            // class graphql - Generate GraphQL schema
            var graphqlCommand = new Command("graphql", "Generate GraphQL schema for entities");
            var gqlDsArg = new Argument<string>("datasource", "Data source name");
            var gqlOutputOption = new Option<string>("--output", "Output file path") { IsRequired = true };
            var gqlNsOption = new Option<string>("--namespace", () => "TheTechIdea.ProjectGraphQL", "Namespace");
            var gqlAllOption = new Option<bool>("--all", () => false, "Include all tables");
            var gqlTablesOption = new Option<string[]>("--tables", "Specific tables to include");

            graphqlCommand.AddArgument(gqlDsArg);
            graphqlCommand.AddOption(gqlOutputOption);
            graphqlCommand.AddOption(gqlNsOption);
            graphqlCommand.AddOption(gqlAllOption);
            graphqlCommand.AddOption(gqlTablesOption);

            graphqlCommand.SetHandler((datasource, output, ns, all, tables) =>
            {
                GenerateGraphQLSchema(datasource, output, ns, all, tables);
            }, gqlDsArg, gqlOutputOption, gqlNsOption, gqlAllOption, gqlTablesOption);

            classCommand.AddCommand(graphqlCommand);

            // class grpc - Generate gRPC service
            var grpcCommand = new Command("grpc", "Generate gRPC service for entity");
            var grpcDsArg = new Argument<string>("datasource", "Data source name");
            var grpcTableArg = new Argument<string>("table", "Table name");
            var grpcOutputOption = new Option<string>("--output", "Output directory path") { IsRequired = true };
            var grpcNsOption = new Option<string>("--namespace", () => "TheTechIdea.ProjectGrpc", "Namespace");

            grpcCommand.AddArgument(grpcDsArg);
            grpcCommand.AddArgument(grpcTableArg);
            grpcCommand.AddOption(grpcOutputOption);
            grpcCommand.AddOption(grpcNsOption);

            grpcCommand.SetHandler((datasource, table, output, ns) =>
            {
                GenerateGrpcService(datasource, table, output, ns);
            }, grpcDsArg, grpcTableArg, grpcOutputOption, grpcNsOption);

            classCommand.AddCommand(grpcCommand);

            // class diff - Generate entity difference report
            var diffCommand = new Command("diff", "Generate entity difference report");
            var diffDsArg = new Argument<string>("datasource", "Data source name");
            var diffTable1Arg = new Argument<string>("table1", "First table name");
            var diffTable2Arg = new Argument<string>("table2", "Second table name");
            var diffOutputOption = new Option<string>("--output", "Output file path");

            diffCommand.AddArgument(diffDsArg);
            diffCommand.AddArgument(diffTable1Arg);
            diffCommand.AddArgument(diffTable2Arg);
            diffCommand.AddOption(diffOutputOption);

            diffCommand.SetHandler((datasource, table1, table2, output) =>
            {
                GenerateDiffReport(datasource, table1, table2, output);
            }, diffDsArg, diffTable1Arg, diffTable2Arg, diffOutputOption);

            classCommand.AddCommand(diffCommand);
        }

        /// <summary>
        /// Generates XML documentation
        /// </summary>
        private void GenerateDocumentation(string datasourceName, string tableName, string outputPath)
        {
            try
            {
                var structure = GetEntityStructure(datasourceName, tableName);
                if (structure == null) return;

                AnsiConsole.Status()
                    .Start($"Generating documentation for {tableName}...", ctx =>
                    {
                        var result = _classCreator.GenerateEntityDocumentation(structure, outputPath);
                        
                        if (!string.IsNullOrEmpty(result))
                        {
                            AnsiConsole.MarkupLine($"[green]✓[/] Documentation generated: {result}");
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates Blazor component
        /// </summary>
        private void GenerateBlazorComponent(string datasourceName, string tableName, string outputDir, string namespaceName)
        {
            try
            {
                var structure = GetEntityStructure(datasourceName, tableName);
                if (structure == null) return;

                AnsiConsole.Status()
                    .Start($"Generating Blazor component for {tableName}...", ctx =>
                    {
                        var result = _classCreator.GenerateBlazorComponent(structure, outputDir, namespaceName);
                        
                        if (!string.IsNullOrEmpty(result))
                        {
                            AnsiConsole.MarkupLine($"[green]✓[/] Blazor component generated: {result}");
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates GraphQL schema
        /// </summary>
        private void GenerateGraphQLSchema(string datasourceName, string outputPath, string namespaceName, bool generateAll, string[] tables)
        {
            try
            {
                var entities = GetEntityStructures(datasourceName, generateAll, tables);
                if (entities.Count == 0) return;

                AnsiConsole.Status()
                    .Start($"Generating GraphQL schema for {entities.Count} entities...", ctx =>
                    {
                        var result = _classCreator.GenerateGraphQLSchema(entities, outputPath, namespaceName);
                        
                        if (!string.IsNullOrEmpty(result))
                        {
                            AnsiConsole.MarkupLine($"[green]✓[/] GraphQL schema generated: {result}");
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates gRPC service
        /// </summary>
        private void GenerateGrpcService(string datasourceName, string tableName, string outputDir, string namespaceName)
        {
            try
            {
                var structure = GetEntityStructure(datasourceName, tableName);
                if (structure == null) return;

                AnsiConsole.Status()
                    .Start($"Generating gRPC service for {tableName}...", ctx =>
                    {
                        var (protoFile, serviceImpl) = _classCreator.GenerateGrpcService(structure, outputDir, namespaceName);
                        
                        if (!string.IsNullOrEmpty(protoFile) && !string.IsNullOrEmpty(serviceImpl))
                        {
                            AnsiConsole.MarkupLine($"[green]✓[/] gRPC files generated:");
                            AnsiConsole.MarkupLine($"  [dim]→[/] Proto: {protoFile}");
                            AnsiConsole.MarkupLine($"  [dim]→[/] Service: {serviceImpl}");
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates entity difference report
        /// </summary>
        private void GenerateDiffReport(string datasourceName, string table1Name, string table2Name, string outputPath)
        {
            try
            {
                var structure1 = GetEntityStructure(datasourceName, table1Name);
                var structure2 = GetEntityStructure(datasourceName, table2Name);
                
                if (structure1 == null || structure2 == null) return;

                AnsiConsole.Status()
                    .Start($"Generating diff report between {table1Name} and {table2Name}...", ctx =>
                    {
                        var result = _classCreator.GenerateEntityDiffReport(structure1, structure2);
                        
                        if (string.IsNullOrEmpty(outputPath))
                        {
                            // Display to console
                            var panel = new Panel(result);
                            panel.Header = new PanelHeader($"[yellow]Difference Report[/]");
                            panel.Border = BoxBorder.Rounded;
                            AnsiConsole.Write(panel);
                        }
                        else
                        {
                            System.IO.File.WriteAllText(outputPath, result);
                            AnsiConsole.MarkupLine($"[green]✓[/] Diff report generated: {outputPath}");
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
