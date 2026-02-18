using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Vis;

namespace TheTechIdea.Beep.Tools.PluginSystem
{
    /// <summary>
    /// Assistant for assembly scanning operations - scans assemblies for types and interfaces
    /// </summary>
    public class AssemblyScanningAssistant : IDisposable
    {
        private readonly SharedContextManager _sharedContextManager;
        private readonly IDMLogger _logger;
        private readonly IConfigEditor _configEditor;
        // Removed _loaderExtensionClasses - now stored in SharedContextManager
        private bool _disposed = false;

        public AssemblyScanningAssistant(SharedContextManager sharedContextManager, IConfigEditor configEditor, IDMLogger logger)
        {
            _sharedContextManager = sharedContextManager;
            _configEditor = configEditor;
            _logger = logger;
        }

        /// <summary>
        /// Gets the loader extension classes discovered by this assistant from SharedContextManager
        /// </summary>
        public List<AssemblyClassDefinition> LoaderExtensionClasses => _sharedContextManager.DiscoveredLoaderExtensions;

        /// <summary>
        /// Scans an assembly and extracts class definitions to the provided list
        /// </summary>
        public bool ScanAssembly(Assembly assembly, List<AssemblyClassDefinition> targetList)
        {
            if (assembly == null) return false;

            Type[] types;

            try
            {
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types.Where(t => t != null).ToArray();
                }
                catch (Exception)
                {
                    types = assembly.GetExportedTypes();
                }

                if (types == null || types.Length == 0) return false;

                // Use parallel processing for large assemblies
                if (types.Length > 100)
                {
                    var typeInfoCollection = new ConcurrentBag<TypeInfo>();

                    Parallel.ForEach(types, type =>
                    {
                        try
                        {
                            TypeInfo typeInfo = type.GetTypeInfo();
                            typeInfoCollection.Add(typeInfo);
                        }
                        catch { }
                    });

                    foreach (var typeInfo in typeInfoCollection)
                    {
                        ProcessTypeInfo(typeInfo, assembly, targetList);
                    }
                }
                else
                {
                    foreach (var type in types)
                    {
                        try
                        {
                            TypeInfo typeInfo = type.GetTypeInfo();
                            ProcessTypeInfo(typeInfo, assembly, targetList);
                        }
                        catch { }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to scan assembly: {assembly.GetName().Name}", ex);
                return false;
            }
        }

        /// <summary>
        /// Processes a TypeInfo and adds appropriate class definitions
        /// </summary>
        private void ProcessTypeInfo(TypeInfo typeInfo, Assembly assembly, List<AssemblyClassDefinition> targetList)
        {
            try
            {
                var discoveredItems = new List<AssemblyClassDefinition>();
                
                // Check for ILoaderExtention interface
                if (typeInfo.ImplementedInterfaces.Contains(typeof(ILoaderExtention)))
                {
                    var classDef = GetAssemblyClassDefinition(typeInfo, "ILoaderExtention");
                    targetList?.Add(classDef);
                    discoveredItems.Add(classDef);
                }

                // Check for IDataSource interface
                if (typeInfo.ImplementedInterfaces.Contains(typeof(IDataSource)))
                {
                    var classDef = GetAssemblyClassDefinition(typeInfo, "IDataSource");
                    targetList?.Add(classDef);
                    _configEditor?.DataSourcesClasses?.Add(classDef);
                    discoveredItems.Add(classDef);
                }

                // Check for IWorkFlowAction interface
                if (typeInfo.ImplementedInterfaces.Contains(typeof(IWorkFlowAction)))
                {
                    var classDef = GetAssemblyClassDefinition(typeInfo, "IWorkFlowAction");
                    _configEditor?.WorkFlowActions?.Add(classDef);
                    discoveredItems.Add(classDef);
                }

                // Check for IDM_Addin interface
                if (typeInfo.ImplementedInterfaces.Contains(typeof(IDM_Addin)))
                {
                    var classDef = GetAssemblyClassDefinition(typeInfo, "IDM_Addin");
                    _configEditor?.Addins?.Add(classDef);
                    discoveredItems.Add(classDef);
                }

                // Check for IWorkFlowStep interface
                if (typeInfo.ImplementedInterfaces.Contains(typeof(IWorkFlowStep)))
                {
                    var classDef = GetAssemblyClassDefinition(typeInfo, "IWorkFlowStep");
                    _configEditor?.WorkFlowSteps?.Add(classDef);
                }

                // Check for IBeepViewModel interface
                if (typeInfo.ImplementedInterfaces.Contains(typeof(IBeepViewModel)))
                {
                    var classDef = GetAssemblyClassDefinition(typeInfo, "IBeepViewModel");
                    _configEditor?.ViewModels?.Add(classDef);
                    discoveredItems.Add(classDef);
                }

                // Check for IWorkFlowEditor interface
                if (typeInfo.ImplementedInterfaces.Contains(typeof(IWorkFlowEditor)))
                {
                    var classDef = GetAssemblyClassDefinition(typeInfo, "IWorkFlowEditor");
                    _configEditor?.WorkFlowStepEditors?.Add(classDef);
                }

                // Check for IWorkFlowRule interface
                if (typeInfo.ImplementedInterfaces.Contains(typeof(IWorkFlowRule)))
                {
                    var classDef = GetAssemblyClassDefinition(typeInfo, "IWorkFlowRule");
                    _configEditor?.Rules?.Add(classDef);
                }

                // Check for IFunctionExtension interface
                if (typeInfo.ImplementedInterfaces.Contains(typeof(IFunctionExtension)))
                {
                    var classDef = GetAssemblyClassDefinition(typeInfo, "IFunctionExtension");
                    _configEditor?.GlobalFunctions?.Add(classDef);
                }

                // Check for IPrintManager interface
                if (typeInfo.ImplementedInterfaces.Contains(typeof(IPrintManager)))
                {
                    var classDef = GetAssemblyClassDefinition(typeInfo, "IPrintManager");
                    _configEditor?.PrintManagers?.Add(classDef);
                }

                // Store discovered items in SharedContextManager based on their types
                if (discoveredItems.Count > 0)
                {
                    var loaderExtensions = discoveredItems.Where(d => d.componentType == "ILoaderExtention").ToList();
                    var dataSources = discoveredItems.Where(d => d.componentType == "IDataSource").ToList();
                    var addins = discoveredItems.Where(d => d.componentType == "IDM_Addin").ToList();
                    var workflowActions = discoveredItems.Where(d => d.componentType == "IWorkFlowAction").ToList();
                    var viewModels = discoveredItems.Where(d => d.componentType == "IBeepViewModel").ToList();

                    if (loaderExtensions.Count > 0)
                        _sharedContextManager.AddDiscoveredLoaderExtensions(loaderExtensions);
                    if (dataSources.Count > 0)
                        _sharedContextManager.AddDiscoveredDataSources(dataSources);
                    if (addins.Count > 0)
                        _sharedContextManager.AddDiscoveredAddins(addins);
                    if (workflowActions.Count > 0)
                        _sharedContextManager.AddDiscoveredWorkflowActions(workflowActions);
                    if (viewModels.Count > 0)
                        _sharedContextManager.AddDiscoveredViewModels(viewModels);
                }

                _logger?.LogWithContext($"Successfully processed type: {typeInfo.FullName}", null);
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error processing type {typeInfo?.FullName}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets the definition of a class within an assembly, including metadata and methods
        /// </summary>
        public AssemblyClassDefinition GetAssemblyClassDefinition(TypeInfo type, string typename)
        {
            var xcls = new AssemblyClassDefinition();
            xcls.Methods = new List<MethodsClass>();
            xcls.className = type.Name;
            xcls.dllname = type.Module.Name;
            xcls.PackageName = type.FullName;
            xcls.componentType = typename;
            xcls.type = type;

            if (type.ImplementedInterfaces.Contains(typeof(ILocalDB)))
            {
                xcls.LocalDB = true;
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
                    if (xcls.VisSchema != null && type.ImplementedInterfaces.Contains(typeof(IAddinVisSchema)))
                    {
                        var addinTree = new AddinTreeStructure
                        {
                            className = type.Name,
                            dllname = type.Module.Name,
                            PackageName = type.FullName,
                            Order = xcls.Order,
                            Imagename = xcls.VisSchema.IconImageName,
                            RootName = xcls.VisSchema.RootNodeName,
                            NodeName = xcls.VisSchema.BranchText,
                            ObjectType = type.Name
                        };
                        _configEditor?.AddinTreeStructure?.Add(addinTree);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWithContext($"Error processing VisSchema for {type.FullName}", ex);
                }

                // Process methods with CommandAttribute
                foreach (MethodInfo methods in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public)
                             .Where(m => m.GetCustomAttributes(typeof(CommandAttribute), true).Length > 0))
                {
                    CommandAttribute methodAttribute = methods.GetCustomAttribute<CommandAttribute>();
                    if (methodAttribute != null)
                    {
                        var methodClass = new MethodsClass
                        {
                            CommandAttr = methodAttribute,
                            Caption = methodAttribute.Caption,
                            Name = methodAttribute.Name,
                            Info = methods,
                            Hidden = methodAttribute.Hidden,
                            Click = methodAttribute.Click,
                            DoubleClick = methodAttribute.DoubleClick,
                            iconimage = methodAttribute.iconimage,
                            PointType = methodAttribute.PointType,
                            Category = methodAttribute.Category,
                            DatasourceType = methodAttribute.DatasourceType,
                            ClassType = methodAttribute.ClassType,
                            misc = methodAttribute.misc,
                            ObjectType = methodAttribute.ObjectType,
                            Showin = methodAttribute.Showin
                        };
                        xcls.Methods.Add(methodClass);
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
                    catch (Exception ex)
                    {
                        _logger?.LogWithContext($"Error creating IOrder instance for {type.FullName}", ex);
                    }
                }
            }

            return xcls;
        }

        /// <summary>
        /// Scans for data source specific implementations
        /// </summary>
        public bool ScanAssemblyForDataSources(Assembly assembly, List<AssemblyClassDefinition> dataSourceClasses)
        {
            Type[] types;

            try
            {
                try
                {
                    types = assembly.GetTypes();
                }
                catch (Exception ex)
                {
                    try
                    {
                        types = assembly.GetExportedTypes();
                    }
                    catch (Exception)
                    {
                        _logger?.LogWithContext($"Could not get types for {assembly.GetName()}", ex);
                        return false;
                    }
                }

                if (types != null)
                {
                    var discoveredDataSources = new List<AssemblyClassDefinition>();
                    
                    foreach (var mytype in types)
                    {
                        TypeInfo type = mytype.GetTypeInfo();

                        if (type.ImplementedInterfaces.Contains(typeof(IDataSource)))
                        {
                            AssemblyClassDefinition xcls = GetAssemblyClassDefinition(type, "IDataSource");
                            dataSourceClasses?.Add(xcls);
                            _configEditor?.DataSourcesClasses?.Add(xcls);
                            discoveredDataSources.Add(xcls);
                        }
                    }
                    
                    // Store discovered data sources in SharedContextManager
                    if (discoveredDataSources.Count > 0)
                    {
                        _sharedContextManager.AddDiscoveredDataSources(discoveredDataSources);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to scan assembly for data sources: {assembly.GetName()}", ex);
                return false;
            }
        }

        /// <summary>
        /// Gets scanning statistics from SharedContextManager
        /// </summary>
        public Dictionary<string, object> GetScanningStatistics()
        {
            return new Dictionary<string, object>
            {
                ["DataSources"] = _sharedContextManager.DiscoveredDataSources.Count,
                ["LoaderExtensions"] = _sharedContextManager.DiscoveredLoaderExtensions.Count,
                ["WorkFlowActions"] = _sharedContextManager.DiscoveredWorkflowActions.Count,
                ["Addins"] = _sharedContextManager.DiscoveredAddins.Count,
                ["ViewModels"] = _sharedContextManager.DiscoveredViewModels.Count,
                // These are maintained in ConfigEditor for backward compatibility
                ["WorkFlowSteps"] = _configEditor?.WorkFlowSteps?.Count ?? 0,
                ["WorkFlowEditors"] = _configEditor?.WorkFlowStepEditors?.Count ?? 0,
                ["Rules"] = _configEditor?.Rules?.Count ?? 0,
                ["GlobalFunctions"] = _configEditor?.GlobalFunctions?.Count ?? 0,
                ["PrintManagers"] = _configEditor?.PrintManagers?.Count ?? 0
            };
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // No need to clear local collections - they're managed by SharedContextManager
                _disposed = true;
            }
        }
    }
}