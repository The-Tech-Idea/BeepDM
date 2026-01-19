using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Core;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.StreamingHelpers
{
    /// <summary>
    /// Helper for streaming and queue systems (Kafka, RabbitMQ, ActiveMQ, Pulsar, Kinesis, SQS, SNS, Azure Service Bus).
    /// Generates command-like strings for create/drop/purge/partition operations.
    /// </summary>
    public class StreamingHelper : IDataSourceHelper
    {
        private readonly IDMEEditor _dmeEditor;

        public StreamingHelper(IDMEEditor dmeEditor)
        {
            _dmeEditor = dmeEditor ?? throw new ArgumentNullException(nameof(dmeEditor));
        }

        public DataSourceType SupportedType { get; set; } = DataSourceType.Kafka;
        public string Name => $"Streaming ({SupportedType})";
        public DataSourceCapabilities Capabilities => DataSourceCapabilityMatrix.GetCapabilities(SupportedType);

        #region Schema Operations
        public (string Query, bool Success) GetSchemaQuery(string userName) => (string.Empty, false);
        public (string Query, bool Success) GetTableExistsQuery(string tableName) => (string.Empty, false);
        public (string Query, bool Success) GetColumnInfoQuery(string tableName) => (string.Empty, false);
        #endregion

        #region DDL Operations
        public (string Sql, bool Success, string ErrorMessage) GenerateCreateTableSql(EntityStructure entity, string schemaName = null, DataSourceType? dataSourceType = null)
        {
            if (entity == null || string.IsNullOrWhiteSpace(entity.EntityName))
                return (string.Empty, false, "Queue/topic name is missing");

            return SupportedType switch
            {
                DataSourceType.Kafka => ($"kafka-topics --create --topic {entity.EntityName} --partitions 1 --replication-factor 1", true, "Kafka create topic"),
                DataSourceType.RabbitMQ => ($"rabbitmqadmin declare queue name={entity.EntityName} durable=true", true, "RabbitMQ create queue"),
                DataSourceType.ActiveMQ => ($"activemq-admin create queue {entity.EntityName}", true, "ActiveMQ create queue"),
                DataSourceType.Pulsar => ($"pulsar-admin topics create persistent://public/default/{entity.EntityName}", true, "Pulsar create topic"),
                DataSourceType.AWSKinesis => ($"aws kinesis create-stream --stream-name {entity.EntityName} --shard-count 1", true, "Kinesis create stream"),
                DataSourceType.AWSSQS => ($"aws sqs create-queue --queue-name {entity.EntityName}", true, "SQS create queue"),
                DataSourceType.AWSSNS => ($"aws sns create-topic --name {entity.EntityName}", true, "SNS create topic"),
                DataSourceType.AzureServiceBus => ($"az servicebus queue create --name {entity.EntityName}", true, "Azure Service Bus create queue"),
                _ => (string.Empty, false, "Create operation not supported for this streaming type")
            };
        }

        public (string Sql, bool Success, string ErrorMessage) GenerateDropTableSql(string tableName, string schemaName = null)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                return (string.Empty, false, "Queue/topic name is missing");

            return SupportedType switch
            {
                DataSourceType.Kafka => ($"kafka-topics --delete --topic {tableName}", true, "Kafka delete topic"),
                DataSourceType.RabbitMQ => ($"rabbitmqadmin delete queue name={tableName}", true, "RabbitMQ delete queue"),
                DataSourceType.ActiveMQ => ($"activemq-admin delete queue {tableName}", true, "ActiveMQ delete queue"),
                DataSourceType.Pulsar => ($"pulsar-admin topics delete persistent://public/default/{tableName}", true, "Pulsar delete topic"),
                DataSourceType.AWSKinesis => ($"aws kinesis delete-stream --stream-name {tableName}", true, "Kinesis delete stream"),
                DataSourceType.AWSSQS => ($"aws sqs delete-queue --queue-url {tableName}", true, "SQS delete queue"),
                DataSourceType.AWSSNS => ($"aws sns delete-topic --topic-arn {tableName}", true, "SNS delete topic"),
                DataSourceType.AzureServiceBus => ($"az servicebus queue delete --name {tableName}", true, "Azure Service Bus delete queue"),
                _ => (string.Empty, false, "Drop operation not supported for this streaming type")
            };
        }

        public (string Sql, bool Success, string ErrorMessage) GenerateTruncateTableSql(string tableName, string schemaName = null)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                return (string.Empty, false, "Queue/topic name is missing");

            return SupportedType switch
            {
                DataSourceType.RabbitMQ => ($"rabbitmqadmin purge queue name={tableName}", true, "RabbitMQ purge queue"),
                DataSourceType.AWSSQS => ($"aws sqs purge-queue --queue-url {tableName}", true, "SQS purge queue"),
                _ => (string.Empty, false, "Purge/truncate not supported for this streaming type")
            };
        }

        public (string Sql, bool Success, string ErrorMessage) GenerateCreateIndexSql(string tableName, string indexName, string[] columns, Dictionary<string, object> options = null)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                return (string.Empty, false, "Queue/topic name is missing");

            if (options != null && options.TryGetValue("partitions", out var partVal) && int.TryParse(partVal?.ToString(), out var partitions))
            {
                return SupportedType switch
                {
                    DataSourceType.Kafka => ($"kafka-topics --alter --topic {tableName} --partitions {partitions}", true, "Kafka update partitions"),
                    DataSourceType.Pulsar => ($"pulsar-admin topics create-partitioned-topic persistent://public/default/{tableName} -p {partitions}", true, "Pulsar create partitioned topic"),
                    DataSourceType.AWSKinesis => ($"aws kinesis update-shard-count --stream-name {tableName} --target-shard-count {partitions} --scaling-type UNIFORM_SCALING", true, "Kinesis update shards"),
                    _ => (string.Empty, false, "Partition update not supported for this streaming type")
                };
            }

            return (string.Empty, false, "Index/partition operation not supported without partitions option");
        }

        public (string Sql, bool Success, string ErrorMessage) GenerateAddColumnSql(string tableName, EntityField column)
            => (string.Empty, true, "Schema updates are handled externally (e.g., schema registry)");

        public (string Sql, bool Success, string ErrorMessage) GenerateAlterColumnSql(string tableName, string columnName, EntityField newColumn)
            => (string.Empty, false, "Schema updates are not supported by this helper");

        public (string Sql, bool Success, string ErrorMessage) GenerateDropColumnSql(string tableName, string columnName)
            => (string.Empty, false, "Schema updates are not supported by this helper");

        public (string Sql, bool Success, string ErrorMessage) GenerateRenameTableSql(string oldTableName, string newTableName)
            => (string.Empty, false, "Renaming queues/topics is not supported by this helper");

        public (string Sql, bool Success, string ErrorMessage) GenerateRenameColumnSql(string tableName, string oldColumnName, string newColumnName)
            => (string.Empty, false, "Schema updates are not supported by this helper");
        #endregion

        #region Constraint Operations
        public (string Sql, bool Success, string ErrorMessage) GenerateAddPrimaryKeySql(string tableName, params string[] columnNames)
            => (string.Empty, false, "Constraints are not supported");
        public (string Sql, bool Success, string ErrorMessage) GenerateAddForeignKeySql(string tableName, string[] columnNames, string referencedTableName, string[] referencedColumnNames)
            => (string.Empty, false, "Constraints are not supported");
        public (string Sql, bool Success, string ErrorMessage) GenerateAddConstraintSql(string tableName, string constraintName, string constraintDefinition)
            => (string.Empty, false, "Constraints are not supported");
        public (string Query, bool Success, string ErrorMessage) GetPrimaryKeyQuery(string tableName)
            => (string.Empty, false, "Constraints are not supported");
        public (string Query, bool Success, string ErrorMessage) GetForeignKeysQuery(string tableName)
            => (string.Empty, false, "Constraints are not supported");
        public (string Query, bool Success, string ErrorMessage) GetConstraintsQuery(string tableName)
            => (string.Empty, false, "Constraints are not supported");
        #endregion

        #region Transaction Control
        public (string Sql, bool Success, string ErrorMessage) GenerateBeginTransactionSql()
            => (string.Empty, false, "Transactions are not supported");
        public (string Sql, bool Success, string ErrorMessage) GenerateCommitSql()
            => (string.Empty, false, "Transactions are not supported");
        public (string Sql, bool Success, string ErrorMessage) GenerateRollbackSql()
            => (string.Empty, false, "Transactions are not supported");
        #endregion

        #region DML Operations
        public (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateInsertSql(string tableName, Dictionary<string, object> data)
            => (string.Empty, new Dictionary<string, object>(), false, "Insert not implemented");
        public (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateUpdateSql(string tableName, Dictionary<string, object> data, Dictionary<string, object> conditions)
            => (string.Empty, new Dictionary<string, object>(), false, "Update not implemented");
        public (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateDeleteSql(string tableName, Dictionary<string, object> conditions)
            => (string.Empty, new Dictionary<string, object>(), false, "Delete not implemented");
        public (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateSelectSql(string tableName, IEnumerable<string> columns = null, Dictionary<string, object> conditions = null, string orderBy = null, int? skip = null, int? take = null)
            => (string.Empty, new Dictionary<string, object>(), false, "Select not implemented");
        #endregion

        #region Utility Methods
        public string QuoteIdentifier(string identifier) => identifier;
        public string MapClrTypeToDatasourceType(Type clrType, int? size = null, int? precision = null, int? scale = null) => "string";
        public Type MapDatasourceTypeToClrType(string datasourceType) => typeof(string);
        public (bool IsValid, List<string> Errors) ValidateEntity(EntityStructure entity) => (entity != null, new List<string>());
        public bool SupportsCapability(CapabilityType capability) => Capabilities.IsCapable(capability);
        public int GetMaxStringSize() => -1;
        public int GetMaxNumericPrecision() => 0;
        #endregion
    }
}
