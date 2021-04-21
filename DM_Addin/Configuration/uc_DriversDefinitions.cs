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
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Vis;
using TheTechIdea.Logger;
using TheTechIdea.Util;
using TheTechIdea.Winforms.VIS;

namespace TheTechIdea.Configuration
{
    public partial class uc_DriversDefinitions : UserControl,IDM_Addin, IAddinVisSchema
    {
        public uc_DriversDefinitions()
        {
            InitializeComponent();
        }

        public string ParentName { get; set; }
        public string ObjectName { get; set; }
        public string AddinName { get; set; } = "Non ADO Drivers Definitions";
        public string ObjectType { get; set; } = "UserControl";
        public string Description { get; set; } = "Non ADO  Data Sources Drivers Defnitions";
        public bool DefaultCreate { get; set; } = true;
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
        public IVisUtil Visutil { get; set; }
        #region "IAddinVisSchema"
        public string RootNodeName { get; set; } = "Configuration";
        public string CatgoryName { get; set; }
        public int Order { get; set; } = 6;
        public int ID { get; set; } = 6;
        public string BranchText { get; set; } = "Non ADO Drivers Definitions";
        public int Level { get; set; }
        public EnumBranchType BranchType { get; set; } = EnumBranchType.Entity;
        public int BranchID { get; set; } = 3;
        public string IconImageName { get; set; } = "driversdefinition.ico";
        public string BranchStatus { get; set; }
        public int ParentBranchID { get; set; }
        public string BranchDescription { get; set; } = "Non ADO Drivers Definitions";
        public string BranchClass { get; set; } = "ADDIN";
        #endregion "IAddinVisSchema"

       // public event EventHandler<PassedArgs> OnObjectSelected;

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

            List<Icon> icons = new List<Icon>();

            Visutil = (IVisUtil)e.Objects.Where(c => c.Name == "VISUTIL").FirstOrDefault().obj;
            driverDefinitionsBindingSource.DataSource = DMEEditor.ConfigEditor.DriverDefinitionsConfig;
            this.dataDriversDataGridView.DataError += DataDriversDataGridView_DataError;
            this.driverDefinitionsBindingNavigatorSaveItem.Click += DriverDefinitionsBindingNavigatorSaveItem_Click;
        }

        private void DataDriversDataGridView_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.Cancel = true;
        }

        private void DriverDefinitionsBindingNavigatorSaveItem_Click(object sender, EventArgs e)
        {
            SaveData();
        }
        private void SaveData()
        {
            try

            {
                //dataConnectionsBindingSource.ResumeBinding();
                driverDefinitionsBindingSource.MoveFirst();
                driverDefinitionsBindingSource.EndEdit();

                DMEEditor.ConfigEditor.SaveConnectionDriversDefinitions();


                MessageBox.Show("Changes Saved Successfuly", "Beep");
            }
            catch (Exception ex)
            {

                ErrorObject.Flag = Errors.Failed;
                string errmsg = "Error Saving DataSource Drivers Path";
                MessageBox.Show(errmsg, "Beep");
                ErrorObject.Message = $"{errmsg}:{ex.Message}";
                Logger.WriteLog($" {errmsg} :{ex.Message}");
            }
        }
    }
}
