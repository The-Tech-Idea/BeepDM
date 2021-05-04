using DevExpress.Data.Browsing.Design;
using DevExpress.XtraReports.Native.Data;
using DevExpress.XtraReports.UserDesigner;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Data;
using System.Drawing;
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
    public partial class Frm_DxDesigner : Form, IDM_Addin, IAddinVisSchema
    {
        public Frm_DxDesigner()
        {
            InitializeComponent();
        }

        public string ParentName { get ; set ; }
        public string ObjectName { get ; set ; }
        public string ObjectType { get ; set ; } = "Form";
        public string AddinName { get ; set ; } = "Report Designer";
        public string Description { get ; set ; } = "Report Designer";
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
        #region "IAddinVisSchema"
        public string RootNodeName { get; set; } = "Reporting";
        public string CatgoryName { get; set; }
        public int Order { get; set; } = 1;
        public int ID { get; set; } = 1;
        public string BranchText { get; set; } = "Report Designer";
        public int Level { get; set; }
        public EnumBranchType BranchType { get; set; } = EnumBranchType.Entity;
        public int BranchID { get; set; } = 1;
        public string IconImageName { get; set; } = "reportdesigner.ico";
        public string BranchStatus { get; set; }
        public int ParentBranchID { get; set; }
        public string BranchDescription { get; set; } = "";
        public string BranchClass { get; set; } = "REPORTING";
        #endregion "IAddinVisSchema"
        IVisUtil Visutil;
        public IReportDefinition ReportDefinition { get; set; }
        public ReportDataManager reportOutput { get; set; }
        XRDesignPanel xrDesignPanel1;
        public void Run(string param1)
        {
            throw new NotImplementedException();
        }
        private void BindReportToData(object data)
        {
            if (xrDesignPanel1.Report == null)
                return;
            // Create a data source and bind it to a report.
            xrDesignPanel1.Report.DataSource = data;// CreateDataSource();

            // Update the Field List.
            FieldListDockPanel fieldList =
                (FieldListDockPanel)xrDesignDockManager1[DesignDockPanelType.FieldList];
            IDesignerHost host =
                (IDesignerHost)xrDesignPanel1.GetService(typeof(IDesignerHost));

            // Clear the Data Context cache.
            ((DataContextServiceBase)host.GetService(typeof(IDataContextService))).Dispose();

            fieldList.UpdateDataSource(host);
        }
        public void SetConfig(IDMEEditor pbl, IDMLogger plogger, IUtil putil, string[] args, PassedArgs e, IErrorsInfo per)
        {
            Passedarg = e;
            Logger = plogger;
            ErrorObject = per;
            DMEEditor = pbl;
           
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
                        BindReportToData(reportOutput.GetDataSet());
                        //reportDesigner1..BeginUpdateDataSource();
                        //snapControl1.Document.DataSources.Add("Data", reportOutput.GetDataSet());
                        //snapControl1.Document.EndUpdateDataSource();
                        // snapControl1.ShowPrintPreview();
                    }
                }
            }

        }
    }
}
