using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.SchemaMigration
{
    /// <summary>
    /// Tier-2 fallback for data sources whose schema is owned externally and cannot be mutated
    /// from BeepDM: SaaS/REST connectors (Salesforce, Slack…), messaging (Kafka, RabbitMQ…),
    /// and web APIs. Every mutating operation returns <see cref="Errors.Failed"/> with a clear
    /// "managed externally" message; <see cref="SchemaMigrationCapabilities.IsReadOnly"/> is true.
    /// </summary>
    public sealed class ExternalReadOnlyMigrationProvider : ISchemaMigrationProvider
    {
        private readonly IDataSource _owner;

        public ExternalReadOnlyMigrationProvider(IDataSource owner)
        {
            _owner = owner;
        }

        public DataSourceType DataSourceType => _owner?.DatasourceType ?? DataSourceType.Unknown;
        public DatasourceCategory Category => _owner?.Category ?? DatasourceCategory.NONE;

        public SchemaMigrationCapabilities Capabilities { get; } = new SchemaMigrationCapabilities
        {
            SupportsCreateEntity = false,
            IsReadOnly = true
        };

        private IErrorsInfo External(string op)
        {
            var type = DataSourceType;
            return new ErrorsInfo
            {
                Flag = Errors.Failed,
                Message = $"{SchemaMigrationResults.UnsupportedPrefix} '{op}' is not available — schema for {type} is managed externally."
            };
        }

        public IErrorsInfo CreateEntity(EntityStructure entity) => External(nameof(CreateEntity));
        public IErrorsInfo AddColumn(string entityName, EntityField column) => External(nameof(AddColumn));
        public IErrorsInfo AlterColumn(string entityName, string columnName, EntityField newColumn) => External(nameof(AlterColumn));
        public IErrorsInfo DropColumn(string entityName, string columnName) => External(nameof(DropColumn));
        public IErrorsInfo RenameColumn(string entityName, string oldColumnName, string newColumnName) => External(nameof(RenameColumn));
        public IErrorsInfo RenameEntity(string oldName, string newName) => External(nameof(RenameEntity));
        public IErrorsInfo DropEntity(string entityName) => External(nameof(DropEntity));
        public IErrorsInfo TruncateEntity(string entityName) => External(nameof(TruncateEntity));
        public IErrorsInfo CreateIndex(string entityName, string indexName, string[] columns, System.Collections.Generic.Dictionary<string, object> options = null) => External(nameof(CreateIndex));
        public IErrorsInfo DropIndex(string entityName, string indexName) => External(nameof(DropIndex));
        public IErrorsInfo AddForeignKey(string entityName, string[] columnNames, string referencedEntityName, string[] referencedColumnNames, string onDeleteBehavior, string onUpdateBehavior, string constraintName) => External(nameof(AddForeignKey));
        public IErrorsInfo DropForeignKey(string entityName, string constraintName) => External(nameof(DropForeignKey));
    }
}
