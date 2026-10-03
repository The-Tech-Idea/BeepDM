using Moq;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.UOWManager.Helpers;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using Xunit;

namespace TheTechIdea.Beep.Editor.UOWManager.Tests;

public class SubscriptionOwnershipTests
{
    public interface IOptionalEvents
    {
        event EventHandler<UnitofWorkParams> PreRollback;
    }

    private static IEntityStructure Schema()
    {
        var schema = new Mock<IEntityStructure>();
        schema.Setup(s => s.EntityName).Returns("Rows");
        schema.Setup(s => s.Fields).Returns(new List<EntityField> { new() { FieldName = "Name", Fieldtype = "string" } });
        return schema.Object;
    }

    [Theory]
    [InlineData("PreInsert")]
    [InlineData("PostEdit")]
    [InlineData("CurrentChanged")]
    public void FailedAttachmentUnwindsEveryAttemptAndRetiresQueuedDelegates(string failedEvent)
    {
        var unit = new Mock<IUnitofWork>();
        var events = new EventManager(new Mock<IDMEEditor>().Object);
        var failure = new InvalidOperationException("attach failed");
        EventHandler<UnitofWorkParams>? queued = null;
        EventHandler? queuedCurrent = null;
        int calls = 0;
        events.OnPreInsert += (_, _) => calls++;
        events.OnPostUpdate += (_, _) => calls++;
        events.OnRecordEnter += (_, _) => calls++;
        if (failedEvent == "PreInsert")
            unit.SetupAdd(u => u.PreInsert += It.IsAny<EventHandler<UnitofWorkParams>>())
                .Callback((EventHandler<UnitofWorkParams> h) => queued = h).Throws(failure);
        else if (failedEvent == "PostEdit")
            unit.SetupAdd(u => u.PostEdit += It.IsAny<EventHandler<UnitofWorkParams>>())
                .Callback((EventHandler<UnitofWorkParams> h) => queued = h).Throws(failure);
        else
            unit.SetupAdd(u => u.CurrentChanged += It.IsAny<EventHandler>())
                .Callback((EventHandler h) => queuedCurrent = h).Throws(failure);

        Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => events.SubscribeOwned(unit.Object, "ROWS")));
        var adds = unit.Invocations.Where(i => i.Method.Name.StartsWith("add_", StringComparison.Ordinal)).ToArray();
        foreach (var add in adds)
        {
            var remove = Assert.Single(unit.Invocations.Where(i => i.Method.Name == "remove_" + add.Method.Name[4..]));
            Assert.Same(add.Arguments[0], remove.Arguments[0]);
        }
        queued?.Invoke(unit.Object, new UnitofWorkParams());
        queuedCurrent?.Invoke(unit.Object, EventArgs.Empty);
        unit.Raise(u => u.PreInsert += null, unit.Object, new UnitofWorkParams());
        Assert.Equal(0, calls);
    }

    [Fact]
    public void RollbackFailureKeepsOriginalFailureAndCannotReviveAttachedDelegates()
    {
        var unit = new Mock<IUnitofWork>();
        var events = new EventManager(new Mock<IDMEEditor>().Object);
        var attach = new InvalidOperationException("attach failed");
        var detach = new InvalidOperationException("detach failed");
        unit.SetupAdd(u => u.PostUpdate += It.IsAny<EventHandler<UnitofWorkParams>>()).Throws(attach);
        unit.SetupRemove(u => u.PreInsert -= It.IsAny<EventHandler<UnitofWorkParams>>()).Throws(detach);
        int calls = 0;
        events.OnPreInsert += (_, _) => calls++;
        var result = Assert.Throws<AggregateException>(() => events.SubscribeOwned(unit.Object, "ROWS"));
        Assert.Contains(attach, result.Flatten().InnerExceptions);
        Assert.Contains(detach, result.Flatten().InnerExceptions);
        unit.Raise(u => u.PreInsert += null, unit.Object, new UnitofWorkParams());
        Assert.Equal(0, calls);
        unit.VerifyRemove(u => u.PostUpdate -= It.IsAny<EventHandler<UnitofWorkParams>>(), Times.Once);
    }

    [Fact]
    public void PresentOptionalAccessorFailureIsNotMisclassifiedAsMissingEvent()
    {
        var unit = new Mock<IUnitofWork>();
        var optional = unit.As<IOptionalEvents>();
        var attach = new InvalidOperationException("optional attach failed");
        optional.SetupAdd(u => u.PreRollback += It.IsAny<EventHandler<UnitofWorkParams>>()).Throws(attach);
        var events = new EventManager(new Mock<IDMEEditor>().Object);
        Assert.Same(attach, Assert.Throws<InvalidOperationException>(() => events.SubscribeOwned(unit.Object, "ROWS")));
        optional.VerifyRemove(u => u.PreRollback -= It.IsAny<EventHandler<UnitofWorkParams>>(), Times.Once);
        unit.VerifyRemove(u => u.PreInsert -= It.IsAny<EventHandler<UnitofWorkParams>>(), Times.Once);
    }

    [Fact]
    public void PreparedSubscriptionDoesNotDispatchInlineAccessorCallbacks()
    {
        var unit = new Mock<IUnitofWork>();
        var events = new EventManager(new Mock<IDMEEditor>().Object);
        EventHandler<UnitofWorkParams>? queued = null;
        int calls = 0;
        events.OnPreInsert += (_, _) => calls++;
        unit.SetupAdd(u => u.PreInsert += It.IsAny<EventHandler<UnitofWorkParams>>())
            .Callback((EventHandler<UnitofWorkParams> h) => { queued = h; h(unit.Object, new UnitofWorkParams()); });
        using var lease = events.SubscribeOwned(unit.Object, "ROWS");
        Assert.Equal(0, calls);
        queued!(unit.Object, new UnitofWorkParams());
        Assert.Equal(1, calls);
    }

    [Fact]
    public void SuccessfulOptionalAttachmentHasExactLeaseCleanup()
    {
        var unit = new Mock<IUnitofWork>();
        var optional = unit.As<IOptionalEvents>();
        var events = new EventManager(new Mock<IDMEEditor>().Object);
        var lease = events.SubscribeOwned(unit.Object, "ROWS");
        optional.VerifyAdd(u => u.PreRollback += It.IsAny<EventHandler<UnitofWorkParams>>(), Times.Once);
        lease.Dispose();
        lease.Dispose();
        optional.VerifyRemove(u => u.PreRollback -= It.IsAny<EventHandler<UnitofWorkParams>>(), Times.Once);
    }

    [Fact]
    public void FailingSubscriptionLogUnwindsRatherThanReportingSuccess()
    {
        var editor = new Mock<IDMEEditor>();
        var failure = new InvalidOperationException("log failed");
        editor.Setup(e => e.AddLogMessage("EventManager", It.IsAny<string>(), It.IsAny<DateTime>(),
            0, null, ConfigUtil.Errors.Ok)).Throws(failure);
        var unit = new Mock<IUnitofWork>();
        var events = new EventManager(editor.Object);
        Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => events.SubscribeOwned(unit.Object, "ROWS")));
        unit.VerifyRemove(u => u.CurrentChanged -= It.IsAny<EventHandler>(), Times.Once);
        int calls = 0;
        events.OnPreInsert += (_, _) => calls++;
        unit.Raise(u => u.PreInsert += null, unit.Object, new UnitofWorkParams());
        Assert.Equal(0, calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SameNameLeasesRetireOnlyTheirOwnSourceAndDelegates(bool sameSource)
    {
        var events = new EventManager(new Mock<IDMEEditor>().Object);
        var first = new Mock<IUnitofWork>();
        var second = sameSource ? first : new Mock<IUnitofWork>();
        using var a = events.SubscribeOwned(first.Object, "ROWS");
        using var b = events.SubscribeOwned(second.Object, "rows");
        int calls = 0;
        events.OnPreInsert += (_, _) => calls++;
        first.Raise(u => u.PreInsert += null, first.Object, new UnitofWorkParams());
        Assert.Equal(sameSource ? 2 : 1, calls);
        a.Dispose();
        calls = 0;
        first.Raise(u => u.PreInsert += null, first.Object, new UnitofWorkParams());
        Assert.Equal(sameSource ? 1 : 0, calls);
        if (!sameSource) second.Raise(u => u.PreInsert += null, second.Object, new UnitofWorkParams());
        Assert.Equal(1, calls);
        b.Dispose();
        calls = 0;
        second.Raise(u => u.PreInsert += null, second.Object, new UnitofWorkParams());
        Assert.Equal(0, calls);
    }

    [Fact]
    public void FailedLegacyReplacementPreservesOldSubscriptionAndUsesCapturedTeardownSource()
    {
        var events = new EventManager(new Mock<IDMEEditor>().Object);
        var original = new Mock<IUnitofWork>();
        var rejected = new Mock<IUnitofWork>();
        rejected.SetupAdd(u => u.PostQuery += It.IsAny<EventHandler<UnitofWorkParams>>())
            .Throws(new InvalidOperationException("replacement rejected"));
        events.SubscribeToUnitOfWorkEvents(original.Object, "ROWS");
        Assert.Throws<InvalidOperationException>(() => events.SubscribeToUnitOfWorkEvents(rejected.Object, "rows"));
        int calls = 0;
        events.OnPreInsert += (_, _) => calls++;
        original.Raise(u => u.PreInsert += null, original.Object, new UnitofWorkParams());
        Assert.Equal(1, calls);
        events.UnsubscribeFromUnitOfWorkEvents(rejected.Object, "rows");
        original.Raise(u => u.PreInsert += null, original.Object, new UnitofWorkParams());
        Assert.Equal(1, calls);
        original.VerifyRemove(u => u.PreInsert -= It.IsAny<EventHandler<UnitofWorkParams>>(), Times.Once);
    }

    [Fact]
    public void DisposalFailureIsReportedOnceAndLaterDelegatesAreStillRemoved()
    {
        var unit = new Mock<IUnitofWork>();
        var events = new EventManager(new Mock<IDMEEditor>().Object);
        var lease = events.SubscribeOwned(unit.Object, "ROWS");
        unit.SetupRemove(u => u.PreInsert -= It.IsAny<EventHandler<UnitofWorkParams>>())
            .Throws(new InvalidOperationException("detach failed"));
        Assert.Throws<AggregateException>(lease.Dispose);
        lease.Dispose();
        unit.VerifyRemove(u => u.PreInsert -= It.IsAny<EventHandler<UnitofWorkParams>>(), Times.Once);
        unit.VerifyRemove(u => u.CurrentChanged -= It.IsAny<EventHandler>(), Times.Once);
    }

    [Fact]
    public void SameNameManagersUsingSharedEventHelperDoNotStealTeardownOwnership()
    {
        var editor = new Mock<IDMEEditor>();
        var events = new EventManager(editor.Object);
        var firstUnit = new Mock<IUnitofWork>();
        var secondUnit = new Mock<IUnitofWork>();
        using var first = new FormsManager(editor.Object, eventManager: events);
        using var second = new FormsManager(editor.Object, eventManager: events);
        first.RegisterBlock("ROWS", firstUnit.Object, Schema());
        second.RegisterBlock("ROWS", secondUnit.Object, Schema());
        first.Dispose();
        int calls = 0;
        events.OnPreInsert += (_, _) => calls++;
        firstUnit.Raise(u => u.PreInsert += null, firstUnit.Object, new UnitofWorkParams());
        Assert.Equal(0, calls);
        secondUnit.Raise(u => u.PreInsert += null, secondUnit.Object, new UnitofWorkParams());
        Assert.Equal(1, calls);
        second.Dispose();
        secondUnit.Raise(u => u.PreInsert += null, secondUnit.Object, new UnitofWorkParams());
        Assert.Equal(1, calls);
    }

    [Fact]
    public void FailedFreshRegistrationRemovesPublishedLookupAndAllAttemptedSubscriptions()
    {
        var editor = new Mock<IDMEEditor>();
        var events = new EventManager(editor.Object);
        var unit = new Mock<IUnitofWork>();
        var failure = new InvalidOperationException("item attachment failed");
        unit.SetupAdd(u => u.ItemChanged += It.IsAny<EventHandler<ItemChangedEventArgs<Entity>>>()).Throws(failure);
        using var manager = new FormsManager(editor.Object, eventManager: events);
        Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => manager.RegisterBlock("ROWS", unit.Object, Schema())));
        Assert.Empty(manager.Blocks);
        Assert.Null(manager.GetBlock("ROWS"));
        unit.VerifyRemove(u => u.ItemChanged -= It.IsAny<EventHandler<ItemChangedEventArgs<Entity>>>(), Times.Once);
        unit.VerifyRemove(u => u.PreInsert -= It.IsAny<EventHandler<UnitofWorkParams>>(), Times.Once);
        unit.VerifyRemove(u => u.CurrentChanged -= It.IsAny<EventHandler>(), Times.Once);
        int calls = 0;
        events.OnPreInsert += (_, _) => calls++;
        unit.Raise(u => u.PreInsert += null, unit.Object, new UnitofWorkParams());
        Assert.Equal(0, calls);
    }

    [Fact]
    public void FailedItemSeedingClearsPartialItemsWithoutTouchingCache()
    {
        var items = new Mock<IItemPropertyManager>();
        var performance = new Mock<IPerformanceManager>();
        items.Setup(i => i.RegisterItemsFromEntityStructure("ROWS", It.IsAny<IEntityStructure>()))
            .Throws(new InvalidOperationException("item seeding failed"));
        using var manager = new FormsManager(new Mock<IDMEEditor>().Object,
            itemPropertyManager: items.Object, performanceManager: performance.Object);
        Assert.Throws<InvalidOperationException>(() => manager.RegisterBlock("ROWS", new Mock<IUnitofWork>().Object, Schema()));
        Assert.Empty(manager.Blocks);
        items.Verify(i => i.ClearBlockItems("ROWS"), Times.Once);
        performance.Verify(p => p.InvalidateBlockCache("ROWS"), Times.Never);
        performance.Verify(p => p.CacheBlockInfo(It.IsAny<string>(), It.IsAny<Models.DataBlockInfo>()), Times.Never);
    }

    [Fact]
    public void FailedDirectCurrentAttachmentRetiresHelperAndDirectItemSubscription()
    {
        var editor = new Mock<IDMEEditor>();
        var unit = new Mock<IUnitofWork>();
        int attempts = 0;
        unit.SetupAdd(u => u.CurrentChanged += It.IsAny<EventHandler>())
            .Callback(() => { if (++attempts == 2) throw new InvalidOperationException("direct current attachment failed"); });
        using var manager = new FormsManager(editor.Object);
        Assert.Throws<InvalidOperationException>(() => manager.RegisterBlock("ROWS", unit.Object, Schema()));
        Assert.Empty(manager.Blocks);
        unit.VerifyRemove(u => u.CurrentChanged -= It.IsAny<EventHandler>(), Times.Exactly(2));
        unit.VerifyRemove(u => u.ItemChanged -= It.IsAny<EventHandler<ItemChangedEventArgs<Entity>>>(), Times.Once);
        unit.VerifyRemove(u => u.PreInsert -= It.IsAny<EventHandler<UnitofWorkParams>>(), Times.Once);
    }

    [Fact]
    public void FailingCachePublicationIsInvalidatedEvenBeforeRegistryPublication()
    {
        var performance = new Mock<IPerformanceManager>();
        performance.Setup(p => p.CacheBlockInfo("ROWS", It.IsAny<Models.DataBlockInfo>()))
            .Throws(new InvalidOperationException("cache publication failed"));
        using var manager = new FormsManager(new Mock<IDMEEditor>().Object, performanceManager: performance.Object);
        Assert.Throws<InvalidOperationException>(() => manager.RegisterBlock("ROWS", new Mock<IUnitofWork>().Object, Schema()));
        Assert.Empty(manager.Blocks);
        performance.Verify(p => p.InvalidateBlockCache("ROWS"), Times.Once);
    }

    [Fact]
    public void FailureNotificationCannotReplaceOriginalRegistrationException()
    {
        var events = new Mock<IEventManager>();
        var failure = new InvalidOperationException("subscription failed");
        events.Setup(e => e.SubscribeToUnitOfWorkEvents(It.IsAny<IUnitofWork>(), "ROWS")).Throws(failure);
        events.Setup(e => e.TriggerError("ROWS", failure)).Throws(new InvalidOperationException("observer failed"));
        using var manager = new FormsManager(new Mock<IDMEEditor>().Object, eventManager: events.Object);
        Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => manager.RegisterBlock("ROWS", new Mock<IUnitofWork>().Object, Schema())));
        Assert.Empty(manager.Blocks);
        Assert.Contains(manager.CleanupFailures, f => f.Exception.Message == "observer failed");
    }

    [Fact]
    public void FailedOwnedFactoryDoesNotUnsubscribeUnrelatedLegacySubscription()
    {
        var editor = new Mock<IDMEEditor>();
        var events = new EventManager(editor.Object);
        var existing = new Mock<IUnitofWork>();
        events.SubscribeToUnitOfWorkEvents(existing.Object, "ROWS");
        var rejected = new Mock<IUnitofWork>();
        rejected.SetupAdd(u => u.PreInsert += It.IsAny<EventHandler<UnitofWorkParams>>())
            .Throws(new InvalidOperationException("attach failed"));
        using var manager = new FormsManager(editor.Object, eventManager: events);
        Assert.Throws<InvalidOperationException>(() => manager.RegisterBlock("ROWS", rejected.Object, Schema()));
        int calls = 0;
        events.OnPreInsert += (_, _) => calls++;
        existing.Raise(u => u.PreInsert += null, existing.Object, new UnitofWorkParams());
        Assert.Equal(1, calls);
        events.UnsubscribeFromUnitOfWorkEvents(existing.Object, "ROWS");
    }

    [Fact]
    public void OwnerClosedDuringFactoryRetiresReturnedLeaseWithoutLegacyUnsubscribe()
    {
        var events = new Mock<IEventManager>();
        var owned = events.As<IOwnedUnitOfWorkEventSubscriptions>();
        var lease = new Mock<IDisposable>();
        var performance = new Mock<IPerformanceManager>();
        using var manager = new FormsManager(new Mock<IDMEEditor>().Object,
            eventManager: events.Object, performanceManager: performance.Object);
        owned.Setup(e => e.SubscribeOwned(It.IsAny<IUnitofWork>(), "ROWS"))
            .Callback(manager.Dispose).Returns(lease.Object);
        Assert.Throws<ObjectDisposedException>(() => manager.RegisterBlock("ROWS", new Mock<IUnitofWork>().Object, Schema()));
        lease.Verify(l => l.Dispose(), Times.Once);
        events.Verify(e => e.UnsubscribeFromUnitOfWorkEvents(It.IsAny<IUnitofWork>(), It.IsAny<string>()), Times.Never);
        Assert.Empty(manager.Blocks);
    }
}
