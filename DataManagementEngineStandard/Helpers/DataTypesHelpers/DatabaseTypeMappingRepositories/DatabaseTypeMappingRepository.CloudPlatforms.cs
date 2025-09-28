using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.DriversConfigurations;

namespace TheTechIdea.Beep.Helpers.DataTypesHelpers
{
    /// <summary>
    /// Repository containing Cloud Platform specific type mappings (AWS, Google Cloud, Azure services).
    /// </summary>
    public static partial class DatabaseTypeMappingRepository
    {
        /// <summary>Returns a list of AWS Redshift data type mappings.</summary>
        /// <returns>A list of AWS Redshift data type mappings.</returns>
        public static List<DatatypeMapping> GetAWSRedshiftDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Numeric Types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "SMALLINT", DataSourceName = "AWSRedshiftDataSource", NetDataType = "System.Int16", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INTEGER", DataSourceName = "AWSRedshiftDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BIGINT", DataSourceName = "AWSRedshiftDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DECIMAL", DataSourceName = "AWSRedshiftDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "NUMERIC", DataSourceName = "AWSRedshiftDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "REAL", DataSourceName = "AWSRedshiftDataSource", NetDataType = "System.Single", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DOUBLE PRECISION", DataSourceName = "AWSRedshiftDataSource", NetDataType = "System.Double", Fav = false },
                
                // Boolean Type
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BOOLEAN", DataSourceName = "AWSRedshiftDataSource", NetDataType = "System.Boolean", Fav = false },
                
                // Character Types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "CHAR", DataSourceName = "AWSRedshiftDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "VARCHAR", DataSourceName = "AWSRedshiftDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TEXT", DataSourceName = "AWSRedshiftDataSource", NetDataType = "System.String", Fav = false },
                
                // Date/Time Types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DATE", DataSourceName = "AWSRedshiftDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIMESTAMP", DataSourceName = "AWSRedshiftDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIMESTAMPTZ", DataSourceName = "AWSRedshiftDataSource", NetDataType = "System.DateTimeOffset", Fav = false },
                
                // Geometry Type
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "GEOMETRY", DataSourceName = "AWSRedshiftDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "GEOGRAPHY", DataSourceName = "AWSRedshiftDataSource", NetDataType = "System.Object", Fav = false },
                
                // Array Type
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "SUPER", DataSourceName = "AWSRedshiftDataSource", NetDataType = "System.Object", Fav = false }
            };
        }

        /// <summary>Returns a list of Google BigQuery data type mappings.</summary>
        /// <returns>A list of Google BigQuery data type mappings.</returns>
        public static List<DatatypeMapping> GetGoogleBigQueryDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Numeric Types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INT64", DataSourceName = "GoogleBigQueryDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "NUMERIC", DataSourceName = "GoogleBigQueryDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BIGNUMERIC", DataSourceName = "GoogleBigQueryDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "FLOAT64", DataSourceName = "GoogleBigQueryDataSource", NetDataType = "System.Double", Fav = false },
                
                // Boolean Type
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BOOL", DataSourceName = "GoogleBigQueryDataSource", NetDataType = "System.Boolean", Fav = false },
                
                // String Type
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "STRING", DataSourceName = "GoogleBigQueryDataSource", NetDataType = "System.String", Fav = false },
                
                // Bytes Type
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BYTES", DataSourceName = "GoogleBigQueryDataSource", NetDataType = "System.Byte[]", Fav = false },
                
                // Date/Time Types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DATE", DataSourceName = "GoogleBigQueryDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DATETIME", DataSourceName = "GoogleBigQueryDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIME", DataSourceName = "GoogleBigQueryDataSource", NetDataType = "System.TimeSpan", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIMESTAMP", DataSourceName = "GoogleBigQueryDataSource", NetDataType = "System.DateTimeOffset", Fav = false },
                
                // Geography Type
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "GEOGRAPHY", DataSourceName = "GoogleBigQueryDataSource", NetDataType = "System.Object", Fav = false },
                
                // Complex Types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "ARRAY", DataSourceName = "GoogleBigQueryDataSource", NetDataType = "System.Array", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "STRUCT", DataSourceName = "GoogleBigQueryDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "JSON", DataSourceName = "GoogleBigQueryDataSource", NetDataType = "System.String", Fav = false },
                
                // Interval Type
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INTERVAL", DataSourceName = "GoogleBigQueryDataSource", NetDataType = "System.TimeSpan", Fav = false }
            };
        }

        /// <summary>Returns a list of Azure SQL data type mappings.</summary>
        /// <returns>A list of Azure SQL data type mappings.</returns>
        public static List<DatatypeMapping> GetAzureSQLDataTypeMappings()
        {
            // Azure SQL uses SQL Server data types, so we can reuse and adapt them
            var mappings = new List<DatatypeMapping>();
            
            // Get SQL Server mappings and adapt them for Azure SQL
            var sqlServerMappings = GenerateSqlServerDataTypesMapping();
            foreach (var mapping in sqlServerMappings)
            {
                mappings.Add(new DatatypeMapping
                {
                    ID = mapping.ID,
                    GuidID = Guid.NewGuid().ToString(),
                    DataType = mapping.DataType,
                    DataSourceName = "AzureSQLDataSource",
                    NetDataType = mapping.NetDataType,
                    Fav = mapping.Fav
                });
            }
            
            return mappings;
        }

        /// <summary>Returns a list of AWS RDS data type mappings.</summary>
        /// <returns>A list of AWS RDS data type mappings.</returns>
        public static List<DatatypeMapping> GetAWSRDSDataTypeMappings()
        {
            // AWS RDS supports multiple engines (MySQL, PostgreSQL, SQL Server, Oracle, MariaDB)
            // This is a generic mapping - specific engine mappings should be used when the engine is known
            return new List<DatatypeMapping>
            {
                // Generic SQL types supported across RDS engines
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "VARCHAR", DataSourceName = "AWSRDSDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "CHAR", DataSourceName = "AWSRDSDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TEXT", DataSourceName = "AWSRDSDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INT", DataSourceName = "AWSRDSDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BIGINT", DataSourceName = "AWSRDSDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "SMALLINT", DataSourceName = "AWSRDSDataSource", NetDataType = "System.Int16", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DECIMAL", DataSourceName = "AWSRDSDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "NUMERIC", DataSourceName = "AWSRDSDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "FLOAT", DataSourceName = "AWSRDSDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DOUBLE", DataSourceName = "AWSRDSDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BOOLEAN", DataSourceName = "AWSRDSDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DATE", DataSourceName = "AWSRDSDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIMESTAMP", DataSourceName = "AWSRDSDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DATETIME", DataSourceName = "AWSRDSDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIME", DataSourceName = "AWSRDSDataSource", NetDataType = "System.TimeSpan", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BLOB", DataSourceName = "AWSRDSDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BINARY", DataSourceName = "AWSRDSDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "VARBINARY", DataSourceName = "AWSRDSDataSource", NetDataType = "System.Byte[]", Fav = false }
            };
        }

        /// <summary>Returns a list of SAP Hana data type mappings.</summary>
        /// <returns>A list of SAP Hana data type mappings.</returns>
        public static List<DatatypeMapping> GetHanaDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Numeric Types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TINYINT", DataSourceName = "HanaDataSource", NetDataType = "System.Byte", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "SMALLINT", DataSourceName = "HanaDataSource", NetDataType = "System.Int16", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INTEGER", DataSourceName = "HanaDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BIGINT", DataSourceName = "HanaDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DECIMAL", DataSourceName = "HanaDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "SMALLDECIMAL", DataSourceName = "HanaDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "REAL", DataSourceName = "HanaDataSource", NetDataType = "System.Single", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DOUBLE", DataSourceName = "HanaDataSource", NetDataType = "System.Double", Fav = false },
                
                // Character Types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "VARCHAR", DataSourceName = "HanaDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "NVARCHAR", DataSourceName = "HanaDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "CHAR", DataSourceName = "HanaDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "NCHAR", DataSourceName = "HanaDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TEXT", DataSourceName = "HanaDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "SHORTTEXT", DataSourceName = "HanaDataSource", NetDataType = "System.String", Fav = false },
                
                // Date/Time Types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DATE", DataSourceName = "HanaDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIME", DataSourceName = "HanaDataSource", NetDataType = "System.TimeSpan", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIMESTAMP", DataSourceName = "HanaDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "SECONDDATE", DataSourceName = "HanaDataSource", NetDataType = "System.DateTime", Fav = false },
                
                // Binary Types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "VARBINARY", DataSourceName = "HanaDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BLOB", DataSourceName = "HanaDataSource", NetDataType = "System.Byte[]", Fav = false },
                
                // Large Object Types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "CLOB", DataSourceName = "HanaDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "NCLOB", DataSourceName = "HanaDataSource", NetDataType = "System.String", Fav = false },
                
                // Boolean Type
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BOOLEAN", DataSourceName = "HanaDataSource", NetDataType = "System.Boolean", Fav = false },
                
                // Spatial Types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "ST_GEOMETRY", DataSourceName = "HanaDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "ST_POINT", DataSourceName = "HanaDataSource", NetDataType = "System.Object", Fav = false }
            };
        }

        /// <summary>Returns a list of Google Spanner data type mappings.</summary>
        /// <returns>A list of Google Spanner data type mappings.</returns>
        public static List<DatatypeMapping> GetSpannerDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Numeric Types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BOOL", DataSourceName = "SpannerDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INT64", DataSourceName = "SpannerDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "FLOAT64", DataSourceName = "SpannerDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "NUMERIC", DataSourceName = "SpannerDataSource", NetDataType = "System.Decimal", Fav = false },
                
                // String Types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "STRING", DataSourceName = "SpannerDataSource", NetDataType = "System.String", Fav = false },
                
                // Bytes Type
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BYTES", DataSourceName = "SpannerDataSource", NetDataType = "System.Byte[]", Fav = false },
                
                // Date/Time Types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DATE", DataSourceName = "SpannerDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIMESTAMP", DataSourceName = "SpannerDataSource", NetDataType = "System.DateTimeOffset", Fav = false },
                
                // Complex Types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "ARRAY", DataSourceName = "SpannerDataSource", NetDataType = "System.Array", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "STRUCT", DataSourceName = "SpannerDataSource", NetDataType = "System.Object", Fav = false },
                
                // JSON Type
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "JSON", DataSourceName = "SpannerDataSource", NetDataType = "System.String", Fav = false }
            };
        }
    }
}