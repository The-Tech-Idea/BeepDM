using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.DriversConfigurations;

namespace TheTechIdea.Beep.Helpers.DataTypesHelpers
{
    /// <summary>
    /// Repository containing Streaming and Messaging platform specific type mappings.
    /// </summary>
    public static partial class DatabaseTypeMappingRepository
    {
        /// <summary>Returns a list of Apache Kafka data type mappings.</summary>
        /// <returns>A list of Apache Kafka data type mappings.</returns>
        public static List<DatatypeMapping> GetKafkaDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Kafka uses Avro, JSON, or other serialization formats
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "STRING", DataSourceName = "KafkaDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BYTES", DataSourceName = "KafkaDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INT", DataSourceName = "KafkaDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "LONG", DataSourceName = "KafkaDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "FLOAT", DataSourceName = "KafkaDataSource", NetDataType = "System.Single", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DOUBLE", DataSourceName = "KafkaDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BOOLEAN", DataSourceName = "KafkaDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "ARRAY", DataSourceName = "KafkaDataSource", NetDataType = "System.Array", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "MAP", DataSourceName = "KafkaDataSource", NetDataType = "System.Collections.Generic.Dictionary<string, object>", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "UNION", DataSourceName = "KafkaDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "RECORD", DataSourceName = "KafkaDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "ENUM", DataSourceName = "KafkaDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "FIXED", DataSourceName = "KafkaDataSource", NetDataType = "System.Byte[]", Fav = false }
            };
        }

        /// <summary>Returns a list of RabbitMQ data type mappings.</summary>
        /// <returns>A list of RabbitMQ data type mappings.</returns>
        public static List<DatatypeMapping> GetRabbitMQDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // RabbitMQ primarily deals with message payloads as bytes
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "binary", DataSourceName = "RabbitMQDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "RabbitMQDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "json", DataSourceName = "RabbitMQDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "xml", DataSourceName = "RabbitMQDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "protobuf", DataSourceName = "RabbitMQDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "avro", DataSourceName = "RabbitMQDataSource", NetDataType = "System.Byte[]", Fav = false }
            };
        }

        /// <summary>Returns a list of ActiveMQ data type mappings.</summary>
        /// <returns>A list of ActiveMQ data type mappings.</returns>
        public static List<DatatypeMapping> GetActiveMQDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // ActiveMQ JMS message types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TextMessage", DataSourceName = "ActiveMQDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BytesMessage", DataSourceName = "ActiveMQDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "ObjectMessage", DataSourceName = "ActiveMQDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "StreamMessage", DataSourceName = "ActiveMQDataSource", NetDataType = "System.IO.Stream", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "MapMessage", DataSourceName = "ActiveMQDataSource", NetDataType = "System.Collections.Generic.Dictionary<string, object>", Fav = false }
            };
        }

        /// <summary>Returns a list of Apache Pulsar data type mappings.</summary>
        /// <returns>A list of Apache Pulsar data type mappings.</returns>
        public static List<DatatypeMapping> GetPulsarDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Pulsar Schema types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BYTES", DataSourceName = "PulsarDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "STRING", DataSourceName = "PulsarDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "AVRO", DataSourceName = "PulsarDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "JSON", DataSourceName = "PulsarDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "PROTOBUF", DataSourceName = "PulsarDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INT8", DataSourceName = "PulsarDataSource", NetDataType = "System.SByte", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INT16", DataSourceName = "PulsarDataSource", NetDataType = "System.Int16", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INT32", DataSourceName = "PulsarDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INT64", DataSourceName = "PulsarDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "FLOAT", DataSourceName = "PulsarDataSource", NetDataType = "System.Single", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DOUBLE", DataSourceName = "PulsarDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BOOLEAN", DataSourceName = "PulsarDataSource", NetDataType = "System.Boolean", Fav = false }
            };
        }

        /// <summary>Returns a list of NATS data type mappings.</summary>
        /// <returns>A list of NATS data type mappings.</returns>
        public static List<DatatypeMapping> GetNatsDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // NATS is primarily byte-oriented
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "bytes", DataSourceName = "NatsDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "NatsDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "json", DataSourceName = "NatsDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "protobuf", DataSourceName = "NatsDataSource", NetDataType = "System.Byte[]", Fav = false }
            };
        }

        /// <summary>Returns a list of ZeroMQ data type mappings.</summary>
        /// <returns>A list of ZeroMQ data type mappings.</returns>
        public static List<DatatypeMapping> GetZeroMQDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // ZeroMQ is message-oriented, primarily bytes
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "bytes", DataSourceName = "ZeroMQDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "ZeroMQDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "multipart", DataSourceName = "ZeroMQDataSource", NetDataType = "System.Collections.Generic.List<byte[]>", Fav = false }
            };
        }

        /// <summary>Returns a list of AWS Kinesis data type mappings.</summary>
        /// <returns>A list of AWS Kinesis data type mappings.</returns>
        public static List<DatatypeMapping> GetAWSKinesisDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Kinesis record data
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Data", DataSourceName = "AWSKinesisDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "PartitionKey", DataSourceName = "AWSKinesisDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "SequenceNumber", DataSourceName = "AWSKinesisDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "ApproximateArrivalTimestamp", DataSourceName = "AWSKinesisDataSource", NetDataType = "System.DateTime", Fav = false }
            };
        }

        /// <summary>Returns a list of AWS SQS data type mappings.</summary>
        /// <returns>A list of AWS SQS data type mappings.</returns>
        public static List<DatatypeMapping> GetAWSSQSDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // SQS message types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "String", DataSourceName = "AWSSQSDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Number", DataSourceName = "AWSSQSDataSource", NetDataType = "System.String", Fav = false }, // SQS stores numbers as strings
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Binary", DataSourceName = "AWSSQSDataSource", NetDataType = "System.Byte[]", Fav = false }
            };
        }

        /// <summary>Returns a list of AWS SNS data type mappings.</summary>
        /// <returns>A list of AWS SNS data type mappings.</returns>
        public static List<DatatypeMapping> GetAWSSNSDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // SNS message types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "String", DataSourceName = "AWSSNSDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "JSON", DataSourceName = "AWSSNSDataSource", NetDataType = "System.String", Fav = false }
            };
        }

        /// <summary>Returns a list of Azure Service Bus data type mappings.</summary>
        /// <returns>A list of Azure Service Bus data type mappings.</returns>
        public static List<DatatypeMapping> GetAzureServiceBusDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Azure Service Bus message types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "String", DataSourceName = "AzureServiceBusDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Bytes", DataSourceName = "AzureServiceBusDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Stream", DataSourceName = "AzureServiceBusDataSource", NetDataType = "System.IO.Stream", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Object", DataSourceName = "AzureServiceBusDataSource", NetDataType = "System.Object", Fav = false }
            };
        }

        /// <summary>Returns a list of MassTransit data type mappings.</summary>
        /// <returns>A list of MassTransit data type mappings.</returns>
        public static List<DatatypeMapping> GetMassTransitDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // MassTransit is a .NET message framework
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Object", DataSourceName = "MassTransitDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "String", DataSourceName = "MassTransitDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Bytes", DataSourceName = "MassTransitDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "JSON", DataSourceName = "MassTransitDataSource", NetDataType = "System.String", Fav = false }
            };
        }

        /// <summary>Returns a list of Apache Flink data type mappings.</summary>
        /// <returns>A list of Apache Flink data type mappings.</returns>
        public static List<DatatypeMapping> GetApacheFlinkDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Flink SQL data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "CHAR", DataSourceName = "ApacheFlinkDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "VARCHAR", DataSourceName = "ApacheFlinkDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "STRING", DataSourceName = "ApacheFlinkDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BOOLEAN", DataSourceName = "ApacheFlinkDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BINARY", DataSourceName = "ApacheFlinkDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "VARBINARY", DataSourceName = "ApacheFlinkDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BYTES", DataSourceName = "ApacheFlinkDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DECIMAL", DataSourceName = "ApacheFlinkDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TINYINT", DataSourceName = "ApacheFlinkDataSource", NetDataType = "System.SByte", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "SMALLINT", DataSourceName = "ApacheFlinkDataSource", NetDataType = "System.Int16", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INT", DataSourceName = "ApacheFlinkDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BIGINT", DataSourceName = "ApacheFlinkDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "FLOAT", DataSourceName = "ApacheFlinkDataSource", NetDataType = "System.Single", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DOUBLE", DataSourceName = "ApacheFlinkDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DATE", DataSourceName = "ApacheFlinkDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIME", DataSourceName = "ApacheFlinkDataSource", NetDataType = "System.TimeSpan", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIMESTAMP", DataSourceName = "ApacheFlinkDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIMESTAMP_LTZ", DataSourceName = "ApacheFlinkDataSource", NetDataType = "System.DateTimeOffset", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INTERVAL", DataSourceName = "ApacheFlinkDataSource", NetDataType = "System.TimeSpan", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "ARRAY", DataSourceName = "ApacheFlinkDataSource", NetDataType = "System.Array", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "MAP", DataSourceName = "ApacheFlinkDataSource", NetDataType = "System.Collections.Generic.Dictionary<object, object>", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "MULTISET", DataSourceName = "ApacheFlinkDataSource", NetDataType = "System.Collections.Generic.List<object>", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "ROW", DataSourceName = "ApacheFlinkDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "RAW", DataSourceName = "ApacheFlinkDataSource", NetDataType = "System.Object", Fav = false }
            };
        }

        /// <summary>Returns a list of Apache Storm data type mappings.</summary>
        /// <returns>A list of Apache Storm data type mappings.</returns>
        public static List<DatatypeMapping> GetApacheStormDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Storm primarily works with Java objects and tuples
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Object", DataSourceName = "ApacheStormDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "String", DataSourceName = "ApacheStormDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Integer", DataSourceName = "ApacheStormDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Long", DataSourceName = "ApacheStormDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Double", DataSourceName = "ApacheStormDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Boolean", DataSourceName = "ApacheStormDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "ByteArray", DataSourceName = "ApacheStormDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Tuple", DataSourceName = "ApacheStormDataSource", NetDataType = "System.ValueTuple", Fav = false }
            };
        }

        /// <summary>Returns a list of Apache Spark Streaming data type mappings.</summary>
        /// <returns>A list of Apache Spark Streaming data type mappings.</returns>
        public static List<DatatypeMapping> GetApacheSparkStreamingDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Spark SQL data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BooleanType", DataSourceName = "ApacheSparkStreamingDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "ByteType", DataSourceName = "ApacheSparkStreamingDataSource", NetDataType = "System.Byte", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "ShortType", DataSourceName = "ApacheSparkStreamingDataSource", NetDataType = "System.Int16", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "IntegerType", DataSourceName = "ApacheSparkStreamingDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "LongType", DataSourceName = "ApacheSparkStreamingDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "FloatType", DataSourceName = "ApacheSparkStreamingDataSource", NetDataType = "System.Single", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DoubleType", DataSourceName = "ApacheSparkStreamingDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DecimalType", DataSourceName = "ApacheSparkStreamingDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "StringType", DataSourceName = "ApacheSparkStreamingDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BinaryType", DataSourceName = "ApacheSparkStreamingDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DateType", DataSourceName = "ApacheSparkStreamingDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TimestampType", DataSourceName = "ApacheSparkStreamingDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "ArrayType", DataSourceName = "ApacheSparkStreamingDataSource", NetDataType = "System.Array", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "MapType", DataSourceName = "ApacheSparkStreamingDataSource", NetDataType = "System.Collections.Generic.Dictionary<object, object>", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "StructType", DataSourceName = "ApacheSparkStreamingDataSource", NetDataType = "System.Object", Fav = false }
            };
        }
    }
}