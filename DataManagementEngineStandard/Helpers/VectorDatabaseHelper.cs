using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Helpers
{
    public static class VectorDatabaseHelper
    {
        #region "Vector Database Helpers"
        /// <summary>
        /// Generates vector database-specific query commands based on database type
        /// </summary>
        /// <param name="dataSourceType">Vector database type</param>
        /// <param name="operationType">Operation to perform (search, insert, delete)</param>
        /// <param name="parameters">Dictionary of parameters specific to the operation</param>
        /// <returns>A command string for the specified vector database operation</returns>
        public static string GenerateVectorDatabaseCommand(DataSourceType dataSourceType, VectorDatabaseOperation operationType, Dictionary<string, object> parameters)
        {
            // Validate inputs
            if (!IsVectorDatabase(dataSourceType))
                return $"Error: {dataSourceType} is not a vector database";

            if (parameters == null)
                parameters = new Dictionary<string, object>();

            string command = "";

            switch (dataSourceType)
            {
                case DataSourceType.ChromaDB:
                    command = GenerateChromaDBCommand(operationType, parameters);
                    break;

                case DataSourceType.PineCone:
                    command = GeneratePineConeCommand(operationType, parameters);
                    break;

                case DataSourceType.Qdrant:
                    command = GenerateQdrantCommand(operationType, parameters);
                    break;

                case DataSourceType.ShapVector:
                    command = GenerateShapVectorCommand(operationType, parameters);
                    break;

                case DataSourceType.Weaviate:
                    command = GenerateWeaviateCommand(operationType, parameters);
                    break;

                case DataSourceType.Milvus:
                    command = GenerateMilvusCommand(operationType, parameters);
                    break;

                case DataSourceType.RedisVector:
                    command = GenerateRedisVectorCommand(operationType, parameters);
                    break;

                case DataSourceType.Zilliz:
                    command = GenerateZillizCommand(operationType, parameters);
                    break;

                case DataSourceType.Vespa:
                    command = GenerateVespaCommand(operationType, parameters);
                    break;

               

                default:
                    command = $"Unsupported vector database: {dataSourceType}";
                    break;
            }

            return command;
        }

        /// <summary>
        /// Checks if a given data source type is a vector database
        /// </summary>
        /// <param name="dataSourceType">The data source type to check</param>
        /// <returns>True if the data source is a vector database, false otherwise</returns>
        public static bool IsVectorDatabase(DataSourceType dataSourceType)
        {
            return dataSourceType == DataSourceType.ChromaDB ||
                   dataSourceType == DataSourceType.PineCone ||
                   dataSourceType == DataSourceType.Qdrant ||
                   dataSourceType == DataSourceType.ShapVector ||
                   dataSourceType == DataSourceType.Weaviate ||
                   dataSourceType == DataSourceType.Milvus ||
                   dataSourceType == DataSourceType.RedisVector ||
                   dataSourceType == DataSourceType.Zilliz ||
                   dataSourceType == DataSourceType.Vespa;
        }

        /// <summary>
        /// Generates metadata queries for vector databases to list collections or schemas
        /// </summary>
        /// <param name="dataSourceType">Vector database type</param>
        /// <returns>A query string to list collections/tables in the vector database</returns>
        public static string GetVectorDatabaseCollections(DataSourceType dataSourceType)
        {
            switch (dataSourceType)
            {
                case DataSourceType.ChromaDB:
                    return "GET /api/v1/collections";

                case DataSourceType.PineCone:
                    return "list_collections()";

                case DataSourceType.Qdrant:
                    return "GET /collections";

                case DataSourceType.ShapVector:
                    return "SHOW COLLECTIONS";

                case DataSourceType.Weaviate:
                    return "GET /v1/schema";

                case DataSourceType.Milvus:
                    return "SHOW COLLECTIONS";

                case DataSourceType.RedisVector:
                    return "FT._LIST";

                case DataSourceType.Zilliz:
                    return "SHOW COLLECTIONS";

                case DataSourceType.Vespa:
                    return "GET /document/v1/";


                default:
                    return $"Unsupported vector database: {dataSourceType}";
            }
        }

        /// <summary>
        /// Generates a vector similarity search query for the specified database
        /// </summary>
        /// <param name="dataSourceType">Vector database type</param>
        /// <param name="collectionName">Name of the collection to search</param>
        /// <param name="vectorDimension">Dimension of the vector</param>
        /// <param name="topK">Number of results to return</param>
        /// <returns>A similarity search query template for the specified vector database</returns>
        public static string GenerateVectorSimilaritySearchQuery(DataSourceType dataSourceType, string collectionName, int vectorDimension, int topK = 10)
        {
            string query = "";

            switch (dataSourceType)
            {
                case DataSourceType.ChromaDB:
                    query = $"{{\"collection_name\": \"{collectionName}\", \"query_embeddings\": [VECTOR_PLACEHOLDER], \"n_results\": {topK}}}";
                    break;

                case DataSourceType.PineCone:
                    query = $"{{\"namespace\": \"{collectionName}\", \"vector\": [VECTOR_PLACEHOLDER], \"topK\": {topK}, \"includeMetadata\": true}}";
                    break;

                case DataSourceType.Qdrant:
                    query = $"{{\"collection_name\": \"{collectionName}\", \"vector\": [VECTOR_PLACEHOLDER], \"limit\": {topK}}}";
                    break;

                case DataSourceType.ShapVector:
                    query = $"SEARCH {collectionName} USING VECTOR([VECTOR_PLACEHOLDER]) LIMIT {topK}";
                    break;

                case DataSourceType.Weaviate:
                    query = $"{{\"query\": {{\"Get\": {{\"{collectionName}\": {{\"nearVector\": {{\"vector\": [VECTOR_PLACEHOLDER], \"limit\": {topK}}}}}}}}}}}";
                    break;

                case DataSourceType.Milvus:
                    query = $"search {collectionName} -d {vectorDimension} -k {topK} -v [VECTOR_PLACEHOLDER]";
                    break;

                case DataSourceType.RedisVector:
                    query = $"FT.SEARCH {collectionName} \"*=>[KNN {topK} @vector $vec AS score]\" PARAMS 2 vec [VECTOR_PLACEHOLDER] SORTBY score RETURN 3 id vector score";
                    break;

                case DataSourceType.Zilliz:
                    query = $"search {collectionName} -d {vectorDimension} -k {topK} -v [VECTOR_PLACEHOLDER]";
                    break;

                case DataSourceType.Vespa:
                    query = $"{{\"yql\": \"select * from {collectionName} where nearestNeighbor(embedding, vector_field)\", \"input.query(vector_field)\": [VECTOR_PLACEHOLDER], \"hits\": {topK}}}";
                    break;


                default:
                    query = $"Unsupported vector database: {dataSourceType}";
                    break;
            }

            return query;
        }

        // Individual vector database command generators
        private static string GenerateChromaDBCommand(VectorDatabaseOperation operationType, Dictionary<string, object> parameters)
        {
            string collection = parameters.ContainsKey("collection") ? parameters["collection"].ToString() : "default";

            switch (operationType)
            {
                case VectorDatabaseOperation.CreateCollection:
                    return $"{{\"name\": \"{collection}\"}}";

                case VectorDatabaseOperation.DeleteCollection:
                    return $"DELETE /api/v1/collections/{collection}";

                case VectorDatabaseOperation.AddVectors:
                    return $"{{\"collection_name\": \"{collection}\", \"embeddings\": [EMBEDDINGS], \"metadatas\": [METADATAS], \"documents\": [DOCUMENTS], \"ids\": [IDS]}}";

                case VectorDatabaseOperation.QueryVectors:
                    int topK = parameters.ContainsKey("topK") ? Convert.ToInt32(parameters["topK"]) : 10;
                    return $"{{\"collection_name\": \"{collection}\", \"query_embeddings\": [QUERY_VECTOR], \"n_results\": {topK}}}";

                case VectorDatabaseOperation.DeleteVectors:
                    return $"{{\"collection_name\": \"{collection}\", \"ids\": [IDS]}}";

                default:
                    return $"Unsupported operation: {operationType} for ChromaDB";
            }
        }

        private static string GeneratePineConeCommand(VectorDatabaseOperation operationType, Dictionary<string, object> parameters)
        {
            string indexName = parameters.ContainsKey("indexName") ? parameters["indexName"].ToString() : "default";

            switch (operationType)
            {
                case VectorDatabaseOperation.CreateCollection:
                    int dimension = parameters.ContainsKey("dimension") ? Convert.ToInt32(parameters["dimension"]) : 1536;
                    string metric = parameters.ContainsKey("metric") ? parameters["metric"].ToString() : "cosine";
                    return $"{{\"name\": \"{indexName}\", \"dimension\": {dimension}, \"metric\": \"{metric}\"}}";

                case VectorDatabaseOperation.DeleteCollection:
                    return $"delete_index(\"{indexName}\")";

                case VectorDatabaseOperation.AddVectors:
                    return $"{{\"index_name\": \"{indexName}\", \"vectors\": [VECTORS]}}";

                case VectorDatabaseOperation.QueryVectors:
                    int topK = parameters.ContainsKey("topK") ? Convert.ToInt32(parameters["topK"]) : 10;
                    return $"{{\"index_name\": \"{indexName}\", \"vector\": [QUERY_VECTOR], \"top_k\": {topK}, \"include_values\": true, \"include_metadata\": true}}";

                case VectorDatabaseOperation.DeleteVectors:
                    return $"{{\"index_name\": \"{indexName}\", \"ids\": [IDS]}}";

                default:
                    return $"Unsupported operation: {operationType} for PineCone";
            }
        }

        // Similar implementation for other vector databases...

        // Add more specific methods for each vector database as needed
        private static string GenerateQdrantCommand(VectorDatabaseOperation operationType, Dictionary<string, object> parameters)
        {
            string collection = parameters.ContainsKey("collection") ? parameters["collection"].ToString() : "default";

            switch (operationType)
            {
                case VectorDatabaseOperation.CreateCollection:
                    int dimension = parameters.ContainsKey("dimension") ? Convert.ToInt32(parameters["dimension"]) : 1536;
                    string distance = parameters.ContainsKey("distance") ? parameters["distance"].ToString() : "Cosine";
                    return $"{{\"name\": \"{collection}\", \"vector_size\": {dimension}, \"distance\": \"{distance}\"}}";

                case VectorDatabaseOperation.AddVectors:
                    return $"{{\"collection_name\": \"{collection}\", \"points\": [POINTS]}}";

                case VectorDatabaseOperation.QueryVectors:
                    int topK = parameters.ContainsKey("topK") ? Convert.ToInt32(parameters["topK"]) : 10;
                    return $"{{\"collection_name\": \"{collection}\", \"vector\": [QUERY_VECTOR], \"limit\": {topK}}}";

                default:
                    return $"Unsupported operation: {operationType} for Qdrant";
            }
        }

        private static string GenerateWeaviateCommand(VectorDatabaseOperation operationType, Dictionary<string, object> parameters)
        {
            // Implementation for Weaviate
            return "Weaviate commands not yet implemented";
        }

        private static string GenerateMilvusCommand(VectorDatabaseOperation operationType, Dictionary<string, object> parameters)
        {
            // Implementation for Milvus
            return "Milvus commands not yet implemented";
        }

        private static string GenerateRedisVectorCommand(VectorDatabaseOperation operationType, Dictionary<string, object> parameters)
        {
            // Implementation for RedisVector
            return "RedisVector commands not yet implemented";
        }

        private static string GenerateZillizCommand(VectorDatabaseOperation operationType, Dictionary<string, object> parameters)
        {
            // Implementation for Zilliz
            return "Zilliz commands not yet implemented";
        }

        private static string GenerateVespaCommand(VectorDatabaseOperation operationType, Dictionary<string, object> parameters)
        {
            // Implementation for Vespa
            return "Vespa commands not yet implemented";
        }

        private static string GenerateShapVectorCommand(VectorDatabaseOperation operationType, Dictionary<string, object> parameters)
        {
            // Implementation for ShapVector
            return "ShapVector commands not yet implemented";
        }

        private static string GenerateMyVectorDBCommand(VectorDatabaseOperation operationType, Dictionary<string, object> parameters) // NEW
        {
            string collection = parameters.ContainsKey("collection") ? parameters["collection"].ToString() : "default";

            switch (operationType)
            {
                case VectorDatabaseOperation.CreateCollection:
                    return $"CREATE COLLECTION {collection}";

                case VectorDatabaseOperation.DeleteCollection:
                    return $"DROP COLLECTION {collection}";

                case VectorDatabaseOperation.AddVectors:
                    return $"INSERT INTO {collection} VALUES ([VECTORS])";

                case VectorDatabaseOperation.QueryVectors:
                    int topK = parameters.ContainsKey("topK") ? Convert.ToInt32(parameters["topK"]) : 10;
                    return $"SELECT * FROM {collection} ORDER BY SIMILARITY([QUERY_VECTOR]) DESC LIMIT {topK}";

                default:
                    return $"Unsupported operation: {operationType} for MyVectorDB";
            }
        }

        /// <summary>
        /// Creates a list of QuerySqlRepo objects for vector databases.
        /// </summary>
        /// <returns>A list of QuerySqlRepo objects for vector databases.</returns>
        public static List<QuerySqlRepo> CreateVectorDatabaseQueries()
        {
            var repos = new List<QuerySqlRepo>();

            // ChromaDB
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.ChromaDB, "GET /api/v1/collections/{0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.ChromaDB, "GET /api/v1/collections", Sqlcommandtype.getlistoftables)
    });

            // PineCone
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.PineCone, "list_vectors({\"namespace\":\"{0}\", \"limit\":100})", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.PineCone, "list_indexes()", Sqlcommandtype.getlistoftables)
    });

            // Qdrant
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.Qdrant, "GET /collections/{0}/points?limit=100", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.Qdrant, "GET /collections", Sqlcommandtype.getlistoftables)
    });

            // ShapVector
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.ShapVector, "SELECT * FROM {0} LIMIT 100", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.ShapVector, "SHOW COLLECTIONS", Sqlcommandtype.getlistoftables)
    });

            // Weaviate
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.Weaviate, "{{\"query\":\"{{Get{{{0}(limit:100){{_additional{{id}} }}}}}}\"}}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.Weaviate, "GET /v1/schema", Sqlcommandtype.getlistoftables)
    });

            // Milvus
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.Milvus, "SELECT * FROM {0} LIMIT 100", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.Milvus, "SHOW COLLECTIONS", Sqlcommandtype.getlistoftables)
    });

            // RedisVector
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.RedisVector, "FT.SEARCH {0} \"*\" LIMIT 0 100", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.RedisVector, "FT._LIST", Sqlcommandtype.getlistoftables)
    });

            // Zilliz
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.Zilliz, "SELECT * FROM {0} LIMIT 100", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.Zilliz, "SHOW COLLECTIONS", Sqlcommandtype.getlistoftables)
    });

            // Vespa
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.Vespa, "{{\"yql\":\"select * from {0} where true limit 100\"}}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.Vespa, "GET /document/v1", Sqlcommandtype.getlistoftables)
    });

   
            return repos;
        }

        /// <summary>
        /// Operations that can be performed on vector databases
        /// </summary>
        public enum VectorDatabaseOperation
        {
            CreateCollection,
            DeleteCollection,
            AddVectors,
            QueryVectors,
            DeleteVectors,
            UpdateVectors,
            GetVectorById,
            ListCollections,
            SimilaritySearch,
            MetadataFilter,
            Upsert
        }

        /// <summary>
        /// Validates parameters for vector database operations
        /// </summary>
        /// <param name="dataSourceType">Vector database type</param>
        /// <param name="operation">Operation to perform</param>
        /// <param name="parameters">Parameters for the operation</param>
        /// <returns>Tuple with validation result and error message</returns>
        public static (bool isValid, string errorMessage) ValidateVectorDatabaseParameters(
            DataSourceType dataSourceType,
            VectorDatabaseOperation operation,
            Dictionary<string, object> parameters)
        {
            if (!IsVectorDatabase(dataSourceType))
            {
                return (false, $"{dataSourceType} is not a vector database");
            }

            if (parameters == null)
            {
                return (false, "Parameters cannot be null");
            }

            // Common validations based on operation type
            switch (operation)
            {
                case VectorDatabaseOperation.CreateCollection:
                    if (!parameters.ContainsKey("collection") && !parameters.ContainsKey("indexName"))
                    {
                        return (false, "Collection or index name must be specified for CreateCollection operation");
                    }
                    break;

                case VectorDatabaseOperation.AddVectors:
                    if (!parameters.ContainsKey("vectors") && !parameters.ContainsKey("embeddings"))
                    {
                        return (false, "Vectors or embeddings must be provided for AddVectors operation");
                    }
                    break;

                case VectorDatabaseOperation.SimilaritySearch:
                    if (!parameters.ContainsKey("queryVector"))
                    {
                        return (false, "Query vector must be provided for SimilaritySearch operation");
                    }
                    break;
            }

            // Database-specific validations
            switch (dataSourceType)
            {
                case DataSourceType.ChromaDB:
                    return ValidateChromaDBParameters(operation, parameters);

                case DataSourceType.PineCone:
                    return ValidatePineConeParameters(operation, parameters);

             

                // Add other vector database validations as needed

                default:
                    return (true, string.Empty); // Default to valid if no specific validation
            }
        }

        private static (bool isValid, string errorMessage) ValidateChromaDBParameters(
            VectorDatabaseOperation operation,
            Dictionary<string, object> parameters)
        {
            switch (operation)
            {
                case VectorDatabaseOperation.AddVectors:
                    if (!parameters.ContainsKey("embeddings") ||
                        !parameters.ContainsKey("ids"))
                    {
                        return (false, "ChromaDB requires 'embeddings' and 'ids' for AddVectors operation");
                    }
                    break;

                case VectorDatabaseOperation.QueryVectors:
                    if (!parameters.ContainsKey("queryEmbeddings") &&
                        !parameters.ContainsKey("queryTexts"))
                    {
                        return (false, "ChromaDB requires either 'queryEmbeddings' or 'queryTexts' for QueryVectors operation");
                    }
                    break;
            }

            return (true, string.Empty);
        }

        private static (bool isValid, string errorMessage) ValidatePineConeParameters(
            VectorDatabaseOperation operation,
            Dictionary<string, object> parameters)
        {
            switch (operation)
            {
                case VectorDatabaseOperation.CreateCollection:
                    if (!parameters.ContainsKey("dimension"))
                    {
                        return (false, "PineCone requires 'dimension' parameter for CreateCollection operation");
                    }
                    break;

                case VectorDatabaseOperation.AddVectors:
                    if (!parameters.ContainsKey("vectors"))
                    {
                        return (false, "PineCone requires 'vectors' parameter for AddVectors operation");
                    }
                    break;
            }

            return (true, string.Empty);
        }
        #endregion "Vector Database Helpers"

    }
}
