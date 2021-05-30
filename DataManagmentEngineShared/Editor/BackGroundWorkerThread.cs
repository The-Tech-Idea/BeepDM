using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.Editor
{
    public class BackgroundWorkerThread
    {

        public event EventHandler<ProgressChangedEventArgs> ReportProgress;
        public event EventHandler<RunWorkerCompletedEventArgs> JobCompleted;

        private BackgroundWorker backgroundWorker1;
        public int percentCompleted { get; set; }
        public PassedArgs args { get; set; }
        DMEEditor DME;
        private int numberToCompute { get; set; } = 0;
        private int highestPercentageReached { get; set; } = 0;
        public IErrorsInfo RunWorker(PassedArgs passedArgs)
        {
            args = passedArgs;
            DME = (DMEEditor)args.Objects.Where(c => c.Name == "DMEEDITOR").FirstOrDefault().obj;
            // Reset the variable for percentage tracking.
            highestPercentageReached = 0;
            args = passedArgs;
            // Start the asynchronous operation.
            backgroundWorker1.RunWorkerAsync(args);
            return DME.ErrorObject;
        }
        public BackgroundWorkerThread(PassedArgs passedArgs)
        {
            backgroundWorker1 = new BackgroundWorker();
            InitializeBackgroundWorker();
            backgroundWorker1.WorkerReportsProgress = true;
            backgroundWorker1.WorkerSupportsCancellation = true;

        }
        private void InitializeBackgroundWorker()
        {
            backgroundWorker1.DoWork +=
                new DoWorkEventHandler(backgroundWorker1_DoWork);
            backgroundWorker1.RunWorkerCompleted +=
                new RunWorkerCompletedEventHandler(
            backgroundWorker1_RunWorkerCompleted);
            backgroundWorker1.ProgressChanged +=
                new ProgressChangedEventHandler(
            backgroundWorker1_ProgressChanged);
        }
        private void cancelAsyncButton_Click(System.Object sender, System.EventArgs e)
        {
            // Cancel the asynchronous operation.
            this.backgroundWorker1.CancelAsync();
        }
        // This event handler is where the actual,
        // potentially time-consuming work is done.
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            // Get the BackgroundWorker that raised this event.
            BackgroundWorker worker = sender as BackgroundWorker;

            // Assign the result of the computation
            // to the Result property of the DoWorkEventArgs
            // object. This is will be available to the 
            // RunWorkerCompleted eventhandler.
            e.Result = RunScript((PassedArgs)e.Argument, worker, e);
            // backgroundWorker1.ReportProgress
        }
        // This event handler deals with the results of the
        // background operation.
        public void RequestCancel()
        {
            backgroundWorker1.CancelAsync();
        }
        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // First, handle the case where an exception was thrown.
            if (e.Error != null)
            {
                //  MessageBox.Show(e.Error.Message);
            }
            else if (e.Cancelled)
            {
                // Next, handle the case where the user canceled 
                // the operation.
                // Note that due to a race condition in 
                // the DoWork event handler, the Cancelled
                // flag may not have been set, even though
                //  CancelAsync was called.
                // resultLabel.Text = "Canceled";
            }
            else
            {
                // Finally, handle the case where the operation 
                // succeeded.
                // resultLabel.Text = e.Result.ToString();
            }
            JobCompleted?.Invoke(sender, e);
            // Enable the UpDown control.

        }
        // This event handler updates the progress bar.
        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            percentCompleted = e.ProgressPercentage;
            ReportProgress?.Invoke(sender, e);
        }
        private IErrorsInfo RunScript(PassedArgs args, BackgroundWorker worker, DoWorkEventArgs e)
        {
            #region "Update Data code "
            IDMEEditor dMEEditor = DME;
          
            int CurrentRecord = 0;
            int highestPercentageReached = 0;
            int numberToCompute = 0;

            IDataSource destds = null;
            IDataSource srcds = null;

            dMEEditor.ETL.trackingHeader = new LScriptTrackHeader();
            dMEEditor.ETL.trackingHeader.parentscriptHeaderid = dMEEditor.ETL.script.id;
            dMEEditor.ETL.trackingHeader.rundate = DateTime.Now;
            numberToCompute = dMEEditor.ETL.script.Scripts.Count();
            List<LScript> crls = dMEEditor.ETL.script.Scripts.Where(i => i.scriptType == DDLScriptType.CreateTable).ToList();
            List<LScript> copudatals = dMEEditor.ETL.script.Scripts.Where(i => i.scriptType == DDLScriptType.CopyData).ToList();
            List<LScript> AlterForls = dMEEditor.ETL.script.Scripts.Where(i => i.scriptType == DDLScriptType.AlterFor).ToList();
            // Run Scripts-----------------
            int o = 0;
            numberToCompute = dMEEditor.ETL.script.Scripts.Count;
            int p1 = dMEEditor.ETL.script.Scripts.Where(u => u.scriptType == DDLScriptType.CreateTable).Count();
            //for (int i = 0; i < dMEEditor.ETL.script.Scripts.Where(u=>u.scriptType == DDLScriptType.CreateTable).Count(); i++)
            //{
            foreach (LScript  sc in dMEEditor.ETL.script.Scripts.Where(u => u.scriptType == DDLScriptType.CreateTable))
            {

                //dMEEditor.ETL.script.Scripts[i]

                destds = DME.GetDataSource(sc.destinationdatasourcename);
               
                srcds = DME.GetDataSource(sc.sourcedatasourcename);
               
                if (destds != null )
                {
                   // destds.Dataconnection.OpenConnection();
                    DME.OpenDataSource(sc.destinationdatasourcename);
                    //  srcds.Dataconnection.OpenConnection();
                    if (destds.ConnectionStatus== System.Data.ConnectionState.Open)
                    {
                        if (sc.scriptType == DDLScriptType.CreateTable)
                        {
                            sc.errorsInfo = destds.ExecuteSql(sc.ddl); // t.Result;
                            LScriptTracker tr = new LScriptTracker();
                            tr.currenrecordentity = sc.entityname;
                            tr.currentrecorddatasourcename = sc.destinationdatasourcename;
                           // tr.currenrecordindex = i;
                            tr.scriptType = sc.scriptType;
                            tr.errorsInfo = sc.errorsInfo;
                            dMEEditor.ETL.trackingHeader.trackingscript.Add(tr);
                            sc.errormessage = DME.ErrorObject.Message;
                            // Report progress as a percentage of the total task.
                            int percentComplete = (int)((float)CurrentRecord / (float)numberToCompute * 100);
                            if (percentComplete > highestPercentageReached)
                            {
                                highestPercentageReached = percentComplete;
                                PassedArgs x = new PassedArgs();
                                x.CurrentEntity = tr.currenrecordentity;
                                x.DatasourceName = destds.DatasourceName;
                                x.Objects.Add(new ObjectItem { obj = tr, Name = "TrackingHeader" });
                                x.ParameterInt1 = percentComplete;
                                DME.Passedarguments = x;
                                worker.ReportProgress(percentComplete);
                            }
                            if (backgroundWorker1.CancellationPending)
                            {
                                e.Cancel = true;
                            }
                            CurrentRecord += 1;
                        }
                    }
                    else
                    {

                        DME.ErrorObject.Flag = Errors.Failed;
                        DME.ErrorObject.Message = $" Could not Connect to on the Data Dources {sc.destinationdatasourcename} or {sc.sourcedatasourcename}";
                        sc.errorsInfo = DME.ErrorObject;
                        sc.errormessage = DME.ErrorObject.Message;
                        LScriptTracker tr = new LScriptTracker();
                        tr.currenrecordentity = sc.entityname;
                        tr.currentrecorddatasourcename = sc.destinationdatasourcename;
                      //  tr.currenrecordindex = i;
                        tr.scriptType = sc.scriptType;
                        tr.errorsInfo = sc.errorsInfo;
                        DME.ETL.trackingHeader.trackingscript.Add(tr);
                    }
               
               
                }

            }
            //------------Update Entity structure
            int p2 = dMEEditor.ETL.script.Scripts.Where(u => u.scriptType == DDLScriptType.CopyData).Count();
            foreach (LScript sc in dMEEditor.ETL.script.Scripts.Where(u => u.scriptType == DDLScriptType.CopyData))
            {



                destds = DME.GetDataSource(sc.destinationdatasourcename);
                srcds = DME.GetDataSource(sc.sourcedatasourcename);

                if (destds != null && srcds != null)
                {
                   // destds.Dataconnection.OpenConnection();
                   // srcds.Dataconnection.OpenConnection();
                    DME.OpenDataSource(sc.destinationdatasourcename);
                    DME.OpenDataSource(sc.sourcedatasourcename);
                    if (destds.ConnectionStatus == System.Data.ConnectionState.Open)
                    {
                        if (sc.scriptType == DDLScriptType.CopyData)
                        {

                            sc.errorsInfo = DME.ETL.CopyEntityData(srcds, destds, sc.entityname, true);  //t1.Result;//DMEEditor.ETL.CopyEntityData(srcds, destds, ScriptHeader.Scripts[i], true);
                            sc.errormessage = DME.ErrorObject.Message;
                            LScriptTracker tr = new LScriptTracker();
                            tr.currenrecordentity = sc.entityname;
                            tr.currentrecorddatasourcename = sc.destinationdatasourcename;
                         //   tr.currenrecordindex = i;
                            tr.scriptType = sc.scriptType;
                            tr.errorsInfo = sc.errorsInfo;
                            DME.ETL.trackingHeader.trackingscript.Add(tr);

                            // Report progress as a percentage of the total task.
                            int percentComplete = (int)((float)CurrentRecord / (float)numberToCompute * 100);
                            if (percentComplete > highestPercentageReached)
                            {
                                highestPercentageReached = percentComplete;
                                PassedArgs x = new PassedArgs();
                                x.CurrentEntity = tr.currenrecordentity;
                                x.DatasourceName = destds.DatasourceName;
                                x.Objects.Add(new ObjectItem { obj = tr, Name = "TrackingHeader" });
                                x.ParameterInt1 = percentComplete;
                                DME.Passedarguments = x;
                                worker.ReportProgress(percentComplete);
                            }
                            if (backgroundWorker1.CancellationPending)
                            {
                                e.Cancel = true;
                            }
                            CurrentRecord += 1;
                        }
                    }
                    else
                    {

                        DME.ErrorObject.Flag = Errors.Failed;
                        DME.ErrorObject.Message = $" Could not Connect to on the Data Dources {sc.destinationdatasourcename} or {sc.sourcedatasourcename}";
                       sc.errorsInfo = DME.ErrorObject;
                        sc.errormessage = DME.ErrorObject.Message;
                        LScriptTracker tr = new LScriptTracker();
                        tr.currenrecordentity = sc.entityname;
                        tr.currentrecorddatasourcename = sc.destinationdatasourcename;
                       // tr.currenrecordindex = i;
                        tr.scriptType = sc.scriptType;
                        tr.errorsInfo = sc.errorsInfo;
                        DME.ETL.trackingHeader.trackingscript.Add(tr);
                    }
                


                }

            }
            int p3 = dMEEditor.ETL.script.Scripts.Where(u => u.scriptType == DDLScriptType.AlterFor).Count();
            foreach (LScript sc in dMEEditor.ETL.script.Scripts.Where(u => u.scriptType == DDLScriptType.AlterFor))
            {


                // CurrentRecord = i;
                destds = DME.GetDataSource(sc.destinationdatasourcename);
                srcds = DME.GetDataSource(sc.sourcedatasourcename);
                if (destds != null )
                {
                    destds.Dataconnection.OpenConnection();
                    DME.OpenDataSource(sc.destinationdatasourcename);
                    //      srcds.Dataconnection.OpenConnection();
                    if (destds.ConnectionStatus == System.Data.ConnectionState.Open)
                    {
                        if (sc.scriptType == DDLScriptType.AlterFor)
                        {

                           sc.errorsInfo = destds.ExecuteSql(sc.ddl);
                            sc.errormessage = DME.ErrorObject.Message;
                            LScriptTracker tr = new LScriptTracker();
                            tr.currenrecordentity = sc.entityname;
                            tr.currentrecorddatasourcename = sc.destinationdatasourcename;
                           // tr.currenrecordindex = i;
                            tr.scriptType = sc.scriptType;
                            tr.errorsInfo = sc.errorsInfo;
                            DME.ETL.trackingHeader.trackingscript.Add(tr);

                            // Report progress as a percentage of the total task.

                            int percentComplete = (int)((float)CurrentRecord / (float)numberToCompute * 100);
                            if (percentComplete > highestPercentageReached)
                            {
                                highestPercentageReached = percentComplete;
                                PassedArgs x = new PassedArgs();
                                x.CurrentEntity = tr.currenrecordentity;
                                x.DatasourceName = destds.DatasourceName;
                                x.Objects.Add(new ObjectItem { obj = tr, Name = "TrackingHeader" });
                                x.ParameterInt1 = percentComplete;
                                DME.Passedarguments = x;
                                worker.ReportProgress(percentComplete);
                            }

                            //  DMEEditor.RaiseEvent(this, x);


                            if (backgroundWorker1.CancellationPending)
                            {
                                e.Cancel = true;
                            }
                            CurrentRecord += 1;
                        }
                    }
                    else
                    {

                        DME.ErrorObject.Flag = Errors.Failed;
                        DME.ErrorObject.Message = $" Could not Connect to on the Data Dources {sc.destinationdatasourcename} or {sc.sourcedatasourcename}";
                        sc.errorsInfo = DME.ErrorObject;
                        sc.errormessage = DME.ErrorObject.Message;
                        LScriptTracker tr = new LScriptTracker();
                        tr.currenrecordentity = sc.entityname;
                        tr.currentrecorddatasourcename = sc.destinationdatasourcename;
                       // tr.currenrecordindex = i;
                        tr.scriptType = sc.scriptType;
                        tr.errorsInfo = sc.errorsInfo;
                        DME.ETL.trackingHeader.trackingscript.Add(tr);

                    }
    
                }

            }

            #endregion

            //-----------------------------

            return DME.ErrorObject;
        }
    }

}
