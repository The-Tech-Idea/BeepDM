using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Editor.UOWManager.Models;

namespace TheTechIdea.Beep.Editor.UOWManager.Interfaces;

public sealed class ItemSecurityProjection
{
    public string Name { get; }
    public ItemInfo Item { get; }
    public bool Enabled { get; }
    public bool Visible { get; }
    public ItemSecurityProjection(string name, ItemInfo item, bool enabled, bool visible)
    { Name = name; Item = item; Enabled = enabled; Visible = visible; }
}

/// <summary>Optional registry-gated projection. Authorizer invokes the pure owned-memory action synchronously, once.</summary>
public interface IItemSecurityProjection
{
    IReadOnlyList<Exception> PublishSecurityFlags(string blockName, Guid owner, long revision,
        IReadOnlyList<ItemSecurityProjection> flags, Action<Action> authorizePublication, Func<bool> canNotify);
}
