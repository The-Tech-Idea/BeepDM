using System;
using System.CommandLine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using TheTechIdea.Beep.CLI.Infrastructure;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Addin;

namespace TheTechIdea.Beep.CLI.Commands
{
    /// <summary>
    /// Class Creator commands for code generation
    /// Provides comprehensive class generation capabilities
    /// </summary>
    public static partial class ClassCreatorCommands
    {
        public static Command Build()
        {
            var classCommand = new Command("class", "Class generation and code creation tools");

            var profileOption = new Option<string>("--profile", () => ProfileManager.DEFAULT_PROFILE, "Profile to use");

            // ============ GENERATE POCO CLASS ============
            
            var generatePocoCommand = new Command("generate-poco", "Generate POCO class from entity");
            var dsNameArg = new Argument<string>("datasource", "Data source name");
            var entityNameArg = new Argument<string>("entity", "Entity name");
            var outputPathOpt = new Option<string>("--output", () => Environment.CurrentDirectory, "Output directory");
            var namespaceOpt = new Option<string>("--namespace", () => "TheTechIdea.ProjectClasses", "Namespace for generated class");
            
            generatePocoCommand.AddArgument(dsNameArg);
            generatePocoCommand.AddArgument(entityNameArg);
            generatePocoCommand.AddOption(outputPathOpt);
            generatePocoCommand.AddOption(namespaceOpt);
            generatePocoCommand.AddOption(profileOption);
            
            generatePocoCommand.SetHandler(async (string dsName, string entityName, string output, string ns, string profile) =>
            {
                await AnsiConsole.Status()
                    .StartAsync($"Generating POCO class for '{entityName}'...", async ctx =>
                    {
                        try
                        {
                            var services = new BeepServiceProvider(profile);
                            var editor = services.GetEditor();
                            var classCreator = new ClassCreator(editor);
                            
                            var ds = CliHelper.ValidateAndGetDataSource(editor, dsName);
                            if (ds == null) return;

                            var entity = CliHelper.ValidateAndGetEntity(ds, entityName);
                            if (entity == null) return;

                            var filePath = classCreator.CreatePOCOClass(
                                entityName, 
                                entity, 
                                "using System;\nusing System.Collections.Generic;", 
                                "", 
                                "", 
                                output, 
                                ns, 
                                true
                            );

                            CliHelper.DisplaySuccess($"POCO class generated: {filePath}");
                        }
                        catch (Exception ex)
                        {
                            CliHelper.DisplayError($"Error: {ex.Message}");
                        }
                    });
            }, dsNameArg, entityNameArg, outputPathOpt, namespaceOpt, profileOption);

            // ============ GENERATE POCO CLASS (BATCH) ============
            
            var generatePocoBatchCommand = new Command("generate-poco-batch", "Generate POCO classes from multiple entities");
            var pocoBatchDsArg = new Argument<string>("datasource", "Data source name");
            var pocoBatchOutputOpt = new Option<string>("--output", () => Environment.CurrentDirectory, "Output directory");
            var pocoBatchNsOpt = new Option<string>("--namespace", () => "TheTechIdea.ProjectClasses", "Namespace for generated classes");
            var pocoBatchEntitiesOpt = new Option<string[]>("--entities", "Specific entities (default: all)");
            var pocoBatchClassNameOpt = new Option<string>("--classname", () => "GeneratedClasses", "Base class name prefix");
            
            generatePocoBatchCommand.AddArgument(pocoBatchDsArg);
            generatePocoBatchCommand.AddOption(pocoBatchOutputOpt);
            generatePocoBatchCommand.AddOption(pocoBatchNsOpt);
            generatePocoBatchCommand.AddOption(pocoBatchEntitiesOpt);
            generatePocoBatchCommand.AddOption(pocoBatchClassNameOpt);
            generatePocoBatchCommand.AddOption(profileOption);
            
            generatePocoBatchCommand.SetHandler(async (string dsName, string output, string ns, string[] entityNames, string className, string profile) =>
            {
                await AnsiConsole.Status()
                    .StartAsync($"Generating POCO classes from multiple entities...", async ctx =>
                    {
                        try
                        {
                            var services = new BeepServiceProvider(profile);
                            var editor = services.GetEditor();
                            var classCreator = new ClassCreator(editor);
                            
                            var ds = CliHelper.ValidateAndGetDataSource(editor, dsName);
                            if (ds == null) return;

                            // Get entities
                            List<EntityStructure> entities = new List<EntityStructure>();
                            
                            if (entityNames != null && entityNames.Length > 0)
                            {
                                // Specific entities
                                foreach (var entityName in entityNames)
                                {
                                    var entity = CliHelper.ValidateAndGetEntity(ds, entityName);
                                    if (entity == null) return;
                                    entities.Add(entity);
                                }
                            }
                            else
                            {
                                // All entities
                                var allEntityNames = ds.GetEntitesList();
                                foreach (var entityName in allEntityNames)
                                {
                                    var entity = ds.GetEntityStructure(entityName, true);
                                    if (entity != null)
                                        entities.Add(entity);
                                }
                            }

                            if (entities.Count == 0)
                            {
                                CliHelper.DisplayWarning("No entities found to generate");
                                return;
                            }

                            var filePath = classCreator.CreatePOCOClass(
                                className, 
                                entities, 
                                "using System;\nusing System.Collections.Generic;", 
                                "", 
                                "", 
                                output, 
                                ns, 
                                true
                            );

                            CliHelper.DisplaySuccess($"Generated {entities.Count} POCO classes: {filePath}");
                            AnsiConsole.MarkupLine($"  [cyan]Classes:[/] {string.Join(", ", entities.Select(e => e.EntityName))}");
                        }
                        catch (Exception ex)
                        {
                            CliHelper.DisplayError($"Error: {ex.Message}");
                        }
                    });
            }, pocoBatchDsArg, pocoBatchOutputOpt, pocoBatchNsOpt, pocoBatchEntitiesOpt, pocoBatchClassNameOpt, profileOption);

            // ============ GENERATE WEB API CONTROLLER ============
            
            var generateWebApiCommand = new Command("generate-webapi", "Generate Web API controllers for entities");
            var webDsNameArg = new Argument<string>("datasource", "Data source name");
            var webOutputPathOpt = new Option<string>("--output", () => Environment.CurrentDirectory, "Output directory");
            var webNamespaceOpt = new Option<string>("--namespace", () => "TheTechIdea.ProjectControllers", "Namespace for controller");
            var webEntitiesOpt = new Option<string[]>("--entities", "Specific entities (default: all)");
            
            generateWebApiCommand.AddArgument(webDsNameArg);
            generateWebApiCommand.AddOption(webOutputPathOpt);
            generateWebApiCommand.AddOption(webNamespaceOpt);
            generateWebApiCommand.AddOption(webEntitiesOpt);
            generateWebApiCommand.AddOption(profileOption);
            
            generateWebApiCommand.SetHandler(async (string dsName, string output, string ns, string[] entityNames, string profile) =>
            {
                await AnsiConsole.Status()
                    .StartAsync($"Generating Web API controllers...", async ctx =>
                    {
                        try
                        {
                            var services = new BeepServiceProvider(profile);
                            var editor = services.GetEditor();
                            var classCreator = new ClassCreator(editor);
                            
                            var ds = CliHelper.ValidateAndGetDataSource(editor, dsName);
                            if (ds == null) return;

                            // Get entities
                            List<EntityStructure> entities = new List<EntityStructure>();
                            
                            if (entityNames != null && entityNames.Length > 0)
                            {
                                // Specific entities
                                foreach (var entityName in entityNames)
                                {
                                    var entity = CliHelper.ValidateAndGetEntity(ds, entityName);
                                    if (entity == null) return;
                                    entities.Add(entity);
                                }
                            }
                            else
                            {
                                // All entities
                                var allEntityNames = ds.GetEntitesList();
                                foreach (var entityName in allEntityNames)
                                {
                                    var entity = ds.GetEntityStructure(entityName, true);
                                    if (entity != null)
                                        entities.Add(entity);
                                }
                            }

                            if (entities.Count == 0)
                            {
                                CliHelper.DisplayError("No entities found");
                                return;
                            }

                            var filePaths = classCreator.GenerateWebApiControllers(
                                dsName,
                                entities,
                                output,
                                ns
                            );

                            CliHelper.DisplaySuccess($"Generated {filePaths.Count} Web API controller(s):");
                            foreach (var path in filePaths)
                            {
                                AnsiConsole.MarkupLine($"  [cyan]{path}[/]");
                            }
                        }
                        catch (Exception ex)
                        {
                            CliHelper.DisplayError($"Error: {ex.Message}");
                        }
                    });
            }, webDsNameArg, webOutputPathOpt, webNamespaceOpt, webEntitiesOpt, profileOption);

            // ============ GENERATE DB CONTEXT ============
            
            var generateDbContextCommand = new Command("generate-dbcontext", "Generate EF Core DbContext for data source");
            var dbCtxDsNameArg = new Argument<string>("datasource", "Data source name");
            var dbCtxOutputPathOpt = new Option<string>("--output", () => Environment.CurrentDirectory, "Output directory");
            var dbCtxNamespaceOpt = new Option<string>("--namespace", () => "TheTechIdea.ProjectData", "Namespace for DbContext");
            var dbCtxEntitiesOpt = new Option<string[]>("--entities", "Specific entities (default: all)");
            
            generateDbContextCommand.AddArgument(dbCtxDsNameArg);
            generateDbContextCommand.AddOption(dbCtxOutputPathOpt);
            generateDbContextCommand.AddOption(dbCtxNamespaceOpt);
            generateDbContextCommand.AddOption(dbCtxEntitiesOpt);
            generateDbContextCommand.AddOption(profileOption);
            
            generateDbContextCommand.SetHandler(async (string dsName, string output, string ns, string[] entities, string profile) =>
            {
                await AnsiConsole.Status()
                    .StartAsync($"Generating DbContext for '{dsName}'...", async ctx =>
                    {
                        try
                        {
                            var services = new BeepServiceProvider(profile);
                            var editor = services.GetEditor();
                            var classCreator = new ClassCreator(editor);
                            
                            var ds = CliHelper.ValidateAndGetDataSource(editor, dsName);
                            if (ds == null) return;

                            List<EntityStructure> entityStructures = new List<EntityStructure>();
                            
                            if (entities != null && entities.Length > 0)
                            {
                                foreach (var entityName in entities)
                                {
                                    var entity = ds.GetEntityStructure(entityName, true);
                                    if (entity != null)
                                        entityStructures.Add(entity);
                                }
                            }
                            else
                            {
                                // Get all entities
                                var entitiesList = ds.GetEntitesList();
                                foreach (var entityName in entitiesList)
                                {
                                    var entity = ds.GetEntityStructure(entityName, true);
                                    if (entity != null)
                                        entityStructures.Add(entity);
                                }
                            }

                            if (entityStructures.Count == 0)
                            {
                                CliHelper.DisplayWarning("No entities found");
                                return;
                            }

                            var filePath = classCreator.GenerateDbContext(entityStructures, ns, output);

                            CliHelper.DisplaySuccess($"DbContext generated with {entityStructures.Count} entities: {filePath}");
                        }
                        catch (Exception ex)
                        {
                            CliHelper.DisplayError($"Error: {ex.Message}");
                        }
                    });
            }, dbCtxDsNameArg, dbCtxOutputPathOpt, dbCtxNamespaceOpt, dbCtxEntitiesOpt, profileOption);

            // ============ GENERATE REPOSITORY ============
            
            var generateRepoCommand = new Command("generate-repository", "Generate repository pattern classes for entity");
            var repoDsNameArg = new Argument<string>("datasource", "Data source name");
            var repoEntityNameArg = new Argument<string>("entity", "Entity name");
            var repoOutputPathOpt = new Option<string>("--output", () => Environment.CurrentDirectory, "Output directory");
            var repoNamespaceOpt = new Option<string>("--namespace", () => "TheTechIdea.ProjectRepositories", "Namespace for repository");
            var repoInterfaceOnlyOpt = new Option<bool>("--interface-only", () => false, "Generate interface only");
            
            generateRepoCommand.AddArgument(repoDsNameArg);
            generateRepoCommand.AddArgument(repoEntityNameArg);
            generateRepoCommand.AddOption(repoOutputPathOpt);
            generateRepoCommand.AddOption(repoNamespaceOpt);
            generateRepoCommand.AddOption(repoInterfaceOnlyOpt);
            generateRepoCommand.AddOption(profileOption);
            
            generateRepoCommand.SetHandler(async (string dsName, string entityName, string output, string ns, bool interfaceOnly, string profile) =>
            {
                await AnsiConsole.Status()
                    .StartAsync($"Generating repository for '{entityName}'...", async ctx =>
                    {
                        try
                        {
                            var services = new BeepServiceProvider(profile);
                            var editor = services.GetEditor();
                            var classCreator = new ClassCreator(editor);
                            
                            var ds = CliHelper.ValidateAndGetDataSource(editor, dsName);
                            if (ds == null) return;

                            var entity = CliHelper.ValidateAndGetEntity(ds, entityName);
                            if (entity == null) return;

                            var filePath = classCreator.GenerateRepositoryImplementation(
                                entity,
                                output,
                                ns,
                                interfaceOnly
                            );

                            CliHelper.DisplaySuccess($"Repository generated: {filePath}");
                        }
                        catch (Exception ex)
                        {
                            CliHelper.DisplayError($"Error: {ex.Message}");
                        }
                    });
            }, repoDsNameArg, repoEntityNameArg, repoOutputPathOpt, repoNamespaceOpt, repoInterfaceOnlyOpt, profileOption);

            // ============ CREATE DLL ============
            
            var createDllCommand = new Command("create-dll", "Create compiled DLL from entities");
            var dllDsNameArg = new Argument<string>("datasource", "Data source name");
            var dllNameArg = new Argument<string>("dllname", "DLL name (without extension)");
            var dllOutputPathOpt = new Option<string>("--output", () => Environment.CurrentDirectory, "Output directory");
            var dllNamespaceOpt = new Option<string>("--namespace", () => "TheTechIdea.ProjectClasses", "Namespace for classes");
            var dllEntitiesOpt = new Option<string[]>("--entities", "Specific entities (default: all)");
            var dllGenerateCsOpt = new Option<bool>("--generate-cs", () => true, "Generate .cs files");
            
            createDllCommand.AddArgument(dllDsNameArg);
            createDllCommand.AddArgument(dllNameArg);
            createDllCommand.AddOption(dllOutputPathOpt);
            createDllCommand.AddOption(dllNamespaceOpt);
            createDllCommand.AddOption(dllEntitiesOpt);
            createDllCommand.AddOption(dllGenerateCsOpt);
            createDllCommand.AddOption(profileOption);
            
            createDllCommand.SetHandler(async (string dsName, string dllName, string output, string ns, string[] entities, bool generateCs, string profile) =>
            {
                await AnsiConsole.Progress()
                    .StartAsync(async ctx =>
                    {
                        var task = ctx.AddTask($"[green]Creating DLL '{dllName}'[/]");
                        
                        try
                        {
                            var services = new BeepServiceProvider(profile);
                            var editor = services.GetEditor();
                            var classCreator = new ClassCreator(editor);
                            
                            var ds = CliHelper.ValidateAndGetDataSource(editor, dsName);
                            if (ds == null) return;

                            List<EntityStructure> entityStructures = new List<EntityStructure>();
                            
                            if (entities != null && entities.Length > 0)
                            {
                                foreach (var entityName in entities)
                                {
                                    var entity = ds.GetEntityStructure(entityName, true);
                                    if (entity != null)
                                        entityStructures.Add(entity);
                                }
                            }
                            else
                            {
                                var entitiesList = ds.GetEntitesList();
                                foreach (var entityName in entitiesList)
                                {
                                    var entity = ds.GetEntityStructure(entityName, true);
                                    if (entity != null)
                                    {
                                        entityStructures.Add(entity);
                                        task.Increment(100.0 / entitiesList.Count());
                                    }
                                }
                            }

                            if (entityStructures.Count == 0)
                            {
                                CliHelper.DisplayWarning("No entities found");
                                return;
                            }

                            task.MaxValue = 100;
                            task.Value = 50;

                            var progress = new Progress<PassedArgs>(args =>
                            {
                                if (!string.IsNullOrEmpty(args.Messege))
                                    task.Description = $"[dim]{args.Messege}[/]";
                            });

                            var filePath = classCreator.CreateDLL(
                                dllName,
                                entityStructures,
                                output,
                                progress,
                                CancellationToken.None,
                                ns,
                                generateCs
                            );

                            task.Value = 100;
                            task.StopTask();

                            CliHelper.DisplaySuccess($"DLL created with {entityStructures.Count} entities: {filePath}");
                        }
                        catch (Exception ex)
                        {
                            task.StopTask();
                            CliHelper.DisplayError($"Error: {ex.Message}");
                        }
                    });
            }, dllDsNameArg, dllNameArg, dllOutputPathOpt, dllNamespaceOpt, dllEntitiesOpt, dllGenerateCsOpt, profileOption);

            // ============ GENERATE DATA ACCESS LAYER ============
            
            var generateDalCommand = new Command("generate-dal", "Generate data access layer for entity");
            var dalDsNameArg = new Argument<string>("datasource", "Data source name");
            var dalEntityNameArg = new Argument<string>("entity", "Entity name");
            var dalOutputPathOpt = new Option<string>("--output", () => Environment.CurrentDirectory, "Output directory");
            
            generateDalCommand.AddArgument(dalDsNameArg);
            generateDalCommand.AddArgument(dalEntityNameArg);
            generateDalCommand.AddOption(dalOutputPathOpt);
            generateDalCommand.AddOption(profileOption);
            
            generateDalCommand.SetHandler(async (string dsName, string entityName, string output, string profile) =>
            {
                await AnsiConsole.Status()
                    .StartAsync($"Generating DAL for '{entityName}'...", async ctx =>
                    {
                        try
                        {
                            var services = new BeepServiceProvider(profile);
                            var editor = services.GetEditor();
                            var classCreator = new ClassCreator(editor);
                            
                            var ds = CliHelper.ValidateAndGetDataSource(editor, dsName);
                            if (ds == null) return;

                            var entity = CliHelper.ValidateAndGetEntity(ds, entityName);
                            if (entity == null) return;

                            var filePath = classCreator.GenerateDataAccessLayer(entity, output);

                            CliHelper.DisplaySuccess($"Data Access Layer generated: {filePath}");
                        }
                        catch (Exception ex)
                        {
                            CliHelper.DisplayError($"Error: {ex.Message}");
                        }
                    });
            }, dalDsNameArg, dalEntityNameArg, dalOutputPathOpt, profileOption);

            // ============ GENERATE MINIMAL API ============
            
            var generateMinimalApiCommand = new Command("generate-minimal-api", "Generate minimal Web API template (ASP.NET Core Minimal API pattern)");
            var minApiOutputPathOpt = new Option<string>("--output", () => Environment.CurrentDirectory, "Output directory");
            var minApiNamespaceOpt = new Option<string>("--namespace", () => "TheTechIdea.ProjectMinimalAPI", "Namespace for API");
            
            generateMinimalApiCommand.AddOption(minApiOutputPathOpt);
            generateMinimalApiCommand.AddOption(minApiNamespaceOpt);
            generateMinimalApiCommand.AddOption(profileOption);
            
            generateMinimalApiCommand.SetHandler(async (string output, string ns, string profile) =>
            {
                await AnsiConsole.Status()
                    .StartAsync("Generating minimal API template...", async ctx =>
                    {
                        try
                        {
                            var services = new BeepServiceProvider(profile);
                            var editor = services.GetEditor();
                            var classCreator = new ClassCreator(editor);

                            var filePath = classCreator.GenerateMinimalWebApi(output, ns);
                            
                            CliHelper.DisplaySuccess($"Minimal API template generated: {filePath}");
                            AnsiConsole.MarkupLine($"  [yellow]Note:[/] This is a template. Customize with your datasource and entities.");
                        }
                        catch (Exception ex)
                        {
                            CliHelper.DisplayError($"Error: {ex.Message}");
                        }
                    });
            }, minApiOutputPathOpt, minApiNamespaceOpt, profileOption);

            // ============ VALIDATE ENTITY ============
            
            var validateEntityCommand = new Command("validate-entity", "Validate entity structure for class generation");
            var valDsNameArg = new Argument<string>("datasource", "Data source name");
            var valEntityNameArg = new Argument<string>("entity", "Entity name");
            
            validateEntityCommand.AddArgument(valDsNameArg);
            validateEntityCommand.AddArgument(valEntityNameArg);
            validateEntityCommand.AddOption(profileOption);
            
            validateEntityCommand.SetHandler((string dsName, string entityName, string profile) =>
            {
                try
                {
                    var services = new BeepServiceProvider(profile);
                    var editor = services.GetEditor();
                    var classCreator = new ClassCreator(editor);
                    
                    var ds = CliHelper.ValidateAndGetDataSource(editor, dsName);
                    if (ds == null) return;

                    var entity = CliHelper.ValidateAndGetEntity(ds, entityName);
                    if (entity == null) return;

                    var validationErrors = classCreator.ValidateEntityStructure(entity);

                    if (validationErrors == null || validationErrors.Count == 0)
                    {
                        CliHelper.DisplaySuccess($"Entity '{entityName}' is valid for code generation");
                    }
                    else
                    {
                        CliHelper.DisplayWarning("Validation issues found:");
                        foreach (var error in validationErrors)
                        {
                            AnsiConsole.MarkupLine($"  [red]•[/] {error}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    CliHelper.DisplayError($"Error: {ex.Message}");
                }
            }, valDsNameArg, valEntityNameArg, profileOption);

            // ============ GENERATE RECORD CLASS (Modern C#) ============
            
            var generateRecordCommand = new Command("generate-record", "Generate C# record class (modern immutable type)");
            var recDsNameArg = new Argument<string>("datasource", "Data source name");
            var recEntityNameArg = new Argument<string>("entity", "Entity name");
            var recOutputPathOpt = new Option<string>("--output", () => Environment.CurrentDirectory, "Output directory");
            var recNamespaceOpt = new Option<string>("--namespace", () => "TheTechIdea.ProjectClasses", "Namespace");
            
            generateRecordCommand.AddArgument(recDsNameArg);
            generateRecordCommand.AddArgument(recEntityNameArg);
            generateRecordCommand.AddOption(recOutputPathOpt);
            generateRecordCommand.AddOption(recNamespaceOpt);
            generateRecordCommand.AddOption(profileOption);
            
            generateRecordCommand.SetHandler(async (string dsName, string entityName, string output, string ns, string profile) =>
            {
                await AnsiConsole.Status()
                    .StartAsync($"Generating record class for '{entityName}'...", async ctx =>
                    {
                        try
                        {
                            var services = new BeepServiceProvider(profile);
                            var editor = services.GetEditor();
                            var classCreator = new ClassCreator(editor);
                            
                            var ds = CliHelper.ValidateAndGetDataSource(editor, dsName);
                            if (ds == null) return;

                            var entity = CliHelper.ValidateAndGetEntity(ds, entityName);
                            if (entity == null) return;

                            var filePath = classCreator.CreateRecordClass(entityName, entity, output, ns);

                            CliHelper.DisplaySuccess($"Record class generated: {filePath}");
                        }
                        catch (Exception ex)
                        {
                            CliHelper.DisplayError($"Error: {ex.Message}");
                        }
                    });
            }, recDsNameArg, recEntityNameArg, recOutputPathOpt, recNamespaceOpt, profileOption);

            // ============ GENERATE INOTIFY CLASS ============
            
            var generateINotifyCommand = new Command("generate-inotify", "Generate INotifyPropertyChanged class");
            var notDsNameArg = new Argument<string>("datasource", "Data source name");
            var notEntityNameArg = new Argument<string>("entity", "Entity name");
            var notOutputPathOpt = new Option<string>("--output", () => Environment.CurrentDirectory, "Output directory");
            var notNamespaceOpt = new Option<string>("--namespace", () => "TheTechIdea.ProjectClasses", "Namespace");
            
            generateINotifyCommand.AddArgument(notDsNameArg);
            generateINotifyCommand.AddArgument(notEntityNameArg);
            generateINotifyCommand.AddOption(notOutputPathOpt);
            generateINotifyCommand.AddOption(notNamespaceOpt);
            generateINotifyCommand.AddOption(profileOption);
            
            generateINotifyCommand.SetHandler(async (string dsName, string entityName, string output, string ns, string profile) =>
            {
                await AnsiConsole.Status()
                    .StartAsync($"Generating INotify class for '{entityName}'...", async ctx =>
                    {
                        try
                        {
                            var services = new BeepServiceProvider(profile);
                            var editor = services.GetEditor();
                            var classCreator = new ClassCreator(editor);
                            
                            var ds = CliHelper.ValidateAndGetDataSource(editor, dsName);
                            if (ds == null) return;

                            var entity = CliHelper.ValidateAndGetEntity(ds, entityName);
                            if (entity == null) return;

                            var filePath = classCreator.CreateINotifyClass(entity, "using System;\nusing System.ComponentModel;", "", "", output, ns);

                            CliHelper.DisplaySuccess($"INotifyPropertyChanged class generated: {filePath}");
                        }
                        catch (Exception ex)
                        {
                            CliHelper.DisplayError($"Error: {ex.Message}");
                        }
                    });
            }, notDsNameArg, notEntityNameArg, notOutputPathOpt, notNamespaceOpt, profileOption);

            // ============ GENERATE INOTIFY CLASS (BATCH) ============
            
            var generateINotifyBatchCommand = new Command("generate-inotify-batch", "Generate INotifyPropertyChanged classes from multiple entities");
            var notBatchDsArg = new Argument<string>("datasource", "Data source name");
            var notBatchOutputOpt = new Option<string>("--output", () => Environment.CurrentDirectory, "Output directory");
            var notBatchNsOpt = new Option<string>("--namespace", () => "TheTechIdea.ProjectClasses", "Namespace for generated classes");
            var notBatchEntitiesOpt = new Option<string[]>("--entities", "Specific entities (default: all)");
            
            generateINotifyBatchCommand.AddArgument(notBatchDsArg);
            generateINotifyBatchCommand.AddOption(notBatchOutputOpt);
            generateINotifyBatchCommand.AddOption(notBatchNsOpt);
            generateINotifyBatchCommand.AddOption(notBatchEntitiesOpt);
            generateINotifyBatchCommand.AddOption(profileOption);
            
            generateINotifyBatchCommand.SetHandler(async (string dsName, string output, string ns, string[] entityNames, string profile) =>
            {
                await AnsiConsole.Status()
                    .StartAsync($"Generating INotify classes from multiple entities...", async ctx =>
                    {
                        try
                        {
                            var services = new BeepServiceProvider(profile);
                            var editor = services.GetEditor();
                            var classCreator = new ClassCreator(editor);
                            
                            var ds = CliHelper.ValidateAndGetDataSource(editor, dsName);
                            if (ds == null) return;

                            // Get entities
                            List<EntityStructure> entities = new List<EntityStructure>();
                            
                            if (entityNames != null && entityNames.Length > 0)
                            {
                                // Specific entities
                                foreach (var entityName in entityNames)
                                {
                                    var entity = CliHelper.ValidateAndGetEntity(ds, entityName);
                                    if (entity == null) return;
                                    entities.Add(entity);
                                }
                            }
                            else
                            {
                                // All entities
                                var allEntityNames = ds.GetEntitesList();
                                foreach (var entityName in allEntityNames)
                                {
                                    var entity = ds.GetEntityStructure(entityName, true);
                                    if (entity != null)
                                        entities.Add(entity);
                                }
                            }

                            if (entities.Count == 0)
                            {
                                CliHelper.DisplayWarning("No entities found to generate");
                                return;
                            }

                            var filePath = classCreator.CreateINotifyClass(
                                entities, 
                                "using System;\nusing System.ComponentModel;", 
                                "", 
                                "", 
                                output, 
                                ns,
                                true
                            );

                            CliHelper.DisplaySuccess($"Generated {entities.Count} INotifyPropertyChanged classes: {filePath}");
                            AnsiConsole.MarkupLine($"  [cyan]Classes:[/] {string.Join(", ", entities.Select(e => e.EntityName))}");
                        }
                        catch (Exception ex)
                        {
                            CliHelper.DisplayError($"Error: {ex.Message}");
                        }
                    });
            }, notBatchDsArg, notBatchOutputOpt, notBatchNsOpt, notBatchEntitiesOpt, profileOption);

            // ============ GENERATE UNIT TESTS ============
            
            var generateTestCommand = new Command("generate-tests", "Generate unit test class");
            var testDsNameArg = new Argument<string>("datasource", "Data source name");
            var testEntityNameArg = new Argument<string>("entity", "Entity name");
            var testOutputPathOpt = new Option<string>("--output", () => Environment.CurrentDirectory, "Output directory");
            
            generateTestCommand.AddArgument(testDsNameArg);
            generateTestCommand.AddArgument(testEntityNameArg);
            generateTestCommand.AddOption(testOutputPathOpt);
            generateTestCommand.AddOption(profileOption);
            
            generateTestCommand.SetHandler(async (string dsName, string entityName, string output, string profile) =>
            {
                await AnsiConsole.Status()
                    .StartAsync($"Generating unit tests for '{entityName}'...", async ctx =>
                    {
                        try
                        {
                            var services = new BeepServiceProvider(profile);
                            var editor = services.GetEditor();
                            var classCreator = new ClassCreator(editor);
                            
                            var ds = CliHelper.ValidateAndGetDataSource(editor, dsName);
                            if (ds == null) return;

                            var entity = CliHelper.ValidateAndGetEntity(ds, entityName);
                            if (entity == null) return;

                            var filePath = classCreator.GenerateUnitTestClass(entity, output);

                            CliHelper.DisplaySuccess($"Unit test class generated: {filePath}");
                        }
                        catch (Exception ex)
                        {
                            CliHelper.DisplayError($"Error: {ex.Message}");
                        }
                    });
            }, testDsNameArg, testEntityNameArg, testOutputPathOpt, profileOption);

            // ============ GENERATE FLUENT VALIDATORS ============
            
            var generateValidatorCommand = new Command("generate-validator", "Generate FluentValidation validator");
            var valDsNameArg2 = new Argument<string>("datasource", "Data source name");
            var valEntityNameArg2 = new Argument<string>("entity", "Entity name");
            var valOutputPathOpt = new Option<string>("--output", () => Environment.CurrentDirectory, "Output directory");
            var valNamespaceOpt = new Option<string>("--namespace", () => "TheTechIdea.ProjectValidators", "Namespace");
            
            generateValidatorCommand.AddArgument(valDsNameArg2);
            generateValidatorCommand.AddArgument(valEntityNameArg2);
            generateValidatorCommand.AddOption(valOutputPathOpt);
            generateValidatorCommand.AddOption(valNamespaceOpt);
            generateValidatorCommand.AddOption(profileOption);
            
            generateValidatorCommand.SetHandler(async (string dsName, string entityName, string output, string ns, string profile) =>
            {
                await AnsiConsole.Status()
                    .StartAsync($"Generating FluentValidation validator for '{entityName}'...", async ctx =>
                    {
                        try
                        {
                            var services = new BeepServiceProvider(profile);
                            var editor = services.GetEditor();
                            var classCreator = new ClassCreator(editor);
                            
                            var ds = CliHelper.ValidateAndGetDataSource(editor, dsName);
                            if (ds == null) return;

                            var entity = CliHelper.ValidateAndGetEntity(ds, entityName);
                            if (entity == null) return;

                            var filePath = classCreator.GenerateFluentValidators(entity, output, ns);

                            CliHelper.DisplaySuccess($"Validator generated: {filePath}");
                        }
                        catch (Exception ex)
                        {
                            CliHelper.DisplayError($"Error: {ex.Message}");
                        }
                    });
            }, valDsNameArg2, valEntityNameArg2, valOutputPathOpt, valNamespaceOpt, profileOption);

            // ============ GENERATE BLAZOR COMPONENT ============
            
            var generateBlazorCommand = new Command("generate-blazor", "Generate Blazor component");
            var blazDsNameArg = new Argument<string>("datasource", "Data source name");
            var blazEntityNameArg = new Argument<string>("entity", "Entity name");
            var blazOutputPathOpt = new Option<string>("--output", () => Environment.CurrentDirectory, "Output directory");
            var blazNamespaceOpt = new Option<string>("--namespace", () => "TheTechIdea.ProjectComponents", "Namespace");
            
            generateBlazorCommand.AddArgument(blazDsNameArg);
            generateBlazorCommand.AddArgument(blazEntityNameArg);
            generateBlazorCommand.AddOption(blazOutputPathOpt);
            generateBlazorCommand.AddOption(blazNamespaceOpt);
            generateBlazorCommand.AddOption(profileOption);
            
            generateBlazorCommand.SetHandler(async (string dsName, string entityName, string output, string ns, string profile) =>
            {
                await AnsiConsole.Status()
                    .StartAsync($"Generating Blazor component for '{entityName}'...", async ctx =>
                    {
                        try
                        {
                            var services = new BeepServiceProvider(profile);
                            var editor = services.GetEditor();
                            var classCreator = new ClassCreator(editor);
                            
                            var ds = CliHelper.ValidateAndGetDataSource(editor, dsName);
                            if (ds == null) return;

                            var entity = CliHelper.ValidateAndGetEntity(ds, entityName);
                            if (entity == null) return;

                            var filePath = classCreator.GenerateBlazorComponent(entity, output, ns);

                            CliHelper.DisplaySuccess($"Blazor component generated: {filePath}");
                        }
                        catch (Exception ex)
                        {
                            CliHelper.DisplayError($"Error: {ex.Message}");
                        }
                    });
            }, blazDsNameArg, blazEntityNameArg, blazOutputPathOpt, blazNamespaceOpt, profileOption);

            // ============ GENERATE GRAPHQL SCHEMA ============
            
            var generateGraphQLCommand = new Command("generate-graphql", "Generate GraphQL schema");
            var gqlDsNameArg = new Argument<string>("datasource", "Data source name");
            var gqlOutputPathOpt = new Option<string>("--output", () => Environment.CurrentDirectory, "Output directory");
            var gqlNamespaceOpt = new Option<string>("--namespace", () => "TheTechIdea.ProjectGraphQL", "Namespace");
            var gqlEntitiesOpt = new Option<string[]>("--entities", "Specific entities (default: all)");
            
            generateGraphQLCommand.AddArgument(gqlDsNameArg);
            generateGraphQLCommand.AddOption(gqlOutputPathOpt);
            generateGraphQLCommand.AddOption(gqlNamespaceOpt);
            generateGraphQLCommand.AddOption(gqlEntitiesOpt);
            generateGraphQLCommand.AddOption(profileOption);
            
            generateGraphQLCommand.SetHandler(async (string dsName, string output, string ns, string[] entities, string profile) =>
            {
                await AnsiConsole.Status()
                    .StartAsync($"Generating GraphQL schema for '{dsName}'...", async ctx =>
                    {
                        try
                        {
                            var services = new BeepServiceProvider(profile);
                            var editor = services.GetEditor();
                            var classCreator = new ClassCreator(editor);
                            
                            var ds = CliHelper.ValidateAndGetDataSource(editor, dsName);
                            if (ds == null) return;

                            List<EntityStructure> entityStructures = new List<EntityStructure>();
                            
                            if (entities != null && entities.Length > 0)
                            {
                                foreach (var entityName in entities)
                                {
                                    var entity = ds.GetEntityStructure(entityName, true);
                                    if (entity != null)
                                        entityStructures.Add(entity);
                                }
                            }
                            else
                            {
                                var entitiesList = ds.GetEntitesList();
                                foreach (var entityName in entitiesList)
                                {
                                    var entity = ds.GetEntityStructure(entityName, true);
                                    if (entity != null)
                                        entityStructures.Add(entity);
                                }
                            }

                            if (entityStructures.Count == 0)
                            {
                                CliHelper.DisplayWarning("No entities found");
                                return;
                            }

                            var filePath = classCreator.GenerateGraphQLSchema(entityStructures, output, ns);

                            CliHelper.DisplaySuccess($"GraphQL schema generated with {entityStructures.Count} entities: {filePath}");
                        }
                        catch (Exception ex)
                        {
                            CliHelper.DisplayError($"Error: {ex.Message}");
                        }
                    });
            }, gqlDsNameArg, gqlOutputPathOpt, gqlNamespaceOpt, gqlEntitiesOpt, profileOption);

            // ============ GENERATE GRPC SERVICE ============
            
            var generateGrpcCommand = new Command("generate-grpc", "Generate gRPC service");
            var grpcDsNameArg = new Argument<string>("datasource", "Data source name");
            var grpcEntityNameArg = new Argument<string>("entity", "Entity name");
            var grpcOutputPathOpt = new Option<string>("--output", () => Environment.CurrentDirectory, "Output directory");
            var grpcNamespaceOpt = new Option<string>("--namespace", () => "TheTechIdea.ProjectGrpc", "Namespace");
            
            generateGrpcCommand.AddArgument(grpcDsNameArg);
            generateGrpcCommand.AddArgument(grpcEntityNameArg);
            generateGrpcCommand.AddOption(grpcOutputPathOpt);
            generateGrpcCommand.AddOption(grpcNamespaceOpt);
            generateGrpcCommand.AddOption(profileOption);
            
            generateGrpcCommand.SetHandler(async (string dsName, string entityName, string output, string ns, string profile) =>
            {
                await AnsiConsole.Status()
                    .StartAsync($"Generating gRPC service for '{entityName}'...", async ctx =>
                    {
                        try
                        {
                            var services = new BeepServiceProvider(profile);
                            var editor = services.GetEditor();
                            var classCreator = new ClassCreator(editor);
                            
                            var ds = CliHelper.ValidateAndGetDataSource(editor, dsName);
                            if (ds == null) return;

                            var entity = CliHelper.ValidateAndGetEntity(ds, entityName);
                            if (entity == null) return;

                            var (protoFile, serviceImpl) = classCreator.GenerateGrpcService(entity, output, ns);

                            CliHelper.DisplaySuccess("gRPC service generated:");
                            AnsiConsole.MarkupLine($"  [cyan]Proto file created[/]");
                            AnsiConsole.MarkupLine($"  [cyan]Service implementation created[/]");
                        }
                        catch (Exception ex)
                        {
                            CliHelper.DisplayError($"Error: {ex.Message}");
                        }
                    });
            }, grpcDsNameArg, grpcEntityNameArg, grpcOutputPathOpt, grpcNamespaceOpt, profileOption);

            // ============ GENERATE ENTITY CONFIGURATION ============
            
            var generateConfigCommand = new Command("generate-entity-config", "Generate EF Core entity configuration");
            var cfgDsNameArg = new Argument<string>("datasource", "Data source name");
            var cfgEntityNameArg = new Argument<string>("entity", "Entity name");
            var cfgOutputPathOpt = new Option<string>("--output", () => Environment.CurrentDirectory, "Output directory");
            var cfgNamespaceOpt = new Option<string>("--namespace", () => "TheTechIdea.ProjectData", "Namespace");
            
            generateConfigCommand.AddArgument(cfgDsNameArg);
            generateConfigCommand.AddArgument(cfgEntityNameArg);
            generateConfigCommand.AddOption(cfgOutputPathOpt);
            generateConfigCommand.AddOption(cfgNamespaceOpt);
            generateConfigCommand.AddOption(profileOption);
            
            generateConfigCommand.SetHandler(async (string dsName, string entityName, string output, string ns, string profile) =>
            {
                await AnsiConsole.Status()
                    .StartAsync($"Generating entity configuration for '{entityName}'...", async ctx =>
                    {
                        try
                        {
                            var services = new BeepServiceProvider(profile);
                            var editor = services.GetEditor();
                            var classCreator = new ClassCreator(editor);
                            
                            var ds = CliHelper.ValidateAndGetDataSource(editor, dsName);
                            if (ds == null) return;

                            var entity = CliHelper.ValidateAndGetEntity(ds, entityName);
                            if (entity == null) return;

                            var filePath = classCreator.GenerateEntityConfiguration(entity, ns, output);

                            CliHelper.DisplaySuccess($"Entity configuration generated: {filePath}");
                        }
                        catch (Exception ex)
                        {
                            CliHelper.DisplayError($"Error: {ex.Message}");
                        }
                    });
            }, cfgDsNameArg, cfgEntityNameArg, cfgOutputPathOpt, cfgNamespaceOpt, profileOption);

            // ============ GENERATE DOCUMENTATION ============
            
            var generateDocsCommand = new Command("generate-docs", "Generate XML documentation for entity");
            var docDsNameArg = new Argument<string>("datasource", "Data source name");
            var docEntityNameArg = new Argument<string>("entity", "Entity name");
            var docOutputPathOpt = new Option<string>("--output", () => Environment.CurrentDirectory, "Output directory");
            
            generateDocsCommand.AddArgument(docDsNameArg);
            generateDocsCommand.AddArgument(docEntityNameArg);
            generateDocsCommand.AddOption(docOutputPathOpt);
            generateDocsCommand.AddOption(profileOption);
            
            generateDocsCommand.SetHandler(async (string dsName, string entityName, string output, string profile) =>
            {
                await AnsiConsole.Status()
                    .StartAsync($"Generating documentation for '{entityName}'...", async ctx =>
                    {
                        try
                        {
                            var services = new BeepServiceProvider(profile);
                            var editor = services.GetEditor();
                            var classCreator = new ClassCreator(editor);
                            
                            var ds = CliHelper.ValidateAndGetDataSource(editor, dsName);
                            if (ds == null) return;

                            var entity = CliHelper.ValidateAndGetEntity(ds, entityName);
                            if (entity == null) return;

                            var filePath = classCreator.GenerateEntityDocumentation(entity, output);

                            CliHelper.DisplaySuccess($"Documentation generated: {filePath}");
                        }
                        catch (Exception ex)
                        {
                            CliHelper.DisplayError($"Error: {ex.Message}");
                        }
                    });
            }, docDsNameArg, docEntityNameArg, docOutputPathOpt, profileOption);

            // ============ GENERATE ENTITY CLASS ============
            
            var generateEntityCommand = new Command("generate-entity", "Generate entity class with change tracking");
            var entDsNameArg = new Argument<string>("datasource", "Data source name");
            var entEntityNameArg = new Argument<string>("entity", "Entity name");
            var entOutputPathOpt = new Option<string>("--output", () => Environment.CurrentDirectory, "Output directory");
            var entNamespaceOpt = new Option<string>("--namespace", () => "TheTechIdea.ProjectClasses", "Namespace");
            
            generateEntityCommand.AddArgument(entDsNameArg);
            generateEntityCommand.AddArgument(entEntityNameArg);
            generateEntityCommand.AddOption(entOutputPathOpt);
            generateEntityCommand.AddOption(entNamespaceOpt);
            generateEntityCommand.AddOption(profileOption);
            
            generateEntityCommand.SetHandler(async (string dsName, string entityName, string output, string ns, string profile) =>
            {
                await AnsiConsole.Status()
                    .StartAsync($"Generating entity class for '{entityName}'...", async ctx =>
                    {
                        try
                        {
                            var services = new BeepServiceProvider(profile);
                            var editor = services.GetEditor();
                            var classCreator = new ClassCreator(editor);
                            
                            var ds = CliHelper.ValidateAndGetDataSource(editor, dsName);
                            if (ds == null) return;

                            var entity = CliHelper.ValidateAndGetEntity(ds, entityName);
                            if (entity == null) return;

                            var filePath = classCreator.CreateEntityClass(entity, "using System;", "", output, ns);

                            CliHelper.DisplaySuccess($"Entity class generated: {filePath}");
                        }
                        catch (Exception ex)
                        {
                            CliHelper.DisplayError($"Error: {ex.Message}");
                        }
                    });
            }, entDsNameArg, entEntityNameArg, entOutputPathOpt, entNamespaceOpt, profileOption);

            // ============ GENERATE ENTITY CLASS (BATCH) ============
            
            var generateEntityBatchCommand = new Command("generate-entity-batch", "Generate entity classes from multiple entities with change tracking");
            var entBatchDsArg = new Argument<string>("datasource", "Data source name");
            var entBatchOutputOpt = new Option<string>("--output", () => Environment.CurrentDirectory, "Output directory");
            var entBatchNsOpt = new Option<string>("--namespace", () => "TheTechIdea.ProjectClasses", "Namespace for generated classes");
            var entBatchEntitiesOpt = new Option<string[]>("--entities", "Specific entities (default: all)");
            
            generateEntityBatchCommand.AddArgument(entBatchDsArg);
            generateEntityBatchCommand.AddOption(entBatchOutputOpt);
            generateEntityBatchCommand.AddOption(entBatchNsOpt);
            generateEntityBatchCommand.AddOption(entBatchEntitiesOpt);
            generateEntityBatchCommand.AddOption(profileOption);
            
            generateEntityBatchCommand.SetHandler(async (string dsName, string output, string ns, string[] entityNames, string profile) =>
            {
                await AnsiConsole.Status()
                    .StartAsync($"Generating entity classes from multiple entities...", async ctx =>
                    {
                        try
                        {
                            var services = new BeepServiceProvider(profile);
                            var editor = services.GetEditor();
                            var classCreator = new ClassCreator(editor);
                            
                            var ds = CliHelper.ValidateAndGetDataSource(editor, dsName);
                            if (ds == null) return;

                            // Get entities
                            List<EntityStructure> entities = new List<EntityStructure>();
                            
                            if (entityNames != null && entityNames.Length > 0)
                            {
                                // Specific entities
                                foreach (var entityName in entityNames)
                                {
                                    var entity = CliHelper.ValidateAndGetEntity(ds, entityName);
                                    if (entity == null) return;
                                    entities.Add(entity);
                                }
                            }
                            else
                            {
                                // All entities
                                var allEntityNames = ds.GetEntitesList();
                                foreach (var entityName in allEntityNames)
                                {
                                    var entity = ds.GetEntityStructure(entityName, true);
                                    if (entity != null)
                                        entities.Add(entity);
                                }
                            }

                            if (entities.Count == 0)
                            {
                                CliHelper.DisplayWarning("No entities found to generate");
                                return;
                            }

                            var filePath = classCreator.CreateEntityClass(
                                entities, 
                                "using System;", 
                                "", 
                                output, 
                                ns,
                                true
                            );

                            CliHelper.DisplaySuccess($"Generated {entities.Count} entity classes: {filePath}");
                            AnsiConsole.MarkupLine($"  [cyan]Classes:[/] {string.Join(", ", entities.Select(e => e.EntityName))}");
                        }
                        catch (Exception ex)
                        {
                            CliHelper.DisplayError($"Error: {ex.Message}");
                        }
                    });
            }, entBatchDsArg, entBatchOutputOpt, entBatchNsOpt, entBatchEntitiesOpt, profileOption);

            // ============ GENERATE NULLABLE-AWARE CLASS ============
            
            var generateNullableCommand = new Command("generate-nullable", "Generate C# 8+ nullable-aware class");
            var nullDsNameArg = new Argument<string>("datasource", "Data source name");
            var nullEntityNameArg = new Argument<string>("entity", "Entity name");
            var nullOutputPathOpt = new Option<string>("--output", () => Environment.CurrentDirectory, "Output directory");
            var nullNamespaceOpt = new Option<string>("--namespace", () => "TheTechIdea.ProjectClasses", "Namespace");
            
            generateNullableCommand.AddArgument(nullDsNameArg);
            generateNullableCommand.AddArgument(nullEntityNameArg);
            generateNullableCommand.AddOption(nullOutputPathOpt);
            generateNullableCommand.AddOption(nullNamespaceOpt);
            generateNullableCommand.AddOption(profileOption);
            
            generateNullableCommand.SetHandler(async (string dsName, string entityName, string output, string ns, string profile) =>
            {
                await AnsiConsole.Status()
                    .StartAsync($"Generating nullable-aware class for '{entityName}'...", async ctx =>
                    {
                        try
                        {
                            var services = new BeepServiceProvider(profile);
                            var editor = services.GetEditor();
                            var classCreator = new ClassCreator(editor);
                            
                            var ds = CliHelper.ValidateAndGetDataSource(editor, dsName);
                            if (ds == null) return;

                            var entity = CliHelper.ValidateAndGetEntity(ds, entityName);
                            if (entity == null) return;

                            var filePath = classCreator.CreateNullableAwareClass(entityName, entity, output, ns);

                            CliHelper.DisplaySuccess($"Nullable-aware class generated: {filePath}");
                        }
                        catch (Exception ex)
                        {
                            CliHelper.DisplayError($"Error: {ex.Message}");
                        }
                    });
            }, nullDsNameArg, nullEntityNameArg, nullOutputPathOpt, nullNamespaceOpt, profileOption);

            // ============ GENERATE DDD AGGREGATE ROOT ============
            
            var generateDddCommand = new Command("generate-ddd-aggregate", "Generate Domain-Driven Design aggregate root");
            var dddDsNameArg = new Argument<string>("datasource", "Data source name");
            var dddEntityNameArg = new Argument<string>("entity", "Entity name");
            var dddOutputPathOpt = new Option<string>("--output", () => Environment.CurrentDirectory, "Output directory");
            var dddNamespaceOpt = new Option<string>("--namespace", () => "TheTechIdea.ProjectDomain", "Namespace");
            
            generateDddCommand.AddArgument(dddDsNameArg);
            generateDddCommand.AddArgument(dddEntityNameArg);
            generateDddCommand.AddOption(dddOutputPathOpt);
            generateDddCommand.AddOption(dddNamespaceOpt);
            generateDddCommand.AddOption(profileOption);
            
            generateDddCommand.SetHandler(async (string dsName, string entityName, string output, string ns, string profile) =>
            {
                await AnsiConsole.Status()
                    .StartAsync($"Generating DDD aggregate root for '{entityName}'...", async ctx =>
                    {
                        try
                        {
                            var services = new BeepServiceProvider(profile);
                            var editor = services.GetEditor();
                            var classCreator = new ClassCreator(editor);
                            
                            var ds = CliHelper.ValidateAndGetDataSource(editor, dsName);
                            if (ds == null) return;

                            var entity = CliHelper.ValidateAndGetEntity(ds, entityName);
                            if (entity == null) return;

                            var filePath = classCreator.CreateDDDAggregateRoot(entity, output, ns);

                            CliHelper.DisplaySuccess($"DDD aggregate root generated: {filePath}");
                        }
                        catch (Exception ex)
                        {
                            CliHelper.DisplayError($"Error: {ex.Message}");
                        }
                    });
            }, dddDsNameArg, dddEntityNameArg, dddOutputPathOpt, dddNamespaceOpt, profileOption);

            // ============ GENERATE PARAMETERIZED WEB API CONTROLLER ============
            
            var generateParamApiCommand = new Command("generate-webapi-params", "Generate Web API controller template with datasource/entity parameters");
            var paramClassNameArg = new Argument<string>("classname", "Controller class name (e.g., CustomerController)");
            var paramOutputPathOpt = new Option<string>("--output", () => Environment.CurrentDirectory, "Output directory");
            var paramNamespaceOpt = new Option<string>("--namespace", () => "TheTechIdea.ProjectControllers", "Namespace");
            
            generateParamApiCommand.AddArgument(paramClassNameArg);
            generateParamApiCommand.AddOption(paramOutputPathOpt);
            generateParamApiCommand.AddOption(paramNamespaceOpt);
            generateParamApiCommand.AddOption(profileOption);
            
            generateParamApiCommand.SetHandler(async (string className, string output, string ns, string profile) =>
            {
                await AnsiConsole.Status()
                    .StartAsync($"Generating parameterized Web API controller '{className}'...", async ctx =>
                    {
                        try
                        {
                            var services = new BeepServiceProvider(profile);
                            var editor = services.GetEditor();
                            var classCreator = new ClassCreator(editor);

                            var filePath = classCreator.GenerateWebApiControllerForEntityWithParams(className, output, ns);

                            CliHelper.DisplaySuccess($"Parameterized Web API controller template generated: {filePath}");
                            AnsiConsole.MarkupLine($"  [cyan]Controller:[/] {className}");
                            AnsiConsole.MarkupLine($"  [yellow]Note:[/] This is a template. Customize datasource/entity parameters in the generated code.");
                        }
                        catch (Exception ex)
                        {
                            CliHelper.DisplayError($"Error: {ex.Message}");
                        }
                    });
            }, paramClassNameArg, paramOutputPathOpt, paramNamespaceOpt, profileOption);

            // ============ GENERATE EF CORE MIGRATION ============
            
            var generateMigrationCommand = new Command("generate-migration", "Generate EF Core migration class");
            var migDsNameArg = new Argument<string>("datasource", "Data source name");
            var migEntityNameArg = new Argument<string>("entity", "Entity name");
            var migOutputPathOpt = new Option<string>("--output", () => Environment.CurrentDirectory, "Output directory");
            var migNamespaceOpt = new Option<string>("--namespace", () => "TheTechIdea.ProjectMigrations", "Namespace");
            
            generateMigrationCommand.AddArgument(migDsNameArg);
            generateMigrationCommand.AddArgument(migEntityNameArg);
            generateMigrationCommand.AddOption(migOutputPathOpt);
            generateMigrationCommand.AddOption(migNamespaceOpt);
            generateMigrationCommand.AddOption(profileOption);
            
            generateMigrationCommand.SetHandler(async (string dsName, string entityName, string output, string ns, string profile) =>
            {
                await AnsiConsole.Status()
                    .StartAsync($"Generating EF Core migration for '{entityName}'...", async ctx =>
                    {
                        try
                        {
                            var services = new BeepServiceProvider(profile);
                            var editor = services.GetEditor();
                            var classCreator = new ClassCreator(editor);
                            
                            var ds = CliHelper.ValidateAndGetDataSource(editor, dsName);
                            if (ds == null) return;

                            var entity = CliHelper.ValidateAndGetEntity(ds, entityName);
                            if (entity == null) return;

                            var filePath = classCreator.GenerateEFCoreMigration(entity, output, ns);

                            CliHelper.DisplaySuccess($"EF Core migration generated: {filePath}");
                        }
                        catch (Exception ex)
                        {
                            CliHelper.DisplayError($"Error: {ex.Message}");
                        }
                    });
            }, migDsNameArg, migEntityNameArg, migOutputPathOpt, migNamespaceOpt, profileOption);

            // ============ GENERATE SERVERLESS FUNCTIONS ============
            
            var generateServerlessCommand = new Command("generate-serverless", "Generate serverless functions (Azure/AWS)");
            var srvDsNameArg = new Argument<string>("datasource", "Data source name");
            var srvEntityNameArg = new Argument<string>("entity", "Entity name");
            var srvOutputPathOpt = new Option<string>("--output", () => Environment.CurrentDirectory, "Output directory");
            var srvProviderOpt = new Option<string>("--provider", () => "Azure", "Cloud provider (Azure or AWS)");
            
            generateServerlessCommand.AddArgument(srvDsNameArg);
            generateServerlessCommand.AddArgument(srvEntityNameArg);
            generateServerlessCommand.AddOption(srvOutputPathOpt);
            generateServerlessCommand.AddOption(srvProviderOpt);
            generateServerlessCommand.AddOption(profileOption);
            
            generateServerlessCommand.SetHandler(async (string dsName, string entityName, string output, string provider, string profile) =>
            {
                await AnsiConsole.Status()
                    .StartAsync($"Generating {provider} serverless functions for '{entityName}'...", async ctx =>
                    {
                        try
                        {
                            var services = new BeepServiceProvider(profile);
                            var editor = services.GetEditor();
                            var classCreator = new ClassCreator(editor);
                            
                            var ds = CliHelper.ValidateAndGetDataSource(editor, dsName);
                            if (ds == null) return;

                            var entity = CliHelper.ValidateAndGetEntity(ds, entityName);
                            if (entity == null) return;

                            CloudProviderType cloudProvider = provider.ToLower() switch
                            {
                                "aws" => CloudProviderType.AWS,
                                "azure" => CloudProviderType.Azure,
                                _ => CloudProviderType.Azure
                            };

                            var filePath = classCreator.GenerateServerlessFunctions(entity, output, cloudProvider);

                            CliHelper.DisplaySuccess($"{provider} serverless functions generated: {filePath}");
                        }
                        catch (Exception ex)
                        {
                            CliHelper.DisplayError($"Error: {ex.Message}");
                        }
                    });
            }, srvDsNameArg, srvEntityNameArg, srvOutputPathOpt, srvProviderOpt, profileOption);

            // ============ GENERATE ENTITY DIFF REPORT ============
            
            var generateDiffCommand = new Command("generate-diff", "Generate difference report between two entity versions");
            var diffDsNameArg = new Argument<string>("datasource", "Data source name");
            var diffEntityArg = new Argument<string>("entity", "Entity name");
            var diffOldDsArg = new Argument<string>("old-datasource", "Old version data source");
            
            generateDiffCommand.AddArgument(diffDsNameArg);
            generateDiffCommand.AddArgument(diffEntityArg);
            generateDiffCommand.AddArgument(diffOldDsArg);
            generateDiffCommand.AddOption(profileOption);
            
            generateDiffCommand.SetHandler(async (string dsName, string entityName, string oldDsName, string profile) =>
            {
                await AnsiConsole.Status()
                    .StartAsync($"Generating diff report for '{entityName}'...", async ctx =>
                    {
                        try
                        {
                            var services = new BeepServiceProvider(profile);
                            var editor = services.GetEditor();
                            var classCreator = new ClassCreator(editor);
                            
                            var ds = CliHelper.ValidateAndGetDataSource(editor, dsName);
                            var oldDs = CliHelper.ValidateAndGetDataSource(editor, oldDsName);
                            
                            if (ds == null || oldDs == null) return;

                            var newEntity = CliHelper.ValidateAndGetEntity(ds, entityName);
                            var oldEntity = CliHelper.ValidateAndGetEntity(oldDs, entityName);
                            
                            if (newEntity == null || oldEntity == null) return;

                            var report = classCreator.GenerateEntityDiffReport(oldEntity, newEntity);

                            AnsiConsole.Write(new Panel(report)
                            {
                                Header = new PanelHeader("[bold]Entity Difference Report[/]"),
                                Border = BoxBorder.Rounded
                            });
                        }
                        catch (Exception ex)
                        {
                            CliHelper.DisplayError($"Error: {ex.Message}");
                        }
                    });
            }, diffDsNameArg, diffEntityArg, diffOldDsArg, profileOption);

            // ============ COMPILE CODE TO ASSEMBLY ============
            
            var compileAssemblyCommand = new Command("compile-assembly", "Compile C# code string to assembly in memory");
            var asmCodeArg = new Argument<string>("code-file", "Path to .cs file containing code");
            
            compileAssemblyCommand.AddArgument(asmCodeArg);
            compileAssemblyCommand.AddOption(profileOption);
            
            compileAssemblyCommand.SetHandler(async (string codeFile, string profile) =>
            {
                await AnsiConsole.Status()
                    .StartAsync($"Compiling code to assembly...", async ctx =>
                    {
                        try
                        {
                            if (!File.Exists(codeFile))
                            {
                                CliHelper.DisplayError($"Code file not found: {codeFile}");
                                return;
                            }

                            var code = File.ReadAllText(codeFile);
                            var services = new BeepServiceProvider(profile);
                            var editor = services.GetEditor();
                            var classCreator = new ClassCreator(editor);

                            var assembly = classCreator.CreateAssemblyFromCode(code);

                            if (assembly != null)
                            {
                                CliHelper.DisplaySuccess($"Assembly created: {assembly.FullName}");
                                AnsiConsole.MarkupLine($"  [cyan]Location:[/] {assembly.Location}");
                                AnsiConsole.MarkupLine($"  [cyan]Types:[/] {assembly.GetTypes().Length}");
                            }
                            else
                            {
                                CliHelper.DisplayError("Failed to create assembly");
                            }
                        }
                        catch (Exception ex)
                        {
                            CliHelper.DisplayError($"Error: {ex.Message}");
                        }
                    });
            }, asmCodeArg, profileOption);

            // ============ COMPILE CODE TO TYPE ============
            
            var compileTypeCommand = new Command("compile-type", "Compile C# code string to a specific type");
            var typeCodeArg = new Argument<string>("code-file", "Path to .cs file containing code");
            var typeNameArg = new Argument<string>("typename", "Full type name to create");
            
            compileTypeCommand.AddArgument(typeCodeArg);
            compileTypeCommand.AddArgument(typeNameArg);
            compileTypeCommand.AddOption(profileOption);
            
            compileTypeCommand.SetHandler(async (string codeFile, string typeName, string profile) =>
            {
                await AnsiConsole.Status()
                    .StartAsync($"Compiling code to type '{typeName}'...", async ctx =>
                    {
                        try
                        {
                            if (!File.Exists(codeFile))
                            {
                                CliHelper.DisplayError($"Code file not found: {codeFile}");
                                return;
                            }

                            var code = File.ReadAllText(codeFile);
                            var services = new BeepServiceProvider(profile);
                            var editor = services.GetEditor();
                            var classCreator = new ClassCreator(editor);

                            var type = classCreator.CreateTypeFromCode(code, typeName);

                            if (type != null)
                            {
                                CliHelper.DisplaySuccess($"Type created: {type.FullName}");
                                AnsiConsole.MarkupLine($"  [cyan]Assembly:[/] {type.Assembly.FullName}");
                                AnsiConsole.MarkupLine($"  [cyan]Namespace:[/] {type.Namespace}");
                                AnsiConsole.MarkupLine($"  [cyan]Properties:[/] {type.GetProperties().Length}");
                                AnsiConsole.MarkupLine($"  [cyan]Methods:[/] {type.GetMethods().Length}");
                            }
                            else
                            {
                                CliHelper.DisplayError("Failed to create type");
                            }
                        }
                        catch (Exception ex)
                        {
                            CliHelper.DisplayError($"Error: {ex.Message}");
                        }
                    });
            }, typeCodeArg, typeNameArg, profileOption);

            // ============ COMPILE CLASS TEXT TO DLL ============
            
            var compileClassToDllCommand = new Command("compile-class-to-dll", "Compile class source code to DLL file");
            var classCodeArg = new Argument<string>("source-file", "Path to .cs file containing class code");
            var classOutputArg = new Argument<string>("output-dll", "Output DLL path");
            
            compileClassToDllCommand.AddArgument(classCodeArg);
            compileClassToDllCommand.AddArgument(classOutputArg);
            compileClassToDllCommand.AddOption(profileOption);
            
            compileClassToDllCommand.SetHandler(async (string sourceFile, string outputDll, string profile) =>
            {
                await AnsiConsole.Status()
                    .StartAsync($"Compiling class to DLL...", async ctx =>
                    {
                        try
                        {
                            if (!File.Exists(sourceFile))
                            {
                                CliHelper.DisplayError($"Source file not found: {sourceFile}");
                                return;
                            }

                            var code = File.ReadAllText(sourceFile);
                            var services = new BeepServiceProvider(profile);
                            var editor = services.GetEditor();
                            var classCreator = new ClassCreator(editor);

                            classCreator.CompileClassFromText(code, outputDll);

                            if (File.Exists(outputDll))
                            {
                                var fileInfo = new FileInfo(outputDll);
                                CliHelper.DisplaySuccess($"DLL compiled: {outputDll}");
                                AnsiConsole.MarkupLine($"  [cyan]Size:[/] {CliHelper.FormatFileSize(fileInfo.Length)}");
                            }
                            else
                            {
                                CliHelper.DisplayError("Compilation failed - no DLL created");
                            }
                        }
                        catch (Exception ex)
                        {
                            CliHelper.DisplayError($"Error: {ex.Message}");
                        }
                    });
            }, classCodeArg, classOutputArg, profileOption);

            // ============ GENERATE CSHARP CODE FROM FILE ============
            
            var generateCSharpCommand = new Command("generate-csharp", "Generate C# code from template/file");
            var csharpFileArg = new Argument<string>("template-file", "Path to template file");
            
            generateCSharpCommand.AddArgument(csharpFileArg);
            generateCSharpCommand.AddOption(profileOption);
            
            generateCSharpCommand.SetHandler(async (string templateFile, string profile) =>
            {
                await AnsiConsole.Status()
                    .StartAsync($"Generating C# code from '{templateFile}'...", async ctx =>
                    {
                        try
                        {
                            if (!File.Exists(templateFile))
                            {
                                CliHelper.DisplayError($"Template file not found: {templateFile}");
                                return;
                            }

                            var services = new BeepServiceProvider(profile);
                            var editor = services.GetEditor();
                            var classCreator = new ClassCreator(editor);

                            classCreator.GenerateCSharpCode(templateFile);

                            CliHelper.DisplaySuccess($"C# code generated from: {templateFile}");
                        }
                        catch (Exception ex)
                        {
                            CliHelper.DisplayError($"Error: {ex.Message}");
                        }
                    });
            }, csharpFileArg, profileOption);

            // ============ CREATE DLL FROM FILES ============
            
            var createDllFromFilesCommand = new Command("create-dll-from-files", "Create DLL from existing .cs files");
            var dllFilesDllNameArg = new Argument<string>("dllname", "DLL name (without extension)");
            var dllFilesPathArg = new Argument<string>("filepath", "Path to .cs files directory");
            var dllFilesOutputOpt = new Option<string>("--output", () => Environment.CurrentDirectory, "Output directory");
            var dllFilesNsOpt = new Option<string>("--namespace", () => "TheTechIdea.ProjectClasses", "Namespace");
            
            createDllFromFilesCommand.AddArgument(dllFilesDllNameArg);
            createDllFromFilesCommand.AddArgument(dllFilesPathArg);
            createDllFromFilesCommand.AddOption(dllFilesOutputOpt);
            createDllFromFilesCommand.AddOption(dllFilesNsOpt);
            createDllFromFilesCommand.AddOption(profileOption);
            
            createDllFromFilesCommand.SetHandler(async (string dllName, string filePath, string output, string ns, string profile) =>
            {
                await AnsiConsole.Progress()
                    .StartAsync(async ctx =>
                    {
                        var task = ctx.AddTask($"[green]Creating DLL '{dllName}' from files[/]");
                        
                        try
                        {
                            var services = new BeepServiceProvider(profile);
                            var editor = services.GetEditor();
                            var classCreator = new ClassCreator(editor);

                            task.IsIndeterminate = true;

                            var progress = new Progress<PassedArgs>(args =>
                            {
                                if (!string.IsNullOrEmpty(args.Messege))
                                    task.Description = $"[dim]{args.Messege}[/]";
                            });

                            var resultPath = classCreator.CreateDLLFromFilesPath(
                                dllName,
                                filePath,
                                output,
                                progress,
                                CancellationToken.None,
                                ns
                            );

                            task.Value = 100;
                            task.StopTask();

                            CliHelper.DisplaySuccess($"DLL created from files: {resultPath}");
                        }
                        catch (Exception ex)
                        {
                            task.StopTask();
                            CliHelper.DisplayError($"Error: {ex.Message}");
                        }
                    });
            }, dllFilesDllNameArg, dllFilesPathArg, dllFilesOutputOpt, dllFilesNsOpt, profileOption);

            // Add all subcommands
            classCommand.AddCommand(generatePocoCommand);
            classCommand.AddCommand(generatePocoBatchCommand);
            classCommand.AddCommand(generateWebApiCommand);
            classCommand.AddCommand(generateDbContextCommand);
            classCommand.AddCommand(generateRepoCommand);
            classCommand.AddCommand(createDllCommand);
            classCommand.AddCommand(generateDalCommand);
            classCommand.AddCommand(generateMinimalApiCommand);
            classCommand.AddCommand(validateEntityCommand);
            
            // Modern class types
            classCommand.AddCommand(generateRecordCommand);
            classCommand.AddCommand(generateINotifyCommand);
            classCommand.AddCommand(generateINotifyBatchCommand);
            classCommand.AddCommand(generateEntityCommand);
            classCommand.AddCommand(generateEntityBatchCommand);
            classCommand.AddCommand(generateNullableCommand);
            classCommand.AddCommand(generateDddCommand);
            
            // Testing & Validation
            classCommand.AddCommand(generateTestCommand);
            classCommand.AddCommand(generateValidatorCommand);
            
            // UI Components
            classCommand.AddCommand(generateBlazorCommand);
            classCommand.AddCommand(generateGraphQLCommand);
            
            // Serverless & Cloud
            classCommand.AddCommand(generateGrpcCommand);
            classCommand.AddCommand(generateServerlessCommand);
            
            // Database & Migrations
            classCommand.AddCommand(generateConfigCommand);
            classCommand.AddCommand(generateMigrationCommand);
            
            // Advanced Web API
            classCommand.AddCommand(generateParamApiCommand);
            
            // Documentation & Utilities
            classCommand.AddCommand(generateDocsCommand);
            classCommand.AddCommand(generateDiffCommand);
            
            // DLL Creation
            classCommand.AddCommand(createDllFromFilesCommand);
            
            // Low-Level Compilation (Advanced)
            classCommand.AddCommand(compileAssemblyCommand);
            classCommand.AddCommand(compileTypeCommand);
            classCommand.AddCommand(compileClassToDllCommand);
            classCommand.AddCommand(generateCSharpCommand);

            // Additional commands using helper methods
            var additionalCommands = BuildAdditionalCommands();
            foreach (var command in additionalCommands)
            {
                classCommand.AddCommand(command);
            }

            return classCommand;
        }
    }
}
