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
using TheTechIdea.DataManagment_Engine.Report;
using TheTechIdea.DataManagment_Engine.Vis;
using TheTechIdea.Logger;
using TheTechIdea.Util;
using TheTechIdea.Winforms.VIS;
using TheTechIdea.Winforms.VIS.ReportGenrerator;

namespace TheTechIdea.ETL
{
    public partial class uc_webapiGetQuery : UserControl, IDM_Addin
    {
        public uc_webapiGetQuery()
        {
            InitializeComponent();
        }

        public string ParentName { get; set; }
        public string AddinName { get; set; } = "WebApi Get Data Point";
        public string Description { get; set; } = "WebApi Get Data Point";
        public string ObjectName { get; set; }
        public string ObjectType { get; set; } = "UserControl";
        public Boolean DefaultCreate { get; set; } = true;
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

       // public event EventHandler<PassedArgs> OnObjectSelected;
        public IVisUtil Visutil { get; set; }
        GenReportusingPrintForm dv { get; set; } = new GenReportusingPrintForm();
        IBranch ParentBranch;
        IBranch branch;
        List<EntityField> ls { get; set; } = new List<EntityField>();
        List<string> lsop { get; set; } = new List<string>();
        BindingList<IReportFilter> Retval { get; set; } = new BindingList<IReportFilter>();
        List<string> FieldNames { get; set; } = new List<string>();
        IDataSource webAPIData { get; set; }
        EntityStructure ent { get; set; }
        string CurrentEntity { get; set; }
        BindingSource DataBindingSource { get; set; } = new BindingSource();
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
            Logger = plogger;
            ErrorObject = per;
            DMEEditor = pbl;
            Visutil = (IVisUtil)e.Objects.Where(c => c.Name == "VISUTIL").FirstOrDefault().obj;

            branch = (IBranch)e.Objects.Where(c => c.Name == "Branch").FirstOrDefault().obj;
            ParentBranch = (IBranch)e.Objects.Where(c => c.Name == "ParentBranch").FirstOrDefault().obj;
            webAPIData = DMEEditor.GetDataSource(e.DatasourceName);
            if (webAPIData != null)
            {
                webAPIData.Dataconnection.OpenConnection();
                CurrentEntity = e.CurrentEntity;
                ConnectionProperties cn = DMEEditor.ConfigEditor.DataConnections.Where(p => string.Equals(p.ConnectionName, e.DatasourceName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                ent = webAPIData.Entities.Where(o => string.Equals(o.EntityName, e.CurrentEntity, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                ls = ent.Fields;
                this.dataGridView1.DataSource = DataBindingSource;
                DataGridView grid = dv.CreateGrid();
                if (ent.Filters == null)
                {
                    ent.Filters = new List<ReportFilter>();
                }
                ent.Filters.Clear();
                for (int i = 0; i <= ls.Count - 1; i++)
                {
                    ReportFilter r = new ReportFilter();
                    r.FieldName = ls[i].fieldname;
                    r.Operator = "=";
                    ent.Filters.Add(r);
                    FieldNames.Add(ls[i].fieldname);


                }
                filtersBindingSource.DataSource = ent.Filters;
                if (lsop == null)
                {
                    lsop = new List<string> { "=", ">=", "<=", ">", "<" };
                }
                grid.AutoGenerateColumns = false;
                grid.DataSource = this.filtersBindingSource;
                grid.Columns.Add(dv.CreateComoboBoxColumnForGrid("FieldName", "Column", FieldNames));
                grid.Columns.Add(dv.CreateComoboBoxColumnForGrid("Operator", "Operator", lsop));
                grid.Columns.Add(dv.CreateTextColumnForGrid("FilterValue", "Value"));

                grid.DataError += Grid_DataError;
                //    grid.AllowUserToAddRows = true;

                grid.Left = 5;
                grid.Top = 50;
                grid.Height = 220;
                grid.Width = FilterPanel.Width - 25;
                FilterPanel.Controls.Add(grid);
                grid.Dock = DockStyle.Fill;
            }else
             MessageBox.Show("Error Could not Find WebApi Datasource", "BeepDM");
           
            this.GetDataButton.Click += GetDataButton_Click;
        }

        private void Grid_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.Cancel = true;
        }

        private void GetDataButton_Click(object sender, EventArgs e)

        {
            DMEEditor.Logger.PauseLog();
            string str = ent.CustomBuildQuery;
            //foreach (ReportFilter item in ent.Filters)
            //{
            //    if(string.IsNullOrEmpty(item.FilterValue) || string.IsNullOrWhiteSpace(item.FilterValue))
            //    {
            //        MessageBox.Show("Error, Please Fill all missing Fields");
            //       // throw new InvalidOperationException("Error, Please Fill all missing Fields");
            //    }
            //}
            foreach (EntityParameters item in ent.Paramenters)
            {
                str = str.Replace("{" + item.parameterIndex + "}", ent.Filters.Where(u => u.FieldName == item.parameterName).Select(p => p.FilterValue).FirstOrDefault()) ;
            }
            
            Task<dynamic> output= GetOutputAsync(CurrentEntity, ent.Filters);
            output.Wait();
            DataBindingSource.DataSource = output.Result;
            DataBindingSource.ResetBindings(true);
            dataGridView1.DataSource = null;
            dataGridView1.AutoGenerateColumns = true;
            dataGridView1.Columns.Clear();
            this.dataGridView1.DataSource = DataBindingSource;


            this.dataGridView1.Refresh();
            DMEEditor.Logger.StartLog();
        }
        private async Task<dynamic> GetOutputAsync(string CurrentEntity, List<ReportFilter> filter)
        {
           

            return await webAPIData.GetEntityAsync(CurrentEntity, filter).ConfigureAwait(false);
           

        }

    }
}
