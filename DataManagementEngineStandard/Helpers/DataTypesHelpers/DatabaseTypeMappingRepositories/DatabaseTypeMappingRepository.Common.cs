using System;
using System.Collections.Generic;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Helpers.DataTypesHelpers
{
    /// <summary>
    /// Repository containing SQLite and other lightweight database specific type mappings.
    /// </summary>
    public static partial class DatabaseTypeMappingRepository
    {
        /// <summary>
        /// Generates a list of datatype mappings for SQLite.
        /// </summary>
        /// <returns>A list of datatype mappings for SQLite.</returns>
        public static List<DatatypeMapping> GenerateSQLiteDataTypesMapping()
        {
            return new List<DatatypeMapping>
        {
            new DatatypeMapping { ID = 0, GuidID = "cd1cb478-2eee-43ee-bb76-d924086a1e79", DataType = "BOOLEAN", DataSourceName = "SQLiteDataSource", NetDataType = "System.Boolean", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "5fa77cbf-2d1d-40a9-b429-d53e5d9996cc", DataType = "INTEGER", DataSourceName = "SQLiteDataSource", NetDataType = "System.Byte", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "601f3268-7001-43cf-a7ca-a7b91d19916c", DataType = "BLOB", DataSourceName = "SQLiteDataSource", NetDataType = "System.Byte[]", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "42d04b99-132c-4639-b8db-1333a88de869", DataType = "CHARACTER(1)", DataSourceName = "SQLiteDataSource", NetDataType = "System.Char", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "a989fa08-53e9-48f8-bef9-9b242a6934e1", DataType = "DATETIME", DataSourceName = "SQLiteDataSource", NetDataType = "System.DateTime", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "de7ca8c8-9658-4591-b564-459827360284", DataType = "TEXT", DataSourceName = "SQLiteDataSource", NetDataType = "System.DateTimeOffset", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "eb1c8f4d-ab33-420e-b099-cc73effa161c", DataType = "NUMERIC", DataSourceName = "SQLiteDataSource", NetDataType = "System.Decimal", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "ecbfc523-5654-465c-8c46-870056d50e0d", DataType = "REAL", DataSourceName = "SQLiteDataSource", NetDataType = "System.Double", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "5adeb0b4-4d07-4ca0-a161-6db7a4ce67ae", DataType = "TEXT", DataSourceName = "SQLiteDataSource", NetDataType = "System.Guid", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "bb0bfc0a-ee18-4228-9a3a-5924d5af1344", DataType = "INTEGER", DataSourceName = "SQLiteDataSource", NetDataType = "System.Int16", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "46902969-44d1-477e-acdb-0c52265f71e3", DataType = "INTEGER", DataSourceName = "SQLiteDataSource", NetDataType = "System.Int32", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "7253187c-9dbb-4902-9d1b-19a8b1d77181", DataType = "INTEGER", DataSourceName = "SQLiteDataSource", NetDataType = "System.Int64", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "a6bff8c1-5ebc-4c8d-9a72-757ba63321f4", DataType = "INTEGER", DataSourceName = "SQLiteDataSource", NetDataType = "System.SByte", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "10a8bf0d-6e20-428f-b970-7e41a4b5e754", DataType = "REAL", DataSourceName = "SQLiteDataSource", NetDataType = "System.Single", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "d331a968-d394-437c-a060-977da08ff0cf", DataType = "VARCHAR(N)", DataSourceName = "SQLiteDataSource", NetDataType = "System.String", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "33aecc69-d54e-4d65-85b5-89b33b2e4b25", DataType = "TEXT", DataSourceName = "SQLiteDataSource", NetDataType = "System.TimeSpan", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "b5adba16-90d6-4b4a-a6f3-c64c5ed36bc1", DataType = "INTEGER", DataSourceName = "SQLiteDataSource", NetDataType = "System.UInt16", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "b3abdcd5-662e-40a9-93e2-0b6ade2348de", DataType = "INTEGER", DataSourceName = "SQLiteDataSource", NetDataType = "System.UInt32", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "9295b8a4-ce98-42eb-bd97-a56ba1313cd5", DataType = "INTEGER", DataSourceName = "SQLiteDataSource", NetDataType = "System.UInt64", Fav = false },
        };
        }

        /// <summary>
        /// Generates a list of datatype mappings for SQL Server Compact Edition.
        /// </summary>
        /// <returns>A list of datatype mappings for SQL Server Compact Edition.</returns>
        public static List<DatatypeMapping> GenerateSqlCompactDataTypesMapping()
        {
            return new List<DatatypeMapping>
            {
                new DatatypeMapping { ID = 0, GuidID = "584adb89-91fd-4b89-9c88-8ff5ca2c12e8", DataType = "bit", DataSourceName = "DatatypeMapping", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = "8124aa72-967a-4df3-aa99-f9058cdf58cb", DataType = "tinyint", DataSourceName = "DatatypeMapping", NetDataType = "System.Byte", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = "9a542d3b-5dd9-499f-8e4d-1ca90cf03957", DataType = "numeric(3,0)", DataSourceName = "DatatypeMapping", NetDataType = "System.SByte", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = "73cef364-9363-4420-85c3-22122aa37e2d", DataType = "bit", DataSourceName = "DatatypeMapping", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = "9237c66a-a4f1-4f65-88df-5aca6d8921e9", DataType = "nchar(1)", DataSourceName = "DatatypeMapping", NetDataType = "System.Char", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = "48a805c9-f4f6-471a-bd40-772b610d0335", DataType = "numeric(19,4)", DataSourceName = "DatatypeMapping", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = "d7dc53fc-e5d5-4e38-9913-4b0d45534f98", DataType = "float", DataSourceName = "DatatypeMapping", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = "185295f2-d10b-49ff-8261-5f7b915457a8", DataType = "real", DataSourceName = "DatatypeMapping", NetDataType = "System.Single", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = "8f4befbe-bf9c-4577-8063-a8157310f4ce", DataType = "bit", DataSourceName = "DatatypeMapping", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = "6c8a507d-6035-4d55-ba6f-0af597656ff7", DataType = "smallint", DataSourceName = "DatatypeMapping", NetDataType = "System.Int16", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = "7469dc2e-b422-4331-9617-e8de81943e4e", DataType = "numeric(5,0)", DataSourceName = "DatatypeMapping", NetDataType = "System.UInt16", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = "6616e01d-a324-48db-8c6c-b2aac9a946b8", DataType = "int", DataSourceName = "DatatypeMapping", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = "6de0d55e-5ad8-46bd-99e4-a48db7e8506b", DataType = "numeric(10,0)", DataSourceName = "DatatypeMapping", NetDataType = "System.UInt32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = "c04dae52-8da0-4846-901f-b7bccc9d4011", DataType = "bigint", DataSourceName = "DatatypeMapping", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = "978c7ad0-6194-4c89-96c1-80c081e06618", DataType = "numeric(20,0)", DataSourceName = "DatatypeMapping", NetDataType = "System.UInt64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = "ca9d585c-75bf-406f-ae6e-08778f9a2f04", DataType = "uniqueidentifier", DataSourceName = "DatatypeMapping", NetDataType = "System.Guid", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = "bc523cdf-3fcd-4a04-b48c-847a6cfcfcaa", DataType = "nvarchar(N)", DataSourceName = "DatatypeMapping", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = "71d52da7-0569-4945-87a9-06b6c09f16c9", DataType = "datetime", DataSourceName = "DatatypeMapping", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = "0ec10616-b8d8-4eaf-bba7-b9f480c7d2ae", DataType = "float", DataSourceName = "DatatypeMapping", NetDataType = "System.TimeSpan", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = "22629847-6984-4752-976b-8347c94799ea", DataType = "varbinary(N)", DataSourceName = "DatatypeMapping", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = "055eecb6-7dde-4c64-beff-04d6d3327241", DataType = "Image", DataSourceName = "DatatypeMapping", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = "afa57ab4-d24d-47ae-853c-88066a0181a6", DataType = "ntext", DataSourceName = "DatatypeMapping", NetDataType = "System.String", Fav = false }
            };
        }

        /// <summary>Returns a list of datatype mappings for DuckDB.</summary>
        /// <returns>A list of datatype mappings for DuckDB.</returns>
        public static List<DatatypeMapping> GetDuckDBDataTypesMapping()
        {
            return new List<DatatypeMapping>
            {
                new DatatypeMapping { ID = 0, GuidID = "dda45fdb-d6e6-4f34-8b70-04ce2cf8ed46", DataType = "BOOLEAN", DataSourceName = "DuckDBDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = "8e8c7dd3-8104-4b94-83b9-d8f0ffa815ab", DataType = "TINYINT", DataSourceName = "DuckDBDataSource", NetDataType = "System.Byte", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = "65188632-bfd4-4845-91c9-685fca0126bb", DataType = "BLOB", DataSourceName = "DuckDBDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = "eec16db7-652f-4eb4-8638-4237595f87be", DataType = "CHAR", DataSourceName = "DuckDBDataSource", NetDataType = "System.Char", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = "7c90542c-9627-4be6-9a88-85a2971684b3", DataType = "TIMESTAMP", DataSourceName = "DuckDBDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = "93a6e1af-f633-408d-8b42-fdf6249e734c", DataType = "DECIMAL", DataSourceName = "DuckDBDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = "cf14597f-c353-47b2-9b0d-abdca99e9576", DataType = "DOUBLE", DataSourceName = "DuckDBDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = "78179a6e-a90a-4e79-8138-20212396eb71", DataType = "UUID", DataSourceName = "DuckDBDataSource", NetDataType = "System.Guid", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = "154658eb-a5a7-468c-98da-e483de388200", DataType = "SMALLINT", DataSourceName = "DuckDBDataSource", NetDataType = "System.Int16", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = "23743f61-47d2-41bb-95a6-26b3ef6fa9a2", DataType = "INTEGER", DataSourceName = "DuckDBDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = "1da0e8db-0cc1-4a12-95a4-debb63f12c38", DataType = "BIGINT", DataSourceName = "DuckDBDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = "11aa1eba-ac25-4b86-ac80-2a0c76200f39", DataType = "BLOB", DataSourceName = "DuckDBDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = "9842130c-a655-46b8-9840-40ed3808d51d", DataType = "UTINYINT", DataSourceName = "DuckDBDataSource", NetDataType = "System.SByte", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = "5b56a4d4-38e8-4c8f-85b1-c4073efd8c42", DataType = "REAL", DataSourceName = "DuckDBDataSource", NetDataType = "System.Single", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = "b6890f86-c0be-4b72-8411-13bda33e6e49", DataType = "INTERVAL", DataSourceName = "DuckDBDataSource", NetDataType = "System.TimeSpan", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = "e3e0b246-46bf-452b-a56c-1724c1e6f1c4", DataType = "UTINYINT", DataSourceName = "DuckDBDataSource", NetDataType = "System.UInt16", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = "29f78f94-4295-4c55-aa26-1371d2d07a67", DataType = "UTINYINT", DataSourceName = "DuckDBDataSource", NetDataType = "System.UInt32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = "e72647f9-2e3f-46a6-a044-5a33b4b9d3e6", DataType = "UTINYINT", DataSourceName = "DuckDBDataSource", NetDataType = "System.UInt64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = "eec16db7-652f-4eb4-8638-4237595f87be", DataType = "VARCHAR", DataSourceName = "DuckDBDataSource", NetDataType = "System.String", Fav = true },
                new DatatypeMapping { ID = 3, GuidID = Guid.NewGuid().ToString(), DataType = "TEXT", DataSourceName = "DuckDBDataSource", NetDataType = "System.String", Fav = true },
                new DatatypeMapping { ID = 4, GuidID = Guid.NewGuid().ToString(), DataType = "FLOAT", DataSourceName = "DuckDBDataSource", NetDataType = "System.Single", Fav = true },
                new DatatypeMapping { ID = 0, GuidID = "08c661ba-0247-466b-9984-0ccb4f3eda89", DataType = "VARCHAR", DataSourceName = "DuckDBDataSource", NetDataType = "System.Xml", Fav = false }
            };
        }
    }
}