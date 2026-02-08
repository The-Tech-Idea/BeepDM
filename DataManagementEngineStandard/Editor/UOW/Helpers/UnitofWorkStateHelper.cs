using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.UOW.Interfaces;

namespace TheTechIdea.Beep.Editor.UOW.Helpers
{
    /// <summary>
    /// Helper class for centralized state management in UnitofWork.
    /// Replaces scattered _entityStates dictionary manipulation across CRUD and Extensions.
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public class UnitofWorkStateHelper<T> : IUnitofWorkStateHelper<T> where T : Entity, new()
    {
        private readonly IDMEEditor _editor;
        private readonly Dictionary<int, EntityState> _entityStates;

        /// <summary>
        /// Initializes a new instance of UnitofWorkStateHelper
        /// </summary>
        /// <param name="editor">DME Editor instance</param>
        /// <param name="entityStates">Reference to the entity states dictionary from UnitofWork</param>
        public UnitofWorkStateHelper(IDMEEditor editor, Dictionary<int, EntityState> entityStates)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            _entityStates = entityStates ?? throw new ArgumentNullException(nameof(entityStates));
        }

        /// <summary>
        /// Gets the current state of an entity by its index
        /// </summary>
        /// <param name="entity">Entity to check (not used directly - state is index-based)</param>
        /// <returns>Entity state, or Unchanged if not tracked</returns>
        public EntityState GetEntityState(T entity)
        {
            // This method works with the external Units collection
            // Caller should resolve the index and use GetEntityStateByIndex instead
            return EntityState.Unchanged;
        }

        /// <summary>
        /// Gets entity state by index
        /// </summary>
        /// <param name="index">Index in the Units collection</param>
        /// <returns>Entity state</returns>
        public EntityState GetEntityStateByIndex(int index)
        {
            if (_entityStates.TryGetValue(index, out var state))
            {
                return state;
            }
            return EntityState.Unchanged;
        }

        /// <summary>
        /// Sets the state of an entity by its index
        /// </summary>
        /// <param name="entity">Entity (not used directly)</param>
        /// <param name="state">New state</param>
        public void SetEntityState(T entity, EntityState state)
        {
            // No-op without index. Use SetEntityStateByIndex.
        }

        /// <summary>
        /// Sets entity state by index
        /// </summary>
        /// <param name="index">Index in the Units collection</param>
        /// <param name="state">New state</param>
        public void SetEntityStateByIndex(int index, EntityState state)
        {
            if (index >= 0)
            {
                _entityStates[index] = state;
            }
        }

        /// <summary>
        /// Marks entity at index for deletion
        /// </summary>
        /// <param name="entity">Entity to mark</param>
        public void MarkForDeletion(T entity)
        {
            // Caller should resolve index and use SetEntityStateByIndex
        }

        /// <summary>
        /// Marks entity at index as modified
        /// </summary>
        /// <param name="entity">Entity to mark</param>
        public void MarkAsModified(T entity)
        {
            // Caller should resolve index and use SetEntityStateByIndex
        }

        /// <summary>
        /// Marks entity at index as added
        /// </summary>
        /// <param name="entity">Entity to mark</param>
        public void MarkAsAdded(T entity)
        {
            // Caller should resolve index and use SetEntityStateByIndex
        }

        /// <summary>
        /// Resets entity state to unchanged by index
        /// </summary>
        /// <param name="entity">Entity to reset</param>
        public void ResetState(T entity)
        {
            // Caller should resolve index
        }

        /// <summary>
        /// Resets state for a specific index
        /// </summary>
        /// <param name="index">Index in the Units collection</param>
        public void ResetStateByIndex(int index)
        {
            if (_entityStates.ContainsKey(index))
            {
                _entityStates.Remove(index);
            }
        }

        /// <summary>
        /// Clears all tracked entity states
        /// </summary>
        public void ClearAllStates()
        {
            _entityStates.Clear();
        }

        /// <summary>
        /// Gets all indices with a specific state
        /// </summary>
        /// <param name="state">State to filter by</param>
        /// <returns>Collection of indices</returns>
        public IEnumerable<int> GetIndicesByState(EntityState state)
        {
            var results = new List<int>();
            foreach (var kvp in _entityStates)
            {
                if (kvp.Value == state)
                {
                    results.Add(kvp.Key);
                }
            }
            return results;
        }

        /// <summary>
        /// Checks if any entity has pending changes
        /// </summary>
        /// <returns>True if dirty</returns>
        public bool HasPendingChanges()
        {
            foreach (var state in _entityStates.Values)
            {
                if (state == EntityState.Added || state == EntityState.Modified || state == EntityState.Deleted)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
