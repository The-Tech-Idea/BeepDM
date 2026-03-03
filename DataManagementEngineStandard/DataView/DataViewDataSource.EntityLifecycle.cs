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
    /// Partial: Entity Lifecycle operations — move, rename, duplicate, reorder, get by name.
    /// </summary>
    public partial class DataViewDataSource
    {
        #region "Entity Lifecycle"

        /// <inheritdoc/>
        public IErrorsInfo MoveEntity(int entityId, int newParentId, bool updateJoins = true)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                var ent = DataView.Entities.FirstOrDefault(e => e.Id == entityId);
                if (ent == null)
                {
                    DMEEditor.AddLogMessage("Beep", $"MoveEntity: entity id={entityId} not found.", DateTime.Now, -1, null, Errors.Failed);
                    return DMEEditor.ErrorObject;
                }

                int oldParentId = ent.ParentId;
                if (updateJoins && oldParentId != 0)
                {
                    var oldParent = DataView.Entities.FirstOrDefault(e => e.Id == oldParentId);
                    if (oldParent != null)
                    {
                        string oldPN = oldParent.EntityName;
                        string entN  = ent.EntityName;
                        DataView.JoinDefinitions.RemoveAll(j =>
                            !j.IsManuallyDefined &&
                            ((j.LeftEntityName.Equals(oldPN, StringComparison.OrdinalIgnoreCase) &&
                              j.RightEntityName.Equals(entN, StringComparison.OrdinalIgnoreCase)) ||
                             (j.RightEntityName.Equals(oldPN, StringComparison.OrdinalIgnoreCase) &&
                              j.LeftEntityName.Equals(entN, StringComparison.OrdinalIgnoreCase))));
                    }
                }

                ent.ParentId = newParentId;
                InvalidateCache();
                DMEEditor.AddLogMessage("Info", $"Entity '{ent.EntityName}' moved from parent {oldParentId} → {newParentId}.", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage(ex.Message, "MoveEntity failed.", DateTime.Now, -1, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }

        /// <inheritdoc/>
        public List<FederatedJoinDefinition> GetAutoJoinsToParent(int entityId)
        {
            var ent = DataView.Entities.FirstOrDefault(e => e.Id == entityId);
            if (ent == null || ent.ParentId == 0) return new List<FederatedJoinDefinition>();

            var parent = DataView.Entities.FirstOrDefault(e => e.Id == ent.ParentId);
            if (parent == null) return new List<FederatedJoinDefinition>();

            string pn = parent.EntityName;
            string en = ent.EntityName;
            return DataView.JoinDefinitions
                .Where(j => !j.IsManuallyDefined &&
                    ((j.LeftEntityName.Equals(pn, StringComparison.OrdinalIgnoreCase) &&
                      j.RightEntityName.Equals(en, StringComparison.OrdinalIgnoreCase)) ||
                     (j.RightEntityName.Equals(pn, StringComparison.OrdinalIgnoreCase) &&
                      j.LeftEntityName.Equals(en, StringComparison.OrdinalIgnoreCase))))
                .ToList();
        }

        /// <inheritdoc/>
        public IErrorsInfo RenameEntity(int entityId, string newName)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                var ent = DataView.Entities.FirstOrDefault(e => e.Id == entityId);
                if (ent == null)
                {
                    DMEEditor.AddLogMessage("Beep", $"RenameEntity: entity id={entityId} not found.", DateTime.Now, -1, null, Errors.Failed);
                    return DMEEditor.ErrorObject;
                }

                string oldName = ent.EntityName;
                string nu = newName?.ToUpperInvariant() ?? oldName;

                // Update join references
                foreach (var j in DataView.JoinDefinitions)
                {
                    if (j.LeftEntityName.Equals(oldName, StringComparison.OrdinalIgnoreCase))
                        j.LeftEntityName = nu;
                    if (j.RightEntityName.Equals(oldName, StringComparison.OrdinalIgnoreCase))
                        j.RightEntityName = nu;
                }

                ent.EntityName = nu;
                ent.Caption    = string.IsNullOrWhiteSpace(ent.Caption) || ent.Caption.Equals(oldName, StringComparison.OrdinalIgnoreCase) ? newName : ent.Caption;

                DMEEditor.AddLogMessage("Info", $"Entity renamed: '{oldName}' → '{nu}'.", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage(ex.Message, "RenameEntity failed.", DateTime.Now, -1, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }

        /// <inheritdoc/>
        public EntityStructure DuplicateEntity(int entityId, string newName, int newParentId = 0)
        {
            try
            {
                var src = DataView.Entities.FirstOrDefault(e => e.Id == entityId);
                if (src == null) return null;

                // Deep clone by serialise/deserialise through JSON
                string json = System.Text.Json.JsonSerializer.Serialize(src);
                var clone   = System.Text.Json.JsonSerializer.Deserialize<EntityStructure>(json);

                clone.Id         = NextHearId();
                clone.EntityName = newName?.ToUpperInvariant() ?? (src.EntityName + "_COPY");
                clone.Caption    = newName ?? (src.Caption + " Copy");
                clone.ParentId   = newParentId;

                DataView.Entities.Add(clone);
                DMEEditor.AddLogMessage("Info", $"Entity duplicated: '{src.EntityName}' → '{clone.EntityName}'.", DateTime.Now, 0, null, Errors.Ok);
                return clone;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage(ex.Message, "DuplicateEntity failed.", DateTime.Now, -1, null, Errors.Failed);
                return null;
            }
        }

        /// <inheritdoc/>
        public IErrorsInfo ReorderEntities(List<int> orderedEntityIds)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                // Write an order index into OriginalEntityName's place — we use a parallel sort
                var reordered = new List<EntityStructure>();
                foreach (int id in orderedEntityIds)
                {
                    var e = DataView.Entities.FirstOrDefault(x => x.Id == id);
                    if (e != null) reordered.Add(e);
                }
                // Append any entities not in the list
                reordered.AddRange(DataView.Entities.Where(e => !orderedEntityIds.Contains(e.Id)));
                DataView.Entities = reordered;
                DMEEditor.AddLogMessage("Info", "Entities reordered.", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage(ex.Message, "ReorderEntities failed.", DateTime.Now, -1, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }

        /// <inheritdoc/>
        public EntityStructure GetEntityStructure(string entityName)
        {
            return DataView.Entities.FirstOrDefault(e =>
                e.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));
        }

        #endregion "Entity Lifecycle"
    }
}
