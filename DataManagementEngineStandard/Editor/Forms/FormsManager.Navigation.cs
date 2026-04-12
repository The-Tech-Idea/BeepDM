using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    /// <summary>
    /// Navigation operations partial class for UnitofWorksManager
    /// </summary>
    public partial class FormsManager
    {
        #region Navigation Events
        /// <summary>
        /// Raised before a navigation operation is performed.
        /// </summary>
        public event EventHandler<NavigationTriggerEventArgs> OnNavigate;
        /// <summary>
        /// Raised after the current record changes.
        /// </summary>
        public event EventHandler<NavigationTriggerEventArgs> OnCurrentChanged;
        #endregion

        #region Navigation Operations

        /// <summary>
        /// Navigates to first record in block
        /// </summary>
        public async Task<bool> FirstRecordAsync(string blockName)
        {
            return await NavigateWithValidationAsync(blockName, NavigationType.First);
        }

        /// <summary>
        /// Navigates to next record in block
        /// </summary>
        public async Task<bool> NextRecordAsync(string blockName)
        {
            return await NavigateWithValidationAsync(blockName, NavigationType.Next);
        }

        /// <summary>
        /// Navigates to previous record in block
        /// </summary>
        public async Task<bool> PreviousRecordAsync(string blockName)
        {
            return await NavigateWithValidationAsync(blockName, NavigationType.Previous);
        }

        /// <summary>
        /// Navigates to last record in block
        /// </summary>
        public async Task<bool> LastRecordAsync(string blockName)
        {
            return await NavigateWithValidationAsync(blockName, NavigationType.Last);
        }

        /// <summary>
        /// Navigates to a specific record by index
        /// </summary>
        public async Task<bool> NavigateToRecordAsync(string blockName, int recordIndex)
        {
            return await NavigateToRecordInternalAsync(blockName, recordIndex, recordHistory: true);
        }

        private async Task<bool> NavigateToRecordInternalAsync(string blockName, int recordIndex, bool recordHistory)
        {
            if (string.IsNullOrWhiteSpace(blockName) || recordIndex < 0)
                return false;

            try
            {
                var blockInfo = GetBlock(blockName);
                if (blockInfo?.UnitOfWork == null)
                {
                    Status = $"Block '{blockName}' not found or has no unit of work";
                    return false;
                }

                var previousIndex = blockInfo.UnitOfWork.Units != null
                    ? GetCurrentIndex(blockInfo.UnitOfWork.Units)
                    : -1;

                if (previousIndex == recordIndex)
                    return true;

                if (Configuration?.Navigation?.ValidateBeforeNavigation == true)
                {
                    if (!ValidateBlock(blockName))
                    {
                        LogOperation($"Navigation blocked: validation failed in block '{blockName}'", blockName);
                        return false;
                    }
                }

                // Check for unsaved changes before navigation
                if (!await CheckAndHandleUnsavedChangesAsync(blockName))
                    return false;

                // Trigger navigation event
                var args = new NavigationTriggerEventArgs(blockName, _currentFormName, NavigationType.ToRecord)
                {
                    TargetIndex = recordIndex
                };
                OnNavigate?.Invoke(this, args);
                
                if (args.Cancel)
                {
                    Status = string.IsNullOrWhiteSpace(args.Message)
                        ? $"Navigation to record {recordIndex} cancelled by trigger in block '{blockName}'"
                        : args.Message;
                    _messageManager?.ShowWarningMessage(blockName, Status);
                    LogOperation($"Navigation to record {recordIndex} cancelled by trigger", blockName);
                    return false;
                }

                // Perform the navigation
                SuppressSync(blockName);
                bool success;
                try
                {
                    success = PerformRecordNavigation(blockInfo, recordIndex);
                }
                finally { ResumeSync(blockName); }
                
                if (success)
                {
                    var currentIndex = blockInfo.UnitOfWork.Units != null
                        ? GetCurrentIndex(blockInfo.UnitOfWork.Units)
                        : previousIndex;

                    if (recordHistory && previousIndex >= 0 && previousIndex != currentIndex)
                        _navHistoryManager.Push(blockName, previousIndex);

                    // Synchronize detail blocks
                    await SynchronizeDetailBlocksAsync(blockName);
                    
                    // Trigger current changed event
                    var currentChangedArgs = new NavigationTriggerEventArgs(blockName, _currentFormName, NavigationType.CurrentChanged);
                    OnCurrentChanged?.Invoke(this, currentChangedArgs);
                    
                    Status = $"Navigated to record {recordIndex} in block '{blockName}'";
                    LogOperation($"Navigated to record {recordIndex}", blockName);
                }
                
                return success;
            }
            catch (Exception ex)
            {
                Status = $"Error navigating to record {recordIndex} in block '{blockName}': {ex.Message}";
                LogError($"Error navigating to record {recordIndex} in block '{blockName}'", ex, blockName);
                _eventManager.TriggerError(blockName, ex);
                return false;
            }
        }

        /// <summary>
        /// Switches to a different block, checking for unsaved changes first
        /// </summary>
        public async Task<bool> SwitchToBlockAsync(string blockName)
        {
            if (string.IsNullOrWhiteSpace(blockName))
                return false;

            try
            {
                // Check for unsaved changes in current block and its children
                if (!string.IsNullOrEmpty(_currentBlockName) && _currentBlockName != blockName)
                {
                    if (!await CheckAndHandleUnsavedChangesAsync(_currentBlockName))
                    {
                        LogOperation($"Block switch cancelled due to unsaved changes in '{_currentBlockName}'");
                        return false;
                    }
                }

                var blockInfo = GetBlock(blockName);
                if (blockInfo == null)
                {
                    Status = $"Block '{blockName}' not found";
                    LogError($"Block '{blockName}' not found", null, blockName);
                    return false;
                }

                // Trigger block leave for current block
                if (!string.IsNullOrEmpty(_currentBlockName) && _currentBlockName != blockName)
                {
                    _eventManager.TriggerBlockLeave(_currentBlockName);
                }

                // Set new current block
                var previousBlock = _currentBlockName;
                _currentBlockName = blockName;

                // Fire WHEN-NEW-BLOCK-INSTANCE trigger (Oracle Forms equivalent)
                await _triggerManager.FireBlockTriggerAsync(
                    TriggerType.WhenNewBlockInstance, blockName,
                    TriggerContext.ForBlock(TriggerType.WhenNewBlockInstance, blockName, null, _dmeEditor));

                // Trigger block enter for new block
                _eventManager.TriggerBlockEnter(blockName);

                // Update performance metrics
                RecordBlockSwitch(previousBlock, blockName);

                Status = $"Switched to block '{blockName}'";
                LogOperation($"Switched from block '{previousBlock}' to '{blockName}'");
                return true;
            }
            catch (Exception ex)
            {
                Status = $"Error switching to block '{blockName}': {ex.Message}";
                LogError($"Error switching to block '{blockName}'", ex, blockName);
                _eventManager.TriggerError(blockName, ex);
                return false;
            }
        }

        /// <summary>
        /// Gets current record information from a block
        /// </summary>
        public NavigationInfo GetCurrentRecordInfo(string blockName)
        {
            try
            {
                var blockInfo = GetBlock(blockName);
                if (blockInfo?.UnitOfWork == null)
                    return null;

                // Use reflection to get navigation information
                var units = blockInfo.UnitOfWork.Units;
                if (units == null)
                    return null;

                return new NavigationInfo
                {
                    BlockName = blockName,
                    CurrentIndex = GetCurrentIndex(units),
                    TotalRecords = GetTotalRecords(units),
                    HasPrevious = HasPrevious(units),
                    HasNext = HasNext(units),
                    CurrentRecord = units.Current,
                    BlockMode = (Models.DataBlockMode)blockInfo.Mode,
                    IsCurrentRecordDirty = blockInfo.UnitOfWork.IsDirty
                };
            }
            catch (Exception ex)
            {
                LogError($"Error getting current record info for block '{blockName}'", ex, blockName);
                return null;
            }
        }

        /// <summary>
        /// Gets navigation information for all blocks
        /// </summary>
        public Dictionary<string, NavigationInfo> GetAllNavigationInfo()
        {
            var navigationInfo = new Dictionary<string, NavigationInfo>();
            
            foreach (var blockName in _blocks.Keys)
            {
                var info = GetCurrentRecordInfo(blockName);
                if (info != null)
                {
                    navigationInfo[blockName] = info;
                }
            }
            
            return navigationInfo;
        }

        #endregion

        #region Oracle Forms Navigation Built-ins

        /// <summary>
        /// Switch focus to a named block.
        /// Corresponds to Oracle Forms GO_BLOCK built-in.
        /// </summary>
        public Task<bool> GoBlockAsync(string blockName) => SwitchToBlockAsync(blockName);

        /// <summary>
        /// Navigate to a specific record by index within a block.
        /// Corresponds to Oracle Forms GO_RECORD built-in.
        /// </summary>
        public Task<bool> GoRecordAsync(string blockName, int recordIndex)
            => NavigateToRecordAsync(blockName, recordIndex);

        /// <summary>
        /// Set the current item (field) within the current block.
        /// Fires KEY-NEXT-ITEM or a generic navigation trigger.
        /// Corresponds to Oracle Forms GO_ITEM built-in.
        /// </summary>
        public async Task<bool> GoItemAsync(string blockName, string itemName)
        {
            if (string.IsNullOrWhiteSpace(blockName) || string.IsNullOrWhiteSpace(itemName))
                return false;

            var ctx = TriggerContext.ForItem(
                TriggerType.KeyNextItem, blockName, itemName, null, null, _dmeEditor);
            await _triggerManager.FireBlockTriggerAsync(TriggerType.KeyNextItem, blockName, ctx);

            LogOperation($"GoItem: block='{blockName}', item='{itemName}'", blockName);
            return true;
        }

        /// <summary>
        /// Move focus to the next item in the current block's tabbing order.
        /// Corresponds to Oracle Forms NEXT_ITEM / KEY-NEXT-ITEM built-in.
        /// </summary>
        public async Task<bool> NextItemAsync(string blockName, string currentItemName = null)
        {
            if (string.IsNullOrWhiteSpace(blockName)) return false;

            var ctx = TriggerContext.ForItem(
                TriggerType.KeyNextItem, blockName, currentItemName ?? string.Empty, null, null, _dmeEditor);
            var result = await _triggerManager.FireBlockTriggerAsync(TriggerType.KeyNextItem, blockName, ctx);
            return result != TriggerResult.Cancelled;
        }

        /// <summary>
        /// Move focus to the previous item in the current block's tabbing order.
        /// Corresponds to Oracle Forms PREVIOUS_ITEM / KEY-PREV-ITEM built-in.
        /// </summary>
        public async Task<bool> PreviousItemAsync(string blockName, string currentItemName = null)
        {
            if (string.IsNullOrWhiteSpace(blockName)) return false;

            var ctx = TriggerContext.ForItem(
                TriggerType.KeyPreviousItem, blockName, currentItemName ?? string.Empty, null, null, _dmeEditor);
            var result = await _triggerManager.FireBlockTriggerAsync(TriggerType.KeyPreviousItem, blockName, ctx);
            return result != TriggerResult.Cancelled;
        }

        /// <summary>
        /// Switch focus to the next registered block after the current one.
        /// Corresponds to Oracle Forms NEXT_BLOCK built-in.
        /// </summary>
        public Task<bool> NextBlockAsync()
        {
            var keys = new List<string>(_blocks.Keys);
            if (keys.Count == 0) return Task.FromResult(false);

            var idx = string.IsNullOrEmpty(_currentBlockName)
                ? 0
                : keys.IndexOf(_currentBlockName);

            var nextIdx = (idx + 1) % keys.Count;
            return SwitchToBlockAsync(keys[nextIdx]);
        }

        /// <summary>
        /// Switch focus to the previous registered block before the current one.
        /// Corresponds to Oracle Forms PREVIOUS_BLOCK built-in.
        /// </summary>
        public Task<bool> PreviousBlockAsync()
        {
            var keys = new List<string>(_blocks.Keys);
            if (keys.Count == 0) return Task.FromResult(false);

            var idx = string.IsNullOrEmpty(_currentBlockName)
                ? 0
                : keys.IndexOf(_currentBlockName);

            var prevIdx = (idx - 1 + keys.Count) % keys.Count;
            return SwitchToBlockAsync(keys[prevIdx]);
        }

        #endregion

        #region Private Navigation Helper Methods

        private async Task<bool> NavigateWithValidationAsync(string blockName, NavigationType navigationType)
        {
            if (string.IsNullOrWhiteSpace(blockName))
                return false;

            try
            {
                // Validate current record before navigation if configured
                if (Configuration?.Navigation?.ValidateBeforeNavigation == true)
                {
                    if (!ValidateBlock(blockName))
                    {
                        LogOperation($"Navigation blocked: validation failed in block '{blockName}'", blockName);
                        return false;
                    }
                }

                // Check for unsaved changes before navigation
                if (!await CheckAndHandleUnsavedChangesAsync(blockName))
                {
                    LogOperation($"Navigation cancelled due to unsaved changes in block '{blockName}'");
                    return false;
                }
                
                return await NavigateAsync(blockName, navigationType, recordHistory: true);
            }
            catch (Exception ex)
            {
                LogError($"Error in navigation with validation for block '{blockName}'", ex, blockName);
                return false;
            }
        }

        private async Task<bool> NavigateAsync(string blockName, NavigationType navigationType, bool recordHistory)
        {
            try
            {
                var blockInfo = GetBlock(blockName);
                if (blockInfo?.UnitOfWork == null)
                {
                    Status = $"Block '{blockName}' not found or has no unit of work";
                    return false;
                }

                var previousIndex = blockInfo.UnitOfWork.Units != null
                    ? GetCurrentIndex(blockInfo.UnitOfWork.Units)
                    : -1;

                // Trigger navigation event
                var args = new NavigationTriggerEventArgs(blockName, _currentFormName, navigationType);
                OnNavigate?.Invoke(this, args);
                
                if (args.Cancel)
                {
                    Status = string.IsNullOrWhiteSpace(args.Message)
                        ? $"Navigation {navigationType} cancelled by trigger in block '{blockName}'"
                        : args.Message;
                    _messageManager?.ShowWarningMessage(blockName, Status);
                    LogOperation($"Navigation {navigationType} cancelled by trigger", blockName);
                    return false;
                }

                // Perform the navigation
                SuppressSync(blockName);
                bool success;
                try
                {
                    success = PerformNavigation(blockInfo, navigationType);
                }
                finally { ResumeSync(blockName); }
                
                if (success)
                {
                    var currentIndex = blockInfo.UnitOfWork.Units != null
                        ? GetCurrentIndex(blockInfo.UnitOfWork.Units)
                        : previousIndex;

                    if (recordHistory && previousIndex >= 0 && previousIndex != currentIndex)
                        _navHistoryManager.Push(blockName, previousIndex);

                    // Synchronize detail blocks
                    await SynchronizeDetailBlocksAsync(blockName);
                    
                    // Trigger current changed event
                    var currentChangedArgs = new NavigationTriggerEventArgs(blockName, _currentFormName, NavigationType.CurrentChanged);
                    OnCurrentChanged?.Invoke(this, currentChangedArgs);
                    
                    Status = $"Navigation {navigationType} completed for block '{blockName}'";
                    LogOperation($"Navigation {navigationType} completed", blockName);
                }
                else
                {
                    Status = $"Navigation {navigationType} failed for block '{blockName}'";
                }
                
                return success;
            }
            catch (Exception ex)
            {
                Status = $"Error navigating in block '{blockName}': {ex.Message}";
                LogError($"Error navigating in block '{blockName}'", ex, blockName);
                _eventManager.TriggerError(blockName, ex);
                return false;
            }
        }

        private bool PerformNavigation(DataBlockInfo blockInfo, NavigationType navigationType)
        {
            try
            {
                var unitOfWork = blockInfo.UnitOfWork;
                
                switch (navigationType)
                {
                    case NavigationType.First:
                        unitOfWork.MoveFirst();
                        break;
                    case NavigationType.Next:
                        unitOfWork.MoveNext();
                        break;
                    case NavigationType.Previous:
                        unitOfWork.MovePrevious();
                        break;
                    case NavigationType.Last:
                        unitOfWork.MoveLast();
                        break;
                    default:
                        return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                LogError($"Error performing navigation {navigationType}", ex, blockInfo.BlockName);
                return false;
            }
        }

        private bool PerformRecordNavigation(DataBlockInfo blockInfo, int recordIndex)
        {
            try
            {
                // This would depend on your UnitOfWork implementation
                // For now, using a generic approach
                var units = blockInfo.UnitOfWork.Units;
                if (units == null || recordIndex < 0 || recordIndex >= GetTotalRecords(units))
                    return false;

                // Navigate to specific record - this would need to be implemented based on your collection type
                // Placeholder implementation
                return SetCurrentIndex(units, recordIndex);
            }
            catch (Exception ex)
            {
                LogError($"Error performing record navigation to index {recordIndex}", ex, blockInfo.BlockName);
                return false;
            }
        }

        private void RecordBlockSwitch(string fromBlock, string toBlock)
        {
            try
            {
                // Record performance metrics for block switching
                // This could be used for optimization
                LogOperation($"Block switch recorded: '{fromBlock}' -> '{toBlock}'");
            }
            catch (Exception ex)
            {
                LogError("Error recording block switch metrics", ex);
            }
        }

        #region Navigation Helper Methods (Implementation-specific)

        private int GetCurrentIndex(object units)
        {
            try
            {
                // Implementation would depend on your collection type
                // This is a placeholder that would need to be adapted
                var property = units.GetType().GetProperty("CurrentIndex") ?? 
                              units.GetType().GetProperty("Position");
                return property != null ? (int)property.GetValue(units) : 0;
            }
            catch
            {
                return 0;
            }
        }

        private int GetTotalRecords(object units)
        {
            try
            {
                // Implementation would depend on your collection type
                var property = units.GetType().GetProperty("Count");
                return property != null ? (int)property.GetValue(units) : 0;
            }
            catch
            {
                return 0;
            }
        }

        private bool HasPrevious(object units)
        {
            return GetCurrentIndex(units) > 0;
        }

        private bool HasNext(object units)
        {
            var currentIndex = GetCurrentIndex(units);
            var totalRecords = GetTotalRecords(units);
            return currentIndex < totalRecords - 1;
        }

        private bool SetCurrentIndex(object units, int index)
        {
            try
            {
                // Implementation would depend on your collection type
                var property = units.GetType().GetProperty("CurrentIndex") ?? 
                              units.GetType().GetProperty("Position");
                              
                if (property != null && property.CanWrite)
                {
                    property.SetValue(units, index);
                    return true;
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #endregion
    }
}