using System;
using System.Collections.Generic;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Helpers.DataTypesHelpers
{
    /// <summary>
    /// Repository containing Vector Database specific type mappings (PineCone, Qdrant, Weaviate, Milvus, etc.).
    /// </summary>
    public static partial class DatabaseTypeMappingRepository
    {
        /// <summary>Returns a list of datatype mappings for PineCone.</summary>
        /// <returns>A list of datatype mappings for PineCone.</returns>
        public static List<DatatypeMapping> GetPineConeDataTypesMapping()
        {
            return new List<DatatypeMapping>
    {
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Vector", DataSourceName = "PineConeDataSource", NetDataType = "System.Collections.Generic.List<float>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "String", DataSourceName = "PineConeDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Integer", DataSourceName = "PineConeDataSource", NetDataType = "System.Int32", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Float", DataSourceName = "PineConeDataSource", NetDataType = "System.Single", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Boolean", DataSourceName = "PineConeDataSource", NetDataType = "System.Boolean", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "ID", DataSourceName = "PineConeDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Metadata", DataSourceName = "PineConeDataSource", NetDataType = "System.Collections.Generic.Dictionary<string, object>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "SparseVector", DataSourceName = "PineConeDataSource", NetDataType = "System.Collections.Generic.Dictionary<int, float>", Fav = false }
    };
        }

        /// <summary>Returns a list of datatype mappings for Qdrant.</summary>
        /// <returns>A list of datatype mappings for Qdrant.</returns>
        public static List<DatatypeMapping> GetQdrantDataTypesMapping()
        {
            return new List<DatatypeMapping>
    {
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Vector", DataSourceName = "QdrantDataSource", NetDataType = "System.Collections.Generic.List<float>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "String", DataSourceName = "QdrantDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Integer", DataSourceName = "QdrantDataSource", NetDataType = "System.Int64", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Float", DataSourceName = "QdrantDataSource", NetDataType = "System.Double", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Boolean", DataSourceName = "QdrantDataSource", NetDataType = "System.Boolean", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Keyword", DataSourceName = "QdrantDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Geo", DataSourceName = "QdrantDataSource", NetDataType = "System.Collections.Generic.List<System.Double>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Array", DataSourceName = "QdrantDataSource", NetDataType = "System.Collections.Generic.List<object>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Payload", DataSourceName = "QdrantDataSource", NetDataType = "System.Collections.Generic.Dictionary<string, object>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "UUID", DataSourceName = "QdrantDataSource", NetDataType = "System.Guid", Fav = false }
    };
        }

        /// <summary>Returns a list of datatype mappings for ShapVector.</summary>
        /// <returns>A list of datatype mappings for ShapVector.</returns>
        public static List<DatatypeMapping> GetShapVectorDataTypesMapping()
        {
            return new List<DatatypeMapping>
    {
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Vector", DataSourceName = "ShapVectorDataSource", NetDataType = "System.Collections.Generic.List<float>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "String", DataSourceName = "ShapVectorDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Integer", DataSourceName = "ShapVectorDataSource", NetDataType = "System.Int32", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Double", DataSourceName = "ShapVectorDataSource", NetDataType = "System.Double", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Boolean", DataSourceName = "ShapVectorDataSource", NetDataType = "System.Boolean", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Document", DataSourceName = "ShapVectorDataSource", NetDataType = "System.Collections.Generic.Dictionary<string, object>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Array", DataSourceName = "ShapVectorDataSource", NetDataType = "System.Collections.Generic.List<object>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Binary", DataSourceName = "ShapVectorDataSource", NetDataType = "System.Byte[]", Fav = false }
    };
        }

        /// <summary>Returns a list of datatype mappings for Weaviate.</summary>
        /// <returns>A list of datatype mappings for Weaviate.</returns>
        public static List<DatatypeMapping> GetWeaviateDataTypesMapping()
        {
            return new List<DatatypeMapping>
    {
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "WeaviateDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "int", DataSourceName = "WeaviateDataSource", NetDataType = "System.Int32", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "number", DataSourceName = "WeaviateDataSource", NetDataType = "System.Double", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "WeaviateDataSource", NetDataType = "System.Boolean", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "date", DataSourceName = "WeaviateDataSource", NetDataType = "System.DateTime", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "text", DataSourceName = "WeaviateDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "geoCoordinates", DataSourceName = "WeaviateDataSource", NetDataType = "System.Collections.Generic.Dictionary<string, double>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "phoneNumber", DataSourceName = "WeaviateDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "uuid", DataSourceName = "WeaviateDataSource", NetDataType = "System.Guid", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "vector", DataSourceName = "WeaviateDataSource", NetDataType = "System.Collections.Generic.List<float>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "object", DataSourceName = "WeaviateDataSource", NetDataType = "System.Collections.Generic.Dictionary<string, object>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "blob", DataSourceName = "WeaviateDataSource", NetDataType = "System.Byte[]", Fav = false }
    };
        }

        /// <summary>Returns a list of datatype mappings for Milvus.</summary>
        /// <returns>A list of datatype mappings for Milvus.</returns>
        public static List<DatatypeMapping> GetMilvusDataTypesMapping()
        {
            return new List<DatatypeMapping>
    {
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BOOL", DataSourceName = "MilvusDataSource", NetDataType = "System.Boolean", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INT8", DataSourceName = "MilvusDataSource", NetDataType = "System.SByte", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INT16", DataSourceName = "MilvusDataSource", NetDataType = "System.Int16", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INT32", DataSourceName = "MilvusDataSource", NetDataType = "System.Int32", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INT64", DataSourceName = "MilvusDataSource", NetDataType = "System.Int64", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "FLOAT", DataSourceName = "MilvusDataSource", NetDataType = "System.Single", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DOUBLE", DataSourceName = "MilvusDataSource", NetDataType = "System.Double", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "STRING", DataSourceName = "MilvusDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "VARCHAR", DataSourceName = "MilvusDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "ARRAY", DataSourceName = "MilvusDataSource", NetDataType = "System.Collections.Generic.List<object>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "JSON", DataSourceName = "MilvusDataSource", NetDataType = "System.Collections.Generic.Dictionary<string, object>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "VECTOR_FLOAT", DataSourceName = "MilvusDataSource", NetDataType = "System.Collections.Generic.List<float>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "VECTOR_BINARY", DataSourceName = "MilvusDataSource", NetDataType = "System.Byte[]", Fav = false }
    };
        }

        /// <summary>Returns a list of datatype mappings for RedisVector.</summary>
        /// <returns>A list of datatype mappings for RedisVector.</returns>
        public static List<DatatypeMapping> GetRedisVectorDataTypesMapping()
        {
            return new List<DatatypeMapping>
    {
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TEXT", DataSourceName = "RedisVectorDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "TAG", DataSourceName = "RedisVectorDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "NUMERIC", DataSourceName = "RedisVectorDataSource", NetDataType = "System.Double", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "VECTOR", DataSourceName = "RedisVectorDataSource", NetDataType = "System.Collections.Generic.List<float>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "GEO", DataSourceName = "RedisVectorDataSource", NetDataType = "System.Collections.Generic.List<double>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "JSON", DataSourceName = "RedisVectorDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BYTE", DataSourceName = "RedisVectorDataSource", NetDataType = "System.Byte[]", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "HASH", DataSourceName = "RedisVectorDataSource", NetDataType = "System.Collections.Generic.Dictionary<string, string>", Fav = false }
    };
        }

        /// <summary>Returns a list of datatype mappings for Zilliz.</summary>
        /// <returns>A list of datatype mappings for Zilliz.</returns>
        public static List<DatatypeMapping> GetZillizDataTypesMapping()
        {
            return new List<DatatypeMapping>
    {
        // Zilliz is a cloud version of Milvus, so it uses similar data types
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "BOOL", DataSourceName = "ZillizDataSource", NetDataType = "System.Boolean", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INT8", DataSourceName = "ZillizDataSource", NetDataType = "System.SByte", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INT16", DataSourceName = "ZillizDataSource", NetDataType = "System.Int16", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INT32", DataSourceName = "ZillizDataSource", NetDataType = "System.Int32", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "INT64", DataSourceName = "ZillizDataSource", NetDataType = "System.Int64", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "FLOAT", DataSourceName = "ZillizDataSource", NetDataType = "System.Single", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DOUBLE", DataSourceName = "ZillizDataSource", NetDataType = "System.Double", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "STRING", DataSourceName = "ZillizDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "VARCHAR", DataSourceName = "ZillizDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "ARRAY", DataSourceName = "ZillizDataSource", NetDataType = "System.Collections.Generic.List<object>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "JSON", DataSourceName = "ZillizDataSource", NetDataType = "System.Collections.Generic.Dictionary<string, object>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "VECTOR_FLOAT", DataSourceName = "ZillizDataSource", NetDataType = "System.Collections.Generic.List<float>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "VECTOR_BINARY", DataSourceName = "ZillizDataSource", NetDataType = "System.Byte[]", Fav = false }
    };
        }

        /// <summary>Returns a list of datatype mappings for Vespa.</summary>
        /// <returns>A list of datatype mappings for Vespa.</returns>
        public static List<DatatypeMapping> GetVespaDataTypesMapping()
        {
            return new List<DatatypeMapping>
    {
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "VespaDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "int", DataSourceName = "VespaDataSource", NetDataType = "System.Int32", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "long", DataSourceName = "VespaDataSource", NetDataType = "System.Int64", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "float", DataSourceName = "VespaDataSource", NetDataType = "System.Single", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "double", DataSourceName = "VespaDataSource", NetDataType = "System.Double", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "bool", DataSourceName = "VespaDataSource", NetDataType = "System.Boolean", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "tensor", DataSourceName = "VespaDataSource", NetDataType = "System.Collections.Generic.List<float>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "byte", DataSourceName = "VespaDataSource", NetDataType = "System.Byte", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "predicate", DataSourceName = "VespaDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "raw", DataSourceName = "VespaDataSource", NetDataType = "System.Byte[]", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "reference", DataSourceName = "VespaDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "array<string>", DataSourceName = "VespaDataSource", NetDataType = "System.Collections.Generic.List<string>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "array<int>", DataSourceName = "VespaDataSource", NetDataType = "System.Collections.Generic.List<int>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "map<string,string>", DataSourceName = "VespaDataSource", NetDataType = "System.Collections.Generic.Dictionary<string, string>", Fav = false }
    };
        }

        /// <summary>Returns a list of datatype mappings for ChromaDB.</summary>
        /// <returns>A list of datatype mappings for ChromaDB.</returns>
        public static List<DatatypeMapping> GetChromaDBDataTypesMapping()
        {
            return new List<DatatypeMapping>
            {
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "String", DataSourceName = "ChromaDBDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Number", DataSourceName = "ChromaDBDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Boolean", DataSourceName = "ChromaDBDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Array", DataSourceName = "ChromaDBDataSource", NetDataType = "System.Collections.Generic.List<object>", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Object", DataSourceName = "ChromaDBDataSource", NetDataType = "System.Collections.Generic.Dictionary<string, object>", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Null", DataSourceName = "ChromaDBDataSource", NetDataType = "System.Object", Fav = false }
            };
        }
    }
}