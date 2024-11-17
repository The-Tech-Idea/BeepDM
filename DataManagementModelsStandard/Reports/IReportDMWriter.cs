using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using TheTechIdea.Beep.AppManager;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Report
{
  public  interface IReportDMWriter
    {
        IAppDefinition Definition { get; set; }
        IDMEEditor DMEEditor { get; set; }
        bool Html { get; set; }
        bool Text { get; set; }
        bool Csv { get; set; }
        bool PDF { get; set; }
        bool Excel { get; set; }
        string OutputFile { get; set; }
        IErrorsInfo RunReport( ReportType reportType, string outputFile);
      

    }
   
}
