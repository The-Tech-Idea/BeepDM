using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheTechIdea.Beep;
using TheTechIdea.Beep.AppBuilder;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.DataView;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Logger;
using TheTechIdea.Util;
using TheTechIdea.Winforms.VIS;

namespace TheTechIdea.ETL
{
    public partial class uc_App : UserControl, IDM_Addin
    {
        public uc_App()
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
        public IPassedArgs Passedarg { get ; set ; }

       // public event EventHandler<PassedArgs> OnObjectSelected;
        //private IDMDataView MyDataView;
        public IVisUtil Visutil { get; set; }
      //  DataViewDataSource ds;
        IBranch RootAppBranch;
        IBranch branch;
      //  App app;

        
        public void RaiseObjectSelected()
        {
            throw new NotImplementedException();
        }

        public void Run(string param1)
        {
            throw new NotImplementedException();
        }

        public void SetConfig(IDMEEditor pbl, IDMLogger plogger, IUtil putil, string[] args, IPassedArgs e, IErrorsInfo per)
        {
            Passedarg = e;
            Logger = plogger;
            ErrorObject = per;
            DMEEditor = pbl;
            Visutil = (IVisUtil)e.Objects.Where(c => c.Name == "VISUTIL").FirstOrDefault().obj;

            branch = (IBranch)e.Objects.Where(c => c.Name == "Branch").FirstOrDefault().obj;
            RootAppBranch = (IBranch)e.Objects.Where(c => c.Name == "RootAppBranch").FirstOrDefault().obj;
         
            dataViewDataSourceNameComboBox.Items.Clear();
            foreach (var item in DMEEditor.ConfigEditor.DataConnections.Where(x => x.Category == DatasourceCategory.VIEWS).ToList())
            {
                dataViewDataSourceNameComboBox.Items.Add(item.ConnectionName);
            }
            this.FolderLocationbutton.Click += FolderLocationbutton_Click;
            this.SaveAppDefinitionbutton.Click += SaveAppDefinitionbutton_Click;
            this.Generatebutton.Click += Generatebutton_Click;
            this.appsBindingSource.DataSource = DMEEditor.ConfigEditor.Apps;
            appsBindingSource.AddingNew += AppsBindingSource_AddingNew;
            if (string.IsNullOrEmpty(e.CurrentEntity))
            {
                appsBindingSource.AddNew();
            }
            else
            {
                appsBindingSource.DataSource= DMEEditor.ConfigEditor.Apps[DMEEditor.ConfigEditor.Apps.FindIndex(x => x.ID == e.ParameterString1)];
                //this.appNameTextBox.Enabled=false;
            }
            if (e.ObjectType == "MISSINGVIEW" && e.EventType=="EDITAPP")
            {
                MessageBox.Show("Missing View, Please update Selected View ");
            }
          
        }

        private void Generatebutton_Click(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void SaveAppDefinitionbutton_Click(object sender, EventArgs e)
        {
            try

            {
                if (string.IsNullOrEmpty(this.ouputFolderTextBox.Text) || string.IsNullOrEmpty(this.appTitleTextBox.Text) || string.IsNullOrEmpty(this.dataViewDataSourceNameComboBox.Text) || string.IsNullOrEmpty(this.appNameTextBox.Text))
                {
                    DMEEditor.AddLogMessage("Fail", $"Please Check All required Fields entered", DateTime.Now, 0, null, Errors.Ok);
                    MessageBox.Show($"Please Check All required Fields entered");
                }
                else
                {
                    appsBindingSource.EndEdit();
                    DMEEditor.ConfigEditor.SaveAppValues();
                    RootAppBranch.CreateChildNodes();
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

        private void AppsBindingSource_AddingNew(object sender, AddingNewEventArgs e)
        {
            App a = new App();
            a.Ver = 1;
            a.ID = Guid.NewGuid().ToString();
            e.NewObject = a;
        }

        private void FolderLocationbutton_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog saveFileDialog1 = new FolderBrowserDialog();
            saveFileDialog1.RootFolder = Environment.SpecialFolder.MyDocuments;
           
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                this.ouputFolderTextBox.Text = saveFileDialog1.SelectedPath;
               // this.FileNametextBox.Text = Path.GetFileName(saveFileDialog1.FileName);
            }
        }
    }
}
