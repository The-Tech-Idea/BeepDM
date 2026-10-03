using Moq;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.Forms.Helpers;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOW;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Utilities;
using Xunit;

namespace TheTechIdea.Beep.Editor.UOWManager.Tests;

public class PagingTests
{
    public sealed class Row : Entity { public int Id { get; set; } }
    public sealed class IgnoredCursor
    {
        public int Count => 5;
        public int CurrentIndex { get => 0; set { } }
        public object Current => new Row();
    }
    public sealed class UnreadableCount { public int Count => throw new InvalidOperationException("count"); }
    private sealed class Harness : IDisposable
    {
        internal readonly Mock<IDMEEditor> Editor = new();
        internal readonly Mock<IDataSource> Source = new();
        internal readonly Mock<ITriggerManager> Triggers = new();
        internal readonly Mock<ISystemVariablesManager> Variables = new();
        internal readonly EntityStructure Schema = new() { EntityName = "Rows", Fields = new() { new() { FieldName = "Id", Fieldtype = "System.Int32", IsKey = true } } };
        internal readonly ObservableBindingList<Row> Rows;
        internal readonly UnitofWork<Row> Unit;
        internal readonly UnitOfWorkWrapper Wrapper;
        internal readonly FormsManager Manager;
        internal Harness(int count = 7, IPagingManager? paging = null)
        {
            Rows = new(Enumerable.Range(1, count).Select(i => new Row { Id = i }).ToList());
            Unit = new(Editor.Object, "db", "Rows", Schema, "Id") { Units = Rows, DataSource = Source.Object };
            if (count > 0) Unit.MoveFirst();
            Wrapper = new(Unit);
            Triggers.Setup(t => t.FireBlockTriggerAsync(It.IsAny<TriggerType>(), It.IsAny<string>(),
                It.IsAny<TriggerContext>(), It.IsAny<CancellationToken>())).ReturnsAsync(TriggerResult.Success);
            Manager = new(Editor.Object, triggerManager: Triggers.Object, systemVariablesManager: Variables.Object, pagingManager: paging);
            Manager.RegisterBlock("ROWS", Wrapper, Schema, "db");
            Manager.Configuration.Navigation.ValidateBeforeNavigation = false;
            Manager.GetBlock("ROWS").Mode = DataBlockMode.CRUD;
            Manager.SetBlockPageSize("ROWS", 2);
        }
        public void Dispose() { Manager.Dispose(); Wrapper.Dispose(); Unit.Dispose(); Rows.Dispose(); }
    }

    [Theory]
    [InlineData(2, 2)]
    [InlineData(99, 6)]
    public async Task LocalPagingUsesLoadedCountAndNeverFetchesRemoteRows(int requested, int index)
    {
        using var h = new Harness();
        h.Manager.SetTotalRecordCount("ROWS", 1_000_000);
        var outcome = await h.Manager.LoadLocalPageWithOutcomeAsync("rows", requested);
        Assert.Equal(LocalPageState.Completed, outcome.State); Assert.True(outcome.PageStatePublished);
        Assert.True(outcome.CursorNavigationAcknowledged); Assert.Equal(index, h.Rows.CurrentIndex);
        Assert.Equal(7, outcome.Page.TotalRecords); Assert.Equal(index / 2 + 1, outcome.Page.PageNumber);
        Assert.Equal(outcome.Page.PageNumber, h.Manager.GetBlock("ROWS").CurrentPage);
        Assert.Equal(7, h.Manager.GetTotalRecordCount("ROWS"));
        Assert.Empty(h.Source.Invocations);
    }

    [Fact]
    public async Task EmptyBufferHasZeroPagesAndNoNavigationAttempt()
    {
        using var h = new Harness(0);
        var result = await h.Manager.LoadLocalPageWithOutcomeAsync("ROWS", 99);
        Assert.True(result.PageStatePublished); Assert.False(result.CursorNavigationAcknowledged);
        Assert.False(result.NavigationEffectsPossible); Assert.Equal(0, result.Page.TotalPagesLong);
        Assert.Equal(1, result.Page.PageNumber);
        h.Triggers.Verify(t => t.FireBlockTriggerAsync(TriggerType.PostRecord, It.IsAny<string>(),
            It.IsAny<TriggerContext>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("cancel event")]
    [InlineData("cancel token")]
    [InlineData("cancel trigger")]
    [InlineData("throw trigger")]
    public async Task RejectedOrCancelledNavigationPreservesAcceptedPageAndCursor(string route)
    {
        using var h = new Harness(); using var cancellation = new CancellationTokenSource();
        if (route == "cancel event") h.Manager.OnNavigate += (_, e) => e.Cancel = true;
        if (route == "cancel token") h.Manager.OnNavigate += (_, _) => cancellation.Cancel();
        if (route == "cancel trigger") h.Triggers.Setup(t => t.FireBlockTriggerAsync(TriggerType.PostRecord, "ROWS",
            It.IsAny<TriggerContext>(), It.IsAny<CancellationToken>())).ReturnsAsync(TriggerResult.Cancelled);
        if (route == "throw trigger") h.Triggers.Setup(t => t.FireBlockTriggerAsync(TriggerType.PostRecord, "ROWS",
            It.IsAny<TriggerContext>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("trigger"));
        var result = await h.Manager.LoadLocalPageWithOutcomeAsync("ROWS", 2, cancellation.Token);
        Assert.False(result.PageStatePublished); Assert.False(result.NavigationEffectsPossible);
        Assert.Equal(0, h.Rows.CurrentIndex); Assert.Equal(1, h.Manager.GetBlock("ROWS").CurrentPage);
        Assert.Equal(1, h.Manager.Paging.GetCurrentPage("ROWS").PageNumber);
    }

    [Fact]
    public async Task CancellationInsideCursorCallbackDoesNotInventPageAcknowledgement()
    {
        using var h = new Harness(); using var cancellation = new CancellationTokenSource();
        h.Unit.CurrentChanged += (_, _) => cancellation.Cancel();
        var result = await h.Manager.LoadLocalPageWithOutcomeAsync("ROWS", 2, cancellation.Token);
        Assert.Equal(LocalPageState.Cancelled, result.State); Assert.False(result.PageStatePublished);
        Assert.True(result.NavigationEffectsPossible); Assert.Equal(2, h.Rows.CurrentIndex);
        Assert.Equal(1, h.Manager.GetBlock("ROWS").CurrentPage);
    }

    [Fact]
    public async Task PostAcknowledgementObserverFailureRetainsAcceptedPageEvidence()
    {
        using var h = new Harness();
        var failure = new InvalidOperationException("observer");
        h.Manager.OnCurrentChanged += (_, _) => throw failure;
        var result = await h.Manager.LoadLocalPageWithOutcomeAsync("ROWS", 2);
        Assert.Equal(LocalPageState.Completed, result.State); Assert.True(result.PageStatePublished);
        Assert.True(result.CursorNavigationAcknowledged); Assert.Contains(failure, result.NotificationFailures);
        Assert.Equal(2, h.Manager.GetBlock("ROWS").CurrentPage);
    }

    [Theory]
    [InlineData("size")]
    [InlineData("count")]
    [InlineData("reset")]
    [InlineData("replacement")]
    [InlineData("buffer ABA")]
    [InlineData("cursor ABA")]
    [InlineData("policy")]
    public async Task ChangedTargetsRejectBeforeCursorMovement(string route)
    {
        using var h = new Harness();
        h.Manager.OnNavigate += (_, _) =>
        {
            if (route == "size") h.Manager.SetBlockPageSize("ROWS", 3);
            if (route == "count") h.Manager.Paging.SetTotalRecordCount("ROWS", 3);
            if (route == "reset") h.Manager.Paging.ResetPaging("ROWS");
            if (route == "replacement") h.Manager.RegisterBlock("ROWS", h.Wrapper, h.Schema, "db");
            if (route == "buffer ABA") { h.Unit.Units = new(new List<Row>()); h.Unit.Units = h.Rows; }
            if (route == "cursor ABA") { h.Rows.MoveTo(1); h.Rows.MoveTo(0); }
            if (route == "policy") h.Manager.SetSecurityContext(new SecurityContext { UserName = "next" });
        };
        var result = await h.Manager.LoadLocalPageWithOutcomeAsync("ROWS", 2);
        Assert.False(result.PageStatePublished); Assert.False(result.NavigationEffectsPossible);
        Assert.Equal(0, h.Rows.CurrentIndex); Assert.Equal(1, h.Manager.GetBlock("ROWS").CurrentPage);
    }

    [Fact]
    public async Task SlowRequestIsSupersededByNewestRequestWithoutOldCursorMutation()
    {
        using var h = new Harness();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        h.Triggers.Setup(t => t.FireBlockTriggerAsync(TriggerType.PostRecord, "ROWS", It.IsAny<TriggerContext>(), It.IsAny<CancellationToken>()))
            .Returns(async () => { if (++calls == 1) { entered.SetResult(); await release.Task; } return TriggerResult.Success; });
        var first = h.Manager.LoadLocalPageWithOutcomeAsync("ROWS", 2);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var newest = h.Manager.LoadLocalPageWithOutcomeAsync("ROWS", 3);
        release.SetResult();
        Assert.Equal(LocalPageState.Superseded, (await first).State);
        var accepted = await newest;
        Assert.True(accepted.PageStatePublished); Assert.Equal(4, h.Rows.CurrentIndex);
        Assert.Equal(3, h.Manager.GetBlock("ROWS").CurrentPage);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancellationAndCloseDrainBlockedPhysicalTrigger(bool close)
    {
        using var h = new Harness(); using var cancellation = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        h.Triggers.Setup(t => t.FireBlockTriggerAsync(TriggerType.PostRecord, "ROWS", It.IsAny<TriggerContext>(), It.IsAny<CancellationToken>()))
            .Returns(async () => { entered.SetResult(); await release.Task; return TriggerResult.Success; });
        var pending = h.Manager.LoadLocalPageWithOutcomeAsync("ROWS", 2, cancellation.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        if (close) h.Manager.Dispose(); else cancellation.Cancel();
        var drain = h.Manager.WaitForPendingCallbacksAsync();
        Assert.False(pending.IsCompleted); Assert.False(drain.IsCompleted);
        release.SetResult();
        Assert.Equal(LocalPageState.Cancelled, (await pending).State);
        await drain.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(0, h.Rows.CurrentIndex);
    }

    [Fact]
    public async Task IgnoredCursorSetterCannotBeReportedAsALoadedPage()
    {
        using var h = new Harness();
        var unit = new Mock<IUnitofWork>(); var collection = new IgnoredCursor();
        unit.SetupGet(u => u.Units).Returns(collection); unit.SetupGet(u => u.TotalItemCount).Returns(5);
        h.Manager.RegisterBlock("ROWS", unit.Object, h.Schema, "db");
        var result = await h.Manager.LoadLocalPageWithOutcomeAsync("ROWS", 2);
        Assert.False(result.PageStatePublished); Assert.True(result.NavigationEffectsPossible);
        Assert.Equal(1, h.Manager.GetBlock("ROWS").CurrentPage);
        Assert.Null(await h.Manager.LoadPageAsync("ROWS", 2));
    }

    [Fact]
    public async Task VirtualBuffersAndLegacyPagingHelpersAreNotProviderPagingCapabilities()
    {
        using var virtualBuffer = new Harness();
        virtualBuffer.Rows.SetDataProvider((_, _) => Task.FromResult(new List<Row>()));
        Assert.Equal(LocalPageState.Unsupported, (await virtualBuffer.Manager.LoadLocalPageWithOutcomeAsync("ROWS", 2)).State);
        using var legacy = new Harness(paging: new Mock<IPagingManager>().Object);
        Assert.Equal(LocalPageState.Unsupported, (await legacy.Manager.LoadLocalPageWithOutcomeAsync("ROWS", 2)).State);
    }

    [Fact]
    public async Task ZeroSizeDisablesPagingAndStoredZeroIsNotAnUnknownCount()
    {
        using var h = new Harness();
        Assert.Equal(7, h.Manager.GetTotalRecordCount("ROWS"));
        h.Manager.SetTotalRecordCount("ROWS", 0);
        Assert.Equal(0, h.Manager.GetTotalRecordCount("rows"));
        h.Manager.SetBlockPageSize("ROWS", 0);
        Assert.Equal(0, h.Manager.Paging.GetPageSize("rows"));
        Assert.Equal(LocalPageState.Unsupported, (await h.Manager.LoadLocalPageWithOutcomeAsync("ROWS", 2)).State);
        Assert.Equal(0, h.Rows.CurrentIndex);
    }

    [Fact]
    public void ExactLongPageMathAndCheckedLegacyPropertiesDoNotWrap()
    {
        var page = new PageInfo { TotalRecords = long.MaxValue, PageSize = 3, PageNumber = int.MaxValue };
        Assert.Equal(long.MaxValue / 3 + 1, page.TotalPagesLong);
        Assert.Throws<OverflowException>(() => page.TotalPages);
        Assert.Equal(((long)int.MaxValue - 1) * 3, page.SkipLong);
        Assert.Throws<OverflowException>(() => page.Skip);
        Assert.True(page.HasNext);
    }

    [Fact]
    public void CountShrinkClampsAndFailedPublicationConsumesItsProposal()
    {
        var paging = new PagingManager();
        paging.SetPageSize("ROWS", 2); paging.SetTotalRecordCount("ROWS", 7); paging.SetCurrentPage("ROWS", 4);
        paging.SetTotalRecordCount("rows", 3);
        Assert.Equal(2, paging.GetCurrentPage("ROWS").PageNumber);
        var plan = paging.PrepareLocalPage("ROWS", 2, 3);
        Assert.Throws<InvalidOperationException>(() => paging.TryPublishLocalPage(plan, () => throw new InvalidOperationException("gate")));
        Assert.False(paging.TryPublishLocalPage(plan, () => throw new Exception("must not execute")));
        Assert.Equal(2, paging.GetCurrentPage("ROWS").PageNumber);
    }

    [Fact]
    public async Task UnreadableCollectionCountCannotBeCertifiedAsAnEmptyPage()
    {
        using var h = new Harness(); var unit = new Mock<IUnitofWork>();
        unit.SetupGet(u => u.Units).Returns(new UnreadableCount()); unit.SetupGet(u => u.TotalItemCount).Returns(0);
        h.Manager.RegisterBlock("ROWS", unit.Object, h.Schema, "db");
        var result = await h.Manager.LoadLocalPageWithOutcomeAsync("ROWS", 1);
        Assert.False(result.PageStatePublished); Assert.Equal(LocalPageState.Failed, result.State);
    }

    [Fact]
    public async Task AcceptedQueryCountShrinkIsClampedByTheNextLocalPage()
    {
        using var h = new Harness();
        Assert.True((await h.Manager.LoadLocalPageWithOutcomeAsync("ROWS", 4)).PageStatePublished);
        h.Source.Setup(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<TheTechIdea.Beep.Report.AppFilter>>()))
            .ReturnsAsync(new object[] { new Row { Id = 1 }, new Row { Id = 2 }, new Row { Id = 3 } });
        Assert.True((await h.Manager.ExecuteQueryWithOutcomeAsync("ROWS")).RecordsPublished);
        h.Source.Invocations.Clear();
        var result = await h.Manager.LoadLocalPageWithOutcomeAsync("ROWS", 99);
        Assert.True(result.PageStatePublished); Assert.Equal(2, result.Page.PageNumber);
        Assert.Equal(3, result.Page.TotalRecords); Assert.Empty(h.Source.Invocations);
    }

    [Fact]
    public async Task NestedPagingRejectsInsteadOfWaitingOnItsOwnGate()
    {
        using var h = new Harness();
        h.Triggers.Setup(t => t.FireBlockTriggerAsync(TriggerType.PostRecord, "ROWS", It.IsAny<TriggerContext>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() => h.Manager.LoadLocalPageWithOutcomeAsync("ROWS", 3));
                return TriggerResult.Success;
            });
        Assert.True((await h.Manager.LoadLocalPageWithOutcomeAsync("ROWS", 2)).PageStatePublished);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task InvalidPageRequestsDoNotChangeState(int page)
    {
        using var h = new Harness();
        Assert.Equal(LocalPageState.InvalidRequest, (await h.Manager.LoadLocalPageWithOutcomeAsync("ROWS", page)).State);
        Assert.Equal(0, h.Rows.CurrentIndex); Assert.Equal(1, h.Manager.GetBlock("ROWS").CurrentPage);
    }

    [Fact]
    public async Task PreCancelledAndClosedRequestsRejectWithoutPageMutation()
    {
        using var h = new Harness(); using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => h.Manager.LoadPageAsync("ROWS", 2, cancellation.Token));
        h.Manager.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => h.Manager.LoadLocalPageWithOutcomeAsync("ROWS", 2));
        Assert.Equal(0, h.Rows.CurrentIndex);
    }
}
