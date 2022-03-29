using Autofac;
using System.Linq;
using TheTechIdea.Logger;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Util;
using TheTechIdea.Tools;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Tools;
using TheTechIdea.DataManagment_Engine.Workflow;
using System;
using DemoAppNet6;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep

{   
    public class MainApp
    {
        private static IContainer Container { get; set; }
        private static ContainerBuilder Builder { get; set; }
        #region "System Components"
        public IDMEEditor DMEEditor { get; set; }
        public IConfigEditor Config_editor { get; set; }

        public IDMLogger lg { get; set; }
        public IUtil util { get; set; }
       
        public IErrorsInfo Erinfo { get; set; }
        public IJsonLoader jsonLoader { get; set; }
        public IAssemblyHandler LLoader { get; set; }
        public IClassCreator classCreator { get; set; }
        public IDataTypesHelper typesHelper { get; set; }
        public IETL eTL { get; set; }
        //----------------------------------------------
        // ---  Needed if WorkFlow Components created
        //public IWorkFlowEditor WorkFlowEditor { get; set; }
        //public IWorkFlowStepEditor WorkFlowStepEditor { get; set; }
        //public IRuleParser ruleparser { get; set; }
        //public IRulesEditor rulesEditor { get; set; }
        //------------------------------------------------

        #endregion
        public static IContainer Configure() //ContainerBuilder builder
        {
            Builder = new ContainerBuilder();
            Builder.RegisterType<ErrorsInfo>().As<IErrorsInfo>().SingleInstance();
            Builder.RegisterType<DMLogger>().As<IDMLogger>().SingleInstance();
            Builder.RegisterType<ConfigEditor>().As<IConfigEditor>().SingleInstance();
            Builder.RegisterType<DataTypesHelper>().As<IDataTypesHelper>().SingleInstance();
            Builder.RegisterType<DMEEditor>().As<IDMEEditor>().SingleInstance();
            Builder.RegisterType<Util>().As<IUtil>().SingleInstance();
            Builder.RegisterType<JsonLoader>().As<IJsonLoader>().SingleInstance();
            Builder.RegisterType<AssemblyHandler>().As<IAssemblyHandler>().SingleInstance();
            Builder.RegisterType<ClassCreator>().As<IClassCreator>().SingleInstance();
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
                eTL = scope.Resolve<IETL>();
                classCreator= scope.Resolve<IClassCreator>();
                typesHelper= scope.Resolve<IDataTypesHelper>(); 
                DMEEditor = scope.Resolve<IDMEEditor>();
                DMEEditor.classCreator = classCreator;
                DMEEditor.ETL = eTL;
                DMEEditor.ETL.DMEEditor = DMEEditor;
                DMEEditor.assemblyHandler = LLoader;
                DMEEditor.typesHelper = typesHelper;
                DMEEditor.typesHelper.DMEEditor = DMEEditor;


                //Enterprize Components Engine Setup
                //DMEEditor.WorkFlowEditor = WorkFlowEditor;
                //DMEEditor.WorkFlowEditor.DMEEditor = DMEEditor;
                //DMEEditor.WorkFlowEditor.DMEEditor = DMEEditor;
                //WorkFlowStepEditor.DMEEditor = DMEEditor;
                //DMEEditor.WorkFlowEditor.StepEditor = WorkFlowStepEditor;
                //ruleparser.DMEEditor = DMEEditor;
                //rulesEditor.DMEEditor = DMEEditor;
                //rulesEditor.Parser = ruleparser;
                //DMEEditor.ETL.RulesEditor = rulesEditor;
                //vis = scope.Resolve<IVisManager>();
                //-------------------------------------------

                //---------------------------------------------------------------------------
                // This has to be last step so that all Configuration is ready for Addin's 
                // to be able to use
                //---------------------------------------------------------------------------

                LLoader.LoadAllAssembly();
                Config_editor.LoadedAssemblies = LLoader.Assemblies.Select(c => c.DllLib).ToList();
                string[] args=null;
                IPassedArgs e = new PassedArgs() ;
                ErrorsInfo ErrorsandMesseges= new ErrorsInfo() ;
                if (e.Objects == null)
                {
                    e.Objects = new List<ObjectItem>();
                }
                Form1 frm = new Form1();
               frm.SetConfig(DMEEditor, DMEEditor.Logger, DMEEditor.Utilfunction, args, e, ErrorsandMesseges);
               
               frm.ShowDialog();   
                
               
                

                
            }
        }
    }
}
