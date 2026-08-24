using System.Collections.Generic;
using System.ComponentModel;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Editor.Migration.Tests;

/// <summary>
/// Coverage for a pending, uncommitted fix found sitting in the working tree (2026-08-24):
/// <c>ObservableBindingList&lt;T&gt;.SetItem</c> replacing an already-tracked row left its
/// <c>EntityState</c> at <c>Unchanged</c> instead of promoting it to <c>Modified</c> — since
/// <c>CommitAllAsync</c> routes strictly by <c>EntityState</c>, a caller doing
/// <c>list[i] = updatedItem</c> (the path <c>UnitofWork.Update</c> uses after a fresh
/// <c>Get()</c>) had its edit silently skipped: <c>Commit()</c> reported success while writing
/// nothing. The fix only promotes <c>Unchanged → Modified</c>; a row that is already
/// <c>Added</c> (still needs an INSERT, not an UPDATE) or <c>Deleted</c>/<c>Detached</c> (must
/// not be silently resurrected by a plain replace) is left alone.
/// </summary>
public class ObservableBindingListEntityStateTests
{
    private sealed class Row : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public int Id { get; set; }
        public string Name { get; set; }
    }

    [Fact]
    public void SetItem_ReplacingAnUnchangedLoadedRow_PromotesItToModified()
    {
        var list = new ObservableBindingList<Row>(new List<Row> { new() { Id = 1, Name = "Original" } });
        var trackingBefore = list.GetTrackingItem(list[0]);
        Assert.Equal(EntityState.Unchanged, trackingBefore.EntityState);

        list[0] = new Row { Id = 1, Name = "Changed" };

        var trackingAfter = list.GetTrackingItem(list[0]);
        Assert.NotNull(trackingAfter);
        Assert.Equal(EntityState.Modified, trackingAfter.EntityState);
    }

    [Fact]
    public void SetItem_ReplacingANewlyAddedRow_LeavesItAddedRatherThanDowngradingToModified()
    {
        var list = new ObservableBindingList<Row>();
        list.Add(new Row { Id = 2, Name = "Brand new" });
        var trackingBefore = list.GetTrackingItem(list[0]);
        Assert.Equal(EntityState.Added, trackingBefore.EntityState);

        list[0] = new Row { Id = 2, Name = "Still new, but edited before its first save" };

        var trackingAfter = list.GetTrackingItem(list[0]);
        Assert.NotNull(trackingAfter);
        // Must still be Added, not Modified — CommitAllAsync must INSERT it, not UPDATE it.
        Assert.Equal(EntityState.Added, trackingAfter.EntityState);
    }
}
