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
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.DataView;
using TheTechIdea.DataManagment_Engine.Report;
using TheTechIdea.DataManagment_Engine.Vis;
using TheTechIdea.Logger;
using TheTechIdea.Util;
using TheTechIdea.Winforms.VIS;

namespace TheTechIdea.ETL
{
    public partial class uc_Report : UserControl, IDM_Addin
    {
        public uc_Report()
        {
            InitializeComponent();
        }

        public string ParentName { get; set; }
        public string AddinName { get; set; } = "Reports";
        public string Description { get; set; } = "Reports";
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
        public IPassedArgs Passedarg { get ; set ; }
      //  private IDMDataView MyDataView;
        public IVisUtil Visutil { get; set; }
        DataViewDataSource ds;
        IBranch RootBranch;
        IBranch branch;

       // public event EventHandler<PassedArgs> OnObjectSelected;

        public void RaiseObjectSelected()
        {
            throw new NotImplementedException();
        }

        public void Run(string param1)
        {
            throw new NotImplementedException();
        }

        public void SetConfig(IDMEEditor pbl, IDMLogger plogger, IUtil putil, string[] args, IPassedArgs e, IErrorsInfo per)
        {
            Passedarg = e;
            Logger = plogger;
            ErrorObject = per;
            DMEEditor = pbl;
            Visutil = (IVisUtil)e.Objects.Where(c => c.Name == "VISUTIL").FirstOrDefault().obj;

            branch = (IBranch)e.Objects.Where(c => c.Name == "Branch").FirstOrDefault().obj;
            RootBranch = (IBranch)e.Objects.Where(c => c.Name == "RootReportBranch").FirstOrDefault().obj;
            this.reportWritersClassesBindingSource.DataSource = DMEEditor.ConfigEditor.ReportWritersClasses;
            this.reportsBindingSource.DataSource = DMEEditor.ConfigEditor.ReportsDefinition;
            this.reportsBindingSource.AddingNew += ReportsBindingSource_AddingNew;
            this.blocksBindingSource.DataSource = reportsBindingSource;
            blockColumnsBindingSource.DataSource = blocksBindingSource;
            this.blocksBindingSource.AddingNew += BlocksBindingSource_AddingNew;
            this.AddBlockbutton.Click += AddBlockbutton_Click;
            if (string.IsNullOrEmpty(e.CurrentEntity))
            {
                reportsBindingSource.AddNew();
                blocksBindingSource.AddNew();
                this.nameTextBox.Enabled =true;
            }
            else
            {
                reportsBindingSource.DataSource = DMEEditor.ConfigEditor.ReportsDefinition[DMEEditor.ConfigEditor.ReportsDefinition.FindIndex(x => x.Name == e.CurrentEntity)];
                this.nameTextBox.Enabled = false;
            }
            foreach (ConnectionProperties item in DMEEditor.ConfigEditor.DataConnections.Where(x => x.Category == DatasourceCategory.VIEWS))
            {
                this.viewIDComboBox.Items.Add(item.ConnectionName);
            }
            this.viewIDComboBox.SelectedValueChanged += ViewIDComboBox_SelectedValueChanged;
            this.Savebutton.Click += Savebutton_Click;
            this.RunReportbutton.Click += RunReportbutton_Click;
            this.blocksBindingSource.CurrentChanged += BlocksBindingSource_CurrentChanged;
            this.packageNameComboBox.SelectedValueChanged += PackageNameComboBox_SelectedValueChanged;
            this.RemoveBlockbutton.Click += RemoveBlockbutton_Click;
            
            //this.blockColumnsDataGridView.CellClick += BlockColumnsDataGridView_CellClick;

            //this.blockColumnsDataGridView.CellContentClick += BlockColumnsDataGridView_CellContentClick;

            #region "Drag and Drop events"
            this.titleTextBox.DragLeave += TitleTextBox_DragLeave;
            this.subTitleTextBox.DragLeave += SubTitleTextBox_DragLeave;
            this.HeaderpictureBox.DragEnter += HeaderpictureBox_DragEnter;
           
            #endregion
        }

        private void RemoveBlockbutton_Click(object sender, EventArgs e)
        {
            if (blocksBindingSource.Count > 0)
            {
                blocksBindingSource.RemoveCurrent();
            }
        }


        #region "Drag Handling Events"
        private void HeaderpictureBox_DragEnter(object sender, DragEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void SubTitleTextBox_DragLeave(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void TitleTextBox_DragLeave(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }
        #endregion


        //private void BlockColumnsDataGridView_CellContentClick(object sender, DataGridViewCellEventArgs e)
        //{
        //    var senderGrid = (DataGridView)sender;

        //    if (senderGrid.Columns[e.ColumnIndex] is DataGridViewButtonColumn &&
        //        e.RowIndex >= 0)
        //    {
        //        //if ((object)column == (object)color)
        //        //{
        //        //    colorDialog.Color = Color.Blue;
        //        //    colorDialog.ShowDialog();
        //        //}
        //    }

        //}

        //private void BlockColumnsDataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        //{
        //    // Ignore clicks that are not in our 
        //    if (e.ColumnIndex == blockColumnsDataGridView.Columns["MyButtonColumn"].Index && e.RowIndex >= 0)
        //    {
        //        Console.WriteLine("Button on row {0} clicked", e.RowIndex);
        //    }
        //}

        private void BlocksBindingSource_CurrentChanged(object sender, EventArgs e)
        {
            ReportBlock x = (ReportBlock)blocksBindingSource.Current;
            if (x.BlockColumns.Count() == 0)
            {
                if (!string.IsNullOrEmpty(this.viewIDComboBox.Text))
                {
                    ds = (DataViewDataSource)DMEEditor.GetDataSource(this.viewIDComboBox.Text);
                    EntityStructure ent = ds.GetEntityStructure(this.entityIDComboBox.Text, true);
                    List<ReportBlockColumns> ls = new List<ReportBlockColumns>();
                    if (ent != null)
                    {
                        int i = 0;
                        foreach (EntityField item in ent.Fields)
                        {
                            ReportBlockColumns c = new ReportBlockColumns();
                            c.ColumnName = item.fieldname;
                            c.ColumnSeq = i;
                            c.DisplayName = item.fieldname;
                            c.Show = true;
                            i += 1;

                            ls.Add(c);
                        }
                        x.BlockColumns = ls;

                    }
                }
                
               
            }
        }
        private void RunReportbutton_Click(object sender, EventArgs e)
        {
            if (!this.nameTextBox.Text.Any(x => Char.IsWhiteSpace(x)))
            {
                string projectData = DMEEditor.ConfigEditor.Config.Folders.Where(h => h.FolderFilesType == FolderFileTypes.ProjectData).FirstOrDefault().FolderPath;
                if (!string.IsNullOrEmpty(this.packageNameComboBox.Text))
                {
                    IReportDMWriter report = (IReportDMWriter)DMEEditor.assemblyHandler.GetInstance(this.packageNameComboBox.SelectedValue.ToString());
                    report.Definition = (IReportDefinition)this.reportsBindingSource.Current;
                    report.DMEEditor = DMEEditor;
                    report.RunReport((ReportType)Enum.Parse(typeof(ReportType), this.ReportOutPutTypecomboBox.Text), Path.Combine(projectData, this.nameTextBox.Text + "." + this.ReportOutPutTypecomboBox.Text));
                    if (DMEEditor.ErrorObject.Flag == Errors.Ok)
                    {
                        if (!string.IsNullOrEmpty(report.OutputFile))
                        {
                            
                            ShowReport(report.OutputFile);
                        }

                    }
                }
            }else
            {
                this.nameTextBox.Text = this.nameTextBox.Text.Replace(" ","");
                MessageBox.Show("Report Name Should not have any spaces");
            }
           
          
        }
        private void ShowReport(string htmlfile)
        {

            List<ObjectItem> ob = new List<ObjectItem>(); ;
            ObjectItem it = new ObjectItem();
            it.obj = this;
            it.Name = "Branch";
            ob.Add(it);
            string[] args = new string[] { htmlfile, null };
            PassedArgs Passedarguments = new PassedArgs
            {
                Addin = null,
                AddinName = null,
                AddinType = "",
                DMView = null,
                CurrentEntity = htmlfile,

                ObjectType = "HTMLREPORT",

                ObjectName = htmlfile,
                Objects = ob,

                EventType = "HTMLREPORT"

            };


            Visutil.ShowUserControlInContainer("uc_Webview", Visutil.DisplayPanel, DMEEditor, args, Passedarguments);
        }
        private void Savebutton_Click(object sender, EventArgs e)
        {
            try

            {
                if (string.IsNullOrEmpty(this.nameTextBox.Text) || string.IsNullOrEmpty(this.titleTextBox.Text) || string.IsNullOrEmpty(this.subTitleTextBox.Text) )
                {
                    DMEEditor.AddLogMessage("Fail", $"Please Check All required Fields entered", DateTime.Now, 0, null, Errors.Ok);
                    MessageBox.Show($"Please Check All required Fields entered");
                }
                else
                {
                    blockColumnsBindingSource.MoveFirst();
                    blocksBindingSource.MoveFirst();
                    reportsBindingSource.MoveFirst();
                    blockColumnsBindingSource.EndEdit();
                    this.blocksBindingSource.EndEdit();
                    this.reportsBindingSource.EndEdit();
                    DMEEditor.ConfigEditor.SaveReportDefinitionsValues();
                    RootBranch.CreateChildNodes();
                    MessageBox.Show($"Generated Report:{nameTextBox.Text}");
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

        private void AddBlockbutton_Click(object sender, EventArgs e)
        {
            blocksBindingSource.AddNew();
        }

        private void BlocksBindingSource_AddingNew(object sender, AddingNewEventArgs e)
        {
            ReportBlock x = new ReportBlock();
            e.NewObject = x;
            ReportTemplate t = (ReportTemplate)reportsBindingSource.Current;
            if (t.Blocks == null)
            {
                t.Blocks = new List<ReportBlock>();
            }
            if (!string.IsNullOrEmpty(this.viewIDComboBox.Text))
            {
                ds = (DataViewDataSource)DMEEditor.GetDataSource(this.viewIDComboBox.Text);
                EntityStructure ent = ds.GetEntityStructure(this.entityIDComboBox.Text, true);
                List<ReportBlockColumns> ls = new List<ReportBlockColumns>();
                if (ent != null)
                {
                    int i = 0;
                    foreach (EntityField item in ent.Fields)
                    {
                        ReportBlockColumns c = new ReportBlockColumns();
                        c.ColumnName = item.fieldname;
                        c.ColumnSeq = i;
                        c.DisplayName = item.fieldname;
                        c.Show = true;
                        i += 1;

                        ls.Add(c);
                    }

                }
                x.BlockColumns = ls;

            }


            t.Blocks.Add(x);
        }

        private void ReportsBindingSource_AddingNew(object sender, AddingNewEventArgs e)
        {
            ReportTemplate x = new ReportTemplate();
            x.Title.Text = null;
            x.SubTitle.Text = null;
            x.Header.Text = null;
            x.Footer.Text = null;
            e.NewObject = x;
        }

        private void ViewIDComboBox_SelectedValueChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(this.viewIDComboBox.Text))
            {
                ds = (DataViewDataSource)DMEEditor.GetDataSource(this.viewIDComboBox.Text);
                if (ds != null)
                {
                    this.entityIDComboBox.Items.Clear();
                    List<string> ls = ds.GetEntitesList().ToList();
                    foreach (string item in ls)
                    {
                        this.entityIDComboBox.Items.Add(item);
                    }
                                       
                }
            }
        }
        private void PackageNameComboBox_SelectedValueChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(this.viewIDComboBox.Text))
            {
                
                if (!string.IsNullOrEmpty(this.packageNameComboBox.Text))
                {
                    this.ReportOutPutTypecomboBox.Items.Clear();

                    foreach (var item in Enum.GetValues(typeof(ReportType)))
                    {
                        this.ReportOutPutTypecomboBox.Items.Add(item);
                    }

                }
            }
        }
    }
}
