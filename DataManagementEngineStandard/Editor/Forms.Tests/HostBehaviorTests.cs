using System.Collections.Concurrent;
using Moq;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.Forms.Helpers;
using TheTechIdea.Beep.Editor.Forms.Hosts;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOW;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Utilities;
using Xunit;

namespace TheTechIdea.Beep.Editor.UOWManager.Tests;

public class HostBehaviorTests
{
    public sealed class Row : Entity
    {
        private int _id;
        private string? _name = "first", _other = "other";
        public int Id { get => _id; set => SetProperty(ref _id, value); }
        public string? Name { get => _name; set => SetProperty(ref _name, value); }
        public string? Other { get => _other; set => SetProperty(ref _other, value); }
    }

    private sealed class PlainRow
    {
        public int Id { get; set; } = 3;
        public object? Name { get; set; } = "plain";
        public string? Other { get; set; } = "plain other";
    }

    private sealed class Dispatcher : IFormsDispatcher
    {
        private readonly AsyncLocal<bool> _ui = new();
        private readonly ConcurrentQueue<(Action Action, CancellationToken Token, TaskCompletionSource Done)> _queue = new();
        internal bool FailNext, WrongContextNext, DuplicateNext, SkipNext, EarlyNext;
        internal Action? Skipped;
        internal Action? WaitUntilEarlyActionStarts;
        internal Task? EarlyWork;
        internal int Submitted, Executed;
        public bool CheckAccess() => _ui.Value;
        internal void OnUi(Action action)
        {
            var prior = _ui.Value; _ui.Value = true;
            try { action(); } finally { _ui.Value = prior; }
        }
        internal void AssertUi() => Assert.True(CheckAccess(), "Unexpected worker-thread UI access");
        public Task InvokeAsync(Action action, CancellationToken cancellationToken = default)
        {
            var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _queue.Enqueue((action, cancellationToken, done)); Submitted++;
            return done.Task;
        }
        internal bool PumpOne()
        {
            if (!_queue.TryDequeue(out var item)) return false;
            if (item.Token.IsCancellationRequested) { item.Done.TrySetCanceled(item.Token); return true; }
            if (FailNext) { FailNext = false; item.Done.TrySetException(new IOException("dispatcher unavailable")); return true; }
            if (SkipNext) { SkipNext = false; Skipped = item.Action; item.Done.TrySetResult(); return true; }
            if (EarlyNext)
            {
                EarlyNext = false;
                EarlyWork = Task.Run(() => OnUi(item.Action));
                WaitUntilEarlyActionStarts!();
                item.Done.TrySetResult();
                return true;
            }
            try
            {
                Executed++;
                if (WrongContextNext) { WrongContextNext = false; item.Action(); } else OnUi(item.Action);
                if (DuplicateNext) { DuplicateNext = false; OnUi(item.Action); }
                item.Done.TrySetResult();
            }
            catch (Exception ex) { item.Done.TrySetException(ex); }
            return true;
        }
        internal async Task PumpUntil(Task task)
        {
            var deadline = DateTime.UtcNow.AddSeconds(10);
            while (!task.IsCompleted)
            {
                Assert.True(DateTime.UtcNow < deadline, "Dispatcher did not acknowledge delivery");
                if (!PumpOne()) await Task.Yield();
            }
            await task;
        }
        internal void PumpAll() { while (PumpOne()) { } }
    }

    private sealed class Presenter
    {
        internal readonly Mock<IFieldPresenter> Mock = new();
        internal readonly Mock<IOriginAwareFieldPresenter>? Origin;
        internal readonly List<(object? Value, Guid Origin, long Revision)> Stamps = new();
        internal readonly List<object?> Presented = new();
        internal Action? OnSet;
        internal int Sets;
        internal Presenter(Dispatcher dispatcher, string field, bool stamped = false)
        {
            if (stamped) Origin = Mock.As<IOriginAwareFieldPresenter>();
            Mock.SetupGet(p => p.FieldName).Returns(() => { dispatcher.AssertUi(); return field; });
            Mock.SetupProperty(p => p.Value);
            Mock.SetupProperty(p => p.IsEnabled, true);
            Mock.SetupProperty(p => p.IsVisible, true);
            Mock.SetupProperty(p => p.IsReadOnly, false);
            Mock.SetupProperty(p => p.ValidationError);
            Mock.Setup(p => p.SetValue(It.IsAny<object?>())).Callback((object? value) =>
            {
                dispatcher.AssertUi(); Sets++; Presented.Add(value); Mock.Object.Value = value;
                Mock.Raise(p => p.ValueChanged += null, Mock.Object, value!);
                OnSet?.Invoke();
            });
            Origin?.Setup(p => p.SetValue(It.IsAny<object>(), It.IsAny<Guid>(), It.IsAny<long>()))
                .Callback((object? value, Guid origin, long revision) =>
                { dispatcher.AssertUi(); Sets++; Presented.Add(value); Mock.Object.Value = value; Stamps.Add((value, origin, revision)); OnSet?.Invoke(); });
        }
        internal void Edit(object? value)
        {
            Mock.Object.Value = value;
            if (Origin != null) Origin.Raise(p => p.ValueChangedWithOrigin += null,
                new FormFieldValueChangedEventArgs(value!, Guid.Empty, 0));
            else Mock.Raise(p => p.ValueChanged += null, Mock.Object, value!);
        }
        internal void Echo(int index = 0)
        {
            var stamp = Stamps[index];
            Origin!.Raise(p => p.ValueChangedWithOrigin += null,
                new FormFieldValueChangedEventArgs(stamp.Value!, stamp.Origin, stamp.Revision));
        }
    }

    private sealed class Harness : IDisposable
    {
        internal readonly Dispatcher Dispatcher = new();
        internal readonly Mock<IDMEEditor> Editor = new();
        internal readonly Mock<IDataSource> Source = new();
        internal readonly FormsManager Manager;
        internal readonly UnitofWork<Row> Unit;
        internal readonly UnitOfWorkWrapper Wrapper;
        internal readonly ObservableBindingList<Row> Rows;
        internal readonly Mock<IBeepFormsHost> Host = new();
        internal readonly Mock<IBlockView> View = new();
        internal readonly Mock<IFormsNotificationService> Notifications = new();
        internal readonly BeepViewState State = new();
        internal readonly List<string> Messages = new();
        internal readonly Presenter Name, Other;
        internal readonly List<IFieldPresenter> Presenters;
        internal FormsViewBinding? Binding;
        internal IBeepFormsHost? BoundHost;
        internal int Binds, Unbinds, Syncs, Focuses;
        internal Harness(bool stamped = false, bool attach = true, ISecurityManager? security = null)
        {
            var schema = new EntityStructure { EntityName = "Rows", Fields = new List<EntityField>
            {
                new() { FieldName = "Id", Fieldtype = "System.Int32", IsKey = true },
                new() { FieldName = "Name", Fieldtype = "System.String" },
                new() { FieldName = "Other", Fieldtype = "System.String" }
            } };
            Rows = new ObservableBindingList<Row>(new List<Row> { new() { Id = 1 }, new() { Id = 2, Name = "second" } });
            Unit = new UnitofWork<Row>(Editor.Object, "db", "Rows", schema, "Id") { Units = Rows, DataSource = Source.Object };
            Unit.MoveFirst(); Wrapper = new UnitOfWorkWrapper(Unit);
            Manager = new FormsManager(Editor.Object, securityManager: security);
            Manager.RegisterBlock("ROWS", Wrapper, schema, "db");
            Manager.GetBlock("ROWS").Mode = DataBlockMode.CRUD;
            Name = new Presenter(Dispatcher, "Name", stamped); Other = new Presenter(Dispatcher, "Other", stamped);
            Presenters = new List<IFieldPresenter> { Name.Mock.Object, Other.Mock.Object };
            Host.SetupProperty(h => h.FormsManager, Manager);
            Host.SetupGet(h => h.ActiveBlockName).Returns(() => { Dispatcher.AssertUi(); return "ROWS"; });
            Host.Setup(h => h.GetBlockInfo("ROWS")).Returns(() => { Dispatcher.AssertUi(); return Manager.GetBlock("ROWS"); });
            Host.Setup(h => h.GetFieldSecurity("ROWS", It.IsAny<string>())).Returns((string block, string field) => Manager.GetFieldSecurity(block, field));
            Host.Setup(h => h.GetSecurityContext()).Returns(() => Manager.SecurityContext);
            Host.Setup(h => h.IsBlockAllowed("ROWS", It.IsAny<SecurityPermission>())).Returns((string block, SecurityPermission permission) => Manager.IsBlockAllowed(block, permission));
            Host.Setup(h => h.GetItemInfo("ROWS", It.IsAny<string>())).Returns((string block, string field) =>
            { Dispatcher.AssertUi(); return Manager.ItemProperties.GetItem(block, field); });
            Host.Setup(h => h.GetFieldValue("ROWS", It.IsAny<string>())).Returns((string _, string field) =>
            { Dispatcher.AssertUi(); return Manager.GetFieldValue(Unit.CurrentItem, field); });
            Host.Setup(h => h.GetMaskedFieldValue("ROWS", It.IsAny<string>(), It.IsAny<object?>()))
                .Returns((string block, string field, object? raw) =>
                { Dispatcher.AssertUi(); return Manager.GetMaskedFieldValue(block, field, raw); });
            View.SetupGet(v => v.BlockName).Returns(() => { Dispatcher.AssertUi(); return "rows"; });
            View.SetupGet(v => v.ManagerBlockName).Returns(() => { Dispatcher.AssertUi(); return "ROWS"; });
            View.SetupGet(v => v.FormsHost).Returns(() => { Dispatcher.AssertUi(); return BoundHost; });
            View.SetupGet(v => v.IsBound).Returns(() => { Dispatcher.AssertUi(); return BoundHost != null; });
            View.SetupGet(v => v.ViewState).Returns(() => { Dispatcher.AssertUi(); return State; });
            View.SetupGet(v => v.FieldPresenters).Returns(() => { Dispatcher.AssertUi(); return Presenters; });
            View.Setup(v => v.FindFieldPresenter(It.IsAny<string>())).Returns((string field) =>
            { Dispatcher.AssertUi(); return Presenters.SingleOrDefault(p => p.FieldName == field); });
            View.Setup(v => v.Bind(Host.Object)).Callback(() => { Dispatcher.AssertUi(); Binds++; BoundHost = Host.Object; });
            View.Setup(v => v.Unbind()).Callback(() => { Dispatcher.AssertUi(); Unbinds++; BoundHost = null; });
            View.Setup(v => v.SyncFromManager()).Callback(() => { Dispatcher.AssertUi(); Syncs++; State.IsDirty = Unit.IsDirty; });
            View.Setup(v => v.FocusField(It.IsAny<string>())).Returns(() => { Dispatcher.AssertUi(); Focuses++; return true; });
            Notifications.Setup(n => n.Publish(State, It.IsAny<string>(), It.IsAny<BeepMessageSeverity>()))
                .Callback((BeepViewState state, string text, BeepMessageSeverity severity) =>
                { Dispatcher.AssertUi(); Messages.Add(text); state.CurrentMessage = text; state.MessageSeverity = severity; });
            Notifications.Setup(n => n.Clear(State)).Callback(() => { Dispatcher.AssertUi(); Messages.Add("clear"); State.CurrentMessage = ""; });
            if (attach) Attach();
        }
        internal void Attach() => Dispatcher.OnUi(() => Binding = FormsViewBinding.Attach(Host.Object, View.Object, Dispatcher, Notifications.Object));
        internal Task Flush() => Dispatcher.PumpUntil(Binding!.WaitForPendingDeliveriesAsync());
        internal FormBindingTarget Capture()
        { Assert.True(Manager.TryCaptureBindingTarget("rows", out var target)); return target; }
        public void Dispose()
        {
            Dispatcher.OnUi(() => Binding?.Dispose()); Dispatcher.PumpAll();
            Manager.Dispose(); Wrapper.Dispose(); Unit.Dispose(); Rows.Dispose();
        }
    }

    [Fact]
    public async Task AttachDispatchesInitialRenderOnceAndProgrammaticEchoDoesNotWriteBack()
    {
        using var h = new Harness();
        Assert.Equal(1, h.Binds); Assert.Equal(0, h.Syncs);
        await h.Flush();
        Assert.Equal(1, h.Syncs); Assert.Equal(1, h.Name.Sets);
        Assert.Equal("first", h.Name.Mock.Object.Value);
        Assert.False(h.Unit.IsDirty);
        Assert.Equal(FormViewDeliveryState.Delivered, Assert.Single(h.Binding!.Outcomes).State);
    }

    [Fact]
    public async Task WorkerNotificationsDispatchOnceInOrderWithoutWorkerUiAccess()
    {
        using var h = new Harness(); await h.Flush();
        await Task.Run(() =>
        {
            h.Host.Raise(x => x.MessageRaised += null, new FormsHostMessageEventArgs("ROWS", "saved", MessageLevel.Info));
            h.Host.Raise(x => x.MessageRaised += null, new FormsHostMessageEventArgs("rows", "warning", MessageLevel.Warning));
            h.Host.Raise(x => x.MessageCleared += null, new FormsHostMessageEventArgs("ROWS", "", MessageLevel.Info));
        });
        Assert.Empty(h.Messages);
        await h.Flush();
        Assert.Equal(new[] { "saved", "warning", "clear" }, h.Messages);
        Assert.Equal(4, h.Dispatcher.Executed);
    }

    [Fact]
    public async Task ChangedViewStateDoesNotReceiveOldBindingNotifications()
    {
        using var h = new Harness(); await h.Flush();
        h.Host.Raise(x => x.MessageRaised += null, new FormsHostMessageEventArgs("ROWS", "old state", MessageLevel.Info));
        h.View.SetupGet(v => v.ViewState).Returns(new BeepViewState());
        await h.Flush();
        Assert.Empty(h.Messages); Assert.Equal("", h.State.CurrentMessage);
        Assert.Equal(FormViewDeliveryState.Superseded, h.Binding!.Outcomes.Last().State);
    }

    [Theory]
    [InlineData("close")]
    [InlineData("replacement")]
    [InlineData("detach")]
    [InlineData("host-manager")]
    public async Task QueuedOldBindingCannotRenderOrNotifyAfterRetirement(string retirement)
    {
        using var h = new Harness(); await h.Flush();
        var pending = h.Binding!.RequestRefreshAsync();
        h.Host.Raise(x => x.MessageRaised += null, new FormsHostMessageEventArgs("ROWS", "late", MessageLevel.Error));
        switch (retirement)
        {
            case "close": h.Manager.Dispose(); break;
            case "replacement": h.Manager.RegisterBlock("ROWS", h.Wrapper, h.Unit.EntityStructure, "db"); break;
            case "detach": h.Dispatcher.OnUi(h.Binding.Dispose); break;
            case "host-manager": h.Host.Object.FormsManager = new Mock<IUnitofWorksManager>().Object; break;
        }
        await h.Flush();
        Assert.NotEqual(FormViewDeliveryState.Delivered, (await pending).State);
        Assert.Equal(1, h.Syncs); Assert.Empty(h.Messages);
    }

    [Fact]
    public async Task DetachIsIdempotentAndUnsubscribesOnlyItsCapturedListeners()
    {
        using var h = new Harness(); await h.Flush();
        var otherListener = 0;
        h.Host.Object.MessageRaised += (_, _) => otherListener++;
        h.Dispatcher.OnUi(() => { h.Binding!.Dispose(); h.Binding.Dispose(); });
        var submitted = h.Dispatcher.Submitted;
        h.Host.Raise(x => x.MessageRaised += null, new FormsHostMessageEventArgs("ROWS", "external", MessageLevel.Info));
        h.Rows[0].Name = "retained source";
        h.Dispatcher.OnUi(() => h.Name.Edit("detached"));
        Assert.Equal(1, otherListener); Assert.Equal(1, h.Unbinds);
        Assert.Equal(submitted, h.Dispatcher.Submitted);
        Assert.Equal("retained source", h.Rows[0].Name);
        Assert.True(h.Manager.TryCaptureBindingTarget("ROWS", out _));
    }

    [Theory]
    [InlineData("move")]
    [InlineData("aba")]
    [InlineData("edit-aba")]
    [InlineData("cancel")]
    public async Task CapturedFocusCannotFocusAChangedOrCancelledRecord(string change)
    {
        using var h = new Harness(); await h.Flush();
        using var cancellation = new CancellationTokenSource();
        var focus = h.Binding!.FocusAsync(h.Capture(), "Name", cancellation.Token);
        switch (change)
        {
            case "move": h.Unit.MoveNext(); break;
            case "aba": h.Unit.MoveNext(); h.Unit.MoveFirst(); break;
            case "edit-aba": h.Rows[0].Name = "changed"; h.Rows[0].Name = "first"; break;
            case "cancel": cancellation.Cancel(); break;
        }
        await h.Flush();
        Assert.Equal(change == "cancel" ? FormViewDeliveryState.Cancelled : FormViewDeliveryState.Superseded, (await focus).State);
        Assert.Equal(0, h.Focuses);
    }

    [Fact]
    public async Task CurrentFocusUsesAcceptedIdentityAndReportsAcknowledgement()
    {
        using var h = new Harness(); await h.Flush();
        var first = h.Capture(); var second = h.Capture();
        Assert.True(h.Manager.IsBindingTargetCurrent(first));
        Assert.True(h.Manager.IsBindingTargetCurrent(second));
        var focus = h.Binding!.FocusAsync(first, "Name"); await h.Flush();
        Assert.Equal(FormViewDeliveryState.Delivered, (await focus).State);
        Assert.True((await focus).Acknowledged); Assert.Equal(1, h.Focuses);
    }

    [Fact]
    public async Task NullUserEditIsCapturedAndOldTokenCannotReplayTheWrite()
    {
        using var h = new Harness(); await h.Flush();
        var prior = h.Capture();
        h.Dispatcher.OnUi(() => h.Name.Edit(null)); await h.Flush();
        Assert.Null(h.Rows[0].Name); Assert.Null(h.Name.Mock.Object.Value);
        Assert.False(h.Manager.IsBindingTargetCurrent(prior));
        var replay = h.Manager.ApplyBindingValue(prior, "Name", "replayed");
        Assert.Equal(FormViewDeliveryState.Superseded, replay.State); Assert.False(replay.EffectsPossible);
    }

    [Fact]
    public async Task UnrelatedUserEditInsidePresentationIsNotGloballySuppressed()
    {
        using var h = new Harness(); await h.Flush();
        h.Name.OnSet = () => { h.Name.OnSet = null; h.Other.Edit("user other"); };
        _ = h.Binding!.RequestRefreshAsync(); await h.Flush();
        Assert.Equal("user other", h.Rows[0].Other);
        Assert.Equal("first", h.Rows[0].Name);
        Assert.Contains(h.Binding.Outcomes, r => r.State == FormViewDeliveryState.Delivered && r.Acknowledged);
    }

    [Fact]
    public async Task OriginAwareDelayedAndRetiredBindingEchoesNeverReenterRecordWrites()
    {
        using var h = new Harness(stamped: true); await h.Flush();
        var before = h.Dispatcher.Submitted;
        h.Name.Echo(); Assert.Equal(before, h.Dispatcher.Submitted); Assert.False(h.Unit.IsDirty);
        h.Dispatcher.OnUi(h.Binding!.Dispose);
        h.Attach(); await h.Flush();
        before = h.Dispatcher.Submitted;
        h.Name.Echo(0); Assert.Equal(before, h.Dispatcher.Submitted);
        h.Dispatcher.OnUi(() => h.Name.Edit("real edit")); await h.Flush();
        Assert.Equal("real edit", h.Rows[0].Name);
        Assert.NotEqual(h.Name.Stamps[0].Origin, h.Name.Stamps[1].Origin);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MaskingNullOrFailureNeverFallsBackToRawPresentation(bool failure)
    {
        using var h = new Harness(); await h.Flush();
        h.Host.Setup(x => x.GetMaskedFieldValue("ROWS", "Name", It.IsAny<object?>()))
            .Returns(() => failure ? throw new InvalidOperationException("masking failed") : null);
        var delivery = h.Binding!.RequestRefreshAsync(); await h.Flush();
        Assert.Null(h.Name.Mock.Object.Value);
        Assert.Equal(failure ? FormViewDeliveryState.Failed : FormViewDeliveryState.Delivered, (await delivery).State);
        Assert.True((await delivery).EffectsPossible);
        Assert.Equal("first", h.Rows[0].Name); Assert.False(h.Unit.IsDirty);
    }

    [Fact]
    public void ManagerMaskingPreservesNullFromInjectedSecurityHelper()
    {
        var security = new Mock<ISecurityManager>();
        security.Setup(s => s.GetMaskedValue("ROWS", "Name", "raw")).Returns((object)null!);
        using var manager = new FormsManager(new Mock<IDMEEditor>().Object, securityManager: security.Object);
        Assert.Null(manager.GetMaskedFieldValue("ROWS", "Name", "raw"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BadDispatcherIsReportedWithoutWorkerUiMutation(bool wrongContext)
    {
        using var h = new Harness(); await h.Flush();
        h.Dispatcher.WrongContextNext = wrongContext; h.Dispatcher.FailNext = !wrongContext;
        var delivery = h.Binding!.RequestRefreshAsync(); await h.Flush();
        Assert.Equal(FormViewDeliveryState.Failed, (await delivery).State);
        Assert.Equal(1, h.Syncs); Assert.False((await delivery).EffectsPossible);
        Assert.Equal(0, h.Binding.PendingDeliveries);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DuplicateOrLateDispatcherExecutionCannotTouchControlsTwice(bool late)
    {
        using var h = new Harness(); await h.Flush();
        h.Dispatcher.SkipNext = late; h.Dispatcher.DuplicateNext = !late;
        var delivery = h.Binding!.RequestRefreshAsync(); await h.Flush();
        Assert.Equal(FormViewDeliveryState.Failed, (await delivery).State);
        Assert.Equal(late ? 1 : 2, h.Syncs);
        if (late)
        {
            h.Dispatcher.OnUi(() => Assert.Throws<InvalidOperationException>(h.Dispatcher.Skipped!));
            Assert.Equal(1, h.Syncs); Assert.False((await delivery).EffectsPossible);
        }
        else { Assert.True((await delivery).EffectsPossible); Assert.True((await delivery).Acknowledged); }
    }

    [Fact]
    public async Task DispatcherEarlyAcknowledgementCannotReleaseAnAlreadyRunningAction()
    {
        using var h = new Harness(); await h.Flush();
        using var entered = new ManualResetEventSlim();
        using var released = new ManualResetEventSlim();
        h.View.Setup(v => v.SyncFromManager()).Callback(() =>
        { h.Dispatcher.AssertUi(); entered.Set(); Assert.True(released.Wait(TimeSpan.FromSeconds(10))); });
        h.Dispatcher.EarlyNext = true;
        h.Dispatcher.WaitUntilEarlyActionStarts = () => Assert.True(entered.Wait(TimeSpan.FromSeconds(10)));
        var delivery = h.Binding!.RequestRefreshAsync();
        Assert.True(h.Dispatcher.PumpOne());
        var drain = h.Binding.WaitForPendingDeliveriesAsync();
        Assert.False(delivery.IsCompleted); Assert.False(drain.IsCompleted); Assert.Equal(1, h.Binding.PendingDeliveries);
        released.Set();
        await h.Dispatcher.PumpUntil(delivery);
        await h.Dispatcher.EarlyWork!.WaitAsync(TimeSpan.FromSeconds(10));
        await drain.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(FormViewDeliveryState.Delivered, (await delivery).State); Assert.True((await delivery).Acknowledged);
    }

    [Fact]
    public async Task AsyncDetachWaitsForQueuedPhysicalAcknowledgement()
    {
        using var h = new Harness(); await h.Flush();
        var late = h.Binding!.RequestRefreshAsync();
        var detach = h.Binding.DisposeAsync().AsTask();
        Assert.False(detach.IsCompleted);
        await h.Dispatcher.PumpUntil(detach);
        Assert.Equal(FormViewDeliveryState.Cancelled, (await late).State);
        Assert.Equal(1, h.Unbinds); Assert.Equal(0, h.Binding.PendingDeliveries);
    }

    [Fact]
    public async Task MissingCleanupDispatchIsReportedAndLateDetachCannotStealTheUiRepair()
    {
        using var h = new Harness(); await h.Flush();
        h.Dispatcher.SkipNext = true;
        var dispose = h.Binding!.DisposeAsync().AsTask();
        await h.Dispatcher.PumpUntil(dispose);
        Assert.Single(h.Binding.CleanupFailures); Assert.Equal(0, h.Unbinds);
        h.Dispatcher.OnUi(() => Assert.Throws<InvalidOperationException>(h.Dispatcher.Skipped!));
        h.Dispatcher.OnUi(h.Binding.Dispose);
        Assert.Equal(1, h.Unbinds); Assert.True(h.Manager.TryCaptureBindingTarget("ROWS", out _));
    }

    [Fact]
    public async Task SelfDrainRejectsAndSynchronousDetachCanFinishOutsideDelivery()
    {
        using var h = new Harness(); await h.Flush();
        h.View.Setup(v => v.SyncFromManager()).Callback(() =>
        {
            Assert.Throws<InvalidOperationException>(() => { _ = h.Binding!.WaitForPendingDeliveriesAsync(); });
            Assert.Throws<InvalidOperationException>(() => { _ = h.Binding!.DisposeAsync(); });
            h.Binding!.Dispose();
        });
        var delivery = h.Binding!.RequestRefreshAsync(); await h.Flush();
        Assert.Equal(FormViewDeliveryState.Cancelled, (await delivery).State);
        Assert.True((await delivery).EffectsPossible); Assert.Equal(1, h.Unbinds);
    }

    [Fact]
    public async Task FailedSubscriptionAttachUnwindsViewAndListenersWithoutRetiringManager()
    {
        using var h = new Harness(attach: false);
        h.Other.Mock.SetupAdd(p => p.ValueChanged += It.IsAny<EventHandler<object?>>()).Throws(new IOException("add failed"));
        h.Dispatcher.OnUi(() => Assert.Throws<IOException>(h.Attach));
        h.Host.Raise(x => x.MessageRaised += null, new FormsHostMessageEventArgs("ROWS", "unused", MessageLevel.Info));
        Assert.Equal(0, h.Binds); Assert.Null(h.BoundHost); Assert.Equal(0, h.Dispatcher.Submitted);
        Assert.True(h.Manager.TryCaptureBindingTarget("ROWS", out _));
        h.Other.Mock.SetupAdd(p => p.ValueChanged += It.IsAny<EventHandler<object?>>());
        h.Attach(); await h.Flush(); Assert.Equal(1, h.Binds);
    }

    [Fact]
    public async Task RemoveAccessorFailureDoesNotAbortRemainingDetachmentOrDispatchRetirement()
    {
        using var h = new Harness(); await h.Flush();
        h.Name.Mock.SetupRemove(p => p.ValueChanged -= It.IsAny<EventHandler<object?>>()).Throws(new IOException("remove failed"));
        h.Dispatcher.OnUi(h.Binding!.Dispose);
        Assert.Single(h.Binding.CleanupFailures); Assert.Equal(1, h.Unbinds);
        var before = h.Dispatcher.Submitted;
        h.Dispatcher.OnUi(() => { h.Name.Edit("retired"); h.Other.Edit("retired"); });
        Assert.Equal(before, h.Dispatcher.Submitted); Assert.Equal("first", h.Rows[0].Name);
        h.Other.Mock.VerifyRemove(p => p.ValueChanged -= It.IsAny<EventHandler<object?>>(), Times.Once);
    }

    [Fact]
    public async Task DuplicateAttachCannotStealOrDetachExistingOwner()
    {
        using var h = new Harness(); await h.Flush();
        var original = h.Binding;
        h.Dispatcher.OnUi(() => Assert.Throws<InvalidOperationException>(() => FormsViewBinding.Attach(h.Host.Object, h.View.Object, h.Dispatcher)));
        Assert.Equal(1, h.Binds); Assert.Equal(0, h.Unbinds);
        Assert.Same(original, h.Binding);
    }

    [Theory]
    [InlineData("disabled")]
    [InlineData("hidden")]
    [InlineData("readonly")]
    [InlineData("masked")]
    public async Task PresentationMetadataAndCapturedWriteEnforceCurrentPermissions(string policy)
    {
        using var h = new Harness(); await h.Flush();
        var item = h.Manager.ItemProperties.GetItem("ROWS", "Name");
        switch (policy)
        {
            case "disabled": item.Enabled = false; break;
            case "hidden": item.Visible = false; break;
            case "readonly": h.Manager.GetBlock("ROWS").Mode = DataBlockMode.ReadOnly; break;
            case "masked": h.Manager.SetFieldSecurity("ROWS", "Name", new FieldSecurity { Masked = true }); break;
        }
        var denied = h.Manager.ApplyBindingValue(h.Capture(), "Name", "denied");
        Assert.Equal(FormViewDeliveryState.Rejected, denied.State); Assert.False(denied.EffectsPossible);
        _ = h.Binding!.RequestRefreshAsync(); await h.Flush();
        Assert.True(h.Name.Mock.Object.IsReadOnly);
        if (policy == "hidden") { Assert.False(h.Name.Mock.Object.IsVisible); Assert.Null(h.Name.Mock.Object.Value); }
        if (policy == "masked") Assert.NotEqual("first", h.Name.Mock.Object.Value);
        Assert.Equal("first", h.Rows[0].Name);
    }

    [Fact]
    public async Task PresenterReplacementInvalidatesOldRosterWithoutWritingRemovedControls()
    {
        using var h = new Harness(); await h.Flush();
        var queued = h.Binding!.RequestRefreshAsync();
        var replacement = new Presenter(h.Dispatcher, "Name");
        h.Presenters[0] = replacement.Mock.Object;
        await h.Flush();
        Assert.Equal(FormViewDeliveryState.Superseded, (await queued).State);
        Assert.Equal(1, h.Name.Sets); Assert.Equal(0, replacement.Sets);
        h.Dispatcher.OnUi(() => h.Name.Edit("removed")); await h.Flush();
        Assert.Equal("first", h.Rows[0].Name);
    }

    [Fact]
    public async Task CancellationInsideFirstPresentationStopsRemainingFieldDelivery()
    {
        using var h = new Harness(); await h.Flush();
        using var cancellation = new CancellationTokenSource();
        h.Name.OnSet = cancellation.Cancel;
        var queued = h.Binding!.RequestRefreshAsync(cancellation.Token); await h.Flush();
        Assert.Equal(FormViewDeliveryState.Cancelled, (await queued).State);
        Assert.True((await queued).EffectsPossible); Assert.False((await queued).Acknowledged);
        Assert.Equal(2, h.Name.Sets); Assert.Equal(1, h.Other.Sets);
    }

    [Fact]
    public async Task ChangedSecurityRevisionAndAbaContextInvalidateQueuedFocus()
    {
        using var h = new Harness(); await h.Flush();
        var target = h.Capture();
        var queued = h.Binding!.FocusAsync(target, "Name");
        h.Manager.SetSecurityContext(new SecurityContext { UserName = "other" });
        h.Manager.SetSecurityContext(new SecurityContext());
        await h.Flush();
        Assert.False(h.Manager.IsBindingTargetCurrent(target));
        Assert.True(h.Manager.IsBindingTargetCurrent(target, includeRecord: false));
        Assert.Equal(FormViewDeliveryState.Superseded, (await queued).State); Assert.Equal(0, h.Focuses);
    }

    [Fact]
    public async Task PolicyChangeInsidePresenterFlagsCannotPublishPreparedRawText()
    {
        using var h = new Harness(); await h.Flush();
        h.Name.Mock.SetupSet(p => p.IsReadOnly = false).Callback(() =>
            h.Manager.SetFieldSecurity("ROWS", "Name", new FieldSecurity { Masked = true }));
        var queued = h.Binding!.RequestRefreshAsync(); await h.Flush();
        Assert.Equal(FormViewDeliveryState.Superseded, (await queued).State);
        Assert.Equal(new object?[] { "first", "*****" }, h.Name.Presented);
        Assert.Equal(2, h.Other.Sets);
        Assert.True(h.Name.Mock.Object.IsReadOnly);
        Assert.Equal("first", h.Rows[0].Name); Assert.False(h.Unit.IsDirty);
    }

    [Fact]
    public async Task FocusRefusalIsRejectedAndNeverAcknowledged()
    {
        using var h = new Harness(); await h.Flush();
        h.View.Setup(v => v.FocusField("Name")).Returns(false);
        var queued = h.Binding!.FocusAsync(h.Capture(), "Name"); await h.Flush();
        Assert.Equal(FormViewDeliveryState.Rejected, (await queued).State);
        Assert.True((await queued).EffectsPossible); Assert.False((await queued).Acknowledged);
    }

    private static void BindLegacyRecord(Harness h, object row)
    {
        h.Dispatcher.OnUi(h.Binding!.Dispose);
        var source = new Mock<IUnitofWork>();
        source.SetupGet(u => u.CurrentItem).Returns(row);
        source.SetupGet(u => u.Units).Returns(new[] { row });
        h.Manager.RegisterBlock("ROWS", source.Object, h.Unit.EntityStructure, "db");
        h.Manager.GetBlock("ROWS").Mode = DataBlockMode.CRUD;
        h.Host.Setup(x => x.GetFieldValue("ROWS", It.IsAny<string>())).Returns((string _, string field) =>
        { h.Dispatcher.AssertUi(); return h.Manager.GetFieldValue(row, field); });
        h.Attach();
    }

    private static async Task ReadAuthorizedBuffer(Harness h, int id = 8, string name = "authorized")
    {
        h.Source.Setup(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<TheTechIdea.Beep.Report.AppFilter>>()))
            .ReturnsAsync(new object[] { new Row { Id = id, Name = name } });
        var result = await h.Manager.ExecuteQueryWithOutcomeAsync("ROWS");
        Assert.Equal(FormQueryState.Completed, result.State);
        Assert.True(result.RecordsPublished);
        h.Unit.MoveFirst();
    }

    [Theory]
    [InlineData("principal")]
    [InlineData("roles")]
    [InlineData("claims")]
    [InlineData("row filter")]
    [InlineData("query denied")]
    [InlineData("ABA")]
    public async Task FreshBindingCannotBlessBufferFromOlderReadAuthorization(string change)
    {
        using var h = new Harness(); await h.Flush();
        var old = h.Capture();
        if (change == "principal" || change == "ABA") h.Manager.SetSecurityContext(new SecurityContext { UserName = "other" });
        if (change == "roles") h.Manager.SetSecurityContext(new SecurityContext { Roles = new List<string> { "tenant" } });
        if (change == "claims") h.Manager.SetSecurityContext(new SecurityContext { Claims = new Dictionary<string, string> { ["tenant"] = "2" } });
        if (change == "row filter") h.Manager.SetBlockSecurity("ROWS", new BlockSecurity { RowFilterClause = "Id = 2" });
        if (change == "query denied") h.Manager.SetBlockSecurity("ROWS", new BlockSecurity { AllowQuery = false });
        if (change == "ABA") h.Manager.SetSecurityContext(new SecurityContext());
        Assert.False(h.Manager.TryCaptureBindingTarget("ROWS", out _));
        Assert.False(h.Manager.IsBindingTargetCurrent(old));
        h.Host.Invocations.Clear();
        Assert.Equal(FormViewDeliveryState.Superseded, (await h.Binding!.RequestRefreshAsync()).State);
        h.Host.Verify(x => x.GetFieldValue(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        Assert.Equal(1, h.Name.Sets);
        h.Dispatcher.OnUi(() => h.Name.Edit("unauthorized")); await h.Flush();
        Assert.Equal("first", h.Rows[0].Name);
        Assert.False(h.Unit.IsDirty);
    }

    [Theory]
    [InlineData("typed")]
    [InlineData("basic")]
    [InlineData("enhanced")]
    public async Task ManagedQueryRestoresBindingForNewPrincipalAndTenant(string route)
    {
        using var h = new Harness(); await h.Flush();
        h.Manager.SetSecurityContext(new SecurityContext { UserName = "tenant-2" });
        h.Manager.SetBlockSecurity("ROWS", new BlockSecurity { RowFilterClause = "Id = :tenant",
            RowFilterValues = new Dictionary<string, object> { ["tenant"] = 2 } });
        List<TheTechIdea.Beep.Report.AppFilter>? received = null;
        h.Source.Setup(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<TheTechIdea.Beep.Report.AppFilter>>()))
            .Callback((string _, List<TheTechIdea.Beep.Report.AppFilter> filters) => received = filters)
            .ReturnsAsync(new object[] { new Row { Id = 2, Name = "tenant-2 row" } });
        Assert.False(h.Manager.TryCaptureBindingTarget("ROWS", out _));
        if (route == "basic") Assert.True(await h.Manager.ExecuteQueryAsync("ROWS"));
        else
        {
            var result = route == "typed" ? await h.Manager.ExecuteQueryWithOutcomeAsync("ROWS") :
                Assert.IsType<FormQueryResult>(await h.Manager.ExecuteQueryEnhancedAsync("ROWS"));
            Assert.True(result.RecordsPublished);
        }
        Assert.Contains(received!, f => f.FieldName == "Id" && f.FilterValue == "2");
        h.Unit.MoveFirst();
        Assert.True(h.Manager.TryCaptureBindingTarget("ROWS", out _));
        var refresh = h.Binding!.RequestRefreshAsync(); await h.Flush();
        Assert.Equal(FormViewDeliveryState.Delivered, (await refresh).State);
        Assert.Equal("tenant-2 row", h.Name.Mock.Object.Value);
        h.Dispatcher.OnUi(() => h.Name.Edit("tenant-2 edit")); await h.Flush();
        Assert.Equal("tenant-2 edit", h.Unit.CurrentItem.Name);
        Assert.Equal("first", h.Rows[0].Name);
        Assert.True(h.Manager.TryCaptureBindingTarget("ROWS", out _));
    }

    [Fact]
    public async Task FieldOnlyPolicyChangeRemasksAuthorizedBufferWithoutProviderRead()
    {
        using var h = new Harness(); await h.Flush();
        h.Manager.SetSecurityContext(new SecurityContext { UserName = "tenant" });
        await ReadAuthorizedBuffer(h);
        var old = h.Capture();
        h.Source.Invocations.Clear();
        h.Manager.SetFieldSecurity("ROWS", "Name", new FieldSecurity { Masked = true, MaskPattern = "*" });
        Assert.False(h.Manager.IsBindingTargetCurrent(old));
        Assert.True(h.Manager.TryCaptureBindingTarget("ROWS", out _));
        var refresh = h.Binding!.RequestRefreshAsync(); await h.Flush();
        Assert.Equal(FormViewDeliveryState.Delivered, (await refresh).State);
        Assert.Equal("**********", h.Name.Mock.Object.Value);
        Assert.True(h.Name.Mock.Object.IsReadOnly);
        h.Source.Verify(x => x.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<TheTechIdea.Beep.Report.AppFilter>>()), Times.Never);
    }

    [Theory]
    [InlineData("replace")]
    [InlineData("replace ABA")]
    [InlineData("clear")]
    [InlineData("source")]
    [InlineData("entity")]
    [InlineData("schema")]
    [InlineData("raw Get")]
    [InlineData("default clause")]
    [InlineData("block entity")]
    public async Task BufferOrProviderReplacementCannotInheritManagedAuthorization(string change)
    {
        using var h = new Harness(); await h.Flush();
        h.Manager.SetSecurityContext(new SecurityContext { UserName = "tenant" });
        await ReadAuthorizedBuffer(h);
        var old = h.Capture();
        var prior = h.Unit.Units;
        if (change == "replace" || change == "replace ABA")
        {
            h.Unit.Units = new ObservableBindingList<Row>(new List<Row> { new() { Id = 99, Name = "foreign" } });
            if (change == "replace ABA") h.Unit.Units = prior;
        }
        if (change == "clear") h.Unit.Clear();
        if (change == "source") h.Unit.DataSource = new Mock<IDataSource>().Object;
        if (change == "entity") h.Unit.EntityName = "Foreign";
        if (change == "schema") h.Manager.GetBlock("ROWS").EntityStructure.Fields[0].FieldName = "ForeignId";
        if (change == "raw Get") await h.Unit.Get();
        if (change == "default clause") h.Manager.SetDefaultWhere("ROWS", "Id = 99");
        if (change == "block entity") h.Manager.GetBlock("ROWS").EntityStructure.EntityName = "Foreign";
        Assert.False(h.Manager.TryCaptureBindingTarget("ROWS", out _));
        Assert.False(h.Manager.IsBindingTargetCurrent(old));
        h.Host.Invocations.Clear();
        Assert.Equal(FormViewDeliveryState.Superseded, (await h.Binding!.RequestRefreshAsync()).State);
        h.Host.Verify(x => x.GetFieldValue(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ReRegistrationUnderConfiguredContextCannotRecertifyOldBuffer()
    {
        using var h = new Harness(attach: false);
        h.Manager.SetSecurityContext(new SecurityContext { UserName = "other" });
        h.Manager.RegisterBlock("ROWS", h.Wrapper, h.Unit.EntityStructure, "db");
        Assert.False(h.Manager.TryCaptureBindingTarget("ROWS", out _));
        await ReadAuthorizedBuffer(h);
        Assert.True(h.Manager.TryCaptureBindingTarget("ROWS", out _));
    }

    [Fact]
    public async Task AuthorizationReceiptIsAvailableToPostQueryObserverBeforeItsNotification()
    {
        using var h = new Harness(attach: false);
        h.Manager.SetSecurityContext(new SecurityContext { UserName = "tenant" });
        var observed = false;
        h.Unit.PostQuery += (_, _) => observed = h.Manager.TryCaptureBindingTarget("ROWS", out _);
        await ReadAuthorizedBuffer(h);
        Assert.True(observed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedRequeryDoesNotRecertifyBufferForChangedPrincipal(bool changePrincipal)
    {
        using var h = new Harness(attach: false);
        h.Manager.SetSecurityContext(new SecurityContext { UserName = "first" });
        await ReadAuthorizedBuffer(h);
        var rows = h.Unit.Units;
        if (changePrincipal) h.Manager.SetSecurityContext(new SecurityContext { UserName = "second" });
        h.Source.Setup(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<TheTechIdea.Beep.Report.AppFilter>>()))
            .ThrowsAsync(new IOException("read failed"));
        var result = await h.Manager.ExecuteQueryWithOutcomeAsync("ROWS");
        Assert.False(result.RecordsPublished);
        Assert.Same(rows, h.Unit.Units);
        Assert.Equal(!changePrincipal, h.Manager.TryCaptureBindingTarget("ROWS", out _));
    }

    [Fact]
    public async Task PolicyChangeDuringPhysicalReadCannotAuthorizeOldOrCandidateBuffer()
    {
        using var h = new Harness(attach: false);
        h.Manager.SetSecurityContext(new SecurityContext { UserName = "first" });
        await ReadAuthorizedBuffer(h);
        var rows = h.Unit.Units;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        h.Source.Setup(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<TheTechIdea.Beep.Report.AppFilter>>()))
            .Returns(async () => { entered.SetResult(); await release.Task; return new object[] { new Row { Id = 9 } }; });
        var query = h.Manager.ExecuteQueryWithOutcomeAsync("ROWS");
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            h.Manager.SetSecurityContext(new SecurityContext { UserName = "second" });
            Assert.False(h.Manager.TryCaptureBindingTarget("ROWS", out _));
        }
        finally { release.TrySetResult(); }
        var outcome = await query.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.False(outcome.RecordsPublished);
        Assert.Same(rows, h.Unit.Units);
        Assert.False(h.Manager.TryCaptureBindingTarget("ROWS", out _));
    }

    [Fact]
    public async Task ManagedDetailPublicationAuthorizesBufferUnderNewContext()
    {
        using var h = new Harness(attach: false);
        h.Manager.SetSecurityContext(new SecurityContext { UserName = "tenant" });
        var master = new Mock<IUnitofWork>();
        master.SetupProperty(u => u.EntityStructure); master.SetupProperty(u => u.DataSource);
        master.SetupGet(u => u.CurrentItem).Returns(new Row { Id = 2 });
        h.Manager.RegisterBlock("MASTER", master.Object, h.Unit.EntityStructure);
        h.Manager.CreateMasterDetailRelation("MASTER", "ROWS", "Id", "Id");
        h.Source.Setup(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<TheTechIdea.Beep.Report.AppFilter>>()))
            .ReturnsAsync(new object[] { new Row { Id = 2, Name = "detail" } });
        Assert.False(h.Manager.TryCaptureBindingTarget("ROWS", out _));
        var result = await h.Manager.SynchronizeDetailBlocksWithOutcomeAsync("MASTER");
        Assert.True(Assert.Single(result.Details).RecordsPublished);
        h.Unit.MoveFirst();
        Assert.True(h.Manager.TryCaptureBindingTarget("ROWS", out var target));
        Assert.Equal("detail", Assert.IsType<Row>(target.Record).Name);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UowTenantScopeChangeAndAbaInvalidateBufferReceipt(bool aba)
    {
        using var h = new Harness(attach: false);
        h.Unit.ScopeToTenant("Id", "1");
        await ReadAuthorizedBuffer(h, id: 1);
        Assert.True(h.Manager.TryCaptureBindingTarget("ROWS", out _));
        h.Unit.ScopeToTenant("Id", "2");
        if (aba) h.Unit.ScopeToTenant("Id", "1");
        Assert.False(h.Manager.TryCaptureBindingTarget("ROWS", out _));
        await ReadAuthorizedBuffer(h, id: aba ? 1 : 2);
        Assert.True(h.Manager.TryCaptureBindingTarget("ROWS", out _));
    }

    [Fact]
    public async Task DisposedWrapperKeepsCapabilityIdentityAndFailsClosed()
    {
        using var h = new Harness(attach: false);
        await ReadAuthorizedBuffer(h);
        h.Wrapper.Dispose();
        var buffers = Assert.IsAssignableFrom<IUnitofWorkReadBufferIdentity>(h.Wrapper);
        Assert.True(buffers.SupportsReadBufferIdentity);
        Assert.False(buffers.TryGetReadBufferIdentity(out _));
        Assert.False(h.Manager.TryCaptureBindingTarget("ROWS", out _));
    }

    [Fact]
    public async Task TenantScopeAbaDuringProviderReadCannotPublishCandidateOrReceipt()
    {
        using var h = new Harness(attach: false);
        h.Unit.ScopeToTenant("Id", "1");
        await ReadAuthorizedBuffer(h, id: 1);
        var rows = h.Unit.Units;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        h.Source.Setup(s => s.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<TheTechIdea.Beep.Report.AppFilter>>()))
            .Returns(async () => { entered.SetResult(); await release.Task; return new object[] { new Row { Id = 1 } }; });
        var query = h.Manager.ExecuteQueryWithOutcomeAsync("ROWS");
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            h.Unit.ScopeToTenant("Id", "2"); h.Unit.ScopeToTenant("Id", "1");
        }
        finally { release.TrySetResult(); }
        var outcome = await query.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.False(outcome.RecordsPublished);
        Assert.Same(rows, h.Unit.Units);
        Assert.False(h.Manager.TryCaptureBindingTarget("ROWS", out _));
    }

    [Fact]
    public async Task RegistrationOfTenantScopedPreloadedRowsRequiresManagedPublication()
    {
        using var h = new Harness(attach: false);
        h.Unit.ScopeToTenant("Id", "2");
        h.Manager.RegisterBlock("ROWS", h.Wrapper, h.Unit.EntityStructure, "db");
        Assert.False(h.Manager.TryCaptureBindingTarget("ROWS", out _));
        Assert.Equal(1, h.Unit.CurrentItem.Id);
        await ReadAuthorizedBuffer(h, id: 2);
        Assert.True(h.Manager.TryCaptureBindingTarget("ROWS", out var target));
        Assert.Equal(2, Assert.IsType<Row>(target.Record).Id);
    }

    [Theory]
    [InlineData("context")]
    [InlineData("block")]
    [InlineData("clear")]
    [InlineData("direct helper")]
    public async Task PolicyPublicationAutomaticallyClearsPresentationWithoutReadingOrDiscardingRows(string route)
    {
        using var h = new Harness(); await h.Flush();
        h.Rows[0].Name = "dirty private text"; await h.Flush();
        h.Name.Mock.Object.ValidationError = "private validation";
        h.State.StatusText = h.State.WorkflowText = h.State.RecordPositionText = h.State.AggregateText = "private state";
        h.State.CurrentMessage = "private message";
        h.State.WorkflowHistoryItems.Add(new BeepWorkflowEntry { Text = "private history" });
        h.State.ErrorCount = 1; h.State.FirstErrorFieldName = "Name";
        h.Host.Invocations.Clear();
        await Task.Run(() =>
        {
            if (route == "context") h.Manager.SetSecurityContext(new SecurityContext { UserName = "new principal" });
            if (route == "block") h.Manager.SetBlockSecurity("ROWS", new BlockSecurity { RowFilterClause = "Id = 2" });
            if (route == "clear") h.Manager.ClearBlockSecurity("ROWS");
            if (route == "direct helper") h.Manager.Security.SetSecurityContext(new SecurityContext { UserName = "new principal" });
        });
        await h.Flush();
        Assert.Null(h.Name.Mock.Object.Value); Assert.Null(h.Other.Mock.Object.Value);
        Assert.False(h.Name.Mock.Object.IsVisible); Assert.False(h.Name.Mock.Object.IsEnabled); Assert.True(h.Name.Mock.Object.IsReadOnly);
        Assert.Null(h.Name.Mock.Object.ValidationError);
        Assert.Empty(h.State.StatusText); Assert.Empty(h.State.WorkflowText); Assert.Empty(h.State.RecordPositionText);
        Assert.Empty(h.State.AggregateText); Assert.Empty(h.State.CurrentMessage); Assert.Empty(h.State.WorkflowHistoryItems);
        Assert.Equal(0, h.State.ErrorCount); Assert.Null(h.State.FirstErrorFieldName);
        Assert.True(h.State.IsDirty); Assert.True(h.Unit.IsDirty);
        Assert.Same(h.Rows, h.Unit.Units); Assert.Equal("dirty private text", h.Rows[0].Name);
        h.Host.Verify(x => x.GetFieldValue(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        Assert.Contains(h.Binding!.Outcomes, r => r.State == FormViewDeliveryState.Delivered && r.Acknowledged);
    }

    [Fact]
    public async Task ClearingDeniedPolicyRestoresUiOnlyAfterAReauthorizedRead()
    {
        using var h = new Harness(); await h.Flush();
        h.Manager.SetBlockSecurity("ROWS", new BlockSecurity { AllowQuery = false });
        h.Manager.SetFieldSecurity("ROWS", "Name", new FieldSecurity { Visible = false, Editable = false });
        await h.Flush();
        h.Manager.ClearBlockSecurity("ROWS"); await h.Flush();
        Assert.True(h.Manager.GetBlock("ROWS").QueryAllowed);
        Assert.True(h.Manager.ItemProperties.GetItem("ROWS", "Name").Visible);
        Assert.Null(h.Name.Mock.Object.Value); Assert.False(h.Name.Mock.Object.IsVisible);
        Assert.False(h.Manager.TryCaptureBindingTarget("ROWS", out _));
        await ReadAuthorizedBuffer(h);
        var refresh = h.Binding!.RequestRefreshAsync(); await h.Flush();
        Assert.Equal(FormViewDeliveryState.Delivered, (await refresh).State);
        Assert.NotNull(h.Name.Mock.Object.Value); Assert.True(h.Name.Mock.Object.IsVisible);
        Assert.True(h.Name.Mock.Object.IsEnabled);
    }

    [Theory]
    [InlineData("masked")]
    [InlineData("hidden")]
    [InlineData("read only")]
    public async Task FieldOnlyPublicationAutomaticallyRepaintsCurrentAuthorizedBuffer(string policy)
    {
        using var h = new Harness(); await h.Flush();
        h.Source.Invocations.Clear();
        await Task.Run(() => h.Manager.SetFieldSecurity("ROWS", "Name", new FieldSecurity
        { Masked = policy == "masked", Visible = policy != "hidden", Editable = policy != "read only" }));
        await h.Flush();
        Assert.Equal(policy == "masked" ? "*****" : policy == "hidden" ? null : "first", h.Name.Mock.Object.Value);
        Assert.Equal(policy != "hidden", h.Name.Mock.Object.IsVisible);
        Assert.True(h.Name.Mock.Object.IsReadOnly);
        Assert.Equal("first", h.Rows[0].Name); Assert.False(h.Unit.IsDirty);
        h.Source.Verify(x => x.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<TheTechIdea.Beep.Report.AppFilter>>()), Times.Never);
    }

    [Fact]
    public async Task BurstPoliciesCoalesceAndCannotReExposeAnIntermediateUnmask()
    {
        using var h = new Harness(); await h.Flush();
        h.Dispatcher.OnUi(() =>
        {
            h.Manager.SetFieldSecurity("ROWS", "Name", new FieldSecurity { Masked = true });
            h.Manager.SetFieldSecurity("ROWS", "Name", new FieldSecurity());
            h.Manager.SetFieldSecurity("ROWS", "Name", new FieldSecurity { Masked = true });
        });
        await h.Flush();
        Assert.Equal("*****", h.Name.Mock.Object.Value);
        Assert.All(h.Name.Presented.Skip(1), value => Assert.Equal("*****", value));
        Assert.Equal(0, h.Binding!.PendingDeliveries);
    }

    [Fact]
    public async Task QueryAcceptedBeforePolicyDispatchRendersNewBufferInsteadOfClearingIt()
    {
        using var h = new Harness(); await h.Flush();
        h.Manager.SetSecurityContext(new SecurityContext { UserName = "new principal" });
        await ReadAuthorizedBuffer(h, name: "new authorized text");
        await h.Flush();
        Assert.Equal("new authorized text", h.Name.Mock.Object.Value);
        Assert.True(h.Name.Mock.Object.IsVisible); Assert.True(h.Name.Mock.Object.IsEnabled);
        Assert.DoesNotContain(null, h.Name.Presented);
        Assert.Equal("first", h.Rows[0].Name);
    }

    [Fact]
    public async Task ANewPolicyDuringClearingStopsOldDeliveryAndSchedulesLatestPolicy()
    {
        using var h = new Harness(); await h.Flush();
        var once = false;
        h.Name.OnSet = () =>
        {
            if (once) return; once = true;
            h.Manager.SetSecurityContext(new SecurityContext { UserName = "latest" });
        };
        h.Manager.SetSecurityContext(new SecurityContext { UserName = "first change" });
        await h.Flush();
        Assert.Null(h.Name.Mock.Object.Value); Assert.Null(h.Other.Mock.Object.Value);
        Assert.Contains(h.Binding!.Outcomes, r => r.State == FormViewDeliveryState.Superseded && r.EffectsPossible);
        Assert.Equal(FormViewDeliveryState.Delivered, h.Binding.Outcomes.Last().State);
        Assert.Equal(0, h.Binding.PendingDeliveries);
    }

    [Theory]
    [InlineData("detach")]
    [InlineData("manager close")]
    [InlineData("registration replacement")]
    [InlineData("host replacement")]
    [InlineData("roster replacement")]
    public async Task QueuedPolicyCannotClearAnotherBindingOwner(string change)
    {
        using var h = new Harness(); await h.Flush();
        h.Manager.SetSecurityContext(new SecurityContext { UserName = "new principal" });
        if (change == "detach") h.Dispatcher.OnUi(h.Binding!.Dispose);
        if (change == "manager close") h.Manager.Dispose();
        if (change == "registration replacement") h.Manager.RegisterBlock("ROWS", h.Wrapper, h.Unit.EntityStructure, "db");
        if (change == "host replacement") h.Host.Object.FormsManager = new Mock<IUnitofWorksManager>().Object;
        if (change == "roster replacement") h.Presenters[0] = new Presenter(h.Dispatcher, "Name").Mock.Object;
        await h.Flush();
        Assert.Equal(new object?[] { "first" }, h.Name.Presented);
        Assert.Equal(1, h.Other.Sets);
        Assert.Equal(0, h.Binding!.PendingDeliveries);
    }

    [Fact]
    public async Task FailingPrivacyClearReportsPartialEffectsWithoutUndoingPolicyOrRows()
    {
        using var h = new Harness(); await h.Flush();
        h.Name.Mock.Setup(p => p.SetValue(null)).Throws(new IOException("presenter unavailable"));
        h.Manager.SetSecurityContext(new SecurityContext { UserName = "new principal" });
        await h.Flush();
        var failed = Assert.Single(h.Binding!.Outcomes, r => r.State == FormViewDeliveryState.Failed);
        Assert.True(failed.EffectsPossible); Assert.False(failed.Acknowledged);
        Assert.IsType<AggregateException>(failed.Exception);
        Assert.False(h.Name.Mock.Object.IsVisible); Assert.False(h.Name.Mock.Object.IsEnabled);
        Assert.Null(h.Other.Mock.Object.Value); Assert.False(h.Other.Mock.Object.IsVisible);
        Assert.Equal("new principal", h.Manager.SecurityContext.UserName);
        Assert.Equal("first", h.Rows[0].Name); Assert.False(h.Unit.IsDirty);
        Assert.False(h.Manager.TryCaptureBindingTarget("ROWS", out _));
    }

    [Fact]
    public async Task PrivacyClearWorksWithoutNotificationServiceAndPreservesDirtyState()
    {
        using var h = new Harness(attach: false);
        h.Dispatcher.OnUi(() => h.Binding = FormsViewBinding.Attach(h.Host.Object, h.View.Object, h.Dispatcher));
        await h.Flush();
        h.State.CurrentMessage = "private"; h.State.IsDirty = true;
        h.Manager.SetSecurityContext(new SecurityContext { UserName = "other" });
        await h.Flush();
        Assert.Empty(h.State.CurrentMessage); Assert.True(h.State.IsDirty); Assert.Null(h.Name.Mock.Object.Value);
        h.Notifications.Verify(n => n.Clear(It.IsAny<BeepViewState>()), Times.Never);
    }

    [Fact]
    public void ObserverFailuresDoNotUndoPolicyOrPreventOtherSubscribers()
    {
        var helper = new SecurityManager();
        var called = 0;
        helper.SecurityPolicyChanged += (_, _) => throw new IOException("helper observer failed");
        using var manager = new FormsManager(new Mock<IDMEEditor>().Object, securityManager: helper);
        manager.SecurityPolicyChanged += (_, _) => throw new IOException("manager observer failed");
        manager.SecurityPolicyChanged += (_, _) => called++;
        manager.SetSecurityContext(new SecurityContext { UserName = "accepted" });
        Assert.Equal("accepted", helper.CurrentContext.UserName); Assert.Equal(1, called);
        Assert.Single(helper.PolicyNotificationFailures); Assert.Single(manager.PolicyNotificationFailures);
        manager.Dispose();
        helper.SetSecurityContext(new SecurityContext { UserName = "after manager close" });
        Assert.Equal(1, called); Assert.Equal(2, helper.PolicyNotificationFailures.Count);
    }

    [Theory]
    [InlineData("context")]
    [InlineData("block")]
    [InlineData("field")]
    [InlineData("clear")]
    public void PolicySettersRejectAfterCloseWithoutMutatingBorrowedHelper(string method)
    {
        var helper = new SecurityManager();
        using var manager = new FormsManager(new Mock<IDMEEditor>().Object, securityManager: helper);
        manager.Dispose();
        Action write = method switch
        {
            "context" => () => manager.SetSecurityContext(new SecurityContext()),
            "block" => () => manager.SetBlockSecurity("ROWS", new BlockSecurity()),
            "field" => () => manager.SetFieldSecurity("ROWS", "Name", new FieldSecurity()),
            _ => () => manager.ClearBlockSecurity("ROWS")
        };
        Assert.Throws<ObjectDisposedException>(write);
        Assert.Equal(0, helper.SecurityRevision);
    }

    [Fact]
    public void PolicyEventsCarryCoherentRevisionsAndNoPrincipalOrRowPayload()
    {
        var helper = new SecurityManager();
        var events = new List<SecurityPolicyChangedEventArgs>();
        helper.SecurityPolicyChanged += (_, e) => events.Add(e);
        helper.SetFieldSecurity("ROWS", "Name", new FieldSecurity { Masked = true });
        helper.SetSecurityContext(new SecurityContext { UserName = "private principal" });
        helper.SetBlockSecurity("ROWS", new BlockSecurity { RowFilterClause = "Id = 2" });
        helper.ClearBlockSecurity("ROWS");
        Assert.Equal(new long[] { 1, 2, 3, 4 }, events.Select(e => e.Revision));
        Assert.Equal(new long[] { 0, 1, 2, 3 }, events.Select(e => e.ReadAuthorizationRevision));
        Assert.Equal(new[] { "ReadAuthorizationRevision", "Revision" },
            typeof(SecurityPolicyChangedEventArgs).GetProperties().Select(p => p.Name).OrderBy(n => n));
    }

    [Fact]
    public async Task PolicyCallbackIsDrainedThroughPhysicalObserverAcknowledgement()
    {
        using var manager = new FormsManager(new Mock<IDMEEditor>().Object);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();
        manager.SecurityPolicyChanged += (_, _) =>
        {
            Assert.Throws<InvalidOperationException>(() => { _ = manager.WaitForPendingCallbacksAsync(); });
            entered.TrySetResult();
            Assert.True(release.Wait(TimeSpan.FromSeconds(10)));
        };
        var write = Task.Run(() => manager.SetSecurityContext(new SecurityContext { UserName = "accepted" }));
        Task? close = null;
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            close = manager.DisposeAsync().AsTask();
            Assert.False(close.IsCompleted);
        }
        finally { release.Set(); }
        await write.WaitAsync(TimeSpan.FromSeconds(10));
        await close!.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task DetachedBindingRemovesOnlyItsOwnPolicyListener()
    {
        using var h = new Harness(); await h.Flush();
        var otherObserver = 0;
        h.Manager.SecurityPolicyChanged += (_, _) => otherObserver++;
        h.Dispatcher.OnUi(h.Binding!.Dispose);
        var submitted = h.Dispatcher.Submitted;
        h.Manager.SetSecurityContext(new SecurityContext { UserName = "other" });
        await h.Flush();
        Assert.Equal(1, otherObserver); Assert.Equal(submitted, h.Dispatcher.Submitted);
        Assert.Equal(new object?[] { "first" }, h.Name.Presented);
    }

    [Fact]
    public async Task SharedHelperRetainsOtherFormPolicyFeedAfterOneFormCloses()
    {
        var helper = new SecurityManager();
        using var first = new Harness(security: helper);
        using var second = new Harness(security: helper);
        await first.Flush(); await second.Flush();
        first.Manager.Dispose();
        helper.SetSecurityContext(new SecurityContext { UserName = "new principal" });
        await first.Flush(); await second.Flush();
        Assert.Equal(new object?[] { "first" }, first.Name.Presented);
        Assert.Null(second.Name.Mock.Object.Value); Assert.False(second.Name.Mock.Object.IsVisible);
        Assert.Equal("first", second.Rows[0].Name); Assert.False(second.Unit.IsDirty);
    }

    [Fact]
    public async Task FacadeSetterNotifiesForSnapshotHelperWithoutObservableCapability()
    {
        var helper = new SecurityManager();
        var legacy = new Mock<ISecurityManager>();
        var snapshots = legacy.As<IQuerySecurityPublication>();
        snapshots.SetupGet(p => p.SecurityRevision).Returns(() => helper.SecurityRevision);
        snapshots.Setup(p => p.CaptureQuerySecurity(It.IsAny<string>())).Returns((string block) => helper.CaptureQuerySecurity(block));
        snapshots.Setup(p => p.TryPublishQuery(It.IsAny<long>(), It.IsAny<Action>()))
            .Returns((long revision, Action action) => helper.TryPublishQuery(revision, action));
        legacy.SetupGet(p => p.CurrentContext).Returns(() => helper.CurrentContext);
        legacy.Setup(p => p.IsBlockAllowed(It.IsAny<string>(), It.IsAny<SecurityPermission>())).Returns(true);
        legacy.Setup(p => p.SetFieldSecurity(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<FieldSecurity>()))
            .Callback((string block, string field, FieldSecurity policy) => helper.SetFieldSecurity(block, field, policy));
        legacy.Setup(p => p.GetFieldSecurity(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string block, string field) => helper.GetFieldSecurity(block, field));
        legacy.Setup(p => p.GetMaskedValue(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
            .Returns((string block, string field, object value) => helper.GetMaskedValue(block, field, value));
        using var h = new Harness(attach: false, security: legacy.Object);
        await ReadAuthorizedBuffer(h, name: "authorized"); h.Attach(); await h.Flush();
        h.Manager.SetFieldSecurity("ROWS", "Name", new FieldSecurity { Masked = true });
        await h.Flush();
        Assert.Equal("**********", h.Name.Mock.Object.Value);
        Assert.Empty(h.Manager.PolicyNotificationFailures);
    }

    [Fact]
    public void PolicyObserverFailureHistoryIsBoundedWithoutRejectingPublications()
    {
        var helper = new SecurityManager();
        helper.SecurityPolicyChanged += (_, _) => throw new IOException("expected failure");
        using var manager = new FormsManager(new Mock<IDMEEditor>().Object, securityManager: helper);
        manager.SecurityPolicyChanged += (_, _) => throw new IOException("expected failure");
        for (var i = 0; i < 140; i++) manager.SetFieldSecurity("ROWS", "Name", new FieldSecurity { Masked = true });
        Assert.Equal(140, helper.SecurityRevision);
        Assert.Equal(128, helper.PolicyNotificationFailures.Count);
        Assert.Equal(128, manager.PolicyNotificationFailures.Count);
        Assert.True(helper.GetFieldSecurity("ROWS", "Name").Masked);
    }

    [Fact]
    public async Task FailedPolicyDispatchCanBeExplicitlyReconciledWithoutRepublishingOrQuerying()
    {
        using var h = new Harness(); await h.Flush();
        h.Dispatcher.FailNext = true;
        h.Manager.SetSecurityContext(new SecurityContext { UserName = "other" });
        await h.Flush();
        Assert.Equal("first", h.Name.Mock.Object.Value);
        Assert.Contains(h.Binding!.Outcomes, r => r.State == FormViewDeliveryState.Failed && r.Exception is IOException);
        var revision = h.Manager.SecurityPolicyRevision;
        h.Source.Invocations.Clear(); h.Host.Invocations.Clear();
        var retry = h.Binding.RequestPolicyReconciliationAsync(); await h.Flush();
        Assert.Equal(FormViewDeliveryState.Delivered, (await retry).State); Assert.True((await retry).Acknowledged);
        Assert.Null(h.Name.Mock.Object.Value); Assert.False(h.Name.Mock.Object.IsVisible);
        Assert.Equal(revision, h.Manager.SecurityPolicyRevision);
        Assert.Equal("first", h.Rows[0].Name); Assert.False(h.Unit.IsDirty);
        h.Host.Verify(x => x.GetFieldValue(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        h.Source.Verify(x => x.GetEntityAsync(It.IsAny<string>(), It.IsAny<List<TheTechIdea.Beep.Report.AppFilter>>()), Times.Never);
    }

    [Fact]
    public async Task ExplicitPolicyReconciliationCancellationAndRetirementHaveNoLateUiEffects()
    {
        using var h = new Harness(); await h.Flush();
        using var cancellation = new CancellationTokenSource();
        var queued = h.Binding!.RequestPolicyReconciliationAsync(cancellation.Token);
        cancellation.Cancel(); await h.Flush();
        Assert.Equal(FormViewDeliveryState.Cancelled, (await queued).State);
        Assert.False((await queued).EffectsPossible); Assert.Equal(1, h.Name.Sets);
        h.Dispatcher.OnUi(h.Binding.Dispose);
        Assert.Equal(FormViewDeliveryState.Superseded, (await h.Binding.RequestPolicyReconciliationAsync()).State);
        Assert.Equal(1, h.Name.Sets);
    }

    [Fact]
    public async Task PolicyPumpDrainsRunningActionWhenDispatcherAcknowledgesEarly()
    {
        using var h = new Harness(); await h.Flush();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();
        h.Name.OnSet = () => { entered.TrySetResult(); Assert.True(release.Wait(TimeSpan.FromSeconds(10))); };
        h.Dispatcher.EarlyNext = true;
        h.Dispatcher.WaitUntilEarlyActionStarts = () => entered.Task.GetAwaiter().GetResult();
        h.Manager.SetSecurityContext(new SecurityContext { UserName = "other" });
        Task? drain = null;
        try
        {
            await h.Dispatcher.PumpUntil(entered.Task);
            drain = h.Binding!.WaitForPendingDeliveriesAsync();
            Assert.False(drain.IsCompleted); Assert.True(h.Binding.PendingDeliveries > 0);
        }
        finally { release.Set(); }
        await h.Dispatcher.PumpUntil(drain!);
        await h.Dispatcher.EarlyWork!.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Null(h.Name.Mock.Object.Value); Assert.Null(h.Other.Mock.Object.Value);
        Assert.Equal(0, h.Binding!.PendingDeliveries);
        Assert.True(h.Binding.Outcomes.Last().Acknowledged);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BindingPreservesPlainAndDictionaryRuntimeRecordsWithoutEntityCoercion(bool dictionary)
    {
        using var h = new Harness(); await h.Flush();
        object row = dictionary ? new Dictionary<string, object?> { ["Id"] = 3, ["Name"] = "plain", ["Other"] = "other" } : new PlainRow();
        BindLegacyRecord(h, row); await h.Flush();
        Assert.Same(row, h.Capture().Record); Assert.Equal("plain", h.Name.Mock.Object.Value);
        h.Dispatcher.OnUi(() => h.Name.Edit("plain edit")); await h.Flush();
        Assert.Equal("plain edit", h.Manager.GetFieldValue(row, "Name"));
        Assert.Same(row, h.Capture().Record);
    }

    [Fact]
    public async Task BytePresentationCannotMutateTheEngineOwnedRecordBuffer()
    {
        using var h = new Harness(); await h.Flush();
        var row = new PlainRow { Name = new byte[] { 1, 2 } };
        BindLegacyRecord(h, row);
        h.Name.OnSet = () => ((byte[])h.Name.Mock.Object.Value!)[0] = 99;
        await h.Flush();
        Assert.Equal(new byte[] { 1, 2 }, (byte[])row.Name!);
        Assert.Equal(99, ((byte[])h.Name.Mock.Object.Value!)[0]);
        Assert.True(h.Manager.IsBindingTargetCurrent(h.Capture()));
    }

    [Fact]
    public void LegacyHostWithoutCapturedTargetsRejectsBindingBeforeSideEffects()
    {
        using var h = new Harness(attach: false);
        h.Host.Object.FormsManager = new Mock<IUnitofWorksManager>().Object;
        h.Dispatcher.OnUi(() => Assert.Throws<NotSupportedException>(h.Attach));
        Assert.Equal(0, h.Binds); Assert.Equal(0, h.Dispatcher.Submitted);
    }

    [Fact]
    public async Task FieldPolicyInputsAndReturnedCopiesCannotMutateActiveMaskOrItsRevision()
    {
        using var h = new Harness(); await h.Flush();
        var input = new FieldSecurity { Masked = true };
        h.Manager.SetFieldSecurity("ROWS", "Name", input);
        var revision = ((IQuerySecuritySnapshotProvider)h.Manager.Security).SecurityRevision;
        input.Masked = false;
        h.Manager.GetFieldSecurity("ROWS", "Name").Masked = false;
        Assert.True(h.Manager.GetFieldSecurity("ROWS", "Name").Masked);
        Assert.Equal(revision, ((IQuerySecuritySnapshotProvider)h.Manager.Security).SecurityRevision);
        Assert.Null(input.BlockName);
        var target = h.Capture();
        h.Manager.SetFieldSecurity("ROWS", "Name", new FieldSecurity());
        h.Manager.SetFieldSecurity("ROWS", "Name", new FieldSecurity { Masked = true });
        Assert.False(h.Manager.IsBindingTargetCurrent(target));
        Assert.Equal(revision + 2, ((IQuerySecuritySnapshotProvider)h.Manager.Security).SecurityRevision);
    }

    [Fact]
    public async Task ActivityRelayRefreshesCurrentRowButIgnoresForeignSourceAndDuplicateItemFeed()
    {
        using var h = new Harness(); await h.Flush();
        h.Unit.MoveNext();
        h.View.Raise(v => v.UnitOfWorkActivity += null, new BeepUnitOfWorkEventArgs
        { BlockName = "ROWS", UnitOfWork = h.Wrapper, EventKind = BeepUnitOfWorkEventKind.CurrentChanged });
        await h.Flush(); Assert.Equal("second", h.Name.Mock.Object.Value);
        var submitted = h.Dispatcher.Submitted;
        h.View.Raise(v => v.UnitOfWorkActivity += null, new BeepUnitOfWorkEventArgs
        { BlockName = "ROWS", UnitOfWork = new Mock<IUnitofWork>().Object, EventKind = BeepUnitOfWorkEventKind.PostCommit });
        h.View.Raise(v => v.UnitOfWorkActivity += null, new BeepUnitOfWorkEventArgs
        { BlockName = "ROWS", UnitOfWork = h.Wrapper, EventKind = BeepUnitOfWorkEventKind.ItemChanged });
        Assert.Equal(submitted, h.Dispatcher.Submitted);
    }

    [Fact]
    public async Task NotificationFailureDoesNotTurnAcknowledgedDatabaseCommitIntoAnUnsavedWrite()
    {
        using var h = new Harness(); await h.Flush();
        var open = false;
        h.Source.SetupGet(s => s.DatasourceType).Returns(DataSourceType.SqlLite);
        h.Source.Setup(s => s.BeginTransaction(It.IsAny<PassedArgs>())).Returns(() =>
        { Assert.False(open); open = true; return new ErrorsInfo { Flag = Errors.Ok }; });
        h.Source.Setup(s => s.Commit(It.IsAny<PassedArgs>())).Returns(() =>
        { Assert.True(open); open = false; return new ErrorsInfo { Flag = Errors.Ok }; });
        h.Source.Setup(s => s.EndTransaction(It.IsAny<PassedArgs>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
        h.Source.Setup(s => s.UpdateEntity(It.IsAny<string>(), It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
        h.Notifications.Setup(n => n.Publish(h.State, It.IsAny<string>(), It.IsAny<BeepMessageSeverity>())).Throws(new IOException("UI notification failed"));
        h.Rows[0].Name = "saved";
        await h.Flush(); Assert.True(h.State.IsDirty);
        h.Unit.PostCommit += (_, _) =>
        {
            h.View.Raise(v => v.UnitOfWorkActivity += null, new BeepUnitOfWorkEventArgs
            { BlockName = "ROWS", UnitOfWork = h.Wrapper, EventKind = BeepUnitOfWorkEventKind.PostCommit });
            h.Host.Raise(x => x.MessageRaised += null, new FormsHostMessageEventArgs("ROWS", "saved", MessageLevel.Info));
        };
        var commit = await h.Manager.CommitFormWithOutcomeAsync();
        Assert.True(commit.AllWritesCommitted, commit.Message); Assert.False(open); Assert.False(h.Unit.IsDirty);
        await h.Flush();
        Assert.Contains(h.Binding!.Outcomes, r => r.State == FormViewDeliveryState.Failed && r.Exception is IOException);
        Assert.True(commit.AllWritesCommitted); Assert.False(h.Unit.IsDirty); Assert.False(h.State.IsDirty);
        h.Source.Verify(s => s.Commit(It.IsAny<PassedArgs>()), Times.Once);
    }
}
