using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.UOW.Interfaces;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.UOW.Helpers
{
    /// <summary>
    /// Helper class for centralized state management in UnitofWork.
    /// Delegates to ObservableBindingList's built-in tracking system instead of a separate _entityStates dictionary.
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public class UnitofWorkStateHelper<T> : IUnitofWorkStateHelper<T> where T : Entity, new()
    {
        private readonly IDMEEditor _editor;
        private readonly Func<ObservableBindingList<T>> _unitsProvider;

        /// <summary>
        /// Initializes a new instance of UnitofWorkStateHelper
        /// </summary>
        /// <param name="editor">DME Editor instance</param>
        /// <param name="unitsProvider">Lazy provider for the Units collection (ObservableBindingList)</param>
        public UnitofWorkStateHelper(IDMEEditor editor, Func<ObservableBindingList<T>> unitsProvider)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            _unitsProvider = unitsProvider ?? throw new ArgumentNullException(nameof(unitsProvider));
        }

        /// <summary>Gets the current Units collection from the provider.</summary>
        private ObservableBindingList<T> Units => _unitsProvider?.Invoke();

        /// <summary>
        /// Gets the current state of an entity by querying OBL's tracking system.
        /// </summary>
        /// <param name="entity">Entity to check</param>
        /// <returns>Entity state, or Unchanged if not tracked</returns>
        public EntityState GetEntityState(T entity)
        {
            var units = Units;
            if (units == null || entity == null) return EntityState.Unchanged;

            var tr = units.GetTrackingItem(entity);
            return tr?.EntityState ?? EntityState.Unchanged;
        }

        /// <summary>
        /// Sets the state of an entity via OBL's tracking system.
        /// </summary>
        /// <param name="entity">Entity to set state for</param>
        /// <param name="state">New state</param>
        public void SetEntityState(T entity, EntityState state)
        {
            var units = Units;
            if (units == null || entity == null) return;

            var tr = units.GetTrackingItem(entity);
            if (tr != null)
            {
                tr.EntityState = state;
            }
        }

        /// <summary>
        /// Marks entity for deletion via OBL's tracking system.
        /// </summary>
        /// <param name="entity">Entity to mark for deletion</param>
        public void MarkForDeletion(T entity)
        {
            SetEntityState(entity, EntityState.Deleted);
        }

        /// <summary>
        /// Marks entity as modified via OBL's tracking system.
        /// </summary>
        /// <param name="entity">Entity to mark as modified</param>
        public void MarkAsModified(T entity)
        {
            SetEntityState(entity, EntityState.Modified);
        }

        /// <summary>
        /// Marks entity as added via OBL's tracking system.
        /// </summary>
        /// <param name="entity">Entity to mark as added</param>
        public void MarkAsAdded(T entity)
        {
            SetEntityState(entity, EntityState.Added);
        }

        /// <summary>
        /// Resets entity state to unchanged via OBL's tracking system.
        /// </summary>
        /// <param name="entity">Entity to reset</param>
        public void ResetState(T entity)
        {
            SetEntityState(entity, EntityState.Unchanged);
        }

        /// <summary>
        /// Clears all tracked entity states via OBL's AcceptChanges.
        /// </summary>
        public void ClearAllStates()
        {
            Units?.AcceptChanges();
        }

        /// <summary>
        /// Gets all indices with a specific state by querying OBL's tracking.
        /// </summary>
        /// <param name="state">State to filter by</param>
        /// <returns>Collection of indices</returns>
        public IEnumerable<int> GetIndicesByState(EntityState state)
        {
            var units = Units;
            if (units == null) yield break;

            for (int i = 0; i < units.Count; i++)
            {
                var tr = units.GetTrackingItem(units[i]);
                if (tr != null && tr.EntityState == state)
                {
                    yield return i;
                }
            }
        }

        /// <summary>
        /// Checks if any entity has pending changes via OBL's HasChanges.
        /// </summary>
        /// <returns>True if dirty</returns>
        public bool HasPendingChanges()
        {
            return Units?.HasChanges ?? false;
        }
    }
}
