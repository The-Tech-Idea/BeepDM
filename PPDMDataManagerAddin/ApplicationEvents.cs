using System;
using System.Globalization;
using DevExpress.XtraBars.Docking2010.Customization;
using DevExpress.XtraBars.Docking2010.Views.WindowsUI;
using DevExpress.XtraEditors;
using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic
using SimpleODM.SharedLib;
using SimpleODM.systemconfigandutil;

namespace My
{

    // The following events are available for MyApplication:
    // 
    // Startup: Raised when the application starts, before the startup form is created.
    // Shutdown: Raised after all application forms are closed.  This event is not raised if the application terminates abnormally.
    // UnhandledException: Raised if the application encounters an unhandled exception.
    // StartupNextInstance: Raised when launching a single-instance application and the application is already active. 
    // NetworkAvailabilityChanged: Raised when the network connection is connected or disconnected.
    internal partial class MyApplication
    {
        public MyApplication()
        {
            this.NetworkAvailabilityChanged += MyApplication_NetworkAvailabilityChanged;
            this.Shutdown += MyApplication_Shutdown;
            this.Startup += MyApplication_Startup;
            this.StartupNextInstance += MyApplication_StartupNextInstance;
            this.UnhandledException += MyApplication_UnhandledException;
        }
        // Public Protection As New ProtectionModule
        public CultureInfo provider;
        public SharedBusinessObjects Sharedbo;
        public WindowsUIView WindowsUIView1;

        private void MyApplication_NetworkAvailabilityChanged(object sender, Microsoft.VisualBasic.Devices.NetworkAvailableEventArgs e)
        {
            if (e.IsNetworkAvailable == false)
            {
                XtraMessageBox.Show("You are Disconnected from Network !!!");
            }
        }

        private void MyApplication_Shutdown(object sender, EventArgs e)
        {
            Sharedbo = default;
        }

        private void MyApplication_Startup(object sender, Microsoft.VisualBasic.ApplicationServices.StartupEventArgs e)
        {
            provider = CultureInfo.InvariantCulture;
            InitSharedBusinessClass();
        }

        private void MyApplication_StartupNextInstance(object sender, Microsoft.VisualBasic.ApplicationServices.StartupNextInstanceEventArgs e)
        {
        }

        private void MyApplication_UnhandledException(object sender, Microsoft.VisualBasic.ApplicationServices.UnhandledExceptionEventArgs e)
        {
        }

        public uc_login uc_login1;

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
                Interaction.MsgBox("Error : Could not init config !!!", MsgBoxStyle.Critical, "Simple ODM");
            }
            // MsgBox("2")

            Sharedbo.MyModules.uc_login1 = new uc_login(Sharedbo.SimpleODMConfig, Sharedbo.SimpleUtil);
            Sharedbo.MyModules.db_def = new uc_dbdefine(Sharedbo.SimpleODMConfig, Sharedbo.SimpleUtil);
            Sharedbo.MyModules.db_def.MyLoginControl = uc_login1;
            Sharedbo.MyModules.no_priv = new SharedLib.uc_NoPrivControl();

            // MsgBox("22")

            try
            {
            }
            // MsgBox("3")
            // Sharedbo.MyModules.uc_login1.Protection = My.Application.Protection
            // MsgBox("33")
            // Sharedbo.MyModules.uc_login1.LoadDBSchemas(Sharedbo.SimpleODMConfig)
            catch (Exception ex)
            {
                Interaction.MsgBox("Error : Could not assign Login Screen Config !!!", MsgBoxStyle.Critical, "Simple ODM");
            }


            // This call is required by the designer.

            // AutoLoginON = True

            try
            {
            }
            // MsgBox("4")
            // My.Application.Protection.UpdateSettings()
            // My.Application.Protection.CheckExpirationDaysLock()
            // Sharedbo.Protection = Protection
            // Sharedbo.SimpleODMConfig.Protection = Protection
            // MsgBox("44")
            catch (Exception ex)
            {
                Interaction.MsgBox("Error : Could not run  protection code !!!", MsgBoxStyle.Critical, "Simple ODM");
            }
            // MsgBox("5")
            DevExpress.Skins.SkinManager.EnableFormSkins();
            DevExpress.LookAndFeel.LookAndFeelHelper.ForceDefaultLookAndFeelChanged();
            DevExpress.UserSkins.BonusSkins.Register();
            DevExpress.LookAndFeel.UserLookAndFeel.Default.SkinName = "Metropolis";
            // MsgBox("55")

            My.Application.Sharedbo.SimpleODMConfig.Defaults.APPLICATIONNAME = "Simple Oil and Gas Data Manager (SODM) - Enterprize Edition";
            My.Application.Sharedbo.SimpleODMConfig.CreatemyFolder();
            My.Application.Sharedbo.SimpleODMConfig.LoadDefaults();
            if (My.Application.Sharedbo.SimpleODMConfig.Defaults.PPDMAGREEMENT == false)
            {
                var agreemppdm = new SharedLib.frm_ppdmagreement(My.Application.Sharedbo.SimpleODMConfig);
                agreemppdm.ShowDialog();
                if (My.Application.Sharedbo.SimpleODMConfig.Defaults.PPDMAGREEMENT == true)
                {
                    My.Application.Sharedbo.SimpleODMConfig.WriteDefaults();
                    var agreem = new SharedLib.frm_Agreement(My.Application.Sharedbo.SimpleODMConfig);
                    var wiz = new SharedLib.frm_startup_wizard();
                    agreem.ShowDialog();
                    if (My.Application.Sharedbo.SimpleODMConfig.Defaults.AGREEMENT == false)
                    {
                        Sharedbo.SimpleODMConfig.WriteDefaults();
                        // Sharedbo.SimpleODMConfig.PPDMContext.closeall()
                        Environment.Exit(0);
                    }
                    else
                    {
                        My.Application.Sharedbo.SimpleODMConfig.WriteDefaults();
                    }
                }
                // wiz.ShowDialog()
                else
                {
                    Environment.Exit(0);
                }
            }
        }
    }
}