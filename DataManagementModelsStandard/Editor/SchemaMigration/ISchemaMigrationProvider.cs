using System.Collections.Generic;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.SchemaMigration
{
    /// <summary>
    /// Logical schema operations a <see cref="ISchemaMigrationProvider"/> can perform.
    /// These are intentionally provider-agnostic — never SQL strings. Each provider
    /// translates an operation into its data source's native API.
    /// </summary>
    public enum SchemaMigrationOp
    {
        CreateEntity,
        AddColumn,
        AlterColumn,
        DropColumn,
        RenameColumn,
        RenameEntity,
        DropEntity,
        TruncateEntity,
        CreateIndex,
        DropIndex,
        AddForeignKey,
        DropForeignKey
    }

    /// <summary>
    /// Declares which <see cref="SchemaMigrationOp"/>s a provider can execute natively.
    /// All flags default to false except <see cref="SupportsCreateEntity"/>; providers
    /// opt in to the operations their data source genuinely supports.
    /// </summary>
    public class SchemaMigrationCapabilities
    {
        public bool SupportsCreateEntity { get; set; } = true;
        public bool SupportsAddColumn { get; set; }
        public bool SupportsAlterColumn { get; set; }
        public bool SupportsDropColumn { get; set; }
        public bool SupportsRenameColumn { get; set; }
        public bool SupportsRenameEntity { get; set; }
        public bool SupportsDropEntity { get; set; }
        public bool SupportsTruncateEntity { get; set; }
        public bool SupportsCreateIndex { get; set; }
        public bool SupportsDropIndex { get; set; }
        public bool SupportsAddForeignKey { get; set; }
        public bool SupportsDropForeignKey { get; set; }

        /// <summary>True when the provider can wrap mutating DDL in a transaction.</summary>
        public bool SupportsTransactionalDdl { get; set; }

        /// <summary>
        /// True when the data source's schema is owned externally and cannot be mutated
        /// (SaaS APIs, queues, streams, read-only model files). All mutating ops return
        /// <see cref="Errors.Failed"/> with an explicit "unsupported" message.
        /// </summary>
        public bool IsReadOnly { get; set; }

        /// <summary>Returns true if the provider declares support for the given operation.</summary>
        public bool Supports(SchemaMigrationOp op) => op switch
        {
            SchemaMigrationOp.CreateEntity => SupportsCreateEntity,
            SchemaMigrationOp.AddColumn => SupportsAddColumn,
            SchemaMigrationOp.AlterColumn => SupportsAlterColumn,
            SchemaMigrationOp.DropColumn => SupportsDropColumn,
            SchemaMigrationOp.RenameColumn => SupportsRenameColumn,
            SchemaMigrationOp.RenameEntity => SupportsRenameEntity,
            SchemaMigrationOp.DropEntity => SupportsDropEntity,
            SchemaMigrationOp.TruncateEntity => SupportsTruncateEntity,
            SchemaMigrationOp.CreateIndex => SupportsCreateIndex,
            SchemaMigrationOp.DropIndex => SupportsDropIndex,
            SchemaMigrationOp.AddForeignKey => SupportsAddForeignKey,
            SchemaMigrationOp.DropForeignKey => SupportsDropForeignKey,
            _ => false
        };
    }

    /// <summary>
    /// Translates logical <see cref="SchemaMigrationOp"/>s into a data source's NATIVE API.
    /// Providers NEVER return SQL strings to the caller — they execute directly and return
    /// an <see cref="IErrorsInfo"/> outcome. A provider is constructed with the owning
    /// <see cref="IDataSource"/> so it always reflects the live connection.
    /// </summary>
    /// <remarks>
    /// Resolution is 3-tier (see <see cref="IMigrationProviderRegistry"/>):
    /// exact <c>DataSourceType</c> override → <c>DatasourceCategory</c> fallback →
    /// the null provider. Operations a provider does not support return
    /// <see cref="Errors.Failed"/> with a clear message — they never throw and never no-op.
    /// </remarks>
    public interface ISchemaMigrationProvider
    {
        /// <summary>The data source type this provider serves.</summary>
        DataSourceType DataSourceType { get; }

        /// <summary>The data source category this provider serves.</summary>
        DatasourceCategory Category { get; }

        /// <summary>What this provider can do natively.</summary>
        SchemaMigrationCapabilities Capabilities { get; }

        IErrorsInfo CreateEntity(EntityStructure entity);
        IErrorsInfo AddColumn(string entityName, EntityField column);
        IErrorsInfo AlterColumn(string entityName, string columnName, EntityField newColumn);
        IErrorsInfo DropColumn(string entityName, string columnName);
        IErrorsInfo RenameColumn(string entityName, string oldColumnName, string newColumnName);
        IErrorsInfo RenameEntity(string oldName, string newName);
        IErrorsInfo DropEntity(string entityName);
        IErrorsInfo TruncateEntity(string entityName);
        IErrorsInfo CreateIndex(string entityName, string indexName, string[] columns, Dictionary<string, object> options = null);
        IErrorsInfo DropIndex(string entityName, string indexName);
        IErrorsInfo AddForeignKey(string entityName, string[] columnNames, string referencedEntityName, string[] referencedColumnNames, string onDeleteBehavior, string onUpdateBehavior, string constraintName);
        IErrorsInfo DropForeignKey(string entityName, string constraintName);
    }
}
