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
    /// Partial: Filter Management — per-entity WHERE filters stored in EntityStructure.CustomBuildQuery.
    /// </summary>
    public partial class DataViewDataSource
    {
        #region "Filter Management"

        /// <inheritdoc/>
        public IErrorsInfo SetEntityFilter(string entityName, string filterExpression)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                var ent = DataView.Entities.FirstOrDefault(e =>
                    e.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));
                if (ent == null)
                {
                    DMEEditor.AddLogMessage("Beep", $"SetEntityFilter: entity '{entityName}' not found.", DateTime.Now, -1, null, Errors.Failed);
                    return DMEEditor.ErrorObject;
                }
                // CustomBuildQuery is already part of EntityStructure — no new property needed
                ent.CustomBuildQuery = filterExpression;
                InvalidateCache(); // filter change requires re-materialisation
                DMEEditor.AddLogMessage("Info", $"Filter set on '{entityName}': {filterExpression}", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage(ex.Message, "SetEntityFilter failed.", DateTime.Now, -1, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }

        /// <inheritdoc/>
        public string GetEntityFilter(string entityName)
        {
            return DataView.Entities
                .FirstOrDefault(e => e.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase))
                ?.CustomBuildQuery;
        }

        /// <inheritdoc/>
        public IErrorsInfo ClearEntityFilter(string entityName)
        {
            return SetEntityFilter(entityName, null);
        }

        /// <inheritdoc/>
        public void ClearAllFilters()
        {
            foreach (var ent in DataView.Entities)
                ent.CustomBuildQuery = null;
            InvalidateCache();
            DMEEditor.AddLogMessage("Info", "All entity filters cleared.", DateTime.Now, 0, null, Errors.Ok);
        }

        #endregion "Filter Management"
    }
}
