using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Workflow.Mapping;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.Workflow.Actions
{
    public class CopyEntity : IWorkFlowActionClassImplementation,IWorkFlowAction

    {
        public System.ComponentModel.BackgroundWorker BackgroundWorker { get; set; }
        public string Description { get; set; } = "Copy Entity From one DataSource(InTable Parameters)  to Another (OutTable Parameters)";
        public string Id { get; set; } = "CopyEntity";
        public string Name { get; set; }="Copy Entity";
        public IErrorsInfo ErrorObject { get; set; }
        public IDMLogger logger { get; set; }
        public bool Finish { get; set; }
        public IDMEEditor DMEEditor { get; set ; }
        public EntityDataMap Mapping { get; set; }
        public List<IPassedArgs> InParameters { get; set; }
        public List<IPassedArgs> OutParameters { get; set; }
        public List<EntityStructure> OutStructures { get; set; }
        public IDataSource Inds { get; set; }
        public IDataSource Outds { get; set; }
      
        public string ClassName { get ; set ; }
        public string FullName { get ; set ; }

        public event EventHandler<IWorkFlowEventArgs> WorkFlowStepStarted;
        public event EventHandler<IWorkFlowEventArgs> WorkFlowStepEnded;
        public event EventHandler<IWorkFlowEventArgs> WorkFlowStepRunning;

        public void OnWorkFlowStepEnded(IWorkFlowEventArgs e)
        {
            throw new NotImplementedException();
        }

        public void OnWorkFlowStepRunning(IWorkFlowEventArgs e)
        {
            throw new NotImplementedException();
        }

        public void OnWorkFlowStepStarted(IWorkFlowEventArgs e)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo PerformAction()
        {
           
            try
            {
                //----------- Chceck if both Source and Target Exist -----
                if (InParameters.Count > 0)
                {

                    if (OutParameters.Count > 0)
                    {
                        Inds = DMEEditor.GetDataSource(InParameters[0].DatasourceName);
                        if (Inds==null)
                        {
                           
                            DMEEditor.AddLogMessage("Fail", $"Error In DataSource does not exists   {InParameters[0].DatasourceName}", DateTime.Now, -1, "", Errors.Failed);
                        }
                        else
                        {
                            Outds = DMEEditor.GetDataSource(OutParameters[0].DatasourceName);
                            if (Outds == null)
                            {

                                DMEEditor.AddLogMessage("Fail", $"Error Out DataSource does not exists   {OutParameters[0].DatasourceName}", DateTime.Now, -1, "", Errors.Failed);
                            }
                            else //---- Everything Checks OK we can Procceed with Copy
                            {
                                string SourceEntityName = InParameters[0].CurrentEntity;
                                if (Outds.CheckEntityExist(SourceEntityName) == false)
                                {
                                    Outds.CreateEntityAs(Inds.GetEntityStructure(SourceEntityName, false));
                                    //   DMEEditor.viewEditor.AddTableToDataView
                                }
                                else
                                { 
                                DMEEditor.AddLogMessage("Fail", $"Entity already Exist at Destination   {SourceEntityName}. in {InParameters[0].DatasourceName}", DateTime.Now, -1, "", Errors.Failed);
                                }


                            }

                        }
                       


                    }
                    else
                    {
                       
                        DMEEditor.AddLogMessage("Fail", $"Error No Target Table Data exist   {OutParameters[0].DatasourceName}", DateTime.Now, -1, "", Errors.Failed);
                    }

                }
                else
                {
                    
                    DMEEditor.AddLogMessage("Fail", $"Error No Source Table Data exist {InParameters[0].CurrentEntity}. in  {InParameters[0].DatasourceName} ", DateTime.Now, -1, "", Errors.Failed);
                }
                


            }
            catch (Exception ex)
            {
                
                

                DMEEditor.AddLogMessage("Fail", $"Error in  Copy {InParameters[0].CurrentEntity}. from   {InParameters[0].DatasourceName} to  {OutParameters[0].DatasourceName}({ex.Message})" , DateTime.Now, -1, "", Errors.Failed);
            }

            return ErrorObject;
        }
        public IErrorsInfo StopAction()
        {
            return ErrorObject;
        }
    }
}
