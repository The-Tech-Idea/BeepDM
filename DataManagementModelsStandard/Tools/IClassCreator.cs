using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Tools
{
    public interface IClassCreator
    {
        IDMEEditor DMEEditor { get; set; }
        string outputFileName { get; set; }
        string outputpath { get; set; }

        //void AddConstructor();
        //void AddFields(EntityField fld);
        //void AddProperties(EntityField fld);
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

        string GenerateBlazorDetailedView(string namespaceName, string entityName, List<EntityField> fields);
        string GenerateBlazorDashboardPage(string namespaceName, List<EntityStructure> entities);
        string GenerateReactListComponent(string entityName, List<EntityField> fields, string outputPath);
        string GenerateReactDashboardComponent(List<string> entityNames, string outputPath);
        string GenerateReactDetailComponent(string entityName, List<EntityField> fields, string outputPath);
        string GenerateReactNavbarComponent(List<string> entityNames, string outputPath);
    }
}