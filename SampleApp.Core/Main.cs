using Autofac;
using System;
using System.Linq;
using TheTechIdea.Beep;
using TheTechIdea.Logger;
using TheTechIdea.Winforms.VIS;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Util;
using TheTechIdea.Tools;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Tools.AssemblyHandling;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Tools;

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
        public IVisUtil vis { get; set; }
        public IErrorsInfo Erinfo { get; set; }
        public IJsonLoader jsonLoader { get; set; }
        public IAssemblyHandler LLoader { get; set; }
        public IControlEditor Controleditor { get; set; }
        public ITree tree { get; set; }
        public IClassCreator classCreator { get; set; }
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
            Builder.RegisterType<ControlEditor>().As<IControlEditor>().SingleInstance();
            Builder.RegisterType<VisUtil>().As<IVisUtil>().SingleInstance();
            Builder.RegisterType<JsonLoader>().As<IJsonLoader>().SingleInstance();
            Builder.RegisterType<AssemblyHandlerCore>().As<IAssemblyHandler>().SingleInstance();
            Builder.RegisterType<ClassCreatorv2>().As<IClassCreator>().SingleInstance();
            Builder.RegisterType<ETL>().As<IETL>().SingleInstance();
            Builder.RegisterType<TreeCore>().As<ITree>().SingleInstance();

            return Builder.Build();
        }
        public MainApp()
        {

            Container = Configure();
            using (var scope = Container.BeginLifetimeScope())
            {
                //  jsonLoader= scope.Resolve<IJsonLoader>();
                //--------------------------------------------------------------------------------
                // a Error Class that will have all error message tracking 
                //---------------------------------------------------------------------------
                //Erinfo = scope.Resolve<IErrorsInfo>();
                //--------------------------------------------------------------------------------
                // a Log Manager 
                //---------------------------------------------------------------------------
                //lg = scope.Resolve<IDMLogger>();
                //lg.WriteLog("App started");
                //-------------------------------------------------------------------------------
                // a onfiguration class for assembly, addin's and  drivers loading into the 
                // application
                //---------------------------------------------------------------------------
                Config_editor = scope.Resolve<IConfigEditor>();
                // a Utility Class for helping in Doing Different functions for  Data Managment

                //util = scope.Resolve<IUtil>();
                //--------------------------------------------------------------------------------

                //-------------------------------------------------------------------------------
                // this is the assembly loader for loading from Addin Folder and Projectdrivers Folder
                //---------------------------------------------------------------------------

                LLoader = scope.Resolve<IAssemblyHandler>();

                // Setup the Database Connection Screen
                // a "Work Flow" class will control all the workflow between different data source 
                // and automation
                // WorkFlowEditor = scope.Resolve<IWorkFlowEditor>();
                //eTL= scope.Resolve<IETL>();
                //-------------------------------------------------------------------------------
                // The Main Class for Data Manager 
                //---------------------------------------------------------------------------

                //DMEEditor.assemblyHandler = LLoader;
                DMEEditor = scope.Resolve<IDMEEditor>();
                //-------------------------------------------------------------------------------
                // The Main Visualization Class tha control the visual aspect of the system
                //---------------------------------------------------------------------------
                vis = scope.Resolve<IVisUtil>();
                tree = scope.Resolve<ITree>();
                vis.treeEditor = tree;
                ITreeView treeView = (ITreeView)tree;
                treeView.Visutil = vis;
                tree.DMEEditor = DMEEditor;
                //-------------------------------------------------------------------------------
                // this Editor will help Generate user controls for visulization
                Controleditor = scope.Resolve<IControlEditor>();
                //-------------------------------------------------------------------------------
                // a tree class will be main visualization control for the system

                vis.controlEditor = Controleditor;

                //---------------------------------------------------------------------------
                // This has to be last step so that all Configuration is ready for Addin's 
                // to be able to use
                //---------------------------------------------------------------------------
                
                LLoader.LoadAllAssembly();
                Config_editor.LoadedAssemblies = LLoader.Assemblies.Select(c => c.DllLib).ToList();
                // Setup the Entry Screen 
                // the screen has to be in one the Addin DLL's loaded by the Assembly loader

                Config_editor.Config.SystemEntryFormName = @"Frm_MainDisplayForm";

                vis.ShowMainDisplayForm();

            }
        }
    }
}
