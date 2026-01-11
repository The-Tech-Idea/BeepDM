using System;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.Core
{
    /// <summary>
    /// Defines the capabilities and features supported by a specific datasource type.
    /// Used to determine what operations (transactions, joins, aggregations, etc.) are possible
    /// with a given datasource, enabling graceful degradation when features aren't available.
    /// </summary>
    public class DataSourceCapabilities
    {
        /// <summary>
        /// Indicates whether the datasource supports ACID transactions.
        /// </summary>
        /// <remarks>
        /// - RDBMS: true (full ACID)
        /// - MongoDB: true (multi-document, v4.0+)
        /// - Redis: true (Lua scripts provide atomicity)
        /// - Cassandra: false (eventual consistency only)
        /// - REST APIs: false (no server-side transaction support)
        /// </remarks>
        public bool SupportsTransactions { get; set; }

        /// <summary>
        /// Indicates whether the datasource supports JOIN operations.
        /// </summary>
        /// <remarks>
        /// - RDBMS: true (full JOIN support)
        /// - MongoDB: false (uses $lookup aggregation, different paradigm)
        /// - NoSQL: false (typically requires denormalization)
        /// - Cassandra: false (no JOIN support)
        /// </remarks>
        public bool SupportsJoins { get; set; }

        /// <summary>
        /// Indicates whether the datasource supports aggregation operations (GROUP BY, COUNT, SUM, etc.).
        /// </summary>
        public bool SupportsAggregations { get; set; }

        /// <summary>
        /// Indicates whether the datasource supports index creation and management.
        /// </summary>
        public bool SupportsIndexes { get; set; }

        /// <summary>
        /// Indicates whether the datasource supports parameterized queries (prevents SQL injection).
        /// </summary>
        /// <remarks>
        /// Note: Some drivers automatically handle parameterization (MongoDB, Elasticsearch),
        /// while others require explicit parameter binding (SQL databases).
        /// </remarks>
        public bool SupportsParameterization { get; set; }

        /// <summary>
        /// Indicates whether the datasource supports auto-increment/identity columns.
        /// </summary>
        /// <remarks>
        /// - RDBMS: true (AUTO_INCREMENT, IDENTITY, SEQUENCE)
        /// - MongoDB: false (application-assigned _id or ObjectId)
        /// - Redis: true (INCR command)
        /// - File-based: false
        /// </remarks>
        public bool SupportsIdentity { get; set; }

        /// <summary>
        /// Indicates whether the datasource supports Time-To-Live (TTL) on records.
        /// </summary>
        /// <remarks>
        /// - Redis: true (EXPIRE command)
        /// - MongoDB: true (TTL indexes)
        /// - Cassandra: true (TTL on columns)
        /// - RDBMS: false (no native TTL)
        /// </remarks>
        public bool SupportsTTL { get; set; }

        /// <summary>
        /// Indicates whether the datasource supports temporal tables (time-versioning).
        /// </summary>
        /// <remarks>
        /// - SQL Server 2016+: true
        /// - PostgreSQL 10+: true
        /// - Most NoSQL: false
        /// </remarks>
        public bool SupportsTemporalTables { get; set; }

        /// <summary>
        /// Indicates whether the datasource supports window functions (OVER, ROW_NUMBER, RANK, etc.).
        /// </summary>
        public bool SupportsWindowFunctions { get; set; }

        /// <summary>
        /// Indicates whether the datasource supports stored procedures or equivalent.
        /// </summary>
        public bool SupportsStoredProcedures { get; set; }

        /// <summary>
        /// Indicates whether the datasource supports bulk insert/update/delete operations.
        /// </summary>
        public bool SupportsBulkOperations { get; set; }

        /// <summary>
        /// Indicates whether the datasource supports full-text search.
        /// </summary>
        public bool SupportsFullTextSearch { get; set; }

        /// <summary>
        /// Indicates whether the datasource has native JSON support.
        /// </summary>
        public bool SupportsNativeJson { get; set; }

        /// <summary>
        /// Indicates whether the datasource supports table/collection partitioning.
        /// </summary>
        public bool SupportsPartitioning { get; set; }

        /// <summary>
        /// Indicates whether the datasource supports data replication.
        /// </summary>
        public bool SupportsReplication { get; set; }

        /// <summary>
        /// Indicates whether the datasource supports views.
        /// </summary>
        public bool SupportsViews { get; set; }

        /// <summary>
        /// Indicates whether the datasource supports schema evolution (adding columns, etc.).
        /// </summary>
        /// <remarks>
        /// - RDBMS: true (ALTER TABLE, but can be slow)
        /// - MongoDB: true (schema-free)
        /// - Redis: false (key-value only)
        /// </remarks>
        public bool SupportsSchemaEvolution { get; set; }

        /// <summary>
        /// Indicates whether the datasource is schema-enforced (requires predefined structure).
        /// </summary>
        /// <remarks>
        /// - RDBMS: true
        /// - MongoDB: false (schema-free)
        /// - Redis: depends on usage
        /// </remarks>
        public bool IsSchemaEnforced { get; set; }

        // ========== Vector Database Capabilities ==========

        /// <summary>
        /// Indicates whether the datasource supports vector similarity search.
        /// </summary>
        public bool SupportsVectorSearch { get; set; }

        /// <summary>
        /// Indicates whether the datasource supports embedding storage and retrieval.
        /// </summary>
        public bool SupportsEmbeddings { get; set; }

        /// <summary>
        /// Indicates whether the datasource supports Approximate Nearest Neighbor algorithms (HNSW, IVF).
        /// </summary>
        public bool SupportsANN { get; set; }

        // ========== Graph Database Capabilities ==========

        /// <summary>
        /// Indicates whether the datasource supports graph traversal operations.
        /// </summary>
        public bool SupportsGraphTraversal { get; set; }

        /// <summary>
        /// Indicates whether the datasource supports Cypher query language (Neo4j).
        /// </summary>
        public bool SupportsCypherQuery { get; set; }

        /// <summary>
        /// Indicates whether the datasource supports Gremlin query language.
        /// </summary>
        public bool SupportsGremlinQuery { get; set; }

        // ========== Time Series Capabilities ==========

        /// <summary>
        /// Indicates whether the datasource supports time series specific queries.
        /// </summary>
        public bool SupportsTimeSeriesQueries { get; set; }

        /// <summary>
        /// Indicates whether the datasource supports data downsampling/aggregation over time.
        /// </summary>
        public bool SupportsDownsampling { get; set; }

        /// <summary>
        /// Indicates whether the datasource supports data retention policies.
        /// </summary>
        public bool SupportsRetentionPolicies { get; set; }

        // ========== Search Engine Capabilities ==========

        /// <summary>
        /// Indicates whether the datasource supports faceted/filtered search.
        /// </summary>
        public bool SupportsFacetedSearch { get; set; }

        /// <summary>
        /// Indicates whether the datasource supports search result highlighting.
        /// </summary>
        public bool SupportsHighlighting { get; set; }

        // ========== Streaming & Protocol Capabilities ==========

        /// <summary>
        /// Indicates whether the datasource supports streaming data operations.
        /// </summary>
        public bool SupportsStreaming { get; set; }

        /// <summary>
        /// Indicates whether the datasource supports publish/subscribe messaging pattern.
        /// </summary>
        public bool SupportsPublishSubscribe { get; set; }

        /// <summary>
        /// Indicates whether the datasource supports message ordering guarantees.
        /// </summary>
        public bool SupportsMessageOrdering { get; set; }

        /// <summary>
        /// Indicates whether the datasource supports dead letter queue for failed messages.
        /// </summary>
        public bool SupportsDeadLetterQueue { get; set; }

        /// <summary>
        /// Indicates whether the datasource supports push notifications / real-time updates.
        /// </summary>
        public bool SupportsPushNotifications { get; set; }

        /// <summary>
        /// Indicates whether the datasource supports append-only operations (logs, streams).
        /// </summary>
        public bool SupportsAppend { get; set; }

        /// <summary>
        /// Indicates whether the datasource supports binary protocol (gRPC, Thrift).
        /// </summary>
        public bool SupportsBinaryProtocol { get; set; }

        /// <summary>
        /// Indicates whether the datasource supports async/await operation pattern.
        /// </summary>
        public bool SupportsAsyncOperations { get; set; }

        /// <summary>
        /// Indicates whether the datasource supports OAuth authentication.
        /// </summary>
        public bool SupportsOAuth { get; set; }

        /// <summary>
        /// Indicates whether the datasource supports continuous queries / change data capture.
        /// </summary>
        public bool SupportsContinuousQueries { get; set; }

        /// <summary>
        /// Gets or sets a description of key limitations or notable features.
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// Determines if a specific capability is supported.
        /// </summary>
        /// <param name="capabilityName">Name of the capability (property name, case-insensitive)</param>
        /// <returns>True if the capability is supported; false otherwise</returns>
        public bool IsCapable(string capabilityName)
        {
            if (string.IsNullOrWhiteSpace(capabilityName))
                return false;

            var prop = typeof(DataSourceCapabilities).GetProperty(
                capabilityName, 
                System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            if (prop == null || prop.PropertyType != typeof(bool))
                return false;

            return (bool)(prop.GetValue(this) ?? false);
        }

        /// <summary>
        /// Determines if a specific capability is supported using strongly-typed enum.
        /// </summary>
        /// <param name="capability">The capability to check</param>
        /// <returns>True if the capability is supported; false otherwise</returns>
        public bool IsCapable(CapabilityType capability)
        {
            return capability switch
            {
                CapabilityType.SupportsTransactions => SupportsTransactions,
                CapabilityType.SupportsJoins => SupportsJoins,
                CapabilityType.SupportsAggregations => SupportsAggregations,
                CapabilityType.SupportsIndexes => SupportsIndexes,
                CapabilityType.SupportsParameterization => SupportsParameterization,
                CapabilityType.SupportsIdentity => SupportsIdentity,
                CapabilityType.SupportsTTL => SupportsTTL,
                CapabilityType.SupportsTemporalTables => SupportsTemporalTables,
                CapabilityType.SupportsWindowFunctions => SupportsWindowFunctions,
                CapabilityType.SupportsStoredProcedures => SupportsStoredProcedures,
                CapabilityType.SupportsBulkOperations => SupportsBulkOperations,
                CapabilityType.SupportsFullTextSearch => SupportsFullTextSearch,
                CapabilityType.SupportsNativeJson => SupportsNativeJson,
                CapabilityType.SupportsPartitioning => SupportsPartitioning,
                CapabilityType.SupportsReplication => SupportsReplication,
                CapabilityType.SupportsViews => SupportsViews,
                CapabilityType.SupportsSchemaEvolution => SupportsSchemaEvolution,
                CapabilityType.IsSchemaEnforced => IsSchemaEnforced,
                // Vector DB capabilities
                CapabilityType.SupportsVectorSearch => SupportsVectorSearch,
                CapabilityType.SupportsEmbeddings => SupportsEmbeddings,
                CapabilityType.SupportsANN => SupportsANN,
                // Graph DB capabilities
                CapabilityType.SupportsGraphTraversal => SupportsGraphTraversal,
                CapabilityType.SupportsCypherQuery => SupportsCypherQuery,
                CapabilityType.SupportsGremlinQuery => SupportsGremlinQuery,
                // Time Series capabilities
                CapabilityType.SupportsTimeSeriesQueries => SupportsTimeSeriesQueries,
                CapabilityType.SupportsDownsampling => SupportsDownsampling,
                CapabilityType.SupportsRetentionPolicies => SupportsRetentionPolicies,
                // Search Engine capabilities
                CapabilityType.SupportsFacetedSearch => SupportsFacetedSearch,
                CapabilityType.SupportsHighlighting => SupportsHighlighting,
                // Streaming & Protocol capabilities
                CapabilityType.SupportsStreaming => SupportsStreaming,
                CapabilityType.SupportsPublishSubscribe => SupportsPublishSubscribe,
                CapabilityType.SupportsMessageOrdering => SupportsMessageOrdering,
                CapabilityType.SupportsDeadLetterQueue => SupportsDeadLetterQueue,
                CapabilityType.SupportsPushNotifications => SupportsPushNotifications,
                CapabilityType.SupportsAppend => SupportsAppend,
                CapabilityType.SupportsBinaryProtocol => SupportsBinaryProtocol,
                CapabilityType.SupportsAsyncOperations => SupportsAsyncOperations,
                CapabilityType.SupportsOAuth => SupportsOAuth,
                CapabilityType.SupportsContinuousQueries => SupportsContinuousQueries,
                _ => false
            };
        }

        /// <summary>
        /// Gets a summary of capabilities in a human-readable format.
        /// </summary>
        public override string ToString()
        {
            return $"Capabilities: Transactions={SupportsTransactions}, Joins={SupportsJoins}, " +
                   $"Aggregations={SupportsAggregations}, Indexes={SupportsIndexes}, " +
                   $"Parameterization={SupportsParameterization}, Identity={SupportsIdentity}, " +
                   $"TTL={SupportsTTL}, WindowFunctions={SupportsWindowFunctions}, " +
                   $"BulkOps={SupportsBulkOperations}, FullText={SupportsFullTextSearch}, " +
                   $"SchemaEnforced={IsSchemaEnforced}";
        }
    }
}
