using System;
using TheTechIdea.Beep.Editor.Forms.Models;

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

    /// <summary>
    /// Applies the form author's per-command overrides from
    /// <see cref="BlockDefinition.Navigation"/>. A null <paramref name="navigation"/>,
    /// or an individual <see cref="BlockNavigationCommand"/> left null on it, means
    /// "not authored" — <see cref="BlockNavigationDefinition.Clone"/>'s own contract —
    /// and that command's state stays exactly what <see cref="Refresh"/> already
    /// computes from live engine state.
    /// </summary>
    /// <remarks>
    /// Authoring can hide a command (<c>Visible = false</c>) or narrow it off
    /// (<c>Enabled = false</c> combines with live state by AND), but never force
    /// one <em>on</em> that live state says is invalid — e.g. an author cannot make
    /// First enabled while already on the first record. Implementations must persist
    /// the passed definition and re-consult it on every subsequent <see cref="Refresh"/>,
    /// not apply it once: this is typically called before the block's first bind, and
    /// <see cref="Refresh"/> runs many times afterward as the engine's state changes.
    /// </remarks>
    void ApplyAuthoredNavigation(BlockNavigationDefinition? navigation);
}
