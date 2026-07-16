using System.Collections.Concurrent;
using Moq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.SchemaMigration;
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

    /// <summary>Entity name → desired structure, as the class-creator would produce from a POCO.</summary>
    private readonly Dictionary<Type, EntityStructure> _desiredByType = new();

    /// <summary>Entity name → current DB structure (null/absent = does not exist).</summary>
    private readonly Dictionary<string, EntityStructure> _existing = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Ordered record of every logical op the resolved provider was asked to perform.</summary>
    public List<string> ProviderCalls { get; } = new();

    /// <summary>When set, the recording provider fails ops whose name is in this set.</summary>
    public HashSet<string> FailOps { get; } = new(StringComparer.OrdinalIgnoreCase);

    public MigrationTestHarness()
    {
        _ds.SetupGet(d => d.DatasourceType).Returns(DataSourceType.SqlServer);
        _ds.SetupGet(d => d.Category).Returns(DatasourceCategory.RDBMS);
        _ds.SetupGet(d => d.DatasourceName).Returns("testdb");
        _ds.SetupGet(d => d.ConnectionStatus).Returns(System.Data.ConnectionState.Open);
        _ds.SetupGet(d => d.ErrorObject).Returns(new ErrorsInfo { Flag = Errors.Ok });
        _ds.SetupGet(d => d.DMEEditor).Returns(() => _editor.Object);

        _ds.Setup(d => d.CheckEntityExist(It.IsAny<string>()))
           .Returns<string>(name => _existing.ContainsKey(name));
        _ds.Setup(d => d.GetEntityStructure(It.IsAny<string>(), It.IsAny<bool>()))
           .Returns<string, bool>((name, _) => _existing.TryGetValue(name, out var s) ? s : null);
        _ds.Setup(d => d.CreateEntityAs(It.IsAny<EntityStructure>()))
           .Returns<EntityStructure>(e => { ProviderCalls.Add($"CreateEntityAs:{e?.EntityName}"); return true; });
        _ds.Setup(d => d.ExecuteSql(It.IsAny<string>()))
           .Returns<string>(_ => new ErrorsInfo { Flag = Errors.Ok });

        _classCreator.Setup(c => c.ConvertToEntityStructure(It.IsAny<Type>(),
                It.IsAny<KeyDetectionStrategy>(), It.IsAny<string>()))
            .Returns<Type, KeyDetectionStrategy, string>((t, _, _) =>
                _desiredByType.TryGetValue(t, out var s) ? s : null);

        _editor.SetupGet(e => e.classCreator).Returns(() => _classCreator.Object);
        _editor.SetupGet(e => e.ErrorObject).Returns(new ErrorsInfo { Flag = Errors.Ok });
        _editor.Setup(e => e.GetMigrationProvider(It.IsAny<IDataSource>()))
               .Returns(() => new RecordingProvider(this));

        // In-memory migration-history store so checkpoint persistence / idempotency round-trip.
        _config.Setup(c => c.LoadMigrationHistory(It.IsAny<string>())).Returns(() => History);
        _config.Setup(c => c.AppendMigrationRecord(It.IsAny<string>(), It.IsAny<DataSourceType>(), It.IsAny<MigrationRecord>()))
               .Callback<string, DataSourceType, MigrationRecord>((_, _, record) => History.Migrations.Add(record));
        _editor.SetupGet(e => e.ConfigEditor).Returns(() => _config.Object);
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

    public MigrationManager Build() => new(Editor, DataSource);

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
            return _h.FailOps.Contains(op)
                ? new ErrorsInfo { Flag = Errors.Failed, Message = $"{op} failed (scripted)." }
                : new ErrorsInfo { Flag = Errors.Ok, Message = $"{op} ok." };
        }

        public IErrorsInfo CreateEntity(EntityStructure entity) => Record("CreateEntity", entity?.EntityName);
        public IErrorsInfo DropEntity(string entityName) => Record("DropEntity", entityName);
        public IErrorsInfo TruncateEntity(string entityName) => Record("TruncateEntity", entityName);
        public IErrorsInfo RenameEntity(string oldName, string newName) => Record("RenameEntity", $"{oldName}->{newName}");
        public IErrorsInfo AddColumn(string entityName, EntityField column) => Record("AddColumn", $"{entityName}.{column?.FieldName}");
        public IErrorsInfo AlterColumn(string entityName, string columnName, EntityField newColumn) => Record("AlterColumn", $"{entityName}.{columnName}");
        public IErrorsInfo DropColumn(string entityName, string columnName) => Record("DropColumn", $"{entityName}.{columnName}");
        public IErrorsInfo RenameColumn(string entityName, string oldColumnName, string newColumnName) => Record("RenameColumn", $"{entityName}.{oldColumnName}->{newColumnName}");
        public IErrorsInfo CreateIndex(string entityName, string indexName, string[] columns, Dictionary<string, object> options = null) => Record("CreateIndex", $"{entityName}.{indexName}");
        public IErrorsInfo DropIndex(string entityName, string indexName) => Record("DropIndex", $"{entityName}.{indexName}");
        public IErrorsInfo AddForeignKey(string entityName, string[] columnNames, string referencedEntityName, string[] referencedColumnNames, string onDeleteBehavior, string onUpdateBehavior, string constraintName) => Record("AddForeignKey", constraintName ?? entityName);
        public IErrorsInfo DropForeignKey(string entityName, string constraintName) => Record("DropForeignKey", $"{entityName}.{constraintName}");
    }
}
