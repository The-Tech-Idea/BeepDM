using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.Logger;

using TheTechIdea.DataManagment_Engine;
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
using TheTechIdea.Winforms.VIS;
using TheTechIdea.DataManagment_Engine.Vis;

namespace TheTechIdea.Configuration
{
    public partial class uc_DataConnection : UserControl ,IDM_Addin, IAddinVisSchema
    {
        public uc_DataConnection()
        {
            InitializeComponent();
        }
        public string AddinName { get; set; } = "RDBMS Data Connection Manager";
        public string Description { get; set; } = "RDBMS Data Connection Manager";
        public string ObjectName { get; set; }
        public string ObjectType { get; set; } = "UserControl";
        public string DllName { get; set; }
        public string DllPath { get; set; }
        public string NameSpace { get; set; }
        public string ParentName { get; set; }
        public Boolean DefaultCreate { get; set; } = true;
        public IRDBSource DestConnection { get; set; }
        public DataSet Dset { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public IDMLogger Logger { get; set; }
        public IRDBSource SourceConnection { get; set; }
        public EntityStructure EntityStructure { get; set; }
        public string EntityName { get; set; }
        public PassedArgs Passedarg { get; set; }
        public IUtil util { get; set; }
        public IVisUtil Visutil { get; set; }
        public IDMEEditor DMEEditor { get; set; }
        #region "IAddinVisSchema"
        public string RootNodeName { get; set; } = "Configuration";
        public string CatgoryName { get; set; }
        public int Order { get; set; } = 1;
        public int ID { get; set; } = 1;
        public string BranchText { get; set; } = "Connection Manager";
        public int Level { get; set; }
        public EnumPointType BranchType { get; set; } = EnumPointType.Entity;
        public int BranchID { get; set; } = 1;
        public string IconImageName { get; set; } = "connections.ico";
        public string BranchStatus { get; set; }
        public int ParentBranchID { get; set; }
        public string BranchDescription { get; set; } = "";
        public string BranchClass { get; set; } = "ADDIN";
        #endregion "IAddinVisSchema"
        string DataSourceCategoryType =DatasourceCategory.RDBMS.ToString();
    
        IBranch branch;
        List<ConnectionProperties> ds { get; set; }= new List<ConnectionProperties>();
       // public event EventHandler<PassedArgs> OnObjectSelected;

        public void RaiseObjectSelected()
        {

        }

        public void Run(string param1)
        {

        }

        public void SetConfig(IDMEEditor pDMEEditor, IDMLogger plogger, IUtil putil, string[] args, PassedArgs obj, IErrorsInfo per)
        {
            Passedarg = obj;
            Visutil = (IVisUtil)obj.Objects.Where(c => c.Name == "VISUTIL").FirstOrDefault().obj;
            Logger = plogger;
            DMEEditor = pDMEEditor;
       //     DataSourceCategoryType = args[0];
            ErrorObject = per;
            if (Visutil.treeEditor != null)
            {
                branch = Visutil.treeEditor.Branches[Visutil.treeEditor.Branches.FindIndex(x => x.BranchClass == "RDBMS" && x.BranchType == EnumPointType.Root)];
            }
            else
                branch = null;
           

            foreach (var item in Enum.GetValues(typeof(DataSourceType)))
            {
                databaseTypeComboBox.Items.Add(item);
            }
            foreach (var item in Enum.GetValues(typeof(DatasourceCategory)))
            {
                CategorycomboBox.Items.Add(item);
                var it=DatasourceCategorycomboBox.Items.Add(item);
                
            }
            foreach (var item in DMEEditor.ConfigEditor.DataDriversClasses)
            {
                try
                {if (!string.IsNullOrEmpty(item.PackageName))
                    {
                        driverNameComboBox.Items.Add(item.PackageName);
                    }
                    
                  
                }
                catch (Exception ex)
                {

                    Logger.WriteLog($"Error for Database connection  :{ex.Message}");
                }
             
            }
            try
            {
                foreach (var item in DMEEditor.ConfigEditor.DataDriversClasses)
                {
                    if (!string.IsNullOrEmpty(item.PackageName))
                    {
                        driverVersionComboBox.Items.Add(item.version);
                    }
                }
            }
            catch (Exception ex)
            {

               
            }
            
            //util.ConfigEditor.LoadDataConnectionsValues();
            dataConnectionsBindingSource.DataSource = null;
            ds =DMEEditor.ConfigEditor.DataConnections.Where(x=>x.Category==DatasourceCategory.RDBMS).ToList();

            //if (ds.FirstOrDefault() == null)
            //{
            //    ConnectionProperties x = new ConnectionProperties();
            //    x.Category = DatasourceCategory.RDBMS;
            //    x.ID = DMEEditor.ConfigEditor.DataConnections.Count + 1;
            //    DMEEditor.ConfigEditor.DataConnections.Add(x);

            //}

            dataConnectionsBindingSource.DataSource = ds;
        //    headersBindingSource.DataSource = ds;
            // dataConnectionsBindingSource.ResetBindings(true);
            dataConnectionsBindingSource.AddingNew += DataConnectionsBindingSource_AddingNew;
            dataConnectionsBindingNavigator.BindingSource = dataConnectionsBindingSource;
            dataConnectionsBindingSource.AllowNew = true;
           // headersBindingSource.AllowNew = true;
          //  entitiesBindingSource.AddingNew += EntitiesBindingSource_AddingNew;
          //  headersBindingSource.AddingNew += HeadersBindingSource_AddingNew;
            this.dataConnectionsBindingNavigatorSaveItem.Click += DataConnectionsBindingNavigatorSaveItem_Click;
            driverNameComboBox.SelectedValueChanged += DriverNameComboBox_SelectedValueChanged;
            DatasourceCategorycomboBox.SelectedValueChanged += DatasourceCategorycomboBox_SelectedValueChanged;
            this.Headesbutton.Click += Headesbutton_Click;
            this.Querybutton.Click += Querybutton_Click;
            

        }

        private void Querybutton_Click(object sender, EventArgs e)
        {
            string[] args = { "New Query Entity", null, null };
            PassedArgs Passedarguments = new PassedArgs
            {
                Addin = null,
                AddinName = null,
                AddinType = "",
                DMView = null,
                CurrentEntity = null,

                ObjectType = "DATACONNECTION",
                DataSource = null,
                ObjectName = this.connectionNameTextBox.Text,

                Objects = null,

                DatasourceName = this.connectionNameTextBox.Text,
                EventType = "WEBAPIQUERY"

            };
            //   ActionNeeded?.Invoke(this, Passedarguments);
            Visutil.ShowUserControlPopUp("uc_webapiQueryParameters", DMEEditor, args, Passedarguments);

        }

        private void Headesbutton_Click(object sender, EventArgs e)
        {
            string[] args = { "New Query Entity", null, null };
            PassedArgs Passedarguments = new PassedArgs
            {
                Addin = null,
                AddinName = null,
                AddinType = "",
                DMView = null,
                CurrentEntity = null,

                ObjectType = "DATACONNECTION",
                DataSource = null,
                ObjectName = this.connectionNameTextBox.Text,
                
                Objects = null,

                DatasourceName = this.connectionNameTextBox.Text,
                EventType = "WEBAPIQUERY"

            };
            //   ActionNeeded?.Invoke(this, Passedarguments);
            Visutil.ShowUserControlPopUp("uc_webapiHeaders", DMEEditor, args, Passedarguments);
        }

        private void DatasourceCategorycomboBox_SelectedValueChanged(object sender, EventArgs e)

        {
            DMEEditor.ConfigEditor.UpdateDataConnection(ds, DataSourceCategoryType);
            DataSourceCategoryType = DatasourceCategorycomboBox.Text;
            ds = DMEEditor.ConfigEditor.DataConnections.Where(x => x.Category.ToString() == DataSourceCategoryType).ToList();
            dataConnectionsBindingSource.DataSource = ds;
            //headersBindingSource.DataSource = ds;
            // dataConnectionsBindingSource.ResetBindings(true);
        }

        private void DriverNameComboBox_SelectedValueChanged(object sender, EventArgs e)
        {
            string pkname = driverNameComboBox.Text;
            driverVersionComboBox.Items.Clear();
            foreach (var item in DMEEditor.ConfigEditor.DataDriversClasses.Where(c => c.PackageName == pkname))
            {
                driverVersionComboBox.Items.Add(item.version);
            }
        }

        private void DataConnectionsBindingSource_AddingNew(object sender, AddingNewEventArgs e)
        {
            ConnectionProperties x = new ConnectionProperties();
            x.Category = DatasourceCategory.RDBMS;
            x.ID = DMEEditor.ConfigEditor.DataConnections.Max(y=>y.ID) + 1;
            e.NewObject = x;
        }

        private void DataConnectionsBindingNavigatorSaveItem_Click(object sender, EventArgs e)
        {
            ErrorObject.Flag = Errors.Ok;
            try

            {
               
                dataConnectionsBindingSource.EndEdit();
               
                ds= (List<ConnectionProperties>)dataConnectionsBindingSource.DataSource;
                DMEEditor.ConfigEditor.UpdateDataConnection(ds,DataSourceCategoryType);
                DMEEditor.ConfigEditor.SaveDataconnectionsValues();
                if (branch != null)
                {
                    branch.CreateChildNodes();

                }
              
                MessageBox.Show("Changes Saved Successfuly", "Beep");
            }
            catch (Exception ex)
            {

                ErrorObject.Flag = Errors.Failed;
                MessageBox.Show("Error Saving Database connection", "Beep");
                ErrorObject.Message = $"Error saving Data for Database connection:{ex.Message}";
                Logger.WriteLog($"Error saving Data for Database connection  :{ex.Message}");
            }
        }

    }
}
