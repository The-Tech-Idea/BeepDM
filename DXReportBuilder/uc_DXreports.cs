using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheTechIdea;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.DataView;
using TheTechIdea.DataManagment_Engine.Report;
using TheTechIdea.DataManagment_Engine.Vis;
using TheTechIdea.Logger;
using TheTechIdea.Util;
using TheTechIdea.Winforms.VIS;
namespace DXReportBuilder
{
    public partial class uc_DXreports : UserControl, IDM_Addin
    {
        public uc_DXreports()
        {
            InitializeComponent();
        }
        public string ParentName { get; set; }
        public string AddinName { get; set; } = "Reports Definition";
        public string Description { get; set; } = "Reports Definition";
        public string ObjectName { get; set; }
        public string ObjectType { get; set; } = "UserControl";
        public Boolean DefaultCreate { get; set; } = true;
        public string DllPath { get; set; }
        public string DllName { get; set; }
        public string NameSpace { get; set; }
        public DataSet Dset { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public IDMLogger Logger { get; set; }
        public IDMEEditor DMEEditor { get; set; }
        public EntityStructure EntityStructure { get; set; }
        public string EntityName { get; set; }
        public PassedArgs Passedarg { get; set; }
        //  private IDMDataView MyDataView;
        public IVisUtil Visutil { get; set; }
        DataViewDataSource ds;
        IBranch RootBranch;
        IBranch branch;
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
            Passedarg = e;
            Logger = plogger;
            ErrorObject = per;
            DMEEditor = pbl;
            Visutil = (IVisUtil)e.Objects.Where(c => c.Name == "VISUTIL").FirstOrDefault().obj;


            branch = (IBranch)e.Objects.Where(c => c.Name == "Branch").FirstOrDefault().obj;
            RootBranch = (IBranch)e.Objects.Where(c => c.Name == "RootReportBranch").FirstOrDefault().obj;
            //this.reportWritersClassesBindingSource.DataSource = DMEEditor.ConfigEditor.ReportWritersClasses;
            this.reportslistBindingSource.DataSource = DMEEditor.ConfigEditor.Reportslist;
            this.Savebutton.Click += Savebutton_Click;

            if (string.IsNullOrEmpty(e.CurrentEntity))
            {
                reportslistBindingSource.AddNew();
                this.reportDefinitionLabel1.Text = e.ObjectName;
                this.reportNameTextBox.Enabled = true;
            }
            else
            {
                reportslistBindingSource.DataSource = DMEEditor.ConfigEditor.Reportslist[DMEEditor.ConfigEditor.Reportslist.FindIndex(x => x.ReportName.Equals(e.CurrentEntity,StringComparison.OrdinalIgnoreCase))];
                this.reportNameTextBox.Enabled = false;
            }
            //foreach (ConnectionProperties item in DMEEditor.ConfigEditor.DataConnections.Where(x => x.Category == DatasourceCategory.VIEWS))
            //{
            //    this.viewIDComboBox.Items.Add(item.ConnectionName);
            //}



        }

        private void Savebutton_Click(object sender, EventArgs e)
        {
            try

            {
                if (string.IsNullOrEmpty(this.reportEngineComboBox.Text) || string.IsNullOrEmpty(this.reportNameTextBox.Text) )
                {
                    DMEEditor.AddLogMessage("Fail", $"Please Check All required Fields entered", DateTime.Now, 0, null, Errors.Ok);
                    MessageBox.Show($"Please Check All required Fields entered");
                }
                else
                {
                  
                   
                    this.reportslistBindingSource.EndEdit();
                    DMEEditor.ConfigEditor.SaveReportsValues();
                    branch.CreateChildNodes();
                    MessageBox.Show($"Generated Report:{reportNameTextBox.Text}");
                    DMEEditor.AddLogMessage("Success", $"Generated Report", DateTime.Now, 0, null, Errors.Ok);
                    this.ParentForm.Close();
                }


            }
            catch (Exception ex)
            {
                string errmsg = "Error in Generating Report";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);

            }
        }
    }
}
