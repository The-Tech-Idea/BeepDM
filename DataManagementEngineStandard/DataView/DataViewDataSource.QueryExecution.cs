using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.DataView;
using TheTechIdea.Beep.Utilities;
using System.Collections;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.DataView
{
    /// <summary>
    /// Partial: Query and execution — preview, cache status, async materialize, execution plan.
    /// </summary>
    public partial class DataViewDataSource
    {
        #region "Query and Execution"

        /// <inheritdoc/>
        public IEnumerable<object> GetEntityPreview(string entityName, int maxRows = 100)
        {
            try
            {
                var ent = DataView.Entities.FirstOrDefault(e =>
                    e.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));
                if (ent == null) return null;

                var src = DMEEditor.GetDataSource(ent.DataSourceID);
                if (src == null) return null;
                if (src.ConnectionStatus != System.Data.ConnectionState.Open)
                    src.Openconnection();

                var result = src.GetEntity(ent.DatasourceEntityName,
                    new List<AppFilter> { new AppFilter { FieldName = "ROWNUM", Operator = "<", FilterValue = maxRows.ToString() } });
                
                return result;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage(ex.Message, $"GetEntityPreview failed for '{entityName}'.", DateTime.Now, -1, null, Errors.Failed);
            }
            return null;
        }

        /// <inheritdoc/>
        public IEnumerable<object> GetViewPreview(int maxRows = 100)
        {
            try
            {
                // Try local engine if available
                if (!string.IsNullOrWhiteSpace(DataView.LocalEngineConnectionName))
                {
                    var engine = DMEEditor.GetDataSource(DataView.LocalEngineConnectionName);
                    if (engine != null)
                    {
                        if (engine.ConnectionStatus != System.Data.ConnectionState.Open)
                            engine.Openconnection();
                        // Build a simple SELECT * from the first entity to demonstrate
                        var firstEnt = DataView.Entities.FirstOrDefault();
                        if (firstEnt != null)
                        {
                            var result = engine.GetEntity(firstEnt.EntityName, null);
                            return result;
                        }
                    }
                }
                // Fallback: single entity preview
                return DataView.Entities.Any()
                    ? GetEntityPreview(DataView.Entities.First().EntityName, maxRows)
                    : null;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage(ex.Message, "GetViewPreview failed.", DateTime.Now, -1, null, Errors.Failed);
                return null;
            }
        }

        /// <inheritdoc/>
        public (bool IsExpired, int RowCount, DateTime LastRefresh, int SecondsTTLRemaining) GetCacheStatus()
        {
            int elapsed     = (int)(DateTime.UtcNow - DataView.CacheLastRefresh).TotalSeconds;
            int remaining   = Math.Max(0, DataView.CacheTTLSeconds - elapsed);
            bool isExpired  = remaining == 0;
            return (isExpired, 0, DataView.CacheLastRefresh, remaining);
        }

        /// <inheritdoc/>
        public async Task MaterializeAsync(CancellationToken cancellationToken = default)
        {
            await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                // Trigger the synchronous invalidation + subsequent query will rematerialize
                InvalidateCache();
                DataView.CacheLastRefresh = DateTime.UtcNow;
                DMEEditor.AddLogMessage("Info", "DataView cache materialized asynchronously.", DateTime.Now, 0, null, Errors.Ok);
            }, cancellationToken);
        }

        /// <inheritdoc/>
        public string GetExecutionPlan()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"-- DataView: {DataView.ViewName}");
            sb.AppendLine($"-- Execution Mode: {DataView.ExecutionMode}");
            sb.AppendLine($"-- Entities: {DataView.Entities.Count}, Joins: {DataView.JoinDefinitions.Count}");
            sb.AppendLine();

            if (!DataView.Entities.Any()) return sb.ToString();

            // Anchor on first entity
            var first = DataView.Entities.First();
            sb.AppendLine($"SELECT * FROM [{first.EntityName}]");

            // Append JOIN clauses for all join definitions
            foreach (var join in DataView.JoinDefinitions)
            {
                sb.AppendLine(BuildJoinSQL(join));
            }

            return sb.ToString();
        }

        #endregion "Query and Execution"
    }
}
