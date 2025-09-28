using System;
using System.Collections.Generic;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Helpers
{
    /// <summary>
    /// Partial class for Vector Database connection configurations
    /// </summary>
    public static partial class ConnectionHelper
    {
        /// <summary>
        /// Gets all Vector Database connection configurations
        /// </summary>
        /// <returns>List of Vector Database connection configurations</returns>
        public static List<ConnectionDriversConfig> GetVectorDBConfigs()
        {
            var configs = new List<ConnectionDriversConfig>
            {
                CreateChromaDBConfig(),
                CreatePineConeConfig(),
                CreateQdrantConfig(),
                CreateShapVectorConfig(),
                CreateWeaviateConfig(),
                CreateMilvusConfig(),
                CreateRedisVectorConfig(),
                CreateZillizConfig(),
                CreateVespaConfig()
            };

            return configs;
        }

        /// <summary>Creates a configuration object for ChromaDB connection drivers.</summary>
        /// <returns>A configuration object for ChromaDB connection drivers.</returns>
        public static ConnectionDriversConfig CreateChromaDBConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "ChromaDB",
                DriverClass = "ChromaDB",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.ChromaDBDatasource.dll",
                AdapterType = "ChromaDBAdapter",
                DbConnectionType = "ChromaDBClient",
                ConnectionString = "Server={Host};Port={Port};Database={Database};APIKey={Password}",
                iconname = "chromadb.svg",
                classHandler = "ChromaDBDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.VectorDB,
                DatasourceType = DataSourceType.ChromaDB,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for PineCone connection drivers.</summary>
        /// <returns>A configuration object for PineCone connection drivers.</returns>
        public static ConnectionDriversConfig CreatePineConeConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "PineCone",
                DriverClass = "PineCone",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.PineConeDatasource.dll",
                AdapterType = "PineConeAdapter",
                DbConnectionType = "PineConeClient",
                ConnectionString = "ApiKey={Password};Environment={Host};ProjectName={Database}",
                iconname = "pinecone.svg",
                classHandler = "PineConeDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.VectorDB,
                DatasourceType = DataSourceType.PineCone,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for Qdrant connection drivers.</summary>
        /// <returns>A configuration object for Qdrant connection drivers.</returns>
        public static ConnectionDriversConfig CreateQdrantConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Qdrant",
                DriverClass = "Qdrant",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.QdrantDatasource.dll",
                AdapterType = "QdrantAdapter",
                DbConnectionType = "QdrantClient",
                ConnectionString = "Url={Url};ApiKey={Password}",
                iconname = "qdrant.svg",
                classHandler = "QdrantDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.VectorDB,
                DatasourceType = DataSourceType.Qdrant,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for ShapVector connection drivers.</summary>
        /// <returns>A configuration object for ShapVector connection drivers.</returns>
        public static ConnectionDriversConfig CreateShapVectorConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "ShapVector",
                DriverClass = "ShapVector",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.ShapVectorDatasource.dll",
                AdapterType = "ShapVectorAdapter",
                DbConnectionType = "ShapVectorClient",
                ConnectionString = "Url={Url};ApiKey={Password}",
                iconname = "shapvector.svg",
                classHandler = "ShapVectorDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.VectorDB,
                DatasourceType = DataSourceType.ShapVector,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for Weaviate connection drivers.</summary>
        /// <returns>A configuration object for Weaviate connection drivers.</returns>
        public static ConnectionDriversConfig CreateWeaviateConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Weaviate",
                DriverClass = "Weaviate",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.WeaviateDatasource.dll",
                AdapterType = "WeaviateAdapter",
                DbConnectionType = "WeaviateClient",
                ConnectionString = "Url={Url};ApiKey={Password}",
                iconname = "weaviate.svg",
                classHandler = "WeaviateDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.VectorDB,
                DatasourceType = DataSourceType.Weaviate,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for Milvus connection drivers.</summary>
        /// <returns>A configuration object for Milvus connection drivers.</returns>
        public static ConnectionDriversConfig CreateMilvusConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Milvus",
                DriverClass = "Milvus",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.MilvusDatasource.dll",
                AdapterType = "MilvusAdapter",
                DbConnectionType = "MilvusClient",
                ConnectionString = "Host={Host};Port={Port};Username={UserID};Password={Password}",
                iconname = "milvus.svg",
                classHandler = "MilvusDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.VectorDB,
                DatasourceType = DataSourceType.Milvus,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for RedisVector connection drivers.</summary>
        /// <returns>A configuration object for RedisVector connection drivers.</returns>
        public static ConnectionDriversConfig CreateRedisVectorConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "RedisVector",
                DriverClass = "RedisVector",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.RedisVectorDatasource.dll",
                AdapterType = "RedisVectorAdapter",
                DbConnectionType = "RedisVectorClient",
                ConnectionString = "Host={Host};Port={Port};Password={Password};Database={Database}",
                iconname = "redisvector.svg",
                classHandler = "RedisVectorDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.VectorDB,
                DatasourceType = DataSourceType.RedisVector,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for Zilliz connection drivers.</summary>
        /// <returns>A configuration object for Zilliz connection drivers.</returns>
        public static ConnectionDriversConfig CreateZillizConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Zilliz",
                DriverClass = "Zilliz",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.ZillizDatasource.dll",
                AdapterType = "ZillizAdapter",
                DbConnectionType = "ZillizClient",
                ConnectionString = "Url={Url};ApiKey={Password};Collection={Database}",
                iconname = "zilliz.svg",
                classHandler = "ZillizDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.VectorDB,
                DatasourceType = DataSourceType.Zilliz,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for Vespa connection drivers.</summary>
        /// <returns>A configuration object for Vespa connection drivers.</returns>
        public static ConnectionDriversConfig CreateVespaConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Vespa",
                DriverClass = "Vespa",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.VespaDatasource.dll",
                AdapterType = "VespaAdapter",
                DbConnectionType = "VespaClient",
                ConnectionString = "Endpoint={Host};Port={Port};ApplicationName={Database}",
                iconname = "vespa.svg",
                classHandler = "VespaDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.VectorDB,
                DatasourceType = DataSourceType.Vespa,
                IsMissing = false
            };
        }
    }
}