using System;

namespace TheTechIdea.Beep.Editor.Forms.Hosts;

/// <summary>
/// Block navigation bar contract shared by WPF and WinForms.
/// Platform-specific implementations render the visual control.
/// </summary>
public interface IBlockNavigationBar
{
    /// <summary>
    /// The block this navigation bar controls, e.g. "Ord". A host with more
    /// than one block cannot correlate a discovered bar to the right block
    /// without this — see the WinForms/WPF hosts' auto-discovery.
    /// </summary>
    string? BlockName { get; set; }

    int CurrentRecordIndex { get; set; }
    int RecordCount { get; set; }
    bool IsQueryMode { get; set; }

    event EventHandler FirstClicked;
    event EventHandler PreviousClicked;
    event EventHandler NextClicked;
    event EventHandler LastClicked;
    event EventHandler<int> RecordIndexChanged;

    /// <summary>CREATE_RECORD (F6) — a new, blank record ready for input.</summary>
    event EventHandler NewRecordClicked;
    /// <summary>DELETE_RECORD — removes the current record.</summary>
    event EventHandler DeleteClicked;
    /// <summary>ENTER_QUERY (F7) — the block starts accepting example criteria.</summary>
    event EventHandler QueryClicked;
    /// <summary>EXECUTE_QUERY (F8) — runs the query (by-example if in query mode, a plain re-query otherwise).</summary>
    event EventHandler ExecuteClicked;
    /// <summary>COMMIT_FORM (F10) — posts pending changes.</summary>
    event EventHandler SaveClicked;
    /// <summary>Discards pending changes since the last commit/query.</summary>
    event EventHandler RollbackClicked;

    /// <summary>Platform-specific visual element (UIElement in WPF, Control in WinForms).</summary>
    object? View { get; }

    void Refresh();
}
