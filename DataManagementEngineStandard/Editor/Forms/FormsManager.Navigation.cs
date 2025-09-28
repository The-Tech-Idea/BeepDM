using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    /// <summary>
    /// Navigation operations partial class for UnitofWorksManager
    /// </summary>
    public partial class FormsManager
    {
        #region Navigation Events
        public event EventHandler<NavigationTriggerEventArgs> OnNavigate;
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
            if (string.IsNullOrWhiteSpace(blockName) || recordIndex < 0)
                return false;

            try
            {
                // Check for unsaved changes before navigation
                if (!await CheckAndHandleUnsavedChangesAsync(blockName))
                    return false;

                var blockInfo = GetBlock(blockName);
                if (blockInfo?.UnitOfWork == null)
                {
                    Status = $"Block '{blockName}' not found or has no unit of work";
                    return false;
                }

                // Trigger navigation event
                var args = new NavigationTriggerEventArgs(blockName, _currentFormName, NavigationType.ToRecord)
                {
                    TargetIndex = recordIndex
                };
                OnNavigate?.Invoke(this, args);
                
                if (args.Cancel)
                {
                    LogOperation($"Navigation to record {recordIndex} cancelled by trigger", blockName);
                    return false;
                }

                // Perform the navigation
                var success = PerformRecordNavigation(blockInfo, recordIndex);
                
                if (success)
                {
                    // Synchronize detail blocks
                    await _relationshipManager.SynchronizeDetailBlocksAsync(blockName);
                    
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

        #region Private Navigation Helper Methods

        private async Task<bool> NavigateWithValidationAsync(string blockName, NavigationType navigationType)
        {
            if (string.IsNullOrWhiteSpace(blockName))
                return false;

            try
            {
                // Check for unsaved changes before navigation
                if (!await CheckAndHandleUnsavedChangesAsync(blockName))
                {
                    LogOperation($"Navigation cancelled due to unsaved changes in block '{blockName}'");
                    return false;
                }
                
                return await NavigateAsync(blockName, navigationType);
            }
            catch (Exception ex)
            {
                LogError($"Error in navigation with validation for block '{blockName}'", ex, blockName);
                return false;
            }
        }

        private async Task<bool> NavigateAsync(string blockName, NavigationType navigationType)
        {
            try
            {
                var blockInfo = GetBlock(blockName);
                if (blockInfo?.UnitOfWork == null)
                {
                    Status = $"Block '{blockName}' not found or has no unit of work";
                    return false;
                }

                // Trigger navigation event
                var args = new NavigationTriggerEventArgs(blockName, _currentFormName, navigationType);
                OnNavigate?.Invoke(this, args);
                
                if (args.Cancel)
                {
                    LogOperation($"Navigation {navigationType} cancelled by trigger", blockName);
                    return false;
                }

                // Perform the navigation
                var success = PerformNavigation(blockInfo, navigationType);
                
                if (success)
                {
                    // Synchronize detail blocks
                    await _relationshipManager.SynchronizeDetailBlocksAsync(blockName);
                    
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