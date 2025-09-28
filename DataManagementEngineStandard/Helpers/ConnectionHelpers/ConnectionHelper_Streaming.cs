using System;
using System.Collections.Generic;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Helpers
{
    /// <summary>
    /// Partial class for Streaming and Messaging connection configurations
    /// </summary>
    public static partial class ConnectionHelper
    {
        /// <summary>
        /// Gets all Streaming and Messaging connection configurations
        /// </summary>
        /// <returns>List of Streaming and Messaging connection configurations</returns>
        public static List<ConnectionDriversConfig> GetStreamingConfigs()
        {
            var configs = new List<ConnectionDriversConfig>
            {
                CreateKafkaConfig(),
                CreateRabbitMQConfig(),
                CreateActiveMQConfig(),
                CreatePulsarConfig(),
                CreateMassTransitConfig(),
                CreateNatsConfig(),
                CreateZeroMQConfig(),
                CreateAWSKinesisConfig(),
                CreateAWSSQSConfig(),
                CreateAWSSNSConfig(),
                CreateAzureServiceBusConfig(),
                CreateEventHubsConfig(),
                CreateApacheFlinkConfig(),
                CreateApacheStormConfig(),
                CreateApacheSparkStreamingConfig()
            };

            return configs;
        }

        /// <summary>Creates a configuration object for Kafka connection drivers.</summary>
        /// <returns>A configuration object for Kafka-specific configuration.</returns>
        public static ConnectionDriversConfig CreateKafkaConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "kafka-guid",
                PackageName = "Kafka",
                DriverClass = "Kafka",
                version = "2.8.0.0",
                dllname = "Kafka.dll",
                AdapterType = "Kafka.KafkaDataAdapter",
                DbConnectionType = "Kafka.KafkaConnection",
                ConnectionString = "BrokerList=your-broker-list;ClientId=your-client-id;",
                iconname = "kafka.svg",
                classHandler = "KafkaDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.STREAM,
                DatasourceType = DataSourceType.Kafka,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for RabbitMQ connection drivers.</summary>
        /// <returns>A configuration object for RabbitMQ connection drivers.</returns>
        public static ConnectionDriversConfig CreateRabbitMQConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "RabbitMQ.Client",
                DriverClass = "RabbitMQ.Client",
                version = "6.0.0.0",
                dllname = "RabbitMQ.Client.dll",
                ConnectionString = "Host={Host};Port={Port};Username={UserID};Password={Password};VirtualHost={VirtualHost};",
                iconname = "rabbitmq.svg",
                classHandler = "RabbitMQDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.QUEUE,
                DatasourceType = DataSourceType.RabbitMQ,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for ActiveMQ connection drivers.</summary>
        /// <returns>A configuration object for ActiveMQ connection drivers.</returns>
        public static ConnectionDriversConfig CreateActiveMQConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Apache.NMS.ActiveMQ",
                DriverClass = "Apache.NMS.ActiveMQ",
                version = "1.8.0.0",
                dllname = "Apache.NMS.ActiveMQ.dll",
                ConnectionString = "BrokerUrl={BrokerUrl};Username={UserID};Password={Password};",
                iconname = "activemq.svg",
                classHandler = "ActiveMQDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.QUEUE,
                DatasourceType = DataSourceType.ActiveMQ,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for Apache Pulsar connection drivers.</summary>
        /// <returns>A configuration object for Apache Pulsar connection drivers.</returns>
        public static ConnectionDriversConfig CreatePulsarConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "DotPulsar",
                DriverClass = "DotPulsar",
                version = "2.0.0.0",
                dllname = "DotPulsar.dll",
                ConnectionString = "ServiceUrl={ServiceUrl};AdminUrl={AdminUrl};AuthParams={AuthParams};",
                iconname = "pulsar.svg",
                classHandler = "PulsarDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.STREAM,
                DatasourceType = DataSourceType.Pulsar,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for MassTransit connection drivers.</summary>
        /// <returns>A configuration object for MassTransit connection drivers.</returns>
        public static ConnectionDriversConfig CreateMassTransitConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "MassTransit",
                DriverClass = "MassTransit",
                version = "8.0.0.0",
                dllname = "MassTransit.dll",
                ConnectionString = "TransportType={TransportType};Host={Host};Username={UserID};Password={Password};",
                iconname = "masstransit.svg",
                classHandler = "MassTransitDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.QUEUE,
                DatasourceType = DataSourceType.MassTransit,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for NATS connection drivers.</summary>
        /// <returns>A configuration object for NATS connection drivers.</returns>
        public static ConnectionDriversConfig CreateNatsConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "NATS.Client",
                DriverClass = "NATS.Client",
                version = "1.0.0.0",
                dllname = "NATS.Client.dll",
                ConnectionString = "Servers={Servers};MaxReconnects={MaxReconnects};",
                iconname = "nats.svg",
                classHandler = "NatsDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.STREAM,
                DatasourceType = DataSourceType.Nats,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for ZeroMQ connection drivers.</summary>
        /// <returns>A configuration object for ZeroMQ connection drivers.</returns>
        public static ConnectionDriversConfig CreateZeroMQConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "NetMQ",
                DriverClass = "NetMQ",
                version = "4.0.0.0",
                dllname = "NetMQ.dll",
                ConnectionString = "SocketType={SocketType};HighWaterMark={HighWaterMark};",
                iconname = "zeromq.svg",
                classHandler = "ZeroMQDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.STREAM,
                DatasourceType = DataSourceType.ZeroMQ,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for AWS Kinesis connection drivers.</summary>
        /// <returns>A configuration object for AWS Kinesis connection drivers.</returns>
        public static ConnectionDriversConfig CreateAWSKinesisConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "AWSSDK.Kinesis",
                DriverClass = "Amazon.Kinesis",
                version = "3.7.0.0",
                dllname = "AWSSDK.Kinesis.dll",
                ConnectionString = "Region={Region};AccessKey={UserID};SecretKey={Password};StreamName={StreamName};",
                iconname = "kinesis.svg",
                classHandler = "AWSKinesisDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.STREAM,
                DatasourceType = DataSourceType.AWSKinesis,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for AWS SQS connection drivers.</summary>
        /// <returns>A configuration object for AWS SQS connection drivers.</returns>
        public static ConnectionDriversConfig CreateAWSSQSConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "AWSSDK.SQS",
                DriverClass = "Amazon.SQS",
                version = "3.7.0.0",
                dllname = "AWSSDK.SQS.dll",
                ConnectionString = "Region={Region};AccessKey={UserID};SecretKey={Password};QueueUrl={QueueUrl};",
                iconname = "sqs.svg",
                classHandler = "AWSSQSDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.QUEUE,
                DatasourceType = DataSourceType.AWSSQS,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for AWS SNS connection drivers.</summary>
        /// <returns>A configuration object for AWS SNS connection drivers.</returns>
        public static ConnectionDriversConfig CreateAWSSNSConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "AWSSDK.SimpleNotificationService",
                DriverClass = "Amazon.SimpleNotificationService",
                version = "3.7.0.0",
                dllname = "AWSSDK.SimpleNotificationService.dll",
                ConnectionString = "Region={Region};AccessKey={UserID};SecretKey={Password};TopicArn={TopicArn};",
                iconname = "sns.svg",
                classHandler = "AWSSNSDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.QUEUE,
                DatasourceType = DataSourceType.AWSSNS,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for Azure Service Bus connection drivers.</summary>
        /// <returns>A configuration object for Azure Service Bus connection drivers.</returns>
        public static ConnectionDriversConfig CreateAzureServiceBusConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Azure.Messaging.ServiceBus",
                DriverClass = "Azure.Messaging.ServiceBus",
                version = "7.0.0.0",
                dllname = "Azure.Messaging.ServiceBus.dll",
                ConnectionString = "Endpoint=sb://{Host}.servicebus.windows.net/;SharedAccessKeyName={UserID};SharedAccessKey={Password};",
                iconname = "servicebus.svg",
                classHandler = "AzureServiceBusDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.QUEUE,
                DatasourceType = DataSourceType.AzureServiceBus,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for Azure Event Hubs connection drivers.</summary>
        /// <returns>A configuration object for Azure Event Hubs connection drivers.</returns>
        public static ConnectionDriversConfig CreateEventHubsConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Azure.Messaging.EventHubs",
                DriverClass = "Azure.Messaging.EventHubs",
                version = "5.0.0.0",
                dllname = "Azure.Messaging.EventHubs.dll",
                ConnectionString = "Endpoint=sb://{Host}.servicebus.windows.net/;SharedAccessKeyName={UserID};SharedAccessKey={Password};EntityPath={Database};",
                iconname = "eventhubs.svg",
                classHandler = "EventHubsDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.STREAM,
                DatasourceType = DataSourceType.AzureServiceBus,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for Apache Flink connection drivers.</summary>
        /// <returns>A configuration object for Apache Flink connection drivers.</returns>
        public static ConnectionDriversConfig CreateApacheFlinkConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Apache.Flink",
                DriverClass = "Apache.Flink",
                version = "1.15.0.0",
                dllname = "Apache.Flink.dll",
                ConnectionString = "JobManagerUrl={Host}:{Port};",
                iconname = "flink.svg",
                classHandler = "ApacheFlinkDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.StreamProcessing,
                DatasourceType = DataSourceType.ApacheFlink,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for Apache Storm connection drivers.</summary>
        /// <returns>A configuration object for Apache Storm connection drivers.</returns>
        public static ConnectionDriversConfig CreateApacheStormConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Apache.Storm",
                DriverClass = "Apache.Storm",
                version = "2.0.0.0",
                dllname = "Apache.Storm.dll",
                ConnectionString = "NimbusHost={Host};NimbusPort={Port};",
                iconname = "storm.svg",
                classHandler = "ApacheStormDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.StreamProcessing,
                DatasourceType = DataSourceType.ApacheStorm,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for Apache Spark Streaming connection drivers.</summary>
        /// <returns>A configuration object for Apache Spark Streaming connection drivers.</returns>
        public static ConnectionDriversConfig CreateApacheSparkStreamingConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Apache.Spark.Streaming",
                DriverClass = "Apache.Spark.Streaming",
                version = "3.0.0.0",
                dllname = "Apache.Spark.Streaming.dll",
                ConnectionString = "SparkMaster={Host}:{Port};ApplicationName={Database};",
                iconname = "sparkstreaming.svg",
                classHandler = "ApacheSparkStreamingDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.StreamProcessing,
                DatasourceType = DataSourceType.ApacheSparkStreaming,
                IsMissing = false
            };
        }
    }
}