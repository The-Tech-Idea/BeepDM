using System.Collections.Concurrent;
using TheTechIdea.Beep.SetUp.State;

namespace TheTechIdea.Beep.SetUp.Tests;

/// <summary>
/// Guards for Phase 3 (.plans/setup/PHASE-03-State-Store-And-Concurrency.md): the state store is
/// substitutable, cross-process safe (local), and optimistically concurrent (remote).
/// </summary>
public class StateStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "beep-setup-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        try { if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true); } catch { }
    }

    private LocalJsonSetupStateStore NewLocal() => new(_root);

    private static SetupState SampleState(int completed = 2) => new()
    {
        RunId = "run-1",
        CompletedStepIds = new(Enumerable.Range(0, completed).Select(i => $"step-{i}")),
        SchemaHash = "abc"
    };

    // ── local: round-trip ────────────────────────────────────────────────────

    [Fact]
    public async Task Local_RoundTrips_State()
    {
        var store = NewLocal();
        var key = new SetupStateKey("wiz", "Development");

        Assert.Null(await store.LoadAsync(key));   // nothing yet

        await store.SaveAsync(key, SampleState());
        var loaded = await store.LoadAsync(key);

        Assert.NotNull(loaded);
        Assert.Equal("run-1", loaded!.RunId);
        Assert.Contains("step-0", loaded.CompletedStepIds);
        Assert.Equal(1, loaded.Revision);          // Save increments
    }

    [Fact]
    public async Task Local_Keys_Isolate_ByWizardIdEnvironmentAndApp()
    {
        var store = NewLocal();
        await store.SaveAsync(new SetupStateKey("a", "Development"), SampleState(1));
        await store.SaveAsync(new SetupStateKey("b", "Development"), SampleState(3));
        await store.SaveAsync(new SetupStateKey("a", "Production"), SampleState(5));
        await store.SaveAsync(new SetupStateKey("a", "Development", "app2"), SampleState(7));

        // Two wizards sharing a directory used to overwrite each other; now the key isolates them.
        Assert.Equal(1, (await store.LoadAsync(new SetupStateKey("a", "Development")))!.CompletedStepIds.Count);
        Assert.Equal(3, (await store.LoadAsync(new SetupStateKey("b", "Development")))!.CompletedStepIds.Count);
        Assert.Equal(5, (await store.LoadAsync(new SetupStateKey("a", "Production")))!.CompletedStepIds.Count);
        Assert.Equal(7, (await store.LoadAsync(new SetupStateKey("a", "Development", "app2")))!.CompletedStepIds.Count);
    }

    // ── local: lease ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Local_SecondRunner_IsRefused_WhileLeaseHeld()
    {
        var store = NewLocal();
        var key = new SetupStateKey("wiz");

        var first = await store.TryAcquireLeaseAsync(key, TimeSpan.FromMinutes(5));
        Assert.NotNull(first);

        // A second runner (even a second store instance — cross-process) cannot acquire.
        var second = await new LocalJsonSetupStateStore(_root).TryAcquireLeaseAsync(key, TimeSpan.FromMinutes(5));
        Assert.Null(second);

        await first!.DisposeAsync();

        // Released → reacquirable.
        var third = await store.TryAcquireLeaseAsync(key, TimeSpan.FromMinutes(5));
        Assert.NotNull(third);
        await third!.DisposeAsync();
    }

    [Fact]
    public async Task Local_ExpiredLease_IsReclaimable()
    {
        var store = NewLocal();
        var key = new SetupStateKey("wiz");

        var stale = await store.TryAcquireLeaseAsync(key, TimeSpan.FromMilliseconds(1));
        Assert.NotNull(stale);
        await Task.Delay(30);   // let it expire

        var fresh = await store.TryAcquireLeaseAsync(key, TimeSpan.FromMinutes(5));
        Assert.NotNull(fresh);   // reclaimed the crashed runner's lease
        await fresh!.DisposeAsync();
    }

    [Fact]
    public async Task Local_Save_UnderLostLease_Throws()
    {
        var store = NewLocal();
        var key = new SetupStateKey("wiz");

        var lease = await store.TryAcquireLeaseAsync(key, TimeSpan.FromMilliseconds(1));
        await Task.Delay(30);
        // Someone else reclaims it.
        var other = await new LocalJsonSetupStateStore(_root).TryAcquireLeaseAsync(key, TimeSpan.FromMinutes(5));
        Assert.NotNull(other);

        await Assert.ThrowsAsync<SetupStateConflictException>(
            () => store.SaveAsync(key, SampleState(), lease));

        await other!.DisposeAsync();
    }

    // ── local: explicit-file back-compat ─────────────────────────────────────

    [Fact]
    public async Task Local_ExplicitFile_WritesExactPath()
    {
        var file = Path.Combine(_root, "legacy", "state.json");
        var store = LocalJsonSetupStateStore.ForExplicitFile(file);

        await store.SaveAsync(new SetupStateKey("ignored"), SampleState());

        Assert.True(File.Exists(file));   // literal path honoured, key ignored (legacy StateFilePath)
    }

    // ── remote: optimistic concurrency over a fake ETag transport ────────────

    [Fact]
    public async Task Remote_RoundTrips_State()
    {
        var store = new RemoteSetupStateStore(new InMemoryTransport());
        var key = new SetupStateKey("wiz");

        Assert.Null(await store.LoadAsync(key));
        await store.SaveAsync(key, SampleState());
        Assert.Equal("run-1", (await store.LoadAsync(key))!.RunId);
    }

    [Fact]
    public async Task Remote_StaleSave_Throws_Conflict()
    {
        var transport = new InMemoryTransport();
        var a = new RemoteSetupStateStore(transport);
        var b = new RemoteSetupStateStore(transport);
        var key = new SetupStateKey("wiz");

        await a.SaveAsync(key, SampleState(1));

        // Both load the same version.
        await a.LoadAsync(key);
        await b.LoadAsync(key);

        // A writes → advances the ETag.
        await a.SaveAsync(key, SampleState(2));

        // B's view is now stale; its write must be refused, not silently interleaved.
        await Assert.ThrowsAsync<SetupStateConflictException>(() => b.SaveAsync(key, SampleState(3)));
    }

    [Fact]
    public async Task Remote_SecondRunner_IsRefused_WhileLeaseHeld()
    {
        var transport = new InMemoryTransport();
        var a = new RemoteSetupStateStore(transport);
        var b = new RemoteSetupStateStore(transport);
        var key = new SetupStateKey("wiz");

        var first = await a.TryAcquireLeaseAsync(key, TimeSpan.FromMinutes(5));
        Assert.NotNull(first);

        Assert.Null(await b.TryAcquireLeaseAsync(key, TimeSpan.FromMinutes(5)));

        await first!.DisposeAsync();
        var third = await b.TryAcquireLeaseAsync(key, TimeSpan.FromMinutes(5));
        Assert.NotNull(third);
        await third!.DisposeAsync();
    }

    /// <summary>
    /// Minimal ETag key/value store: compare-and-set on PutAsync, mirroring an HTTP If-Match backend.
    /// </summary>
    private sealed class InMemoryTransport : ISetupStateTransport
    {
        private readonly ConcurrentDictionary<string, (string body, string etag)> _data = new();
        private int _counter;

        public Task<TransportEntry> GetAsync(string resourceId, CancellationToken token = default)
            => Task.FromResult(_data.TryGetValue(resourceId, out var v)
                ? new TransportEntry(v.body, v.etag) : null);

        public Task<string> PutAsync(string resourceId, string body, string ifMatchETag,
            CancellationToken token = default)
        {
            var newEtag = $"etag-{Interlocked.Increment(ref _counter)}";
            var exists = _data.TryGetValue(resourceId, out var cur);

            // null If-Match = create-only; a value = must match current.
            bool ok = ifMatchETag == null ? !exists : (exists && cur.etag == ifMatchETag);
            if (!ok)
                throw new SetupStateConflictException($"ETag precondition failed for '{resourceId}'.");

            _data[resourceId] = (body, newEtag);
            return Task.FromResult(newEtag);
        }

        public Task DeleteAsync(string resourceId, string ifMatchETag, CancellationToken token = default)
        {
            if (_data.TryGetValue(resourceId, out var cur) && (ifMatchETag == null || cur.etag == ifMatchETag))
                _data.TryRemove(resourceId, out _);
            return Task.CompletedTask;
        }
    }
}
