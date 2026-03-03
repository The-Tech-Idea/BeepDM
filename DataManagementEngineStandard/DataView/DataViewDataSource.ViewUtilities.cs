using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.DataView;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.DataView
{
    /// <summary>
    /// Partial: View-level utilities — clone, merge, datasource list, summary.
    /// </summary>
    public partial class DataViewDataSource
    {
        #region "View Utilities"

        /// <inheritdoc/>
        public IDMDataView CloneView(string newViewName)
        {
            try
            {
                // Deep clone via JSON round-trip
                string json   = JsonSerializer.Serialize(DataView as DMDataView);
                var    clone  = JsonSerializer.Deserialize<DMDataView>(json);
                clone.GuidID   = Guid.NewGuid().ToString();
                clone.VID      = Guid.NewGuid().ToString();
                clone.ViewName = newViewName;
                clone.ViewID   = 0;

                // Assign new GUIDs to all join definitions to prevent collisions
                foreach (var j in clone.JoinDefinitions)
                    j.GuidID = Guid.NewGuid().ToString();

                DMEEditor.AddLogMessage("Info", $"DataView cloned as '{newViewName}'.", DateTime.Now, 0, null, Errors.Ok);
                return clone;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage(ex.Message, "CloneView failed.", DateTime.Now, -1, null, Errors.Failed);
                return null;
            }
        }

        /// <inheritdoc/>
        public IErrorsInfo MergeView(IDMDataView otherView)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                if (otherView == null) return DMEEditor.ErrorObject;

                // Merge entities — skip duplicates by EntityName
                foreach (var ent in otherView.Entities)
                {
                    bool exists = DataView.Entities.Any(e =>
                        e.EntityName.Equals(ent.EntityName, StringComparison.OrdinalIgnoreCase));
                    if (!exists)
                    {
                        var clone = JsonSerializer.Deserialize<EntityStructure>(
                            JsonSerializer.Serialize(ent));
                        clone.Id = NextHearId();
                        DataView.Entities.Add(clone);
                    }
                }

                // Merge joins — skip duplicates by GuidID
                foreach (var j in otherView.JoinDefinitions)
                {
                    bool exists = DataView.JoinDefinitions.Any(x =>
                        x.GuidID.Equals(j.GuidID, StringComparison.OrdinalIgnoreCase));
                    if (!exists)
                    {
                        var clone = JsonSerializer.Deserialize<FederatedJoinDefinition>(
                            JsonSerializer.Serialize(j));
                        clone.GuidID = Guid.NewGuid().ToString();
                        DataView.JoinDefinitions.Add(clone);
                    }
                }

                InvalidateCache();
                DMEEditor.AddLogMessage("Info", $"Merged view '{otherView.ViewName}' into '{DataView.ViewName}'.", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage(ex.Message, "MergeView failed.", DateTime.Now, -1, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }

        /// <inheritdoc/>
        public List<string> GetAllDataSourceIDs()
        {
            return DataView.Entities
                .Where(e => !string.IsNullOrWhiteSpace(e.DataSourceID))
                .Select(e => e.DataSourceID)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <inheritdoc/>
        public ViewSummary GetViewSummary()
        {
            var errors    = ValidateView();
            var broken    = GetBrokenEntities();
            var unconnect = GetUnconnectedEntities();
            var cacheInfo = GetCacheStatus();

            return new ViewSummary
            {
                EntityCount        = DataView.Entities.Count,
                JoinCount          = DataView.JoinDefinitions.Count,
                ManualJoinCount    = DataView.JoinDefinitions.Count(j => j.IsManuallyDefined),
                DataSourceCount    = GetAllDataSourceIDs().Count,
                IsValid            = errors.Count == 0,
                Warnings           = errors,
                BrokenEntities     = broken,
                UnconnectedEntities= unconnect,
                CacheLastRefresh   = cacheInfo.LastRefresh,
                CacheTTLSeconds    = DataView.CacheTTLSeconds
            };
        }

        #endregion "View Utilities"
    }
}
