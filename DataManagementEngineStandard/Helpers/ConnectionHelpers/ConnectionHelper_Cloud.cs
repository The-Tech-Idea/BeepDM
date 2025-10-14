using System;
using System.Collections.Generic;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Helpers
{
    /// <summary>
    /// Partial class for Cloud Services connection configurations
    /// </summary>
    public static partial class ConnectionHelper
    {
        /// <summary>
        /// Gets all Cloud Services connection configurations
        /// </summary>
        /// <returns>List of Cloud Services connection configurations</returns>
        public static List<ConnectionDriversConfig> GetCloudConfigs()
        {
            var configs = new List<ConnectionDriversConfig>
            {
                CreateAWSRedshiftConfig(),
                CreateGoogleBigQueryConfig(),
                CreateAWSGlueConfig(),
                CreateAWSAthenaConfig(),
                CreateAzureCloudConfig(),
                CreateDataBricksConfig(),
                CreateFireboltConfig(),
                CreateHologresConfig(),
                CreateSupabaseConfig(),
                CreateAWSStepFunctionsConfig(),
                CreateAWSWorkflowConfig(),
                CreateAWSIoTConfig(),
                CreateAWSIoTCoreConfig(),
                CreateAWSIoTAnalyticsConfig(),
                CreateGoogleCloudStorageConfig(),
                CreateAmazonS3Config(),
                CreateAzureDataFactoryConfig(),
                CreateAzureSynapseConfig(),
                CreateAzureBlobStorageConfig()
            };

            return configs;
        }

        /// <summary>Creates a configuration object for AWS Redshift connection drivers.</summary>
        /// <returns>A configuration object for AWS Redshift connection drivers.</returns>
        public static ConnectionDriversConfig CreateAWSRedshiftConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Npgsql",
                DriverClass = "Npgsql",
                version = "4.1.3.0",
                dllname = "Npgsql.dll",
                AdapterType = "Npgsql.NpgsqlDataAdapter",
                CommandBuilderType = "Npgsql.NpgsqlCommandBuilder",
                DbConnectionType = "Npgsql.NpgsqlConnection",
                ConnectionString = "User ID={UserID};Password={Password};Host={Host};Port={Port};Database={DataBase};",
                iconname = "redshift.svg",
                classHandler = "AWSRedshiftDataSource",
                ADOType = true,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.CLOUD,
                DatasourceType = DataSourceType.AWSRedshift,
                IsMissing = false,
                NuggetVersion = "4.1.3.0",
                NuggetSource = "Npgsql",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Google BigQuery connection drivers.</summary>
        /// <returns>A configuration object for Google BigQuery connection drivers.</returns>
        public static ConnectionDriversConfig CreateGoogleBigQueryConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Google.Cloud.BigQuery.V2",
                DriverClass = "Google.Cloud.BigQuery.V2",
                version = "3.0.0.0",
                dllname = "Google.Cloud.BigQuery.V2.dll",
                ConnectionString = "ProjectId={ProjectId};",
                iconname = "bigquery.svg",
                classHandler = "GoogleBigQueryDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.CLOUD,
                DatasourceType = DataSourceType.GoogleBigQuery,
                IsMissing = false,
                NuggetVersion = "3.0.0.0",
                NuggetSource = "Google.Cloud.BigQuery.V2",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for AWS Glue connection drivers.</summary>
        /// <returns>A configuration object for AWS Glue connection drivers.</returns>
        public static ConnectionDriversConfig CreateAWSGlueConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "AWSSDK.Glue",
                DriverClass = "Amazon.Glue",
                version = "3.7.0.0",
                dllname = "AWSSDK.Glue.dll",
                ConnectionString = "Region={Region};AccessKey={UserID};SecretKey={Password};",
                iconname = "awsglue.svg",
                classHandler = "AWSGlueDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.CLOUD,
                DatasourceType = DataSourceType.AWSGlue,
                IsMissing = false,
                NuggetVersion = "3.7.0.0",
                NuggetSource = "AWSSDK.Glue",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for AWS Athena connection drivers.</summary>
        /// <returns>A configuration object for AWS Athena connection drivers.</returns>
        public static ConnectionDriversConfig CreateAWSAthenaConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "AWSSDK.Athena",
                DriverClass = "Amazon.Athena",
                version = "3.7.0.0",
                dllname = "AWSSDK.Athena.dll",
                ConnectionString = "Region={Region};AccessKey={UserID};SecretKey={Password};S3OutputLocation={S3OutputLocation};",
                iconname = "athena.svg",
                classHandler = "AWSAthenaDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.CLOUD,
                DatasourceType = DataSourceType.AWSAthena,
                IsMissing = false,
                NuggetVersion = "3.7.0.0",
                NuggetSource = "AWSSDK.Athena",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Azure Cloud connection drivers.</summary>
        /// <returns>A configuration object for Azure Cloud connection drivers.</returns>
        public static ConnectionDriversConfig CreateAzureCloudConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Microsoft.Azure.Management.ResourceManager",
                DriverClass = "Microsoft.Azure.Management.ResourceManager",
                version = "3.0.0.0",
                dllname = "Microsoft.Azure.Management.ResourceManager.dll",
                ConnectionString = "SubscriptionId={SubscriptionId};TenantId={TenantId};ClientId={ClientId};ClientSecret={ClientSecret};",
                iconname = "azure.svg",
                classHandler = "AzureCloudDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.CLOUD,
                DatasourceType = DataSourceType.AzureCloud,
                IsMissing = false,
                NuggetVersion = "3.0.0.0",
                NuggetSource = "Microsoft.Azure.Management.ResourceManager",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Databricks connection drivers.</summary>
        /// <returns>A configuration object for Databricks connection drivers.</returns>
        public static ConnectionDriversConfig CreateDataBricksConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Databricks.Client",
                DriverClass = "Databricks.Client",
                version = "1.0.0.0",
                dllname = "Databricks.Client.dll",
                ConnectionString = "WorkspaceUrl={WorkspaceUrl};AccessToken={Password};ClusterId={ClusterId};",
                iconname = "databricks.svg",
                classHandler = "DataBricksDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.CLOUD,
                DatasourceType = DataSourceType.DataBricks,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "Databricks.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Firebolt connection drivers.</summary>
        /// <returns>A configuration object for Firebolt connection drivers.</returns>
        public static ConnectionDriversConfig CreateFireboltConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Firebolt.Client",
                DriverClass = "Firebolt.Client",
                version = "1.0.0.0",
                dllname = "Firebolt.Client.dll",
                ConnectionString = "Engine={Engine};Account={Account};Database={Database};UserID={UserID};Password={Password};",
                iconname = "firebolt.svg",
                classHandler = "FireboltDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.CLOUD,
                DatasourceType = DataSourceType.Firebolt,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "Firebolt.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Hologres connection drivers.</summary>
        /// <returns>A configuration object for Hologres connection drivers.</returns>
        public static ConnectionDriversConfig CreateHologresConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Hologres.Client",
                DriverClass = "Hologres.Client",
                version = "1.0.0.0",
                dllname = "Hologres.Client.dll",
                ConnectionString = "InstanceId={InstanceId};Host={Host};Database={Database};UserID={UserID};Password={Password};",
                iconname = "hologres.svg",
                classHandler = "HologresDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.CLOUD,
                DatasourceType = DataSourceType.Hologres,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "Hologres.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Supabase connection drivers.</summary>
        /// <returns>A configuration object for Supabase connection drivers.</returns>
        public static ConnectionDriversConfig CreateSupabaseConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Supabase",
                DriverClass = "Supabase",
                version = "1.0.0.0",
                dllname = "Supabase.dll",
                ConnectionString = "ProjectUrl={ProjectUrl};ApiKey={Password};",
                iconname = "supabase.svg",
                classHandler = "SupabaseDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.CLOUD,
                DatasourceType = DataSourceType.Supabase,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "Supabase",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for AWS Step Functions connection drivers.</summary>
        /// <returns>A configuration object for AWS Step Functions connection drivers.</returns>
        public static ConnectionDriversConfig CreateAWSStepFunctionsConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "AWSSDK.StepFunctions",
                DriverClass = "Amazon.StepFunctions",
                version = "3.7.0.0",
                dllname = "AWSSDK.StepFunctions.dll",
                ConnectionString = "Region={Region};AccessKey={UserID};SecretKey={Password};StateMachineArn={StateMachineArn};",
                iconname = "stepfunctions.svg",
                classHandler = "AWSStepFunctionsDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Workflow,
                DatasourceType = DataSourceType.AWSStepFunctions,
                IsMissing = false,
                NuggetVersion = "3.7.0.0",
                NuggetSource = "AWSSDK.StepFunctions",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for AWS Simple Workflow connection drivers.</summary>
        /// <returns>A configuration object for AWS Simple Workflow connection drivers.</returns>
        public static ConnectionDriversConfig CreateAWSWorkflowConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "AWSSDK.SimpleWorkflow",
                DriverClass = "Amazon.SimpleWorkflow",
                version = "3.7.0.0",
                dllname = "AWSSDK.SimpleWorkflow.dll",
                ConnectionString = "Region={Region};AccessKey={UserID};SecretKey={Password};TaskList={TaskList};",
                iconname = "swf.svg",
                classHandler = "AWSWorkflowDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Workflow,
                DatasourceType = DataSourceType.AWSSWF,
                IsMissing = false,
                NuggetVersion = "3.7.0.0",
                NuggetSource = "AWSSDK.SimpleWorkflow",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for AWS IoT connection drivers.</summary>
        /// <returns>A configuration object for AWS IoT connection drivers.</returns>
        public static ConnectionDriversConfig CreateAWSIoTConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "AWSSDK.IoT",
                DriverClass = "Amazon.IoT",
                version = "3.7.0.0",
                dllname = "AWSSDK.IoT.dll",
                ConnectionString = "Region={Region};AccessKey={UserID};SecretKey={Password};ThingName={ThingName};",
                iconname = "iot.svg",
                classHandler = "AWSIoTDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.IoT,
                DatasourceType = DataSourceType.AWSIoT,
                IsMissing = false,
                NuggetVersion = "3.7.0.0",
                NuggetSource = "AWSSDK.IoT",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for AWS IoT Core connection drivers.</summary>
        /// <returns>A configuration object for AWS IoT Core connection drivers.</returns>
        public static ConnectionDriversConfig CreateAWSIoTCoreConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "AWSSDK.IotData",
                DriverClass = "Amazon.IotData",
                version = "3.7.0.0",
                dllname = "AWSSDK.IotData.dll",
                ConnectionString = "Region={Region};AccessKey={UserID};SecretKey={Password};Endpoint={Host};",
                iconname = "iotcore.svg",
                classHandler = "AWSIoTCoreDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.IoT,
                DatasourceType = DataSourceType.AWSIoTCore,
                IsMissing = false,
                NuggetVersion = "3.7.0.0",
                NuggetSource = "AWSSDK.IotData",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for AWS IoT Analytics connection drivers.</summary>
        /// <returns>A configuration object for AWS IoT Analytics connection drivers.</returns>
        public static ConnectionDriversConfig CreateAWSIoTAnalyticsConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "AWSSDK.IoTAnalytics",
                DriverClass = "Amazon.IoTAnalytics",
                version = "3.7.0.0",
                dllname = "AWSSDK.IoTAnalytics.dll",
                ConnectionString = "Region={Region};AccessKey={UserID};SecretKey={Password};ChannelName={ChannelName};",
                iconname = "iotanalytics.svg",
                classHandler = "AWSIoTAnalyticsDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.IoT,
                DatasourceType = DataSourceType.AWSIoTAnalytics,
                IsMissing = false,
                NuggetVersion = "3.7.0.0",
                NuggetSource = "AWSSDK.IoTAnalytics",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Google Cloud Storage connection drivers.</summary>
        /// <returns>A configuration object for Google Cloud Storage connection drivers.</returns>
        public static ConnectionDriversConfig CreateGoogleCloudStorageConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Google.Cloud.Storage.V1",
                DriverClass = "Google.Cloud.Storage.V1",
                version = "4.0.0.0",
                dllname = "Google.Cloud.Storage.V1.dll",
                ConnectionString = "ProjectId={ProjectId};BucketName={BucketName};",
                iconname = "gcs.svg",
                classHandler = "GoogleCloudStorageDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.CLOUD,
                DatasourceType = DataSourceType.GoogleCloudStorage,
                IsMissing = false,
                NuggetVersion = "4.0.0.0",
                NuggetSource = "Google.Cloud.Storage.V1",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Amazon S3 connection drivers.</summary>
        /// <returns>A configuration object for Amazon S3 connection drivers.</returns>
        public static ConnectionDriversConfig CreateAmazonS3Config()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "AWSSDK.S3",
                DriverClass = "Amazon.S3",
                version = "3.7.0.0",
                dllname = "AWSSDK.S3.dll",
                ConnectionString = "Region={Region};AccessKey={UserID};SecretKey={Password};BucketName={BucketName};",
                iconname = "s3.svg",
                classHandler = "AmazonS3DataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.CLOUD,
                DatasourceType = DataSourceType.AmazonS3,
                IsMissing = false,
                NuggetVersion = "3.7.0.0",
                NuggetSource = "AWSSDK.S3",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Azure Data Factory connection drivers.</summary>
        /// <returns>A configuration object for Azure Data Factory connection drivers.</returns>
        public static ConnectionDriversConfig CreateAzureDataFactoryConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Microsoft.Azure.Management.DataFactory",
                DriverClass = "Microsoft.Azure.Management.DataFactory",
                version = "4.0.0.0",
                dllname = "Microsoft.Azure.Management.DataFactory.dll",
                ConnectionString = "SubscriptionId={SubscriptionId};TenantId={TenantId};ClientId={ClientId};ClientSecret={ClientSecret};ResourceGroupName={ResourceGroup};",
                iconname = "adf.svg",
                classHandler = "AzureDataFactoryDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.DataPipeline,
                DatasourceType = DataSourceType.AzureCloud,
                IsMissing = false,
                NuggetVersion = "4.0.0.0",
                NuggetSource = "Microsoft.Azure.Management.DataFactory",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Azure Synapse connection drivers.</summary>
        /// <returns>A configuration object for Azure Synapse connection drivers.</returns>
        public static ConnectionDriversConfig CreateAzureSynapseConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "System.Data.SqlClient",
                DriverClass = "System.Data.SqlClient",
                version = "4.6.1.1",
                dllname = "System.Data.SqlClient.dll",
                AdapterType = "System.Data.SqlClient.SqlDataAdapter",
                CommandBuilderType = "System.Data.SqlClient.SqlCommandBuilder",
                DbConnectionType = "System.Data.SqlClient.SqlConnection",
                DbTransactionType = "System.Data.SqlClient.SqlTransaction",
                ConnectionString = "Server={Host};Database={Database};User Id ={UserID}; Password ={Password};Encrypt=True;TrustServerCertificate=False;",
                iconname = "synapse.svg",
                classHandler = "AzureSynapseDataSource",
                ADOType = true,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.DataWarehouse,
                DatasourceType = DataSourceType.AzureCloud,
                IsMissing = false,
                NuggetVersion = "4.6.1.1",
                NuggetSource = "System.Data.SqlClient",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Azure Blob Storage connection drivers.</summary>
        /// <returns>A configuration object for Azure Blob Storage connection drivers.</returns>
        public static ConnectionDriversConfig CreateAzureBlobStorageConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Azure.Storage.Blobs",
                DriverClass = "Azure.Storage.Blobs",
                version = "12.0.0.0",
                dllname = "Azure.Storage.Blobs.dll",
                ConnectionString = "DefaultEndpointsProtocol=https;AccountName={AccountName};AccountKey={Password};EndpointSuffix=core.windows.net",
                iconname = "blob.svg",
                classHandler = "AzureBlobStorageDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.CLOUD,
                DatasourceType = DataSourceType.AzureCloud,
                IsMissing = false,
                NuggetVersion = "12.0.0.0",
                NuggetSource = "Azure.Storage.Blobs",
                NuggetMissing = false
            };
        }
    }
}