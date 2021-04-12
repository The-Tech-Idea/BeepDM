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
using TheTechIdea.DataManagment_Engine.AppBuilder.DynamicRdlcReport;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Report;
using TheTechIdea.Logger;
using TheTechIdea.Util;
using TheTechIdea.Winforms.VIS;
using TheTechIdea.Winforms.VIS.ReportGenrerator;

namespace TheTechIdea.DataManagment_Engine.AppBuilder.UserControls
{
    public partial class uc_getentities : UserControl,IDM_Addin
    {
        public uc_getentities()
        {
            InitializeComponent();
        }

        public string ParentName { get ; set ; }
        public string ObjectName { get ; set ; }
        public string ObjectType { get ; set ; }
        public string AddinName { get ; set ; }
        public string Description { get ; set ; }
        public bool DefaultCreate { get ; set ; }
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
            ds.Dataconnection.OpenConnection();
            if (ds != null && ds.ConnectionStatus== ConnectionState.Open)
            {
                EntityName = e.CurrentEntity;
                EntityStructure = ds.GetEntityStructure(EntityName, true);
                EntityStructure.Filters = new List<ReportFilter>();
                enttype = ds.GetEntityType(EntityName);
                if (EntityStructure != null)
                {
                    if (EntityStructure.Fields != null)
                    {
                        if (EntityStructure.Fields.Count > 0)
                        {
                            EntityName = EntityStructure.EntityName;
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
        private void DeleteSelectedbutton_Click(object sender, EventArgs e)
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
                                RefreshData();
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
        private void InsertNewEntitybutton_Click(object sender, EventArgs e)
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
        private void GetData()
        {
            if (ds != null && ds.ConnectionStatus == ConnectionState.Open)
            {
                object retval = ds.GetEntity(EntityName, EntityStructure.Filters);
                if (retval != null)
                {
                    if (retval.GetType().FullName == "System.Data.DataTable")
                    {
                        DataList = DMEEditor.Utilfunction.ConvertTableToList((DataTable)retval, EntityStructure, ds.GetEntityType(EntityName));
                    }
                    else
                    {
                        DataList = (List<object>)retval;
                    }

                }
                else
                {
                    DataList = null;
                }

                RefreshData();
            }
        }
        private void RefreshData()
        {
            EntitybindingSource.DataSource = DataList;
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
            GetData();
        }
     
    }
}
