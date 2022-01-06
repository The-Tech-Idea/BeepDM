using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.Workflow
{
    public interface IWorkFlowActionClassImplementation
    {
         System.ComponentModel.BackgroundWorker BackgroundWorker { get; set; }
         IDMEEditor DMEEditor { get; set; }
        List<IPassedArgs> InParameters { get; set; }
        List<IPassedArgs> OutParameters { get; set; }
        List<EntityStructure> OutStructures { get; set; }
      //   Mapping_rep Mapping { get; set; }
         bool Finish { get; set; }
         string ClassName { get; set; }
         string FullName { get; set; }
         event EventHandler<IDataWorkFlowEventArgs> WorkFlowStepStarted;
         event EventHandler<IDataWorkFlowEventArgs> WorkFlowStepEnded;
         event EventHandler<IDataWorkFlowEventArgs> WorkFlowStepRunning;
        IErrorsInfo PerformAction();
        IErrorsInfo StopAction();

    }
}
