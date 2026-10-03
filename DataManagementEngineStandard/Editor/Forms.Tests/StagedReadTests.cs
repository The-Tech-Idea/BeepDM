using Microsoft.Data.Sqlite;
using Moq;
using TheTechIdea.Beep;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.Forms.Helpers;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOW;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Extensions;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using Xunit;

namespace TheTechIdea.Beep.Editor.UOWManager.Tests;

public class StagedReadTests
{
    public class Row : Entity
    {
        private int _id;
        private int _parentId;
        private string _name = "prior";
        public int Id { get => _id; set => SetProperty(ref _id, value); }
        public int ParentId { get => _parentId; set => SetProperty(ref _parentId, value); }
        public string Name { get => _name; set => SetProperty(ref _name, value); }
    }

    private sealed class OverrideReader : UnitofWork<Row>
    {
        public OverrideReader(IDMEEditor editor) : base(editor, "database", "Rows", Schema(), "Id") { }
        public override Task<ObservableBindingList<Row>> Get(List<AppFilter> filters) => base.Get(filters);
    }

    private static EntityStructure Schema() => new()
    {
        EntityName = "Rows", Fields = new List<EntityField>
        {
            new() { FieldName = "Id", Fieldtype = "System.Int32", IsKey = true },
            new() { FieldName = "ParentId", Fieldtype = "System.Int32" },
            new() { FieldName = "Name", Fieldtype = "System.String" }
        }
    };

    private static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static async Task Finish(Task task) => await task.WaitAsync(TimeSpan.FromSeconds(10));

    private sealed class Harness : IDisposable
    {
        internal readonly Mock<IDMEEditor> Editor = new();
        internal readonly Mock<IDataSource> Source = new();
        internal readonly UnitofWork<Row> Unit;
        internal readonly ObservableBindingList<Row> Prior;
        internal Harness()
        {
            Source.SetupGet(s => s.DatasourceName).Returns("database");
            Source.SetupGet(s => s.DatasourceType).Returns(DataSourceType.SqlLite);
            Source.Setup(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<AppFilter>>()))
                .ReturnsAsync(new object[] { new Row { Id = 8, ParentId = 1, Name = "loaded" } });
            Prior = new ObservableBindingList<Row>(new List<Row> { new Row { Id = 3 }, new Row { Id = 4 } });
            Unit = new UnitofWork<Row>(Editor.Object, "database", "Rows", Schema(), "Id")
                { DataSource = Source.Object, Units = Prior };
        }
        public void Dispose() { Unit.Dispose(); Prior.Dispose(); }
    }

    [Fact]
    public async Task PreparationAndAbortPreserveLiveCollectionCursorAndTracking()
    {
        using var h = new Harness();
        h.Prior.MoveTo(1);
        var current = h.Unit.CurrentItem;
        using (var stage = await h.Unit.PrepareReadAsync(new()))
        {
            Assert.Same(h.Prior, h.Unit.Units);
            Assert.Same(current, h.Unit.CurrentItem);
            Assert.False(h.Unit.IsDirty);
            Assert.False(stage.IsPublished);
        }
        Assert.Same(h.Prior, h.Unit.Units);
        Assert.Equal(1, h.Unit.Units.CurrentIndex);
        using var retry = await h.Unit.PrepareReadAsync(new());
        retry.Publish(publish => publish());
        Assert.True(retry.IsPublished);
        Assert.NotSame(h.Prior, h.Unit.Units);
        Assert.Equal(8, Assert.Single(h.Unit.Units).Id);
        Assert.False(h.Unit.IsDirty);
    }

    [Theory]
    [InlineData("reject")]
    [InlineData("omit")]
    public async Task RejectedPublicationPreservesRowsAndReleasesAdmissionOnDispose(string mode)
    {
        using var h = new Harness();
        using (var stage = await h.Unit.PrepareReadAsync(new()))
        {
            Assert.Throws<InvalidOperationException>(() => stage.Publish(publish =>
            { if (mode == "reject") throw new InvalidOperationException("rejected"); }));
            Assert.False(stage.IsPublished);
            Assert.Same(h.Prior, h.Unit.Units);
        }
        using var retry = await h.Unit.PrepareReadAsync(new());
        retry.Publish(publish => publish());
    }

    [Fact]
    public async Task PublishedStageCannotPublishTwiceAndRetainedActionExpires()
    {
        using var h = new Harness();
        using var stage = await h.Unit.PrepareReadAsync(new());
        Action? retained = null;
        stage.Publish(publish => { retained = publish; publish(); });
        var live = h.Unit.Units;
        Assert.Throws<InvalidOperationException>(() => retained!());
        Assert.Throws<InvalidOperationException>(() => stage.Publish(publish => publish()));
        Assert.Same(live, h.Unit.Units);
    }

    [Fact]
    public async Task DisposalInvalidatesUnpublishedStage()
    {
        using var h = new Harness();
        var stage = await h.Unit.PrepareReadAsync(new());
        stage.Dispose(); stage.Dispose();
        Assert.Throws<ObjectDisposedException>(() => stage.Publish(publish => publish()));
        Assert.Same(h.Prior, h.Unit.Units);
    }

    [Fact]
    public async Task AuthorizerCannotDispatchPublicationToAnotherThread()
    {
        using var h = new Harness();
        using var stage = await h.Unit.PrepareReadAsync(new());
        Assert.Throws<InvalidOperationException>(() => stage.Publish(publish =>
        {
            Exception? failure = null;
            var thread = new Thread(() => { try { publish(); } catch (Exception ex) { failure = ex; } });
            thread.Start();
            Assert.True(thread.Join(TimeSpan.FromSeconds(10)));
            Assert.IsType<InvalidOperationException>(failure);
        }));
        Assert.False(stage.IsPublished);
        Assert.Same(h.Prior, h.Unit.Units);
    }

    [Fact]
    public async Task ObserverFailuresDoNotUndoPublicationAndOtherObserversRun()
    {
        using var h = new Harness();
        var notifications = 0;
        h.Unit.PropertyChanged += (_, _) => throw new InvalidOperationException("property observer");
        h.Unit.PropertyChanged += (_, _) => notifications++;
        h.Unit.PostQuery += (_, _) => throw new InvalidOperationException("post observer");
        h.Unit.PostQuery += (_, args) => { notifications++; args.Cancel = true; };
        using var stage = await h.Unit.PrepareReadAsync(new());
        stage.Publish(publish => publish());
        Assert.True(stage.IsPublished);
        Assert.Equal(3, stage.NotificationFailures.Count);
        Assert.Equal(2, notifications);
        Assert.Equal(8, Assert.Single(h.Unit.Units).Id);
    }

    [Fact]
    public async Task AuthorizerThrowAfterSwapIsAPublishedNotificationFailure()
    {
        using var h = new Harness();
        using var stage = await h.Unit.PrepareReadAsync(new());
        stage.Publish(publish => { publish(); throw new InvalidOperationException("after swap"); });
        Assert.True(stage.IsPublished);
        Assert.Single(stage.NotificationFailures);
    }

    [Theory]
    [InlineData("edit")]
    [InlineData("cancel")]
    [InlineData("dispose")]
    public async Task LastPublicationBoundaryRechecksObservedChanges(string change)
    {
        using var h = new Harness();
        using var token = new CancellationTokenSource();
        using var stage = await h.Unit.PrepareReadAsync(new(), token.Token);
        Assert.ThrowsAny<Exception>(() => stage.Publish(publish =>
        {
            if (change == "edit") h.Prior[0].Name = "unsaved";
            if (change == "cancel") token.Cancel();
            if (change == "dispose") h.Unit.Dispose();
            publish();
        }));
        Assert.False(stage.IsPublished);
        Assert.Same(h.Prior, h.Unit.Units);
    }

    [Fact]
    public async Task PreparedRowsHaveIndependentShellsAndLiveHandlerWiring()
    {
        using var h = new Harness();
        var providerRow = new Row { Id = 20 };
        var providerNotifications = 0;
        providerRow.PropertyChanged += (_, _) => providerNotifications++;
        h.Source.Setup(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<AppFilter>>()))
            .ReturnsAsync(new object[] { providerRow });
        var itemChanges = 0;
        h.Unit.ItemChanged += (_, _) => itemChanges++;
        using (var stage = await h.Unit.PrepareReadAsync(new())) stage.Publish(publish => publish());
        var live = Assert.Single(h.Unit.Units);
        Assert.NotSame(providerRow, live);
        live.Name = "edited";
        Assert.True(h.Unit.IsDirty);
        Assert.Equal(1, itemChanges);
        Assert.Equal(0, providerNotifications);
        h.Prior[0].Name = "retained old list";
        Assert.Equal(1, itemChanges);
        Assert.Equal("prior", providerRow.Name);
    }

    [Theory]
    [InlineData("edit")]
    [InlineData("cursor")]
    [InlineData("tenant")]
    [InlineData("entity")]
    [InlineData("dispose")]
    public async Task ChangedTargetDuringProviderAwaitNeverPublishes(string change)
    {
        using var h = new Harness();
        var entered = Signal(); var release = Signal();
        h.Source.Setup(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<AppFilter>>())).Returns(async () =>
        { entered.SetResult(); await release.Task; return new object[] { new Row { Id = 8 } }; });
        var read = h.Unit.PrepareReadAsync(new());
        try
        {
            await Finish(entered.Task);
            switch (change)
            {
                case "edit": h.Prior[0].Name = "unsaved"; break;
                case "cursor": h.Prior.MoveTo(1); break;
                case "tenant": h.Unit.ScopeToTenant("Name", "tenant"); break;
                case "entity": h.Unit.EntityName = "Other"; break;
                case "dispose": h.Unit.Dispose(); break;
            }
        }
        finally { release.TrySetResult(); }
        await Assert.ThrowsAnyAsync<Exception>(async () => await read.WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.Same(h.Prior, h.Unit.Units);
        Assert.Equal(2, h.Unit.Units.Count);
    }

    [Fact]
    public async Task DirtyBeforeReadFailsBeforeProviderAndCanRetryAfterExplicitDiscard()
    {
        using var h = new Harness();
        h.Prior[0].Name = "unsaved";
        await Assert.ThrowsAsync<InvalidOperationException>(() => h.Unit.PrepareReadAsync(new()));
        h.Source.Verify(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<AppFilter>>()), Times.Never);
        h.Prior.RejectChanges();
        using var retry = await h.Unit.PrepareReadAsync(new());
    }

    [Fact]
    public async Task DirtyAfterPreparationRejectsPublicationWithoutDiscardingEdits()
    {
        using var h = new Harness();
        using var stage = await h.Unit.PrepareReadAsync(new());
        h.Prior[0].Name = "unsaved";
        Assert.Throws<InvalidOperationException>(() => stage.Publish(publish => publish()));
        Assert.False(stage.IsPublished);
        Assert.Same(h.Prior, h.Unit.Units);
        Assert.Equal("unsaved", h.Prior[0].Name);
    }

    [Fact]
    public async Task ActiveReadRejectsOverlappingReadCommitAndReplacementRoutes()
    {
        using var h = new Harness();
        using (var stage = await h.Unit.PrepareReadAsync(new()))
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => h.Unit.PrepareReadAsync(new()));
            await Assert.ThrowsAsync<InvalidOperationException>(() => h.Unit.Get());
            await Assert.ThrowsAsync<InvalidOperationException>(() => h.Unit.Get(new List<AppFilter>()));
            await Assert.ThrowsAsync<InvalidOperationException>(() => h.Unit.GetQuery("SELECT * FROM Rows"));
            Assert.Throws<InvalidOperationException>(() => h.Unit.Clear());
            Assert.Throws<InvalidOperationException>(() => h.Unit.Units = new());
            Assert.Throws<InvalidOperationException>(() => h.Unit.FilteredUnits = new());
            Assert.Throws<InvalidOperationException>(() => h.Unit.DataSource = new Mock<IDataSource>().Object);
            Assert.Equal(Errors.Failed, (await h.Unit.Commit()).Flag);
            Assert.Equal(Errors.Failed, (await h.Unit.Rollback()).Flag);
        }
        h.Unit.Clear();
        Assert.Empty(h.Unit.Units);
    }

    [Theory]
    [InlineData("null-result")]
    [InlineData("null-row")]
    [InlineData("bad-conversion")]
    [InlineData("unknown-record")]
    [InlineData("pre-query")]
    [InlineData("provider-throws")]
    public async Task FailedPreparePreservesPriorAndReleasesAdmission(string failure)
    {
        using var h = new Harness();
        EventHandler<UnitofWorkParams> cancel = (_, args) => args.Cancel = true;
        switch (failure)
        {
            case "null-result": h.Source.Setup(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<AppFilter>>())).ReturnsAsync((IEnumerable<object>)null!); break;
            case "null-row": h.Source.Setup(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<AppFilter>>())).ReturnsAsync(new object[] { null! }); break;
            case "bad-conversion": h.Source.Setup(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<AppFilter>>())).ReturnsAsync(new object[] { new Dictionary<string, object> { ["Id"] = "invalid" } }); break;
            case "unknown-record": h.Source.Setup(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<AppFilter>>())).ReturnsAsync(new object[] { new object() }); break;
            case "pre-query": h.Unit.PreQuery += cancel; break;
            case "provider-throws": h.Source.Setup(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<AppFilter>>())).ThrowsAsync(new InvalidOperationException("provider")); break;
        }
        await Assert.ThrowsAnyAsync<Exception>(() => h.Unit.PrepareReadAsync(new()));
        Assert.Same(h.Prior, h.Unit.Units);
        h.Unit.PreQuery -= cancel;
        h.Source.Setup(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<AppFilter>>())).ReturnsAsync(Array.Empty<object>());
        using var retry = await h.Unit.PrepareReadAsync(new());
        retry.Publish(publish => publish());
        Assert.Empty(h.Unit.Units);
    }

    [Fact]
    public async Task TenantRestrictionIsIndependentAndFiltersAreCopied()
    {
        using var h = new Harness();
        h.Unit.ScopeToTenant("Name", "allowed");
        var caller = new AppFilter { FieldName = "Name", Operator = "=", FilterValue = "foreign" };
        List<AppFilter>? sent = null;
        h.Source.Setup(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<AppFilter>>()))
            .Returns((string _, List<AppFilter> filters) =>
            {
                sent = filters; filters[0].FilterValue = "provider mutation";
                return Task.FromResult<IEnumerable<object>>(Array.Empty<object>());
            });
        using var stage = await h.Unit.PrepareReadAsync(new() { caller });
        Assert.Equal("foreign", caller.FilterValue);
        Assert.Equal(2, sent!.Count);
        Assert.Equal("allowed", sent[1].FilterValue);
    }

    [Fact]
    public async Task CallerCancellationWaitsForProviderAcknowledgementAndPreservesRows()
    {
        using var h = new Harness();
        using var token = new CancellationTokenSource();
        var entered = Signal(); var release = Signal();
        h.Source.Setup(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<AppFilter>>())).Returns(async () =>
        { entered.SetResult(); await release.Task; return Array.Empty<object>(); });
        var read = h.Unit.PrepareReadAsync(new(), token.Token);
        try
        {
            await Finish(entered.Task);
            token.Cancel();
            Assert.False(read.IsCompleted);
            Assert.Same(h.Prior, h.Unit.Units);
        }
        finally { release.TrySetResult(); }
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await read.WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.Same(h.Prior, h.Unit.Units);
    }

    [Fact]
    public async Task PrecancelNeverCallsProvider()
    {
        using var h = new Harness();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => h.Unit.PrepareReadAsync(new(), new CancellationToken(true)));
        h.Source.Verify(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<AppFilter>>()), Times.Never);
    }

    [Fact]
    public async Task WrapperForwardsCapabilityWithoutTurningErrorsIntoLegacyFallback()
    {
        using var h = new Harness();
        using var wrapper = new UnitOfWorkWrapper(h.Unit);
        Assert.True(wrapper.SupportsStagedRead);
        h.Source.Setup(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<AppFilter>>())).ThrowsAsync(new InvalidOperationException("read failed"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => wrapper.PrepareReadAsync(new()));
        h.Source.Verify(s => s.GetEntity(It.IsAny<string>(), It.IsAny<List<AppFilter>>()), Times.Never);
        Assert.Same(h.Prior, h.Unit.Units);
    }

    [Fact]
    public void OverriddenGetAndListModeDoNotImplicitlyOptIn()
    {
        using var h = new Harness();
        using var overridden = new OverrideReader(h.Editor.Object) { DataSource = h.Source.Object };
        Assert.False(overridden.SupportsStagedRead);
        h.Unit.IsInListMode = true;
        Assert.False(h.Unit.SupportsStagedRead);
    }

    private static (FormsManager Manager, Mock<IUnitofWork> Master, Action<Row> SetMaster) RegisterDetail(Harness h,
        ISecurityManager? security = null)
    {
        var triggers = new Mock<ITriggerManager>();
        triggers.Setup(t => t.FireBlockTriggerAsync(It.IsAny<TriggerType>(), It.IsAny<string>(),
            It.IsAny<TriggerContext>(), It.IsAny<CancellationToken>())).ReturnsAsync(TriggerResult.Success);
        var manager = new FormsManager(h.Editor.Object, triggerManager: triggers.Object, securityManager: security);
        var master = new Mock<IUnitofWork>();
        master.SetupProperty(u => u.EntityStructure);
        master.SetupProperty(u => u.DataSource);
        Row current = new() { Id = 1 };
        master.SetupGet(u => u.CurrentItem).Returns(() => current);
        manager.RegisterBlock("MASTER", master.Object, Schema());
        manager.RegisterBlock("DETAIL", new UnitOfWorkWrapper(h.Unit), Schema());
        manager.CreateMasterDetailRelation("MASTER", "DETAIL", "Id", "ParentId");
        return (manager, master, row => current = row);
    }

    [Theory]
    [InlineData("master")]
    [InlineData("policy")]
    [InlineData("unregister")]
    [InlineData("rebind")]
    [InlineData("dirty")]
    public async Task RealDetailStaleReadLeavesPriorCollectionUnpublished(string change)
    {
        using var h = new Harness();
        var (manager, _, setMaster) = RegisterDetail(h);
        using (manager)
        {
            var entered = Signal(); var release = Signal();
            h.Source.Setup(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<AppFilter>>())).Returns(async () =>
            { entered.SetResult(); await release.Task; return new object[] { new Row { Id = 8 } }; });
            var run = manager.SynchronizeDetailBlocksWithOutcomeAsync("MASTER");
            try
            {
                await Finish(entered.Task);
                Assert.Same(h.Prior, h.Unit.Units);
                switch (change)
                {
                    case "master": setMaster(new Row { Id = 2 }); break;
                    case "policy": manager.SetBlockSecurity("DETAIL", new BlockSecurity { RowFilterClause = "Name = 'tenant'" }); break;
                    case "unregister": manager.UnregisterBlock("DETAIL"); break;
                    case "dirty": h.Prior[0].Name = "unsaved"; break;
                    case "rebind":
                        var replacement = new Mock<IUnitofWork>();
                        replacement.SetupProperty(u => u.EntityStructure);
                        replacement.SetupProperty(u => u.DataSource);
                        manager.RegisterBlock("DETAIL", replacement.Object, Schema()); break;
                }
            }
            finally { release.TrySetResult(); }
            var outcome = Assert.Single((await run.WaitAsync(TimeSpan.FromSeconds(10))).Details);
            Assert.NotEqual(DetailSynchronizationState.Refreshed, outcome.State);
            Assert.False(outcome.RecordsPublished);
            Assert.False(outcome.ProviderMayHavePublished);
            Assert.Same(h.Prior, h.Unit.Units);
        }
    }

    [Fact]
    public async Task RealDetailCloseDrainsPhysicalProviderBeforeAsyncDisposal()
    {
        using var h = new Harness();
        var (manager, _, _) = RegisterDetail(h);
        var entered = Signal(); var release = Signal();
        h.Source.Setup(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<AppFilter>>())).Returns(async () =>
        { entered.SetResult(); await release.Task; return new object[] { new Row { Id = 8 } }; });
        var run = manager.SynchronizeDetailBlocksWithOutcomeAsync("MASTER");
        Task? close = null;
        try
        {
            await Finish(entered.Task);
            close = manager.DisposeAsync().AsTask();
            Assert.False(close.IsCompleted);
            Assert.Same(h.Prior, h.Unit.Units);
        }
        finally { release.TrySetResult(); manager.Dispose(); }
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await run.WaitAsync(TimeSpan.FromSeconds(10)));
        await Finish(close!);
        Assert.Same(h.Prior, h.Unit.Units);
    }

    [Fact]
    public async Task RealDetailQueuedNewMasterWinsWithoutPublishingOldCandidate()
    {
        using var h = new Harness();
        var (manager, _, setMaster) = RegisterDetail(h);
        using (manager)
        {
            var entered = Signal(); var release = Signal();
            var calls = 0;
            h.Source.Setup(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<AppFilter>>()))
                .Returns(async (string _, List<AppFilter> filters) =>
                {
                    if (++calls == 1) { entered.SetResult(); await release.Task; }
                    Assert.Same(h.Prior, h.Unit.Units);
                    return new object[] { new Row { Id = int.Parse(Assert.Single(filters).FilterValue) } };
                });
            var old = manager.SynchronizeDetailBlocksWithOutcomeAsync("MASTER");
            Task<DetailSynchronizationResult>? newer = null;
            try
            {
                await Finish(entered.Task);
                setMaster(new Row { Id = 2 });
                newer = manager.SynchronizeDetailBlocksWithOutcomeAsync("MASTER");
                Assert.False(newer.IsCompleted);
                Assert.Equal(1, calls);
            }
            finally { release.TrySetResult(); }
            var oldOutcome = Assert.Single((await old.WaitAsync(TimeSpan.FromSeconds(10))).Details);
            Assert.Equal(DetailSynchronizationState.Superseded, oldOutcome.State);
            Assert.False(oldOutcome.RecordsPublished);
            Assert.False(oldOutcome.ProviderMayHavePublished);
            var accepted = Assert.Single((await newer!.WaitAsync(TimeSpan.FromSeconds(10))).Details);
            Assert.Equal(DetailSynchronizationState.Refreshed, accepted.State);
            Assert.True(accepted.RecordsPublished);
            Assert.Equal(2, Assert.Single(h.Unit.Units).Id);
            Assert.Equal(2, calls);
        }
    }

    [Fact]
    public async Task RealDetailCallerCancellationNeverPublishesCandidate()
    {
        using var h = new Harness();
        var (manager, _, _) = RegisterDetail(h);
        using (manager)
        using (var token = new CancellationTokenSource())
        {
            var entered = Signal(); var release = Signal();
            h.Source.Setup(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<AppFilter>>())).Returns(async () =>
            { entered.SetResult(); await release.Task; return new object[] { new Row { Id = 8 } }; });
            var run = manager.SynchronizeDetailBlocksWithOutcomeAsync("MASTER", token.Token);
            try
            {
                await Finish(entered.Task);
                token.Cancel();
                Assert.False(run.IsCompleted);
            }
            finally { release.TrySetResult(); }
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await run.WaitAsync(TimeSpan.FromSeconds(10)));
            Assert.Same(h.Prior, h.Unit.Units);
            h.Source.Setup(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<AppFilter>>())).ReturnsAsync(Array.Empty<object>());
            using var next = await h.Unit.PrepareReadAsync(new());
        }
    }

    [Fact]
    public async Task PolicyChangeAtFinalAuthorizationGateRejectsPreparedRealRows()
    {
        using var h = new Harness();
        var ownedPolicy = new SecurityManager();
        var security = new Mock<ISecurityManager>();
        security.Setup(s => s.IsBlockAllowed(It.IsAny<string>(), It.IsAny<SecurityPermission>())).Returns(true);
        security.Setup(s => s.GetBlockSecurity(It.IsAny<string>())).Returns((string name) => ownedPolicy.GetBlockSecurity(name));
        var gate = security.As<IQuerySecurityPublication>();
        gate.SetupGet(s => s.SecurityRevision).Returns(() => ownedPolicy.SecurityRevision);
        gate.Setup(s => s.CaptureQuerySecurity(It.IsAny<string>())).Returns((string name) => ownedPolicy.CaptureQuerySecurity(name));
        gate.Setup(s => s.TryPublishQuery(It.IsAny<long>(), It.IsAny<Action>())).Returns((long revision, Action publish) =>
        {
            ownedPolicy.SetSecurityContext(new SecurityContext());
            return ownedPolicy.TryPublishQuery(revision, publish);
        });
        var (manager, _, _) = RegisterDetail(h, security.Object);
        using (manager)
        {
            var outcome = Assert.Single((await manager.SynchronizeDetailBlocksWithOutcomeAsync("MASTER")).Details);
            Assert.Equal(DetailSynchronizationState.Failed, outcome.State);
            Assert.False(outcome.RecordsPublished);
            Assert.False(outcome.ProviderMayHavePublished);
            Assert.Same(h.Prior, h.Unit.Units);
            h.Source.Verify(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<AppFilter>>()), Times.Once);
        }
    }

    [Fact]
    public async Task RealDetailPublishedWithObserverFailureIsNotReadFailure()
    {
        using var h = new Harness();
        var (manager, _, _) = RegisterDetail(h);
        using (manager)
        {
            h.Unit.PostQuery += (_, _) => throw new InvalidOperationException("observer failed");
            var result = await manager.SynchronizeDetailBlocksWithOutcomeAsync("MASTER");
            var detail = Assert.Single(result.Details);
            Assert.Equal(DetailSynchronizationState.Refreshed, detail.State);
            Assert.True(detail.RecordsPublished);
            Assert.True(detail.ProviderMayHavePublished);
            Assert.Single(detail.NotificationFailures);
            Assert.Equal(8, Assert.Single(h.Unit.Units).Id);
        }
    }

    [Fact]
    public async Task SecurityHelperWithoutPublicationGateRejectsBeforeProvider()
    {
        using var h = new Harness();
        var security = new Mock<ISecurityManager>();
        security.Setup(s => s.IsBlockAllowed(It.IsAny<string>(), It.IsAny<SecurityPermission>())).Returns(true);
        var (manager, _, _) = RegisterDetail(h, security.Object);
        using (manager)
        {
            var result = await manager.SynchronizeDetailBlocksWithOutcomeAsync("MASTER");
            Assert.Equal(DetailSynchronizationState.Failed, Assert.Single(result.Details).State);
            h.Source.Verify(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<AppFilter>>()), Times.Never);
            Assert.Same(h.Prior, h.Unit.Units);
        }
    }

    [Fact]
    public void DefaultSecurityGateRejectsChangedRevisionWithoutInvokingPublication()
    {
        var security = new SecurityManager();
        var revision = security.SecurityRevision;
        security.SetSecurityContext(new SecurityContext());
        Assert.False(security.TryPublishQuery(revision, () => throw new Exception("must not run")));
        var calls = 0;
        Assert.True(security.TryPublishQuery(security.SecurityRevision, () => calls++));
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task RealUowSqliteReadAppliesCompiledDetailAndSecurityFiltersBeforePublication()
    {
        using var h = new Harness();
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        using (var seed = connection.CreateCommand())
        {
            seed.CommandText = "CREATE TABLE Rows (Id INTEGER, ParentId INTEGER, Name TEXT); INSERT INTO Rows VALUES (1,1,'allowed'),(2,1,'foreign'),(3,2,'allowed');";
            await seed.ExecuteNonQueryAsync();
        }
        h.Source.Setup(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<AppFilter>>()))
            .Returns(async (string _, List<AppFilter> filters) =>
            {
                Assert.Equal(2, filters.Count);
                using var command = connection.CreateCommand();
                var definition = h.Source.Object.BuildSelectQueryDefinition("Rows", filters);
                command.ApplyFilterQueryDefinition(definition);
                using var reader = await command.ExecuteReaderAsync();
                var rows = new List<object>();
                while (await reader.ReadAsync()) rows.Add(new Dictionary<string, object>
                    { ["Id"] = reader.GetInt64(0), ["ParentId"] = reader.GetInt64(1), ["Name"] = reader.GetString(2) });
                Assert.Same(h.Prior, h.Unit.Units);
                return rows;
            });
        var (manager, _, _) = RegisterDetail(h);
        using (manager)
        {
            manager.SetBlockSecurity("DETAIL", new BlockSecurity { RowFilterClause = "Name = :tenant",
                RowFilterValues = new Dictionary<string, object> { ["tenant"] = "allowed" } });
            var result = await manager.SynchronizeDetailBlocksWithOutcomeAsync("MASTER");
            Assert.True(result.AllDetailsCurrent);
            Assert.True(Assert.Single(result.Details).RecordsPublished);
            var row = Assert.Single(h.Unit.Units);
            Assert.Equal(1, row.Id);
            Assert.Equal("allowed", row.Name);
            Assert.False(h.Unit.IsDirty);
        }
    }
}
