#nullable enable
using System;

namespace TheTechIdea.Beep.Editor.Forms.Models;

/// <summary>Immutable public identity; private captured evidence belongs to the issuing manager.</summary>
public sealed class FormBindingTarget
{
    internal FormBindingTarget(object owner, object evidence, Guid form, Guid registration, string block, object? record, long? securityRevision)
    { Owner = owner; Evidence = evidence; FormInstanceId = form; RegistrationId = registration; BlockName = block; Record = record; SecurityRevision = securityRevision; }
    internal object Owner { get; }
    internal object Evidence { get; }
    internal long? SecurityRevision { get; }
    public Guid FormInstanceId { get; }
    public Guid RegistrationId { get; }
    public string BlockName { get; }
    public object? Record { get; }
}

public enum FormViewDeliveryState { Delivered, Cancelled, Superseded, Rejected, Failed }

/// <summary>UI/edit acknowledgement, not database durability or atomic multi-field delivery.</summary>
public sealed class FormViewDeliveryResult
{
    public FormViewDeliveryState State { get; internal set; }
    public bool EffectsPossible { get; internal set; }
    public bool Acknowledged { get; internal set; }
    public Exception? Exception { get; internal set; }
}
