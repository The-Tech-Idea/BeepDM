using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.Logger;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheTechIdea.Util;
using TheTechIdea;
using TheTechIdea.DataManagment_Engine.Vis;

namespace TheTechIdea.DDL
{
    public partial class uc_CreateEntity : UserControl, IDM_Addin, IAddinVisSchema
    {
        public uc_CreateEntity()
        {
            InitializeComponent();
        }
        

      

        public string AddinName { get; set; } = "Entity Creator";
        public string Description { get; set; } = "Entity Creator for all DataSource's";
        public string ObjectName { get; set; }
        public string ObjectType { get; set; } = "UserControl";
        public string DllPath { get; set; }
        public string DllName { get; set; }
        public string NameSpace { get; set; }
        public string ParentName { get; set; }
        public Boolean DefaultCreate { get; set; } = true;
        public IDataSource DestConnection { get; set; }
        public IDataSource SourceConnection { get; set; }
        public DataSet Dset { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public IDMLogger Logger { get; set; }
        public IDMEEditor DMEEditor { get; set; }
        public EntityStructure EntityStructure { get; set; }
        public string EntityName { get; set; }
        public PassedArgs Args { get; set; }
   #region "IAddinVisSchema"
        public string RootNodeName { get; set; } = "DDL";
        public string CatgoryName { get; set; }
        public int Order { get; set; } = 2;
        public int ID { get; set; } = 2;
        public string BranchText { get; set; } = "Entity Creator";
        public int Level { get; set; }
        public EnumBranchType BranchType { get; set; } = EnumBranchType.Entity;
        public int BranchID { get; set; } = 2;
        public string IconImageName { get; set; } = "createentity.ico";
        public string BranchStatus { get; set; }
        public int ParentBranchID { get; set; }
        public string BranchDescription { get; set; } = "";
        public string BranchClass { get; set; } = "DDL";
        #endregion "IAddinVisSchema"
        private EntityStructure tb = new EntityStructure();
       // public event EventHandler<PassedArgs> OnObjectSelected;

        public void RaiseObjectSelected()
        {

        }

        public void Run(string param1)
        {

        }

        public void SetConfig(IDMEEditor pbl, IDMLogger plogger, IUtil putil, string[] args, PassedArgs e, IErrorsInfo per)
        {
            Args = e;
            Logger = plogger;
            DMEEditor = pbl;
            ErrorObject = per;
            foreach (ConnectionProperties c in DMEEditor.ConfigEditor.DataConnections.Where(c => c.Category == DatasourceCategory.RDBMS))
            {
                var t = databaseTypeComboBox.Items.Add(c.ConnectionName);

            }


            // DMEEditor.DDLEditor.ReadFieldTypes();

            DMEEditor.ConfigEditor.LoadTablesEntities(); 
            fieldtypeDataGridViewTextBoxColumn.DataSource = DMEEditor.typesHelper.GetNetDataTypes2();
            this.entitiesBindingNavigatorSaveItem.Click += EntitiesBindingNavigatorSaveItem_Click;
            entitiesBindingSource.DataSource = DMEEditor.ConfigEditor.EntityCreateObjects;
            this.CreateinDBbutton.Click += CreateinDBbutton_Click1;
            this.fieldsDataGridView.DataError += FieldsDataGridView_DataError;
            // this.databaseTypeComboBox.SelectedIndexChanged += DatabaseTypeComboBox_SelectedIndexChanged;
        }

        private void EntitiesBindingNavigatorSaveItem_Click(object sender, EventArgs e)
        {
            DMEEditor.ConfigEditor.SaveTablesEntities(DMEEditor.ConfigEditor.EntityCreateObjects);
        }

        private void CreateinDBbutton_Click1(object sender, EventArgs e)
        {

            try
            {
                tb = (EntityStructure)entitiesBindingSource.Current;
                SourceConnection = DMEEditor.GetDataSource(databaseTypeComboBox.Text);
                if (SourceConnection.ConnectionStatus == ConnectionState.Open)
                {
                     SourceConnection.CreateEntityAs( tb);
                    if (DMEEditor.ErrorObject.Flag == Errors.Ok)
                    {
                        MessageBox.Show("Table Creation Success", "DB Engine");
                        DMEEditor.AddLogMessage("Success", "Table Creation Success", DateTime.Now, -1, "", Errors.Failed);
                    }
                    else
                    {
                        string mes = "Table Creation Failed";
                        MessageBox.Show(mes, "DB Engine");
                        DMEEditor.AddLogMessage("Create Table", mes, DateTime.Now, -1, mes, Errors.Failed);
                    }

                }
                else
                {
                    MessageBox.Show("Table Creation Not Success Could not open Database", "DB Engine");
                    DMEEditor.AddLogMessage("Fail", "Table Creation Not Success Could not open Database", DateTime.Now, -1, "", Errors.Failed);
                }



            }
            catch (Exception ex)
            {

                string mes = "Table Creation Failed";
                MessageBox.Show(mes, "DB Engine");
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };

        }

      
        private void FieldsDataGridView_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.ThrowException = false;
        }




        private void SaveTableConfigbutton_Click(object sender, EventArgs e)
        {
            DMEEditor.ConfigEditor.SaveTablesEntities(DMEEditor.ConfigEditor.EntityCreateObjects);
        }

        private void NewTablebutton_Click(object sender, EventArgs e)
        {
            entitiesBindingSource.AddNew();
        }
    }
}
