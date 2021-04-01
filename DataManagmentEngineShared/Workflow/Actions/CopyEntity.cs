using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Workflow;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.Workflow.Actions
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
        public Mapping_rep Mapping { get; set; }
        public List<IPassedArgs> InParameters { get; set; }
        public List<IPassedArgs> OutParameters { get; set; }
        public List<EntityStructure> OutStructures { get; set; }
        public IDataSource Inds { get; set; }
        public IDataSource Outds { get; set; }
      
        public string ClassName { get ; set ; }
        public string FullName { get ; set ; }

        public event EventHandler<IDataWorkFlowEventArgs> WorkFlowStepStarted;
        public event EventHandler<IDataWorkFlowEventArgs> WorkFlowStepEnded;
        public event EventHandler<IDataWorkFlowEventArgs> WorkFlowStepRunning;

        public void OnWorkFlowStepEnded(IDataWorkFlowEventArgs e)
        {
            throw new NotImplementedException();
        }

        public void OnWorkFlowStepRunning(IDataWorkFlowEventArgs e)
        {
            throw new NotImplementedException();
        }

        public void OnWorkFlowStepStarted(IDataWorkFlowEventArgs e)
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
                            string errmsg = "Error In DataSource  does not exists ";
                            ErrorObject.Flag = Errors.Failed;
                            ErrorObject.Message = errmsg;
                            logger.WriteLog(errmsg);
                        }
                        else
                        {
                            Outds = DMEEditor.GetDataSource(OutParameters[0].DatasourceName);
                            if (Outds == null)
                            {
                                string errmsg = "Error Out DataSource does not exists ";
                                ErrorObject.Flag = Errors.Failed;
                                ErrorObject.Message = errmsg;
                                logger.WriteLog(errmsg);
                            }
                            else //---- Everything Checks OK we can Procceed with Copy
                            {
                                string SourceEntityName = InParameters[0].CurrentEntity;
                                if (Outds.CheckEntityExist(SourceEntityName) == false)
                                {
                                    Outds.CreateEntityAs(Inds.GetEntityStructure(SourceEntityName,false));
                                 //   DMEEditor.viewEditor.AddTableToDataView
                                } else
                                {
                                    string errmsg = "Entity already Exist at Destination";
                                    ErrorObject.Flag = Errors.Failed;
                                    ErrorObject.Message = errmsg;
                                    logger.WriteLog(errmsg);
                                }


                            }

                        }
                       


                    }
                    else
                    {
                        string errmsg = "Error No Target Table Data exist ";
                        ErrorObject.Flag = Errors.Failed;
                        ErrorObject.Message = errmsg;
                        logger.WriteLog(errmsg);
                    }

                }
                else
                {
                    string errmsg = "Error No Source Table Data exist ";
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = errmsg;
                    logger.WriteLog(errmsg);
                }
                


            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;

                logger.WriteLog($"Error in Copying Table ({ex.Message})");


            }

            return ErrorObject;
        }
        public IErrorsInfo StopAction()
        {
            return ErrorObject;
        }
    }
}
