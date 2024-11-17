using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Workflow
{
    public interface IWorkFlowStepEditor
    {
      
        IWorkFlowEditor WorkEditor { get; set; }

        IDMEEditor DMEEditor { get; set; }
        List<PassedArgs> InTableParameters { get; set; }
        List<PassedArgs> OutTableParameters { get; set; }
        IDataSource Inds { get; set; }
        IDataSource Outds { get; set; }
        DataTable InData { get; set; }
        DataTable OutData { get; set; }
        IErrorsInfo PerformAction();
      
    }
}
