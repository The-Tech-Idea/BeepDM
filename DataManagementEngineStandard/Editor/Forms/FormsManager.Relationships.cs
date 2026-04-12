using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.UOW;
using TheTechIdea.Beep.Editor.UOWManager.Configuration;
using TheTechIdea.Beep.Editor.UOWManager.Helpers;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.Forms.Helpers;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    public partial class FormsManager
    {
        #region Relationship Management (Delegated)

        /// <summary>
        /// Creates a master-detail relationship between blocks
        /// </summary>
        public void CreateMasterDetailRelation(string masterBlockName, string detailBlockName, 
            string masterKeyField, string detailForeignKeyField, RelationshipType relationshipType = RelationshipType.OneToMany)
        {
            var masterBlock = GetBlock(masterBlockName)
                ?? throw new InvalidOperationException($"Master block '{masterBlockName}' is not registered");
            var detailBlock = GetBlock(detailBlockName)
                ?? throw new InvalidOperationException($"Detail block '{detailBlockName}' is not registered");

            ValidateRelationshipParameters(masterBlockName, detailBlockName);

            var resolution = _masterDetailKeyResolver.Resolve(masterBlock, detailBlock, masterKeyField, detailForeignKeyField);
            if (!resolution.IsResolved)
            {
                throw new InvalidOperationException(resolution.ErrorMessage);
            }

            foreach (var warning in resolution.Warnings)
            {
                LogOperation($"Relationship resolution warning: {warning}", detailBlockName);
            }

            lock (_lockObject)
            {
                if (!_relationships.TryGetValue(masterBlockName, out var relationships))
                {
                    relationships = new List<DataBlockRelationship>();
                    _relationships[masterBlockName] = relationships;
                }

                var existing = relationships.FirstOrDefault(r =>
                    string.Equals(r.DetailBlockName, detailBlockName, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    existing.MasterKeyField = resolution.MasterKeyField;
                    existing.DetailForeignKeyField = resolution.DetailForeignKeyField;
                    existing.RelationshipType = relationshipType;
                    existing.Description = $"Resolved via {resolution.Source}";
                    existing.IsActive = true;
                    existing.ModifiedDate = DateTime.Now;
                }
                else
                {
                    relationships.Add(new DataBlockRelationship
                    {
                        MasterBlockName = masterBlockName,
                        DetailBlockName = detailBlockName,
                        MasterKeyField = resolution.MasterKeyField,
                        DetailForeignKeyField = resolution.DetailForeignKeyField,
                        RelationshipType = relationshipType,
                        Description = $"Resolved via {resolution.Source}",
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    });
                }
            }

            masterBlock.IsMasterBlock = true;
            detailBlock.MasterBlockName = masterBlockName;
            detailBlock.MasterKeyField = resolution.MasterKeyField;
            detailBlock.ForeignKeyField = resolution.DetailForeignKeyField;
            detailBlock.IsMasterBlock = false;
        }

        /// <summary>
        /// Synchronizes detail blocks when master record changes.
        /// Fires ON-POPULATE-DETAILS trigger before delegating to the relationship manager.
        /// </summary>
        public async Task SynchronizeDetailBlocksAsync(string masterBlockName)
        {
            // Fire ON-POPULATE-DETAILS to allow triggers to intervene
            await _triggerManager.FireBlockTriggerAsync(
                TriggerType.OnPopulateDetails, masterBlockName,
                TriggerContext.ForBlock(TriggerType.OnPopulateDetails, masterBlockName, null, _dmeEditor));

            await SynchronizeDetailHierarchyAsync(masterBlockName, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets all blocks that are detail blocks of the specified master block
        /// </summary>
        public List<string> GetDetailBlocks(string masterBlockName)
        {
            lock (_lockObject)
            {
                if (!_relationships.TryGetValue(masterBlockName, out var relationships))
                    return new List<string>();

                return relationships
                    .Where(r => r.IsActive)
                    .Select(r => r.DetailBlockName)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
        }

        /// <summary>
        /// Gets the master block name for a detail block
        /// </summary>
        public string GetMasterBlock(string detailBlockName)
        {
            return GetBlock(detailBlockName)?.MasterBlockName;
        }

        #endregion

        private void RemoveBlockRelationships(string blockName)
        {
            var blockInfo = GetBlock(blockName);
            if (blockInfo == null)
                return;

            lock (_lockObject)
            {
                if (_relationships.TryRemove(blockName, out var ownedRelationships))
                {
                    foreach (var relationship in ownedRelationships)
                    {
                        var detailBlock = GetBlock(relationship.DetailBlockName);
                        if (detailBlock != null)
                        {
                            detailBlock.MasterBlockName = null;
                            detailBlock.MasterKeyField = null;
                            detailBlock.ForeignKeyField = null;
                        }
                    }
                }

                foreach (var kvp in _relationships.ToList())
                {
                    kvp.Value.RemoveAll(r => string.Equals(r.DetailBlockName, blockName, StringComparison.OrdinalIgnoreCase));
                    if (!kvp.Value.Any())
                    {
                        var masterBlock = GetBlock(kvp.Key);
                        if (masterBlock != null)
                        {
                            masterBlock.IsMasterBlock = false;
                        }
                        _relationships.TryRemove(kvp.Key, out _);
                    }
                }
            }

            blockInfo.MasterBlockName = null;
            blockInfo.MasterKeyField = null;
            blockInfo.ForeignKeyField = null;
            blockInfo.IsMasterBlock = GetDetailBlocks(blockName).Any();
        }
    }
}
