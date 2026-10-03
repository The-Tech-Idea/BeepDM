using Microsoft.Data.Sqlite;
using Moq;
using TheTechIdea.Beep;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.Forms.Helpers;
using TheTechIdea.Beep.Editor.UOW;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Extensions;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using Xunit;

namespace TheTechIdea.Beep.Editor.UOWManager.Tests;

public class QueryPublicationTests
{
    public sealed class Row : Entity
    {
        private int _id;
        private int _tenantId = 42;
        private string _name = "prior";
        public int Id { get => _id; set => SetProperty(ref _id, value); }
        public int TenantId { get => _tenantId; set => SetProperty(ref _tenantId, value); }
        public string Name { get => _name; set => SetProperty(ref _name, value); }
    }

    private static EntityStructure Schema() => new()
    {
        EntityName = "Rows", Fields = new List<EntityField>
        {
            new() { FieldName = "Id", Fieldtype = "System.Int32", IsKey = true },
            new() { FieldName = "TenantId", Fieldtype = "System.Int32" },
            new() { FieldName = "Name", Fieldtype = "System.String" }
        }
    };
    private static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static async Task Finish(Task task) => await task.WaitAsync(TimeSpan.FromSeconds(10));

    private sealed class Harness : IDisposable
    {
        internal readonly Mock<IDMEEditor> Editor = new();
        internal readonly Mock<IDataSource> Source = new();
        internal readonly Mock<ITriggerManager> Triggers = new();
        internal readonly Mock<ISystemVariablesManager> Variables = new();
        internal readonly Mock<IMessageQueueManager> Messages = new();
        internal readonly UnitofWork<Row> Unit;
        internal readonly FormsManager Manager;
        internal readonly ObservableBindingList<Row> Prior;
        internal Harness(ISecurityManager? security = null)
        {
            Source.SetupGet(s => s.DatasourceType).Returns(DataSourceType.SqlLite);
            Source.SetupGet(s => s.DatasourceName).Returns("db");
            Source.Setup(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<AppFilter>>()))
                .ReturnsAsync(new object[] { new Row { Id = 8, Name = "loaded" } });
            Editor.Setup(e => e.GetDataSource("db")).Returns(Source.Object);
            Triggers.Setup(t => t.FireBlockTriggerAsync(It.IsAny<TriggerType>(), It.IsAny<string>(),
                It.IsAny<TriggerContext>(), It.IsAny<CancellationToken>())).ReturnsAsync(TriggerResult.Success);
            Prior = new ObservableBindingList<Row>(new List<Row> { new() { Id = 1 }, new() { Id = 2 } });
            Unit = new UnitofWork<Row>(Editor.Object, "db", "Rows", Schema(), "Id") { DataSource = Source.Object, Units = Prior };
            Manager = new FormsManager(Editor.Object, triggerManager: Triggers.Object, systemVariablesManager: Variables.Object,
                messageManager: Messages.Object, securityManager: security);
            Manager.RegisterBlock("ROWS", new UnitOfWorkWrapper(Unit), Schema(), "db");
            Manager.GetBlock("ROWS").Mode = DataBlockMode.CRUD;
            Editor.Invocations.Clear(); Variables.Invocations.Clear(); Messages.Invocations.Clear();
        }
        internal List<AppFilter> Filters(string value) => new() { new() { FieldName = "Name", Operator = "=", FilterValue = value } };
        public void Dispose() { Manager.Dispose(); Unit.Dispose(); Prior.Dispose(); }
    }

    [Theory]
    [InlineData("typed", DataBlockMode.CRUD)]
    [InlineData("typed", DataBlockMode.Query)]
    [InlineData("typed", DataBlockMode.EnterQuery)]
    [InlineData("basic", DataBlockMode.CRUD)]
    [InlineData("basic", DataBlockMode.Query)]
    [InlineData("basic", DataBlockMode.EnterQuery)]
    [InlineData("enhanced", DataBlockMode.CRUD)]
    [InlineData("enhanced", DataBlockMode.Query)]
    [InlineData("enhanced", DataBlockMode.EnterQuery)]
    public async Task AllQueryRoutesUseRealStagedReadWithoutPreclearing(string route, DataBlockMode mode)
    {
        using var h = new Harness();
        h.Manager.GetBlock("ROWS").Mode = mode;
        h.Source.Setup(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<AppFilter>>())).Returns(() =>
        {
            Assert.Same(h.Prior, h.Unit.Units);
            Assert.Equal(mode, h.Manager.GetBlock("ROWS").Mode);
            Assert.Equal(2, h.Unit.Units.Count);
            return Task.FromResult<IEnumerable<object>>(new object[] { new Row { Id = 8 } });
        });
        if (route == "basic") Assert.True(await h.Manager.ExecuteQueryAsync("ROWS"));
        else
        {
            var result = route == "typed" ? await h.Manager.ExecuteQueryWithOutcomeAsync("ROWS") :
                Assert.IsType<FormQueryResult>(await h.Manager.ExecuteQueryEnhancedAsync("ROWS"));
            Assert.True(result.UsedStaging);
            Assert.True(result.RecordsPublished);
            Assert.False(result.LegacyPublicationPossible);
            Assert.Equal(FormQueryState.Completed, result.State);
            Assert.Equal(Errors.Ok, result.Flag);
        }
        Assert.Equal(8, Assert.Single(h.Unit.Units).Id);
        Assert.Equal(DataBlockMode.CRUD, h.Manager.GetBlock("ROWS").Mode);
        h.Source.Verify(s => s.GetEntity(It.IsAny<string>(), It.IsAny<List<AppFilter>>()), Times.Never);
        h.Triggers.Verify(t => t.FireBlockTriggerAsync(TriggerType.EnterQuery, It.IsAny<string>(),
            It.IsAny<TriggerContext>(), It.IsAny<CancellationToken>()), Times.Never);
        h.Triggers.Verify(t => t.FireBlockTriggerAsync(TriggerType.ExitQuery, It.IsAny<string>(),
            It.IsAny<TriggerContext>(), It.IsAny<CancellationToken>()), route == "basic" && mode == DataBlockMode.EnterQuery ? Times.Once : Times.Never);
    }

    [Fact]
    public async Task ExplicitEnterQueryStillClearsAndFiresEnterTrigger()
    {
        using var h = new Harness();
        Assert.Equal(Errors.Ok, (await h.Manager.EnterQueryModeAsync("ROWS")).Flag);
        Assert.Empty(h.Unit.Units);
        Assert.Equal(DataBlockMode.EnterQuery, h.Manager.GetBlock("ROWS").Mode);
        h.Triggers.Verify(t => t.FireBlockTriggerAsync(TriggerType.EnterQuery, "ROWS", It.IsAny<TriggerContext>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("caller")]
    [InlineData("pre-query")]
    [InlineData("provider")]
    [InlineData("null")]
    [InlineData("dirty")]
    [InlineData("denied")]
    public async Task RejectedImplicitQueryRetainsRecordsCursorModeAndTimestamp(string reason)
    {
        using var h = new Harness();
        h.Prior.MoveTo(1);
        var stamp = h.Manager.GetBlock("ROWS").LastModeChange;
        using var token = new CancellationTokenSource();
        switch (reason)
        {
            case "caller": token.Cancel(); break;
            case "pre-query": h.Triggers.Setup(t => t.FireBlockTriggerAsync(TriggerType.PreQuery, "ROWS", It.IsAny<TriggerContext>(), It.IsAny<CancellationToken>())).ReturnsAsync(TriggerResult.Cancelled); break;
            case "provider": h.Source.Setup(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<AppFilter>>())).ThrowsAsync(new InvalidOperationException("provider failed")); break;
            case "null": h.Source.Setup(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<AppFilter>>())).ReturnsAsync((IEnumerable<object>)null!); break;
            case "dirty": h.Prior[0].Name = "unsaved"; break;
            case "denied": h.Manager.SetBlockSecurity("ROWS", new BlockSecurity { AllowQuery = false }); break;
        }
        if (reason == "caller") await Assert.ThrowsAnyAsync<OperationCanceledException>(() => h.Manager.ExecuteQueryWithOutcomeAsync("ROWS", cancellationToken: token.Token));
        else
        {
            var result = await h.Manager.ExecuteQueryWithOutcomeAsync("ROWS", cancellationToken: token.Token);
            Assert.Equal(Errors.Failed, result.Flag);
            Assert.False(result.RecordsPublished);
            Assert.False(result.LegacyPublicationPossible);
        }
        Assert.Same(h.Prior, h.Unit.Units);
        Assert.Equal(1, h.Unit.Units.CurrentIndex);
        Assert.Equal(2, h.Unit.Units.Count);
        Assert.Equal(DataBlockMode.CRUD, h.Manager.GetBlock("ROWS").Mode);
        Assert.Equal(stamp, h.Manager.GetBlock("ROWS").LastModeChange);
        if (reason is "caller" or "pre-query" or "dirty" or "denied")
            h.Source.Verify(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<AppFilter>>()), Times.Never);
    }

    [Theory]
    [InlineData("policy")]
    [InlineData("mode")]
    [InlineData("unregister")]
    [InlineData("rebind")]
    [InlineData("dirty")]
    public async Task LateRejectedQueryNeverPublishesCandidate(string change)
    {
        using var h = new Harness();
        var entered = Signal(); var release = Signal();
        h.Source.Setup(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<AppFilter>>())).Returns(async () =>
        { entered.SetResult(); await release.Task; return new object[] { new Row { Id = 8 } }; });
        var run = h.Manager.ExecuteQueryWithOutcomeAsync("ROWS");
        try
        {
            await Finish(entered.Task);
            Assert.Equal(1, h.Manager.PendingCallbackCount);
            switch (change)
            {
                case "policy": h.Manager.SetSecurityContext(new SecurityContext()); break;
                case "mode": h.Manager.GetBlock("ROWS").Mode = DataBlockMode.EnterQuery; break;
                case "unregister": h.Manager.UnregisterBlock("ROWS"); break;
                case "dirty": h.Prior[0].Name = "unsaved"; break;
                case "rebind":
                    var replacement = new Mock<IUnitofWork>();
                    replacement.SetupProperty(u => u.EntityStructure); replacement.SetupProperty(u => u.DataSource);
                    h.Manager.RegisterBlock("ROWS", replacement.Object, Schema()); break;
            }
        }
        finally { release.TrySetResult(); }
        var result = await run.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.NotEqual(FormQueryState.Completed, result.State);
        Assert.False(result.RecordsPublished);
        Assert.False(result.LegacyPublicationPossible);
        Assert.Same(h.Prior, h.Unit.Units);
        Assert.Equal(0, h.Manager.PendingCallbackCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancelOrCloseWaitsForProviderAcknowledgementAndKeepsPriorRows(bool close)
    {
        using var h = new Harness();
        using var token = new CancellationTokenSource();
        var entered = Signal(); var release = Signal();
        h.Source.Setup(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<AppFilter>>())).Returns(async () =>
        { entered.SetResult(); await release.Task; return new object[] { new Row { Id = 8 } }; });
        var run = h.Manager.ExecuteQueryWithOutcomeAsync("ROWS", cancellationToken: token.Token);
        Task? drain = null;
        try
        {
            await Finish(entered.Task);
            if (close) { drain = h.Manager.DisposeAsync().AsTask(); Assert.False(drain.IsCompleted); }
            else token.Cancel();
            Assert.False(run.IsCompleted);
            Assert.Same(h.Prior, h.Unit.Units);
        }
        finally { release.TrySetResult(); }
        var result = await run.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(FormQueryState.Cancelled, result.State);
        Assert.False(result.RecordsPublished);
        Assert.False(result.LegacyPublicationPossible);
        if (drain != null) await Finish(drain);
        Assert.Same(h.Prior, h.Unit.Units);
        h.Variables.Verify(v => v.SetBlockStatus(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        h.Source.Setup(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<AppFilter>>())).ReturnsAsync(Array.Empty<object>());
        using var next = await h.Unit.PrepareReadAsync(new());
    }

    [Fact]
    public async Task NewerQueuedQuerySupersedesOldCandidateAndUsesCopiedFilters()
    {
        using var h = new Harness();
        var entered = Signal(); var release = Signal();
        var calls = new List<string>();
        h.Source.Setup(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<AppFilter>>()))
            .Returns(async (string _, List<AppFilter> filters) =>
            {
                var name = Assert.Single(filters).FilterValue; calls.Add(name);
                if (calls.Count == 1) { entered.SetResult(); await release.Task; }
                Assert.Same(h.Prior, h.Unit.Units);
                return new object[] { new Row { Name = name } };
            });
        var old = h.Manager.ExecuteQueryWithOutcomeAsync("ROWS", h.Filters("old"));
        Task<FormQueryResult>? newer = null;
        try
        {
            await Finish(entered.Task);
            var filters = h.Filters("new");
            newer = h.Manager.ExecuteQueryWithOutcomeAsync("rows", filters);
            filters[0].FilterValue = "changed after capture";
            Assert.False(newer.IsCompleted);
            Assert.Equal(2, h.Manager.PendingCallbackCount);
        }
        finally { release.TrySetResult(); }
        var rejected = await old.WaitAsync(TimeSpan.FromSeconds(10));
        var accepted = await newer!.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(FormQueryState.Superseded, rejected.State);
        Assert.False(rejected.RecordsPublished);
        Assert.True(accepted.RecordsPublished);
        Assert.True(accepted.RequestRevision > rejected.RequestRevision);
        Assert.Equal(new[] { "old", "new" }, calls);
        Assert.Equal("new", Assert.Single(h.Unit.Units).Name);
    }

    [Fact]
    public async Task SameNameReplacementCannotReuseQueryOutcomeIdentity()
    {
        using var h = new Harness();
        var before = await h.Manager.ExecuteQueryWithOutcomeAsync("rows");
        h.Manager.RegisterBlock("ROWS", new UnitOfWorkWrapper(h.Unit), Schema(), "db");
        var after = await h.Manager.ExecuteQueryWithOutcomeAsync("rows");
        Assert.True(before.RecordsPublished);
        Assert.True(after.RecordsPublished);
        Assert.Equal("ROWS", before.BlockName);
        Assert.Equal(before.FormInstanceId, after.FormInstanceId);
        Assert.NotEqual(before.RegistrationId, after.RegistrationId);
        Assert.Equal(1, before.RequestRevision);
        Assert.Equal(1, after.RequestRevision);
    }

    [Fact]
    public async Task QueuedCancellationDoesNotReachProviderOrReviveSupersededRead()
    {
        using var h = new Harness();
        using var token = new CancellationTokenSource();
        var entered = Signal(); var release = Signal();
        h.Source.Setup(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<AppFilter>>())).Returns(async () =>
        { entered.SetResult(); await release.Task; return Array.Empty<object>(); });
        var old = h.Manager.ExecuteQueryWithOutcomeAsync("ROWS");
        try
        {
            await Finish(entered.Task);
            var queued = h.Manager.ExecuteQueryWithOutcomeAsync("ROWS", cancellationToken: token.Token);
            token.Cancel();
            Assert.Equal(FormQueryState.Cancelled, (await queued.WaitAsync(TimeSpan.FromSeconds(10))).State);
        }
        finally { release.TrySetResult(); }
        Assert.Equal(FormQueryState.Superseded, (await old.WaitAsync(TimeSpan.FromSeconds(10))).State);
        Assert.Same(h.Prior, h.Unit.Units);
        h.Source.Verify(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<AppFilter>>()), Times.Once);
    }

    [Fact]
    public async Task QueryAndDetailReadsShareOrderingWithoutOverlappingRealUowAdmission()
    {
        using var h = new Harness();
        var master = new Mock<IUnitofWork>();
        master.SetupProperty(u => u.EntityStructure); master.SetupProperty(u => u.DataSource);
        master.SetupGet(u => u.CurrentItem).Returns(new Row { Id = 1 });
        h.Manager.RegisterBlock("MASTER", master.Object, Schema());
        h.Manager.CreateMasterDetailRelation("MASTER", "ROWS", "Id", "Id");
        var entered = Signal(); var release = Signal();
        var calls = 0;
        h.Source.Setup(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<AppFilter>>())).Returns(async () =>
        { if (++calls == 1) { entered.SetResult(); await release.Task; } return new object[] { new Row { Id = calls } }; });
        var query = h.Manager.ExecuteQueryWithOutcomeAsync("ROWS");
        Task<DetailSynchronizationResult>? detail = null;
        try
        {
            await Finish(entered.Task);
            detail = h.Manager.SynchronizeDetailBlocksWithOutcomeAsync("MASTER");
            Assert.False(detail.IsCompleted);
            Assert.Equal(1, calls);
        }
        finally { release.TrySetResult(); }
        Assert.True((await query.WaitAsync(TimeSpan.FromSeconds(10))).RecordsPublished);
        Assert.True((await detail!.WaitAsync(TimeSpan.FromSeconds(10))).AllDetailsCurrent);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task DirtyCapturedDetailBlocksPrimaryQueryBeforeProvider()
    {
        using var h = new Harness();
        var child = new Mock<IUnitofWork>();
        child.SetupProperty(u => u.EntityStructure); child.SetupProperty(u => u.DataSource);
        child.SetupGet(u => u.IsDirty).Returns(true);
        h.Manager.RegisterBlock("CHILD", child.Object, Schema());
        h.Manager.CreateMasterDetailRelation("ROWS", "CHILD", "Id", "Id");
        var result = await h.Manager.ExecuteQueryWithOutcomeAsync("ROWS");
        Assert.Equal(FormQueryState.BlockedDirty, result.State);
        Assert.Same(h.Prior, h.Unit.Units);
        h.Source.Verify(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<AppFilter>>()), Times.Never);
    }

    [Fact]
    public async Task PostPublicationObserverFailuresDoNotTurnAcceptedRowsIntoFailedRead()
    {
        using var h = new Harness();
        h.Unit.PostQuery += (_, _) => throw new InvalidOperationException("UoW observer");
        h.Triggers.Setup(t => t.FireBlockTriggerAsync(TriggerType.PostQuery, "ROWS", It.IsAny<TriggerContext>(),
            It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("Forms observer"));
        var result = await h.Manager.ExecuteQueryWithOutcomeAsync("ROWS");
        Assert.Equal(Errors.Ok, result.Flag);
        Assert.Equal(FormQueryState.Completed, result.State);
        Assert.True(result.RecordsPublished);
        Assert.Equal(2, result.NotificationFailures.Count);
        Assert.Equal(8, Assert.Single(h.Unit.Units).Id);
    }

    [Fact]
    public async Task ReentrantCloseAfterPublicationRetainsEvidenceAndSkipsLaterNotifications()
    {
        using var h = new Harness();
        h.Unit.PostQuery += (_, _) => h.Manager.Dispose();
        var result = await h.Manager.ExecuteQueryWithOutcomeAsync("ROWS");
        Assert.True(result.RecordsPublished);
        Assert.Equal(FormQueryState.Completed, result.State);
        Assert.Equal(Errors.Ok, result.Flag);
        Assert.Single(result.NotificationFailures);
        h.Variables.Verify(v => v.SetBlockStatus(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        h.Triggers.Verify(t => t.FireBlockTriggerAsync(TriggerType.PostQuery, It.IsAny<string>(),
            It.IsAny<TriggerContext>(), It.IsAny<CancellationToken>()), Times.Never);
        await h.Manager.DisposeAsync();
    }

    [Fact]
    public async Task ReadTriggerCannotAwaitNestedQueryDetailOrItsOwnDrain()
    {
        using var h = new Harness();
        h.Triggers.Setup(t => t.FireBlockTriggerAsync(TriggerType.PreQuery, "ROWS", It.IsAny<TriggerContext>(),
            It.IsAny<CancellationToken>())).Returns(async () =>
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => h.Manager.ExecuteQueryWithOutcomeAsync("ROWS"));
            await Assert.ThrowsAsync<InvalidOperationException>(() => h.Manager.SynchronizeDetailBlocksWithOutcomeAsync("ROWS"));
            Assert.Throws<InvalidOperationException>(() => { _ = h.Manager.WaitForPendingCallbacksAsync(); });
            return TriggerResult.Success;
        });
        Assert.True((await h.Manager.ExecuteQueryWithOutcomeAsync("ROWS").WaitAsync(TimeSpan.FromSeconds(10))).RecordsPublished);
    }

    [Fact]
    public async Task CustomSecurityWithoutPublicationGateFailsBeforeReadAndModeChange()
    {
        var security = new Mock<ISecurityManager>();
        security.Setup(s => s.IsBlockAllowed(It.IsAny<string>(), It.IsAny<SecurityPermission>())).Returns(true);
        using var h = new Harness(security.Object);
        var result = await h.Manager.ExecuteQueryWithOutcomeAsync("ROWS");
        Assert.Equal(FormQueryState.Failed, result.State);
        Assert.False(result.RecordsPublished);
        Assert.Same(h.Prior, h.Unit.Units);
        Assert.Equal(DataBlockMode.CRUD, h.Manager.GetBlock("ROWS").Mode);
        h.Source.Verify(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<AppFilter>>()), Times.Never);
    }

    [Fact]
    public async Task LegacyNullReadReportsUnknownPublicationInsteadOfEmptySuccess()
    {
        var editor = new Mock<IDMEEditor>();
        using var manager = new FormsManager(editor.Object);
        var unit = new Mock<IUnitofWork>();
        unit.SetupProperty(u => u.EntityStructure); unit.SetupProperty(u => u.DataSource);
        manager.RegisterBlock("ROWS", unit.Object, Schema());
        var result = await manager.ExecuteQueryWithOutcomeAsync("ROWS");
        Assert.Equal(FormQueryState.Failed, result.State);
        Assert.True(result.LegacyPublicationPossible);
        Assert.False(result.RecordsPublished);
        Assert.False(result.UsedStaging);
    }

    [Theory]
    [InlineData("policy")]
    [InlineData("retire")]
    public async Task LastAuthorizationBoundaryRejectsPolicyOrRegistrationChange(string change)
    {
        var owned = new SecurityManager();
        var security = new Mock<ISecurityManager>();
        security.Setup(s => s.IsBlockAllowed(It.IsAny<string>(), It.IsAny<SecurityPermission>())).Returns(true);
        security.Setup(s => s.GetBlockSecurity(It.IsAny<string>())).Returns((string name) => owned.GetBlockSecurity(name));
        var publication = security.As<IQuerySecurityPublication>();
        publication.SetupGet(s => s.SecurityRevision).Returns(() => owned.SecurityRevision);
        publication.Setup(s => s.CaptureQuerySecurity(It.IsAny<string>())).Returns((string name) => owned.CaptureQuerySecurity(name));
        using var h = new Harness(security.Object);
        publication.Setup(s => s.TryPublishQuery(It.IsAny<long>(), It.IsAny<Action>())).Returns((long revision, Action publish) =>
        {
            if (change == "policy") owned.SetSecurityContext(new SecurityContext());
            else h.Manager.UnregisterBlock("ROWS");
            return owned.TryPublishQuery(revision, publish);
        });
        var result = await h.Manager.ExecuteQueryWithOutcomeAsync("ROWS");
        Assert.NotEqual(FormQueryState.Completed, result.State);
        Assert.False(result.RecordsPublished);
        Assert.Same(h.Prior, h.Unit.Units);
        h.Source.Verify(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<AppFilter>>()), Times.Once);
    }

    [Fact]
    public async Task ClaimedStagingWithoutPublicationAcknowledgementFailsClosed()
    {
        using var manager = new FormsManager(new Mock<IDMEEditor>().Object);
        var unit = new Mock<IUnitofWork>();
        unit.SetupProperty(u => u.EntityStructure); unit.SetupProperty(u => u.DataSource);
        var reader = unit.As<IStagedUnitofWorkRead>();
        reader.SetupGet(r => r.SupportsStagedRead).Returns(true);
        var stage = new Mock<IUnitofWorkReadStage>();
        reader.Setup(r => r.PrepareReadAsync(It.IsAny<List<AppFilter>>(), It.IsAny<CancellationToken>())).ReturnsAsync(stage.Object);
        manager.RegisterBlock("ROWS", unit.Object, Schema());
        var result = await manager.ExecuteQueryWithOutcomeAsync("ROWS");
        Assert.True(result.UsedStaging);
        Assert.False(result.RecordsPublished);
        Assert.Equal(FormQueryState.Failed, result.State);
        unit.Verify(u => u.Get(), Times.Never);
        unit.Verify(u => u.Get(It.IsAny<List<AppFilter>>()), Times.Never);
        stage.Verify(s => s.Dispose(), Times.Once);
    }

    [Fact]
    public async Task LegacyCancelledReadRetainsPossiblePublicationEvidence()
    {
        using var manager = new FormsManager(new Mock<IDMEEditor>().Object);
        var unit = new Mock<IUnitofWork>();
        unit.SetupProperty(u => u.EntityStructure); unit.SetupProperty(u => u.DataSource);
        manager.RegisterBlock("ROWS", unit.Object, Schema());
        var entered = Signal(); var release = Signal();
        unit.Setup(u => u.Get()).Returns(async () =>
        { entered.SetResult(); await release.Task; return (object)new List<object>(); });
        using var token = new CancellationTokenSource();
        var run = manager.ExecuteQueryWithOutcomeAsync("ROWS", cancellationToken: token.Token);
        try { await Finish(entered.Task); token.Cancel(); Assert.False(run.IsCompleted); }
        finally { release.TrySetResult(); }
        var result = await run.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(FormQueryState.Cancelled, result.State);
        Assert.True(result.LegacyPublicationPossible);
        Assert.False(result.RecordsPublished);
    }

    [Fact]
    public async Task ModeWrapperRetainsPublishedEvidenceAcrossReentrantClose()
    {
        using var h = new Harness();
        h.Manager.GetBlock("ROWS").Mode = DataBlockMode.Query;
        h.Unit.PostQuery += (_, _) => h.Manager.Dispose();
        var result = Assert.IsType<FormQueryResult>(await h.Manager.ExecuteQueryAndEnterCrudModeAsync("ROWS"));
        Assert.True(result.ReadAcknowledged);
        Assert.True(result.RecordsPublished);
        Assert.Equal(Errors.Ok, result.Flag);
        Assert.NotEmpty(result.NotificationFailures);
        h.Variables.Verify(v => v.SetBlockStatus(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        await h.Manager.DisposeAsync();
    }

    [Fact]
    public async Task BasicCloseInsideMessageObserverStopsExitQueryAndDrains()
    {
        using var h = new Harness();
        h.Manager.GetBlock("ROWS").Mode = DataBlockMode.EnterQuery;
        h.Messages.Setup(m => m.ShowInfoMessage("ROWS", It.IsAny<string>())).Callback(() => h.Manager.Dispose());
        Assert.True(await h.Manager.ExecuteQueryAsync("ROWS"));
        h.Triggers.Verify(t => t.FireBlockTriggerAsync(TriggerType.ExitQuery, It.IsAny<string>(),
            It.IsAny<TriggerContext>(), It.IsAny<CancellationToken>()), Times.Never);
        await h.Manager.DisposeAsync();
        Assert.Equal(0, h.Manager.PendingCallbackCount);
    }

    [Fact]
    public async Task ActualSqlitePrimaryReadCombinesCallerDefaultAndPolicyBeforePublication()
    {
        using var h = new Harness();
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        using (var seed = connection.CreateCommand())
        {
            seed.CommandText = "CREATE TABLE Rows (Id INTEGER, TenantId INTEGER, Name TEXT); INSERT INTO Rows VALUES (1,42,'allowed'),(2,99,'allowed'),(3,42,'other');";
            await seed.ExecuteNonQueryAsync();
        }
        h.Manager.SetDefaultWhere("ROWS", "Id > 0");
        h.Manager.SetBlockSecurity("ROWS", new BlockSecurity { RowFilterClause = "TenantId = :tenant",
            RowFilterValues = new Dictionary<string, object> { ["tenant"] = 42 } });
        h.Source.Setup(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<AppFilter>>()))
            .Returns(async (string name, List<AppFilter> filters) =>
            {
                Assert.Equal(3, filters.Count);
                using var command = connection.CreateCommand();
                command.ApplyFilterQueryDefinition(h.Source.Object.BuildSelectQueryDefinition(name, filters));
                using var reader = await command.ExecuteReaderAsync();
                var rows = new List<object>();
                while (await reader.ReadAsync()) rows.Add(new Dictionary<string, object>
                    { ["Id"] = reader.GetInt64(0), ["TenantId"] = reader.GetInt64(1), ["Name"] = reader.GetString(2) });
                Assert.Same(h.Prior, h.Unit.Units);
                return rows;
            });
        var result = await h.Manager.ExecuteQueryWithOutcomeAsync("ROWS", h.Filters("allowed"));
        Assert.True(result.RecordsPublished);
        Assert.Equal(Errors.Ok, result.Flag);
        Assert.Equal(1, Assert.Single(h.Unit.Units).Id);
        Assert.False(h.Unit.IsDirty);
    }
}
