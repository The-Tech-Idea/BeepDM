using DevExpress.Snap;
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
using TheTechIdea.DataManagment_Engine.Report;
using TheTechIdea.DataManagment_Engine.Vis;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace DXReportBuilder
{
    public partial class uc_snapreportviewer : Form, IDM_Addin, IAddinVisSchema
    {
        DxDataSourceInfo sourceInfo = new DxDataSourceInfo();
        public uc_snapreportviewer()
        {
            InitializeComponent();
        }

        public string ParentName { get; set; }
        public string ObjectName { get; set; }
        public string ObjectType { get; set; } = "Form";
        public string AddinName { get; set; } = "Snap Report Previewer";
        public string Description { get; set; } = "Snap Report Previewer";
        public bool DefaultCreate { get; set; } = true;
        public string DllPath { get; set; }
        public string DllName { get; set; }
        public string NameSpace { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public IDMLogger Logger { get; set; }
        public IDMEEditor DMEEditor { get; set; }
        public EntityStructure EntityStructure { get; set; }
        public string EntityName { get; set; }
        public PassedArgs Passedarg { get; set; }
        #region "IAddinVisSchema"
        public string RootNodeName { get; set; } = "Reporting";
        public string CatgoryName { get; set; }
        public int Order { get; set; } = 1;
        public int ID { get; set; } = 1;
        public string BranchText { get; set; } = "Snap Report Previewer";
        public int Level { get; set; }
        public EnumBranchType BranchType { get; set; } = EnumBranchType.Entity;
        public int BranchID { get; set; } = 1;
        public string IconImageName { get; set; } = "snapdesigner.ico";
        public string BranchStatus { get; set; }
        public int ParentBranchID { get; set; }
        public string BranchDescription { get; set; } = "";
        public string BranchClass { get; set; } = "REPORTING";
        #endregion "IAddinVisSchema"
        IVisUtil Visutil;
        public IReportDefinition ReportDefinition { get; set; }
        public ReportDataManager reportOutput { get; set; }
        SnapControl snapControl1 = new SnapControl();
        string reportpath;
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
        //    snapControl1.BeforeExport += SnapControl1_BeforeExport;
            // snapControl1.DocumentLoaded += SnapControl1_DocumentLoaded;

            if (e != null)
            {
                if (e.Objects != null)
                {
                    if (e.Objects.Where(c => c.Name == "VISUTIL").Any())
                    {
                        Visutil = (IVisUtil)e.Objects.Where(c => c.Name == "VISUTIL").FirstOrDefault().obj;
                    }

                    if (e.Objects.Where(c => c.Name == "ReportDefinition").Any())
                    {
                        ReportDefinition = (IReportDefinition)e.Objects.Where(c => c.Name == "ReportDefinition").FirstOrDefault().obj;

                        reportOutput = new ReportDataManager(DMEEditor, ReportDefinition);
                        reportpath = reportOutput.GetReportFilePath(ReportDefinition);
                        if (File.Exists(reportpath + ".snx"))
                        {
                            snapControl1.LoadDocument(reportpath + ".snx");
                        }
                        else
                        {
                            snapControl1.SaveDocument(reportpath + ".snx");
                        }
                        //fileNewItem1.Visibility = DevExpress.XtraBars.BarItemVisibility.Never;
                        //fileOpenItem1.Visibility = DevExpress.XtraBars.BarItemVisibility.Never;
                        //fileSaveAsItem1.Visibility = DevExpress.XtraBars.BarItemVisibility.Never;
                        // fileSaveItem1.Visibility = DevExpress.XtraBars.BarItemVisibility.Never;
                        snapControl1.Document.BeginUpdateDataSource();
                        snapControl1.Document.DataSources.Add("Data", reportOutput.GetDataSet());
                        snapControl1.Document.EndUpdateDataSource();
                        snapControl1.ShowPrintPreview();
                        this.Close();
                    }
                }
            }



        }




        private void SnapControl1_BeforeExport(object sender, DevExpress.XtraRichEdit.BeforeExportEventArgs e)
        {

        }

        private void reportExplorerDockPanel1_Click(object sender, EventArgs e)
        {

        }
    }
}
