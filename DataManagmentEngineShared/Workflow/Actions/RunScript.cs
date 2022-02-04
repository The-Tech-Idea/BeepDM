using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Workflow.Mapping;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.Workflow.Actions
{
    public class RunScript : IWorkFlowActionClassImplementation, IWorkFlowAction
    {
        public BackgroundWorker BackgroundWorker { get ; set ; }
        public IDMEEditor DMEEditor { get ; set ; }
        public List<IPassedArgs> InParameters { get ; set ; }
        public List<IPassedArgs> OutParameters { get ; set ; }
        public List<EntityStructure> OutStructures { get ; set ; }
        public EntityDataMap Mapping { get ; set ; }
        public bool Finish { get ; set ; }
        public string ClassName { get ; set ; } = "RunScript";
        public string FullName { get ; set ; } = "RunScript";
        public string Description { get; set; } = "Copy Entity From one DataSource(InTable Parameters)  to Another (OutTable Parameters)";
        public IDataSource Outds { get; set; }
        public string Id { get; set; } = "RunScript";

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
                        Outds = DMEEditor.GetDataSource(InParameters[0].DatasourceName);
                        if (Outds == null)
                        {
                            string errmsg = "Error In DataSource exists ";
                         
                            DMEEditor.AddLogMessage("Error", errmsg, DateTime.Now, -1, InParameters[0].DatasourceName, Errors.Failed);
                        }
                        else
                        {
                            //    DMEEditor.ErrorObject = RunCreateEntityBackWorker();
                        }



                    }
                    else
                    {
                        string errmsg = "Error No Target Table Data exist ";
                        DMEEditor.AddLogMessage("Error", errmsg, DateTime.Now, -1, InParameters[0].DatasourceName, Errors.Failed);
                       
                    }

                }
                else
                {
                    string errmsg = "Error No Source Table Data exist ";
                    DMEEditor.AddLogMessage("Error", errmsg, DateTime.Now, -1, InParameters[0].DatasourceName, Errors.Failed);
                   
                }



            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Error", $"Error in Copying Table ({ex.Message})", DateTime.Now, -1, OutParameters[0].DatasourceName, Errors.Failed);
               


            }

            return DMEEditor.ErrorObject;
        }

        public IErrorsInfo StopAction()
        {
            throw new NotImplementedException();
        }
        #region "worker methods"
        private IErrorsInfo RunBackWorker()
        {
            int tot = 0;
            int cur = 0;

            BackgroundWorker = new BackgroundWorker
            {
                WorkerSupportsCancellation = true,
                WorkerReportsProgress = true
            };
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
            BackgroundWorker.DoWork += (sender, e) =>
            {
                IWorkFlowEventArgs passedArgs = new IWorkFlowEventArgs();
                ObjectItem item1 = new ObjectItem();
                item1.obj = e;
                item1.Name = "backgroundworkerstarted";
                passedArgs.Objects.Add(item1);

                WorkFlowStepStarted?.Invoke(this, passedArgs);
                if (Outds.EntitiesNames.Count() == 0)
                {
                    Outds.GetEntitesList();
                }
                tot = Outds.EntitiesNames.Count();
                foreach (string item in Outds.EntitiesNames)
                {
                    cur += 1;
                    string SourceEntityName = item;
                    if (Outds.CheckEntityExist(SourceEntityName) == false)
                    {
                        Outds.CreateEntityAs(Outds.GetEntityStructure(SourceEntityName, false));
                        ((BackgroundWorker)sender).ReportProgress(cur);
                        if (BackgroundWorker.CancellationPending)
                        {
                            break;
                        }
                    }


                }
                e.Result = DMEEditor.ErrorObject;

            };
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

            return DMEEditor.ErrorObject;
        }
        #endregion
    }
}
