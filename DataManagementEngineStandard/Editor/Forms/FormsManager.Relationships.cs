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

            // B13 (audit pass 3, 2026-06): the previous version
            // let any exception from the key resolver propagate
            // to the caller. The resolver is metadata-driven
            // (it inspects entity structures and field maps);
            // a malformed entity structure (e.g. a missing
            // EntityStructure on the detail block) can throw
            // NRE deep in the resolver. Now: catch, log, and
            // re-throw as InvalidOperationException with a
            // clearer message — the caller (typically a host
            // UI handler) gets a useful error rather than a
            // raw NRE.
            MasterDetailKeyResolution resolution;
            try
            {
                resolution = _masterDetailKeyResolver.Resolve(masterBlock, detailBlock, masterKeyField, detailForeignKeyField);
            }
            catch (Exception ex)
            {
                LogError(
                    $"CreateMasterDetailRelation: key resolver threw for ({masterBlockName}.{masterKeyField} -> {detailBlockName}.{detailForeignKeyField})",
                    ex, detailBlockName);
                throw new InvalidOperationException(
                    $"Failed to resolve master/detail keys for {masterBlockName}.{masterKeyField} -> {detailBlockName}.{detailForeignKeyField}: {ex.Message}",
                    ex);
            }

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
                    existing.KeyFieldMappings = resolution.Mappings != null
                        ? new List<DataBlockFieldMapping>(resolution.Mappings)
                        : new List<DataBlockFieldMapping>();
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
                        KeyFieldMappings = resolution.Mappings != null
                            ? new List<DataBlockFieldMapping>(resolution.Mappings)
                            : new List<DataBlockFieldMapping>(),
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
        /// Creates a master-detail relationship with composite (multi-field) keys.
        /// The key mappings are passed as explicit field pairs; the resolver is bypassed
        /// and the mappings are stored directly for use during detail synchronization.
        /// </summary>
        public void CreateMasterDetailRelation(string masterBlockName, string detailBlockName,
            DataBlockFieldMapping[] keyFieldMappings, RelationshipType relationshipType = RelationshipType.OneToMany)
        {
            if (keyFieldMappings == null || keyFieldMappings.Length == 0)
                throw new ArgumentException("At least one key field mapping is required for composite-key relationships", nameof(keyFieldMappings));

            var masterBlock = GetBlock(masterBlockName)
                ?? throw new InvalidOperationException($"Master block '{masterBlockName}' is not registered");
            var detailBlock = GetBlock(detailBlockName)
                ?? throw new InvalidOperationException($"Detail block '{detailBlockName}' is not registered");

            ValidateRelationshipParameters(masterBlockName, detailBlockName);

            var mappingList = new List<DataBlockFieldMapping>(keyFieldMappings);

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
                    existing.MasterKeyField = mappingList[0].MasterField;
                    existing.DetailForeignKeyField = mappingList[0].DetailField;
                    existing.KeyFieldMappings = mappingList;
                    existing.RelationshipType = relationshipType;
                    existing.Description = "Composite-key (explicit)";
                    existing.IsActive = true;
                    existing.ModifiedDate = DateTime.Now;
                }
                else
                {
                    relationships.Add(new DataBlockRelationship
                    {
                        MasterBlockName = masterBlockName,
                        DetailBlockName = detailBlockName,
                        MasterKeyField = mappingList[0].MasterField,
                        DetailForeignKeyField = mappingList[0].DetailField,
                        KeyFieldMappings = mappingList,
                        RelationshipType = relationshipType,
                        Description = "Composite-key (explicit)",
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    });
                }
            }

            masterBlock.IsMasterBlock = true;
            detailBlock.MasterBlockName = masterBlockName;
            detailBlock.MasterKeyField = mappingList[0].MasterField;
            detailBlock.ForeignKeyField = mappingList[0].DetailField;
            detailBlock.IsMasterBlock = false;
        }

        /// <summary>
        /// Synchronizes detail blocks when master record changes.
        /// Fires ON-POPULATE-DETAILS trigger before delegating to the relationship manager.
        /// </summary>
        /// <param name="masterBlockName">Master block whose detail blocks should be synchronized.</param>
        /// <param name="ct">
        /// Cancellation token. Propagated through the detail
        /// hierarchy walk; observed at the start of each
        /// relationship iteration.
        /// </param>
        public async Task SynchronizeDetailBlocksAsync(string masterBlockName, CancellationToken ct = default)
        {
            // Fire ON-POPULATE-DETAILS to allow triggers to intervene
            await _triggerManager.FireBlockTriggerAsync(
                TriggerType.OnPopulateDetails, masterBlockName,
                TriggerContext.ForBlock(TriggerType.OnPopulateDetails, masterBlockName, null, _dmeEditor));

            await SynchronizeDetailHierarchyAsync(
                masterBlockName,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                ct);
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

        public void ClearBlockRelationships(string blockName) => RemoveBlockRelationships(blockName);

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

        /// <summary>All registered blocks. Standalone filtering requires per-block form tracking (deferred).</summary>
        public IReadOnlyList<DataBlockInfo> StandaloneBlocks =>
            _blocks.Values.ToList().AsReadOnly();

        /// <summary>
        /// Runtime status snapshot for IDE display. Returns non-null even if block doesn't exist
        /// (empty status with the requested name).
        /// </summary>
        public BlockStatus GetBlockStatus(string blockName)
        {
            var result = new BlockStatus { BlockName = blockName ?? "(unknown)" };
            if (!_blocks.TryGetValue(blockName ?? string.Empty, out var info))
                return result;

            var uow = info.UnitOfWork;
            if (uow != null)
            {
                try
                {
                    dynamic dyn = uow;
                    result.RecordCount = (int)(dyn.Units?.Count ?? 0);
                    result.CurrentRecordIndex = (int)(dyn.CurrentIndex ?? 0);
                }
                catch { /* dynamic access may fail on some UoW implementations */ }
            }
            result.IsInQueryMode = info.Mode == DataBlockMode.Query || info.Mode == DataBlockMode.EnterQuery;
            result.CurrentMode = info.Mode.ToString();
            result.HasUnsavedChanges = uow?.IsDirty == true;
            return result;
        }

        /// <summary>
        /// Register a form discovered by the IDE scanner in the shared form registry.
        /// Does NOT create a FormsManager — the host creates that separately.
        /// </summary>
        public void RegisterDiscoveredForm(string formName, string codeFilePath, string designerFilePath, string hostName = null)
        {
            if (_formRegistry == null || string.IsNullOrWhiteSpace(formName)) return;
            _formRegistry.RegisterForm(formName, this);
        }
    }
}
