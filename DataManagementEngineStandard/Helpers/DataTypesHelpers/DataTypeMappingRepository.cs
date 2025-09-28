using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.DriversConfigurations;

namespace TheTechIdea.Beep.Helpers.DataTypesHelpers
{
    /// <summary>
    /// Helper class for retrieving data type mappings for different data sources.
    /// </summary>
    public static class DataTypeMappingRepository
    {
        /// <summary>Gets the data type mappings for a specific data source.</summary>
        /// <param name="DSname">The name of the data source.</param>
        /// <param name="DMEEditor">The IDMEEditor instance used for accessing the data source.</param>
        /// <returns>The List of DataTypeMapping for DataSource.</returns>
        public static List<DatatypeMapping> GetDataTypes(string DSname, IDMEEditor DMEEditor)
        {
            IDataSource ds;
            try
            {
                if (DSname != null)
                {
                    ds = DMEEditor.GetDataSource(DSname);
                    if (DMEEditor.ConfigEditor.DataTypesMap == null)
                    {
                        DMEEditor.ConfigEditor.ReadDataTypeFile();
                    }

                    AssemblyClassDefinition classhandler = DMEEditor.GetDataSourceClass(DSname);
                    if (classhandler != null)
                    {
                        return DMEEditor.ConfigEditor.DataTypesMap.Where(x => x.DataSourceName.Equals(classhandler.className, StringComparison.InvariantCultureIgnoreCase)).ToList();
                    }
                    else
                    {
                        DMEEditor.AddLogMessage("Fail", "Could not Find Class Handler for " + DSname, DateTime.Now, -1, null, Errors.Failed);
                    }
                }
                else
                {
                    DMEEditor.AddLogMessage("Fail", "Could not Convert Field Type to Provider Type " + DSname, DateTime.Now, -1, null, Errors.Failed);
                }
            }
            catch (Exception ex)
            {
                string mes = "";
                DMEEditor.AddLogMessage(ex.Message, "Could not Convert Field Type to Provider Type " + mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return null;
        }

        /// <summary>Gets the data type mappings for a specific data source type.</summary>
        /// <param name="DSname">The type of the data source.</param>
        /// <param name="DMEEditor">The IDMEEditor instance used for accessing the data source.</param>
        /// <returns>The List of DataTypeMapping for DataSource.</returns>
        public static List<DatatypeMapping> GetDataTypes(DataSourceType DSname, IDMEEditor DMEEditor)
        {
            try
            {
                if (DSname != DataSourceType.NONE)
                {
                    var classhandler = DMEEditor.ConfigEditor.DataDriversClasses.Where(x => x.DatasourceType == DSname).FirstOrDefault();

                    if (DMEEditor.ConfigEditor.DataTypesMap == null)
                    {
                        DMEEditor.ConfigEditor.ReadDataTypeFile();
                    }

                    if (classhandler != null)
                    {
                        return DMEEditor.ConfigEditor.DataTypesMap.Where(x => x.DataSourceName.Equals(classhandler.classHandler, StringComparison.InvariantCultureIgnoreCase)).ToList();
                    }
                    else
                    {
                        DMEEditor.AddLogMessage("Fail", "Could not Find Class Handler for " + DSname, DateTime.Now, -1, null, Errors.Failed);
                    }
                }
                else
                {
                    DMEEditor.AddLogMessage("Fail", "Could not Convert Field Type to Provider Type " + DSname, DateTime.Now, -1, null, Errors.Failed);
                }
            }
            catch (Exception ex)
            {
                string mes = "";
                DMEEditor.AddLogMessage(ex.Message, "Could not Convert Field Type to Provider Type " + mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return null;
        }

        /// <summary>Returns a comprehensive list of all datatype mappings for all supported databases.</summary>
        /// <returns>A comprehensive list of datatype mappings.</returns>
        public static List<DatatypeMapping> GetAllMappings()
        {
            List<DatatypeMapping> ls = new List<DatatypeMapping>();
            
            // Traditional RDBMS
            ls.AddRange(DatabaseTypeMappingRepository.GenerateOracleDataTypesMapping());
            ls.AddRange(DatabaseTypeMappingRepository.GenerateSqlServerDataTypesMapping());
            ls.AddRange(DatabaseTypeMappingRepository.GenerateSQLiteDataTypesMapping());
            ls.AddRange(DatabaseTypeMappingRepository.GenerateSqlCompactDataTypesMapping());
            ls.AddRange(DatabaseTypeMappingRepository.GetPostgreDataTypesMapping());
            ls.AddRange(DatabaseTypeMappingRepository.GetMySqlDataTypesMapping());
            ls.AddRange(DatabaseTypeMappingRepository.GetFireBirdDataTypesMapping());
            ls.AddRange(DatabaseTypeMappingRepository.GetDuckDBDataTypesMapping());
            ls.AddRange(DatabaseTypeMappingRepository.GetDB2DataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetSybaseDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetCockroachDBDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetVerticaDataTypesMapping());
            ls.AddRange(DatabaseTypeMappingRepository.GetTeradataDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetMariaDBDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetTimeScaleDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetH2DatabaseDataTypeMappings());

            // Cloud Platform Services
            ls.AddRange(DatabaseTypeMappingRepository.GetAWSRedshiftDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetGoogleBigQueryDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetAzureSQLDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetAWSRDSDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetHanaDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetSpannerDataTypeMappings());

            // NoSQL Databases
            ls.AddRange(DatabaseTypeMappingRepository.GetMongoDBDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetCassandraDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetRedisDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetDynamoDBDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetLiteDBDataTypesMapping());
            ls.AddRange(DatabaseTypeMappingRepository.GetArangoDBDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetHBaseDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetRavenDBDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetVistaDBDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetCouchbaseDataTypeMappings());

            // Graph Databases
            ls.AddRange(DatabaseTypeMappingRepository.GetNeo4jDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetTigerGraphDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetJanusGraphDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetOrientDBDataTypeMappings());

            // Search and Analytics
            ls.AddRange(DatabaseTypeMappingRepository.GetElasticSearchDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetSolrDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetClickHouseDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetInfluxDBDataTypeMappings());

            // In-Memory Databases
            ls.AddRange(DatabaseTypeMappingRepository.GetMemcachedDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetGridGainDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetHazelcastDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetApacheIgniteDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetChronicleMapDataTypeMappings());

            // Streaming and Messaging
            ls.AddRange(DatabaseTypeMappingRepository.GetKafkaDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetRabbitMQDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetActiveMQDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetPulsarDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetNatsDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetZeroMQDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetAWSKinesisDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetAWSSQSDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetAWSSNSDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetAzureServiceBusDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetMassTransitDataTypeMappings());

            // Stream Processing
            ls.AddRange(DatabaseTypeMappingRepository.GetApacheFlinkDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetApacheStormDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetApacheSparkStreamingDataTypeMappings());

            // Big Data / Columnar
            ls.AddRange(DatabaseTypeMappingRepository.GetHadoopDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetKuduDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetDruidDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetPinotDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetParquetDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetAvroDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetORCDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetFeatherDataTypeMappings());

            // Machine Learning
            ls.AddRange(DatabaseTypeMappingRepository.GetTFRecordDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetONNXDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetPyTorchDataDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetScikitLearnDataDataTypeMappings());

            // Specialized Formats
            ls.AddRange(DatabaseTypeMappingRepository.GetHdf5DataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetLibSVMDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetGraphMLDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetDICOMDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetLASDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetRecordIODataTypeMappings());

            // Workflow Systems
            ls.AddRange(DatabaseTypeMappingRepository.GetAWSStepFunctionsDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetAWSSWFDataTypeMappings());

            // IoT Systems
            ls.AddRange(DatabaseTypeMappingRepository.GetAWSIoTDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetAWSIoTCoreDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetAWSIoTAnalyticsDataTypeMappings());

            // Industrial Systems
            ls.AddRange(DatabaseTypeMappingRepository.GetOPCDataTypeMappings());

            // Query Engines
            ls.AddRange(DatabaseTypeMappingRepository.GetPrestoDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetTrinoDataTypeMappings());

            // Miscellaneous
            ls.AddRange(DatabaseTypeMappingRepository.GetGoogleSheetsDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetMiModelDataTypeMappings());

            // Web APIs and Protocols
            ls.AddRange(DatabaseTypeMappingRepository.GetWebApiDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetRestApiDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetGraphQLDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetODataDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetODBCDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetOLEDBDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetADODataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetProtocolDataTypeMappings());

            // Cloud Services
            ls.AddRange(DatabaseTypeMappingRepository.GetBerkeleyDBDataTypesMapping());
            ls.AddRange(DatabaseTypeMappingRepository.GetSnowflakeDataTypesMapping());
            ls.AddRange(DatabaseTypeMappingRepository.GetAzureCosmosDBDataTypesMapping());
            ls.AddRange(DatabaseTypeMappingRepository.GetFirebaseDataTypeMappings());
            ls.AddRange(DatabaseTypeMappingRepository.GetSupabaseDataTypeMappings());

            // Vector Databases
            ls.AddRange(DatabaseTypeMappingRepository.GetPineConeDataTypesMapping());
            ls.AddRange(DatabaseTypeMappingRepository.GetQdrantDataTypesMapping());
            ls.AddRange(DatabaseTypeMappingRepository.GetShapVectorDataTypesMapping());
            ls.AddRange(DatabaseTypeMappingRepository.GetWeaviateDataTypesMapping());
            ls.AddRange(DatabaseTypeMappingRepository.GetMilvusDataTypesMapping());
            ls.AddRange(DatabaseTypeMappingRepository.GetRedisVectorDataTypesMapping());
            ls.AddRange(DatabaseTypeMappingRepository.GetZillizDataTypesMapping());
            ls.AddRange(DatabaseTypeMappingRepository.GetVespaDataTypesMapping());
            ls.AddRange(DatabaseTypeMappingRepository.GetChromaDBDataTypesMapping());

            return ls;
        }
    }
}