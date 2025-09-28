using System;
using System.Collections.Generic;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Helpers.DataTypesHelpers
{
    /// <summary>
    /// Repository containing Modern Database specific type mappings (MariaDB, TimeScale, H2Database, Memcached, etc.).
    /// </summary>
    public static partial class DatabaseTypeMappingRepository
    {
        /// <summary>Returns a list of MariaDB data type mappings.</summary>
        /// <returns>A list of MariaDB data type mappings.</returns>
        public static List<DatatypeMapping> GetMariaDBDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // String Data Types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "CHAR", DataSourceName = "MariaDBDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "VARCHAR", DataSourceName = "MariaDBDataSource", NetDataType = "System.String", Fav = true },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BINARY", DataSourceName = "MariaDBDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "VARBINARY", DataSourceName = "MariaDBDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TINYBLOB", DataSourceName = "MariaDBDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TINYTEXT", DataSourceName = "MariaDBDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BLOB", DataSourceName = "MariaDBDataSource", NetDataType = "System.Byte[]", Fav = true },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TEXT", DataSourceName = "MariaDBDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "MEDIUMBLOB", DataSourceName = "MariaDBDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "MEDIUMTEXT", DataSourceName = "MariaDBDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "LONGBLOB", DataSourceName = "MariaDBDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "LONGTEXT", DataSourceName = "MariaDBDataSource", NetDataType = "System.String", Fav = false },
                
                // Numeric Data Types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TINYINT", DataSourceName = "MariaDBDataSource", NetDataType = "System.SByte", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "SMALLINT", DataSourceName = "MariaDBDataSource", NetDataType = "System.Int16", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "MEDIUMINT", DataSourceName = "MariaDBDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INT", DataSourceName = "MariaDBDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INTEGER", DataSourceName = "MariaDBDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BIGINT", DataSourceName = "MariaDBDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DECIMAL", DataSourceName = "MariaDBDataSource", NetDataType = "System.Decimal", Fav = true },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DEC", DataSourceName = "MariaDBDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "NUMERIC", DataSourceName = "MariaDBDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "FIXED", DataSourceName = "MariaDBDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "FLOAT", DataSourceName = "MariaDBDataSource", NetDataType = "System.Single", Fav = true },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DOUBLE", DataSourceName = "MariaDBDataSource", NetDataType = "System.Double", Fav = true },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DOUBLE PRECISION", DataSourceName = "MariaDBDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "REAL", DataSourceName = "MariaDBDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BIT", DataSourceName = "MariaDBDataSource", NetDataType = "System.Boolean", Fav = true },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BOOLEAN", DataSourceName = "MariaDBDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BOOL", DataSourceName = "MariaDBDataSource", NetDataType = "System.Boolean", Fav = false },
                
                // Date and Time Data Types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DATE", DataSourceName = "MariaDBDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DATETIME", DataSourceName = "MariaDBDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIMESTAMP", DataSourceName = "MariaDBDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIME", DataSourceName = "MariaDBDataSource", NetDataType = "System.TimeSpan", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "YEAR", DataSourceName = "MariaDBDataSource", NetDataType = "System.Int32", Fav = false },
                
                // JSON Data Type
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "JSON", DataSourceName = "MariaDBDataSource", NetDataType = "System.String", Fav = false },
                
                // Enum and Set
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "ENUM", DataSourceName = "MariaDBDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "SET", DataSourceName = "MariaDBDataSource", NetDataType = "System.String", Fav = false }
            };
        }

        /// <summary>Returns a list of TimeScale data type mappings.</summary>
        /// <returns>A list of TimeScale data type mappings.</returns>
        public static List<DatatypeMapping> GetTimeScaleDataTypeMappings()
        {
            // TimeScale extends PostgreSQL, so it uses PostgreSQL data types plus some time-series specific types
            var mappings = new List<DatatypeMapping>();
            
            // Add PostgreSQL base types (TimeScale is built on PostgreSQL)
            mappings.AddRange(GetPostgreDataTypesMapping().ConvertAll(mapping => new DatatypeMapping
            {
                ID = mapping.ID,
                GuidID = Guid.NewGuid().ToString(),
                DataType = mapping.DataType,
                DataSourceName = "TimeScaleDataSource",
                NetDataType = mapping.NetDataType,
                Fav = mapping.Fav
            }));
            
            // Add TimeScale-specific types
            mappings.AddRange(new List<DatatypeMapping>
            {
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIMESCALEDB_INFORMATION", DataSourceName = "TimeScaleDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "HYPERTABLE", DataSourceName = "TimeScaleDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "CHUNK", DataSourceName = "TimeScaleDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "CONTINUOUS_AGGREGATE", DataSourceName = "TimeScaleDataSource", NetDataType = "System.Object", Fav = false }
            });
            
            return mappings;
        }

        /// <summary>Returns a list of H2Database data type mappings.</summary>
        /// <returns>A list of H2Database data type mappings.</returns>
        public static List<DatatypeMapping> GetH2DatabaseDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Integer types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INT", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INTEGER", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "MEDIUMINT", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INT4", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "SIGNED", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BOOLEAN", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BIT", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BOOL", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TINYINT", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.Byte", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "SMALLINT", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.Int16", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INT2", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.Int16", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "YEAR", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.Int16", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BIGINT", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INT8", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "IDENTITY", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.Int64", Fav = false },
                
                // Decimal types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DECIMAL", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "NUMBER", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DEC", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "NUMERIC", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.Decimal", Fav = false },
                
                // Floating point types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DOUBLE", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "FLOAT", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "FLOAT4", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.Single", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "FLOAT8", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "REAL", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.Single", Fav = false },
                
                // Date and time types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIME", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.TimeSpan", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DATE", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIMESTAMP", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DATETIME", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "SMALLDATETIME", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.DateTime", Fav = false },
                
                // String types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "VARCHAR", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "LONGVARCHAR", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "VARCHAR2", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "NVARCHAR", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "NVARCHAR2", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "CHAR", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "CHARACTER", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "NCHAR", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.String", Fav = false },
                
                // LOB types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BINARY", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "VARBINARY", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "LONGVARBINARY", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "RAW", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BYTEA", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TINYBLOB", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BLOB", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "MEDIUMBLOB", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "LONGBLOB", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "IMAGE", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "OID", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.Int64", Fav = false },
                
                // Text types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "CLOB", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "NCLOB", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TEXT", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "NTEXT", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TINYTEXT", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "MEDIUMTEXT", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "LONGTEXT", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.String", Fav = false },
                
                // Other types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "UUID", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.Guid", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "ARRAY", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.Array", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "GEOMETRY", DataSourceName = "H2DatabaseDataSource", NetDataType = "System.Object", Fav = false }
            };
        }

        /// <summary>Returns a list of Memcached data type mappings.</summary>
        /// <returns>A list of Memcached data type mappings.</returns>
        public static List<DatatypeMapping> GetMemcachedDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Memcached is a key-value store with simple data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "MemcachedDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "binary", DataSourceName = "MemcachedDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "object", DataSourceName = "MemcachedDataSource", NetDataType = "System.Object", Fav = false }
            };
        }

        /// <summary>Returns a list of GridGain data type mappings.</summary>
        /// <returns>A list of GridGain data type mappings.</returns>
        public static List<DatatypeMapping> GetGridGainDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // GridGain/Apache Ignite data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BOOLEAN", DataSourceName = "GridGainDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TINYINT", DataSourceName = "GridGainDataSource", NetDataType = "System.Byte", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "SMALLINT", DataSourceName = "GridGainDataSource", NetDataType = "System.Int16", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INT", DataSourceName = "GridGainDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BIGINT", DataSourceName = "GridGainDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DECIMAL", DataSourceName = "GridGainDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DOUBLE", DataSourceName = "GridGainDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "REAL", DataSourceName = "GridGainDataSource", NetDataType = "System.Single", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIME", DataSourceName = "GridGainDataSource", NetDataType = "System.TimeSpan", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DATE", DataSourceName = "GridGainDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIMESTAMP", DataSourceName = "GridGainDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "VARCHAR", DataSourceName = "GridGainDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "CHAR", DataSourceName = "GridGainDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "UUID", DataSourceName = "GridGainDataSource", NetDataType = "System.Guid", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BINARY", DataSourceName = "GridGainDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "VARBINARY", DataSourceName = "GridGainDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "ARRAY", DataSourceName = "GridGainDataSource", NetDataType = "System.Array", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "OTHER", DataSourceName = "GridGainDataSource", NetDataType = "System.Object", Fav = false }
            };
        }

        /// <summary>Returns a list of Hazelcast data type mappings.</summary>
        /// <returns>A list of Hazelcast data type mappings.</returns>
        public static List<DatatypeMapping> GetHazelcastDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Hazelcast SQL data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BOOLEAN", DataSourceName = "HazelcastDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TINYINT", DataSourceName = "HazelcastDataSource", NetDataType = "System.Byte", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "SMALLINT", DataSourceName = "HazelcastDataSource", NetDataType = "System.Int16", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INTEGER", DataSourceName = "HazelcastDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BIGINT", DataSourceName = "HazelcastDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DECIMAL", DataSourceName = "HazelcastDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "REAL", DataSourceName = "HazelcastDataSource", NetDataType = "System.Single", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DOUBLE", DataSourceName = "HazelcastDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DATE", DataSourceName = "HazelcastDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIME", DataSourceName = "HazelcastDataSource", NetDataType = "System.TimeSpan", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIMESTAMP", DataSourceName = "HazelcastDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIMESTAMP_WITH_TIME_ZONE", DataSourceName = "HazelcastDataSource", NetDataType = "System.DateTimeOffset", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "VARCHAR", DataSourceName = "HazelcastDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "OBJECT", DataSourceName = "HazelcastDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "NULL", DataSourceName = "HazelcastDataSource", NetDataType = "System.Object", Fav = false }
            };
        }

        /// <summary>Returns a list of ApacheIgnite data type mappings.</summary>
        /// <returns>A list of ApacheIgnite data type mappings.</returns>
        public static List<DatatypeMapping> GetApacheIgniteDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Apache Ignite data types (similar to GridGain but more comprehensive)
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BOOLEAN", DataSourceName = "ApacheIgniteDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TINYINT", DataSourceName = "ApacheIgniteDataSource", NetDataType = "System.Byte", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "SMALLINT", DataSourceName = "ApacheIgniteDataSource", NetDataType = "System.Int16", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INT", DataSourceName = "ApacheIgniteDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BIGINT", DataSourceName = "ApacheIgniteDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DECIMAL", DataSourceName = "ApacheIgniteDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DOUBLE", DataSourceName = "ApacheIgniteDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "REAL", DataSourceName = "ApacheIgniteDataSource", NetDataType = "System.Single", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIME", DataSourceName = "ApacheIgniteDataSource", NetDataType = "System.TimeSpan", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DATE", DataSourceName = "ApacheIgniteDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIMESTAMP", DataSourceName = "ApacheIgniteDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "VARCHAR", DataSourceName = "ApacheIgniteDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "CHAR", DataSourceName = "ApacheIgniteDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "UUID", DataSourceName = "ApacheIgniteDataSource", NetDataType = "System.Guid", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BINARY", DataSourceName = "ApacheIgniteDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "VARBINARY", DataSourceName = "ApacheIgniteDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "ARRAY", DataSourceName = "ApacheIgniteDataSource", NetDataType = "System.Array", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "OTHER", DataSourceName = "ApacheIgniteDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "GEOMETRY", DataSourceName = "ApacheIgniteDataSource", NetDataType = "System.Object", Fav = false }
            };
        }

        /// <summary>Returns a list of ChronicleMap data type mappings.</summary>
        /// <returns>A list of ChronicleMap data type mappings.</returns>
        public static List<DatatypeMapping> GetChronicleMapDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // ChronicleMap is a key-value store with Java-centric types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "String", DataSourceName = "ChronicleMapDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Integer", DataSourceName = "ChronicleMapDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Long", DataSourceName = "ChronicleMapDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Double", DataSourceName = "ChronicleMapDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Float", DataSourceName = "ChronicleMapDataSource", NetDataType = "System.Single", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Boolean", DataSourceName = "ChronicleMapDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Byte", DataSourceName = "ChronicleMapDataSource", NetDataType = "System.Byte", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Short", DataSourceName = "ChronicleMapDataSource", NetDataType = "System.Int16", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Character", DataSourceName = "ChronicleMapDataSource", NetDataType = "System.Char", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "ByteArray", DataSourceName = "ChronicleMapDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "CharSequence", DataSourceName = "ChronicleMapDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Serializable", DataSourceName = "ChronicleMapDataSource", NetDataType = "System.Object", Fav = false }
            };
        }
    }
}