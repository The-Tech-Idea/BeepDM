using System;
using System.Collections.Generic;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Helpers.DataTypesHelpers
{
    /// <summary>
    /// Repository containing SQL Server specific type mappings.
    /// </summary>
    public static partial class DatabaseTypeMappingRepository
    {
        /// <summary>
        /// Generates a list of datatype mappings between SQL Server data types and corresponding .NET data types.
        /// </summary>
        /// <returns>A list of datatype mappings.</returns>
        public static List<DatatypeMapping> GenerateSqlServerDataTypesMapping()
        {
            List<DatatypeMapping> mappings = new List<DatatypeMapping>
            {
                new DatatypeMapping { ID = 1, GuidID = Guid.NewGuid().ToString(), DataType = "bigint", DataSourceName = "SQLServerDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 2, GuidID = Guid.NewGuid().ToString(), DataType = "binary", DataSourceName = "SQLServerDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 3, GuidID = Guid.NewGuid().ToString(), DataType = "bit", DataSourceName = "SQLServerDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 4, GuidID = Guid.NewGuid().ToString(), DataType = "char", DataSourceName = "SQLServerDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 5, GuidID = Guid.NewGuid().ToString(), DataType = "date", DataSourceName = "SQLServerDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 6, GuidID = Guid.NewGuid().ToString(), DataType = "datetime", DataSourceName = "SQLServerDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 7, GuidID = Guid.NewGuid().ToString(), DataType = "datetime2", DataSourceName = "SQLServerDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 8, GuidID = Guid.NewGuid().ToString(), DataType = "decimal", DataSourceName = "SQLServerDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 9, GuidID = Guid.NewGuid().ToString(), DataType = "float", DataSourceName = "SQLServerDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 10, GuidID = Guid.NewGuid().ToString(), DataType = "image", DataSourceName = "SQLServerDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 11, GuidID = Guid.NewGuid().ToString(), DataType = "int", DataSourceName = "SQLServerDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 12, GuidID = Guid.NewGuid().ToString(), DataType = "money", DataSourceName = "SQLServerDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 13, GuidID = Guid.NewGuid().ToString(), DataType = "nchar", DataSourceName = "SQLServerDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 14, GuidID = Guid.NewGuid().ToString(), DataType = "ntext", DataSourceName = "SQLServerDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 15, GuidID = Guid.NewGuid().ToString(), DataType = "numeric", DataSourceName = "SQLServerDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 16, GuidID = Guid.NewGuid().ToString(), DataType = "nvarchar", DataSourceName = "SQLServerDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 17, GuidID = Guid.NewGuid().ToString(), DataType = "real", DataSourceName = "SQLServerDataSource", NetDataType = "System.Single", Fav = false },
                new DatatypeMapping { ID = 18, GuidID = Guid.NewGuid().ToString(), DataType = "smallint", DataSourceName = "SQLServerDataSource", NetDataType = "System.Int16", Fav = false },
                new DatatypeMapping { ID = 19, GuidID = Guid.NewGuid().ToString(), DataType = "text", DataSourceName = "SQLServerDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 20, GuidID = Guid.NewGuid().ToString(), DataType = "tinyint", DataSourceName = "SQLServerDataSource", NetDataType = "System.Byte", Fav = false },
                new DatatypeMapping { ID = 21, GuidID = Guid.NewGuid().ToString(), DataType = "varbinary", DataSourceName = "SQLServerDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 22, GuidID = Guid.NewGuid().ToString(), DataType = "varchar", DataSourceName = "SQLServerDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 23, GuidID = Guid.NewGuid().ToString(), DataType = "xml", DataSourceName = "SQLServerDataSource", NetDataType = "System.Xml.XmlDocument", Fav = false },
                
                // Additional SQL Server types
                new DatatypeMapping
                {
                    ID = 0,
                    GuidID = Guid.NewGuid().ToString(),
                    DataType = "smallmoney",
                    DataSourceName = "SQLServerDataSource",
                    NetDataType = "System.Decimal",
                    Fav = false
                },
                new DatatypeMapping
                {
                    ID = 0,
                    GuidID = Guid.NewGuid().ToString(),
                    DataType = "datetimeoffset",
                    DataSourceName = "SQLServerDataSource",
                    NetDataType = "System.DateTimeOffset",
                    Fav = false
                },
                new DatatypeMapping
                {
                    ID = 0,
                    GuidID = Guid.NewGuid().ToString(),
                    DataType = "time",
                    DataSourceName = "SQLServerDataSource",
                    NetDataType = "System.TimeSpan",
                    Fav = false
                },
                new DatatypeMapping
                {
                    ID = 0,
                    GuidID = Guid.NewGuid().ToString(),
                    DataType = "uniqueidentifier",
                    DataSourceName = "SQLServerDataSource",
                    NetDataType = "System.Guid",
                    Fav = false
                },
                new DatatypeMapping
                {
                    ID = 0,
                    GuidID = Guid.NewGuid().ToString(),
                    DataType = "sql_variant",
                    DataSourceName = "SQLServerDataSource",
                    NetDataType = "System.Object",
                    Fav = false
                },
                new DatatypeMapping
                {
                    ID = 0,
                    GuidID = Guid.NewGuid().ToString(),
                    DataType = "geography",
                    DataSourceName = "SQLServerDataSource",
                    NetDataType = "Microsoft.SqlServer.Types.SqlGeography",
                    Fav = false
                },
                new DatatypeMapping
                {
                    ID = 0,
                    GuidID = Guid.NewGuid().ToString(),
                    DataType = "geometry",
                    DataSourceName = "SQLServerDataSource",
                    NetDataType = "Microsoft.SqlServer.Types.SqlGeometry",
                    Fav = false
                },
                new DatatypeMapping
                {
                    ID = 0,
                    GuidID = Guid.NewGuid().ToString(),
                    DataType = "hierarchyid",
                    DataSourceName = "SQLServerDataSource",
                    NetDataType = "Microsoft.SqlServer.Types.SqlHierarchyId",
                    Fav = false
                }
            };

            return mappings;
        }
    }
}