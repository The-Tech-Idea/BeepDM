using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.Tools
{
    public interface IAssemblyLoader
    {
        AppDomain CurrentDomain { get; set; }
        List<IDM_Addin> AddIns { get; set; }
        List<assemblies_rep> Assemblies { get; set; }
        IDMEEditor DME_editor { get; set; }
        IErrorsInfo Erinfo { get; set; }
        IDMLogger Logger { get; set; }
        //List<ConnectionDriversConfig> DataDrivers { get; set; }
        List<ParentChildObject> GetClasses(Assembly asm);
        List<ConnectionDriversConfig> GetDrivers(Assembly asm);
        ParentChildObject GetObject(string p, string parentid, string Objt);
        IErrorsInfo LoadAddinAssemblies();
        IErrorsInfo LoadOtherAssemblies();
        IErrorsInfo LoadConnectionDriversAssemblies();
      
         Type GetTypeFromName(string typeName);
        IErrorsInfo LoadProjectClassesAssemblies();
        IErrorsInfo GetBuiltinClasses();
        bool AddEngineDefaultDrivers();
        //IErrorsInfo GetWorkFlowActionsClasses();
        object CreateInstanceFromString(string typeName, params object[] args);
    }
}