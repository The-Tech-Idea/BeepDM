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
            //object UploadData = lsob[1];
            //IDataSource dataSource = (IDataSource)lsob[0];
            //IMapping_rep Mapping = (IMapping_rep)lsob[2];
            //string EntityName = (string)lsob[3];
            //IDbTransaction sqlTran;
            //DataTable tb = (DataTable)UploadData;
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
            for (int i = 0; i < dMEEditor.ETL.script.Scripts.Count(); i++)
            {



                destds = DME.GetDataSource(dMEEditor.ETL.script.Scripts[i].destinationdatasourcename);
                srcds = DME.GetDataSource(dMEEditor.ETL.script.Scripts[i].sourcedatasourcename);
                if (destds != null)
                {
                    if (dMEEditor.ETL.script.Scripts[i].scriptType == DDLScriptType.CreateTable)
                    {
                        dMEEditor.ETL.script.Scripts[i].errorsInfo = destds.ExecuteSql(dMEEditor.ETL.script.Scripts[i].ddl); // t.Result;
                        LScriptTracker tr = new LScriptTracker();
                        tr.currenrecordentity = dMEEditor.ETL.script.Scripts[i].entityname;
                        tr.currentrecorddatasourcename = dMEEditor.ETL.script.Scripts[i].destinationdatasourcename;
                        tr.currenrecordindex = i;
                        tr.scriptType = dMEEditor.ETL.script.Scripts[i].scriptType;
                        tr.errorsInfo = dMEEditor.ETL.script.Scripts[i].errorsInfo;
                        dMEEditor.ETL.trackingHeader.trackingscript.Add(tr);
                        dMEEditor.ETL.script.Scripts[i].errormessage = DME.ErrorObject.Message;
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
                    //else
                    //{
                    //    var t1 = Task.Run<IErrorsInfo>(() => { return DMEEditor.ETL.CopyEntityData(srcds, destds, ScriptHeader.Scripts[i], true); });
                    //    t1.Wait();
                    //    ScriptHeader.Scripts[i].errorsInfo = t1.Result;
                    //}

                    //destds.ExecuteSql(DMEEditor.DDLEditor.script[i].ddl);


                }

                //   ReportProgress?.Invoke(this, x);


            }
            //------------Update Entity structure

            for (int i = 0; i < dMEEditor.ETL.script.Scripts.Count(); i++)
            {



                destds = DME.GetDataSource(dMEEditor.ETL.script.Scripts[i].destinationdatasourcename);
                srcds = DME.GetDataSource(dMEEditor.ETL.script.Scripts[i].sourcedatasourcename);
              
                if (destds != null)
                {
                    if (dMEEditor.ETL.script.Scripts[i].scriptType == DDLScriptType.CopyData)
                    {

                        dMEEditor.ETL.script.Scripts[i].errorsInfo = DME.ETL.CopyEntityData(srcds, destds, dMEEditor.ETL.script.Scripts[i].entityname, true);  //t1.Result;//DMEEditor.ETL.CopyEntityData(srcds, destds, ScriptHeader.Scripts[i], true);
                        dMEEditor.ETL.script.Scripts[i].errormessage = DME.ErrorObject.Message;
                        LScriptTracker tr = new LScriptTracker();
                        tr.currenrecordentity = dMEEditor.ETL.script.Scripts[i].entityname;
                        tr.currentrecorddatasourcename = dMEEditor.ETL.script.Scripts[i].destinationdatasourcename;
                        tr.currenrecordindex = i;
                        tr.scriptType = dMEEditor.ETL.script.Scripts[i].scriptType;
                        tr.errorsInfo = dMEEditor.ETL.script.Scripts[i].errorsInfo;
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




            }
            for (int i = 0; i < dMEEditor.ETL.script.Scripts.Count(); i++)
            {


                // CurrentRecord = i;
                destds = DME.GetDataSource(dMEEditor.ETL.script.Scripts[i].destinationdatasourcename);
                srcds = DME.GetDataSource(dMEEditor.ETL.script.Scripts[i].sourcedatasourcename);
                if (destds != null)
                {
                    if (dMEEditor.ETL.script.Scripts[i].scriptType == DDLScriptType.AlterFor)
                    {

                        dMEEditor.ETL.script.Scripts[i].errorsInfo = destds.ExecuteSql(dMEEditor.ETL.script.Scripts[i].ddl);
                        dMEEditor.ETL.script.Scripts[i].errormessage = DME.ErrorObject.Message;
                        LScriptTracker tr = new LScriptTracker();
                        tr.currenrecordentity = dMEEditor.ETL.script.Scripts[i].entityname;
                        tr.currentrecorddatasourcename = dMEEditor.ETL.script.Scripts[i].destinationdatasourcename;
                        tr.currenrecordindex = i;
                        tr.scriptType = dMEEditor.ETL.script.Scripts[i].scriptType;
                        tr.errorsInfo = dMEEditor.ETL.script.Scripts[i].errorsInfo;
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



            }

            #endregion



            //-----------------------------

            return DME.ErrorObject;
        }
    }

}
