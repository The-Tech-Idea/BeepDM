using System;
using System.Collections.Generic;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Helpers.DataTypesHelpers
{
    /// <summary>
    /// Repository containing search and analytics platform specific type mappings (ElasticSearch, Solr, ClickHouse).
    /// </summary>
    public static partial class DatabaseTypeMappingRepository
    {
        /// <summary>Returns a list of ElasticSearch data type mappings.</summary>
        /// <returns>A list of ElasticSearch data type mappings.</returns>
        public static List<DatatypeMapping> GetElasticSearchDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Core datatypes
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "text", DataSourceName = "ElasticSearchDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "keyword", DataSourceName = "ElasticSearchDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "long", DataSourceName = "ElasticSearchDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "integer", DataSourceName = "ElasticSearchDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "short", DataSourceName = "ElasticSearchDataSource", NetDataType = "System.Int16", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "byte", DataSourceName = "ElasticSearchDataSource", NetDataType = "System.Byte", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "double", DataSourceName = "ElasticSearchDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "float", DataSourceName = "ElasticSearchDataSource", NetDataType = "System.Single", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "half_float", DataSourceName = "ElasticSearchDataSource", NetDataType = "System.Single", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "scaled_float", DataSourceName = "ElasticSearchDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "date", DataSourceName = "ElasticSearchDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "date_nanos", DataSourceName = "ElasticSearchDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "ElasticSearchDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "binary", DataSourceName = "ElasticSearchDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "integer_range", DataSourceName = "ElasticSearchDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "float_range", DataSourceName = "ElasticSearchDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "long_range", DataSourceName = "ElasticSearchDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "double_range", DataSourceName = "ElasticSearchDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "date_range", DataSourceName = "ElasticSearchDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "ip", DataSourceName = "ElasticSearchDataSource", NetDataType = "System.Net.IPAddress", Fav = false },
                
                // Complex datatypes
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "object", DataSourceName = "ElasticSearchDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "flattened", DataSourceName = "ElasticSearchDataSource", NetDataType = "System.Collections.Generic.Dictionary<string, object>", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "nested", DataSourceName = "ElasticSearchDataSource", NetDataType = "System.Collections.Generic.List<object>", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "join", DataSourceName = "ElasticSearchDataSource", NetDataType = "System.Object", Fav = false },
                
                // Geo datatypes
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "geo_point", DataSourceName = "ElasticSearchDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "geo_shape", DataSourceName = "ElasticSearchDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "point", DataSourceName = "ElasticSearchDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "shape", DataSourceName = "ElasticSearchDataSource", NetDataType = "System.Object", Fav = false },
                
                // Specialized datatypes
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "completion", DataSourceName = "ElasticSearchDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "token_count", DataSourceName = "ElasticSearchDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "murmur3", DataSourceName = "ElasticSearchDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "annotated-text", DataSourceName = "ElasticSearchDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "percolator", DataSourceName = "ElasticSearchDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "alias", DataSourceName = "ElasticSearchDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "rank_feature", DataSourceName = "ElasticSearchDataSource", NetDataType = "System.Single", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "rank_features", DataSourceName = "ElasticSearchDataSource", NetDataType = "System.Collections.Generic.Dictionary<string, float>", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "dense_vector", DataSourceName = "ElasticSearchDataSource", NetDataType = "System.Collections.Generic.List<float>", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "sparse_vector", DataSourceName = "ElasticSearchDataSource", NetDataType = "System.Collections.Generic.Dictionary<string, float>", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "search_as_you_type", DataSourceName = "ElasticSearchDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "histogram", DataSourceName = "ElasticSearchDataSource", NetDataType = "System.Object", Fav = false }
            };
        }

        /// <summary>Returns a list of Solr data type mappings.</summary>
        /// <returns>A list of Solr data type mappings.</returns>
        public static List<DatatypeMapping> GetSolrDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Primitive field types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "SolrDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "strings", DataSourceName = "SolrDataSource", NetDataType = "System.Collections.Generic.List<string>", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "SolrDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "booleans", DataSourceName = "SolrDataSource", NetDataType = "System.Collections.Generic.List<bool>", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "pint", DataSourceName = "SolrDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "pints", DataSourceName = "SolrDataSource", NetDataType = "System.Collections.Generic.List<int>", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "pfloat", DataSourceName = "SolrDataSource", NetDataType = "System.Single", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "pfloats", DataSourceName = "SolrDataSource", NetDataType = "System.Collections.Generic.List<float>", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "plong", DataSourceName = "SolrDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "plongs", DataSourceName = "SolrDataSource", NetDataType = "System.Collections.Generic.List<long>", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "pdouble", DataSourceName = "SolrDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "pdoubles", DataSourceName = "SolrDataSource", NetDataType = "System.Collections.Generic.List<double>", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "pdate", DataSourceName = "SolrDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "pdates", DataSourceName = "SolrDataSource", NetDataType = "System.Collections.Generic.List<DateTime>", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "pdaterange", DataSourceName = "SolrDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "binary", DataSourceName = "SolrDataSource", NetDataType = "System.Byte[]", Fav = false },
                
                // Text field types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "text_general", DataSourceName = "SolrDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "text_en", DataSourceName = "SolrDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "text_en_splitting", DataSourceName = "SolrDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "text_en_splitting_tight", DataSourceName = "SolrDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "text_ws", DataSourceName = "SolrDataSource", NetDataType = "System.String", Fav = false },
                
                // Numeric field types (legacy)
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "int", DataSourceName = "SolrDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "float", DataSourceName = "SolrDataSource", NetDataType = "System.Single", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "long", DataSourceName = "SolrDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "double", DataSourceName = "SolrDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "date", DataSourceName = "SolrDataSource", NetDataType = "System.DateTime", Fav = false },
                
                // Spatial field types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "location", DataSourceName = "SolrDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "location_rpt", DataSourceName = "SolrDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "bbox", DataSourceName = "SolrDataSource", NetDataType = "System.Object", Fav = false },
                
                // Currency and other specialized types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "currency", DataSourceName = "SolrDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "uuid", DataSourceName = "SolrDataSource", NetDataType = "System.Guid", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "random", DataSourceName = "SolrDataSource", NetDataType = "System.Single", Fav = false }
            };
        }

        /// <summary>Returns a list of ClickHouse data type mappings.</summary>
        /// <returns>A list of ClickHouse data type mappings.</returns>
        public static List<DatatypeMapping> GetClickHouseDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Integer types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Int8", DataSourceName = "ClickHouseDataSource", NetDataType = "System.SByte", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Int16", DataSourceName = "ClickHouseDataSource", NetDataType = "System.Int16", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Int32", DataSourceName = "ClickHouseDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Int64", DataSourceName = "ClickHouseDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "UInt8", DataSourceName = "ClickHouseDataSource", NetDataType = "System.Byte", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "UInt16", DataSourceName = "ClickHouseDataSource", NetDataType = "System.UInt16", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "UInt32", DataSourceName = "ClickHouseDataSource", NetDataType = "System.UInt32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "UInt64", DataSourceName = "ClickHouseDataSource", NetDataType = "System.UInt64", Fav = false },
                
                // Floating point types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Float32", DataSourceName = "ClickHouseDataSource", NetDataType = "System.Single", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Float64", DataSourceName = "ClickHouseDataSource", NetDataType = "System.Double", Fav = false },
                
                // Decimal types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Decimal", DataSourceName = "ClickHouseDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Decimal32", DataSourceName = "ClickHouseDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Decimal64", DataSourceName = "ClickHouseDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Decimal128", DataSourceName = "ClickHouseDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Decimal256", DataSourceName = "ClickHouseDataSource", NetDataType = "System.Decimal", Fav = false },
                
                // Boolean type
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Boolean", DataSourceName = "ClickHouseDataSource", NetDataType = "System.Boolean", Fav = false },
                
                // String types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "String", DataSourceName = "ClickHouseDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "FixedString", DataSourceName = "ClickHouseDataSource", NetDataType = "System.String", Fav = false },
                
                // Date and DateTime types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Date", DataSourceName = "ClickHouseDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Date32", DataSourceName = "ClickHouseDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DateTime", DataSourceName = "ClickHouseDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DateTime64", DataSourceName = "ClickHouseDataSource", NetDataType = "System.DateTime", Fav = false },
                
                // UUID type
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "UUID", DataSourceName = "ClickHouseDataSource", NetDataType = "System.Guid", Fav = false },
                
                // Array types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Array", DataSourceName = "ClickHouseDataSource", NetDataType = "System.Array", Fav = false },
                
                // Tuple type
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Tuple", DataSourceName = "ClickHouseDataSource", NetDataType = "System.ValueTuple", Fav = false },
                
                // Map type
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Map", DataSourceName = "ClickHouseDataSource", NetDataType = "System.Collections.Generic.Dictionary<object, object>", Fav = false },
                
                // Nullable type
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Nullable", DataSourceName = "ClickHouseDataSource", NetDataType = "System.Object", Fav = false },
                
                // LowCardinality type
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "LowCardinality", DataSourceName = "ClickHouseDataSource", NetDataType = "System.String", Fav = false },
                
                // Special types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "IPv4", DataSourceName = "ClickHouseDataSource", NetDataType = "System.Net.IPAddress", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "IPv6", DataSourceName = "ClickHouseDataSource", NetDataType = "System.Net.IPAddress", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Enum8", DataSourceName = "ClickHouseDataSource", NetDataType = "System.Enum", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Enum16", DataSourceName = "ClickHouseDataSource", NetDataType = "System.Enum", Fav = false },
                
                // Nested type
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Nested", DataSourceName = "ClickHouseDataSource", NetDataType = "System.Object", Fav = false },
                
                // JSON type
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "JSON", DataSourceName = "ClickHouseDataSource", NetDataType = "System.String", Fav = false }
            };
        }

        /// <summary>Returns a list of RavenDB data type mappings.</summary>
        /// <returns>A list of RavenDB data type mappings.</returns>
        public static List<DatatypeMapping> GetRavenDBDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "String", DataSourceName = "RavenDBDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Number", DataSourceName = "RavenDBDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Boolean", DataSourceName = "RavenDBDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Array", DataSourceName = "RavenDBDataSource", NetDataType = "System.Collections.Generic.List<object>", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Object", DataSourceName = "RavenDBDataSource", NetDataType = "System.Collections.Generic.Dictionary<string, object>", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Null", DataSourceName = "RavenDBDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DateTime", DataSourceName = "RavenDBDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TimeSpan", DataSourceName = "RavenDBDataSource", NetDataType = "System.TimeSpan", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DateTimeOffset", DataSourceName = "RavenDBDataSource", NetDataType = "System.DateTimeOffset", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Guid", DataSourceName = "RavenDBDataSource", NetDataType = "System.Guid", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Decimal", DataSourceName = "RavenDBDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Binary", DataSourceName = "RavenDBDataSource", NetDataType = "System.Byte[]", Fav = false }
            };
        }

        /// <summary>Returns a list of VistaDB data type mappings.</summary>
        /// <returns>A list of VistaDB data type mappings.</returns>
        public static List<DatatypeMapping> GetVistaDBDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "VARCHAR", DataSourceName = "VistaDBDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "NVARCHAR", DataSourceName = "VistaDBDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "CHAR", DataSourceName = "VistaDBDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "NCHAR", DataSourceName = "VistaDBDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TEXT", DataSourceName = "VistaDBDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "NTEXT", DataSourceName = "VistaDBDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INT", DataSourceName = "VistaDBDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BIGINT", DataSourceName = "VistaDBDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "SMALLINT", DataSourceName = "VistaDBDataSource", NetDataType = "System.Int16", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TINYINT", DataSourceName = "VistaDBDataSource", NetDataType = "System.Byte", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BIT", DataSourceName = "VistaDBDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "REAL", DataSourceName = "VistaDBDataSource", NetDataType = "System.Single", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "FLOAT", DataSourceName = "VistaDBDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "MONEY", DataSourceName = "VistaDBDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DECIMAL", DataSourceName = "VistaDBDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "NUMERIC", DataSourceName = "VistaDBDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DATETIME", DataSourceName = "VistaDBDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "SMALLDATETIME", DataSourceName = "VistaDBDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "IMAGE", DataSourceName = "VistaDBDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "VARBINARY", DataSourceName = "VistaDBDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BINARY", DataSourceName = "VistaDBDataSource", NetDataType = "System.Byte[]", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "UNIQUEIDENTIFIER", DataSourceName = "VistaDBDataSource", NetDataType = "System.Guid", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TIMESTAMP", DataSourceName = "VistaDBDataSource", NetDataType = "System.Byte[]", Fav = false }
            };
        }
    }
}