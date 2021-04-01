using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.Logger;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.Winforms.VIS;
using TheTechIdea.Util;
using TheTechIdea.DataManagment_Engine.Vis;
using TheTechIdea.DataManagment_Engine.DataView;
using TheTechIdea.DataManagment_Engine.CompositeLayer;

namespace TheTechIdea.ETL
{
   
    public partial class Uc_DataViewEntityEditor : UserControl, IDM_Addin
    {
        public Uc_DataViewEntityEditor()
        {
            InitializeComponent();
        }

        public string AddinName { get; set; } = "Data View Entity Editor";
        public string Description { get; set; } = "Select Entity from View first";
        public string ObjectName { get; set; }
        public string ObjectType { get; set; } = "UserControl";
        public string DllName { get; set; }
        public string DllPath { get; set; }
        public string NameSpace { get; set; }
        public string ParentName { get; set; }
        public Boolean DefaultCreate { get; set; } = true;
        public IDMLogger Logger { get; set; }
        public IDataSource SourceConnection { get; set; }

        public IDMEEditor DMEEditor { get; set; }
        public string EntityName { get; set; }
        public EntityStructure EntityStructure { get; set; }
        public DataSet Dset { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        private IDMDataView MyDataView;
        public IVisUtil Visutil { get; set; }
        public PassedArgs Args { get; set; }
        public IUtil util { get; set; }
      //  public IDataViewEditor ViewEditor { get; set; }
      
        IBranch branch;
        // public ITreeEditor treeeditor { get; set; }
       // public event EventHandler<PassedArgs> OnObjectSelected;
      //  OpenFileDialog openFileDialog1;
        public EntityStructure  MyEntity { get; set; }
        public EntityStructure ParentEntity { get; set; }
        DataViewDataSource ds;
        CompositeLayerDataSource cds;
        public void Run(string param1)
        {
           
        }

        public void SetConfig(IDMEEditor pDMEEditor, IDMLogger plogger, IUtil putil, string[] args, PassedArgs obj, IErrorsInfo per)
        {
            Args=  obj;

            Logger = plogger;
            util = putil;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
         //   ViewEditor = pDMEEditor.viewEditor;
            Visutil = (IVisUtil)obj.Objects.Where(c => c.Name == "VISUTIL").FirstOrDefault().obj;
           
            branch = (IBranch)obj.Objects.Where(c => c.Name == "Branch").FirstOrDefault().obj;
            foreach (ConnectionProperties c in DMEEditor.ConfigEditor.DataConnections)
            {
                var t = dataSourceIDComboBox.Items.Add(c.ConnectionName);

            }
            foreach (var item in Enum.GetValues(typeof(ViewType)))
            {
                viewtypeComboBox.Items.Add(item);
            }

            this.dataSourceIDComboBox.SelectedIndexChanged += ComboBox1_SelectedIndexChanged;
            this.ValidateQuerybutton.Click += ValidateQuerybutton_Click;
            this.ValidateFKbutton.Click += ValidateFKbutton_Click;
            this.SaveEntitybutton.Click += SaveEntitybutton_Click;
            this.ValidateFieldsbutton.Click += ValidateFieldsbutton_Click;
            EntityName = obj.CurrentEntity;
            switch (Args.EventType)
            {
                case "VIEWENTITY":
                    ds = (DataViewDataSource)DMEEditor.GetDataSource(obj.DMView.DataViewDataSourceID);
                    MyDataView = ds.Dataview;
                    MyEntity = MyDataView.Entities[ds.ViewReader.EntityListIndex(EntityName)];

                    break;
                case "LAYERENTITY":
                    SourceConnection = DMEEditor.GetDataSource(obj.DatasourceName);
                    MyEntity = SourceConnection.GetEntityStructure(EntityName, true); 
                    break;
                case "CLAYERENTITY":
                    cds = (CompositeLayerDataSource)obj.Objects.Where(c => c.Name == "Clayer").FirstOrDefault().obj;
                    SourceConnection = DMEEditor.GetDataSource(obj.DatasourceName);
                    MyEntity = cds.LayerInfo.Entities[cds.LayerInfo.Entities.FindIndex(p => p.EntityName == obj.CurrentEntity)];
                    break;
                case "RDBMSENTITY":
                    SourceConnection = DMEEditor.GetDataSource(obj.DatasourceName);
                    MyEntity = SourceConnection.GetEntityStructure(EntityName, true);
                    this.dataSourceIDComboBox.Enabled = false;
                    this.viewtypeComboBox.Enabled = false;
                    break;
                default:
                    break;
            }
            SourceConnection = DMEEditor.GetDataSource(obj.DatasourceName);
          
          
            if (EntityName == null)
          
            {
                MyEntity = new EntityStructure();
                MyEntity.ViewID = MyDataView.ViewID;
              
                MyEntity.Id= MyDataView.Entities.Max(m => m.Id)+1;
                MyEntity.ParentId = obj.Id;

                if (branch.BranchType == EnumBranchType.Entity)
                {

                    ParentEntity = ds.GetEntityStructure(branch.BranchText, false);
                }
                if (branch.BranchType == EnumBranchType.DataPoint)
                {

                }
            }
            this.dataHierarchyBindingSource.ResetBindings(true);
            this.fieldsBindingSource.ResetBindings(true);
            dataHierarchyBindingSource.DataSource = MyEntity;
       

        }

        private void ValidateFieldsbutton_Click(object sender, EventArgs e)
        {

        
                SourceConnection = DMEEditor.GetDataSource(dataSourceIDComboBox.Text);
                if (SourceConnection == null)
                {
                    DMEEditor.AddLogMessage("Error", "Could not Find DataSource " + MyEntity.DataSourceID, DateTime.Now, MyEntity.Id, MyEntity.EntityName, Errors.Failed);
                    MessageBox.Show($"{ErrorObject.Message}");
                } else
                {
                    EntityStructure ent = SourceConnection.GetEntityStructure(MyEntity.EntityName, true);
                    MyEntity.Fields = ent.Fields;
                    this.dataHierarchyBindingSource.ResetBindings(true);
                    this.fieldsBindingSource.ResetBindings(true);
                }
               
           
        }

        private void ValidateQuerybutton_Click(object sender, EventArgs e)
        {
            // ds = (DataViewDataSource)DMEEditor.GetDataSource(dataSourceIDComboBox.Text);

            DataTable dt = new DataTable();
            SourceConnection = DMEEditor.GetDataSource(dataSourceIDComboBox.Text);
            if (SourceConnection != null && MyEntity.CustomBuildQuery != null)
            {

                dt = SourceConnection.RunQuery(MyEntity.CustomBuildQuery);

            }
            else
            {
                dt = SourceConnection.GetEntity(EntityName, "");

            }

            CustomQueryDatadataGridView.DataSource = dt;
        }

        private void SaveEntitybutton_Click(object sender, EventArgs e)
        {
          //  IConnectionProperties cn;
           try

            {
                switch (Args.EventType)
                {
                    case "VIEWENTITY":
                        if (EntityName != null)
                        {
                            MyDataView.Entities[ds.ViewReader.EntityListIndex(MyEntity.Id)] = MyEntity;
                        }
                        else
                        {
                            if (MyDataView.Entities.Where(j => j.EntityName == MyEntity.EntityName).Any())
                            {
                                var t = MyDataView.Entities[ds.ViewReader.EntityListIndex(MyEntity.Id)];
                                t = MyEntity;
                            }
                            else
                            {
                                MyDataView.Entities.Add(MyEntity);
                            }
                        }
                        ds.Dataview = MyDataView;
                        break;
                    case "LAYERENTITY":
                        IDataSource layer = DMEEditor.GetDataSource(Args.DatasourceName);
                        // cn = DMEEditor.ConfigEditor.DataConnections[DMEEditor.ConfigEditor.DataConnections.FindIndex(p => p.ConnectionName == Args.DatasourceName)];

                        layer.Entities[layer.Entities.FindIndex(p => p.EntityName.ToLower() == MyEntity.EntityName.ToLower())] = MyEntity;
                        break;
                    case "CLAYERENTITY":
                        cds.LayerInfo.Entities[cds.LayerInfo.Entities.FindIndex(o => o.EntityName == MyEntity.EntityName)] = MyEntity;
                        DMEEditor.ConfigEditor.SaveCompositeLayersValues();
                        break;
                    case "RDBMSENTITY":
                        IDataSource dblayer = DMEEditor.GetDataSource(Args.DatasourceName);
                       // cn = DMEEditor.ConfigEditor.DataConnections[DMEEditor.ConfigEditor.DataConnections.FindIndex(p => p.ConnectionName == Args.DatasourceName)];

                        dblayer.Entities[dblayer.Entities.FindIndex(p => p.EntityName.ToLower() == MyEntity.EntityName.ToLower())] = MyEntity;
                        DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new DataManagment_Engine.ConfigUtil.DatasourceEntities { datasourcename = Args.DatasourceName, Entities = dblayer.Entities });
                        break;

                    default:
                        break;
                }
               
                
            MessageBox.Show("Function Mapping Saved successfully", "DB Engine");
             
            }
            catch (Exception ex)
            {

                ErrorObject.Flag = Errors.Failed;
                string errmsg = "Error Saving Function Mapping ";
                ErrorObject.Message = $"{errmsg}:{ex.Message}";
                errmsg = ErrorObject.Message;
                MessageBox.Show(errmsg, "DB Engine");
                Logger.WriteLog($" {errmsg} :{ex.Message}");
            }
        }
        private void ValidateFKbutton_Click(object sender, EventArgs e)
        {
            string schemaname = "";
            SourceConnection = DMEEditor.GetDataSource(dataSourceIDComboBox.Text);
            IRDBSource rdb=null;
           
            if (SourceConnection.Category == DatasourceCategory.RDBMS)
            {
                 rdb = (IRDBSource)SourceConnection;
                schemaname = rdb.GetSchemaName();
                MyEntity.Relations = rdb.GetEntityforeignkeys(EntityName.ToUpper(), schemaname);
                dataHierarchyBindingSource.ResetBindings(false);
            }
           

         
      
        }

        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)

        {
            SourceConnection = (IDataSource)DMEEditor.DataSources.Where(f => f.DatasourceName == dataSourceIDComboBox.Items[dataSourceIDComboBox.SelectedIndex].ToString()).FirstOrDefault();
            ConnectionProperties cn = DMEEditor.ConfigEditor.DataConnections.Where(f => f.ConnectionName == dataSourceIDComboBox.Items[dataSourceIDComboBox.SelectedIndex].ToString()).FirstOrDefault();
            if (SourceConnection == null)

            {
                SourceConnection = DMEEditor.GetDataSource(dataSourceIDComboBox.Items[dataSourceIDComboBox.SelectedIndex].ToString());

             
                try
                {
                    if (SourceConnection != null)
                    {
                        SourceConnection.ConnectionStatus = SourceConnection.Dataconnection.OpenConnection();
                        if (ErrorObject.Flag == Errors.Ok)
                        {
                            DMEEditor.DataSources.Add(SourceConnection);
                        }
                        else
                        {
                            MessageBox.Show($"Error in  opening the Database ,{ErrorObject.Message}");
                            Logger.WriteLog($"Error in  opening the Database ,{ErrorObject.Message}");
                        }
                    }
                   

                 
                }
                catch (Exception e1)
                {
                   
                   // Logger.WriteLog($"Error in  opening the Database ,{e1.Message}");
                    MessageBox.Show($"Error in  opening the Database ,{e1.Message}");
                }
               

            }

         
        }
        private void AddNewItem_Click(object sender, EventArgs e)
        {
            Logger.WriteLog($"Add Record to Grid  ");
        }
        public void RaiseObjectSelected()
        {
           
        }
    }
}
