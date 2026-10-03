using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;

namespace TheTechIdea.Beep.Editor.Forms.Helpers;

public partial class ItemPropertyManager : IItemSecurityProjection
{
    public IReadOnlyList<Exception> PublishSecurityFlags(string blockName, Guid owner, long revision,
        IReadOnlyList<ItemSecurityProjection> flags, Action<Action> authorizePublication, Func<bool> canNotify)
    {
        ArgumentNullException.ThrowIfNull(flags);
        ArgumentNullException.ThrowIfNull(authorizePublication);
        ArgumentNullException.ThrowIfNull(canNotify);
        var captured = flags.ToArray();
        var changes = new List<ItemPropertyChangedEventArgs>();
        var published = false;
        var attempted = false;
        lock (_registrationGate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (captured.Select(f => f?.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != captured.Length ||
                !_blockItems.TryGetValue(blockName, out var items) || items.Count != captured.Length ||
                captured.Any(f => f?.Item == null || !items.TryGetValue(f.Name, out var item) || !ReferenceEquals(item, f.Item)))
                throw new InvalidOperationException("Item security projection lost its captured registry.");
            var window = true;
            var thread = Environment.CurrentManagedThreadId;
            try
            {
                authorizePublication(() =>
                {
                    if (!window || attempted || thread != Environment.CurrentManagedThreadId)
                        throw new InvalidOperationException("Item security action is no longer available.");
                    attempted = true;
                    foreach (var flag in captured)
                    {
                        var enabled = flag.Item.Enabled; var visible = flag.Item.Visible;
                        flag.Item.PublishSecurityPermissions(owner, revision, flag.Enabled, flag.Visible);
                        if (enabled != flag.Item.Enabled) changes.Add(Change(flag.Name, nameof(ItemInfo.Enabled), enabled, flag.Item.Enabled));
                        if (visible != flag.Item.Visible) changes.Add(Change(flag.Name, nameof(ItemInfo.Visible), visible, flag.Item.Visible));
                    }
                    published = true;
                });
            }
            finally { window = false; }
        }
        if (!published) throw new InvalidOperationException("Item security projection was not authorized.");
        var failures = new List<Exception>();
        foreach (var change in changes)
            foreach (EventHandler<ItemPropertyChangedEventArgs> observer in ItemPropertyChanged?.GetInvocationList() ?? Array.Empty<Delegate>())
            {
                lock (_registrationGate)
                    if (_disposed || !_blockItems.TryGetValue(blockName, out var current) ||
                        current.Count != captured.Length || captured.Any(f =>
                            !current.TryGetValue(f.Name, out var item) || !ReferenceEquals(item, f.Item))) return failures;
                if (!canNotify()) return failures;
                try { observer(this, change); } catch (Exception ex) { failures.Add(ex); }
            }
        return failures;

        ItemPropertyChangedEventArgs Change(string name, string property, bool prior, bool next) => new()
        { BlockName = blockName, ItemName = name, PropertyName = property, OldValue = prior, NewValue = next };
    }
}
