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

namespace TheTechIdea.ETL
{
    public partial class uc_DataEntityStructureViewer : UserControl, IDM_Addin
    {
        public uc_DataEntityStructureViewer()
        {
            InitializeComponent();
        }

        public string ParentName { get ; set ; }
        public string AddinName { get; set; } = "Entity Structure Viewer";
        public string Description { get; set; } = "Entity Structure Viewer";
        public string ObjectName { get; set; }
        public string ObjectType { get; set; } = "UserControl";
        public bool DefaultCreate { get; set; } = true;
        public string DllPath { get ; set ; }
        public string DllName { get ; set ; }
        public string NameSpace { get ; set ; }
        public IErrorsInfo ErrorObject { get ; set ; }
        public IDMLogger Logger { get ; set ; }
        public IDMEEditor DMEEditor { get ; set ; }
        public EntityStructure EntityStructure { get ; set ; }
        public string EntityName { get ; set ; }
        public PassedArgs Passedarg { get ; set ; }
        IBranch branch = null;
        IBranch Parentbranch = null;
        public IVisUtil Visutil { get; set; }
        public IDataSource SourceConnection { get; set; }
        public IDataSource ds { get; set; }
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
            SourceConnection = DMEEditor.GetDataSource(e.DatasourceName);
            Visutil = (IVisUtil)e.Objects.Where(c => c.Name == "VISUTIL").FirstOrDefault().obj;
            branch = (IBranch)e.Objects.Where(c => c.Name == "Branch").FirstOrDefault().obj;
            if (e.Objects.Where(c => c.Name == "ParentBranch").Any())
            {
                Parentbranch = (IBranch)e.Objects.Where(c => c.Name == "ParentBranch").FirstOrDefault().obj;
                //ParentEntity = SourceConnection.GetEntityStructure(Parentbranch.BranchText, true);
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
            this.ValidateFKbutton.Click += ValidateFKbutton_Click;
            this.SaveEntitybutton.Click += SaveEntitybutton_Click;
            this.ValidateFieldsbutton.Click += ValidateFieldsbutton_Click;

            if (e.CurrentEntity != null)
            {
                EntityName = e.CurrentEntity;

            }
            else
            {
                EntityName = "";
            }

           
                EntityStructure = SourceConnection.GetEntityStructure(EntityName, true);
            
           




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
            ds = DMEEditor.GetDataSource(dataSourceIDComboBox.Text);
            if (SourceConnection != null && EntityStructure.CustomBuildQuery != null)
            {

                dt = SourceConnection.RunQuery(EntityStructure.CustomBuildQuery);

            }
            else
            {
                dt = SourceConnection.GetEntity(EntityName, null);

            }

            CustomQueryDatadataGridView.DataSource = dt;
        }

        private void SaveEntitybutton_Click(object sender, EventArgs e)
        {
            //  IConnectionProperties cn;
            try

            {
                EntityStructure.Drawn = true;
                if (SourceConnection.Entities.Where(o => o.EntityName == EntityStructure.EntityName).Any())
                {
                    SourceConnection.Entities[SourceConnection.Entities.FindIndex(i => i.EntityName == EntityStructure.EntityName)] = EntityStructure;
                }
                else
                {
                    SourceConnection.CreateEntityAs(EntityStructure);
                }

                DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new DataManagment_Engine.ConfigUtil.DatasourceEntities { datasourcename = Passedarg.DatasourceName, Entities = SourceConnection.Entities });

                //switch (Passedarg.EventType)
                //{
                //    case "VIEWENTITY":
                //        if (EntityName != null)
                //        {
                //            MyDataView.Entities[ds.ViewReader.EntityListIndex(MyEntity.Id)] = MyEntity;
                //        }
                //        else
                //        {
                //            if (MyDataView.Entities.Where(j => j.EntityName == MyEntity.EntityName).Any())
                //            {
                //                var t = MyDataView.Entities[ds.ViewReader.EntityListIndex(MyEntity.Id)];
                //                t = MyEntity;
                //            }
                //            else
                //            {
                //                MyDataView.Entities.Add(MyEntity);
                //            }
                //        }
                //        ds.Dataview = MyDataView;
                //        break;
                //    case "LAYERENTITY":
                //        IDataSource layer = DMEEditor.GetDataSource(Passedarg.DatasourceName);
                //        // cn = DMEEditor.ConfigEditor.DataConnections[DMEEditor.ConfigEditor.DataConnections.FindIndex(p => p.ConnectionName == Args.DatasourceName)];

                //        layer.Entities[layer.Entities.FindIndex(p => p.EntityName.ToLower() == MyEntity.EntityName.ToLower())] = MyEntity;
                //        break;
                //    case "CLAYERENTITY":
                //        cds.LayerInfo.Entities[cds.LayerInfo.Entities.FindIndex(o => o.EntityName == MyEntity.EntityName)] = MyEntity;
                //        DMEEditor.ConfigEditor.SaveCompositeLayersValues();
                //        break;
                //    case "RDBMSENTITY":
                //        IDataSource dblayer = DMEEditor.GetDataSource(Passedarg.DatasourceName);
                //       // cn = DMEEditor.ConfigEditor.DataConnections[DMEEditor.ConfigEditor.DataConnections.FindIndex(p => p.ConnectionName == Args.DatasourceName)];

                //        dblayer.Entities[dblayer.Entities.FindIndex(p => p.EntityName.ToLower() == MyEntity.EntityName.ToLower())] = MyEntity;
                //        DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new DataManagment_Engine.ConfigUtil.DatasourceEntities { datasourcename = Passedarg.DatasourceName, Entities = dblayer.Entities });
                //        break;

                //    default:
                //        break;
                //}



                MessageBox.Show("Entity Saved successfully", "Beep");

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
            ds = DMEEditor.GetDataSource(dataSourceIDComboBox.Text);
            IRDBSource rdb = null;

            if (ds.Category == DatasourceCategory.RDBMS)
            {
                rdb = (IRDBSource)SourceConnection;
                schemaname = rdb.GetSchemaName();
                EntityStructure.Relations = rdb.GetEntityforeignkeys(EntityName.ToUpper(), schemaname);
                dataHierarchyBindingSource.ResetBindings(false);
            }




        }

        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)

        {
            ds = (IDataSource)DMEEditor.DataSources.Where(f => f.DatasourceName == dataSourceIDComboBox.Items[dataSourceIDComboBox.SelectedIndex].ToString()).FirstOrDefault();
            ConnectionProperties cn = DMEEditor.ConfigEditor.DataConnections.Where(f => f.ConnectionName == dataSourceIDComboBox.Items[dataSourceIDComboBox.SelectedIndex].ToString()).FirstOrDefault();
            if (ds == null)

            {
                ds = DMEEditor.GetDataSource(dataSourceIDComboBox.Items[dataSourceIDComboBox.SelectedIndex].ToString());


                try
                {
                    if (ds != null)
                    {
                       // ds.ConnectionStatus = SourceConnection.Dataconnection.OpenConnection();
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
    }
}
