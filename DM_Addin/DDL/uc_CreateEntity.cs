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
        public IPassedArgs Passedarg { get; set; }
   #region "IAddinVisSchema"
        public string RootNodeName { get; set; } = "DDL";
        public string CatgoryName { get; set; }
        public int Order { get; set; } = 2;
        public int ID { get; set; } = 2;
        public string BranchText { get; set; } = "Entity Creator";
        public int Level { get; set; }
        public EnumPointType BranchType { get; set; } = EnumPointType.Entity;
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

        public void SetConfig(IDMEEditor pbl, IDMLogger plogger, IUtil putil, string[] args, IPassedArgs e, IErrorsInfo per)
        {
            Passedarg = e;
            Logger = plogger;
            DMEEditor = pbl;
            ErrorObject = per;
            foreach (ConnectionProperties c in DMEEditor.ConfigEditor.DataConnections.Where(c => c.Category == DatasourceCategory.RDBMS))
            {
                var t = databaseTypeComboBox.Items.Add(c.ConnectionName);

            }
         //   DMEEditor.ConfigEditor.LoadTablesEntities();
            fieldtypeDataGridViewTextBoxColumn.DataSource = dataTypesMapBindingSource;
            //fieldtypeDataGridViewTextBoxColumn.DataSource = DMEEditor.typesHelper.GetNetDataTypes2();
            this.entitiesBindingNavigatorSaveItem.Click += EntitiesBindingNavigatorSaveItem_Click;
            entitiesBindingSource.DataSource = DMEEditor.ConfigEditor.EntityCreateObjects;
            this.entitiesBindingSource.AddingNew += EntitiesBindingSource_AddingNew;
            this.fieldsBindingSource.AddingNew += FieldsBindingSource_AddingNew;
            this.CreateinDBbutton.Click += CreateinDBbutton_Click1;
            this.fieldsDataGridView.DataError += FieldsDataGridView_DataError;
            this.databaseTypeComboBox.SelectedIndexChanged += DatabaseTypeComboBox_SelectedIndexChanged;
           // this.fieldsDataGridView.RowValidated += FieldsDataGridView_RowValidated;
            this.fieldsDataGridView.RowValidating += FieldsDataGridView_RowValidating;
            this.fieldsDataGridView.CellEndEdit += FieldsDataGridView_CellEndEdit;
            // this.databaseTypeComboBox.SelectedIndexChanged += DatabaseTypeComboBox_SelectedIndexChanged;
        }

        private void FieldsDataGridView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            DataGridViewRow row = fieldsDataGridView.Rows[e.RowIndex];
            DataGridViewCell fieldtype = row.Cells[2];
            DataGridViewCell size1 = row.Cells[3];
            DataGridViewCell nperc = row.Cells[4];
            DataGridViewCell nscale = row.Cells[5];
            if (e.ColumnIndex == 2)
            {
                if (fieldtype.Value.ToString().Contains("N"))
                {
                    size1.ReadOnly = false;
                    nperc.ReadOnly = true;
                    nscale.ReadOnly = true;
                }
                if (fieldtype.Value.ToString().Contains("P,S"))
                {
                    size1.ReadOnly = true;
                    nperc.ReadOnly = false;
                    nscale.ReadOnly = false;
                }
            }
        }

        private void FieldsDataGridView_RowValidating(object sender, DataGridViewCellCancelEventArgs e)
        {
            DataGridViewRow row = fieldsDataGridView.Rows[e.RowIndex];
            DataGridViewCell id = row.Cells[0];
            DataGridViewCell fieldname = row.Cells[1];
            DataGridViewCell fieldtype = row.Cells[2];
            DataGridViewCell size1 = row.Cells[3];
            DataGridViewCell nperc = row.Cells[4];
           
            DataGridViewCell nscale = row.Cells[5];
            DataGridViewCell Autoinc = row.Cells[6];
            DataGridViewCell isdbnull = row.Cells[7];
            DataGridViewCell ischeck = row.Cells[8];
            DataGridViewCell isunique = row.Cells[9];
            DataGridViewCell iskey = row.Cells[10];

         //   e.Cancel = !(IsDoc(Docnamecell) && IsGender(Gendercell) && IsAddress(Addresscell) && IsContactno(Contactnocell) && IsDate(Datecell));
        }

       

        private void DatabaseTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(this.databaseTypeComboBox.Text))
            {
                ConnectionProperties connection = DMEEditor.ConfigEditor.DataConnections.Where(o => o.ConnectionName.Equals(this.databaseTypeComboBox.Text, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                ConnectionDriversConfig conf = DMEEditor.Utilfunction.LinkConnection2Drivers(connection);              
                if (conf != null)
                {
                    //   AssemblyClassDefinition def = DMEEditor.ConfigEditor.DataSourcesClasses.Where(u => u.PackageName.Equals(connection.DriverName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                    //if (def != null)
                    //{
                    dataTypesMapBindingSource.DataSource = DMEEditor.ConfigEditor.DataTypesMap.Where(p => p.DataSourceName.Equals(conf.classHandler, StringComparison.OrdinalIgnoreCase)).Distinct();
                    //}
                 
                }
                  
            }
        }

        private void FieldsBindingSource_AddingNew(object sender, AddingNewEventArgs e)
        {
            EntityStructure entityStructure = (EntityStructure)entitiesBindingSource.Current;
            EntityField entityField = new EntityField();
            entityField.EntityName = entityStructure.EntityName;
            
        }

        private void EntitiesBindingSource_AddingNew(object sender, AddingNewEventArgs e)
        {
            EntityStructure entityStructure = new EntityStructure();
            entityStructure.Drawn = false;
            entityStructure.Editable = true;
            e.NewObject = entityStructure;
        }

        private void EntitiesBindingNavigatorSaveItem_Click(object sender, EventArgs e)
        {
            save();


        }
        private void save()
        {
            try
            {
                DMEEditor.ConfigEditor.SaveTablesEntities();
            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", $"Could not Save Entity Creation Script {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
            }

        }
        private void CreateinDBbutton_Click1(object sender, EventArgs e)
        {

            try
            {
                tb = (EntityStructure)entitiesBindingSource.Current;
                SourceConnection = DMEEditor.GetDataSource(databaseTypeComboBox.Text);
                DMEEditor.OpenDataSource(databaseTypeComboBox.Text);
                //SourceConnection.Dataconnection.OpenConnection();
                SourceConnection.ConnectionStatus = SourceConnection.Dataconnection.ConnectionStatus;
                if (SourceConnection.ConnectionStatus == ConnectionState.Open)
                {
                    tb.DatasourceEntityName = tb.EntityName;
                    
                    SourceConnection.CreateEntityAs(tb);
                    if (DMEEditor.ErrorObject.Flag == Errors.Ok)
                    {
                        MessageBox.Show("Entity Creation Success", "Beep");
                        DMEEditor.AddLogMessage("Success", "Table Creation Success", DateTime.Now, -1, "", Errors.Failed);
                    }
                    else
                    {
                        string mes = "Entity Creation Failed";
                        MessageBox.Show(mes, "Beep");
                        DMEEditor.AddLogMessage("Create Table", mes, DateTime.Now, -1, mes, Errors.Failed);
                    }

                }
                else
                {
                    MessageBox.Show("Entity Creation Not Success Could not open Database", "Beep");
                    DMEEditor.AddLogMessage("Fail", "Table Creation Not Success Could not open Database", DateTime.Now, -1, "", Errors.Failed);
                }



            }
            catch (Exception ex)
            {

                string mes = "Entity Creation Failed";
                MessageBox.Show(mes, "Beep");
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };

        }

      
        private void FieldsDataGridView_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.ThrowException = false;
        }




        private void SaveTableConfigbutton_Click(object sender, EventArgs e)
        {
            save();
        }

        private void NewTablebutton_Click(object sender, EventArgs e)
        {
            entitiesBindingSource.AddNew();
        }
    }
}
