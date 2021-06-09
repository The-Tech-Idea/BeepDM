//using Microsoft.CodeAnalysis;
//using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.Addin;
using TheTechIdea.DataManagment_Engine.AI;
using TheTechIdea.DataManagment_Engine.AppBuilder;
using TheTechIdea.DataManagment_Engine.ConfigUtil;
using TheTechIdea.DataManagment_Engine.Report;
using TheTechIdea.DataManagment_Engine.Vis;
using TheTechIdea.DataManagment_Engine.Workflow;
using TheTechIdea.Logger;
using TheTechIdea.Util;
using TypeInfo = System.Reflection.TypeInfo;

namespace TheTechIdea.Tools
{
    public class AssemblyHandler : IAssemblyHandler
    {
        ParentChildObject a;
        public AppDomain CurrentDomain { get; set; }

        private string Name { get; set; }
        private string Descr { get; set; }
        public IDMEEditor DMEEditor { get; set; }
        public List<assemblies_rep> Assemblies { get; set; } = new List<assemblies_rep>();
        public List<IDM_Addin> AddIns { get; set; } = new List<IDM_Addin>();
        public List<AssemblyClassDefinition> DataSourcesClasses { get; set; } = new List<AssemblyClassDefinition>();
        private List<ConnectionDriversConfig> DataDriversConfig = new List<ConnectionDriversConfig>();

        public AssemblyHandler(IDMEEditor pDMEEditor)
        {

            DMEEditor = pDMEEditor;
            CurrentDomain = AppDomain.CurrentDomain;
            DataSourcesClasses = new List<AssemblyClassDefinition>();
            CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
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

             //   DMEEditor.Logger.WriteLog($"error loading current assembly {ex.Message} ");
            }

            try
            {
                ScanAssembly(rootassembly);
                DMEEditor.Utilfunction.FunctionHierarchy = GetAddinObjects(rootassembly);

            }
            catch (Exception ex)
            {

               // DMEEditor.Logger.WriteLog($"error loading current assembly {ex.Message} ");
            }
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(x => x.FullName.Contains("DataManagmentEngine"));
            try
            {
                foreach (Assembly item in assemblies)
                {
                    ScanAssembly(item);
                    DMEEditor.Utilfunction.FunctionHierarchy = GetAddinObjects(item);
                }

            }
            catch (Exception ex)
            {

                //DMEEditor.Logger.WriteLog($"error loading current assembly {ex.Message} ");
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
           
            GetNonADODrivers();
            GetBuiltinClasses();
          
         
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
            // Get Driver from Loaded Assembly
                foreach (assemblies_rep item in Assemblies)
                {
                    DMEEditor.AddLogMessage("Start", $"Started Processing Drivers  {item.DllName}", DateTime.Now, -1, item.DllName, Errors.Ok);
                    GetDrivers(item.DllLib);
                    DMEEditor.AddLogMessage("End", $"Started Processing Drivers  {item.DllName}", DateTime.Now, -1, item.DllName, Errors.Ok);
                }
           

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
                        DMEEditor.AddLogMessage("Start", $"Started Processing DLL {s.DllName}", DateTime.Now, -1, s.DllName, Errors.Ok);
                        ScanAssembly(s.DllLib);
                        DMEEditor.Utilfunction.FunctionHierarchy = GetAddinObjects(s.DllLib);
                        DMEEditor.AddLogMessage("End", $"Ended Processing DLL {s.DllName}", DateTime.Now, -1, s.DllName, Errors.Ok);

                    }
                    catch (Exception ex)
                    {
                        DMEEditor.ErrorObject.Flag = Errors.Failed;
                        DMEEditor.AddLogMessage("Fail", $"Could not Process DLL {s.DllName}", DateTime.Now, -1,s.DllName, Errors.Failed);
                    }

                }
               
                //------------------------------
           
            AddEngineDefaultDrivers();
            CheckDriverAlreadyExistinList();
            return DMEEditor.ErrorObject;
        }
        /// <summary>
        ///     Method Will Load All Assembly found in the Passed Path
        /// </summary>
        /// <param name="Path"></param>
        /// <param name="FolderFileTypes"></param>
        /// <returns></returns>
        public string LoadAssembly(string path, FolderFileTypes fileTypes)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            string res = "";

            foreach (string dll in Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories))
            {
                try
                {
                    Assembly loadedAssembly = Assembly.LoadFrom(dll);

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
                                                DMEEditor.AddLogMessage("Success", $"Got Addin object {type.Name}", DateTime.Now, -1, type.Name, Errors.Ok);
                                            }
                                            catch (Exception ex)
                                            {
                                                
                                                DMEEditor.AddLogMessage("Fail", $"Could get Addin information {type.Name}", DateTime.Now, -1, type.Name, Errors.Failed);
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
                
                    catch (Exception ex1)
                    {
                        DMEEditor.AddLogMessage("Fail", $"Could get Addin object {type.Name} - {ex1.Message}", DateTime.Now, -1, type.Name, Errors.Failed);
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
                    try
                    {
                        t = asm.GetTypes();
                    }
                    catch (Exception ex2)
                    {
                        DMEEditor.AddLogMessage("Failed", $"Could not get types for {asm.GetName().ToString()}", DateTime.Now, -1, asm.GetName().ToString(), Errors.Failed);
                        try
                    {
                        DMEEditor.AddLogMessage("Try", $"Trying to get exported types for {asm.GetName().ToString()}", DateTime.Now, -1, asm.GetName().ToString(), Errors.Ok);
                        t = asm.GetExportedTypes();
                        }
                        catch (Exception ex3)
                        {
                            t = null;
                            DMEEditor.AddLogMessage("Failed", $"Could not get types for {asm.GetName().ToString()}", DateTime.Now, -1, asm.GetName().ToString(), Errors.Failed);
                        }
                       
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
                                xcls.classProperties= (ClassProperties)type.GetCustomAttribute(typeof(ClassProperties), false);
                                DataSourcesClasses.Add(xcls);
                                DMEEditor.ConfigEditor.DataSourcesClasses.Add(xcls);
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
                                xcls.classProperties = (ClassProperties)type.GetCustomAttribute(typeof(ClassProperties), false);
                                DMEEditor.WorkFlowEditor.WorkFlowActions.Add(xcls);
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
                                xcls.classProperties = (ClassProperties)type.GetCustomAttribute(typeof(ClassProperties), false);
                                DMEEditor.ConfigEditor.AppWritersClasses.Add(xcls);
                            }
                            if (type.ImplementedInterfaces.Contains(typeof(IAppComponent)))
                            {
                                AssemblyClassDefinition xcls = new AssemblyClassDefinition();
                                xcls.className = type.Name;
                                xcls.dllname = type.Module.Name;
                                xcls.PackageName = type.FullName;
                                xcls.type = type;
                                xcls.componentType = "IAppComponent";
                                xcls.classProperties = (ClassProperties)type.GetCustomAttribute(typeof(ClassProperties), false);
                                DMEEditor.ConfigEditor.AppComponents.Add(xcls);
                            }
                            if (type.ImplementedInterfaces.Contains(typeof(IAppDesigner)))
                            {
                                AssemblyClassDefinition xcls = new AssemblyClassDefinition();
                                xcls.className = type.Name;
                                xcls.dllname = type.Module.Name;
                                xcls.PackageName = type.FullName;
                                xcls.type = type;
                                xcls.componentType = "IAppDesigner";
                                xcls.classProperties = (ClassProperties)type.GetCustomAttribute(typeof(ClassProperties), false);
                                DMEEditor.ConfigEditor.AppComponents.Add(xcls);
                            }
                            if (type.ImplementedInterfaces.Contains(typeof(IAppScreen)))
                            {
                                AssemblyClassDefinition xcls = new AssemblyClassDefinition();
                                xcls.className = type.Name;
                                xcls.dllname = type.Module.Name;
                                xcls.PackageName = type.FullName;
                                xcls.type = type;
                                xcls.componentType = "IAppScreen";
                                xcls.classProperties = (ClassProperties)type.GetCustomAttribute(typeof(ClassProperties), false);
                                DMEEditor.ConfigEditor.AppComponents.Add(xcls);
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
                                xcls.classProperties = (ClassProperties)type.GetCustomAttribute(typeof(ClassProperties), false);
                                DMEEditor.ConfigEditor.ReportWritersClasses.Add(xcls);
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
                                
                                xcls.classProperties = (ClassProperties)type.GetCustomAttribute(typeof(ClassProperties), false);
                                if (xcls.classProperties != null)
                                {
                                    xcls.RootName = xcls.classProperties.FileType;
                                }
                                
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
                                    catch (Exception )
                                    {
                                       

                                    }

                                }
                                DMEEditor.ConfigEditor.BranchesClasses.Add(xcls);
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
                                DMEEditor.ConfigEditor.BranchesClasses.Add(xcls);
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                   DMEEditor.AddLogMessage("Failed", $"Could not get Any types for {asm.GetName().ToString()}" , DateTime.Now, -1, asm.GetName().ToString(), Errors.Failed);
                };

                return true;
           
           
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
        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            // Ignore missing resources
            if (args.Name.Contains(".resources"))
                return null;
            string filename = args.Name.Split(',')[0] + ".dll".ToLower();
            string filenamewo = args.Name.Split(',')[0];
            // check for assemblies already loaded
            //   var s = AppDomain.CurrentDomain.GetAssemblies();
            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName.StartsWith(filenamewo));
            if (assembly == null)
            {
                assemblies_rep s = Assemblies.FirstOrDefault(a => a.DllLib.FullName.StartsWith(filenamewo));
                if (s != null)
                {
                    assembly = s.DllLib;
                }
                
            }
            if (assembly != null)
                return assembly;
            foreach (var moduleDir in DMEEditor.ConfigEditor.Config.Folders.Where(c =>  c.FolderFilesType == FolderFileTypes.OtherDLL))
            {
                var di = new DirectoryInfo(moduleDir.FolderPath);
                var module = di.GetFiles().FirstOrDefault(i => i.Name == filename );
                if (module != null)
                {
                    return Assembly.LoadFrom(module.FullName);
                }
            }
            if (assembly != null)
                return assembly;
            foreach (var moduleDir in DMEEditor.ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.ConnectionDriver))
            {
                var di = new DirectoryInfo(moduleDir.FolderPath);
                var module = di.GetFiles().FirstOrDefault(i => i.Name == filename);
                if (module != null)
                {
                    return Assembly.LoadFrom(module.FullName);
                }
            }
            if (assembly != null)
                return assembly;
            foreach (var moduleDir in DMEEditor.ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.ProjectClass))
            {
                var di = new DirectoryInfo(moduleDir.FolderPath);
                var module = di.GetFiles().FirstOrDefault(i => i.Name == filename);
                if (module != null)
                {
                    return Assembly.LoadFrom(module.FullName);
                }
            }
           
             
            return null;

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
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = asm.GetType(strFullyQualifiedName);
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
            catch (Exception )
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
            try
            {
                if (asm.GetType() == null)
                {
                    t = asm.GetExportedTypes();
                }
                else
                    t = asm.GetTypes().Where(typeof(IDbDataAdapter).IsAssignableFrom).ToArray();
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
                DMEEditor.AddLogMessage("Success", $"Got ADO Type Drivers {asm.GetName().ToString()}", DateTime.Now, -1, asm.GetName().ToString(), Errors.Ok);
            }
            catch (Exception)
            {
                t = null;
                retval = false;
                DMEEditor.AddLogMessage("Failed", $"Could not ADO Type Drivers for {asm.GetName().ToString()}", DateTime.Now, -1, asm.GetName().ToString(), Errors.Failed);
            }
           
          
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

            //    DMEEditor.Logger.WriteLog($"error in creating addin {ex.Message} ");
               
            }



        }
        public List<ConnectionDriversConfig> GetDrivers(Assembly asm)
        {
            try
            { 
                if (asm.GetType() != null)
                {
                    GetADOTypeDrivers(asm);
                }
                
            }
            catch (ReflectionTypeLoadException ex1)
            {
                DMEEditor.AddLogMessage("Failed", $"Showing Details for  {asm.GetName().ToString()}", DateTime.Now, -1, asm.GetName().ToString(), Errors.Failed);
                StringBuilder sb = new StringBuilder();
                foreach (Exception exSub in ex1.LoaderExceptions)
                {
                    sb.AppendLine(exSub.Message);
                    FileNotFoundException exFileNotFound = exSub as FileNotFoundException;
                    if (exFileNotFound != null)
                    {
                        if (!string.IsNullOrEmpty(exFileNotFound.FusionLog))
                        {
                            sb.AppendLine("Fusion Log:");
                            sb.AppendLine(exFileNotFound.FusionLog);
                        }
                    }
                    sb.AppendLine();
                }
                string errorMessage = sb.ToString();
                DMEEditor.AddLogMessage("Failed", $"Could not get Any types for {asm.GetName().ToString()} \n {errorMessage}", DateTime.Now, -1, asm.GetName().ToString(), Errors.Failed);

                
            }


            return DataDriversConfig;


        }
        public List<string> CreateFileExtensionString()
        {
            List<AssemblyClassDefinition> cls = DataSourcesClasses.Where(o => o.classProperties != null).ToList();
           

            IEnumerable<string> extensionslist = cls.Where(o => o.classProperties.Category == DatasourceCategory.FILE).Select(p => p.classProperties.FileType);
            string extstring = string.Join(",", extensionslist);
            return extstring.Split(',').ToList() ;
           
          
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
                DMEEditor.AddLogMessage(ex.Message, "Could not Add Driver" + mes, DateTime.Now, -1, mes, Errors.Failed);
                return false;
            };
        }
        #endregion
        //public void CreateAssembly(Dictionary<string, string> propertiesToEmit)
        //{
        //    Assembly ourAssembly= null;
        //    if (ourAssembly == null)
        //    {
        //        StringBuilder sb = new StringBuilder();
        //        sb.AppendLine("using System;");
        //        sb.AppendLine("public class MyClass");
        //        sb.AppendLine("{");
        //        sb.AppendLine("  public static void Main()");
        //        sb.AppendLine("  {");
        //        sb.AppendLine("  }");
        //        foreach (var kvp in propertiesToEmit)
        //        {
        //            sb.AppendLine($"  public {kvp.Value} {kvp.Key}" + " { get; set; }");
        //        }
        //        sb.AppendLine("  public  MyClass CreateFromDynamic(Dictionary<string, object> sourceItem)");
        //        sb.AppendLine("  {");
        //        sb.AppendLine("     MyClass newOne = new MyClass();");
        //        foreach (var kvp in propertiesToEmit)
        //        {
        //            sb.AppendLine($@"  newOne.{kvp.Key} = sourceItem[""{kvp.Key}""];");
        //        }
        //        sb.AppendLine("  return newOne;");
        //        sb.AppendLine("  }");
        //        sb.AppendLine("}");

        //        var tree = CSharpSyntaxTree.ParseText(sb.ToString());
        //        var mscorlib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        //        var dictsLib = MetadataReference.CreateFromFile(typeof(Dictionary<,>).Assembly.Location);

        //        var compilation = CSharpCompilation.Create("MyCompilation",
        //            syntaxTrees: new[] { tree }, references: new[] { mscorlib, dictsLib });

        //        //Emit to stream
        //        var ms = new MemoryStream();
        //        var emitResult = compilation.Emit(ms);

        //        //Load into currently running assembly. Normally we'd probably
        //        //want to do this in an AppDomain
        //        ourAssembly = Assembly.Load(ms.ToArray());
        //    }
        //}
    }
}
