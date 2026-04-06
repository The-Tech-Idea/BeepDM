using System;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.UOW;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    /// <summary>
    /// Generic and LOV operations partial class for FormsManager.
    /// Provides type-safe block registration, typed record accessors and LOV display helpers.
    /// Phase 1 — 1.1 Generic Block Registration / 1.4 LOV Full Integration.
    /// </summary>
    public partial class FormsManager
    {
        #region Generic Block Registration (Phase 1.1)

        /// <summary>
        /// Registers a typed data block. The CLR type <typeparamref name="T"/> is stored on
        /// <see cref="DataBlockInfo.EntityType"/> so that <c>CreateNewRecord</c> can instantiate
        /// the correct concrete type instead of falling back to <c>ExpandoObject</c>.
        /// </summary>
        public void RegisterBlock<T>(
            string blockName,
            IUnitofWork unitOfWork,
            IEntityStructure entityStructure,
            string dataSourceName = null,
            bool isMasterBlock = false)
        {
            RegisterBlock(blockName, unitOfWork, entityStructure, dataSourceName, isMasterBlock);

            // Stamp the CLR type on the block info
            if (_blocks.TryGetValue(blockName, out var info))
                info.EntityType = typeof(T);
        }

        /// <summary>
        /// Returns the <see cref="DataBlockInfo"/> for <paramref name="blockName"/> and
        /// verifies that it was registered with entity type <typeparamref name="T"/>.
        /// Returns <c>null</c> if the block is not found or the type does not match.
        /// </summary>
        public DataBlockInfo GetBlock<T>(string blockName)
        {
            var info = GetBlock(blockName);
            if (info == null)
                return null;

            if (info.EntityType != null && info.EntityType != typeof(T))
            {
                LogOperation($"GetBlock<{typeof(T).Name}>: block '{blockName}' is registered as '{info.EntityType.Name}'", blockName);
                return null;
            }

            return info;
        }

        /// <summary>
        /// Creates a new, default instance of <typeparamref name="T"/> and inserts it into
        /// the specified block. Equivalent to <c>InsertRecordAsync(blockName, new T())</c>.
        /// </summary>
        public async Task<bool> InsertRecordAsync<T>(string blockName, T record = default)
            where T : class, new()
        {
            var instance = record ?? new T();
            return await InsertRecordAsync(blockName, instance);
        }

        #endregion

        #region LOV Display Helper (Phase 1.4)

        /// <summary>
        /// Loads and returns the LOV data for a block/field combination — equivalent to Oracle
        /// Forms <c>SHOW_LOV</c>. If a selected record is returned by the caller, related fields
        /// are automatically populated on the current record via <c>GetRelatedFieldValues</c>.
        /// </summary>
        /// <param name="blockName">Name of the block owning the field.</param>
        /// <param name="fieldName">Name of the field the LOV is attached to.</param>
        /// <param name="searchText">Optional filter text passed to <c>LoadLOVDataAsync</c>.</param>
        /// <param name="selectedRecord">
        /// Optional record chosen by the caller (UI layer).  When non-null, related field values
        /// are auto-populated on the current block record.
        /// </param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The <see cref="LOVResult"/> containing available records.</returns>
        public async Task<LOVResult> ShowLOVAsync(
            string blockName,
            string fieldName,
            string searchText = null,
            object selectedRecord = null,
            CancellationToken ct = default)
        {
            if (!_lovManager.HasLOV(blockName, fieldName))
                return LOVResult.Fail($"No LOV registered for {blockName}.{fieldName}");

            // Fire WHEN-LOV-VALIDATE trigger before showing
            var ctx = TriggerContext.ForItem(TriggerType.WhenLOVValidate, blockName, fieldName, null, null, _dmeEditor);
            var triggerResult = await _triggerManager.FireBlockTriggerAsync(TriggerType.WhenLOVValidate, blockName, ctx, ct);
            if (triggerResult == TriggerResult.Cancelled)
                return LOVResult.Fail("LOV cancelled by WHEN-LOV-VALIDATE trigger");

            // Load data
            var result = await _lovManager.LoadLOVDataAsync(blockName, fieldName, searchText);

            // If the caller already has a selection, auto-populate related fields
            if (selectedRecord != null && result.Success)
            {
                var lov = _lovManager.GetLOV(blockName, fieldName);
                if (lov != null)
                {
                    var relatedValues = _lovManager.GetRelatedFieldValues(lov, selectedRecord);
                    var blockInfo = GetBlock(blockName);
                    var currentRecord = blockInfo?.UnitOfWork?.CurrentItem;
                    if (currentRecord != null && relatedValues != null)
                    {
                        foreach (var kv in relatedValues)
                            SetFieldValue(currentRecord, kv.Key, kv.Value);
                    }
                }
            }

            return result;
        }

        #endregion
    }
}
