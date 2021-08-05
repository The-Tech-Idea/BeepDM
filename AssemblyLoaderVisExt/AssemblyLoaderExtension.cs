using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Vis;
using TheTechIdea.DataManagment_Engine.Vis;
using TheTechIdea.Tools;
using TheTechIdea.Util;

namespace AssemblyLoaderExtension
{
    public class AssemblyLoaderExtension : ILoaderExtention
    {
        ParentChildObject a;
        private string Name { get; set; }
        private string Descr { get; set; }
        public AppDomain CurrentDomain { get; set; }
        //public List<IDM_Addin> AddIns { get; set; }
        //public List<assemblies_rep> Assemblies { get; set; }
        //public List<AssemblyClassDefinition> DataSourcesClasses { get; set; }
        public IAssemblyHandler Loader { get; set; }
        public AssemblyLoaderExtension(IAssemblyHandler ploader)
        {
            Loader = ploader;
            CurrentDomain = AppDomain.CurrentDomain;
            //DataSourcesClasses = new List<AssemblyClassDefinition>();
            CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        public IErrorsInfo LoadAllAssembly()
        {
            ErrorsInfo er = new ErrorsInfo();
            try
            {
               
                foreach (var item in Loader.Assemblies)
                {
                    ScanAssembly(item.DllLib);
                    GetAddinObjects(item.DllLib);
                }
                er.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {
                er.Ex = ex;
                er.Flag = Errors.Failed;
                er.Message = ex.Message;
                
            }
            return er;
        }
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
                       
                     
                        //-------------------------------------------------------
                   
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
                            Loader.ConfigEditor.BranchesClasses.Add(xcls);
                        }
                        // --- Get all AI app Interfaces
                        //-----------------------------------------------------
                       

                    }
                }

            }
            catch (Exception ex)
            {
                //DMEEditor.AddLogMessage("Failed", $"Could not get Any types for {asm.GetName().ToString()}" , DateTime.Now, -1, asm.GetName().ToString(), Errors.Failed);
            };

            return true;


        }
        #endregion "Class Extractors"
        #region "Class ordering"
        public ParentChildObject RearrangeAddin(string p, string parentid, string Objt)
        {

            ParentChildObject a;
            if (parentid == null)
            {
                if (Loader.Utilfunction.FunctionHierarchy.Where(f => f.id == p && f.ObjType == Objt).Count() == 0)
                {
                    a = new ParentChildObject() { id = p, ParentID = null, ObjType = Objt, AddinName = Name, Description = Descr };
                    Loader.Utilfunction.FunctionHierarchy.Add(a);

                }
                else
                {
                    a = Loader.Utilfunction.FunctionHierarchy.Where(f => f.id == p && f.ParentID == null && f.ObjType == Objt).FirstOrDefault();

                }

            }
            else
            {
                if (Loader.Utilfunction.FunctionHierarchy.Where(f => f.id == p && f.ParentID == parentid && f.ObjType == Objt).Count() == 0)
                {
                    a = new ParentChildObject() { id = p, ParentID = parentid, ObjType = Objt, AddinName = Name, Description = Descr };
                    Loader.Utilfunction.FunctionHierarchy.Add(a);

                }
                else
                {
                    a = Loader.Utilfunction.FunctionHierarchy.Where(f => f.id == p && f.ParentID == parentid && f.ObjType == Objt).FirstOrDefault();

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
                                                AddinAttribute attrib = (AddinAttribute)type.GetCustomAttribute(typeof(AddinAttribute), false);
                                                
                                                    IDM_Addin uc = (IDM_Addin)Activator.CreateInstance(type);
                                                    if (uc != null)
                                                    {
                                                        addin = (IDM_Addin)uc;
                                                        addin.DllPath = Path.GetDirectoryName(asm.Location);
                                                        addin.ObjectName = type.Name;
                                                        addin.DllName = Path.GetFileName(asm.Location);
                                                        Show = addin.DefaultCreate;
                                                       
                                                    }
                                                    Name = addin.AddinName;
                                                    Descr = addin.Description;
                                                
                                                Loader.AddIns.Add(addin);

                                                if (addin.DefaultCreate)
                                                {
                                                    if (Loader.ConfigEditor.AddinTreeStructure.Where(x => x.className == type.Name).Any() == false)
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
                                                            Loader.ConfigEditor.AddinTreeStructure.Add(xcls);

                                                        }
                                                        catch (Exception)
                                                        {

                                                        }
                                                    }

                                                }
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

            Loader.ConfigEditor.SaveAddinTreeStructure();
            return Loader.Utilfunction.FunctionHierarchy;
        }
        #endregion
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
                assemblies_rep s = Loader.Assemblies.FirstOrDefault(a => a.DllLib.FullName.StartsWith(filenamewo));
                if (s != null)
                {
                    assembly = s.DllLib;
                }

            }
            if (assembly != null)
                return assembly;
            foreach (var moduleDir in Loader.ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.OtherDLL))
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
            foreach (var moduleDir in Loader.ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.ConnectionDriver))
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
            foreach (var moduleDir in Loader.ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.ProjectClass))
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

        public IErrorsInfo Scan()
        {
            throw new NotImplementedException();
        }
    }
}
