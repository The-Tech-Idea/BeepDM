#nullable enable
using System.Diagnostics.CodeAnalysis;
using TheTechIdea.Beep.Editor.Forms.Models;

namespace TheTechIdea.Beep.Editor.Forms.Hosts;

/// <summary>Optional manager capability for generation-bound adapter work; not a replacement host.</summary>
public interface IFormsBindingTargets
{
    bool TryCaptureBindingTarget(string blockName, [MaybeNullWhen(false)] out FormBindingTarget target);
    bool IsBindingTargetCurrent(FormBindingTarget? target, bool includeRecord = true);
    FormViewDeliveryResult ApplyBindingValue(FormBindingTarget? target, string fieldName, object? value);
}
