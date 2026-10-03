using System.Globalization;
using Moq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Report;
using Xunit;

namespace TheTechIdea.Beep.Editor.UOWManager.Tests;

public class DetailCoordinationTests
{
    public sealed class Row { public int Id { get; set; } = 1; public decimal Part { get; set; } = 2.5m; }

    private sealed class Harness : IDisposable
    {
        internal readonly Mock<IDMEEditor> Editor = new();
        internal readonly Mock<ITriggerManager> Triggers = new();
        internal readonly FormsManager Manager;
        internal readonly Mock<IUnitofWork> Master;
        internal object? Current = new Row();
        internal Harness()
        {
            Triggers.Setup(t => t.FireBlockTriggerAsync(It.IsAny<TriggerType>(), It.IsAny<string>(),
                It.IsAny<TriggerContext>(), It.IsAny<CancellationToken>())).ReturnsAsync(TriggerResult.Success);
            Manager = new FormsManager(Editor.Object, triggerManager: Triggers.Object);
            Master = Unit();
            Master.SetupGet(u => u.CurrentItem).Returns(() => Current!);
            Manager.RegisterBlock("MASTER", Master.Object, Schema());
        }
        internal static EntityStructure Schema() => new()
        {
            EntityName = "Rows", Fields = new List<EntityField>
            {
                new() { FieldName = "Id", Fieldtype = "System.Int32" },
                new() { FieldName = "ParentId", Fieldtype = "System.Int32" },
                new() { FieldName = "Part", Fieldtype = "System.Decimal" }
            }
        };
        internal static Mock<IUnitofWork> Unit()
        {
            var unit = new Mock<IUnitofWork>();
            unit.SetupProperty(u => u.EntityStructure);
            unit.SetupProperty(u => u.DataSource);
            unit.Setup(u => u.Get(It.IsAny<List<AppFilter>>())).ReturnsAsync((object)new List<object>());
            return unit;
        }
        internal Mock<IUnitofWork> Add(string name = "DETAIL", string parent = "MASTER", bool deferred = false)
        {
            var unit = Unit();
            Manager.RegisterBlock(name, unit.Object, Schema());
            Manager.CreateMasterDetailRelation(parent, name, "Id", "ParentId");
            Manager.GetActiveRelationships(parent).Single(r => r.DetailBlockName == name).Coordination =
                deferred ? DetailCoordination.Deferred : DetailCoordination.Immediate;
            return unit;
        }
        public void Dispose() => Manager.Dispose();
    }

    private static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static async Task Finish(Task task) => await task.WaitAsync(TimeSpan.FromSeconds(10));
    private static DetailBlockSynchronizationResult Detail(DetailSynchronizationResult result, string name = "DETAIL") =>
        Assert.Single(result.Details, d => d.BlockName == name);

    [Fact]
    public async Task ForcedDeferredRefreshNeverMutatesModeOrRefreshesImmediateSiblings()
    {
        using var h = new Harness();
        var deferred = h.Add(deferred: true);
        var sibling = h.Add("SIBLING");
        await h.Manager.SynchronizeDetailBlocksAsync("MASTER");
        sibling.Invocations.Clear();
        var relation = h.Manager.GetActiveRelationships("MASTER").Single(r => r.DetailBlockName == "DETAIL");
        var entered = Signal(); var release = Signal();
        deferred.Setup(u => u.Get(It.IsAny<List<AppFilter>>())).Returns(async () =>
        { entered.SetResult(); await release.Task; return (object)new List<object>(); });
        var run = h.Manager.SynchronizeDeferredDetailWithOutcomeAsync("MASTER", "DETAIL");
        try
        {
            await Finish(entered.Task);
            Assert.Equal(DetailCoordination.Deferred, relation.Coordination);
            Assert.True(h.Manager.HasPendingDeferredSync("DETAIL"));
            sibling.Verify(u => u.Get(It.IsAny<List<AppFilter>>()), Times.Never);
        }
        finally { release.TrySetResult(); }
        var result = await run.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(DetailSynchronizationState.Refreshed, Detail(result).State);
        Assert.False(h.Manager.HasPendingDeferredSync("DETAIL"));
        Assert.Equal(DetailCoordination.Deferred, relation.Coordination);
        sibling.Verify(u => u.Get(It.IsAny<List<AppFilter>>()), Times.Never);
    }

    [Fact]
    public async Task SlowOldRequestAndNewMasterAreSerializedWithNewDetailsLast()
    {
        using var h = new Harness();
        var detail = h.Add();
        var entered = Signal(); var release = Signal();
        var keys = new List<string>(); int reads = 0; string? published = null;
        detail.Setup(u => u.Get(It.IsAny<List<AppFilter>>())).Returns(async (List<AppFilter> filters) =>
        {
            var key = Assert.Single(filters).FilterValue;
            keys.Add(key);
            if (++reads == 1) { entered.SetResult(); await release.Task; }
            published = key;
            return (object)new List<object>();
        });
        var old = h.Manager.SynchronizeDetailBlocksWithOutcomeAsync("MASTER");
        Task<DetailSynchronizationResult>? newer = null;
        try
        {
            await Finish(entered.Task);
            h.Current = new Row { Id = 2 };
            newer = h.Manager.SynchronizeDetailBlocksWithOutcomeAsync("MASTER");
            Assert.Equal(1, reads);
            Assert.False(newer.IsCompleted);
            Assert.Equal(2, h.Manager.PendingCallbackCount);
        }
        finally { release.TrySetResult(); }
        var first = await old.WaitAsync(TimeSpan.FromSeconds(10));
        var second = await newer!.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(DetailSynchronizationState.Superseded, Detail(first).State);
        Assert.True(Detail(first).ProviderMayHavePublished);
        Assert.True(second.AllDetailsCurrent);
        Assert.Equal(new[] { "1", "2" }, keys);
        Assert.Equal("2", published);
        Assert.Equal(0, h.Manager.PendingCallbackCount);
    }

    [Fact]
    public async Task QueuedRequestUsesCapturedMappingsNotLaterMutableConfiguration()
    {
        using var h = new Harness();
        var detail = h.Add();
        var entered = Signal(); var release = Signal();
        var filters = new List<List<AppFilter>>(); int calls = 0;
        detail.Setup(u => u.Get(It.IsAny<List<AppFilter>>())).Returns(async (List<AppFilter> input) =>
        {
            filters.Add(input);
            if (++calls == 1) { entered.SetResult(); await release.Task; }
            return (object)new List<object>();
        });
        var first = h.Manager.SynchronizeDetailBlocksWithOutcomeAsync("MASTER");
        Task<DetailSynchronizationResult>? second = null;
        try
        {
            await Finish(entered.Task);
            second = h.Manager.SynchronizeDetailBlocksWithOutcomeAsync("MASTER");
            var relation = Assert.Single(h.Manager.GetActiveRelationships("MASTER"));
            relation.KeyFieldMappings[0].MasterField = "Part";
            relation.KeyFieldMappings[0].DetailField = "Part";
            relation.Coordination = DetailCoordination.Deferred;
        }
        finally { release.TrySetResult(); }
        Assert.True((await first).AllDetailsCurrent);
        Assert.True((await second!).AllDetailsCurrent);
        Assert.Equal(2, filters.Count);
        Assert.All(filters, input => Assert.Contains(input, f => f.FieldName == "ParentId" && f.FilterValue == "1"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DirtyDetailIsNeverQueriedOrCleared(bool missingMaster)
    {
        using var h = new Harness();
        var detail = h.Add();
        detail.SetupGet(u => u.IsDirty).Returns(true);
        if (missingMaster) h.Current = null;
        var result = await h.Manager.SynchronizeDetailBlocksWithOutcomeAsync("MASTER");
        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Equal(DetailSynchronizationState.BlockedDirty, Detail(result).State);
        Assert.False(Detail(result).ProviderMayHavePublished);
        detail.Verify(u => u.Get(It.IsAny<List<AppFilter>>()), Times.Never);
        detail.Verify(u => u.Clear(), Times.Never);
        await Assert.ThrowsAsync<InvalidOperationException>(() => h.Manager.SynchronizeDetailBlocksAsync("MASTER"));
    }

    [Fact]
    public async Task DirtyDescendantPreventsParentReadAndPreservesDeferredMarker()
    {
        using var h = new Harness();
        var detail = h.Add(deferred: true);
        var child = h.Add("CHILD", "DETAIL");
        child.SetupGet(u => u.IsDirty).Returns(true);
        await h.Manager.SynchronizeDetailBlocksAsync("MASTER");
        var result = await h.Manager.SynchronizeDeferredDetailWithOutcomeAsync("MASTER", "DETAIL");
        Assert.Equal(DetailSynchronizationState.BlockedDirty, Detail(result).State);
        Assert.True(h.Manager.HasPendingDeferredSync("DETAIL"));
        detail.Verify(u => u.Get(It.IsAny<List<AppFilter>>()), Times.Never);
        child.Verify(u => u.Clear(), Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingMasterLeavesDeferredItemsUntouchedUnlessExplicitlyForced(bool force)
    {
        using var h = new Harness();
        var detail = h.Add(deferred: true);
        h.Current = null;
        var result = force ? await h.Manager.SynchronizeDeferredDetailWithOutcomeAsync("MASTER", "DETAIL") :
            await h.Manager.SynchronizeDetailBlocksWithOutcomeAsync("MASTER");
        Assert.Equal(force ? DetailSynchronizationState.Cleared : DetailSynchronizationState.Deferred, Detail(result).State);
        detail.Verify(u => u.Clear(), force ? Times.Once() : Times.Never());
        Assert.Equal(!force, h.Manager.HasPendingDeferredSync("DETAIL"));
    }

    [Fact]
    public async Task MissingKeyClearsImmediateHierarchyWithoutReadingOldChildKeys()
    {
        using var h = new Harness();
        var detail = h.Add();
        detail.SetupGet(u => u.CurrentItem).Returns(new Row { Id = 90 });
        var child = h.Add("CHILD", "DETAIL");
        h.Current = null;
        var result = await h.Manager.SynchronizeDetailBlocksWithOutcomeAsync("MASTER");
        Assert.True(result.AllDetailsCurrent);
        Assert.All(result.Details, d => Assert.Equal(DetailSynchronizationState.Cleared, d.State));
        child.Verify(u => u.Clear(), Times.Once);
        child.Verify(u => u.Get(It.IsAny<List<AppFilter>>()), Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedOrNullReadLeavesDescendantsUnattemptedAndDeferredMarkerPending(bool nullResult)
    {
        using var h = new Harness();
        var detail = h.Add(deferred: true);
        detail.SetupGet(u => u.CurrentItem).Returns(new Row());
        var child = h.Add("CHILD", "DETAIL");
        await h.Manager.SynchronizeDetailBlocksAsync("MASTER");
        if (nullResult) detail.Setup(u => u.Get(It.IsAny<List<AppFilter>>())).ReturnsAsync((object)null!);
        else detail.Setup(u => u.Get(It.IsAny<List<AppFilter>>())).ThrowsAsync(new InvalidOperationException("provider failed"));
        var result = await h.Manager.SynchronizeDeferredDetailWithOutcomeAsync("MASTER", "DETAIL");
        Assert.Equal(DetailSynchronizationState.Failed, Detail(result).State);
        Assert.True(Detail(result).ProviderMayHavePublished);
        Assert.Equal(DetailSynchronizationState.Unattempted, Detail(result, "CHILD").State);
        Assert.True(h.Manager.HasPendingDeferredSync("DETAIL"));
        child.Verify(u => u.Get(It.IsAny<List<AppFilter>>()), Times.Never);
    }

    [Fact]
    public async Task OneFailedBranchDoesNotHideIndependentSiblingResult()
    {
        using var h = new Harness();
        var detail = h.Add();
        var sibling = h.Add("SIBLING");
        detail.Setup(u => u.Get(It.IsAny<List<AppFilter>>())).ThrowsAsync(new InvalidOperationException("failed"));
        var result = await h.Manager.SynchronizeDetailBlocksWithOutcomeAsync("MASTER");
        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Equal(DetailSynchronizationState.Failed, Detail(result).State);
        Assert.Equal(DetailSynchronizationState.Refreshed, Detail(result, "SIBLING").State);
        sibling.Verify(u => u.Get(It.IsAny<List<AppFilter>>()), Times.Once);
    }

    [Fact]
    public async Task CloseCancelsQueuedRequestsAndWaitsForUncooperativeReadAcknowledgement()
    {
        using var h = new Harness();
        var detail = h.Add(deferred: true);
        await h.Manager.SynchronizeDetailBlocksAsync("MASTER");
        var entered = Signal(); var release = Signal(); int calls = 0;
        detail.Setup(u => u.Get(It.IsAny<List<AppFilter>>())).Returns(async () =>
        { calls++; entered.SetResult(); await release.Task; return (object)new List<object>(); });
        var active = h.Manager.SynchronizeDeferredDetailWithOutcomeAsync("MASTER", "DETAIL");
        Task<DetailSynchronizationResult>? queued = null;
        Task? drain = null;
        try
        {
            await Finish(entered.Task);
            queued = h.Manager.SynchronizeDeferredDetailWithOutcomeAsync("MASTER", "DETAIL");
            drain = h.Manager.DisposeAsync().AsTask();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => queued.WaitAsync(TimeSpan.FromSeconds(10)));
            Assert.False(drain.IsCompleted);
            Assert.Equal(1, h.Manager.PendingCallbackCount);
        }
        finally { release.TrySetResult(); }
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => active.WaitAsync(TimeSpan.FromSeconds(10)));
        await Finish(drain!);
        Assert.Equal(1, calls);
        Assert.False(h.Manager.HasPendingDeferredSync("DETAIL"));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => h.Manager.SynchronizeDetailBlocksWithOutcomeAsync("MASTER"));
        detail.Verify(u => u.Dispose(), Times.Never);
    }

    [Fact]
    public async Task CallerCancellationWhileQueuedDoesNotEnterProviderOrLosePendingMarker()
    {
        using var h = new Harness();
        var detail = h.Add(deferred: true);
        await h.Manager.SynchronizeDetailBlocksAsync("MASTER");
        var entered = Signal(); var release = Signal();
        detail.Setup(u => u.Get(It.IsAny<List<AppFilter>>())).Returns(async () =>
        { entered.SetResult(); await release.Task; return (object)new List<object>(); });
        var first = h.Manager.SynchronizeDeferredDetailWithOutcomeAsync("MASTER", "DETAIL");
        using var cts = new CancellationTokenSource();
        try
        {
            await Finish(entered.Task);
            var queued = h.Manager.SynchronizeDeferredDetailWithOutcomeAsync("MASTER", "DETAIL", cts.Token);
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => queued.WaitAsync(TimeSpan.FromSeconds(10)));
            Assert.True(h.Manager.HasPendingDeferredSync("DETAIL"));
            detail.Verify(u => u.Get(It.IsAny<List<AppFilter>>()), Times.Once);
        }
        finally { release.TrySetResult(); }
        Assert.True((await first).AllDetailsCurrent);
    }

    [Fact]
    public async Task PrecancelledRequestCannotInvokeTriggerOrProvider()
    {
        using var h = new Harness();
        var detail = h.Add();
        h.Triggers.Invocations.Clear();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            h.Manager.SynchronizeDetailBlocksWithOutcomeAsync("MASTER", new CancellationToken(true)));
        Assert.Empty(h.Triggers.Invocations);
        detail.Verify(u => u.Get(It.IsAny<List<AppFilter>>()), Times.Never);
        Assert.Equal(0, h.Manager.PendingCallbackCount);
    }

    [Fact]
    public async Task ReentrantProviderTraversalIsRejectedInsteadOfAwaitingItsOwnGate()
    {
        using var h = new Harness();
        var detail = h.Add();
        detail.Setup(u => u.Get(It.IsAny<List<AppFilter>>())).Returns(async () =>
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => h.Manager.SynchronizeDetailBlocksWithOutcomeAsync("MASTER"));
            return (object)new List<object>();
        });
        Assert.True((await h.Manager.SynchronizeDetailBlocksWithOutcomeAsync("MASTER").WaitAsync(TimeSpan.FromSeconds(10))).AllDetailsCurrent);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CapturedMasterOrDetailReplacementRejectsQueuedOldTargets(bool replaceMaster)
    {
        using var h = new Harness();
        var detail = h.Add();
        var entered = Signal(); var release = Signal();
        detail.Setup(u => u.Get(It.IsAny<List<AppFilter>>())).Returns(async () =>
        { entered.SetResult(); await release.Task; return (object)new List<object>(); });
        var first = h.Manager.SynchronizeDetailBlocksWithOutcomeAsync("MASTER");
        Task<DetailSynchronizationResult>? queued = null;
        var replacement = Harness.Unit();
        replacement.SetupGet(u => u.CurrentItem).Returns(new Row());
        try
        {
            await Finish(entered.Task);
            queued = h.Manager.SynchronizeDetailBlocksWithOutcomeAsync("MASTER");
            h.Manager.RegisterBlock(replaceMaster ? "MASTER" : "DETAIL", replacement.Object, Harness.Schema());
        }
        finally { release.TrySetResult(); }
        Assert.Equal(Errors.Failed, (await first).Flag);
        Assert.Equal(DetailSynchronizationState.Superseded, Detail(await queued!).State);
        replacement.Verify(u => u.Get(It.IsAny<List<AppFilter>>()), Times.Never);
        detail.Verify(u => u.Get(It.IsAny<List<AppFilter>>()), Times.Once);
    }

    [Fact]
    public async Task CompositeMappingsUseEveryFieldAndInvariantValues()
    {
        using var h = new Harness();
        var detail = h.Add();
        h.Manager.CreateMasterDetailRelation("MASTER", "DETAIL", new[]
        {
            new DataBlockFieldMapping { MasterField = "Id", DetailField = "ParentId" },
            new DataBlockFieldMapping { MasterField = "Part", DetailField = "Part" }
        });
        var prior = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            Assert.True((await h.Manager.SynchronizeDetailBlocksWithOutcomeAsync("MASTER")).AllDetailsCurrent);
        }
        finally { CultureInfo.CurrentCulture = prior; }
        detail.Verify(u => u.Get(It.Is<List<AppFilter>>(f => f.Count == 2 &&
            f.Any(x => x.FieldName == "Part" && x.FilterValue == "2.5") &&
            f.Any(x => x.FieldName == "ParentId" && x.FilterValue == "1"))), Times.Once);
    }

    [Fact]
    public async Task CyclesFailBeforeReadAndReturnAnActionableOutcome()
    {
        using var h = new Harness();
        var detail = h.Add();
        h.Manager.CreateMasterDetailRelation("DETAIL", "MASTER", "Id", "ParentId");
        var result = await h.Manager.SynchronizeDetailBlocksWithOutcomeAsync("MASTER");
        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Contains("cycle", result.Message);
        Assert.False(result.AllDetailsCurrent);
        detail.Verify(u => u.Get(It.IsAny<List<AppFilter>>()), Times.Never);
        h.Master.Verify(u => u.Get(It.IsAny<List<AppFilter>>()), Times.Never);
    }

    [Fact]
    public async Task TriggerCancellationHasNoDetailEffectsAndReceivesCallerCancellation()
    {
        using var h = new Harness();
        var detail = h.Add();
        h.Triggers.Setup(t => t.FireBlockTriggerAsync(TriggerType.OnPopulateDetails, "MASTER",
            It.IsAny<TriggerContext>(), It.Is<CancellationToken>(ct => ct.CanBeCanceled)))
            .ReturnsAsync(TriggerResult.Cancelled);
        var result = await h.Manager.SynchronizeDetailBlocksWithOutcomeAsync("MASTER");
        Assert.Equal(Errors.Failed, result.Flag);
        detail.Verify(u => u.Get(It.IsAny<List<AppFilter>>()), Times.Never);
        detail.Verify(u => u.Clear(), Times.Never);
    }

    [Fact]
    public async Task SameRecordKeyMutationSupersedesBothActiveAndQueuedOldRequests()
    {
        using var h = new Harness();
        var detail = h.Add();
        var entered = Signal(); var release = Signal();
        detail.Setup(u => u.Get(It.IsAny<List<AppFilter>>())).Returns(async () =>
        { entered.SetResult(); await release.Task; return (object)new List<object>(); });
        var first = h.Manager.SynchronizeDetailBlocksWithOutcomeAsync("MASTER");
        Task<DetailSynchronizationResult>? queued = null;
        try
        {
            await Finish(entered.Task);
            queued = h.Manager.SynchronizeDetailBlocksWithOutcomeAsync("MASTER");
            ((Row)h.Current!).Id = 88;
        }
        finally { release.TrySetResult(); }
        Assert.Equal(DetailSynchronizationState.Superseded, Detail(await first).State);
        var second = Detail(await queued!);
        Assert.Equal(DetailSynchronizationState.Superseded, second.State);
        Assert.False(second.ProviderMayHavePublished);
        detail.Verify(u => u.Get(It.IsAny<List<AppFilter>>()), Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnregisterPrunesOutgoingOrIncomingGraphAfterLookupRetirement(bool master)
    {
        using var h = new Harness();
        h.Add(deferred: true);
        await h.Manager.SynchronizeDetailBlocksAsync("MASTER");
        Assert.True(h.Manager.HasPendingDeferredSync("DETAIL"));
        Assert.True(h.Manager.UnregisterBlock(master ? "MASTER" : "DETAIL"));
        Assert.Empty(h.Manager.GetActiveRelationships("MASTER"));
        Assert.False(h.Manager.HasPendingDeferredSync("DETAIL"));
        if (master) Assert.Null(h.Manager.GetBlock("DETAIL").MasterBlockName);
        else Assert.False(h.Manager.GetBlock("MASTER").IsMasterBlock);
    }

    [Fact]
    public async Task CallerCancellationAfterProviderAcknowledgementPreventsDescendantRead()
    {
        using var h = new Harness();
        var detail = h.Add(deferred: true);
        detail.SetupGet(u => u.CurrentItem).Returns(new Row());
        var child = h.Add("CHILD", "DETAIL");
        await h.Manager.SynchronizeDetailBlocksAsync("MASTER");
        using var cts = new CancellationTokenSource();
        detail.Setup(u => u.Get(It.IsAny<List<AppFilter>>())).Returns(() =>
        { cts.Cancel(); return Task.FromResult<dynamic>(new List<object>()); });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            h.Manager.SynchronizeDeferredDetailWithOutcomeAsync("MASTER", "DETAIL", cts.Token));
        Assert.True(h.Manager.HasPendingDeferredSync("DETAIL"));
        child.Verify(u => u.Get(It.IsAny<List<AppFilter>>()), Times.Never);
    }

    [Fact]
    public async Task DetailTriggerCannotDrainItsOwnOperation()
    {
        using var h = new Harness();
        h.Add();
        h.Triggers.Setup(t => t.FireBlockTriggerAsync(TriggerType.OnPopulateDetails, "MASTER",
            It.IsAny<TriggerContext>(), It.IsAny<CancellationToken>())).Callback(() =>
            {
                Assert.Throws<InvalidOperationException>(() => { _ = h.Manager.WaitForPendingCallbacksAsync(); });
                Assert.Throws<InvalidOperationException>(() => h.Manager.DisposeAsync());
            }).ReturnsAsync(TriggerResult.Success);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => h.Manager.SynchronizeDetailBlocksWithOutcomeAsync("MASTER"));
        await h.Manager.DisposeAsync();
        Assert.Equal(0, h.Manager.PendingCallbackCount);
    }

    [Fact]
    public async Task NoRelationshipDoesNotClaimThatAnyDetailWasRefreshed()
    {
        using var h = new Harness();
        var result = await h.Manager.SynchronizeDeferredDetailWithOutcomeAsync("MASTER", "ABSENT");
        Assert.Equal(Errors.Ok, result.Flag);
        Assert.Empty(result.Details);
        Assert.False(result.AllDetailsCurrent);
    }

    [Fact]
    public async Task OversizedHierarchyFailsBeforeAnyProviderRead()
    {
        using var h = new Harness();
        var first = h.Add();
        var parent = "DETAIL";
        for (var index = 0; index < 128; index++)
        {
            var name = $"LEVEL{index}";
            h.Add(name, parent);
            parent = name;
        }
        var result = await h.Manager.SynchronizeDetailBlocksWithOutcomeAsync("MASTER");
        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Contains("128 hierarchy", result.Message);
        first.Verify(u => u.Get(It.IsAny<List<AppFilter>>()), Times.Never);
    }

    public sealed class UnreadableRow { public int Id => throw new InvalidOperationException("getter failed"); }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnreadableOrAbsentMasterPropertyIsNotTreatedAsAnEmptyKey(bool absent)
    {
        using var h = new Harness();
        var detail = h.Add();
        h.Current = absent ? new object() : new UnreadableRow();
        var result = await h.Manager.SynchronizeDetailBlocksWithOutcomeAsync("MASTER");
        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Contains("unreadable", result.Message);
        detail.Verify(u => u.Clear(), Times.Never);
        detail.Verify(u => u.Get(It.IsAny<List<AppFilter>>()), Times.Never);
    }

    [Fact]
    public async Task RetiredReadSuppressionCannotSilenceNewRegistrationCurrentChanged()
    {
        using var h = new Harness();
        var detail = h.Add();
        var child = h.Add("CHILD", "DETAIL");
        var entered = Signal(); var release = Signal();
        detail.Setup(u => u.Get(It.IsAny<List<AppFilter>>())).Returns(async () =>
        { entered.SetResult(); await release.Task; return (object)new List<object>(); });
        var first = h.Manager.SynchronizeDetailBlocksWithOutcomeAsync("MASTER");
        try
        {
            await Finish(entered.Task);
            var replacement = Harness.Unit();
            replacement.SetupGet(u => u.CurrentItem).Returns(new Row { Id = 9 });
            h.Manager.RegisterBlock("DETAIL", replacement.Object, Harness.Schema());
            replacement.Raise(u => u.CurrentChanged += null, replacement.Object, EventArgs.Empty);
        }
        finally { release.TrySetResult(); }
        Assert.Equal(Errors.Failed, (await first).Flag);
        await Finish(h.Manager.WaitForPendingCallbacksAsync());
        child.Verify(u => u.Get(It.Is<List<AppFilter>>(filters => filters.Any(f => f.FilterValue == "9"))), Times.Once);
    }

    [Fact]
    public async Task ReentrantCloseFromMasterGetterCannotInvokePopulateTrigger()
    {
        using var h = new Harness();
        var detail = h.Add();
        h.Triggers.Invocations.Clear();
        h.Master.SetupGet(u => u.CurrentItem).Callback(h.Manager.Dispose).Returns(new Row());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => h.Manager.SynchronizeDetailBlocksWithOutcomeAsync("MASTER"));
        h.Triggers.Verify(t => t.FireBlockTriggerAsync(It.IsAny<TriggerType>(), It.IsAny<string>(),
            It.IsAny<TriggerContext>(), It.IsAny<CancellationToken>()), Times.Never);
        detail.Verify(u => u.Get(It.IsAny<List<AppFilter>>()), Times.Never);
        await h.Manager.DisposeAsync();
    }
}
