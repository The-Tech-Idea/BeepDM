﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.Addin;
using TheTechIdea.DataManagment_Engine.AI;
using TheTechIdea.DataManagment_Engine.AppBuilder;
using TheTechIdea.DataManagment_Engine.Report;
using TheTechIdea.DataManagment_Engine.Vis;
using TheTechIdea.DataManagment_Engine.Workflow;
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
        public List<assemblies_rep> Assemblies { get; set; } = new List<assemblies_rep>();
        public List<IDM_Addin> AddIns { get; set; } = new List<IDM_Addin>();
        public List<AssemblyClassDefinition> DataSourcesClasses { get; set; } = new List<AssemblyClassDefinition>();
        private List<ConnectionDriversConfig> DataDriversConfig = new List<ConnectionDriversConfig>();

        public AssemblyHandlerCore()
        {

            DataSourcesClasses = new List<AssemblyClassDefinition>();
        }
        static Assembly LoadPlugin(string relativePath)
        {
            // Navigate up to the solution root
          
            string pluginLocation = Path.GetFullPath(Path.Combine(relativePath, relativePath.Replace('\\', Path.DirectorySeparatorChar)));
            Console.WriteLine($"Loading commands from: {pluginLocation}");
            PluginLoadContext loadContext = new PluginLoadContext(pluginLocation);
            return loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(pluginLocation)));
        }
        private string LoadAssembly(string path, FolderFileTypes fileTypes)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
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
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    res = "The Assembly has already been loaded" + loadEx.Message;
                } // The Assembly has already been loaded.
                catch (BadImageFormatException imgEx)
                {
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    res = imgEx.Message;
                }
                catch (Exception ex)
                {
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    res = ex.Message;
                }
            }


            DMEEditor.ErrorObject.Message = res;
            return res;
        }
        #region "Loaders"
        public IErrorsInfo GetBuiltinClasses()
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
           
            // look through assembly list
            Assembly currentAssem = Assembly.GetExecutingAssembly();
            Assembly rootassembly = Assembly.GetEntryAssembly();
          
            try
            {
                ScanAssembly(currentAssem);
                DMEEditor.Utilfunction.FunctionHierarchy = GetAddinObjects(currentAssem);
            }
            catch (Exception ex)
            {

                DMEEditor.Logger.WriteLog($"error loading current assembly {ex.Message} ");
            }

            try
            {
                ScanAssembly(rootassembly);
                DMEEditor.Utilfunction.FunctionHierarchy = GetAddinObjects(rootassembly);

            }
            catch (Exception ex)
            {

                DMEEditor.Logger.WriteLog($"error loading current assembly {ex.Message} ");
            }
            var assemblies = rootassembly.GetReferencedAssemblies();
            try
            {
                foreach (AssemblyName item in assemblies)
                {
                    var assembly = Assembly.Load(item);
                    ScanAssembly(assembly);
                    DMEEditor.Utilfunction.FunctionHierarchy = GetAddinObjects(assembly);
                }

            }
            catch (Exception ex)
            {

                DMEEditor.Logger.WriteLog($"error loading current assembly {ex.Message} ");
            }
         
            return DMEEditor.ErrorObject;

        }
        /// <summary>
        ///     This Method will go through all Folders ProjectClass,OtherDLL,Addin, Drivers and load DLL
        /// </summary>
        /// <returns></returns>
        public IErrorsInfo LoadAllAssembly()
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            string res;
            DMEEditor.Utilfunction.FunctionHierarchy = new List<ParentChildObject>();
            DMEEditor.Utilfunction.Namespacelist = new List<string>();
            DMEEditor.Utilfunction.Classlist = new List<string>();
            DataDriversConfig = new List<ConnectionDriversConfig>();
            AddEngineDefaultDrivers();
            GetNonADODrivers();
            GetBuiltinClasses();
            try
            {
                foreach (string p in DMEEditor.ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.OtherDLL).Select(x => x.FolderPath))
                {
                    try
                    {
                        LoadAssembly(p, FolderFileTypes.OtherDLL);
                    }
                    catch (FileLoadException loadEx)
                    {
                        DMEEditor.ErrorObject.Flag = Errors.Failed;
                        res = "The Assembly has already been loaded" + loadEx.Message;
                        // MessageBox.Show("The Assembly has already been loaded" + loadEx.Message, "Simple ODM", MessageBoxButtons.OK);
                    } // The Assembly has already been loaded.
                    catch (BadImageFormatException imgEx)
                    {
                        DMEEditor.ErrorObject.Flag = Errors.Failed;
                        // MessageBox.Show(imgEx.Message, "Simple ODM", MessageBoxButtons.OK);  // If a BadImageFormatException exception is thrown, the file is not an assembly.
                        res = imgEx.Message;
                    }
                    catch (Exception ex)
                    {
                        DMEEditor.ErrorObject.Flag = Errors.Failed;
                        // MessageBox.Show(ex.Message, "Simple ODM", MessageBoxButtons.OK);  // If a BadImageFormatException exception is thrown, the file is not an assembly
                        res = ex.Message;
                    }

                }
                foreach (string p in DMEEditor.ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.ConnectionDriver).Select(x => x.FolderPath))
                {
                    try
                    {
                        LoadAssembly(p, FolderFileTypes.ConnectionDriver);


                    }
                    catch (FileLoadException loadEx)
                    {
                        DMEEditor.ErrorObject.Flag = Errors.Failed;
                        res = "The Assembly has already been loaded" + loadEx.Message;
                        // MessageBox.Show("The Assembly has already been loaded" + loadEx.Message, "Simple ODM", MessageBoxButtons.OK);
                    } // The Assembly has already been loaded.
                    catch (BadImageFormatException imgEx)
                    {
                        DMEEditor.ErrorObject.Flag = Errors.Failed;
                        // MessageBox.Show(imgEx.Message, "Simple ODM", MessageBoxButtons.OK);  // If a BadImageFormatException exception is thrown, the file is not an assembly.
                        res = imgEx.Message;
                    }
                    catch (Exception ex)
                    {
                        DMEEditor.ErrorObject.Flag = Errors.Failed;
                        // MessageBox.Show(ex.Message, "Simple ODM", MessageBoxButtons.OK);  // If a BadImageFormatException exception is thrown, the file is not an assembly
                        res = ex.Message;
                    }

                }
                foreach (string p in DMEEditor.ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.ProjectClass).Select(x => x.FolderPath))
                {
                    try
                    {
                        LoadAssembly(p, FolderFileTypes.ProjectClass);
                    }
                    catch (FileLoadException loadEx)
                    {
                        DMEEditor.ErrorObject.Flag = Errors.Failed;
                        res = "The Assembly has already been loaded" + loadEx.Message;
                        // MessageBox.Show("The Assembly has already been loaded" + loadEx.Message, "Simple ODM", MessageBoxButtons.OK);
                    } // The Assembly has already been loaded.
                    catch (BadImageFormatException imgEx)
                    {
                        DMEEditor.ErrorObject.Flag = Errors.Failed;
                        // MessageBox.Show(imgEx.Message, "Simple ODM", MessageBoxButtons.OK);  // If a BadImageFormatException exception is thrown, the file is not an assembly.
                        res = imgEx.Message;
                    }
                    catch (Exception ex)
                    {
                        DMEEditor.ErrorObject.Flag = Errors.Failed;
                        // MessageBox.Show(ex.Message, "Simple ODM", MessageBoxButtons.OK);  // If a BadImageFormatException exception is thrown, the file is not an assembly
                        res = ex.Message;
                    }

                }

                // Get Driver from Loaded Assembly
                foreach (assemblies_rep item in Assemblies)
                {
                    GetDrivers(item.DllLib);
                }
                CheckDriverAlreadyExistinList();

                foreach (string p in DMEEditor.ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.Addin).Select(x => x.FolderPath))
                {
                    try
                    {
                        LoadAssembly(p, FolderFileTypes.Addin);

                    }
                    catch (FileLoadException loadEx)
                    {
                        DMEEditor.ErrorObject.Flag = Errors.Failed;
                        res = "The Assembly has already been loaded" + loadEx.Message;
                        // MessageBox.Show("The Assembly has already been loaded" + loadEx.Message, "Simple ODM", MessageBoxButtons.OK);
                    } // The Assembly has already been loaded.
                    catch (BadImageFormatException imgEx)
                    {
                        DMEEditor.ErrorObject.Flag = Errors.Failed;
                        // MessageBox.Show(imgEx.Message, "Simple ODM", MessageBoxButtons.OK);  // If a BadImageFormatException exception is thrown, the file is not an assembly.
                        res = imgEx.Message;
                    }
                    catch (Exception ex)
                    {
                        DMEEditor.ErrorObject.Flag = Errors.Failed;
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
                        DMEEditor.Utilfunction.FunctionHierarchy = GetAddinObjects(s.DllLib);

                    }

                    catch (Exception ex)
                    {
                        DMEEditor.ErrorObject.Flag = Errors.Failed;
                        res = ex.Message;
                    }

                }

                //------------------------------
                
            }
            catch (System.Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Ex = ex;
                DMEEditor.Logger.WriteLog($"Error Loading Addin Assemblies ({ex.Message})");
            }
            return DMEEditor.ErrorObject;
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
                if (DMEEditor.Utilfunction.FunctionHierarchy.Where(f => f.id == p && f.ObjType == Objt).Count() == 0)
                {
                    a = new ParentChildObject() { id = p, ParentID = null, ObjType = Objt, AddinName = Name, Description = Descr };
                    DMEEditor.Utilfunction.FunctionHierarchy.Add(a);

                }
                else
                {
                    a = DMEEditor.Utilfunction.FunctionHierarchy.Where(f => f.id == p && f.ParentID == null && f.ObjType == Objt).FirstOrDefault();

                }

            }
            else
            {
                if (DMEEditor.Utilfunction.FunctionHierarchy.Where(f => f.id == p && f.ParentID == parentid && f.ObjType == Objt).Count() == 0)
                {
                    a = new ParentChildObject() { id = p, ParentID = parentid, ObjType = Objt, AddinName = Name, Description = Descr };
                    DMEEditor.Utilfunction.FunctionHierarchy.Add(a);

                }
                else
                {
                    a = DMEEditor.Utilfunction.FunctionHierarchy.Where(f => f.id == p && f.ParentID == parentid && f.ObjType == Objt).FirstOrDefault();

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
                                                    if (DMEEditor.ConfigEditor.AddinTreeStructure.Where(x => x.className == type.Name).Any() == false)
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
                                                            DMEEditor.ConfigEditor.AddinTreeStructure.Add(xcls);

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
                                                DMEEditor.AddLogMessage(ex.Message, "Could" + mes, DateTime.Now, -1, mes, Errors.Failed);
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

                        DMEEditor.Logger.WriteLog($"error in creating addin {ex.Message} ");
                    }
                }

            }

            DMEEditor.ConfigEditor.SaveAddinTreeStructure();
            return DMEEditor.Utilfunction.FunctionHierarchy;
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
                            if (type.ImplementedInterfaces.Where(i => i.Name == "IDataSource").Any() )
                            {
                                AssemblyClassDefinition xcls = new AssemblyClassDefinition();
                                xcls.className = type.Name;
                                xcls.dllname = type.Module.Name;
                                xcls.PackageName = type.FullName;
                                DataSourcesClasses.Add(xcls);
                                DMEEditor.ConfigEditor.DataSourcesClasses.Add(xcls);
                            }
                            //-------------------------------------------------------
                            // Get WorkFlow Definitions
                            if (type.ImplementedInterfaces.Where(i => i.Name == "IWorkFlowAction").Any() )
                            {
                                AssemblyClassDefinition xcls = new AssemblyClassDefinition();
                                xcls.className = type.Name;
                                xcls.dllname = type.Module.Name;
                                xcls.PackageName = type.FullName;
                                DMEEditor.WorkFlowEditor.WorkFlowActions.Add(xcls);
                            }
                            //-------------------------------------------------------
                            // Get IAppBuilder  Definitions
                            if (type.ImplementedInterfaces.Where(i => i.Name == "IAppBuilder").Any() )
                            {
                                AssemblyClassDefinition xcls = new AssemblyClassDefinition();
                                xcls.className = type.Name;
                                xcls.dllname = type.Module.Name;
                                xcls.PackageName = type.FullName;
                                DMEEditor.ConfigEditor.AppWritersClasses.Add(xcls);
                            }
                            //-------------------------------------------------------
                            // Get Reports Implementations Definitions
                            if (type.ImplementedInterfaces.Where(i => i.Name == "IReportDMWriter").Any())
                            {
                                AssemblyClassDefinition xcls = new AssemblyClassDefinition();
                                xcls.className = type.Name;
                                xcls.dllname = type.Module.Name;
                                xcls.PackageName = type.FullName;
                                DMEEditor.ConfigEditor.ReportWritersClasses.Add(xcls);
                            }
                            //-------------------------------------------------------
                            // Get IBranch Definitions //IAddinVisSchema
                            if (type.ImplementedInterfaces.Where(i=>i.Name=="IBranch").Any() )
                            {

                                AssemblyClassDefinition xcls = new AssemblyClassDefinition();
                                xcls.Methods = new List<MethodsClass>();
                                xcls.className = type.Name;
                                xcls.dllname = type.Module.Name;
                                xcls.PackageName = type.FullName;
                                //   xcls.RootName = brcls.BranchClass;
                                xcls.type = type;
                                //   xcls.RootName = "AI";
                                //   xcls.BranchType = brcls.BranchType;
                                foreach (MethodInfo methods in type.GetMethods()
                                             .Where(m => m.GetCustomAttributes(typeof(BranchDelegate), false).Length > 0)
                                              .ToArray())
                                {

                                    BranchDelegate methodAttribute = methods.GetCustomAttribute<BranchDelegate>();
                                    MethodsClass x = new MethodsClass();
                                    x.Caption = methodAttribute.Caption;
                                    x.Info = methods;
                                    x.Hidden = methodAttribute.Hidden;
                                    x.Click = methodAttribute.Click;
                                    x.DoubleClick = methodAttribute.DoubleClick;
                                    xcls.Methods.Add(x);
                                }
                                if (type.ImplementedInterfaces.Where(i => i.Name == "IOrder").Any() )
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
                                DMEEditor.ConfigEditor.BranchesClasses.Add(xcls);
                            }
                            // --- Get all AI app Interfaces
                            //-----------------------------------------------------
                            if (type.ImplementedInterfaces.Where(i => i.Name == "IAAPP").Any() )
                            {
                                AssemblyClassDefinition xcls = new AssemblyClassDefinition();
                                xcls.Methods = new List<MethodsClass>();
                                xcls.className = type.Name;
                                xcls.dllname = type.Module.Name;
                                xcls.PackageName = type.FullName;

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
                                if (type.ImplementedInterfaces.Where(i => i.Name == "IOrder").Any())
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
                                DMEEditor.ConfigEditor.BranchesClasses.Add(xcls);
                            }
                        }
                    }

                }
                catch (Exception ex)
                {

                    string mes = "";
                    DMEEditor.AddLogMessage(ex.Message, "Could not exported  types" + mes, DateTime.Now, -1, mes, Errors.Failed);
                };

                return true;
            }
            catch (Exception ex)
            {
                string mes = "";
                DMEEditor.AddLogMessage(ex.Message, "Could not scan assembly " + mes, DateTime.Now, -1, mes, Errors.Failed);
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
        public Type GetType(string strFullyQualifiedName)
        {
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
           

            return null;
        }
        public bool RunMethod(object ObjInstance, string FullClassName, string MethodName)
        {

            try
            {
                AssemblyClassDefinition cls = DMEEditor.ConfigEditor.BranchesClasses.Where(x => x.className == FullClassName).FirstOrDefault();
                dynamic obj = GetInstance(cls.PackageName);
                obj = ObjInstance;
                MethodInfo method = cls.Methods.Where(x => x.Caption == MethodName).FirstOrDefault().Info;
                method.Invoke(ObjInstance, null);
                return true;
                //    DMEEditor.AddLogMessage("Success", "Running method", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception)
            {
                string mes = "Could not Run Method " + MethodName;
                //  DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
                return false;
            };

        }
        #endregion "Helpers"
        #region "Connection Drivers Loaders"
        public void CheckDriverAlreadyExistinList()
        {

            foreach (ConnectionDriversConfig dr in DataDriversConfig)
            {
                ConnectionDriversConfig founddr = DMEEditor.ConfigEditor.DataDriversClasses.Where(c => c.PackageName == dr.PackageName && c.version == dr.version).FirstOrDefault();
                if (founddr == null)
                {
                    DMEEditor.ConfigEditor.DataDriversClasses.Add(dr);
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

                    DMEEditor.Logger.WriteLog($"error loading Database drivers {ex.Message} ");
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

                foreach (ConnectionDriversConfig item in DMEEditor.ConfigEditor.DriverDefinitionsConfig)
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

                DMEEditor.Logger.WriteLog($"error in creating addin {ex.Message} ");

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

                DMEEditor.Logger.WriteLog($"error Cannot find defined types from assembly {ex1.Message} ");
                //try
                //{

                //    GetNonADODrivers(asm);

                //}
                //catch (Exception ex2)
                //{

                //    DMEEditor.Logger.WriteLog($"error Cannot find exported types from assembly {ex2.Message} ");
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
                ConnectionDriversConfig TXTFileDriver = new ConnectionDriversConfig();
                TXTFileDriver.AdapterType = "DEFAULT";
                TXTFileDriver.dllname = "DataManagmentEngine";
                TXTFileDriver.PackageName = "FileReader";
                TXTFileDriver.DriverClass = "FileReader";
                TXTFileDriver.version = "1";
                DataDriversConfig.Add(TXTFileDriver);
                ConnectionDriversConfig JSONFileDriver = new ConnectionDriversConfig();
                JSONFileDriver.AdapterType = "DEFAULT";
                JSONFileDriver.dllname = "DataManagmentEngine";
                JSONFileDriver.PackageName = "JSONFileReader";
                JSONFileDriver.DriverClass = "JSONFileReader";
                JSONFileDriver.version = "1";
                DataDriversConfig.Add(JSONFileDriver);
                ConnectionDriversConfig WebAPIDriver = new ConnectionDriversConfig();
                WebAPIDriver.AdapterType = "DEFAULT";
                WebAPIDriver.dllname = "DataManagmentEngine";
                WebAPIDriver.PackageName = "WebApiReader";
                WebAPIDriver.DriverClass = "WebApiReader";
                WebAPIDriver.version = "1";
                DataDriversConfig.Add(WebAPIDriver);

                return true;
            }
            catch (Exception ex)
            {
                string mes = "";
                DMEEditor.AddLogMessage(ex.Message, "Could not Add Driver" + mes, DateTime.Now, -1, mes, Errors.Failed);
                return false;
            };
        }
        #endregion
    }
}