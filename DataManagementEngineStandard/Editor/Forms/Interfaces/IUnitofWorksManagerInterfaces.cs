using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Editor.UOWManager.Configuration;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Editor.UOWManager.Interfaces
{
    /// <summary>
    /// Core interface for UnitofWorksManager functionality
    /// </summary>
    public interface IUnitofWorksManager : IDisposable
    {
        #region Properties
        IDMEEditor DMEEditor { get; }
        string CurrentFormName { get; set; }
        string CurrentBlockName { get; set; }
        IReadOnlyDictionary<string, DataBlockInfo> Blocks { get; }
        bool IsDirty { get; }
        string Status { get; }
        int BlockCount { get; }
        #endregion

        #region Block Management
        void RegisterBlock(string blockName, IUnitofWork unitOfWork, IEntityStructure entityStructure, 
            string dataSourceName = null, bool isMasterBlock = false);
        bool UnregisterBlock(string blockName);
        DataBlockInfo GetBlock(string blockName);
        IUnitofWork GetUnitOfWork(string blockName);
        bool BlockExists(string blockName);
        #endregion

        #region Form Operations
        Task<bool> OpenFormAsync(string formName);
        Task<bool> CloseFormAsync();
        Task<IErrorsInfo> CommitFormAsync();
        Task<IErrorsInfo> RollbackFormAsync();
        Task ClearAllBlocksAsync();
        Task ClearBlockAsync(string blockName);
        #endregion

        #region Navigation
        Task<bool> FirstRecordAsync(string blockName);
        Task<bool> NextRecordAsync(string blockName);
        Task<bool> PreviousRecordAsync(string blockName);
        Task<bool> LastRecordAsync(string blockName);
        #endregion

        #region Data Operations
        Task<bool> SwitchToBlockAsync(string blockName);
        Task<bool> InsertRecordAsync(string blockName, object record = null);
        Task<bool> DeleteCurrentRecordAsync(string blockName);
        Task<bool> EnterQueryAsync(string blockName);
        Task<bool> ExecuteQueryAsync(string blockName, List<AppFilter> filters = null);
        #endregion

        #region Validation
        bool ValidateField(string blockName, string FieldName, object value);
        bool ValidateBlock(string blockName);
        bool ValidateForm();
        #endregion
    }

    /// <summary>
    /// Interface for relationship management functionality
    /// </summary>
    public interface IRelationshipManager
    {
        void CreateMasterDetailRelation(string masterBlockName, string detailBlockName, 
            string masterKeyField, string detailForeignKeyField, RelationshipType relationshipType = RelationshipType.OneToMany);
        Task SynchronizeDetailBlocksAsync(string masterBlockName);
        List<string> GetDetailBlocks(string masterBlockName);
        string GetMasterBlock(string detailBlockName);
        void RemoveBlockRelationships(string blockName);
    }

    /// <summary>
    /// Interface for dirty state management functionality
    /// </summary>
    public interface IDirtyStateManager
    {
        Task<bool> CheckAndHandleUnsavedChangesAsync(string blockName);
        bool HasUnsavedChanges();
        List<string> GetDirtyBlocks();
        void CollectDirtyDetailBlocks(string blockName, List<string> dirtyBlocks);
        Task<bool> SaveDirtyBlocksAsync(List<string> dirtyBlocks);
        Task<bool> RollbackDirtyBlocksAsync(List<string> dirtyBlocks);
        event EventHandler<UnsavedChangesEventArgs> OnUnsavedChanges;
    }

    /// <summary>
    /// Interface for event handling functionality
    /// </summary>
    public interface IEventManager
    {
        void SubscribeToUnitOfWorkEvents(IUnitofWork unitOfWork, string blockName);
        void UnsubscribeFromUnitOfWorkEvents(IUnitofWork unitOfWork, string blockName);
        void TriggerBlockEnter(string blockName);
        void TriggerBlockLeave(string blockName);
        void TriggerError(string blockName, Exception ex);
        bool TriggerFieldValidation(string blockName, string FieldName, object value);
        bool TriggerRecordValidation(string blockName, object record);
    }

    /// <summary>
    /// Interface for Oracle Forms simulation helpers
    /// </summary>
    public interface IFormsSimulationHelper
    {
        void SetAuditDefaults(object record, string currentUser = null);
        bool SetFieldValue(object record, string FieldName, object value);
        object GetFieldValue(object record, string FieldName);
        bool ExecuteSequence(string blockName, object record, string FieldName, string sequenceName);
        object GetPropertyValue(object obj, string propertyName);
    }

    /// <summary>
    /// Interface for performance and caching functionality
    /// </summary>
    public interface IPerformanceManager : IDisposable
    {
        void OptimizeBlockAccess();
        void CacheBlockInfo(string blockName, DataBlockInfo blockInfo);
        DataBlockInfo GetCachedBlockInfo(string blockName);
        void ClearCache();
        PerformanceStatistics GetPerformanceStatistics();
    }

    /// <summary>
    /// Interface for configuration management
    /// </summary>
    public interface IConfigurationManager
    {
        UnitofWorksManagerConfiguration Configuration { get; set; }
        void LoadConfiguration();
        void SaveConfiguration();
        void ResetToDefaults();
        bool ValidateConfiguration();
    }
}