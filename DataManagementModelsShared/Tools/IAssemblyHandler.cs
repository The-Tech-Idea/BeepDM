﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.Util;

namespace TheTechIdea.Tools
{
    public interface IAssemblyHandler
    {
        List<IDM_Addin> AddIns { get; set; }
        List<assemblies_rep> Assemblies { get; set; }
     // AppDomain CurrentDomain { get; set; }
        List<AssemblyClassDefinition> DataSources { get; set; }
        IDMEEditor DMEEditor { get; set; }

        bool AddEngineDefaultDrivers();
        void CheckDriverAlreadyExistinList();
        object CreateInstanceFromString(string typeName, params object[] args);
        IErrorsInfo GetBuiltinClasses();
        List<ParentChildObject> GetAddinObjects(Assembly asm);
        List<ConnectionDriversConfig> GetDrivers(Assembly asm);
        object GetInstance(string strFullyQualifiedName);
        ParentChildObject RearrangeAddin(string p, string parentid, string Objt);
        Type GetType(string strFullyQualifiedName);
        Type GetTypeFromName(string typeName);
        IErrorsInfo LoadAllAssembly();
        bool RunMethod(object ObjInstance, string FullClassName, string MethodName);
    }
}