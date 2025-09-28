using System;
using System.Collections.Generic;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Helpers.DataTypesHelpers
{
    /// <summary>
    /// Repository containing Firebird and LiteDB specific type mappings.
    /// </summary>
    public static partial class DatabaseTypeMappingRepository
    {
        /// <summary>Returns a list of datatype mappings for Firebird database.</summary>
        /// <returns>A list of datatype mappings for Firebird database.</returns>
        public static List<DatatypeMapping> GetFireBirdDataTypesMapping()
        {
            return new List<DatatypeMapping>
        {
            new DatatypeMapping { ID = 0, GuidID = "18399b26-ec58-494a-b018-39e852e12439", DataType = "char(1)", DataSourceName = "FireBirdDataSource", NetDataType = "System.Boolean", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "51a6afdf-c4c8-4972-b4e4-847697a0f0f0", DataType = "numeric(3,0)", DataSourceName = "FireBirdDataSource", NetDataType = "System.Byte", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "b4a0040f-beed-4e46-98eb-3b364b09490c", DataType = "numeric(3,0)", DataSourceName = "FireBirdDataSource", NetDataType = "System.SByte", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "711bc9fe-f122-49e5-a8e7-747619b82928", DataType = "char", DataSourceName = "FireBirdDataSource", NetDataType = "System.Char", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "c9edda9b-1710-4f09-ad6d-9d4c0c620359", DataType = "decimal(18,4)", DataSourceName = "FireBirdDataSource", NetDataType = "System.Decimal", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "4428d11c-ffb7-41ce-84c7-9e2f4905eba1", DataType = "double precision", DataSourceName = "FireBirdDataSource", NetDataType = "System.Double", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "25290082-e54f-45de-bb8b-2fc0dee541db", DataType = "float", DataSourceName = "FireBirdDataSource", NetDataType = "System.Single", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "96ef68ab-ee2f-4436-a5f9-2f8254cb8556", DataType = "small int", DataSourceName = "FireBirdDataSource", NetDataType = "System.Int16", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "702f96ae-53a3-449d-af0c-4dccf3ae05a3", DataType = "numeric(5,0)", DataSourceName = "FireBirdDataSource", NetDataType = "System.UInt16", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "249d4f90-67d6-4b5e-bd05-d208da821bce", DataType = "integer", DataSourceName = "FireBirdDataSource", NetDataType = "System.Int32", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "e4a808f2-494d-4600-b8c2-ae3816b5097c", DataType = "numeric(10,0)", DataSourceName = "FireBirdDataSource", NetDataType = "System.UInt32", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "8b33c04e-e841-49ed-a338-4beca3fee468", DataType = "bigint", DataSourceName = "FireBirdDataSource", NetDataType = "System.Int64", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "8bd33d9d-9e76-4b3d-86de-d872d1c4b9cf", DataType = "numeric(18,0)", DataSourceName = "FireBirdDataSource", NetDataType = "System.UInt64", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "4b38b9a2-4fa1-4ccd-bde8-72d62264c238", DataType = "char(36)", DataSourceName = "FireBirdDataSource", NetDataType = "System.Guid", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "5881f031-4e6c-4a38-b090-f62c25928ab7", DataType = "char(N)", DataSourceName = "FireBirdDataSource", NetDataType = "System.String", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "86dd903d-0ef3-40de-9f2a-3b4536fd2b79", DataType = "timestamp", DataSourceName = "FireBirdDataSource", NetDataType = "System.DateTime", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "c5c155d3-af68-4c59-8afc-860ddcbe5888", DataType = "double precision", DataSourceName = "FireBirdDataSource", NetDataType = "System.TimeSpan", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "a77ff872-fbf9-4e0d-b1f5-50fc2c6036bb", DataType = "BLOB", DataSourceName = "FireBirdDataSource", NetDataType = "System.Byte[]", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "28dd3198-4a74-4d4c-a078-65a62ca605cf", DataType = "TEXT", DataSourceName = "FireBirdDataSource", NetDataType = "System.String", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "VARCHAR", DataSourceName = "FireBirdDataSource", NetDataType = "System.String", Fav = true },
            new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DATE", DataSourceName = "FireBirdDataSource", NetDataType = "System.DateTime", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIME", DataSourceName = "FireBirdDataSource", NetDataType = "System.TimeSpan", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "NUMERIC", DataSourceName = "FireBirdDataSource", NetDataType = "System.Decimal", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DECIMAL", DataSourceName = "FireBirdDataSource", NetDataType = "System.Decimal", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BLOB SUB_TYPE TEXT", DataSourceName = "FireBirdDataSource", NetDataType = "System.String", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BLOB SUB_TYPE BINARY", DataSourceName = "FireBirdDataSource", NetDataType = "System.Byte[]", Fav = false }
        };
        }

        /// <summary>Returns a list of LiteDB data type mappings.</summary>
        /// <returns>A list of DatatypeMapping objects representing the mappings between LiteDB data types and their corresponding .NET data types.</returns>
        public static List<DatatypeMapping> GetLiteDBDataTypesMapping()
        {
            return new List<DatatypeMapping>
    {
        new DatatypeMapping
        {
            ID = 0,
            GuidID = Guid.NewGuid().ToString(),
            DataType = "Int32",
            DataSourceName = "LiteDBDataSource",
            NetDataType = typeof(int).FullName,
            Fav = false
        },
        new DatatypeMapping
        {
            ID = 0,
            GuidID = Guid.NewGuid().ToString(),
            DataType = "Int64",
            DataSourceName = "LiteDBDataSource",
            NetDataType = typeof(long).FullName,
            Fav = false
        },
        new DatatypeMapping
        {
            ID = 0,
            GuidID = Guid.NewGuid().ToString(),
            DataType = "Double",
            DataSourceName = "LiteDBDataSource",
            NetDataType = typeof(double).FullName,
            Fav = false
        },
        new DatatypeMapping
        {
            ID = 0,
            GuidID = Guid.NewGuid().ToString(),
            DataType = "Decimal",
            DataSourceName = "LiteDBDataSource",
            NetDataType = typeof(decimal).FullName,
            Fav = false
        },
        new DatatypeMapping
        {
            ID = 0,
            GuidID = Guid.NewGuid().ToString(),
            DataType = "String",
            DataSourceName = "LiteDBDataSource",
            NetDataType = "System.String",
            Fav = false
        },
        new DatatypeMapping
        {
            ID = 0,
            GuidID = Guid.NewGuid().ToString(),
            DataType = "Document",
            DataSourceName = "LiteDBDataSource",
            NetDataType = "System.Collections.Generic.Dictionary<string, LiteDB.BsonValue>",
            Fav = false
        },
        new DatatypeMapping
        {
            ID = 0,
            GuidID = Guid.NewGuid().ToString(),
            DataType = "Array",
            DataSourceName = "LiteDBDataSource",
            NetDataType = "System.Collections.Generic.List<LiteDB.BsonValue>",
            Fav = false
        },
        new DatatypeMapping
        {
            ID = 0,
            GuidID = Guid.NewGuid().ToString(),
            DataType = "Binary",
            DataSourceName = "LiteDBDataSource",
            NetDataType = typeof(byte[]).FullName,
            Fav = false
        },
        new DatatypeMapping
        {
            ID = 0,
            GuidID = Guid.NewGuid().ToString(),
            DataType = "ObjectId",
            DataSourceName = "LiteDBDataSource",
            NetDataType = "System.String",
            Fav = false
        },
        new DatatypeMapping
        {
            ID = 0,
            GuidID = Guid.NewGuid().ToString(),
            DataType = "Guid",
            DataSourceName = "LiteDBDataSource",
            NetDataType = "System.String",
            Fav = false
        },
        new DatatypeMapping
        {
            ID = 0,
            GuidID = Guid.NewGuid().ToString(),
            DataType = "Boolean",
            DataSourceName = "LiteDBDataSource",
            NetDataType = typeof(bool).FullName,
            Fav = false
        },
        new DatatypeMapping
        {
            ID = 0,
            GuidID = Guid.NewGuid().ToString(),
            DataType = "DateTime",
            DataSourceName = "LiteDBDataSource",
            NetDataType = typeof(DateTime).FullName,
            Fav = false
        }
    };
        }
    }
}