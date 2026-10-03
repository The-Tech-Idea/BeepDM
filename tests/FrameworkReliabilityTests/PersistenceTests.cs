using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using JsonSerializationException = Newtonsoft.Json.JsonSerializationException;
using TheTechIdea.Beep.Editor.Importing;
using TheTechIdea.Beep.Editor.Importing.ErrorStore;
using TheTechIdea.Beep.Editor.Importing.History;
using TheTechIdea.Beep.Editor.Importing.Sync;
using TheTechIdea.Beep.JsonLoaderService;
using TheTechIdea.Beep.Services.Persistence;
using Xunit;
using JsonException = System.Text.Json.JsonException;

namespace FrameworkReliabilityTests;

public sealed partial class PersistenceTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "BeepDM-AtomicTests", Guid.NewGuid().ToString("N"));

    public PersistenceTests() => Directory.CreateDirectory(_folder);

    [Fact]
    public void JsonLoader_SerializationFailure_PreservesPreviousFile_AndThrows()
    {
        var path = Path.Combine(_folder, "config.json");
        var loader = new JsonLoader();
        loader.Serialize(path, new { Name = "previous" });
        var before = File.ReadAllBytes(path);

        Assert.Throws<JsonSerializationException>(() => loader.Serialize(path, new ThrowingValue()));

        Assert.Equal(before, File.ReadAllBytes(path));
        Assert.Empty(Directory.GetFiles(_folder, "*.tmp"));
    }

    [Fact]
    public async Task AtomicUpdate_CallbackFailure_PreservesPreviousFile()
    {
        var path = Path.Combine(_folder, "value.json");
        await AtomicFileStore.WriteTextAsync(path, "previous");
        await Assert.ThrowsAsync<InvalidOperationException>(() => AtomicFileStore.UpdateTextAsync(path,
            _ => throw new InvalidOperationException("serialization failed")));
        Assert.Equal("previous", await AtomicFileStore.ReadTextAsync(path));
    }

    [Fact]
    public async Task AtomicWrite_EncodingFailure_PreservesPreviousFile()
    {
        var path = Path.Combine(_folder, "value.json");
        await AtomicFileStore.WriteTextAsync(path, "previous");
        await Assert.ThrowsAsync<System.Text.EncoderFallbackException>(() => AtomicFileStore.WriteTextAsync(path, "\uD800"));
        Assert.Equal("previous", await AtomicFileStore.ReadTextAsync(path));
        Assert.Empty(Directory.GetFiles(_folder, "*.tmp"));
    }

    [Fact]
    public async Task AtomicWrite_ReplacementFailure_CleansTemporaryFile_AndIsObservable()
    {
        var path = Path.Combine(_folder, "directory.json");
        Directory.CreateDirectory(path);
        var error = await Record.ExceptionAsync(() => AtomicFileStore.WriteTextAsync(path, "replacement"));
        Assert.True(error is IOException or UnauthorizedAccessException, error?.ToString());
        Assert.True(Directory.Exists(path));
        Assert.Empty(Directory.GetFiles(_folder, "*.tmp"));
    }

    [Fact]
    public async Task AtomicWrite_CancellationBeforeCommit_PreservesPreviousFile()
    {
        var path = Path.Combine(_folder, "value.json");
        await AtomicFileStore.WriteTextAsync(path, "previous");
        using var cancelled = new CancellationTokenSource();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => AtomicFileStore.UpdateTextAsync(path, _ =>
        {
            cancelled.Cancel();
            return "replacement";
        }, cancelled.Token));
        Assert.Equal("previous", await AtomicFileStore.ReadTextAsync(path));
        Assert.Empty(Directory.GetFiles(_folder, "*.tmp"));
    }

    [Fact]
    public async Task AtomicLease_Wait_IsCancellable_AndNeverDeletesLeaseFile()
    {
        var path = Path.Combine(_folder, "value.json");
        await AtomicFileStore.WriteTextAsync(path, "previous");
        using var lease = new FileStream(path + ".beep.lock", FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        using var cancel = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => AtomicFileStore.WriteTextAsync(path, "replacement", cancel.Token));
        Assert.True(File.Exists(path + ".beep.lock"));
        Assert.Equal("previous", await AtomicFileStore.ReadTextAsync(path));
    }

    [Fact]
    public async Task AtomicUpdate_ConcurrentWriters_DoNotLoseUpdates()
    {
        var path = Path.Combine(_folder, "counter");
        await Task.WhenAll(Enumerable.Range(0, 50).Select(_ => AtomicFileStore.UpdateTextAsync(path,
            current => (int.Parse(current ?? "0", CultureInfo.InvariantCulture) + 1).ToString(CultureInfo.InvariantCulture))));
        Assert.Equal("50", await AtomicFileStore.ReadTextAsync(path));
    }

    [Fact]
    public async Task AtomicSnapshots_ReadersNeverSeePartialJson()
    {
        var path = Path.Combine(_folder, "snapshot.json");
        await AtomicFileStore.WriteTextAsync(path, JsonSerializer.Serialize(new { Index = 0, Payload = new string('x', 32768) }));
        var writer = Task.Run(async () =>
        {
            for (int i = 1; i <= 30; i++)
                await AtomicFileStore.WriteTextAsync(path, JsonSerializer.Serialize(new { Index = i, Payload = new string('x', 32768) }));
        });
        int reads = 0;
        try
        {
            do
            {
                using var json = JsonDocument.Parse((await AtomicFileStore.ReadTextAsync(path))!);
                Assert.Equal(32768, json.RootElement.GetProperty("Payload").GetString()!.Length);
                reads++;
            } while (!writer.IsCompleted);
        }
        finally { await writer; }
        Assert.True(reads > 0);
    }

    [Fact]
    public async Task AtomicUpdate_SeparateProcesses_DoNotLoseUpdates()
    {
        var path = Path.Combine(_folder, "process-counter");
        using var first = StartWorker("increment", path, "25");
        using var second = StartWorker("increment", path, "25");
        using var third = StartWorker("increment", path, "25");
        await Task.WhenAll(CompleteWorker(first), CompleteWorker(second), CompleteWorker(third));
        Assert.Equal("75", await AtomicFileStore.ReadTextAsync(path));
    }

    [Fact]
    public async Task AtomicLease_KilledOwner_ReleasesLeaseWithoutChangingPreviousFile()
    {
        var path = Path.Combine(_folder, "process-held");
        await AtomicFileStore.WriteTextAsync(path, "previous");
        using var child = StartWorker("hold", path);
        try
        {
            Assert.Equal("lease-acquired", await child.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(30)));
            using var cancel = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => AtomicFileStore.WriteTextAsync(path, "wrong", cancel.Token));
        }
        finally
        {
            if (!child.HasExited) child.Kill(entireProcessTree: true);
            await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(30));
        }
        Assert.Equal("previous", await AtomicFileStore.ReadTextAsync(path));
        await AtomicFileStore.WriteTextAsync(path, "after-restart");
        Assert.Equal("after-restart", await AtomicFileStore.ReadTextAsync(path));
    }

    public static IEnumerable<object[]> ScalarCursors()
    {
        yield return new object[] { "original" };
        yield return new object[] { true };
        yield return new object[] { (byte)255 };
        yield return new object[] { (sbyte)-127 };
        yield return new object[] { (short)-30000 };
        yield return new object[] { (ushort)60000 };
        yield return new object[] { -123456 };
        yield return new object[] { uint.MaxValue };
        yield return new object[] { long.MaxValue };
        yield return new object[] { ulong.MaxValue };
        yield return new object[] { 1234567890.0123456789m };
        yield return new object[] { 1.234567f };
        yield return new object[] { 1.234567891234567d };
        yield return new object[] { new DateTime(2026, 10, 2, 12, 30, 0, DateTimeKind.Utc) };
        yield return new object[] { new DateTime(2026, 10, 2, 12, 30, 0, DateTimeKind.Local) };
        yield return new object[] { new DateTime(2026, 10, 2, 12, 30, 0, DateTimeKind.Unspecified) };
        yield return new object[] { new DateTimeOffset(2026, 10, 2, 12, 30, 0, TimeSpan.FromHours(3)) };
        yield return new object[] { Guid.Parse("1c1b9cdc-75c9-47a3-ab61-273f3fdb29ee") };
    }

    [Theory]
    [MemberData(nameof(ScalarCursors))]
    public async Task Watermark_Restart_PreservesTypeAndValueAcrossCultures(object cursor)
    {
        var prior = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            await new FileWatermarkStore(_folder).SaveWatermarkAsync("source/schema", cursor);
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            var loaded = await new FileWatermarkStore(_folder).LoadWatermarkAsync("SOURCE/SCHEMA");
            Assert.NotNull(loaded);
            Assert.Equal(cursor.GetType(), loaded.GetType());
            Assert.Equal(cursor, loaded);
            if (cursor is DateTime timestamp) Assert.Equal(timestamp.Kind, ((DateTime)loaded).Kind);
            if (cursor is DateTimeOffset offset) Assert.Equal(offset.Offset, ((DateTimeOffset)loaded).Offset);
        }
        finally { CultureInfo.CurrentCulture = prior; }
    }

    [Fact]
    public async Task Watermark_CompositeCursor_RetainsNestedTypes()
    {
        var date = DateTime.UtcNow;
        await new FileWatermarkStore(_folder).SaveWatermarkAsync("composite", new Dictionary<string, object>
        {
            ["time"] = date, ["sequence"] = 42L, ["nested"] = new object[] { 5m, "id", null! }
        });
        var loaded = Assert.IsType<Dictionary<string, object>>(await new FileWatermarkStore(_folder).LoadWatermarkAsync("composite"));
        Assert.Equal(date, Assert.IsType<DateTime>(loaded["time"]));
        Assert.Equal(42L, Assert.IsType<long>(loaded["sequence"]));
        var nested = Assert.IsType<object[]>(loaded["nested"]);
        Assert.Equal(5m, Assert.IsType<decimal>(nested[0]));
        Assert.Equal("id", nested[1]);
        Assert.Null(nested[2]);
    }

    [Fact]
    public async Task Watermark_UnsupportedType_DoesNotReplacePreviousValue()
    {
        var store = new FileWatermarkStore(_folder);
        await store.SaveWatermarkAsync("key", 42L);
        await Assert.ThrowsAsync<NotSupportedException>(() => store.SaveWatermarkAsync("key", new { Value = 43 }));
        Assert.Equal(42L, await store.LoadWatermarkAsync("key"));
    }

    [Fact]
    public async Task Watermark_NullAndBytes_RoundTrip()
    {
        var store = new FileWatermarkStore(_folder);
        await store.SaveWatermarkAsync("null", null!);
        Assert.Null(await new FileWatermarkStore(_folder).LoadWatermarkAsync("null"));
        var bytes = new byte[] { 0, 255, 17 };
        await store.SaveWatermarkAsync("bytes", bytes);
        Assert.Equal(bytes, Assert.IsType<byte[]>(await new FileWatermarkStore(_folder).LoadWatermarkAsync("bytes")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Watermark_CancelledOperations_NeverChangeState(bool durable)
    {
        IWatermarkStore store = durable ? new FileWatermarkStore(_folder) : new InMemoryWatermarkStore();
        await store.SaveWatermarkAsync("key", 42L);
        using var cancel = new CancellationTokenSource();
        cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.SaveWatermarkAsync("key", 43L, cancel.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.LoadWatermarkAsync("key", cancel.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.ClearWatermarkAsync("key", cancel.Token));
        Assert.Equal(42L, await store.LoadWatermarkAsync("key"));
        await store.SaveWatermarkAsync("key", null!);
        Assert.Null(await store.LoadWatermarkAsync("key"));
    }

    [Fact]
    public async Task Watermark_CyclicComposite_IsRejectedWithoutTouchingPreviousValue()
    {
        var store = new FileWatermarkStore(_folder);
        await store.SaveWatermarkAsync("key", 42L);
        var cycle = new Dictionary<string, object>();
        cycle["self"] = cycle;
        await Assert.ThrowsAsync<NotSupportedException>(() => store.SaveWatermarkAsync("key", cycle));
        Assert.Equal(42L, await store.LoadWatermarkAsync("key"));
    }

    [Fact]
    public async Task JsonLines_LegacyStores_AreExplicitlyRejectedWithoutDeletingOriginals()
    {
        var errors = Path.Combine(_folder, "legacy.errors.jsonl");
        var history = Path.Combine(_folder, "legacy.history.jsonl");
        await File.WriteAllTextAsync(errors, "original-errors");
        await File.WriteAllTextAsync(history, "original-history");
        await Assert.ThrowsAsync<InvalidDataException>(() => new JsonFileImportErrorStore(_folder).LoadAsync("legacy"));
        await Assert.ThrowsAsync<InvalidDataException>(() => new JsonFileImportRunHistoryStore(_folder).GetRunsAsync("legacy"));
        Assert.Equal("original-errors", await File.ReadAllTextAsync(errors));
        Assert.Equal("original-history", await File.ReadAllTextAsync(history));
    }

    [Fact]
    public async Task Watermark_SanitizationCollisions_DoNotAliasContexts()
    {
        var store = new FileWatermarkStore(_folder);
        await store.SaveWatermarkAsync("source/schema", 1L);
        await store.SaveWatermarkAsync("source_schema", 2L);
        Assert.Equal(1L, await store.LoadWatermarkAsync("source/schema"));
        Assert.Equal(2L, await store.LoadWatermarkAsync("source_schema"));
        Assert.Equal(2, Directory.GetFiles(_folder, "*.watermark.json").Length);
    }

    [Fact]
    public async Task Watermark_LegacyFile_IsRejectedWithoutGuessingOrModifying()
    {
        var path = Path.Combine(_folder, "legacy.watermark.json");
        await File.WriteAllTextAsync(path, "{\"Value\":\"42\"}");
        var store = new FileWatermarkStore(_folder);
        await Assert.ThrowsAsync<InvalidDataException>(() => store.LoadWatermarkAsync("legacy"));
        await Assert.ThrowsAsync<InvalidDataException>(() => store.SaveWatermarkAsync("legacy", 43L));
        Assert.Equal("{\"Value\":\"42\"}", await File.ReadAllTextAsync(path));
    }

    [Theory]
    [InlineData("\"Version\":2", "\"Version\":1")]
    [InlineData("\"ContextKey\":\"other\"", "\"ContextKey\":\"key\"")]
    [InlineData("\"Kind\":\"runtime-type\"", "\"Kind\":\"i64\"")]
    public async Task Watermark_InvalidEnvelope_IsRejected_AndNeverOverwritten(string replacement, string original)
    {
        var store = new FileWatermarkStore(_folder);
        await store.SaveWatermarkAsync("key", 42L);
        var path = Assert.Single(Directory.GetFiles(_folder, "*.watermark.json"));
        var invalid = (await File.ReadAllTextAsync(path)).Replace(original, replacement, StringComparison.Ordinal);
        await File.WriteAllTextAsync(path, invalid);
        await Assert.ThrowsAsync<InvalidDataException>(() => store.LoadWatermarkAsync("key"));
        await Assert.ThrowsAsync<InvalidDataException>(() => store.SaveWatermarkAsync("key", 43L));
        Assert.Equal(invalid, await File.ReadAllTextAsync(path));
    }

    [Fact]
    public async Task ErrorStore_MultipleInstances_AppendAndReplay_DoNotLoseRecords()
    {
        await Task.WhenAll(Enumerable.Range(0, 40).Select(index => new JsonFileImportErrorStore(_folder)
            .SaveAsync(new ImportErrorRecord { ContextKey = "errors", BatchNumber = 1, RecordIndex = index })));
        await Task.WhenAll(Enumerable.Range(0, 40).Select(index => new JsonFileImportErrorStore(_folder).MarkReplayedAsync("errors", 1, index)));
        var records = await new JsonFileImportErrorStore(_folder).LoadAsync("errors");
        Assert.Equal(40, records.Count);
        Assert.All(records, record => Assert.True(record.Replayed));
        Assert.Empty(await new JsonFileImportErrorStore(_folder).LoadPendingAsync("errors"));
    }

    [Fact]
    public async Task ErrorStore_ProcessesAppendingDuringReplay_DoNotLoseRecords()
    {
        var store = new JsonFileImportErrorStore(_folder);
        for (int i = 0; i < 15; i++)
            await store.SaveAsync(new ImportErrorRecord { ContextKey = "process-context", BatchNumber = 1, RecordIndex = i });
        using var first = StartWorker("errors", _folder, "20", "0");
        using var second = StartWorker("errors", _folder, "20", "20");
        await Task.WhenAll(Enumerable.Range(0, 15).Select(index => store.MarkReplayedAsync("process-context", 1, index)));
        await Task.WhenAll(CompleteWorker(first), CompleteWorker(second));
        var records = await new JsonFileImportErrorStore(_folder).LoadAsync("process-context");
        Assert.Equal(55, records.Count);
        Assert.Equal(15, records.Count(record => record.Replayed));
        Assert.Equal(40, records.Count(record => record.BatchNumber == 2));
    }

    [Fact]
    public async Task ErrorStore_CorruptOrForeignRecords_AreNotOverwritten()
    {
        var store = new JsonFileImportErrorStore(_folder);
        var record = new ImportErrorRecord { ContextKey = "errors", RecordIndex = 1 };
        await store.SaveAsync(record);
        var path = Assert.Single(Directory.GetFiles(_folder, "*.errors.jsonl"));
        await File.WriteAllTextAsync(path, "{not-json\n");
        await Assert.ThrowsAsync<JsonException>(() => store.SaveAsync(record));
        Assert.Equal("{not-json\n", await File.ReadAllTextAsync(path));
        var foreign = JsonSerializer.Serialize(new ImportErrorRecord { ContextKey = "foreign" });
        await File.WriteAllTextAsync(path, foreign);
        await Assert.ThrowsAsync<InvalidDataException>(() => store.MarkReplayedAsync("errors", 1, 1));
        Assert.Equal(foreign, await File.ReadAllTextAsync(path));
    }

    [Fact]
    public async Task History_MultipleInstances_RestartAndSort_PreserveRecords()
    {
        var start = DateTime.UtcNow;
        await Task.WhenAll(Enumerable.Range(0, 30).Select(index => new JsonFileImportRunHistoryStore(_folder)
            .SaveRunAsync(new ImportRunRecord
            {
                ContextKey = "history", RunId = index.ToString(CultureInfo.InvariantCulture),
                StartedAt = start.AddSeconds(index), FinalState = index % 2 == 0 ? ImportState.Completed : ImportState.Faulted
            })));
        var restarted = new JsonFileImportRunHistoryStore(_folder);
        Assert.Equal(30, (await restarted.GetRunsAsync("history")).Count);
        Assert.Equal("29", (await restarted.GetRunsAsync("history"))[0].RunId);
        Assert.Equal("28", (await restarted.GetLastSuccessfulRunAsync("history"))!.RunId);
        await restarted.ClearAsync("history");
        Assert.Empty(await restarted.GetRunsAsync("history"));
    }

    private static Process StartWorker(params string[] arguments)
    {
        var worker = Path.Combine(AppContext.BaseDirectory, "persistence-worker", "FrameworkPersistenceWorker.dll");
        Assert.True(File.Exists(worker), $"Persistence worker was not built/copied: {worker}");
        var start = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true
        };
        start.ArgumentList.Add(worker);
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        return Process.Start(start)!;
    }

    private static async Task CompleteWorker(Process process)
    {
        try
        {
            var errors = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(45));
            Assert.True(process.ExitCode == 0, await errors);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }
        }
    }

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    public sealed class ThrowingValue
    {
        public string Value => throw new InvalidOperationException("cannot serialize");
    }
}
