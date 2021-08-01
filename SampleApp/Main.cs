using Autofac;
using System;
using System.Linq;
using TheTechIdea.Beep;
using TheTechIdea.Logger;

using TheTechIdea.Beep.Workflow;
using TheTechIdea.Util;
using TheTechIdea.Tools;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Tools;
using BeepEnterprize.Vis.Module;
using BeepEnterprize.Winform.Vis;

namespace DataManagment_Engine

{   
    public class MainApp
    {
        private static IContainer Container { get; set; }
        private static ContainerBuilder Builder { get; set; }
        #region "System Components"
        public IDMEEditor DMEEditor { get; set; }
        public IConfigEditor Config_editor { get; set; }
        public IWorkFlowEditor WorkFlowEditor { get; set; }
        public IDMLogger lg { get; set; }
        public IUtil util { get; set; }
        public  IVisManager vis { get; set; }
        public IErrorsInfo Erinfo { get; set; }
        public IJsonLoader jsonLoader { get; set; }
        public IAssemblyHandler LLoader { get; set; }
    //    public IControlEditor Controleditor { get; set; }
     //   public ITree tree { get; set; }
        public IClassCreator classCreator { get; set; }
        public IDataTypesHelper typesHelper { get; set; }
        public IETL eTL { get; set; }
        #endregion
        public static IContainer Configure() //ContainerBuilder builder
        {

            Builder = new ContainerBuilder();
            Builder.RegisterType<ErrorsInfo>().As<IErrorsInfo>().SingleInstance();
            Builder.RegisterType<DMLogger>().As<IDMLogger>().SingleInstance();
            Builder.RegisterType<ConfigEditor>().As<IConfigEditor>().SingleInstance();
            Builder.RegisterType<DataTypesHelper>().As<IDataTypesHelper>().SingleInstance();
            Builder.RegisterType<DMEEditor>().As<IDMEEditor>().SingleInstance();
            Builder.RegisterType<WorkFlowEditor>().As<IWorkFlowEditor>().SingleInstance();
            Builder.RegisterType<Util>().As<IUtil>().SingleInstance();
            Builder.RegisterType<VisManager>().As<IVisManager>().SingleInstance();
            Builder.RegisterType<JsonLoader>().As<IJsonLoader>().SingleInstance();
            Builder.RegisterType<AssemblyHandler>().As<IAssemblyHandler>().SingleInstance();
            Builder.RegisterType<ClassCreatorv2>().As<IClassCreator>().SingleInstance();
            Builder.RegisterType<ETL>().As<IETL>().SingleInstance();
            return Builder.Build();
        }
        public MainApp()
        {
        
            Container = Configure();
            using (var scope = Container.BeginLifetimeScope())
            {
            
                Config_editor = scope.Resolve<IConfigEditor>();
              
               
                LLoader = scope.Resolve<IAssemblyHandler>();

                DMEEditor = scope.Resolve<IDMEEditor>();
                vis = scope.Resolve<IVisManager>();
                //-------------------------------------------------------------------------------
                // The Main Visualization Class tha control the visual aspect of the system
                ////---------------------------------------------------------------------------
                //vis = scope.Resolve<IVisUtil>();
                //tree = scope.Resolve<ITree>();
                //vis.treeEditor = tree;
                //ITreeView treeView = (ITreeView)tree;
                //treeView.Visutil = vis;
                //tree.DMEEditor = DMEEditor;
                //-------------------------------------------------------------------------------
                // this Editor will help Generate user controls for visulization
                //Controleditor = scope.Resolve<IControlEditor>();
                //-------------------------------------------------------------------------------
                // a tree class will be main visualization control for the system
              
               // vis.controlEditor = Controleditor;
              
                //---------------------------------------------------------------------------
                // This has to be last step so that all Configuration is ready for Addin's 
                // to be able to use
                //---------------------------------------------------------------------------
                AppDomainSetup pluginsDomainSetup = new AppDomainSetup
                {
                    ApplicationBase = AppDomain.CurrentDomain.BaseDirectory,
                    PrivateBinPath = @"ConnectionDrivers;ProjectClasses"
                };
                LLoader.LoadAllAssembly();
                Config_editor.LoadedAssemblies = LLoader.Assemblies.Select(c => c.DllLib).ToList();
                // Setup the Entry Screen 
                // the screen has to be in one the Addin DLL's loaded by the Assembly loader
              
                Config_editor.Config.SystemEntryFormName = @"Frm_main";
               
                //vis.ShowMainDisplayForm();
              
            }
        }
    }
}
