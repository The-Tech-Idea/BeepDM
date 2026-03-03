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
    /// Partial: Field/Column management — add, remove, get, refresh schema.
    /// </summary>
    public partial class DataViewDataSource
    {
        #region "Field Management"

        /// <inheritdoc/>
        public List<EntityField> GetEntityFields(string entityName)
        {
            var ent = DataView.Entities.FirstOrDefault(e =>
                e.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));
            return ent?.Fields ?? new List<EntityField>();
        }

        /// <inheritdoc/>
        public IErrorsInfo AddFieldToEntity(string entityName, EntityField field)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                var ent = DataView.Entities.FirstOrDefault(e =>
                    e.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));
                if (ent == null)
                {
                    DMEEditor.AddLogMessage("Beep", $"AddFieldToEntity: entity '{entityName}' not found.", DateTime.Now, -1, null, Errors.Failed);
                    return DMEEditor.ErrorObject;
                }
                if (ent.Fields.Any(f => f.FieldName.Equals(field.FieldName, StringComparison.OrdinalIgnoreCase)))
                {
                    DMEEditor.AddLogMessage("Beep", $"AddFieldToEntity: field '{field.FieldName}' already exists on '{entityName}'.", DateTime.Now, -1, null, Errors.Failed);
                    return DMEEditor.ErrorObject;
                }
                ent.Fields.Add(field);
                DMEEditor.AddLogMessage("Info", $"Field '{field.FieldName}' added to entity '{entityName}'.", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage(ex.Message, "AddFieldToEntity failed.", DateTime.Now, -1, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }

        /// <inheritdoc/>
        public IErrorsInfo RemoveFieldFromEntity(string entityName, string fieldName)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                var ent = DataView.Entities.FirstOrDefault(e =>
                    e.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));
                if (ent == null)
                {
                    DMEEditor.AddLogMessage("Beep", $"RemoveFieldFromEntity: entity '{entityName}' not found.", DateTime.Now, -1, null, Errors.Failed);
                    return DMEEditor.ErrorObject;
                }
                int removed = ent.Fields.RemoveAll(f =>
                    f.FieldName.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
                if (removed == 0)
                    DMEEditor.AddLogMessage("Beep", $"RemoveFieldFromEntity: field '{fieldName}' not found.", DateTime.Now, -1, null, Errors.Failed);
                else
                    DMEEditor.AddLogMessage("Info", $"Field '{fieldName}' removed from entity '{entityName}'.", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage(ex.Message, "RemoveFieldFromEntity failed.", DateTime.Now, -1, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }

        /// <inheritdoc/>
        public List<string> RefreshEntitySchema(string entityName)
        {
            var changes = DetectSchemaChanges(entityName);
            try
            {
                var ent = DataView.Entities.FirstOrDefault(e =>
                    e.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));
                if (ent == null) return changes;

                var src = DMEEditor.GetDataSource(ent.DataSourceID);
                if (src == null) return changes;

                if (src.ConnectionStatus != System.Data.ConnectionState.Open)
                    src.Openconnection();

                var live = src.GetEntityStructure(ent.DatasourceEntityName, true);
                if (live?.Fields != null)
                {
                    ent.Fields = live.Fields;
                    DMEEditor.AddLogMessage("Info", $"Schema refreshed for entity '{entityName}'.", DateTime.Now, 0, null, Errors.Ok);
                }
            }
            catch (Exception ex)
            {
                changes.Add($"RefreshEntitySchema error: {ex.Message}");
                DMEEditor.AddLogMessage(ex.Message, "RefreshEntitySchema failed.", DateTime.Now, -1, null, Errors.Failed);
            }
            return changes;
        }

        #endregion "Field Management"
    }
}
