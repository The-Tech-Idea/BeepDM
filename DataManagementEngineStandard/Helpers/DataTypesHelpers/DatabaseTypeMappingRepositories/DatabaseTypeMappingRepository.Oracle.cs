using System;
using System.Collections.Generic;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Helpers.DataTypesHelpers
{
    /// <summary>
    /// Repository containing database-specific type mappings for various database systems.
    /// </summary>
    public static partial class DatabaseTypeMappingRepository
    {
        /// <summary>
        /// Generates a list of datatype mappings for Oracle database.
        /// </summary>
        /// <returns>A list of datatype mappings for Oracle database.</returns>
        public static List<DatatypeMapping> GenerateOracleDataTypesMapping()
        {
            return new List<DatatypeMapping>
        {
            new DatatypeMapping { ID = 0, GuidID = "bffb7e56-8027-4a94-aacf-4eb7f12226bf", DataType = "BFILE", DataSourceName = "OracleDataSource", NetDataType = "System.Byte[]", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "ce0cc840-dcdc-4c50-87c0-b0fcd6d7d098", DataType = "VARCHAR(N)", DataSourceName = "OracleDataSource", NetDataType = "System.String", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "f689373e-8732-41c0-98c2-c304826420fe", DataType = "BINARY_DOUBLE", DataSourceName = "OracleDataSource", NetDataType = "System.Decimal", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "a071d1a8-4d6b-4b69-8eb0-dc62a19a2745", DataType = "BLOB", DataSourceName = "OracleDataSource", NetDataType = "System.Byte[]", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "ddeb8169-b370-4f5f-a2a1-3a50284a06fa", DataType = "CHAR", DataSourceName = "OracleDataSource", NetDataType = "System.String", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "42acc5ee-e831-4c98-8e95-ff12d37a098c", DataType = "CLOB", DataSourceName = "OracleDataSource", NetDataType = "System.String", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "5256a22f-95f8-44a2-ab22-18e6896b59c6", DataType = "INTERVAL DAY TO SECOND", DataSourceName = "OracleDataSource", NetDataType = "System.DateTime", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "206862cd-f204-4fae-93c0-82cb8578b1f5", DataType = "INTERVAL YEAR TO MONTH", DataSourceName = "OracleDataSource", NetDataType = "System.DateTime", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "fbe447b3-6e11-4a29-bfe9-c32e7f646d24", DataType = "LONG", DataSourceName = "OracleDataSource", NetDataType = "System.String", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "62da0061-76f7-42f6-b29e-cb9421f3c470", DataType = "LONG RAW", DataSourceName = "OracleDataSource", NetDataType = "System.Byte[]", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "6637f246-5998-4aad-b0be-a4d4bb60ea6a", DataType = "NCHAR(N)", DataSourceName = "OracleDataSource", NetDataType = "System.String", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "ec2f27a6-9759-4710-9406-659ab8265094", DataType = "NUMBER(P,S)", DataSourceName = "OracleDataSource", NetDataType = "System.Decimal", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "1819ee57-7c3b-4598-a7f7-ba37b9425cb3", DataType = "PLS_INTEGER", DataSourceName = "OracleDataSource", NetDataType = "System.Decimal", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "b48b34c4-12d3-4ed2-ba50-88af84e0fbaa", DataType = "RAW", DataSourceName = "OracleDataSource", NetDataType = "System.Byte[]", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "6c4edb03-1d0d-4fd1-8879-f7d35b54df1c", DataType = "REF", DataSourceName = "OracleDataSource", NetDataType = "System.String", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "cd303042-afae-4d5c-a451-97c2098793eb", DataType = "ROWID", DataSourceName = "OracleDataSource", NetDataType = "System.String", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "a6768c2c-d283-4027-8346-4a608558e55d", DataType = "TIMESTAMP", DataSourceName = "OracleDataSource", NetDataType = "System.DateTime", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "468fe152-227c-47a0-a3a0-ac675eaa8b49", DataType = "TIMESTAMP WITH LOCAL TIME ZONE", DataSourceName = "OracleDataSource", NetDataType = "System.DateTime", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "d45036a5-17b2-495e-b8b0-d0a2e46ea9a2", DataType = "UROWID", DataSourceName = "OracleDataSource", NetDataType = "System.String", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "dfac1056-5c99-40ef-b76d-b60ec95eb777", DataType = "NCLOB", DataSourceName = "OracleDataSource", NetDataType = "System.String", Fav = false },

            // Float mapping
            new DatatypeMapping
            {
                ID = 0,
                GuidID = Guid.NewGuid().ToString(),
                DataType = "FLOAT",
                DataSourceName = "OracleDataSource",
                NetDataType = "System.Double",
                Fav = false
            },

            // Additional mappings
            new DatatypeMapping
            {
                ID = 0,
                GuidID = Guid.NewGuid().ToString(),
                DataType = "INTEGER",
                DataSourceName = "OracleDataSource",
                NetDataType = "System.Int32",
                Fav = false
            },
            new DatatypeMapping
            {
                ID = 0,
                GuidID = Guid.NewGuid().ToString(),
                DataType = "SMALLINT",
                DataSourceName = "OracleDataSource",
                NetDataType = "System.Int16",
                Fav = false
            },
            new DatatypeMapping
            {
                ID = 0,
                GuidID = Guid.NewGuid().ToString(),
                DataType = "REAL",
                DataSourceName = "OracleDataSource",
                NetDataType = "System.Single",
                Fav = false
            },
            new DatatypeMapping
            {
                ID = 0,
                GuidID = Guid.NewGuid().ToString(),
                DataType = "DOUBLE PRECISION",
                DataSourceName = "OracleDataSource",
                NetDataType = "System.Double",
                Fav = false
            },
            new DatatypeMapping
            {
                ID = 0,
                GuidID = Guid.NewGuid().ToString(),
                DataType = "BINARY_FLOAT",
                DataSourceName = "OracleDataSource",
                NetDataType = "System.Single",
                Fav = false
            },
            new DatatypeMapping
            {
                ID = 0,
                GuidID = Guid.NewGuid().ToString(),
                DataType = "VARCHAR2",
                DataSourceName = "OracleDataSource",
                NetDataType = "System.String",
                Fav = false
            },
            new DatatypeMapping
            {
                ID = 0,
                GuidID = Guid.NewGuid().ToString(),
                DataType = "NVARCHAR2",
                DataSourceName = "OracleDataSource",
                NetDataType = "System.String",
                Fav = false
            },
            new DatatypeMapping
            {
                ID = 0,
                GuidID = Guid.NewGuid().ToString(),
                DataType = "DATE",
                DataSourceName = "OracleDataSource",
                NetDataType = "System.DateTime",
                Fav = false
            },
            new DatatypeMapping
            {
                ID = 0,
                GuidID = Guid.NewGuid().ToString(),
                DataType = "TIMESTAMP WITH TIME ZONE",
                DataSourceName = "OracleDataSource",
                NetDataType = "System.DateTimeOffset",
                Fav = false
            },
            new DatatypeMapping
            {
                ID = 0,
                GuidID = Guid.NewGuid().ToString(),
                DataType = "XMLTYPE",
                DataSourceName = "OracleDataSource",
                NetDataType = "System.String",
                Fav = false
            }
        };
        }
    }
}