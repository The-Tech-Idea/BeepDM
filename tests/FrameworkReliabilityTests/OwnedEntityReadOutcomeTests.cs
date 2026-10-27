using Moq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.UOW;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using Xunit;

namespace TheTechIdea.Beep.Framework.Tests;

public sealed class OwnedEntityReadOutcomeTests
{
    public sealed class Row : Entity { public string Id { get; set; } = "retained"; }
    private static (UnitofWork<Row> Work, Mock<IDataSource> Source, Mock<IOwnedEntityReadDataSource> Owned) Harness(bool supported = true)
    {
        var source = new Mock<IDataSource>(); var owned = source.As<IOwnedEntityReadDataSource>();
        owned.SetupGet(x => x.SupportsOwnedEntityReads).Returns(supported);
        source.SetupGet(x => x.DatasourceType).Returns(DataSourceType.SqlServer);
        source.SetupGet(x => x.ErrorObject).Returns(new ErrorsInfo { Flag = Errors.Failed, Message = "Unrelated legacy failure" });
        var editor = new Mock<IDMEEditor>(); editor.SetupGet(x => x.ErrorObject).Returns(new ErrorsInfo());
        editor.Setup(x => x.GetDataSource("database")).Returns(source.Object);
        var work = new UnitofWork<Row>(editor.Object, "database", "Rows", new EntityStructure
            { EntityName = "Rows", Fields = new List<EntityField> { new() { FieldName = "Id", Fieldtype = "System.String", IsKey = true } } }, "Id");
        work.Units = new ObservableBindingList<Row>(new[] { new Row() });
        return (work, source, owned);
    }
    private static void Respond(Mock<IOwnedEntityReadDataSource> owned, EntityReadResult response) =>
        owned.Setup(x => x.ReadEntityAsync("Rows", It.IsAny<List<AppFilter>>(), It.IsAny<CancellationToken>())).ReturnsAsync(response);

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task Published_reads_retain_notification_and_reporting_warnings_without_losing_success(bool filtered, bool reportingFails)
    {
        var (work, _, owned) = Harness(); using var lifetime = work;
        var notification = new IOException("Injected observer fault"); var reporting = new IOException("Injected reporting fault");
        work.PostQuery += (_, _) => throw notification;
        if (reportingFails) Mock.Get(work.DMEEditor).Setup(x => x.AddLogMessage(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Errors>())).Throws(reporting);
        Respond(owned, new EntityReadResult(new ErrorsInfo { Flag = Errors.Ok }, new object[] { new Row { Id = "published" } }));
        var rows = filtered ? await work.Get(new List<AppFilter>()) : await work.Get();
        Assert.Equal("published", Assert.Single(rows).Id); Assert.Equal(Errors.Ok, work.ReadOutcome.Flag);
        Assert.Contains(work.ReadOutcome.Errors, x => x.Flag == Errors.Warning && ReferenceEquals(x.Ex, notification));
        if (reportingFails) Assert.Contains(work.ReadOutcome.Errors, x => x.Flag == Errors.Warning && ReferenceEquals(x.Ex, reporting));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task A_read_uses_its_explicit_acknowledgement_instead_of_an_unrelated_legacy_error(bool filtered)
    {
        var (work, source, owned) = Harness(); using var lifetime = work;
        Respond(owned, new EntityReadResult(new ErrorsInfo { Flag = Errors.Ok }, new object[] { new Row { Id = "acknowledged" } }));
        var rows = filtered ? await work.Get(new List<AppFilter>()) : await work.Get();
        Assert.Equal("acknowledged", Assert.Single(rows).Id); Assert.Equal(Errors.Ok, work.ReadOutcome.Flag);
        Assert.Equal(Errors.Failed, source.Object.ErrorObject.Flag);
        source.Verify(x => x.GetEntity(It.IsAny<string>(), It.IsAny<List<AppFilter>>()), Times.Never);
        source.Verify(x => x.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<AppFilter>>()), Times.Never);
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task A_failed_read_cannot_publish_rows_or_erase_the_previous_buffer(bool staged)
    {
        var (work, source, owned) = Harness(); using var lifetime = work; var prior = work.Units;
        source.SetupGet(x => x.ErrorObject).Returns(new ErrorsInfo { Flag = Errors.Ok });
        var failure = new IOException("Injected read failure");
        Respond(owned, new EntityReadResult(new ErrorsInfo { Flag = Errors.Failed, Ex = failure, Message = "Refused" }, new object[] { new Row { Id = "must-not-publish" } }));
        if (staged) await Assert.ThrowsAsync<InvalidOperationException>(async () => { using var stage = await work.PrepareReadAsync(new List<AppFilter>()); });
        else await Assert.ThrowsAsync<InvalidOperationException>(() => work.Get());
        Assert.Same(prior, work.Units); Assert.Equal("retained", Assert.Single(work.Units).Id);
        Assert.Equal(Errors.Failed, work.ReadOutcome.Flag); Assert.Same(failure, work.ReadOutcome.Ex);
    }
    [Fact]
    public async Task An_acknowledged_empty_result_is_distinct_from_a_failed_read()
    {
        var (work, _, owned) = Harness(); using var lifetime = work;
        Respond(owned, new EntityReadResult(new ErrorsInfo { Flag = Errors.Ok }, Array.Empty<object>()));
        Assert.Empty(await work.Get()); Assert.Equal(Errors.Ok, work.ReadOutcome.Flag);
    }
    [Fact]
    public async Task Unsupported_advertised_ownership_cannot_fall_back_to_a_legacy_read()
    {
        var (work, source, owned) = Harness(false); using var lifetime = work;
        await Assert.ThrowsAsync<NotSupportedException>(() => work.Get());
        owned.Verify(x => x.ReadEntityAsync(It.IsAny<string>(), It.IsAny<List<AppFilter>>(), It.IsAny<CancellationToken>()), Times.Never);
        source.Verify(x => x.GetEntity(It.IsAny<string>(), It.IsAny<List<AppFilter>>()), Times.Never);
        source.Verify(x => x.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<AppFilter>>()), Times.Never);
        Assert.Equal("retained", Assert.Single(work.Units).Id);
    }
    [Fact]
    public async Task A_missing_owned_acknowledgement_is_not_an_empty_success()
    {
        var (work, _, owned) = Harness(); using var lifetime = work;
        Respond(owned, null!);
        await Assert.ThrowsAsync<InvalidOperationException>(() => work.Get());
        Assert.Equal(Errors.Failed, work.ReadOutcome.Flag); Assert.Equal("retained", Assert.Single(work.Units).Id);
    }
    [Fact]
    public async Task A_provider_acknowledgement_cannot_publish_an_undecodable_record()
    {
        var (work, _, owned) = Harness(); using var lifetime = work; var prior = work.Units;
        Respond(owned, new EntityReadResult(new ErrorsInfo { Flag = Errors.Ok }, new object[] { new object() }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => work.Get());
        Assert.Equal(Errors.Failed, work.ReadOutcome.Flag); Assert.Same(prior, work.Units);
        Assert.Equal("retained", Assert.Single(work.Units).Id);
    }
    [Fact]
    public async Task Cancellation_waits_for_physical_acknowledgement_and_preserves_the_buffer()
    {
        var (work, _, owned) = Harness(); using var lifetime = work; using var cancellation = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var physical = new TaskCompletionSource<EntityReadResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        owned.Setup(x => x.ReadEntityAsync("Rows", It.IsAny<List<AppFilter>>(), cancellation.Token)).Returns(() => { entered.TrySetResult(); return physical.Task; });
        var pending = work.PrepareReadAsync(new List<AppFilter>(), cancellation.Token); await entered.Task;
        cancellation.Cancel(); Assert.False(pending.IsCompleted);
        physical.SetResult(new EntityReadResult(new ErrorsInfo { Flag = Errors.Ok }, new object[] { new Row { Id = "cancelled" } }));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.Equal(Errors.Failed, work.ReadOutcome.Flag);
        Assert.Equal("retained", Assert.Single(work.Units).Id);
        Respond(owned, new EntityReadResult(new ErrorsInfo { Flag = Errors.Ok }, Array.Empty<object>()));
        Assert.Empty(await work.Get());
    }
}
