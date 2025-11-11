using System;
using System.CommandLine;
using System.Linq;
using System.Collections.Generic;
using BeepShell.Infrastructure;
using Spectre.Console;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.ConfigUtil;

namespace BeepShell.Commands
{
    /// <summary>
    /// Data synchronization commands for BeepShell
    /// Uses persistent DMEEditor for sync operations
    /// </summary>
    public class SyncShellCommands : IShellCommand
    {
        private IDMEEditor _editor;

        public string CommandName => "sync";
        public string Description => "Synchronize data between sources";
        public string Category => "Data";
        public string Version => "1.0.0";
        public string Author => "BeepDM Team";
        public string[] Aliases => new[] { "synchronize", "replicate" };

        public void Initialize(IDMEEditor editor)
        {
            _editor = editor;
        }

        public Command BuildCommand()
        {
            var syncCommand = new Command("sync", Description);

            // sync start
            var startCommand = new Command("start", "Start data synchronization");
            var sourceOption = new Option<string>("--source", "Source data source") { IsRequired = true };
            var destOption = new Option<string>("--dest", "Destination data source") { IsRequired = true };
            var tableOption = new Option<string>("--table", "Table to synchronize");
            var allOption = new Option<bool>("--all", "Synchronize all tables");

            startCommand.AddOption(sourceOption);
            startCommand.AddOption(destOption);
            startCommand.AddOption(tableOption);
            startCommand.AddOption(allOption);

            startCommand.SetHandler((source, dest, table, all) =>
            {
                StartSync(source, dest, table, all);
            }, sourceOption, destOption, tableOption, allOption);

            syncCommand.AddCommand(startCommand);

            // sync status
            var statusCommand = new Command("status", "Show synchronization status");
            statusCommand.SetHandler(() => ShowSyncStatus());
            syncCommand.AddCommand(statusCommand);

            return syncCommand;
        }

        private void StartSync(string sourceName, string destName, string tableName, bool syncAll)
        {
            try
            {
                var sourceDs = _editor.GetDataSource(sourceName);
                var destDs = _editor.GetDataSource(destName);

                if (sourceDs == null)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Source data source '{sourceName}' not found");
                    return;
                }

                if (destDs == null)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Destination data source '{destName}' not found");
                    return;
                }

                // Ensure connections are open
                if (sourceDs.ConnectionStatus != System.Data.ConnectionState.Open)
                {
                    _editor.OpenDataSource(sourceName);
                }

                if (destDs.ConnectionStatus != System.Data.ConnectionState.Open)
                {
                    _editor.OpenDataSource(destName);
                }

                if (syncAll)
                {
                    SyncAllTables(sourceDs, destDs);
                }
                else if (!string.IsNullOrEmpty(tableName))
                {
                    SyncTable(sourceDs, destDs, tableName);
                }
                else
                {
                    AnsiConsole.MarkupLine("[yellow]Specify --table <name> or --all[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }

        private void SyncTable(IDataSource source, IDataSource dest, string tableName)
        {
            try
            {
                AnsiConsole.Status()
                    .Start($"Synchronizing {tableName}...", ctx =>
                    {
                        // Get source data
                        ctx.Status($"Reading from {source.DatasourceName}.{tableName}...");
                        var data = source.GetEntity(tableName, null)?.ToList();

                        if (data == null || data.Count == 0)
                        {
                            AnsiConsole.MarkupLine("[yellow]No data to sync[/]");
                            return;
                        }

                        ctx.Status($"Read {data.Count} rows");

                        // Check if destination table exists
                        var destEntities = dest.GetEntitesList()?.ToList();
                        var tableExists = destEntities?.Any(e => e.Equals(tableName, StringComparison.OrdinalIgnoreCase)) ?? false;

                        if (!tableExists)
                        {
                            ctx.Status($"Creating table {tableName} in destination...");
                            var entityStructure = source.GetEntityStructure(tableName, false);
                            dest.CreateEntityAs(entityStructure);
                        }

                        // Sync data
                        ctx.Status($"Writing to {dest.DatasourceName}.{tableName}...");
                        var result = dest.UpdateEntity(tableName, data);

                        if (result != null && result.Flag == Errors.Ok)
                        {
                            AnsiConsole.MarkupLine($"[green]✓[/] Synchronized {data.Count} rows");
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] Error syncing: {result?.Message}");
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error syncing table: {ex.Message}");
            }
        }

        private void SyncAllTables(IDataSource source, IDataSource dest)
        {
            try
            {
                var entities = source.GetEntitesList()?.ToList();
                if (entities == null || entities.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No tables found in source[/]");
                    return;
                }

                AnsiConsole.MarkupLine($"[cyan]Synchronizing {entities.Count} tables...[/]");

                var progress = AnsiConsole.Progress();
                progress.Start(ctx =>
                {
                    var task = ctx.AddTask("[cyan]Syncing tables[/]", maxValue: entities.Count);

                    foreach (var entityName in entities)
                    {
                        try
                        {
                            task.Description = $"[cyan]Syncing {entityName}[/]";
                            SyncTable(source, dest, entityName);
                            task.Increment(1);
                        }
                        catch (Exception ex)
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] Error syncing {entityName}: {ex.Message}");
                        }
                    }
                });

                AnsiConsole.MarkupLine($"[green]✓[/] Synchronization complete");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }

        private void ShowSyncStatus()
        {
            AnsiConsole.MarkupLine("[cyan]Sync Status:[/]");
            AnsiConsole.MarkupLine("[dim]No active synchronization jobs[/]");
            AnsiConsole.MarkupLine("[dim]Use 'sync start' to begin synchronization[/]");
        }

        public bool CanExecute() => _editor != null;

        public string[] GetExamples()
        {
            return new[]
            {
                "sync start --source db1 --dest db2 --table Users",
                "sync start --source sourceDb --dest destDb --all",
                "sync status"
            };
        }
    }
}
