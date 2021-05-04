using DevExpress.Snap;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.Report;
using TheTechIdea.Util;

namespace DXReportBuilder
{
    public class DXSnapReporting : IReportDMWriter
    {
        public DXSnapReporting()
        {
            
        }
        public DXSnapReporting(IDMEEditor pDMEEditor)
        {
            DMEEditor = pDMEEditor;
        }
        public IReportDefinition Definition { get ; set ; }
        public IDMEEditor DMEEditor { get ; set ; }
        public bool Html { get ; set ; }
        public bool Text { get ; set ; }
        public bool Csv { get ; set ; }
        public bool PDF { get ; set ; }
        public bool Excel { get ; set ; }
        public string OutputFile { get; set; }
        public  SnapControl ReportControl { get; set; }
        public  Frm_DxSnapDesigner SnapDesigner { get; set; }
        public Frm_DxDesigner ReportDesigner { get; set; }
        public ReportDataManager reportOutput { get; set; }
        
        public IErrorsInfo RunReport(ReportType reportType, string outputFile)
        {
            ReportControl = new SnapControl();
            SnapDesigner = new Frm_DxSnapDesigner();
            ReportDesigner = new Frm_DxDesigner();
            SnapDesigner.SetConfig(DMEEditor, DMEEditor.Logger, DMEEditor.Utilfunction, null, DMEEditor.Passedarguments, DMEEditor.ErrorObject);
            reportOutput = new ReportDataManager(DMEEditor, Definition);
            if (Definition == null)
            {
                if (DMEEditor.Passedarguments.Objects.Where(c => c.Name == "ReportDefinition").Any())
                {
                    Definition = (IReportDefinition)DMEEditor.Passedarguments.Objects.Where(c => c.Name == "ReportDefinition").FirstOrDefault().obj;
                    
                   
                }
            }

            
            reportOutput = new ReportDataManager(DMEEditor, Definition);
            //SnapDesigner.snapControl1.Document.BeginUpdateDataSource();
            //SnapDesigner.snapControl1.Document.DataSources.Add("Data", reportOutput.GetDataSet());
            //SnapDesigner.snapControl1.Document.EndUpdateDataSource();
            //ReportControl.Document.DataSources.Add("Data",reportOutput.GetDataSet());
            //ReportControl.ShowPrintPreview();
            SnapDesigner.ShowDialog();
            return DMEEditor.ErrorObject;
        }
    }
}
