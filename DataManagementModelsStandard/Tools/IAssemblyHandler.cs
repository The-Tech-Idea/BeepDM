
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Logger;

using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Tools
{
    public interface IAssemblyHandler:IDisposable
    {
      //  List<IDM_Addin> AddIns { get; set; }

        List<string> NamespacestoIgnore { get; set; }
        List<assemblies_rep> Assemblies { get; set; }
        List<Assembly> LoadedAssemblies { get;  set; }
        List<Type> LoaderExtensions { get; set; }
        List<AssemblyClassDefinition> LoaderExtensionClasses { get; set; }
        Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args);
        List<AssemblyClassDefinition> DataSourcesClasses { get; set; }
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
        Assembly LoadAssembly(string path);
        bool LoadNugget(string path);
        bool UnloadNugget(string nuggetname);
        bool UnloadAssembly(string assemblyname);
        bool RunMethod(object ObjInstance, string FullClassName, string MethodName);
        AssemblyClassDefinition GetAssemblyClassDefinition(TypeInfo type, string typename);
        void AddTypeToCache(string fullName, Type type);
    }
}
