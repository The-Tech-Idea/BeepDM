using System;
using System.Collections.Generic;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Helpers.DataTypesHelpers
{
    /// <summary>
    /// Repository containing PostgreSQL and MySQL specific type mappings.
    /// </summary>
    public static partial class DatabaseTypeMappingRepository
    {
        /// <summary>Returns a list of datatype mappings for PostgreSQL.</summary>
        /// <returns>A list of datatype mappings for PostgreSQL.</returns>
        public static List<DatatypeMapping> GetPostgreDataTypesMapping()
        {
            return new List<DatatypeMapping>
        {
            new DatatypeMapping { ID = 0, GuidID = "a0462937-cf4d-48cd-88ab-406f195ab256", DataType = "bool", DataSourceName = "PostgreDataSource", NetDataType = "System.Boolean", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "8e057539-b9e4-4a42-8fa0-8647293872a2", DataType = "smallint", DataSourceName = "PostgreDataSource", NetDataType = "System.Byte", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "bfcdab96-6f35-4513-bc00-2487fca0b064", DataType = "smallint", DataSourceName = "PostgreDataSource", NetDataType = "System.SByte", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "cd4b8eae-4093-42a1-b907-511a07f71ca9", DataType = "char(1)", DataSourceName = "PostgreDataSource", NetDataType = "System.Char", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "4ec6d7b5-fd5b-42eb-83e1-03a2a1233b96", DataType = "decimal(28,8)", DataSourceName = "PostgreDataSource", NetDataType = "System.Decimal", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "b82f13b0-d933-475c-a9b9-6e14b5008284", DataType = "double precision", DataSourceName = "PostgreDataSource", NetDataType = "System.Double", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "a068c9d5-5618-4a61-b2a3-a6d8472aef69", DataType = "real", DataSourceName = "PostgreDataSource", NetDataType = "System.Single", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "f92ddd3c-302a-44e6-93d4-1ae77cd74d5b", DataType = "smallint", DataSourceName = "PostgreDataSource", NetDataType = "System.Int16", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "e4eb09bd-38f0-4cf2-884b-01df85f13ef2", DataType = "numeric(5,0)", DataSourceName = "PostgreDataSource", NetDataType = "System.UInt16", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "7b87f4ad-2114-4634-aed7-ed83f60df1fe", DataType = "int", DataSourceName = "PostgreDataSource", NetDataType = "System.Int32", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "040fac6c-f381-405e-9950-fc83fbfe0aac", DataType = "numeric(10,0)", DataSourceName = "PostgreDataSource", NetDataType = "System.UInt32", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "2da3fe2d-3a4e-479f-af4a-010673048bea", DataType = "bigint", DataSourceName = "PostgreDataSource", NetDataType = "System.Int64", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "ab4f85b2-9559-4f11-86b1-83b0bedb1803", DataType = "numeric(20,0)", DataSourceName = "PostgreDataSource", NetDataType = "System.UInt64", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "549d44ac-05d1-4fab-8e83-e46b2f3aa351", DataType = "uuid", DataSourceName = "PostgreDataSource", NetDataType = "System.Guid", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "2bf31711-81df-4076-85e3-48270dca1b33", DataType = "varchar(N)", DataSourceName = "PostgreDataSource", NetDataType = "System.String", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "a756ee8d-af4c-44c8-8308-f38e7d5a3a6d", DataType = "timestamp without time zone", DataSourceName = "PostgreDataSource", NetDataType = "System.DateTime", Fav = true },
            new DatatypeMapping { ID = 0, GuidID = "a7329dde-29ad-40f0-93a6-dc4830131543", DataType = "double precision", DataSourceName = "PostgreDataSource", NetDataType = "System.TimeSpan", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "e1efb191-fdb1-443e-a969-8492a45b6744", DataType = "bytea", DataSourceName = "PostgreDataSource", NetDataType = "System.Byte[]", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "text", DataSourceName = "PostgreDataSource", NetDataType = "System.String", Fav = true },
            new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "timestamp with time zone", DataSourceName = "PostgreDataSource", NetDataType = "System.DateTimeOffset", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "interval", DataSourceName = "PostgreDataSource", NetDataType = "System.TimeSpan", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "json", DataSourceName = "PostgreDataSource", NetDataType = "System.String", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "jsonb", DataSourceName = "PostgreDataSource", NetDataType = "System.String", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "cidr", DataSourceName = "PostgreDataSource", NetDataType = "System.String", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "inet", DataSourceName = "PostgreDataSource", NetDataType = "System.String", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "macaddr", DataSourceName = "PostgreDataSource", NetDataType = "System.String", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "tsvector", DataSourceName = "PostgreDataSource", NetDataType = "System.String", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "tsquery", DataSourceName = "PostgreDataSource", NetDataType = "System.String", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "xml", DataSourceName = "PostgreDataSource", NetDataType = "System.Xml.Linq.XElement", Fav = false }
        };
        }

        /// <summary>Returns a list of datatype mappings between MySQL and .NET data types.</summary>
        /// <returns>A list of datatype mappings.</returns>
        public static List<DatatypeMapping> GetMySqlDataTypesMapping()
        {
            return new List<DatatypeMapping>
        {
            new DatatypeMapping { ID = 0, GuidID = "0f2dda35-a3f7-4d73-8ae9-50d5d2ebc9a6", DataType = "bit", DataSourceName = "MySQLDataSource", NetDataType = "System.Boolean", Fav = true },
            new DatatypeMapping { ID = 0, GuidID = "1715cf14-1d7a-46fd-abee-9fbf0e97fb1d", DataType = "tiny unsigned", DataSourceName = "MySQLDataSource", NetDataType = "System.Byte", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "43139130-3531-4252-8e2c-efd8999849f6", DataType = "tinyint", DataSourceName = "MySQLDataSource", NetDataType = "System.SByte", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "bd1c2f8c-6c00-4545-a250-98cd706d7520", DataType = "char", DataSourceName = "MySQLDataSource", NetDataType = "System.Char", Fav = true },
            new DatatypeMapping { ID = 0, GuidID = "7c1380a6-b976-4896-ac61-c1dd95829128", DataType = "decimal", DataSourceName = "MySQLDataSource", NetDataType = "System.Decimal", Fav = true },
            new DatatypeMapping { ID = 0, GuidID = "4340401d-6058-47fe-98bb-2c92bf37ffb7", DataType = "double", DataSourceName = "MySQLDataSource", NetDataType = "System.Double", Fav = true },
            new DatatypeMapping { ID = 0, GuidID = "e225d674-0f64-432e-86bb-7c6f25f15362", DataType = "real", DataSourceName = "MySQLDataSource", NetDataType = "System.Single", Fav = true },
            new DatatypeMapping { ID = 0, GuidID = "e52090b4-dff2-403d-9bf6-dc95e92380ca", DataType = "smallint", DataSourceName = "MySQLDataSource", NetDataType = "System.Int16", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "a0fe7651-e9f4-441f-bc73-28defc35ad57", DataType = "smallint unsigned", DataSourceName = "MySQLDataSource", NetDataType = "System.UInt16", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "42affd50-333b-4a84-803a-fb2803575e36", DataType = "int", DataSourceName = "MySQLDataSource", NetDataType = "System.Int32", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "918c4769-b8fe-4175-9c94-089e9b7d21a9", DataType = "int unsigned", DataSourceName = "MySQLDataSource", NetDataType = "System.UInt32", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "111cf764-016d-40e3-97c1-881f0f212e66", DataType = "bigint", DataSourceName = "MySQLDataSource", NetDataType = "System.Int64", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "63ab5e03-0a2d-40d1-ae46-b3cc86902ed1", DataType = "bigint unsigned", DataSourceName = "MySQLDataSource", NetDataType = "System.UInt64", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "b28dd595-d0a8-4703-bae7-b97939d3d20f", DataType = "char(38)", DataSourceName = "MySQLDataSource", NetDataType = "System.Guid", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "daf8b650-039e-4519-bc67-4dcf860faa66", DataType = "varchar(N)", DataSourceName = "MySQLDataSource", NetDataType = "System.String", Fav = true },
            new DatatypeMapping { ID = 0, GuidID = "50d0c48f-8b86-48f2-94db-d3aa4bf700de", DataType = "datetime(6)", DataSourceName = "MySQLDataSource", NetDataType = "System.DateTime", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "bfdcffea-bf66-499b-93be-d8cd22382458", DataType = "double", DataSourceName = "MySQLDataSource", NetDataType = "System.TimeSpan", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "6825dddb-4459-4c2f-b3b3-fe750d1988e0", DataType = "TINYBLOB", DataSourceName = "MySQLDataSource", NetDataType = "System.Byte[]", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "78627e34-63b8-4ab2-b31c-84f28256b481", DataType = "BLOB", DataSourceName = "MySQLDataSource", NetDataType = "System.Byte[]", Fav = true },
            new DatatypeMapping { ID = 0, GuidID = "8bc228bc-301f-40c5-a8a6-5ca44c83a5e5", DataType = "MEDIUMBLOB", DataSourceName = "MySQLDataSource", NetDataType = "System.Byte[]", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = "e4b66269-b72d-492a-80c5-31d61a455ff7", DataType = "LONGBLOB", DataSourceName = "MySQLDataSource", NetDataType = "System.Byte[]", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "tinytext", DataSourceName = "MySQLDataSource", NetDataType = "System.String", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "mediumtext", DataSourceName = "MySQLDataSource", NetDataType = "System.String", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "longtext", DataSourceName = "MySQLDataSource", NetDataType = "System.String", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "enum", DataSourceName = "MySQLDataSource", NetDataType = "System.String", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "set", DataSourceName = "MySQLDataSource", NetDataType = "System.String", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "date", DataSourceName = "MySQLDataSource", NetDataType = "System.DateTime", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "time", DataSourceName = "MySQLDataSource", NetDataType = "System.TimeSpan", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "year", DataSourceName = "MySQLDataSource", NetDataType = "System.Int32", Fav = false },
            new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "json", DataSourceName = "MySQLDataSource", NetDataType = "System.String", Fav = false }
        };
        }
    }
}