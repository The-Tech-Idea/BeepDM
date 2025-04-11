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
       
        Assembly CreateAssemblyFromCode(string code);
        string CreateClass(string classname, List<EntityField> flds, string poutputpath, string NameSpacestring = "TheTechIdea.ProjectClasses", bool GenerateCSharpCodeFiles = true);
        string CreateClass(string classname, EntityStructure entity, string poutputpath, string NameSpacestring = "TheTechIdea.ProjectClasses", bool GenerateCSharpCodeFiles = true);
        string CreateClassFromTemplate(string classname, EntityStructure entity, string template, string usingheader, string implementations, string extracode, string outputpath, string nameSpacestring = "TheTechIdea.ProjectClasses", bool GenerateCSharpCodeFiles = true);
        string CreateDLL(string dllname, List<EntityStructure> entities, string outputpath, IProgress<PassedArgs> progress, CancellationToken token, string NameSpacestring = "TheTechIdea.ProjectClasses", bool GenerateCSharpCodeFiles = true);
        string CreateDLLFromFilesPath(string dllname, string filespath, string outputpath, IProgress<PassedArgs> progress, CancellationToken token, string NameSpacestring = "TheTechIdea.ProjectClasses");
        string CreateINotifyClass(EntityStructure entity, string usingheader, string implementations, string extracode, string outputpath, string nameSpacestring = "TheTechIdea.ProjectClasses", bool GenerateCSharpCodeFiles = true);
        string CreatEntityClass(EntityStructure entity, string usingheader, string extracode, string outputpath, string nameSpacestring = "TheTechIdea.ProjectClasses", bool GenerateCSharpCodeFiles = true);
        string CreatePOCOClass(string classname, EntityStructure entity, string usingheader, string implementations, string extracode, string outputpath, string nameSpacestring = "TheTechIdea.ProjectClasses", bool GenerateCSharpCodeFiles = true);
        Type CreateTypeFromCode(string code, string outputtypename);
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
        string GenerateBlazorComponent(EntityStructure entity, string outputPath,
                                      string namespaceName = "TheTechIdea.ProjectComponents");

    }
}