using DynamicRdlcReport;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheTechIdea.DataManagment_Engine.DynamicRdlcReport;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Report;
using TheTechIdea.Logger;
using TheTechIdea.Util;
using TheTechIdea.DataManagment_Engine.Vis;
using System.Reflection;
using System.Collections;

namespace TheTechIdea.DataManagment_Engine.AppBuilder.UserControls
{
    public partial class uc_getentities : UserControl,IDM_Addin
    {
        public uc_getentities()
        {
            InitializeComponent();
        }

        public string ParentName { get ; set ; }
        public string ObjectName { get; set; } = "Entity Data Editor";
        public string ObjectType { get; set; } = "UserControl";
        public string AddinName { get ; set ; } = "Entity Data Editor";
        public string Description { get ; set ; } = "Entity Data Editor";
        public bool DefaultCreate { get; set; } = false;
        public string DllPath { get ; set ; }
        public string DllName { get ; set ; }
        public string NameSpace { get ; set ; }
        public IErrorsInfo ErrorObject { get ; set ; }
        public IDMLogger Logger { get ; set ; }
        public IDMEEditor DMEEditor { get ; set ; }
        public EntityStructure EntityStructure { get ; set ; }
        public string EntityName { get ; set ; }
        public PassedArgs Passedarg { get ; set ; }
        IVisUtil Visutil { get; set; }
        IDataSource ds;
        Type enttype;
        object ob;
       

        List<object> DataList = new List<object>();
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
            Visutil = (IVisUtil)e.Objects.Where(c => c.Name == "VISUTIL").FirstOrDefault().obj;
            ds = DMEEditor.GetDataSource(e.DatasourceName);
            // ds.Dataconnection.OpenConnection();
            DMEEditor.OpenDataSource(e.DatasourceName);
            ds.ConnectionStatus = ds.Dataconnection.ConnectionStatus;
            if (ds != null && ds.ConnectionStatus== ConnectionState.Open)
            {
                EntityName = e.CurrentEntity;

                if (e.Objects.Where(c => c.Name == "EntityStructure").Any())
                {
                    EntityStructure =(EntityStructure) e.Objects.Where(c => c.Name == "EntityStructure").FirstOrDefault().obj;
                }
                else
                {
                    EntityStructure = ds.GetEntityStructure(EntityName, true);
                    e.Objects.Add(new ObjectItem { Name = "EntityStructure", obj = EntityStructure });
                }
                EntityStructure.Filters = new List<ReportFilter>();
            //    enttype = ds.GetEntityType(EntityName);
                if (EntityStructure != null)
                {
                    if (EntityStructure.Fields != null)
                    {
                        if (EntityStructure.Fields.Count > 0)
                        {
                          
                            uc_filtercontrol1.SetConfig(pbl, plogger, putil, args, e, per);

                            // grid = dv.CreateGrid();
                            //Filterpanel.Controls.Add(dv.GridView);
                            //CreateFilterGrid();
                        }
                    }
                }
            }
            SubmitFilterbutton.Click += SubmitFilterbutton_Click;
            expandbutton.Click += Expandbutton_Click;
            InsertNewEntitybutton.Click += InsertNewEntitybutton_Click;
            DeleteSelectedbutton.Click += DeleteSelectedbutton_Click;
            EditSelectedbutton.Click += EditSelectedbutton_Click;
            Printbutton.Click += Printbutton_Click;
           // CreateFilterGrid();


        }

        private void Printbutton_Click(object sender, EventArgs e)
        {
            var f = new ReportForm();
            f.ReportColumns = this.dataGridView1.Columns.Cast<DataGridViewColumn>()
                                  .Select(x => new ReportColumn(x.DataPropertyName)
                                  {
                                      Title = x.HeaderText,
                                      Width = x.Width
                                  }).ToList();
            f.ReportData = this.dataGridView1.DataSource;
            f.ShowDialog();
        }
        #region "CRUD Methods"
        private void EditSelectedbutton_Click(object sender, EventArgs e)
        {
            if(EntityStructure.Viewtype!= ViewType.Table)
            {
                MessageBox.Show("Cannot Edit an Non Table Structure", "BeepDM");

             
            }else
            {
                if (ds != null && ds.ConnectionStatus == ConnectionState.Open)
                {
                    if (dataGridView1.SelectedRows.Count > 0)
                    {
                        ob = EntitybindingSource.Current;
                        if (Passedarg.Objects.Where(i => i.Name == EntityName).Any())
                        {
                            Passedarg.Objects.Remove(Passedarg.Objects.Where(i => i.Name == EntityName).FirstOrDefault());
                        }
                        if (Passedarg.Objects.Where(i => i.Name == "BindingSource").Any())
                        {
                            Passedarg.Objects.Remove(Passedarg.Objects.Where(i => i.Name == "BindingSource").FirstOrDefault());
                        }
                        Passedarg.Objects.Add(new ObjectItem() { Name = EntityName, obj = ob });
                        Passedarg.Objects.Add(new ObjectItem() { Name = "BindingSource", obj = EntitybindingSource });
                        Visutil.ShowUserControlPopUp("uc_updateentity", DMEEditor, new string[] { "" }, Passedarg);

                    }
                }


            }
        }
        private void DeleteSelectedbutton_Click(object sender, EventArgs e)
        {
            if (EntityStructure.Viewtype != ViewType.Table)
            {
                MessageBox.Show("Cannot Edit an Non Table Structure", "BeepDM");


            }
            else
            {
                if (MessageBox.Show(this, "Delete", "Are you sure ? ", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    if (ds != null && ds.ConnectionStatus == ConnectionState.Open)
                    {
                        if (dataGridView1.SelectedRows.Count > 0)
                        {
                            object ob = EntitybindingSource.Current;
                            try
                            {
                                if (ds.DeleteEntity(EntityName, ob).Flag == Errors.Failed)
                                {
                                    EntitybindingSource.RemoveCurrent();
                                    GetData();
                                    MessageBox.Show("Failed to Delete Record");

                                }
                                else
                                {
                                    MessageBox.Show("Success to Delete Record");
                                }
                            }
                            catch (Exception ex)
                            {

                                throw;
                            }

                        }
                    }
                }
            }
        }
        private void InsertNewEntitybutton_Click(object sender, EventArgs e)
        {
            if (EntityStructure.Viewtype != ViewType.Table)
            {
                MessageBox.Show("Cannot Edit an Non Table Structure", "BeepDM");


            }
            else
            {
                if (ds != null && ds.ConnectionStatus == ConnectionState.Open)
                {


                    EntitybindingSource.AddNew();
                    ob = EntitybindingSource.Current;
                    if (Passedarg.Objects.Where(i => i.Name == EntityName).Any())
                    {
                        Passedarg.Objects.Remove(Passedarg.Objects.Where(i => i.Name == EntityName).FirstOrDefault());
                    }
                    if (Passedarg.Objects.Where(i => i.Name == "BindingSource").Any())
                    {
                        Passedarg.Objects.Remove(Passedarg.Objects.Where(i => i.Name == "BindingSource").FirstOrDefault());
                    }
                    Passedarg.Objects.Add(new ObjectItem() { Name = EntityName, obj = ob });
                    Passedarg.Objects.Add(new ObjectItem() { Name = "BindingSource", obj = EntitybindingSource });
                    Visutil.ShowUserControlPopUp("uc_Insertentity", DMEEditor, new string[] { "" }, Passedarg);
                    //  RefreshData();

                }
            }
        }
        private async Task<dynamic> GetOutputAsync(string CurrentEntity, List<ReportFilter> filter)
        {
            return await ds.GetEntityAsync(CurrentEntity, filter).ConfigureAwait(false);
        }
        private void GetFieldForWebApi(dynamic retval)
        {
            Type tp = retval.GetType();
            DataTable dt;
            if (EntityStructure.Fields.Count == 0)
            {
                if (tp.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IList)))
                {
                    dt = DMEEditor.Utilfunction.ToDataTable((IList) retval, DMEEditor.Utilfunction.GetListType(tp));
                }
                else
                {
                     dt = (DataTable)retval;
                  
                }
                foreach (DataColumn item in dt.Columns)
                {
                    EntityField x = new EntityField();
                    try
                    {

                        x.fieldname = item.ColumnName;
                        x.fieldtype = item.DataType.ToString(); //"ColumnSize"
                        x.Size1 = item.MaxLength;
                        try
                        {
                            x.IsAutoIncrement = item.AutoIncrement;
                        }
                        catch (Exception)
                        {

                        }
                        try
                        {
                            x.AllowDBNull = item.AllowDBNull;
                        }
                        catch (Exception)
                        {


                        }



                        try
                        {
                            x.IsUnique = item.Unique;
                        }
                        catch (Exception)
                        {

                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog("Error in Creating Field Type");
                        ErrorObject.Flag = Errors.Failed;
                        ErrorObject.Ex = ex;
                    }

                    if (x.IsKey)
                    {
                        EntityStructure.PrimaryKeys.Add(x);
                    }


                    EntityStructure.Fields.Add(x);
                }




            }
            ds.Entities[ds.Entities.FindIndex(p => p.EntityName == EntityStructure.EntityName)] = EntityStructure;
            DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new ConfigUtil.DatasourceEntities { datasourcename = EntityStructure.DataSourceID, Entities = ds.Entities });
        }
        private void GetData()
        {
            object retval = null ;
           
            if (ds != null && ds.ConnectionStatus == ConnectionState.Open)
            {
                if(ds.Category== DatasourceCategory.WEBAPI)
                {
                    try
                    {
                        Task<dynamic> output = GetOutputAsync(EntityName, EntityStructure.Filters);
                        output.Wait();
                        dynamic t = output.Result;
                        Type tp = t.GetType();
                        if (!tp.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IList)))
                        
                        {
                            retval = DMEEditor.ConfigEditor.JsonLoader.JsonToDataTable(t.ToString());
                        }
                        else
                        {
                            retval = t;
                        }
                      
                       
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error, {ex.Message}");
                    }
                }
                else
                {
                    try
                    {
                        retval = ds.GetEntity(EntityName, EntityStructure.Filters);
                        
                    }
                    catch (Exception ex)
                    {

                        MessageBox.Show($"Error, {ex.Message}");
                    }

                }
                if (retval != null)
                {
                    try
                    {
                        GetFieldForWebApi(retval);
                    }
                    catch (Exception ex)
                    {

                        throw;
                    }
                  
                    //if (retval.GetType().FullName == "System.Data.DataTable")
                    //{
                    //    retval = DMEEditor.Utilfunction.ConvertTableToList((DataTable)retval, EntityStructure, DMEEditor.Utilfunction.GetEntityType(EntityStructure.EntityName,EntityStructure.Fields));
                    //}
                    
                }
                else
                {
                    retval = null;
                }
                RefreshData(retval);
            }
        }
        private void RefreshData(object obj)
        {
            EntitybindingSource.DataSource = obj;
            EntitybindingSource.ResetBindings(true);
            dataGridView1.AutoGenerateColumns = true;
            dataGridView1.DataSource = EntitybindingSource;
            dataGridView1.Refresh();
            EntityNamelabel.Text = EntityName;
            subtitlelabel.Text = $"From Data Source : {ds.DatasourceName}";
        }
        #endregion
        #region "Filter code"
        private string getfilter()
        {
            string str = EntityStructure.CustomBuildQuery;
            foreach (ReportFilter item in EntityStructure.Filters)
            {
                if (!string.IsNullOrEmpty(item.Operator))
                {
                    if (!string.IsNullOrEmpty(item.FilterValue) || string.IsNullOrWhiteSpace(item.FilterValue))
                    {
                        switch (item.valueType)
                        {
                            case "System.string":
                                break;
                            case "System.DateTime":
                                if (item.Operator == "between")
                                {

                                }else
                                {

                                }
                                break;
                            case "System.Boolean":
                                break;
                            case "System.Char":
                                break;
                            case "System.Int16":
                            case "System.Int32":
                            case "System.Int64":
                            case "System.Decimal":
                            case "System.Double":
                            case "System.Single":
                                if (item.Operator == "between")
                                {

                                }
                                else
                                {

                                }
                                break;
                            default:
                                break;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Error, Please Fill all missing filter Value or remove filter condition");
                        throw new InvalidOperationException("Error, Please Fill all missing Fields");
                    }
                }

                if (string.IsNullOrEmpty(item.FilterValue) || string.IsNullOrWhiteSpace(item.FilterValue))
                {
                   
                }
            }
            foreach (EntityParameters item in EntityStructure.Paramenters)
            {
                str = str.Replace("{" + item.parameterIndex + "}", EntityStructure.Filters.Where(u => u.FieldName == item.parameterName).Select(p => p.FilterValue).FirstOrDefault());
            }
            return str;

        }
     
       #endregion

        private void Expandbutton_Click(object sender, EventArgs e)
        {
            splitContainer1.Panel1Collapsed = !splitContainer1.Panel1Collapsed;
        }

        private void SubmitFilterbutton_Click(object sender, EventArgs e)
        {
            DMEEditor.Logger.PauseLog();
            
            GetData();
            DMEEditor.Logger.StartLog();
        }
     
    }
}
