using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Tools.Interfaces
{
    /// <summary>
    /// Unified interface for comprehensive class creation functionality
    /// Combines all functionality from POCO generation, modern patterns, WebAPI, Database, 
    /// Serverless, UI, Testing, Documentation, and DLL creation
    /// </summary>
    public interface IClassCreator
    {
        #region Core Functionality

        IDMEEditor DMEEditor { get; set; }
        string outputFileName { get; set; }
        string outputpath { get; set; }

        Assembly CreateAssemblyFromCode(string code);
        Type CreateTypeFromCode(string code, string outputTypeName);
        void CompileClassFromText(string sourceString, string output);
        void GenerateCSharpCode(string fileName);

        #endregion

        #region POCO Class Generation

        string CreatePOCOClass(string datasourcename, string classname, List<string> entities, string usingheader,
            string implementations, string extracode, string outputpath,
            string nameSpacestring = "TheTechIdea.ProjectClasses", bool generateCSharpCodeFiles = true);

        string CreateINotifyClass(string datasourcename, List<string> entities, string usingheader, string implementations,
            string extracode, string outputpath, string nameSpacestring = "TheTechIdea.ProjectClasses",
            bool generateCSharpCodeFiles = true);

        string CreateEntityClass(string datasourcename, List<string> entities, string usingHeader, string extraCode,
            string outputPath, string namespaceString = "TheTechIdea.ProjectClasses", bool generateFiles = true);

        string CreatePOCOClass(string classname, List<EntityStructure> entities, string usingheader,
            string implementations, string extracode, string outputpath,
            string nameSpacestring = "TheTechIdea.ProjectClasses", bool generateCSharpCodeFiles = true);

        string CreatePOCOClass(string classname, EntityStructure entity, string usingheader,
            string implementations, string extracode, string outputpath,
            string nameSpacestring = "TheTechIdea.ProjectClasses", bool generateCSharpCodeFiles = true);

        string CreateINotifyClass(List<EntityStructure> entities, string usingheader, string implementations,
            string extracode, string outputpath, string nameSpacestring = "TheTechIdea.ProjectClasses",
            bool generateCSharpCodeFiles = true);

        string CreateINotifyClass(EntityStructure entity, string usingheader, string implementations,
            string extracode, string outputpath, string nameSpacestring = "TheTechIdea.ProjectClasses",
            bool generateCSharpCodeFiles = true);

        string CreateEntityClass(EntityStructure entity, string usingHeader, string extraCode,
            string outputPath, string namespaceString = "TheTechIdea.ProjectClasses", bool generateFiles = true);

        string CreateEntityClass(List<EntityStructure> entities, string usingHeader, string extraCode,
            string outputPath, string namespaceString = "TheTechIdea.ProjectClasses", bool generateFiles = true);

        #endregion

        #region Modern Class Generation

        string CreateRecordClass(string recordName, EntityStructure entity, string outputPath,
            string namespaceName = "TheTechIdea.ProjectClasses", bool generateFile = true);

        string CreateNullableAwareClass(string className, EntityStructure entity, string outputPath,
            string namespaceName = "TheTechIdea.ProjectClasses", bool generateNullableAnnotations = true);

        string CreateDDDAggregateRoot(EntityStructure entity, string outputPath,
            string namespaceName = "TheTechIdea.ProjectDomain");

        #endregion

        #region Web API Generation

        List<string> GenerateWebApiControllers(string dataSourceName, List<EntityStructure> entities,
            string outputPath, string namespaceName = "TheTechIdea.ProjectControllers");

        string GenerateWebApiControllerForEntityWithParams(string className, string outputPath,
            string namespaceName = "TheTechIdea.ProjectControllers");

        string GenerateMinimalWebApi(string outputPath, string namespaceName = "TheTechIdea.ProjectMinimalAPI");

        #endregion

        #region Database Class Generation

        string GenerateDataAccessLayer(EntityStructure entity, string outputPath);

        string GenerateDbContext(List<EntityStructure> entities, string namespaceString, string outputPath);

        string GenerateEntityConfiguration(EntityStructure entity, string namespaceString, string outputPath);

        string GenerateRepositoryImplementation(EntityStructure entity, string outputPath,
            string namespaceName = "TheTechIdea.ProjectRepositories", bool interfaceOnly = false);

        string GenerateEFCoreMigration(EntityStructure entity, string outputPath,
            string namespaceName = "TheTechIdea.ProjectMigrations");

        #endregion

        #region Serverless and Cloud Generation

        string GenerateServerlessFunctions(EntityStructure entity, string outputPath,
            CloudProviderType cloudProvider = CloudProviderType.Azure);

        (string ProtoFile, string ServiceImplementation) GenerateGrpcService(EntityStructure entity,
            string outputPath, string namespaceName);

        #endregion

        #region UI Component Generation

        string GenerateBlazorComponent(EntityStructure entity, string outputPath,
            string namespaceName = "TheTechIdea.ProjectComponents");

        string GenerateGraphQLSchema(List<EntityStructure> entities, string outputPath,
            string namespaceName = "TheTechIdea.ProjectGraphQL");

        #endregion

        #region Validation and Testing

        List<string> ValidateEntityStructure(EntityStructure entity);

        string GenerateUnitTestClass(EntityStructure entity, string outputPath);

        string GenerateFluentValidators(EntityStructure entity, string outputPath,
            string namespaceName = "TheTechIdea.ProjectValidators");

        #endregion

        #region Documentation and Utilities

        string GenerateEntityDocumentation(EntityStructure entity, string outputPath);

        string GenerateEntityDiffReport(EntityStructure originalEntity, EntityStructure newEntity);

        #endregion

        #region POCO to Entity Conversion

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

        #endregion

        #region DLL Creation

        string CreateDLL(string dllname, List<EntityStructure> entities, string outputpath,
            IProgress<PassedArgs> progress, CancellationToken token,
            string nameSpacestring = "TheTechIdea.ProjectClasses", bool generateCSharpCodeFiles = true);

        string CreateDLLFromFilesPath(string dllname, string filepath, string outputpath,
            IProgress<PassedArgs> progress, CancellationToken token,
            string nameSpacestring = "TheTechIdea.ProjectClasses");

        #endregion
    }
}



