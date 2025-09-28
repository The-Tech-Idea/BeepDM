using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.DriversConfigurations;

namespace TheTechIdea.Beep.Helpers.DataTypesHelpers
{
    /// <summary>
    /// Repository containing Workflow, IoT, Industrial, and Cloud Service specific type mappings.
    /// </summary>
    public static partial class DatabaseTypeMappingRepository
    {
        /// <summary>Returns a list of AWS Step Functions data type mappings.</summary>
        /// <returns>A list of AWS Step Functions data type mappings.</returns>
        public static List<DatatypeMapping> GetAWSStepFunctionsDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // AWS Step Functions State Language data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "String", DataSourceName = "AWSStepFunctionsDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Number", DataSourceName = "AWSStepFunctionsDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Boolean", DataSourceName = "AWSStepFunctionsDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Null", DataSourceName = "AWSStepFunctionsDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Array", DataSourceName = "AWSStepFunctionsDataSource", NetDataType = "System.Array", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Object", DataSourceName = "AWSStepFunctionsDataSource", NetDataType = "System.Collections.Generic.Dictionary<string, object>", Fav = false }
            };
        }

        /// <summary>Returns a list of AWS Simple Workflow data type mappings.</summary>
        /// <returns>A list of AWS Simple Workflow data type mappings.</returns>
        public static List<DatatypeMapping> GetAWSSWFDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // AWS Simple Workflow Service data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "WorkflowId", DataSourceName = "AWSSWFDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "ActivityId", DataSourceName = "AWSSWFDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TaskToken", DataSourceName = "AWSSWFDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Input", DataSourceName = "AWSSWFDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Result", DataSourceName = "AWSSWFDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Reason", DataSourceName = "AWSSWFDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Details", DataSourceName = "AWSSWFDataSource", NetDataType = "System.String", Fav = false }
            };
        }

        /// <summary>Returns a list of AWS IoT data type mappings.</summary>
        /// <returns>A list of AWS IoT data type mappings.</returns>
        public static List<DatatypeMapping> GetAWSIoTDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // AWS IoT message payload types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "JSON", DataSourceName = "AWSIoTDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Binary", DataSourceName = "AWSIoTDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "String", DataSourceName = "AWSIoTDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Number", DataSourceName = "AWSIoTDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Boolean", DataSourceName = "AWSIoTDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Timestamp", DataSourceName = "AWSIoTDataSource", NetDataType = "System.DateTime", Fav = false }
            };
        }

        /// <summary>Returns a list of AWS IoT Core data type mappings.</summary>
        /// <returns>A list of AWS IoT Core data type mappings.</returns>
        public static List<DatatypeMapping> GetAWSIoTCoreDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // AWS IoT Core device shadow and telemetry data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "ShadowDocument", DataSourceName = "AWSIoTCoreDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TelemetryData", DataSourceName = "AWSIoTCoreDataSource", NetDataType = "System.Collections.Generic.Dictionary<string, object>", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DeviceState", DataSourceName = "AWSIoTCoreDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Command", DataSourceName = "AWSIoTCoreDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Metadata", DataSourceName = "AWSIoTCoreDataSource", NetDataType = "System.Collections.Generic.Dictionary<string, object>", Fav = false }
            };
        }

        /// <summary>Returns a list of AWS IoT Analytics data type mappings.</summary>
        /// <returns>A list of AWS IoT Analytics data type mappings.</returns>
        public static List<DatatypeMapping> GetAWSIoTAnalyticsDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // AWS IoT Analytics dataset types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "STRING", DataSourceName = "AWSIoTAnalyticsDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INTEGER", DataSourceName = "AWSIoTAnalyticsDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DOUBLE", DataSourceName = "AWSIoTAnalyticsDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BOOLEAN", DataSourceName = "AWSIoTAnalyticsDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIMESTAMP", DataSourceName = "AWSIoTAnalyticsDataSource", NetDataType = "System.DateTime", Fav = false }
            };
        }

        /// <summary>Returns a list of OPC data type mappings.</summary>
        /// <returns>A list of OPC data type mappings.</returns>
        public static List<DatatypeMapping> GetOPCDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // OPC UA built-in data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Boolean", DataSourceName = "OPCDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "SByte", DataSourceName = "OPCDataSource", NetDataType = "System.SByte", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Byte", DataSourceName = "OPCDataSource", NetDataType = "System.Byte", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Int16", DataSourceName = "OPCDataSource", NetDataType = "System.Int16", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "UInt16", DataSourceName = "OPCDataSource", NetDataType = "System.UInt16", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Int32", DataSourceName = "OPCDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "UInt32", DataSourceName = "OPCDataSource", NetDataType = "System.UInt32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Int64", DataSourceName = "OPCDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "UInt64", DataSourceName = "OPCDataSource", NetDataType = "System.UInt64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Float", DataSourceName = "OPCDataSource", NetDataType = "System.Single", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Double", DataSourceName = "OPCDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "String", DataSourceName = "OPCDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DateTime", DataSourceName = "OPCDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Guid", DataSourceName = "OPCDataSource", NetDataType = "System.Guid", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "ByteString", DataSourceName = "OPCDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "XmlElement", DataSourceName = "OPCDataSource", NetDataType = "System.Xml.XmlElement", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "NodeId", DataSourceName = "OPCDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "ExpandedNodeId", DataSourceName = "OPCDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "StatusCode", DataSourceName = "OPCDataSource", NetDataType = "System.UInt32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "QualifiedName", DataSourceName = "OPCDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "LocalizedText", DataSourceName = "OPCDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "ExtensionObject", DataSourceName = "OPCDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DataValue", DataSourceName = "OPCDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Variant", DataSourceName = "OPCDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DiagnosticInfo", DataSourceName = "OPCDataSource", NetDataType = "System.Object", Fav = false }
            };
        }

        /// <summary>Returns a list of Presto data type mappings.</summary>
        /// <returns>A list of Presto data type mappings.</returns>
        public static List<DatatypeMapping> GetPrestoDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Presto/Trino SQL data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BOOLEAN", DataSourceName = "PrestoDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TINYINT", DataSourceName = "PrestoDataSource", NetDataType = "System.SByte", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "SMALLINT", DataSourceName = "PrestoDataSource", NetDataType = "System.Int16", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INTEGER", DataSourceName = "PrestoDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BIGINT", DataSourceName = "PrestoDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "REAL", DataSourceName = "PrestoDataSource", NetDataType = "System.Single", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DOUBLE", DataSourceName = "PrestoDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DECIMAL", DataSourceName = "PrestoDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "VARCHAR", DataSourceName = "PrestoDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "CHAR", DataSourceName = "PrestoDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "VARBINARY", DataSourceName = "PrestoDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "JSON", DataSourceName = "PrestoDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DATE", DataSourceName = "PrestoDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIME", DataSourceName = "PrestoDataSource", NetDataType = "System.TimeSpan", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIME WITH TIME ZONE", DataSourceName = "PrestoDataSource", NetDataType = "System.DateTimeOffset", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIMESTAMP", DataSourceName = "PrestoDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIMESTAMP WITH TIME ZONE", DataSourceName = "PrestoDataSource", NetDataType = "System.DateTimeOffset", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INTERVAL YEAR TO MONTH", DataSourceName = "PrestoDataSource", NetDataType = "System.TimeSpan", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INTERVAL DAY TO SECOND", DataSourceName = "PrestoDataSource", NetDataType = "System.TimeSpan", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "ARRAY", DataSourceName = "PrestoDataSource", NetDataType = "System.Array", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "MAP", DataSourceName = "PrestoDataSource", NetDataType = "System.Collections.Generic.Dictionary<object, object>", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "ROW", DataSourceName = "PrestoDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "IPADDRESS", DataSourceName = "PrestoDataSource", NetDataType = "System.Net.IPAddress", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "UUID", DataSourceName = "PrestoDataSource", NetDataType = "System.Guid", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "HYPERLOGLOG", DataSourceName = "PrestoDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "P4HYPERLOGLOG", DataSourceName = "PrestoDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "QDIGEST", DataSourceName = "PrestoDataSource", NetDataType = "System.Object", Fav = false }
            };
        }

        /// <summary>Returns a list of Trino data type mappings.</summary>
        /// <returns>A list of Trino data type mappings.</returns>
        public static List<DatatypeMapping> GetTrinoDataTypeMappings()
        {
            // Trino uses the same data types as Presto
            var mappings = new List<DatatypeMapping>();
            var prestoMappings = GetPrestoDataTypeMappings();
            
            foreach (var mapping in prestoMappings)
            {
                mappings.Add(new DatatypeMapping
                {
                    ID = mapping.ID,
                    GuidID = Guid.NewGuid().ToString(),
                    DataType = mapping.DataType,
                    DataSourceName = "TrinoDataSource",
                    NetDataType = mapping.NetDataType,
                    Fav = mapping.Fav
                });
            }
            
            return mappings;
        }

        /// <summary>Returns a list of Google Sheets data type mappings.</summary>
        /// <returns>A list of Google Sheets data type mappings.</returns>
        public static List<DatatypeMapping> GetGoogleSheetsDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Google Sheets data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "STRING_VALUE", DataSourceName = "GoogleSheetsDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "NUMBER_VALUE", DataSourceName = "GoogleSheetsDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BOOL_VALUE", DataSourceName = "GoogleSheetsDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "FORMULA_VALUE", DataSourceName = "GoogleSheetsDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "ERROR_VALUE", DataSourceName = "GoogleSheetsDataSource", NetDataType = "System.String", Fav = false }
            };
        }

        /// <summary>Returns a list of MiModel data type mappings.</summary>
        /// <returns>A list of MiModel data type mappings.</returns>
        public static List<DatatypeMapping> GetMiModelDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Generic model data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "String", DataSourceName = "MiModelDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Integer", DataSourceName = "MiModelDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Float", DataSourceName = "MiModelDataSource", NetDataType = "System.Single", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Double", DataSourceName = "MiModelDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Boolean", DataSourceName = "MiModelDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DateTime", DataSourceName = "MiModelDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Binary", DataSourceName = "MiModelDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Object", DataSourceName = "MiModelDataSource", NetDataType = "System.Object", Fav = false }
            };
        }
    }
}