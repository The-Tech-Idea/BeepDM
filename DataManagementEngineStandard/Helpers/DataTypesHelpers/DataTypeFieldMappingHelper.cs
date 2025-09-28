using TheTechIdea.Beep.DriversConfigurations;
using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Helpers.DataTypesHelpers
{
    /// <summary>
    /// Main helper class for mapping data types to field names.
    /// This class has been refactored into smaller, focused helper classes.
    /// </summary>
    public static partial class DataTypeFieldMappingHelper
    {
        #region Delegation Methods - Forward calls to specialized helpers

        /// <summary>Returns an array of .NET data types.</summary>
        /// <returns>An array of .NET data types.</returns>
        public static string[] GetNetDataTypes()
        {
            return DataTypeBasicOperations.GetNetDataTypes();
        }

        /// <summary>Returns an array of .NET data types.</summary>
        /// <returns>An array of .NET data types.</returns>
        public static string[] GetNetDataTypes2()
        {
            return DataTypeBasicOperations.GetNetDataTypes2();
        }

        /// <summary>
        /// Gets a custom data type using a custom converter function.
        /// </summary>
        /// <param name="DSname">Data source name</param>
        /// <param name="fld">Entity field</param>
        /// <param name="DMEEditor">DME Editor instance</param>
        /// <param name="customTypeConverter">Custom type converter function</param>
        /// <returns>Custom data type string</returns>
        public static string GetCustomDataType(string DSname, EntityField fld, IDMEEditor DMEEditor, Func<string, string> customTypeConverter)
        {
            return DataTypeBasicOperations.GetCustomDataType(DSname, fld, DMEEditor, customTypeConverter);
        }

        /// <summary>
        /// Validates if a field mapping is valid for the given data source.
        /// </summary>
        /// <param name="DSname">Data source name</param>
        /// <param name="fld">Entity field</param>
        /// <param name="DMEEditor">DME Editor instance</param>
        /// <returns>True if valid field mapping exists</returns>
        public static bool IsValidFieldMapping(string DSname, EntityField fld, IDMEEditor DMEEditor)
        {
            return DataTypeBasicOperations.IsValidFieldMapping(DSname, fld, DMEEditor);
        }

        /// <summary>Gets the datatype mapping for a given class name, field type, entity field, and DME editor.</summary>
        /// <param name="className">The name of the class.</param>
        /// <param name="fieldType">The type of the field.</param>
        /// <param name="fld">The entity field.</param>
        /// <param name="DMEEditor">The DME editor.</param>
        /// <returns>The datatype mapping for the given parameters.</returns>
        public static DatatypeMapping GetDataTypeMappingForString(string className, string fieldType, EntityField fld, IDMEEditor DMEEditor)
        {
            return DataTypeMappingLookup.GetDataTypeMappingForString(className, fieldType, fld, DMEEditor);
        }

        /// <summary>Gets the data type of a field in a specific data source.</summary>
        /// <param name="DSname">The name of the data source.</param>
        /// <param name="providerfldtype">The provider field type.</param>
        /// <param name="DMEEditor">The IDMEEditor instance used for accessing the data source.</param>
        /// <returns>The data type of the specified field.</returns>
        public static string GetDataType(string DSname, string providerfldtype, IDMEEditor DMEEditor)
        {
            return DataTypeMappingLookup.GetDataType(DSname, providerfldtype, DMEEditor);
        }

        /// <summary>Gets the data type of a field in a specific data source.</summary>
        /// <param name="DSname">The name of the data source.</param>
        /// <param name="fld">The field for which to retrieve the data type.</param>
        /// <param name="DMEEditor">The IDMEEditor instance used for accessing the data source.</param>
        /// <returns>The data type of the specified field.</returns>
        public static string GetDataType(string DSname, EntityField fld, IDMEEditor DMEEditor)
        {
            return DataTypeMappingLookup.GetDataType(DSname, fld, DMEEditor);
        }

        /// <summary>Gets the data type of a field in a specific data source.</summary>
        /// <param name="className">The name of the data source class</param>
        /// <param name="fld">The field for which to retrieve the data type.</param>
        /// <param name="DMEEditor">The IDMEEditor instance used for accessing the data source.</param>
        /// <returns>The data type of the specified field.</returns>
        public static string GetDataTypeFromDataSourceClassName(string className, EntityField fld, IDMEEditor DMEEditor)
        {
            return DataTypeMappingLookup.GetDataTypeFromDataSourceClassName(className, fld, DMEEditor);
        }

        /// <summary>Gets the data type mappings for a specific data source.</summary>
        /// <param name="DSname">The name of the data source.</param>
        /// <param name="DMEEditor">The IDMEEditor instance used for accessing the data source.</param>
        /// <returns>The List of DataTypeMapping for DataSource.</returns>
        public static List<DatatypeMapping> GetDataTypes(string DSname, IDMEEditor DMEEditor)
        {
            return DataTypeMappingRepository.GetDataTypes(DSname, DMEEditor);
        }

        /// <summary>Gets the data type mappings for a specific data source type.</summary>
        /// <param name="DSname">The type of the data source.</param>
        /// <param name="DMEEditor">The IDMEEditor instance used for accessing the data source.</param>
        /// <returns>The List of DataTypeMapping for DataSource.</returns>
        public static List<DatatypeMapping> GetDataTypes(DataSourceType DSname, IDMEEditor DMEEditor)
        {
            return DataTypeMappingRepository.GetDataTypes(DSname, DMEEditor);
        }

        /// <summary>Gets the field type without conversion.</summary>
        /// <param name="DSname">The name of the data source.</param>
        /// <param name="fld">The entity field.</param>
        /// <param name="DMEEditor">The DME editor.</param>
        /// <returns>The field type without conversion.</returns>
        public static string GetFieldTypeWoConversion(string DSname, EntityField fld, IDMEEditor DMEEditor)
        {
            return DataTypeMappingLookup.GetFieldTypeWoConversion(DSname, fld, DMEEditor);
        }

        /// <summary>Returns a comprehensive list of all datatype mappings for all supported databases.</summary>
        /// <returns>A comprehensive list of datatype mappings.</returns>
        public static List<DatatypeMapping> GetMappings()
        {
            return DataTypeMappingRepository.GetAllMappings();
        }

        #endregion

        #region Database-Specific Mapping Methods - Delegated to DatabaseTypeMappingRepository

        /// <summary>
        /// Generates a list of datatype mappings for Oracle database.
        /// </summary>
        /// <returns>A list of datatype mappings for Oracle database.</returns>
        public static List<DatatypeMapping> GenerateOracleDataTypesMapping()
        {
            return DatabaseTypeMappingRepository.GenerateOracleDataTypesMapping();
        }

        /// <summary>
        /// Generates a list of datatype mappings for SQLite.
        /// </summary>
        /// <returns>A list of datatype mappings for SQLite.</returns>
        public static List<DatatypeMapping> GenerateSQLiteDataTypesMapping()
        {
            return DatabaseTypeMappingRepository.GenerateSQLiteDataTypesMapping();
        }

        /// <summary>
        /// Generates a list of datatype mappings between SQL Server data types and corresponding .NET data types.
        /// </summary>
        /// <returns>A list of datatype mappings.</returns>
        public static List<DatatypeMapping> GenerateSqlServerDataTypesMapping()
        {
            return DatabaseTypeMappingRepository.GenerateSqlServerDataTypesMapping();
        }

        /// <summary>
        /// Generates a list of datatype mappings for SQL Server Compact Edition.
        /// </summary>
        /// <returns>A list of datatype mappings for SQL Server Compact Edition.</returns>
        public static List<DatatypeMapping> GenerateSqlCompactDataTypesMapping()
        {
            return DatabaseTypeMappingRepository.GenerateSqlCompactDataTypesMapping();
        }

        /// <summary>Returns a list of datatype mappings for PostgreSQL.</summary>
        /// <returns>A list of datatype mappings for PostgreSQL.</returns>
        public static List<DatatypeMapping> GetPostgreDataTypesMapping()
        {
            return DatabaseTypeMappingRepository.GetPostgreDataTypesMapping();
        }

        /// <summary>Returns a list of datatype mappings between MySQL and .NET data types.</summary>
        /// <returns>A list of datatype mappings.</returns>
        public static List<DatatypeMapping> GetMySqlDataTypesMapping()
        {
            return DatabaseTypeMappingRepository.GetMySqlDataTypesMapping();
        }

        /// <summary>Returns a list of datatype mappings for Firebird database.</summary>
        /// <returns>A list of datatype mappings for Firebird database.</returns>
        public static List<DatatypeMapping> GetFireBirdDataTypesMapping()
        {
            return DatabaseTypeMappingRepository.GetFireBirdDataTypesMapping();
        }

        /// <summary>Returns a list of LiteDB data type mappings.</summary>
        /// <returns>A list of DatatypeMapping objects representing the mappings between LiteDB data types and their corresponding .NET data types.</returns>
        public static List<DatatypeMapping> GetLiteDBDataTypesMapping()
        {
            return DatabaseTypeMappingRepository.GetLiteDBDataTypesMapping();
        }

        /// <summary>Returns a list of datatype mappings for DuckDB.</summary>
        /// <returns>A list of datatype mappings for DuckDB.</returns>
        public static List<DatatypeMapping> GetDuckDBDataTypesMapping()
        {
            return DatabaseTypeMappingRepository.GetDuckDBDataTypesMapping();
        }

        /// <summary>Returns a list of datatype mappings for DB2.</summary>
        /// <returns>A list of datatype mappings for DB2.</returns>
        public static List<DatatypeMapping> GetDB2DataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetDB2DataTypeMappings();
        }

        /// <summary>Returns a list of MongoDB data type mappings.</summary>
        /// <returns>A list of DataTypeMapping objects representing the mappings between .NET data types and MongoDB data types.</returns>
        public static List<DatatypeMapping> GetMongoDBDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetMongoDBDataTypeMappings();
        }

        /// <summary>Returns a list of mappings between .NET data types and Cassandra data types.</summary>
        /// <returns>A list of <see cref="DatatypeMapping"/> objects representing the mappings.</returns>
        public static List<DatatypeMapping> GetCassandraDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetCassandraDataTypeMappings();
        }

        /// <summary>Returns a list of Redis data type mappings.</summary>
        /// <returns>A list of Redis data type mappings.</returns>
        public static List<DatatypeMapping> GetRedisDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetRedisDataTypeMappings();
        }

        /// <summary>Returns a list of DynamoDB data type mappings.</summary>
        /// <returns>A list of DynamoDB data type mappings.</returns>
        public static List<DatatypeMapping> GetDynamoDBDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetDynamoDBDataTypeMappings();
        }

        /// <summary>Returns a list of datatype mappings for InfluxDB.</summary>
        /// <returns>A list of datatype mappings for InfluxDB.</returns>
        public static List<DatatypeMapping> GetInfluxDBDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetInfluxDBDataTypeMappings();
        }

        /// <summary>Returns a list of datatype mappings for Sybase database.</summary>
        /// <returns>A list of datatype mappings for Sybase database.</returns>
        public static List<DatatypeMapping> GetSybaseDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetSybaseDataTypeMappings();
        }

        /// <summary>Returns a list of HBase data type mappings.</summary>
        /// <returns>A list of DatatypeMapping objects representing the mappings between HBase data types and .NET data types.</returns>
        public static List<DatatypeMapping> GetHBaseDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetHBaseDataTypeMappings();
        }

        /// <summary>Returns a list of datatype mappings for CockroachDB.</summary>
        /// <returns>A list of datatype mappings for CockroachDB.</returns>
        public static List<DatatypeMapping> GetCockroachDBDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetCockroachDBDataTypeMappings();
        }

        /// <summary>Returns a list of datatype mappings for Berkeley DB.</summary>
        /// <returns>A list of datatype mappings for Berkeley DB.</returns>
        public static List<DatatypeMapping> GetBerkeleyDBDataTypesMapping()
        {
            return DatabaseTypeMappingRepository.GetBerkeleyDBDataTypesMapping();
        }

        /// <summary>Returns a list of Snowflake data type mappings.</summary>
        /// <returns>A list of DatatypeMapping objects representing the mappings between Snowflake data types and their corresponding .NET data types.</returns>
        public static List<DatatypeMapping> GetSnowflakeDataTypesMapping()
        {
            return DatabaseTypeMappingRepository.GetSnowflakeDataTypesMapping();
        }

        /// <summary>Returns a list of Azure Cosmos DB data type mappings.</summary>
        /// <returns>A list of DatatypeMapping objects representing the mappings between Azure Cosmos DB data types and .NET data types.</returns>
        public static List<DatatypeMapping> GetAzureCosmosDBDataTypesMapping()
        {
            return DatabaseTypeMappingRepository.GetAzureCosmosDBDataTypesMapping();
        }

        /// <summary>Returns a list of datatype mappings for Vertica database.</summary>
        /// <returns>A list of datatype mappings for Vertica database.</returns>
        public static List<DatatypeMapping> GetVerticaDataTypesMapping()
        {
            return DatabaseTypeMappingRepository.GetVerticaDataTypesMapping();
        }

        /// <summary>Returns a list of Teradata data type mappings.</summary>
        /// <returns>A list of Teradata data type mappings.</returns>
        public static List<DatatypeMapping> GetTeradataDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetTeradataDataTypeMappings();
        }

        /// <summary>Returns a list of datatype mappings for ArangoDB.</summary>
        /// <returns>A list of datatype mappings for ArangoDB.</returns>
        public static List<DatatypeMapping> GetArangoDBDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetArangoDBDataTypeMappings();
        }

        /// <summary>Returns a list of Firebase data type mappings.</summary>
        /// <returns>A list of DatatypeMapping objects representing the mappings between Firebase data types and .NET data types.</returns>
        public static List<DatatypeMapping> GetFirebaseDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetFirebaseDataTypeMappings();
        }

        /// <summary>Returns a list of Supabase data type mappings.</summary>
        /// <returns>A list of Supabase data type mappings.</returns>
        public static List<DatatypeMapping> GetSupabaseDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetSupabaseDataTypeMappings();
        }
        #endregion
        #region Newly Added Database Mappings

        /// <summary>Returns a list of MariaDB data type mappings.</summary>
        /// <returns>A list of MariaDB data type mappings.</returns>
        public static List<DatatypeMapping> GetMariaDBDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetMariaDBDataTypeMappings();
        }

        /// <summary>Returns a list of TimeScale data type mappings.</summary>
        /// <returns>A list of TimeScale data type mappings.</returns>
        public static List<DatatypeMapping> GetTimeScaleDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetTimeScaleDataTypeMappings();
        }

        /// <summary>Returns a list of H2Database data type mappings.</summary>
        /// <returns>A list of H2Database data type mappings.</returns>
        public static List<DatatypeMapping> GetH2DatabaseDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetH2DatabaseDataTypeMappings();
        }

        /// <summary>Returns a list of Neo4j data type mappings.</summary>
        /// <returns>A list of Neo4j data type mappings.</returns>
        public static List<DatatypeMapping> GetNeo4jDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetNeo4jDataTypeMappings();
        }

        /// <summary>Returns a list of TigerGraph data type mappings.</summary>
        /// <returns>A list of TigerGraph data type mappings.</returns>
        public static List<DatatypeMapping> GetTigerGraphDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetTigerGraphDataTypeMappings();
        }

        /// <summary>Returns a list of JanusGraph data type mappings.</summary>
        /// <returns>A list of JanusGraph data type mappings.</returns>
        public static List<DatatypeMapping> GetJanusGraphDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetJanusGraphDataTypeMappings();
        }

        /// <summary>Returns a list of OrientDB data type mappings.</summary>
        /// <returns>A list of OrientDB data type mappings.</returns>
        public static List<DatatypeMapping> GetOrientDBDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetOrientDBDataTypeMappings();
        }

        /// <summary>Returns a list of ElasticSearch data type mappings.</summary>
        /// <returns>A list of ElasticSearch data type mappings.</returns>
        public static List<DatatypeMapping> GetElasticSearchDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetElasticSearchDataTypeMappings();
        }

        /// <summary>Returns a list of Solr data type mappings.</summary>
        /// <returns>A list of Solr data type mappings.</returns>
        public static List<DatatypeMapping> GetSolrDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetSolrDataTypeMappings();
        }

        /// <summary>Returns a list of ClickHouse data type mappings.</summary>
        /// <returns>A list of datatype mappings for ClickHouse.</returns>
        public static List<DatatypeMapping> GetClickHouseDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetClickHouseDataTypeMappings();
        }

        /// <summary>Returns a list of RavenDB data type mappings.</summary>
        /// <returns>A list of RavenDB data type mappings.</returns>
        public static List<DatatypeMapping> GetRavenDBDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetRavenDBDataTypeMappings();
        }

        /// <summary>Returns a list of VistaDB data type mappings.</summary>
        /// <returns>A list of VistaDB data type mappings.</returns>
        public static List<DatatypeMapping> GetVistaDBDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetVistaDBDataTypeMappings();
        }

        /// <summary>Returns a list of Memcached data type mappings.</summary>
        /// <returns>A list of Memcached data type mappings.</returns>
        public static List<DatatypeMapping> GetMemcachedDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetMemcachedDataTypeMappings();
        }

        /// <summary>Returns a list of GridGain data type mappings.</summary>
        /// <returns>A list of GridGain data type mappings.</returns>
        public static List<DatatypeMapping> GetGridGainDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetGridGainDataTypeMappings();
        }

        /// <summary>Returns a list of Hazelcast data type mappings.</summary>
        /// <returns>A list of Hazelcast data type mappings.</returns>
        public static List<DatatypeMapping> GetHazelcastDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetHazelcastDataTypeMappings();
        }

        /// <summary>Returns a list of ApacheIgnite data type mappings.</summary>
        /// <returns>A list of ApacheIgnite data type mappings.</returns>
        public static List<DatatypeMapping> GetApacheIgniteDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetApacheIgniteDataTypeMappings();
        }

        /// <summary>Returns a list of ChronicleMap data type mappings.</summary>
        /// <returns>A list of ChronicleMap data type mappings.</returns>
        public static List<DatatypeMapping> GetChronicleMapDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetChronicleMapDataTypeMappings();
        }

        #endregion

        #region Vector Database Mappings

        /// <summary>Returns a list of datatype mappings for PineCone.</summary>
        /// <returns>A list of datatype mappings for PineCone.</returns>
        public static List<DatatypeMapping> GetPineConeDataTypesMapping()
        {
            return DatabaseTypeMappingRepository.GetPineConeDataTypesMapping();
        }

        /// <summary>Returns a list of datatype mappings for Qdrant.</summary>
        /// <returns>A list of datatype mappings for Qdrant.</returns>
        public static List<DatatypeMapping> GetQdrantDataTypesMapping()
        {
            return DatabaseTypeMappingRepository.GetQdrantDataTypesMapping();
        }

        /// <summary>Returns a list of datatype mappings for ShapVector.</summary>
        /// <returns>A list of datatype mappings for ShapVector.</returns>
        public static List<DatatypeMapping> GetShapVectorDataTypesMapping()
        {
            return DatabaseTypeMappingRepository.GetShapVectorDataTypesMapping();
        }

        /// <summary>Returns a list of datatype mappings for Weaviate.</summary>
        /// <returns>A list of datatype mappings for Weaviate.</returns>
        public static List<DatatypeMapping> GetWeaviateDataTypesMapping()
        {
            return DatabaseTypeMappingRepository.GetWeaviateDataTypesMapping();
        }

        /// <summary>Returns a list of datatype mappings for Milvus.</summary>
        /// <returns>A list of datatype mappings for Milvus.</returns>
        public static List<DatatypeMapping> GetMilvusDataTypesMapping()
        {
            return DatabaseTypeMappingRepository.GetMilvusDataTypesMapping();
        }

        /// <summary>Returns a list of datatype mappings for RedisVector.</summary>
        /// <returns>A list of datatype mappings for RedisVector.</returns>
        public static List<DatatypeMapping> GetRedisVectorDataTypesMapping()
        {
            return DatabaseTypeMappingRepository.GetRedisVectorDataTypesMapping();
        }

        /// <summary>Returns a list of datatype mappings for Zilliz.</summary>
        /// <returns>A list of datatype mappings for Zilliz.</returns>
        public static List<DatatypeMapping> GetZillizDataTypesMapping()
        {
            return DatabaseTypeMappingRepository.GetZillizDataTypesMapping();
        }

        /// <summary>Returns a list of datatype mappings for Vespa.</summary>
        /// <returns>A list of datatype mappings for Vespa.</returns>
        public static List<DatatypeMapping> GetVespaDataTypesMapping()
        {
            return DatabaseTypeMappingRepository.GetVespaDataTypesMapping();
        }

        /// <summary>Returns a list of datatype mappings for ChromaDB.</summary>
        /// <returns>A list of datatype mappings for ChromaDB.</returns>
        public static List<DatatypeMapping> GetChromaDBDataTypesMapping()
        {
            return DatabaseTypeMappingRepository.GetChromaDBDataTypesMapping();
        }

        #endregion

        #region Machine Learning and Specialized Format Mappings

        /// <summary>Returns a list of TFRecord data type mappings.</summary>
        /// <returns>A list of TFRecord data type mappings.</returns>
        public static List<DatatypeMapping> GetTFRecordDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetTFRecordDataTypeMappings();
        }

        /// <summary>Returns a list of ONNX data type mappings.</summary>
        /// <returns>A list of ONNX data type mappings.</returns>
        public static List<DatatypeMapping> GetONNXDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetONNXDataTypeMappings();
        }

        /// <summary>Returns a list of PyTorch data type mappings.</summary>
        /// <returns>A list of PyTorch data type mappings.</returns>
        public static List<DatatypeMapping> GetPyTorchDataDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetPyTorchDataDataTypeMappings();
        }

        /// <summary>Returns a list of Scikit-Learn data type mappings.</summary>
        /// <returns>A list of Scikit-Learn data type mappings.</returns>
        public static List<DatatypeMapping> GetScikitLearnDataDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetScikitLearnDataDataTypeMappings();
        }

        /// <summary>Returns a list of HDF5 data type mappings.</summary>
        /// <returns>A list of HDF5 data type mappings.</returns>
        public static List<DatatypeMapping> GetHdf5DataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetHdf5DataTypeMappings();
        }

        /// <summary>Returns a list of LibSVM data type mappings.</summary>
        /// <returns>A list of LibSVM data type mappings.</returns>
        public static List<DatatypeMapping> GetLibSVMDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetLibSVMDataTypeMappings();
        }

        /// <summary>Returns a list of GraphML data type mappings.</summary>
        /// <returns>A list of GraphML data type mappings.</returns>
        public static List<DatatypeMapping> GetGraphMLDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetGraphMLDataTypeMappings();
        }

        /// <summary>Returns a list of DICOM data type mappings.</summary>
        /// <returns>A list of DICOM data type mappings.</returns>
        public static List<DatatypeMapping> GetDICOMDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetDICOMDataTypeMappings();
        }

        /// <summary>Returns a list of LAS data type mappings.</summary>
        /// <returns>A list of LAS data type mappings.</returns>
        public static List<DatatypeMapping> GetLASDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetLASDataTypeMappings();
        }

        /// <summary>Returns a list of RecordIO data type mappings.</summary>
        /// <returns>A list of RecordIO data type mappings.</returns>
        public static List<DatatypeMapping> GetRecordIODataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetRecordIODataTypeMappings();
        }

        #endregion

        #region Workflow and IoT System Mappings

        /// <summary>Returns a list of AWS Step Functions data type mappings.</summary>
        /// <returns>A list of AWS Step Functions data type mappings.</returns>
        public static List<DatatypeMapping> GetAWSStepFunctionsDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetAWSStepFunctionsDataTypeMappings();
        }

        /// <summary>Returns a list of AWS Simple Workflow data type mappings.</summary>
        /// <returns>A list of AWS Simple Workflow data type mappings.</returns>
        public static List<DatatypeMapping> GetAWSSWFDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetAWSSWFDataTypeMappings();
        }

        /// <summary>Returns a list of AWS IoT data type mappings.</summary>
        /// <returns>A list of AWS IoT data type mappings.</returns>
        public static List<DatatypeMapping> GetAWSIoTDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetAWSIoTDataTypeMappings();
        }

        /// <summary>Returns a list of AWS IoT Core data type mappings.</summary>
        /// <returns>A list of AWS IoT Core data type mappings.</returns>
        public static List<DatatypeMapping> GetAWSIoTCoreDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetAWSIoTCoreDataTypeMappings();
        }

        /// <summary>Returns a list of AWS IoT Analytics data type mappings.</summary>
        /// <returns>A list of AWS IoT Analytics data type mappings.</returns>
        public static List<DatatypeMapping> GetAWSIoTAnalyticsDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetAWSIoTAnalyticsDataTypeMappings();
        }

        /// <summary>Returns a list of OPC data type mappings.</summary>
        /// <returns>A list of OPC data type mappings.</returns>
        public static List<DatatypeMapping> GetOPCDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetOPCDataTypeMappings();
        }

        #endregion

        #region Query Engine and Miscellaneous Mappings

        /// <summary>Returns a list of Presto data type mappings.</summary>
        /// <returns>A list of Presto data type mappings.</returns>
        public static List<DatatypeMapping> GetPrestoDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetPrestoDataTypeMappings();
        }

        /// <summary>Returns a list of Trino data type mappings.</summary>
        /// <returns>A list of Trino data type mappings.</returns>
        public static List<DatatypeMapping> GetTrinoDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetTrinoDataTypeMappings();
        }

        /// <summary>Returns a list of Google Sheets data type mappings.</summary>
        /// <returns>A list of Google Sheets data type mappings.</returns>
        public static List<DatatypeMapping> GetGoogleSheetsDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetGoogleSheetsDataTypeMappings();
        }

        /// <summary>Returns a list of MiModel data type mappings.</summary>
        /// <returns>A list of MiModel data type mappings.</returns>
        public static List<DatatypeMapping> GetMiModelDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetMiModelDataTypeMappings();
        }

        #endregion

        #region Web API and Protocol Mappings

        /// <summary>Returns a list of WebAPI data type mappings.</summary>
        /// <returns>A list of WebAPI data type mappings.</returns>
        public static List<DatatypeMapping> GetWebApiDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetWebApiDataTypeMappings();
        }

        /// <summary>Returns a list of REST API data type mappings.</summary>
        /// <returns>A list of REST API data type mappings.</returns>
        public static List<DatatypeMapping> GetRestApiDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetRestApiDataTypeMappings();
        }

        /// <summary>Returns a list of GraphQL data type mappings.</summary>
        /// <returns>A list of GraphQL data type mappings.</returns>
        public static List<DatatypeMapping> GetGraphQLDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetGraphQLDataTypeMappings();
        }

        /// <summary>Returns a list of OData data type mappings.</summary>
        /// <returns>A list of OData data type mappings.</returns>
        public static List<DatatypeMapping> GetODataDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetODataDataTypeMappings();
        }

        /// <summary>Returns a list of ODBC data type mappings.</summary>
        /// <returns>A list of ODBC data type mappings.</returns>
        public static List<DatatypeMapping> GetODBCDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetODBCDataTypeMappings();
        }

        /// <summary>Returns a list of OLE DB data type mappings.</summary>
        /// <returns>A list of OLE DB data type mappings.</returns>
        public static List<DatatypeMapping> GetOLEDBDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetOLEDBDataTypeMappings();
        }

        /// <summary>Returns a list of ADO data type mappings.</summary>
        /// <returns>A list of ADO data type mappings.</returns>
        public static List<DatatypeMapping> GetADODataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetADODataTypeMappings();
        }

        /// <summary>Returns a list of Protocol data type mappings.</summary>
        /// <returns>A list of Protocol data type mappings.</returns>
        public static List<DatatypeMapping> GetProtocolDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetProtocolDataTypeMappings();
        }

        #endregion

        #region Additional Missing Database Mappings

        /// <summary>Returns a list of AWS Redshift data type mappings.</summary>
        /// <returns>A list of AWS Redshift data type mappings.</returns>
        public static List<DatatypeMapping> GetAWSRedshiftDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetAWSRedshiftDataTypeMappings();
        }

        /// <summary>Returns a list of Google BigQuery data type mappings.</summary>
        /// <returns>A list of Google BigQuery data type mappings.</returns>
        public static List<DatatypeMapping> GetGoogleBigQueryDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetGoogleBigQueryDataTypeMappings();
        }

        /// <summary>Returns a list of Azure SQL data type mappings.</summary>
        /// <returns>A list of Azure SQL data type mappings.</returns>
        public static List<DatatypeMapping> GetAzureSQLDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetAzureSQLDataTypeMappings();
        }

        /// <summary>Returns a list of AWS RDS data type mappings.</summary>
        /// <returns>A list of AWS RDS data type mappings.</returns>
        public static List<DatatypeMapping> GetAWSRDSDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetAWSRDSDataTypeMappings();
        }

        /// <summary>Returns a list of SAP Hana data type mappings.</summary>
        /// <returns>A list of SAP Hana data type mappings.</returns>
        public static List<DatatypeMapping> GetHanaDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetHanaDataTypeMappings();
        }

        /// <summary>Returns a list of Google Spanner data type mappings.</summary>
        /// <returns>A list of Google Spanner data type mappings.</returns>
        public static List<DatatypeMapping> GetSpannerDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetSpannerDataTypeMappings();
        }

        /// <summary>Returns a list of Apache Kafka data type mappings.</summary>
        /// <returns>A list of Apache Kafka data type mappings.</returns>
        public static List<DatatypeMapping> GetKafkaDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetKafkaDataTypeMappings();
        }

        /// <summary>Returns a list of RabbitMQ data type mappings.</summary>
        /// <returns>A list of RabbitMQ data type mappings.</returns>
        public static List<DatatypeMapping> GetRabbitMQDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetRabbitMQDataTypeMappings();
        }

        /// <summary>Returns a list of ActiveMQ data type mappings.</summary>
        /// <returns>A list of ActiveMQ data type mappings.</returns>
        public static List<DatatypeMapping> GetActiveMQDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetActiveMQDataTypeMappings();
        }

        /// <summary>Returns a list of Apache Pulsar data type mappings.</summary>
        /// <returns>A list of Apache Pulsar data type mappings.</returns>
        public static List<DatatypeMapping> GetPulsarDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetPulsarDataTypeMappings();
        }

        /// <summary>Returns a list of NATS data type mappings.</summary>
        /// <returns>A list of NATS data type mappings.</returns>
        public static List<DatatypeMapping> GetNatsDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetNatsDataTypeMappings();
        }

        /// <summary>Returns a list of ZeroMQ data type mappings.</summary>
        /// <returns>A list of ZeroMQ data type mappings.</returns>
        public static List<DatatypeMapping> GetZeroMQDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetZeroMQDataTypeMappings();
        }

        /// <summary>Returns a list of AWS Kinesis data type mappings.</summary>
        /// <returns>A list of AWS Kinesis data type mappings.</returns>
        public static List<DatatypeMapping> GetAWSKinesisDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetAWSKinesisDataTypeMappings();
        }

        /// <summary>Returns a list of AWS SQS data type mappings.</summary>
        /// <returns>A list of AWS SQS data type mappings.</returns>
        public static List<DatatypeMapping> GetAWSSQSDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetAWSSQSDataTypeMappings();
        }

        /// <summary>Returns a list of AWS SNS data type mappings.</summary>
        /// <returns>A list of AWS SNS data type mappings.</returns>
        public static List<DatatypeMapping> GetAWSSNSDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetAWSSNSDataTypeMappings();
        }

        /// <summary>Returns a list of Azure Service Bus data type mappings.</summary>
        /// <returns>A list of Azure Service Bus data type mappings.</returns>
        public static List<DatatypeMapping> GetAzureServiceBusDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetAzureServiceBusDataTypeMappings();
        }

        /// <summary>Returns a list of MassTransit data type mappings.</summary>
        /// <returns>A list of MassTransit data type mappings.</returns>
        public static List<DatatypeMapping> GetMassTransitDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetMassTransitDataTypeMappings();
        }

        /// <summary>Returns a list of Apache Flink data type mappings.</summary>
        /// <returns>A list of Apache Flink data type mappings.</returns>
        public static List<DatatypeMapping> GetApacheFlinkDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetApacheFlinkDataTypeMappings();
        }

        /// <summary>Returns a list of Apache Storm data type mappings.</summary>
        /// <returns>A list of Apache Storm data type mappings.</returns>
        public static List<DatatypeMapping> GetApacheStormDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetApacheStormDataTypeMappings();
        }

        /// <summary>Returns a list of Apache Spark Streaming data type mappings.</summary>
        /// <returns>A list of Apache Spark Streaming data type mappings.</returns>
        public static List<DatatypeMapping> GetApacheSparkStreamingDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetApacheSparkStreamingDataTypeMappings();
        }

        /// <summary>Returns a list of Apache Hadoop data type mappings.</summary>
        /// <returns>A list of Apache Hadoop data type mappings.</returns>
        public static List<DatatypeMapping> GetHadoopDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetHadoopDataTypeMappings();
        }

        /// <summary>Returns a list of Apache Kudu data type mappings.</summary>
        /// <returns>A list of Apache Kudu data type mappings.</returns>
        public static List<DatatypeMapping> GetKuduDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetKuduDataTypeMappings();
        }

        /// <summary>Returns a list of Apache Druid data type mappings.</summary>
        /// <returns>A list of Apache Druid data type mappings.</returns>
        public static List<DatatypeMapping> GetDruidDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetDruidDataTypeMappings();
        }

        /// <summary>Returns a list of Apache Pinot data type mappings.</summary>
        /// <returns>A list of Apache Pinot data type mappings.</returns>
        public static List<DatatypeMapping> GetPinotDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetPinotDataTypeMappings();
        }

        /// <summary>Returns a list of Parquet data type mappings.</summary>
        /// <returns>A list of Parquet data type mappings.</returns>
        public static List<DatatypeMapping> GetParquetDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetParquetDataTypeMappings();
        }

        /// <summary>Returns a list of Avro data type mappings.</summary>
        /// <returns>A list of Avro data type mappings.</returns>
        public static List<DatatypeMapping> GetAvroDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetAvroDataTypeMappings();
        }

        /// <summary>Returns a list of ORC data type mappings.</summary>
        /// <returns>A list of ORC data type mappings.</returns>
        public static List<DatatypeMapping> GetORCDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetORCDataTypeMappings();
        }

        /// <summary>Returns a list of Feather data type mappings.</summary>
        /// <returns>A list of Feather data type mappings.</returns>
        public static List<DatatypeMapping> GetFeatherDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetFeatherDataTypeMappings();
        }

        /// <summary>Returns a list of Couchbase data type mappings.</summary>
        /// <returns>A list of Couchbase data type mappings.</returns>
        public static List<DatatypeMapping> GetCouchbaseDataTypeMappings()
        {
            return DatabaseTypeMappingRepository.GetCouchbaseDataTypeMappings();
        }

        #endregion

        #region Legacy Support Method

        /// <summary>
        /// Initializes a list of default query values using comprehensive database type mappings.
        /// </summary>
        /// <returns>A comprehensive list of all database type mappings.</returns>
        public static List<DatatypeMapping> InitQueryDefaultValues()
        {
            return DataTypeMappingRepository.GetAllMappings();
        }

        #endregion
    }
}
