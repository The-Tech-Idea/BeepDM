
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.Workflow
{
    public class WorkFlowStepEditor 
    {
        public WorkFlowStepEditor(IWorkFlowEditor pWorkEdit, IDMEEditor pDME, IWorkFlowStep step)
        {

            DMEEditor = pDME;
            ErrorObject = DMEEditor.ErrorObject;
            logger = DMEEditor.Logger;
            WorkFlowStep = step;
        }

        public IWorkFlowStep WorkFlowStep { get; set; }
        public IWorkFlowEditor WorkEditor { get; set; }

        public IDMEEditor DMEEditor { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public IDMLogger logger { get; set; }
        public List<PassedArgs> InTableParameters { get; set; }
        public List<PassedArgs> OutTableParameters { get; set; }
        public IDataSource Inds { get; set; }
        public IDataSource Outds { get; set; }
        public DataTable InData { get; set; }
        public DataTable OutData { get; set; }
        public IErrorsInfo PerformAction()
        {

            try
            {
                //----------- Chceck if both Source and Target Exist -----
                if (InTableParameters.Count > 0)
                {

                    if (OutTableParameters.Count > 0)
                    {
                        Inds = DMEEditor.GetDataSource(InTableParameters[0].DatasourceName);
                        if (Inds == null)
                        {
                            string errmsg = "Error In DataSource exists ";
                            ErrorObject.Flag = Errors.Failed;
                            ErrorObject.Message = errmsg;
                            logger.WriteLog(errmsg);
                        }
                        else
                        {
                            Outds = DMEEditor.GetDataSource(OutTableParameters[0].DatasourceName);
                            if (Outds == null)
                            {
                                string errmsg = "Error Out DataSource exists ";
                                ErrorObject.Flag = Errors.Failed;
                                ErrorObject.Message = errmsg;
                                logger.WriteLog(errmsg);
                            }
                            else //---- Everything Checks OK we can Procceed with Copy
                            {
                                string SourceEntityName = InTableParameters[0].CurrentEntity;
                                if (Outds.CheckEntityExist(SourceEntityName) == false)
                                {
                                    Outds.CreateEntityAs(Inds.GetEntityStructure(SourceEntityName, false));
                                    //   DMEEditor.viewEditor.AddTableToDataView
                                }
                                else
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
        private IErrorsInfo CheckDataSourcesExistForParameters(List<PassedArgs> p)
        {

            foreach (PassedArgs d in p)
            {
                Inds = DMEEditor.GetDataSource(d.DatasourceName);
                if (Inds == null)
                {
                    DMEEditor.AddLogMessage("Checking DataSource Exist", "DataSource Doesnot Exist", DateTime.Now, -1, d.DatasourceName, ErrorObject.Flag);

                }
            }
            return ErrorObject;
        }
        public IErrorsInfo ValidateStepData()
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                //----------- Chceck if both Source and Target Exist -----
                if (InTableParameters.Count > 0)
                {

                    if (OutTableParameters.Count > 0)
                    {
                        Inds = DMEEditor.GetDataSource(InTableParameters[0].DatasourceName);
                        if (Inds == null)
                        {
                            string errmsg = "Error In DataSource exists ";
                            ErrorObject.Flag = Errors.Failed;
                            ErrorObject.Message = errmsg;
                            logger.WriteLog(errmsg);
                        }
                        else
                        {
                            Outds = DMEEditor.GetDataSource(OutTableParameters[0].DatasourceName);
                            if (Outds == null)
                            {
                                string errmsg = "Error Out DataSource exists ";
                                ErrorObject.Flag = Errors.Failed;
                                ErrorObject.Message = errmsg;
                                logger.WriteLog(errmsg);
                            }
                            else //---- Everything Checks OK we can Procceed with Copy
                            {
                                string SourceEntityName = InTableParameters[0].CurrentEntity;
                                if (Outds.CheckEntityExist(SourceEntityName) == false)
                                {

                                }
                                else
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
        public Boolean FinishTrigger { get; set; }

    }
}
