using System;
using System.Collections.Generic;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Helpers.DataTypesHelpers
{
    /// <summary>
    /// Repository containing Cloud and Enterprise database specific type mappings (DB2, DynamoDB, InfluxDB, etc.).
    /// </summary>
    public static partial class DatabaseTypeMappingRepository
    {
        /// <summary>Returns a list of datatype mappings for DB2.</summary>
        /// <returns>A list of datatype mappings for DB2.</returns>
        public static List<DatatypeMapping> GetDB2DataTypeMappings()
        {
            return new List<DatatypeMapping>
    {
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "CHAR", DataSourceName = "DB2DataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "VARCHAR", DataSourceName = "DB2DataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "CLOB", DataSourceName = "DB2DataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INTEGER", DataSourceName = "DB2DataSource", NetDataType = "System.Int32", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "SMALLINT", DataSourceName = "DB2DataSource", NetDataType = "System.Int16", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BIGINT", DataSourceName = "DB2DataSource", NetDataType = "System.Int64", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DECIMAL", DataSourceName = "DB2DataSource", NetDataType = "System.Decimal", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "REAL", DataSourceName = "DB2DataSource", NetDataType = "System.Single", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DOUBLE", DataSourceName = "DB2DataSource", NetDataType = "System.Double", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DATE", DataSourceName = "DB2DataSource", NetDataType = "System.DateTime", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIME", DataSourceName = "DB2DataSource", NetDataType = "System.DateTime", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIMESTAMP", DataSourceName = "DB2DataSource", NetDataType = "System.DateTime", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BLOB", DataSourceName = "DB2DataSource", NetDataType = "System.Byte[]", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "GRAPHIC", DataSourceName = "DB2DataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "VARGRAPHIC", DataSourceName = "DB2DataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DBCLOB", DataSourceName = "DB2DataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DECFLOAT", DataSourceName = "DB2DataSource", NetDataType = "System.Decimal", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "NUMERIC", DataSourceName = "DB2DataSource", NetDataType = "System.Decimal", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "XML", DataSourceName = "DB2DataSource", NetDataType = "System.Xml.Linq.XElement", Fav = false }
    };
        }

        /// <summary>Returns a list of DynamoDB data type mappings.</summary>
        /// <returns>A list of DynamoDB data type mappings.</returns>
        public static List<DatatypeMapping> GetDynamoDBDataTypeMappings()
        {
            return new List<DatatypeMapping>
    {
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "String", DataSourceName = "DynamoDBDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Number", DataSourceName = "DynamoDBDataSource", NetDataType = "System.Double", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Binary", DataSourceName = "DynamoDBDataSource", NetDataType = "System.Byte[]", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Boolean", DataSourceName = "DynamoDBDataSource", NetDataType = "System.Boolean", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "List", DataSourceName = "DynamoDBDataSource", NetDataType = "System.Collections.Generic.List<object>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Map", DataSourceName = "DynamoDBDataSource", NetDataType = "System.Collections.Generic.Dictionary<string, object>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "String Set", DataSourceName = "DynamoDBDataSource", NetDataType = "System.Collections.Generic.HashSet<string>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Number Set", DataSourceName = "DynamoDBDataSource", NetDataType = "System.Collections.Generic.HashSet<double>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Binary Set", DataSourceName = "DynamoDBDataSource", NetDataType = "System.Collections.Generic.HashSet<byte[]>", Fav = false }
    };
        }

        /// <summary>Returns a list of datatype mappings for InfluxDB.</summary>
        /// <returns>A list of datatype mappings for InfluxDB.</returns>
        public static List<DatatypeMapping> GetInfluxDBDataTypeMappings()
        {
            return new List<DatatypeMapping>
    {
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Float", DataSourceName = "InfluxDBDataSource", NetDataType = "System.Double", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Integer", DataSourceName = "InfluxDBDataSource", NetDataType = "System.Int64", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "String", DataSourceName = "InfluxDBDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Boolean", DataSourceName = "InfluxDBDataSource", NetDataType = "System.Boolean", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Timestamp", DataSourceName = "InfluxDBDataSource", NetDataType = "System.DateTime", Fav = false }
    };
        }

        /// <summary>Returns a list of datatype mappings for Sybase database.</summary>
        /// <returns>A list of datatype mappings for Sybase database.</returns>
        public static List<DatatypeMapping> GetSybaseDataTypeMappings()
        {
            return new List<DatatypeMapping>
    {
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INT", DataSourceName = "SybaseDataSource", NetDataType = "System.Int32", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "VARCHAR", DataSourceName = "SybaseDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TEXT", DataSourceName = "SybaseDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DATETIME", DataSourceName = "SybaseDataSource", NetDataType = "System.DateTime", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "FLOAT", DataSourceName = "SybaseDataSource", NetDataType = "System.Double", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "SMALLINT", DataSourceName = "SybaseDataSource", NetDataType = "System.Int16", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "MONEY", DataSourceName = "SybaseDataSource", NetDataType = "System.Decimal", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BIT", DataSourceName = "SybaseDataSource", NetDataType = "System.Boolean", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "CHAR", DataSourceName = "SybaseDataSource", NetDataType = "System.Char", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "NUMERIC", DataSourceName = "SybaseDataSource", NetDataType = "System.Decimal", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "REAL", DataSourceName = "SybaseDataSource", NetDataType = "System.Single", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "IMAGE", DataSourceName = "SybaseDataSource", NetDataType = "System.Byte[]", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BINARY", DataSourceName = "SybaseDataSource", NetDataType = "System.Byte[]", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "VARBINARY", DataSourceName = "SybaseDataSource", NetDataType = "System.Byte[]", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TINYINT", DataSourceName = "SybaseDataSource", NetDataType = "System.Byte", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "UNICHAR", DataSourceName = "SybaseDataSource", NetDataType = "System.Char", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "UNIVARCHAR", DataSourceName = "SybaseDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BIGINT", DataSourceName = "SybaseDataSource", NetDataType = "System.Int64", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "SMALLMONEY", DataSourceName = "SybaseDataSource", NetDataType = "System.Decimal", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "SMALLDATETIME", DataSourceName = "SybaseDataSource", NetDataType = "System.DateTime", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIMESTAMP", DataSourceName = "SybaseDataSource", NetDataType = "System.Byte[]", Fav = false }
    };
        }

        /// <summary>Returns a list of HBase data type mappings.</summary>
        /// <returns>A list of DatatypeMapping objects representing the mappings between HBase data types and .NET data types.</returns>
        public static List<DatatypeMapping> GetHBaseDataTypeMappings()
        {
            return new List<DatatypeMapping>
    {
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Bytes", DataSourceName = "HBaseDataSource", NetDataType = "System.Byte[]", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Boolean", DataSourceName = "HBaseDataSource", NetDataType = "System.Boolean", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Int32", DataSourceName = "HBaseDataSource", NetDataType = "System.Int32", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Int64", DataSourceName = "HBaseDataSource", NetDataType = "System.Int64", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Float", DataSourceName = "HBaseDataSource", NetDataType = "System.Single", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Double", DataSourceName = "HBaseDataSource", NetDataType = "System.Double", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "String", DataSourceName = "HBaseDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Binary", DataSourceName = "HBaseDataSource", NetDataType = "System.Byte[]", Fav = false }
    };
        }

        /// <summary>Returns a list of datatype mappings for CockroachDB.</summary>
        /// <returns>A list of datatype mappings for CockroachDB.</returns>
        public static List<DatatypeMapping> GetCockroachDBDataTypeMappings()
        {
            return new List<DatatypeMapping>
    {
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BOOL", DataSourceName = "CockroachDBDataSource", NetDataType = "System.Boolean", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INT", DataSourceName = "CockroachDBDataSource", NetDataType = "System.Int32", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BIGINT", DataSourceName = "CockroachDBDataSource", NetDataType = "System.Int64", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "FLOAT", DataSourceName = "CockroachDBDataSource", NetDataType = "System.Double", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DECIMAL", DataSourceName = "CockroachDBDataSource", NetDataType = "System.Decimal", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "STRING", DataSourceName = "CockroachDBDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BYTES", DataSourceName = "CockroachDBDataSource", NetDataType = "System.Byte[]", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIMESTAMP", DataSourceName = "CockroachDBDataSource", NetDataType = "System.DateTime", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "UUID", DataSourceName = "CockroachDBDataSource", NetDataType = "System.Guid", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "ARRAY", DataSourceName = "CockroachDBDataSource", NetDataType = "System.Array", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "JSON", DataSourceName = "CockroachDBDataSource", NetDataType = "Newtonsoft.Json.Linq.JObject", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "JSONB", DataSourceName = "CockroachDBDataSource", NetDataType = "Newtonsoft.Json.Linq.JObject", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INET", DataSourceName = "CockroachDBDataSource", NetDataType = "System.Net.IPAddress", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "ENUM", DataSourceName = "CockroachDBDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BIT", DataSourceName = "CockroachDBDataSource", NetDataType = "System.Boolean", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "SERIAL", DataSourceName = "CockroachDBDataSource", NetDataType = "System.Int64", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIME", DataSourceName = "CockroachDBDataSource", NetDataType = "System.TimeSpan", Fav = false }
    };
        }
    }
}