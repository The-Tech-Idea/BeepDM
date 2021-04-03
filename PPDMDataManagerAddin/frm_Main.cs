using System;
using System.Data;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using DevExpress.XtraBars.Docking2010.Views.WindowsUI;
using DevExpress.XtraEditors;
using SimpleODM.SharedLib;
using TheTechIdea;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace SimpleODM
{
    public partial class frm_Main :Form, IDM_Addin
    {
        private object current;
        public string Title;
        private bool loginStatus = false;
        private bool InitForms = false;
        private string WellBoreItem;
        private string TransType;
        private string ucType;
        private bool closeflag = false;

        public bool AutoLoginON { get; set; } = false;
        public string ParentName { get; set; }
        public string ObjectName { get; set; }
        public string ObjectType { get; set; } = "Form";
        public string AddinName { get; set; } = "Simpl Oil&Gas Data Manager";
        public string Description { get; set; } = "Simpl Oil&Gas Data Manager";
        public bool DefaultCreate { get; set; } = true;
        public string DllPath { get; set; }
        public string DllName { get; set; }
        public string NameSpace { get; set; }
        public DataSet Dset { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public IDMLogger Logger { get; set; }
        public IDMEEditor DME_Editor { get; set; }
        public EntityStructure EntityStructure { get; set; }
        public string EntityName { get; set; }
        public PassedArgs Args { get; set; }

        public event DoAutoLoginEventHandler DoAutoLogin;
        public event EventHandler<PassedArgs> OnObjectSelected;

        public delegate void DoAutoLoginEventHandler(ref bool Cancel);

        private bool drag;
        private int mousex;
        private int mousey;

        public frm_Main()

        {

            // This call is required by the designer.
            InitializeComponent();




        }
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
                XtraMessageBox.Show("Error : Could not init config !!!", "Simple ODM");
            }
            // MsgBox("2")

            Sharedbo.MyModules.uc_login1 = new uc_login(Sharedbo.SimpleODMConfig, Sharedbo.SimpleUtil);
            Sharedbo.MyModules.db_def = new uc_dbdefine(Sharedbo.SimpleODMConfig, Sharedbo.SimpleUtil);
            Sharedbo.MyModules.db_def.MyLoginControl = Sharedbo.MyModules.uc_login1;
            Sharedbo.MyModules.no_priv = new SimpleODM.SharedLib.uc_NoPrivControl();

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
                var agreemppdm = new SimpleODM.SharedLib.frm_ppdmagreement(Sharedbo.SimpleODMConfig);
                agreemppdm.ShowDialog();
                if (Sharedbo.SimpleODMConfig.Defaults.PPDMAGREEMENT == true)
                {
                    Sharedbo.SimpleODMConfig.WriteDefaults();
                    var agreem = new SimpleODM.SharedLib.frm_Agreement(Sharedbo.SimpleODMConfig);
                    var wiz = new SimpleODM.SharedLib.frm_startup_wizard();
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

        }

        public frm_Main(SharedBusinessObjects pSharedbo)
        {

            // My.Application.Protection.UpdateSettings()

            // This call is required by the designer.

            // Add any initialization after the InitializeComponent() call.

        }

        private SharedBusinessObjects _Sharedbo;

        public SharedBusinessObjects Sharedbo
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _Sharedbo;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_Sharedbo != null)
                {
                    /* TODO ERROR: Skipped RegionDirectiveTrivia */
                    _Sharedbo.Logout -= Uc_login1_Logout;
                    _Sharedbo.ShowDatabase -= Uc_login1_ShowDatabase;
                    _Sharedbo.LoginCancel -= _uc_login_LoginCancel;
                    _Sharedbo.LoginSucccess -= _uc_login_LoginSucccess;

                    /* TODO ERROR: Skipped EndRegionDirectiveTrivia *//* TODO ERROR: Skipped RegionDirectiveTrivia */
                    _Sharedbo.ShowControlOnTile -= Sharedbo_ShowControlOnTile;
                }

                _Sharedbo = value;
                if (_Sharedbo != null)
                {
                    _Sharedbo.Logout += Uc_login1_Logout;
                    _Sharedbo.ShowDatabase += Uc_login1_ShowDatabase;
                    _Sharedbo.LoginCancel += _uc_login_LoginCancel;
                    _Sharedbo.LoginSucccess += _uc_login_LoginSucccess;
                    _Sharedbo.ShowControlOnTile += Sharedbo_ShowControlOnTile;
                }
            }
        }

        private WindowsUIView _WindowsUIView1;

        public WindowsUIView WindowsUIView1
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _WindowsUIView1;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_WindowsUIView1 != null)
                {
                    _WindowsUIView1.NavigatedFrom -= WindowsUIView1_NavigatedFrom;
                    _WindowsUIView1.ContentContainerActionCustomization -= WindowsUIView1_ContentContainerActionCustomization;
                    _WindowsUIView1.QueryStartupContentContainer -= WindowsUIView1_QueryStartupContentContainer;
                }

                _WindowsUIView1 = value;
                if (_WindowsUIView1 != null)
                {
                    _WindowsUIView1.NavigatedFrom += WindowsUIView1_NavigatedFrom;
                    _WindowsUIView1.ContentContainerActionCustomization += WindowsUIView1_ContentContainerActionCustomization;
                    _WindowsUIView1.QueryStartupContentContainer += WindowsUIView1_QueryStartupContentContainer;
                }
            }
        }



        public static void ScaleForm(System.Windows.Forms.Form WindowsForm)
        {
            using (var g = WindowsForm.CreateGraphics())
            {
                float sngScaleFactor = 1f;
                float sngFontFactor = 1f;
                if (g.DpiX > 96f)
                {
                    sngScaleFactor = g.DpiX / 96f;
                    // sngFontFactor = 96 / g.DpiY
                }

                if (WindowsForm.AutoScaleDimensions == WindowsForm.CurrentAutoScaleDimensions)
                {
                    // ucWindowsFormHost.ScaleControl(WindowsForm, sngFontFactor)
                    WindowsForm.Scale(sngScaleFactor);
                }
            }
        }

        private void PanelControl1_DoubleClick(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Maximized)
            {
                this.WindowState = FormWindowState.Normal;
            }
            else
            {
                this.WindowState = FormWindowState.Maximized;
            }
        }

        private void Form1_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            drag = true;
            mousex = e.X - this.Left;
            mousey = e.Y - this.Top;
        }

        private void Form1_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (drag)
            {
                this.Top = e.Y - mousey;
                this.Left = e.X - mousex;
            }
        }

        private void Form1_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            drag = false;
        }

        private void MainMenuWindow8UI_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            Sharedbo.SimpleODMConfig.RaisedKeyDown(sender, e);
        }

        private void MainMenuWindow8UI_Load(object sender, EventArgs e)
        {
            ScaleForm(this);
            if (this.DesignMode == false)
            {
                try
                {
                    Sharedbo.WindowsUIView1 = WindowsUIView1;
                    WindowsUIView1.QueryControl += Sharedbo.WindowsUIView1_QueryControl;
                    WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.LoginPage);
                }
                catch (Exception ex)
                {
                    XtraMessageBox.Show(ex.Message + " in Login Screen", "Simple ODM");
                }
            }
        }

        private void WindowsUIView1_NavigatedFrom(object sender, DevExpress.XtraBars.Docking2010.Views.WindowsUI.NavigationEventArgs e)
        {
            // If e.Target.Caption = "Well Logs" Then
            // If Sharedbo.MyModules.Well_Logs.WellON = False Then
            // WindowsUIView1.Controller.Activate(Me.Uc_tileMenuUI1.WellLogManagerPage)


            // End If
            // End If
            // If e.Target.Caption = "Well Set Management" Then

            // If Sharedbo.MyModules.Well_Logs.WellON = False And e.Document.Caption = "Well Logs" Then
            // WindowsUIView1.Controller.Activate(Me.Uc_tileMenuUI1.WellLogManagerPage)


            // End If
            // End If
        }

        private void WindowsUIView1_ContentContainerActionCustomization(object sender, DevExpress.XtraBars.Docking2010.Views.WindowsUI.ContentContainerActionCustomizationEventArgs e)
        {
            string name = e.ContentContainer.Name;
            // Select Case name
            // 'The default 'Back' action 
            // Case "page1"
            // e.Remove(ContentContainerAction.Back)
            // 'The default split group 'Overview' action 
            // Case "slideGroup1"
            // e.Remove(SplitGroupAction.Overview)
            // 'The default tile container 'Clear Container' action 
            // Case "tileContainer1"
            // e.Remove(TileContainerAction.ClearSelection)
            // 'Custom action 
            // Case "pageGroup1"
            // e.Remove(myAction)
            // End Select
        }

        private void WindowsUIView1_QueryStartupContentContainer(object sender, QueryContentContainerEventArgs e)
        {
            try
            {
                e.ContentContainer = this.Uc_tileMenuUI1.LoginPage;
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(ex.Message + " in Login Screen", "Simple ODM");
            }
        }

        private void Uc_login1_Logout()
        {
            loginStatus = false;
            // My.Application.InitSharedBusinessClass()
            // For Each x As Document In WindowsUIView1.Documents
            // If Not x.Equals(WindowsUIView1.Controller.View.ActiveDocument) Then

            // sharedbo.loginStatus = False
            // Debug.Print(x.ActionCaption)
            // WindowsUIView1.ReleaseDeferredLoadControl(x)
            // sharedbo.loginStatus = True
            // End If
            // 'If x.ActionCaption <> "Login" Or x.ActionCaption <> "Simple Oil and Gas Data Manager" Then
            // '    sharedbo.loginStatus = False
            // '    Debug.Print(x.ActionCaption)
            // '    WindowsUIView1.ReleaseDeferredLoadControl(x)
            // 'Else
            // '    sharedbo.loginStatus = True
            // 'End If
            // Next

            // WindowsUIView1.Controller
        }

        private void Uc_login1_ShowDatabase()
        {
            WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.DatabaseContainer);
        }

        private void _uc_login_LoginCancel()
        {
            CloseMe();
        }

        private void _uc_login_LoginSucccess()
        {
            loginStatus = true;
            InitForms = false;
            WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.TileContainer1);
        }

        /* TODO ERROR: Skipped EndRegionDirectiveTrivia *//* TODO ERROR: Skipped RegionDirectiveTrivia */    // Private Sub AddGlobalActions()

        // Dim HelpAction As DelegateAction = New DelegateAction(AddressOf HelpCanExecuteFunction, AddressOf HelpExecuteFunction)
        // HelpAction.Caption = "Help"
        // HelpAction.Type = ActionType.Context
        // HelpAction.Edge = ActionEdge.Left
        // HelpAction.Image = ImageCollection1.Images(8)
        // HelpAction.Behavior = ActionBehavior.HideBarOnClick
        // WindowsUIView1.ContentContainerActions.Add(HelpAction)

        // Dim TileContainercustomAction As DelegateAction = New DelegateAction(AddressOf TileContcanExecuteGlobalFunction, AddressOf TileContactionGlobalFunction)
        // TileContainercustomAction.Caption = "Menu"
        // TileContainercustomAction.Type = ActionType.Navigation
        // TileContainercustomAction.Edge = ActionEdge.Left
        // TileContainercustomAction.Image = ImageCollection1.Images(7)
        // TileContainercustomAction.Behavior = ActionBehavior.HideBarOnClick

        // WindowsUIView1.ContentContainerActions.Add(TileContainercustomAction)


        // Dim LoginContainercustomAction As DelegateAction = New DelegateAction(AddressOf LogincanExecuteGlobalFunction, AddressOf LoginactionGlobalFunction)
        // LoginContainercustomAction.Caption = "Login/Logout"
        // LoginContainercustomAction.Type = ActionType.Navigation
        // LoginContainercustomAction.Edge = ActionEdge.Right
        // LoginContainercustomAction.Image = ImageCollection1.Images(4)
        // LoginContainercustomAction.Behavior = ActionBehavior.HideBarOnClick
        // WindowsUIView1.ContentContainerActions.Add(LoginContainercustomAction)

        // Dim ExitContainercustomAction As DelegateAction = New DelegateAction(AddressOf LogincanExecuteGlobalFunction, AddressOf ExitactionGlobalFunction)
        // ExitContainercustomAction.Caption = "Exit"
        // ExitContainercustomAction.Type = ActionType.Navigation
        // ExitContainercustomAction.Edge = ActionEdge.Right
        // ExitContainercustomAction.Image = ImageCollection1.Images(3)
        // ExitContainercustomAction.Behavior = ActionBehavior.HideBarOnClick
        // WindowsUIView1.ContentContainerActions.Add(ExitContainercustomAction)
        // WindowsUIView1.UpdateDocumentActions()
        // End Sub
        private bool HelpCanExecuteFunction()
        {
            return true;
        }

        private void HelpExecuteFunction()
        {
            Help.ShowHelp(this, Sharedbo.SimpleODMConfig.appFolder + @"\simpleodm.chm");
        }

        private bool LogincanExecuteGlobalFunction()
        {
            // Checks if current conditions meet requirements to execute the 'actionFunction' 

            return true;
        }

        private void ExitactionGlobalFunction()
        {

            // Performs custom action 
            if (XtraMessageBox.Show("Do you Want to exit the application?", "Simple ODM", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                CloseMe();
            }
        }

        private void LoginactionGlobalFunction()
        {
            // Performs custom action 
            WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.LoginPage);
        }

        private bool TileContcanExecuteGlobalFunction()
        {
            // Checks if current conditions meet requirements to execute the 'actionFunction' 
            if (loginStatus)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void TileContactionGlobalFunction()
        {
            // Performs custom action 
            if (loginStatus == true)
            {
                WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.TileContainer1);
            }
        }

        private bool canExecuteGlobalFunction()
        {
            // Checks if current conditions meet requirements to execute the 'actionFunction' 
            if (loginStatus)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void ExitsystemactionGlobalFunction()
        {
            // Performs custom action 

            WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.OilFieldManagerDocument);
        }

        private void CloseMe()
        {
            this.Close();
            // If closeflag = False Then
            // 'My.Application.Sharedbo.MyModules.uc_login1.Dispose()
            // My.Application.Sharedbo.MyModules = Nothing
            // My.Application.Sharedbo.SimpleODMconfig.ppdmcontext.closeall()
            // My.Application.Sharedbo.SimpleODMConfig = Nothing
            // My.Application.Sharedbo.SimpleUtil = Nothing
            // 'Sharedbo.MyModules = Nothing
            // closeflag = True
            // Me.Close()


            // End If
        }

        public void Sharedbo_ShowControlOnTile(string Title, string pType, string trans)
        {
            WellBoreItem = Title;
            ucType = pType;
            TransType = trans;
            switch (ucType ?? "")
            {
                case "LOGLOADER":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.LogLoaderPage);
                        break;
                    }

                case "WELLLOGMANAGER":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.LogFileLoaderPage);
                        break;
                    }

                case "DBCREATE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.DatabaseModelCreateToolPage);
                        break;
                    }

                case "RVM":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.RVMPage);
                        break;
                    }

                case "ADMINISTRATOR":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.AdministratorPageGroup);
                        break;
                    }
                // -------------------------------------------Well Log Management ---------------------------------------
                case "WELLLOGPARAMETER":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellLogParametersPage);
                        break;
                    }

                case "WELLLOGREMARK":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellLogRemarkPage);
                        break;
                    }

                case "WELLLOGJOB":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.welllogjobPage);
                        break;
                    }

                case "WELLLOGCURVE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellLogCurvePage);
                        break;
                    }

                case "WELLLOGPARAMETERAPP":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellLogParametersPageapp);
                        break;
                    }

                case "WELLLOGREMARKAPP":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellLogRemarkPageapp);
                        break;
                    }

                case "WELLLOGJOBAPP":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.welllogjobPageapp);
                        break;
                    }

                case "WELLLOGCURVEAPP":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellLogCurvePageapp);
                        break;
                    }

                case "WELLLOGPARAMETERCLASS":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellLogParameterClassificationPage);
                        break;
                    }

                case "WELLLOGPARAMETERARRAY":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellLogParameterArrayPage);
                        break;
                    }

                case "WELLLOGJOBTRIP":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellLogJobTripPage);
                        break;
                    }

                case "WELLLOGJOBTRIPPASS":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellLogJobTripPassPage);
                        break;
                    }

                case "WELLLOGJOBTRIPREMARK":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellLogJobTripRemarkPage);
                        break;
                    }

                case "WELLLOGDICTIONARYAPP":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.LogDataDictManagerPageapp);
                        break;
                    }

                case "WELLLOGDICTIONARY":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.LogDataDictManagerPage);
                        break;
                    }

                case "LOGDICTALIAS":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellLogDictionaryAliasPage);
                        break;
                    }

                case "LOGDICTCURVE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellLogDictionaryCurvePage);
                        break;
                    }

                case "LOGDICTCURVECLASS":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellLogDictionaryCurveClassificationPage);
                        break;
                    }

                case "LOGDICTPARAMETER":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellLogDictionaryParameterPage);
                        break;
                    }

                case "LOGDICTPARAMETERCLASS":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellLogDictionaryParameterClassificationPage);
                        break;
                    }

                case "LOGDICTPARAMETERCLASSTYPES":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellLogDictionaryParameterClassificationTypesPage);
                        break;
                    }

                case "LOGDICTPARAMETERVALUE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellLogDictionaryParameterValuesPage);
                        break;
                    }

                case "LOGDICTBA":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellLogDictionaryBusinessAssociatePage);
                        break;
                    }

                case "LOGDICTPROCEDURE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellLogDictionaryProcedurePage);
                        break;
                    }

                case "WELLLOGCLASSESAPP":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellLogClassificationPageapp);
                        break;
                    }

                case "WELLLOGCLASSES":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellLogClassificationPage);
                        break;
                    }

                case "LOGACTIVITY":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.LogActivityPage);
                        break;
                    }



                // ------------------------------------ Lithology -------------------------------------------------------
                case "LITHLOG":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.LithDescriptiveRecordofLitholgyPage);
                        break;
                    }

                case "LITHLOGREMARK":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.LithDescriptiveRecordofLitholgyRemarksPage);
                        break;
                    }

                case "LITHLOGBASERVICE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.LithDescriptiveRecordofLitholgyBAServicesPage);
                        break;
                    }

                case "LITHLOGENVINT":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.LithAnInterpreteddepositionalEnvoverSpecifiedIntervalofaDescriptiveRecordofLitholgyPage);
                        break;
                    }

                case "LITHLOGDEPTHINT":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.LithADepthIntervalDescriptiveRecordofLitholgyPage);
                        break;
                    }

                case "LITHINTERVALROCKTYPE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.LithDescriptionofRockTypeComprisinganIntervalPage);
                        break;
                    }

                case "LITHINTERVALROCKTYPECOMP":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.LithDescriptionofMajororminorRockComponentsPage);
                        break;
                    }

                case "LITHCOMPCOLOR":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.LithColorDescriptionofMajororminorRockComponentsPage);
                        break;
                    }

                case "LITHCOMPGRAINSIZE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.LithMeasuredSizesinRockComponentsPage);
                        break;
                    }

                case "LITHINTERVALROCKTYPEDIAG":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.LithDescriptionofthePostDepositionalAlterationsPage);
                        break;
                    }

                case "LITHINTERVALROCKTYPEGRAINSIZE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.LithDescriptionofGrainorCrystalsizesofRockComponentsPage);
                        break;
                    }

                case "LITHINTERVALROCKTYPEPOROSITY":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.LithTheObservedPorosityofRockComponentsPage);
                        break;
                    }

                case "LITHINTERVALROCKTYPECOLOR":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.LithDescriptiveRecordofLitholgyPage);
                        break;
                    }

                case "LITHINTERVALROCKTYPESTRUCTURE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.LithDescriptionofPhysicalStructureRockTypePage);
                        break;
                    }

                case "LITHINTERVALSTRUCTURE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.LithPhysicalStructurewithinamajorrocktypeorSubIntervalPage);
                        break;
                    }

                case "LITHSAMPLECOLLECTION":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.LithLithologySampleCollectionPage);
                        break;
                    }

                case "LITHSAMPLEDESC":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.LithologySampleDescriptionPage);
                        break;
                    }

                case "LITHSAMPLEPREP":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.LithDescribethePhysicalorChemicalProcessusedtoperparetheSamplePage);
                        break;
                    }

                case "LITHSAMPLEPREPMETH":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.LithDescribetheMethodsusedtoperparetheSamplePage);
                        break;
                    }

                case "LITHDESCOTHER":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.LithOtherDescriptionstotheLithogloySamplePage);
                        break;
                    }
                // ------------------------------------ Reserve Entities and Classisifactions --------------------
                case "RESERVECLASSECO":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ReserveEntityEconomicRunPage);
                        break;
                    }

                case "RESERVECLASSPRODUCT":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ReserveEntityProductPage);
                        break;
                    }

                case "RESERVECLASSECOPARAM":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ReserveEntityEconomicRunParametersPage);
                        break;
                    }

                case "RESERVECLASSECOVOLUME":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ReserveEntityEconomicRunVolumePage);
                        break;
                    }

                case "RESERVEPRODPROP":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ReserveEntityProductPropertiesPage);
                        break;
                    }

                case "RESERVEPRODVOLSUMMARY":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ReserveEntityProductVolumeSummaryPage);
                        break;
                    }

                case "RESERVEVOLREVISIONS":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ReserveEntityProductVolumeRevisionPage);
                        break;
                    }

                case "RESERVECLASSIFICATIONSFORMULA":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ReserveClassFormulaPage);
                        break;
                    }

                case "RESERVECLASSIFICATIONSFORMULACALC":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ReserveClassFormulaCalculationPage);
                        break;
                    }

                case "RESERVECLASSIFICATIONS":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ReserveClassificationsPage);
                        break;
                    }

                case "RESERVEENTITYCLASS":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ReserveEntitiesClassPage);
                        break;
                    }

                case "RESERVECROSSREF":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ReserveEntitiesCrossReferencePage);
                        break;
                    }

                case "RESERVEUNITREGIME":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.VolumeUnitRegimePage);
                        break;
                    }

                case "RESERVEREVISION":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ReserveRevisionCategoryPage);
                        break;
                    }

                // ------------------------------------ PDEN ----------------------------

                case "PDENBA":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ProductionEntityasBusinessAssociatePage);
                        break;
                    }

                case "PDENAREA":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ProductionEntityasAreaPage);
                        break;
                    }

                case "PDENOTHER":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ProductionEntityasOtherPage);
                        break;
                    }

                case "PDENWELL":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ProductionEntityasWellPage);
                        break;
                    }

                case "PDENPRODSTRING":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ProductionEntityasProductionStringPage);
                        break;
                    }

                case "PDENRESERVE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ProductionEntityasReservePage);
                        break;
                    }

                case "PDENFORMATION":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ProductionEntityasFormationPage);
                        break;
                    }

                case "PDENPOOL":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ProductionEntityasPoolReservoirPage);
                        break;
                    }

                case "PDENLEASE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ProductionEntityasLeasePage);
                        break;
                    }

                case "PDENRESERVECLASS":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ProductionEntityasReserveClassPage);
                        break;
                    }

                case "PDENFACILITY":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ProductionEntityasFacilityPage);
                        break;
                    }

                case "PDENFIELD":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ProductionEntityasFieldPage);
                        break;
                    }

                case "PDENALLOCFACTOR":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ProductionEntityAllocationFactorPage);
                        break;
                    }

                case "PDENDECLINECASE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ProductionEntityDeclineForcastCasePage);
                        break;
                    }

                case "PDENDECLINECASECOND":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ProductionEntityDeclineForcastCaseConditionsPage);
                        break;
                    }

                case "PDENDECLINECASESEG":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ProductionEntityDeclineForcastCaseSegmentsPage);
                        break;
                    }

                case "PDENFLOWMEASURE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ProductionEntityFlowMeasurementPage);
                        break;
                    }

                case "PDENINAREA":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ProductionEntityInAreaPage);
                        break;
                    }

                case "PDENPRODSTRTOPDENCROSSREF":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ProductionEntityProductionStringtoPDENCrossReferencePage);
                        break;
                    }

                case "PDENALLOWABLE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ProductionEntityProductionStringContributionAllowablePage);
                        break;
                    }

                case "PDENMATERIALBAL":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ProductionEntityMaterialBalancePage);
                        break;
                    }

                case "PDENOPERHIST":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ProductionEntityOperatorHistoryPage);
                        break;
                    }

                case "PDENSTATUSHIST":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ProductionEntityStatusHistoryPage);
                        break;
                    }

                case "PDENVOLDISP":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ProductionEntityVolumeDispositionPage);
                        break;
                    }

                case "PDENVOLREGIME":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ProductionEntityUnitRegimePage);
                        break;
                    }

                case "PDENVOLSUMMARY":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ProductionEntityVolumeSummaryPage);
                        break;
                    }

                case "PDENVOLSUMMARYOTHER":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ProductionEntityVolumeSummaryOtherPage);
                        break;
                    }

                case "PDENVOLANALYSIS":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ProductionEntityVolumeAnalysisPage);
                        break;
                    }

                case "PDENCROSSREF":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ProductionEntityCrossRefererncePage);
                        break;
                    }
                // ------------------------------------Pool ------------------------------
                case "POOLALIAS":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.PoolAliasPage);
                        break;
                    }

                case "POOLAREA":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.PoolAreaPage);
                        break;
                    }

                case "POOLINSTRUMENT":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.PoolInstrumentPage);
                        break;
                    }

                case "POOLVERSION":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.PoolVersionPage);
                        break;
                    }
                // ------------------------------------- Area ----------------------------
                case "AREAALIAS":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.AreaAliasPage);
                        break;
                    }

                case "AREACONTAIN":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.AreaContainPage);
                        break;
                    }

                case "AREADESCRIPTION":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.AreaDescriptionPage);
                        break;
                    }
                // -------------------------------------Applications --------------------
                case "APPLICATIONALIAS":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ApplicationsAliasPage);
                        break;
                    }

                case "APPLICATIONAREA":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ApplicationsAreaPage);
                        break;
                    }

                case "APPLICATIONATTACH":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ApplicationsAttachmentPage);
                        break;
                    }

                case "APPLICATIONBA":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ApplicationsBusinessAssociatePage);
                        break;
                    }

                case "APPLICATIONDESC":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ApplicationsDescriptionPage);
                        break;
                    }

                case "APPLICATIONREMARK":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ApplicationsRemarkPage);
                        break;
                    }
                // -------------------------------------Catalog -------------------------
                case "CAT_ADDITIVEALIAS":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.CatalogAdditiveAliasPage);
                        break;
                    }

                case "CAT_ADDITIVESPEC":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.CatalogAdditiveSpecificationPage);
                        break;
                    }

                case "CAT_ADDITIVETYPE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.CatalogAdditiveTypePage);
                        break;
                    }

                case "CAT_ADDITIVEXREF":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.CatalogAdditiveCrossReferencePage);
                        break;
                    }

                case "CAT_EQUIPALIAS":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.CatalogEquipmentAliasPage);
                        break;
                    }

                case "CAT_EQUIPSPEC":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.CatalogEquipmentSpecificationPage);
                        break;
                    }
                // ------------------------------------- Facility ------------------------

                case "FACILITYALIAS":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.FacilityAliasPage);
                        break;
                    }

                case "FACILITYAREA":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.FacilityAreaPage);
                        break;
                    }

                case "FACILITYBASERVICE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.FacilityBAServicePage);
                        break;
                    }

                case "FACILITYCLASS":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.FacilityClassificationPage);
                        break;
                    }

                case "FACILITYDESCRIPTION":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.FacilityDescriptionPage);
                        break;
                    }

                case "FACILITYEQUIPMENT":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.FacilityEquipmentPage);
                        break;
                    }

                case "FACILITYFIELD":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.FacilityFieldPage);
                        break;
                    }

                case "FACILITYLICENSE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.FacilityLicensePage);
                        break;
                    }

                case "FACILITYLICENSEALIAS":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.FacilityLicenseAliasPage);
                        break;
                    }

                case "FACILITYLICENSEAREA":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.FacilityLicenseAreaPage);
                        break;
                    }

                case "FACILITYLICENSECOND":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.FacilityLicenseConditionsPage);
                        break;
                    }

                case "FACILITYLICENSEREMARK":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.FacilityLicenseRemarksPage);
                        break;
                    }

                case "FACILITYLICENSESTATUS":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.FacilityLicenseStatusPage);
                        break;
                    }

                case "FACILITYLICENSETYPE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.FacilityLicenseTypePage);
                        break;
                    }

                case "FACILITYLICENSEVIOLATION":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.FacilityLicenseViolationsPage);
                        break;
                    }

                case "FACILITYMAINTAIN":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.FacilityMaintainancePage);
                        break;
                    }

                case "FACILITYMAINTAINSTATUS":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.FacilityMaintainanceStatusPage);
                        break;
                    }

                case "FACILITYRATE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.FacilityRatePage);
                        break;
                    }

                case "FACILITYRESTRICTION":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.FacilityRestrictionsPage);
                        break;
                    }

                case "FACILITYSTATUS":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.FacilityStatusPage);
                        break;
                    }

                case "FACILITYSUBSTANCE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.FacilitySubstancePage);
                        break;
                    }

                case "FACILITYVERSION":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.FacilityVersionPage);
                        break;
                    }

                case "FACILITYXREF":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.FacilityCrossReferencePage);
                        break;
                    }
                // ------------------------------------- Equipment ------------------------

                case "EQUIPMENTALIAS":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.EquipmentAliasPage);
                        break;
                    }

                case "EQUIPMENTBA":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.EquipmentBusinessAssociatePage);
                        break;
                    }

                case "EQUIPMENTMAINTAIN":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.EquipmentMaintainancePage);
                        break;
                    }

                case "EQUIPMENTMAINTAINSTATUS":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.EquipmentMaintainanceStatusPage);
                        break;
                    }

                case "EQUIPMENTMAINTAINTYPE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.EquipmentMaintainanceTypePage);
                        break;
                    }

                case "EQUIPMENTSPEC":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.EquipmentSpecificationPage);
                        break;
                    }

                case "EQUIPMENTSPECSET":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.EquipmentSpecificationSetPage);
                        break;
                    }

                case "EQUIPMENTSPECSETSPEC":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.EquipmentSpecificationSetSpecPage);
                        break;
                    }

                case "EQUIPMENTSTATUS":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.EquipmentStatusPage);
                        break;
                    }

                case "EQUIPMENTUSAGE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.EquipmentUsageStatisticsPage);
                        break;
                    }

                case "EQUIPMENTCROSSREFERNCE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.EquipmentCrossReferencePage);
                        break;
                    }
                // -------------------------------------- BA ------------------------------
                case "BUSINESSASSOCIATEADDRESS":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.BusinessAssociateAddressPage);
                        break;
                    }

                case "BUSINESSASSOCIATEALIAS":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.BusinessAssociateAliasPage);
                        break;
                    }

                case "BUSINESSASSOCIATEAUTHORITY":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.BusinessAssociateAuthorityPage);
                        break;
                    }

                case "BUSINESSASSOCIATECONSURTUIMSERVICE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.BusinessAssociateConsurtiumServicePage);
                        break;
                    }

                case "BUSINESSASSOCIATECONTACTINFO":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.BusinessAssociateContactInformationPage);
                        break;
                    }

                case "BUSINESSASSOCIATECREW":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.BusinessAssociateCrewPage);
                        break;
                    }

                case "BUSINESSASSOCIATECREWMEMEBERS":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.BusinessAssociateCrewMemebersPage);
                        break;
                    }

                case "BUSINESSASSOCIATEEMPLOYEE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.BusinessAssociateEmployeePage);
                        break;
                    }

                case "BUSINESSASSOCIATELICENSE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.BusinessAssociateLicensePage);
                        break;
                    }

                case "BUSINESSASSOCIATELICENSEALIAS":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.BusinessAssociateLicenseAliasPage);
                        break;
                    }

                case "BUSINESSASSOCIATELICENSEAREA":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.BusinessAssociateLicenseAreaPage);
                        break;
                    }

                case "BUSINESSASSOCIATELICENSECONDITION":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.BusinessAssociateLicenseConditionPage);
                        break;
                    }

                case "BUSINESSASSOCIATELICENSETYPECONDVIOLATION":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.BusinessAssociateLicenseConditionViolationPage);
                        break;
                    }

                case "BUSINESSASSOCIATELICENSEREMARK":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.BusinessAssociateLicenseRemarkPage);
                        break;
                    }

                case "BUSINESSASSOCIATELICENSESTATUS":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.BusinessAssociateLicenseStatusPage);
                        break;
                    }

                case "BUSINESSASSOCIATELICENSETYPE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.BusinessAssociateLicenseTypePage);
                        break;
                    }

                case "BUSINESSASSOCIATELICENSETYPECONDTYPE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.BusinessAssociateLicenseConditionTypePage);
                        break;
                    }

                case "BUSINESSASSOCIATEORGANIZATION":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.BusinessAssociateOrganizationPage);
                        break;
                    }

                case "BUSINESSASSOCIATEPERMIT":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.BusinessAssociatePermitPage);
                        break;
                    }

                case "BUSINESSASSOCIATESERVICE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.BusinessAssociateServicePage);
                        break;
                    }

                case "BUSINESSASSOCIATESERVICEADDRESS":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.BusinessAssociateServiceAddressPage);
                        break;
                    }

                case "BUSINESSASSOCIATEPREFERENCE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.BusinessAssociatePreferencePage);
                        break;
                    }

                case "BUSINESSASSOCIATECROSSREFERENCE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.BusinessAssociateCrossReferencePage);
                        break;
                    }

                case "BUSINESSASSOCIATELICENSETYPECONDTYPECODE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.BusinessAssociateLicenseConditionTypeCodePage);
                        break;
                    }

                case "BUSINESSASSOCIATEDESCRIPTION":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.BusinessAssociateDescriptionPage);
                        break;
                    }

                case "ENTITLEMENTSFORGROUP":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.SecurityGroupsEntitlementsPage);
                        break;
                    }

                case "ENTITLEMENTGROUPS":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.SecurityGroupsPage);
                        break;
                    }

                case "ENTITLEMENTS":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.EntitlementsTypePage);
                        break;
                    }

                case "BUSINESSASSOCIATEENTITLEMENT":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.BusinessAssociateEntitlementPage);
                        break;
                    }

                case "ENTITLEMENTSCOMPONENTS":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.EntitlementsComponentsPage);
                        break;
                    }
                // -------------------------------------- End BA ------------------------------
                // ---------------------------
                case "STRATFIELDINTREPAGE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.StratigraphicFieldStationInterpretedAgePage1);
                        break;
                    }

                case "STRATFIELDNODEVERSION":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.StratigraphicFieldNodeVersionPage);
                        break;
                    }

                case "STRATFIELDNODE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.StratigraphicFieldNodePage);
                        break;
                    }

                case "STRATFIELDSECTION":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.StratigraphicFieldSectionPage);
                        break;
                    }

                case "STRATFIELDFEOMETRY":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.StratigraphicFieldGeometryPage);
                        break;
                    }

                case "STRATFIELDACQUISITION":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.StratigraphicFieldAcquisitionPage);
                        break;
                    }

                case "STRATNAMESETXREF":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.StratigraphicNameSetCrossReferencePage);
                        break;
                    }

                case "STRATUNIT":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.StratigraphicUnitPage);
                        break;
                    }

                case "STRATUNITALIAS":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.StratigraphicUnitAliasPage);
                        break;
                    }

                case "STRATUNITEQUIVALANCE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.StratigraphicUnitEquivalencePage);
                        break;
                    }

                case "STRATUNITHIERARCHY":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.StratigraphicUnitHierarchyPage);
                        break;
                    }

                case "STRATHIERARCYDESCR":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.StratigraphicUnitHierarchyDescriptionPage);
                        break;
                    }

                case "STRATUNITAGE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.StratigraphicUnitAgePage);
                        break;
                    }

                case "STRATUNITDESCRIPTION":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.StratigraphicUnitDescriptionPage);
                        break;
                    }

                case "STRATUNITTOPOLOGY":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.StratigraphicUnitTopologyPage);
                        break;
                    }

                case "STRATCOLUMNAGE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.StratigraphicColumnAgePage);
                        break;
                    }

                case "STRATCOLUMNACQTN":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.StratigraphicColumnAcquisitionPage);
                        break;
                    }

                case "STRATCOLUMNUNIT":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.StratigraphicColumnUnitPage);
                        break;
                    }

                case "STRATCOLUMNCROSSREF":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.StratigraphicColumnCrossReferencePage);
                        break;
                    }

                case "WELLFORMATION":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.StratigraphicWellSectionPage);
                        break;
                    }

                case "WELLSECTIONINTREPAGE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.StratigraphicWellSectionIntrepAgePage);
                        break;
                    }

                case "WELLSECTIONACQUISTION":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.StratigraphicWellSectionAcquisitionPage);
                        break;
                    }

                case "WELLMUDSAMPLE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.MudSamplePage);
                        break;
                    }

                case "WELLMUDRESISTIVITY":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.MudSampleResistivityPage);
                        break;
                    }

                case "WELLMUDPROPERTY":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.MudSamplePropertyPage);
                        break;
                    }

                case "WELLAIRDRILL":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.AirDrillPage);
                        break;
                    }

                case "WELLAIRDRILLINTERVAL":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.AirDrillIntervalPage);
                        break;
                    }

                case "WELLAIRDRILLINTERVALPERIOD":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.AirDrillIntervalPeriodPage);
                        break;
                    }

                case "WELLHORIZDRILL":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.HorizDrillPage);
                        break;
                    }

                case "WELLHORIZDRILLKOP":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.HorizDrillKOPPage);
                        break;
                    }

                case "WELLHORIZDRILLPOE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.HorizDrillPOEPage);
                        break;
                    }

                case "WELLHORIZDRILLSPOKE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.HorizDrillSPOKEPage);
                        break;
                    }

                case "WELLBORETUBULAR":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellboreTubularsPage);
                        break;
                    }

                case "WELLBORETUBULARCEMENT":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellboreTubularsCementPage);
                        break;
                    }

                case "WELLBOREPAYZONE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellborePayzonePage);
                        break;
                    }

                case "WELLBOREZONEINTERVAL":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellboreZoneIntervalPage);
                        break;
                    }

                case "WELLBOREPOROUSINTERVAL":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellborePorousIntervalPage);
                        break;
                    }

                case "WELLBOREPLUGBACK":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellborePlugbackPage);
                        break;
                    }

                case "WELLZONEINTERVALVALUE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellboreZoneIntervalValuePage);
                        break;
                    }

                case "PRESSUREAOF4PT":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellTestGasPressure4ptPage);
                        break;
                    }

                case "PRESSUREAOF":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellTestGasPressureAOFPage);
                        break;
                    }

                case "PRESSUREBH":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellTestGasPressureBHPage);
                        break;
                    }

                case "WELLTESTPRESSURE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellTestGasPressurePage);
                        break;
                    }

                case "WELLTESTREMARKS":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellTestRemarkPage);
                        break;
                    }

                case "WELLTESTMUD":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellTestMudPage);
                        break;
                    }

                case "WELLTESTEQUIP":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellTestEquipmentPage);
                        break;
                    }

                case "WELLTESTSHUTOFF":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellTestShutoffPage);
                        break;
                    }

                case "WELLTESTSTRAT":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellTestStratandFormationPage);
                        break;
                    }

                case "WELLTESTRECORDER":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellTestRecorderPage);
                        break;
                    }

                case "WELLTESTPRESS":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellTestPressurePage);
                        break;
                    }

                case "WELLTESTPRESSMEAS":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellTestPressureMeasurmentPage);
                        break;
                    }

                case "WELLTESTFLOW":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellTestFlowPage);
                        break;
                    }

                case "WELLTESTFLOWMEAS":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellTestFlowMeasurmentPage);
                        break;
                    }

                case "WELLTESTRECOVERY":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellTestRecoveryPage);
                        break;
                    }

                case "WELLTESTCONTAMINANT":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellTestContaminantPage);
                        break;
                    }

                case "WELLTESTPERIOD":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellTestPeriodsPage);
                        break;
                    }

                case "WELLTESTANALYSIS":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellTestAnalysisPage);
                        break;
                    }

                case "WELLTESTCUSHION":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellTestCushionPage);
                        break;
                    }

                case "WELLBORE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellBoreInformationManagementPage);
                        break;
                    }

                case "WELLORIGIN":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellOriginPage);
                        break;
                    }

                case "PRODSTRING":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellProdstringPage);
                        break;
                    }

                case "FORMATION":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellFormationPage);
                        break;
                    }

                case "COMPLETION":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellCompletionPage);
                        break;
                    }

                case "WELLCOMPCI":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.CompletionContactIntervalsPage);
                        break;
                    }

                case "WELLCOMPXREF":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellCompletionLocationandWellCrossReferencePage);
                        break;
                    }

                case "WELLCOMPSTRING2FORM":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellCompletionStringandFormationConnectionPage);
                        break;
                    }

                case "PERFORATION":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellPerforationPage);
                        break;
                    }

                case "PRODSTRINGEQUIPMENT":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.EquipmentOnWellPage); // ProdStringEquipmentPage)
                        break;
                    }

                case "EQUIPMENTSEARCH":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.EquipmentOnWellPage);
                        break;
                    }

                case "WELLBOREEQUIPMENT":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.EquipmentOnWellPage); // WellboreEquipmentPage)
                        break;
                    }

                case "PRODSTRINGFACILITIES":
                case "WBFACILITIES":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.ProdStringConnectedFacilitiesPage);
                        break;
                    }

                case "PRODSTRINGTEST":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellTestdocumentPage);
                        break;
                    }

                case "WELLDESIGN":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellDesignBuildManagementPage);
                        break;
                    }

                case "WELLREMARK":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellRemarkPage);
                        break;
                    }

                case "WELLMISC":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellMiscPage);
                        break;
                    }

                case "WELLAREA":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellAreaPage);
                        break;
                    }

                case "WELLALIAS":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellAliasPage);
                        break;
                    }

                case "WELLVERSION":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellversionPage);
                        break;
                    }

                case "WELLTEST":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellTestdocumentPage);
                        break;
                    }

                case "WELLPRESS":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellGasPressurePage);
                        break;
                    }

                case "WELLFACILITY":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellFacilityPage);
                        break;
                    }

                case "WELLINTER":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellInterpretationPage);
                        break;
                    }

                case "WELLBASERVICES":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellBAServicesPage);
                        break;
                    }

                case "WELLDIC":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellDictionariesPage);
                        break;
                    }

                case "WELLLOG":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellLogsPage);
                        break;
                    }

                case "WELLLAND":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellLandRightsPage);
                        break;
                    }

                case "WELLGEO": // Geometry"
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellGeometryPage);
                        break;
                    }

                case "WELLSRV": // Survey"
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellSurveyPage);
                        break;
                    }

                case "WELLSRVSTATION":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellSurveyStationPage);
                        break;
                    }

                case "WELLSRVGEOMTERY":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellSurveyGeometryPage);
                        break;
                    }

                case "WELLSUPFAC": // Support Facility"
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellSupportFacilityPage);
                        break;
                    }

                case "WELLPERMIT": // Permit"
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellPermitPage);
                        break;
                    }

                case var @case when @case == "WELLALIAS":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellAliasPage);
                        break;
                    }

                case "WELLNODE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellNodePage);
                        break;
                    }

                case "WELLNODEAREA":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellNodeAreaPage);
                        break;
                    }

                case "WELLNODEGEO":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellNodeGeometryPage);
                        break;
                    }

                case "WELLNODEMETEANDBOUND":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellNodeMetesandBoundPage);
                        break;
                    }

                case "WELLNODESTRAT":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellNodeStratigraphyPage);
                        break;
                    }

                case "WELLACTIVITY":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellActivityPage);
                        break;
                    }

                case "WELLACTIVITYCAUSE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellActivityConditionsandEventsPage);
                        break;
                    }

                case "WELLACTIVITYDURATION":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellActivityDurationPage);
                        break;
                    }

                case "WELLCORE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellCorePage);
                        break;
                    }

                case "WELLCOREANALYSIS":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellCoreAnalysisPage);
                        break;
                    }

                case "WELLCOREANALYSISSAMPLE":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellCoreAanlysisSamplePage);
                        break;
                    }

                case "WELLCOREANALYSISSAMPLEREMARK":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellCoreAanlysisSampleRemarksPage);
                        break;
                    }

                case "WELLCOREANALYSISSAMPLEDESCRIPTION":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellCoreAanlysisSampleDescriptionPage);
                        break;
                    }

                case "WELLCOREANALYSISMETHOD":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellCoreAanlysisMethodPage);
                        break;
                    }

                case "WELLCOREANALYSISREMARK":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellCoreAanlysisRemarkPage);
                        break;
                    }

                case "WELLCOREDESCRIPTION":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellCoreDescriptionPage);
                        break;
                    }

                case "WELLCOREDESCRIPTIONSTRAT":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellCoreDescriptionStratigraphyPage);
                        break;
                    }

                case "WELLCORESHIFT":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellCoreShiftPage);
                        break;
                    }

                case "WELLCOREREMARK":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellCoreRemarkPage);
                        break;
                    }

                case "WELLLIC":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellLicensePage);
                        break;
                    }

                case "WELLLICVIOLATIONS":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellLicenseViolationPage);
                        break;
                    }

                case "WELLLICSTATUS":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellLicenseStatusPage);
                        break;
                    }

                case "WELLLICCONDTION":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellLicenseConditionPage);
                        break;
                    }

                case "WELLLICAREA":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellLicenseAreaPage);
                        break;
                    }

                case "WELLLICREMARK":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellLicenseRemarkPage);
                        break;
                    }

                case "WELLSHOW":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellShowPage);
                        break;
                    }

                case "WELLSHOWREMARK":
                    {
                        WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.WellShowRemarkPage);
                        break;
                    }
            }
        }


        /* TODO ERROR: Skipped EndRegionDirectiveTrivia */

        private void CloseSimpleButton_Click(object sender, EventArgs e)
        {
            if (XtraMessageBox.Show("Are you sure you want to exit ?", "SimpleODM", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                Sharedbo.SimpleODMConfig.WriteDefaults();
                // Sharedbo.SimpleODMConfig.PPDMContext.closeall()
                Close();
            }
        }

        private void MaxSimpleButton_Click(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Maximized)
            {
                this.WindowState = FormWindowState.Normal;
            }
            else
            {
                this.WindowState = FormWindowState.Maximized;
            }
        }

        private void MinSimpleButton_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void HelpSimpleButton_Click(object sender, EventArgs e)
        {
            Help.ShowHelp(this, Sharedbo.SimpleODMConfig.appFolder + @"\sodm_help.chm");
            // Dim pdfv As New frm_showPDF
            // pdfv.Showpdf(Sharedbo.SimpleODMConfig.appFolder & "/simpleodm.pdf")
            // pdfv.ShowDialog(Me)
        }

        private void AboutSimpleButton_Click(object sender, EventArgs e)
        {
            var frmabout = new frm_about();
            // frmabout.Protection = My.Application.Protection
            frmabout.ShowDialog(this);
        }

        private void Uc_tileMenuUI1_Load(object sender, EventArgs e)
        {
        }

        private void LoginSimpleButton_Click(object sender, EventArgs e)
        {
            if (XtraMessageBox.Show("Would you like to Logout from this Database and Login to another one?", "SimpleODM", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                Sharedbo.MyModules.uc_login1.LogoutRoutine();
                WindowsUIView1.Controller.Activate(this.Uc_tileMenuUI1.LoginPage);
            }
        }

        private void PanelControl1_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
        }

        public void RaiseObjectSelected()
        {
            throw new NotImplementedException();
        }

        public void Run(string param1)
        {
            throw new NotImplementedException();
        }

        public void SetConfig(IDMEEditor pbl, IDMLogger plogger, IUtil putil, string[] args, PassedArgs e, IErrorsInfo per)
        {
            InitSharedBusinessClass();
            this.KeyDown += MainMenuWindow8UI_KeyDown;
            this.Load += MainMenuWindow8UI_Load;
            WindowsUIView1 = this.Uc_tileMenuUI1.WindowsUIView1;
            Sharedbo.WindowsUIView1 = WindowsUIView1;
            WindowsUIView1.QueryControl += Sharedbo.WindowsUIView1_QueryControl;
        }
    }
}
