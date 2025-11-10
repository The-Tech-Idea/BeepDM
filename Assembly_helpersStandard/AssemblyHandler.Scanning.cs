using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Workflow;

namespace TheTechIdea.Beep.Tools
{
    /// <summary>
    /// AssemblyHandler partial class - Assembly Scanning Methods
    /// </summary>
    public partial class AssemblyHandler
    {
        #region Scanning Methods

        /// <summary>
        /// Scan an assembly for all types and add to appropriate collections
        /// </summary>
        public bool ScanAssembly(Assembly asm)
        {
            if (asm == null) return false;

            Type[] types;

            try
            {
                try
                {
                    types = asm.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types.Where(t => t != null).ToArray();
                }
                catch (Exception)
                {
                    types = asm.GetExportedTypes();
                }

                if (types == null || types.Length == 0) return false;

                // Use parallel processing for large assemblies
                if (types.Length > 100)
                {
                    var typeInfoCollection = new ConcurrentBag<TypeInfo>();

                    // Process types in parallel and collect the TypeInfo objects
                    Parallel.ForEach(types, type =>
                    {
                        try
                        {
                            TypeInfo typeInfo = type.GetTypeInfo();
                            typeInfoCollection.Add(typeInfo);
                        }
                        catch { }
                    });

                    // Process the collected TypeInfo objects
                    foreach (var typeInfo in typeInfoCollection)
                    {
                        ProcessTypeInfo(typeInfo, asm);
                    }
                }
                else
                {
                    // For smaller assemblies, use sequential processing
                    foreach (var type in types)
                    {
                        try
                        {
                            TypeInfo typeInfo = type.GetTypeInfo();
                            ProcessTypeInfo(typeInfo, asm);
                        }
                        catch { }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"ScanAssembly: Failed to scan assembly: {asm.GetName().Name} - {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Process a TypeInfo and add to appropriate collections based on implemented interfaces
        /// </summary>
        private void ProcessTypeInfo(TypeInfo typeInfo, Assembly asm)
        {
            try
            {
                // Check for ILoaderExtention interface
                if (typeInfo.ImplementedInterfaces.Contains(typeof(ILoaderExtention)))
                {
                    LoaderExtensions.Add(typeInfo);
                    LoaderExtensionClasses.Add(GetAssemblyClassDefinition(typeInfo, "ILoaderExtention"));
                }

                // Check for IDataSource interface
                if (typeInfo.ImplementedInterfaces.Contains(typeof(IDataSource)))
                {
                    var xcls = GetAssemblyClassDefinition(typeInfo, "IDataSource");
                    DataSourcesClasses.Add(xcls);
                    ConfigEditor.DataSourcesClasses.Add(xcls);
                }

                // Check for IWorkFlowAction interface
                if (typeInfo.ImplementedInterfaces.Contains(typeof(IWorkFlowAction)))
                {
                    ConfigEditor.WorkFlowActions.Add(GetAssemblyClassDefinition(typeInfo, "IWorkFlowAction"));
                }

                // Check for IDM_Addin interface
                if (typeInfo.ImplementedInterfaces.Contains(typeof(IDM_Addin)))
                {
                    ConfigEditor.Addins.Add(GetAssemblyClassDefinition(typeInfo, "IDM_Addin"));
                }

                // Check for IWorkFlowStep interface
                if (typeInfo.ImplementedInterfaces.Contains(typeof(IWorkFlowStep)))
                {
                    ConfigEditor.WorkFlowSteps.Add(GetAssemblyClassDefinition(typeInfo, "IWorkFlowStep"));
                }

                // Check for IBeepViewModel interface
                if (typeInfo.ImplementedInterfaces.Contains(typeof(IBeepViewModel)))
                {
                    ConfigEditor.ViewModels.Add(GetAssemblyClassDefinition(typeInfo, "IBeepViewModel"));
                }

                // Check for IWorkFlowEditor interface
                if (typeInfo.ImplementedInterfaces.Contains(typeof(IWorkFlowEditor)))
                {
                    ConfigEditor.WorkFlowStepEditors.Add(GetAssemblyClassDefinition(typeInfo, "IWorkFlowEditor"));
                }

                // Check for IWorkFlowRule interface
                if (typeInfo.ImplementedInterfaces.Contains(typeof(IWorkFlowRule)))
                {
                    ConfigEditor.Rules.Add(GetAssemblyClassDefinition(typeInfo, "IWorkFlowRule"));
                }

                // Check for IFunctionExtension interface
                if (typeInfo.ImplementedInterfaces.Contains(typeof(IFunctionExtension)))
                {
                    ConfigEditor.GlobalFunctions.Add(GetAssemblyClassDefinition(typeInfo, "IFunctionExtension"));
                }

                // Check for IPrintManager interface
                if (typeInfo.ImplementedInterfaces.Contains(typeof(IPrintManager)))
                {
                    ConfigEditor.PrintManagers.Add(GetAssemblyClassDefinition(typeInfo, "IPrintManager"));
                }

                // Check for IAddinVisSchema interface
                if (typeInfo.ImplementedInterfaces.Contains(typeof(IAddinVisSchema)))
                {
                    GetAddinObjects(asm);
                }
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"ProcessTypeInfo: Error processing type {typeInfo?.FullName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Scan assembly specifically for data source implementations
        /// </summary>
        private bool ScanAssemblyForDataSources(Assembly asm)
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
                    try
                    {
                        t = asm.GetExportedTypes();
                    }
                    catch (Exception ex3)
                    {
                        t = null;
                    }
                }

                if (t != null)
                {
                    foreach (var mytype in t)
                    {
                        TypeInfo type = mytype.GetTypeInfo();
                        
                        // Get DataBase Implementation Classes
                        if (type.ImplementedInterfaces.Contains(typeof(IDataSource)))
                        {
                            AssemblyClassDefinition xcls = GetAssemblyClassDefinition(type, "IDataSource");
                            DataSourcesClasses.Add(xcls);
                            ConfigEditor.DataSourcesClasses.Add(xcls);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"ScanAssemblyForDataSources: Error - {ex.Message}");
            }

            return true;
        }

        /// <summary>
        /// Scan extension loaders from assemblies
        /// </summary>
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
                            skip = false;
                            
                            // Filter using namespaces to ignore
                            if (NamespacestoIgnore.Count > 0)
                            {
                                if (NamespacestoIgnore.Any(ns => assembly1.FullName.Contains(ns)))
                                {
                                    skip = true;
                                }
                            }
                            
                            if (!assembly1.FullName.StartsWith("System") && 
                                !assembly1.FullName.StartsWith("Microsoft") && 
                                skip == false)
                            {
                                cls.Scan(assembly1);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger?.WriteLog($"ScanExtensions: Error scanning assembly {assembly1.FullName}: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger?.WriteLog($"ScanExtensions: Error creating extension instance: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Scan a single extension in an assembly
        /// </summary>
        private void ScanExtension(Assembly assembly)
        {
            foreach (Type item in LoaderExtensions)
            {
                try
                {
                    ILoaderExtention cls = (ILoaderExtention)Activator.CreateInstance(item, new object[] { this });
                    cls.Scan(assembly);
                }
                catch (Exception ex)
                {
                    Logger?.WriteLog($"ScanExtension: Error - {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Scan assemblies for drivers
        /// </summary>
        private void ScanForDrivers()
        {
            foreach (assemblies_rep item in Assemblies.Where(c =>
                c.FileTypes == FolderFileTypes.ConnectionDriver ||
                c.FileTypes == FolderFileTypes.Builtin).ToList())
            {
                GetDrivers(item.DllLib);
            }
        }

        /// <summary>
        /// Scan assemblies for data sources
        /// </summary>
        private void ScanForDataSources()
        {
            foreach (assemblies_rep item in Assemblies.Where(c => c.FileTypes == FolderFileTypes.DataSources).ToList())
            {
                ScanAssemblyForDataSources(item.DllLib);
            }
        }

        /// <summary>
        /// Scan project and addin assemblies
        /// </summary>
        private void ScanProjectAndAddinAssemblies()
        {
            foreach (assemblies_rep s in Assemblies.Where(x => 
                x.FileTypes == FolderFileTypes.ProjectClass || 
                x.FileTypes == FolderFileTypes.Addin))
            {
                try
                {
                    ScanAssembly(s.DllLib);
                }
                catch (Exception ex)
                {
                    ErrorObject.Flag = Errors.Failed;
                    Logger?.WriteLog($"ScanProjectAndAddinAssemblies: Error - {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Scan extensions in project and addin assemblies
        /// </summary>
        private void ScanExtensionsInAssemblies()
        {
            foreach (assemblies_rep s in Assemblies.Where(x => 
                x.FileTypes == FolderFileTypes.ProjectClass || 
                x.FileTypes == FolderFileTypes.Addin))
            {
                try
                {
                    ScanExtension(s.DllLib);
                }
                catch (Exception ex)
                {
                    ErrorObject.Flag = Errors.Failed;
                    Logger?.WriteLog($"ScanExtensionsInAssemblies: Error - {ex.Message}");
                }
            }
        }

        #endregion
    }
}
