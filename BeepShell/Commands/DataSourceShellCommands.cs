using System;
using System.CommandLine;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using BeepShell.Infrastructure;
using Spectre.Console;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utils;
using TheTechIdea.Beep.Report;

namespace BeepShell.Commands
{
    /// <summary>
    /// Data source management commands for BeepShell
    /// Uses persistent DMEEditor with open connections
    /// </summary>
    public class DataSourceShellCommands : IShellCommand
    {
        private IDMEEditor _editor;

        public string CommandName => "datasource";
        public string Description => "Manage data sources and connections";
        public string Category => "Data";
        public string Version => "1.0.0";
        public string Author => "BeepDM Team";
        public string[] Aliases => new[] { "ds", "source" };

        public void Initialize(IDMEEditor editor)
        {
            _editor = editor;
        }

        public Command BuildCommand()
        {
            var dsCommand = new Command("datasource", Description);
            dsCommand.AddAlias("ds");

            // datasource list
            var listCommand = new Command("list", "List all data sources");
            listCommand.SetHandler(() => ListDataSources());
            dsCommand.AddCommand(listCommand);

            // datasource open
            var openCommand = new Command("open", "Open a data source connection");
            var nameArg = new Argument<string>("name", "Data source name");
            openCommand.AddArgument(nameArg);
            openCommand.SetHandler((name) => OpenDataSource(name), nameArg);
            dsCommand.AddCommand(openCommand);

            // datasource close
            var closeCommand = new Command("close", "Close a data source connection");
            var closeNameArg = new Argument<string>("name", "Data source name");
            closeCommand.AddArgument(closeNameArg);
            closeCommand.SetHandler((name) => CloseDataSource(name), closeNameArg);
            dsCommand.AddCommand(closeCommand);

            // datasource info
            var infoCommand = new Command("info", "Show data source information");
            var infoNameArg = new Argument<string>("name", "Data source name");
            infoCommand.AddArgument(infoNameArg);
            infoCommand.SetHandler((name) => ShowDataSourceInfo(name), infoNameArg);
            dsCommand.AddCommand(infoCommand);

            // datasource entities
            var entitiesCommand = new Command("entities", "List entities in a data source");
            var entitiesNameArg = new Argument<string>("name", "Data source name");
            entitiesCommand.AddArgument(entitiesNameArg);
            entitiesCommand.SetHandler((name) => ListEntities(name), entitiesNameArg);
            dsCommand.AddCommand(entitiesCommand);

            // datasource test
            var testCommand = new Command("test", "Test connection to a data source");
            var testNameArg = new Argument<string>("name", "Data source name");
            testCommand.AddArgument(testNameArg);
            testCommand.SetHandler((name) => TestConnection(name), testNameArg);
            dsCommand.AddCommand(testCommand);

            // datasource schema
            var schemaCommand = new Command("schema", "Get entity schema/structure");
            var schemaNameArg = new Argument<string>("datasource", "Data source name");
            var entityArg = new Argument<string>("entity", "Entity name");
            schemaCommand.AddArgument(schemaNameArg);
            schemaCommand.AddArgument(entityArg);
            schemaCommand.SetHandler((ds, entity) => ShowEntitySchema(ds, entity), schemaNameArg, entityArg);
            dsCommand.AddCommand(schemaCommand);

            // datasource refresh
            var refreshCommand = new Command("refresh", "Refresh entity list");
            var refreshNameArg = new Argument<string>("name", "Data source name");
            refreshCommand.AddArgument(refreshNameArg);
            refreshCommand.SetHandler((name) => RefreshEntities(name), refreshNameArg);
            dsCommand.AddCommand(refreshCommand);

            // datasource query
            var queryCommand = new Command("query", "Execute query on data source");
            var queryDsArg = new Argument<string>("datasource", "Data source name");
            var querySqlArg = new Argument<string>("sql", "SQL query to execute");
            queryCommand.AddArgument(queryDsArg);
            queryCommand.AddArgument(querySqlArg);
            queryCommand.SetHandler((ds, sql) => ExecuteQuery(ds, sql), queryDsArg, querySqlArg);
            dsCommand.AddCommand(queryCommand);

            // datasource create
            var createCommand = new Command("create", "Create new entity in data source");
            var createDsArg = new Argument<string>("datasource", "Data source name");
            var createEntityArg = new Argument<string>("entity", "Entity name to create");
            createCommand.AddArgument(createDsArg);
            createCommand.AddArgument(createEntityArg);
            createCommand.SetHandler((ds, entity) => CreateEntity(ds, entity), createDsArg, createEntityArg);
            dsCommand.AddCommand(createCommand);

            // datasource view - View data with pagination
            var viewCommand = new Command("view", "View entity data with pagination");
            var viewDsArg = new Argument<string>("datasource", "Data source name");
            var viewEntityArg = new Argument<string>("entity", "Entity name");
            var pageOption = new Option<int>("--page", () => 1, "Page number (default: 1)");
            var pageSizeOption = new Option<int>("--size", () => 25, "Page size (default: 25)");
            var filterOption = new Option<string>("--filter", "Filter condition (SQL WHERE clause)");
            
            viewCommand.AddArgument(viewDsArg);
            viewCommand.AddArgument(viewEntityArg);
            viewCommand.AddOption(pageOption);
            viewCommand.AddOption(pageSizeOption);
            viewCommand.AddOption(filterOption);
            viewCommand.SetHandler((ds, entity, page, size, filter) => 
                ViewData(ds, entity, page, size, filter), 
                viewDsArg, viewEntityArg, pageOption, pageSizeOption, filterOption);
            dsCommand.AddCommand(viewCommand);

            // datasource export - Export data to CSV
            var exportCommand = new Command("export", "Export entity data to CSV");
            var exportDsArg = new Argument<string>("datasource", "Data source name");
            var exportEntityArg = new Argument<string>("entity", "Entity name");
            var exportPathArg = new Argument<string>("output", "Output file path");
            var exportFilterOption = new Option<string>("--filter", "Filter condition");
            var exportLimitOption = new Option<int?>("--limit", "Maximum rows to export");
            
            exportCommand.AddArgument(exportDsArg);
            exportCommand.AddArgument(exportEntityArg);
            exportCommand.AddArgument(exportPathArg);
            exportCommand.AddOption(exportFilterOption);
            exportCommand.AddOption(exportLimitOption);
            exportCommand.SetHandler((ds, entity, output, filter, limit) => 
                ExportToCSV(ds, entity, output, filter, limit), 
                exportDsArg, exportEntityArg, exportPathArg, exportFilterOption, exportLimitOption);
            dsCommand.AddCommand(exportCommand);

            // datasource count - Count records in entity
            var countCommand = new Command("count", "Count records in entity");
            var countDsArg = new Argument<string>("datasource", "Data source name");
            var countEntityArg = new Argument<string>("entity", "Entity name");
            var countFilterOption = new Option<string>("--filter", "Filter condition");
            
            countCommand.AddArgument(countDsArg);
            countCommand.AddArgument(countEntityArg);
            countCommand.AddOption(countFilterOption);
            countCommand.SetHandler((ds, entity, filter) => 
                CountRecords(ds, entity, filter), 
                countDsArg, countEntityArg, countFilterOption);
            dsCommand.AddCommand(countCommand);

            return dsCommand;
        }

        private void ListDataSources()
        {
            try
            {
                if (_editor.DataSources == null || _editor.DataSources.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No data sources available[/]");
                    return;
                }

                var table = new Table();
                table.Border = TableBorder.Rounded;
                table.AddColumn("[cyan]Name[/]");
                table.AddColumn("[cyan]Type[/]");
                table.AddColumn("[cyan]Category[/]");
                table.AddColumn("[cyan]Status[/]");
                table.AddColumn("[cyan]Entities[/]");

                foreach (var ds in _editor.DataSources)
                {
                    var statusColor = ds.ConnectionStatus == System.Data.ConnectionState.Open ? "green" : "dim";
                    var status = ds.ConnectionStatus == System.Data.ConnectionState.Open ? "Open" : "Closed";
                    var entityCount = ds.Entities?.Count ?? 0;

                    table.AddRow(
                        ds.DatasourceName,
                        ds.DatasourceType.ToString(),
                        ds.Category.ToString(),
                        $"[{statusColor}]{status}[/]",
                        entityCount.ToString()
                    );
                }

                AnsiConsole.Write(table);
                AnsiConsole.MarkupLine($"\n[dim]Total: {_editor.DataSources.Count} data sources[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error listing data sources: {ex.Message}");
            }
        }

        private void OpenDataSource(string name)
        {
            try
            {
                var ds = _editor.GetDataSource(name);
                if (ds == null)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Data source '{name}' not found");
                    return;
                }

                if (ds.ConnectionStatus == System.Data.ConnectionState.Open)
                {
                    AnsiConsole.MarkupLine($"[yellow]![/] Data source '{name}' is already open");
                    return;
                }

                AnsiConsole.Status()
                    .Start($"Opening connection to '{name}'...", ctx =>
                    {
                        _editor.OpenDataSource(name);
                    });

                AnsiConsole.MarkupLine($"[green]✓[/] Data source '{name}' opened successfully");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error opening data source: {ex.Message}");
            }
        }

        private void CloseDataSource(string name)
        {
            try
            {
                var ds = _editor.GetDataSource(name);
                if (ds == null)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Data source '{name}' not found");
                    return;
                }

                _editor.CloseDataSource(name);
                AnsiConsole.MarkupLine($"[green]✓[/] Data source '{name}' closed");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error closing data source: {ex.Message}");
            }
        }

        private void ShowDataSourceInfo(string name)
        {
            try
            {
                var ds = _editor.GetDataSource(name);
                if (ds == null)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Data source '{name}' not found");
                    return;
                }

                var status = ds.ConnectionStatus == System.Data.ConnectionState.Open ? "[green]Open[/]" : "[dim]Closed[/]";
                var info = new Panel(new Markup(
                    $"[cyan]Name:[/] {ds.DatasourceName}\n" +
                    $"[cyan]Type:[/] {ds.DatasourceType}\n" +
                    $"[cyan]Category:[/] {ds.Category}\n" +
                    $"[cyan]Status:[/] {status}\n" +
                    $"[cyan]Entities:[/] {ds.Entities?.Count ?? 0}\n" +
                    $"[cyan]ID:[/] {ds.Id}"
                ));
                info.Header = new PanelHeader($"[green]Data Source: {name}[/]");
                info.Border = BoxBorder.Rounded;

                AnsiConsole.Write(info);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error showing data source info: {ex.Message}");
            }
        }

        private void ListEntities(string name)
        {
            try
            {
                var ds = _editor.GetDataSource(name);
                if (ds == null)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Data source '{name}' not found");
                    return;
                }

                if (ds.ConnectionStatus != System.Data.ConnectionState.Open)
                {
                    AnsiConsole.MarkupLine($"[yellow]![/] Opening connection to '{name}'...");
                    _editor.OpenDataSource(name);
                }

                var entities = ds.GetEntitesList()?.ToList();
                if (entities == null || entities.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No entities found[/]");
                    return;
                }

                var table = new Table();
                table.Border = TableBorder.Rounded;
                table.AddColumn("[cyan]Entity Name[/]");
                table.AddColumn("[cyan]Type[/]");

                foreach (var entityName in entities)
                {
                    table.AddRow(
                        entityName,
                        "Entity"
                    );
                }

                AnsiConsole.Write(table);
                AnsiConsole.MarkupLine($"\n[dim]Total: {entities.Count} entities[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error listing entities: {ex.Message}");
            }
        }

        private void TestConnection(string name)
        {
            try
            {
                var ds = _editor.GetDataSource(name);
                if (ds == null)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Data source '{name}' not found");
                    return;
                }

                bool success = false;
                AnsiConsole.Status()
                    .Start($"Testing connection to '{name}'...", ctx =>
                    {
                        try
                        {
                            var wasOpen = ds.ConnectionStatus == System.Data.ConnectionState.Open;
                            
                            if (!wasOpen)
                                _editor.OpenDataSource(name);

                            success = ds.ConnectionStatus == System.Data.ConnectionState.Open;

                            if (!wasOpen && success)
                                _editor.CloseDataSource(name);
                        }
                        catch
                        {
                            success = false;
                        }
                    });

                if (success)
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] Connection test successful");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Connection test failed");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error testing connection: {ex.Message}");
            }
        }

        private void ShowEntitySchema(string datasourceName, string entityName)
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
                    AnsiConsole.MarkupLine($"[yellow]![/] Opening connection...");
                    _editor.OpenDataSource(datasourceName);
                }

                var structure = ds.GetEntityStructure(entityName, true);
                if (structure == null)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Entity '{entityName}' not found");
                    return;
                }

                // Display entity information
                var panel = new Panel(new Markup(
                    $"[cyan]Entity:[/] {structure.EntityName}\n" +
                    $"[cyan]Schema:[/] {structure.SchemaOrOwnerOrDatabase}\n" +
                    $"[cyan]Type:[/] {structure.EntityType}\n" +
                    $"[cyan]Editable:[/] {structure.Editable}\n" +
                    $"[cyan]Fields:[/] {structure.Fields?.Count ?? 0}"
                ));
                panel.Header = new PanelHeader($"[green]Entity Structure[/]");
                panel.Border = BoxBorder.Rounded;
                AnsiConsole.Write(panel);

                // Display fields
                if (structure.Fields != null && structure.Fields.Count > 0)
                {
                    var table = new Table();
                    table.Border = TableBorder.Rounded;
                    table.AddColumn("[cyan]Field Name[/]");
                    table.AddColumn("[cyan]Type[/]");
                    table.AddColumn("[cyan]Size[/]");
                    table.AddColumn("[cyan]Nullable[/]");
                    table.AddColumn("[cyan]Key[/]");
                    table.AddColumn("[cyan]Identity[/]");

                    foreach (var field in structure.Fields)
                    {
                        table.AddRow(
                            field.fieldname ?? "",
                            field.fieldtype ?? "",
                            field.Size1.ToString(),
                            field.AllowDBNull ? "[green]Yes[/]" : "[red]No[/]",
                            field.IsKey ? "[green]✓[/]" : "",
                            field.IsAutoIncrement ? "[green]✓[/]" : ""
                        );
                    }

                    AnsiConsole.Write(table);
                }

                // Display primary keys
                if (structure.PrimaryKeys != null && structure.PrimaryKeys.Count > 0)
                {
                    AnsiConsole.MarkupLine($"\n[cyan]Primary Keys:[/] {string.Join(", ", structure.PrimaryKeys.Select(k => k.fieldname))}");
                }

                // Display relations
                if (structure.Relations != null && structure.Relations.Count > 0)
                {
                    AnsiConsole.MarkupLine($"\n[cyan]Relations:[/] {structure.Relations.Count}");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error showing entity schema: {ex.Message}");
            }
        }

        private void RefreshEntities(string name)
        {
            try
            {
                var ds = _editor.GetDataSource(name);
                if (ds == null)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Data source '{name}' not found");
                    return;
                }

                if (ds.ConnectionStatus != System.Data.ConnectionState.Open)
                {
                    AnsiConsole.MarkupLine($"[yellow]![/] Opening connection...");
                    _editor.OpenDataSource(name);
                }

                AnsiConsole.Status()
                    .Start("Refreshing entity list...", ctx =>
                    {
                        ds.GetEntitesList();
                    });

                var count = ds.Entities?.Count ?? 0;
                AnsiConsole.MarkupLine($"[green]✓[/] Refreshed {count} entities");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error refreshing entities: {ex.Message}");
            }
        }

        private void ExecuteQuery(string datasourceName, string sql)
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
                    AnsiConsole.MarkupLine($"[yellow]![/] Opening connection...");
                    _editor.OpenDataSource(datasourceName);
                }

                object? result = null;
                AnsiConsole.Status()
                    .Start("Executing query...", ctx =>
                    {
                        result = ds.RunQuery(sql);
                    });

                if (result != null)
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] Query executed successfully");
                    
                    // If result is enumerable, show count
                    if (result is System.Collections.IEnumerable enumerable)
                    {
                        var count = enumerable.Cast<object>().Count();
                        AnsiConsole.MarkupLine($"[dim]Results: {count} rows[/]");
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine($"[yellow]![/] Query executed (no results)");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error executing query: {ex.Message}");
            }
        }

        private void CreateEntity(string datasourceName, string entityName)
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
                    AnsiConsole.MarkupLine($"[yellow]![/] Opening connection...");
                    _editor.OpenDataSource(datasourceName);
                }

                // Check if entity already exists
                if (ds.CheckEntityExist(entityName))
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Entity '{entityName}' already exists");
                    return;
                }

                // This would typically be done interactively with field definitions
                AnsiConsole.MarkupLine($"[yellow]![/] Interactive entity creation not yet implemented");
                AnsiConsole.MarkupLine($"[dim]Use 'class generate' to create entities from existing structures[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error creating entity: {ex.Message}");
            }
        }

        private void ViewData(string datasourceName, string entityName, int page, int pageSize, string? filter)
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
                    AnsiConsole.MarkupLine($"[yellow]![/] Opening connection...");
                    _editor.OpenDataSource(datasourceName);
                }

                // Build filter list
                var filters = new List<AppFilter>();
                if (!string.IsNullOrEmpty(filter))
                {
                    // Parse simple filter format: "FieldName=Value" or use CustomBuildQuery
                    filters.Add(new AppFilter
                    {
                        FilterValue = filter,
                        Operator = "Custom"
                    });
                }

                AnsiConsole.Status()
                    .Start($"Loading page {page} ({pageSize} rows)...", ctx =>
                    {
                        // Get paginated data
                        var pagedResult = ds.GetEntity(entityName, filters, page, pageSize);
                        
                        if (pagedResult == null || pagedResult.Data == null)
                        {
                            AnsiConsole.MarkupLine("[yellow]No data found[/]");
                            return;
                        }

                        // Convert to DataTable for display
                        DataTable? dt = null;
                        if (pagedResult.Data is DataTable dataTable)
                        {
                            dt = dataTable;
                        }
                        else if (pagedResult.Data is IEnumerable<object> enumerable)
                        {
                            var list = enumerable.ToList();
                            if (list.Count > 0)
                            {
                                var type = list[0].GetType();
                                dt = _editor.Utilfunction.ToDataTable(list, type);
                            }
                        }

                        if (dt == null || dt.Rows.Count == 0)
                        {
                            AnsiConsole.MarkupLine("[yellow]No data to display[/]");
                            return;
                        }

                        // Display data in table
                        var table = new Table();
                        table.Border = TableBorder.Rounded;
                        table.Title = new TableTitle($"[cyan]{entityName}[/] - Page {page} of {pagedResult.TotalPages}");

                        // Add columns (limit to avoid overflow)
                        var columnCount = Math.Min(dt.Columns.Count, 10);
                        for (int i = 0; i < columnCount; i++)
                        {
                            table.AddColumn($"[cyan]{dt.Columns[i].ColumnName}[/]");
                        }

                        if (dt.Columns.Count > columnCount)
                        {
                            table.AddColumn($"[dim]...({dt.Columns.Count - columnCount} more)[/]");
                        }

                        // Add rows
                        foreach (DataRow row in dt.Rows)
                        {
                            var rowValues = new List<string>();
                            for (int i = 0; i < columnCount; i++)
                            {
                                var value = row[i];
                                var displayValue = value == DBNull.Value ? "[dim]NULL[/]" : 
                                                   value?.ToString()?.Length > 50 ? 
                                                   value.ToString()!.Substring(0, 47) + "..." : 
                                                   value?.ToString() ?? "";
                                rowValues.Add(displayValue);
                            }

                            if (dt.Columns.Count > columnCount)
                            {
                                rowValues.Add("[dim]...[/]");
                            }

                            table.AddRow(rowValues.ToArray());
                        }

                        AnsiConsole.Write(table);

                        // Display pagination info
                        AnsiConsole.MarkupLine($"\n[dim]Showing rows {((page - 1) * pageSize) + 1}-{Math.Min(page * pageSize, pagedResult.TotalRecords)} of {pagedResult.TotalRecords} total[/]");
                        
                        if (pagedResult.HasNextPage)
                        {
                            AnsiConsole.MarkupLine($"[dim]Next page: datasource view {datasourceName} {entityName} --page {page + 1} --size {pageSize}[/]");
                        }

                        if (dt.Columns.Count > columnCount)
                        {
                            AnsiConsole.MarkupLine($"[yellow]![/] Showing first {columnCount} of {dt.Columns.Count} columns. Use export to see all data.");
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error viewing data: {ex.Message}");
            }
        }

        private void ExportToCSV(string datasourceName, string entityName, string outputPath, string? filter, int? limit)
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
                    AnsiConsole.MarkupLine($"[yellow]![/] Opening connection...");
                    _editor.OpenDataSource(datasourceName);
                }

                // Build filter list
                var filters = new List<AppFilter>();
                if (!string.IsNullOrEmpty(filter))
                {
                    filters.Add(new AppFilter
                    {
                        FilterValue = filter,
                        Operator = "Custom"
                    });
                }

                AnsiConsole.Status()
                    .Start($"Exporting data to {outputPath}...", ctx =>
                    {
                        // Get data
                        var data = ds.GetEntity(entityName, filters);
                        
                        if (data == null)
                        {
                            AnsiConsole.MarkupLine("[yellow]No data to export[/]");
                            return;
                        }

                        // Convert to DataTable
                        DataTable? dt = null;
                        var dataList = data.ToList();

                        if (dataList.Count == 0)
                        {
                            AnsiConsole.MarkupLine("[yellow]No data to export[/]");
                            return;
                        }

                        // Apply limit if specified
                        if (limit.HasValue && limit.Value > 0)
                        {
                            dataList = dataList.Take(limit.Value).ToList();
                        }

                        if (dataList[0] is DataRow)
                        {
                            dt = ((DataRow)dataList[0]).Table;
                        }
                        else
                        {
                            var type = dataList[0].GetType();
                            dt = _editor.Utilfunction.ToDataTable(dataList, type);
                        }

                        if (dt == null || dt.Rows.Count == 0)
                        {
                            AnsiConsole.MarkupLine("[yellow]No data to export[/]");
                            return;
                        }

                        // Export to CSV using Util
                        bool success = _editor.Utilfunction.ToCSVFile(dt, outputPath);

                        if (success)
                        {
                            AnsiConsole.MarkupLine($"[green]✓[/] Exported {dt.Rows.Count} rows to {outputPath}");
                            AnsiConsole.MarkupLine($"[dim]Columns: {dt.Columns.Count}[/]");
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] Failed to export data");
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error exporting data: {ex.Message}");
            }
        }

        private void CountRecords(string datasourceName, string entityName, string? filter)
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
                    AnsiConsole.MarkupLine($"[yellow]![/] Opening connection...");
                    _editor.OpenDataSource(datasourceName);
                }

                string query;
                if (!string.IsNullOrEmpty(filter))
                {
                    query = $"SELECT COUNT(*) FROM {entityName} WHERE {filter}";
                }
                else
                {
                    query = $"SELECT COUNT(*) FROM {entityName}";
                }

                AnsiConsole.Status()
                    .Start("Counting records...", ctx =>
                    {
                        var count = ds.GetScalar(query);
                        
                        AnsiConsole.MarkupLine($"[green]✓[/] Entity: [cyan]{entityName}[/]");
                        AnsiConsole.MarkupLine($"[green]✓[/] Record count: [yellow]{count:N0}[/]");
                        
                        if (!string.IsNullOrEmpty(filter))
                        {
                            AnsiConsole.MarkupLine($"[dim]Filter: {filter}[/]");
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error counting records: {ex.Message}");
                AnsiConsole.MarkupLine($"[dim]Note: Some data sources may not support COUNT queries[/]");
            }
        }

        public bool CanExecute() => _editor != null;

        public string[] GetExamples()
        {
            return new[]
            {
                "datasource list",
                "ds list",
                "datasource open mydb",
                "datasource info mydb",
                "datasource entities mydb",
                "datasource test mydb",
                "datasource schema mydb Customers",
                "datasource refresh mydb",
                "datasource query mydb \"SELECT * FROM Customers\"",
                "datasource view mydb Customers",
                "datasource view mydb Customers --page 2 --size 50",
                "datasource view mydb Customers --filter \"Country='USA'\"",
                "datasource export mydb Customers ./data.csv",
                "datasource export mydb Orders ./orders.csv --limit 1000",
                "datasource count mydb Customers",
                "datasource count mydb Orders --filter \"Status='Shipped'\"",
                "datasource create mydb NewTable",
                "datasource close mydb"
            };
        }
    }
}
