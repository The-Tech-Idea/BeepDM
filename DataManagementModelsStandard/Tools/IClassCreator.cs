using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Tools
{
    public interface IClassCreator
    {
        IDMEEditor DMEEditor { get; set; }
        string outputFileName { get; set; }
        string outputpath { get; set; }

        void CompileClassFromText(string SourceString, string output);
        public EntityStructure ConvertEntityTypeToEntityStructure(Type EntityType,
            KeyDetectionStrategy strategy = KeyDetectionStrategy.AttributeThenConvention,
            string entityName =null, string datasourceName=null);
        Assembly CreateAssemblyFromCode(string code);
        string CreateClass(string classname, List<EntityField> flds, string poutputpath, string NameSpacestring = "TheTechIdea.ProjectClasses", bool GenerateCSharpCodeFiles = true);
        string CreateClass(string classname, EntityStructure entity, string poutputpath, string NameSpacestring = "TheTechIdea.ProjectClasses", bool GenerateCSharpCodeFiles = true);
        string CreateClassFromTemplate(string classname, EntityStructure entity, string template, string usingheader, string implementations, string extracode, string outputpath, string nameSpacestring = "TheTechIdea.ProjectClasses", bool GenerateCSharpCodeFiles = true);
        string CreateDLL(string dllname, List<EntityStructure> entities, string outputpath, IProgress<PassedArgs> progress, CancellationToken token, string NameSpacestring = "TheTechIdea.ProjectClasses", bool GenerateCSharpCodeFiles = true);
        string CreateDLLFromFilesPath(string dllname, string filespath, string outputpath, IProgress<PassedArgs> progress, CancellationToken token, string NameSpacestring = "TheTechIdea.ProjectClasses");
        string CreateINotifyClass(EntityStructure entity, string usingheader, string implementations, string extracode, string outputpath, string nameSpacestring = "TheTechIdea.ProjectClasses", bool GenerateCSharpCodeFiles = true);
        string CreateEntityClass(EntityStructure entity, string usingheader, string extracode, string outputpath, string nameSpacestring = "TheTechIdea.ProjectClasses", bool GenerateCSharpCodeFiles = true);
        string CreatePOCOClass(string classname, EntityStructure entity, string usingheader, string implementations, string extracode, string outputpath, string nameSpacestring = "TheTechIdea.ProjectClasses", bool GenerateCSharpCodeFiles = true);
        string CreatePOCOClass(string datasourcename, string classname, List<string> entities, string usingheader, string implementations, string extracode, string outputpath, string nameSpacestring = "TheTechIdea.ProjectClasses", bool generateCSharpCodeFiles = true);
        string CreateINotifyClass(string datasourcename, List<string> entities, string usingheader, string implementations, string extracode, string outputpath, string nameSpacestring = "TheTechIdea.ProjectClasses", bool generateCSharpCodeFiles = true);
        string CreateEntityClass(string datasourcename, List<string> entities, string usingHeader, string extraCode, string outputPath, string namespaceString = "TheTechIdea.ProjectClasses", bool generateFiles = true);
        string CreatePOCOClass(string classname, List<EntityStructure> entities, string usingheader, string implementations, string extracode, string outputpath, string nameSpacestring = "TheTechIdea.ProjectClasses", bool generateCSharpCodeFiles = true);
        string CreateINotifyClass(List<EntityStructure> entities, string usingheader, string implementations, string extracode, string outputpath, string nameSpacestring = "TheTechIdea.ProjectClasses", bool generateCSharpCodeFiles = true);
        string CreateEntityClass(List<EntityStructure> entities, string usingHeader, string extraCode, string outputPath, string namespaceString = "TheTechIdea.ProjectClasses", bool generateFiles = true);
        Type CreateTypeFromCode(string code, string outputTypeName);
        void GenerateCSharpCode(string fileName);
        string GenerateMinimalWebApi(string outputPath, string namespaceName = "TheTechIdea.ProjectMinimalAPI");
        List<string> GenerateWebApiControllers(string dataSourceName, List<EntityStructure> entities, string outputPath, string namespaceName = "TheTechIdea.ProjectControllers");
        string GenerateWebApiControllerForEntityWithParams(
            string className,
            string outputPath,
            string namespaceName = "TheTechIdea.ProjectControllers");
        List<string> ValidateEntityStructure(EntityStructure entity);
        string GenerateDataAccessLayer(EntityStructure entity, string outputPath);
        string GenerateUnitTestClass(EntityStructure entity, string outputPath);
        string GenerateDbContext(List<EntityStructure> entities, string namespaceString, string outputPath);
        string GenerateEntityConfiguration(EntityStructure entity, string namespaceString, string outputPath);
        /// <summary>
        /// Generates a class with C# record type for immutable data models
        /// </summary>
        /// <param name="recordName">Name of the record to create</param>
        /// <param name="entity">Entity structure to base the record on</param>
        /// <param name="outputPath">Output file path</param>
        /// <param name="namespaceName">Namespace to use</param>
        /// <param name="generateFile">Whether to generate physical file</param>
        /// <returns>The generated code as string</returns>
        string CreateRecordClass(string recordName, EntityStructure entity, string outputPath,
                                  string namespaceName = "TheTechIdea.ProjectClasses",
                                  bool generateFile = true);

        /// <summary>
        /// Creates a class with support for nullable reference types
        /// </summary>
        /// <param name="className">Name of the class</param>
        /// <param name="entity">Entity structure</param>
        /// <param name="outputPath">Output path</param>
        /// <param name="namespaceName">Namespace name</param>
        /// <param name="generateNullableAnnotations">Whether to add nullable annotations</param>
        /// <returns>The generated code</returns>
        string CreateNullableAwareClass(string className, EntityStructure entity, string outputPath,
                                        string namespaceName = "TheTechIdea.ProjectClasses",
                                        bool generateNullableAnnotations = true);
        /// <summary>
        /// Creates a domain-driven design style aggregate root class from entity
        /// </summary>
        /// <param name="entity">The entity structure</param>
        /// <param name="outputPath">Output path</param>
        /// <param name="namespaceName">Namespace</param>
        /// <returns>Generated code as string</returns>
        string CreateDDDAggregateRoot(EntityStructure entity, string outputPath,
                                     string namespaceName = "TheTechIdea.ProjectDomain");

        /// <summary>
        /// Generates GraphQL type definitions from entity structures
        /// </summary>
        /// <param name="entities">The entity structures to convert</param>
        /// <param name="outputPath">Output path</param>
        /// <param name="namespaceName">Namespace</param>
        /// <returns>The generated GraphQL schema</returns>
        string GenerateGraphQLSchema(List<EntityStructure> entities, string outputPath,
                                   string namespaceName = "TheTechIdea.ProjectGraphQL");

        /// <summary>
        /// Generates repository pattern implementation for an entity
        /// </summary>
        /// <param name="entity">Entity structure</param>
        /// <param name="outputPath">Output path</param>
        /// <param name="namespaceName">Namespace</param>
        /// <param name="interfaceOnly">Whether to generate interface only</param>
        /// <returns>The generated repository code</returns>
        string GenerateRepositoryImplementation(EntityStructure entity, string outputPath,
                                               string namespaceName = "TheTechIdea.ProjectRepositories",
                                               bool interfaceOnly = false);
        /// <summary>
        /// Generates XML documentation from entity structure
        /// </summary>
        /// <param name="entity">Entity structure to document</param>
        /// <param name="outputPath">Output path</param>
        /// <returns>The XML documentation string</returns>
        string GenerateEntityDocumentation(EntityStructure entity, string outputPath);

        /// <summary>
        /// Generates FluentValidation validators for entity
        /// </summary>
        /// <param name="entity">Entity structure</param>
        /// <param name="outputPath">Output path</param>
        /// <param name="namespaceName">Namespace</param>
        /// <returns>The validator class code</returns>
        string GenerateFluentValidators(EntityStructure entity, string outputPath,
                                       string namespaceName = "TheTechIdea.ProjectValidators");
        /// <summary>
        /// Generates Entity Framework Core migration code for entity
        /// </summary>
        /// <param name="entity">Entity structure</param>
        /// <param name="outputPath">Output path</param>
        /// <param name="namespaceName">Namespace</param>
        /// <returns>The migration code</returns>
        string GenerateEFCoreMigration(EntityStructure entity, string outputPath,
                                      string namespaceName = "TheTechIdea.ProjectMigrations");

        /// <summary>
        /// Generates a differenct report between two versions of an entity
        /// </summary>
        /// <param name="originalEntity">Original entity</param>
        /// <param name="newEntity">New entity</param>
        /// <returns>Difference report as string</returns>
        string GenerateEntityDiffReport(EntityStructure originalEntity, EntityStructure newEntity);
        /// <summary>
        /// Generates serverless function code (Azure Functions/AWS Lambda) for entity operations
        /// </summary>
        /// <param name="entity">Entity structure</param>
        /// <param name="outputPath">Output path</param>
        /// <param name="cloudProvider">Cloud provider type (Azure, AWS, etc)</param>
        /// <returns>The serverless function code</returns>
        string GenerateServerlessFunctions(EntityStructure entity, string outputPath,
                                         CloudProviderType cloudProvider = CloudProviderType.Azure);

        /// <summary>
        /// Generates gRPC service definitions for entity
        /// </summary>
        /// <param name="entity">Entity structure</param>
        /// <param name="outputPath">Output path</param>
        /// <param name="namespaceName">Namespace</param>
        /// <returns>The generated proto file and service implementation</returns>
        (string ProtoFile, string ServiceImplementation) GenerateGrpcService(EntityStructure entity,
                                                                            string outputPath,
                                                                            string namespaceName);

        /// <summary>
        /// Generates Blazor component for displaying and editing entity
        /// </summary>
        /// <param name="entity">Entity structure</param>
        /// <param name="outputPath">Output path</param>
        /// <param name="namespaceName">Component namespace</param>
        /// <returns>The Blazor component code</returns>

        #region Batch Class Creation Methods

        /// <summary>
        /// Creates multiple POCO classes from a list of entity structures
        /// </summary>
        List<string> CreatePOCOClasses(List<EntityStructure> entities, string usingheader, 
            string implementations, string extracode, string outputpath, 
            string nameSpacestring = "TheTechIdea.ProjectClasses", bool generateCSharpCodeFiles = true);

        /// <summary>
        /// Creates multiple INotify classes from a list of entity structures
        /// </summary>
        List<string> CreateINotifyClasses(List<EntityStructure> entities, string usingheader, 
            string implementations, string extracode, string outputpath, 
            string nameSpacestring = "TheTechIdea.ProjectClasses", bool generateCSharpCodeFiles = true);

        /// <summary>
        /// Creates multiple Entity classes from a list of entity structures
        /// </summary>
        List<string> CreateEntityClasses(List<EntityStructure> entities, string usingheader, 
            string extracode, string outputpath, 
            string nameSpacestring = "TheTechIdea.ProjectClasses", bool generateCSharpCodeFiles = true);

        /// <summary>
        /// Creates multiple record classes from a list of entity structures
        /// </summary>
        List<string> CreateRecordClasses(List<EntityStructure> entities, string outputPath,
            string namespaceName = "TheTechIdea.ProjectClasses", bool generateFile = true);

        /// <summary>
        /// Creates multiple nullable-aware classes from a list of entity structures
        /// </summary>
        List<string> CreateNullableAwareClasses(List<EntityStructure> entities, string outputPath,
            string namespaceName = "TheTechIdea.ProjectClasses", bool generateNullableAnnotations = true);

        /// <summary>
        /// Creates multiple DDD aggregate root classes from a list of entity structures
        /// </summary>
        List<string> CreateDDDAggregateRoots(List<EntityStructure> entities, string outputPath,
            string namespaceName = "TheTechIdea.ProjectDomain");

        #endregion

        #region Batch Code Generation Methods

        /// <summary>
        /// Generates data access layer classes for multiple entities
        /// </summary>
        List<string> GenerateDataAccessLayers(List<EntityStructure> entities, string outputPath);

        /// <summary>
        /// Generates unit test classes for multiple entities
        /// </summary>
        List<string> GenerateUnitTestClasses(List<EntityStructure> entities, string outputPath);

        /// <summary>
        /// Generates Entity Framework configurations for multiple entities
        /// </summary>
        List<string> GenerateEntityConfigurations(List<EntityStructure> entities, string namespaceString, string outputPath);

        /// <summary>
        /// Generates repository implementations for multiple entities
        /// </summary>
        List<string> GenerateRepositoryImplementations(List<EntityStructure> entities, string outputPath,
            string namespaceName = "TheTechIdea.ProjectRepositories", bool interfaceOnly = false);

        /// <summary>
        /// Generates XML documentation for multiple entities
        /// </summary>
        List<string> GenerateEntityDocumentations(List<EntityStructure> entities, string outputPath);

        /// <summary>
        /// Generates FluentValidation validators for multiple entities
        /// </summary>
        List<string> GenerateFluentValidatorsForEntities(List<EntityStructure> entities, string outputPath,
            string namespaceName = "TheTechIdea.ProjectValidators");

        /// <summary>
        /// Generates Entity Framework Core migrations for multiple entities
        /// </summary>
        List<string> GenerateEFCoreMigrations(List<EntityStructure> entities, string outputPath,
            string namespaceName = "TheTechIdea.ProjectMigrations");

        /// <summary>
        /// Generates serverless functions for multiple entities
        /// </summary>
        List<string> GenerateServerlessFunctionsForEntities(List<EntityStructure> entities, string outputPath,
            CloudProviderType cloudProvider = CloudProviderType.Azure);

        /// <summary>
        /// Generates gRPC service definitions for multiple entities
        /// </summary>
        List<(string ProtoFile, string ServiceImplementation)> GenerateGrpcServices(List<EntityStructure> entities,
            string outputPath, string namespaceName);

        /// <summary>
        /// Generates Blazor components for multiple entities
        /// </summary>
        List<string> GenerateBlazorComponents(List<EntityStructure> entities, string outputPath,
            string namespaceName = "TheTechIdea.ProjectComponents");

        #endregion

        #region Batch POCO Conversion Methods

        /// <summary>
        /// Converts multiple POCO types to EntityStructures
        /// </summary>
        List<EntityStructure> ConvertPocosToEntities(List<Type> pocoTypes, bool detectRelationships = true);

        /// <summary>
        /// Converts multiple objects to EntityStructures
        /// </summary>
        List<EntityStructure> ConvertToEntityStructures(List<object> instances,
            KeyDetectionStrategy strategy = KeyDetectionStrategy.AttributeThenConvention);

        /// <summary>
        /// Converts multiple POCO types to EntityStructures using generic method
        /// </summary>
        List<EntityStructure> ConvertToEntityStructures<T>(List<T> instances,
            KeyDetectionStrategy strategy = KeyDetectionStrategy.AttributeThenConvention) where T : class;

        /// <summary>
        /// Generates entity classes from multiple POCO types
        /// </summary>
        List<string> GenerateEntityClassesFromPocos(List<Type> pocoTypes, string outputPath = null,
            string namespaceString = "TheTechIdea.ProjectClasses", bool generateFile = true);

        /// <summary>
        /// Creates runtime types from multiple EntityStructures
        /// </summary>
        List<Type> CreateEntityTypesAtRuntime(List<EntityStructure> entities, 
            string namespaceString = "TheTechIdea.ProjectClasses");

        /// <summary>
        /// Creates runtime types from multiple POCO types
        /// </summary>
        List<Type> CreateEntityTypesFromPocosAtRuntime(List<Type> pocoTypes, 
            string namespaceString = "TheTechIdea.ProjectClasses");

        #endregion

        #region Namespace-Based Conversion Methods

        /// <summary>
        /// Converts all POCO classes from a namespace in loaded library to Entity classes and saves to files
        /// </summary>
        /// <param name="namespaceName">Namespace containing POCO classes</param>
        /// <param name="outputPath">Output directory path for generated entity files</param>
        /// <param name="assembly">Optional specific assembly to search</param>
        /// <param name="namespaceString">Target namespace for generated entity classes</param>
        /// <returns>List of generated entity class file paths</returns>
        List<string> ConvertNamespacePocoClassesToEntities(string namespaceName, string outputPath, 
            Assembly assembly = null, string namespaceString = "TheTechIdea.ProjectClasses");

        /// <summary>
        /// Converts all POCO classes from a namespace in loaded library to Entity code and saves to single file
        /// </summary>
        /// <param name="namespaceName">Namespace containing POCO classes</param>
        /// <param name="outputPath">Output file path for generated combined entity code</param>
        /// <param name="assembly">Optional specific assembly to search</param>
        /// <param name="namespaceString">Target namespace for generated entity classes</param>
        /// <returns>Path to generated file</returns>
        string ConvertNamespacePocoClassesToEntityFile(string namespaceName, string outputPath, 
            Assembly assembly = null, string namespaceString = "TheTechIdea.ProjectClasses");

        /// <summary>
        /// Converts all EF Core mapped classes from a namespace in loaded library to Entity classes
        /// Automatically excludes navigation properties and virtual collections
        /// </summary>
        /// <param name="namespaceName">Namespace containing EF Core DbSet classes</param>
        /// <param name="outputPath">Output directory path for generated entity files</param>
        /// <param name="assembly">Optional specific assembly to search</param>
        /// <param name="namespaceString">Target namespace for generated entity classes</param>
        /// <returns>List of generated entity class file paths</returns>
        List<string> ConvertNamespaceEFCoreClassesToEntities(string namespaceName, string outputPath, 
            Assembly assembly = null, string namespaceString = "TheTechIdea.ProjectClasses");

        /// <summary>
        /// Converts all EF Core mapped classes from a namespace in loaded library to Entity code and saves to single file
        /// Automatically excludes navigation properties and virtual collections
        /// </summary>
        /// <param name="namespaceName">Namespace containing EF Core DbSet classes</param>
        /// <param name="outputPath">Output file path for generated combined entity code</param>
        /// <param name="assembly">Optional specific assembly to search</param>
        /// <param name="namespaceString">Target namespace for generated entity classes</param>
        /// <returns>Path to generated file</returns>
        string ConvertNamespaceEFCoreClassesToEntityFile(string namespaceName, string outputPath, 
            Assembly assembly = null, string namespaceString = "TheTechIdea.ProjectClasses");

        #endregion

        /// <summary>
        /// Scans a namespace and discovers all POCO classes
        /// </summary>
        List<Type> ScanNamespaceForPocos(string namespaceName, Assembly assembly = null);

        /// <summary>
        /// Finds a specific class by name in a namespace
        /// </summary>
        Type FindClassByName(string namespaceName, string className, Assembly assembly = null);

        /// <summary>
        /// Converts a POCO type to EntityStructure with relationship detection
        /// </summary>
        EntityStructure ConvertPocoToEntity(Type pocoType, bool detectRelationships = true);

        /// <summary>
        /// Converts a POCO type to EntityStructure using generic type with KeyDetectionStrategy
        /// </summary>
        EntityStructure ConvertToEntityStructure<T>(
            KeyDetectionStrategy strategy = KeyDetectionStrategy.AttributeThenConvention,
            string entityName = null) where T : class;

        /// <summary>
        /// Converts a runtime POCO type to EntityStructure with KeyDetectionStrategy
        /// </summary>
        EntityStructure ConvertToEntityStructure(Type pocoType,
            KeyDetectionStrategy strategy = KeyDetectionStrategy.AttributeThenConvention,
            string entityName = null);

        /// <summary>
        /// Converts a POCO object instance to EntityStructure with KeyDetectionStrategy
        /// </summary>
        EntityStructure ConvertToEntityStructure(object instance,
            KeyDetectionStrategy strategy = KeyDetectionStrategy.AttributeThenConvention,
            string entityName = null);

        /// <summary>
        /// Generates entity class code from a POCO type
        /// </summary>
        string GenerateEntityClassFromPoco(Type pocoType, string outputPath = null,
            string namespaceString = "TheTechIdea.ProjectClasses", bool generateFile = true);

        /// <summary>
        /// Generates entity classes from all POCOs in a namespace
        /// </summary>
        List<string> GenerateEntityClassesFromNamespace(string sourceNamespace, string outputPath = null,
            string targetNamespace = "TheTechIdea.ProjectClasses", bool generateFiles = true, Assembly assembly = null);

        /// <summary>
        /// Compiles entity code and returns the Type at runtime (no file output)
        /// </summary>
        Type CreateEntityTypeAtRuntime(EntityStructure entity, string namespaceString = "TheTechIdea.ProjectClasses");

        /// <summary>
        /// Compiles entity code from POCO and returns the Type at runtime
        /// </summary>
        Type CreateEntityTypeFromPocoAtRuntime(Type pocoType, string namespaceString = "TheTechIdea.ProjectClasses");

        /// <summary>
        /// Generates and compiles multiple entity types at runtime from a namespace
        /// </summary>
        Dictionary<string, Type> CreateEntityTypesFromNamespaceAtRuntime(string sourceNamespace,
            string targetNamespace = "TheTechIdea.ProjectClasses", Assembly assembly = null);

        /// <summary>
        /// Gets circular reference diagnostics for a POCO type
        /// </summary>
        List<string> GetCircularReferences<T>() where T : class;

        /// <summary>
        /// Generates and compiles multiple entity types at runtime from a datasource
        /// </summary>
        Dictionary<string, Type> CreateEntityTypesFromDataSourceAtRuntime(string datasourceName,
            List<string> entityNames = null, string targetNamespace = "TheTechIdea.ProjectClasses");

        /// <summary>
        /// Clears the runtime type cache
        /// </summary>
        void ClearPocoToEntityCache();

     

        #region Utility Methods

        /// <summary>
        /// Generates a safe C# property name from a field name
        /// </summary>
        string GenerateSafePropertyName(string fieldName, int index = 0);

        /// <summary>
        /// Converts a name to snake_case
        /// </summary>
        string ToSnakeCase(string name);

        /// <summary>
        /// Converts a name to PascalCase
        /// </summary>
        string ToPascalCase(string name);

        /// <summary>
        /// Converts a name to camelCase
        /// </summary>
        string ToCamelCase(string name);

        #endregion

        string GenerateBlazorComponent(EntityStructure entity, string outputPath,
                                      string namespaceName = "TheTechIdea.ProjectComponents");

    }
}