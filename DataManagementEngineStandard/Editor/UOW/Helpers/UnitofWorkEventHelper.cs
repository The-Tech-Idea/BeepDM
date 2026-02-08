using System;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.UOW.Interfaces;

namespace TheTechIdea.Beep.Editor.UOW.Helpers
{
    /// <summary>
    /// Helper class for centralized event management in UnitofWork.
    /// Standardizes event creation, property change handling, and event dispatch.
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public class UnitofWorkEventHelper<T> : IUnitofWorkEventHelper<T> where T : Entity, new()
    {
        private readonly IDMEEditor _editor;
        private readonly string _entityName;

        /// <summary>
        /// Initializes a new instance of UnitofWorkEventHelper
        /// </summary>
        /// <param name="editor">DME Editor instance</param>
        /// <param name="entityName">Name of the entity type</param>
        public UnitofWorkEventHelper(IDMEEditor editor, string entityName = null)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            _entityName = entityName ?? typeof(T).Name;
        }

        /// <summary>
        /// Creates standardized event parameters for UnitofWork operations
        /// </summary>
        /// <param name="entity">Entity involved in the event</param>
        /// <param name="action">The event action type</param>
        /// <returns>Populated UnitofWorkParams</returns>
        public UnitofWorkParams CreateEventParams(T entity, EventAction action)
        {
            return new UnitofWorkParams
            {
                Cancel = false,
                EventAction = action,
                EntityName = _entityName,
                Record = entity
            };
        }

        /// <summary>
        /// Creates event parameters with property change details
        /// </summary>
        /// <param name="entity">Entity that changed</param>
        /// <param name="propertyName">Name of changed property</param>
        /// <param name="newValue">New value of the property</param>
        /// <returns>Populated UnitofWorkParams</returns>
        public UnitofWorkParams CreatePropertyChangeParams(T entity, string propertyName, object newValue)
        {
            return new UnitofWorkParams
            {
                Cancel = false,
                EventAction = EventAction.PostEdit,
                EntityName = _entityName,
                Record = entity,
                PropertyName = propertyName,
                PropertyValue = newValue?.ToString()
            };
        }

        /// <summary>
        /// Handles property changed events by creating a change record
        /// </summary>
        /// <param name="entity">Entity that changed</param>
        /// <param name="propertyName">Name of changed property</param>
        /// <param name="oldValue">Old value</param>
        /// <param name="newValue">New value</param>
        public void HandlePropertyChanged(T entity, string propertyName, object oldValue, object newValue)
        {
            try
            {
                if (_editor != null && _editor.ConfigEditor != null)
                {
                    _editor.AddLogMessage("UnitofWork",
                        $"Property '{propertyName}' changed on {_entityName}: '{oldValue}' -> '{newValue}'",
                        DateTime.Now, 0, _entityName, Errors.Ok);
                }
            }
            catch (Exception ex)
            {
                _editor?.AddLogMessage("UnitofWorkEventHelper",
                    $"Error handling property change: {ex.Message}",
                    DateTime.Now, -1, null, Errors.Failed);
            }
        }

        /// <summary>
        /// Raises appropriate events for entity operations using the provided event handlers
        /// </summary>
        /// <param name="entity">Entity involved</param>
        /// <param name="action">Action being performed</param>
        /// <param name="isPreEvent">True for pre-events, false for post-events</param>
        /// <returns>True if operation should continue (not cancelled)</returns>
        public bool RaiseEntityEvent(T entity, EventAction action, bool isPreEvent)
        {
            // This method is designed to be called by the UnitofWork class
            // which owns the actual event handlers. The helper creates the params
            // and the UnitofWork invokes the event.
            // Returns true (continue) by default - actual cancellation is handled
            // by the UnitofWork when it checks eventArgs.Cancel after invoking.
            return true;
        }

        /// <summary>
        /// Raises an event with cancellation support
        /// </summary>
        /// <param name="eventHandler">The event handler to invoke</param>
        /// <param name="sender">Event sender</param>
        /// <param name="entity">Entity involved</param>
        /// <param name="action">Event action type</param>
        /// <returns>True if operation should continue (not cancelled)</returns>
        public bool RaiseEvent(EventHandler<UnitofWorkParams> eventHandler, object sender, T entity, EventAction action)
        {
            if (eventHandler == null) return true;

            var args = CreateEventParams(entity, action);
            eventHandler.Invoke(sender, args);
            return !args.Cancel;
        }
    }
}
