using System;

namespace TheTechIdea.Beep.Editor.Forms.Hosts;

/// <summary>
/// Block navigation bar contract shared by WPF and WinForms.
/// Platform-specific implementations render the visual control.
/// </summary>
public interface IBlockNavigationBar
{
    int CurrentRecordIndex { get; set; }
    int RecordCount { get; set; }
    bool IsQueryMode { get; set; }

    event EventHandler FirstClicked;
    event EventHandler PreviousClicked;
    event EventHandler NextClicked;
    event EventHandler LastClicked;
    event EventHandler<int> RecordIndexChanged;

    /// <summary>Platform-specific visual element (UIElement in WPF, Control in WinForms).</summary>
    object? View { get; }

    void Refresh();
}
