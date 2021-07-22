﻿using McMaster.NETCore.Plugins;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.AI;
using TheTechIdea.Beep.AppBuilder;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.Tools.AssemblyHandling
{
    public class AssemblyHandlerCore: IAssemblyHandler
    {
        string[] pluginPaths = new string[] { };
        ParentChildObject a;
        private string Name { get; set; }
        private string Descr { get; set; }
        public IDMEEditor DMEEditor { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public IDMLogger Logger { get; set; }
        public IUtil Utilfunction { get; set; }
        public IConfigEditor ConfigEditor { get; set; }
        public List<assemblies_rep> Assemblies { get; set; } = new List<assemblies_rep>();
        public List<IDM_Addin> AddIns { get; set; } = new List<IDM_Addin>();
        public List<AssemblyClassDefinition> DataSourcesClasses { get; set; } = new List<AssemblyClassDefinition>();
        private List<ConnectionDriversConfig> DataDriversConfig = new List<ConnectionDriversConfig>();
        #region "Plugin Loader"
        static PluginLoadContext  PluginLoadContext;
        AssemblyDependencyResolver _resolver;
        AssemblyLoadContext loadContext;
        static Assembly LoadPlugin(string relativePath)
        {
            string pluginLocation = Path.GetFullPath(Path.Combine(relativePath, relativePath.Replace('\\', Path.DirectorySeparatorChar)));
            PluginLoadContext.SetResolver(pluginLocation);
          return  PluginLoadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(pluginLocation)));

          
        }

      
        #endregion

        public AssemblyHandlerCore(IConfigEditor pConfigEditor, IErrorsInfo pErrorObject, IDMLogger pLogger, IUtil pUtilfunction)
        {

            ConfigEditor = pConfigEditor;
            ErrorObject = pErrorObject;
            Logger = pLogger;
            Utilfunction = pUtilfunction;
            PluginLoadContext = new PluginLoadContext();
            DataSourcesClasses = new List<AssemblyClassDefinition>();
        }

        public string LoadAssembly(string path, FolderFileTypes fileTypes)
        {
            ErrorObject.Flag = Errors.Ok;
            string res = "";

            foreach (string dll in Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories))
            {
                try
                {
                    Assembly loadedAssembly = LoadPlugin(dll);

                    assemblies_rep x = new assemblies_rep(loadedAssembly, path, dll, fileTypes);
                    Assemblies.Add(x);
                }
                catch (FileLoadException loadEx)
                {
                    ErrorObject.Flag = Errors.Failed;
                    res = "The Assembly has already been loaded" + loadEx.Message;
                } // The Assembly has already been loaded.
                catch (BadImageFormatException imgEx)
                {
                    ErrorObject.Flag = Errors.Failed;
                    res = imgEx.Message;
                }
                catch (Exception ex)
                {
                    ErrorObject.Flag = Errors.Failed;
                    res = ex.Message;
                }
            }


            ErrorObject.Message = res;
            return res;
        }
        #region "Loaders"
        public IErrorsInfo GetBuiltinClasses()
        {
            ErrorObject.Flag = Errors.Ok;

            // look through assembly list
            Assembly currentAssem = Assembly.GetExecutingAssembly();
            Assembly rootassembly = Assembly.GetEntryAssembly();

            try
            {
                ScanAssembly(currentAssem);
                Utilfunction.FunctionHierarchy = GetAddinObjects(currentAssem);
            }
            catch (Exception ex)
            {

                Logger.WriteLog($"error loading current assembly {ex.Message} ");
            }

            try
            {
                ScanAssembly(rootassembly);
                Utilfunction.FunctionHierarchy = GetAddinObjects(rootassembly);

            }
            catch (Exception ex)
            {

                Logger.WriteLog($"error loading current assembly {ex.Message} ");
            }
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(x => x.FullName.Contains("DataManagmentEngine"));
            try
            {
                foreach (Assembly item in assemblies)
                {
                    ScanAssembly(item);
                    Utilfunction.FunctionHierarchy = GetAddinObjects(item);
                }

            }
            catch (Exception ex)
            {

                Logger.WriteLog($"error loading current assembly {ex.Message} ");
            }
           
            return ErrorObject;

        }
        /// <summary>
        ///     This Method will go through all Folders ProjectClass,OtherDLL,Addin, Drivers and load DLL
        /// </summary>
        /// <returns></returns>
        public IErrorsInfo LoadAllAssembly()
        {
            ErrorObject.Flag = Errors.Ok;
            string res;
            Utilfunction.FunctionHierarchy = new List<ParentChildObject>();
            Utilfunction.Namespacelist = new List<string>();
            Utilfunction.Classlist = new List<string>();
            DataDriversConfig = new List<ConnectionDriversConfig>();

            GetNonADODrivers();
            GetBuiltinClasses();
            try
            {
                foreach (string p in ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.OtherDLL).Select(x => x.FolderPath))
                {
                    try
                    {
                        LoadAssembly(p, FolderFileTypes.OtherDLL);
                    }
                    catch (FileLoadException loadEx)
                    {
                        ErrorObject.Flag = Errors.Failed;
                        res = "The Assembly has already been loaded" + loadEx.Message;
                        // MessageBox.Show("The Assembly has already been loaded" + loadEx.Message, "Simple ODM", MessageBoxButtons.OK);
                    } // The Assembly has already been loaded.
                    catch (BadImageFormatException imgEx)
                    {
                        ErrorObject.Flag = Errors.Failed;
                        // MessageBox.Show(imgEx.Message, "Simple ODM", MessageBoxButtons.OK);  // If a BadImageFormatException exception is thrown, the file is not an assembly.
                        res = imgEx.Message;
                    }
                    catch (Exception ex)
                    {
                        ErrorObject.Flag = Errors.Failed;
                        // MessageBox.Show(ex.Message, "Simple ODM", MessageBoxButtons.OK);  // If a BadImageFormatException exception is thrown, the file is not an assembly
                        res = ex.Message;
                    }

                }
                foreach (string p in ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.ConnectionDriver).Select(x => x.FolderPath))
                {
                    try
                    {
                        LoadAssembly(p, FolderFileTypes.ConnectionDriver);


                    }
                    catch (FileLoadException loadEx)
                    {
                        ErrorObject.Flag = Errors.Failed;
                        res = "The Assembly has already been loaded" + loadEx.Message;
                        // MessageBox.Show("The Assembly has already been loaded" + loadEx.Message, "Simple ODM", MessageBoxButtons.OK);
                    } // The Assembly has already been loaded.
                    catch (BadImageFormatException imgEx)
                    {
                        ErrorObject.Flag = Errors.Failed;
                        // MessageBox.Show(imgEx.Message, "Simple ODM", MessageBoxButtons.OK);  // If a BadImageFormatException exception is thrown, the file is not an assembly.
                        res = imgEx.Message;
                    }
                    catch (Exception ex)
                    {
                        ErrorObject.Flag = Errors.Failed;
                        // MessageBox.Show(ex.Message, "Simple ODM", MessageBoxButtons.OK);  // If a BadImageFormatException exception is thrown, the file is not an assembly
                        res = ex.Message;
                    }

                }
                foreach (string p in ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.ProjectClass).Select(x => x.FolderPath))
                {
                    try
                    {
                        LoadAssembly(p, FolderFileTypes.ProjectClass);
                    }
                    catch (FileLoadException loadEx)
                    {
                        ErrorObject.Flag = Errors.Failed;
                        res = "The Assembly has already been loaded" + loadEx.Message;
                        // MessageBox.Show("The Assembly has already been loaded" + loadEx.Message, "Simple ODM", MessageBoxButtons.OK);
                    } // The Assembly has already been loaded.
                    catch (BadImageFormatException imgEx)
                    {
                        ErrorObject.Flag = Errors.Failed;
                        // MessageBox.Show(imgEx.Message, "Simple ODM", MessageBoxButtons.OK);  // If a BadImageFormatException exception is thrown, the file is not an assembly.
                        res = imgEx.Message;
                    }
                    catch (Exception ex)
                    {
                        ErrorObject.Flag = Errors.Failed;
                        // MessageBox.Show(ex.Message, "Simple ODM", MessageBoxButtons.OK);  // If a BadImageFormatException exception is thrown, the file is not an assembly
                        res = ex.Message;
                    }

                }

                // Get Driver from Loaded Assembly
                foreach (assemblies_rep item in Assemblies)
                {
                    GetDrivers(item.DllLib);
                }
                foreach (string p in ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.Addin).Select(x => x.FolderPath))
                {
                    try
                    {
                        LoadAssembly(p, FolderFileTypes.Addin);

                    }
                    catch (FileLoadException loadEx)
                    {
                        ErrorObject.Flag = Errors.Failed;
                        res = "The Assembly has already been loaded" + loadEx.Message;
                        // MessageBox.Show("The Assembly has already been loaded" + loadEx.Message, "Simple ODM", MessageBoxButtons.OK);
                    } // The Assembly has already been loaded.
                    catch (BadImageFormatException imgEx)
                    {
                        ErrorObject.Flag = Errors.Failed;
                        // MessageBox.Show(imgEx.Message, "Simple ODM", MessageBoxButtons.OK);  // If a BadImageFormatException exception is thrown, the file is not an assembly.
                        res = imgEx.Message;
                    }
                    catch (Exception ex)
                    {
                        ErrorObject.Flag = Errors.Failed;
                        // MessageBox.Show(ex.Message, "Simple ODM", MessageBoxButtons.OK);  // If a BadImageFormatException exception is thrown, the file is not an assembly
                        res = ex.Message;
                    }

                }
                // Scan Addin Assemblies
                //-------------------------------
                // Scan Project Class Assemblies
                foreach (assemblies_rep s in Assemblies.Where(x => x.FileTypes == FolderFileTypes.ProjectClass || x.FileTypes == FolderFileTypes.Addin))
                {
                    try
                    {

                        ScanAssembly(s.DllLib);
                        Utilfunction.FunctionHierarchy = GetAddinObjects(s.DllLib);

                    }

                    catch (Exception ex)
                    {
                        ErrorObject.Flag = Errors.Failed;
                    //    AddLogMessage("Fail", $"Error Scanning DLL {s.DllLib}-{ex.Message}", DateTime.Now, 0, ex.Message, Errors.Failed);
                        res = ex.Message;
                    }

                }

                //------------------------------
            }
            catch (System.Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
                Logger.WriteLog($"Error Loading Addin Assemblies ({ex.Message})");
            }
            AddEngineDefaultDrivers();
            CheckDriverAlreadyExistinList();
            return ErrorObject;
        }
        /// <summary>
        ///     Method Will Load All Assembly found in the Passed Path
        /// </summary>
        /// <param name="Path"></param>
        /// <param name="FolderFileTypes"></param>
        /// <returns></returns>
    
        #endregion "Loaders"
        #region "Class ordering"
        public ParentChildObject RearrangeAddin(string p, string parentid, string Objt)
        {
            ParentChildObject a;
            if (parentid == null)
            {
                if (Utilfunction.FunctionHierarchy.Where(f => f.id == p && f.ObjType == Objt).Count() == 0)
                {
                    a = new ParentChildObject() { id = p, ParentID = null, ObjType = Objt, AddinName = Name, Description = Descr };
                    Utilfunction.FunctionHierarchy.Add(a);
                }
                else
                {
                    a = Utilfunction.FunctionHierarchy.Where(f => f.id == p && f.ParentID == null && f.ObjType == Objt).FirstOrDefault();

                }
            }
            else
            {
                if (Utilfunction.FunctionHierarchy.Where(f => f.id == p && f.ParentID == parentid && f.ObjType == Objt).Count() == 0)
                {
                    a = new ParentChildObject() { id = p, ParentID = parentid, ObjType = Objt, AddinName = Name, Description = Descr };
                    Utilfunction.FunctionHierarchy.Add(a);

                }
                else
                {
                    a = Utilfunction.FunctionHierarchy.Where(f => f.id == p && f.ParentID == parentid && f.ObjType == Objt).FirstOrDefault();

                }
            }
            return a;
        }
        public List<ParentChildObject> GetAddinObjects(Assembly asm)
        {
            IDM_Addin addin = null;
            string objtype = "";
            Boolean Show = true;
            int cnt = 0;
            foreach (Type type in asm.DefinedTypes)
            {
                if (typeof(IDM_Addin).IsAssignableFrom(type))
                {
                    try
                    {
                        if (type.FullName.Contains("Properties") == false)
                        {

                            string[] p = type.FullName.Split(new char[] { '.' });

                            if (p.Length >= 0)
                            {
                                for (int i = 0; i < p.Length; i++)
                                {
                                    cnt += 1;
                                    if (i == 0)
                                    {
                                        Name = p[i];
                                        Descr = p[i];
                                        a = RearrangeAddin(p[i], null, "namespace");
                                    }
                                    else
                                    {
                                        if (i == p.Length - 1)
                                        {


                                            try
                                            {
                                                IDM_Addin uc = (IDM_Addin)Activator.CreateInstance(type);
                                                if (uc != null)
                                                {
                                                    addin = (IDM_Addin)uc;
                                                    addin.DllPath = Path.GetDirectoryName(asm.Location);
                                                    addin.ObjectName = type.Name;
                                                    addin.DllName = Path.GetFileName(asm.Location);
                                                    Show = addin.DefaultCreate;
                                                    AddIns.Add(addin);
                                                }
                                                Name = addin.AddinName;
                                                Descr = addin.Description;

                                                if (addin.DefaultCreate)
                                                {
                                                    if (ConfigEditor.AddinTreeStructure.Where(x => x.className == type.Name).Any() == false)
                                                    {
                                                        Show = true;
                                                        try
                                                        {
                                                            IAddinVisSchema cls = (IAddinVisSchema)addin;
                                                            AddinTreeStructure xcls = new AddinTreeStructure();
                                                            xcls.className = type.Name;
                                                            xcls.dllname = type.Module.Name;
                                                            xcls.PackageName = type.FullName;
                                                            xcls.Order = cls.Order;
                                                            xcls.Imagename = cls.IconImageName;
                                                            xcls.RootName = cls.RootNodeName;
                                                            xcls.NodeName = cls.BranchText;
                                                            xcls.ObjectType = addin.ObjectType;
                                                            ConfigEditor.AddinTreeStructure.Add(xcls);

                                                        }
                                                        catch (Exception)
                                                        {

                                                        }
                                                    }

                                                }
                                                else Show = false;
                                                // objtype = "UserControl";

                                                if (Show)
                                                {
                                                    a = RearrangeAddin(p[i], p[i - 1], objtype);
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                string mes = ex.Message;
                                               // AddLogMessage(ex.Message, "Could" + mes, DateTime.Now, -1, mes, Errors.Failed);
                                            };


                                        }
                                        else
                                        {
                                            Name = p[i];
                                            Descr = p[i];
                                            a = RearrangeAddin(p[i], p[i - 1], "namespace");

                                        }

                                    }


                                }

                            }

                        }
                    }

                    catch (Exception ex)
                    {

                        Logger.WriteLog($"error in creating addin {ex.Message} ");
                    }
                }

            }
            ConfigEditor.SaveAddinTreeStructure();
            return Utilfunction.FunctionHierarchy;
        }
        #endregion
        #region "Class Extractors"
        private bool ScanAssembly(Assembly asm)
        {
            Type[] t;
            try
            {
                // Scan for Defined Types
                try
                {
                    try
                    {
                        t = asm.GetTypes();
                    }
                    catch (Exception)
                    {

                        t = asm.GetExportedTypes();
                    }

                    if (t != null)
                    {
                        foreach (var mytype in t) //asm.DefinedTypes
                        {

                            TypeInfo type = mytype.GetTypeInfo();
                            string[] p = asm.FullName.Split(new char[] { ',' });
                            p[1] = p[1].Substring(p[1].IndexOf("=") + 1);
                            //-------------------------------------------------------
                            // Get DataBase Drivers
                            if (type.ImplementedInterfaces.Contains(typeof(IDataSource)))
                            {
                                AssemblyClassDefinition xcls = new AssemblyClassDefinition();
                                xcls.className = type.Name;
                                xcls.dllname = type.Module.Name;
                                xcls.PackageName = type.FullName;
                                xcls.type = type;
                                xcls.componentType = "IDataSource";
                                xcls.classProperties = (ClassProperties)type.GetCustomAttribute(typeof(ClassProperties), false);
                                DataSourcesClasses.Add(xcls);
                                ConfigEditor.DataSourcesClasses.Add(xcls);
                            }
                            //-------------------------------------------------------
                            // Get WorkFlow Definitions
                            if (type.ImplementedInterfaces.Contains(typeof(IWorkFlowAction)))
                            {
                                AssemblyClassDefinition xcls = new AssemblyClassDefinition();
                                xcls.className = type.Name;
                                xcls.dllname = type.Module.Name;
                                xcls.PackageName = type.FullName;
                                xcls.type = type;
                                xcls.componentType = "IWorkFlowAction";
                                ConfigEditor.WorkFlowActions.Add(xcls);
                            }
                            //-------------------------------------------------------
                            // Get IAppBuilder  Definitions
                            if (type.ImplementedInterfaces.Contains(typeof(IAppBuilder)))
                            {
                                AssemblyClassDefinition xcls = new AssemblyClassDefinition();
                                xcls.className = type.Name;
                                xcls.dllname = type.Module.Name;
                                xcls.PackageName = type.FullName;
                                xcls.type = type;
                                xcls.componentType = "IAppBuilder";
                                ConfigEditor.AppWritersClasses.Add(xcls);
                            }
                            if (type.ImplementedInterfaces.Contains(typeof(IAppComponent)))
                            {
                                AssemblyClassDefinition xcls = new AssemblyClassDefinition();
                                xcls.className = type.Name;
                                xcls.dllname = type.Module.Name;
                                xcls.PackageName = type.FullName;
                                xcls.type = type;
                                xcls.componentType = "IAppComponent";
                                ConfigEditor.AppComponents.Add(xcls);
                            }
                            if (type.ImplementedInterfaces.Contains(typeof(IAppDesigner)))
                            {
                                AssemblyClassDefinition xcls = new AssemblyClassDefinition();
                                xcls.className = type.Name;
                                xcls.dllname = type.Module.Name;
                                xcls.PackageName = type.FullName;
                                xcls.type = type;
                                xcls.componentType = "IAppDesigner";
                                ConfigEditor.AppComponents.Add(xcls);
                            }
                            if (type.ImplementedInterfaces.Contains(typeof(IAppScreen)))
                            {
                                AssemblyClassDefinition xcls = new AssemblyClassDefinition();
                                xcls.className = type.Name;
                                xcls.dllname = type.Module.Name;
                                xcls.PackageName = type.FullName;
                                xcls.type = type;
                                xcls.componentType = "IAppScreen";
                                ConfigEditor.AppComponents.Add(xcls);
                            }

                            //-------------------------------------------------------
                            // Get Reports Implementations Definitions
                            if (type.ImplementedInterfaces.Contains(typeof(IReportDMWriter)))
                            {
                                AssemblyClassDefinition xcls = new AssemblyClassDefinition();
                                xcls.className = type.Name;
                                xcls.dllname = type.Module.Name;
                                xcls.PackageName = type.FullName;
                                xcls.componentType = "IReportDMWriter";
                                ConfigEditor.ReportWritersClasses.Add(xcls);
                            }
                            //-------------------------------------------------------
                            // Get IBranch Definitions
                            if (type.ImplementedInterfaces.Contains(typeof(IBranch)))
                            {

                                AssemblyClassDefinition xcls = new AssemblyClassDefinition();
                                xcls.Methods = new List<MethodsClass>();
                                xcls.className = type.Name;
                                xcls.dllname = type.Module.Name;
                                xcls.PackageName = type.FullName;
                                xcls.componentType = "IBranch";
                                xcls.type = type;
                                //   xcls.RootName = "AI";
                                //   xcls.BranchType = brcls.BranchType;
                                foreach (MethodInfo methods in type.GetMethods()
                                             .Where(m => m.GetCustomAttributes(typeof(CommandAttribute), false).Length > 0)
                                              .ToArray())
                                {

                                    CommandAttribute methodAttribute = methods.GetCustomAttribute<CommandAttribute>();
                                    MethodsClass x = new MethodsClass();
                                    x.Caption = methodAttribute.Caption;
                                    x.Info = methods;
                                    x.Hidden = methodAttribute.Hidden;
                                    x.Click = methodAttribute.Click;
                                    x.DoubleClick = methodAttribute.DoubleClick;
                                    x.iconimage = methodAttribute.iconimage;
                                    xcls.Methods.Add(x);
                                }
                                if (type.ImplementedInterfaces.Contains(typeof(IOrder)))
                                {
                                    try
                                    {
                                        IOrder cls = (IOrder)Activator.CreateInstance(type);
                                        xcls.Order = cls.Order;
                                        cls = null;
                                    }
                                    catch (Exception)
                                    {


                                    }

                                }
                                ConfigEditor.BranchesClasses.Add(xcls);
                            }
                            // --- Get all AI app Interfaces
                            //-----------------------------------------------------
                            if (type.ImplementedInterfaces.Contains(typeof(IAAPP)))
                            {
                                AssemblyClassDefinition xcls = new AssemblyClassDefinition();
                                xcls.Methods = new List<MethodsClass>();
                                xcls.className = type.Name;
                                xcls.dllname = type.Module.Name;
                                xcls.PackageName = type.FullName;
                                xcls.componentType = "IAAPP";
                                foreach (MethodInfo methods in type.GetMethods()
                                             .Where(m => m.GetCustomAttributes(typeof(MLMethod), false).Length > 0)
                                              .ToArray())
                                {

                                    MLMethod methodAttribute = methods.GetCustomAttribute<MLMethod>();
                                    MethodsClass x = new MethodsClass();
                                    x.Caption = methodAttribute.Caption;
                                    x.Info = methods;
                                    x.Hidden = methodAttribute.Hidden;
                                    x.Click = methodAttribute.Click;
                                    x.type = typeof(MLMethod);
                                    x.DoubleClick = methodAttribute.DoubleClick;

                                    xcls.Methods.Add(x);
                                }
                                foreach (MethodInfo methods in type.GetMethods()
                                            .Where(m => m.GetCustomAttributes(typeof(MLPredict), false).Length > 0)
                                             .ToArray())
                                {

                                    MLPredict methodAttribute = methods.GetCustomAttribute<MLPredict>();
                                    MethodsClass x = new MethodsClass();
                                    x.Caption = methodAttribute.Caption;
                                    x.Info = methods;
                                    x.Hidden = methodAttribute.Hidden;
                                    x.Click = methodAttribute.Click;
                                    x.type = typeof(MLPredict);
                                    x.DoubleClick = methodAttribute.DoubleClick;

                                    xcls.Methods.Add(x);
                                }
                                foreach (MethodInfo methods in type.GetMethods()
                                         .Where(m => m.GetCustomAttributes(typeof(MLLoadModule), false).Length > 0)
                                          .ToArray())
                                {

                                    MLLoadModule methodAttribute = methods.GetCustomAttribute<MLLoadModule>();
                                    MethodsClass x = new MethodsClass();
                                    x.Caption = methodAttribute.Caption;
                                    x.Info = methods;
                                    x.Hidden = methodAttribute.Hidden;
                                    x.Click = methodAttribute.Click;
                                    x.type = typeof(MLLoadModule);
                                    x.DoubleClick = methodAttribute.DoubleClick;
                                    xcls.Methods.Add(x);
                                }
                                foreach (MethodInfo methods in type.GetMethods()
                                      .Where(m => m.GetCustomAttributes(typeof(MLEval), false).Length > 0)
                                       .ToArray())
                                {

                                    MLEval methodAttribute = methods.GetCustomAttribute<MLEval>();
                                    MethodsClass x = new MethodsClass();
                                    x.Caption = methodAttribute.Caption;
                                    x.Info = methods;
                                    x.Hidden = methodAttribute.Hidden;
                                    x.Click = methodAttribute.Click;
                                    x.type = typeof(MLEval);
                                    x.DoubleClick = methodAttribute.DoubleClick;
                                    xcls.Methods.Add(x);
                                }
                                foreach (MethodInfo methods in type.GetMethods()
                                     .Where(m => m.GetCustomAttributes(typeof(MLLoadData), false).Length > 0)
                                      .ToArray())
                                {

                                    MLLoadData methodAttribute = methods.GetCustomAttribute<MLLoadData>();
                                    MethodsClass x = new MethodsClass();
                                    x.Caption = methodAttribute.Caption;
                                    x.Info = methods;
                                    x.Hidden = methodAttribute.Hidden;
                                    x.Click = methodAttribute.Click;
                                    x.type = typeof(MLLoadData);
                                    x.DoubleClick = methodAttribute.DoubleClick;
                                    xcls.Methods.Add(x);
                                }
                                if (type.ImplementedInterfaces.Contains(typeof(IOrder)))
                                {
                                    try
                                    {
                                        IOrder cls = (IOrder)Activator.CreateInstance(type);
                                        xcls.Order = cls.Order;
                                        cls = null;
                                    }
                                    catch (Exception)
                                    {


                                    }

                                }

                                ConfigEditor.BranchesClasses.Add(xcls);

                            }
                            if (type.ImplementedInterfaces.Contains(typeof(IFunctionExtension)))
                            {

                                AssemblyClassDefinition xcls = new AssemblyClassDefinition();
                                xcls.Methods = new List<MethodsClass>();
                                xcls.className = type.Name;
                                xcls.dllname = type.Module.Name;
                                xcls.PackageName = type.FullName;
                                xcls.componentType = "IFunctionExtension";
                                xcls.type = type;

                                xcls.classProperties = (ClassProperties)type.GetCustomAttribute(typeof(ClassProperties), false);
                                if (xcls.classProperties != null)
                                {
                                    xcls.RootName = "IFunctionExtension";
                                }

                                //   xcls.RootName = "AI";
                                //   xcls.BranchType = brcls.BranchType;
                                foreach (MethodInfo methods in type.GetMethods()
                                             .Where(m => m.GetCustomAttributes(typeof(CommandAttribute), false).Length > 0)
                                              .ToArray())
                                {

                                    CommandAttribute methodAttribute = methods.GetCustomAttribute<CommandAttribute>();
                                    MethodsClass x = new MethodsClass();
                                    x.Caption = methodAttribute.Caption;
                                    x.Name = methodAttribute.Name;
                                    x.Info = methods;
                                    x.Hidden = methodAttribute.Hidden;
                                    x.Click = methodAttribute.Click;
                                    x.DoubleClick = methodAttribute.DoubleClick;
                                    x.iconimage = methodAttribute.iconimage;
                                    x.PointType = methodAttribute.PointType;
                                    xcls.Methods.Add(x);
                                }
                                if (type.ImplementedInterfaces.Contains(typeof(IOrder)))
                                {
                                    try
                                    {
                                        IOrder cls = (IOrder)Activator.CreateInstance(type);
                                        xcls.Order = cls.Order;
                                        cls = null;
                                    }
                                    catch (Exception)
                                    {


                                    }

                                }
                                ConfigEditor.GlobalFunctions.Add(xcls);
                            }
                        }
                    }

                }
                catch (Exception ex)
                {

                    string mes = "";
                   // AddLogMessage(ex.Message, "Could not exported  types" + mes, DateTime.Now, -1, mes, Errors.Failed);
                };

                return true;
            }
            catch (Exception ex)
            {
                string mes = "";
               // AddLogMessage(ex.Message, "Could not scan assembly " + mes, DateTime.Now, -1, mes, Errors.Failed);
                return false;
            };
        }
        #endregion "Class Extractors"
        #region "Helpers"
        public object CreateInstanceFromString(string typeName, params object[] args)
        {
            object instance = null;
            Type type = null;

            try
            {
                type = GetType(typeName);
                if (type == null)
                    return null;

                instance = Activator.CreateInstance(type, args);
            }
            catch
            {
                return null;
            }

            return instance;
        }
        public object GetInstance(string strFullyQualifiedName)
        {
            Type type = GetType(strFullyQualifiedName);
            if (type != null)
                return Activator.CreateInstance(type);
          
            return null;
        }
        private readonly object _resolutionLock = new object();
        public Type GetType(string strFullyQualifiedName)
        {


            //-----------------------------------------------
            Type type = Type.GetType(strFullyQualifiedName);
            if (type != null)
                return type;
            foreach (assemblies_rep asm in Assemblies)
            {
               type = asm.DllLib.GetType(strFullyQualifiedName);
                //var assembly = Assembly.Load(asm.DllLib.GetName());
                //type = assembly.GetType(strFullyQualifiedName);
                if (type != null)
                    return type;
            }
            Assembly rootassembly = Assembly.GetEntryAssembly();
            var assemblies = rootassembly.GetReferencedAssemblies().Where(x => x.FullName.Contains("DataManagmentEngine"));
            foreach (AssemblyName item in assemblies)
            {
                var assembly = Assembly.Load(item);
                type = assembly.GetType(strFullyQualifiedName);
                // type = asm.GetType(strFullyQualifiedName);
                if (type != null)
                    return type;
            }
            AppDomain.CurrentDomain.GetAssemblies();
             var assem = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly item in assem)
            {
              //  var assembly = Assembly.Load(item);
                type = item.GetType(strFullyQualifiedName);
                // type = asm.GetType(strFullyQualifiedName);
                if (type != null)
                    return type;
            }
            return null;
        }
        private Assembly Context_Resolving(AssemblyLoadContext context, AssemblyName assemblyName)
        {
            var expectedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, assemblyName.Name + ".dll");
            return context.LoadFromAssemblyPath(expectedPath);
        }
        public bool RunMethod(object ObjInstance, string FullClassName, string MethodName)
        {

            try
            {
                AssemblyClassDefinition cls = ConfigEditor.BranchesClasses.Where(x => x.className == FullClassName).FirstOrDefault();
                dynamic obj = GetInstance(cls.PackageName);
                obj = ObjInstance;
                MethodInfo method = cls.Methods.Where(x => x.Caption == MethodName).FirstOrDefault().Info;
                method.Invoke(ObjInstance, null);
                return true;
                //    AddLogMessage("Success", "Running method", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception)
            {
                string mes = "Could not Run Method " + MethodName;
                //  AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
                return false;
            };

        }
        #endregion "Helpers"
        #region "Connection Drivers Loaders"
        public void CheckDriverAlreadyExistinList()
        {

            foreach (ConnectionDriversConfig dr in DataDriversConfig)
            {
                ConnectionDriversConfig founddr = ConfigEditor.DataDriversClasses.Where(c => c.PackageName == dr.PackageName && c.version == dr.version).FirstOrDefault();
                if (founddr == null)
                {
                    ConfigEditor.DataDriversClasses.Add(dr);
                }
            }
        }
        private bool GetADOTypeDrivers(Assembly asm)
        {
            ConnectionDriversConfig driversConfig = new ConnectionDriversConfig();

            bool retval;
            string[] p;
            Type[] t;
            if (asm.GetType() == null)
            {
                t = asm.GetExportedTypes();
            }
            else
                t = asm.GetTypes();
            foreach (var mytype in t)
            {
                try
                {
                    //if (mytype.FullName.Contains("DataAdapter"))
                    //{
                    //    Debug.WriteLine("found");
                    //}
                    TypeInfo type = mytype.GetTypeInfo();
                    p = asm.FullName.Split(new char[] { ',' });
                    p[1] = p[1].Substring(p[1].IndexOf("=") + 1);

                    driversConfig = DataDriversConfig.Where(c => c.DriverClass == p[0]).FirstOrDefault();
                    bool recexist = false;
                    //DbTransaction uc = (DbTransaction)Activator.CreateInstance(type);
                    //if (uc != null)
                    //{
                    if (driversConfig == null)
                    {
                        driversConfig = new ConnectionDriversConfig();
                        recexist = false;
                    }
                    else
                    {
                        recexist = true;
                    }
                    //-------------------------------------------------------
                    // Get DataBase Drivers
                    if (type.ImplementedInterfaces.Contains(typeof(IDbDataAdapter)))
                    {
                        //Logger.WriteLog($" NameSpaces {type.Namespace} ");
                        //IDataAdapter uc = (IDataAdapter)Activator.CreateInstance(type);

                        driversConfig.version = p[1];
                        driversConfig.AdapterType = type.FullName;
                        driversConfig.PackageName = p[0];
                        driversConfig.DriverClass = p[0];
                        driversConfig.dllname = type.Module.Name;
                        driversConfig.ADOType = true;
                        if (recexist == false)
                        {
                            DataDriversConfig.Add(driversConfig);
                        }




                    }
                    if (type.IsSubclassOf(typeof(DbCommandBuilder)))
                    {

                        driversConfig.CommandBuilderType = type.FullName;
                        driversConfig.version = p[1];
                        driversConfig.PackageName = p[0];
                        driversConfig.DriverClass = p[0];
                        driversConfig.dllname = type.Module.Name;
                        if (recexist == false)
                        {
                            DataDriversConfig.Add(driversConfig);
                        }

                        //  }
                    }
                    if (type.IsSubclassOf(typeof(DbConnection)))
                    {

                        driversConfig.DbConnectionType = type.FullName;
                        driversConfig.PackageName = p[0];
                        driversConfig.DriverClass = p[0];
                        driversConfig.version = p[1];
                        driversConfig.dllname = type.Module.Name;
                        if (recexist == false)
                        {
                            DataDriversConfig.Add(driversConfig);
                        }

                        //}
                    }
                    if (type.IsSubclassOf(typeof(DbTransaction)))
                    {


                        driversConfig.PackageName = p[0];
                        driversConfig.DriverClass = p[0];
                        driversConfig.version = p[1];
                        driversConfig.dllname = type.Module.Name;
                        driversConfig.DbTransactionType = type.FullName;
                        if (recexist == false)
                        {
                            DataDriversConfig.Add(driversConfig);
                        }

                        // }
                    }



                    //-----------------------------------------------------------
                }
                catch (Exception ex)
                {

                    Logger.WriteLog($"error loading Database drivers {ex.Message} ");
                    return false;
                }

            }
            if (driversConfig.dllname == null)
            {
                p = asm.FullName.Split(new char[] { ',' });
                p[1] = p[1].Substring(p[1].IndexOf("=") + 1);
                //---------------------------------------------------------
                // Get NoSQL Drivers 
                //  bool driverfound = false;
                //  bool recexist = false;
                driversConfig = DataDriversConfig.Where(c => c.DriverClass == p[0]).FirstOrDefault();
                if (driversConfig == null)
                {
                    driversConfig = new ConnectionDriversConfig();
                    // recexist = false;
                }
                //else
                //{
                //   // recexist = true;
                //}
                driversConfig.version = p[1];
                driversConfig.PackageName = p[0];
                driversConfig.DriverClass = p[0];
                driversConfig.dllname = asm.ManifestModule.Name;

                if (p[0] == "System.Data.SqlClient")
                {
                    driversConfig.version = p[1];
                    driversConfig.AdapterType = p[0] + "." + "SqlDataAdapter";
                    driversConfig.DbConnectionType = p[0] + "." + "SqlConnection";
                    driversConfig.CommandBuilderType = p[0] + "." + "SqlCommandBuilder";
                    driversConfig.DbTransactionType = p[0] + "." + "SqlTransaction";
                    driversConfig.PackageName = p[0];
                    driversConfig.DriverClass = p[0];
                    driversConfig.ADOType = true;
                    DataDriversConfig.Add(driversConfig);
                }
            }
            if (driversConfig.dllname == null)
            {
                retval = false;
            }
            else
                retval = true;
            return retval;
        }
        private void GetNonADODrivers()
        {
            ConnectionDriversConfig driversConfig = new ConnectionDriversConfig();
            try
            {

                foreach (ConnectionDriversConfig item in ConfigEditor.DriverDefinitionsConfig)
                {

                    driversConfig = DataDriversConfig.Where(c => c.PackageName == item.PackageName).FirstOrDefault();
                    if (driversConfig == null)
                    {
                        driversConfig = new ConnectionDriversConfig();
                        driversConfig.version = item.version;
                        driversConfig.PackageName = item.PackageName;
                        driversConfig.DriverClass = item.DriverClass;
                        driversConfig.dllname = item.dllname;
                        driversConfig.parameter1 = item.parameter1;
                        driversConfig.parameter2 = item.parameter2;
                        driversConfig.parameter3 = item.parameter3;

                        DataDriversConfig.Add(driversConfig);
                    }



                }



                //-----------------------------------------------------------
            }
            catch (Exception ex)
            {

                Logger.WriteLog($"error in creating addin {ex.Message} ");

            }



        }
        public List<ConnectionDriversConfig> GetDrivers(Assembly asm)
        {
            // int cnt = 1;


            try
            {
                if (asm.GetType() != null)
                {
                    GetADOTypeDrivers(asm);
                }

            }
            catch (Exception ex1)
            {

                Logger.WriteLog($"error Cannot find defined types from assembly {ex1.Message} ");
                //try
                //{

                //    GetNonADODrivers(asm);

                //}
                //catch (Exception ex2)
                //{

                //    Logger.WriteLog($"error Cannot find exported types from assembly {ex2.Message} ");
                //}
            }


            return DataDriversConfig;


        }
        public bool AddEngineDefaultDrivers()
        {

            try
            {
                ConnectionDriversConfig DataviewDriver = new ConnectionDriversConfig();
                DataviewDriver.AdapterType = "DEFAULT";
                DataviewDriver.dllname = "DataManagmentEngine";
                DataviewDriver.PackageName = "DataViewReader";
                DataviewDriver.DriverClass = "DataViewReader";
                DataviewDriver.version = "1";
                DataDriversConfig.Add(DataviewDriver);
                //----------------- 
                // Get File extensions
                //--------------
                List<AssemblyClassDefinition> cls = DataSourcesClasses.Where(o => o.classProperties != null).ToList().Where(p => p.classProperties.Category == DatasourceCategory.FILE).ToList();
                foreach (AssemblyClassDefinition item in cls)
                {

                    foreach (string extension in item.classProperties.FileType.Split(',').ToList())
                    {
                        ConnectionDriversConfig TXTFileDriver = new ConnectionDriversConfig();
                        TXTFileDriver.AdapterType = "DEFAULT";
                        TXTFileDriver.dllname = "DataManagmentEngine";
                        //if (DataDriversConfig.Where(i => i.PackageName.Contains(extension.ToLower() + "FileReader")).Any())
                        //{
                        //    TXTFileDriver.version = DataDriversConfig.Where(i => i.PackageName.Contains(extension.ToLower() + "FileReader")).Max(i => i.version) + 1;
                        //}
                        TXTFileDriver.PackageName = item.className;//   extension.ToLower() + "FileReader";
                        TXTFileDriver.DriverClass = item.className;
                        TXTFileDriver.classHandler = item.className;
                        TXTFileDriver.iconname = extension + ".ico";
                        TXTFileDriver.extensionstoHandle = item.classProperties.FileType;
                        TXTFileDriver.DatasourceCategory = DatasourceCategory.FILE;
                        TXTFileDriver.version = "1";
                        DataDriversConfig.Add(TXTFileDriver);
                    }

                }


                return true;
            }
            catch (Exception ex)
            {
                string mes = "";
                //AddLogMessage(ex.Message, "Could not Add Driver" + mes, DateTime.Now, -1, mes, Errors.Failed);
                return false;
            };
        }
        #endregion
    }

   
}
