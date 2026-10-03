using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Moq;
using TheTechIdea.Beep;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOW;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Extensions;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using Xunit;

namespace TheTechIdea.Beep.Editor.UOWManager.Tests;

public class ProviderPagingTests
{
    public sealed class Row : Entity
    {
        private int _id, _tenantId = 42;
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

    private static BoundedPageRequest Request(long page = 1, int bytes = 4096, IEnumerable<PageOrder>? order = null,
        IEnumerable<AppFilter>? filters = null) => new(page, 2, bytes, order, filters);
    private static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static BoundedPageResponse Response(BoundedPageRequest request, string json, long total = 2) =>
        new(request.RequestId, request.PageNumber, request.PageSize, total, Encoding.UTF8.GetBytes(json));
    private const string TwoRows = "[{\"Id\":8,\"TenantId\":42,\"Name\":\"new\"},{\"Id\":9,\"TenantId\":42,\"Name\":\"new\"}]";

    private sealed class Harness : IDisposable
    {
        internal readonly Mock<IDMEEditor> Editor = new();
        internal readonly Mock<IDataSource> Source = new();
        internal readonly Mock<ITriggerManager> Triggers = new();
        internal readonly Mock<ISystemVariablesManager> Variables = new();
        internal readonly UnitofWork<Row> Unit;
        internal readonly FormsManager Manager;
        internal readonly ObservableBindingList<Row> Prior;
        internal Func<BoundedPageRequest, CancellationToken, Task<BoundedPageResponse>> Read =
            (request, _) => Task.FromResult(Response(request, TwoRows));
        internal BoundedPageRequest? Last;
        internal int Calls;

        internal Harness(bool capability = true)
        {
            if (capability)
                Source.As<IBoundedPagedDataSource>().Setup(s => s.ReadPageAsync(It.IsAny<string>(),
                    It.IsAny<BoundedPageRequest>(), It.IsAny<CancellationToken>())).Returns((string _, BoundedPageRequest request, CancellationToken token) =>
                    { Calls++; Last = request; return Read(request, token); });
            Source.SetupGet(s => s.DatasourceType).Returns(DataSourceType.SqlLite);
            Source.SetupGet(s => s.DatasourceName).Returns("db");
            Source.Setup(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<AppFilter>>()))
                .ReturnsAsync(new object[] { new Row { Id = 99, Name = "query" } });
            Editor.Setup(e => e.GetDataSource("db")).Returns(Source.Object);
            Triggers.Setup(t => t.FireBlockTriggerAsync(It.IsAny<TriggerType>(), It.IsAny<string>(),
                It.IsAny<TriggerContext>(), It.IsAny<CancellationToken>())).ReturnsAsync(TriggerResult.Success);
            Prior = new ObservableBindingList<Row>(new List<Row> { new() { Id = 1 }, new() { Id = 2 } });
            Unit = new UnitofWork<Row>(Editor.Object, "db", "Rows", Schema(), "Id") { DataSource = Source.Object, Units = Prior };
            Manager = new FormsManager(Editor.Object, triggerManager: Triggers.Object, systemVariablesManager: Variables.Object);
            Manager.RegisterBlock("ROWS", new UnitOfWorkWrapper(Unit), Schema(), "db");
            Block.Mode = DataBlockMode.CRUD;
            Block.Configuration.PageSize = 2;
        }
        internal DataBlockInfo Block => Manager.GetBlock("ROWS");
        internal Task<FormQueryResult> Fetch(BoundedPageRequest? request = null, CancellationToken token = default) =>
            Manager.FetchPageWithOutcomeAsync("ROWS", request ?? Request(), token);
        internal void AssertNoFallback()
        {
            Source.Verify(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<AppFilter>>()), Times.Never);
            Source.Verify(s => s.GetEntity(It.IsAny<string>(), It.IsAny<List<AppFilter>>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }
        public void Dispose() { Manager.Dispose(); Unit.Dispose(); Prior.Dispose(); }
    }

    [Fact]
    public void RequestsAndResponsesOwnValuesAndCheckLongMath()
    {
        var filters = new List<AppFilter> { new() { FieldName = "Name", Operator = "=", FilterValue = "before" } };
        var order = new List<PageOrder> { new("Name", true) };
        var request = Request((long)int.MaxValue + 5, order: order, filters: filters);
        filters[0].FilterValue = "after"; order.Clear();
        request.CopyFilters()[0].FilterValue = "escaped";
        Assert.Equal("before", Assert.Single(request.CopyFilters()).FilterValue);
        Assert.True(Assert.Single(request.Order).Descending);
        Assert.Equal(((long)int.MaxValue + 4) * 2, request.Offset);
        Assert.Throws<OverflowException>(() => Request(long.MaxValue));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BoundedPageRequest(1, 5001, 4096));
        Assert.Throws<ArgumentOutOfRangeException>(() => Request(bytes: BoundedPageRequest.MaximumPayloadBytes + 1));
        var payload = Encoding.UTF8.GetBytes("[]");
        var response = new BoundedPageResponse(request.RequestId, request.PageNumber, 2, 0, payload);
        payload[0] = (byte)'x'; response.CopyPayload()[0] = (byte)'y';
        Assert.Equal("[]", Encoding.UTF8.GetString(response.CopyPayload()));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BoundedPageResponse(Guid.NewGuid(), 1, 2, 0,
            new byte[BoundedPageRequest.MaximumPayloadBytes + 1]));
    }

    [Fact]
    public async Task ActualUowAndWrapperStageBeforeAcknowledgementWithoutLocalPageCoercion()
    {
        using var h = new Harness();
        var requested = Request((long)int.MaxValue + 5, order: new[] { new PageOrder("Name", true) });
        h.Read = (request, _) =>
        {
            Assert.Same(h.Prior, h.Unit.Units);
            Assert.Equal(DataBlockMode.CRUD, h.Block.Mode);
            Assert.Equal(new[] { "Name", "Id" }, request.Order.Select(o => o.FieldName));
            Assert.True(request.Order[0].Descending);
            return Task.FromResult(Response(request, TwoRows, request.Offset + 2));
        };
        var result = await h.Fetch(requested);
        Assert.Equal(FormQueryState.Completed, result.State);
        Assert.True(result.RecordsPublished);
        Assert.True(result.UsedStaging);
        Assert.False(result.LegacyPublicationPossible);
        Assert.Equal(requested.PageNumber, result.ProviderPage.PageNumber);
        Assert.Equal(requested.Offset + 2, result.ProviderPage.TotalRecords);
        Assert.Equal(requested.PageNumber, result.ProviderPage.TotalPages);
        Assert.Equal(2, result.ProviderPage.LoadedRecords);
        Assert.Equal(1, h.Block.CurrentPage);
        Assert.Equal(new[] { 8, 9 }, h.Unit.Units.Select(row => row.Id));
        Assert.False(h.Unit.IsDirty);
        Assert.True(h.Manager.TryCaptureBindingTarget("ROWS", out var target));
        Assert.True(h.Manager.IsBindingTargetCurrent(target));
        h.Variables.Verify(v => v.SetLastQuery(It.IsAny<string>()), Times.Never);
        h.AssertNoFallback();
    }

    [Theory]
    [InlineData("id")]
    [InlineData("page")]
    [InlineData("size")]
    [InlineData("count")]
    [InlineData("bytes")]
    [InlineData("rows")]
    [InlineData("short")]
    [InlineData("json")]
    [InlineData("object")]
    [InlineData("null-row")]
    [InlineData("unknown")]
    [InlineData("duplicate-field")]
    [InlineData("nested")]
    [InlineData("missing")]
    [InlineData("null-value")]
    [InlineData("duplicate-key")]
    [InlineData("wrong-type")]
    [InlineData("null-response")]
    [InlineData("utf8")]
    public async Task MalformedProviderNeverPublishesOrReplaysUnboundedRead(string fault)
    {
        using var h = new Harness();
        h.Read = (request, _) => Task.FromResult(fault switch
        {
            "id" => new BoundedPageResponse(Guid.NewGuid(), 1, 2, 2, Encoding.UTF8.GetBytes(TwoRows)),
            "page" => new BoundedPageResponse(request.RequestId, 2, 2, 2, Encoding.UTF8.GetBytes(TwoRows)),
            "size" => new BoundedPageResponse(request.RequestId, 1, 3, 2, Encoding.UTF8.GetBytes(TwoRows)),
            "count" => Response(request, TwoRows, -1),
            "bytes" => Response(request, TwoRows + new string(' ', 4096)),
            "rows" => Response(request, TwoRows[..^1] + ",{\"Id\":10,\"TenantId\":42,\"Name\":\"third\"}]", 3),
            "short" => Response(request, "[]"),
            "json" => Response(request, "["),
            "object" => Response(request, "{}"),
            "null-row" => Response(request, "[null,null]"),
            "unknown" => Response(request, TwoRows.Replace("\"Id\":8", "\"Other\":8")),
            "duplicate-field" => Response(request, TwoRows.Replace("\"Id\":8", "\"Id\":8,\"id\":8")),
            "nested" => Response(request, TwoRows.Replace("\"new\"", "{}")),
            "missing" => Response(request, TwoRows.Replace("\"TenantId\":42,", "")),
            "null-value" => Response(request, TwoRows.Replace("\"Id\":8", "\"Id\":null")),
            "duplicate-key" => Response(request, TwoRows.Replace("\"Id\":9", "\"Id\":8")),
            "wrong-type" => Response(request, TwoRows.Replace("\"Id\":8", "\"Id\":\"wrong\"")),
            "null-response" => null!,
            "utf8" => new BoundedPageResponse(request.RequestId, 1, 2, 2,
                Encoding.UTF8.GetBytes(TwoRows).Select(value => value == (byte)'n' ? (byte)0xff : value).ToArray()),
            _ => throw new InvalidOperationException()
        });
        var result = await h.Fetch();
        Assert.Equal(FormQueryState.Failed, result.State);
        Assert.False(result.RecordsPublished);
        Assert.Same(h.Prior, h.Unit.Units);
        Assert.Equal(DataBlockMode.CRUD, h.Block.Mode);
        h.AssertNoFallback();
        h.Read = (request, _) => Task.FromResult(Response(request, TwoRows));
        Assert.True((await h.Fetch()).RecordsPublished); // Rejection released read/commit admission.
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task CountShrinkReportsOutOfRangeWithoutReplacingOrClampingImplicitly(long count)
    {
        using var h = new Harness();
        h.Read = (request, _) => Task.FromResult(Response(request, "[]", count));
        var result = await h.Fetch(Request(4));
        Assert.Equal(FormQueryState.PageOutOfRange, result.State);
        Assert.Equal(count, result.ProviderPage.TotalRecords);
        Assert.True(result.ProviderPage.IsOutOfRange);
        Assert.False(result.RecordsPublished);
        Assert.Same(h.Prior, h.Unit.Units);
        Assert.Equal(1, h.Calls);
    }

    [Fact]
    public async Task KnownEmptyFirstPageIsAcceptedAndOwnedStageCannotPublishOutOfRange()
    {
        using var h = new Harness();
        h.Read = (request, _) => Task.FromResult(Response(request, "[]", 0));
        Assert.True((await h.Fetch()).RecordsPublished);
        Assert.Empty(h.Unit.Units);
        using var stage = await h.Unit.PreparePageReadAsync(Request(4));
        Assert.True(stage.Page.IsOutOfRange);
        Assert.Throws<InvalidOperationException>(() => stage.Publish(publish => publish()));
    }

    [Theory]
    [InlineData("capability")]
    [InlineData("disabled")]
    [InlineData("mismatch")]
    [InlineData("max-fetch")]
    [InlineData("max-records")]
    [InlineData("raw-order")]
    [InlineData("order-column")]
    [InlineData("order-duplicate")]
    [InlineData("no-key")]
    [InlineData("denied")]
    [InlineData("policy")]
    [InlineData("dirty")]
    [InlineData("trigger")]
    public async Task InvalidAdmissionFailsBeforeProvider(string fault)
    {
        using var h = new Harness(fault != "capability");
        var request = Request();
        switch (fault)
        {
            case "disabled": h.Block.Configuration.PageSize = 0; break;
            case "mismatch": h.Block.Configuration.PageSize = 3; break;
            case "max-fetch": h.Block.Configuration.MaxRecordsPerFetch = 1; break;
            case "max-records": h.Block.Configuration.MaxRecords = 1; break;
            case "raw-order": h.Block.DefaultOrderByClause = "Id DESC"; break;
            case "order-column": request = Request(order: new[] { new PageOrder("Id; DROP TABLE Rows") }); break;
            case "order-duplicate": request = Request(order: new[] { new PageOrder("Id"), new PageOrder("id") }); break;
            case "no-key": h.Block.EntityStructure.Fields[0].IsKey = false; break;
            case "denied": h.Manager.SetBlockSecurity("ROWS", new BlockSecurity { AllowQuery = false }); break;
            case "policy": h.Manager.SetBlockSecurity("ROWS", new BlockSecurity { RowFilterClause = "TenantId = :missing" }); break;
            case "dirty": h.Prior[0].Name = "unsaved"; break;
            case "trigger": h.Triggers.Setup(t => t.FireBlockTriggerAsync(TriggerType.PreQuery, "ROWS", It.IsAny<TriggerContext>(),
                It.IsAny<CancellationToken>())).ReturnsAsync(TriggerResult.Cancelled); break;
        }
        var result = await h.Fetch(request);
        Assert.False(result.RecordsPublished);
        Assert.NotEqual(FormQueryState.Completed, result.State);
        Assert.Equal(0, h.Calls);
        Assert.Same(h.Prior, h.Unit.Units);
        h.AssertNoFallback();
    }

    [Theory]
    [InlineData("policy")]
    [InlineData("config")]
    [InlineData("config-replace")]
    [InlineData("default-order")]
    [InlineData("keys")]
    [InlineData("unit-keys")]
    [InlineData("dirty")]
    [InlineData("unregister")]
    public async Task InFlightTargetOrPolicyChangeCannotReplacePriorBuffer(string change)
    {
        using var h = new Harness();
        var entered = Signal(); var release = Signal();
        h.Read = async (request, _) => { entered.TrySetResult(); await release.Task; return Response(request, TwoRows); };
        var fetch = h.Fetch();
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        switch (change)
        {
            case "policy": h.Manager.SetBlockSecurity("ROWS", new BlockSecurity { AllowQuery = false }); break;
            case "config": h.Block.Configuration.MaxRecordsPerFetch = 1; break;
            case "config-replace": h.Block.Configuration = new BlockConfiguration { PageSize = 2 }; break;
            case "default-order": h.Block.DefaultOrderByClause = "Id DESC"; break;
            case "keys": h.Block.EntityStructure.Fields[0].IsKey = false; break;
            case "unit-keys": h.Unit.EntityStructure.Fields[0].IsKey = false; break;
            case "dirty": h.Prior[0].Name = "unsaved"; break;
            case "unregister": h.Manager.UnregisterBlock("ROWS"); break;
        }
        release.TrySetResult();
        var result = await fetch.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.False(result.RecordsPublished);
        Assert.Same(h.Prior, h.Unit.Units);
        h.AssertNoFallback();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancellationAndCloseAwaitPhysicalProviderCompletion(bool close)
    {
        using var h = new Harness();
        using var cancellation = new CancellationTokenSource();
        var entered = Signal(); var release = Signal();
        h.Read = async (request, _) => { entered.TrySetResult(); await release.Task; return Response(request, TwoRows); };
        var fetch = h.Fetch(token: cancellation.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Task? drain = null;
        if (close) drain = h.Manager.DisposeAsync().AsTask(); else cancellation.Cancel();
        Assert.False(fetch.IsCompleted);
        if (drain != null) Assert.False(drain.IsCompleted);
        release.TrySetResult();
        Assert.Equal(FormQueryState.Cancelled, (await fetch.WaitAsync(TimeSpan.FromSeconds(10))).State);
        if (drain != null) await drain.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Same(h.Prior, h.Unit.Units);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LaterPageOrNormalQuerySharesOrderingAndSupersedesUnpublishedPage(bool normalQuery)
    {
        using var h = new Harness();
        var entered = Signal(); var release = Signal();
        h.Read = async (request, _) => { entered.TrySetResult(); await release.Task; return Response(request, TwoRows, 4); };
        var old = h.Fetch();
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var newer = normalQuery ? h.Manager.ExecuteQueryWithOutcomeAsync("ROWS") : h.Fetch(Request(2));
        Assert.False(newer.IsCompleted);
        release.TrySetResult();
        Assert.Equal(FormQueryState.Superseded, (await old.WaitAsync(TimeSpan.FromSeconds(10))).State);
        var current = await newer.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.True(current.RecordsPublished);
        Assert.Equal(normalQuery ? 99 : 8, h.Unit.Units[0].Id);
        Assert.Equal(normalQuery ? 1 : 2, h.Calls);
    }

    [Fact]
    public async Task AcceptedPageRemainsAcceptedWhenPostQueryObserverFails()
    {
        using var h = new Harness();
        h.Unit.PostQuery += (_, _) => throw new InvalidOperationException("observer");
        var result = await h.Fetch();
        Assert.Equal(FormQueryState.Completed, result.State);
        Assert.True(result.RecordsPublished);
        Assert.Contains(result.NotificationFailures, error => error.Message == "observer");
        Assert.Equal(8, h.Unit.Units[0].Id);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DirtyDetailBlocksPageBeforeFetchOrBeforePublication(bool later)
    {
        using var h = new Harness();
        var dirty = !later;
        var child = new Mock<IUnitofWork>();
        child.SetupProperty(unit => unit.EntityStructure); child.SetupProperty(unit => unit.DataSource);
        child.SetupGet(unit => unit.IsDirty).Returns(() => dirty);
        h.Manager.RegisterBlock("CHILD", child.Object, Schema());
        h.Manager.CreateMasterDetailRelation("ROWS", "CHILD", "Id", "Id");
        h.Read = (request, _) => { dirty = true; return Task.FromResult(Response(request, TwoRows)); };
        var result = await h.Fetch();
        Assert.Equal(FormQueryState.BlockedDirty, result.State);
        Assert.False(result.RecordsPublished);
        Assert.Equal(later ? 1 : 0, h.Calls);
        Assert.Same(h.Prior, h.Unit.Units);
    }

    [Fact]
    public async Task AuthoritativeUowTenantFilterCannotBeReplacedByCallerPredicate()
    {
        using var h = new Harness();
        h.Unit.ScopeToTenant("TenantId", "42");
        h.Read = (request, _) =>
        {
            Assert.Equal(new[] { "43", "42" }, request.CopyFilters().Where(filter => filter.FieldName == "TenantId").Select(filter => filter.FilterValue));
            return Task.FromResult(Response(request, "[]", 0));
        };
        var result = await h.Fetch(Request(filters: new[] { new AppFilter { FieldName = "TenantId", Operator = "=", FilterValue = "43" } }));
        Assert.True(result.RecordsPublished);
        Assert.Empty(h.Unit.Units);
        Assert.True(h.Manager.TryCaptureBindingTarget("ROWS", out var target));
        Assert.True(h.Manager.IsBindingTargetCurrent(target));
    }

    private sealed class OverrideReader(IDMEEditor editor, bool overridePreparation) : UnitofWork<Row>(editor, "db", "Rows", Schema(), "Id")
    {
        public override Task<IUnitofWorkReadStage> PrepareReadAsync(List<AppFilter> filters, CancellationToken cancellationToken = default) =>
            overridePreparation ? throw new InvalidOperationException("Custom preparation must not be bypassed.") : base.PrepareReadAsync(filters, cancellationToken);
    }

    [Fact]
    public async Task InheritedPageCapabilityCannotBypassOverriddenReadPreparation()
    {
        using var h = new Harness();
        using var custom = new OverrideReader(h.Editor.Object, true) { DataSource = h.Source.Object };
        Assert.False(custom.SupportsStagedPageRead);
        var wrapper = new UnitOfWorkWrapper(custom);
        Assert.False(wrapper.SupportsStagedPageRead);
        await Assert.ThrowsAsync<NotSupportedException>(() => custom.PreparePageReadAsync(Request()));
        Assert.Throws<NotSupportedException>(() => { _ = wrapper.PreparePageReadAsync(Request()); });
        Assert.Equal(0, h.Calls);
    }

    [Fact]
    public async Task QueuedPageCancellationDoesNotFetchOrReviveSupersededPage()
    {
        using var h = new Harness();
        using var cancellation = new CancellationTokenSource();
        var entered = Signal(); var release = Signal();
        h.Read = async (request, _) => { entered.TrySetResult(); await release.Task; return Response(request, TwoRows); };
        var prior = h.Fetch();
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var queued = h.Fetch(token: cancellation.Token);
        cancellation.Cancel();
        Assert.Equal(FormQueryState.Cancelled, (await queued.WaitAsync(TimeSpan.FromSeconds(10))).State);
        Assert.Equal(1, h.Calls);
        release.TrySetResult();
        Assert.Equal(FormQueryState.Superseded, (await prior.WaitAsync(TimeSpan.FromSeconds(10))).State);
        Assert.Same(h.Prior, h.Unit.Units);
    }

    [Fact]
    public async Task ProviderCannotAwaitNestedReadOrManagerDrain()
    {
        using var h = new Harness();
        h.Read = async (request, token) =>
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => h.Fetch());
            await Assert.ThrowsAsync<InvalidOperationException>(() => h.Manager.ExecuteQueryWithOutcomeAsync("ROWS"));
            Assert.Throws<InvalidOperationException>(() => { _ = h.Manager.WaitForPendingCallbacksAsync(); });
            return Response(request, TwoRows);
        };
        Assert.True((await h.Fetch()).RecordsPublished);
    }

    [Fact]
    public async Task CompositeKeysAreCompletedAndDistinctTuplesAreAccepted()
    {
        using var h = new Harness();
        h.Block.EntityStructure.Fields[1].IsKey = h.Unit.EntityStructure.Fields[1].IsKey = true;
        h.Read = (request, _) =>
        {
            Assert.Equal(new[] { "Id", "TenantId" }, request.Order.Select(column => column.FieldName));
            return Task.FromResult(Response(request, TwoRows.Replace("\"Id\":9,\"TenantId\":42", "\"Id\":8,\"TenantId\":43")));
        };
        Assert.True((await h.Fetch(Request(order: new[] { new PageOrder("Id", true) }))).RecordsPublished);
        Assert.Equal(new[] { 42, 43 }, h.Unit.Units.Select(row => row.TenantId));
    }

    private sealed class LimitedPayloadStream(int limit) : MemoryStream
    {
        private void Check(int count)
        {
            if (count > limit - Length) throw new InvalidOperationException("Page exceeds producer byte budget.");
        }
        public override void Write(byte[] buffer, int offset, int count) { Check(count); base.Write(buffer, offset, count); }
        public override void Write(ReadOnlySpan<byte> buffer) { Check(buffer.Length); base.Write(buffer); }
        public override void WriteByte(byte value) { Check(1); base.WriteByte(value); }
    }

    [Fact]
    public async Task CustomStageCannotPublishEvidenceFromAnotherRequest()
    {
        using var h = new Harness();
        var unit = new Mock<IUnitofWork>();
        unit.SetupProperty(source => source.EntityStructure, Schema());
        unit.SetupProperty(source => source.DataSource, h.Source.Object);
        unit.SetupProperty(source => source.EntityName, "Rows");
        var reader = unit.As<IStagedUnitofWorkPageRead>();
        reader.SetupGet(source => source.SupportsStagedPageRead).Returns(true);
        var stage = new Mock<IUnitofWorkPageReadStage>();
        stage.SetupGet(source => source.Page).Returns(new ProviderPageInfo(Request(), 2, 2, 100));
        reader.Setup(source => source.PreparePageReadAsync(It.IsAny<BoundedPageRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(stage.Object);
        h.Manager.RegisterBlock("CUSTOM", unit.Object, Schema(), "db");
        h.Manager.GetBlock("CUSTOM").Configuration.PageSize = 2;
        var result = await h.Manager.FetchPageWithOutcomeAsync("CUSTOM", Request());
        Assert.Equal(FormQueryState.Failed, result.State);
        Assert.False(result.RecordsPublished);
        Assert.Null(result.ProviderPage);
        stage.Verify(source => source.Publish(It.IsAny<Action<Action>>()), Times.Never);
        stage.Verify(source => source.Dispose(), Times.Once);
    }

    [Theory]
    [InlineData("pages")]
    [InlineData("bytes")]
    [InlineData("hostile-literal")]
    [InlineData("tenant-conflict")]
    public async Task RealSqliteProducerUsesBoundedLimitStableKeysAndMandatoryFiltersForCountAndRows(string scenario)
    {
        using var h = new Harness();
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        using (var seed = connection.CreateCommand())
        {
            seed.CommandText = "CREATE TABLE Rows (Id INTEGER PRIMARY KEY, TenantId INTEGER, Name TEXT); " +
                "INSERT INTO Rows VALUES (1,42,'same'),(2,42,'same'),(3,42,'same'),(4,42,'same'),(5,42,'other'),(6,43,'same');";
            await seed.ExecuteNonQueryAsync();
        }
        h.Manager.SetBlockSecurity("ROWS", new BlockSecurity { RowFilterClause = "TenantId = :tenant",
            RowFilterValues = new Dictionary<string, object> { ["tenant"] = 42 } });
        h.Block.DefaultWhereClause = "Name = 'same'";
        var rowsRead = 0;
        h.Read = async (request, token) =>
        {
            using var transaction = connection.BeginTransaction();
            var definition = h.Source.Object.BuildSelectQueryDefinition("Rows", request.CopyFilters());
            Assert.Equal(3, definition.Parameters.Count);
            using var count = connection.CreateCommand();
            count.Transaction = transaction;
            count.ApplyFilterQueryDefinition(definition);
            count.CommandText = "SELECT COUNT(*) FROM (" + count.CommandText + ")";
            var total = Convert.ToInt64(await count.ExecuteScalarAsync(token));
            using var page = connection.CreateCommand();
            page.Transaction = transaction;
            page.ApplyFilterQueryDefinition(definition);
            var order = string.Join(",", request.Order.Select(column => "\"" + column.FieldName.Replace("\"", "\"\"") + "\"" + (column.Descending ? " DESC" : " ASC")));
            page.CommandText = "SELECT Id,TenantId,Name,length(CAST(Name AS BLOB)) FROM (" + page.CommandText + ") ORDER BY " + order + " LIMIT @take OFFSET @offset";
            page.Parameters.AddWithValue("@take", request.PageSize);
            page.Parameters.AddWithValue("@offset", request.Offset);
            using var reader = await page.ExecuteReaderAsync(token);
            using var output = new LimitedPayloadStream(request.MaxPayloadBytes);
            using (var writer = new Utf8JsonWriter(output))
            {
                writer.WriteStartArray(); writer.Flush();
                while (await reader.ReadAsync(token))
                {
                    rowsRead++;
                    // Bound a single large cell before materializing it. The encoder's worst-case
                    // JSON escaping expands a UTF-8 string at most sixfold, plus fixed row overhead.
                    if (reader.GetInt64(3) > (request.MaxPayloadBytes - 128) / 6)
                        throw new InvalidOperationException("Cell exceeds producer byte budget.");
                    writer.WriteStartObject();
                    writer.WriteNumber("Id", reader.GetInt32(0)); writer.WriteNumber("TenantId", reader.GetInt32(1));
                    writer.WriteString("Name", reader.GetString(2)); writer.WriteEndObject(); writer.Flush();
                    if (output.Length + 1 > request.MaxPayloadBytes) throw new InvalidOperationException("Page exceeds producer byte budget.");
                }
                writer.WriteEndArray(); writer.Flush();
            }
            Assert.True(output.Length <= request.MaxPayloadBytes);
            return new BoundedPageResponse(request.RequestId, request.PageNumber, request.PageSize, total, output.ToArray());
        };
        var caller = new[] { scenario switch
        {
            "hostile-literal" => new AppFilter { FieldName = "Name", Operator = "=", FilterValue = "same' OR 1=1 --" },
            "tenant-conflict" => new AppFilter { FieldName = "TenantId", Operator = "=", FilterValue = "43" },
            _ => new AppFilter { FieldName = "Id", Operator = ">=", FilterValue = "1" }
        } };
        var first = await h.Fetch(Request(bytes: scenario == "bytes" ? 130 : 4096,
            order: new[] { new PageOrder("Name") }, filters: caller));
        if (scenario == "bytes")
        {
            Assert.Equal(FormQueryState.Failed, first.State);
            Assert.Same(h.Prior, h.Unit.Units);
            Assert.False(first.RecordsPublished);
            Assert.Equal(1, h.Calls);
            h.AssertNoFallback();
            return;
        }
        if (scenario is "hostile-literal" or "tenant-conflict")
        {
            Assert.True(first.RecordsPublished, first.Message);
            Assert.Equal(0, first.ProviderPage.TotalRecords);
            Assert.Empty(h.Unit.Units);
            Assert.Equal(0, rowsRead);
            h.AssertNoFallback();
            return;
        }
        Assert.True(first.RecordsPublished, first.Message);
        Assert.Equal(4, first.ProviderPage.TotalRecords);
        Assert.Equal(new[] { 1, 2 }, h.Unit.Units.Select(row => row.Id));
        var second = await h.Fetch(Request(2, order: new[] { new PageOrder("Name") }, filters: caller));
        Assert.True(second.RecordsPublished, second.Message);
        Assert.Equal(new[] { 3, 4 }, h.Unit.Units.Select(row => row.Id));
        Assert.Equal(4, rowsRead); // Native LIMIT, not an eager unbounded Get then Take.
        Assert.Equal(2, h.Calls);
        h.AssertNoFallback();
    }
}
