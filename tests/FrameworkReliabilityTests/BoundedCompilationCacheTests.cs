using System.Reflection;
using System.Runtime.ExceptionServices;
using TheTechIdea.Beep.Roslyn;
using Xunit;

namespace TheTechIdea.Beep.Framework.Tests;

public class BoundedCompilationCacheTests
{
    // Exercise the actual Engine helper without adding a production friend-assembly dependency.
    private sealed class Cache
    {
        private readonly Type _type = typeof(RoslynCompiler).Assembly
            .GetType("TheTechIdea.Beep.Roslyn.BoundedCompilationCache`2")!.MakeGenericType(typeof(string), typeof(object));
        private readonly object _instance;
        public Cache(int limit) => _instance = Activator.CreateInstance(_type, limit)!;
        public int Count => (int)_type.GetProperty("Count")!.GetValue(_instance)!;
        public object Get(string key, Func<object> factory) => Call("GetOrAdd", key, factory)!;
        public bool Remove(Func<string, bool> predicate) => (bool)Call("RemoveWhere", predicate)!;
        public void Clear() => Call("Clear");
        private object? Call(string name, params object[] arguments)
        {
            try { return _type.GetMethod(name)!.Invoke(_instance, arguments); }
            catch (TargetInvocationException error) when (error.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(error.InnerException).Throw();
                throw;
            }
        }
    }

    [Fact]
    public void CompletedEntriesEvictByBoundWithoutDiscardingOtherOwners()
    {
        var cache = new Cache(2);
        var first = cache.Get("first", () => new object());
        var second = cache.Get("second", () => new object());
        var third = cache.Get("third", () => new object());
        Assert.Equal(2, cache.Count);
        Assert.Same(second, cache.Get("second", () => throw new Exception("must not compile")));
        Assert.Same(third, cache.Get("third", () => throw new Exception("must not compile")));
        Assert.NotSame(first, cache.Get("first", () => new object()));
        Assert.Equal(2, cache.Count);
    }

    [Fact]
    public void FailedFactoryDoesNotPoisonTheIdentity()
    {
        var cache = new Cache(2);
        var failure = new InvalidOperationException("original failure");
        Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => cache.Get("key", () => throw failure)));
        Assert.Equal(0, cache.Count);
        var result = cache.Get("key", () => new object());
        Assert.Same(result, cache.Get("key", () => throw new Exception("must not compile")));
    }

    [Fact]
    public void RecursiveSameKeyFailsInsteadOfDeadlockingAndCanRetry()
    {
        var cache = new Cache(2);
        Assert.Throws<InvalidOperationException>(() => cache.Get("key", () => cache.Get("key", () => new object())));
        Assert.Equal(0, cache.Count);
        Assert.NotNull(cache.Get("key", () => new object()));
    }

    [Fact]
    public async Task FactoryRunsOutsideLockAndDifferentKeyMakesProgress()
    {
        var cache = new Cache(2);
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var blocked = Task.Factory.StartNew(() => cache.Get("blocked", () =>
        {
            entered.Set();
            Assert.True(release.Wait(TimeSpan.FromSeconds(20)));
            return new object();
        }), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(10)));
            var independent = Task.Run(() => cache.Get("other", () => new object()));
            Assert.NotNull(await independent.WaitAsync(TimeSpan.FromSeconds(10)));
        }
        finally { release.Set(); }
        await blocked.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task SameKeyIsSingleFlightWhileUnrelatedCompletedEntriesAreTrimmed()
    {
        var cache = new Cache(1);
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        int calls = 0;
        var value = new object();
        var blocked = Task.Factory.StartNew(() => cache.Get("blocked", () =>
        {
            Interlocked.Increment(ref calls);
            entered.Set();
            Assert.True(release.Wait(TimeSpan.FromSeconds(20)));
            return value;
        }), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        Task<object>? joined = null;
        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(10)));
            cache.Get("other", () => new object());
            cache.Get("latest", () => new object());
            Assert.Equal(2, cache.Count); // one in-flight plus one completed, not eviction of active work
            joined = Task.Run(() => cache.Get("blocked", () =>
            {
                Interlocked.Increment(ref calls);
                return value;
            }));
        }
        finally { release.Set(); }
        Assert.Same(value, await blocked.WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.Same(value, await joined!.WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.Equal(1, calls);
        Assert.Equal(1, cache.Count);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task InvalidationDuringGenerationDoesNotRepublishStaleEntry(bool clearAll)
    {
        var cache = new Cache(2);
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var old = new object();
        var current = new object();
        var blocked = Task.Factory.StartNew(() => cache.Get("key", () =>
        {
            entered.Set();
            Assert.True(release.Wait(TimeSpan.FromSeconds(20)));
            return old;
        }), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(10)));
            if (clearAll) cache.Clear(); else Assert.True(cache.Remove(key => key == "key"));
            Assert.Same(current, cache.Get("key", () => current));
        }
        finally { release.Set(); }
        Assert.Same(old, await blocked.WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.Same(current, cache.Get("key", () => throw new Exception("must not compile")));
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    public void RemovalDropsOnlyMatchingCompletedEntriesAndPreservesBound()
    {
        var cache = new Cache(2);
        var first = cache.Get("first", () => new object());
        cache.Get("second", () => new object());
        Assert.True(cache.Remove(key => key == "second"));
        Assert.False(cache.Remove(key => key == "second"));
        cache.Get("third", () => new object());
        Assert.Same(first, cache.Get("first", () => throw new Exception("must not compile")));
        Assert.Equal(2, cache.Count);
        cache.Clear();
        Assert.Equal(0, cache.Count);
    }
}
