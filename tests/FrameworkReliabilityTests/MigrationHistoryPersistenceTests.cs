using Moq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.ConfigUtil.Managers;
using TheTechIdea.Beep.JsonLoaderService;
using TheTechIdea.Beep.Utilities;
using Xunit;

namespace FrameworkReliabilityTests;

public sealed partial class PersistenceTests
{
    private MigrationHistoryManager MigrationStore(IJsonLoader? loader = null) =>
        new(null!, loader ?? new JsonLoader(), new ConfigandSettings { ConfigPath = _folder }, null!);

    private static MigrationRecord RecordFor(string id) => new() { MigrationId = id, Name = "test", Notes = id };

    [Fact]
    public void MigrationHistory_AcknowledgedFacade_RoundTrips()
    {
        using var config = new ConfigEditor(new Mock<TheTechIdea.Beep.Logger.IDMLogger>().Object,
            new ErrorsInfo(), new JsonLoader(), _folder);
        var store = Assert.IsAssignableFrom<IMigrationHistoryPersistence>(config);
        Assert.True(store.AppendMigrationRecordAcknowledged("target", DataSourceType.SqlServer, RecordFor("first")).IsSaved);
        var loaded = config.LoadMigrationHistory("target");
        Assert.Equal(1, loaded.StorageFormatVersion);
        Assert.Equal("first", Assert.Single(loaded.Migrations).Notes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("{broken")]
    [InlineData("{\"DataSourceName\":\"other\",\"Migrations\":[]}")]
    [InlineData("{\"DataSourceName\":\"target\",\"Migrations\":null}")]
    [InlineData("{\"DataSourceName\":\"target\",\"Migrations\":[null]}")]
    [InlineData("{\"DataSourceName\":\"target\",\"Migrations\":[],\"StorageFormatVersion\":99}")]
    [InlineData("{\"DataSourceName\":\"target\",\"Migrations\":[]} {}")]
    [InlineData("{\"DataSourceName\":\"other\",\"DataSourceName\":\"target\",\"Migrations\":[]}")]
    public void MigrationHistory_Corruption_IsNotFreshState_AndCannotBeOverwritten(string corrupt)
    {
        var store = MigrationStore();
        store.AppendRecord("target", DataSourceType.SqlServer, RecordFor("previous"));
        var path = Assert.Single(Directory.GetFiles(Path.Combine(_folder, "Migrations"), "*.json"));
        File.WriteAllText(path, corrupt);
        Assert.ThrowsAny<Exception>(() => store.Load("target"));
        Assert.False(store.AppendAcknowledged("target", DataSourceType.SqlServer, RecordFor("new")).IsSaved);
        Assert.False(store.SaveAcknowledged(new MigrationHistory { DataSourceName = "target" }).IsSaved);
        Assert.ThrowsAny<Exception>(() => store.AppendRecord("target", DataSourceType.SqlServer, RecordFor("legacy")));
        Assert.Equal(corrupt, File.ReadAllText(path));
    }

    [Fact]
    public async Task MigrationHistory_ConcurrentInstances_RetainEveryAppend()
    {
        await Task.WhenAll(Enumerable.Range(0, 40).Select(index => Task.Run(() =>
            MigrationStore().AppendRecord("target", DataSourceType.SqlServer, RecordFor(index.ToString())))));
        var records = MigrationStore().Load("TARGET").Migrations;
        Assert.Equal(40, records.Count);
        Assert.Equal(40, records.Select(record => record.MigrationId).Distinct().Count());
    }

    [Fact]
    public async Task MigrationHistory_SeparateProcesses_RetainEveryAppend()
    {
        using var first = StartWorker("migration-history", _folder, "20", "0");
        using var second = StartWorker("migration-history", _folder, "20", "20");
        await Task.WhenAll(CompleteWorker(first), CompleteWorker(second));
        var records = MigrationStore().Load("process-db").Migrations;
        Assert.Equal(40, records.Count);
        Assert.Equal(40, records.Select(record => record.MigrationId).Distinct().Count());
    }

    [Fact]
    public void MigrationHistory_LegacyPromotion_PreservesOriginalBytes()
    {
        var directory = Path.Combine(_folder, "Migrations");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "target_migrations.json");
        var original = "{\"DataSourceName\":\"target\",\"DataSourceType\":\"SqlServer\",\"Migrations\":[{\"MigrationId\":\"old\"}]}";
        File.WriteAllText(path, original);
        Assert.Equal("old", Assert.Single(MigrationStore().Load("target").Migrations).MigrationId);
        MigrationStore().AppendRecord("target", DataSourceType.SqlServer, RecordFor("new"));
        Assert.Equal(original, File.ReadAllText(path));
        Assert.Equal(2, MigrationStore().Load("target").Migrations.Count);
        Assert.Equal(2, Directory.GetFiles(directory, "*.json").Length);
    }

    [Fact]
    public void MigrationHistory_SanitizedNames_DoNotShareStorage()
    {
        MigrationStore().AppendRecord("a:b", DataSourceType.SqlServer, RecordFor("colon"));
        MigrationStore().AppendRecord("a?b", DataSourceType.SqlServer, RecordFor("question"));
        Assert.Equal("colon", Assert.Single(MigrationStore().Load("a:b").Migrations).MigrationId);
        Assert.Equal("question", Assert.Single(MigrationStore().Load("a?b").Migrations).MigrationId);
    }

    [Fact]
    public void MigrationHistory_AmbiguousLegacyIdentity_IsRejectedAndPreserved()
    {
        var directory = Path.Combine(_folder, "Migrations");
        Directory.CreateDirectory(directory);
        var name = "a?b";
        var safe = string.Concat(name.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch));
        var path = Path.Combine(directory, safe + "_migrations.json");
        var json = "{\"DataSourceName\":\"different-target\",\"Migrations\":[]}";
        File.WriteAllText(path, json);
        Assert.False(MigrationStore().AppendAcknowledged(name, DataSourceType.SqlServer, RecordFor("new")).IsSaved);
        Assert.Equal(json, File.ReadAllText(path));
        Assert.Single(Directory.GetFiles(directory, "*.json"));
    }

    [Fact]
    public void MigrationHistory_TypeMismatch_IsRejected()
    {
        MigrationStore().AppendRecord("target", DataSourceType.SqlServer, RecordFor("sql"));
        var result = MigrationStore().AppendAcknowledged("target", DataSourceType.Postgre, RecordFor("other"));
        Assert.False(result.IsSaved);
        Assert.Single(MigrationStore().Load("target").Migrations);
    }

    [Fact]
    public void MigrationHistory_UnsupportedLoader_DoesNotUseVoidFallback()
    {
        var loader = new Mock<IJsonLoader>(MockBehavior.Strict);
        var result = MigrationStore(loader.Object).AppendAcknowledged("target", DataSourceType.SqlServer, RecordFor("new"));
        Assert.Equal(PersistenceWriteStatus.Unsupported, result.Status);
        loader.VerifyNoOtherCalls();
        Assert.False(Directory.Exists(Path.Combine(_folder, "Migrations")));
    }

    [Fact]
    public void MigrationHistory_SerializerFailure_PreservesPreviousBytes()
    {
        MigrationStore().AppendRecord("target", DataSourceType.SqlServer, RecordFor("previous"));
        var path = Assert.Single(Directory.GetFiles(Path.Combine(_folder, "Migrations"), "*.json"));
        var before = File.ReadAllBytes(path);
        var loader = new Mock<IJsonLoader>();
        loader.As<IJsonSnapshotCodec>().Setup(c => c.SerializeSnapshot(It.IsAny<object>()))
            .Throws(new InvalidOperationException("serializer fault"));
        Assert.Equal(PersistenceWriteStatus.Failed,
            MigrationStore(loader.Object).AppendAcknowledged("target", DataSourceType.SqlServer, RecordFor("new")).Status);
        Assert.Equal(before, File.ReadAllBytes(path));
    }

    [Fact]
    public void MigrationHistory_Cancellation_IsObservableWithoutWriting()
    {
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        Assert.Equal(PersistenceWriteStatus.Cancelled, MigrationStore()
            .AppendAcknowledged("target", DataSourceType.SqlServer, RecordFor("new"), cancelled.Token).Status);
        Assert.False(Directory.Exists(Path.Combine(_folder, "Migrations")));
    }

    [Fact]
    public void MigrationHistory_CancelledLeaseWait_PreservesHistory()
    {
        MigrationStore().AppendRecord("target", DataSourceType.SqlServer, RecordFor("previous"));
        var path = Assert.Single(Directory.GetFiles(Path.Combine(_folder, "Migrations"), "*.json"));
        var before = File.ReadAllBytes(path);
        using var lease = new FileStream(path + ".beep.lock", FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        using var cancel = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));
        var outcome = MigrationStore().AppendAcknowledged("target", DataSourceType.SqlServer, RecordFor("new"), cancel.Token);
        Assert.Equal(PersistenceWriteStatus.Cancelled, outcome.Status);
        Assert.Equal(before, File.ReadAllBytes(path));
    }

    [Fact]
    public void MigrationHistory_ReplacementDenial_IsObservableAndPreservesPreviousFile()
    {
        if (!OperatingSystem.IsWindows()) return; // Windows sharing-denial fixture; not Unix release evidence.
        MigrationStore().AppendRecord("target", DataSourceType.SqlServer, RecordFor("previous"));
        var path = Assert.Single(Directory.GetFiles(Path.Combine(_folder, "Migrations"), "*.json"));
        var before = File.ReadAllBytes(path);
        using var reader = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var outcome = MigrationStore().AppendAcknowledged("target", DataSourceType.SqlServer, RecordFor("new"));
        Assert.Equal(PersistenceWriteStatus.Failed, outcome.Status);
        Assert.Equal(before, File.ReadAllBytes(path));
        Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(path)!, "*.tmp"));
    }

    [Fact]
    public void MigrationHistory_SerializationInsideLease_PreservesPreviousFile()
    {
        MigrationStore().AppendRecord("target", DataSourceType.SqlServer, RecordFor("previous"));
        var path = Assert.Single(Directory.GetFiles(Path.Combine(_folder, "Migrations"), "*.json"));
        var before = File.ReadAllBytes(path);
        var json = new JsonLoader();
        var loader = new Mock<IJsonLoader>();
        var codec = loader.As<IJsonSnapshotCodec>();
        codec.Setup(c => c.SerializeSnapshot(It.IsAny<object>())).Returns<object>(value => value is MigrationHistory
            ? throw new InvalidOperationException("history serialization fault") : json.SerializeSnapshot(value));
        codec.Setup(c => c.DeserializeSnapshot<MigrationRecord>(It.IsAny<string>())).Returns<string>(json.DeserializeSnapshot<MigrationRecord>);
        codec.Setup(c => c.DeserializeSnapshot<MigrationHistory>(It.IsAny<string>())).Returns<string>(json.DeserializeSnapshot<MigrationHistory>);
        Assert.Equal(PersistenceWriteStatus.Failed,
            MigrationStore(loader.Object).AppendAcknowledged("target", DataSourceType.SqlServer, RecordFor("new")).Status);
        Assert.Equal(before, File.ReadAllBytes(path));
    }

    [Fact]
    public void MigrationHistory_UnknownSaveVersion_CannotBeSilentlyRelabelled()
    {
        Assert.Equal(PersistenceWriteStatus.Unsupported,
            MigrationStore().SaveAcknowledged(new MigrationHistory { DataSourceName = "target", StorageFormatVersion = 99 }).Status);
        Assert.False(Directory.Exists(Path.Combine(_folder, "Migrations")));
    }

    [Fact]
    public async Task AtomicSnapshots_MixedCatalogCalls_ProgressWithBoundedThreadPool()
    {
        // Isolate scheduler limits in the child; never mutate the test host's thread pool.
        using var child = StartWorker("bounded-pool-catalog", _folder);
        await CompleteWorker(child);
    }
}
