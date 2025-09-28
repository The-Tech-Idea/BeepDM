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
    /// Core interface for class creation functionality
    /// </summary>
    public interface IClassCreatorCore
    {
        IDMEEditor DMEEditor { get; set; }
        string outputFileName { get; set; }
        string outputpath { get; set; }
        
        Assembly CreateAssemblyFromCode(string code);
        Type CreateTypeFromCode(string code, string outputTypeName);
        void CompileClassFromText(string sourceString, string output);
        void GenerateCSharpCode(string fileName);
    }

    /// <summary>
    /// Interface for basic POCO class generation
    /// </summary>
    public interface IPocoClassGenerator
    {
        string CreatePOCOClass(string classname, EntityStructure entity, string usingheader, 
            string implementations, string extracode, string outputpath, 
            string nameSpacestring = "TheTechIdea.ProjectClasses", bool generateCSharpCodeFiles = true);
        
        string CreateINotifyClass(EntityStructure entity, string usingheader, string implementations, 
            string extracode, string outputpath, string nameSpacestring = "TheTechIdea.ProjectClasses", 
            bool generateCSharpCodeFiles = true);
        
        string CreateEntityClass(EntityStructure entity, string usingHeader, string extraCode, 
            string outputPath, string namespaceString = "TheTechIdea.ProjectClasses", bool generateFiles = true);
    }

    /// <summary>
    /// Interface for modern class generation patterns
    /// </summary>
    public interface IModernClassGenerator
    {
        string CreateRecordClass(string recordName, EntityStructure entity, string outputPath,
            string namespaceName = "TheTechIdea.ProjectClasses", bool generateFile = true);
        
        string CreateNullableAwareClass(string className, EntityStructure entity, string outputPath,
            string namespaceName = "TheTechIdea.ProjectClasses", bool generateNullableAnnotations = true);
        
        string CreateDDDAggregateRoot(EntityStructure entity, string outputPath,
            string namespaceName = "TheTechIdea.ProjectDomain");
    }

    /// <summary>
    /// Interface for web API generation
    /// </summary>
    public interface IWebApiGenerator
    {
        List<string> GenerateWebApiControllers(string dataSourceName, List<EntityStructure> entities, 
            string outputPath, string namespaceName = "TheTechIdea.ProjectControllers");
        
        string GenerateWebApiControllerForEntityWithParams(string className, string outputPath,
            string namespaceName = "TheTechIdea.ProjectControllers");
        
        string GenerateMinimalWebApi(string outputPath, string namespaceName = "TheTechIdea.ProjectMinimalAPI");
    }

    /// <summary>
    /// Interface for database-related class generation
    /// </summary>
    public interface IDatabaseClassGenerator
    {
        string GenerateDataAccessLayer(EntityStructure entity, string outputPath);
        string GenerateDbContext(List<EntityStructure> entities, string namespaceString, string outputPath);
        string GenerateEntityConfiguration(EntityStructure entity, string namespaceString, string outputPath);
        string GenerateRepositoryImplementation(EntityStructure entity, string outputPath,
            string namespaceName = "TheTechIdea.ProjectRepositories", bool interfaceOnly = false);
        string GenerateEFCoreMigration(EntityStructure entity, string outputPath,
            string namespaceName = "TheTechIdea.ProjectMigrations");
    }

    /// <summary>
    /// Interface for serverless and cloud generation
    /// </summary>
    public interface IServerlessGenerator
    {
        string GenerateServerlessFunctions(EntityStructure entity, string outputPath,
            CloudProviderType cloudProvider = CloudProviderType.Azure);
        
        (string ProtoFile, string ServiceImplementation) GenerateGrpcService(EntityStructure entity,
            string outputPath, string namespaceName);
    }

    /// <summary>
    /// Interface for UI component generation
    /// </summary>
    public interface IUiComponentGenerator
    {
        string GenerateBlazorComponent(EntityStructure entity, string outputPath,
            string namespaceName = "TheTechIdea.ProjectComponents");
        
        string GenerateGraphQLSchema(List<EntityStructure> entities, string outputPath,
            string namespaceName = "TheTechIdea.ProjectGraphQL");
    }

    /// <summary>
    /// Interface for validation and testing
    /// </summary>
    public interface IValidationAndTestingGenerator
    {
        List<string> ValidateEntityStructure(EntityStructure entity);
        string GenerateUnitTestClass(EntityStructure entity, string outputPath);
        string GenerateFluentValidators(EntityStructure entity, string outputPath,
            string namespaceName = "TheTechIdea.ProjectValidators");
    }

    /// <summary>
    /// Interface for documentation and utilities
    /// </summary>
    public interface IDocumentationGenerator
    {
        string GenerateEntityDocumentation(EntityStructure entity, string outputPath);
        string GenerateEntityDiffReport(EntityStructure originalEntity, EntityStructure newEntity);
    }

    /// <summary>
    /// Interface for DLL creation and compilation
    /// </summary>
    public interface IDllCreator
    {
        string CreateDLL(string dllname, List<EntityStructure> entities, string outputpath, 
            IProgress<PassedArgs> progress, CancellationToken token, 
            string nameSpacestring = "TheTechIdea.ProjectClasses", bool generateCSharpCodeFiles = true);
        
        string CreateDLLFromFilesPath(string dllname, string filepath, string outputpath,
            IProgress<PassedArgs> progress, CancellationToken token,
            string nameSpacestring = "TheTechIdea.ProjectClasses");
    }
}



