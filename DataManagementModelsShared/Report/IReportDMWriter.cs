using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.Report
{
  public  interface IReportDMWriter
    {
        IReportDefinition Definition { get; set; }
        IDMEEditor DMEEditor { get; set; }
        bool Html { get; set; }
        bool Text { get; set; }
        bool Csv { get; set; }
        bool PDF { get; set; }
        bool Excel { get; set; }
        string OutputFile { get; set; }
        IErrorsInfo RunReport( ReportType reportType, string outputFile);
      

    }
    public enum ReportType
    {
        html,xls,csv,pdf,txt
    }
    public enum ReportOrientation
    {
        Portrait = 0,
        Landscape = 1
    }
}
