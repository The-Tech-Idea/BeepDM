using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Text;
using BeepShell.Infrastructure;
using Spectre.Console;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Tools;

namespace BeepShell.Commands
{
    /// <summary>
    /// Class creator commands for BeepShell - Core functionality
    /// Modular architecture using partial classes matching ClassCreator design
    /// </summary>
    public partial class ClassCreatorShellCommands : IShellCommand
    {
        private IDMEEditor _editor = null!;
        private ClassCreator _classCreator = null!;

        public string CommandName => "class";
        public string Description => "Generate C# classes from database entities";
        public string Category => "Tools";
        public string Version => "1.0.0";
        public string Author => "BeepDM Team";
        public string[] Aliases => new[] { "codegen" };

        public void Initialize(IDMEEditor editor)
        {
            _editor = editor;
            _classCreator = new ClassCreator(editor);
        }

        public Command BuildCommand()
        {
            var classCommand = new Command("class", Description);

            // Add core commands
            AddCoreCommands(classCommand);
            
            // Add Web API commands
            AddWebApiCommands(classCommand);
            
            // Add Database commands
            AddDatabaseCommands(classCommand);
            
            // Add Advanced commands
            AddAdvancedCommands(classCommand);
            
            // Add DLL creation commands
            AddDllCommands(classCommand);
            
            // Add Testing commands
            AddTestingCommands(classCommand);

            return classCommand;
        }

        /// <summary>
        /// Builds core class generation commands
        /// </summary>
        private void AddCoreCommands(Command classCommand)
        {
            // class generate - Generate single POCO class
            var generateCommand = new Command("generate", "Generate POCO class from table");
            var dsArg = new Argument<string>("datasource", "Data source name");
            var tableArg = new Argument<string>("table", "Table name");
            var outputOption = new Option<string>("--output", "Output file path");
            var namespaceOption = new Option<string>("--namespace", () => "TheTechIdea.ProjectClasses", "Namespace for generated class");
            var publicOption = new Option<bool>("--public", () => true, "Generate public class");

            generateCommand.AddArgument(dsArg);
            generateCommand.AddArgument(tableArg);
            generateCommand.AddOption(outputOption);
            generateCommand.AddOption(namespaceOption);
            generateCommand.AddOption(publicOption);

            generateCommand.SetHandler((datasource, table, output, ns, isPublic) =>
            {
                GeneratePocoClass(datasource, table, output, ns, isPublic);
            }, dsArg, tableArg, outputOption, namespaceOption, publicOption);

            classCommand.AddCommand(generateCommand);

            // class batch - Generate multiple classes
            var batchCommand = new Command("batch", "Generate POCO classes for all tables");
            var batchDsArg = new Argument<string>("datasource", "Data source name");
            var batchOutputOption = new Option<string>("--output", "Output directory path") { IsRequired = true };
            var batchNsOption = new Option<string>("--namespace", () => "TheTechIdea.ProjectClasses", "Namespace for generated classes");

            batchCommand.AddArgument(batchDsArg);
            batchCommand.AddOption(batchOutputOption);
            batchCommand.AddOption(batchNsOption);

            batchCommand.SetHandler((datasource, output, ns) =>
            {
                GenerateBatchPocoClasses(datasource, output, ns);
            }, batchDsArg, batchOutputOption, batchNsOption);

            classCommand.AddCommand(batchCommand);

            // class inotify - Generate INotifyPropertyChanged class
            var inotifyCommand = new Command("inotify", "Generate INotifyPropertyChanged class");
            var inotifyDsArg = new Argument<string>("datasource", "Data source name");
            var inotifyTableArg = new Argument<string>("table", "Table name");
            var inotifyOutputOption = new Option<string>("--output", "Output file path") { IsRequired = true };
            var inotifyNsOption = new Option<string>("--namespace", () => "TheTechIdea.ProjectClasses", "Namespace");

            inotifyCommand.AddArgument(inotifyDsArg);
            inotifyCommand.AddArgument(inotifyTableArg);
            inotifyCommand.AddOption(inotifyOutputOption);
            inotifyCommand.AddOption(inotifyNsOption);

            inotifyCommand.SetHandler((datasource, table, output, ns) =>
            {
                GenerateINotifyClass(datasource, table, output, ns);
            }, inotifyDsArg, inotifyTableArg, inotifyOutputOption, inotifyNsOption);

            classCommand.AddCommand(inotifyCommand);

            // class entity - Generate Entity class with full features
            var entityCommand = new Command("entity", "Generate Entity class with validation and metadata");
            var entityDsArg = new Argument<string>("datasource", "Data source name");
            var entityTableArg = new Argument<string>("table", "Table name");
            var entityOutputOption = new Option<string>("--output", "Output file path") { IsRequired = true };
            var entityNsOption = new Option<string>("--namespace", () => "TheTechIdea.ProjectEntities", "Namespace");

            entityCommand.AddArgument(entityDsArg);
            entityCommand.AddArgument(entityTableArg);
            entityCommand.AddOption(entityOutputOption);
            entityCommand.AddOption(entityNsOption);

            entityCommand.SetHandler((datasource, table, output, ns) =>
            {
                GenerateEntityClass(datasource, table, output, ns);
            }, entityDsArg, entityTableArg, entityOutputOption, entityNsOption);

            classCommand.AddCommand(entityCommand);
        }

        #region Core Generation Methods

        /// <summary>
        /// Generates a POCO class
        /// </summary>
        private void GeneratePocoClass(string datasourceName, string tableName, string outputPath, string namespaceName, bool isPublic)
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

                var structure = ds.GetEntityStructure(tableName, false);
                if (structure == null || structure.Fields == null || structure.Fields.Count == 0)
                {
                    AnsiConsole.MarkupLine($"[yellow]Could not retrieve structure for '{tableName}'[/]");
                    return;
                }

                var className = SanitizeClassName(tableName);
                
                // Use ClassCreator to generate POCO class
                var code = _classCreator.CreatePOCOClass(className, structure, string.Empty, string.Empty, 
                    string.Empty, outputPath ?? string.Empty, namespaceName, !string.IsNullOrEmpty(outputPath));

                if (string.IsNullOrEmpty(outputPath))
                {
                    // Display to console
                    AnsiConsole.WriteLine();
                    var panel = new Panel(code);
                    panel.Header = new PanelHeader($"[green]{className}.cs[/]");
                    panel.Border = BoxBorder.Rounded;
                    AnsiConsole.Write(panel);
                }
                else
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] POCO class generated: {outputPath}");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates POCO classes for all tables in batch
        /// </summary>
        private void GenerateBatchPocoClasses(string datasourceName, string outputDir, string namespaceName)
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

                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                    AnsiConsole.MarkupLine($"[cyan]Created directory: {outputDir}[/]");
                }

                var entities = ds.GetEntitesList()?.ToList();
                if (entities == null || entities.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No entities found[/]");
                    return;
                }

                AnsiConsole.MarkupLine($"[cyan]Generating classes for {entities.Count} entities...[/]");

                var progress = AnsiConsole.Progress();
                progress.Start(ctx =>
                {
                    var task = ctx.AddTask("[cyan]Generating POCO classes[/]", maxValue: entities.Count);

                    foreach (var entityName in entities)
                    {
                        try
                        {
                            task.Description = $"[cyan]Generating {entityName}[/]";

                            var structure = ds.GetEntityStructure(entityName, false);
                            if (structure?.Fields != null && structure.Fields.Count > 0)
                            {
                                var className = SanitizeClassName(entityName);
                                _classCreator.CreatePOCOClass(className, structure, string.Empty, string.Empty, 
                                    string.Empty, outputDir, namespaceName, true);
                            }

                            task.Increment(1);
                        }
                        catch (Exception ex)
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] Error generating {entityName}: {ex.Message}");
                        }
                    }
                });

                AnsiConsole.MarkupLine($"[green]✓[/] Generated POCO classes in: {outputDir}");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates INotifyPropertyChanged class
        /// </summary>
        private void GenerateINotifyClass(string datasourceName, string tableName, string outputPath, string namespaceName)
        {
            try
            {
                var structure = GetEntityStructure(datasourceName, tableName);
                if (structure == null) return;

                AnsiConsole.Status()
                    .Start($"Generating INotify class for {tableName}...", ctx =>
                    {
                        var code = _classCreator.CreateINotifyClass(structure, string.Empty, string.Empty, 
                            string.Empty, outputPath, namespaceName, true);
                        
                        if (!string.IsNullOrEmpty(code))
                        {
                            AnsiConsole.MarkupLine($"[green]✓[/] INotifyPropertyChanged class generated: {outputPath}");
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates full Entity class
        /// </summary>
        private void GenerateEntityClass(string datasourceName, string tableName, string outputPath, string namespaceName)
        {
            try
            {
                var structure = GetEntityStructure(datasourceName, tableName);
                if (structure == null) return;

                AnsiConsole.Status()
                    .Start($"Generating Entity class for {tableName}...", ctx =>
                    {
                        var code = _classCreator.CreateEntityClass(structure, string.Empty, string.Empty, 
                            outputPath, namespaceName, true);
                        
                        if (!string.IsNullOrEmpty(code))
                        {
                            AnsiConsole.MarkupLine($"[green]✓[/] Entity class generated: {outputPath}");
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets a single entity structure from datasource
        /// </summary>
        private EntityStructure? GetEntityStructure(string datasourceName, string tableName)
        {
            var ds = _editor.GetDataSource(datasourceName);
            if (ds == null)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Data source '{datasourceName}' not found");
                return null;
            }

            if (ds.ConnectionStatus != System.Data.ConnectionState.Open)
            {
                _editor.OpenDataSource(datasourceName);
            }

            var structure = ds.GetEntityStructure(tableName, false);
            if (structure == null || structure.Fields == null || structure.Fields.Count == 0)
            {
                AnsiConsole.MarkupLine($"[yellow]Could not retrieve structure for '{tableName}'[/]");
                return null;
            }

            return structure;
        }

        /// <summary>
        /// Gets multiple entity structures from datasource
        /// </summary>
        private List<EntityStructure> GetEntityStructures(string datasourceName, bool getAll, string[] tableNames)
        {
            var entities = new List<EntityStructure>();
            var ds = _editor.GetDataSource(datasourceName);
            
            if (ds == null)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Data source '{datasourceName}' not found");
                return entities;
            }

            if (ds.ConnectionStatus != System.Data.ConnectionState.Open)
            {
                _editor.OpenDataSource(datasourceName);
            }

            var entitiesToProcess = new List<string>();

            if (getAll)
            {
                var allEntities = ds.GetEntitesList()?.ToList();
                if (allEntities != null)
                    entitiesToProcess.AddRange(allEntities);
            }
            else if (tableNames != null && tableNames.Length > 0)
            {
                entitiesToProcess.AddRange(tableNames);
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]Please specify --all or --tables option[/]");
                return entities;
            }

            AnsiConsole.Status()
                .Start("Loading entity structures...", ctx =>
                {
                    foreach (var tableName in entitiesToProcess)
                    {
                        ctx.Status($"Loading {tableName}...");
                        var structure = ds.GetEntityStructure(tableName, false);
                        if (structure != null && structure.Fields != null && structure.Fields.Count > 0)
                        {
                            entities.Add(structure);
                        }
                    }
                });

            return entities;
        }

        private string SanitizeClassName(string name)
        {
            // Remove invalid characters and ensure valid C# identifier
            var sanitized = new string(name.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
            if (sanitized.Length > 0 && char.IsDigit(sanitized[0]))
                sanitized = "_" + sanitized;
            return sanitized;
        }

        #endregion

        #region IShellCommand Implementation

        public bool CanExecute() => _editor != null;

        public string[] GetExamples()
        {
            return new[]
            {
                // Core commands
                "class generate mydb Users --output User.cs",
                "class batch mydb --output ./Models --namespace MyApp.Models",
                "class inotify mydb Products --output ProductNotify.cs",
                "class entity mydb Orders --output Order.cs --namespace MyApp.Entities",
                
                // Web API commands
                "class webapi mydb --output ./Controllers --all",
                "class webapi mydb --output ./Controllers --tables Users,Products",
                "class minimal-api --output Program.cs",
                "class api-param DynamicController --output ./Controllers",
                
                // Database commands
                "class dal mydb Users --output ./DAL",
                "class dbcontext mydb --output ./Data --namespace MyApp.Data --all",
                "class ef-config mydb Products --output ./Configurations",
                "class repository mydb Orders --output ./Repositories",
                "class migration mydb Users --output ./Migrations",
                
                // Advanced commands
                "class docs mydb Users --output UserDocs.xml",
                "class blazor mydb Products --output ./Components",
                "class graphql mydb --output schema.graphql --all",
                "class grpc mydb Orders --output ./Grpc",
                "class diff mydb Users UserV2 --output changes.md",
                
                // DLL commands
                "class dll MyProject mydb --output ./bin --all",
                "class dll MyProject mydb --output ./bin --tables Users,Products",
                "class dll-from-path MyClasses ./src/Models --output ./bin",
                
                // Testing commands
                "class test mydb Users --output ./Tests",
                "class validator mydb Products --output ./Validators"
            };
        }

        #endregion
    }
}
