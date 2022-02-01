using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Report;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.Addin
{
    public interface IPrintManager
    {
        IDMEEditor DMEEditor { get; set; }
        IErrorsInfo PrintTable(DataTable dataTable);
        IErrorsInfo PrintList<T>(List<T> ls);
        IErrorsInfo Print<T>(T obj);
        IErrorsInfo Print(IReportDefinition ReportDef);

    }
}
