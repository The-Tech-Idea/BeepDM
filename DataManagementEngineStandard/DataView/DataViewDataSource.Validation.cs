using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.DataView;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.DataView
{
    /// <summary>
    /// Partial: Validation and health-check operations.
    /// </summary>
    public partial class DataViewDataSource
    {
        #region "Validation and Health"

        /// <inheritdoc/>
        public List<string> ValidateView()
        {
            var errors = new List<string>();
            errors.AddRange(ValidateEntities());
            errors.AddRange(ValidateJoins());
            return errors;
        }

        /// <inheritdoc/>
        public List<string> ValidateEntities()
        {
            var errors = new List<string>();
            try
            {
                foreach (var ent in DataView.Entities)
                {
                    if (string.IsNullOrWhiteSpace(ent.DataSourceID))
                    {
                        errors.Add($"Entity '{ent.EntityName}': no DataSourceID assigned.");
                        continue;
                    }
                    var src = DMEEditor.GetDataSource(ent.DataSourceID);
                    if (src == null)
                    {
                        errors.Add($"Entity '{ent.EntityName}': datasource '{ent.DataSourceID}' not found.");
                        continue;
                    }
                    // Try to open (non-destructive)
                    try
                    {
                        if (src.ConnectionStatus != System.Data.ConnectionState.Open)
                            src.Openconnection();
                        if (src.ConnectionStatus != System.Data.ConnectionState.Open)
                            errors.Add($"Entity '{ent.EntityName}': datasource '{ent.DataSourceID}' could not be opened.");
                    }
                    catch
                    {
                        errors.Add($"Entity '{ent.EntityName}': datasource '{ent.DataSourceID}' threw on open.");
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"ValidateEntities: unexpected error — {ex.Message}");
            }
            return errors;
        }

        /// <inheritdoc/>
        public List<string> GetBrokenEntities()
        {
            var broken = new List<string>();
            foreach (var ent in DataView.Entities)
            {
                var src = DMEEditor.GetDataSource(ent.DataSourceID);
                if (src == null)
                {
                    broken.Add(ent.EntityName);
                    continue;
                }
                try
                {
                    if (src.ConnectionStatus != System.Data.ConnectionState.Open)
                        src.Openconnection();
                    if (src.ConnectionStatus != System.Data.ConnectionState.Open)
                        broken.Add(ent.EntityName);
                }
                catch
                {
                    broken.Add(ent.EntityName);
                }
            }
            return broken;
        }

        /// <inheritdoc/>
        public List<string> GetUnconnectedEntities()
        {
            var graph = GetJoinGraph();
            return DataView.Entities
                .Select(e => e.EntityName.ToUpperInvariant())
                .Where(name => !graph.ContainsKey(name) || graph[name].Count == 0)
                .ToList();
        }

        /// <inheritdoc/>
        public List<string> DetectSchemaChanges(string entityName)
        {
            var changes = new List<string>();
            try
            {
                var ent = DataView.Entities.FirstOrDefault(e =>
                    e.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));
                if (ent == null)
                {
                    changes.Add($"Entity '{entityName}' not found in DataView.");
                    return changes;
                }

                var src = DMEEditor.GetDataSource(ent.DataSourceID);
                if (src == null)
                {
                    changes.Add($"DataSource '{ent.DataSourceID}' not available.");
                    return changes;
                }

                if (src.ConnectionStatus != System.Data.ConnectionState.Open)
                    src.Openconnection();

                var live = src.GetEntityStructure(ent.DatasourceEntityName, true);
                if (live == null)
                {
                    changes.Add($"Entity '{ent.DatasourceEntityName}' no longer exists in source.");
                    return changes;
                }

                // Fields in view but not in source (removed)
                foreach (var f in ent.Fields)
                {
                    if (!live.Fields.Any(lf => lf.FieldName.Equals(f.FieldName, StringComparison.OrdinalIgnoreCase)))
                        changes.Add($"REMOVED: field '{f.FieldName}' no longer exists in source.");
                }
                // Fields in source but not in view (added)
                foreach (var lf in live.Fields)
                {
                    if (!ent.Fields.Any(f => f.FieldName.Equals(lf.FieldName, StringComparison.OrdinalIgnoreCase)))
                        changes.Add($"ADDED: field '{lf.FieldName}' ({lf.Fieldtype}) exists in source but not in view.");
                }
            }
            catch (Exception ex)
            {
                changes.Add($"DetectSchemaChanges error: {ex.Message}");
            }
            return changes;
        }

        #endregion "Validation and Health"
    }
}
