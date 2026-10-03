using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;

namespace TheTechIdea.Beep.Editor.Forms.Helpers
{
    public partial class ItemPropertyManager
    {
        private readonly object _registrationGate = new();
        private readonly Dictionary<string, Guid> _registrationOwners = new(StringComparer.OrdinalIgnoreCase);

        public IBlockItemsRegistration PrepareBlockItems(Guid formInstanceId, string blockName, IEntityStructure structure)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (string.IsNullOrWhiteSpace(blockName)) throw new ArgumentNullException(nameof(blockName));
            if (structure == null) throw new ArgumentNullException(nameof(structure));
            var items = new ConcurrentDictionary<string, ItemInfo>(StringComparer.OrdinalIgnoreCase);
            var order = new List<string>();
            foreach (var field in structure.Fields.ToArray())
            {
                if (string.IsNullOrWhiteSpace(field.FieldName) || items.ContainsKey(field.FieldName))
                    throw new InvalidOperationException("Item metadata contains an empty or duplicate field name.");
                items[field.FieldName] = new ItemInfo
                {
                    BlockName = blockName, ItemName = field.FieldName, BoundProperty = field.FieldName,
                    DataType = field.Fieldtype != null ? Type.GetType(field.Fieldtype) ?? typeof(object) : typeof(object),
                    DatabaseTypeName = field.Fieldtype, MaxLength = Math.Max(0, field.Size1),
                    Precision = field.NumericPrecision, Scale = field.NumericScale,
                    AllowNull = field.AllowDBNull, Required = !field.AllowDBNull,
                    PromptText = field.FieldName, TabIndex = order.Count,
                    QueryAllowed = true, InsertAllowed = !field.IsAutoIncrement,
                    UpdateAllowed = !field.IsAutoIncrement && !field.IsKey, Enabled = true, Visible = true
                };
                order.Add(field.FieldName);
            }
            return new PreparedItems(this, formInstanceId, blockName, items, order);
        }

        private sealed class PreparedItems : IBlockItemsRegistration
        {
            private readonly ItemPropertyManager _owner;
            private readonly Guid _form;
            private readonly string _name;
            private readonly ConcurrentDictionary<string, ItemInfo> _items;
            private readonly List<string> _order;
            private bool _disposed, _committed;

            internal PreparedItems(ItemPropertyManager owner, Guid form, string name,
                ConcurrentDictionary<string, ItemInfo> items, List<string> order)
            { _owner = owner; _form = form; _name = name; _items = items; _order = order; }

            public void Commit(Action publishRegistration)
            {
                if (publishRegistration == null) throw new ArgumentNullException(nameof(publishRegistration));
                lock (_owner._registrationGate)
                {
                    ObjectDisposedException.ThrowIf(_disposed || _owner._disposed, this);
                    if (_committed) throw new InvalidOperationException("Item registration already committed.");
                    if (_owner._registrationOwners.TryGetValue(_name, out var existingOwner) && existingOwner != _form)
                        throw new InvalidOperationException("A shared item store cannot bind the same block name to different forms.");
                    var hadItems = _owner._blockItems.TryGetValue(_name, out var previousItems);
                    var hadOrder = _owner._tabOrders.TryGetValue(_name, out var previousOrder);
                    var hadOwner = _owner._registrationOwners.TryGetValue(_name, out var previousOwner);
                    _owner._blockItems[_name] = _items;
                    _owner._tabOrders[_name] = _order;
                    _owner._registrationOwners[_name] = _form;
                    try
                    {
                        publishRegistration();
                        _committed = true;
                    }
                    catch
                    {
                        if (hadItems) _owner._blockItems[_name] = previousItems;
                        else _owner._blockItems.TryRemove(_name, out _);
                        if (hadOrder) _owner._tabOrders[_name] = previousOrder;
                        else _owner._tabOrders.TryRemove(_name, out _);
                        if (hadOwner) _owner._registrationOwners[_name] = previousOwner;
                        else _owner._registrationOwners.Remove(_name);
                        throw;
                    }
                }
            }

            public void Dispose()
            {
                lock (_owner._registrationGate)
                {
                    if (_disposed) return;
                    _disposed = true;
                    if (_committed && _owner._blockItems.TryGetValue(_name, out var current) && ReferenceEquals(current, _items))
                    {
                        _owner._blockItems.TryRemove(_name, out _);
                        _owner._tabOrders.TryRemove(_name, out _);
                        _owner._registrationOwners.Remove(_name);
                    }
                }
            }
        }
    }
}
