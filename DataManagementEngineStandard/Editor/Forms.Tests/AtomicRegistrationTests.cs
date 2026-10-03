using Moq;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Forms.Helpers;
using TheTechIdea.Beep.Editor.UOWManager.Helpers;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using Xunit;

namespace TheTechIdea.Beep.Editor.UOWManager.Tests;

public class AtomicRegistrationTests
{
    private static EntityStructure Schema(params string[] fields) => new()
    {
        EntityName = "Rows", Fields = fields.Select(f => new EntityField { FieldName = f, Fieldtype = "System.String" }).ToList()
    };

    private static Mock<IUnitofWork> Unit()
    {
        var unit = new Mock<IUnitofWork>();
        unit.SetupProperty(u => u.EntityStructure);
        unit.SetupProperty(u => u.DataSource);
        return unit;
    }

    private sealed class FaultItems : ItemPropertyManager, IPreparedBlockItems
    {
        internal string? Failure;
        internal readonly InvalidOperationException Error = new("item preparation failed");
        IBlockItemsRegistration IPreparedBlockItems.PrepareBlockItems(Guid owner, string name, IEntityStructure schema)
        {
            if (Failure == "itemPrepare") throw Error;
            var stage = PrepareBlockItems(owner, name, schema);
            return Failure == "itemCommit" ? new RejectPublication(stage, Error) : stage;
        }
        private sealed class RejectPublication(IBlockItemsRegistration stage, Exception error) : IBlockItemsRegistration
        {
            public void Commit(Action publish) => stage.Commit(() => throw error);
            public void Dispose() => stage.Dispose();
        }
    }

    [Theory]
    [InlineData("PreInsert")]
    [InlineData("ItemChanged")]
    [InlineData("CurrentChanged")]
    [InlineData("cache")]
    [InlineData("metadata")]
    [InlineData("itemPrepare")]
    [InlineData("itemCommit")]
    public void FailedReplacementPreservesOldRegistrationItemStateAndEventOwnership(string point)
    {
        var editor = new Mock<IDMEEditor>();
        var events = new EventManager(editor.Object);
        using var items = new FaultItems();
        var performance = new Mock<IPerformanceManager>();
        using var manager = new FormsManager(editor.Object, eventManager: events,
            itemPropertyManager: items, performanceManager: performance.Object);
        var oldUnit = Unit();
        manager.RegisterBlock("ROWS", oldUnit.Object, Schema("Name", "OldField"));
        var oldBlock = manager.GetBlock("ROWS");
        var oldItem = items.GetItem("ROWS", "Name");
        oldItem.IsDirty = true;
        oldItem.HasFocus = true;
        oldItem.ValidationRuleNames.Add("required");
        oldItem.SetError("original error");
        items.SetTabOrder("ROWS", new[] { "OldField", "Name" });
        var replacement = Unit();
        var error = new InvalidOperationException("setup failed");
        switch (point)
        {
            case "PreInsert": replacement.SetupAdd(u => u.PreInsert += It.IsAny<EventHandler<UnitofWorkParams>>()).Throws(error); break;
            case "ItemChanged": replacement.SetupAdd(u => u.ItemChanged += It.IsAny<EventHandler<ItemChangedEventArgs<Entity>>>()).Throws(error); break;
            case "CurrentChanged":
                int count = 0;
                replacement.SetupAdd(u => u.CurrentChanged += It.IsAny<EventHandler>())
                    .Callback(() => { if (++count == 2) throw error; });
                break;
            case "cache":
                performance.Setup(p => p.CacheBlockInfo("ROWS", It.Is<DataBlockInfo>(b => b.UnitOfWork == replacement.Object))).Throws(error);
                break;
            case "metadata":
                EntityStructure? assigned = null;
                replacement.SetupGet(u => u.EntityStructure).Returns(() => assigned!);
                replacement.SetupSet(u => u.EntityStructure = It.IsAny<EntityStructure>())
                    .Callback((EntityStructure value) => { assigned = value; if (value != null) throw error; });
                break;
            default: items.Failure = point; error = items.Error; break;
        }

        Assert.Same(error, Assert.Throws<InvalidOperationException>(() => manager.RegisterBlock("rows", replacement.Object, Schema("Name", "NewField"))));
        Assert.Same(oldBlock, manager.GetBlock("ROWS"));
        Assert.True(oldBlock.IsRegistered);
        Assert.Equal("ROWS", manager.CurrentBlockName);
        Assert.Same(oldItem, items.GetItem("ROWS", "Name"));
        Assert.True(oldItem.IsDirty);
        Assert.True(oldItem.HasFocus);
        Assert.Equal("original error", oldItem.ErrorMessage);
        Assert.Equal(new[] { "required" }, oldItem.ValidationRuleNames);
        Assert.Equal(new[] { "OldField", "Name" }, items.GetTabOrder("ROWS"));
        Assert.False(items.ItemExists("ROWS", "NewField"));
        Assert.Null(replacement.Object.EntityStructure);
        int calls = 0;
        events.OnPreInsert += (_, _) => calls++;
        replacement.Raise(u => u.PreInsert += null, replacement.Object, new UnitofWorkParams());
        Assert.Equal(0, calls);
        oldUnit.Raise(u => u.PreInsert += null, oldUnit.Object, new UnitofWorkParams());
        Assert.Equal(1, calls);
        oldUnit.Verify(u => u.Dispose(), Times.Never);
        replacement.Verify(u => u.Dispose(), Times.Never);
    }

    [Fact]
    public void SuccessfulReplacementPublishesNewItemsWithoutRetiringThemWithTheOldLease()
    {
        var editor = new Mock<IDMEEditor>();
        var events = new EventManager(editor.Object);
        using var items = new ItemPropertyManager();
        using var manager = new FormsManager(editor.Object, eventManager: events, itemPropertyManager: items);
        var first = Unit();
        var second = Unit();
        manager.RegisterBlock("ROWS", first.Object, Schema("Name", "OldField"));
        var old = manager.GetBlock("ROWS");
        var snapshot = manager.Blocks;
        var replacementSchema = Schema("Name", "NewField");
        manager.RegisterBlock("rows", second.Object, replacementSchema);
        Assert.False(old.IsRegistered);
        Assert.Same(second.Object, manager.GetUnitOfWork("ROWS"));
        Assert.True(items.ItemExists("ROWS", "NewField"));
        Assert.False(items.ItemExists("ROWS", "OldField"));
        Assert.Same(old, snapshot["ROWS"]);
        int calls = 0;
        events.OnPreInsert += (_, _) => calls++;
        first.Raise(u => u.PreInsert += null, first.Object, new UnitofWorkParams());
        Assert.Equal(0, calls);
        second.Raise(u => u.PreInsert += null, second.Object, new UnitofWorkParams());
        Assert.Equal(1, calls);
        Assert.True(manager.UnregisterBlock("rows"));
        Assert.Empty(items.GetAllItems("ROWS"));
        Assert.Null(manager.CurrentBlockName);
        Assert.Empty(manager.SystemVariables.GetFormSystemVariables().CURRENT_BLOCK);
        Assert.Same(replacementSchema, second.Object.EntityStructure);
        first.Verify(u => u.Dispose(), Times.Never);
        second.Verify(u => u.Dispose(), Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CloseRevokesPendingSetupAndDrainsItsAcknowledgedCleanup(bool replacing)
    {
        var editor = new Mock<IDMEEditor>();
        using var items = new ItemPropertyManager();
        var manager = new FormsManager(editor.Object, itemPropertyManager: items);
        var original = Unit();
        if (replacing) manager.RegisterBlock("ROWS", original.Object, Schema("Name"));
        var pending = Unit();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        pending.SetupAdd(u => u.PreInsert += It.IsAny<EventHandler<UnitofWorkParams>>())
            .Callback(() => { entered.SetResult(); release.Task.GetAwaiter().GetResult(); });
        var preparation = Task.Run(() => manager.RegisterBlock("ROWS", pending.Object, Schema("Name", "NewField")));
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal(replacing, manager.BlockExists("ROWS"));
            Assert.False(items.ItemExists("ROWS", "NewField"));
            var drain = manager.DisposeAsync().AsTask();
            Assert.False(drain.IsCompleted);
            Assert.Equal(1, manager.PendingCallbackCount);
            pending.VerifyRemove(u => u.PreInsert -= It.IsAny<EventHandler<UnitofWorkParams>>(), Times.Never);
            release.SetResult();
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await preparation.WaitAsync(TimeSpan.FromSeconds(10)));
            await drain.WaitAsync(TimeSpan.FromSeconds(10));
            pending.VerifyRemove(u => u.PreInsert -= It.IsAny<EventHandler<UnitofWorkParams>>(), Times.Once);
            Assert.Empty(manager.Blocks);
            Assert.Empty(items.GetAllItems("ROWS"));
            Assert.Null(pending.Object.EntityStructure);
            Assert.Equal(0, manager.PendingCallbackCount);
        }
        finally { release.TrySetResult(); manager.Dispose(); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnregisterRevokesPreparationAndKeepsTheNameReservedUntilCleanup(bool replacing)
    {
        var editor = new Mock<IDMEEditor>();
        var events = new EventManager(editor.Object);
        using var items = new ItemPropertyManager();
        using var manager = new FormsManager(editor.Object, eventManager: events, itemPropertyManager: items);
        if (replacing) manager.RegisterBlock("ROWS", Unit().Object, Schema("Name"));
        var pending = Unit();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int errors = 0;
        events.OnError += (_, _) => errors++;
        pending.SetupAdd(u => u.PreInsert += It.IsAny<EventHandler<UnitofWorkParams>>())
            .Callback(() => { entered.SetResult(); release.Task.GetAwaiter().GetResult(); });
        var preparation = Task.Run(() => manager.RegisterBlock("ROWS", pending.Object, Schema("Name")));
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.True(manager.UnregisterBlock("rows"));
            Assert.False(manager.UnregisterBlock("ROWS"));
            Assert.False(manager.BlockExists("ROWS"));
            Assert.Throws<InvalidOperationException>(() => manager.RegisterBlock("ROWS", Unit().Object, Schema("Name")));
            release.SetResult();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await preparation.WaitAsync(TimeSpan.FromSeconds(10)));
            await manager.WaitForPendingCallbacksAsync().WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal(0, errors);
            Assert.Empty(items.GetAllItems("ROWS"));
            manager.RegisterBlock("ROWS", Unit().Object, Schema("Name"));
            Assert.True(manager.BlockExists("ROWS"));
        }
        finally { release.TrySetResult(); }
    }

    [Fact]
    public async Task ConflictingPreparationIsRejectedWhileOtherNamesCanPublish()
    {
        var editor = new Mock<IDMEEditor>();
        using var manager = new FormsManager(editor.Object);
        var pending = Unit();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        pending.SetupAdd(u => u.PreInsert += It.IsAny<EventHandler<UnitofWorkParams>>())
            .Callback(() => { entered.SetResult(); release.Task.GetAwaiter().GetResult(); });
        var preparation = Task.Run(() => manager.RegisterBlock("ROWS", pending.Object, Schema("Name")));
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Throws<InvalidOperationException>(() => manager.RegisterBlock("rows", Unit().Object, Schema("Name")));
            manager.RegisterBlock("OTHER", Unit().Object, Schema("Name"));
            Assert.True(manager.BlockExists("OTHER"));
            Assert.False(manager.BlockExists("ROWS"));
            release.SetResult();
            await preparation.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal(2, manager.BlockCount);
            Assert.Equal("OTHER", manager.CurrentBlockName);
        }
        finally { release.TrySetResult(); }
    }

    [Fact]
    public void ReentrantPreparationCommandIsRejectedWithoutRecursiveErrorDelivery()
    {
        var editor = new Mock<IDMEEditor>();
        var events = new EventManager(editor.Object);
        using var manager = new FormsManager(editor.Object, eventManager: events);
        var unit = Unit();
        int errors = 0;
        events.OnError += (_, _) => { errors++; manager.RegisterBlock("ROWS", Unit().Object, Schema("Name")); };
        unit.SetupAdd(u => u.PreInsert += It.IsAny<EventHandler<UnitofWorkParams>>())
            .Callback(() => Assert.Throws<InvalidOperationException>(() => manager.RegisterBlock("rows", Unit().Object, Schema("Name"))));
        manager.RegisterBlock("ROWS", unit.Object, Schema("Name"));
        Assert.Same(unit.Object, manager.GetUnitOfWork("ROWS"));
        Assert.Equal(0, errors);
    }

    [Fact]
    public void CandidateEventStreamIsGatedUntilRegistrationPublication()
    {
        var editor = new Mock<IDMEEditor>();
        var events = new EventManager(editor.Object);
        using var manager = new FormsManager(editor.Object, eventManager: events);
        var unit = Unit();
        int calls = 0;
        events.OnPreInsert += (_, _) => calls++;
        unit.SetupAdd(u => u.ItemChanged += It.IsAny<EventHandler<ItemChangedEventArgs<Entity>>>())
            .Callback(() => unit.Raise(u => u.PreInsert += null, unit.Object, new UnitofWorkParams()));
        manager.RegisterBlock("ROWS", unit.Object, Schema("Name"));
        Assert.Equal(0, calls);
        unit.Raise(u => u.PreInsert += null, unit.Object, new UnitofWorkParams());
        Assert.Equal(1, calls);
    }

    [Fact]
    public void PostPublicationObserverFailureDoesNotUndoACompleteRegistration()
    {
        var events = new Mock<IEventManager>();
        events.Setup(e => e.TriggerBlockEnter("ROWS")).Throws(new InvalidOperationException("observer failed"));
        using var manager = new FormsManager(new Mock<IDMEEditor>().Object, eventManager: events.Object);
        var unit = Unit();
        manager.RegisterBlock("ROWS", unit.Object, Schema("Name"));
        Assert.Same(unit.Object, manager.GetUnitOfWork("ROWS"));
        Assert.True(manager.GetBlock("ROWS").IsRegistered);
        Assert.Contains(manager.CleanupFailures, f => f.Exception.Message == "observer failed");
    }

    [Fact]
    public void OldAccessorRemovalFailureCannotRetargetOrRemoveReplacementItems()
    {
        var editor = new Mock<IDMEEditor>();
        var events = new EventManager(editor.Object);
        using var items = new ItemPropertyManager();
        using var manager = new FormsManager(editor.Object, eventManager: events, itemPropertyManager: items);
        var first = Unit();
        manager.RegisterBlock("ROWS", first.Object, Schema("Name"));
        first.SetupRemove(u => u.PreInsert -= It.IsAny<EventHandler<UnitofWorkParams>>()).Throws(new InvalidOperationException("detach failed"));
        var second = Unit();
        manager.RegisterBlock("ROWS", second.Object, Schema("Name", "NewField"));
        Assert.True(items.ItemExists("ROWS", "NewField"));
        Assert.Same(second.Object, manager.GetUnitOfWork("ROWS"));
        Assert.Contains(manager.CleanupFailures, f => f.Exception is AggregateException);
        int calls = 0;
        events.OnPreInsert += (_, _) => calls++;
        first.Raise(u => u.PreInsert += null, first.Object, new UnitofWorkParams());
        Assert.Equal(0, calls);
    }

    [Fact]
    public void SharedItemStoreRejectsCrossFormNameAliasingWithoutChangingItsOwner()
    {
        var editor = new Mock<IDMEEditor>();
        using var items = new ItemPropertyManager();
        using var first = new FormsManager(editor.Object, itemPropertyManager: items);
        using var second = new FormsManager(editor.Object, itemPropertyManager: items);
        first.RegisterBlock("ROWS", Unit().Object, Schema("Name"));
        var original = items.GetItem("ROWS", "Name");
        Assert.Throws<InvalidOperationException>(() => second.RegisterBlock("ROWS", Unit().Object, Schema("NewField")));
        Assert.Same(original, items.GetItem("ROWS", "Name"));
        Assert.True(first.BlockExists("ROWS"));
        Assert.False(second.BlockExists("ROWS"));
        second.Dispose();
        Assert.Same(original, items.GetItem("ROWS", "Name"));
        first.Dispose();
        Assert.Empty(items.GetAllItems("ROWS"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void UnsupportedHelperReplacementIsRejectedBeforeChangingTheOldRegistration(int helperKind)
    {
        var editor = new Mock<IDMEEditor>();
        var eventMock = new Mock<IEventManager>();
        if (helperKind == 2)
            eventMock.As<IOwnedUnitOfWorkEventSubscriptions>()
                .Setup(e => e.SubscribeOwned(It.IsAny<IUnitofWork>(), It.IsAny<string>()))
                .Returns(() => new Mock<IDisposable>().Object);
        var events = helperKind == 1 ? new EventManager(editor.Object) : eventMock.Object;
        IItemPropertyManager items = helperKind == 1 ? new Mock<IItemPropertyManager>().Object : new ItemPropertyManager();
        using var manager = new FormsManager(editor.Object, eventManager: events, itemPropertyManager: items);
        var first = Unit();
        manager.RegisterBlock("ROWS", first.Object, Schema("Name"));
        var old = manager.GetBlock("ROWS");
        var rejected = Unit();
        Assert.Throws<NotSupportedException>(() => manager.RegisterBlock("ROWS", rejected.Object, Schema("Name")));
        Assert.Same(old, manager.GetBlock("ROWS"));
        rejected.VerifyAdd(u => u.PreInsert += It.IsAny<EventHandler<UnitofWorkParams>>(), Times.Never);
        rejected.VerifySet(u => u.EntityStructure = It.IsAny<EntityStructure>(), Times.Never);
    }

    [Fact]
    public void EnterObserverCanUnregisterWithoutLeavingCurrentBlockOrItemsBehind()
    {
        var editor = new Mock<IDMEEditor>();
        var events = new EventManager(editor.Object);
        using var items = new ItemPropertyManager();
        using var manager = new FormsManager(editor.Object, eventManager: events, itemPropertyManager: items);
        events.OnBlockEnter += (_, _) => manager.UnregisterBlock("ROWS");
        var unit = Unit();
        manager.RegisterBlock("ROWS", unit.Object, Schema("Name"));
        Assert.Empty(manager.Blocks);
        Assert.Empty(items.GetAllItems("ROWS"));
        Assert.Null(manager.CurrentBlockName);
        Assert.Empty(manager.SystemVariables.GetFormSystemVariables().CURRENT_BLOCK);
        unit.VerifyRemove(u => u.ItemChanged -= It.IsAny<EventHandler<ItemChangedEventArgs<Entity>>>(), Times.Once);
    }

    private sealed class Row : Entity { }

    [Fact]
    public void SourceResolutionIsPreparedBeforeEntityTypeInferenceAndRetainedOnSuccess()
    {
        var editor = new Mock<IDMEEditor>();
        var source = new Mock<IDataSource>();
        var schema = Schema("Name");
        source.Setup(s => s.GetEntityStructure("Rows", false)).Returns(schema);
        source.Setup(s => s.GetEntityType("Rows")).Returns(typeof(Row));
        editor.Setup(e => e.GetDataSource("DB")).Returns(source.Object);
        var unit = Unit();
        unit.SetupGet(u => u.EntityName).Returns("Rows");
        unit.SetupGet(u => u.DatasourceName).Returns("DB");
        using var manager = new FormsManager(editor.Object);
        manager.RegisterBlock("ROWS", unit.Object);
        Assert.Same(source.Object, unit.Object.DataSource);
        Assert.Same(schema, unit.Object.EntityStructure);
        Assert.Equal(typeof(Row), manager.GetBlock("ROWS").EntityType);
    }

    [Fact]
    public void FailedPreparationRestoresResolvedSourceAndMetadataWithoutDisposingTheProvider()
    {
        var editor = new Mock<IDMEEditor>();
        var source = new Mock<IDataSource>();
        source.Setup(s => s.GetEntityStructure("Rows", false)).Returns(Schema("Name"));
        editor.Setup(e => e.GetDataSource("DB")).Returns(source.Object);
        var unit = Unit();
        unit.SetupGet(u => u.EntityName).Returns("Rows");
        unit.SetupGet(u => u.DatasourceName).Returns("DB");
        var performance = new Mock<IPerformanceManager>();
        performance.Setup(p => p.CacheBlockInfo("ROWS", It.IsAny<DataBlockInfo>())).Throws(new InvalidOperationException("cache failed"));
        using var manager = new FormsManager(editor.Object, performanceManager: performance.Object);
        Assert.Throws<InvalidOperationException>(() => manager.RegisterBlock("ROWS", unit.Object));
        Assert.Null(unit.Object.EntityStructure);
        Assert.Null(unit.Object.DataSource);
        Assert.Empty(manager.Blocks);
        source.Verify(s => s.Dispose(), Times.Never);
    }

    [Fact]
    public void MutableBlockNameCannotRedirectCapturedLeaseCleanup()
    {
        var editor = new Mock<IDMEEditor>();
        using var items = new ItemPropertyManager();
        using var manager = new FormsManager(editor.Object, itemPropertyManager: items);
        var unit = Unit();
        manager.RegisterBlock("ROWS", unit.Object, Schema("Name"));
        manager.GetBlock("ROWS").BlockName = "BROKEN";
        Assert.True(manager.UnregisterBlock("ROWS"));
        Assert.Empty(items.GetAllItems("ROWS"));
        unit.VerifyRemove(u => u.ItemChanged -= It.IsAny<EventHandler<ItemChangedEventArgs<Entity>>>(), Times.Once);
        unit.VerifyRemove(u => u.CurrentChanged -= It.IsAny<EventHandler>(), Times.Exactly(2));
        Assert.Empty(manager.Blocks);
    }

    [Theory]
    [InlineData("")]
    [InlineData("name")]
    public void InvalidItemNamesRejectReplacementWithoutChangingOldItems(string extraField)
    {
        using var items = new ItemPropertyManager();
        using var manager = new FormsManager(new Mock<IDMEEditor>().Object, itemPropertyManager: items);
        manager.RegisterBlock("ROWS", Unit().Object, Schema("Name"));
        var old = manager.GetBlock("ROWS");
        var oldItem = items.GetItem("ROWS", "Name");
        Assert.Throws<InvalidOperationException>(() => manager.RegisterBlock("ROWS", Unit().Object, Schema("Name", extraField)));
        Assert.Same(old, manager.GetBlock("ROWS"));
        Assert.Same(oldItem, items.GetItem("ROWS", "Name"));
    }

    [Fact]
    public void DisposedItemHelperCannotBeResurrectedByLegacyItemRegistration()
    {
        var items = new ItemPropertyManager();
        items.Dispose();
        Assert.Throws<ObjectDisposedException>(() => items.RegisterItem("ROWS", "Name", new ItemInfo()));
        Assert.Throws<ObjectDisposedException>(() => items.PrepareBlockItems(Guid.NewGuid(), "ROWS", Schema("Name")));
        Assert.Empty(items.GetAllItems("ROWS"));
    }
}
