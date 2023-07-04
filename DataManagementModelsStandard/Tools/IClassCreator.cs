using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Tools
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

    }
}