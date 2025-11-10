using BeepShell.Infrastructure;
using Spectre.Console;
using TheTechIdea.Beep.Editor;

namespace BeepShell.Extensions.Example
{
    /// <summary>
    /// Example workflow that performs ETL operation between two data sources
    /// </summary>
    public class DataSyncWorkflow : IShellWorkflow
    {
        private IDMEEditor _editor;

        public string WorkflowName => "data-sync";
        public string Description => "Synchronize data between two data sources";
        public string Category => "ETL";

        public void Initialize(IDMEEditor editor)
        {
            _editor = editor;
        }

        public async Task<WorkflowResult> ExecuteAsync(Dictionary<string, object> parameters)
        {
            var result = new WorkflowResult();
            var startTime = DateTime.Now;

            try
            {
                // Extract parameters
                var sourceDs = parameters["source"].ToString();
                var sourceTable = parameters["sourceTable"].ToString();
                var targetDs = parameters["target"].ToString();
                var targetTable = parameters["targetTable"].ToString();
                var batchSize = parameters.ContainsKey("batchSize") ? 
                    (int)parameters["batchSize"] : 1000;

                await AnsiConsole.Progress()
                    .Columns(new ProgressColumn[]
                    {
                        new TaskDescriptionColumn(),
                        new ProgressBarColumn(),
                        new PercentageColumn(),
                        new SpinnerColumn(),
                    })
                    .StartAsync(async ctx =>
                    {
                        var task = ctx.AddTask($"[green]Syncing {sourceTable}[/]");

                        // Step 1: Open connections
                        task.Description = "Opening connections...";
                        var source = _editor.GetDataSource(sourceDs);
                        var target = _editor.GetDataSource(targetDs);

                        if (source.ConnectionStatus != System.Data.ConnectionState.Open)
                            source.Openconnection();
                        if (target.ConnectionStatus != System.Data.ConnectionState.Open)
                            target.Openconnection();

                        task.Increment(20);

                        // Step 2: Read source data
                        task.Description = $"Reading from {sourceTable}...";
                        var data = source.GetEntity(sourceTable, null);
                        var totalRows = data.Rows.Count;
                        task.Increment(30);

                        // Step 3: Truncate or prepare target
                        task.Description = $"Preparing {targetTable}...";
                        // Optionally truncate target table
                        task.Increment(10);

                        // Step 4: Write in batches
                        task.Description = $"Writing {totalRows} rows...";
                        var written = 0;
                        
                        for (int i = 0; i < totalRows; i += batchSize)
                        {
                            var batch = data.Clone();
                            var end = Math.Min(i + batchSize, totalRows);
                            
                            for (int j = i; j < end; j++)
                            {
                                batch.ImportRow(data.Rows[j]);
                            }

                            target.UpdateEntity(targetTable, batch);
                            written += batch.Rows.Count;
                            
                            task.Increment((double)(batch.Rows.Count) / totalRows * 40);
                        }

                        result.Success = true;
                        result.Message = $"Successfully synced {written} rows";
                        result.OutputData["rowsProcessed"] = written;
                        result.OutputData["source"] = sourceDs;
                        result.OutputData["target"] = targetDs;
                    });
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = "Sync failed";
                result.Errors.Add(ex.Message);
                AnsiConsole.MarkupLine($"[red]✗[/] {ex.Message}");
            }

            result.Duration = DateTime.Now - startTime;
            return result;
        }

        public bool ValidateParameters(Dictionary<string, object> parameters)
        {
            var required = new[] { "source", "sourceTable", "target", "targetTable" };
            
            foreach (var param in required)
            {
                if (!parameters.ContainsKey(param) || parameters[param] == null)
                {
                    AnsiConsole.MarkupLine($"[red]Missing required parameter:[/] {param}");
                    return false;
                }
            }

            return true;
        }

        public List<WorkflowParameter> GetRequiredParameters()
        {
            return new List<WorkflowParameter>
            {
                new() { Name = "source", Description = "Source data source name", 
                       ParameterType = typeof(string), Required = true },
                new() { Name = "sourceTable", Description = "Source table/entity name", 
                       ParameterType = typeof(string), Required = true },
                new() { Name = "target", Description = "Target data source name", 
                       ParameterType = typeof(string), Required = true },
                new() { Name = "targetTable", Description = "Target table/entity name", 
                       ParameterType = typeof(string), Required = true },
                new() { Name = "batchSize", Description = "Number of rows per batch", 
                       ParameterType = typeof(int), Required = false, DefaultValue = 1000 }
            };
        }
    }
}
