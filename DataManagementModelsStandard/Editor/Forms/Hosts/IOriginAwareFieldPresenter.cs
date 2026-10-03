#nullable enable
using System;

namespace TheTechIdea.Beep.Editor.Forms.Hosts;

/// <summary>Optional origin/revision protocol for presenters which defer ValueChanged delivery.</summary>
public interface IOriginAwareFieldPresenter : IFieldPresenter
{
    /// <summary>User edits use Guid.Empty/zero; programmatic changes preserve the supplied stamp.</summary>
    event EventHandler<FormFieldValueChangedEventArgs> ValueChangedWithOrigin;
    void SetValue(object? value, Guid origin, long revision);
}

public sealed class FormFieldValueChangedEventArgs(object? value, Guid origin, long revision) : EventArgs
{
    public object? Value { get; } = value;
    public Guid Origin { get; } = origin;
    public long Revision { get; } = revision;
}
