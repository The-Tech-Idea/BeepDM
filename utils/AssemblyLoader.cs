using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.Workflow;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.Tools
{
    public class AssemblyLoader : IAssemblyLoader
    {

        ParentChildObject a;
        public AppDomain CurrentDomain { get; set; }

        private string Name { get; set; }
        private string Descr { get; set; }
        public IDMLogger Logger { get; set; }
        public IErrorsInfo Erinfo { get; set; }
        public IDMEEditor DME_editor { get; set; }
        public List<assemblies_rep> Assemblies { get; set; } = new List<assemblies_rep>();
        public List<IDM_Addin> AddIns { get; set; } = new List<IDM_Addin>();
        public List<DataSourceClasses> DataSources { get; set; } = new List<DataSourceClasses>();
        private List<ConnectionDriversConfig> DataDrivers = new List<ConnectionDriversConfig>();
        public AssemblyLoader(IDMEEditor pDME_editor, IDMLogger logger, IErrorsInfo per)
        {
            Logger = logger;
            DME_editor = pDME_editor;
            Erinfo = per;
             CurrentDomain = AppDomain.CurrentDomain;
            CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }
        public IErrorsInfo LoadOtherAssemblies()
        {
            Erinfo.Flag = Errors.Ok;
            string res;
            try
            {

                foreach (string p in DME_editor.ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.OtherDLL).Select(x => x.FolderPath))
                {


                    try
                    {

                        LoadAssembly(p, FolderFileTypes.OtherDLL);

                    }
                    catch (FileLoadException loadEx)
                    {
                        Erinfo.Flag = Errors.Failed;
                        res = "The Assembly has already been loaded" + loadEx.Message;
                        // MessageBox.Show("The Assembly has already been loaded" + loadEx.Message, "Simple ODM", MessageBoxButtons.OK);
                    } // The Assembly has already been loaded.
                    catch (BadImageFormatException imgEx)
                    {
                        Erinfo.Flag = Errors.Failed;
                        // MessageBox.Show(imgEx.Message, "Simple ODM", MessageBoxButtons.OK);  // If a BadImageFormatException exception is thrown, the file is not an assembly.
                        res = imgEx.Message;
                    }
                    catch (Exception ex)
                    {
                        Erinfo.Flag = Errors.Failed;
                        // MessageBox.Show(ex.Message, "Simple ODM", MessageBoxButtons.OK);  // If a BadImageFormatException exception is thrown, the file is not an assembly
                        res = ex.Message;
                    }

                }
     

            }
            catch (System.Exception ex)
            {

                Erinfo.Flag = Errors.Failed;
                Erinfo.Ex = ex;
                Logger.WriteLog($"Error Loading Addin Assemblies ({ex.Message})");
            }
            return Erinfo;
        }
        public IErrorsInfo LoadProjectClassesAssemblies()
        {
            Erinfo.Flag = Errors.Ok;
            string res;
            try
            {

                foreach (string p in DME_editor.ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.ProjectClass).Select(x => x.FolderPath))
                {


                    try
                    {
                     
                        LoadAssembly(p,FolderFileTypes.ProjectClass);

                    }
                    catch (FileLoadException loadEx)
                    {
                        Erinfo.Flag = Errors.Failed;
                        res = "The Assembly has already been loaded" + loadEx.Message;
                        // MessageBox.Show("The Assembly has already been loaded" + loadEx.Message, "Simple ODM", MessageBoxButtons.OK);
                    } // The Assembly has already been loaded.
                    catch (BadImageFormatException imgEx)
                    {
                        Erinfo.Flag = Errors.Failed;
                        // MessageBox.Show(imgEx.Message, "Simple ODM", MessageBoxButtons.OK);  // If a BadImageFormatException exception is thrown, the file is not an assembly.
                        res = imgEx.Message;
                    }
                    catch (Exception ex)
                    {
                        Erinfo.Flag = Errors.Failed;
                        // MessageBox.Show(ex.Message, "Simple ODM", MessageBoxButtons.OK);  // If a BadImageFormatException exception is thrown, the file is not an assembly
                        res = ex.Message;
                    }

                }
                foreach (assemblies_rep s in Assemblies.Where(x => x.FileTypes == FolderFileTypes.ProjectClass))
                {
                    try
                    {

                        ScanAssembly(s.DllLib);

                    }
                    catch (FileLoadException loadEx)
                    {
                        Erinfo.Flag = Errors.Failed;
                        res = "The Assembly has already been loaded" + loadEx.Message;
                        // MessageBox.Show("The Assembly has already been loaded" + loadEx.Message, "Simple ODM", MessageBoxButtons.OK);
                    } // The Assembly has already been loaded.
                    catch (BadImageFormatException imgEx)
                    {
                        Erinfo.Flag = Errors.Failed;
                        // MessageBox.Show(imgEx.Message, "Simple ODM", MessageBoxButtons.OK);  // If a BadImageFormatException exception is thrown, the file is not an assembly.
                        res = imgEx.Message;
                    }
                    catch (Exception ex)
                    {
                        Erinfo.Flag = Errors.Failed;
                        // MessageBox.Show(ex.Message, "Simple ODM", MessageBoxButtons.OK);  // If a BadImageFormatException exception is thrown, the file is not an assembly
                        res = ex.Message;
                    }

                }


            }
            catch (System.Exception ex)
            {

                Erinfo.Flag = Errors.Failed;
                Erinfo.Ex = ex;
                Logger.WriteLog($"Error Loading Addin Assemblies ({ex.Message})");
            }
            return Erinfo;
        }
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

                                DataSourceClasses xcls = new DataSourceClasses();
                                xcls.className = type.Name;
                                xcls.dllname = type.Module.Name;
                                xcls.PackageName = type.FullName;
                                DataSources.Add(xcls);
                                DME_editor.ConfigEditor.DataSources.Add(xcls);


                            }
                            if (type.ImplementedInterfaces.Contains(typeof(IWorkFlowAction)))
                            {

                                DataSourceClasses xcls = new DataSourceClasses();
                                xcls.className = type.Name;
                                xcls.dllname = type.Module.Name;
                                xcls.PackageName = type.FullName;
                                DataSources.Add(xcls);
                                DME_editor.WorkFlowEditor.WorkFlowActions.Add(xcls);


                            }
                        }
                    }
                 
                }
                catch (Exception ex)
                {

                    string mes = "";
                    DME_editor.AddLogMessage(ex.Message, "Could not exported  types" + mes, DateTime.Now, -1, mes, Errors.Failed);
                };

                return true;
            }
            catch (Exception ex)
            {
                string mes = "";
                DME_editor.AddLogMessage(ex.Message, "Could not scan assembly " + mes, DateTime.Now, -1, mes, Errors.Failed);
                return false;
            };
        }
        public IErrorsInfo GetBuiltinClasses()
        {
            Erinfo.Flag = Errors.Ok;
            DataSources = new List<DataSourceClasses>();
            // look through assembly list
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(x => x.FullName.Contains("DataManagerEditors"));

            // try to find manually
            foreach (Assembly asm in assemblies)
            {

                try
                {
                    foreach (var type in asm.DefinedTypes)
                    {


                        string[] p = asm.FullName.Split(new char[] { ',' });
                        p[1] = p[1].Substring(p[1].IndexOf("=") + 1);
                        //-------------------------------------------------------
                        // Get DataBase Drivers
                        if (type.ImplementedInterfaces.Contains(typeof(IDataSource)))
                        {

                            DataSourceClasses xcls = new DataSourceClasses();
                            xcls.className = type.Name;
                            xcls.dllname = type.Module.Name;
                            xcls.PackageName = type.FullName;
                            DataSources.Add(xcls);
                            DME_editor.ConfigEditor.DataSources.Add(xcls);


                        }
                        if (type.ImplementedInterfaces.Contains(typeof(IWorkFlowAction)))
                        {

                            DataSourceClasses xcls = new DataSourceClasses();
                            xcls.className = type.Name;
                            xcls.dllname = type.Module.Name;
                            xcls.PackageName = type.FullName;
                            DataSources.Add(xcls);
                            DME_editor.WorkFlowEditor.WorkFlowActions.Add(xcls);


                        }

                    }



                    //-----------------------------------------------------------
                }
                catch (Exception ex)
                {

                    Logger.WriteLog($"error loading Database drivers {ex.Message} ");
                }
            }
            return Erinfo;

        }
        //public IErrorsInfo GetWorkFlowActionsClasses()
        //{
        //    Erinfo.Flag = Errors.Ok;
        //    DME_editor.WorkFlowEditor.WorkFlowActions = new BindingList<DataSourceClasses>();
        //    // look through assembly list
        //    //var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        //    // try to find manually
        //    foreach (string path in DME_editor.ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.ProjectClass).Select(x => x.FolderPath))
        //    {
        //        foreach (string dll in Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories))
        //        {
        //            Assembly loadedAssembly = Assembly.LoadFile(dll);

        //                try
        //                {
        //                    foreach (var type in loadedAssembly.DefinedTypes)
        //                    {


        //                        string[] p = loadedAssembly.FullName.Split(new char[] { ',' });
        //                        p[1] = p[1].Substring(p[1].IndexOf("=") + 1);
        //                        //-------------------------------------------------------
        //                        // Get DataBase Drivers
        //                        if (type.ImplementedInterfaces.Contains(typeof(IWorkFlowAction)))
        //                        {

        //                            DataSourceClasses xcls = new DataSourceClasses();
        //                            xcls.className = type.Name;
        //                            xcls.dllname = type.Module.Name;
        //                            xcls.PackageName = type.FullName;
        //                            DataSources.Add(xcls);
        //                            DME_editor.WorkFlowEditor.WorkFlowActions.Add(xcls);


        //                        }
        //                    }



        //                    //-----------------------------------------------------------
        //                }
        //                catch (Exception ex)
        //                {

        //                    Logger.WriteLog($"error loading Database drivers {ex.Message} ");
        //                }

        //        }


        //    }


        //    return Erinfo;

        //}
        //---------------------------------------------------------------------
        // Method to load Dll from Addin Folder
        //---------------------------------------------------------------------
        public IErrorsInfo LoadAddinAssemblies()
        {
            Erinfo.Flag = Errors.Ok;
            Erinfo.Flag = Errors.Ok;
            string res = "";
            DME_editor.Utilfunction.Rootnamespacelist = new List<ParentChildObject>();
            DME_editor.Utilfunction.Namespacelist = new List<string>();
            DME_editor.Utilfunction.Classlist = new List<string>();
            try
            {

                foreach (string p in DME_editor.ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.Addin).Select(x => x.FolderPath))
                {

                    LoadAssembly(p,FolderFileTypes.Addin);
                }
                foreach (assemblies_rep s in Assemblies.Where(x=>x.FileTypes==FolderFileTypes.Addin))
                {
                    try
                    {
                        DME_editor.Utilfunction.Rootnamespacelist = GetClasses(s.DllLib);

                    }
                    catch (FileLoadException loadEx)
                    {
                        Erinfo.Flag = Errors.Failed;
                        res = "The Assembly has already been loaded" + loadEx.Message;
                        // MessageBox.Show("The Assembly has already been loaded" + loadEx.Message, "Simple ODM", MessageBoxButtons.OK);
                    } // The Assembly has already been loaded.
                    catch (BadImageFormatException imgEx)
                    {
                        Erinfo.Flag = Errors.Failed;
                        // MessageBox.Show(imgEx.Message, "Simple ODM", MessageBoxButtons.OK);  // If a BadImageFormatException exception is thrown, the file is not an assembly.
                        res = imgEx.Message;
                    }
                    catch (Exception ex)
                    {
                        Erinfo.Flag = Errors.Failed;
                        // MessageBox.Show(ex.Message, "Simple ODM", MessageBoxButtons.OK);  // If a BadImageFormatException exception is thrown, the file is not an assembly
                        res = ex.Message;
                    }

                }


            }
            catch (System.Exception ex)
            {

                Erinfo.Flag = Errors.Failed;
                Erinfo.Ex = ex;
                Logger.WriteLog($"Error Loading Addin Assemblies ({ex.Message})");
            }
            return Erinfo;
        }
        public ParentChildObject GetObject(string p, string parentid, string Objt)
        {

            ParentChildObject a;
            if (parentid == null)
            {
                if (DME_editor.Utilfunction.Rootnamespacelist.Where(f => f.id == p && f.ObjType == Objt).Count() == 0)
                {
                    a = new ParentChildObject() { id = p, ParentID = null, ObjType = Objt, AddinName = Name, Description = Descr };
                    DME_editor.Utilfunction.Rootnamespacelist.Add(a);

                }
                else
                {
                    a = DME_editor.Utilfunction.Rootnamespacelist.Where(f => f.id == p && f.ParentID == null && f.ObjType == Objt).FirstOrDefault();

                }

            }
            else
            {
                if (DME_editor.Utilfunction.Rootnamespacelist.Where(f => f.id == p && f.ParentID == parentid && f.ObjType == Objt).Count() == 0)
                {
                    a = new ParentChildObject() { id = p, ParentID = parentid, ObjType = Objt, AddinName = Name, Description = Descr };
                    DME_editor.Utilfunction.Rootnamespacelist.Add(a);

                }
                else
                {
                    a = DME_editor.Utilfunction.Rootnamespacelist.Where(f => f.id == p && f.ParentID == parentid && f.ObjType == Objt).FirstOrDefault();

                }
            }

            return a;
        }
        public List<ParentChildObject> GetClasses(Assembly asm)
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
                            // DME_editor.Utilfunction.Namespacelist.Add(type.Name);
                            //  a = GetObject(type.Name, null, "namespace");
                            Logger.WriteLog($" NameSpaces {type.Namespace} ");

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
                                        a = GetObject(p[i], null, "namespace");
                                        Logger.WriteLog($"  getting namespace first {p[i]} ");
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
                                                    addin.ObjectName = uc.ObjectName;
                                                    addin.DllName = Path.GetFileName(asm.Location);
                                                    Show = addin.DefaultCreate;
                                                    AddIns.Add(addin);
                                                }
                                                Name = addin.AddinName;
                                                Descr = addin.Description;

                                                if (addin.DefaultCreate)
                                                {
                                                    Show = true;
                                                }
                                                else Show = false;
                                                // objtype = "UserControl";
                                                Logger.WriteLog($"  getting object {p[i]} ");
                                                if (Show)
                                                {
                                                    a = GetObject(p[i], p[i - 1], objtype);
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                string mes = ex.Message;
                                                DME_editor.AddLogMessage(ex.Message, "Could" + mes, DateTime.Now, -1, mes, Errors.Failed);
                                            };


                                        }
                                        else
                                        {
                                            Name = p[i];
                                            Descr = p[i];
                                            a = GetObject(p[i], p[i - 1], "namespace");
                                            Logger.WriteLog($"  getting namespace in middle {p[i]} ");
                                        }

                                    }

                                    Logger.WriteLog($" split {p[i]} ");
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
       

            return DME_editor.Utilfunction.Rootnamespacelist;
        }
        //---------------------------------------------------------------------
        // Methods to load dll from Project Drivers Folder to support
        // Data Source Connections
        //---------------------------------------------------------------------
        private string LoadAssembly(string path, FolderFileTypes fileTypes)
        {
            // Dim binPath As String = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "bin")
            // note: don't use CurrentEntryAssembly or anything like that.
            Erinfo.Flag = Errors.Ok;
            string res = "";

            foreach (string dll in Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories))
            {
                try
                {
                    Assembly loadedAssembly = Assembly.LoadFile(dll);

                    assemblies_rep x = new assemblies_rep(loadedAssembly, path, dll, fileTypes);
                    Assemblies.Add(x);
                 


                }
                catch (FileLoadException loadEx)
                {
                    Erinfo.Flag = Errors.Failed;
                    res = "The Assembly has already been loaded" + loadEx.Message;
                    // MessageBox.Show("The Assembly has already been loaded" + loadEx.Message, "Simple ODM", MessageBoxButtons.OK);
                } // The Assembly has already been loaded.
                catch (BadImageFormatException imgEx)
                {
                    Erinfo.Flag = Errors.Failed;
                    // MessageBox.Show(imgEx.Message, "Simple ODM", MessageBoxButtons.OK);  // If a BadImageFormatException exception is thrown, the file is not an assembly.
                    res = imgEx.Message;
                }
                catch (Exception ex)
                {
                    Erinfo.Flag = Errors.Failed;
                    // MessageBox.Show(ex.Message, "Simple ODM", MessageBoxButtons.OK);  // If a BadImageFormatException exception is thrown, the file is not an assembly
                    res = ex.Message;
                }
            }


            Erinfo.Message = res;
            return res;
        }
        public IErrorsInfo LoadConnectionDriversAssemblies()
        {

            Erinfo.Flag = Errors.Ok;
             DataDrivers = new List<ConnectionDriversConfig>();
            string res = "";

            try
            {

                foreach (string p in DME_editor.ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.ConnectionDriver).Select(x => x.FolderPath))
                {
                  //  var domain = AppDomain.CreateDomain("Drivers");

                    foreach (string dll in Directory.GetFiles(p, "*.dll", SearchOption.AllDirectories))
                    {
                        try
                        {
                            Assembly loadedAssembly = Assembly.LoadFile(dll);
                            if (loadedAssembly.FullName.Contains("MySql"))
                            {
                                Debug.WriteLine("found");
                            }
                            GetDrivers(loadedAssembly);
                            LoadChildReferences(loadedAssembly);


                        }
                        catch (FileLoadException loadEx)
                        {
                            Erinfo.Flag = Errors.Failed;
                            res = "The Assembly has already been loaded" + loadEx.Message;
                            DME_editor.AddLogMessage("Assembly Loader for " + dll, res, DateTime.Now, -1, "", Errors.Failed);
                            // MessageBox.Show("The Assembly has already been loaded" + loadEx.Message, "Simple ODM", MessageBoxButtons.OK);
                        } // The Assembly has already been loaded.

                        catch (BadImageFormatException imgEx)
                        {
                            Erinfo.Flag = Errors.Failed;
                            // MessageBox.Show(imgEx.Message, "Simple ODM", MessageBoxButtons.OK);  // If a BadImageFormatException exception is thrown, the file is not an assembly.
                            res = imgEx.Message;
                            DME_editor.AddLogMessage("Assembly Loader for " + dll, res, DateTime.Now, -1, "", Errors.Failed);
                        }
                        catch (Exception ex)
                        {
                            Erinfo.Flag = Errors.Failed;
                            // MessageBox.Show(ex.Message, "Simple ODM", MessageBoxButtons.OK);  // If a BadImageFormatException exception is thrown, the file is not an assembly
                            res = ex.Message;
                            DME_editor.AddLogMessage("Assembly Loader for " + dll, res, DateTime.Now, -1, "", Errors.Failed);
                        }
                    }

                }
                AddEngineDefaultDrivers();
                CheckDriverAlreadyExistinList();

            }
            catch (System.Exception ex)
            {

                Erinfo.Flag = Errors.Failed;
                Erinfo.Ex = ex;
                Logger.WriteLog($"Error Loading Connection Drivers Assemblies ({ex.Message})");
            }
            return Erinfo;
        }
        public void CheckDriverAlreadyExistinList()
        {

            foreach (ConnectionDriversConfig dr in DataDrivers)
            {
                ConnectionDriversConfig founddr = DME_editor.ConfigEditor.DataDrivers.Where(c => c.DriverClass == dr.DriverClass && c.version == dr.version).FirstOrDefault();
                if (founddr == null)
                {
                    DME_editor.ConfigEditor.DataDrivers.Add(dr);
                }



            }
           // DME_editor.ConfigEditor.DataDrivers = DataDrivers;



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
            }else
                 t = asm.GetTypes();

            //if (t.Contains(typeof(IDbDataAdapter)))
            //{
                foreach (var mytype in t)
                {
                    try
                    {
                    if (mytype.FullName.Contains("DataAdapter"))
                    {
                        Debug.WriteLine("found");
                    }
                    TypeInfo type = mytype.GetTypeInfo();
                        p = asm.FullName.Split(new char[] { ',' });
                        p[1] = p[1].Substring(p[1].IndexOf("=") + 1);

                        driversConfig = DataDrivers.Where(c => c.DriverClass == p[0]).FirstOrDefault();
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
                            Logger.WriteLog($" NameSpaces {type.Namespace} ");
                            //IDataAdapter uc = (IDataAdapter)Activator.CreateInstance(type);

                            driversConfig.version = p[1];
                            driversConfig.AdapterType = type.FullName;
                            driversConfig.PackageName = p[0];
                            driversConfig.DriverClass = p[0];
                            driversConfig.dllname = type.Module.Name;
                            driversConfig.ADOType = true;
                            if (recexist == false)
                            {
                                DataDrivers.Add(driversConfig);
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
                                DataDrivers.Add(driversConfig);
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
                                DataDrivers.Add(driversConfig);
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
                                DataDrivers.Add(driversConfig);
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
           // }
      
            if (driversConfig.dllname == null)
            {
                p = asm.FullName.Split(new char[] { ',' });
                p[1] = p[1].Substring(p[1].IndexOf("=") + 1);
                //---------------------------------------------------------
                // Get NoSQL Drivers 
                bool driverfound = false;
                bool recexist = false;
                driversConfig = DataDrivers.Where(c => c.DriverClass == p[0]).FirstOrDefault();
                if (driversConfig == null)
                {
                    driversConfig = new ConnectionDriversConfig();
                    recexist = false;
                }
                else
                {
                    recexist = true;
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

                    DataDrivers.Add(driversConfig);
                }
            }

            if (driversConfig.dllname == null)
            {
                driversConfig= GetNonADODrivers( asm);
            }
           
            if (driversConfig.dllname == null)
            {
                retval = false;
            }
            else
                retval = true;
            return retval;
        }
        private ConnectionDriversConfig GetNonADODrivers(Assembly asm)
        {
            ConnectionDriversConfig driversConfig;
              try
                {
                    string[] p = asm.FullName.Split(new char[] { ',' });
                    p[1] = p[1].Substring(p[1].IndexOf("=") + 1);

                    //---------------------------------------------------------
                    // Get NoSQL Drivers 
                    bool driverfound = false;
                    bool recexist = false;
                    driversConfig = DataDrivers.Where(c => c.DriverClass == p[0]).FirstOrDefault();
                    if (driversConfig == null)
                    {
                        driversConfig = new ConnectionDriversConfig();
                        recexist = false;
                    }
                    else
                    {
                        recexist = true;
                    }
                    driversConfig.version = p[1];
                    driversConfig.PackageName = p[0];
                    driversConfig.DriverClass = p[0];
                    driversConfig.dllname =asm.ManifestModule.Name;

                    switch (p[0])
                    {
                        case "StackExchange.Redis":

                            driversConfig.AdapterType = p[0] + "." + "IDatabase";
                            driversConfig.DbConnectionType = p[0] + "." + "ConnectionMultiplexer";
                            driversConfig.CommandBuilderType = p[0] + "." + "ISubscriber";



                            break;


                        case "Couchbase.Lite":

                            driversConfig.version = p[1];
                            driversConfig.parameter2 = p[0] + "." + "DatabaseConfiguration";
                            driversConfig.parameter1 = p[0] + "." + "Database";


                            break;
                        case "Couchbase":


                            driversConfig.parameter2 = p[0] + "." + "Bucket";
                            driversConfig.parameter1 = p[0] + "." + "Cluster";
                            driversConfig.parameter3 = p[0] + "." + "Collection";


                            break;

                        case "MongoDB.Driver":
                            driversConfig.parameter2 = "BsonDocument";
                            driversConfig.parameter1 = "MongoClient";
                            driverfound = true;
                            break;
                        case "Elasticsearch.Net":
                            driversConfig.parameter1 = "ElasticLowLevelClient";
                            driversConfig.parameter2 = "ConnectionConfiguration";
                            driverfound = true;
                            break;
                        case "Cassandra.DataStax":
                            driversConfig.parameter2 = "Session";
                            driversConfig.parameter1 = "Cluster";
                            driverfound = true;
                            break;

                        case "Raven.Client":
                            driversConfig.parameter2 = "Sesssion";
                            driversConfig.parameter1 = "Database";
                            driversConfig.parameter3 = "Collection";
                            driverfound = true;
                            break;
                        default:
                        driverfound = true;
                            break;

                    }

                    if (driverfound)
                    {
                        if (recexist == false)
                        {
                            DataDrivers.Add(driversConfig);
                        }

                    }


                return driversConfig;

                    //-----------------------------------------------------------
               }
                catch (Exception ex)
                {

                    Logger.WriteLog($"error in creating addin {ex.Message} ");
                return null;
                }


            
        }
        public List<ConnectionDriversConfig> GetDrivers(Assembly asm)
        {
            int cnt = 1;


            try
            {
                if (asm.GetType() != null)
                {
                    GetADOTypeDrivers(asm);
                }
                else { GetNonADODrivers(asm); }
            }
            catch (Exception ex1)
            {

                Logger.WriteLog($"error Cannot find defined types from assembly {ex1.Message} ");
                try
                {

                    GetNonADODrivers(asm);

                }
                catch (Exception ex2)
                {

                    Logger.WriteLog($"error Cannot find exported types from assembly {ex2.Message} ");
                }
            }




            return DataDrivers;
            

           
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
                DataDrivers.Add(DataviewDriver);
                ConnectionDriversConfig TXTFileDriver = new ConnectionDriversConfig();
                TXTFileDriver.AdapterType = "DEFAULT";
                TXTFileDriver.dllname = "DataManagmentEngine";
                TXTFileDriver.PackageName = "FileReader";
                TXTFileDriver.DriverClass = "FileReader";
                TXTFileDriver.version = "1";
                DataDrivers.Add(TXTFileDriver);
                ConnectionDriversConfig JSONFileDriver = new ConnectionDriversConfig();
                JSONFileDriver.AdapterType = "DEFAULT";
                JSONFileDriver.dllname = "DataManagmentEngine";
                JSONFileDriver.PackageName = "JSONFileReader";
                JSONFileDriver.DriverClass = "JSONFileReader";
                JSONFileDriver.version = "1";
                DataDrivers.Add(JSONFileDriver);
                ConnectionDriversConfig WebAPIDriver = new ConnectionDriversConfig();
                WebAPIDriver.AdapterType = "DEFAULT";
                WebAPIDriver.dllname = "DataManagmentEngine";
                WebAPIDriver.PackageName = "WebApiReader";
                WebAPIDriver.DriverClass = "WebApiReader";
                WebAPIDriver.version = "1";

                DataDrivers.Add(WebAPIDriver);

                return true;
            }
            catch (Exception ex)
            {
                string mes = "";
                DME_editor.AddLogMessage(ex.Message, "Could not Add Driver" + mes, DateTime.Now, -1, mes, Errors.Failed);
                return false;
            };
        }
        public  Type GetTypeFromName(string typeName)
        {
            Type type = null;

            // Let default name binding find it
            type = Type.GetType(typeName, false);
            if (type != null)
                return type;

            // look through assembly list
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            // try to find manually
            foreach (Assembly asm in assemblies)
            {
                type = asm.GetType(typeName, false);

                if (type != null)
                    break;
            }
            return type;
        }
        public  object CreateInstanceFromString(string typeName, params object[] args)
        {
            object instance = null;
            Type type = null;

            try
            {
                type = GetTypeFromName(typeName);
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

            // check for assemblies already loaded
         //   var s = AppDomain.CurrentDomain.GetAssemblies();

            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == args.Name);
            if (assembly != null)
                return assembly;
            foreach (var moduleDir in DME_editor.ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.ConnectionDriver && c.FolderFilesType == FolderFileTypes.ProjectClass && c.FolderFilesType == FolderFileTypes.OtherDLL))
            {
                var di = new DirectoryInfo(moduleDir.FolderPath);
                var module = di.GetFiles().FirstOrDefault(i => i.Name == args.Name + ".dll");
                if (module != null)
                {
                    return Assembly.LoadFrom(module.FullName);
                }
            }
            return null;
            //// Try to load by filename - split out the filename of the full assembly name
            //// and append the base path of the original assembly (ie. look in the same dir)
            //string filename = args.Name.Split(',')[0] + ".dll".ToLower();

            //string asmFile = Path.Combine(@".\", "Addins", filename);

            //try
            //{
            //    return System.Reflection.Assembly.LoadFrom(asmFile);
            //}
            //catch (Exception ex)
            //{
            //    return null;
            //}
        }
        private static void LoadChildReferences(Assembly curAsm)
        {
            foreach (var assemblyName in curAsm.GetReferencedAssemblies())
            {
                try
                {
                    Assembly loadedAssembly = Assembly.LoadFile(assemblyName.FullName);
                }
                catch { }
            }
        }
        //---------------------------------------------------------------------

    }
}
