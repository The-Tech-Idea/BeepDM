using System.Collections.Concurrent;
using Moq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.SchemaMigration;
using TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.RdbmsHelpers;
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.Migration.Tests;

/// <summary>
/// Builds a <see cref="MigrationManager"/> wired to fully in-memory fakes, so the planning →
/// policy → dry-run → preflight → execution → rollback pipeline can be exercised with **no live
/// database**. Mirrors the Moq recording-fake pattern used by <c>tests/SetupWizardTests</c>.
///
/// The fake datasource is scriptable: register which entities "exist" and what their current
/// structure is, and it records every DDL call the provider makes.
/// </summary>
public sealed class MigrationTestHarness
{
    private readonly Mock<IDataSource> _ds = new(MockBehavior.Loose);
    private readonly Mock<IDMEEditor> _editor = new(MockBehavior.Loose);
    private readonly Mock<IClassCreator> _classCreator = new(MockBehavior.Loose);
    private readonly Mock<IConfigEditor> _config = new(MockBehavior.Loose);

    /// <summary>In-memory per-datasource migration history — persists checkpoints + named records.</summary>
    public MigrationHistory History { get; } = new() { DataSourceName = "testdb", DataSourceType = DataSourceType.SqlServer };
    public Func<MigrationRecord, PersistenceWriteResult> HistoryWrite { get; set; }
    public IConfigEditor ConfigOverride { get; set; }
    public IMigrationExecutionStorage ExecutionStorage { get; set; }
    public Action? BeforeProviderResolve { get; set; }
    public Action<EntityStructure>? BeforeCreateEntity { get; set; }

    /// <summary>Entity name → desired structure, as the class-creator would produce from a POCO.</summary>
    private readonly Dictionary<Type, EntityStructure> _desiredByType = new();

    /// <summary>Entity name → current DB structure (null/absent = does not exist).</summary>
    private readonly Dictionary<string, EntityStructure> _existing = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Ordered record of every logical op the resolved provider was asked to perform.</summary>
    public List<string> ProviderCalls { get; } = new();
    public List<EntityStructure> ReceivedEntities { get; } = new();
    public List<EntityField> ReceivedColumns { get; } = new();
    public List<(string Name, string[] Columns, Dictionary<string, object> Options)> ReceivedIndexes { get; } = new();
    public List<(string Name, string[] Columns, string ReferencedEntity, string[] ReferencedColumns, string OnDelete)> ReceivedForeignKeys { get; } = new();
    public string TargetName { get; set; } = "testdb";
    public string TargetGuid { get; set; } = "test-target";
    public ConnectionProperties TargetConnectionProperties { get; set; }

    /// <summary>When set, the recording provider fails ops whose name is in this set.</summary>
    public HashSet<string> FailOps { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Number of times the fake class-creator was asked to convert a type (Phase 7 cache probe).</summary>
    public int ConversionCount { get; private set; }

    /// <summary>
    /// Every SQL string passed to <see cref="IDataSource.ExecuteSql"/> directly — the path
    /// operations bypassing <see cref="ISchemaMigrationProvider"/> use (e.g.
    /// <c>MigrationManager.WidenPrimaryKeyColumn</c>, implemented straight against
    /// <see cref="RdbmsHelper"/> because that capability has no home on the provider interface
    /// — see that method's own remarks).
    /// </summary>
    public List<string> ExecutedSql { get; } = new();

    /// <summary>When any statement executed via ExecuteSql contains this substring, that call fails.</summary>
    public HashSet<string> FailSqlContaining { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Row(s) <see cref="IDataSource.RunQuery"/> returns for any query — used for the live primary-key-name lookup.</summary>
    public List<object[]> RunQueryRows { get; set; } = new();

    public MigrationTestHarness()
    {
        ExecutionStorage = new MemoryMigrationExecutionStorage(this);
        _ds.SetupGet(d => d.DatasourceType).Returns(DataSourceType.SqlServer);
        _ds.SetupGet(d => d.Category).Returns(DatasourceCategory.RDBMS);
        _ds.SetupGet(d => d.DatasourceName).Returns(() => TargetName);
        _ds.SetupGet(d => d.GuidID).Returns(() => TargetGuid);
        var connection = new Mock<IDataConnection>();
        connection.SetupGet(c => c.ConnectionProp).Returns(() => TargetConnectionProperties);
        _ds.SetupGet(d => d.Dataconnection).Returns(connection.Object);
        _ds.SetupGet(d => d.ConnectionStatus).Returns(System.Data.ConnectionState.Open);
        _ds.SetupGet(d => d.ErrorObject).Returns(new ErrorsInfo { Flag = Errors.Ok });
        _ds.SetupGet(d => d.DMEEditor).Returns(() => _editor.Object);

        _ds.Setup(d => d.CheckEntityExist(It.IsAny<string>()))
           .Returns<string>(name => _existing.ContainsKey(name));
        _ds.Setup(d => d.GetEntityStructure(It.IsAny<string>(), It.IsAny<bool>()))
           .Returns<string, bool>((name, _) => _existing.TryGetValue(name, out var s) ? s : null);
        _ds.Setup(d => d.CreateEntityAs(It.IsAny<EntityStructure>()))
           .Returns<EntityStructure>(e =>
           {
               ReceivedEntities.Add(ClonePayload(e));
               var operation = $"CreateEntityAs:{e?.EntityName}";
               ProviderCalls.Add(operation);
               BeforeCreateEntity?.Invoke(e);
               return !FailOps.Contains("CreateEntityAs") && !FailOps.Contains(operation);
           });
        _ds.Setup(d => d.ExecuteSql(It.IsAny<string>()))
           .Returns<string>(sql =>
           {
               ExecutedSql.Add(sql);
               return FailSqlContaining.Any(needle => sql?.Contains(needle, StringComparison.OrdinalIgnoreCase) == true)
                   ? new ErrorsInfo { Flag = Errors.Failed, Message = $"Execution failed (scripted): {sql}" }
                   : new ErrorsInfo { Flag = Errors.Ok };
           });
        _ds.Setup(d => d.RunQuery(It.IsAny<string>()))
           .Returns<string>(_ => RunQueryRows.Cast<object>());

        // Real RdbmsHelper, not a fake — WidenPrimaryKeyColumn is deliberately implemented
        // straight against it (see that method's own remarks), so a test exercising it needs
        // the actual SQL-generation logic, not a stand-in.
        _editor.Setup(e => e.GetDataSourceHelper(It.IsAny<DataSourceType>()))
               .Returns<DataSourceType>(t => new RdbmsHelper(_editor.Object) { SupportedType = t });

        _classCreator.Setup(c => c.ConvertToEntityStructure(It.IsAny<Type>(),
                It.IsAny<KeyDetectionStrategy>(), It.IsAny<string>()))
            .Returns<Type, KeyDetectionStrategy, string>((t, _, _) =>
            {
                ConversionCount++;
                return _desiredByType.TryGetValue(t, out var s) ? s : null;
            });

        _editor.SetupGet(e => e.classCreator).Returns(() => _classCreator.Object);
        _editor.SetupGet(e => e.ErrorObject).Returns(new ErrorsInfo { Flag = Errors.Ok });
        _editor.Setup(e => e.GetMigrationProvider(It.IsAny<IDataSource>()))
               .Returns(() => { BeforeProviderResolve?.Invoke(); return new RecordingProvider(this); });

        // In-memory migration-history store so checkpoint persistence / idempotency round-trip.
        _config.Setup(c => c.LoadMigrationHistory(It.IsAny<string>())).Returns(() => History);
        _config.Setup(c => c.AppendMigrationRecord(It.IsAny<string>(), It.IsAny<DataSourceType>(), It.IsAny<MigrationRecord>()))
               .Callback<string, DataSourceType, MigrationRecord>((_, _, record) => History.Migrations.Add(record));
        _config.As<IMigrationHistoryPersistence>()
            .Setup(c => c.AppendMigrationRecordAcknowledged(It.IsAny<string>(), It.IsAny<DataSourceType>(), It.IsAny<MigrationRecord>(), It.IsAny<CancellationToken>()))
            .Returns<string, DataSourceType, MigrationRecord, CancellationToken>((_, _, record, token) =>
            {
                token.ThrowIfCancellationRequested();
                var outcome = HistoryWrite?.Invoke(record) ?? new PersistenceWriteResult(PersistenceWriteStatus.Saved);
                if (outcome.IsSaved) History.Migrations.Add(record);
                return outcome;
            });
        _editor.SetupGet(e => e.ConfigEditor).Returns(() => ConfigOverride ?? _config.Object);
        _config.As<IMigrationExecutionStorageProvider>().Setup(c => c.CaptureMigrationExecutionStorage())
            .Returns(() => ExecutionStorage);
    }

    public IDataSource DataSource => _ds.Object;
    public IDMEEditor Editor => _editor.Object;

    /// <summary>Registers the desired structure a POCO type maps to (via the fake class-creator).</summary>
    public MigrationTestHarness WithDesired(Type type, EntityStructure structure)
    {
        _desiredByType[type] = structure;
        return this;
    }

    /// <summary>Marks an entity as already present in the DB with the given current structure.</summary>
    public MigrationTestHarness WithExisting(EntityStructure current)
    {
        _existing[current.EntityName] = current;
        return this;
    }

    public MigrationManager Build() => new(Editor, DataSource) { ExecutionTargetIdentity = "test-target/sqlserver" };

    private static T ClonePayload<T>(T value) => Newtonsoft.Json.JsonConvert.DeserializeObject<T>(
        Newtonsoft.Json.JsonConvert.SerializeObject(value));

    /// <summary>Convenience: an EntityStructure with the given name and simple string fields.</summary>
    public static EntityStructure Entity(string name, params string[] fieldNames)
    {
        return new EntityStructure
        {
            EntityName = name,
            DatasourceEntityName = name,
            Fields = fieldNames.Select(f => new EntityField
            {
                FieldName = f,
                Fieldtype = "System.String",
                AllowDBNull = true
            }).ToList()
        };
    }

    /// <summary>
    /// A provider that records the logical ops asked of it and reports success (or failure for ops
    /// in <see cref="FailOps"/>). Declares full capabilities so nothing is refused for capability.
    /// </summary>
    private sealed class RecordingProvider : ISchemaMigrationProvider
    {
        private readonly MigrationTestHarness _h;
        public RecordingProvider(MigrationTestHarness h) => _h = h;

        public DataSourceType DataSourceType => DataSourceType.SqlServer;
        public DatasourceCategory Category => DatasourceCategory.RDBMS;
        public SchemaMigrationCapabilities Capabilities { get; } = new()
        {
            SupportsCreateEntity = true, SupportsAddColumn = true, SupportsAlterColumn = true,
            SupportsDropColumn = true, SupportsRenameColumn = true, SupportsRenameEntity = true,
            SupportsDropEntity = true, SupportsTruncateEntity = true, SupportsCreateIndex = true,
            SupportsDropIndex = true, SupportsAddForeignKey = true, SupportsDropForeignKey = true,
            SupportsTransactionalDdl = true
        };

        private IErrorsInfo Record(string op, string detail)
        {
            _h.ProviderCalls.Add($"{op}:{detail}");
            return _h.FailOps.Contains(op) || _h.FailOps.Contains($"{op}:{detail}")
                ? new ErrorsInfo { Flag = Errors.Failed, Message = $"{op} failed (scripted)." }
                : new ErrorsInfo { Flag = Errors.Ok, Message = $"{op} ok." };
        }

        public IErrorsInfo CreateEntity(EntityStructure entity)
        {
            _h.ReceivedEntities.Add(ClonePayload(entity));
            return Record("CreateEntity", entity?.EntityName);
        }
        public IErrorsInfo DropEntity(string entityName) => Record("DropEntity", entityName);
        public IErrorsInfo TruncateEntity(string entityName) => Record("TruncateEntity", entityName);
        public IErrorsInfo RenameEntity(string oldName, string newName) => Record("RenameEntity", $"{oldName}->{newName}");
        public IErrorsInfo AddColumn(string entityName, EntityField column)
        {
            _h.ReceivedColumns.Add(ClonePayload(column));
            return Record("AddColumn", $"{entityName}.{column?.FieldName}");
        }
        public IErrorsInfo AlterColumn(string entityName, string columnName, EntityField newColumn) => Record("AlterColumn", $"{entityName}.{columnName}");
        public IErrorsInfo DropColumn(string entityName, string columnName) => Record("DropColumn", $"{entityName}.{columnName}");
        public IErrorsInfo RenameColumn(string entityName, string oldColumnName, string newColumnName) => Record("RenameColumn", $"{entityName}.{oldColumnName}->{newColumnName}");
        public IErrorsInfo CreateIndex(string entityName, string indexName, string[] columns, Dictionary<string, object> options = null)
        {
            _h.ReceivedIndexes.Add((indexName, columns.ToArray(), ClonePayload(options)));
            return Record("CreateIndex", $"{entityName}.{indexName}");
        }
        public IErrorsInfo DropIndex(string entityName, string indexName) => Record("DropIndex", $"{entityName}.{indexName}");
        public IErrorsInfo AddForeignKey(string entityName, string[] columnNames, string referencedEntityName, string[] referencedColumnNames, string onDeleteBehavior, string onUpdateBehavior, string constraintName)
        {
            _h.ReceivedForeignKeys.Add((constraintName, columnNames.ToArray(), referencedEntityName, referencedColumnNames.ToArray(), onDeleteBehavior));
            return Record("AddForeignKey", constraintName ?? entityName);
        }
        public IErrorsInfo DropForeignKey(string entityName, string constraintName) => Record("DropForeignKey", $"{entityName}.{constraintName}");
    }
}
