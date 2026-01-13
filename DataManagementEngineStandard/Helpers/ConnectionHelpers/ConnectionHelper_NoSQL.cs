using System;
using System.Collections.Generic;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Helpers
{
    /// <summary>
    /// Partial class for NoSQL database connection configurations
    /// </summary>
    public static partial class ConnectionHelper
    {
        /// <summary>
        /// Gets all NoSQL connection configurations
        /// </summary>
        /// <returns>List of NoSQL connection configurations</returns>
        public static List<ConnectionDriversConfig> GetNoSQLConfigs()
        {
            var configs = new List<ConnectionDriversConfig>
            {
                CreateMongoDBConfig(),
                CreateCouchDBConfig(),
                CreateRavenDBConfig(),
                CreateCouchbaseConfig(),
                CreateCouchbaseLiteConfig(),
                CreateRedisConfig(),
                CreateStackExchangeRedisConfig(),
                CreateFirebaseConfig(),
                CreateLiteDBDataSourceConfig(),
                CreateArangoDBConfig(),
                CreateNeo4jConfig(),
                CreateCassandraConfig(),
                CreateOrientDBConfig(),
                CreateElasticsearchConfig(),
                CreateClickHouseConfig(),
                CreateInfluxDBConfig(),
                CreateDynamoDBConfig(),
                CreateRealIMConfig()
            };

            return configs;
        }

        /// <summary>Creates a configuration object for MongoDB connection drivers.</summary>
        /// <returns>A configuration object for MongoDB connection drivers.</returns>
        public static ConnectionDriversConfig CreateMongoDBConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "f0ca152d-a371-4109-a7e9-5fb49b406305",
                PackageName = "MongoDB.Driver",
                DriverClass = "MongoDB.Driver",
                version = "2.10.4.0",
                dllname = "MongoDB.Driver.dll",
                AdapterType = "BsonDocument",
                DbConnectionType = "MongoClient",
                iconname = "mongodb.svg",
                classHandler = "MongoDBDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                ConnectionString = "mongodb://{username}:{password}@{host}:{port}/{database}",
                DatasourceCategory = DatasourceCategory.NOSQL,
                DatasourceType = DataSourceType.MongoDB,
                IsMissing = false,
                NuggetVersion = "2.10.4.0",
                NuggetSource = "MongoDB.Driver",
                NuggetMissing = false
            };
        }

        /// <summary>
        /// Creates a configuration object for connecting to CouchDB.
        /// </summary>
        /// <returns>A configuration object for connecting to CouchDB.</returns>
        public static ConnectionDriversConfig CreateCouchDBConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "couchdb-guid",
                PackageName = "CouchDB",
                DriverClass = "CouchDB",
                version = "3.1.0.0",
                dllname = "CouchDB.dll",
                AdapterType = "CouchDB.CouchDBDataAdapter",
                CommandBuilderType = "CouchDB.CouchDBCommandBuilder",
                DbConnectionType = "CouchDB.CouchDBConnection",
                ConnectionString = "Server={host};Port={port};Database={database};",
                iconname = "couchdb.svg",
                classHandler = "CouchDBDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.NOSQL,
                DatasourceType = DataSourceType.CouchDB,
                IsMissing = false,
                NuggetVersion = "3.1.0.0",
                NuggetSource = "CouchDB",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for RavenDB connection drivers.</summary>
        /// <returns>A configuration object for RavenDB connection drivers.</returns>
        public static ConnectionDriversConfig CreateRavenDBConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "30391117-229f-44d4-bb47-7f6ea1dba2c1",
                PackageName = "Raven.Client",
                DriverClass = "Raven.Client",
                version = "5.0.1.0",
                dllname = "Raven.Client.dll",
                parameter1 = "Database",
                parameter2 = "Sesssion",
                parameter3 = "Collection",
                iconname = "ravendb.svg",
                classHandler = "RavenDBDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = true,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.NOSQL,
                DatasourceType = DataSourceType.RavenDB,
                IsMissing = false,
                NuggetVersion = "5.0.1.0",
                NuggetSource = "Raven.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for connecting to Couchbase.</summary>
        /// <returns>A configuration object for connecting to Couchbase.</returns>
        public static ConnectionDriversConfig CreateCouchbaseConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "couchbase-guid",
                PackageName = "Couchbase",
                DriverClass = "Couchbase",
                version = "3.0.0.0",
                dllname = "Couchbase.dll",
                AdapterType = "Couchbase.CouchbaseDataAdapter",
                CommandBuilderType = "Couchbase.CouchbaseCommandBuilder",
                DbConnectionType = "Couchbase.CouchbaseConnection",
                ConnectionString = "Server={host};Bucket={database};Username={username};Password={password};",
                iconname = "couchbase.svg",
                classHandler = "CouchbaseDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = true,
                DatasourceCategory = DatasourceCategory.NOSQL,
                DatasourceType = DataSourceType.Couchbase,
                IsMissing = false,
                NuggetVersion = "3.0.0.0",
                NuggetSource = "Couchbase",
                NuggetMissing = false
            };
        }

        /// <summary>
        /// Creates a configuration object for connecting to Couchbase Lite.
        /// </summary>
        /// <returns>A configuration object for connecting to Couchbase Lite.</returns>
        public static ConnectionDriversConfig CreateCouchbaseLiteConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "eeade0d2-2372-40c2-bdec-aaab150ecda8",
                PackageName = "Couchbase.Lite",
                DriverClass = "Couchbase.Lite",
                version = "2.7.1.0",
                dllname = "Couchbase.Lite.dll",
                parameter1 = "Database",
                parameter2 = "DatabaseConfiguration",
                iconname = "couchbase.svg",
                classHandler = "CouchBaseLiteDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.NOSQL,
                DatasourceType = DataSourceType.Couchbase,
                IsMissing = false,
                NuggetVersion = "2.7.1.0",
                NuggetSource = "Couchbase.Lite",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Redis connection drivers.</summary>
        /// <returns>A configuration object for Redis connection drivers.</returns>
        public static ConnectionDriversConfig CreateRedisConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "redis-guid",
                PackageName = "StackExchange.Redis",
                DriverClass = "StackExchange.Redis",
                version = "2.2.4.0",
                dllname = "StackExchange.Redis.dll",
                AdapterType = "StackExchange.Redis.RedisDataAdapter",
                DbConnectionType = "StackExchange.Redis.RedisConnection",
                ConnectionString = "Configuration=your-redis-server:port;",
                iconname = "redis.svg",
                classHandler = "RedisDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.NOSQL,
                DatasourceType = DataSourceType.Redis,
                IsMissing = false,
                NuggetVersion = "2.2.4.0",
                NuggetSource = "StackExchange.Redis",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for StackExchange.Redis.</summary>
        /// <returns>A configuration object for StackExchange.Redis.</returns>
        public static ConnectionDriversConfig CreateStackExchangeRedisConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "c90262d9-ffd9-489f-b8b8-aaa39c82beb4",
                PackageName = "StackExchange.Redis",
                DriverClass = "StackExchange.Redis",
                version = "2.0.0.0",
                dllname = "StackExchange.Redis.dll",
                AdapterType = "IDatabase",
                CommandBuilderType = "ISubscriber",
                DbConnectionType = "ConnectionMultiplexer",
                classHandler = "StackExchangeRedisDatasource",
                iconname = "redis.svg",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.NOSQL,
                DatasourceType = DataSourceType.Redis,
                IsMissing = false,
                NuggetVersion = "2.0.0.0",
                NuggetSource = "StackExchange.Redis",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Firebase connection drivers.</summary>
        /// <returns>A configuration object for Firebase connection drivers.</returns>
        public static ConnectionDriversConfig CreateFirebaseConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "firebase-guid",
                PackageName = "Firebase",
                DriverClass = "Firebase",
                version = "7.0.0.0",
                dllname = "Firebase.dll",
                AdapterType = "Firebase.FirebaseDataAdapter",
                CommandBuilderType = "Firebase.FirebaseCommandBuilder",
                DbConnectionType = "Firebase.FirebaseConnection",
                ConnectionString = "ApiKey={apikey};DatabaseURL={url}",
                iconname = "firebase.svg",
                classHandler = "FirebaseDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = true,
                DatasourceCategory = DatasourceCategory.NOSQL,
                DatasourceType = DataSourceType.Firebase,
                IsMissing = false,
                NuggetVersion = "7.0.0.0",
                NuggetSource = "Firebase",
                NuggetMissing = false
            };
        }

        /// <summary>
        /// Creates a configuration object for a LiteDB data source connection driver.
        /// </summary>
        /// <returns>A configuration object for a LiteDB data source connection driver.</returns>
        public static ConnectionDriversConfig CreateLiteDBDataSourceConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "bfcb718d-608f-4164-86ee-24e35c689638",
                PackageName = "LiteDB",
                DriverClass = "LiteDB",
                version = "5.0.21",
                dllname = "LiteDB.dll",
                ConnectionString = "{File}",
                iconname = "litedb.svg",
                classHandler = "LiteDBDataSource",
                ADOType = false,
                CreateLocal = true,
                InMemory = false,
                Favourite = true,
                DatasourceCategory = DatasourceCategory.NOSQL,
                DatasourceType = DataSourceType.LiteDB,
                IsMissing = false,
                NuggetVersion = "5.0.21",
                NuggetSource = "LiteDB",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for ArangoDB connection drivers.</summary>
        /// <returns>A configuration object for ArangoDB connection drivers.</returns>
        public static ConnectionDriversConfig CreateArangoDBConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "arangodb-guid",
                PackageName = "ArangoDB.Client",
                DriverClass = "ArangoDB.Client",
                version = "1.0.0.0",
                dllname = "ArangoDB.Client.dll",
                ConnectionString = "Server={Host};Port={Port};Database={Database};User={UserID};Password={Password};",
                iconname = "arangodb.svg",
                classHandler = "ArangoDBDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.NOSQL,
                DatasourceType = DataSourceType.ArangoDB,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "ArangoDB.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Neo4j connection drivers.</summary>
        /// <returns>A configuration object for Neo4j connection drivers.</returns>
        public static ConnectionDriversConfig CreateNeo4jConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "neo4j-guid",
                PackageName = "Neo4j.Driver",
                DriverClass = "Neo4j.Driver",
                version = "4.4.0.0",
                dllname = "Neo4j.Driver.dll",
                ConnectionString = "bolt://{Host}:{Port};user={UserID};password={Password};",
                iconname = "neo4j.svg",
                classHandler = "Neo4jDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.GraphDB,
                DatasourceType = DataSourceType.Neo4j,
                IsMissing = false,
                NuggetVersion = "4.4.0.0",
                NuggetSource = "Neo4j.Driver",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Cassandra connection drivers.</summary>
        /// <returns>A configuration object for Cassandra connection drivers.</returns>
        public static ConnectionDriversConfig CreateCassandraConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "47f2ed83-066c-41fd-96f6-1ba4d188c3b6",
                PackageName = "Cassandra",
                DriverClass = "Cassandra",
                version = "3.99.0.0",
                dllname = "Cassandra.dll",
                AdapterType = "Cassandra.Data.CqlDataAdapter",
                DbConnectionType = "Cassandra.Data.CqlConnection",
                DbTransactionType = "Cassandra.Data.CqlBatchTransaction",
                iconname = "Cassandra.svg",
                classHandler = "CassandraDataSource",
                ADOType = true,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.NOSQL,
                DatasourceType = DataSourceType.Cassandra,
                IsMissing = false,
                NuggetVersion = "3.99.0.0",
                NuggetSource = "Cassandra",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for OrientDB connection drivers.</summary>
        /// <returns>A configuration object for OrientDB connection drivers.</returns>
        public static ConnectionDriversConfig CreateOrientDBConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "orientdb-guid",
                PackageName = "OrientDB.Net",
                DriverClass = "OrientDB.Net",
                version = "1.0.0.0",
                dllname = "OrientDB.Net.dll",
                ConnectionString = "Server={Host};Port={Port};Database={Database};User={UserID};Password={Password};",
                iconname = "orientdb.svg",
                classHandler = "OrientDBDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.NOSQL,
                DatasourceType = DataSourceType.OrientDB,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "OrientDB.Net",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Elasticsearch connection drivers.</summary>
        /// <returns>A configuration object for Elasticsearch connection drivers.</returns>
        public static ConnectionDriversConfig CreateElasticsearchConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "f9b81106-c01c-4d10-9f08-735088af953d",
                PackageName = "Elasticsearch.Net",
                DriverClass = "Elasticsearch.Net",
                version = "7.0.0.0",
                dllname = "Elasticsearch.Net.dll",
                parameter1 = "ElasticLowLevelClient",
                parameter2 = "ConnectionConfiguration",
                classHandler = "ElasticsearchDatasource",
                iconname = "elasticsearch.svg",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.SearchEngine,
                DatasourceType = DataSourceType.ElasticSearch,
                IsMissing = false,
                NuggetVersion = "7.0.0.0",
                NuggetSource = "Elasticsearch.Net",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for ClickHouse connection drivers.</summary>
        /// <returns>A configuration object for ClickHouse connection drivers.</returns>
        public static ConnectionDriversConfig CreateClickHouseConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "clickhouse-guid",
                PackageName = "ClickHouse.Client",
                DriverClass = "ClickHouse.Client",
                version = "1.0.0.0",
                dllname = "ClickHouse.Client.dll",
                ConnectionString = "Host={Host};Port={Port};Database={Database};Username={UserID};Password={Password};",
                iconname = "clickhouse.svg",
                classHandler = "ClickHouseDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.ColumnarDB,
                DatasourceType = DataSourceType.ClickHouse,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "ClickHouse.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for InfluxDB connection drivers.</summary>
        /// <returns>A configuration object for InfluxDB connection drivers.</returns>
        public static ConnectionDriversConfig CreateInfluxDBConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "influxdb-guid",
                PackageName = "InfluxDB.Client",
                DriverClass = "InfluxDB.Client",
                version = "4.12.0.0",
                dllname = "InfluxDB.Client.dll",
                ConnectionString = "Url={Host}:{Port};Token={Password};Org={Org};Bucket={Database};",
                iconname = "influxdb.svg",
                classHandler = "InfluxDBDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.TimeSeriesDB,
                DatasourceType = DataSourceType.InfluxDB,
                IsMissing = false,
                NuggetVersion = "4.12.0.0",
                NuggetSource = "InfluxDB.Client",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for DynamoDB connection drivers.</summary>
        /// <returns>A configuration object for DynamoDB connection drivers.</returns>
        public static ConnectionDriversConfig CreateDynamoDBConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "dynamodb-guid",
                PackageName = "AWSSDK.DynamoDBv2",
                DriverClass = "Amazon.DynamoDBv2",
                version = "3.7.0.0",
                dllname = "AWSSDK.DynamoDBv2.dll",
                ConnectionString = "Region={Region};AccessKey={UserID};SecretKey={Password};",
                iconname = "dynamodb.svg",
                classHandler = "DynamoDBDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.NOSQL,
                DatasourceType = DataSourceType.DynamoDB,
                IsMissing = false,
                NuggetVersion = "3.7.0.0",
                NuggetSource = "AWSSDK.DynamoDBv2",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for connecting to a Realm database.</summary>
        /// <returns>A configuration object for connecting to a Realm database.</returns>
        public static ConnectionDriversConfig CreateRealIMConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "realm-guid",
                PackageName = "Realm",
                DriverClass = "Realm",
                version = "5.0.0.0",
                dllname = "Realm.dll",
                AdapterType = "Realm.RealmDataAdapter",
                CommandBuilderType = "Realm.RealmCommandBuilder",
                DbConnectionType = "Realm.RealmConnection",
                ConnectionString = "Path={file}",
                iconname = "realm.svg",
                classHandler = "RealMDataSource",
                ADOType = false,
                CreateLocal = true,
                InMemory = true,
                Favourite = true,
                DatasourceCategory = DatasourceCategory.INMEMORY,
                DatasourceType = DataSourceType.RealIM,
                IsMissing = false,
                NuggetVersion = "5.0.0.0",
                NuggetSource = "Realm",
                NuggetMissing = false
            };
        }
    }
}