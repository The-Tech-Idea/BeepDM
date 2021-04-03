﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;

using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Editor;
using TheTechIdea.DataManagment_Engine.Workflow;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.Workflow.Actions
{
    public class CopyEntities : IWorkFlowActionClassImplementation, IWorkFlowAction
    {
        public string Description { get; set; } = "Copy All Entities From one DataSource(InTable Parameters)  to Another (OutTable Parameters)";
        public string Id { get; set; } = "CopyEntities";
        public string ClassName { get ; set ; }
        public string FullName { get ; set ; }
        public BackgroundWorker BackgroundWorker { get ; set ; }
        public IDMEEditor DMEEditor { get ; set ; }
        public IErrorsInfo ErrorObject { get; set; }
        public IDMLogger logger { get; set; }
        public Mapping_rep Mapping { get; set; }
        public List<IPassedArgs> InParameters { get ; set ; }
        public List<IPassedArgs> OutParameters { get ; set ; }
        public List<EntityStructure> OutStructures { get ; set ; }
        public IDataSource Inds { get; set; }
        public IDataSource Outds { get; set; }
        public bool Finish { get ; set ; }
        public List<string> EntitesNames { get; set; }

        public event EventHandler<IDataWorkFlowEventArgs> WorkFlowStepStarted;
        public event EventHandler<IDataWorkFlowEventArgs> WorkFlowStepEnded;
        public event EventHandler<IDataWorkFlowEventArgs> WorkFlowStepRunning;

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
                        if (Inds == null)
                        {
                            string errmsg = "Error In DataSource does not exists ";
                            ErrorObject.Flag = Errors.Failed;
                            ErrorObject.Message = errmsg;
                            DMEEditor.AddLogMessage("Error", errmsg, DateTime.Now, -1, InParameters[0].DatasourceName, Errors.Failed);
                        }
                        else
                        {
                            Outds = DMEEditor.GetDataSource(OutParameters[0].DatasourceName);
                            if (Outds == null)
                            {
                                string errmsg = "Error Out DataSource does not exists ";
                                ErrorObject.Flag = Errors.Failed;
                                ErrorObject.Message = errmsg;
                                DMEEditor.AddLogMessage("Error", errmsg, DateTime.Now, -1, OutParameters[0].DatasourceName, Errors.Failed);
                            }
                            else //---- Everything Checks OK we can Procceed with Copy
                            {

                                if (InParameters[1] != null)
                                {
                                    if (InParameters[1].Objects != null)
                                    {
                                        List<string> ents = (List<string>)InParameters[1].Objects.Where(c => c.Name == "ENTITIES").FirstOrDefault().obj;
                                        if (ents != null)
                                        {
                                            EntitesNames = ents;
                                        }


                                    }

                                }
                                if (EntitesNames == null || EntitesNames.Count == 0)
                                {
                                    if (Inds.EntitiesNames.Count() == 0)
                                    {
                                        Inds.GetEntitesList();
                                    }
                                    EntitesNames = Inds.EntitiesNames;
                                }


                               // tot = EntitesNames.Count();
                              //  List<LScript> sc = Outds.GetCreateEntityScript(EntitesNames);
                             //   Outds.RunScripts(sc);
                               // ErrorObject =RunCreateEntityBackWorker();

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

        private IErrorsInfo RunCreateEntityBackWorker()
        {
            int tot = 0;
            int cur = 0;

            BackgroundWorker = new BackgroundWorker { WorkerSupportsCancellation = true,
                WorkerReportsProgress = true };
            BackgroundWorker.WorkerReportsProgress = true;
            BackgroundWorker.ProgressChanged += (sender, eventArgs) =>
            {
                IDataWorkFlowEventArgs passedArgs = new IDataWorkFlowEventArgs();
                ObjectItem item = new ObjectItem();
                item.obj = eventArgs;
                item.Name = "backgroundworkerprogress";
                passedArgs.Objects.Add(item);

                WorkFlowStepRunning?.Invoke(this, passedArgs);
                DMEEditor.AddLogMessage("Copy Entity Action ", "Success in Coping entity " + eventArgs.ProgressPercentage + " out of " + tot, DateTime.Now, eventArgs.ProgressPercentage, "", Errors.Ok);

            };
            //---- Do Work here
            BackgroundWorker.DoWork += (sender, e) =>
            {
                IDataWorkFlowEventArgs passedArgs = new IDataWorkFlowEventArgs();
                ObjectItem item1 = new ObjectItem();
                item1.obj = e;
                item1.Name = "backgroundworkerstarted";
                passedArgs.Objects.Add(item1);

                WorkFlowStepStarted?.Invoke(this, passedArgs);
                if (InParameters[1] != null)
                {
                    if (InParameters[1].Objects != null)
                    {
                        List<string> ents = (List<string>)InParameters[1].Objects.Where(c => c.Name == "ENTITIES").FirstOrDefault().obj;
                        if (ents != null)
                        {
                            EntitesNames = ents;
                        }


                    }

                }
                if (EntitesNames == null || EntitesNames.Count==0)
                {
                    if (Inds.EntitiesNames.Count() == 0)
                    {
                        Inds.GetEntitesList();
                    }
                    EntitesNames = Inds.EntitiesNames;
                }
             

                tot = EntitesNames.Count();
           //    List<LScript> sc =Outds.GetCreateEntityScript(EntitesNames);
              //  Outds.RunScripts(sc);
                foreach (string item in EntitesNames)
                {
                    cur += 1;
                    string SourceEntityName = item;
                  
                    //if (Outds.CheckEntityExist(SourceEntityName) == false)
                    //{
                    //    Outds.CreateEntityAs(Inds.GetEntityStructure(SourceEntityName,false));
                    //    ((BackgroundWorker)sender).ReportProgress(cur);
                    //    if (BackgroundWorker.CancellationPending)
                    //    {
                    //        break;
                    //    }
                    //}
                  

                }
                e.Result = ErrorObject;
               
            };
            //----- Worker finish 
            BackgroundWorker.RunWorkerCompleted += (sender, eventArgs) =>
            {
                // do something on the UI thread, like
                // update status or display "result"
                IDataWorkFlowEventArgs passedArgs = new IDataWorkFlowEventArgs();
                ObjectItem item = new ObjectItem();
                item.obj = eventArgs;
                item.Name = "backgroundworkerended";
                passedArgs.Objects.Add(item);

                WorkFlowStepEnded?.Invoke(this, passedArgs);
                DMEEditor.AddLogMessage("End of Copy Entity Action ", "Success in Coping entity " + cur + " out of " + tot, DateTime.Now, tot, "", Errors.Ok);
                DMEEditor.AddLogMessage("Copy Entity Action ", "Success in Coping entity " + cur + " out of " + tot, DateTime.Now, tot, "", Errors.Ok);
            };
           
            BackgroundWorker.RunWorkerAsync();

            return ErrorObject;
        }
        public IErrorsInfo StopAction()
        {
            BackgroundWorker.CancelAsync();
            return ErrorObject;
        }
    }
}