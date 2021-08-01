using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea;
using TheTechIdea.Beep;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Tools;
using TheTechIdea.Util;

namespace AssemblyLoaderExtension
{
    public class AssemblyLoaderExtension : ILoaderExtention
    {
        public AppDomain CurrentDomain { get; set; }
        public List<IDM_Addin> AddIns { get; set; }
        public List<assemblies_rep> Assemblies { get; set; }
        public List<AssemblyClassDefinition> DataSourcesClasses { get; set; }
        public IAssemblyHandler Loader { get; set; }
        public AssemblyLoaderExtension(IAssemblyHandler ploader)
        {
            Loader = ploader;
            CurrentDomain = AppDomain.CurrentDomain;
            DataSourcesClasses = new List<AssemblyClassDefinition>();
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
