using System.Diagnostics;
using Moq;
using Newtonsoft.Json.Linq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.BeepSync;
using TheTechIdea.Beep.Editor.BeepSync.Helpers;
using TheTechIdea.Beep.Editor.Importing;
using Xunit;

namespace FrameworkReliabilityTests;

public sealed class SyncPersistenceTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "BeepDM-SyncPersistence", Guid.NewGuid().ToString("N"));
    private SchemaPersistenceHelper Store() => new(Mock.Of<IDMEEditor>(), _folder);
    private static DataSyncSchema Schema(string id = "schema", object? cursor = null) => new()
    {
        Id = id, SourceDataSourceName = "source", DestinationDataSourceName = "destination",
        SourceEntityName = "rows", DestinationEntityName = "rows",
        WatermarkPolicy = new WatermarkPolicy { LastWatermarkValue = cursor }
    };
    private static SyncCheckpoint Checkpoint(string id = "schema", object? key = null) => new()
    {
        SchemaId = id, RunId = "run-one", ProcessedOffset = 10, LastProcessedKeyValue = key,
        SavedAt = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };
    private static SyncSchemaVersion Version(string id = "schema", int number = 1) => new()
    {
        SchemaId = id, Version = number, VersionGuid = "version-" + number, SchemaHash = new string('a', 64)
    };

    private static SyncCheckpoint FailureCheckpoint() => new()
    {
        SchemaId = "schema", RunId = "failed-run", SchemaFingerprint = new string('a', 64), Status = "Failed",
        RequiresReconciliation = true, ProcessedOffset = 1, TotalExpected = 2,
        FailureEvidence = new SyncFailureEvidence
        {
            Kind = SyncRunFailureKind.QualityThreshold, RecordsAttempted = 2, RecordsAcknowledged = 1,
            RecordsFailed = 1, WriteAttempts = 1, RecordsQualityEvaluated = 2, RecordsQualityRejected = 1,
            RecordsBlocked = 1,
            Threshold = new SyncBatchThresholdResult(SyncBatchThresholdOutcome.Rejected, QualityFailureMode.Required, 2, 1, 0.05)
        }
    };
    public SyncPersistenceTests() => Directory.CreateDirectory(_folder);
    public static IEnumerable<object?[]> Cursors => new object?[]
    {
        null, "opaque", true, (byte)1, (sbyte)-1, (short)-3, (ushort)3, 5, 6U, long.MaxValue, ulong.MaxValue,
        1.234567890123456789M, 1.25F, 2.5D,
        new DateTime(638950000000000000, DateTimeKind.Utc),
        new DateTime(638950000000000000, DateTimeKind.Unspecified),
        new DateTime(638950000000000000, DateTimeKind.Local),
        new DateTimeOffset(2026, 10, 2, 10, 1, 2, TimeSpan.FromHours(3)),
        Guid.Parse("436cd8ce-aa18-45e0-8eb9-bdfaa62b4178"), new byte[] { 0, 1, 255 },
        new object[] { 1, "two", Guid.Empty, new DateTime(12345, DateTimeKind.Utc) },
        new Dictionary<string, object> { ["sequence"] = 42L, ["tie"] = new object[] { 1M, true } }
    }.Select(value => new object?[] { value });

    private static void ExactCursor(object? expected, object? actual)
    {
        if (expected == null) { Assert.Null(actual); return; }
        Assert.NotNull(actual); Assert.Equal(expected.GetType(), actual.GetType());
        if (expected is Dictionary<string, object> fields)
        {
            var loaded = Assert.IsType<Dictionary<string, object>>(actual);
            Assert.Equal(fields.Count, loaded.Count);
            foreach (var field in fields) ExactCursor(field.Value, loaded[field.Key]);
        }
        else if (expected is object[] items)
        {
            var loaded = Assert.IsType<object[]>(actual);
            Assert.Equal(items.Length, loaded.Length);
            for (int i = 0; i < items.Length; i++) ExactCursor(items[i], loaded[i]);
        }
        else
        {
            Assert.Equal(expected, actual);
            if (expected is DateTime time) Assert.Equal(time.Kind, ((DateTime)actual).Kind);
            if (expected is DateTimeOffset offset) Assert.Equal(offset.Offset, ((DateTimeOffset)actual).Offset);
        }
    }

    [Theory]
    [MemberData(nameof(Cursors))]
    public async Task SchemaAndCheckpoint_RestartPreservesExactClosedCursorTypes(object? cursor)
    {
        var store = Store();
        var schema = Schema(cursor: cursor); var checkpoint = Checkpoint(key: cursor);
        await store.SaveSchemaAsync(schema); await store.SaveCheckpointAsync(checkpoint);
        var restarted = Store();
        ExactCursor(cursor, Assert.Single(await restarted.LoadSchemasAsync()).WatermarkPolicy.LastWatermarkValue);
        ExactCursor(cursor, (await restarted.LoadCheckpointAsync("schema")).LastProcessedKeyValue);
        Assert.Equal(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc), checkpoint.SavedAt);
        Assert.Equal(1, JObject.Parse(File.ReadAllText(store.GetSchemasFilePath()))["FormatVersion"]!.Value<int>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("{broken")]
    [InlineData("{}")]
    [InlineData("{\"FormatVersion\":2,\"Schemas\":[]}")]
    [InlineData("{\"FormatVersion\":1,\"FormatVersion\":1,\"Schemas\":[]}")]
    [InlineData("[null]")]
    [InlineData("[{}]")]
    [InlineData("[{\"Id\":42}]")]
    [InlineData("[{\"Id\":\"one\",\"id\":\"two\"}]")]
    [InlineData("{\"FormatVersion\":1,\"Schemas\":[{}]}")]
    [InlineData("{\"FormatVersion\":1,\"Schemas\":[{\"Id\":true}]}")]
    [InlineData("{\"FormatVersion\":1,\"Schemas\":[{\"Id\":\"one\",\"ID\":\"one\"}]}")]
    [InlineData("[{\"Id\":\"same\"},{\"Id\":\"same\"}]")]
    [InlineData("[] []")]
    public async Task CorruptSchemas_AreNotFreshStateAndCannotBeOverwrittenOrDeleted(string invalid)
    {
        var store = Store(); File.WriteAllText(store.GetSchemasFilePath(), invalid);
        Assert.NotNull(await Record.ExceptionAsync(() => store.LoadSchemasAsync()));
        Assert.Equal(PersistenceWriteStatus.Failed, (await store.SaveSchemaAcknowledgedAsync(Schema())).Status);
        Assert.NotNull(await Record.ExceptionAsync(() => store.SaveSchemasAsync(Array.Empty<DataSyncSchema>())));
        Assert.NotNull(await Record.ExceptionAsync(() => store.DeleteSchemaAsync("schema")));
        Assert.False(await store.CreateBackupAsync());
        Assert.Equal(invalid, File.ReadAllText(store.GetSchemasFilePath()));
    }

    [Fact]
    public async Task EligibleLegacyMetadata_IsUpgraded_ButUntypedCursorIsPreservedAndRejected()
    {
        var store = Store();
        File.WriteAllText(store.GetSchemasFilePath(), "[{\"Id\":\"legacy\"}]");
        Assert.Single(await store.LoadSchemasAsync());
        await store.SaveSchemaAsync(Schema("second"));
        Assert.Equal(2, (await store.LoadSchemasAsync()).Count);
        var untyped = "[{\"Id\":\"legacy\",\"WatermarkPolicy\":{\"LastWatermarkValue\":\"2026-10-02T00:00:00Z\"}}]";
        File.WriteAllText(store.GetSchemasFilePath(), untyped);
        Assert.NotNull(await Record.ExceptionAsync(() => store.LoadSchemasAsync()));
        Assert.False((await store.SaveSchemaAcknowledgedAsync(Schema())).IsSaved);
        Assert.Equal(untyped, File.ReadAllText(store.GetSchemasFilePath()));
    }

    [Theory]
    [InlineData("Id")]
    [InlineData("id")]
    [InlineData("ID")]
    public async Task UnambiguousLegacyIdentityCasing_PreservesExactIdentityOnUpgrade(string property)
    {
        var store = Store();
        File.WriteAllText(store.GetSchemasFilePath(), "[{\"" + property + "\":\"Case Sensitive ID\"}]");
        Assert.Equal("Case Sensitive ID", Assert.Single(await store.LoadSchemasAsync()).Id);
        await store.SaveSchemaAsync(Schema("second"));
        Assert.Contains(await store.LoadSchemasAsync(), schema => schema.Id == "Case Sensitive ID");
        Assert.Equal("Case Sensitive ID", JObject.Parse(File.ReadAllText(store.GetSchemasFilePath()))["Schemas"]![0]!["Id"]!.Value<string>());
    }

    [Fact]
    public async Task ConcurrentInstancesAndProcesses_RetainEveryIndividualSchemaUpdate()
    {
        using var first = StartWorker("sync-save", "0"); using var second = StartWorker("sync-save", "10");
        var local = Enumerable.Range(0, 20).Select(i => Store().SaveSchemaAsync(Schema("parent-" + i)));
        await Task.WhenAll(local);
        await Complete(first); await Complete(second);
        Assert.Equal(40, (await Store().LoadSchemasAsync()).Count);
    }

    [Fact]
    public async Task DeleteAndUpsert_CoordinateWholeUpdate_WhileWholeSaveRemainsExplicitReplacement()
    {
        var store = Store(); await store.SaveSchemasAsync(new[] { Schema("remove"), Schema("kept") });
        await Task.WhenAll(Store().DeleteSchemaAsync("remove"), Store().SaveSchemaAsync(Schema("added")));
        Assert.Equal(new[] { "added", "kept" }, (await store.LoadSchemasAsync()).Select(item => item.Id).OrderBy(id => id));
        await store.SaveSchemasAsync(new[] { Schema("replacement") });
        Assert.Equal("replacement", Assert.Single(await store.LoadSchemasAsync()).Id);
        await store.DeleteSchemaAsync("missing");
        Assert.Single(await store.LoadSchemasAsync());
    }

    [Theory]
    [InlineData("format")]
    [InlineData("kind")]
    [InlineData("schema")]
    [InlineData("snapshot-schema")]
    [InlineData("offset")]
    [InlineData("cursor")]
    public async Task InvalidCheckpoint_RejectsLoadSaveAndClearWithoutDeletingEvidence(string fault)
    {
        var store = Store(); await store.SaveCheckpointAsync(Checkpoint());
        var path = store.GetCheckpointFilePath("schema"); var document = JObject.Parse(File.ReadAllText(path));
        if (fault == "format") document["FormatVersion"] = 2;
        if (fault == "kind") document["ArtifactKind"] = "foreign";
        if (fault == "schema") document["SchemaId"] = "foreign";
        if (fault == "snapshot-schema") document["Snapshot"]!["SchemaId"] = "foreign";
        if (fault == "offset") document["Snapshot"]!["ProcessedOffset"] = -1;
        if (fault == "cursor") document["Snapshot"]!["LastProcessedKeyValue"] = new JObject { ["Kind"] = "System.Type", ["Value"] = "foreign" };
        var invalid = document.ToString(); File.WriteAllText(path, invalid);
        Assert.NotNull(await Record.ExceptionAsync(() => store.LoadCheckpointAsync("schema")));
        Assert.False((await store.SaveCheckpointAcknowledgedAsync(Checkpoint())).IsSaved);
        Assert.NotNull(await Record.ExceptionAsync(() => store.ClearCheckpointAsync("schema")));
        Assert.Equal(invalid, File.ReadAllText(path));
    }

    [Theory]
    [InlineData("legacy")]
    [InlineData("version-legacy")]
    public async Task LegacyArtifacts_ArePreservedRatherThanSilentlyInterpreted(string artifact)
    {
        var store = Store();
        var path = artifact == "legacy" ? Path.Combine(_folder, "checkpoints", "schema.json") : Path.Combine(_folder, "versions", "schema", "v0001.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllText(path, "legacy evidence");
        if (artifact == "legacy")
        {
            Assert.NotNull(await Record.ExceptionAsync(() => store.LoadCheckpointAsync("schema")));
            Assert.False((await store.SaveCheckpointAcknowledgedAsync(Checkpoint())).IsSaved);
            Assert.NotNull(await Record.ExceptionAsync(() => store.ClearCheckpointAsync("schema")));
        }
        else
        {
            Assert.NotNull(await Record.ExceptionAsync(() => store.LoadSchemaVersionsAsync("schema")));
            Assert.NotNull(await Record.ExceptionAsync(() => store.SaveVersionedSchemaAsync(Schema(), Version())));
        }
        Assert.Equal("legacy evidence", File.ReadAllText(path));
    }

    [Fact]
    public async Task CheckpointConflictAndRegression_PreserveTheSavedRun()
    {
        var store = Store(); await store.SaveCheckpointAsync(Checkpoint());
        var saved = File.ReadAllBytes(store.GetCheckpointFilePath("schema"));
        var conflict = Checkpoint(); conflict.RunId = "different-run";
        Assert.False((await store.SaveCheckpointAcknowledgedAsync(conflict)).IsSaved);
        var regression = Checkpoint(); regression.ProcessedOffset = 9;
        Assert.False((await store.SaveCheckpointAcknowledgedAsync(regression)).IsSaved);
        Assert.Equal(saved, File.ReadAllBytes(store.GetCheckpointFilePath("schema")));
        var completed = Checkpoint(); completed.Status = "Completed"; await store.SaveCheckpointAsync(completed);
        await store.SaveCheckpointAsync(conflict);
        Assert.Equal("different-run", (await store.LoadCheckpointAsync("schema")).RunId);
    }

    [Fact]
    public async Task HashPaths_AvoidTraversalCaseAndSanitizedNameCollisions()
    {
        var store = Store();
        var ids = new[] { "../escape", "a/b", "a\\b", "a:b", "Schema", "schema" };
        foreach (var id in ids)
        {
            Assert.StartsWith(Path.GetFullPath(_folder) + Path.DirectorySeparatorChar, store.GetCheckpointFilePath(id));
            await store.SaveCheckpointAsync(Checkpoint(id));
            Assert.Equal(id, (await store.LoadCheckpointAsync(id)).SchemaId);
        }
        Assert.Equal(ids.Length, ids.Select(store.GetCheckpointFilePath).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public async Task Versions_AreImmutableOrderedAndDoNotSkipCorruption()
    {
        var store = Store(); var one = Version();
        await store.SaveVersionedSchemaAsync(Schema(), one); await store.SaveVersionedSchemaAsync(Schema(), one);
        var path = store.GetVersionFilePath("schema", 1); var saved = File.ReadAllBytes(path);
        one.ChangeNotes = "different content";
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.SaveVersionedSchemaAsync(Schema(), one));
        Assert.Equal(saved, File.ReadAllBytes(path));
        await store.SaveVersionedSchemaAsync(Schema(), Version(number: 10000));
        Assert.Equal(new[] { 10000, 1 }, (await store.LoadSchemaVersionsAsync("schema")).Select(item => item.Version));
        File.WriteAllText(path, "{broken");
        Assert.NotNull(await Record.ExceptionAsync(() => store.LoadSchemaVersionsAsync("schema")));
        Assert.NotNull(await Record.ExceptionAsync(() => store.DiffSchemaToPersistedAsync(Schema())));
        Assert.Equal("{broken", File.ReadAllText(path));
    }

    [Fact]
    public async Task UnsupportedCursorAndCancellation_PreserveBytes_AndLoggerFailureCannotConcealOutcome()
    {
        var editor = new Mock<IDMEEditor>();
        editor.Setup(e => e.AddLogMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Errors>())).Throws(new IOException("logger"));
        var store = new SchemaPersistenceHelper(editor.Object, _folder);
        await store.SaveSchemaAsync(Schema()); var saved = File.ReadAllBytes(store.GetSchemasFilePath());
        Assert.False((await store.SaveSchemaAcknowledgedAsync(Schema(cursor: new object()))).IsSaved);
        Assert.Equal(saved, File.ReadAllBytes(store.GetSchemasFilePath()));
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        Assert.Equal(PersistenceWriteStatus.Cancelled, (await store.SaveSchemaAcknowledgedAsync(Schema("new"), cancelled.Token)).Status);
        using var lease = new FileStream(store.GetSchemasFilePath() + ".beep.lock", FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));
        Assert.Equal(PersistenceWriteStatus.Cancelled, (await store.SaveSchemaAcknowledgedAsync(Schema("new"), timeout.Token)).Status);
        Assert.Equal(saved, File.ReadAllBytes(store.GetSchemasFilePath()));
    }

    [Fact]
    public async Task Backups_AreUniqueValidatedSnapshots_AndMissingStateIsFresh()
    {
        var store = Store(); Assert.Empty(await store.LoadSchemasAsync()); Assert.Null(await store.LoadCheckpointAsync("missing"));
        Assert.Empty(await store.LoadSchemaVersionsAsync("missing")); await store.ClearCheckpointAsync("missing");
        await store.SaveSchemaAsync(Schema(cursor: 42L));
        Assert.True(await store.CreateBackupAsync()); Assert.True(await store.CreateBackupAsync());
        var backups = Directory.GetFiles(_folder, "SyncSchemas_Backup_*.json"); Assert.Equal(2, backups.Length);
        Assert.All(backups, path => Assert.Equal(File.ReadAllText(store.GetSchemasFilePath()), File.ReadAllText(path)));
    }

    [Fact]
    public async Task SeparateProcess_ReloadsTypedSchemaAndCheckpoint()
    {
        var store = Store(); await store.SaveSchemaAsync(Schema(cursor: new DateTimeOffset(2026, 10, 2, 10, 1, 2, TimeSpan.FromHours(3))));
        await store.SaveCheckpointAsync(Checkpoint(key: 42L));
        using var process = StartWorker("sync-reload", "0"); await Complete(process);
    }

    [Fact]
    public async Task FailureEvidence_SeparateProcessReloadsClosedCountsAndThreshold()
    {
        var store = Store(); var checkpoint = FailureCheckpoint();
        await store.SaveCheckpointAsync(checkpoint);
        checkpoint.FailureEvidence.RecordsAcknowledged = 99;
        var reloaded = await Store().LoadCheckpointAsync("schema");
        Assert.Equal(1, reloaded.FailureEvidence.RecordsAcknowledged);
        Assert.Equal(0.5, reloaded.FailureEvidence.Threshold.RejectRate);
        using var process = StartWorker("sync-failure-reload", "0"); await Complete(process);
    }

    [Theory]
    [InlineData("missing-count")]
    [InlineData("missing-kind")]
    [InlineData("missing-uncertainty")]
    [InlineData("negative-count")]
    [InlineData("ack-mismatch")]
    [InlineData("missing-threshold-action")]
    [InlineData("malformed-threshold-limit")]
    [InlineData("foreign-version")]
    [InlineData("aliased-count")]
    public async Task FailureEvidence_CorruptionIsPreservedAndCannotBeOverwrittenOrCleared(string defect)
    {
        var store = Store(); await store.SaveCheckpointAsync(FailureCheckpoint());
        var path = store.GetCheckpointFilePath("schema"); var root = JObject.Parse(File.ReadAllText(path));
        var evidence = (JObject)root["Snapshot"]!["FailureEvidence"]!;
        switch (defect)
        {
            case "missing-count": evidence.Remove("RecordsAcknowledged"); break;
            case "missing-kind": evidence.Remove("Kind"); break;
            case "missing-uncertainty": evidence.Remove("HasUncertainWrites"); break;
            case "negative-count": evidence["RecordsQualityRejected"] = -1; break;
            case "ack-mismatch": evidence["RecordsAcknowledged"] = 0; break;
            case "missing-threshold-action": ((JObject)evidence["Threshold"]!).Remove("Outcome"); break;
            case "malformed-threshold-limit": evidence["Threshold"]!["MaxRejectRate"] = "secret-limit"; break;
            case "foreign-version": evidence["FormatVersion"] = 2; break;
            case "aliased-count": evidence["recordsFailed"] = 0; break;
        }
        var bytes = System.Text.Encoding.UTF8.GetBytes(root.ToString()); File.WriteAllBytes(path, bytes);
        await Assert.ThrowsAnyAsync<Exception>(() => store.LoadCheckpointAsync("schema"));
        Assert.False((await store.SaveCheckpointAcknowledgedAsync(FailureCheckpoint())).IsSaved);
        await Assert.ThrowsAnyAsync<Exception>(() => store.ClearCheckpointForRunAsync("schema", "failed-run"));
        Assert.Equal(bytes, File.ReadAllBytes(path));
    }

    [Theory]
    [InlineData("same-run")]
    [InlineData("new-run")]
    [InlineData("altered-count")]
    public async Task FailureEvidence_TerminalRunCannotBeReopenedOrRewritten(string change)
    {
        var store = Store(); await store.SaveCheckpointAsync(FailureCheckpoint());
        var bytes = File.ReadAllBytes(store.GetCheckpointFilePath("schema"));
        await store.SaveCheckpointAsync(FailureCheckpoint());
        Assert.Equal(bytes, File.ReadAllBytes(store.GetCheckpointFilePath("schema")));
        var candidate = FailureCheckpoint();
        if (change == "new-run") candidate.RunId = "new-run";
        if (change == "same-run") { candidate.Status = "Running"; candidate.FailureEvidence = null; }
        if (change == "altered-count") candidate.FailureEvidence.RecordsWarned = 1;
        Assert.False((await store.SaveCheckpointAcknowledgedAsync(candidate)).IsSaved);
        Assert.Equal(bytes, File.ReadAllBytes(store.GetCheckpointFilePath("schema")));
        await store.ClearCheckpointForRunAsync("schema", "failed-run");
        Assert.Null(await store.LoadCheckpointAsync("schema"));
    }

    [Theory]
    [InlineData("RunId")]
    [InlineData("ProcessedOffset")]
    [InlineData("AttemptCount")]
    [InlineData("SavedAt")]
    [InlineData("Status")]
    public async Task RequiredCheckpointFields_CannotBeInventedByConstructorDefaults(string field)
    {
        var store = Store(); await store.SaveCheckpointAsync(Checkpoint());
        var path = store.GetCheckpointFilePath("schema"); var value = JObject.Parse(File.ReadAllText(path));
        ((JObject)value["Snapshot"]!).Remove(field); var invalid = value.ToString(); File.WriteAllText(path, invalid);
        await Assert.ThrowsAsync<InvalidDataException>(() => store.LoadCheckpointAsync("schema"));
        Assert.False((await store.SaveCheckpointAcknowledgedAsync(Checkpoint())).IsSaved);
        Assert.Equal(invalid, File.ReadAllText(path));
    }

    [Fact]
    public async Task CompletedRun_IsImmutable_AndOwnedClearCannotDeleteAnotherRun()
    {
        var store = Store(); var completed = Checkpoint(); completed.Status = "Completed";
        await store.SaveCheckpointAsync(completed); var path = store.GetCheckpointFilePath("schema"); var saved = File.ReadAllBytes(path);
        await store.SaveCheckpointAsync(completed); Assert.Equal(saved, File.ReadAllBytes(path));
        Assert.False((await store.SaveCheckpointAcknowledgedAsync(Checkpoint())).IsSaved);
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.ClearCheckpointForRunAsync("schema", "different"));
        Assert.Equal(saved, File.ReadAllBytes(path)); await store.ClearCheckpointForRunAsync("schema", "run-one");
        Assert.Null(await store.LoadCheckpointAsync("schema")); Assert.True(File.Exists(path + ".beep.lock"));
    }

    [Fact]
    public async Task WindowsReplacementDenial_ReportsFailureAndKeepsSnapshot()
    {
        if (!OperatingSystem.IsWindows()) return;
        var store = Store(); await store.SaveSchemaAsync(Schema()); var saved = File.ReadAllBytes(store.GetSchemasFilePath());
        using (var held = new FileStream(store.GetSchemasFilePath(), FileMode.Open, FileAccess.Read, FileShare.Read))
            Assert.Equal(PersistenceWriteStatus.Failed, (await store.SaveSchemaAcknowledgedAsync(Schema("new"))).Status);
        Assert.Equal(saved, File.ReadAllBytes(store.GetSchemasFilePath())); Assert.Empty(Directory.GetFiles(_folder, "*.tmp"));
    }

    private Process StartWorker(string mode, string offset)
    {
        var worker = Path.Combine(AppContext.BaseDirectory, "persistence-worker", "FrameworkPersistenceWorker.dll");
        Assert.True(File.Exists(worker));
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true };
        foreach (var arg in new[] { worker, mode, _folder, offset }) start.ArgumentList.Add(arg);
        return Process.Start(start)!;
    }
    private static async Task Complete(Process process)
    {
        try { var errors = process.StandardError.ReadToEndAsync(); await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(45)); Assert.True(process.ExitCode == 0, await errors); }
        finally { if (!process.HasExited) { process.Kill(entireProcessTree: true); await process.WaitForExitAsync(); } }
    }
    public void Dispose() => Directory.Delete(_folder, recursive: true);
}
