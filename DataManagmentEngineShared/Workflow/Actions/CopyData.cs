
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Logger;
using TheTechIdea.Util;
using TheTechIdea;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.Workflow;
using TheTechIdea.DataManagment_Engine.DataBase;

namespace TheTechIdea.DataManagment_Engine.Workflow.Actions
{
    public class CopyData :IWorkFlowAction, IWorkFlowActionClassImplementation
    {
        public string Id { get; set; }
        public string ClassName { get; set; }
        public string FullName { get; set; }
        public string Description { get; set; } = "Copy Entity From one DataSource(InTable Parameters)  to Another (OutTable Parameters)";
        public BackgroundWorker BackgroundWorker { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public IDMLogger logger { get; set; }
        public IDMEEditor DMEEditor { get; set; }
        public List<IPassedArgs> InParameters { get; set; }
        public List<IPassedArgs> OutParameters { get; set; }
        public List<EntityStructure> OutStructures { get; set; }
        public Mapping_rep Mapping { get; set; }
        public bool Finish { get; set; }
        public IDataSource Inds { get; set; }
        public IDataSource Outds { get; set; }
        public DataTable InData { get; set; }
        public DataTable OutData { get; set; }

        public List<string> EntitesNames { get; set; }
        EntityStructure ent;
        public event EventHandler<IDataWorkFlowEventArgs> WorkFlowStepStarted;
        public event EventHandler<IDataWorkFlowEventArgs> WorkFlowStepEnded;
        public event EventHandler<IDataWorkFlowEventArgs> WorkFlowStepRunning;

      
       public  IErrorsInfo PerformAction()
        {
           return RunCopyData();
        }
      
        private IErrorsInfo RunCopyData()
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
                            DMEEditor.AddLogMessage("Error", " No DataSource exists ", DateTime.Now, -1, "", Errors.Failed);
                          
                        }
                        else
                        {
                            if (DMEEditor.ErrorObject.Flag == Errors.Ok) //  --- Successful in Getting Data
                            {
                                Outds = DMEEditor.GetDataSource(OutParameters[0].DatasourceName);
                                if (Outds == null)
                                {
                                   
                                    DMEEditor.AddLogMessage("Error", "Getting Data From Source  " , DateTime.Now, -1, "", Errors.Failed);
                                  
                                }
                                else //---- Everything Checks OK we can Procceed with Data Loading
                                {
                                   

                                        try
                                        {
                                           RunCopyDataBackWorker();
                                            logger.WriteLog("Data Written Successfully");
                                        }
                                        catch (Exception ex)
                                        {
                                            DMEEditor.AddLogMessage("Error", "Getting Destination Table  " + ex.Message, DateTime.Now, -1, "", Errors.Failed);
                                        }
                                   
                                }
                            }
                        }
                    }
                    else
                    {
                        DMEEditor.AddLogMessage("Error", " No Target Table Data exist ", DateTime.Now, -1, "", Errors.Failed);
                    
                    }
                }
                else
                {
                    DMEEditor.AddLogMessage("Error", " No Source Table Data exist" , DateTime.Now, -1, "", Errors.Failed);
                  
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Error", "Loading Data" + ex.Message, DateTime.Now, -1, "", Errors.Failed);
            }
            return ErrorObject;
        }
        private IErrorsInfo RunCopyDataBackWorker()
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
                IDataWorkFlowEventArgs passedArgs = new IDataWorkFlowEventArgs();
                ObjectItem item = new ObjectItem();
                item.obj = eventArgs;
                item.Name = "backgroundworkerprogress";
                passedArgs.Objects.Add(item);

                WorkFlowStepRunning?.Invoke(this, passedArgs);
                DMEEditor.RaiseEvent(this, passedArgs);
                DMEEditor.AddLogMessage("Copy Data Action ", "Success in Copying Data " + eventArgs.ProgressPercentage + " out of " + tot, DateTime.Now, eventArgs.ProgressPercentage, "", Errors.Ok);

            };

            BackgroundWorker.DoWork += (sender, e) =>
            {
                IDataWorkFlowEventArgs passedArgs = new IDataWorkFlowEventArgs();
                ObjectItem item1 = new ObjectItem();
                item1.obj = e;
                item1.Name = "backgroundworkerstarted";
                passedArgs.Objects.Add(item1);

                WorkFlowStepStarted?.Invoke(this, passedArgs);
                //  OutData = await Inds.GetEntityDataAsTableAsync(OutParameters[0].Parameter, " 1=2 ");

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

                //--------------------------------------------------
                DMEEditor.ErrorObject.Flag = Errors.Ok;


                foreach (string item in EntitesNames)
                {
                    cur += 1;

                    var t = Task.Run<DataTable>(() => { return Inds.RunQuery(item); });
                    t.Wait();
                    InData = t.Result;
                    if (!Outds.CheckEntityExist(item))
                    {
                        Outds.CreateEntityAs(Inds.GetEntityStructure(item, true));
                    }
                    var t1 = Task.Run<IErrorsInfo>(() => { return Outds.UpdateEntity(item, InData); });
                    t1.Wait();
                    DMEEditor.ErrorObject = t1.Result;
                    ((BackgroundWorker)sender).ReportProgress(cur);
                    if (BackgroundWorker.CancellationPending)
                    {
                        break;
                    }
                }

                //--------------------------------------------------
                e.Result = ErrorObject;
            };
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
