using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheTechIdea.Winforms.VIS;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.Logger;

using TheTechIdea.DataManagment_Engine;
using TheTechIdea.Util;
using TheTechIdea;
using TheTechIdea.DataManagment_Engine.Vis;

namespace TheTechIdea.CRUD
{
    public partial class Uc_DataTableGridEdit : UserControl, IDM_Addin,IWinFormAddin

    {
        public string AddinName { get; set; } = "Data Table Grid Control";
        public string Description { get; set; } = "Data Table Grid Edit";
        public string ObjectName { get; set; } = "Data Table Grid Control";
        public string ObjectType { get; set; } = "UserControl";
        public string DllPath { get; set; }
        public string NameSpace { get; set; }
        public string DllName { get; set; }
        public string ParentName { get; set; }
       
        public Boolean DefaultCreate { get; set; } = false;
        public IDMLogger Logger { get; set; }
        public IDataSource SourceConnection { get; set; }
        public IDMEEditor DMEEditor { get; set; }
        public string EntityName { get; set; }
        public EntityStructure EntityStructure { get; set; }
        public DataSet Dset { get; set; }
        private IDMDataView MyDataView { get; set; } = null;
        public IVisUtil Visutil { get; set; }
        public IUtil util { get; set; }
        public IErrorsInfo ErrorObject  { get; set; }
        public PassedArgs Passedarg { get; set; }
        DataTable t;
        RDBSource rdb;
        public void SetConfig(IDMEEditor pDMEEditor, IDMLogger plogger, IUtil putil, string[] args, PassedArgs obj, IErrorsInfo per)
        {
            Passedarg = obj;
            // SourceConnection = pdataSource;
            Logger = plogger;
            // Visutil = new VisUtil(Logger,putil,per);
            util = putil;
            MyDataView = (DMDataView)Passedarg.DMView;
            DMEEditor = pDMEEditor;
            ErrorObject = per;
            Visutil = (IVisUtil)obj.Objects.Where(c => c.Name == "VISUTIL").FirstOrDefault().obj;
            rdb = (RDBSource)DMEEditor.GetDataSource(Passedarg.DatasourceName);
            switch (obj.ObjectType)
            {
                case "RDBMSTABLE":
                    EntityName = obj.CurrentEntity;
                    SourceConnection = (IRDBSource)Passedarg.DataSource;
                  
                    break;
                case "CRUDENTITY":
                    EntityName = obj.CurrentEntity;
                    SourceConnection = DMEEditor.GetDataSource(Passedarg.DatasourceName);
                    break;
                default:
                    break;
            }



            LoadTable(EntityName);
        }
        public void Run(string param1)
        {
           // LoadView(param1.ToUpper());

        }

        public IErrorsInfo LoadTable(string tablename)
        {
            Logger.WriteLog($"Start Load Table in Form");
            ErrorObject.Flag = Errors.Ok;
            try
            {
                EntityStructure = new EntityStructure(tablename);

                Logger.WriteLog($"Created Table Object");
                EntityStructure = rdb.GetEntityStructure(EntityName,false);
                Logger.WriteLog($"Got Table from Database) : {tablename}");
                bindingNavigator1.BindingSource = bindingSource1;
                t= (DataTable) rdb.GetEntity(tablename, null);
                bindingSource1.DataSource = t;
                DataGridViewEdit.AutoGenerateColumns = true;
                DataGridViewEdit.Columns.Clear();
                Logger.WriteLog($"Reset Grid Columns");
                DataGridViewEdit.DataSource = bindingSource1;
                bindingSource1.ResumeBinding();
                Logger.WriteLog($"Reset Datasource");
                this.SavetoolStripButton.Click+= new System.EventHandler(this.SaveData_Click);
                this.bindingNavigatorAddNewItem.Click += new System.EventHandler(this.AddNewItem_Click);
                // bindingSource1.ResetBindings(true);
                //DataGridViewEdit.Refresh();

                // ShowViewonTree(MyDataView);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
                Logger.WriteLog($"Error in Loading Table in Grid ({ex.Message}) ");

            }


            return ErrorObject;

        }
        private void AddNewItem_Click(object sender, EventArgs e)
        {
            Logger.WriteLog($"Add Record to Grid  ");
        }

        private void SaveData_Click(object sender, EventArgs e)
        {
            try
            {
                this.Validate();
                bindingSource1.EndEdit();
                rdb.UpdateEntities(EntityName, t.GetChanges());
                Logger.WriteLog($"Saved  to Grid  ");
                MessageBox.Show("Data Saved");
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
                Logger.WriteLog($"Error in Loading Table in Grid ({ex.Message}) ");
            }


        }

        public void RaiseObjectSelected()
        {
           
        }

        public Uc_DataTableGridEdit()
        {
            InitializeComponent();
        }
    }
}
