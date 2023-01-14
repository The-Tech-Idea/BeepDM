using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using System.Text;
using System.Threading.Tasks;

using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Workflow.Mapping;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.Workflow.Actions
{
    public class CopyEntities : IWorkFlowActionClassImplementation, IWorkFlowAction
    {
        public string Description { get; set; } = "Copy All Entities From one DataSource(InTable Parameters)  to Another (OutTable Parameters)";
        public string Id { get; set; } = "CopyEntities";
        public string ClassName { get ; set ; }
        public string Name { get ; set ; }
        public BackgroundWorker BackgroundWorker { get ; set ; }
        public IDMEEditor DMEEditor { get ; set ; }
        public IErrorsInfo ErrorObject { get; set; }
        public IDMLogger logger { get; set; }
        public EntityDataMap Mapping { get; set; }
        public List<IPassedArgs> InParameters { get ; set ; }
        public List<IPassedArgs> OutParameters { get ; set ; }
        public List<EntityStructure> OutStructures { get ; set ; }
        public IDataSource Inds { get; set; }
        public IDataSource Outds { get; set; }
        public bool IsFinish { get ; set ; }
        public List<string> EntitesNames { get; set; }

        public event EventHandler<IWorkFlowEventArgs> WorkFlowStepStarted;
        public event EventHandler<IWorkFlowEventArgs> WorkFlowStepEnded;
        public event EventHandler<IWorkFlowEventArgs> WorkFlowStepRunning;

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
                        DMEEditor.AddLogMessage("Fail", $"Error No Target Table Data exist  {OutParameters[0].DatasourceName}", DateTime.Now, -1, "", Errors.Failed);
                    }

                }
                else
                {
                    string errmsg = "Error No Source Table Data exist ";
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = errmsg;
                    DMEEditor.AddLogMessage("Fail", $"Error No Source Table Data exist  {InParameters[0].DatasourceName}", DateTime.Now, -1, "", Errors.Failed);
                }



            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error in Copying  {OutParameters[0].DatasourceName} to  {OutParameters[0].DatasourceName}", DateTime.Now, -1, "", Errors.Failed);

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
                IWorkFlowEventArgs passedArgs = new IWorkFlowEventArgs();
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
                IWorkFlowEventArgs passedArgs = new IWorkFlowEventArgs();
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
                IWorkFlowEventArgs passedArgs = new IWorkFlowEventArgs();
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
