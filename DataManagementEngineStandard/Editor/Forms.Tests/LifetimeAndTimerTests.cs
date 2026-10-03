using Moq;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.Forms.Helpers;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Editor.UOWManager.Helpers;
using Xunit;

namespace TheTechIdea.Beep.Editor.UOWManager.Tests;

public class LifetimeAndTimerTests
{
    private sealed class ManualTimeProvider : TimeProvider
    {
        internal readonly List<ManualTimer> Timers = new();
        internal Action<ManualTimer>? NextChange;
        public override DateTimeOffset GetUtcNow() => new(2026, 10, 3, 12, 0, 0, TimeSpan.Zero);
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            Assert.Equal(Timeout.InfiniteTimeSpan, dueTime);
            Assert.Equal(Timeout.InfiniteTimeSpan, period);
            var timer = new ManualTimer(callback, state, NextChange);
            NextChange = null;
            lock (Timers) Timers.Add(timer);
            return timer;
        }
    }

    private sealed class ManualTimer(TimerCallback callback, object? state, Action<ManualTimer>? onChange) : ITimer
    {
        internal bool IsDisposed;
        internal bool ThrowOnDispose;
        internal bool ActivationResult = true;
        internal TimeSpan DueTime;
        internal TimeSpan Period;
        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            DueTime = dueTime; Period = period;
            onChange?.Invoke(this);
            return ActivationResult;
        }
        internal void FireQueued() => callback(state);
        public void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;
            if (ThrowOnDispose) throw new InvalidOperationException("scheduler cleanup failure");
        }
        public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
    }

    [Fact]
    public void OldQueuedCallbackCannotFireOrExpireSameNameReplacement()
    {
        var clock = new ManualTimeProvider();
        using var timers = new TimerManager(clock);
        var fires = new List<TimerFiredEventArgs>();
        timers.TimerFired += (_, e) => fires.Add(e);
        timers.CreateTimer("T", TimeSpan.FromSeconds(1));
        var old = clock.Timers[0];
        timers.CreateTimer("t", TimeSpan.FromSeconds(2));
        Assert.True(old.IsDisposed);
        old.FireQueued();
        Assert.Empty(fires);
        Assert.True(timers.TimerExists("T"));
        Assert.Equal(0, timers.GetTimer("T").FireCount);
        clock.Timers[1].FireQueued();
        Assert.Single(fires);
        Assert.Equal("t", fires[0].TimerName);
        Assert.False(timers.TimerExists("T"));
    }

    [Fact]
    public void QueuedDeletedOrDisposedCallbacksCannotEmit()
    {
        var clock = new ManualTimeProvider();
        var timers = new TimerManager(clock);
        int fires = 0;
        timers.TimerFired += (_, _) => fires++;
        timers.CreateTimer("first", TimeSpan.FromSeconds(1), true);
        Assert.True(timers.DeleteTimer("first"));
        clock.Timers[0].FireQueued();
        timers.CreateTimer("second", TimeSpan.FromSeconds(1), true);
        timers.Dispose(); timers.Dispose();
        clock.Timers[1].FireQueued();
        Assert.Equal(0, fires);
        Assert.Empty(timers.GetAllTimers());
        Assert.Throws<ObjectDisposedException>(() => timers.CreateTimer("late", TimeSpan.FromSeconds(1)));
        Assert.Equal(2, clock.Timers.Count);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    [InlineData(4294967295d)]
    public void InvalidIntervalCannotRemoveExistingTimer(double milliseconds)
    {
        var clock = new ManualTimeProvider();
        using var timers = new TimerManager(clock);
        timers.CreateTimer("T", TimeSpan.FromSeconds(1), true);
        Assert.Throws<ArgumentOutOfRangeException>(() => timers.CreateTimer("T", TimeSpan.FromMilliseconds(milliseconds)));
        Assert.True(timers.TimerExists("T"));
        Assert.False(clock.Timers[0].IsDisposed);
        Assert.Single(clock.Timers);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FailedActivationRestoresExistingTimerAndSuppressesEarlyTick(bool throws)
    {
        var clock = new ManualTimeProvider();
        using var timers = new TimerManager(clock);
        int fires = 0;
        timers.TimerFired += (_, _) => fires++;
        timers.CreateTimer("T", TimeSpan.FromSeconds(1), true);
        clock.NextChange = timer =>
        {
            timer.FireQueued();
            if (throws) throw new InvalidOperationException("activation failed");
            timer.ActivationResult = false;
        };
        Assert.Throws<InvalidOperationException>(() => timers.CreateTimer("T", TimeSpan.FromSeconds(2)));
        Assert.Equal(0, fires);
        Assert.True(clock.Timers[1].IsDisposed);
        Assert.False(clock.Timers[0].IsDisposed);
        clock.Timers[0].FireQueued();
        Assert.Equal(1, fires);
        Assert.Equal(TimeSpan.FromSeconds(1), timers.GetTimer("T").Interval);
    }

    [Fact]
    public void InlineActivationTickIsDeliveredOnlyAfterAcknowledgedPublication()
    {
        var clock = new ManualTimeProvider();
        using var timers = new TimerManager(clock);
        int fires = 0;
        timers.TimerFired += (_, _) => fires++;
        clock.NextChange = timer => { timer.FireQueued(); Assert.Equal(0, fires); };
        var result = timers.CreateTimer("T", TimeSpan.FromSeconds(1));
        Assert.Equal(1, fires);
        Assert.Equal(TimerState.Expired, result.State);
        Assert.False(timers.TimerExists("T"));
    }

    [Fact]
    public void DefinitionMutationCannotControlOwnedTimer()
    {
        var clock = new ManualTimeProvider();
        using var timers = new TimerManager(clock);
        var definition = timers.CreateTimer("T", TimeSpan.FromSeconds(1), true);
        definition.TimerName = "spoof"; definition.Repeating = false; definition.FireCount = int.MaxValue;
        timers.GetTimer("T").State = TimerState.Deleted;
        timers.GetAllTimers()[0].Interval = TimeSpan.Zero;
        clock.Timers[0].FireQueued();
        var current = timers.GetTimer("T");
        Assert.True(current.Repeating);
        Assert.Equal("T", current.TimerName);
        Assert.Equal(1, current.FireCount);
        Assert.Equal(TimerState.Running, current.State);
        Assert.Equal(TimeSpan.FromSeconds(1), current.Interval);
        Assert.Equal(int.MaxValue, definition.FireCount);
    }

    [Fact]
    public void SubscriberFailureIsIsolatedAndFailureLedgerIsBounded()
    {
        var clock = new ManualTimeProvider();
        using var timers = new TimerManager(clock);
        int delivered = 0;
        timers.TimerFired += (_, _) => throw new InvalidOperationException("observer failed");
        timers.TimerFired += (_, _) => delivered++;
        timers.CreateTimer("T", TimeSpan.FromSeconds(1), true);
        for (int i = 0; i < 70; i++) clock.Timers[0].FireQueued();
        Assert.Equal(70, delivered);
        Assert.Equal(64, timers.CallbackFailures.Count);
        Assert.All(timers.CallbackFailures, f => Assert.Equal("T", f.TimerName));
    }

    [Fact]
    public void SchedulerCleanupFailureDoesNotSkipOtherTimerCleanup()
    {
        var clock = new ManualTimeProvider();
        var timers = new TimerManager(clock);
        timers.CreateTimer("first", TimeSpan.FromSeconds(1));
        timers.CreateTimer("second", TimeSpan.FromSeconds(1));
        clock.Timers[0].ThrowOnDispose = true;
        timers.Dispose();
        Assert.All(clock.Timers, t => Assert.True(t.IsDisposed));
        Assert.Single(timers.CallbackFailures);
        Assert.Empty(timers.GetAllTimers());
    }

    [Fact]
    public void ReentrantObserverCannotDeleteReplacementByOldEntryName()
    {
        var clock = new ManualTimeProvider();
        using var timers = new TimerManager(clock);
        timers.CreateTimer("T", TimeSpan.FromSeconds(1));
        timers.TimerFired += (_, _) => timers.CreateTimer("T", TimeSpan.FromSeconds(2), true);
        clock.Timers[0].FireQueued();
        Assert.True(timers.TimerExists("T"));
        Assert.Equal(0, timers.GetTimer("T").FireCount);
        Assert.False(clock.Timers[1].IsDisposed);
    }

    [Fact]
    public async Task OverlappingFailedActivationsCannotResurrectRetiredEntry()
    {
        var clock = new ManualTimeProvider();
        using var timers = new TimerManager(clock);
        timers.CreateTimer("T", TimeSpan.FromSeconds(1), true);
        var enteredA = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var enteredB = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseA = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseB = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        clock.NextChange = _ => { enteredA.SetResult(); releaseA.Task.GetAwaiter().GetResult(); throw new InvalidOperationException("A"); };
        var first = Task.Run(() => timers.CreateTimer("T", TimeSpan.FromSeconds(2)));
        await enteredA.Task.WaitAsync(TimeSpan.FromSeconds(10));
        clock.NextChange = _ => { enteredB.SetResult(); releaseB.Task.GetAwaiter().GetResult(); throw new InvalidOperationException("B"); };
        var second = Task.Run(() => timers.CreateTimer("T", TimeSpan.FromSeconds(3)));
        await enteredB.Task.WaitAsync(TimeSpan.FromSeconds(10));
        releaseA.SetResult();
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await first);
        releaseB.SetResult();
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await second);
        Assert.False(timers.TimerExists("T"));
        Assert.All(clock.Timers, t => Assert.True(t.IsDisposed));
    }

    private sealed class Row : Entity { public string Name { get; set; } = "one"; }

    private static IEntityStructure Schema()
    {
        var entity = new Mock<IEntityStructure>();
        entity.Setup(e => e.EntityName).Returns("Records");
        entity.Setup(e => e.Fields).Returns(new List<EntityField> { new() { FieldName = "Name", Fieldtype = "string" } });
        return entity.Object;
    }

    [Fact]
    public void DisposeDetachesDirectHandlersFromCapturedSourceNotMutatedBlockSource()
    {
        using var h = new QueryPolicyTests.Harness();
        var other = new Mock<IUnitofWork>();
        h.Manager.GetBlock("ROWS").UnitOfWork = other.Object;
        h.Manager.Dispose();
        h.Unit.VerifyRemove(u => u.ItemChanged -= It.IsAny<EventHandler<ItemChangedEventArgs<Entity>>>(), Times.Once);
        h.Unit.VerifyRemove(u => u.CurrentChanged -= It.IsAny<EventHandler>(), Times.Exactly(2));
        other.VerifyRemove(u => u.ItemChanged -= It.IsAny<EventHandler<ItemChangedEventArgs<Entity>>>(), Times.Never);
        Assert.Null(h.Manager.GetBlock("ROWS"));
        Assert.Throws<ObjectDisposedException>(() => h.Manager.RegisterBlock("late", other.Object, Schema()));
        Assert.Throws<ObjectDisposedException>(() => h.Manager.CreateTimer("late", TimeSpan.FromSeconds(1)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void QueuedItemCallbackCannotDispatchAfterDisposeOrReplacement(bool replace)
    {
        using var h = new QueryPolicyTests.Harness();
        var queued = (EventHandler<ItemChangedEventArgs<Entity>>)h.Unit.Invocations.Single(i => i.Method.Name == "add_ItemChanged").Arguments[0];
        int changed = 0;
        h.Manager.OnBlockFieldChanged += (_, _) => changed++;
        if (replace) h.Manager.RegisterBlock("rows", new Mock<IUnitofWork>().Object, Schema());
        else h.Manager.Dispose();
        queued(h.Unit.Object, new ItemChangedEventArgs<Entity>(new Row(), "Name"));
        Assert.Equal(0, changed);
        Assert.Equal(0, h.Manager.PendingCallbackCount);
    }

    [Fact]
    public void FailedPreparationPreservesExistingBlockAndCaseInsensitiveIdentity()
    {
        using var h = new QueryPolicyTests.Harness();
        var original = h.Manager.GetBlock("ROWS");
        Assert.Same(original, h.Manager.GetBlock("rows"));
        Assert.Throws<ArgumentNullException>(() => h.Manager.RegisterBlock("rows", new Mock<IUnitofWork>().Object, (IEntityStructure)null!));
        Assert.Same(original, h.Manager.GetBlock("ROWS"));
        Assert.Single(h.Manager.Blocks);
        h.Unit.VerifyRemove(u => u.ItemChanged -= It.IsAny<EventHandler<ItemChangedEventArgs<Entity>>>(), Times.Never);
    }

    [Fact]
    public void CleanupFailuresDoNotSkipDirectHandlersOrBorrowedHelpers()
    {
        var editor = new Mock<IDMEEditor>();
        var events = new Mock<IEventManager>();
        var performance = new Mock<IPerformanceManager>();
        var timers = new Mock<ITimerManager>();
        var unit = new Mock<IUnitofWork>();
        events.Setup(e => e.UnsubscribeFromUnitOfWorkEvents(unit.Object, "ROWS")).Throws(new InvalidOperationException("helper failed"));
        performance.Setup(p => p.InvalidateBlockCache("ROWS")).Throws(new InvalidOperationException("cache failed"));
        var manager = new FormsManager(editor.Object, eventManager: events.Object, performanceManager: performance.Object, timerManager: timers.Object);
        manager.RegisterBlock("ROWS", unit.Object, Schema());
        manager.Dispose();
        unit.VerifyRemove(u => u.ItemChanged -= It.IsAny<EventHandler<ItemChangedEventArgs<Entity>>>(), Times.Once);
        unit.VerifyRemove(u => u.CurrentChanged -= It.IsAny<EventHandler>(), Times.Once);
        timers.VerifyRemove(t => t.TimerFired -= It.IsAny<EventHandler<TimerFiredEventArgs>>(), Times.Once);
        performance.Verify(p => p.Dispose(), Times.Never);
        timers.Verify(t => t.Dispose(), Times.Never);
        Assert.Equal(2, manager.CleanupFailures.Count);
        Assert.Empty(manager.Blocks);
    }

    [Fact]
    public void DefaultEventHelperRetiresDispatchAndDetachesRemainingHandlersAfterAccessorFailure()
    {
        var editor = new Mock<IDMEEditor>();
        var events = new EventManager(editor.Object);
        var unit = new Mock<IUnitofWork>();
        using var manager = new FormsManager(editor.Object, eventManager: events);
        manager.RegisterBlock("ROWS", unit.Object, Schema());
        int calls = 0;
        events.OnPreInsert += (_, _) => calls++;
        unit.SetupRemove(u => u.PreInsert -= It.IsAny<EventHandler<UnitofWorkParams>>())
            .Throws(new InvalidOperationException("event accessor failed"));
        manager.Dispose();
        unit.Raise(u => u.PreInsert += null, unit.Object, new UnitofWorkParams());
        Assert.Equal(0, calls);
        unit.VerifyRemove(u => u.PostQuery -= It.IsAny<EventHandler<UnitofWorkParams>>(), Times.Once);
        unit.VerifyRemove(u => u.CurrentChanged -= It.IsAny<EventHandler>(), Times.Exactly(2));
        Assert.IsType<AggregateException>(Assert.Single(manager.CleanupFailures).Exception);
    }

    [Fact]
    public async Task ConcurrentAsyncDisposeWaitsForAlreadyStartedSynchronousCleanup()
    {
        var events = new Mock<IEventManager>();
        var editor = new Mock<IDMEEditor>();
        var unit = new Mock<IUnitofWork>();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        events.Setup(e => e.UnsubscribeFromUnitOfWorkEvents(unit.Object, "ROWS"))
            .Callback(() => { entered.SetResult(); release.Task.GetAwaiter().GetResult(); });
        var manager = new FormsManager(editor.Object, eventManager: events.Object);
        manager.RegisterBlock("ROWS", unit.Object, Schema());
        var first = Task.Run(manager.Dispose);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var second = manager.DisposeAsync().AsTask();
        Assert.False(second.IsCompleted);
        release.SetResult();
        await first.WaitAsync(TimeSpan.FromSeconds(10));
        await second.WaitAsync(TimeSpan.FromSeconds(10));
        events.Verify(e => e.UnsubscribeFromUnitOfWorkEvents(unit.Object, "ROWS"), Times.Once);
    }

    [Fact]
    public void UnregisterRemovesIdentityAndAttemptsAllCleanupAfterLeaveFailure()
    {
        var events = new Mock<IEventManager>();
        events.Setup(e => e.TriggerBlockLeave("ROWS")).Throws(new InvalidOperationException("leave failed"));
        var editor = new Mock<IDMEEditor>();
        using var manager = new FormsManager(editor.Object, eventManager: events.Object);
        var unit = new Mock<IUnitofWork>();
        manager.RegisterBlock("ROWS", unit.Object, Schema());
        Assert.False(manager.UnregisterBlock("rows"));
        Assert.False(manager.BlockExists("ROWS"));
        unit.VerifyRemove(u => u.ItemChanged -= It.IsAny<EventHandler<ItemChangedEventArgs<Entity>>>(), Times.Once);
        events.Verify(e => e.UnsubscribeFromUnitOfWorkEvents(unit.Object, "ROWS"), Times.Once);
        Assert.Single(manager.CleanupFailures);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InFlightLovCannotPublishAfterReplacementOrAsyncDisposal(bool replace)
    {
        var editor = new Mock<IDMEEditor>();
        var lov = new Mock<ILOVManager>();
        var items = new Mock<IItemPropertyManager>();
        items.As<IPreparedBlockItems>()
            .Setup(i => i.PrepareBlockItems(It.IsAny<Guid>(), "ROWS", It.IsAny<IEntityStructure>()))
            .Returns(() =>
            {
                var stage = new Mock<IBlockItemsRegistration>();
                stage.Setup(s => s.Commit(It.IsAny<Action>())).Callback((Action publish) => publish());
                return stage.Object;
            });
        var unit = new Mock<IUnitofWork>();
        var record = new Row();
        unit.Setup(u => u.CurrentItem).Returns(record);
        lov.Setup(l => l.HasLOV("ROWS", "Name")).Returns(true);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<LOVValidationResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        lov.Setup(l => l.ValidateLOVValueAsync("ROWS", "Name", It.IsAny<object>())).Callback(() => entered.SetResult()).Returns(release.Task);
        var manager = new FormsManager(editor.Object, lovManager: lov.Object, itemPropertyManager: items.Object);
        manager.RegisterBlock("ROWS", unit.Object, Schema());
        unit.Raise(u => u.ItemChanged += null, unit.Object, new ItemChangedEventArgs<Entity>(record, "Name"));
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(1, manager.PendingCallbackCount);
        Task draining;
        if (replace)
        {
            manager.RegisterBlock("ROWS", new Mock<IUnitofWork>().Object, Schema());
            draining = manager.WaitForPendingCallbacksAsync();
        }
        else draining = manager.DisposeAsync().AsTask();
        Assert.False(draining.IsCompleted);
        release.SetResult(LOVValidationResult.Invalid("late result"));
        await draining.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(0, manager.PendingCallbackCount);
        items.Verify(i => i.SetItemError("ROWS", "Name", "late result"), Times.Never);
        await manager.DisposeAsync();
    }

    [Fact]
    public async Task CallbackCannotAwaitItsOwnDisposal()
    {
        var timers = new Mock<ITimerManager>();
        var triggers = new Mock<ITriggerManager>();
        var editor = new Mock<IDMEEditor>();
        using var manager = new FormsManager(editor.Object, timerManager: timers.Object, triggerManager: triggers.Object);
        var observed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        triggers.Setup(t => t.FireFormTriggerAsync(TriggerType.WhenTimerExpired, It.IsAny<string>(), It.IsAny<TriggerContext>(), It.IsAny<CancellationToken>()))
            .Callback(() => { Assert.Throws<InvalidOperationException>(() => manager.DisposeAsync()); observed.SetResult(); })
            .ReturnsAsync(TriggerResult.Success);
        timers.Raise(t => t.TimerFired += null, timers.Object, new TimerFiredEventArgs { TimerName = "T" });
        await observed.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await manager.DisposeAsync();
        Assert.Equal(0, manager.PendingCallbackCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SameNameManagersUnsubscribeOnlyTheirOwnMessageHandlers(bool dispose)
    {
        var bus = new FormMessageBus();
        var editor = new Mock<IDMEEditor>();
        using var first = new FormsManager(editor.Object, messageBus: bus) { CurrentFormName = "same" };
        using var second = new FormsManager(editor.Object, messageBus: bus) { CurrentFormName = "same" };
        int firstCalls = 0, secondCalls = 0;
        first.SubscribeToMessage("refresh", _ => firstCalls++);
        second.SubscribeToMessage("refresh", _ => secondCalls++);
        if (dispose) first.Dispose(); else first.UnsubscribeFromMessage("refresh");
        bus.PostMessage("same", "refresh", null!);
        Assert.Equal(0, firstCalls);
        Assert.Equal(1, secondCalls);
    }

    [Fact]
    public void LegacyBorrowedBusHandlerIsDeactivatedWithoutNameWideUnsubscribe()
    {
        var bus = new Mock<IFormMessageBus>();
        Action<FormMessage>? queued = null;
        bus.Setup(b => b.Subscribe("form", "refresh", It.IsAny<Action<FormMessage>>()))
            .Callback<string, string, Action<FormMessage>>((_, _, handler) => queued = handler);
        var editor = new Mock<IDMEEditor>();
        var manager = new FormsManager(editor.Object, messageBus: bus.Object) { CurrentFormName = "form" };
        int calls = 0;
        manager.SubscribeToMessage("refresh", _ => calls++);
        manager.Dispose();
        queued!(new FormMessage());
        Assert.Equal(0, calls);
        bus.Verify(b => b.UnsubscribeAll(It.IsAny<string>()), Times.Never);
        bus.Verify(b => b.Unsubscribe(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        Assert.Throws<ObjectDisposedException>(() => manager.SubscribeToMessage("refresh", _ => { }));
    }
}
