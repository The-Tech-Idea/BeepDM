using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Vis;
using TypeInfo = System.Reflection.TypeInfo;

namespace TheTechIdea.Beep.Tools.PluginSystem
{
    /// <summary>
    /// Abstraction for scanning assemblies to discover Beep components (data sources, addins, workflow, etc.).
    /// </summary>
    public interface IScanningService
    {
    /// <summary>
    /// Scans the provided assembly for all supported Beep component types and updates the SharedContextManager and optional target list.
    /// </summary>
    /// <param name="assembly">Assembly to scan.</param>
    /// <param name="targetList">Optional list that will receive discovered <see cref="AssemblyClassDefinition"/> entries (in addition to central caches).</param>
    void ScanAssembly(Assembly assembly, List<AssemblyClassDefinition> targetList = null);

    /// <summary>
    /// Specifically scans the assembly for IDataSource implementations and records them in both configuration and shared context stores.
    /// </summary>
    /// <param name="assembly">Assembly to scan.</param>
    /// <param name="dataSourceClasses">Optional list that will receive the discovered data source definitions.</param>
    void ScanAssemblyForDataSources(Assembly assembly, List<AssemblyClassDefinition> dataSourceClasses);

    /// <summary>
    /// Builds a rich <see cref="AssemblyClassDefinition"/> descriptor for a discovered component type including attributes, methods and ordering metadata.
    /// </summary>
    /// <param name="type">Type info for the component.</param>
    /// <param name="componentType">Logical component type label (e.g. IDataSource, IDM_Addin).</param>
    /// <returns>Populated class definition.</returns>
    AssemblyClassDefinition GetAssemblyClassDefinition(TypeInfo type, string componentType);

    /// <summary>
    /// Returns aggregated statistics about discovered component categories during the current scanning session.
    /// </summary>
    /// <returns>Dictionary keyed by category name with counts.</returns>
    Dictionary<string, object> GetScanningStatistics();
    }

    internal class ScanningService : IScanningService
    {
        private readonly SharedContextManager _sharedContextManager;
        private readonly IConfigEditor _configEditor;
        private readonly IDMLogger _logger;

        public ScanningService(SharedContextManager sharedContextManager, IConfigEditor configEditor, IDMLogger logger)
        {
            _sharedContextManager = sharedContextManager;
            _configEditor = configEditor;
            _logger = logger;
        }

        public void ScanAssembly(Assembly assembly, List<AssemblyClassDefinition> targetList = null)
        {
            if (assembly == null) return;
            Type[] types;
            try
            {
                try { types = assembly.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { types = ex.Types != null ? ex.Types.Where(t => t != null).ToArray() : Array.Empty<Type>(); }
                catch { types = assembly.GetExportedTypes(); }
                if (types == null || types.Length == 0) return;
                foreach (var t in types)
                {
                    try { ProcessTypeInfo(t.GetTypeInfo(), targetList); } catch { }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to scan assembly: {assembly.GetName().Name}", ex);
            }
        }

        public void ScanAssemblyForDataSources(Assembly assembly, List<AssemblyClassDefinition> dataSourceClasses)
        {
            if (assembly == null) return;
            Type[] types;
            try
            {
                try { types = assembly.GetTypes(); }
                catch { types = assembly.GetExportedTypes(); }
                if (types == null) return;
                var discovered = new List<AssemblyClassDefinition>();
                foreach (var t in types)
                {
                    var ti = t.GetTypeInfo();
                    if (ti.ImplementedInterfaces.Contains(typeof(IDataSource)))
                    {
                        var def = GetAssemblyClassDefinition(ti, "IDataSource");
                        dataSourceClasses?.Add(def);
                        _configEditor?.DataSourcesClasses?.Add(def);
                        discovered.Add(def);
                    }
                }
                if (discovered.Count > 0)
                {
                    _sharedContextManager.AddDiscoveredDataSources(discovered);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to scan assembly for data sources: {assembly.GetName()}", ex);
            }
        }

        private void ProcessTypeInfo(TypeInfo typeInfo, List<AssemblyClassDefinition> targetList)
        {
            try
            {
                var discoveredItems = new List<AssemblyClassDefinition>();
                void AddIf(bool cond, string compType, Action<AssemblyClassDefinition> extra = null)
                {
                    if (!cond) return; var cd = GetAssemblyClassDefinition(typeInfo, compType); targetList?.Add(cd); extra?.Invoke(cd); discoveredItems.Add(cd);
                }
                AddIf(typeInfo.ImplementedInterfaces.Contains(typeof(ILoaderExtention)), "ILoaderExtention");
                AddIf(typeInfo.ImplementedInterfaces.Contains(typeof(IDataSource)), "IDataSource", c => _configEditor?.DataSourcesClasses?.Add(c));
                AddIf(typeInfo.ImplementedInterfaces.Contains(typeof(IWorkFlowAction)), "IWorkFlowAction", c => _configEditor?.WorkFlowActions?.Add(c));
                AddIf(typeInfo.ImplementedInterfaces.Contains(typeof(IDM_Addin)), "IDM_Addin", c => _configEditor?.Addins?.Add(c));
                AddIf(typeInfo.ImplementedInterfaces.Contains(typeof(IWorkFlowStep)), "IWorkFlowStep", c => _configEditor?.WorkFlowSteps?.Add(c));
                AddIf(typeInfo.ImplementedInterfaces.Contains(typeof(IBeepViewModel)), "IBeepViewModel", c => _configEditor?.ViewModels?.Add(c));
                AddIf(typeInfo.ImplementedInterfaces.Contains(typeof(IWorkFlowEditor)), "IWorkFlowEditor", c => _configEditor?.WorkFlowStepEditors?.Add(c));
                AddIf(typeInfo.ImplementedInterfaces.Contains(typeof(IWorkFlowRule)), "IWorkFlowRule", c => _configEditor?.Rules?.Add(c));
                AddIf(typeInfo.ImplementedInterfaces.Contains(typeof(IFunctionExtension)), "IFunctionExtension", c => _configEditor?.GlobalFunctions?.Add(c));
                AddIf(typeInfo.ImplementedInterfaces.Contains(typeof(IPrintManager)), "IPrintManager", c => _configEditor?.PrintManagers?.Add(c));
                if (discoveredItems.Count > 0)
                {
                    _sharedContextManager.AddDiscoveredLoaderExtensions(discoveredItems.Where(d => d.componentType == "ILoaderExtention"));
                    _sharedContextManager.AddDiscoveredDataSources(discoveredItems.Where(d => d.componentType == "IDataSource"));
                    _sharedContextManager.AddDiscoveredAddins(discoveredItems.Where(d => d.componentType == "IDM_Addin"));
                    _sharedContextManager.AddDiscoveredWorkflowActions(discoveredItems.Where(d => d.componentType == "IWorkFlowAction"));
                    _sharedContextManager.AddDiscoveredViewModels(discoveredItems.Where(d => d.componentType == "IBeepViewModel"));
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error processing type {typeInfo?.FullName}", ex);
            }
        }

        public AssemblyClassDefinition GetAssemblyClassDefinition(TypeInfo type, string componentType)
        {
            var xcls = new AssemblyClassDefinition
            {
                Methods = new List<MethodsClass>(),
                className = type.Name,
                dllname = type.Module.Name,
                PackageName = type.FullName,
                componentType = componentType,
                type = type
            };
            if (type.ImplementedInterfaces.Contains(typeof(ILocalDB))) xcls.LocalDB = true;
            if (type.ImplementedInterfaces.Contains(typeof(IDataSource))) xcls.IsDataSource = true;
            if (type.ImplementedInterfaces.Contains(typeof(IInMemoryDB))) xcls.InMemory = true;
            xcls.classProperties = (AddinAttribute)type.GetCustomAttribute(typeof(AddinAttribute), false);
            if (xcls.classProperties != null)
            {
                xcls.Order = xcls.classProperties.order;
                xcls.RootName = xcls.classProperties.misc;
                if (type.ImplementedInterfaces.Contains(typeof(IDataSource)))
                {
                    xcls.IsDataSource = true;
                    xcls.DatasourceType = xcls.classProperties.DatasourceType;
                    xcls.Category = xcls.classProperties.Category;
                }
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
                catch (Exception ex) { _logger?.LogWithContext($"Error processing VisSchema for {type.FullName}", ex); }
                foreach (MethodInfo methods in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public).Where(m => m.GetCustomAttributes(typeof(CommandAttribute), true).Length > 0))
                {
                    var methodAttribute = methods.GetCustomAttribute<CommandAttribute>();
                    if (methodAttribute != null)
                    {
                        xcls.Methods.Add(new MethodsClass
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
                        });
                    }
                }
                if (type.ImplementedInterfaces.Contains(typeof(IOrder)))
                {
                    try { IOrder cls = (IOrder)Activator.CreateInstance(type); xcls.Order = cls.Order; } catch (Exception ex) { _logger?.LogWithContext($"Error creating IOrder instance for {type.FullName}", ex); }
                }
            }
            return xcls;
        }

        public Dictionary<string, object> GetScanningStatistics() => new()
        {
            ["DataSources"] = _sharedContextManager.DiscoveredDataSources.Count,
            ["LoaderExtensions"] = _sharedContextManager.DiscoveredLoaderExtensions.Count,
            ["WorkFlowActions"] = _sharedContextManager.DiscoveredWorkflowActions.Count,
            ["Addins"] = _sharedContextManager.DiscoveredAddins.Count,
            ["ViewModels"] = _sharedContextManager.DiscoveredViewModels.Count,
            ["WorkFlowSteps"] = _configEditor?.WorkFlowSteps?.Count ?? 0,
            ["WorkFlowEditors"] = _configEditor?.WorkFlowStepEditors?.Count ?? 0,
            ["Rules"] = _configEditor?.Rules?.Count ?? 0,
            ["GlobalFunctions"] = _configEditor?.GlobalFunctions?.Count ?? 0,
            ["PrintManagers"] = _configEditor?.PrintManagers?.Count ?? 0
        };
    }
}
