# IDataSourceHelper Implementation Plan

## Overview
Complete refactor of all helper implementations to match the exact `IDataSourceHelper` contract. Factory already covers all 287 DataSourceType enum values.

**Status**: 🔴 In Progress  
**Last Updated**: 2026-01-11

---

## Phase 1: Fix Existing Core Helpers (Priority 1 - Required for Build)

### 1.1 RdbmsHelper Core - Main File ✅ COMPLETE
**File**: `DataManagementEngineStandard/Helpers/UniversalDataSourceHelpers/RdbmsHelpers/RdbmsHelper.cs`

#### Properties
- [x] ✅ Add `public DataSourceType SupportedType { get; set; } = DataSourceType.SqlServer;`
- [x] ✅ Add `public string Name => $"RDBMS ({SupportedType})";`
- [x] ✅ Add `public DataSourceCapabilities Capabilities => DataSourceCapabilityMatrix.GetCapabilities(SupportedType);`

#### Schema Methods (Change Signatures)
- [x] ✅ `GetSchemaQuery(string userName)` - Remove EntityStructure/DataSourceType params, return `(string Query, bool Success)`
- [x] ✅ `GetTableExistsQuery(string tableName)` - Remove EntityStructure/DataSourceType params, return `(string Query, bool Success)`
- [x] ✅ `GetColumnInfoQuery(string tableName)` - Remove EntityStructure/DataSourceType params, return `(string Query, bool Success)`

#### DDL Methods (Remove Parameters Dict from Return)
- [x] ✅ `GenerateCreateTableSql(EntityStructure entity, string schemaName, DataSourceType? dataSourceType)` - Change return to `(string Sql, bool Success, string ErrorMessage)`
- [x] ✅ `GenerateDropTableSql(string tableName, string schemaName)` - Remove EntityStructure/DataSourceType params
- [x] ✅ Rename `GenerateTruncateSql` → `GenerateTruncateTableSql(string tableName, string schemaName)`
- [x] ✅ `GenerateCreateIndexSql(string tableName, string indexName, string[] columns, Dictionary<string, object> options)` - Remove EntityStructure/DataSourceType, change options type

#### Add Missing DDL Methods
- [x] ✅ `GenerateAddColumnSql(string tableName, EntityField column)`
- [x] ✅ `GenerateAlterColumnSql(string tableName, string columnName, EntityField newColumn)`
- [x] ✅ `GenerateDropColumnSql(string tableName, string columnName)`
- [x] ✅ `GenerateRenameTableSql(string oldTableName, string newTableName)`
- [x] ✅ `GenerateRenameColumnSql(string tableName, string oldColumnName, string newColumnName)`

#### DML Methods (Remove EntityStructure/DataSourceType Params)
- [x] ✅ `GenerateInsertSql(string tableName, Dictionary<string, object> data)`
- [x] ✅ `GenerateUpdateSql(string tableName, Dictionary<string, object> data, Dictionary<string, object> conditions)`
- [x] ✅ `GenerateDeleteSql(string tableName, Dictionary<string, object> conditions)`
- [x] ✅ `GenerateSelectSql(string tableName, IEnumerable<string> columns, Dictionary<string, object> conditions, string orderBy, int? skip, int? take)`

#### Utility Methods (Remove DataSourceType Params, Add New Params)
- [x] ✅ `QuoteIdentifier(string identifier)` - Remove DataSourceType param
- [x] ✅ `MapClrTypeToDatasourceType(Type clrType, int? size, int? precision, int? scale)` - Remove DataSourceType, add size/precision/scale
- [x] ✅ `MapDatasourceTypeToClrType(string datasourceType)` - Remove DataSourceType param
- [x] ✅ `ValidateEntity(EntityStructure entity)` - Remove DataSourceType param, return `(bool IsValid, List<string> Errors)`

#### Add New Utility Methods
- [x] ✅ `SupportsCapability(CapabilityType capability)`
- [x] ✅ `GetMaxStringSize()`
- [x] ✅ `GetMaxNumericPrecision()`

---

### 1.2 RdbmsHelper.Constraints Partial ✅ COMPLETE
**File**: `DataManagementEngineStandard/Helpers/UniversalDataSourceHelpers/RdbmsHelpers/RdbmsHelper.Constraints.cs`

- [x] ✅ Update all `QuoteIdentifier` calls to single-param form
- [x] ✅ Update internal helper calls to pass `this.SupportedType`
- [x] ✅ Verify all method signatures match interface (already correct)

---

### 1.3 RdbmsHelper.Transactions Partial ✅ COMPLETE
**File**: `DataManagementEngineStandard/Helpers/UniversalDataSourceHelpers/RdbmsHelpers/RdbmsHelper.Transactions.cs`

- [x] ✅ Verify `GenerateBeginTransactionSql()` signature (already correct)
- [x] ✅ Verify `GenerateCommitSql()` signature (already correct)
- [x] ✅ Verify `GenerateRollbackSql()` signature (already correct)

---

### 1.4 RdbmsHelper.Schema Partial
**File**: `DataManagementEngineStandard/Helpers/UniversalDataSourceHelpers/RdbmsHelpers/RdbmsHelper.Schema.cs`

- [ ] Read file and verify all DDL method signatures
- [ ] Update any legacy signatures to match interface

---

### 1.5 MongoDBHelper
**File**: `DataManagementEngineStandard/Helpers/UniversalDataSourceHelpers/MongoDBHelpers/MongoDBHelper.cs`

#### Verify Existing Methods
- [ ] Schema methods return `(string Query, bool Success)` (already correct)
- [ ] DDL methods return `(string Sql, bool Success, string ErrorMessage)` (already correct)
- [ ] Transaction methods signatures correct

#### Verify DML Methods Return Tuples
- [ ] `GenerateInsertSql` returns `(string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage)`
- [ ] `GenerateUpdateSql` returns `(string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage)`
- [ ] `GenerateDeleteSql` returns `(string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage)`
- [ ] `GenerateSelectSql` returns `(string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage)`

#### Add Missing Utility Methods
- [ ] `SupportsCapability(CapabilityType capability)`
- [ ] `GetMaxStringSize()`
- [ ] `GetMaxNumericPrecision()`

---

### 1.6 RedisHelper
**File**: `DataManagementEngineStandard/Helpers/UniversalDataSourceHelpers/RedisHelpers/RedisHelper.cs`

#### Verify Existing Methods
- [ ] Schema methods correct
- [ ] DDL methods correct
- [ ] Transaction methods correct

#### Verify DML Methods Return Tuples
- [ ] `GenerateInsertSql` returns correct tuple with Parameters dict
- [ ] `GenerateUpdateSql` returns correct tuple with Parameters dict
- [ ] `GenerateDeleteSql` returns correct tuple with Parameters dict
- [ ] `GenerateSelectSql` returns correct tuple with Parameters dict

#### Add Missing Utility Methods
- [ ] `SupportsCapability(CapabilityType capability)`
- [ ] `GetMaxStringSize()`
- [ ] `GetMaxNumericPrecision()`

---

### 1.7 CassandraHelper
**File**: `DataManagementEngineStandard/Helpers/UniversalDataSourceHelpers/CassandraHelpers/CassandraHelper.cs`

#### Verify Existing Methods
- [ ] All schema/DDL/transaction methods correct

#### Verify DML Methods Return Tuples
- [ ] `GenerateInsertSql` returns correct tuple with Parameters dict
- [ ] `GenerateUpdateSql` returns correct tuple with Parameters dict
- [ ] `GenerateDeleteSql` returns correct tuple with Parameters dict
- [ ] `GenerateSelectSql` returns correct tuple with Parameters dict

#### Add Missing Utility Methods
- [ ] `SupportsCapability(CapabilityType capability)`
- [ ] `GetMaxStringSize()`
- [ ] `GetMaxNumericPrecision()`

---

### 1.8 RestApiHelper
**File**: `DataManagementEngineStandard/Helpers/UniversalDataSourceHelpers/RestApiHelpers/RestApiHelper.cs`

#### Verify Existing Methods
- [ ] All schema/DDL/transaction methods correct

#### Verify DML Methods Return Tuples
- [ ] `GenerateInsertSql` returns correct tuple with Parameters dict
- [ ] `GenerateUpdateSql` returns correct tuple with Parameters dict
- [ ] `GenerateDeleteSql` returns correct tuple with Parameters dict
- [ ] `GenerateSelectSql` returns correct tuple with Parameters dict

#### Add Missing Utility Methods
- [ ] `SupportsCapability(CapabilityType capability)`
- [ ] `GetMaxStringSize()`
- [ ] `GetMaxNumericPrecision()`

---

### 1.9 GeneralDataSourceHelper
**File**: `DataManagementEngineStandard/Helpers/UniversalDataSourceHelpers/Core/GeneralDataSourceHelper.cs`

- [x] ✅ Already fully implements interface via delegation - NO CHANGES NEEDED

---

### 1.10 DataSourceHelperFactory
**File**: `DataManagementEngineStandard/Helpers/UniversalDataSourceHelpers/Core/DataSourceHelperFactory.cs`

- [ ] Update all RDBMS factory lambdas to set `SupportedType`:
  - [ ] SqlServer, Mysql, Postgre, Oracle, SqlLite, SqlCompact, DB2, FireBird
  - [ ] Hana, TerraData, Vertica, AzureSQL, AWSRDS, SnowFlake, Cockroach, Spanner
  - [ ] DuckDB, MariaDB, H2Database, AWSRedshift, GoogleBigQuery, etc.
- [ ] Verify MongoDB/Redis/Cassandra/RestApi helpers set `SupportedType` in constructors

---

## Phase 2: Build and Test

### 2.1 Build
- [ ] Run `dotnet build BeepDM.sln`
- [ ] Fix all compilation errors
- [ ] Achieve zero-error build

### 2.2 Smoke Tests
- [ ] Test RdbmsHelper (SQL Server): `GenerateCreateTableSql`, `GenerateInsertSql`, `GenerateBeginTransactionSql`
- [ ] Test MongoDBHelper: Verify MongoDB command generation
- [ ] Test RedisHelper: Verify Redis command generation
- [ ] Test CassandraHelper: Verify CQL generation
- [ ] Test RestApiHelper: Verify HTTP request generation

### 2.3 Integration Tests
- [ ] Run tests in `tests/IntegrationTests/`
- [ ] Verify CRUD operations for one RDBMS helper
- [ ] Verify CRUD operations for one NoSQL helper

---

## Phase 3: Add New Specialized Helpers (Optional - Post-Build)

### 3.1 FileFormatHelper
**Path**: `DataManagementEngineStandard/Helpers/UniversalDataSourceHelpers/FileFormatHelpers/FileFormatHelper.cs`

- [ ] Create FileFormatHelper implementing `IDataSourceHelper`
- [ ] Support 28 file types: CSV, JSON, XML, Parquet, Avro, ORC, etc.
- [ ] Implement schema detection from file metadata
- [ ] Update factory to use FileFormatHelper for file types

### 3.2 StreamingHelper
**Path**: `DataManagementEngineStandard/Helpers/UniversalDataSourceHelpers/StreamingHelpers/StreamingHelper.cs`

- [ ] Create StreamingHelper for Kafka, RabbitMQ, Kinesis, etc.
- [ ] Implement publish/subscribe patterns
- [ ] Support offset management and consumer groups
- [ ] Update factory for 14 streaming types

### 3.3 GraphDatabaseHelper
**Path**: `DataManagementEngineStandard/Helpers/UniversalDataSourceHelpers/GraphHelpers/GraphDatabaseHelper.cs`

- [ ] Create GraphDatabaseHelper for Neo4j, TigerGraph, etc.
- [ ] Implement Cypher/Gremlin query generation
- [ ] Support vertex/edge operations
- [ ] Update factory for 5 graph database types

### 3.4 SearchEngineHelper
**Path**: `DataManagementEngineStandard/Helpers/UniversalDataSourceHelpers/SearchHelpers/SearchEngineHelper.cs`

- [ ] Create SearchEngineHelper for ElasticSearch, Solr
- [ ] Implement full-text indexing queries
- [ ] Support faceted search and analyzers
- [ ] Update factory for 2 search engine types

### 3.5 TimeSeriesHelper
**Path**: `DataManagementEngineStandard/Helpers/UniversalDataSourceHelpers/TimeSeriesHelpers/TimeSeriesHelper.cs`

- [ ] Create TimeSeriesHelper for InfluxDB, TimeScale
- [ ] Implement retention policies
- [ ] Support downsampling and time-based aggregations
- [ ] Update factory for 2 time series types

### 3.6 VectorDatabaseHelper
**Path**: `DataManagementEngineStandard/Helpers/UniversalDataSourceHelpers/VectorHelpers/VectorDatabaseHelper.cs`

- [ ] Create VectorDatabaseHelper for ChromaDB, PineCone, Qdrant, etc.
- [ ] Implement similarity search and ANN algorithms
- [ ] Support embedding storage
- [ ] Update factory for 8 vector database types

### 3.7 BigDataHelper
**Path**: `DataManagementEngineStandard/Helpers/UniversalDataSourceHelpers/BigDataHelpers/BigDataHelper.cs`

- [ ] Create BigDataHelper for Hadoop, Kudu, Druid, Pinot
- [ ] Implement distributed query coordination
- [ ] Support partition pruning
- [ ] Update factory for 4 big data types

### 3.8 BlockchainHelper
**Path**: `DataManagementEngineStandard/Helpers/UniversalDataSourceHelpers/BlockchainHelpers/BlockchainHelper.cs`

- [ ] Create BlockchainHelper for Ethereum, Hyperledger, BitcoinCore
- [ ] Implement transaction signing
- [ ] Support smart contract interaction
- [ ] Update factory for 3 blockchain types

### 3.9 EmailProtocolHelper
**Path**: `DataManagementEngineStandard/Helpers/UniversalDataSourceHelpers/EmailHelpers/EmailProtocolHelper.cs`

- [ ] Create EmailProtocolHelper for IMAP, POP3, SMTP, etc.
- [ ] Implement MIME parsing
- [ ] Support OAuth2 authentication flows
- [ ] Update factory for 7 email protocol types

---

## Progress Summary

| Component | Total Tasks | Completed | Status |
|-----------|-------------|-----------|--------|
| RdbmsHelper Core | 27 | 27 | ✅ Complete |
| RdbmsHelper Partials | 7 | 6 | 🔄 In Progress |
| MongoDBHelper | 8 | 0 | 🔴 Not Started |
| RedisHelper | 8 | 0 | 🔴 Not Started |
| CassandraHelper | 8 | 0 | 🔴 Not Started |
| RestApiHelper | 8 | 0 | 🔴 Not Started |
| GeneralDataSourceHelper | 1 | 1 | ✅ Complete |
| DataSourceHelperFactory | 2 | 0 | 🔴 Not Started |
| Build & Test | 8 | 0 | 🔴 Not Started |
| New Helpers (Phase 3) | 27 | 0 | ⚪ Not Started |
| **TOTAL** | **104** | **34** | **33% Complete** |

---

## Notes

### Factory Coverage
- ✅ All 287 DataSourceType enum values are registered in the factory
- ✅ Current mappings use 5 existing helpers (RDBMS, MongoDB, Redis, Cassandra, RestApi)
- 🔄 Phase 3 will add 9 specialized helpers for better type-specific support

### Breaking Changes
- User confirmed: "don't worry about legacy or breaking changes, just fix it"
- Internal adapter methods will translate interface calls to legacy helper methods
- `SupportedType` property enables dynamic behavior per datasource type

### Key Implementation Patterns
1. **RdbmsHelper**: Instance property `SupportedType` determines which RDBMS dialect to use
2. **Internal Adapters**: Public methods use interface signatures; internal adapters call legacy helpers with `this.SupportedType`
3. **Factory Initialization**: Set `SupportedType` in factory lambdas for RDBMS variants
4. **Utility Methods**: Use `DataSourceCapabilityMatrix.GetCapabilities(SupportedType)` for capability checks

---

## Current Blockers

None - ready to proceed with implementation.

---

## Next Steps

1. Start with RdbmsHelper core file (largest refactor)
2. Work through partials sequentially
3. Update other 4 helpers (smaller changes)
4. Update factory
5. Build and fix compilation errors iteratively
6. Run smoke tests
7. (Optional) Implement Phase 3 specialized helpers
