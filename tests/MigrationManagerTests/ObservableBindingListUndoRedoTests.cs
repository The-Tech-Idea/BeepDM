using System.Collections.Generic;
using System.ComponentModel;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Editor.Migration.Tests;

/// <summary>
/// Coverage for a fix (2026-08-26) to <c>ObservableBindingList&lt;T&gt;</c>'s Undo/Redo
/// path: <c>ApplyUndoAction</c>'s <c>PropertyChange</c> case set the reverted value via
/// raw reflection (<c>prop.SetValue</c>), which re-raises <c>Item_PropertyChanged</c> —
/// but that handler only ever transitions <c>Unchanged -&gt; Modified</c>, never the
/// reverse, so an Undo that brought every property back to its original snapshot left
/// <c>EntityState</c> stuck at <c>Modified</c> (and therefore <c>HasChanges</c>/a host's
/// <c>IsDirty</c> stuck <c>true</c>) even though <c>RejectChanges</c> would report the
/// same values as clean. Found via Beep.Forms' WinForms example app's own dirty-state +
/// Undo demonstration (<c>CallFormTimerUndoSelfTest.cs</c>), added there for parity with
/// the WPF example's identical demo — <c>UndoBlock</c> reverted the value correctly but
/// <c>IsBlockDirty</c> stayed <c>true</c> afterward.
/// <para>
/// <b>Deliberately not covered here: Redo after an Undo that fully reconciled an item
/// back to <c>Unchanged</c>.</b> That sequence hits a second, separate, pre-existing gap
/// — <c>Item_PropertyChanged</c>'s own <c>OriginalValues ??= SnapshotValues(item)</c>
/// (Phase 1B) snapshots lazily on the first change seen while <c>Unchanged</c>, and by
/// the time it runs the value has already been set, so re-mutating a just-cleaned item
/// (whether via Redo or any other path that reaches <c>Unchanged</c> mid-sequence, e.g.
/// <c>AcceptChanges</c>/<c>RejectChanges</c> followed by a further edit) snapshots the
/// <em>new</em> value as "original" rather than the true one — a defect in that lazy
/// snapshot timing itself, not something this Undo fix introduced or can fix in
/// isolation. Not attempted in this pass.
/// </para>
/// </summary>
public class ObservableBindingListUndoRedoTests
{
    private sealed class Row : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private int _id;
        public int Id
        {
            get => _id;
            set { _id = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Id))); }
        }

        private string _name;
        public string Name
        {
            get => _name;
            set { _name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name))); }
        }
    }

    [Fact]
    public void Undo_RevertingAPropertyToItsOriginalValue_ClearsEntityStateBackToUnchanged()
    {
        var row = new Row { Id = 1, Name = "Original" };
        var list = new ObservableBindingList<Row>(new List<Row> { row });
        list.IsUndoEnabled = true;

        row.Name = "Edited";
        var trackingAfterEdit = list.GetTrackingItem(row);
        Assert.Equal(EntityState.Modified, trackingAfterEdit.EntityState);
        Assert.True(list.HasChanges);

        Assert.True(list.Undo());

        Assert.Equal("Original", row.Name);
        var trackingAfterUndo = list.GetTrackingItem(row);
        Assert.Equal(EntityState.Unchanged, trackingAfterUndo.EntityState);
        Assert.False(list.HasChanges);
    }

    [Fact]
    public void Undo_OnlyOneOfTwoEditedProperties_LeavesEntityStateModified()
    {
        var row = new Row { Id = 1, Name = "Original" };
        var list = new ObservableBindingList<Row>(new List<Row> { row });
        list.IsUndoEnabled = true;

        row.Name = "Edited once";
        row.Id = 2;

        // Undo reverts the most recent action (Id: 2 -> 1); the Name edit stands.
        Assert.True(list.Undo());

        Assert.Equal(1, row.Id);
        Assert.Equal("Edited once", row.Name);
        var tracking = list.GetTrackingItem(row);
        Assert.Equal(EntityState.Modified, tracking.EntityState);
        Assert.True(list.HasChanges);
    }
}
