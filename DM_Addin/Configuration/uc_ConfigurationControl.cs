using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheTechIdea;
using TheTechIdea.Util;
using TheTechIdea.Logger;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Vis;

namespace TheTechIdea.Configuration
{
    public partial class uc_ConfigurationControl : UserControl, IDM_Addin, IAddinVisSchema
    {
        public uc_ConfigurationControl()
        {
            InitializeComponent();
        }

        public string ParentName { get  ; set  ; }
        public string ObjectName { get  ; set  ; }
        public string ObjectType { get; set; } = "UserControl";
        public string AddinName { get; set; } = "Configuration Manager";
        public string Description { get  ; set  ; } = "Configuration Manager";
        public bool DefaultCreate { get; set; } = true;
        public string DllPath { get  ; set  ; }
        public string DllName { get  ; set  ; }
        public string NameSpace { get  ; set  ; }
        public DataSet Dset { get  ; set  ; }
        public IErrorsInfo ErrorObject { get  ; set  ; }
        public IDMLogger Logger { get  ; set  ; }
        public IDMEEditor DMEEditor { get  ; set  ; }
        public EntityStructure EntityStructure { get  ; set  ; }
        public string EntityName { get  ; set  ; }
        public PassedArgs Passedarg { get  ; set  ; }
        #region "IAddinVisSchema"
        public string RootNodeName { get; set; } = "Configuration";
        public string CatgoryName { get; set; }
        public int Order { get; set; } = 7;
        public int ID { get; set; } =1;
        public string BranchText { get; set; } = "Folders";
        public int Level { get; set; }
        public EnumBranchType BranchType { get; set; } = EnumBranchType.Entity;
        public int BranchID { get; set; } = 2;
        public string IconImageName { get; set; } = "folder.ico";
        public string BranchStatus { get; set; }
        public int ParentBranchID { get; set; }
        public string BranchDescription { get; set; } = "";
        public string BranchClass { get; set; } = "ADDIN";
        #endregion "IAddinVisSchema"
       // public event EventHandler<PassedArgs> OnObjectSelected;

        public void Run(string param1)
        {
           
        }

        public void SetConfig(IDMEEditor pbl, IDMLogger plogger, IUtil putil, string[] args, PassedArgs e, IErrorsInfo per)
        {
            Passedarg=  e;
            Logger = plogger;
            ErrorObject = per;
            DMEEditor = pbl;
            foreach (var item in Enum.GetValues(typeof(FolderFileTypes)))
            {
                this.folderFilesTypeDataGridViewTextBoxColumn.Items.Add(item);
            }
            this.foldersBindingSource.DataSource=DMEEditor.ConfigEditor.Config.Folders;
            this.bindingNavigator1.BindingSource = foldersBindingSource;
            addinFoldersDataGridView.DataSource = foldersBindingSource;
            this.Savebutton.Click += Savebutton_Click;

        }

        private void Savebutton_Click(object sender, EventArgs e)
        {
            SaveData();
        }

        private void SavetoolStripButton1_Click(object sender, EventArgs e)
        {
            SaveData();
        }
        private void SaveData()
        {
            ErrorObject.Flag = Errors.Ok;
            try

            {
                //dataConnectionsBindingSource.ResumeBinding();
                foldersBindingSource.EndEdit();

                DMEEditor.ConfigEditor.SaveConfigValues();


                MessageBox.Show("Changes Saved Successfuly", "DB Engine");
            }
            catch (Exception ex)
            {
                string ermsg = "Error Saving Folder paths";
                ErrorObject.Flag = Errors.Failed;
                MessageBox.Show(ermsg, "DB Engine");
                ErrorObject.Message = $"{ermsg}:{ex.Message}";
                Logger.WriteLog($"{ermsg}:{ex.Message}");
            }
        }
    }
}
