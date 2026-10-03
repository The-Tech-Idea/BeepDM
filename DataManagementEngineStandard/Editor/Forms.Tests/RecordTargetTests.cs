using Moq;
using TheTechIdea.Beep;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOW;
using TheTechIdea.Beep.Editor.UOWManager.Helpers;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Report;
using Xunit;

namespace TheTechIdea.Beep.Editor.UOWManager.Tests;

public class RecordTargetTests
{
    public sealed class Row : Entity
    {
        private int _id;
        private string _name = "prior", _related = "old";
        public int Id { get => _id; set => SetProperty(ref _id, value); }
        public string Name { get => _name; set => SetProperty(ref _name, value); }
        public string Related { get => _related; set => SetProperty(ref _related, value); }
        public string FixedText => "fixed";
    }

    private static EntityStructure Schema() => new()
    {
        EntityName = "Rows", Fields = new List<EntityField>
        {
            new() { FieldName = "Id", Fieldtype = "System.Int32", IsKey = true },
            new() { FieldName = "Name", Fieldtype = "System.String" },
            new() { FieldName = "Related", Fieldtype = "System.String" }
        }
    };

    private static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static async Task Finish(Task task) => await task.WaitAsync(TimeSpan.FromSeconds(10));
    private static ItemValidationResult Invalid(string message) => new()
    {
        RuleResults = new List<ValidationRuleResult> { ValidationRuleResult.Failure("rule", "Name", message) }
    };

    private sealed class Harness : IDisposable
    {
        internal readonly Mock<IDMEEditor> Editor = new();
        internal readonly Mock<IDataSource> Source = new();
        internal readonly Mock<ILOVManager> Lov = new();
        internal readonly Mock<IValidationManager> Validation = new();
        internal readonly Mock<ITriggerManager> Triggers = new();
        internal readonly Mock<IEditorProvider> Popup = new();
        internal readonly UnitofWork<Row> Unit;
        internal readonly UnitOfWorkWrapper Wrapper;
        internal readonly ObservableBindingList<Row> Rows;
        internal readonly FormsManager Manager;
        internal readonly LOVDefinition Definition = new()
        {
            DataSourceName = "db", EntityName = "Choices", ReturnField = "Name", DisplayField = "Name",
            UseCache = false, RelatedFieldMappings = new Dictionary<string, string> { ["Related"] = "Related" }
        };
        internal readonly Row Selection = new() { Id = 99, Name = "selected", Related = "joined" };
        internal Harness(bool defaultLov = false)
        {
            Editor.Setup(e => e.GetDataSource("db")).Returns(Source.Object);
            Source.Setup(s => s.GetEntity(It.IsAny<string>(), It.IsAny<List<AppFilter>>()))
                .Returns(() => new List<object> { Selection });
            Source.Setup(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<AppFilter>>()))
                .ReturnsAsync(new object[] { new Row { Id = 5 } });
            Validation.Setup(v => v.ValidateItem(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<ValidationTiming>()))
                .Returns(() => new ItemValidationResult());
            Validation.Setup(v => v.ValidateRecord(It.IsAny<string>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<ValidationTiming>()))
                .Returns(() => new RecordValidationResult());
            Triggers.Setup(t => t.FireBlockTriggerAsync(It.IsAny<TriggerType>(), It.IsAny<string>(),
                It.IsAny<TriggerContext>(), It.IsAny<CancellationToken>())).ReturnsAsync(TriggerResult.Success);
            Rows = new ObservableBindingList<Row>(new List<Row> { new() { Id = 1 }, new() { Id = 2, Name = "second" } });
            Unit = new UnitofWork<Row>(Editor.Object, "db", "Rows", Schema(), "Id") { DataSource = Source.Object, Units = Rows };
            Unit.MoveFirst();
            Wrapper = new UnitOfWorkWrapper(Unit);
            Lov.Setup(l => l.GetLOV("ROWS", "Name")).Returns(Definition);
            Lov.Setup(l => l.LoadLOVDataAsync("ROWS", "Name", It.IsAny<string>()))
                .ReturnsAsync(LOVResult.Ok(new List<object> { Selection }));
            Lov.Setup(l => l.GetRelatedFieldValues(It.IsAny<LOVDefinition>(), It.IsAny<object>()))
                .Returns((LOVDefinition d, object row) => new LOVManager(Editor.Object, new())
                    .GetRelatedFieldValues(d, row));
            Lov.Setup(l => l.ValidateLOVValueAsync("ROWS", "Name", It.IsAny<object>()))
                .ReturnsAsync(LOVValidationResult.Valid());
            Manager = new FormsManager(Editor.Object, lovManager: defaultLov ? null : Lov.Object,
                validationManager: Validation.Object, triggerManager: Triggers.Object, editorProvider: Popup.Object);
            Manager.RegisterBlock("ROWS", Wrapper, Schema(), "db");
            Manager.GetBlock("ROWS").Mode = DataBlockMode.CRUD;
            if (defaultLov) Manager.LOV.RegisterLOV("ROWS", "Name", Definition);
        }
        internal (TaskCompletionSource Entered, TaskCompletionSource<LOVResult> Release) PauseLoad()
        {
            var entered = Signal();
            var release = new TaskCompletionSource<LOVResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            Lov.Setup(l => l.LoadLOVDataAsync("ROWS", "Name", It.IsAny<string>()))
                .Callback(() => entered.TrySetResult()).Returns(release.Task);
            return (entered, release);
        }
        public void Dispose() { Manager.Dispose(); Wrapper.Dispose(); Unit.Dispose(); Rows.Dispose(); }
    }

    private static TaskCompletionSource<EditorResult> PauseEditor(Harness h)
    {
        var release = new TaskCompletionSource<EditorResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        h.Popup.Setup(p => p.ShowEditorAsync(It.IsAny<EditorDefinition>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(release.Task);
        return release;
    }

    [Fact]
    public async Task EditorSetterRejectionIsNotReportedAsLegacyCommit()
    {
        using var h = new Harness();
        h.Popup.Setup(p => p.ShowEditorAsync(It.IsAny<EditorDefinition>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EditorResult.Ok("cannot write"));
        var result = await h.Manager.ShowEditorWithOutcomeAsync("ROWS", "FixedText");
        Assert.Equal(FormEditorState.Failed, result.State);
        Assert.True(result.ProviderCommitted);
        Assert.True(result.WriteEffectsPossible);
        Assert.False(result.WriteAcknowledged);
        Assert.False((await h.Manager.ShowEditorAsync("ROWS", "FixedText")).Committed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EditorUsesTrackedInsertPermissionsInsteadOfLoadedRowUpdatePermissions(bool allowInsert)
    {
        using var h = new Harness();
        var row = new Row { Id = 3, Name = "new row" };
        h.Unit.Add(row);
        h.Unit.MoveLast();
        Assert.Equal(EntityState.Added, h.Unit.GetTrackingItem(row).EntityState);
        var block = h.Manager.GetBlock("ROWS");
        block.UpdateAllowed = false;
        block.InsertAllowed = allowInsert;
        h.Popup.Setup(p => p.ShowEditorAsync(It.IsAny<EditorDefinition>(), "new row", It.IsAny<CancellationToken>()))
            .ReturnsAsync(EditorResult.Ok("insert text"));
        var result = await h.Manager.ShowEditorWithOutcomeAsync("ROWS", "Name");
        Assert.Equal(allowInsert ? FormEditorState.Completed : FormEditorState.Denied, result.State);
        Assert.Equal(allowInsert ? "insert text" : "new row", row.Name);
    }

    [Fact]
    public async Task EditorPrecancelAndPostcloseRejectBeforeProviderAdmission()
    {
        using var h = new Harness();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => h.Manager.ShowEditorWithOutcomeAsync("ROWS", "Name", new CancellationToken(true)));
        h.Manager.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => h.Manager.ShowEditorWithOutcomeAsync("ROWS", "Name"));
        h.Popup.Verify(p => p.ShowEditorAsync(It.IsAny<EditorDefinition>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task QueuedUiProviderOwnsDispatchAndAcknowledgesCloseBeforeDrainCompletes()
    {
        using var h = new Harness();
        Action? dispatch = null;
        var finished = new TaskCompletionSource<EditorResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        h.Popup.Setup(p => p.ShowEditorAsync(It.IsAny<EditorDefinition>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((EditorDefinition definition, string text, CancellationToken ct) =>
            {
                // A manual UI dispatch queue: cancellation does not acknowledge a queued dialog.
                dispatch = () => { Assert.True(ct.IsCancellationRequested); finished.SetResult(EditorResult.Cancel()); };
                return finished.Task;
            });
        var operation = h.Manager.ShowEditorWithOutcomeAsync("ROWS", "Name");
        Assert.NotNull(dispatch);
        h.Manager.Dispose();
        var drain = h.Manager.WaitForPendingCallbacksAsync();
        Assert.False(operation.IsCompleted);
        Assert.False(drain.IsCompleted);
        dispatch();
        Assert.Equal(FormEditorState.Cancelled, (await operation.WaitAsync(TimeSpan.FromSeconds(10))).State);
        await Finish(drain);
        Assert.Equal("prior", h.Rows[0].Name);
    }

    [Fact]
    public async Task EditorAppliesCapturedTextAndCopiesDefinitionForBorrowedProvider()
    {
        using var h = new Harness();
        var definition = h.Manager.CreateEditor("large", "Original");
        h.Manager.ItemProperties.GetItem("ROWS", "Name").EditorName = "large";
        h.Popup.Setup(p => p.ShowEditorAsync(It.IsAny<EditorDefinition>(), "prior", It.IsAny<CancellationToken>()))
            .Returns((EditorDefinition supplied, string text, CancellationToken ct) =>
            {
                Assert.NotSame(definition, supplied);
                supplied.Title = "Provider mutation";
                return Task.FromResult(EditorResult.Ok("accepted"));
            });
        var result = await h.Manager.ShowEditorWithOutcomeAsync("rows", "Name");
        Assert.Equal(FormEditorState.Completed, result.State);
        Assert.True(result.Committed, result.ErrorMessage);
        Assert.True(result.ProviderAcknowledged);
        Assert.True(result.WriteAcknowledged);
        Assert.Equal("accepted", result.Value);
        Assert.Equal("accepted", h.Rows[0].Name);
        Assert.Equal("Original", definition.Title);
        Assert.Equal(0, h.Manager.PendingCallbackCount);
    }

    [Theory]
    [InlineData("cursor")]
    [InlineData("cursor-aba")]
    [InlineData("edit-aba")]
    [InlineData("unregister")]
    [InlineData("mode")]
    [InlineData("item")]
    [InlineData("definition")]
    [InlineData("configuration")]
    public async Task LateEditorCannotApplyAfterTargetOrConfigurationChanges(string change)
    {
        using var h = new Harness();
        var definition = h.Manager.CreateEditor("large");
        h.Manager.ItemProperties.GetItem("ROWS", "Name").EditorName = "large";
        var release = PauseEditor(h);
        var task = h.Manager.ShowEditorWithOutcomeAsync("ROWS", "Name");
        Assert.Equal(1, h.Manager.PendingCallbackCount);
        switch (change)
        {
            case "cursor": h.Unit.MoveNext(); break;
            case "cursor-aba": h.Unit.MoveNext(); h.Unit.MoveFirst(); break;
            case "edit-aba": h.Rows[0].Name = "other"; h.Rows[0].Name = "prior"; break;
            case "unregister": h.Manager.UnregisterBlock("ROWS"); break;
            case "mode": h.Manager.GetBlock("ROWS").Mode = DataBlockMode.Query; break;
            case "item": h.Manager.ItemProperties.RegisterItem("ROWS", "Name", new ItemInfo { ItemName = "Name" }); break;
            case "definition": h.Manager.CreateEditor("large"); break;
            case "configuration": definition.Width++; break;
        }
        release.SetResult(EditorResult.Ok("late"));
        var result = await task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(FormEditorState.Superseded, result.State);
        Assert.True(result.ProviderCommitted);
        Assert.False(result.WriteEffectsPossible);
        Assert.Null(result.Value);
        Assert.Equal("prior", h.Rows[0].Name);
        Assert.Equal("second", h.Rows[1].Name);
    }

    [Theory]
    [InlineData("readonly")]
    [InlineData("criteria")]
    [InlineData("disabled")]
    [InlineData("hidden")]
    [InlineData("item-update")]
    [InlineData("block-update")]
    [InlineData("masked")]
    [InlineData("security")]
    public async Task EditorDeniedBeforeRawValueIsSentToProvider(string policy)
    {
        using var h = new Harness();
        var block = h.Manager.GetBlock("ROWS");
        var item = h.Manager.ItemProperties.GetItem("ROWS", "Name");
        switch (policy)
        {
            case "readonly": block.Mode = DataBlockMode.ReadOnly; break;
            case "criteria": block.Mode = DataBlockMode.EnterQuery; break;
            case "disabled": item.Enabled = false; break;
            case "hidden": item.Visible = false; break;
            case "item-update": item.UpdateAllowed = false; break;
            case "block-update": block.UpdateAllowed = false; break;
            case "masked": h.Manager.SetFieldSecurity("ROWS", "Name", new FieldSecurity { Masked = true }); break;
            case "security": h.Manager.SetBlockSecurity("ROWS", new BlockSecurity { AllowUpdate = false }); break;
        }
        var result = await h.Manager.ShowEditorWithOutcomeAsync("ROWS", "Name");
        Assert.Equal(FormEditorState.Denied, result.State);
        Assert.False(result.ProviderInvoked);
        h.Popup.Verify(p => p.ShowEditorAsync(It.IsAny<EditorDefinition>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EditorRechecksPermissionAfterProviderAcknowledgement()
    {
        using var h = new Harness();
        var release = PauseEditor(h);
        var task = h.Manager.ShowEditorWithOutcomeAsync("ROWS", "Name");
        h.Manager.ItemProperties.GetItem("ROWS", "Name").UpdateAllowed = false;
        release.SetResult(EditorResult.Ok("denied"));
        var result = await task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(FormEditorState.Denied, result.State);
        Assert.True(result.ProviderAcknowledged);
        Assert.False(result.WriteEffectsPossible);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EditorCancellationWaitsForPhysicalPopupCompletion(bool close)
    {
        using var h = new Harness();
        using var cancellation = new CancellationTokenSource();
        var release = PauseEditor(h);
        var task = h.Manager.ShowEditorWithOutcomeAsync("ROWS", "Name", cancellation.Token);
        if (close) h.Manager.Dispose(); else cancellation.Cancel();
        var drain = h.Manager.WaitForPendingCallbacksAsync();
        Assert.False(task.IsCompleted);
        Assert.False(drain.IsCompleted);
        release.SetResult(EditorResult.Ok("late"));
        var result = await task.WaitAsync(TimeSpan.FromSeconds(10));
        await Finish(drain);
        Assert.Equal(FormEditorState.Cancelled, result.State);
        Assert.True(result.ProviderCommitted);
        Assert.False(result.WriteEffectsPossible);
        Assert.Equal("prior", h.Rows[0].Name);
    }

    [Fact]
    public async Task NewerEditorRequestSupersedesOlderRequestOnSameField()
    {
        using var h = new Harness();
        var old = PauseEditor(h);
        var first = h.Manager.ShowEditorWithOutcomeAsync("ROWS", "Name");
        h.Popup.Setup(p => p.ShowEditorAsync(It.IsAny<EditorDefinition>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EditorResult.Cancel());
        var newer = await h.Manager.ShowEditorWithOutcomeAsync("ROWS", "Name");
        old.SetResult(EditorResult.Ok("old"));
        var prior = await first.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(FormEditorState.Cancelled, newer.State);
        Assert.Equal(FormEditorState.Superseded, prior.State);
        Assert.True(newer.RequestRevision > prior.RequestRevision);
        Assert.Equal("prior", h.Rows[0].Name);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EditorNullOrFaultAcknowledgementDoesNotFabricateSuccess(bool fault)
    {
        using var h = new Harness();
        h.Popup.Setup(p => p.ShowEditorAsync(It.IsAny<EditorDefinition>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(fault ? Task.FromException<EditorResult>(new InvalidOperationException("provider failure")) : Task.FromResult<EditorResult>(null!));
        var result = await h.Manager.ShowEditorWithOutcomeAsync("ROWS", "Name");
        Assert.Equal(FormEditorState.Failed, result.State);
        Assert.True(result.ProviderInvoked);
        Assert.False(result.ProviderAcknowledged);
        Assert.False(result.WriteEffectsPossible);
        Assert.Equal(0, h.Manager.PendingCallbackCount);
    }

    [Fact]
    public async Task EditorRetirementInsideSetterPreservesPossibleAndAcknowledgedEffects()
    {
        using var h = new Harness();
        h.Popup.Setup(p => p.ShowEditorAsync(It.IsAny<EditorDefinition>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EditorResult.Ok("applied"));
        h.Manager.OnBlockFieldChanged += (_, _) => h.Manager.Dispose();
        var result = await h.Manager.ShowEditorWithOutcomeAsync("ROWS", "Name");
        Assert.Equal(FormEditorState.Cancelled, result.State);
        Assert.True(result.WriteEffectsPossible);
        Assert.True(result.WriteAcknowledged);
        Assert.False(result.Committed);
        Assert.Null(result.Value);
        Assert.Equal("applied", h.Rows[0].Name);
    }

    [Fact]
    public async Task NestedEditorAndSelfDrainRejectWithoutDeadlock()
    {
        using var h = new Harness();
        h.Popup.Setup(p => p.ShowEditorAsync(It.IsAny<EditorDefinition>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                Assert.Throws<InvalidOperationException>(() => { _ = h.Manager.WaitForPendingCallbacksAsync(); });
                var nested = await h.Manager.ShowEditorWithOutcomeAsync("ROWS", "Name");
                Assert.Equal(FormEditorState.Failed, nested.State);
                return EditorResult.Cancel();
            });
        var result = await h.Manager.ShowEditorWithOutcomeAsync("ROWS", "Name");
        Assert.Equal(FormEditorState.Cancelled, result.State);
        h.Popup.Verify(p => p.ShowEditorAsync(It.IsAny<EditorDefinition>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CurrentLovAppliesCapturedSelectionAndRelatedFields(bool legacy)
    {
        using var h = new Harness();
        var result = legacy ? Assert.IsType<FormLovResult>(await h.Manager.ShowLOVAsync("rows", "Name", selectedRecord: h.Selection)) :
            await h.Manager.ShowLOVWithOutcomeAsync("rows", "Name", selectedRecord: h.Selection);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(FormLovState.Completed, result.State);
        Assert.True(result.LoadAcknowledged);
        Assert.True(result.SelectionApplied);
        Assert.True(result.SelectionEffectsPossible);
        Assert.Equal(new[] { "Name", "Related" }, result.AppliedFields);
        Assert.Equal("selected", h.Rows[0].Name);
        Assert.Equal("joined", h.Rows[0].Related);
        Assert.Equal("second", h.Rows[1].Name);
        Assert.Equal("ROWS", result.BlockName);
        Assert.NotEqual(Guid.Empty, result.RegistrationId);
        Assert.Equal(0, h.Manager.PendingCallbackCount);
    }

    [Theory]
    [InlineData("move")]
    [InlineData("aba")]
    [InlineData("edit")]
    [InlineData("edit-aba")]
    [InlineData("clear")]
    [InlineData("replacement")]
    [InlineData("unregister")]
    [InlineData("mode")]
    [InlineData("item")]
    [InlineData("definition")]
    [InlineData("mapping")]
    [InlineData("query")]
    public async Task DelayedSelectionCannotRetargetOrApplyAfterTargetChanges(string change)
    {
        using var h = new Harness();
        var (entered, release) = h.PauseLoad();
        var first = h.Rows[0];
        var task = h.Manager.ShowLOVWithOutcomeAsync("ROWS", "Name", selectedRecord: h.Selection);
        await Finish(entered.Task);
        switch (change)
        {
            case "move": h.Unit.MoveNext(); break;
            case "aba": h.Unit.MoveNext(); h.Unit.MoveFirst(); break;
            case "edit": first.Name = "user edit"; break;
            case "edit-aba": first.Name = "user edit"; first.Name = "prior"; break;
            case "clear": h.Unit.Clear(); break;
            case "replacement": h.Manager.RegisterBlock("rows", new Mock<IUnitofWork>().Object, Schema()); break;
            case "unregister": h.Manager.UnregisterBlock("ROWS"); break;
            case "mode": h.Manager.GetBlock("ROWS").Mode = DataBlockMode.Query; break;
            case "item": h.Manager.ItemProperties.RegisterItem("ROWS", "Name", new ItemInfo { BlockName = "ROWS", ItemName = "Name" }); break;
            case "definition": h.Lov.Setup(l => l.GetLOV("ROWS", "Name")).Returns(new LOVDefinition()); break;
            case "mapping": h.Definition.RelatedFieldMappings["Related"] = "Id"; break;
            case "query": Assert.True((await h.Manager.ExecuteQueryWithOutcomeAsync("ROWS")).ReadAcknowledged); break;
        }
        release.SetResult(LOVResult.Ok(new List<object> { h.Selection }));
        var result = await task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.False(result.Success);
        Assert.Equal(FormLovState.Superseded, result.State);
        Assert.True(result.LoadAcknowledged);
        Assert.False(result.SelectionEffectsPossible);
        Assert.Empty(result.AppliedFields);
        Assert.Empty(result.Records);
        Assert.NotEqual("selected", first.Name);
        Assert.Equal("old", first.Related);
        h.Lov.Verify(l => l.LoadLOVDataAsync("ROWS", "Name", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task NewerSameFieldLovSupersedesOldRequestEvenWithoutRecordChanges()
    {
        using var h = new Harness();
        var (entered, release) = h.PauseLoad();
        var old = h.Manager.ShowLOVWithOutcomeAsync("ROWS", "Name", "slow");
        await Finish(entered.Task);
        h.Lov.Setup(l => l.GetLOV("ROWS", "name")).Returns(h.Definition);
        h.Lov.Setup(l => l.LoadLOVDataAsync("ROWS", "name", "fast")).ReturnsAsync(LOVResult.Ok(new()));
        var latest = await h.Manager.ShowLOVWithOutcomeAsync("rows", "name", "fast");
        Assert.True(latest.Success, latest.ErrorMessage);
        Assert.Equal(2, latest.RequestRevision);
        release.SetResult(LOVResult.Ok(new()));
        var result = await old.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(FormLovState.Superseded, result.State);
        Assert.Equal(latest.RegistrationId, result.RegistrationId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancellationOrCloseWaitsForPhysicalLoadAcknowledgement(bool close)
    {
        using var h = new Harness();
        var (entered, release) = h.PauseLoad();
        using var cancellation = new CancellationTokenSource();
        var task = h.Manager.ShowLOVWithOutcomeAsync("ROWS", "Name", selectedRecord: h.Selection,
            cancellationToken: cancellation.Token);
        await Finish(entered.Task);
        Task drain;
        if (close) drain = h.Manager.DisposeAsync().AsTask();
        else { cancellation.Cancel(); drain = h.Manager.WaitForPendingCallbacksAsync(); }
        Assert.False(drain.IsCompleted);
        Assert.False(task.IsCompleted);
        release.SetResult(LOVResult.Ok(new()));
        var result = await task.WaitAsync(TimeSpan.FromSeconds(10));
        await Finish(drain);
        Assert.Equal(FormLovState.Cancelled, result.State);
        Assert.True(result.LoadAcknowledged);
        Assert.True(result.HelperEffectsPossible);
        Assert.Empty(result.AppliedFields);
        Assert.Equal("prior", h.Rows[0].Name);
        Assert.Equal(0, h.Manager.PendingCallbackCount);
        h.Unit.MoveNext(); // Borrowed UoW is still usable.
    }

    [Fact]
    public async Task SelectedValuesAreCapturedBeforeLoadNotReadFromLaterMutatedSelection()
    {
        using var h = new Harness();
        var (entered, release) = h.PauseLoad();
        var task = h.Manager.ShowLOVWithOutcomeAsync("ROWS", "Name", selectedRecord: h.Selection);
        await Finish(entered.Task);
        h.Selection.Name = "changed outside";
        h.Selection.Related = "other";
        release.SetResult(LOVResult.Ok(new()));
        var result = await task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.True(result.SelectionApplied, result.ErrorMessage);
        Assert.Equal("selected", h.Rows[0].Name);
        Assert.Equal("joined", h.Rows[0].Related);
    }

    [Fact]
    public async Task NavigationInsideFirstSetterStopsLaterFieldsAndPreservesPartialEvidence()
    {
        using var h = new Harness();
        h.Manager.OnBlockFieldChanged += (_, e) => { if (e.FieldName == "Name") h.Unit.MoveNext(); };
        var result = await h.Manager.ShowLOVWithOutcomeAsync("ROWS", "Name", selectedRecord: h.Selection);
        Assert.Equal(FormLovState.Superseded, result.State);
        Assert.True(result.SelectionEffectsPossible);
        Assert.Equal(new[] { "Name" }, result.AppliedFields);
        Assert.False(result.SelectionApplied);
        Assert.Equal("selected", h.Rows[0].Name);
        Assert.Equal("old", h.Rows[0].Related);
        Assert.Equal("second", h.Rows[1].Name);
        Assert.Equal("old", h.Rows[1].Related);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TypedValueLovCompletionCannotOverwriteNewerEditError(bool oldValid)
    {
        using var h = new Harness();
        h.Lov.Setup(l => l.HasLOV("ROWS", "Name")).Returns(true);
        var entered = Signal();
        var release = new TaskCompletionSource<LOVValidationResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        h.Lov.Setup(l => l.ValidateLOVValueAsync("ROWS", "Name", "first"))
            .Callback(() => entered.TrySetResult()).Returns(release.Task);
        h.Lov.Setup(l => l.ValidateLOVValueAsync("ROWS", "Name", "second"))
            .ReturnsAsync(LOVValidationResult.Invalid("new error"));
        h.Rows[0].Name = "first";
        await Finish(entered.Task);
        h.Rows[0].Name = "second";
        Assert.Equal("new error", h.Manager.ItemProperties.GetItemErrorMessage("ROWS", "Name"));
        release.SetResult(oldValid ? LOVValidationResult.Valid() : LOVValidationResult.Invalid("old error"));
        await Finish(h.Manager.WaitForPendingCallbacksAsync());
        Assert.Equal("new error", h.Manager.ItemProperties.GetItemErrorMessage("ROWS", "Name"));
    }

    [Fact]
    public async Task TypedValueLovOnCurrentUnchangedRecordStillPublishesAfterAwait()
    {
        using var h = new Harness();
        h.Lov.Setup(l => l.HasLOV("ROWS", "Name")).Returns(true);
        var entered = Signal();
        var release = new TaskCompletionSource<LOVValidationResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        h.Lov.Setup(l => l.ValidateLOVValueAsync("ROWS", "Name", "first"))
            .Callback(() => entered.TrySetResult()).Returns(release.Task);
        h.Rows[0].Name = "first";
        await Finish(entered.Task);
        release.SetResult(LOVValidationResult.Invalid("actual error"));
        await Finish(h.Manager.WaitForPendingCallbacksAsync());
        Assert.Equal("actual error", h.Manager.ItemProperties.GetItemErrorMessage("ROWS", "Name"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TypedValueCompletionCannotAnnotateAnotherRecordOrAbaCursor(bool aba)
    {
        using var h = new Harness();
        h.Lov.Setup(l => l.HasLOV("ROWS", "Name")).Returns(true);
        var entered = Signal();
        var release = new TaskCompletionSource<LOVValidationResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        h.Lov.Setup(l => l.ValidateLOVValueAsync("ROWS", "Name", "first"))
            .Callback(() => entered.TrySetResult()).Returns(release.Task);
        h.Rows[0].Name = "first";
        await Finish(entered.Task);
        h.Unit.MoveNext();
        if (aba) h.Unit.MoveFirst();
        h.Manager.ItemProperties.SetItemError("ROWS", "Name", "current record error");
        release.SetResult(LOVValidationResult.Invalid("stale record error"));
        await Finish(h.Manager.WaitForPendingCallbacksAsync());
        Assert.Equal("current record error", h.Manager.ItemProperties.GetItemErrorMessage("ROWS", "Name"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SynchronousRulesCannotAnnotateNewRecordAfterReentrantNavigation(bool block)
    {
        using var h = new Harness();
        h.Manager.ItemProperties.SetItemError("ROWS", "Name", "retained error");
        if (block)
            h.Validation.Setup(v => v.ValidateRecord("ROWS", It.IsAny<IDictionary<string, object>>(), ValidationTiming.Manual))
                .Callback(() => h.Unit.MoveNext()).Returns(new RecordValidationResult
                { ItemResults = new Dictionary<string, ItemValidationResult> { ["Name"] = Invalid("stale rule error") } });
        else
            h.Validation.Setup(v => v.ValidateItem("ROWS", "Name", "prior", ValidationTiming.OnChange))
                .Callback(() => h.Unit.MoveNext()).Returns(Invalid("stale rule error"));
        Assert.False(block ? h.Manager.ValidateBlock("ROWS") : h.Manager.ValidateField("ROWS", "Name", "prior"));
        Assert.Equal("retained error", h.Manager.ItemProperties.GetItemErrorMessage("ROWS", "Name"));
    }

    [Fact]
    public async Task RealDefaultLovLoaderCannotRetargetDelayedSelection()
    {
        using var h = new Harness(defaultLov: true);
        using var release = new ManualResetEventSlim();
        var entered = Signal();
        h.Source.Setup(s => s.GetEntity("Choices", It.IsAny<List<AppFilter>>())).Returns(() =>
        {
            entered.TrySetResult();
            if (!release.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException();
            return new List<object> { h.Selection };
        });
        var task = h.Manager.ShowLOVWithOutcomeAsync("ROWS", "Name", selectedRecord: h.Selection);
        await Finish(entered.Task);
        h.Unit.MoveNext();
        release.Set();
        var result = await task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(FormLovState.Superseded, result.State);
        Assert.True(result.LoadAcknowledged);
        Assert.Equal("prior", h.Rows[0].Name);
        Assert.Equal("second", h.Rows[1].Name);
    }

    [Fact]
    public async Task PreCancelledAndPostCloseCallsDoNotInvokeHelpers()
    {
        using var h = new Harness();
        using var ct = new CancellationTokenSource();
        ct.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => h.Manager.ShowLOVWithOutcomeAsync("ROWS", "Name", cancellationToken: ct.Token));
        h.Manager.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => h.Manager.ShowLOVWithOutcomeAsync("ROWS", "Name"));
        Assert.False(h.Manager.ValidateField("ROWS", "Name", "prior"));
        Assert.False(h.Manager.ValidateBlock("ROWS"));
        h.Lov.Verify(l => l.LoadLOVDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData(TriggerResult.Cancelled)]
    [InlineData(TriggerResult.Failure)]
    [InlineData(TriggerResult.Exception)]
    [InlineData(TriggerResult.Timeout)]
    [InlineData(TriggerResult.FormTriggerFailure)]
    public async Task NonSuccessfulTriggerDoesNotLoadOrWrite(TriggerResult trigger)
    {
        using var h = new Harness();
        h.Triggers.Setup(t => t.FireBlockTriggerAsync(TriggerType.WhenLOVValidation, "ROWS",
            It.IsAny<TriggerContext>(), It.IsAny<CancellationToken>())).ReturnsAsync(trigger);
        var result = await h.Manager.ShowLOVWithOutcomeAsync("ROWS", "Name", selectedRecord: h.Selection);
        Assert.False(result.Success);
        Assert.Equal(trigger == TriggerResult.Cancelled ? FormLovState.Cancelled : FormLovState.Failed, result.State);
        Assert.False(result.HelperEffectsPossible);
        Assert.False(result.SelectionEffectsPossible);
        Assert.Equal("prior", h.Rows[0].Name);
        h.Lov.Verify(l => l.LoadLOVDataAsync("ROWS", "Name", It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("failed")]
    [InlineData("throw")]
    public async Task FailedOrUnacknowledgedLoadDoesNotApplySelection(string failure)
    {
        using var h = new Harness();
        var setup = h.Lov.Setup(l => l.LoadLOVDataAsync("ROWS", "Name", It.IsAny<string>()));
        if (failure == "throw") setup.ThrowsAsync(new InvalidOperationException("provider error"));
        else setup.ReturnsAsync(failure == "null" ? null! : LOVResult.Fail("provider error"));
        var result = await h.Manager.ShowLOVWithOutcomeAsync("ROWS", "Name", selectedRecord: h.Selection);
        Assert.Equal(FormLovState.Failed, result.State);
        Assert.False(result.Success);
        Assert.True(result.HelperEffectsPossible);
        Assert.Equal(failure == "failed", result.LoadAcknowledged);
        Assert.False(result.SelectionEffectsPossible);
        Assert.Empty(result.AppliedFields);
        Assert.Equal("prior", h.Rows[0].Name);
    }

    [Fact]
    public async Task DuplicateTargetMappingRejectsBeforeTriggerOrLoad()
    {
        using var h = new Harness();
        h.Definition.RelatedFieldMappings["Related"] = "Name";
        var result = await h.Manager.ShowLOVWithOutcomeAsync("ROWS", "Name", selectedRecord: h.Selection);
        Assert.Equal(FormLovState.Failed, result.State);
        Assert.Contains("duplicate", result.ErrorMessage);
        Assert.False(result.SelectionEffectsPossible);
        h.Lov.Verify(l => l.LoadLOVDataAsync("ROWS", "Name", It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task NestedLovAndSelfDrainRejectWithoutDeadlocking()
    {
        using var h = new Harness();
        h.Triggers.Setup(t => t.FireBlockTriggerAsync(TriggerType.WhenLOVValidation, "ROWS",
            It.IsAny<TriggerContext>(), It.IsAny<CancellationToken>())).Returns(async () =>
        {
            Assert.Throws<InvalidOperationException>(() => { _ = h.Manager.WaitForPendingCallbacksAsync(); });
            var nested = await h.Manager.ShowLOVWithOutcomeAsync("ROWS", "Name");
            Assert.Equal(FormLovState.Failed, nested.State);
            Assert.Contains("nested LOV", nested.ErrorMessage);
            return TriggerResult.Success;
        });
        var result = await h.Manager.ShowLOVWithOutcomeAsync("ROWS", "Name");
        Assert.True(result.Success, result.ErrorMessage);
        h.Lov.Verify(l => l.LoadLOVDataAsync("ROWS", "Name", It.IsAny<string>()), Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DisposedRevisionSourceFailsClosedWithoutLegacyFallback(bool wrapper)
    {
        using var h = new Harness();
        Assert.True(h.Wrapper.SupportsRecordRevision);
        if (wrapper) h.Wrapper.Dispose(); else h.Unit.Dispose();
        var result = await h.Manager.ShowLOVWithOutcomeAsync("ROWS", "Name");
        Assert.False(result.Success);
        Assert.Equal(FormLovState.Failed, result.State);
        Assert.False(result.HelperEffectsPossible);
        h.Lov.Verify(l => l.LoadLOVDataAsync("ROWS", "Name", It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RealDefaultLovLoaderAndValidationAllowHealthyMultiFieldSelection()
    {
        using var h = new Harness(defaultLov: true);
        var result = await h.Manager.ShowLOVWithOutcomeAsync("ROWS", "Name", selectedRecord: h.Selection);
        await Finish(h.Manager.WaitForPendingCallbacksAsync());
        Assert.True(result.Success, result.ErrorMessage);
        Assert.True(result.SelectionApplied);
        Assert.Equal("selected", h.Rows[0].Name);
        Assert.Equal("joined", h.Rows[0].Related);
        Assert.False(h.Manager.ItemProperties.HasItemError("ROWS", "Name"));
    }

    [Fact]
    public void NoncurrentRecordEditKeepsBlockChangeFeedWithoutAnnotatingCurrentItem()
    {
        using var h = new Harness();
        h.Manager.ItemProperties.SetItemError("ROWS", "Name", "current error");
        BlockFieldChangedEventArgs? observed = null;
        h.Manager.OnBlockFieldChanged += (_, e) => observed = e;
        h.Rows[1].Name = "other row edit";
        Assert.NotNull(observed);
        Assert.Equal(1, observed.RecordIndex);
        Assert.Equal("other row edit", observed.NewValue);
        Assert.Equal("current error", h.Manager.ItemProperties.GetItemErrorMessage("ROWS", "Name"));
        Assert.Equal("prior", h.Unit.CurrentItem.Name);
    }

    [Fact]
    public async Task IndependentAbaInsideSetterIsNotAcknowledgedAsAnOwnEdit()
    {
        using var h = new Harness();
        h.Manager.OnBlockFieldChanged += (_, e) =>
        {
            if (e.FieldName != "Name") return;
            Task movement;
            using (ExecutionContext.SuppressFlow())
                movement = Task.Run(() => { h.Unit.MoveNext(); h.Unit.MoveFirst(); });
            movement.WaitAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
        };
        var result = await h.Manager.ShowLOVWithOutcomeAsync("ROWS", "Name", selectedRecord: h.Selection);
        Assert.Equal(FormLovState.Superseded, result.State);
        Assert.Equal(new[] { "Name" }, result.AppliedFields);
        Assert.Same(h.Rows[0], h.Unit.CurrentItem);
        Assert.Equal("selected", h.Rows[0].Name);
        Assert.Equal("old", h.Rows[0].Related);
    }

    [Fact]
    public void NestedFieldValidationSupersedesOuterRecordAnnotationsEvenWithoutDataChanges()
    {
        using var h = new Harness();
        h.Validation.Setup(v => v.ValidateItem("ROWS", "Name", "prior", ValidationTiming.OnChange))
            .Returns(Invalid("newer field error"));
        h.Validation.Setup(v => v.ValidateRecord("ROWS", It.IsAny<IDictionary<string, object>>(), ValidationTiming.Manual))
            .Callback(() => Assert.False(h.Manager.ValidateField("ROWS", "Name", "prior")))
            .Returns(new RecordValidationResult { ItemResults = new Dictionary<string, ItemValidationResult>
                { ["Name"] = Invalid("older record error") } });
        Assert.False(h.Manager.ValidateBlock("ROWS"));
        Assert.Equal("newer field error", h.Manager.ItemProperties.GetItemErrorMessage("ROWS", "Name"));
        Assert.Equal("prior", h.Rows[0].Name);
    }

    [Fact]
    public void RecordValidationCannotAnnotateAReplacedItemOnTheSameUnchangedRecord()
    {
        using var h = new Harness();
        h.Validation.Setup(v => v.ValidateRecord("ROWS", It.IsAny<IDictionary<string, object>>(), ValidationTiming.Manual))
            .Callback(() =>
            {
                h.Manager.ItemProperties.RegisterItem("ROWS", "Name", new ItemInfo { BlockName = "ROWS", ItemName = "Name" });
                h.Manager.ItemProperties.SetItemError("ROWS", "Name", "new item error");
            })
            .Returns(new RecordValidationResult { ItemResults = new Dictionary<string, ItemValidationResult>
                { ["Name"] = Invalid("older item error") } });
        Assert.False(h.Manager.ValidateBlock("ROWS"));
        Assert.Equal("new item error", h.Manager.ItemProperties.GetItemErrorMessage("ROWS", "Name"));
        Assert.Equal("prior", h.Rows[0].Name);
    }
}
