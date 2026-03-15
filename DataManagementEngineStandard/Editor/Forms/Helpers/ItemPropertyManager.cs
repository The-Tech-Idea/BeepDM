using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;

namespace TheTechIdea.Beep.Editor.Forms.Helpers
{
    /// <summary>
    /// Manages item/field properties for data blocks.
    /// Oracle Forms equivalent: SET_ITEM_PROPERTY, GET_ITEM_PROPERTY built-ins.
    /// Thread-safe implementation using ConcurrentDictionary.
    /// </summary>
    public class ItemPropertyManager : IItemPropertyManager, IDisposable
    {
        #region Private Fields
        
        /// <summary>
        /// Storage: blockName -> (itemName -> ItemInfo)
        /// </summary>
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ItemInfo>> _blockItems;
        
        /// <summary>
        /// Tab order per block
        /// </summary>
        private readonly ConcurrentDictionary<string, List<string>> _tabOrders;
        
        private readonly IDMEEditor _editor;
        private readonly object _eventLock = new object();
        private bool _disposed;
        
        #endregion
        
        #region Events
        
        /// <inheritdoc />
        public event EventHandler<ItemPropertyChangedEventArgs> ItemPropertyChanged;
        
        /// <inheritdoc />
        public event EventHandler<ItemValueChangedEventArgs> ItemValueChanged;
        
        /// <inheritdoc />
        public event EventHandler<ItemErrorEventArgs> ItemErrorChanged;
        
        #endregion
        
        #region Constructors
        
        /// <summary>
        /// Create a new ItemPropertyManager
        /// </summary>
        public ItemPropertyManager() : this(null) { }
        
        /// <summary>
        /// Create a new ItemPropertyManager with editor reference
        /// </summary>
        /// <param name="editor">DME Editor for logging</param>
        public ItemPropertyManager(IDMEEditor editor)
        {
            _editor = editor;
            _blockItems = new ConcurrentDictionary<string, ConcurrentDictionary<string, ItemInfo>>(StringComparer.OrdinalIgnoreCase);
            _tabOrders = new ConcurrentDictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        }
        
        #endregion
        
        #region Item Registration
        
        /// <inheritdoc />
        public void RegisterItem(string blockName, string itemName, ItemInfo info)
        {
            if (string.IsNullOrEmpty(blockName))
                throw new ArgumentNullException(nameof(blockName));
            if (string.IsNullOrEmpty(itemName))
                throw new ArgumentNullException(nameof(itemName));
            if (info == null)
                throw new ArgumentNullException(nameof(info));
            
            // Ensure block name and item name are set in the info
            info.BlockName = blockName;
            info.ItemName = itemName;
            
            var blockDict = _blockItems.GetOrAdd(blockName,
                _ => new ConcurrentDictionary<string, ItemInfo>(StringComparer.OrdinalIgnoreCase));
            
            blockDict[itemName] = info;
            
            // Update tab order if not present
            var tabOrder = _tabOrders.GetOrAdd(blockName, _ => new List<string>());
            lock (tabOrder)
            {
                if (!tabOrder.Contains(itemName, StringComparer.OrdinalIgnoreCase))
                {
                    // Insert based on TabIndex
                    int insertIndex = tabOrder.Count;
                    for (int i = 0; i < tabOrder.Count; i++)
                    {
                        var existingItem = GetItem(blockName, tabOrder[i]);
                        if (existingItem != null && existingItem.TabIndex > info.TabIndex)
                        {
                            insertIndex = i;
                            break;
                        }
                    }
                    tabOrder.Insert(insertIndex, itemName);
                }
            }
            
            _editor?.AddLogMessage($"ItemPropertyManager: Registered item: {blockName}.{itemName}");
        }
        
        /// <inheritdoc />
        public void RegisterItemsFromEntityStructure(string blockName, IEntityStructure structure)
        {
            if (string.IsNullOrEmpty(blockName))
                throw new ArgumentNullException(nameof(blockName));
            if (structure == null)
                throw new ArgumentNullException(nameof(structure));
            
            int tabIndex = 0;
            foreach (var field in structure.Fields)
            {
                var info = new ItemInfo
                {
                    BlockName = blockName,
                    ItemName = field.FieldName,
                    BoundProperty = field.FieldName,
                    
                    // Map from EntityField
                    DataType = field.Fieldtype != null ? Type.GetType(field.Fieldtype) ?? typeof(object) : typeof(object),
                    DatabaseTypeName = field.Fieldtype,
                    MaxLength = field.Size1 > 0 ? field.Size1 : 0,
                    Precision = field.NumericPrecision,
                    Scale = field.NumericScale,
                    AllowNull = field.AllowDBNull,
                    
                    // Properties
                    Required = !field.AllowDBNull,
                    PromptText = field.FieldName, // Default to field name
                    TabIndex = tabIndex++,
                    
                    // Default permissions
                    QueryAllowed = true,
                    InsertAllowed = !field.IsAutoIncrement,
                    UpdateAllowed = !field.IsAutoIncrement && !field.IsKey,
                    Enabled = true,
                    Visible = true
                };
                
                RegisterItem(blockName, field.FieldName, info);
            }
            
            _editor?.AddLogMessage($"ItemPropertyManager: Registered {structure.Fields.Count} items from entity structure for block: {blockName}");
        }
        
        /// <inheritdoc />
        public void UnregisterItem(string blockName, string itemName)
        {
            if (string.IsNullOrEmpty(blockName) || string.IsNullOrEmpty(itemName))
                return;
            
            if (_blockItems.TryGetValue(blockName, out var blockDict))
            {
                blockDict.TryRemove(itemName, out _);
            }
            
            if (_tabOrders.TryGetValue(blockName, out var tabOrder))
            {
                lock (tabOrder)
                {
                    tabOrder.RemoveAll(x => x.Equals(itemName, StringComparison.OrdinalIgnoreCase));
                }
            }
            
            _editor?.AddLogMessage($"ItemPropertyManager: Unregistered item: {blockName}.{itemName}");
        }
        
        /// <inheritdoc />
        public void ClearBlockItems(string blockName)
        {
            if (string.IsNullOrEmpty(blockName))
                return;
            
            _blockItems.TryRemove(blockName, out _);
            _tabOrders.TryRemove(blockName, out _);
            
            _editor?.AddLogMessage($"ItemPropertyManager: Cleared all items for block: {blockName}");
        }
        
        #endregion
        
        #region Item Retrieval
        
        /// <inheritdoc />
        public ItemInfo GetItem(string blockName, string itemName)
        {
            if (string.IsNullOrEmpty(blockName) || string.IsNullOrEmpty(itemName))
                return null;
            
            if (_blockItems.TryGetValue(blockName, out var blockDict))
            {
                if (blockDict.TryGetValue(itemName, out var info))
                {
                    return info;
                }
            }
            
            return null;
        }
        
        /// <inheritdoc />
        public IReadOnlyList<ItemInfo> GetAllItems(string blockName)
        {
            if (string.IsNullOrEmpty(blockName))
                return Array.Empty<ItemInfo>();
            
            if (_blockItems.TryGetValue(blockName, out var blockDict))
            {
                return blockDict.Values.OrderBy(i => i.TabIndex).ToList().AsReadOnly();
            }
            
            return Array.Empty<ItemInfo>();
        }
        
        /// <inheritdoc />
        public bool ItemExists(string blockName, string itemName)
        {
            return GetItem(blockName, itemName) != null;
        }
        
        /// <inheritdoc />
        public int GetItemCount(string blockName)
        {
            if (string.IsNullOrEmpty(blockName))
                return 0;
            
            if (_blockItems.TryGetValue(blockName, out var blockDict))
            {
                return blockDict.Count;
            }
            
            return 0;
        }
        
        #endregion
        
        #region SET_ITEM_PROPERTY Built-ins
        
        /// <inheritdoc />
        public void SetItemProperty(string blockName, string itemName, string propertyName, object value)
        {
            var item = GetItem(blockName, itemName);
            if (item == null)
            {
                _editor?.AddLogMessage($"ItemPropertyManager: Item not found for SetItemProperty: {blockName}.{itemName}");
                return;
            }
            
            var property = typeof(ItemInfo).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property == null || !property.CanWrite)
            {
                _editor?.AddLogMessage($"ItemPropertyManager: Property not found or not writable: {propertyName}");
                return;
            }
            
            object oldValue = property.GetValue(item);
            
            try
            {
                object convertedValue = ConvertValue(value, property.PropertyType);
                property.SetValue(item, convertedValue);
                
                RaiseItemPropertyChanged(blockName, itemName, propertyName, oldValue, convertedValue);
            }
            catch (Exception ex)
            {
                _editor?.AddLogMessage($"ItemPropertyManager: Error setting property {propertyName}: {ex.Message}");
            }
        }
        
        /// <inheritdoc />
        public void SetItemEnabled(string blockName, string itemName, bool enabled)
        {
            var item = GetItem(blockName, itemName);
            if (item == null) return;
            
            var oldValue = item.Enabled;
            if (oldValue != enabled)
            {
                item.Enabled = enabled;
                RaiseItemPropertyChanged(blockName, itemName, nameof(ItemInfo.Enabled), oldValue, enabled);
            }
        }
        
        /// <inheritdoc />
        public void SetItemVisible(string blockName, string itemName, bool visible)
        {
            var item = GetItem(blockName, itemName);
            if (item == null) return;
            
            var oldValue = item.Visible;
            if (oldValue != visible)
            {
                item.Visible = visible;
                RaiseItemPropertyChanged(blockName, itemName, nameof(ItemInfo.Visible), oldValue, visible);
            }
        }
        
        /// <inheritdoc />
        public void SetItemRequired(string blockName, string itemName, bool required)
        {
            var item = GetItem(blockName, itemName);
            if (item == null) return;
            
            var oldValue = item.Required;
            if (oldValue != required)
            {
                item.Required = required;
                RaiseItemPropertyChanged(blockName, itemName, nameof(ItemInfo.Required), oldValue, required);
            }
        }
        
        /// <inheritdoc />
        public void SetItemQueryAllowed(string blockName, string itemName, bool allowed)
        {
            var item = GetItem(blockName, itemName);
            if (item == null) return;
            
            var oldValue = item.QueryAllowed;
            if (oldValue != allowed)
            {
                item.QueryAllowed = allowed;
                RaiseItemPropertyChanged(blockName, itemName, nameof(ItemInfo.QueryAllowed), oldValue, allowed);
            }
        }
        
        /// <inheritdoc />
        public void SetItemInsertAllowed(string blockName, string itemName, bool allowed)
        {
            var item = GetItem(blockName, itemName);
            if (item == null) return;
            
            var oldValue = item.InsertAllowed;
            if (oldValue != allowed)
            {
                item.InsertAllowed = allowed;
                RaiseItemPropertyChanged(blockName, itemName, nameof(ItemInfo.InsertAllowed), oldValue, allowed);
            }
        }
        
        /// <inheritdoc />
        public void SetItemUpdateAllowed(string blockName, string itemName, bool allowed)
        {
            var item = GetItem(blockName, itemName);
            if (item == null) return;
            
            var oldValue = item.UpdateAllowed;
            if (oldValue != allowed)
            {
                item.UpdateAllowed = allowed;
                RaiseItemPropertyChanged(blockName, itemName, nameof(ItemInfo.UpdateAllowed), oldValue, allowed);
            }
        }
        
        /// <inheritdoc />
        public void SetItemDefaultValue(string blockName, string itemName, object value)
        {
            var item = GetItem(blockName, itemName);
            if (item == null) return;
            
            var oldValue = item.DefaultValue;
            item.DefaultValue = value;
            RaiseItemPropertyChanged(blockName, itemName, nameof(ItemInfo.DefaultValue), oldValue, value);
        }
        
        /// <inheritdoc />
        public void SetItemLOV(string blockName, string itemName, string lovName)
        {
            var item = GetItem(blockName, itemName);
            if (item == null) return;
            
            var oldValue = item.LOVName;
            if (!string.Equals(oldValue, lovName, StringComparison.OrdinalIgnoreCase))
            {
                item.LOVName = lovName;
                RaiseItemPropertyChanged(blockName, itemName, nameof(ItemInfo.LOVName), oldValue, lovName);
            }
        }
        
        /// <inheritdoc />
        public void SetItemFormatMask(string blockName, string itemName, string formatMask)
        {
            var item = GetItem(blockName, itemName);
            if (item == null) return;
            
            var oldValue = item.FormatMask;
            if (!string.Equals(oldValue, formatMask, StringComparison.Ordinal))
            {
                item.FormatMask = formatMask;
                RaiseItemPropertyChanged(blockName, itemName, nameof(ItemInfo.FormatMask), oldValue, formatMask);
            }
        }
        
        /// <inheritdoc />
        public void SetItemPromptText(string blockName, string itemName, string text)
        {
            var item = GetItem(blockName, itemName);
            if (item == null) return;
            
            var oldValue = item.PromptText;
            if (!string.Equals(oldValue, text, StringComparison.Ordinal))
            {
                item.PromptText = text;
                RaiseItemPropertyChanged(blockName, itemName, nameof(ItemInfo.PromptText), oldValue, text);
            }
        }
        
        /// <inheritdoc />
        public void SetItemHintText(string blockName, string itemName, string text)
        {
            var item = GetItem(blockName, itemName);
            if (item == null) return;
            
            var oldValue = item.HintText;
            if (!string.Equals(oldValue, text, StringComparison.Ordinal))
            {
                item.HintText = text;
                RaiseItemPropertyChanged(blockName, itemName, nameof(ItemInfo.HintText), oldValue, text);
            }
        }
        
        #endregion
        
        #region GET_ITEM_PROPERTY Built-ins
        
        /// <inheritdoc />
        public object GetItemProperty(string blockName, string itemName, string propertyName)
        {
            var item = GetItem(blockName, itemName);
            if (item == null)
                return null;
            
            var property = typeof(ItemInfo).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property == null || !property.CanRead)
                return null;
            
            return property.GetValue(item);
        }
        
        /// <inheritdoc />
        public bool IsItemEnabled(string blockName, string itemName)
        {
            var item = GetItem(blockName, itemName);
            return item?.Enabled ?? false;
        }
        
        /// <inheritdoc />
        public bool IsItemVisible(string blockName, string itemName)
        {
            var item = GetItem(blockName, itemName);
            return item?.Visible ?? false;
        }
        
        /// <inheritdoc />
        public bool IsItemRequired(string blockName, string itemName)
        {
            var item = GetItem(blockName, itemName);
            return item?.Required ?? false;
        }
        
        /// <inheritdoc />
        public bool IsItemQueryAllowed(string blockName, string itemName)
        {
            var item = GetItem(blockName, itemName);
            return item?.QueryAllowed ?? false;
        }
        
        /// <inheritdoc />
        public bool IsItemInsertAllowed(string blockName, string itemName)
        {
            var item = GetItem(blockName, itemName);
            return item?.InsertAllowed ?? false;
        }
        
        /// <inheritdoc />
        public bool IsItemUpdateAllowed(string blockName, string itemName)
        {
            var item = GetItem(blockName, itemName);
            return item?.UpdateAllowed ?? false;
        }
        
        /// <inheritdoc />
        public object GetItemDefaultValue(string blockName, string itemName)
        {
            var item = GetItem(blockName, itemName);
            return item?.DefaultValue;
        }
        
        /// <inheritdoc />
        public string GetItemLOV(string blockName, string itemName)
        {
            var item = GetItem(blockName, itemName);
            return item?.LOVName;
        }
        
        /// <inheritdoc />
        public string GetItemFormatMask(string blockName, string itemName)
        {
            var item = GetItem(blockName, itemName);
            return item?.FormatMask;
        }
        
        #endregion
        
        #region Value Management
        
        /// <inheritdoc />
        public void SetItemValue(string blockName, string itemName, object value)
        {
            var item = GetItem(blockName, itemName);
            if (item == null) return;
            
            object oldValue = item.CurrentValue;
            bool valueChanged = !Equals(oldValue, value);
            
            if (valueChanged)
            {
                item.OldValue = oldValue;
                item.CurrentValue = value;
                item.IsDirty = true;
                
                RaiseItemValueChanged(blockName, itemName, oldValue, value, true, -1);
            }
        }
        
        /// <inheritdoc />
        public object GetItemValue(string blockName, string itemName)
        {
            var item = GetItem(blockName, itemName);
            return item?.CurrentValue;
        }
        
        /// <inheritdoc />
        public void ApplyDefaultValues(string blockName, object record)
        {
            if (record == null) return;
            
            var items = GetAllItems(blockName);
            var recordType = record.GetType();
            
            foreach (var item in items)
            {
                if (item.DefaultValue != null && !string.IsNullOrEmpty(item.BoundProperty))
                {
                    var prop = recordType.GetProperty(item.BoundProperty, BindingFlags.Public | BindingFlags.Instance);
                    if (prop != null && prop.CanWrite)
                    {
                        try
                        {
                            object convertedValue = ConvertValue(item.DefaultValue, prop.PropertyType);
                            prop.SetValue(record, convertedValue);
                        }
                        catch (Exception ex)
                        {
                            _editor?.AddLogMessage($"ItemPropertyManager: Error applying default value for {item.ItemName}: {ex.Message}");
                        }
                    }
                }
            }
        }
        
        /// <inheritdoc />
        public void ClearItemValues(string blockName)
        {
            var items = GetAllItems(blockName);
            foreach (var item in items)
            {
                item.CurrentValue = null;
                item.OldValue = null;
                item.IsDirty = false;
            }
        }
        
        /// <inheritdoc />
        public Dictionary<string, object> GetAllItemValues(string blockName)
        {
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var items = GetAllItems(blockName);
            
            foreach (var item in items)
            {
                result[item.ItemName] = item.CurrentValue;
            }
            
            return result;
        }
        
        /// <inheritdoc />
        public void SetAllItemValues(string blockName, IDictionary<string, object> values)
        {
            if (values == null) return;
            
            foreach (var kvp in values)
            {
                SetItemValue(blockName, kvp.Key, kvp.Value);
            }
        }
        
        #endregion
        
        #region State Management
        
        /// <inheritdoc />
        public void MarkItemDirty(string blockName, string itemName, object oldValue)
        {
            var item = GetItem(blockName, itemName);
            if (item == null) return;
            
            item.OldValue = oldValue;
            item.IsDirty = true;
        }
        
        /// <inheritdoc />
        public void ClearItemDirty(string blockName, string itemName)
        {
            var item = GetItem(blockName, itemName);
            if (item == null) return;
            
            item.IsDirty = false;
            item.OldValue = item.CurrentValue;
        }
        
        /// <inheritdoc />
        public bool IsItemDirty(string blockName, string itemName)
        {
            var item = GetItem(blockName, itemName);
            return item?.IsDirty ?? false;
        }
        
        /// <inheritdoc />
        public IReadOnlyList<string> GetDirtyItems(string blockName)
        {
            var items = GetAllItems(blockName);
            return items.Where(i => i.IsDirty).Select(i => i.ItemName).ToList().AsReadOnly();
        }
        
        /// <inheritdoc />
        public void ClearAllDirtyFlags(string blockName)
        {
            var items = GetAllItems(blockName);
            foreach (var item in items)
            {
                item.IsDirty = false;
                item.OldValue = item.CurrentValue;
            }
        }
        
        #endregion
        
        #region Error State
        
        /// <inheritdoc />
        public void SetItemError(string blockName, string itemName, string errorMessage)
        {
            var item = GetItem(blockName, itemName);
            if (item == null) return;
            
            bool hadError = item.HasError;
            item.SetError(errorMessage);
            
            RaiseItemErrorChanged(blockName, itemName, true, errorMessage, null);
        }
        
        /// <inheritdoc />
        public void ClearItemError(string blockName, string itemName)
        {
            var item = GetItem(blockName, itemName);
            if (item == null) return;
            
            if (item.HasError)
            {
                item.ClearError();
                RaiseItemErrorChanged(blockName, itemName, false, null, null);
            }
        }
        
        /// <inheritdoc />
        public void ClearAllItemErrors(string blockName)
        {
            var items = GetAllItems(blockName);
            foreach (var item in items)
            {
                if (item.HasError)
                {
                    item.ClearError();
                    RaiseItemErrorChanged(blockName, item.ItemName, false, null, null);
                }
            }
        }
        
        /// <inheritdoc />
        public bool HasItemError(string blockName, string itemName)
        {
            var item = GetItem(blockName, itemName);
            return item?.HasError ?? false;
        }
        
        /// <inheritdoc />
        public string GetItemErrorMessage(string blockName, string itemName)
        {
            var item = GetItem(blockName, itemName);
            return item?.ErrorMessage;
        }
        
        /// <inheritdoc />
        public IReadOnlyList<ItemInfo> GetItemsWithErrors(string blockName)
        {
            var items = GetAllItems(blockName);
            return items.Where(i => i.HasError).ToList().AsReadOnly();
        }
        
        #endregion
        
        #region Navigation Order
        
        /// <inheritdoc />
        public void SetTabOrder(string blockName, IEnumerable<string> itemOrder)
        {
            if (string.IsNullOrEmpty(blockName) || itemOrder == null)
                return;
            
            var newOrder = itemOrder.ToList();
            _tabOrders[blockName] = newOrder;
            
            // Update tab indices on items
            for (int i = 0; i < newOrder.Count; i++)
            {
                var item = GetItem(blockName, newOrder[i]);
                if (item != null)
                {
                    item.TabIndex = i;
                }
            }
        }
        
        /// <inheritdoc />
        public IReadOnlyList<string> GetTabOrder(string blockName)
        {
            if (_tabOrders.TryGetValue(blockName, out var tabOrder))
            {
                lock (tabOrder)
                {
                    return tabOrder.ToList().AsReadOnly();
                }
            }
            
            // Return items sorted by TabIndex
            return GetAllItems(blockName).Select(i => i.ItemName).ToList().AsReadOnly();
        }
        
        /// <inheritdoc />
        public string GetNextItem(string blockName, string currentItem)
        {
            var tabOrder = GetTabOrder(blockName);
            if (tabOrder.Count == 0)
                return null;
            
            int currentIndex = -1;
            for (int i = 0; i < tabOrder.Count; i++)
            {
                if (tabOrder[i].Equals(currentItem, StringComparison.OrdinalIgnoreCase))
                {
                    currentIndex = i;
                    break;
                }
            }
            
            if (currentIndex == -1)
                return tabOrder.FirstOrDefault();
            
            // Find next enabled item
            for (int i = currentIndex + 1; i < tabOrder.Count; i++)
            {
                var item = GetItem(blockName, tabOrder[i]);
                if (item != null && item.Enabled && item.Visible)
                {
                    return tabOrder[i];
                }
            }
            
            // Wrap around to beginning
            for (int i = 0; i <= currentIndex; i++)
            {
                var item = GetItem(blockName, tabOrder[i]);
                if (item != null && item.Enabled && item.Visible)
                {
                    return tabOrder[i];
                }
            }
            
            return null;
        }
        
        /// <inheritdoc />
        public string GetPreviousItem(string blockName, string currentItem)
        {
            var tabOrder = GetTabOrder(blockName);
            if (tabOrder.Count == 0)
                return null;
            
            int currentIndex = -1;
            for (int i = 0; i < tabOrder.Count; i++)
            {
                if (tabOrder[i].Equals(currentItem, StringComparison.OrdinalIgnoreCase))
                {
                    currentIndex = i;
                    break;
                }
            }
            
            if (currentIndex == -1)
                return tabOrder.LastOrDefault();
            
            // Find previous enabled item
            for (int i = currentIndex - 1; i >= 0; i--)
            {
                var item = GetItem(blockName, tabOrder[i]);
                if (item != null && item.Enabled && item.Visible)
                {
                    return tabOrder[i];
                }
            }
            
            // Wrap around to end
            for (int i = tabOrder.Count - 1; i >= currentIndex; i--)
            {
                var item = GetItem(blockName, tabOrder[i]);
                if (item != null && item.Enabled && item.Visible)
                {
                    return tabOrder[i];
                }
            }
            
            return null;
        }
        
        /// <inheritdoc />
        public string GetFirstItem(string blockName)
        {
            var tabOrder = GetTabOrder(blockName);
            
            foreach (var itemName in tabOrder)
            {
                var item = GetItem(blockName, itemName);
                if (item != null && item.Enabled && item.Visible)
                {
                    return itemName;
                }
            }
            
            return tabOrder.FirstOrDefault();
        }
        
        /// <inheritdoc />
        public string GetLastItem(string blockName)
        {
            var tabOrder = GetTabOrder(blockName);
            
            for (int i = tabOrder.Count - 1; i >= 0; i--)
            {
                var item = GetItem(blockName, tabOrder[i]);
                if (item != null && item.Enabled && item.Visible)
                {
                    return tabOrder[i];
                }
            }
            
            return tabOrder.LastOrDefault();
        }
        
        #endregion
        
        #region Form Mode Support
        
        /// <inheritdoc />
        public IReadOnlyList<ItemInfo> GetEditableItems(string blockName, FormMode mode)
        {
            var items = GetAllItems(blockName);
            return items.Where(i => i.IsEditable(mode)).ToList().AsReadOnly();
        }
        
        /// <inheritdoc />
        public bool IsItemEditable(string blockName, string itemName, FormMode mode)
        {
            var item = GetItem(blockName, itemName);
            return item?.IsEditable(mode) ?? false;
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Convert value to target type
        /// </summary>
        private object ConvertValue(object value, Type targetType)
        {
            if (value == null)
            {
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
            }
            
            if (targetType.IsAssignableFrom(value.GetType()))
            {
                return value;
            }
            
            // Handle nullable types
            var underlyingType = Nullable.GetUnderlyingType(targetType);
            if (underlyingType != null)
            {
                targetType = underlyingType;
            }
            
            // Try Convert.ChangeType
            return Convert.ChangeType(value, targetType);
        }
        
        /// <summary>
        /// Raise ItemPropertyChanged event
        /// </summary>
        private void RaiseItemPropertyChanged(string blockName, string itemName, string propertyName, object oldValue, object newValue)
        {
            var handler = ItemPropertyChanged;
            if (handler != null)
            {
                var args = new ItemPropertyChangedEventArgs
                {
                    BlockName = blockName,
                    ItemName = itemName,
                    PropertyName = propertyName,
                    OldValue = oldValue,
                    NewValue = newValue,
                    Timestamp = DateTime.Now
                };
                
                handler.Invoke(this, args);
            }
        }
        
        /// <summary>
        /// Raise ItemValueChanged event
        /// </summary>
        private void RaiseItemValueChanged(string blockName, string itemName, object oldValue, object newValue, bool isDirty, int recordIndex)
        {
            var handler = ItemValueChanged;
            if (handler != null)
            {
                var args = new ItemValueChangedEventArgs
                {
                    BlockName = blockName,
                    ItemName = itemName,
                    OldValue = oldValue,
                    NewValue = newValue,
                    IsDirty = isDirty,
                    RecordIndex = recordIndex,
                    Timestamp = DateTime.Now
                };
                
                handler.Invoke(this, args);
            }
        }
        
        /// <summary>
        /// Raise ItemErrorChanged event
        /// </summary>
        private void RaiseItemErrorChanged(string blockName, string itemName, bool hasError, string errorMessage, string ruleName)
        {
            var handler = ItemErrorChanged;
            if (handler != null)
            {
                var args = new ItemErrorEventArgs
                {
                    BlockName = blockName,
                    ItemName = itemName,
                    HasError = hasError,
                    ErrorMessage = errorMessage,
                    ValidationRuleName = ruleName,
                    Timestamp = DateTime.Now
                };
                
                handler.Invoke(this, args);
            }
        }
        
        #endregion
        
        #region IDisposable
        
        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
                return;
            
            _blockItems.Clear();
            _tabOrders.Clear();
            
            ItemPropertyChanged = null;
            ItemValueChanged = null;
            ItemErrorChanged = null;
            
            _disposed = true;
        }
        
        #endregion
    }
}
