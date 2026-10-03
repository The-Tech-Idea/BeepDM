using Moq;
using TheTechIdea.Beep.Editor.Forms.Helpers;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using Xunit;

namespace TheTechIdea.Beep.Editor.UOWManager.Tests;

public class PermissionProjectionTests
{
    [Fact]
    public async Task ClearingDeniedRulesRestoresUnconfiguredFlagsAndQueries()
    {
        using var h = new QueryPolicyTests.Harness();
        h.Unit.Setup(u => u.Get()).ReturnsAsync((object)new List<object>());
        var block = h.Manager.GetBlock("ROWS");
        var item = h.Manager.ItemProperties.GetItem("ROWS", "Name");
        h.Manager.SetBlockSecurity("ROWS", new BlockSecurity
        { AllowQuery = false, AllowInsert = false, AllowUpdate = false, AllowDelete = false });
        h.Manager.SetFieldSecurity("ROWS", "Name", new FieldSecurity { Visible = false, Editable = false });
        Assert.False(block.QueryAllowed); Assert.False(block.InsertAllowed);
        Assert.False(block.UpdateAllowed); Assert.False(block.DeleteAllowed);
        Assert.False(item.Enabled); Assert.False(item.Visible);
        Assert.False(await h.Manager.ExecuteQueryAsync("ROWS"));
        h.Unit.Verify(u => u.Get(), Times.Never);
        h.Manager.ClearBlockSecurity("ROWS");
        Assert.True(block.QueryAllowed); Assert.True(block.InsertAllowed);
        Assert.True(block.UpdateAllowed); Assert.True(block.DeleteAllowed);
        Assert.True(item.Enabled); Assert.True(item.Visible);
        Assert.True(await h.Manager.ExecuteQueryAsync("ROWS"));
        h.Unit.Verify(u => u.Get(), Times.Once);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void BlockConfigurationSurvivesDenialAndGrant(bool authoredDuringDenial, bool admin)
    {
        using var h = new QueryPolicyTests.Harness();
        var block = h.Manager.GetBlock("ROWS");
        void Configure() { block.QueryAllowed = false; block.InsertAllowed = false; block.UpdateAllowed = false; block.DeleteAllowed = false; }
        if (!authoredDuringDenial) Configure();
        h.Manager.SetBlockSecurity("ROWS", new BlockSecurity
        { AllowQuery = false, AllowInsert = false, AllowUpdate = false, AllowDelete = false });
        if (authoredDuringDenial) Configure();
        if (admin) h.Manager.SetSecurityContext(new SecurityContext { IsAdmin = true });
        else h.Manager.ClearBlockSecurity("ROWS");
        Assert.False(block.QueryAllowed); Assert.False(block.InsertAllowed);
        Assert.False(block.UpdateAllowed); Assert.False(block.DeleteAllowed);
        block.QueryAllowed = true; block.InsertAllowed = true; block.UpdateAllowed = true; block.DeleteAllowed = true;
        Assert.True(block.QueryAllowed); Assert.True(block.InsertAllowed);
        Assert.True(block.UpdateAllowed); Assert.True(block.DeleteAllowed);
    }

    [Theory]
    [InlineData("typed", false)]
    [InlineData("typed", true)]
    [InlineData("generic", false)]
    [InlineData("generic", true)]
    [InlineData("direct", false)]
    [InlineData("direct", true)]
    public void SameValueWritesWhileDeniedConfigureTheItemWithoutLiftingPolicy(string route, bool configured)
    {
        using var h = new QueryPolicyTests.Harness();
        var items = h.Manager.ItemProperties;
        var item = items.GetItem("ROWS", "Name");
        h.Manager.SetFieldSecurity("ROWS", "Name", new FieldSecurity { Editable = false, Visible = false });
        var changes = new List<ItemPropertyChangedEventArgs>();
        items.ItemPropertyChanged += (_, e) => changes.Add(e);
        if (route == "typed") { items.SetItemEnabled("ROWS", "Name", configured); items.SetItemVisible("ROWS", "Name", configured); }
        if (route == "generic") { items.SetItemProperty("ROWS", "Name", nameof(ItemInfo.Enabled), configured); items.SetItemProperty("ROWS", "Name", nameof(ItemInfo.Visible), configured); }
        if (route == "direct") { item.Enabled = configured; item.Visible = configured; }
        Assert.False(item.Enabled); Assert.False(item.Visible);
        Assert.Empty(changes);
        h.Manager.ClearBlockSecurity("ROWS");
        Assert.Equal(configured, item.Enabled); Assert.Equal(configured, item.Visible);
        Assert.Equal(configured ? 2 : 0, changes.Count);
        Assert.All(changes, e => Assert.True(Assert.IsType<bool>(e.NewValue)));
    }

    [Fact]
    public void DefinitionCloneDoesNotCarryRuntimeDenialAndWireFormatDoesNotExposeConfigurationInternals()
    {
        using var h = new QueryPolicyTests.Harness();
        var item = h.Manager.ItemProperties.GetItem("ROWS", "Name");
        h.Manager.SetFieldSecurity("ROWS", "Name", new FieldSecurity { Editable = false, Visible = false });
        var clone = item.Clone();
        Assert.True(clone.Enabled); Assert.True(clone.Visible);
        // System.Type metadata already requires an application serializer policy.
        var resolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(info =>
        {
            var typeMetadata = info.Properties.FirstOrDefault(p => p.Name == nameof(ItemInfo.DataType));
            if (info.Type == typeof(ItemInfo) && typeMetadata != null) info.Properties.Remove(typeMetadata);
        });
        var json = System.Text.Json.JsonSerializer.Serialize(item, new System.Text.Json.JsonSerializerOptions { TypeInfoResolver = resolver });
        Assert.Contains("\"Enabled\":false", json);
        Assert.DoesNotContain("Configured", json);
        Assert.DoesNotContain("Configured", Newtonsoft.Json.JsonConvert.SerializeObject(item));
        Assert.DoesNotContain(typeof(DataBlockInfo).GetProperties(), p => p.Name.StartsWith("Configured"));
        item.Enabled = false; item.Visible = false;
        clone = item.Clone();
        Assert.False(clone.Enabled); Assert.False(clone.Visible);
    }

    [Fact]
    public void ReplacementGetsNewDefinitionFlagsButRetainsCurrentPolicy()
    {
        using var h = new QueryPolicyTests.Harness();
        var old = h.Manager.ItemProperties.GetItem("ROWS", "Name");
        old.Enabled = false; old.Visible = false;
        h.Manager.SetFieldSecurity("ROWS", "Name", new FieldSecurity { Editable = false, Visible = false });
        h.Manager.RegisterBlock("ROWS", h.Unit.Object, h.Manager.GetBlock("ROWS").EntityStructure, "db");
        var current = h.Manager.ItemProperties.GetItem("ROWS", "Name");
        Assert.NotSame(old, current);
        Assert.False(current.Enabled); Assert.False(current.Visible);
        h.Manager.ClearBlockSecurity("ROWS");
        Assert.True(current.Enabled); Assert.True(current.Visible);
        Assert.False(old.Enabled); Assert.False(old.Visible);
    }

    [Fact]
    public void ThrowingProjectionObserverDoesNotPreventOthersOrUndoEffectiveFlags()
    {
        using var h = new QueryPolicyTests.Harness();
        var changes = new List<ItemPropertyChangedEventArgs>();
        var failure = new InvalidOperationException("observer");
        h.Manager.ItemProperties.ItemPropertyChanged += (_, _) => throw failure;
        h.Manager.ItemProperties.ItemPropertyChanged += (_, e) => changes.Add(e);
        h.Manager.SetFieldSecurity("ROWS", "Name", new FieldSecurity { Editable = false, Visible = false });
        Assert.Equal(2, changes.Count);
        Assert.False(h.Manager.ItemProperties.GetItem("ROWS", "Name").Enabled);
        Assert.Contains(failure, h.Manager.PolicyNotificationFailures);
    }

    [Fact]
    public void ReentrantNewPolicyStopsOldProjectionNotifications()
    {
        using var h = new QueryPolicyTests.Harness();
        var changes = new List<bool>(); var superseded = false;
        h.Manager.ItemProperties.ItemPropertyChanged += (_, _) =>
        {
            if (superseded) return;
            superseded = true;
            h.Manager.ClearBlockSecurity("ROWS");
        };
        h.Manager.ItemProperties.ItemPropertyChanged += (_, e) => changes.Add(Assert.IsType<bool>(e.NewValue));
        h.Manager.SetFieldSecurity("ROWS", "Name", new FieldSecurity { Editable = false, Visible = false });
        Assert.Equal(new[] { true, true }, changes);
        var item = h.Manager.ItemProperties.GetItem("ROWS", "Name");
        Assert.True(item.Enabled); Assert.True(item.Visible);
    }

    [Fact]
    public void ReplacedItemStopsNotificationsForTheCapturedRegistry()
    {
        using var h = new QueryPolicyTests.Harness();
        var delivered = 0; var replaced = false;
        h.Manager.ItemProperties.ItemPropertyChanged += (_, _) =>
        {
            if (replaced) return;
            replaced = true;
            h.Manager.ItemProperties.RegisterItem("ROWS", "Name", new ItemInfo());
        };
        h.Manager.ItemProperties.ItemPropertyChanged += (_, _) => delivered++;
        h.Manager.SetFieldSecurity("ROWS", "Name", new FieldSecurity { Editable = false, Visible = false });
        Assert.True(replaced); Assert.Equal(0, delivered);
    }

    [Theory]
    [InlineData("registry")]
    [InlineData("duplicate")]
    [InlineData("late")]
    [InlineData("foreign thread")]
    public void ProjectionRejectsStaleDuplicateOrLatePublication(string route)
    {
        using var helper = new ItemPropertyManager();
        var item = new ItemInfo(); helper.RegisterItem("ROWS", "Name", item);
        var flag = new ItemSecurityProjection("Name", item, false, false);
        var projection = (IItemSecurityProjection)helper;
        var flags = route == "duplicate" ? new[] { flag, flag } : new[] { flag };
        if (route == "registry") helper.RegisterItem("ROWS", "Name", new ItemInfo());
        Action? late = null;
        Assert.Throws<InvalidOperationException>(() => projection.PublishSecurityFlags("ROWS", Guid.NewGuid(), 1,
            flags, action =>
            {
                if (route == "late") late = action;
                else if (route == "foreign thread")
                {
                    Exception? failure = null;
                    var worker = new Thread(() => { try { action(); } catch (Exception ex) { failure = ex; } }) { IsBackground = true };
                    worker.Start(); Assert.True(worker.Join(TimeSpan.FromSeconds(10)));
                    if (failure != null) throw failure;
                }
                else action();
            }, () => true));
        if (late != null) Assert.Throws<InvalidOperationException>(late);
        Assert.True(item.Enabled); Assert.True(item.Visible);
    }

    [Fact]
    public void CapturedModelRejectsAnotherOwnerAndOlderRevision()
    {
        using var helper = new ItemPropertyManager();
        var item = new ItemInfo(); helper.RegisterItem("ROWS", "Name", item);
        var projection = (IItemSecurityProjection)helper;
        var owner = Guid.NewGuid();
        void Publish(Guid identity, long revision, bool allowed) => projection.PublishSecurityFlags("ROWS", identity,
            revision, new[] { new ItemSecurityProjection("Name", item, allowed, allowed) }, action => action(), () => true);
        Publish(owner, 2, false);
        Assert.Throws<InvalidOperationException>(() => Publish(Guid.NewGuid(), 3, true));
        Assert.Throws<InvalidOperationException>(() => Publish(owner, 1, true));
        Assert.False(item.Enabled); Assert.False(item.Visible);
        Publish(owner, 3, true);
        Assert.True(item.Enabled); Assert.True(item.Visible);
    }

    [Fact]
    public void PreinstalledPolicyProjectsBeforeBlockEnterObservers()
    {
        using var h = new QueryPolicyTests.Harness();
        var security = new SecurityManager();
        security.SetBlockSecurity("ROWS", new BlockSecurity { AllowQuery = false });
        security.SetFieldSecurity("ROWS", "Name", new FieldSecurity { Editable = false, Visible = false });
        var events = new TheTechIdea.Beep.Editor.UOWManager.Helpers.EventManager(h.Editor.Object);
        using var manager = new FormsManager(h.Editor.Object, eventManager: events, securityManager: security);
        var entries = 0;
        (bool Query, bool Enabled, bool Visible)? observed = null;
        events.OnBlockEnter += (_, _) =>
        {
            entries++;
            observed = (manager.GetBlock("ROWS").QueryAllowed,
                manager.ItemProperties.GetItem("ROWS", "Name").Enabled,
                manager.ItemProperties.GetItem("ROWS", "Name").Visible);
        };
        manager.RegisterBlock("ROWS", h.Unit.Object, h.Manager.GetBlock("ROWS").EntityStructure, "db");
        Assert.Equal(1, entries);
        Assert.Equal((false, false, false), observed);
        Assert.Empty(manager.PolicyNotificationFailures);
    }

    [Fact]
    public void FailedPublicationActionCannotBeRetriedInsideItsAuthorizationWindow()
    {
        using var helper = new ItemPropertyManager();
        var item = new ItemInfo(); helper.RegisterItem("ROWS", "Name", item);
        var projection = (IItemSecurityProjection)helper;
        projection.PublishSecurityFlags("ROWS", Guid.NewGuid(), 1,
            new[] { new ItemSecurityProjection("Name", item, false, false) }, action => action(), () => true);
        Assert.Throws<InvalidOperationException>(() => projection.PublishSecurityFlags("ROWS", Guid.NewGuid(), 2,
            new[] { new ItemSecurityProjection("Name", item, true, true) }, action =>
            {
                var first = Assert.Throws<InvalidOperationException>(action);
                Assert.Contains("another registration", first.Message);
                var repeated = Assert.Throws<InvalidOperationException>(action);
                Assert.Contains("no longer available", repeated.Message);
            }, () => true));
        Assert.False(item.Enabled); Assert.False(item.Visible);
    }
}
