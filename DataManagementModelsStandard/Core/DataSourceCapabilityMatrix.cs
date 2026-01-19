using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Core
{
    /// <summary>
    /// Comprehensive capability matrix for 40+ datasource types.
    /// Defines which operations (transactions, joins, aggregations, etc.) are supported by each datasource.
    /// Enables intelligent query generation and graceful degradation when features aren't available.
    /// </summary>
    public static class DataSourceCapabilityMatrix
    {
        private static readonly Dictionary<DataSourceType, DataSourceCapabilities> Matrix =
            new Dictionary<DataSourceType, DataSourceCapabilities>
            {
                #region Relational Databases

                {
                    DataSourceType.SqlServer, new DataSourceCapabilities
                    {
                        SupportsTransactions = true,
                        SupportsJoins = true,
                        SupportsAggregations = true,
                        SupportsIndexes = true,
                        SupportsParameterization = true,
                        SupportsIdentity = true,
                        SupportsTTL = false,
                        SupportsTemporalTables = true,
                        SupportsWindowFunctions = true,
                        SupportsStoredProcedures = true,
                        SupportsBulkOperations = true,
                        SupportsFullTextSearch = true,
                        SupportsNativeJson = true,
                        SupportsPartitioning = true,
                        SupportsReplication = true,
                        SupportsViews = true,
                        SupportsSchemaEvolution = true,
                        IsSchemaEnforced = true,
                        Notes = "Full ACID support; excellent for transactional systems"
                    }
                },
                {
                    DataSourceType.Mysql, new DataSourceCapabilities
                    {
                        SupportsTransactions = true,
                        SupportsJoins = true,
                        SupportsAggregations = true,
                        SupportsIndexes = true,
                        SupportsParameterization = true,
                        SupportsIdentity = true,
                        SupportsTTL = false,
                        SupportsTemporalTables = true,
                        SupportsWindowFunctions = true,
                        SupportsStoredProcedures = true,
                        SupportsBulkOperations = true,
                        SupportsFullTextSearch = true,
                        SupportsNativeJson = true,
                        SupportsPartitioning = true,
                        SupportsReplication = true,
                        SupportsViews = true,
                        SupportsSchemaEvolution = true,
                        IsSchemaEnforced = true,
                        Notes = "MySQL 5.7+ recommended; MariaDB compatible"
                    }
                },
                {
                    DataSourceType.Postgre, new DataSourceCapabilities
                    {
                        SupportsTransactions = true,
                        SupportsJoins = true,
                        SupportsAggregations = true,
                        SupportsIndexes = true,
                        SupportsParameterization = true,
                        SupportsIdentity = true,
                        SupportsTTL = false,
                        SupportsTemporalTables = true,
                        SupportsWindowFunctions = true,
                        SupportsStoredProcedures = true,
                        SupportsBulkOperations = true,
                        SupportsFullTextSearch = true,
                        SupportsNativeJson = true,
                        SupportsPartitioning = true,
                        SupportsReplication = true,
                        SupportsViews = true,
                        SupportsSchemaEvolution = true,
                        IsSchemaEnforced = true,
                        Notes = "Modern RDBMS with excellent JSON support"
                    }
                },
                {
                    DataSourceType.Oracle, new DataSourceCapabilities
                    {
                        SupportsTransactions = true,
                        SupportsJoins = true,
                        SupportsAggregations = true,
                        SupportsIndexes = true,
                        SupportsParameterization = true,
                        SupportsIdentity = true,
                        SupportsTTL = false,
                        SupportsTemporalTables = true,
                        SupportsWindowFunctions = true,
                        SupportsStoredProcedures = true,
                        SupportsBulkOperations = true,
                        SupportsFullTextSearch = true,
                        SupportsNativeJson = true,
                        SupportsPartitioning = true,
                        SupportsReplication = true,
                        SupportsViews = true,
                        SupportsSchemaEvolution = true,
                        IsSchemaEnforced = true,
                        Notes = "Enterprise-grade RDBMS; excellent for large systems"
                    }
                },
                {
                    DataSourceType.SqlLite, new DataSourceCapabilities
                    {
                        SupportsTransactions = true,
                        SupportsJoins = true,
                        SupportsAggregations = true,
                        SupportsIndexes = true,
                        SupportsParameterization = true,
                        SupportsIdentity = true,
                        SupportsTTL = false,
                        SupportsTemporalTables = false,
                        SupportsWindowFunctions = true,
                        SupportsStoredProcedures = false,
                        SupportsBulkOperations = true,
                        SupportsFullTextSearch = false,
                        SupportsNativeJson = true,
                        SupportsPartitioning = false,
                        SupportsReplication = false,
                        SupportsViews = true,
                        SupportsSchemaEvolution = true,
                        IsSchemaEnforced = true,
                        Notes = "Lightweight embedded database; great for mobile/desktop"
                    }
                },
                {
                    DataSourceType.DB2, new DataSourceCapabilities
                    {
                        SupportsTransactions = true,
                        SupportsJoins = true,
                        SupportsAggregations = true,
                        SupportsIndexes = true,
                        SupportsParameterization = true,
                        SupportsIdentity = true,
                        SupportsTTL = false,
                        SupportsTemporalTables = true,
                        SupportsWindowFunctions = true,
                        SupportsStoredProcedures = true,
                        SupportsBulkOperations = true,
                        SupportsFullTextSearch = true,
                        SupportsNativeJson = true,
                        SupportsPartitioning = true,
                        SupportsReplication = true,
                        SupportsViews = true,
                        SupportsSchemaEvolution = true,
                        IsSchemaEnforced = true,
                        Notes = "IBM enterprise database"
                    }
                },
                {
                    DataSourceType.FireBird, new DataSourceCapabilities
                    {
                        SupportsTransactions = true,
                        SupportsJoins = true,
                        SupportsAggregations = true,
                        SupportsIndexes = true,
                        SupportsParameterization = true,
                        SupportsIdentity = true,
                        SupportsTTL = false,
                        SupportsTemporalTables = false,
                        SupportsWindowFunctions = false,
                        SupportsStoredProcedures = true,
                        SupportsBulkOperations = true,
                        SupportsFullTextSearch = false,
                        SupportsNativeJson = false,
                        SupportsPartitioning = false,
                        SupportsReplication = false,
                        SupportsViews = true,
                        SupportsSchemaEvolution = true,
                        IsSchemaEnforced = true,
                        Notes = "Open-source RDBMS"
                    }
                },

                #endregion

                #region Cloud Databases

                {
                    DataSourceType.AzureSQL, new DataSourceCapabilities
                    {
                        SupportsTransactions = true,
                        SupportsJoins = true,
                        SupportsAggregations = true,
                        SupportsIndexes = true,
                        SupportsParameterization = true,
                        SupportsIdentity = true,
                        SupportsTTL = false,
                        SupportsTemporalTables = true,
                        SupportsWindowFunctions = true,
                        SupportsStoredProcedures = true,
                        SupportsBulkOperations = true,
                        SupportsFullTextSearch = true,
                        SupportsNativeJson = true,
                        SupportsPartitioning = true,
                        SupportsReplication = true,
                        SupportsViews = true,
                        SupportsSchemaEvolution = true,
                        IsSchemaEnforced = true,
                        Notes = "SQL Server in Azure; fully managed"
                    }
                },
                {
                    DataSourceType.AWSRDS, new DataSourceCapabilities
                    {
                        SupportsTransactions = true,
                        SupportsJoins = true,
                        SupportsAggregations = true,
                        SupportsIndexes = true,
                        SupportsParameterization = true,
                        SupportsIdentity = true,
                        SupportsTTL = false,
                        SupportsTemporalTables = true,
                        SupportsWindowFunctions = true,
                        SupportsStoredProcedures = true,
                        SupportsBulkOperations = true,
                        SupportsFullTextSearch = true,
                        SupportsNativeJson = true,
                        SupportsPartitioning = true,
                        SupportsReplication = true,
                        SupportsViews = true,
                        SupportsSchemaEvolution = true,
                        IsSchemaEnforced = true,
                        Notes = "AWS managed relational database"
                    }
                },
                {
                    DataSourceType.SnowFlake, new DataSourceCapabilities
                    {
                        SupportsTransactions = true,
                        SupportsJoins = true,
                        SupportsAggregations = true,
                        SupportsIndexes = false,
                        SupportsParameterization = true,
                        SupportsIdentity = true,
                        SupportsTTL = false,
                        SupportsTemporalTables = false,
                        SupportsWindowFunctions = true,
                        SupportsStoredProcedures = true,
                        SupportsBulkOperations = true,
                        SupportsFullTextSearch = false,
                        SupportsNativeJson = true,
                        SupportsPartitioning = true,
                        SupportsReplication = true,
                        SupportsViews = true,
                        SupportsSchemaEvolution = true,
                        IsSchemaEnforced = true,
                        Notes = "Cloud data warehouse; excellent for analytics"
                    }
                },
                {
                    DataSourceType.Cockroach, new DataSourceCapabilities
                    {
                        SupportsTransactions = true,
                        SupportsJoins = true,
                        SupportsAggregations = true,
                        SupportsIndexes = true,
                        SupportsParameterization = true,
                        SupportsIdentity = true,
                        SupportsTTL = true,
                        SupportsTemporalTables = false,
                        SupportsWindowFunctions = true,
                        SupportsStoredProcedures = false,
                        SupportsBulkOperations = true,
                        SupportsFullTextSearch = false,
                        SupportsNativeJson = true,
                        SupportsPartitioning = true,
                        SupportsReplication = true,
                        SupportsViews = true,
                        SupportsSchemaEvolution = true,
                        IsSchemaEnforced = true,
                        Notes = "Distributed SQL; strong consistency with scalability"
                    }
                },
                {
                    DataSourceType.Spanner, new DataSourceCapabilities
                    {
                        SupportsTransactions = true,
                        SupportsJoins = true,
                        SupportsAggregations = true,
                        SupportsIndexes = true,
                        SupportsParameterization = true,
                        SupportsIdentity = true,
                        SupportsTTL = false,
                        SupportsTemporalTables = false,
                        SupportsWindowFunctions = true,
                        SupportsStoredProcedures = false,
                        SupportsBulkOperations = true,
                        SupportsFullTextSearch = false,
                        SupportsNativeJson = true,
                        SupportsPartitioning = true,
                        SupportsReplication = true,
                        SupportsViews = true,
                        SupportsSchemaEvolution = true,
                        IsSchemaEnforced = true,
                        Notes = "Google Cloud global distributed database"
                    }
                },

                #endregion

                #region NoSQL Databases

                {
                    DataSourceType.MongoDB, new DataSourceCapabilities
                    {
                        SupportsTransactions = true,
                        SupportsJoins = false,
                        SupportsAggregations = true,
                        SupportsIndexes = true,
                        SupportsParameterization = false,
                        SupportsIdentity = false,
                        SupportsTTL = true,
                        SupportsTemporalTables = false,
                        SupportsWindowFunctions = false,
                        SupportsStoredProcedures = false,
                        SupportsBulkOperations = true,
                        SupportsFullTextSearch = true,
                        SupportsNativeJson = true,
                        SupportsPartitioning = true,
                        SupportsReplication = true,
                        SupportsViews = false,
                        SupportsSchemaEvolution = true,
                        IsSchemaEnforced = false,
                        Notes = "Document-oriented; multi-doc ACID (v4.0+); use $lookup for joins"
                    }
                },
                {
                    DataSourceType.Redis, new DataSourceCapabilities
                    {
                        SupportsTransactions = true,
                        SupportsJoins = false,
                        SupportsAggregations = false,
                        SupportsIndexes = false,
                        SupportsParameterization = false,
                        SupportsIdentity = true,
                        SupportsTTL = true,
                        SupportsTemporalTables = false,
                        SupportsWindowFunctions = false,
                        SupportsStoredProcedures = true,
                        SupportsBulkOperations = true,
                        SupportsFullTextSearch = false,
                        SupportsNativeJson = false,
                        SupportsPartitioning = false,
                        SupportsReplication = true,
                        SupportsViews = false,
                        SupportsSchemaEvolution = false,
                        IsSchemaEnforced = false,
                        Notes = "In-memory data store; Lua scripts provide atomic operations; excellent for caching"
                    }
                },
                {
                    DataSourceType.Cassandra, new DataSourceCapabilities
                    {
                        SupportsTransactions = false,
                        SupportsJoins = false,
                        SupportsAggregations = false,
                        SupportsIndexes = true,
                        SupportsParameterization = true,
                        SupportsIdentity = false,
                        SupportsTTL = true,
                        SupportsTemporalTables = false,
                        SupportsWindowFunctions = false,
                        SupportsStoredProcedures = false,
                        SupportsBulkOperations = true,
                        SupportsFullTextSearch = false,
                        SupportsNativeJson = false,
                        SupportsPartitioning = true,
                        SupportsReplication = true,
                        SupportsViews = false,
                        SupportsSchemaEvolution = true,
                        IsSchemaEnforced = true,
                        Notes = "Distributed NoSQL; eventual consistency; no joins; denormalization required"
                    }
                },
                {
                    DataSourceType.Neo4j, new DataSourceCapabilities
                    {
                        SupportsTransactions = true,
                        SupportsJoins = false,
                        SupportsAggregations = true,
                        SupportsIndexes = true,
                        SupportsParameterization = true,
                        SupportsIdentity = false,
                        SupportsTTL = false,
                        SupportsTemporalTables = false,
                        SupportsWindowFunctions = false,
                        SupportsStoredProcedures = false,
                        SupportsBulkOperations = true,
                        SupportsFullTextSearch = true,
                        SupportsNativeJson = false,
                        SupportsPartitioning = false,
                        SupportsReplication = true,
                        SupportsViews = false,
                        SupportsSchemaEvolution = true,
                        IsSchemaEnforced = false,
                        Notes = "Graph database; Cypher query language; excellent for relationship queries"
                    }
                },
                {
                    DataSourceType.CouchDB, new DataSourceCapabilities
                    {
                        SupportsTransactions = true,
                        SupportsJoins = false,
                        SupportsAggregations = false,
                        SupportsIndexes = true,
                        SupportsParameterization = false,
                        SupportsIdentity = false,
                        SupportsTTL = false,
                        SupportsTemporalTables = false,
                        SupportsWindowFunctions = false,
                        SupportsStoredProcedures = false,
                        SupportsBulkOperations = true,
                        SupportsFullTextSearch = true,
                        SupportsNativeJson = true,
                        SupportsPartitioning = true,
                        SupportsReplication = true,
                        SupportsViews = true,
                        SupportsSchemaEvolution = true,
                        IsSchemaEnforced = false,
                        Notes = "Document database with REST API; MVCC concurrency"
                    }
                },
                {
                    DataSourceType.Couchbase, new DataSourceCapabilities
                    {
                        SupportsTransactions = true,
                        SupportsJoins = false,
                        SupportsAggregations = false,
                        SupportsIndexes = true,
                        SupportsParameterization = true,
                        SupportsIdentity = false,
                        SupportsTTL = true,
                        SupportsTemporalTables = false,
                        SupportsWindowFunctions = false,
                        SupportsStoredProcedures = false,
                        SupportsBulkOperations = true,
                        SupportsFullTextSearch = true,
                        SupportsNativeJson = true,
                        SupportsPartitioning = true,
                        SupportsReplication = true,
                        SupportsViews = false,
                        SupportsSchemaEvolution = true,
                        IsSchemaEnforced = false,
                        Notes = "NoSQL with SQL-like query language (N1QL); TTL support"
                    }
                },

                #endregion

                #region Search & Analytics

                {
                    DataSourceType.ElasticSearch, new DataSourceCapabilities
                    {
                        SupportsTransactions = false,
                        SupportsJoins = false,
                        SupportsAggregations = true,
                        SupportsIndexes = true,
                        SupportsParameterization = false,
                        SupportsIdentity = false,
                        SupportsTTL = true,
                        SupportsTemporalTables = false,
                        SupportsWindowFunctions = false,
                        SupportsStoredProcedures = false,
                        SupportsBulkOperations = true,
                        SupportsFullTextSearch = true,
                        SupportsNativeJson = true,
                        SupportsPartitioning = true,
                        SupportsReplication = true,
                        SupportsViews = false,
                        SupportsSchemaEvolution = true,
                        IsSchemaEnforced = false,
                        Notes = "Full-text search engine; aggregations via bucket aggregations; no joins"
                    }
                },
                {
                    DataSourceType.ClickHouse, new DataSourceCapabilities
                    {
                        SupportsTransactions = false,
                        SupportsJoins = true,
                        SupportsAggregations = true,
                        SupportsIndexes = true,
                        SupportsParameterization = true,
                        SupportsIdentity = false,
                        SupportsTTL = true,
                        SupportsTemporalTables = false,
                        SupportsWindowFunctions = true,
                        SupportsStoredProcedures = false,
                        SupportsBulkOperations = true,
                        SupportsFullTextSearch = true,
                        SupportsNativeJson = true,
                        SupportsPartitioning = true,
                        SupportsReplication = true,
                        SupportsViews = true,
                        SupportsSchemaEvolution = true,
                        IsSchemaEnforced = true,
                        Notes = "Columnar database; OLAP workloads; limited transactions"
                    }
                },
                {
                    DataSourceType.AWSRedshift, new DataSourceCapabilities
                    {
                        SupportsTransactions = true,
                        SupportsJoins = true,
                        SupportsAggregations = true,
                        SupportsIndexes = true,
                        SupportsParameterization = true,
                        SupportsIdentity = true,
                        SupportsTTL = false,
                        SupportsTemporalTables = false,
                        SupportsWindowFunctions = true,
                        SupportsStoredProcedures = true,
                        SupportsBulkOperations = true,
                        SupportsFullTextSearch = false,
                        SupportsNativeJson = true,
                        SupportsPartitioning = true,
                        SupportsReplication = true,
                        SupportsViews = true,
                        SupportsSchemaEvolution = true,
                        IsSchemaEnforced = true,
                        Notes = "AWS data warehouse; SQL-based; excellent for analytics"
                    }
                },
                {
                    DataSourceType.GoogleBigQuery, new DataSourceCapabilities
                    {
                        SupportsTransactions = true,
                        SupportsJoins = true,
                        SupportsAggregations = true,
                        SupportsIndexes = false,
                        SupportsParameterization = true,
                        SupportsIdentity = false,
                        SupportsTTL = false,
                        SupportsTemporalTables = false,
                        SupportsWindowFunctions = true,
                        SupportsStoredProcedures = true,
                        SupportsBulkOperations = true,
                        SupportsFullTextSearch = false,
                        SupportsNativeJson = true,
                        SupportsPartitioning = true,
                        SupportsReplication = false,
                        SupportsViews = true,
                        SupportsSchemaEvolution = true,
                        IsSchemaEnforced = true,
                        Notes = "Google Cloud analytics warehouse; serverless; excellent query performance"
                    }
                },

                #endregion

                #region File-Based & Legacy

                {
                    DataSourceType.FlatFile, new DataSourceCapabilities
                    {
                        SupportsTransactions = false,
                        SupportsJoins = false,
                        SupportsAggregations = false,
                        SupportsIndexes = false,
                        SupportsParameterization = false,
                        SupportsIdentity = false,
                        SupportsTTL = false,
                        SupportsTemporalTables = false,
                        SupportsWindowFunctions = false,
                        SupportsStoredProcedures = false,
                        SupportsBulkOperations = false,
                        SupportsFullTextSearch = false,
                        SupportsNativeJson = false,
                        SupportsPartitioning = false,
                        SupportsReplication = false,
                        SupportsViews = false,
                        SupportsSchemaEvolution = true,
                        IsSchemaEnforced = false,
                        Notes = "Flat text files; limited capabilities; basic read/write"
                    }
                },
                {
                    DataSourceType.CSV, new DataSourceCapabilities
                    {
                        SupportsTransactions = false,
                        SupportsJoins = false,
                        SupportsAggregations = false,
                        SupportsIndexes = false,
                        SupportsParameterization = false,
                        SupportsIdentity = false,
                        SupportsTTL = false,
                        SupportsTemporalTables = false,
                        SupportsWindowFunctions = false,
                        SupportsStoredProcedures = false,
                        SupportsBulkOperations = false,
                        SupportsFullTextSearch = false,
                        SupportsNativeJson = false,
                        SupportsPartitioning = false,
                        SupportsReplication = false,
                        SupportsViews = false,
                        SupportsSchemaEvolution = true,
                        IsSchemaEnforced = false,
                        Notes = "CSV files; comma-separated values; requires parsing"
                    }
                },
                {
                    DataSourceType.Json, new DataSourceCapabilities
                    {
                        SupportsTransactions = false,
                        SupportsJoins = false,
                        SupportsAggregations = false,
                        SupportsIndexes = false,
                        SupportsParameterization = false,
                        SupportsIdentity = false,
                        SupportsTTL = false,
                        SupportsTemporalTables = false,
                        SupportsWindowFunctions = false,
                        SupportsStoredProcedures = false,
                        SupportsBulkOperations = false,
                        SupportsFullTextSearch = false,
                        SupportsNativeJson = true,
                        SupportsPartitioning = false,
                        SupportsReplication = false,
                        SupportsViews = false,
                        SupportsSchemaEvolution = true,
                        IsSchemaEnforced = false,
                        Notes = "JSON files; structured format; requires parsing"
                    }
                },
                {
                    DataSourceType.XML, new DataSourceCapabilities
                    {
                        SupportsTransactions = false,
                        SupportsJoins = false,
                        SupportsAggregations = false,
                        SupportsIndexes = false,
                        SupportsParameterization = false,
                        SupportsIdentity = false,
                        SupportsTTL = false,
                        SupportsTemporalTables = false,
                        SupportsWindowFunctions = false,
                        SupportsStoredProcedures = false,
                        SupportsBulkOperations = false,
                        SupportsFullTextSearch = false,
                        SupportsNativeJson = false,
                        SupportsPartitioning = false,
                        SupportsReplication = false,
                        SupportsViews = false,
                        SupportsSchemaEvolution = true,
                        IsSchemaEnforced = false,
                        Notes = "XML files; hierarchical format; XPath for querying"
                    }
                },

                #endregion

                #region In-Memory

                {
                    DataSourceType.InMemoryCache, new DataSourceCapabilities
                    {
                        SupportsTransactions = false,
                        SupportsJoins = false,
                        SupportsAggregations = false,
                        SupportsIndexes = false,
                        SupportsParameterization = false,
                        SupportsIdentity = false,
                        SupportsTTL = true,
                        SupportsTemporalTables = false,
                        SupportsWindowFunctions = false,
                        SupportsStoredProcedures = false,
                        SupportsBulkOperations = false,
                        SupportsFullTextSearch = false,
                        SupportsNativeJson = false,
                        SupportsPartitioning = false,
                        SupportsReplication = false,
                        SupportsViews = false,
                        SupportsSchemaEvolution = false,
                        IsSchemaEnforced = false,
                        Notes = "In-memory objects; temporary storage; TTL via eviction"
                    }
                },

                #endregion

                #region REST APIs & Web Services

                {
                    DataSourceType.RestApi, new DataSourceCapabilities
                    {
                        SupportsTransactions = false,
                        SupportsJoins = false,
                        SupportsAggregations = false,
                        SupportsIndexes = false,
                        SupportsParameterization = false,
                        SupportsIdentity = false,
                        SupportsTTL = false,
                        SupportsTemporalTables = false,
                        SupportsWindowFunctions = false,
                        SupportsStoredProcedures = false,
                        SupportsBulkOperations = false,
                        SupportsFullTextSearch = false,
                        SupportsNativeJson = true,
                        SupportsPartitioning = false,
                        SupportsReplication = false,
                        SupportsViews = false,
                        SupportsSchemaEvolution = false,
                        IsSchemaEnforced = false,
                        Notes = "Generic REST API; limited to HTTP operations; server-side capabilities vary"
                    }
                },
                {
                    DataSourceType.GraphQL, new DataSourceCapabilities
                    {
                        SupportsTransactions = false,
                        SupportsJoins = false,
                        SupportsAggregations = false,
                        SupportsIndexes = false,
                        SupportsParameterization = true,
                        SupportsIdentity = false,
                        SupportsTTL = false,
                        SupportsTemporalTables = false,
                        SupportsWindowFunctions = false,
                        SupportsStoredProcedures = false,
                        SupportsBulkOperations = false,
                        SupportsFullTextSearch = false,
                        SupportsNativeJson = true,
                        SupportsPartitioning = false,
                        SupportsReplication = false,
                        SupportsViews = false,
                        SupportsSchemaEvolution = false,
                        IsSchemaEnforced = false,
                        Notes = "GraphQL queries; flexible schema negotiation; client-defined queries"
                    }
                },
                {
                    DataSourceType.OData, new DataSourceCapabilities
                    {
                        SupportsTransactions = false,
                        SupportsJoins = false,
                        SupportsAggregations = true,
                        SupportsIndexes = false,
                        SupportsParameterization = true,
                        SupportsIdentity = false,
                        SupportsTTL = false,
                        SupportsTemporalTables = false,
                        SupportsWindowFunctions = false,
                        SupportsStoredProcedures = false,
                        SupportsBulkOperations = false,
                        SupportsFullTextSearch = false,
                        SupportsNativeJson = true,
                        SupportsPartitioning = false,
                        SupportsReplication = false,
                        SupportsViews = false,
                        SupportsSchemaEvolution = false,
                        IsSchemaEnforced = false,
                        Notes = "Open Data Protocol; filtering, sorting, aggregations supported"
                    }
                },

                #endregion

                #region Vector Databases

                {
                    DataSourceType.ChromaDB, new DataSourceCapabilities
                    {
                        SupportsVectorSearch = true,
                        SupportsEmbeddings = true,
                        SupportsANN = true,
                        SupportsIndexes = true,
                        SupportsNativeJson = true,
                        SupportsBulkOperations = true,
                        SupportsAsyncOperations = true,
                        Notes = "Open-source embedding database; HNSW indexing; Python-native"
                    }
                },
                {
                    DataSourceType.PineCone, new DataSourceCapabilities
                    {
                        SupportsVectorSearch = true,
                        SupportsEmbeddings = true,
                        SupportsANN = true,
                        SupportsIndexes = true,
                        SupportsNativeJson = true,
                        SupportsBulkOperations = true,
                        SupportsAsyncOperations = true,
                        SupportsReplication = true,
                        SupportsPartitioning = true,
                        SupportsOAuth = true,
                        Notes = "Managed vector database; serverless; metadata filtering"
                    }
                },
                {
                    DataSourceType.Milvus, new DataSourceCapabilities
                    {
                        SupportsVectorSearch = true,
                        SupportsEmbeddings = true,
                        SupportsANN = true,
                        SupportsIndexes = true,
                        SupportsNativeJson = true,
                        SupportsBulkOperations = true,
                        SupportsAsyncOperations = true,
                        SupportsPartitioning = true,
                        SupportsReplication = true,
                        Notes = "Open-source vector DB; supports IVF, HNSW, DISKANN; GPU acceleration"
                    }
                },
                {
                    DataSourceType.Weaviate, new DataSourceCapabilities
                    {
                        SupportsVectorSearch = true,
                        SupportsEmbeddings = true,
                        SupportsANN = true,
                        SupportsIndexes = true,
                        SupportsNativeJson = true,
                        SupportsBulkOperations = true,
                        SupportsAsyncOperations = true,
                        SupportsFullTextSearch = true,
                        SupportsOAuth = true,
                        Notes = "Vector + keyword hybrid search; GraphQL API; multi-modal"
                    }
                },
                {
                    DataSourceType.Qdrant, new DataSourceCapabilities
                    {
                        SupportsVectorSearch = true,
                        SupportsEmbeddings = true,
                        SupportsANN = true,
                        SupportsIndexes = true,
                        SupportsNativeJson = true,
                        SupportsBulkOperations = true,
                        SupportsAsyncOperations = true,
                        SupportsPartitioning = true,
                        Notes = "High-performance vector DB; payload filtering; Rust-based"
                    }
                },
                {
                    DataSourceType.ShapVector, new DataSourceCapabilities
                    {
                        SupportsVectorSearch = true,
                        SupportsEmbeddings = true,
                        SupportsANN = true,
                        SupportsNativeJson = true,
                        Notes = "Lightweight vector search library"
                    }
                },
                {
                    DataSourceType.RedisVector, new DataSourceCapabilities
                    {
                        SupportsVectorSearch = true,
                        SupportsEmbeddings = true,
                        SupportsANN = true,
                        SupportsIndexes = true,
                        SupportsTTL = true,
                        SupportsBulkOperations = true,
                        SupportsAsyncOperations = true,
                        SupportsReplication = true,
                        Notes = "Redis Stack with RediSearch; vector + JSON + full-text"
                    }
                },
                {
                    DataSourceType.Zilliz, new DataSourceCapabilities
                    {
                        SupportsVectorSearch = true,
                        SupportsEmbeddings = true,
                        SupportsANN = true,
                        SupportsIndexes = true,
                        SupportsNativeJson = true,
                        SupportsBulkOperations = true,
                        SupportsAsyncOperations = true,
                        SupportsPartitioning = true,
                        SupportsReplication = true,
                        SupportsOAuth = true,
                        Notes = "Managed Milvus cloud service; enterprise features"
                    }
                },
                {
                    DataSourceType.Vespa, new DataSourceCapabilities
                    {
                        SupportsVectorSearch = true,
                        SupportsEmbeddings = true,
                        SupportsANN = true,
                        SupportsIndexes = true,
                        SupportsNativeJson = true,
                        SupportsBulkOperations = true,
                        SupportsAsyncOperations = true,
                        SupportsFullTextSearch = true,
                        SupportsPartitioning = true,
                        SupportsReplication = true,
                        Notes = "Yahoo's search platform; combines vector + keyword + ML ranking"
                    }
                },

                #endregion

                #region Graph Databases

                {
                    DataSourceType.Neo4j, new DataSourceCapabilities
                    {
                        SupportsGraphTraversal = true,
                        SupportsCypherQuery = true,
                        SupportsTransactions = true,
                        SupportsIndexes = true,
                        SupportsAggregations = true,
                        SupportsParameterization = true,
                        SupportsBulkOperations = true,
                        SupportsFullTextSearch = true,
                        SupportsNativeJson = true,
                        SupportsAsyncOperations = true,
                        Notes = "Leading graph database; Cypher query language; ACID compliant"
                    }
                },
                {
                    DataSourceType.ArangoDB, new DataSourceCapabilities
                    {
                        SupportsGraphTraversal = true,
                        SupportsGremlinQuery = false,
                        SupportsTransactions = true,
                        SupportsJoins = true,
                        SupportsIndexes = true,
                        SupportsAggregations = true,
                        SupportsParameterization = true,
                        SupportsBulkOperations = true,
                        SupportsFullTextSearch = true,
                        SupportsNativeJson = true,
                        SupportsAsyncOperations = true,
                        SupportsReplication = true,
                        Notes = "Multi-model DB (graph + document + key-value); AQL language"
                    }
                },
                {
                    DataSourceType.TigerGraph, new DataSourceCapabilities
                    {
                        SupportsGraphTraversal = true,
                        SupportsTransactions = true,
                        SupportsIndexes = true,
                        SupportsAggregations = true,
                        SupportsParameterization = true,
                        SupportsBulkOperations = true,
                        SupportsPartitioning = true,
                        SupportsReplication = true,
                        SupportsAsyncOperations = true,
                        Notes = "Enterprise graph analytics; GSQL language; parallel processing"
                    }
                },
                {
                    DataSourceType.JanusGraph, new DataSourceCapabilities
                    {
                        SupportsGraphTraversal = true,
                        SupportsGremlinQuery = true,
                        SupportsTransactions = true,
                        SupportsIndexes = true,
                        SupportsAggregations = true,
                        SupportsBulkOperations = true,
                        SupportsPartitioning = true,
                        SupportsReplication = true,
                        Notes = "Distributed graph DB; pluggable storage (Cassandra, HBase); Gremlin"
                    }
                },

                #endregion

                #region Time Series Databases

                {
                    DataSourceType.InfluxDB, new DataSourceCapabilities
                    {
                        SupportsTimeSeriesQueries = true,
                        SupportsDownsampling = true,
                        SupportsRetentionPolicies = true,
                        SupportsContinuousQueries = true,
                        SupportsAggregations = true,
                        SupportsIndexes = true,
                        SupportsBulkOperations = true,
                        SupportsAsyncOperations = true,
                        SupportsStreaming = true,
                        Notes = "Purpose-built time series DB; Flux query language; high ingest rate"
                    }
                },
                {
                    DataSourceType.TimeScale, new DataSourceCapabilities
                    {
                        SupportsTimeSeriesQueries = true,
                        SupportsDownsampling = true,
                        SupportsRetentionPolicies = true,
                        SupportsContinuousQueries = true,
                        SupportsTransactions = true,
                        SupportsJoins = true,
                        SupportsAggregations = true,
                        SupportsIndexes = true,
                        SupportsParameterization = true,
                        SupportsWindowFunctions = true,
                        SupportsBulkOperations = true,
                        SupportsPartitioning = true,
                        SupportsReplication = true,
                        Notes = "PostgreSQL extension for time series; full SQL support; hypertables"
                    }
                },
                {
                    DataSourceType.Druid, new DataSourceCapabilities
                    {
                        SupportsTimeSeriesQueries = true,
                        SupportsDownsampling = true,
                        SupportsAggregations = true,
                        SupportsIndexes = true,
                        SupportsBulkOperations = true,
                        SupportsPartitioning = true,
                        SupportsReplication = true,
                        SupportsStreaming = true,
                        Notes = "Real-time analytics DB; OLAP queries; column-oriented"
                    }
                },

                #endregion

                #region Message Queues and Streaming

                {
                    DataSourceType.Kafka, new DataSourceCapabilities
                    {
                        SupportsStreaming = true,
                        SupportsPublishSubscribe = true,
                        SupportsMessageOrdering = true,
                        SupportsDeadLetterQueue = true,
                        SupportsAppend = true,
                        SupportsPartitioning = true,
                        SupportsReplication = true,
                        SupportsBulkOperations = true,
                        SupportsAsyncOperations = true,
                        SupportsContinuousQueries = true,
                        Notes = "Distributed event streaming; high throughput; exactly-once semantics"
                    }
                },
                {
                    DataSourceType.RabbitMQ, new DataSourceCapabilities
                    {
                        SupportsPublishSubscribe = true,
                        SupportsMessageOrdering = true,
                        SupportsDeadLetterQueue = true,
                        SupportsAsyncOperations = true,
                        SupportsReplication = true,
                        Notes = "AMQP message broker; flexible routing; acknowledgments"
                    }
                },
                {
                    DataSourceType.ActiveMQ, new DataSourceCapabilities
                    {
                        SupportsPublishSubscribe = true,
                        SupportsMessageOrdering = true,
                        SupportsDeadLetterQueue = true,
                        SupportsAsyncOperations = true,
                        SupportsTransactions = true,
                        Notes = "JMS-compliant message broker; multiple protocols"
                    }
                },
                {
                    DataSourceType.Pulsar, new DataSourceCapabilities
                    {
                        SupportsStreaming = true,
                        SupportsPublishSubscribe = true,
                        SupportsMessageOrdering = true,
                        SupportsDeadLetterQueue = true,
                        SupportsPartitioning = true,
                        SupportsReplication = true,
                        SupportsBulkOperations = true,
                        SupportsAsyncOperations = true,
                        SupportsTTL = true,
                        Notes = "Multi-tenant messaging; tiered storage; Kafka-like with enhancements"
                    }
                },
                {
                    DataSourceType.Nats, new DataSourceCapabilities
                    {
                        SupportsPublishSubscribe = true,
                        SupportsStreaming = true,
                        SupportsAsyncOperations = true,
                        SupportsReplication = true,
                        Notes = "Lightweight cloud-native messaging; JetStream for persistence"
                    }
                },
                {
                    DataSourceType.ZeroMQ, new DataSourceCapabilities
                    {
                        SupportsPublishSubscribe = true,
                        SupportsAsyncOperations = true,
                        SupportsBinaryProtocol = true,
                        Notes = "High-performance async messaging library; socket-like API"
                    }
                },
                {
                    DataSourceType.AWSKinesis, new DataSourceCapabilities
                    {
                        SupportsStreaming = true,
                        SupportsPublishSubscribe = true,
                        SupportsMessageOrdering = true,
                        SupportsPartitioning = true,
                        SupportsReplication = true,
                        SupportsBulkOperations = true,
                        SupportsAsyncOperations = true,
                        SupportsOAuth = true,
                        Notes = "AWS managed streaming; Kinesis Data Streams + Firehose"
                    }
                },
                {
                    DataSourceType.AWSSQS, new DataSourceCapabilities
                    {
                        SupportsPublishSubscribe = true,
                        SupportsDeadLetterQueue = true,
                        SupportsAsyncOperations = true,
                        SupportsTTL = true,
                        SupportsOAuth = true,
                        Notes = "AWS managed queue service; standard + FIFO queues"
                    }
                },
                {
                    DataSourceType.AWSSNS, new DataSourceCapabilities
                    {
                        SupportsPublishSubscribe = true,
                        SupportsPushNotifications = true,
                        SupportsAsyncOperations = true,
                        SupportsOAuth = true,
                        Notes = "AWS notification service; fan-out messaging; mobile push"
                    }
                },
                {
                    DataSourceType.AzureServiceBus, new DataSourceCapabilities
                    {
                        SupportsPublishSubscribe = true,
                        SupportsMessageOrdering = true,
                        SupportsDeadLetterQueue = true,
                        SupportsTransactions = true,
                        SupportsAsyncOperations = true,
                        SupportsTTL = true,
                        SupportsOAuth = true,
                        Notes = "Azure enterprise messaging; queues + topics; sessions"
                    }
                },
                {
                    DataSourceType.MassTransit, new DataSourceCapabilities
                    {
                        SupportsPublishSubscribe = true,
                        SupportsMessageOrdering = true,
                        SupportsAsyncOperations = true,
                        Notes = ".NET message bus abstraction; supports multiple transports"
                    }
                },

                #endregion

                #region Big Data and Columnar

                {
                    DataSourceType.DuckDB, new DataSourceCapabilities
                    {
                        SupportsTransactions = true,
                        SupportsJoins = true,
                        SupportsAggregations = true,
                        SupportsIndexes = true,
                        SupportsParameterization = true,
                        SupportsWindowFunctions = true,
                        SupportsBulkOperations = true,
                        SupportsNativeJson = true,
                        SupportsViews = true,
                        Notes = "In-process OLAP database; Parquet/CSV native; vectorized execution"
                    }
                },
                {
                    DataSourceType.Hadoop, new DataSourceCapabilities
                    {
                        SupportsBulkOperations = true,
                        SupportsPartitioning = true,
                        SupportsReplication = true,
                        SupportsAppend = true,
                        SupportsStreaming = true,
                        Notes = "Distributed file system + MapReduce; batch processing"
                    }
                },
                {
                    DataSourceType.Parquet, new DataSourceCapabilities
                    {
                        SupportsAggregations = true,
                        SupportsBulkOperations = true,
                        SupportsPartitioning = true,
                        SupportsNativeJson = false,
                        IsSchemaEnforced = true,
                        Notes = "Columnar file format; compression; predicate pushdown"
                    }
                },
                {
                    DataSourceType.Avro, new DataSourceCapabilities
                    {
                        SupportsBulkOperations = true,
                        SupportsSchemaEvolution = true,
                        IsSchemaEnforced = true,
                        SupportsNativeJson = true,
                        Notes = "Row-based format with schema; compact binary; schema registry"
                    }
                },
                {
                    DataSourceType.ORC, new DataSourceCapabilities
                    {
                        SupportsAggregations = true,
                        SupportsBulkOperations = true,
                        SupportsPartitioning = true,
                        SupportsIndexes = true,
                        IsSchemaEnforced = true,
                        Notes = "Optimized Row Columnar; Hive native; ACID in Hive 3+"
                    }
                },
                {
                    DataSourceType.Feather, new DataSourceCapabilities
                    {
                        SupportsBulkOperations = true,
                        IsSchemaEnforced = true,
                        Notes = "Fast columnar format; Arrow-based; language interop"
                    }
                },
                {
                    DataSourceType.Kudu, new DataSourceCapabilities
                    {
                        SupportsTransactions = false,
                        SupportsJoins = true,
                        SupportsAggregations = true,
                        SupportsIndexes = true,
                        SupportsBulkOperations = true,
                        SupportsPartitioning = true,
                        SupportsReplication = true,
                        Notes = "Hadoop-native columnar store; real-time analytics + fast scans"
                    }
                },
                {
                    DataSourceType.Pinot, new DataSourceCapabilities
                    {
                        SupportsAggregations = true,
                        SupportsIndexes = true,
                        SupportsBulkOperations = true,
                        SupportsPartitioning = true,
                        SupportsReplication = true,
                        SupportsStreaming = true,
                        Notes = "Real-time OLAP; LinkedIn-developed; low-latency analytics"
                    }
                },
                {
                    DataSourceType.ClickHouse, new DataSourceCapabilities
                    {
                        SupportsJoins = true,
                        SupportsAggregations = true,
                        SupportsIndexes = true,
                        SupportsParameterization = true,
                        SupportsBulkOperations = true,
                        SupportsPartitioning = true,
                        SupportsReplication = true,
                        SupportsViews = true,
                        SupportsWindowFunctions = true,
                        Notes = "Column-oriented OLAP DB; extremely fast aggregations; SQL-like"
                    }
                },

                #endregion

                #region File Formats

                {
                    DataSourceType.CSV, new DataSourceCapabilities
                    {
                        SupportsBulkOperations = true,
                        SupportsAppend = true,
                        SupportsStreaming = true,
                        IsSchemaEnforced = false,
                        Notes = "Comma-separated values; universal compatibility; no schema"
                    }
                },
                {
                    DataSourceType.TSV, new DataSourceCapabilities
                    {
                        SupportsBulkOperations = true,
                        SupportsAppend = true,
                        SupportsStreaming = true,
                        IsSchemaEnforced = false,
                        Notes = "Tab-separated values; handles commas in data"
                    }
                },
                {
                    DataSourceType.Text, new DataSourceCapabilities
                    {
                        SupportsAppend = true,
                        SupportsStreaming = true,
                        IsSchemaEnforced = false,
                        Notes = "Plain text files; line-based processing"
                    }
                },
                {
                    DataSourceType.YAML, new DataSourceCapabilities
                    {
                        SupportsNativeJson = false,
                        SupportsSchemaEvolution = true,
                        IsSchemaEnforced = false,
                        Notes = "Human-readable data serialization; config files"
                    }
                },
                {
                    DataSourceType.Markdown, new DataSourceCapabilities
                    {
                        SupportsAppend = true,
                        IsSchemaEnforced = false,
                        Notes = "Formatted text; documentation; limited data structure"
                    }
                },
                {
                    DataSourceType.Log, new DataSourceCapabilities
                    {
                        SupportsAppend = true,
                        SupportsStreaming = true,
                        IsSchemaEnforced = false,
                        Notes = "Log file format; append-only; timestamped entries"
                    }
                },
                {
                    DataSourceType.INI, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = false,
                        Notes = "INI configuration format; section-based key-value"
                    }
                },
                {
                    DataSourceType.Xls, new DataSourceCapabilities
                    {
                        SupportsBulkOperations = true,
                        IsSchemaEnforced = false,
                        Notes = "Legacy Excel format; BIFF8; limited to 65536 rows"
                    }
                },
                {
                    DataSourceType.FlatFile, new DataSourceCapabilities
                    {
                        SupportsBulkOperations = true,
                        SupportsAppend = true,
                        IsSchemaEnforced = false,
                        Notes = "Fixed-width or delimited flat files"
                    }
                },
                {
                    DataSourceType.Hdf5, new DataSourceCapabilities
                    {
                        SupportsBulkOperations = true,
                        SupportsPartitioning = true,
                        IsSchemaEnforced = true,
                        Notes = "Hierarchical Data Format; scientific data; multi-dimensional arrays"
                    }
                },
                {
                    DataSourceType.LibSVM, new DataSourceCapabilities
                    {
                        SupportsBulkOperations = true,
                        IsSchemaEnforced = true,
                        Notes = "Sparse feature format for ML; label:feature:value"
                    }
                },
                {
                    DataSourceType.GraphML, new DataSourceCapabilities
                    {
                        SupportsGraphTraversal = true,
                        IsSchemaEnforced = true,
                        Notes = "XML-based graph format; nodes + edges + attributes"
                    }
                },
                {
                    DataSourceType.DICOM, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        Notes = "Medical imaging format; metadata + pixel data"
                    }
                },
                {
                    DataSourceType.LAS, new DataSourceCapabilities
                    {
                        SupportsBulkOperations = true,
                        IsSchemaEnforced = true,
                        Notes = "LiDAR point cloud format; geospatial data"
                    }
                },

                #endregion

                #region Protocols

                {
                    DataSourceType.FTP, new DataSourceCapabilities
                    {
                        SupportsStreaming = true,
                        SupportsAppend = true,
                        SupportsAsyncOperations = true,
                        Notes = "File Transfer Protocol; unencrypted; legacy systems"
                    }
                },
                {
                    DataSourceType.SFTP, new DataSourceCapabilities
                    {
                        SupportsStreaming = true,
                        SupportsAppend = true,
                        SupportsAsyncOperations = true,
                        Notes = "SSH File Transfer Protocol; encrypted; key-based auth"
                    }
                },
                {
                    DataSourceType.IMAP, new DataSourceCapabilities
                    {
                        SupportsAsyncOperations = true,
                        SupportsOAuth = true,
                        Notes = "Internet Message Access Protocol; email retrieval"
                    }
                },
                {
                    DataSourceType.POP3, new DataSourceCapabilities
                    {
                        SupportsAsyncOperations = true,
                        Notes = "Post Office Protocol v3; download + delete email"
                    }
                },
                {
                    DataSourceType.SMTP, new DataSourceCapabilities
                    {
                        SupportsAsyncOperations = true,
                        SupportsOAuth = true,
                        Notes = "Simple Mail Transfer Protocol; sending email"
                    }
                },
                {
                    DataSourceType.Email, new DataSourceCapabilities
                    {
                        SupportsAsyncOperations = true,
                        SupportsOAuth = true,
                        Notes = "Generic email connector; combines IMAP/POP3/SMTP"
                    }
                },
                {
                    DataSourceType.GRPC, new DataSourceCapabilities
                    {
                        SupportsStreaming = true,
                        SupportsBinaryProtocol = true,
                        SupportsAsyncOperations = true,
                        SupportsOAuth = true,
                        Notes = "gRPC binary RPC protocol; HTTP/2; protobuf messages"
                    }
                },
                {
                    DataSourceType.WebSocket, new DataSourceCapabilities
                    {
                        SupportsStreaming = true,
                        SupportsPushNotifications = true,
                        SupportsAsyncOperations = true,
                        SupportsOAuth = true,
                        Notes = "Full-duplex communication; real-time bidirectional"
                    }
                },
                {
                    DataSourceType.SSE, new DataSourceCapabilities
                    {
                        SupportsStreaming = true,
                        SupportsPushNotifications = true,
                        SupportsAsyncOperations = true,
                        Notes = "Server-Sent Events; unidirectional server-to-client"
                    }
                },
                {
                    DataSourceType.SOAP, new DataSourceCapabilities
                    {
                        SupportsAsyncOperations = true,
                        SupportsTransactions = false,
                        Notes = "Simple Object Access Protocol; XML-based; WS-* standards"
                    }
                },
                {
                    DataSourceType.XMLRPC, new DataSourceCapabilities
                    {
                        SupportsAsyncOperations = true,
                        Notes = "XML-RPC protocol; simple remote procedure calls"
                    }
                },
                {
                    DataSourceType.JSONRPC, new DataSourceCapabilities
                    {
                        SupportsAsyncOperations = true,
                        SupportsNativeJson = true,
                        Notes = "JSON-RPC protocol; lightweight RPC; JSON encoding"
                    }
                },

                #endregion

                #region Remaining RDBMS

                {
                    DataSourceType.FireBird, new DataSourceCapabilities
                    {
                        SupportsTransactions = true,
                        SupportsJoins = true,
                        SupportsAggregations = true,
                        SupportsIndexes = true,
                        SupportsParameterization = true,
                        SupportsIdentity = true,
                        SupportsStoredProcedures = true,
                        SupportsViews = true,
                        IsSchemaEnforced = true,
                        Notes = "Open-source RDBMS; embedded or server mode"
                    }
                },
                {
                    DataSourceType.DB2, new DataSourceCapabilities
                    {
                        SupportsTransactions = true,
                        SupportsJoins = true,
                        SupportsAggregations = true,
                        SupportsIndexes = true,
                        SupportsParameterization = true,
                        SupportsIdentity = true,
                        SupportsTemporalTables = true,
                        SupportsWindowFunctions = true,
                        SupportsStoredProcedures = true,
                        SupportsBulkOperations = true,
                        SupportsPartitioning = true,
                        SupportsReplication = true,
                        SupportsViews = true,
                        IsSchemaEnforced = true,
                        Notes = "IBM enterprise RDBMS; z/OS + distributed"
                    }
                },
                {
                    DataSourceType.Hana, new DataSourceCapabilities
                    {
                        SupportsTransactions = true,
                        SupportsJoins = true,
                        SupportsAggregations = true,
                        SupportsIndexes = true,
                        SupportsParameterization = true,
                        SupportsIdentity = true,
                        SupportsWindowFunctions = true,
                        SupportsStoredProcedures = true,
                        SupportsBulkOperations = true,
                        SupportsFullTextSearch = true,
                        SupportsNativeJson = true,
                        SupportsPartitioning = true,
                        SupportsReplication = true,
                        SupportsViews = true,
                        IsSchemaEnforced = true,
                        Notes = "SAP in-memory DB; column + row store; analytics + transactions"
                    }
                },
                {
                    DataSourceType.Cockroach, new DataSourceCapabilities
                    {
                        SupportsTransactions = true,
                        SupportsJoins = true,
                        SupportsAggregations = true,
                        SupportsIndexes = true,
                        SupportsParameterization = true,
                        SupportsIdentity = true,
                        SupportsWindowFunctions = true,
                        SupportsBulkOperations = true,
                        SupportsNativeJson = true,
                        SupportsPartitioning = true,
                        SupportsReplication = true,
                        SupportsViews = true,
                        SupportsSchemaEvolution = true,
                        IsSchemaEnforced = true,
                        Notes = "Distributed SQL; PostgreSQL wire protocol; geo-partitioning"
                    }
                },
                {
                    DataSourceType.Spanner, new DataSourceCapabilities
                    {
                        SupportsTransactions = true,
                        SupportsJoins = true,
                        SupportsAggregations = true,
                        SupportsIndexes = true,
                        SupportsParameterization = true,
                        SupportsIdentity = true,
                        SupportsBulkOperations = true,
                        SupportsPartitioning = true,
                        SupportsReplication = true,
                        SupportsViews = true,
                        IsSchemaEnforced = true,
                        SupportsOAuth = true,
                        Notes = "Google Cloud globally distributed DB; external consistency"
                    }
                },
                {
                    DataSourceType.TerraData, new DataSourceCapabilities
                    {
                        SupportsTransactions = true,
                        SupportsJoins = true,
                        SupportsAggregations = true,
                        SupportsIndexes = true,
                        SupportsParameterization = true,
                        SupportsWindowFunctions = true,
                        SupportsStoredProcedures = true,
                        SupportsBulkOperations = true,
                        SupportsPartitioning = true,
                        SupportsReplication = true,
                        SupportsViews = true,
                        IsSchemaEnforced = true,
                        Notes = "Enterprise data warehouse; MPP architecture"
                    }
                },
                {
                    DataSourceType.Vertica, new DataSourceCapabilities
                    {
                        SupportsTransactions = true,
                        SupportsJoins = true,
                        SupportsAggregations = true,
                        SupportsIndexes = true,
                        SupportsParameterization = true,
                        SupportsWindowFunctions = true,
                        SupportsBulkOperations = true,
                        SupportsFullTextSearch = true,
                        SupportsPartitioning = true,
                        SupportsReplication = true,
                        SupportsViews = true,
                        IsSchemaEnforced = true,
                        Notes = "Column-oriented analytics DB; MPP; machine learning built-in"
                    }
                },
                {
                    DataSourceType.MariaDB, new DataSourceCapabilities
                    {
                        SupportsTransactions = true,
                        SupportsJoins = true,
                        SupportsAggregations = true,
                        SupportsIndexes = true,
                        SupportsParameterization = true,
                        SupportsIdentity = true,
                        SupportsTemporalTables = true,
                        SupportsWindowFunctions = true,
                        SupportsStoredProcedures = true,
                        SupportsBulkOperations = true,
                        SupportsFullTextSearch = true,
                        SupportsNativeJson = true,
                        SupportsPartitioning = true,
                        SupportsReplication = true,
                        SupportsViews = true,
                        IsSchemaEnforced = true,
                        Notes = "MySQL fork; additional features; Galera clustering"
                    }
                },
                {
                    DataSourceType.SqlCompact, new DataSourceCapabilities
                    {
                        SupportsTransactions = true,
                        SupportsJoins = true,
                        SupportsAggregations = true,
                        SupportsIndexes = true,
                        SupportsParameterization = true,
                        SupportsIdentity = true,
                        SupportsViews = false,
                        IsSchemaEnforced = true,
                        Notes = "Microsoft SQL CE; embedded; discontinued"
                    }
                },

                #endregion

                #region In-Memory Databases

                {
                    DataSourceType.Memcached, new DataSourceCapabilities
                    {
                        SupportsTTL = true,
                        SupportsBulkOperations = true,
                        SupportsAsyncOperations = true,
                        SupportsReplication = false,
                        Notes = "Distributed memory cache; simple key-value; no persistence"
                    }
                },
                {
                    DataSourceType.GridGain, new DataSourceCapabilities
                    {
                        SupportsTransactions = true,
                        SupportsJoins = true,
                        SupportsAggregations = true,
                        SupportsIndexes = true,
                        SupportsBulkOperations = true,
                        SupportsPartitioning = true,
                        SupportsReplication = true,
                        SupportsAsyncOperations = true,
                        Notes = "Apache Ignite commercial; distributed in-memory computing"
                    }
                },
                {
                    DataSourceType.Hazelcast, new DataSourceCapabilities
                    {
                        SupportsTransactions = true,
                        SupportsTTL = true,
                        SupportsBulkOperations = true,
                        SupportsPartitioning = true,
                        SupportsReplication = true,
                        SupportsAsyncOperations = true,
                        SupportsStreaming = true,
                        Notes = "In-memory data grid; caching + computing + streaming"
                    }
                },
                {
                    DataSourceType.ApacheIgnite, new DataSourceCapabilities
                    {
                        SupportsTransactions = true,
                        SupportsJoins = true,
                        SupportsAggregations = true,
                        SupportsIndexes = true,
                        SupportsBulkOperations = true,
                        SupportsPartitioning = true,
                        SupportsReplication = true,
                        SupportsAsyncOperations = true,
                        Notes = "Distributed in-memory platform; SQL + key-value + compute"
                    }
                },
                {
                    DataSourceType.ChronicleMap, new DataSourceCapabilities
                    {
                        SupportsBulkOperations = true,
                        SupportsAsyncOperations = true,
                        Notes = "Off-heap in-memory map; low latency; memory-mapped files"
                    }
                },
                {
                    DataSourceType.H2Database, new DataSourceCapabilities
                    {
                        SupportsTransactions = true,
                        SupportsJoins = true,
                        SupportsAggregations = true,
                        SupportsIndexes = true,
                        SupportsParameterization = true,
                        SupportsIdentity = true,
                        SupportsWindowFunctions = true,
                        SupportsStoredProcedures = true,
                        SupportsViews = true,
                        IsSchemaEnforced = true,
                        Notes = "Java in-memory DB; embedded mode; PostgreSQL compatibility"
                    }
                },
                {
                    DataSourceType.RealIM, new DataSourceCapabilities
                    {
                        SupportsBulkOperations = true,
                        SupportsAsyncOperations = true,
                        Notes = "Real-time in-memory data platform"
                    }
                },
                {
                    DataSourceType.InMemoryCache, new DataSourceCapabilities
                    {
                        SupportsTTL = true,
                        SupportsBulkOperations = true,
                        SupportsAsyncOperations = true,
                        Notes = "Generic in-memory cache; application-level caching"
                    }
                },
                {
                    DataSourceType.CachedMemory, new DataSourceCapabilities
                    {
                        SupportsTTL = true,
                        SupportsBulkOperations = true,
                        SupportsAsyncOperations = true,
                        Notes = "Cached memory store; temporary data storage"
                    }
                },

                #endregion

                #region Additional NoSQL

                {
                    DataSourceType.CouchDB, new DataSourceCapabilities
                    {
                        SupportsNativeJson = true,
                        SupportsBulkOperations = true,
                        SupportsReplication = true,
                        SupportsViews = true,
                        SupportsAsyncOperations = true,
                        Notes = "Document DB with MVCC; CouchDB protocol; offline-first"
                    }
                },
                {
                    DataSourceType.RavenDB, new DataSourceCapabilities
                    {
                        SupportsTransactions = true,
                        SupportsIndexes = true,
                        SupportsAggregations = true,
                        SupportsNativeJson = true,
                        SupportsBulkOperations = true,
                        SupportsFullTextSearch = true,
                        SupportsReplication = true,
                        SupportsAsyncOperations = true,
                        Notes = ".NET-native document DB; ACID; LINQ queries"
                    }
                },
                {
                    DataSourceType.Couchbase, new DataSourceCapabilities
                    {
                        SupportsTransactions = true,
                        SupportsJoins = true,
                        SupportsAggregations = true,
                        SupportsIndexes = true,
                        SupportsNativeJson = true,
                        SupportsBulkOperations = true,
                        SupportsFullTextSearch = true,
                        SupportsTTL = true,
                        SupportsReplication = true,
                        SupportsAsyncOperations = true,
                        Notes = "Document + key-value DB; N1QL SQL-like queries; mobile sync"
                    }
                },
                {
                    DataSourceType.DynamoDB, new DataSourceCapabilities
                    {
                        SupportsTransactions = true,
                        SupportsIndexes = true,
                        SupportsAggregations = false,
                        SupportsNativeJson = true,
                        SupportsBulkOperations = true,
                        SupportsTTL = true,
                        SupportsPartitioning = true,
                        SupportsReplication = true,
                        SupportsAsyncOperations = true,
                        SupportsOAuth = true,
                        SupportsStreaming = true,
                        Notes = "AWS managed NoSQL; single-digit ms latency; DynamoDB Streams"
                    }
                },
                {
                    DataSourceType.Firebase, new DataSourceCapabilities
                    {
                        SupportsNativeJson = true,
                        SupportsBulkOperations = true,
                        SupportsReplication = true,
                        SupportsPushNotifications = true,
                        SupportsAsyncOperations = true,
                        SupportsOAuth = true,
                        Notes = "Google real-time DB; Firestore + Realtime DB; mobile-first"
                    }
                },
                {
                    DataSourceType.LiteDB, new DataSourceCapabilities
                    {
                        SupportsTransactions = true,
                        SupportsIndexes = true,
                        SupportsNativeJson = true,
                        SupportsBulkOperations = true,
                        Notes = "Embedded .NET NoSQL; single file; serverless"
                    }
                },
                {
                    DataSourceType.OrientDB, new DataSourceCapabilities
                    {
                        SupportsTransactions = true,
                        SupportsGraphTraversal = true,
                        SupportsJoins = true,
                        SupportsAggregations = true,
                        SupportsIndexes = true,
                        SupportsNativeJson = true,
                        SupportsBulkOperations = true,
                        SupportsReplication = true,
                        Notes = "Multi-model: document + graph + key-value + object"
                    }
                },

                #endregion

                #region Search Engines

                {
                    DataSourceType.Solr, new DataSourceCapabilities
                    {
                        SupportsFullTextSearch = true,
                        SupportsFacetedSearch = true,
                        SupportsHighlighting = true,
                        SupportsAggregations = true,
                        SupportsIndexes = true,
                        SupportsBulkOperations = true,
                        SupportsReplication = true,
                        SupportsPartitioning = true,
                        SupportsNativeJson = true,
                        SupportsAsyncOperations = true,
                        Notes = "Apache Lucene-based; enterprise search; SolrCloud"
                    }
                },

                #endregion

                #region Cloud Data Warehouses

                {
                    DataSourceType.AWSRedshift, new DataSourceCapabilities
                    {
                        SupportsTransactions = true,
                        SupportsJoins = true,
                        SupportsAggregations = true,
                        SupportsIndexes = true,
                        SupportsParameterization = true,
                        SupportsWindowFunctions = true,
                        SupportsBulkOperations = true,
                        SupportsPartitioning = true,
                        SupportsReplication = true,
                        SupportsViews = true,
                        SupportsOAuth = true,
                        Notes = "AWS columnar data warehouse; PostgreSQL-based; Spectrum for S3"
                    }
                },
                {
                    DataSourceType.AWSGlue, new DataSourceCapabilities
                    {
                        SupportsBulkOperations = true,
                        SupportsPartitioning = true,
                        SupportsSchemaEvolution = true,
                        SupportsAsyncOperations = true,
                        SupportsOAuth = true,
                        Notes = "AWS ETL service; serverless; data catalog"
                    }
                },
                {
                    DataSourceType.AWSAthena, new DataSourceCapabilities
                    {
                        SupportsJoins = true,
                        SupportsAggregations = true,
                        SupportsParameterization = true,
                        SupportsWindowFunctions = true,
                        SupportsPartitioning = true,
                        SupportsViews = true,
                        SupportsOAuth = true,
                        Notes = "AWS serverless query service; Presto-based; S3 data lake"
                    }
                },
                {
                    DataSourceType.DataBricks, new DataSourceCapabilities
                    {
                        SupportsTransactions = true,
                        SupportsJoins = true,
                        SupportsAggregations = true,
                        SupportsParameterization = true,
                        SupportsWindowFunctions = true,
                        SupportsBulkOperations = true,
                        SupportsPartitioning = true,
                        SupportsStreaming = true,
                        SupportsAsyncOperations = true,
                        SupportsOAuth = true,
                        Notes = "Unified analytics platform; Delta Lake; Spark-based"
                    }
                },
                {
                    DataSourceType.Supabase, new DataSourceCapabilities
                    {
                        SupportsTransactions = true,
                        SupportsJoins = true,
                        SupportsAggregations = true,
                        SupportsIndexes = true,
                        SupportsParameterization = true,
                        SupportsNativeJson = true,
                        SupportsBulkOperations = true,
                        SupportsViews = true,
                        SupportsReplication = true,
                        SupportsOAuth = true,
                        SupportsPushNotifications = true,
                        Notes = "Open-source Firebase alternative; PostgreSQL backend; realtime"
                    }
                },

                #endregion
                #region Auto Defaults
                {
                    DataSourceType.ActiveCampaign, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.ADO, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = true,
                        Notes = "Default: RDBMS helper"
                    }
                },
                {
                    DataSourceType.Adyen, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Airtable, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.AmazonS3, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Amplitude, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.AnyDo, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.ApacheFlink, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = true,
                        Notes = "Default: RDBMS helper"
                    }
                },
                {
                    DataSourceType.ApacheSparkStreaming, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = true,
                        Notes = "Default: RDBMS helper"
                    }
                },
                {
                    DataSourceType.ApacheStorm, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = true,
                        Notes = "Default: RDBMS helper"
                    }
                },
                {
                    DataSourceType.ArduinoCloud, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Asana, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.AuthorizeNet, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.AWSIoT, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = true,
                        Notes = "Default: RDBMS helper"
                    }
                },
                {
                    DataSourceType.AWSIoTAnalytics, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = true,
                        Notes = "Default: RDBMS helper"
                    }
                },
                {
                    DataSourceType.AWSIoTCore, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = true,
                        Notes = "Default: RDBMS helper"
                    }
                },
                {
                    DataSourceType.AWSStepFunctions, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = true,
                        Notes = "Default: RDBMS helper"
                    }
                },
                {
                    DataSourceType.AWSSWF, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = true,
                        Notes = "Default: RDBMS helper"
                    }
                },
                {
                    DataSourceType.AzureBoards, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.AzureCloud, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = true,
                        Notes = "Default: RDBMS helper"
                    }
                },
                {
                    DataSourceType.AzureDevOps, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.AzureIoTHub, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = true,
                        Notes = "Default: RDBMS helper"
                    }
                },
                {
                    DataSourceType.Backblaze, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Basecamp, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.BenchAccounting, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.BigCartel, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.BigCommerce, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Bitbucket, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.BitcoinCore, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.BitPay, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Bluesky, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Box, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Braintree, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Buffer, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Calendly, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.CampaignMonitor, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Chanty, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.CircleCI, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.CitrixShareFile, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.ClickSend, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.ClickUp, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Coinbase, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.ConstantContact, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.ConvertKit, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Copper, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Criteo, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Cyfe, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Databox, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Discord, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Doc, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = true,
                        Notes = "Default: RDBMS helper"
                    }
                },
                {
                    DataSourceType.DocuSign, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Docx, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = true,
                        Notes = "Default: RDBMS helper"
                    }
                },
                {
                    DataSourceType.Doodle, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Drift, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Drip, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Dropbox, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Ecwid, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Egnyte, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Ethereum, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Etsy, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Eventbrite, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Facebook, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Fathom, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Firebolt, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = true,
                        Notes = "Default: RDBMS helper"
                    }
                },
                {
                    DataSourceType.Flock, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.FreshBooks, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Freshdesk, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Freshsales, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Front, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Geckoboard, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.GitHub, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.GitLab, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Gmail, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.GoogleAds, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.GoogleAnalytics, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.GoogleChat, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.GoogleCloudStorage, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.GoogleDrive, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.GoogleSheets, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = true,
                        Notes = "Default: RDBMS helper"
                    }
                },
                {
                    DataSourceType.Heap, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.HelpScout, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Hologres, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = true,
                        Notes = "Default: RDBMS helper"
                    }
                },
                {
                    DataSourceType.Hootsuite, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.HootsuiteMarketing, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Hotjar, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.HubSpot, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Hyperledger, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.iCloud, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Insightly, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Instagram, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Integromat, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Intercom, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Jenkins, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Jira, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Jotform, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Kayako, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Klaviyo, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Kudosity, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.LinkedIn, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.LiveAgent, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Loomly, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Magento, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Mailchimp, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.MailerLite, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Mailgun, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Make, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Marketo, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Mastodon, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Mattermost, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.MediaFire, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Mega, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.MicrosoftDynamics365, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.MicrosoftPowerAutomate, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.MicrosoftTeams, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.MiModel, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = true,
                        Notes = "Default: RDBMS helper"
                    }
                },
                {
                    DataSourceType.Mixpanel, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Monday, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.MYOB, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Nest, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.NONE, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = false,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: unspecified helper"
                    }
                },
                {
                    DataSourceType.Notion, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Nutshell, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.ODBC, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = true,
                        Notes = "Default: RDBMS helper"
                    }
                },
                {
                    DataSourceType.OLEDB, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = true,
                        Notes = "Default: RDBMS helper"
                    }
                },
                {
                    DataSourceType.OneDrive, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Onnx, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = true,
                        Notes = "Default: RDBMS helper"
                    }
                },
                {
                    DataSourceType.ONNX, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = true,
                        Notes = "Default: RDBMS helper"
                    }
                },
                {
                    DataSourceType.OPC, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = true,
                        Notes = "Default: RDBMS helper"
                    }
                },
                {
                    DataSourceType.OpenCart, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.OracleCRM, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Other, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = false,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: unspecified helper"
                    }
                },
                {
                    DataSourceType.Outlook, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Particle, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Payoneer, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.PayPal, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.pCloud, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.PDF, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = true,
                        Notes = "Default: RDBMS helper"
                    }
                },
                {
                    DataSourceType.Petastorm, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = false,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: key-value helper"
                    }
                },
                {
                    DataSourceType.PhilipsHue, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Pinterest, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Pipedrive, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Plaid, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Podio, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Postman, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.PowerBI, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.PPT, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = true,
                        Notes = "Default: RDBMS helper"
                    }
                },
                {
                    DataSourceType.PPTX, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = true,
                        Notes = "Default: RDBMS helper"
                    }
                },
                {
                    DataSourceType.PrestaShop, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Presto, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = true,
                        Notes = "Default: RDBMS helper"
                    }
                },
                {
                    DataSourceType.PyTorchData, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = true,
                        Notes = "Default: RDBMS helper"
                    }
                },
                {
                    DataSourceType.QuickBooks, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.QuickBooksOnline, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Razorpay, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.RecordIO, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = true,
                        Notes = "Default: RDBMS helper"
                    }
                },
                {
                    DataSourceType.Reddit, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.RocketChat, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.RocketChatComm, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.RocketSet, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = false,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: key-value helper"
                    }
                },
                {
                    DataSourceType.SageBusinessCloud, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.SageIntacct, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Salesforce, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.SAPCRM, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.ScikitLearnData, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = true,
                        Notes = "Default: RDBMS helper"
                    }
                },
                {
                    DataSourceType.SendGrid, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Sendinblue, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Shopify, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Slack, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Smartsheet, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.SmartsheetPM, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.SmartThings, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Snapchat, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.SonarQube, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Square, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Squarespace, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Stripe, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.SugarCRM, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.SwaggerHub, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Tableau, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Teamwork, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Telegram, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.TFRecord, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = true,
                        Notes = "Default: RDBMS helper"
                    }
                },
                {
                    DataSourceType.Threads, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.TikTok, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.TikTokAds, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.TLDV, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.TrayIO, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Trello, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Trino, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = true,
                        Notes = "Default: RDBMS helper"
                    }
                },
                {
                    DataSourceType.Tuya, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Twilio, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Twist, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Twitter, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.TwoCheckout, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Typeform, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Unknown, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = false,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: unspecified helper"
                    }
                },
                {
                    DataSourceType.Venmo, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.VistaDB, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = false,
                        SupportsSchemaEvolution = true,
                        Notes = "Default: document helper"
                    }
                },
                {
                    DataSourceType.Volusion, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.WaveApps, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.WebApi, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.WhatsAppBusiness, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Wise, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Wix, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.WooCommerce, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.WordPress, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Worldpay, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Wrike, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Xero, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Yahoo, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.YouTube, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Zapier, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Zendesk, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Zoho, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.ZohoBooks, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.ZohoDesk, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                {
                    DataSourceType.Zoom, new DataSourceCapabilities
                    {
                        IsSchemaEnforced = true,
                        SupportsSchemaEvolution = false,
                        Notes = "Default: REST API helper"
                    }
                },
                #endregion
            };

        /// <summary>
        /// Default capabilities for connectors (REST API-based business applications).
        /// </summary>
        private static readonly DataSourceCapabilities DefaultConnectorCapabilities = new DataSourceCapabilities
        {
            SupportsTransactions = false,
            SupportsJoins = false,
            SupportsAggregations = false,
            SupportsIndexes = false,
            SupportsParameterization = true,
            SupportsNativeJson = true,
            SupportsBulkOperations = false,
            SupportsAsyncOperations = true,
            SupportsOAuth = true,
            Notes = "REST API connector; capabilities depend on specific service"
        };

        /// <summary>
        /// Default capabilities by datasource category for fallback.
        /// </summary>
        private static readonly Dictionary<DatasourceCategory, DataSourceCapabilities> CategoryDefaults = 
            new Dictionary<DatasourceCategory, DataSourceCapabilities>
            {
                { DatasourceCategory.Connector, DefaultConnectorCapabilities },
                { DatasourceCategory.WEBAPI, new DataSourceCapabilities
                    {
                        SupportsNativeJson = true,
                        SupportsAsyncOperations = true,
                        SupportsOAuth = true,
                        Notes = "Web API; HTTP-based operations"
                    }
                },
                { DatasourceCategory.FILE, new DataSourceCapabilities
                    {
                        SupportsBulkOperations = true,
                        SupportsAppend = true,
                        SupportsStreaming = true,
                        Notes = "File-based datasource"
                    }
                },
                { DatasourceCategory.QUEUE, new DataSourceCapabilities
                    {
                        SupportsPublishSubscribe = true,
                        SupportsAsyncOperations = true,
                        Notes = "Message queue; async messaging"
                    }
                },
                { DatasourceCategory.STREAM, new DataSourceCapabilities
                    {
                        SupportsStreaming = true,
                        SupportsPublishSubscribe = true,
                        SupportsAppend = true,
                        SupportsAsyncOperations = true,
                        Notes = "Streaming platform; continuous data flow"
                    }
                },
                { DatasourceCategory.VectorDB, new DataSourceCapabilities
                    {
                        SupportsVectorSearch = true,
                        SupportsEmbeddings = true,
                        SupportsANN = true,
                        SupportsAsyncOperations = true,
                        Notes = "Vector database; similarity search"
                    }
                },
                { DatasourceCategory.GraphDB, new DataSourceCapabilities
                    {
                        SupportsGraphTraversal = true,
                        SupportsIndexes = true,
                        SupportsAsyncOperations = true,
                        Notes = "Graph database; relationship queries"
                    }
                },
                { DatasourceCategory.TimeSeriesDB, new DataSourceCapabilities
                    {
                        SupportsTimeSeriesQueries = true,
                        SupportsDownsampling = true,
                        SupportsRetentionPolicies = true,
                        SupportsAggregations = true,
                        SupportsAsyncOperations = true,
                        Notes = "Time series database"
                    }
                },
                { DatasourceCategory.SearchEngine, new DataSourceCapabilities
                    {
                        SupportsFullTextSearch = true,
                        SupportsFacetedSearch = true,
                        SupportsHighlighting = true,
                        SupportsAggregations = true,
                        SupportsAsyncOperations = true,
                        Notes = "Search engine; text indexing and queries"
                    }
                },
                { DatasourceCategory.MessageQueue, new DataSourceCapabilities
                    {
                        SupportsPublishSubscribe = true,
                        SupportsMessageOrdering = true,
                        SupportsDeadLetterQueue = true,
                        SupportsAsyncOperations = true,
                        Notes = "Message queue; reliable messaging"
                    }
                },
                { DatasourceCategory.BigData, new DataSourceCapabilities
                    {
                        SupportsBulkOperations = true,
                        SupportsPartitioning = true,
                        SupportsAggregations = true,
                        SupportsAsyncOperations = true,
                        Notes = "Big data platform; large-scale processing"
                    }
                },
                { DatasourceCategory.INMEMORY, new DataSourceCapabilities
                    {
                        SupportsTTL = true,
                        SupportsBulkOperations = true,
                        SupportsAsyncOperations = true,
                        Notes = "In-memory storage; fast access"
                    }
                }
            };

        /// <summary>
        /// Gets the capability information for a specific datasource type.
        /// </summary>
        /// <param name="type">The datasource type</param>
        /// <returns>Capabilities object; returns empty/default if type not found</returns>
        public static DataSourceCapabilities GetCapabilities(DataSourceType type)
        {
            return Matrix.TryGetValue(type, out var caps) 
                ? caps 
                : new DataSourceCapabilities { Notes = $"Capabilities for {type} not defined" };
        }

        /// <summary>
        /// Gets capability information for a datasource type with category-based fallback.
        /// Use this method when the datasource type might not have explicit capabilities defined.
        /// </summary>
        /// <param name="type">The datasource type</param>
        /// <param name="category">The datasource category for fallback</param>
        /// <returns>Explicit capabilities if defined, otherwise category defaults</returns>
        public static DataSourceCapabilities GetCapabilities(DataSourceType type, DatasourceCategory category)
        {
            // First try explicit type mapping
            if (Matrix.TryGetValue(type, out var caps))
                return caps;

            // Fall back to category defaults
            if (CategoryDefaults.TryGetValue(category, out var categoryDefaults))
                return categoryDefaults;

            // Ultimate fallback
            return new DataSourceCapabilities { Notes = $"Capabilities for {type} ({category}) not defined" };
        }

        /// <summary>
        /// Gets the default capabilities for a datasource category.
        /// Useful for connectors and other types that share common patterns.
        /// </summary>
        /// <param name="category">The datasource category</param>
        /// <returns>Default capabilities for the category</returns>
        public static DataSourceCapabilities GetCapabilitiesByCategory(DatasourceCategory category)
        {
            return CategoryDefaults.TryGetValue(category, out var caps)
                ? caps
                : new DataSourceCapabilities { Notes = $"Default capabilities for {category} not defined" };
        }

        /// <summary>
        /// Checks if a specific datasource supports a given capability.
        /// </summary>
        /// <param name="type">The datasource type</param>
        /// <param name="capabilityName">The capability name (property name, case-insensitive)</param>
        /// <returns>True if supported; false otherwise</returns>
        public static bool Supports(DataSourceType type, string capabilityName)
        {
            var caps = GetCapabilities(type);
            return caps.IsCapable(capabilityName);
        }

        /// <summary>
        /// Checks if a specific datasource supports a given capability (type-safe).
        /// </summary>
        /// <param name="type">The datasource type</param>
        /// <param name="capability">The capability to check</param>
        /// <returns>True if supported; false otherwise</returns>
        public static bool Supports(DataSourceType type, CapabilityType capability)
        {
            var caps = GetCapabilities(type);
            return caps.IsCapable(capability);
        }

        /// <summary>
        /// Gets all datasource types that support a specific capability.
        /// </summary>
        /// <param name="capabilityName">The capability name</param>
        /// <returns>List of datasource types that support the capability</returns>
        public static List<DataSourceType> GetDatasourcesSupportingCapability(string capabilityName)
        {
            return Matrix
                .Where(kvp => kvp.Value.IsCapable(capabilityName))
                .Select(kvp => kvp.Key)
                .ToList();
        }

        /// <summary>
        /// Gets a summary of all datasource capabilities (for debugging/analysis).
        /// </summary>
        /// <returns>Dictionary mapping datasource type to capability summary</returns>
        public static Dictionary<DataSourceType, string> GetCapabilitySummary()
        {
            return Matrix
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.ToString());
        }
    }
}


