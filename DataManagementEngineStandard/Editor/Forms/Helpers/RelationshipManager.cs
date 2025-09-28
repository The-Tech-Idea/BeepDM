using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.UOWManager.Helpers
{
    /// <summary>
    /// Helper class for managing master-detail relationships between data blocks
    /// </summary>
    public class RelationshipManager : IRelationshipManager
    {
        #region Fields
        private readonly IDMEEditor _dmeEditor;
        private readonly ConcurrentDictionary<string, DataBlockInfo> _blocks;
        private readonly ConcurrentDictionary<string, List<DataBlockRelationship>> _relationships;
        private readonly object _lockObject = new object();
        #endregion

        #region Constructor
        public RelationshipManager(
            IDMEEditor dmeEditor,
            ConcurrentDictionary<string, DataBlockInfo> blocks,
            ConcurrentDictionary<string, List<DataBlockRelationship>> relationships)
        {
            _dmeEditor = dmeEditor ?? throw new ArgumentNullException(nameof(dmeEditor));
            _blocks = blocks ?? throw new ArgumentNullException(nameof(blocks));
            _relationships = relationships ?? throw new ArgumentNullException(nameof(relationships));
        }
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Creates a master-detail relationship between blocks
        /// </summary>
        public void CreateMasterDetailRelation(string masterBlockName, string detailBlockName, 
            string masterKeyField, string detailForeignKeyField, RelationshipType relationshipType = RelationshipType.OneToMany)
        {
            lock (_lockObject)
            {
                try
                {
                    ValidateRelationshipParameters(masterBlockName, detailBlockName, masterKeyField, detailForeignKeyField);

                    var relationship = new DataBlockRelationship
                    {
                        MasterBlockName = masterBlockName,
                        DetailBlockName = detailBlockName,
                        MasterKeyField = masterKeyField,
                        DetailForeignKeyField = detailForeignKeyField,
                        RelationshipType = (Models.RelationshipType)relationshipType,
                        CreatedDate = DateTime.Now,
                        IsActive = true
                    };

                    // Initialize relationship list for master block if not exists
                    if (!_relationships.ContainsKey(masterBlockName))
                        _relationships[masterBlockName] = new List<DataBlockRelationship>();

                    // Check for duplicate relationships
                    var existingRelationship = _relationships[masterBlockName]
                        .FirstOrDefault(r => r.DetailBlockName == detailBlockName);

                    if (existingRelationship != null)
                    {
                        // Update existing relationship
                        existingRelationship.MasterKeyField = masterKeyField;
                        existingRelationship.DetailForeignKeyField = detailForeignKeyField;
                        existingRelationship.RelationshipType = (Models.RelationshipType)relationshipType;
                        existingRelationship.ModifiedDate = DateTime.Now;
                    }
                    else
                    {
                        // Add new relationship
                        _relationships[masterBlockName].Add(relationship);
                    }

                    // Update detail block info
                    UpdateDetailBlockInfo(detailBlockName, masterBlockName, masterKeyField, detailForeignKeyField);

                    _dmeEditor.AddLogMessage("RelationshipManager", 
                        $"Relationship created: {masterBlockName}.{masterKeyField} -> {detailBlockName}.{detailForeignKeyField}",
                        DateTime.Now, 0, null, Errors.Ok);
                }
                catch (Exception ex)
                {
                    _dmeEditor.AddLogMessage("RelationshipManager", 
                        $"Error creating relationship: {ex.Message}",
                        DateTime.Now, -1, null, Errors.Failed);
                    throw;
                }
            }
        }

        /// <summary>
        /// Synchronizes detail blocks when master record changes
        /// </summary>
        public async Task SynchronizeDetailBlocksAsync(string masterBlockName)
        {
            try
            {
                if (!_relationships.TryGetValue(masterBlockName, out var relationships))
                    return;

                var masterBlock = GetBlock(masterBlockName);
                if (masterBlock?.UnitOfWork?.Units?.Current == null)
                {
                    // Clear all detail blocks if no current master record
                    await ClearDetailBlocksAsync(relationships);
                    return;
                }

                // Process each relationship
                var synchronizationTasks = relationships
                    .Where(r => r.IsActive)
                    .Select(relationship => SynchronizeDetailBlockAsync(masterBlock, relationship));

                await Task.WhenAll(synchronizationTasks);
            }
            catch (Exception ex)
            {
                _dmeEditor.AddLogMessage("RelationshipManager", 
                    $"Error synchronizing detail blocks for {masterBlockName}: {ex.Message}",
                    DateTime.Now, -1, null, Errors.Failed);
                throw;
            }
        }

        /// <summary>
        /// Gets all blocks that are detail blocks of the specified master block
        /// </summary>
        public List<string> GetDetailBlocks(string masterBlockName)
        {
            if (_relationships.TryGetValue(masterBlockName, out var relationships))
            {
                return relationships
                    .Where(r => r.IsActive)
                    .Select(r => r.DetailBlockName)
                    .ToList();
            }
            return new List<string>();
        }

        /// <summary>
        /// Gets the master block name for a detail block
        /// </summary>
        public string GetMasterBlock(string detailBlockName)
        {
            var detailBlock = GetBlock(detailBlockName);
            return detailBlock?.MasterBlockName;
        }

        /// <summary>
        /// Removes all relationships for a block
        /// </summary>
        public void RemoveBlockRelationships(string blockName)
        {
            lock (_lockObject)
            {
                try
                {
                    // Remove as master block
                    _relationships.TryRemove(blockName, out _);
                    
                    // Remove as detail block from other relationships
                    foreach (var kvp in _relationships.ToList())
                    {
                        var relationships = kvp.Value;
                        var toRemove = relationships.Where(r => r.DetailBlockName == blockName).ToList();
                        
                        foreach (var rel in toRemove)
                        {
                            relationships.Remove(rel);
                        }
                        
                        // Remove empty relationship lists
                        if (!relationships.Any())
                        {
                            _relationships.TryRemove(kvp.Key, out _);
                        }
                    }

                    _dmeEditor.AddLogMessage("RelationshipManager", 
                        $"Relationships removed for block: {blockName}",
                        DateTime.Now, 0, null, Errors.Ok);
                }
                catch (Exception ex)
                {
                    _dmeEditor.AddLogMessage("RelationshipManager", 
                        $"Error removing relationships for block {blockName}: {ex.Message}",
                        DateTime.Now, -1, null, Errors.Failed);
                    throw;
                }
            }
        }

        #endregion

        #region Private Helper Methods

        private void ValidateRelationshipParameters(string masterBlockName, string detailBlockName, 
            string masterKeyField, string detailForeignKeyField)
        {
            if (string.IsNullOrWhiteSpace(masterBlockName))
                throw new ArgumentException("Master block name cannot be null or empty", nameof(masterBlockName));
                
            if (string.IsNullOrWhiteSpace(detailBlockName))
                throw new ArgumentException("Detail block name cannot be null or empty", nameof(detailBlockName));
                
            if (string.IsNullOrWhiteSpace(masterKeyField))
                throw new ArgumentException("Master key field cannot be null or empty", nameof(masterKeyField));
                
            if (string.IsNullOrWhiteSpace(detailForeignKeyField))
                throw new ArgumentException("Detail foreign key field cannot be null or empty", nameof(detailForeignKeyField));

            if (!_blocks.ContainsKey(masterBlockName))
                throw new InvalidOperationException($"Master block '{masterBlockName}' is not registered");
                
            if (!_blocks.ContainsKey(detailBlockName))
                throw new InvalidOperationException($"Detail block '{detailBlockName}' is not registered");
        }

        private void UpdateDetailBlockInfo(string detailBlockName, string masterBlockName, 
            string masterKeyField, string detailForeignKeyField)
        {
            if (_blocks.TryGetValue(detailBlockName, out var detailBlock))
            {
                detailBlock.MasterBlockName = masterBlockName;
                detailBlock.MasterKeyField = masterKeyField;
                detailBlock.ForeignKeyField = detailForeignKeyField;
                detailBlock.IsMasterBlock = false;
            }
        }

        private DataBlockInfo GetBlock(string blockName)
        {
            _blocks.TryGetValue(blockName, out var block);
            return block;
        }

        private async Task ClearDetailBlocksAsync(List<DataBlockRelationship> relationships)
        {
            var clearTasks = relationships
                .Where(r => r.IsActive)
                .Select(async relationship =>
                {
                    var detailBlock = GetBlock(relationship.DetailBlockName);
                    if (detailBlock?.UnitOfWork != null)
                    {
                        try
                        {
                            detailBlock.UnitOfWork.Clear();
                        }
                        catch (Exception ex)
                        {
                            _dmeEditor.AddLogMessage("RelationshipManager", 
                                $"Error clearing detail block {relationship.DetailBlockName}: {ex.Message}",
                                DateTime.Now, -1, null, Errors.Failed);
                        }
                    }
                });

            await Task.WhenAll(clearTasks);
        }

        private async Task SynchronizeDetailBlockAsync(DataBlockInfo masterBlock, DataBlockRelationship relationship)
        {
            try
            {
                var detailBlock = GetBlock(relationship.DetailBlockName);
                if (detailBlock?.UnitOfWork == null)
                    return;

                // Get master key value
                var masterValue = GetPropertyValue(masterBlock.UnitOfWork.Units.Current, relationship.MasterKeyField);
                
                if (masterValue != null && !IsNullOrEmpty(masterValue))
                {
                    // Apply filter to detail block
                    var filters = new List<AppFilter>
                    {
                        new AppFilter
                        {
                            FieldName = relationship.DetailForeignKeyField,
                            Operator = "=",
                            FilterValue = masterValue.ToString()
                        }
                    };

                    // Execute query on detail block
                    await ExecuteQueryOnDetailBlock(relationship.DetailBlockName, filters);
                }
                else
                {
                    // Clear detail block if no master value
                    detailBlock.UnitOfWork.Clear();
                }
            }
            catch (Exception ex)
            {
                _dmeEditor.AddLogMessage("RelationshipManager", 
                    $"Error synchronizing detail block {relationship.DetailBlockName}: {ex.Message}",
                    DateTime.Now, -1, null, Errors.Failed);
            }
        }

        private object GetPropertyValue(object obj, string propertyName)
        {
            try
            {
                if (obj == null || string.IsNullOrEmpty(propertyName))
                    return null;

                var property = obj.GetType().GetProperty(propertyName, 
                    System.Reflection.BindingFlags.IgnoreCase | 
                    System.Reflection.BindingFlags.Public | 
                    System.Reflection.BindingFlags.Instance);
                    
                return property?.GetValue(obj);
            }
            catch (Exception ex)
            {
                _dmeEditor.AddLogMessage("RelationshipManager", 
                    $"Error getting property value for {propertyName}: {ex.Message}",
                    DateTime.Now, -1, null, Errors.Failed);
                return null;
            }
        }

        private bool IsNullOrEmpty(object value)
        {
            return value == null || 
                   (value is string str && string.IsNullOrWhiteSpace(str)) ||
                   (value is DBNull);
        }

        private async Task ExecuteQueryOnDetailBlock(string blockName, List<AppFilter> filters)
        {
            try
            {
                var blockInfo = GetBlock(blockName);
                if (blockInfo?.UnitOfWork == null)
                    return;

                // Execute query using reflection
                var getWithFilters = blockInfo.UnitOfWork.GetType()
                    .GetMethod("Get", new[] { typeof(List<AppFilter>) });
                    
                if (getWithFilters != null)
                {
                    var task = (Task)getWithFilters.Invoke(blockInfo.UnitOfWork, new object[] { filters });
                    await task;
                }
            }
            catch (Exception ex)
            {
                _dmeEditor.AddLogMessage("RelationshipManager", 
                    $"Error executing query on detail block {blockName}: {ex.Message}",
                    DateTime.Now, -1, null, Errors.Failed);
            }
        }

        #endregion
    }
}