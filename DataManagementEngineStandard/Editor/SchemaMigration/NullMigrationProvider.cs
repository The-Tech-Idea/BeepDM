using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.SchemaMigration
{
    /// <summary>
    /// Tier-3 fallback provider. Every operation returns <see cref="Errors.Failed"/> with an
    /// explicit "unsupported" message. Used when no Tier-1 override and no Tier-2 category
    /// fallback is registered for a data source — guarantees <see cref="IMigrationProviderRegistry.Resolve"/>
    /// never returns null.
    /// </summary>
    public sealed class NullMigrationProvider : ISchemaMigrationProvider
    {
        private readonly IDataSource _owner;

        public NullMigrationProvider(IDataSource owner)
        {
            _owner = owner;
        }

        public DataSourceType DataSourceType => _owner?.DatasourceType ?? DataSourceType.Unknown;
        public DatasourceCategory Category => _owner?.Category ?? DatasourceCategory.NONE;

        /// <summary>Read-only; no operation is supported.</summary>
        public SchemaMigrationCapabilities Capabilities { get; } = new SchemaMigrationCapabilities
        {
            SupportsCreateEntity = false,
            IsReadOnly = true
        };

        private IErrorsInfo Unsupported(string op) => SchemaMigrationResults.Unsupported(op, DataSourceType);

        public IErrorsInfo CreateEntity(EntityStructure entity) => Unsupported(nameof(CreateEntity));
        public IErrorsInfo AddColumn(string entityName, EntityField column) => Unsupported(nameof(AddColumn));
        public IErrorsInfo AlterColumn(string entityName, string columnName, EntityField newColumn) => Unsupported(nameof(AlterColumn));
        public IErrorsInfo DropColumn(string entityName, string columnName) => Unsupported(nameof(DropColumn));
        public IErrorsInfo RenameColumn(string entityName, string oldColumnName, string newColumnName) => Unsupported(nameof(RenameColumn));
        public IErrorsInfo RenameEntity(string oldName, string newName) => Unsupported(nameof(RenameEntity));
        public IErrorsInfo DropEntity(string entityName) => Unsupported(nameof(DropEntity));
        public IErrorsInfo TruncateEntity(string entityName) => Unsupported(nameof(TruncateEntity));
        public IErrorsInfo CreateIndex(string entityName, string indexName, string[] columns, System.Collections.Generic.Dictionary<string, object> options = null) => Unsupported(nameof(CreateIndex));
        public IErrorsInfo DropIndex(string entityName, string indexName) => Unsupported(nameof(DropIndex));
        public IErrorsInfo AddForeignKey(string entityName, string[] columnNames, string referencedEntityName, string[] referencedColumnNames, string onDeleteBehavior, string onUpdateBehavior, string constraintName) => Unsupported(nameof(AddForeignKey));
        public IErrorsInfo DropForeignKey(string entityName, string constraintName) => Unsupported(nameof(DropForeignKey));
    }
}
