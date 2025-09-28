using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using TheTechIdea.Beep.DataBase;
using System.Threading;
using TheTechIdea.Beep.Utilities;
using System.Linq;
using System.Text.RegularExpressions;
using TheTechIdea.Beep.Roslyn;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Helpers;

namespace TheTechIdea.Beep.Tools
{
    ///// <summary>
    ///// LEGACY COMPATIBILITY CLASS - DEPRECATED
    ///// 
    ///// This class is maintained for backward compatibility only.
    ///// New development should use the modular ClassCreator architecture:
    ///// - ClassCreator.Core.cs - Main functionality
    ///// - ClassCreator.DllCreation.cs - DLL compilation
    ///// - ClassCreator.WebApi.cs - Web API generation
    ///// - ClassCreator.Database.cs - Database class generation
    ///// - Various helper classes in Tools/Helpers/
    ///// </summary>
    //[Obsolete("This monolithic class is deprecated. Use the new modular ClassCreator architecture.", false)]
    //public class LegacyClassCreator : IClassCreator
    //{
    //    public string outputFileName { get; set; }
    //    public string outputpath { get; set; }
    //    public IDMEEditor DMEEditor { get; set; }

    //    private readonly ClassCreator _modernClassCreator;

    //    public LegacyClassCreator(IDMEEditor pDMEEditor)
    //    {
    //        DMEEditor = pDMEEditor;
    //        _modernClassCreator = new ClassCreator(pDMEEditor);
    //    }

    //    #region Legacy Method Implementations - Delegating to Modern Architecture

    //    public void CompileClassFromText(string SourceString, string output)
    //    {
    //        _modernClassCreator.CompileClassFromText(SourceString, output);
    //    }

    //    public string CreateDLL(string dllname, List<EntityStructure> entities, string outputpath, 
    //        IProgress<PassedArgs> progress, CancellationToken token, string NameSpacestring = "TheTechIdea.ProjectClasses", 
    //        bool GenerateCSharpCodeFiles = true)
    //    {
    //        return _modernClassCreator.CreateDLL(dllname, entities, outputpath, progress, token, 
    //            NameSpacestring, GenerateCSharpCodeFiles);
    //    }

    //    public string CreateDLLFromFilesPath(string dllname, string filepath, string outputpath, 
    //        IProgress<PassedArgs> progress, CancellationToken token, string NameSpacestring = "TheTechIdea.ProjectClasses")
    //    {
    //        return _modernClassCreator.CreateDLLFromFilesPath(dllname, filepath, outputpath, progress, token, NameSpacestring);
    //    }

    //    public string CreateClass(string classname, List<EntityField> flds, string poutputpath, 
    //        string NameSpacestring = "TheTechIdea.ProjectClasses", bool GenerateCSharpCodeFiles = true)
    //    {
    //        return _modernClassCreator.CreateClass(classname, flds, poutputpath, NameSpacestring, GenerateCSharpCodeFiles);
    //    }

    //    public string CreateClass(string classname, EntityStructure entity, string poutputpath, 
    //        string NameSpacestring = "TheTechIdea.ProjectClasses", bool GenerateCSharpCodeFiles = true)
    //    {
    //        return _modernClassCreator.CreateClass(classname, entity, poutputpath, NameSpacestring, GenerateCSharpCodeFiles);
    //    }

    //    public void GenerateCSharpCode(string fileName)
    //    {
    //        _modernClassCreator.GenerateCSharpCode(fileName);
    //    }

    //    public string CreatePOCOClass(string classname, EntityStructure entity, string usingheader, 
    //        string implementations, string extracode, string outputpath, string nameSpacestring = "TheTechIdea.ProjectClasses", 
    //        bool GenerateCSharpCodeFiles = true)
    //    {
    //        return _modernClassCreator.CreatePOCOClass(classname, entity, usingheader, implementations, 
    //            extracode, outputpath, nameSpacestring, GenerateCSharpCodeFiles);
    //    }

    //    public string CreateINotifyClass(EntityStructure entity, string usingheader, string implementations, 
    //        string extracode, string outputpath, string nameSpacestring = "TheTechIdea.ProjectClasses", 
    //        bool GenerateCSharpCodeFiles = true)
    //    {
    //        return _modernClassCreator.CreateINotifyClass(entity, usingheader, implementations, extracode, 
    //            outputpath, nameSpacestring, GenerateCSharpCodeFiles);
    //    }

    //    public string CreatEntityClass(EntityStructure entity, string usingHeader, string extraCode, 
    //        string outputPath, string namespaceString = "TheTechIdea.ProjectClasses", bool generateFiles = true)
    //    {
    //        return _modernClassCreator.CreateEntityClass(entity, usingHeader, extraCode, outputPath, 
    //            namespaceString, generateFiles);
    //    }

    //    public Assembly CreateAssemblyFromCode(string code)
    //    {
    //        return _modernClassCreator.CreateAssemblyFromCode(code);
    //    }

    //    public Type CreateTypeFromCode(string code, string outputtypename)
    //    {
    //        return _modernClassCreator.CreateTypeFromCode(code, outputtypename);
    //    }

    //    public List<string> ValidateEntityStructure(EntityStructure entity)
    //    {
    //        return _modernClassCreator.ValidateEntityStructure(entity);
    //    }

    //    public List<string> GenerateWebApiControllers(string dataSourceName, List<EntityStructure> entities, 
    //        string outputPath, string namespaceName = "TheTechIdea.ProjectControllers")
    //    {
    //        return _modernClassCreator.GenerateWebApiControllers(dataSourceName, entities, outputPath, namespaceName);
    //    }

    //    public string GenerateWebApiControllerForEntityWithParams(string className, string outputPath,
    //        string namespaceName = "TheTechIdea.ProjectControllers")
    //    {
    //        return _modernClassCreator.GenerateWebApiControllerForEntityWithParams(className, outputPath, namespaceName);
    //    }

    //    public string GenerateMinimalWebApi(string outputPath, string namespaceName = "TheTechIdea.ProjectMinimalAPI")
    //    {
    //        return _modernClassCreator.GenerateMinimalWebApi(outputPath, namespaceName);
    //    }

    //    public string GenerateDataAccessLayer(EntityStructure entity, string outputPath)
    //    {
    //        return _modernClassCreator.GenerateDataAccessLayer(entity, outputPath);
    //    }

    //    public string GenerateDbContext(List<EntityStructure> entities, string namespaceString, string outputPath)
    //    {
    //        return _modernClassCreator.GenerateDbContext(entities, namespaceString, outputPath);
    //    }

    //    public string GenerateEntityConfiguration(EntityStructure entity, string namespaceString, string outputPath)
    //    {
    //        return _modernClassCreator.GenerateEntityConfiguration(entity, namespaceString, outputPath);
    //    }

    //    public string GenerateRepositoryImplementation(EntityStructure entity, string outputPath,
    //        string namespaceName = "TheTechIdea.ProjectRepositories", bool interfaceOnly = false)
    //    {
    //        return _modernClassCreator.GenerateRepositoryImplementation(entity, outputPath, namespaceName, interfaceOnly);
    //    }

    //    public string GenerateEFCoreMigration(EntityStructure entity, string outputPath,
    //        string namespaceName = "TheTechIdea.ProjectMigrations")
    //    {
    //        return _modernClassCreator.GenerateEFCoreMigration(entity, outputPath, namespaceName);
    //    }

    //    // Additional methods would be delegated similarly...

    //    #endregion
    //}
}
