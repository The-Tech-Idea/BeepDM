using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.AppBuilder;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.DataView;
using TheTechIdea.DataManagment_Engine.Vis;
using TheTechIdea.Logger;
using TheTechIdea.Util;
using TheTechIdea.Winforms.VIS;

namespace TheTechIdea.ETL
{
    public partial class uc_AppCreateDefinition : UserControl, IDM_Addin
    {
        public uc_AppCreateDefinition()
        {
            InitializeComponent();
        }

        public string ParentName { get; set; }
        public string AddinName { get; set; } = "Applications";
        public string Description { get; set; } = "Applications";
        public string ObjectName { get; set; }
        public string ObjectType { get; set; } = "UserControl";
        public Boolean DefaultCreate { get; set; } = true;
        public string DllPath { get ; set ; }
        public string DllName { get ; set ; }
        public string NameSpace { get ; set ; }
        public DataSet Dset { get ; set ; }
        public IErrorsInfo ErrorObject { get ; set ; }
        public IDMLogger Logger { get ; set ; }
        public IDMEEditor DMEEditor { get ; set ; }
        public EntityStructure EntityStructure { get ; set ; }
        public string EntityName { get ; set ; }
        public PassedArgs Passedarg { get ; set ; }
      //  private IDMDataView MyDataView;
        public IVisUtil Visutil { get; set; }
      //  DataViewDataSource ds;
        IBranch RootAppBranch;
        App app;
        int ver = 0;
        IBranch branch;
       // public event EventHandler<PassedArgs> OnObjectSelected;
        BindingSource GeneratorsBindingSource = new BindingSource();
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
            Passedarg = e;
            Logger = plogger;
            ErrorObject = per;
            DMEEditor = pbl;
            Visutil = (IVisUtil)e.Objects.Where(c => c.Name == "VISUTIL").FirstOrDefault().obj;
            branch = (IBranch)e.Objects.Where(c => c.Name == "Branch").FirstOrDefault().obj;
            RootAppBranch = (IBranch)e.Objects.Where(c => c.Name == "RootAppBranch").FirstOrDefault().obj;
            this.Folderbutton.Click += Folderbutton_Click;
            this.Generatebutton.Click += Generatebutton_Click;
            this.appsBindingSource.DataSource = DMEEditor.ConfigEditor.Apps;
            GeneratorsBindingSource.DataSource = DMEEditor.ConfigEditor.AppWritersClasses;
            apptypeComboBox.Items.Clear();
            appVersionsBindingSource.AddingNew += AppVersionsBindingSource_AddingNew;
            generatorNameComboBox.DisplayMember = "className";
            generatorNameComboBox.ValueMember = "PackageName";
            generatorNameComboBox.DataSource = GeneratorsBindingSource;
            //foreach (AssemblyClassDefinition item in DMEEditor.ConfigEditor.AppWritersClasses)
            //{
            //    this.generatorNameComboBox.Items.Add(item.className);
            //}
            generatorNameComboBox.SelectedValueChanged += GeneratorNameComboBox_SelectedValueChanged;
            appsBindingSource.DataSource = DMEEditor.ConfigEditor.Apps[DMEEditor.ConfigEditor.Apps.FindIndex(x => x.ID == e.ParameterString1)];
            this.appVersionsBindingSource.DataSource = appsBindingSource;
            //     appsBindingSource.ResetBindings(true);
            app = DMEEditor.ConfigEditor.Apps[DMEEditor.ConfigEditor.Apps.FindIndex(x => x.AppName == e.CurrentEntity)];
            if (e.Id==0) // 0 indicate its a new record , other value means id equals record version 
            {
                if (app.AppVersions != null)
                {
                    if( app.AppVersions.Count() == 0)
                    {
                        ver = 1;
                       
                    }
                    else
                    {
                        ver = app.AppVersions.Max(p => p.Ver) + 1;
                    }
                   
                }
                else
                {
                    ver =  1;
                }
              
                this.appVersionsBindingSource.AddNew();
            }
            else
            {
                appVersionsBindingSource.MoveFirst();
                bool found = false;
                foreach (var item in appVersionsBindingSource)
                {
                    AppVersion version = (AppVersion)item;
                    if (version.Ver == e.Id)
                    {
                        found = true;
                        break;

                    }
                   
                   appVersionsBindingSource.MoveNext();
                   
                }
               this.appNameTextBox.Enabled = false;
            }
           
           
        }

        private void GeneratorNameComboBox_SelectedValueChanged(object sender, EventArgs e)
        {
            apptypeComboBox.Items.Clear();
            if (!string.IsNullOrEmpty(generatorNameComboBox.Text))
            {
                string pkname = generatorNameComboBox.SelectedValue.ToString();
                IAppBuilder appBuilder = (IAppBuilder)DMEEditor.Utilfunction.GetInstance(pkname);
                if (appBuilder != null)
                {
                    if (appBuilder.IOS)
                    {
                        apptypeComboBox.Items.Add(AppType.IOS.ToString());
                    }
                    if (appBuilder.Web)
                    {
                        apptypeComboBox.Items.Add(AppType.Web.ToString());
                    }
                    if (appBuilder.Winform)
                    {
                        apptypeComboBox.Items.Add(AppType.Winform.ToString());
                    }
                    if (appBuilder.WPF)
                    {
                        apptypeComboBox.Items.Add(AppType.WPF.ToString());
                    }
                    if (appBuilder.Andriod)
                    {
                        apptypeComboBox.Items.Add(AppType.Andriod.ToString());
                    }
                    apptypeComboBox.SelectedIndex = 0;
                }
            }
        }

        private void Generatebutton_Click(object sender, EventArgs e)
        {
            try

            {
                if (string.IsNullOrEmpty(this.ouputFolderTextBox.Text) || string.IsNullOrEmpty(this.apptypeComboBox.Text)||string.IsNullOrEmpty(this.generatorNameComboBox.Text) )
                {
                    DMEEditor.AddLogMessage("Fail", $"Please Check All required Fields entered", DateTime.Now, 0, null, Errors.Ok);
                    MessageBox.Show($"Please Check All required Fields entered");
                }
                else
                {
                    appVersionsBindingSource.EndEdit();
                    appsBindingSource.EndEdit();
                  
                    DMEEditor.ConfigEditor.SaveAppValues();
                    if (Passedarg.Id == 0)
                    {
                        branch.CreateChildNodes();
                    }
                    else
                    {
                        RootAppBranch.CreateChildNodes();
                    }
                   
                    DMEEditor.AddLogMessage("Success", $"Generated App:{appNameTextBox.Text}", DateTime.Now, 0, null, Errors.Ok);
                    MessageBox.Show($"Generated App:{appNameTextBox.Text}");
                    this.ParentForm.Close();
                }

            }
            catch (Exception ex)
            {
                string errmsg = "Error in Generating App";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                MessageBox.Show($"{errmsg}:{ex.Message}");
            }
        }

        private void AppVersionsBindingSource_AddingNew(object sender, AddingNewEventArgs e)
        {
            AppVersion x = new AppVersion(ver);

            //app = (App) appsBindingSource.Current;
            //app.AppVersions.Add(x);
            x.Ver = ver;
            e.NewObject = x;
        }

        private void Folderbutton_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog saveFileDialog1 = new FolderBrowserDialog();
            saveFileDialog1.RootFolder = Environment.SpecialFolder.MyDocuments;

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                this.ouputFolderTextBox.Text = saveFileDialog1.SelectedPath;
                this.ouputFolderTextBox.SelectAll();
                this.ouputFolderTextBox.Focus();
                // this.FileNametextBox.Text = Path.GetFileName(saveFileDialog1.FileName);
            }
        }

      
    }
}
