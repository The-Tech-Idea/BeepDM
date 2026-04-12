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
        #region IDisposable Implementation

        /// <summary>
        /// Releases helper subscriptions, cached block state, and timer resources held by the FormsManager instance.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            
            try
            {
                // Unsubscribe from all events and dispose resources
                foreach (var blockInfo in _blocks.Values)
                {
                    if (blockInfo.UnitOfWork != null)
                    {
                        _eventManager.UnsubscribeFromUnitOfWorkEvents(blockInfo.UnitOfWork, blockInfo.BlockName);
                    }
                }
                
                // Dispose helper managers
                _performanceManager?.Dispose();
                _messageBus.OnFormMessage -= OnMessageBusFormMessage;
                _messageBus?.UnsubscribeAll(_currentFormName ?? string.Empty);
                _timerManager?.TimerFired -= OnTimerManagerFired;
                _timerManager?.Dispose();
                
                _blocks.Clear();
                
                LogOperation("UnitofWorksManager disposed");
            }
            catch (Exception ex)
            {
                LogError("Error during UnitofWorksManager disposal", ex);
            }
            finally
            {
                _disposed = true;
            }
        }

        #endregion

        #region Protected Helper Methods (For Partial Classes)

        /// <summary>
        /// Loads configuration and wires manager-level event handlers required after construction.
        /// </summary>
        protected void InitializeManager()
        {
            try
            {
                // Load configuration
                _configurationManager.LoadConfiguration();
                
                // Subscribe to dirty state events
                _dirtyStateManager.OnUnsavedChanges += OnUnsavedChangesHandler;
                
                LogOperation("UnitofWorksManager initialized successfully");
            }
            catch (Exception ex)
            {
                LogError("Error initializing UnitofWorksManager", ex);
                throw;
            }
        }

        /// <summary>
        /// Handles timer fire events from TimerManager by firing the
        /// WHEN-TIMER-EXPIRED trigger on the current form.
        /// </summary>
        private void OnTimerManagerFired(object sender, TimerFiredEventArgs e)
        {
            try
            {
                var ctx = Forms.Models.TriggerContext.ForForm(
                    Forms.Models.TriggerType.WhenTimerExpired, _currentFormName ?? string.Empty, _dmeEditor);
                ctx.Parameters["TimerName"] = e.TimerName;
                ctx.Parameters["FireCount"] = e.FireCount;
                _ = _triggerManager.FireFormTriggerAsync(
                    Forms.Models.TriggerType.WhenTimerExpired, _currentFormName ?? string.Empty, ctx);
            }
            catch (Exception ex)
            {
                LogError($"Error handling timer fired for '{e.TimerName}'", ex);
            }
        }

        /// <summary>
        /// Relays shared-bus messages addressed to this form through the per-form event surface.
        /// </summary>
        /// <param name="sender">Shared message bus instance.</param>
        /// <param name="e">Delivered message payload.</param>
        private void OnMessageBusFormMessage(object sender, FormMessageEventArgs e)
        {
            var message = e?.Message;
            var currentFormName = _currentFormName;

            if (message == null || string.IsNullOrWhiteSpace(currentFormName))
                return;

            if (string.Equals(message.TargetForm, currentFormName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(message.TargetForm, "*", StringComparison.OrdinalIgnoreCase))
            {
                OnFormMessage?.Invoke(this, e);
            }
        }

        /// <summary>
        /// Default handler for unsaved-change notifications raised by the dirty-state manager.
        /// </summary>
        /// <param name="sender">Dirty-state manager instance.</param>
        /// <param name="e">Unsaved-change event payload.</param>
        protected void OnUnsavedChangesHandler(object sender, UnsavedChangesEventArgs e)
        {
            // This can be overridden by derived classes or handled by event subscribers
            // Default behavior could be to show a dialog or log the event
            LogOperation($"Unsaved changes detected in block '{e.BlockName}' with {e.DirtyBlocks.Count} affected blocks");
        }

        #endregion
    }
}
