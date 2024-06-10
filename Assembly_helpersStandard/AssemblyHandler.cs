
using DataManagementModels.DataBase;
using DataManagementModels.DriversConfigurations;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Logger;
using TheTechIdea.Util;
using TypeInfo = System.Reflection.TypeInfo;
using Microsoft.Extensions.DependencyModel;


namespace TheTechIdea.Tools
{
    /// <summary>
    /// Handles assembly-related operations such as loading, scanning for extensions, and managing driver configurations.
    /// </summary>
    public class AssemblyHandler : IAssemblyHandler
    {
        private ParentChildObject a;
        private string Name { get; set; }
        private string Descr { get; set; }
        private List<Type> LoaderExtensions { get; set; } = new List<Type>();
        private List<ConnectionDriversConfig> DataDriversConfig = new List<ConnectionDriversConfig>();
        private bool disposedValue;
     
        public List<string> NamespacestoIgnore { get; set; } = new List<string>();
        /// <summary>
        /// Gets or sets the current domain in which the assembly is executed.
        /// </summary>
        public AppDomain CurrentDomain { get; set; }

        /// <summary>
        /// Error handling object.
        /// </summary>
        public IErrorsInfo ErrorObject { get; set; }

        /// <summary>
        /// Logging interface for tracking activities and errors.
        /// </summary>
        public IDMLogger Logger { get; set; }

        /// <summary>
        /// Utility functions for assembly handling.
        /// </summary>
        public IUtil Utilfunction { get; set; }

        /// <summary>
        /// Interface for configuration editing.
        /// </summary>
        public IConfigEditor ConfigEditor { get; set; }

        /// <summary>
        /// List of classes that extend the loader functionality.
        /// </summary>
        public List<AssemblyClassDefinition> LoaderExtensionClasses { get; set; } = new List<AssemblyClassDefinition>();

        /// <summary>
        /// List of assemblies loaded or referenced.
        /// </summary>
        public List<assemblies_rep> Assemblies { get; set; } = new List<assemblies_rep>();

        /// <summary>
        /// List of classes that represent data sources.
        /// </summary>
        public List<AssemblyClassDefinition> DataSourcesClasses { get; set; } = new List<AssemblyClassDefinition>();
        public List<Assembly> LoadedAssemblies { get;  set; } = new List<Assembly>();

        /// <summary>
        /// Constructor for AssemblyHandler, initializes necessary properties.
        /// </summary>
        /// <param name="pConfigEditor">Configuration editor.</param>
        /// <param name="pErrorObject">Error handling object.</param>
        /// <param name="pLogger">Logging interface.</param>
        /// <param name="pUtilfunction">Utility functions.</param>
        public AssemblyHandler(IConfigEditor pConfigEditor, IErrorsInfo pErrorObject, IDMLogger pLogger, IUtil pUtilfunction)
        {

            ConfigEditor = pConfigEditor;
            ErrorObject = pErrorObject;
            Logger = pLogger;
            Utilfunction = pUtilfunction;
            CurrentDomain = AppDomain.CurrentDomain;
            DataSourcesClasses = new List<AssemblyClassDefinition>();
            CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            // Get current, executing, and calling assemblies
           var assemblies = new List<Assembly>
    {
        Assembly.GetExecutingAssembly(),
        Assembly.GetCallingAssembly(),
        Assembly.GetEntryAssembly()
    };
          var  dependencyAssemblies = DependencyContext.Default.RuntimeLibraries
    .SelectMany(library => library.GetDefaultAssemblyNames(DependencyContext.Default))
    .Select(Assembly.Load)
    .Where(assembly => !assembly.FullName.StartsWith("System") && !assembly.FullName.StartsWith("Microsoft"))
    .ToList();
            //LoadedAssemblies.AddRange(assemblies);
            // Load all assemblies from the current domain to ensure referenced projects are included
            //LoadedAssemblies.AddRange(AppDomain.CurrentDomain.GetAssemblies()
            //    .Where(assembly => !assembly.FullName.StartsWith("System") && !assembly.FullName.StartsWith("Microsoft")));
            // Combine both sets of assemblies
            LoadedAssemblies = dependencyAssemblies.Concat(assemblies).Distinct().ToList();
           
        }

        #region "Loaders"
        /// <summary>
        /// Scans and initializes loader extensions within a given assembly representation.
        /// </summary>
        /// <param name="assembly">The assemblies_rep object representing the assembly to be scanned.</param>
        private void ScanExtensions(assemblies_rep assembly)
        {
            foreach (Type item in LoaderExtensions)
            {
                try
                {
                    ILoaderExtention cls = (ILoaderExtention)Activator.CreateInstance(item, new object[] { this });
                   
                    cls.Scan(assembly);

                }
                catch (Exception)
                {


                }
            }
        }
        /// <summary>
        /// Scans and initializes loader extensions within a given .NET Assembly object.
        /// </summary>
        /// <param name="assembly">The Assembly object to be scanned.</param>
        private void ScanExtensions()
        {
            bool skip = false;  
            foreach (Type item in LoaderExtensions)
            {
                try
                {
                    ILoaderExtention cls = (ILoaderExtention)Activator.CreateInstance(item, new object[] { this });
                    foreach (Assembly assembly1 in LoadedAssemblies)
                    {
                        try
                        {
                            skip= false;    
                            // Filter out system assemblies or non-NuGet assemblies if necessary
                            // For example, you might check the assembly's name, location, etc.
                            // filter also using the namespacetoignore
                            if (NamespacestoIgnore.Count>0)
                            {
                                if (NamespacestoIgnore.Any(ns => assembly1.FullName.Contains(ns)))
                                {
                                    skip = true;
                                }
                            }
                            if (!assembly1.FullName.StartsWith("System") && !assembly1.FullName.StartsWith("Microsoft") && skip==false )
                            {
                                cls.Scan(assembly1);
                            }
                        }
                        catch (Exception ex)
                        {

                            //DMEEditor.Logger.WriteLog($"error loading current assembly {ex.Message} ");
                        }
                    }
                    //foreach (assemblies_rep assembly1 in Assemblies.Where(p=>p.FileTypes  == FolderFileTypes.Addin || p.FileTypes == FolderFileTypes.ProjectClass))
                    //{
                    //    try
                    //    {
                    //        // Filter out system assemblies or non-NuGet assemblies if necessary
                    //        // For example, you might check the assembly's name, location, etc.
                    //        // filter also using the namespacetoignore
                    //        if (!assembly1.DllName.StartsWith("System") && !assembly1.DllName.StartsWith("Microsoft"))
                    //        {
                    //            cls.Scan(assembly1);
                    //        }
                    //    }
                    //    catch (Exception ex)
                    //    {

                    //        //DMEEditor.Logger.WriteLog($"error loading current assembly {ex.Message} ");
                    //    }
                    //}

                }
                catch (Exception)
                {


                }
            }
        }
        private void ScanExtension(Assembly assembly)
        {
            foreach (Type item in LoaderExtensions)
            {
                try
                {
                    ILoaderExtention cls = (ILoaderExtention)Activator.CreateInstance(item, new object[] { this });
                    cls.Scan(assembly);
                    
                }
                catch (Exception)
                {


                }
            }
        }
        /// <summary>
        /// Loads assemblies from a specified path and scans them for extension scanners, 
        /// reporting progress through IProgress.
        /// </summary>
        /// <param name="progress">The progress reporting mechanism.</param>
        /// <param name="token">The token to monitor for cancellation requests.</param>
        public void GetExtensionScanners(IProgress<PassedArgs> progress, CancellationToken token)
        {
           
                try
                {
                   
                    LoadAssembly(Path.Combine(ConfigEditor.ExePath,"LoadingExtensions"), FolderFileTypes.LoaderExtensions);
                }
                catch (FileLoadException loadEx)
                {
                    ErrorObject.Flag = Errors.Failed;
                   // res = "The Assembly has already been loaded" + loadEx.Message;
                    // MessageBox.Show("The Assembly has already been loaded" + loadEx.Message, "Simple ODM", MessageBoxButtons.OK);
                } // The Assembly has already been loaded.
                catch (BadImageFormatException imgEx)
                {
                    ErrorObject.Flag = Errors.Failed;
                    // MessageBox.Show(imgEx.Message, "Simple ODM", MessageBoxButtons.OK);  // If a BadImageFormatException exception is thrown, the file is not an assembly.
                   // res = imgEx.Message;
                }
                catch (Exception ex)
                {
                    ErrorObject.Flag = Errors.Failed;
                    // MessageBox.Show(ex.Message, "Simple ODM", MessageBoxButtons.OK);  // If a BadImageFormatException exception is thrown, the file is not an assembly
                  //  res = ex.Message;
                }
                foreach (assemblies_rep s in Assemblies.Where(x => x.FileTypes == FolderFileTypes.LoaderExtensions ))
                {
                    try
                    {
                        ////DMEEditor.AddLogMessage("Start", $"Started Processing DLL {s.DllName}", DateTime.Now, -1, s.DllName, Errors.Ok);
                        ScanAssembly(s.DllLib);
                       
                        //   //DMEEditor.AddLogMessage("End", $"Ended Processing DLL {s.DllName}", DateTime.Now, -1, s.DllName, Errors.Ok);

                    }
                    catch (Exception ex)
                    {
                        ErrorObject.Flag = Errors.Failed;
                        //  //DMEEditor.AddLogMessage("Fail", $"Could not Process DLL {s.DllName}", DateTime.Now, -1,s.DllName, Errors.Failed);
                    }

                }
               

            
        }
        /// <summary>
        /// Scans the current executing assembly and the root assembly for built-in classes.
        /// </summary>
        /// <returns>Returns an IErrorsInfo object indicating the success or failure of the operation.</returns>
        public IErrorsInfo GetBuiltinClasses()
        {
            //  DMEEditor.ErrorObject.Flag = Errors.Ok;
            var assemblies = LoadedAssemblies;

            foreach (Assembly item in assemblies)
            {
                try
                {
                    // Filter out system assemblies or non-NuGet assemblies if necessary
                    // For example, you might check the assembly's name, location, etc.
                    if (!item.FullName.StartsWith("System") && !item.FullName.StartsWith("Microsoft"))
                    {
                        Assemblies.Add(new assemblies_rep(item, "", item.FullName, FolderFileTypes.Builtin));
                        ScanAssembly(item);
                        GetDrivers(item);
                    }
             ;
                }
                catch (Exception ex)
                {

                    //DMEEditor.Logger.WriteLog($"error loading current assembly {ex.Message} ");
                }
            }
            // look through assembly list
            Assembly currentAssem = Assembly.GetExecutingAssembly();
            Assembly rootassembly = Assembly.GetEntryAssembly();
         
            try
            {
                if (!currentAssem.FullName.StartsWith("System") && !currentAssem.FullName.StartsWith("Microsoft"))
                {
                    ScanAssembly(currentAssem);
                    GetDrivers(currentAssem);
                }
                //  Utilfunction.FunctionHierarchy = GetAddinObjects(currentAssem);
            }
            catch (Exception ex)
            {

             //   DMEEditor.Logger.WriteLog($"error loading current assembly {ex.Message} ");
            }

            try
            {
                ScanAssembly(rootassembly);
              //  Utilfunction.FunctionHierarchy = GetAddinObjects(rootassembly);

            }
            catch (Exception ex)
            {

               // DMEEditor.Logger.WriteLog($"error loading current assembly {ex.Message} ");
            }
          
            return ErrorObject;

        }
        /// <summary>
        ///     This Method will go through all Folders ProjectClass,OtherDLL,Addin, Drivers and load DLL
        /// </summary>
        /// <returns></returns>
        public IErrorsInfo LoadAllAssembly(IProgress<PassedArgs> progress, CancellationToken token)
        {

            ErrorObject.Flag = Errors.Ok;
            string res;
            Utilfunction.FunctionHierarchy = new List<ParentChildObject>();
            Utilfunction.Namespacelist = new List<string>();
            Utilfunction.Classlist = new List<string>();
            DataDriversConfig = new List<ConnectionDriversConfig>();
          
            //SendMessege(progress, token,"Getting Non ADO Drivers");
            //GetNonADODrivers();
            SendMessege(progress, token, "Getting Builtin Classes");
            GetBuiltinClasses();
            LoadAssemblyFormRunTime();
            SendMessege(progress, token, "Getting FrameWork Extensions");
            GetExtensionScanners(progress, token);

            SendMessege(progress, token, "Getting Drivers Classes");
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
            SendMessege(progress, token, "Getting Data Sources Classes");
            foreach (string p in ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.DataSources).Select(x => x.FolderPath))
            {
                try
                {
                    LoadAssembly(p, FolderFileTypes.DataSources);


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
            if (ConfigEditor.ConfigType!= BeepConfigType.DataConnector)
            {
                SendMessege(progress, token, "Getting Project and Addin Classes");
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
                SendMessege(progress, token, "Getting Other DLL Classes");
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
            }
          
            // Get Driver from Loaded Assembly
            SendMessege(progress, token, "Scanning Classes For Drivers");
            foreach (assemblies_rep item in Assemblies.Where(c=>c.FileTypes==FolderFileTypes.ConnectionDriver).ToList())
            {
                   // AddLogMessage("Start", $"Started Processing Drivers  {item.DllName}", DateTime.Now, -1, item.DllName, Errors.Ok);
                    GetDrivers(item.DllLib);
                   // AddLogMessage("End", $"Started Processing Drivers  {item.DllName}", DateTime.Now, -1, item.DllName, Errors.Ok);
            }
            SendMessege(progress, token, "Scanning Classes For DataSources");
            foreach (assemblies_rep item in Assemblies.Where(c => c.FileTypes == FolderFileTypes.DataSources).ToList())
            {
                // Get DataBase Implementation Classes
                ScanAssemblyForDataSources(item.DllLib);
                
            }
            if (ConfigEditor.ConfigType != BeepConfigType.DataConnector)
            {
                SendMessege(progress, token, "Scanning Classes For Addins");
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
                SendMessege(progress, token, "Scanning Classes For Project's and Addin's");
                foreach (assemblies_rep s in Assemblies.Where(x => x.FileTypes == FolderFileTypes.ProjectClass || x.FileTypes == FolderFileTypes.Addin))
                {
                    try
                    {
                        ////DMEEditor.AddLogMessage("Start", $"Started Processing DLL {s.DllName}", DateTime.Now, -1, s.DllName, Errors.Ok);
                        ScanAssembly(s.DllLib);
                        //  Utilfunction.FunctionHierarchy = GetAddinObjects(s.DllLib);
                        //   //DMEEditor.AddLogMessage("End", $"Ended Processing DLL {s.DllName}", DateTime.Now, -1, s.DllName, Errors.Ok);

                    }
                    catch (Exception ex)
                    {
                        ErrorObject.Flag = Errors.Failed;
                        //  //DMEEditor.AddLogMessage("Fail", $"Could not Process DLL {s.DllName}", DateTime.Now, -1,s.DllName, Errors.Failed);
                    }

                }
            }
            //------------------------------
            SendMessege(progress, token, "Adding Default Engine Drivers");
            AddEngineDefaultDrivers();
            SendMessege(progress, token, "Organizing Drivers");
            CheckDriverAlreadyExistinList();
            SendMessege(progress, token, "Scanning Extensions");
            ScanExtensions();
            SendMessege(progress, token, "Scanning Folders  For Extension Project's and Addin's");
            foreach (assemblies_rep s in Assemblies.Where(x => x.FileTypes == FolderFileTypes.ProjectClass || x.FileTypes == FolderFileTypes.Addin))
            {
                try
                {
                    ////DMEEditor.AddLogMessage("Start", $"Started Processing DLL {s.DllName}", DateTime.Now, -1, s.DllName, Errors.Ok);
                    ScanExtension(s.DllLib);
                    //  Utilfunction.FunctionHierarchy = GetAddinObjects(s.DllLib);
                    //   //DMEEditor.AddLogMessage("End", $"Ended Processing DLL {s.DllName}", DateTime.Now, -1, s.DllName, Errors.Ok);

                }
                catch (Exception ex)
                {
                    ErrorObject.Flag = Errors.Failed;
                    //  //DMEEditor.AddLogMessage("Fail", $"Could not Process DLL {s.DllName}", DateTime.Now, -1,s.DllName, Errors.Failed);
                }

            }
            if (ConfigEditor.ConfigType != BeepConfigType.DataConnector)
            {
                Utilfunction.FunctionHierarchy = GetAddinObjectsFromTree();
            }
            return ErrorObject;
        }
        /// <summary>
        ///     Method Will Load All Assembly found in the Passed Path
        /// </summary>
        /// <param name="Path"></param>
        /// <param name="FolderFileTypes"></param>
        /// <returns></returns>
        public string LoadAssembly(string path, FolderFileTypes fileTypes)
        {
            ErrorObject.Flag = Errors.Ok;
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
        /// <summary>
        ///     Method Will Load All Assembly found in the Passed Path
        /// </summary>
        /// <param name="Path"></param>
        /// <param name="FolderFileTypes"></param>
        /// <returns></returns>
        public string LoadAssemblyFormRunTime()
        {
            ErrorObject.Flag = Errors.Ok;
            string res = "";

            foreach (Assembly loadedAssembly in LoadedAssemblies)
            {
                try
                {
                  // if loadedassembly not found in Assemblies then add to assemblies
                  if (Assemblies.Where(x => x.DllLib == loadedAssembly).Count() == 0)
                    {
                        assemblies_rep x = new assemblies_rep(loadedAssembly, "Builtin", loadedAssembly.FullName, FolderFileTypes.Builtin);
                        Assemblies.Add(x);
                    }
                    
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
        #endregion "Loaders"
        #region "Class ordering"
        /// <summary>
        /// Rearranges or adds a new addin object to the function hierarchy.
        /// </summary>
        /// <param name="p">The ID of the addin object.</param>
        /// <param name="parentid">The parent ID of the addin object. Null if it's a root object.</param>
        /// <param name="Objt">The type of the object.</param>
        /// <returns>Returns a new or existing ParentChildObject based on the input parameters.</returns>
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
        /// <summary>
        /// Retrieves addin objects from the specified assembly and organizes them into a hierarchical structure.
        /// </summary>
        /// <param name="asm">The assembly to scan for addin objects.</param>
        /// <returns>A list of ParentChildObjects representing the hierarchical structure of addins.</returns>
        public List<ParentChildObject> GetAddinObjects(Assembly asm)
        {
            IDM_Addin addin = null;
            string objtype = "";
            Boolean Show = true;
            int cnt = 0;
            var itype = typeof(IDM_Addin);
            
            foreach (Type type in asm.DefinedTypes.Where(p => itype.IsAssignableFrom(p)))
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
                                                   //else Show = false;
                                                    // objtype = "UserControl";

                                                if (Show)
                                                {
                                                    a = RearrangeAddin(p[i], p[i - 1], objtype);
                                                }
                                                //DMEEditor.AddLogMessage("Success", $"Got Addin object {type.Name}", DateTime.Now, -1, type.Name, Errors.Ok);
                                            }
                                            catch (Exception ex)
                                            {
                                                
                                                //DMEEditor.AddLogMessage("Fail", $"Could get Addin information {type.Name}", DateTime.Now, -1, type.Name, Errors.Failed);
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
                        //DMEEditor.AddLogMessage("Fail", $"Could get Addin object {type.Name} - {ex1.Message}", DateTime.Now, -1, type.Name, Errors.Failed);
                    }
                }

            }
          
             ConfigEditor.SaveAddinTreeStructure();
            return Utilfunction.FunctionHierarchy;
        }
        /// <summary>
        /// Retrieves addin objects from the addin tree structure defined in the configuration editor.
        /// </summary>
        /// <returns>A list of ParentChildObjects representing the addins organized in a hierarchical structure.</returns>
        public List<ParentChildObject> GetAddinObjectsFromTree( )
        {
            IDM_Addin addin = null;
            string objtype = "";
            Boolean Show = true;
            int cnt = 0;
            

            foreach (AddinTreeStructure tree in ConfigEditor.AddinTreeStructure)
            {
              
                    try
                    {
                        if (tree.PackageName.Contains("Properties") == false)
                        {

                            string[] p = tree.PackageName.Split(new char[] { '.' });

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
                                                //else Show = false;
                                                // objtype = "UserControl";

                                                if (Show)
                                                {
                                                    a = RearrangeAddin(p[i], p[i - 1], tree.ObjectType);
                                                }
                                                //DMEEditor.AddLogMessage("Success", $"Got Addin object {type.Name}", DateTime.Now, -1, type.Name, Errors.Ok);
                                            }
                                            catch (Exception ex)
                                            {

                                                //DMEEditor.AddLogMessage("Fail", $"Could get Addin information {type.Name}", DateTime.Now, -1, type.Name, Errors.Failed);
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
                        //DMEEditor.AddLogMessage("Fail", $"Could get Addin object {type.Name} - {ex1.Message}", DateTime.Now, -1, type.Name, Errors.Failed);
                    }
          

            }

            ConfigEditor.SaveAddinTreeStructure();
            return Utilfunction.FunctionHierarchy;
        }
        #endregion
        #region "Class Extractors"
        // <summary>
        /// Scans an assembly to identify and extract data source implementations.
        /// </summary>
        /// <param name="asm">The assembly to scan for data source types.</param>
        /// <returns>Returns true if the scanning is successful, otherwise false.</returns>
        private bool ScanAssemblyForDataSources(Assembly asm)
        {
            Type[] t;
            //  Console.WriteLine(asm.FullName);
            try
            {
                try
                {
                    t = asm.GetTypes();
                }
                catch (Exception ex2)
                {
                    //DMEEditor.AddLogMessage("Failed", $"Could not get types for {asm.GetName().ToString()}", DateTime.Now, -1, asm.GetName().ToString(), Errors.Failed);
                    try
                    {
                        //DMEEditor.AddLogMessage("Try", $"Trying to get exported types for {asm.GetName().ToString()}", DateTime.Now, -1, asm.GetName().ToString(), Errors.Ok);
                        t = asm.GetExportedTypes();
                    }
                    catch (Exception ex3)
                    {
                        t = null;
                        //DMEEditor.AddLogMessage("Failed", $"Could not get types for {asm.GetName().ToString()}", DateTime.Now, -1, asm.GetName().ToString(), Errors.Failed);
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
                        // Get DataBase Implementation Classes
                        if (type.ImplementedInterfaces.Contains(typeof(IDataSource)))
                        {

                            AssemblyClassDefinition xcls = GetAssemblyClassDefinition(type, "IDataSource");
                            DataSourcesClasses.Add(xcls);
                            ConfigEditor.DataSourcesClasses.Add(xcls);
                        }
                  
                    }
                    //ScanExtension(asm);
                }

            }
            catch (Exception ex)
            {
                //DMEEditor.AddLogMessage("Failed", $"Could not get Any types for {asm.GetName().ToString()}" , DateTime.Now, -1, asm.GetName().ToString(), Errors.Failed);
            };

            return true;


        }
        /// <summary>
        /// Scans an assembly to identify various implementations like data sources, loader extensions, workflow actions, and more.
        /// </summary>
        /// <param name="asm">The assembly to scan.</param>
        /// <returns>Returns true if the scanning is successful, otherwise false.</returns>
        private bool ScanAssembly(Assembly asm)
        {
            Type[] t;
          //  Console.WriteLine(asm.FullName);
                try
                {
                    try
                    {
                        t = asm.GetTypes();
                    }
                    catch (Exception ex2)
                    {
                        //DMEEditor.AddLogMessage("Failed", $"Could not get types for {asm.GetName().ToString()}", DateTime.Now, -1, asm.GetName().ToString(), Errors.Failed);
                        try
                    {
                        //DMEEditor.AddLogMessage("Try", $"Trying to get exported types for {asm.GetName().ToString()}", DateTime.Now, -1, asm.GetName().ToString(), Errors.Ok);
                        t = asm.GetExportedTypes();
                        }
                        catch (Exception ex3)
                        {
                            t = null;
                            //DMEEditor.AddLogMessage("Failed", $"Could not get types for {asm.GetName().ToString()}", DateTime.Now, -1, asm.GetName().ToString(), Errors.Failed);
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
                        // Get WorkFlow Definitions
                        if (type.ImplementedInterfaces.Contains(typeof(ILoaderExtention)))
                         {
                            
                            LoaderExtensions.Add(type);
                            LoaderExtensionClasses.Add(GetAssemblyClassDefinition(type, "ILoaderExtention"));
                        }
                        //-------------------------------------------------------
                        // Get DataBase Drivers
                        if (type.ImplementedInterfaces.Contains(typeof(IDataSource)))
                        {
                              
                            AssemblyClassDefinition xcls = GetAssemblyClassDefinition(type, "IDataSource");
                            DataSourcesClasses.Add(xcls);
                            ConfigEditor.DataSourcesClasses.Add(xcls);
                        }
                         //-------------------------------------------------------
                         // Get WorkFlow Definitions
                        if (type.ImplementedInterfaces.Contains(typeof(IWorkFlowAction)))
                        {
                      
                            ConfigEditor.WorkFlowActions.Add(GetAssemblyClassDefinition(type, "IWorkFlowAction"));
                        }
                        if (type.ImplementedInterfaces.Contains(typeof(IDM_Addin)))
                        {
                            AssemblyClassDefinition cls=GetAssemblyClassDefinition(type, "IDM_Addin");
                            ConfigEditor.Addins.Add(cls);
                        }
                        if (type.ImplementedInterfaces.Contains(typeof(IWorkFlowStep)))
                        {

                            ConfigEditor.WorkFlowSteps.Add(GetAssemblyClassDefinition(type, "IWorkFlowStep"));
                        }
                        if (type.ImplementedInterfaces.Contains(typeof(IWorkFlowStepEditor)))
                        {

                            ConfigEditor.WorkFlowStepEditors.Add(GetAssemblyClassDefinition(type, "IWorkFlowStepEditor"));
                        }
                        if (type.ImplementedInterfaces.Contains(typeof(IWorkFlowEditor)))
                        {

                            ConfigEditor.WorkFlowStepEditors.Add(GetAssemblyClassDefinition(type, "IWorkFlowEditor"));
                        }
                        if (type.ImplementedInterfaces.Contains(typeof(IWorkFlowRule)))
                        {
                            ConfigEditor.Rules.Add(GetAssemblyClassDefinition(type, "IWorkFlowRule"));
                        }
                        // Get IFunctionExtension Definitions
                        if (type.ImplementedInterfaces.Contains(typeof(IFunctionExtension)))
                        {
                            ConfigEditor.GlobalFunctions.Add(GetAssemblyClassDefinition(type, "IFunctionExtension"));
                        }
                        // Get Print Managers Definitions
                        if (type.ImplementedInterfaces.Contains(typeof(IPrintManager)))
                        {
                            ConfigEditor.PrintManagers.Add(GetAssemblyClassDefinition(type, "IPrintManager"));
                        }
                        if (type.ImplementedInterfaces.Contains(typeof(IAddinVisSchema)))
                        {
                            //GetAddinObjects(asm);
                        } 
                       
                    }
                      
                    }

                }
                catch (Exception ex)
                {
                   //DMEEditor.AddLogMessage("Failed", $"Could not get Any types for {asm.GetName().ToString()}" , DateTime.Now, -1, asm.GetName().ToString(), Errors.Failed);
                };

                return true;
           
           
        }
        /// <summary>
        /// Gets the definition of a class within an assembly, including metadata and methods.
        /// </summary>
        /// <param name="type">TypeInfo object of the class.</param>
        /// <param name="typename">The name of the type being defined.</param>
        /// <returns>Returns an AssemblyClassDefinition object containing class details.</returns>
        public AssemblyClassDefinition GetAssemblyClassDefinition(TypeInfo type,string typename)
        {
           
            AssemblyClassDefinition xcls = new AssemblyClassDefinition();
            xcls.Methods = new List<MethodsClass>();
            xcls.className = type.Name;
            xcls.dllname = type.Module.Name;
            xcls.PackageName = type.FullName;
            xcls.componentType = typename;
            xcls.type = type;
            if (type.ImplementedInterfaces.Contains(typeof(ILocalDB)))
            {
                xcls.LocalDB= true;
            }
            if (type.ImplementedInterfaces.Contains(typeof(IDataSource)))
            {
                xcls.IsDataSource = true;
            }
            if (type.ImplementedInterfaces.Contains(typeof(IInMemoryDB)))
            {
                xcls.InMemory = true;
            }
            xcls.classProperties = (AddinAttribute)type.GetCustomAttribute(typeof(AddinAttribute), false);
            if (xcls.classProperties != null)
            {
                xcls.Order = xcls.classProperties.order;
                
                xcls.RootName = xcls.classProperties.misc;
                try
                {
                    xcls.VisSchema = (AddinVisSchema)type.GetCustomAttribute(typeof(AddinVisSchema), false);
                    if (xcls.VisSchema != null)
                    {
                        if (type.ImplementedInterfaces.Contains(typeof(IAddinVisSchema)))
                        {
                            AddinTreeStructure AddinTree = new AddinTreeStructure();
                            AddinTree.className = type.Name;
                            AddinTree.dllname = type.Module.Name;
                            AddinTree.PackageName = type.FullName;
                            AddinTree.Order = xcls.Order;
                            if (xcls.VisSchema != null)
                            {
                                AddinTree.className = type.Name;
                                AddinTree.dllname = type.Module.Name;
                                AddinTree.PackageName = type.FullName;
                                AddinTree.Order = xcls.Order;
                                AddinTree.Imagename = xcls.VisSchema.IconImageName;
                                AddinTree.RootName = xcls.VisSchema.RootNodeName;
                                AddinTree.NodeName = xcls.VisSchema.BranchText;
                                AddinTree.ObjectType = type.Name;
                            }
                            ConfigEditor.AddinTreeStructure.Add(AddinTree);
                        }
                    }

                }
                catch (Exception ex)
                {
                }

                foreach (MethodInfo methods in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public)
                             .Where(m => m.GetCustomAttributes(typeof(CommandAttribute), true).Length > 0)
                              .ToArray())
                {

                    CommandAttribute methodAttribute = methods.GetCustomAttribute<CommandAttribute>();
                    if (methodAttribute != null)
                    {
                        MethodsClass x = new MethodsClass();
              
                        x.CommandAttr = methodAttribute;
                        x.Caption = methodAttribute.Caption;
                        x.Name = methodAttribute.Name;
                        x.Info = methods;
                        x.Hidden = methodAttribute.Hidden;
                        x.Click = methodAttribute.Click;
                        x.DoubleClick = methodAttribute.DoubleClick;
                        x.iconimage = methodAttribute.iconimage;
                        //if (xcls.classProperties != null)
                        //{
                        //    x.AddinAttr = xcls.classProperties;
                        //    if (xcls.classProperties.BranchType != methodAttribute.PointType)
                        //    {
                        //        x.PointType = xcls.classProperties.BranchType;
                        //    }
                        //    else
                        //        x.PointType = methodAttribute.PointType;
                        //}
                        //else
                            x.PointType = methodAttribute.PointType;


                        x.Category = methodAttribute.Category;
                        x.DatasourceType = methodAttribute.DatasourceType;
                        x.ClassType = methodAttribute.ClassType;
                        x.misc = methodAttribute.misc;
                        x.ObjectType = methodAttribute.ObjectType;
                        x.Showin = methodAttribute.Showin;
                        xcls.Methods.Add(x);
                    }


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
            }
      
            return xcls;
        }
        #endregion "Class Extractors"
        #region "Helpers"
        /// <summary>
        /// Sends a progress update message.
        /// </summary>
        /// <param name="progress">The progress reporter to report the message.</param>
        /// <param name="token">A cancellation token for the task.</param>
        /// <param name="messege">The message to be sent. Default is null.</param>
        private void SendMessege(IProgress<PassedArgs> progress, CancellationToken token,  string messege = null)
        {
                         
                if (progress != null)
                {
                    PassedArgs ps = new PassedArgs { EventType = "Update", Messege=messege, ErrorCode = ErrorObject.Message };
                    progress.Report(ps);
                }
           
        }
        /// <summary>
        /// Creates an instance of a class from its type name.
        /// </summary>
        /// <param name="typeName">The fully qualified name of the type.</param>
        /// <param name="args">Arguments for the type constructor.</param>
        /// <returns>An instance of the specified type or null if the type cannot be created.</returns>
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
        /// <summary>
        /// Creates an instance of a class from its type name within a specific assembly.
        /// </summary>
        /// <param name="dll">The name of the DLL containing the type.</param>
        /// <param name="typeName">The fully qualified name of the type.</param>
        /// <param name="args">Arguments for the type constructor.</param>
        /// <returns>An instance of the specified type or null if the type cannot be created.</returns>
        public object CreateInstanceFromString(string dll,string typeName, params object[] args)
        {
            object instance = null;
            Type type = null;

            try
            {
                assemblies_rep dllas = Assemblies.Where(p => Path.GetFileName(p.DllName) == dll).FirstOrDefault();
                if(dllas != null)
                {
                    type = dllas.DllLib.GetType(typeName);
                    if (type == null)
                        return null;


                    instance = Activator.CreateInstance(type, args);
                }
                else
                {
                    instance= CreateInstanceFromString(typeName, args);
                }
           
            }
            catch( Exception ex) 
            {
                return null;
            }

            return instance;
        }
        /// <summary>
        /// Handles the assembly resolution for the current application domain.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="args">Arguments related to the assembly resolve event.</param>
        /// <returns>The resolved assembly or null if the assembly cannot be resolved.</returns>
        public Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            // Ignore missing resources
            if (args.Name.Contains(".resources"))
                return null;
            string filename = args.Name.Split(',')[0] + ".dll".ToLower();
            string filenamewo = args.Name.Split(',')[0];
            // check for assemblies already loaded
            //   var s = AppDomain.CurrentDomain.GetAssemblies();
            if(LoadedAssemblies.FirstOrDefault(a => a.FullName == args.Name)!=null)
            {
                return LoadedAssemblies.FirstOrDefault(a => a.FullName == args.Name);
            }
           
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
            foreach (var moduleDir in  ConfigEditor.Config.Folders.Where(c =>  c.FolderFilesType == FolderFileTypes.OtherDLL))
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
            foreach (var moduleDir in  ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.ConnectionDriver))
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
            foreach (var moduleDir in  ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.ProjectClass))
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
        /// <summary>
        /// Creates an instance of a type specified by a fully qualified name.
        /// </summary>
        /// <param name="strFullyQualifiedName">The fully qualified name of the type.</param>
        /// <returns>An instance of the specified type or null if the type cannot be instantiated.</returns>
        public object GetInstance(string strFullyQualifiedName)
        {
            Type type = GetType(strFullyQualifiedName);
            if (type != null)
                return Activator.CreateInstance(type);
           
            return null;
        }
        /// <summary>
        /// Retrieves a type by its fully qualified name.
        /// </summary>
        /// <param name="strFullyQualifiedName">The fully qualified name of the type.</param>
        /// <returns>The type corresponding to the name, or null if it cannot be found.</returns>
        public Type GetType(string strFullyQualifiedName)
        {
            string[] assemblynamespace = strFullyQualifiedName.Split('.');

            Type type = Type.GetType(strFullyQualifiedName);
            if (type != null)
                return type;
            try
            {
                foreach (var asm in CurrentDomain.GetAssemblies().Where(o=>o.FullName.StartsWith(assemblynamespace[0])))
                {
                    try
                    {
                        type = asm.GetType(strFullyQualifiedName);
                    }
                    catch (MissingMethodException exin)
                    {

                        
                    }
                  
                    if (type != null)
                        return type;
                }
            }
            catch (Exception ex)
            {

               
            }
            try
            {
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

            }
            catch (Exception ex1)
            {

               
            }
            //assemblies_rep dllas = Assemblies.Where(p=>p.DllLib.FullName== assemblynamespace[0]).FirstOrDefault();

            //type = dllas.DllLib.GetType(strFullyQualifiedName);
            foreach (var item in Assemblies)
            {
                var assembly = item.DllLib;
                try
                {
                    //Console.WriteLine(assembly.FullName);
                    //if (assembly.FullName.Contains("MsDashboardFunctions"))
                    //{
                    //    Console.WriteLine("Found");
                    //}
                    type = assembly.GetType(strFullyQualifiedName);
                    if (type != null)
                        return type;
                }
                catch (Exception ex2)
                {

                    throw;
                }
            }
               

                return null;
        }
        // <summary>
        /// Executes a method on an object instance using reflection.
        /// </summary>
        /// <param name="ObjInstance">The object instance on which to invoke the method.</param>
        /// <param name="FullClassName">The full name of the class containing the method.</param>
        /// <param name="MethodName">The name of the method to run.</param>
        /// <returns>True if the method runs successfully, false otherwise.</returns>
        public bool RunMethod(object ObjInstance, string FullClassName, string MethodName)
        {

            try
            {
                AssemblyClassDefinition cls =  ConfigEditor.BranchesClasses.Where(x => x.className == FullClassName).FirstOrDefault();
                dynamic obj = GetInstance(cls.PackageName);
                obj = ObjInstance;
                MethodInfo method = cls.Methods.Where(x => x.Caption == MethodName).FirstOrDefault().Info;
                method.Invoke(ObjInstance, null);
                return true;
                //    //DMEEditor.AddLogMessage("Success", "Running method", DateTime.Now, 0, null, Errors.Ok);
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
        /// <summary>
        /// Checks and updates the list of driver configurations to ensure no duplicates exist.
        /// </summary>
        public void CheckDriverAlreadyExistinList()
        {
            
            foreach (ConnectionDriversConfig dr in DataDriversConfig)
            {
                ConfigEditor.AddDriver(dr);
            }
        }
        /// <summary>
        /// Extracts and configures ADO.NET type driver information from an assembly.
        /// </summary>
        /// <param name="asm">The assembly to scan for ADO.NET drivers.</param>
        /// <returns>True if drivers are successfully extracted, false otherwise.</returns>
        private bool GetADOTypeDrivers(Assembly asm)
        {
            ConnectionDriversConfig driversConfig = new ConnectionDriversConfig();
            bool retval;
            string[] p;
            Type[] t;
            List<Type> t1;
            try
            {
                if (asm.ExportedTypes != null)
                {
                    t = asm.ExportedTypes.ToArray();
                }
                else
                {
                    //t1 = asm.GetTypes().Where(typeof(IDbDataAdapter).IsAssignableFrom).ToList() ;
                    //t1.AddRange(asm.GetTypes().Where(typeof(IDataConnection).IsAssignableFrom).ToList());
                    //t1.AddRange(asm.GetTypes().Where(e=>e.BaseType.ToString().Contains("DbCommandBuilder")).ToList());
                    //t1.AddRange(asm.GetTypes().Where(typeof(IDbTransaction).IsAssignableFrom).ToList());
                     t1 = asm.GetTypes().Where(type => typeof(IDbDataAdapter).IsAssignableFrom(type)).ToList();
                    t1.AddRange(asm.GetTypes().Where(type => typeof(IDbConnection).IsAssignableFrom(type)).ToList());
                    t1.AddRange(asm.GetTypes().Where(type => type.BaseType != null && type.BaseType.ToString().Contains("DbCommandBuilder")).ToList());
                    t1.AddRange(asm.GetTypes().Where(type => typeof(IDbTransaction).IsAssignableFrom(type)).ToList());
                    t1.AddRange(asm.GetTypes().Where(type => type.IsSubclassOf(typeof(DbConnection))).ToList());
                    t1.AddRange(asm.GetTypes().Where(type => type.IsSubclassOf(typeof(DbCommand))).ToList());
                    t1.AddRange(asm.GetTypes().Where(type => type.IsSubclassOf(typeof(DbDataReader))).ToList());
                    t1.AddRange(asm.GetTypes().Where(type => type.IsSubclassOf(typeof(DbParameter))).ToList());
                    t1.AddRange(asm.GetTypes().Where(type => type.IsSubclassOf(typeof(DbTransaction))).ToList());
                    t = t1.ToArray();
                }
               // Console.WriteLine(asm.FullName);
                foreach (var mytype in t)
                {
                    try
                    {
                        if (mytype.BaseType != null)
                        {
                            TypeInfo type = mytype.GetTypeInfo();
                            p = asm.FullName.Split(new char[] { ',' });
                            p[1] = p[1].Substring(p[1].IndexOf("=") + 1);

                            driversConfig = DataDriversConfig.Where(c => c.DriverClass == p[0]).FirstOrDefault();
                            bool recexist = false;

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
                            if (type.ImplementedInterfaces.Contains(typeof(IDbDataAdapter)) )
                            {
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
                            if (type.BaseType.ToString().Contains("DbCommandBuilder"))
                            {

                                driversConfig.CommandBuilderType = type.FullName;
                                driversConfig.version = p[1];
                                driversConfig.PackageName = p[0];
                                driversConfig.DriverClass = p[0];
                                driversConfig.dllname = type.Module.Name;
                                driversConfig.ADOType = true;
                                if (recexist == false)
                                {
                                    DataDriversConfig.Add(driversConfig);
                                }
                            }
                            if (type.ImplementedInterfaces.Contains(typeof(IDbConnection))|| typeof(DbConnection).IsAssignableFrom(type))
                            {
                                driversConfig.DbConnectionType = type.FullName;
                                driversConfig.PackageName = p[0];
                                driversConfig.DriverClass = p[0];
                                driversConfig.version = p[1];
                                driversConfig.dllname = type.Module.Name;
                                driversConfig.ADOType = true;
                                if (recexist == false)
                                {
                                    DataDriversConfig.Add(driversConfig);
                                }
                            }
                            if (type.ImplementedInterfaces.Contains(typeof(IDbTransaction)) || typeof(DbTransaction).IsAssignableFrom(type))
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
                            }
                        }
                    }
                    catch (Exception ex)
                    {
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
                    }
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
                //DMEEditor.AddLogMessage("Success", $"Got ADO Type Drivers {asm.GetName().ToString()}", DateTime.Now, -1, asm.GetName().ToString(), Errors.Ok);
            }
            catch (Exception)
            {
                t = null;
                retval = false;
                //DMEEditor.AddLogMessage("Failed", $"Could not ADO Type Drivers for {asm.GetName().ToString()}", DateTime.Now, -1, asm.GetName().ToString(), Errors.Failed);
            }
            return retval;
        }
        /// <summary>
        /// Configures non-ADO.NET type driver information based on predefined driver definitions.
        /// </summary>
        //private void GetNonADODrivers()
        //{
        //    ConnectionDriversConfig driversConfig = new ConnectionDriversConfig();
        //    try
        //    {
        //        foreach (ConnectionDriversConfig item in  ConfigEditor.DriverDefinitionsConfig)
        //        {
        //            driversConfig = DataDriversConfig.Where(c => c.PackageName == item.PackageName).FirstOrDefault();
        //            if (driversConfig == null)
        //            {
        //                driversConfig = new ConnectionDriversConfig();
        //                driversConfig.version = item.version;
        //                driversConfig.PackageName = item.PackageName;
        //                driversConfig.DriverClass = item.DriverClass;
        //                driversConfig.dllname = item.dllname;
        //                driversConfig.parameter1 = item.parameter1;
        //                driversConfig.parameter2 = item.parameter2;
        //                driversConfig.parameter3 = item.parameter3;
        //                DataDriversConfig.Add(driversConfig);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //    }
        //}
        /// <summary>
        /// Retrieves a list of driver configurations from an assembly.
        /// </summary>
        /// <param name="asm">The assembly to scan for drivers.</param>
        /// <returns>A list of driver configurations.</returns>
        public List<ConnectionDriversConfig> GetDrivers(Assembly asm)
        {
            try
            { 
                if (asm.GetType() != null)
                {
                    GetADOTypeDrivers(asm);
                }
                
            }
            catch (Exception ex1)
            {
 
            }
            return DataDriversConfig;
        }
        /// <summary>
        /// Creates a list of file extensions supported by the data sources.
        /// </summary>
        /// <returns>A list of file extension strings.</returns>
        public List<string> CreateFileExtensionString()
        {
            List<AssemblyClassDefinition> cls = DataSourcesClasses.Where(o => o.classProperties != null).ToList();
            IEnumerable<string> extensionslist = cls.Where(o => o.classProperties.Category == DatasourceCategory.FILE).Select(p => p.classProperties.FileType);
            string extstring = string.Join(",", extensionslist);
            return extstring.Split(',').ToList() ;
        }
        /// <summary>
        /// Adds default engine drivers to the driver configurations.
        /// </summary>
        /// <returns>True if default drivers are successfully added, false otherwise.</returns>
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
                return false;
            };
        }
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing">Indicates whether the method call comes from a Dispose method (its value is true) or from a finalizer (its value is false).</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    LoaderExtensions = null;
                    LoaderExtensionClasses = null;
                    Assemblies = null;
                    // public List<IDM_Addin> AddIns { get; set; } = new List<IDM_Addin>();
                    DataSourcesClasses = null;
                    DataDriversConfig = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~AssemblyHandler()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion

    }
}
