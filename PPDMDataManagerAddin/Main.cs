using Autofac;
using DevExpress.XtraBars.Docking2010.Views.WindowsUI;
using DevExpress.XtraEditors;
using SimpleODM.SharedLib;
using SimpleODM.systemconfigandutil;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace SimpleODM
{
    public class Main: IDM_Addin
    {
        private static IContainer Container { get; set; }
        private static ContainerBuilder Builder { get; set; }
        public SharedBusinessObjects Sharedbo { get; set; }
        public WindowsUIView WindowsUIView1 { get; set; }
        public uc_login uc_login1 { get; set; }
        // public IPPDMContext PPDMContext { get; set; }
        // public IPPDMConfig PPDMConfig { get; set; }
        public IRDBSource RDBMS { get; set; }
        public string ParentName { get  ; set  ; }
        public string ObjectName { get  ; set  ; }
        public string ObjectType { get; set; } = "Form";
        public string AddinName { get; set; } = "Simpl Oil&Gas Data Manager";
        public string Description { get  ; set  ; } = "Simpl Oil&Gas Data Manager";
        public bool DefaultCreate { get; set; } = true;
        public string DllPath { get  ; set  ; }
        public string DllName { get  ; set  ; }
        public string NameSpace { get  ; set  ; }
        public DataSet Dset { get  ; set  ; }
        public IErrorsInfo ErrorObject { get  ; set  ; }
        public IDMLogger Logger { get  ; set  ; }
        public IDMEEditor DME_Editor { get  ; set  ; }
        public EntityStructure EntityStructure { get  ; set  ; }
        public string EntityName { get  ; set  ; }
        public PassedArgs Args { get  ; set  ; }

        public event EventHandler<PassedArgs> OnObjectSelected;

        //public static IContainer Configure() //ContainerBuilder builder
        //{

        //    Builder = new ContainerBuilder();

        //    Builder.RegisterType<PPDMContext>().As<IPPDMContext>().SingleInstance();
        //    Builder.RegisterType<PPDMConfig>().As<IPPDMConfig>().SingleInstance();

        //    return Builder.Build();
        //}
        //public void MainApp(IRDBSource pRDBMS)
        //{
        //    RDBMS = pRDBMS;
        //    Container = Configure();
        //    using (var scope = Container.BeginLifetimeScope())
        //    {
        //        PPDMContext = scope.Resolve<IPPDMContext>();
        //        PPDMConfig = scope.Resolve<IPPDMConfig>();
        //        PPDMContext.DatabaseConnectionstring = RDBMS.Dataconnection.ConnectionProp.ConnectionString;
        //        PPDMContext.OpenConnection();
        //    }
        //}
        public void InitSharedBusinessClass()
        {
            try
            {
                Sharedbo = new SharedBusinessObjects();
                // MsgBox("1")
                Sharedbo.SimpleODMConfig = new SimpleODM.systemconfigandutil.PPDMConfig();
                Sharedbo.SimpleODMConfig.AppType = "SIMPLEODMEE";
                Sharedbo.SimpleODMConfig.CreatemyFolder();
            }
            // MsgBox("11")
            catch (Exception ex)
            {
                XtraMessageBox.Show("Error : Could not init config !!!",  "Simple ODM");
            }
            // MsgBox("2")

            Sharedbo.MyModules.uc_login1 = new uc_login(  Sharedbo.SimpleODMConfig, Sharedbo.SimpleUtil);
            Sharedbo.MyModules.db_def = new uc_dbdefine( Sharedbo.SimpleODMConfig, Sharedbo.SimpleUtil);
            Sharedbo.MyModules.db_def.MyLoginControl = uc_login1;
            Sharedbo.MyModules.no_priv = new SharedLib.uc_NoPrivControl();

            // MsgBox("22")



            // MsgBox("5")
            DevExpress.Skins.SkinManager.EnableFormSkins();
            DevExpress.LookAndFeel.LookAndFeelHelper.ForceDefaultLookAndFeelChanged();
           // DevExpress.UserSkins.BonusSkins.Register();
            DevExpress.LookAndFeel.UserLookAndFeel.Default.SkinName = "Metropolis";
            // MsgBox("55")

            Sharedbo.SimpleODMConfig.Defaults.APPLICATIONNAME = "Simple Oil and Gas Data Manager (SODM) - Enterprize Edition";
            Sharedbo.SimpleODMConfig.CreatemyFolder();
            Sharedbo.SimpleODMConfig.LoadDefaults();
            if (Sharedbo.SimpleODMConfig.Defaults.PPDMAGREEMENT == false)
            {
                var agreemppdm = new SharedLib.frm_ppdmagreement(Sharedbo.SimpleODMConfig);
                agreemppdm.ShowDialog();
                if (Sharedbo.SimpleODMConfig.Defaults.PPDMAGREEMENT == true)
                {
                    Sharedbo.SimpleODMConfig.WriteDefaults();
                    var agreem = new SharedLib.frm_Agreement(Sharedbo.SimpleODMConfig);
                    var wiz = new SharedLib.frm_startup_wizard();
                    agreem.ShowDialog();
                    if (Sharedbo.SimpleODMConfig.Defaults.AGREEMENT == false)
                    {
                        Sharedbo.SimpleODMConfig.WriteDefaults();
                        // Sharedbo.SimpleODMConfig.PPDMContext.closeall()
                        Environment.Exit(0);
                    }
                    else
                    {
                        Sharedbo.SimpleODMConfig.WriteDefaults();
                    }
                }
               
                else
                {
                    Environment.Exit(0);
                }
            }
            frm_Main frm_Main = new frm_Main(Sharedbo);
            frm_Main.Show();
        }

        public void RaiseObjectSelected()
        {
            throw new NotImplementedException();
        }

        public void Run(string param1)
        {
            throw new NotImplementedException();
        }
        public Main()
        {

        }
        public void SetConfig(IDMEEditor pbl, IDMLogger plogger, IUtil putil, string[] args, PassedArgs e, IErrorsInfo per)
        {
            InitSharedBusinessClass();
        }
    }
}
