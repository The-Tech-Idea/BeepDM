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
        public PassedArgs Passedarg { get; set; }
        public IUtil util { get; set; }
      //  public IDataViewEditor ViewEditor { get; set; }
      
        IBranch branch=null;
        IBranch Parentbranch = null;

       
        public EntityStructure ParentEntity { get; set; } = null;
        DataViewDataSource vds;
        CompositeLayerDataSource cds;
        public void Run(string param1)
        {
           
        }

        public void SetConfig(IDMEEditor pDMEEditor, IDMLogger plogger, IUtil putil, string[] args, PassedArgs obj, IErrorsInfo per)
        {
            Passedarg=  obj;

            Logger = plogger;
            util = putil;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            vds = (DataViewDataSource)DMEEditor.GetDataSource(obj.DMView.DataViewDataSourceID);
            Visutil = (IVisUtil)obj.Objects.Where(c => c.Name == "VISUTIL").FirstOrDefault().obj;
           
            branch = (IBranch)obj.Objects.Where(c => c.Name == "Branch").FirstOrDefault().obj;
           
            if (obj.Objects.Where(c => c.Name == "ParentBranch").Any())
            {
                Parentbranch = (IBranch)obj.Objects.Where(c => c.Name == "ParentBranch").FirstOrDefault().obj;
                ParentEntity = vds.GetEntityStructure(Parentbranch.BranchText,true);
            }
            
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
          //  this.ValidateFKbutton.Click += ValidateFKbutton_Click;
            this.SaveEntitybutton.Click += SaveEntitybutton_Click;
           // this.ValidateFieldsbutton.Click += ValidateFieldsbutton_Click;
         
            if (obj.CurrentEntity != null)
            {
                EntityName = obj.CurrentEntity;

            }else
            {
                EntityName = "";
            }
           
            if (Passedarg.EventType== "NEWENTITY" || Passedarg.EventType == "NEWECHILDNTITY")
            {
                EntityStructure = new EntityStructure();
                EntityStructure.Created = false;
                EntityStructure.Fields = new System.Collections.Generic.List<EntityField>();
                EntityStructure.Id = vds.Entities.Max(p => p.Id) + 1;
                EntityStructure.DataSourceID = vds.DatasourceName;
                if (vds.Category== DatasourceCategory.VIEWS)
                {
                   
                    EntityStructure.ViewID = vds.ViewID;
                    EntityStructure.Viewtype = ViewType.Query;
                    EntityStructure.DatabaseType = DataSourceType.Json;
                    
                }
              
                if (ParentEntity != null)
                {
                    EntityStructure.ParentId = ParentEntity.Id;

                }else
                {
                    EntityStructure.ParentId = -1;
                }
                EntityStructure.Drawn = false;
                this.nameTextBox.Enabled = true;

            }
            else
            {
                this.nameTextBox.Enabled = false;
                EntityStructure = (EntityStructure)obj.Objects.Where(c => c.Name == "EntityStructure").FirstOrDefault().obj;
            }

            this.dataHierarchyBindingSource.ResetBindings(true);
            this.fieldsBindingSource.ResetBindings(true);
            dataHierarchyBindingSource.DataSource = EntityStructure;
       

        }

        private void ValidateFieldsbutton_Click(object sender, EventArgs e)
        {

            if (EntityStructure.Drawn == true)
            {

                SourceConnection = DMEEditor.GetDataSource(dataSourceIDComboBox.Text);
                if (SourceConnection == null)
                {
                    DMEEditor.AddLogMessage("Error", "Could not Find DataSource " + EntityStructure.DataSourceID, DateTime.Now, EntityStructure.Id, EntityStructure.EntityName, Errors.Failed);
                    MessageBox.Show($"{ErrorObject.Message}");
                }
                else
                {
                    EntityStructure ent = SourceConnection.GetEntityStructure(EntityStructure.EntityName, true);
                    EntityStructure.Fields = ent.Fields;
                    this.dataHierarchyBindingSource.ResetBindings(true);
                    this.fieldsBindingSource.ResetBindings(true);
                }
            }
               
              
               
           
        }

        private void ValidateQuerybutton_Click(object sender, EventArgs e)
        {
            // ds = (DataViewDataSource)DMEEditor.GetDataSource(dataSourceIDComboBox.Text);

            object dt;
            if (!string.IsNullOrEmpty(EntityStructure.CustomBuildQuery)&& !string.IsNullOrWhiteSpace(EntityStructure.CustomBuildQuery))
            {
                SourceConnection = DMEEditor.GetDataSource(dataSourceIDComboBox.Text);
                if(SourceConnection.Category!= DatasourceCategory.VIEWS)
                {
                    if (SourceConnection != null && EntityStructure.CustomBuildQuery != null)
                    {

                        dt = SourceConnection.RunQuery(EntityStructure.CustomBuildQuery);

                    }
                    else
                    {
                        dt = SourceConnection.GetEntity(EntityName, null);

                    }
                    if (dt != null)
                    {
                        EntityStructure.Fields = DMEEditor.Utilfunction.GetFieldFromGeneratedObject(dt);
                        this.dataHierarchyBindingSource.ResetBindings(true);
                        this.fieldsBindingSource.ResetBindings(true);
                        fieldsBindingSource.DataSource = dataHierarchyBindingSource;
                        fieldsDataGridView.Refresh();
                        CustomQueryDatadataGridView.DataSource = dt;
                    }
                    
                }
                else
                {
                    MessageBox.Show("Cannot Run Query on DataView");
                }

            }
            else
            {
                MessageBox.Show("Query Missing");
            }
            
        }

        private void SaveEntitybutton_Click(object sender, EventArgs e)
        {
          //  IConnectionProperties cn;
           try

            {
                if (!string.IsNullOrEmpty(this.captionTextBox1.Text) && !string.IsNullOrWhiteSpace(this.captionTextBox1.Text))
                {
                    EntityStructure.Drawn = true;
                    

                    if (vds.Entities.Where(o => o.EntityName == EntityStructure.EntityName).Any())
                    {
                        vds.Entities[vds.Entities.FindIndex(i => i.EntityName.Equals(EntityStructure.EntityName, StringComparison.OrdinalIgnoreCase))] = EntityStructure;
                    }
                    else
                    {
                        vds.CreateEntityAs(EntityStructure);
                    }

                    DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new DataManagment_Engine.ConfigUtil.DatasourceEntities { datasourcename = Passedarg.DatasourceName, Entities = vds.Entities });
                    vds.WriteDataViewFile(vds.DatasourceName);
                    MessageBox.Show("Entity Saved successfully", "Beep");
                }
                else
                    MessageBox.Show("Please Enter Name for Entity", "Beep");


            }
            catch (Exception ex)
            {

                ErrorObject.Flag = Errors.Failed;
                string errmsg = "Error Saving Function Mapping ";
                ErrorObject.Message = $"{errmsg}:{ex.Message}";
                errmsg = ErrorObject.Message;
                MessageBox.Show(errmsg, "Beep");
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
                EntityStructure.Relations = rdb.GetEntityforeignkeys(EntityName.ToUpper(), schemaname);
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
                       // SourceConnection.ConnectionStatus = SourceConnection.Dataconnection.OpenConnection();
                       DMEEditor.OpenDataSource(dataSourceIDComboBox.Items[dataSourceIDComboBox.SelectedIndex].ToString());
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
