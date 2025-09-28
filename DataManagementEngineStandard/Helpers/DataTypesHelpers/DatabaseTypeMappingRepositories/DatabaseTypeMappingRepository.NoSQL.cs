using System;
using System.Collections.Generic;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Helpers.DataTypesHelpers
{
    /// <summary>
    /// Repository containing NoSQL database specific type mappings (MongoDB, Redis, Cassandra, etc.).
    /// </summary>
    public static partial class DatabaseTypeMappingRepository
    {
        /// <summary>Returns a list of MongoDB data type mappings.</summary>
        /// <returns>A list of DataTypeMapping objects representing the mappings between .NET data types and MongoDB data types.</returns>
        public static List<DatatypeMapping> GetMongoDBDataTypeMappings()
        {
            return new List<DatatypeMapping>
    {
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "String", DataSourceName = "MongoDBDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "ObjectId", DataSourceName = "MongoDBDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Boolean", DataSourceName = "MongoDBDataSource", NetDataType = "System.Boolean", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Int32", DataSourceName = "MongoDBDataSource", NetDataType = "System.Int32", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Int64", DataSourceName = "MongoDBDataSource", NetDataType = "System.Int64", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Double", DataSourceName = "MongoDBDataSource", NetDataType = "System.Double", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "DateTime", DataSourceName = "MongoDBDataSource", NetDataType = "System.DateTime", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Array", DataSourceName = "MongoDBDataSource", NetDataType = "System.Collections.Generic.List<object>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Binary", DataSourceName = "MongoDBDataSource", NetDataType = "System.Byte[]", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Null", DataSourceName = "MongoDBDataSource", NetDataType = "System.Object", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "RegularExpression", DataSourceName = "MongoDBDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Document", DataSourceName = "MongoDBDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Decimal128", DataSourceName = "MongoDBDataSource", NetDataType = "System.Decimal", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "JavaScript", DataSourceName = "MongoDBDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "JavaScriptWithScope", DataSourceName = "MongoDBDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "MaxKey", DataSourceName = "MongoDBDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "MinKey", DataSourceName = "MongoDBDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Symbol", DataSourceName = "MongoDBDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "Timestamp", DataSourceName = "MongoDBDataSource", NetDataType = "System.DateTime", Fav = false }
    };
        }

        /// <summary>Returns a list of mappings between .NET data types and Cassandra data types.</summary>
        /// <returns>A list of <see cref="DatatypeMapping"/> objects representing the mappings.</returns>
        public static List<DatatypeMapping> GetCassandraDataTypeMappings()
        {
            return new List<DatatypeMapping>
    {
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "ascii", DataSourceName = "CassandraDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "bigint", DataSourceName = "CassandraDataSource", NetDataType = "System.Int64", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "CassandraDataSource", NetDataType = "System.Boolean", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "decimal", DataSourceName = "CassandraDataSource", NetDataType = "System.Decimal", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "double", DataSourceName = "CassandraDataSource", NetDataType = "System.Double", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "float", DataSourceName = "CassandraDataSource", NetDataType = "System.Single", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "int", DataSourceName = "CassandraDataSource", NetDataType = "System.Int32", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "text", DataSourceName = "CassandraDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "timestamp", DataSourceName = "CassandraDataSource", NetDataType = "System.DateTimeOffset", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "uuid", DataSourceName = "CassandraDataSource", NetDataType = "System.Guid", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "varint", DataSourceName = "CassandraDataSource", NetDataType = "System.Numerics.BigInteger", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "list", DataSourceName = "CassandraDataSource", NetDataType = "System.Collections.Generic.List<>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "map", DataSourceName = "CassandraDataSource", NetDataType = "System.Collections.Generic.Dictionary<,>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "set", DataSourceName = "CassandraDataSource", NetDataType = "System.Collections.Generic.HashSet<>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "blob", DataSourceName = "CassandraDataSource", NetDataType = "System.Byte[]", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "counter", DataSourceName = "CassandraDataSource", NetDataType = "System.Int64", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "date", DataSourceName = "CassandraDataSource", NetDataType = "System.DateTime", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "inet", DataSourceName = "CassandraDataSource", NetDataType = "System.Net.IPAddress", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "smallint", DataSourceName = "CassandraDataSource", NetDataType = "System.Int16", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "time", DataSourceName = "CassandraDataSource", NetDataType = "System.TimeSpan", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "tinyint", DataSourceName = "CassandraDataSource", NetDataType = "System.Byte", Fav = false }
    };
        }

        /// <summary>Returns a list of Redis data type mappings.</summary>
        /// <returns>A list of Redis data type mappings.</returns>
        public static List<DatatypeMapping> GetRedisDataTypeMappings()
        {
            return new List<DatatypeMapping>
    {
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "RedisDataSource", NetDataType = "System.String", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "list", DataSourceName = "RedisDataSource", NetDataType = "System.Collections.Generic.List<System.String>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "set", DataSourceName = "RedisDataSource", NetDataType = "System.Collections.Generic.HashSet<System.String>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "sorted set", DataSourceName = "RedisDataSource", NetDataType = "System.Collections.Generic.SortedSet<System.String>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "hash", DataSourceName = "RedisDataSource", NetDataType = "System.Collections.Generic.Dictionary<System.String, System.String>", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "bitmap", DataSourceName = "RedisDataSource", NetDataType = "System.Collections.BitArray", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "hyperloglog", DataSourceName = "RedisDataSource", NetDataType = "System.Object", Fav = false },
        new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "geospatial index", DataSourceName = "RedisDataSource", NetDataType = "System.Object", Fav = false }
    };
        }

        /// <summary>Returns a list of Couchbase data type mappings.</summary>
        /// <returns>A list of Couchbase data type mappings.</returns>
        public static List<DatatypeMapping> GetCouchbaseDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Couchbase N1QL data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "CouchbaseDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "number", DataSourceName = "CouchbaseDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "CouchbaseDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "array", DataSourceName = "CouchbaseDataSource", NetDataType = "System.Collections.Generic.List<object>", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "object", DataSourceName = "CouchbaseDataSource", NetDataType = "System.Collections.Generic.Dictionary<string, object>", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "null", DataSourceName = "CouchbaseDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "missing", DataSourceName = "CouchbaseDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "binary", DataSourceName = "CouchbaseDataSource", NetDataType = "System.Byte[]", Fav = false }
            };
        }
    }
}