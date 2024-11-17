using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using TheTechIdea.Beep;
using TheTechIdea.Beep.AppManager;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Addin
{
    /// <summary>Interface for managing printing functionality.</summary>
     public interface IPrintManager
    {
        IDMEEditor DMEEditor { get; set; }
        IErrorsInfo PrintTable(DataTable dataTable);
        IErrorsInfo PrintList<T>(List<T> ls);
        IErrorsInfo Print<T>(T obj);
        IErrorsInfo Print(IAppDefinition ReportDef);

    }
}
