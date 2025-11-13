using System;
using System.CommandLine;
using System.Collections.Generic;
using System.Linq;
using Spectre.Console;
using TheTechIdea.Beep.DataBase;

namespace BeepShell.Commands
{
    /// <summary>
    /// Database-related class generation commands for ClassCreatorShellCommands
    /// </summary>
    public partial class ClassCreatorShellCommands
    {
        /// <summary>
        /// Builds database-related generation commands
        /// </summary>
        private void AddDatabaseCommands(Command classCommand)
        {
            // class dal - Generate Data Access Layer
            var dalCommand = new Command("dal", "Generate Data Access Layer for entity");
            var dalDsArg = new Argument<string>("datasource", "Data source name");
            var dalTableArg = new Argument<string>("table", "Table name");
            var dalOutputOption = new Option<string>("--output", "Output directory path") { IsRequired = true };

            dalCommand.AddArgument(dalDsArg);
            dalCommand.AddArgument(dalTableArg);
            dalCommand.AddOption(dalOutputOption);

            dalCommand.SetHandler((datasource, table, output) =>
            {
                GenerateDataAccessLayer(datasource, table, output);
            }, dalDsArg, dalTableArg, dalOutputOption);

            classCommand.AddCommand(dalCommand);

            // class dbcontext - Generate EF DbContext
            var dbContextCommand = new Command("dbcontext", "Generate Entity Framework DbContext");
            var dbDsArg = new Argument<string>("datasource", "Data source name");
            var dbOutputOption = new Option<string>("--output", "Output directory path") { IsRequired = true };
            var dbNsOption = new Option<string>("--namespace", () => "TheTechIdea.ProjectData", "Namespace");
            var dbAllOption = new Option<bool>("--all", () => false, "Include all tables");
            var dbTablesOption = new Option<string[]>("--tables", "Specific tables to include");

            dbContextCommand.AddArgument(dbDsArg);
            dbContextCommand.AddOption(dbOutputOption);
            dbContextCommand.AddOption(dbNsOption);
            dbContextCommand.AddOption(dbAllOption);
            dbContextCommand.AddOption(dbTablesOption);

            dbContextCommand.SetHandler((datasource, output, ns, all, tables) =>
            {
                GenerateDbContext(datasource, output, ns, all, tables);
            }, dbDsArg, dbOutputOption, dbNsOption, dbAllOption, dbTablesOption);

            classCommand.AddCommand(dbContextCommand);

            // class ef-config - Generate EF Core entity configuration
            var efConfigCommand = new Command("ef-config", "Generate EF Core entity configuration");
            var efDsArg = new Argument<string>("datasource", "Data source name");
            var efTableArg = new Argument<string>("table", "Table name");
            var efOutputOption = new Option<string>("--output", "Output directory path") { IsRequired = true };
            var efNsOption = new Option<string>("--namespace", () => "TheTechIdea.ProjectConfigurations", "Namespace");

            efConfigCommand.AddArgument(efDsArg);
            efConfigCommand.AddArgument(efTableArg);
            efConfigCommand.AddOption(efOutputOption);
            efConfigCommand.AddOption(efNsOption);

            efConfigCommand.SetHandler((datasource, table, output, ns) =>
            {
                GenerateEntityConfiguration(datasource, table, output, ns);
            }, efDsArg, efTableArg, efOutputOption, efNsOption);

            classCommand.AddCommand(efConfigCommand);

            // class repository - Generate Repository pattern
            var repoCommand = new Command("repository", "Generate Repository pattern implementation");
            var repoDsArg = new Argument<string>("datasource", "Data source name");
            var repoTableArg = new Argument<string>("table", "Table name");
            var repoOutputOption = new Option<string>("--output", "Output directory path") { IsRequired = true };
            var repoNsOption = new Option<string>("--namespace", () => "TheTechIdea.ProjectRepositories", "Namespace");
            var repoInterfaceOnlyOption = new Option<bool>("--interface-only", () => false, "Generate interface only");

            repoCommand.AddArgument(repoDsArg);
            repoCommand.AddArgument(repoTableArg);
            repoCommand.AddOption(repoOutputOption);
            repoCommand.AddOption(repoNsOption);
            repoCommand.AddOption(repoInterfaceOnlyOption);

            repoCommand.SetHandler((datasource, table, output, ns, interfaceOnly) =>
            {
                GenerateRepository(datasource, table, output, ns, interfaceOnly);
            }, repoDsArg, repoTableArg, repoOutputOption, repoNsOption, repoInterfaceOnlyOption);

            classCommand.AddCommand(repoCommand);

            // class migration - Generate EF Core migration
            var migrationCommand = new Command("migration", "Generate EF Core migration");
            var migDsArg = new Argument<string>("datasource", "Data source name");
            var migTableArg = new Argument<string>("table", "Table name");
            var migOutputOption = new Option<string>("--output", "Output directory path") { IsRequired = true };
            var migNsOption = new Option<string>("--namespace", () => "TheTechIdea.ProjectMigrations", "Namespace");

            migrationCommand.AddArgument(migDsArg);
            migrationCommand.AddArgument(migTableArg);
            migrationCommand.AddOption(migOutputOption);
            migrationCommand.AddOption(migNsOption);

            migrationCommand.SetHandler((datasource, table, output, ns) =>
            {
                GenerateMigration(datasource, table, output, ns);
            }, migDsArg, migTableArg, migOutputOption, migNsOption);

            classCommand.AddCommand(migrationCommand);
        }

        /// <summary>
        /// Generates Data Access Layer for an entity
        /// </summary>
        private void GenerateDataAccessLayer(string datasourceName, string tableName, string outputDir)
        {
            try
            {
                var structure = GetEntityStructure(datasourceName, tableName);
                if (structure == null) return;

                DisplayEntityInfo(structure);

                AnsiConsole.Status()
                    .Start($"Generating DAL for {tableName}...", ctx =>
                    {
                        var result = _classCreator.GenerateDataAccessLayer(structure, outputDir);
                        
                        if (!string.IsNullOrEmpty(result))
                        {
                            AnsiConsole.MarkupLine($"[green]✓[/] Data Access Layer generated: {result}");
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates EF DbContext
        /// </summary>
        private void GenerateDbContext(string datasourceName, string outputDir, string namespaceName, bool generateAll, string[] tables)
        {
            try
            {
                var entities = GetEntityStructures(datasourceName, generateAll, tables);
                if (entities.Count == 0) return;

                AnsiConsole.Status()
                    .Start($"Generating DbContext for {entities.Count} entities...", ctx =>
                    {
                        var result = _classCreator.GenerateDbContext(entities, namespaceName, outputDir);
                        
                        if (!string.IsNullOrEmpty(result))
                        {
                            AnsiConsole.MarkupLine($"[green]✓[/] DbContext generated: {result}");
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates EF Core entity configuration
        /// </summary>
        private void GenerateEntityConfiguration(string datasourceName, string tableName, string outputDir, string namespaceName)
        {
            try
            {
                var structure = GetEntityStructure(datasourceName, tableName);
                if (structure == null) return;

                AnsiConsole.Status()
                    .Start($"Generating EF configuration for {tableName}...", ctx =>
                    {
                        var result = _classCreator.GenerateEntityConfiguration(structure, namespaceName, outputDir);
                        
                        if (!string.IsNullOrEmpty(result))
                        {
                            AnsiConsole.MarkupLine($"[green]✓[/] Entity configuration generated: {result}");
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates Repository pattern
        /// </summary>
        private void GenerateRepository(string datasourceName, string tableName, string outputDir, string namespaceName, bool interfaceOnly)
        {
            try
            {
                var structure = GetEntityStructure(datasourceName, tableName);
                if (structure == null) return;

                var type = interfaceOnly ? "interface" : "implementation";
                AnsiConsole.Status()
                    .Start($"Generating repository {type} for {tableName}...", ctx =>
                    {
                        var result = _classCreator.GenerateRepositoryImplementation(structure, outputDir, namespaceName, interfaceOnly);
                        
                        if (!string.IsNullOrEmpty(result))
                        {
                            AnsiConsole.MarkupLine($"[green]✓[/] Repository {type} generated: {result}");
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates EF Core migration
        /// </summary>
        private void GenerateMigration(string datasourceName, string tableName, string outputDir, string namespaceName)
        {
            try
            {
                var structure = GetEntityStructure(datasourceName, tableName);
                if (structure == null) return;

                AnsiConsole.Status()
                    .Start($"Generating migration for {tableName}...", ctx =>
                    {
                        var result = _classCreator.GenerateEFCoreMigration(structure, outputDir, namespaceName);
                        
                        if (!string.IsNullOrEmpty(result))
                        {
                            AnsiConsole.MarkupLine($"[green]✓[/] Migration generated: {result}");
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }

        #region Helper Methods for DataSource Integration

        /// <summary>
        /// Gets and validates a data source, ensuring it's open
        /// </summary>
        private TheTechIdea.Beep.IDataSource? GetDataSource(string datasourceName)
        {
            var ds = _editor.GetDataSource(datasourceName);
            if (ds == null)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Data source '{datasourceName}' not found");
                AnsiConsole.MarkupLine("[yellow]Tip:[/] Use 'datasource list' to see available data sources");
                return null;
            }

            if (ds.ConnectionStatus != System.Data.ConnectionState.Open)
            {
                AnsiConsole.MarkupLine($"[yellow]![/] Opening connection to '{datasourceName}'...");
                try
                {
                    _editor.OpenDataSource(datasourceName);
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to open connection: {ex.Message}");
                    return null;
                }
            }

            return ds;
        }

        /// <summary>
        /// Gets multiple entity structures from a data source
        /// </summary>
        private List<EntityStructure>? GetEntityStructures(string datasourceName, string[] tableNames)
        {
            var ds = GetDataSource(datasourceName);
            if (ds == null) return null;

            var structures = new List<EntityStructure>();
            
            foreach (var tableName in tableNames)
            {
                try
                {
                    var structure = ds.GetEntityStructure(tableName, true);
                    if (structure != null)
                    {
                        structures.Add(structure);
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[yellow]![/] Entity '{tableName}' not found, skipping...");
                    }
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Error getting structure for '{tableName}': {ex.Message}");
                }
            }

            return structures.Count > 0 ? structures : null;
        }

        /// <summary>
        /// Gets all entity structures from a data source
        /// </summary>
        private List<EntityStructure>? GetAllEntityStructures(string datasourceName)
        {
            var ds = GetDataSource(datasourceName);
            if (ds == null) return null;

            var entities = ds.GetEntitesList()?.ToList();
            if (entities == null || entities.Count == 0)
            {
                AnsiConsole.MarkupLine($"[yellow]![/] No entities found in '{datasourceName}'");
                return null;
            }

            return GetEntityStructures(datasourceName, entities.ToArray());
        }

        /// <summary>
        /// Displays entity information before generation
        /// </summary>
        private void DisplayEntityInfo(EntityStructure structure)
        {
            var info = new Panel(new Markup(
                $"[cyan]Entity:[/] {structure.EntityName}\n" +
                $"[cyan]Schema:[/] {structure.SchemaOrOwnerOrDatabase ?? "N/A"}\n" +
                $"[cyan]Fields:[/] {structure.Fields?.Count ?? 0}\n" +
                $"[cyan]Primary Keys:[/] {structure.PrimaryKeys?.Count ?? 0}\n" +
                $"[cyan]Relations:[/] {structure.Relations?.Count ?? 0}"
            ));
            info.Header = new PanelHeader($"[green]Generating for {structure.EntityName}[/]");
            info.Border = BoxBorder.Rounded;
            AnsiConsole.Write(info);
        }

        /// <summary>
        /// Validates that a data source has tables before generation
        /// </summary>
        private bool ValidateDataSourceHasTables(string datasourceName)
        {
            var ds = GetDataSource(datasourceName);
            if (ds == null) return false;

            var entities = ds.GetEntitesList()?.ToList();
            if (entities == null || entities.Count == 0)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] No tables found in data source '{datasourceName}'");
                AnsiConsole.MarkupLine("[yellow]Tip:[/] Use 'datasource entities {datasourceName}' to verify tables exist");
                return false;
            }

            return true;
        }

        #endregion
    }
}
