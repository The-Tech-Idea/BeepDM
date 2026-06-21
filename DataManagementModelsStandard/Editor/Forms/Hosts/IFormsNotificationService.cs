using TheTechIdea.Beep.Editor.Forms.Models;

namespace TheTechIdea.Beep.Editor.Forms.Hosts;

/// <summary>
/// Message notification service shared by WPF and WinForms.
/// Publishes messages into a <see cref="BeepViewState"/> for UI consumption.
/// </summary>
public interface IFormsNotificationService
{
    string CurrentMessage { get; }
    BeepMessageSeverity CurrentSeverity { get; }
    void Publish(BeepViewState viewState, string message, BeepMessageSeverity severity = BeepMessageSeverity.Info);
    void Clear(BeepViewState viewState);
}
