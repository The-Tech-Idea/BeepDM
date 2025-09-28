using System;
using System.Collections.Generic;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Helpers.DataTypesHelpers
{
    /// <summary>
    /// Repository containing modern cloud database specific type mappings (Snowflake, BerkeleyDB, Azure Cosmos DB, etc.).
    /// </summary>
    public static partial class DatabaseTypeMappingRepository
    {
        /// <summary>Returns a list of datatype mappings for Berkeley DB.</summary>
        /// <returns>A list of datatype mappings for Berkeley DB.</returns>
        public static List<DatatypeMapping> GetBerkeleyDBDataTypesMapping()
        {
            return new List<DatatypeMapping>
    {
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Key", DataSourceName = "BerkeleyDBDataSource", NetDataType = "System.Byte[]", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Value", DataSourceName = "BerkeleyDBDataSource", NetDataType = "System.Byte[]", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "StringKey", DataSourceName = "BerkeleyDBDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "StringValue", DataSourceName = "BerkeleyDBDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "IntKey", DataSourceName = "BerkeleyDBDataSource", NetDataType = "System.Int32", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "IntValue", DataSourceName = "BerkeleyDBDataSource", NetDataType = "System.Int32", Fav = false }
    };
        }

        /// <summary>Returns a list of Snowflake data type mappings.</summary>
        /// <returns>A list of DatatypeMapping objects representing the mappings between Snowflake data types and their corresponding .NET data types.</returns>
        public static List<DatatypeMapping> GetSnowflakeDataTypesMapping()
        {
            return new List<DatatypeMapping>
    {
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BOOLEAN", DataSourceName = "SnowflakeDataSource", NetDataType = "System.Boolean", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TEXT", DataSourceName = "SnowflakeDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INTEGER", DataSourceName = "SnowflakeDataSource", NetDataType = "System.Int32", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "FLOAT", DataSourceName = "SnowflakeDataSource", NetDataType = "System.Double", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DOUBLE", DataSourceName = "SnowflakeDataSource", NetDataType = "System.Double", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DECIMAL", DataSourceName = "SnowflakeDataSource", NetDataType = "System.Decimal", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DATE", DataSourceName = "SnowflakeDataSource", NetDataType = "System.DateTime", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIME", DataSourceName = "SnowflakeDataSource", NetDataType = "System.TimeSpan", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "ARRAY", DataSourceName = "SnowflakeDataSource", NetDataType = "System.Object", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "OBJECT", DataSourceName = "SnowflakeDataSource", NetDataType = "System.Object", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "GEOGRAPHY", DataSourceName = "SnowflakeDataSource", NetDataType = "System.Object", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "VARIANT", DataSourceName = "SnowflakeDataSource", NetDataType = "System.Object", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "MONEY", DataSourceName = "SnowflakeDataSource", NetDataType = "System.Decimal", Fav = false }
    };
        }

        /// <summary>Returns a list of Azure Cosmos DB data type mappings.</summary>
        /// <returns>A list of DatatypeMapping objects representing the mappings between Azure Cosmos DB data types and .NET data types.</returns>
        public static List<DatatypeMapping> GetAzureCosmosDBDataTypesMapping()
        {
            return new List<DatatypeMapping>
    {
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "String", DataSourceName = "AzureCosmosDB", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Number", DataSourceName = "AzureCosmosDB", NetDataType = "System.Double", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Boolean", DataSourceName = "AzureCosmosDB", NetDataType = "System.Boolean", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Null", DataSourceName = "AzureCosmosDB", NetDataType = "System.Object", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Object", DataSourceName = "AzureCosmosDB", NetDataType = "System.Object", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Array", DataSourceName = "AzureCosmosDB", NetDataType = "System.Collections.Generic.List<System.Object>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DateTime", DataSourceName = "AzureCosmosDB", NetDataType = "System.DateTime", Fav = false }
    };
        }

        /// <summary>Returns a list of datatype mappings for Vertica database.</summary>
        /// <returns>A list of datatype mappings for Vertica database.</returns>
        public static List<DatatypeMapping> GetVerticaDataTypesMapping()
        {
            return new List<DatatypeMapping>
    {
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BOOLEAN", DataSourceName = "VerticaDataSource", NetDataType = "System.Boolean", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INTEGER", DataSourceName = "VerticaDataSource", NetDataType = "System.Int32", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BIGINT", DataSourceName = "VerticaDataSource", NetDataType = "System.Int64", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "FLOAT", DataSourceName = "VerticaDataSource", NetDataType = "System.Single", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DOUBLE PRECISION", DataSourceName = "VerticaDataSource", NetDataType = "System.Double", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "CHAR", DataSourceName = "VerticaDataSource", NetDataType = "System.Char", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "VARCHAR", DataSourceName = "VerticaDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DATE", DataSourceName = "VerticaDataSource", NetDataType = "System.DateTime", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIME", DataSourceName = "VerticaDataSource", NetDataType = "System.TimeSpan", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIMESTAMP", DataSourceName = "VerticaDataSource", NetDataType = "System.DateTime", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BINARY", DataSourceName = "VerticaDataSource", NetDataType = "System.Byte[]", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "VARBINARY", DataSourceName = "VerticaDataSource", NetDataType = "System.Byte[]", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "NUMERIC", DataSourceName = "VerticaDataSource", NetDataType = "System.Decimal", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "UUID", DataSourceName = "VerticaDataSource", NetDataType = "System.Guid", Fav = false }
    };
        }

        /// <summary>Returns a list of Teradata data type mappings.</summary>
        /// <returns>A list of Teradata data type mappings.</returns>
        public static List<DatatypeMapping> GetTeradataDataTypeMappings()
        {
            return new List<DatatypeMapping>
    {
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BYTEINT", DataSourceName = "TeradataDataSource", NetDataType = "System.SByte", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "SMALLINT", DataSourceName = "TeradataDataSource", NetDataType = "System.Int16", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INTEGER", DataSourceName = "TeradataDataSource", NetDataType = "System.Int32", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BIGINT", DataSourceName = "TeradataDataSource", NetDataType = "System.Int64", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DECIMAL", DataSourceName = "TeradataDataSource", NetDataType = "System.Decimal", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "FLOAT", DataSourceName = "TeradataDataSource", NetDataType = "System.Double", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "CHAR", DataSourceName = "TeradataDataSource", NetDataType = "System.Char", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "VARCHAR", DataSourceName = "TeradataDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DATE", DataSourceName = "TeradataDataSource", NetDataType = "System.DateTime", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIME", DataSourceName = "TeradataDataSource", NetDataType = "System.TimeSpan", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIMESTAMP", DataSourceName = "TeradataDataSource", NetDataType = "System.DateTime", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BYTE", DataSourceName = "TeradataDataSource", NetDataType = "System.Byte[]", Fav = false }
    };
        }

        /// <summary>Returns a list of datatype mappings for ArangoDB.</summary>
        /// <returns>A list of datatype mappings for ArangoDB.</returns>
        public static List<DatatypeMapping> GetArangoDBDataTypeMappings()
        {
            return new List<DatatypeMapping>
    {
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Boolean", DataSourceName = "ArangoDBDataSource", NetDataType = "System.Boolean", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Number", DataSourceName = "ArangoDBDataSource", NetDataType = "System.Double", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "String", DataSourceName = "ArangoDBDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Object", DataSourceName = "ArangoDBDataSource", NetDataType = "System.Object", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Array", DataSourceName = "ArangoDBDataSource", NetDataType = "System.Collections.Generic.List<object>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Null", DataSourceName = "ArangoDBDataSource", NetDataType = "System.Object", Fav = false }
    };
        }
    }
}