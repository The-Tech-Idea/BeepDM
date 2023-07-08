using DataManagementModels.DriversConfigurations;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.Tools
{
    public interface IAssemblyHandler
    {
      //  List<IDM_Addin> AddIns { get; set; }
        List<assemblies_rep> Assemblies { get; set; }
     // AppDomain CurrentDomain { get; set; }
        List<AssemblyClassDefinition> DataSourcesClasses { get; set; }
        // public IDMEEditor DMEEditor { get; set; }
        IConfigEditor ConfigEditor { get; set; }
        IErrorsInfo ErrorObject { get; set; }
        IDMLogger Logger { get; set; }
        IUtil Utilfunction { get; set; }
        bool AddEngineDefaultDrivers();
        void CheckDriverAlreadyExistinList();
        object CreateInstanceFromString(string typeName, params object[] args);
        object CreateInstanceFromString(string dll, string typeName, params object[] args);
        IErrorsInfo GetBuiltinClasses();
        List<ParentChildObject> GetAddinObjects(Assembly asm);
        List<ConnectionDriversConfig> GetDrivers(Assembly asm);
        object GetInstance(string strFullyQualifiedName);
        ParentChildObject RearrangeAddin(string p, string parentid, string Objt);
        Type GetType(string strFullyQualifiedName);
        string LoadAssembly(string path, FolderFileTypes fileTypes);
        IErrorsInfo LoadAllAssembly(IProgress<PassedArgs> progress, CancellationToken token);
        bool RunMethod(object ObjInstance, string FullClassName, string MethodName);
        AssemblyClassDefinition GetAssemblyClassDefinition(TypeInfo type, string typename);
    }
}
