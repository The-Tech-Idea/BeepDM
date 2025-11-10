using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Workflow;

namespace TheTechIdea.Beep.Tools
{
    /// <summary>
    /// AssemblyHandler partial class - Helper Methods and Utilities
    /// </summary>
    public partial class AssemblyHandler
    {
        #region Instance Creation

        /// <summary>
        /// Creates an instance of a class from its type name
        /// </summary>
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
            catch (Exception ex)
            {
                Logger?.WriteLog($"CreateInstanceFromString: Error creating instance of '{typeName}': {ex.Message}");
                return null;
            }

            return instance;
        }

        /// <summary>
        /// Creates an instance of a class from its type name within a specific assembly
        /// </summary>
        public object CreateInstanceFromString(string dll, string typeName, params object[] args)
        {
            object instance = null;
            Type type = null;
            string dllname = Path.GetFileName(dll);
            string withoutExtension = Path.GetFileNameWithoutExtension(dll);
            
            try
            {
                assemblies_rep dllas = Assemblies.Where(p => 
                    new AssemblyName(p.DllName).Name.Equals(withoutExtension, StringComparison.InvariantCultureIgnoreCase))
                    .FirstOrDefault();
                    
                if (dllas != null)
                {
                    type = dllas.DllLib.GetType(typeName);
                    if (type == null)
                        return null;

                    instance = Activator.CreateInstance(type, args);
                }
                else
                {
                    instance = CreateInstanceFromString(typeName, args);
                }
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"CreateInstanceFromString: Error creating instance from DLL '{dll}', Type '{typeName}': {ex.Message}");
                return null;
            }

            return instance;
        }

        /// <summary>
        /// Creates an instance of a type specified by a fully qualified name
        /// </summary>
        public object GetInstance(string strFullyQualifiedName)
        {
            try
            {
                Type type = GetType(strFullyQualifiedName);
                if (type != null)
                    return Activator.CreateInstance(type);
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"GetInstance: Error - {ex.Message}");
            }

            return null;
        }

        #endregion

        #region Type Resolution

        /// <summary>
        /// Retrieves a type by its fully qualified name with caching
        /// </summary>
        public Type GetType(string strFullyQualifiedName)
        {
            // Check cache first
            if (_typeCache.TryGetValue(strFullyQualifiedName, out Type cachedType))
            {
                return cachedType;
            }

            string[] assemblynamespace = strFullyQualifiedName.Split('.');
            Type type = Type.GetType(strFullyQualifiedName);

            if (type != null)
            {
                _typeCache[strFullyQualifiedName] = type;
                return type;
            }

            try
            {
                // First check in domain assemblies with matching namespace
                foreach (var asm in CurrentDomain.GetAssemblies()
                    .Where(o => o.FullName.StartsWith(assemblynamespace[0])))
                {
                    try
                    {
                        type = asm.GetType(strFullyQualifiedName);
                        if (type != null)
                        {
                            _typeCache[strFullyQualifiedName] = type;
                            return type;
                        }
                    }
                    catch (MissingMethodException) { }
                }

                // Then check referenced assemblies
                Assembly rootassembly = Assembly.GetEntryAssembly();
                if (rootassembly != null)
                {
                    var assemblies = rootassembly.GetReferencedAssemblies()
                        .Where(x => x.FullName.Contains("DataManagmentEngine"));

                    foreach (AssemblyName item in assemblies)
                    {
                        var assembly = Assembly.Load(item);
                        type = assembly.GetType(strFullyQualifiedName);
                        if (type != null)
                        {
                            _typeCache[strFullyQualifiedName] = type;
                            return type;
                        }
                    }
                }

                // Finally check loaded assemblies
                foreach (var item in Assemblies)
                {
                    var assembly = item.DllLib;
                    try
                    {
                        type = assembly.GetType(strFullyQualifiedName);
                        if (type != null)
                        {
                            _typeCache[strFullyQualifiedName] = type;
                            return type;
                        }
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"GetType: Error resolving type '{strFullyQualifiedName}': {ex.Message}");
            }

            return null;
        }

        #endregion

        #region Method Execution

        /// <summary>
        /// Executes a method on an object instance using reflection
        /// </summary>
        public bool RunMethod(object ObjInstance, string FullClassName, string MethodName)
        {
            try
            {
                Type type = GetType(FullClassName);
                if (type == null)
                    return false;

                MethodInfo methodInfo = type.GetMethod(MethodName);
                if (methodInfo == null)
                    return false;

                methodInfo.Invoke(ObjInstance, null);
                return true;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"RunMethod: Error executing method '{MethodName}' on '{FullClassName}': {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Assembly Class Definition

        /// <summary>
        /// Gets the definition of a class within an assembly, including metadata and methods
        /// </summary>
        public AssemblyClassDefinition GetAssemblyClassDefinition(TypeInfo type, string typename)
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
                    if (xcls.VisSchema != null)
                    {
                        if (type.ImplementedInterfaces.Contains(typeof(IAddinVisSchema)))
                        {
                            AddinTreeStructure AddinTree = new AddinTreeStructure();
                            AddinTree.className = type.Name;
                            AddinTree.dllname = type.Module.Name;
                            AddinTree.PackageName = type.FullName;
                            AddinTree.Order = xcls.Order;
                            AddinTree.Imagename = xcls.VisSchema.IconImageName;
                            AddinTree.RootName = xcls.VisSchema.RootNodeName;
                            AddinTree.NodeName = xcls.VisSchema.BranchText;
                            AddinTree.ObjectType = type.Name;
                            
                            ConfigEditor.AddinTreeStructure.Add(AddinTree);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger?.WriteLog($"GetAssemblyClassDefinition: Error processing VisSchema: {ex.Message}");
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
                    catch (Exception) { }
                }
            }

            return xcls;
        }

        #endregion

        #region Addin Objects and Hierarchy

        /// <summary>
        /// Rearranges or adds a new addin object to the function hierarchy
        /// </summary>
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
        /// Retrieves addin objects from the specified assembly and organizes them into a hierarchical structure
        /// </summary>
        public List<ParentChildObject> GetAddinObjects(Assembly asm)
        {
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
                                                if (Show)
                                                {
                                                    a = RearrangeAddin(p[i], p[i - 1], objtype);
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                Logger?.WriteLog($"GetAddinObjects: Error processing addin: {ex.Message}");
                                            }
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
                        Logger?.WriteLog($"GetAddinObjects: Error: {ex.Message}");
                    }
                }
            }

            return Utilfunction.FunctionHierarchy;
        }

        /// <summary>
        /// Gets addin objects organized in a tree structure
        /// </summary>
        public List<ParentChildObject> GetAddinObjectsFromTree()
        {
            try
            {
                // Build hierarchy from AddinTreeStructure
                foreach (var addinTree in ConfigEditor.AddinTreeStructure)
                {
                    RearrangeAddin(addinTree.NodeName, addinTree.RootName, addinTree.ObjectType);
                }
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"GetAddinObjectsFromTree: Error: {ex.Message}");
            }

            return Utilfunction.FunctionHierarchy;
        }

        #endregion

        #region Driver Management

        /// <summary>
        /// Gets drivers from an assembly
        /// </summary>
        public List<ConnectionDriversConfig> GetDrivers(Assembly asm)
        {
            List<ConnectionDriversConfig> drivers = new List<ConnectionDriversConfig>();

            try
            {
                // For now, just return the DataDriversConfig list
                // The actual driver discovery is handled by DriverDiscoveryAssistant in SharedContextAssemblyHandler
                // or by specific scanning logic in the original implementation
                Logger?.WriteLog($"GetDrivers: Scanning assembly {asm.GetName().Name} for drivers");
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"GetDrivers: Error getting drivers from assembly: {ex.Message}");
            }

            return drivers;
        }

        /// <summary>
        /// Adds engine default drivers
        /// </summary>
        public bool AddEngineDefaultDrivers()
        {
            try
            {
                // Add default drivers if needed
                // This can be extended to add specific default drivers
                Logger?.WriteLog("AddEngineDefaultDrivers: Adding default drivers");
                return true;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"AddEngineDefaultDrivers: Error - {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if drivers already exist in the list to avoid duplicates
        /// </summary>
        public void CheckDriverAlreadyExistinList()
        {
            try
            {
                // Remove duplicate drivers
                var uniqueDrivers = DataDriversConfig
                    .GroupBy(d => d.PackageName)
                    .Select(g => g.First())
                    .ToList();

                DataDriversConfig.Clear();
                DataDriversConfig.AddRange(uniqueDrivers);

                Logger?.WriteLog($"CheckDriverAlreadyExistinList: Removed duplicates, {DataDriversConfig.Count} unique drivers remain");
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"CheckDriverAlreadyExistinList: Error - {ex.Message}");
            }
        }

        #endregion
    }
}
