using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace TheTechIdea.Beep.Editor
{
    public partial class ObservableBindingList<T>
    {
        #region "Master-Detail — Phase 5B"

        /// <summary>
        /// Internal record that describes a registered detail relationship.
        /// </summary>
        private class DetailRegistration
        {
            /// <summary>The child list (stored as object because TChild varies).</summary>
            public object ChildList { get; set; }

            /// <summary>PropertyInfo on the child type for the foreign key (e.g. "ParentId").</summary>
            public PropertyInfo ForeignKeyProperty { get; set; }

            /// <summary>PropertyInfo on T for the master key (e.g. "Id").</summary>
            public PropertyInfo MasterKeyProperty { get; set; }

            /// <summary>Action that applies the filter on the child list given the master key value.</summary>
            public Action<object> ApplyFilterAction { get; set; }

            /// <summary>Action that removes the filter on the child list.</summary>
            public Action RemoveFilterAction { get; set; }
        }

        /// <summary>
        /// All registered detail relationships.
        /// </summary>
        private readonly List<DetailRegistration> _detailRegistrations = new List<DetailRegistration>();

        /// <summary>
        /// Public read-only list of registered child list references.
        /// </summary>
        public IReadOnlyList<object> DetailLists => _detailRegistrations.Select(r => r.ChildList).ToList().AsReadOnly();

        /// <summary>
        /// Registers a child list as a detail of this (master) list.
        /// When the current item on this list changes, the child list is automatically
        /// filtered to show only items whose foreignKeyProp matches the master's masterKeyProp.
        /// </summary>
        /// <typeparam name="TChild">The entity type of the child list.</typeparam>
        /// <param name="childList">The child ObservableBindingList to auto-filter.</param>
        /// <param name="foreignKeyProp">Property name on TChild that holds the FK value (e.g. "ParentId").</param>
        /// <param name="masterKeyProp">Property name on T that holds the master key value (e.g. "Id").</param>
        public void RegisterDetail<TChild>(
            ObservableBindingList<TChild> childList,
            string foreignKeyProp,
            string masterKeyProp)
            where TChild : class, INotifyPropertyChanged, new()
        {
            if (childList == null) throw new ArgumentNullException(nameof(childList));
            if (string.IsNullOrEmpty(foreignKeyProp)) throw new ArgumentNullException(nameof(foreignKeyProp));
            if (string.IsNullOrEmpty(masterKeyProp)) throw new ArgumentNullException(nameof(masterKeyProp));

            var fkProp = typeof(TChild).GetProperty(foreignKeyProp);
            if (fkProp == null) throw new ArgumentException($"Property '{foreignKeyProp}' not found on type {typeof(TChild).Name}.", nameof(foreignKeyProp));

            var mkProp = typeof(T).GetProperty(masterKeyProp);
            if (mkProp == null) throw new ArgumentException($"Property '{masterKeyProp}' not found on type {typeof(T).Name}.", nameof(masterKeyProp));

            // Check if already registered
            if (_detailRegistrations.Any(r => ReferenceEquals(r.ChildList, childList)))
                return; // Already registered — skip

            var reg = new DetailRegistration
            {
                ChildList = childList,
                ForeignKeyProperty = fkProp,
                MasterKeyProperty = mkProp,
                ApplyFilterAction = (masterKeyValue) =>
                {
                    // Build a predicate that filters child items by FK == masterKeyValue
                    childList.ApplyFilter(child =>
                    {
                        var fkValue = fkProp.GetValue(child);
                        return Equals(fkValue, masterKeyValue);
                    });
                    // Reset child cursor to first item
                    if (childList.Count > 0)
                        childList.SetPosition(0);
                },
                RemoveFilterAction = () =>
                {
                    childList.RemoveFilter();
                }
            };

            _detailRegistrations.Add(reg);

            // Apply initial filter if there's a current master item
            if (IsPositionValid)
            {
                var masterKey = mkProp.GetValue(Current);
                reg.ApplyFilterAction(masterKey);
            }
        }

        /// <summary>
        /// Unregisters a child list from the master-detail relationship.
        /// Removes any active filter that was applied by the relationship.
        /// </summary>
        public void UnregisterDetail<TChild>(ObservableBindingList<TChild> childList)
            where TChild : class, INotifyPropertyChanged, new()
        {
            if (childList == null) return;

            var reg = _detailRegistrations.FirstOrDefault(r => ReferenceEquals(r.ChildList, childList));
            if (reg != null)
            {
                reg.RemoveFilterAction();
                _detailRegistrations.Remove(reg);
            }
        }

        /// <summary>
        /// Unregisters all detail lists.
        /// </summary>
        public void UnregisterAllDetails()
        {
            foreach (var reg in _detailRegistrations.ToList())
            {
                reg.RemoveFilterAction();
            }
            _detailRegistrations.Clear();
        }

        /// <summary>
        /// Called internally when the current position changes.
        /// Iterates all registered detail lists and re-applies their FK filter
        /// based on the new master key value.
        /// </summary>
        private void SyncDetailLists()
        {
            if (_detailRegistrations.Count == 0) return;

            if (IsPositionValid)
            {
                T currentMaster = Current;
                foreach (var reg in _detailRegistrations)
                {
                    var masterKeyValue = reg.MasterKeyProperty.GetValue(currentMaster);
                    reg.ApplyFilterAction(masterKeyValue);
                }
            }
            else
            {
                // No valid master item — clear all detail filters
                foreach (var reg in _detailRegistrations)
                {
                    reg.RemoveFilterAction();
                }
            }
        }

        #endregion
    }
}
