using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.DataView;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.DataView
{
    /// <summary>
    /// Partial: Join Graph Intelligence — auto-detect joins, shortest path, adjacency list.
    /// </summary>
    public partial class DataViewDataSource
    {
        #region "Join Graph Intelligence"

        /// <inheritdoc/>
        public IErrorsInfo AutoDetectJoins()
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                foreach (var ent in DataView.Entities)
                {
                    if (string.IsNullOrWhiteSpace(ent.DataSourceID)) continue;
                    var src = DMEEditor.GetDataSource(ent.DataSourceID);
                    if (src == null) continue;
                    if (src.ConnectionStatus != System.Data.ConnectionState.Open)
                        src.Openconnection();

                    var relations = src.GetEntityStructure(ent.DatasourceEntityName, false)?.Relations;
                    if (relations == null) continue;

                    foreach (var rel in relations)
                    {
                        // Only add if the related entity is also in the view and the join doesn't already exist
                        bool relEntityInView = DataView.Entities.Any(e =>
                            e.EntityName.Equals(rel.RalationName, StringComparison.OrdinalIgnoreCase));
                        if (!relEntityInView) continue;

                        bool alreadyExists = DataView.JoinDefinitions.Any(j =>
                            j.LeftEntityName.Equals(ent.EntityName, StringComparison.OrdinalIgnoreCase) &&
                            j.RightEntityName.Equals(rel.RalationName, StringComparison.OrdinalIgnoreCase) &&
                            j.LeftColumn.Equals(rel.EntityColumnID, StringComparison.OrdinalIgnoreCase));
                        if (alreadyExists) continue;

                        DataView.JoinDefinitions.Add(new FederatedJoinDefinition
                        {
                            GuidID            = Guid.NewGuid().ToString(),
                            LeftEntityName    = ent.EntityName.ToUpperInvariant(),
                            LeftColumn        = rel.EntityColumnID,
                            LeftDataSourceID  = ent.DataSourceID,
                            RightEntityName   = rel.RalationName.ToUpperInvariant(),
                            RightColumn       = rel.RelatedEntityColumnID,
                            RightDataSourceID = ent.DataSourceID,   // same source FK
                            JoinType          = FederatedJoinType.Inner,
                            IsManuallyDefined = false,
                            Description       = $"Auto: {ent.EntityName}.{rel.EntityColumnID} → {rel.RalationName}.{rel.RelatedEntityColumnID}"
                        });
                    }
                }
                InvalidateCache();
                DMEEditor.AddLogMessage("Info", "AutoDetectJoins complete.", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage(ex.Message, "AutoDetectJoins failed.", DateTime.Now, -1, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }

        /// <inheritdoc/>
        public List<string> FindJoinPath(string fromEntityName, string toEntityName)
        {
            // BFS over the join graph
            var graph = GetJoinGraph();
            string from = fromEntityName?.ToUpperInvariant();
            string to   = toEntityName?.ToUpperInvariant();

            if (!graph.ContainsKey(from) || !graph.ContainsKey(to)) return null;
            if (from == to) return new List<string> { from };

            var visited = new HashSet<string>();
            var queue   = new Queue<List<string>>();
            queue.Enqueue(new List<string> { from });

            while (queue.Count > 0)
            {
                var path = queue.Dequeue();
                string current = path[path.Count - 1];
                if (current == to) return path;
                if (!visited.Add(current)) continue;

                if (graph.TryGetValue(current, out var neighbors))
                    foreach (var n in neighbors)
                        if (!visited.Contains(n))
                        {
                            var newPath = new List<string>(path) { n };
                            queue.Enqueue(newPath);
                        }
            }
            return null; // no path
        }

        /// <inheritdoc/>
        public Dictionary<string, List<string>> GetJoinGraph()
        {
            var graph = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            // Initialise all entity nodes
            foreach (var e in DataView.Entities)
            {
                string key = e.EntityName.ToUpperInvariant();
                if (!graph.ContainsKey(key))
                    graph[key] = new List<string>();
            }

            // Add edges from joins (undirected)
            foreach (var j in DataView.JoinDefinitions)
            {
                string l = j.LeftEntityName?.ToUpperInvariant();
                string r = j.RightEntityName?.ToUpperInvariant();
                if (l == null || r == null) continue;

                if (!graph.ContainsKey(l)) graph[l] = new List<string>();
                if (!graph.ContainsKey(r)) graph[r] = new List<string>();

                if (!graph[l].Contains(r)) graph[l].Add(r);
                if (!graph[r].Contains(l)) graph[r].Add(l);
            }
            return graph;
        }

        #endregion "Join Graph Intelligence"
    }
}
