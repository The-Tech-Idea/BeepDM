using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.CompositeLayer;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.DataView;
using TheTechIdea.DataManagment_Engine.Editor;
using TheTechIdea.DataManagment_Engine.Vis;
using TheTechIdea.DataManagment_Engine.Workflow;
using TheTechIdea.Logger;
using TheTechIdea.Util;
using TheTechIdea.Winforms.VIS;

namespace TheTechIdea.ETL
{
    public partial class uc_ScriptRun : UserControl,IDM_Addin
    {
        public uc_ScriptRun()
        {
            InitializeComponent();
        }

        public string ParentName { get; set; }
        public string AddinName { get; set; } = "Script Runner";
        public string Description { get; set; } = "Script Runner";
        public string ObjectName { get; set; }
        public string ObjectType { get; set; } = "UserControl";
        public Boolean DefaultCreate { get; set; } = true;
        public string DllPath { get; set; }
        public string DllName { get; set; }
        public string NameSpace { get; set; }
        public DataSet Dset { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public IDMLogger Logger { get; set; }
        public IDMEEditor DMEEditor { get; set; }
        public EntityStructure EntityStructure { get; set; }
        public string EntityName { get; set; }
        public PassedArgs Passedarg { get; set; }
        IBranch RootAppBranch;
        public IVisUtil Visutil { get; set; }
        IBranch branch;
        BackgroundWorkerThread backgroundWorker;
        public void Run(string param1)
        {
            throw new NotImplementedException();
        }

        public void SetConfig(IDMEEditor pbl, IDMLogger plogger, IUtil putil, string[] args, PassedArgs e, IErrorsInfo per)
        {
            Passedarg = e;
            Logger = plogger;
            ErrorObject = per;
            DMEEditor = pbl;
            Visutil = (IVisUtil)e.Objects.Where(c => c.Name == "VISUTIL").FirstOrDefault().obj;
            if (e.Objects.Where(c => c.Name == "Branch").Any())
            {
                branch = (IBranch)e.Objects.Where(c => c.Name == "Branch").FirstOrDefault().obj;
            }
            if (e.Objects.Where(c => c.Name == "RootBranch").Any())
            {
                RootAppBranch = (IBranch)e.Objects.Where(c => c.Name == "RootBranch").FirstOrDefault().obj;
            }
            //foreach (var item in Enum.GetValues(typeof(DDLScriptType)))
            //{
            //    scriptTypeComboBox.Items.Add(item);
            //}
           
            //if (e.Objects.Where(c => c.Name == "Script").Any())
            //{
                
            //    ScriptHeader = (LScriptHeader)e.Objects.Where(c => c.Name == "Script").FirstOrDefault().obj;
            //}
            //if (ScriptHeader==null)
            //{
              //  ScriptHeader = DMEEditor.ETL.script;
            //}
            this.dataConnectionsBindingSource.DataSource = DMEEditor.ConfigEditor.DataConnections;
            scriptBindingSource.DataSource = DMEEditor.ETL.script.Scripts;

            this.RunScriptbutton.Click += RunScriptbutton_Click;
            this.StopButton.Click += StopButton_Click;
            // this.CreateScriptButton.Click += CreateScriptButton_Click;
            this.trackingscriptBindingSource.DataSource = DMEEditor.ETL.trackingHeader.trackingscript;
            //this.DMEEditor.PassEvent += DMEEditor_PassEvent;
           
        }

       

      
        private void StopButton_Click(object sender, EventArgs e)
        {
            backgroundWorker.RequestCancel();
        }
        private void RunScriptbutton_Click(object sender, EventArgs e)
        {
            ObjectItem item1 = new ObjectItem();
            item1.obj = DMEEditor;
            item1.Name = "DMEEDITOR";
            Passedarg.Objects.Add(item1);
            backgroundWorker = new BackgroundWorkerThread(Passedarg);
            backgroundWorker.ReportProgress += BackgroundWorker_ReportProgress;
            backgroundWorker1.RunWorkerCompleted += BackgroundWorker1_RunWorkerCompleted;
            progressBar1.Step = 2;
            backgroundWorker.RunWorker(Passedarg);

        }

        private void BackgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            MessageBox.Show("Job Completed");
        }

        public IErrorsInfo RunScript(IDMEEditor dMEEditor)
        {
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


                
                 destds = dMEEditor.GetDataSource(dMEEditor.ETL.script.Scripts[i].destinationdatasourcename);
                 srcds = dMEEditor.GetDataSource(dMEEditor.ETL.script.Scripts[i].sourcedatasourcename);
                if (destds != null)
                {
                    if (dMEEditor.ETL.script.Scripts[i].scriptType == DDLScriptType.CreateTable)
                    {

                        
                        var t = Task.Run<IErrorsInfo>(() => { return destds.ExecuteSql(dMEEditor.ETL.script.Scripts[i].ddl); });
                        t.Wait();
                        dMEEditor.ETL.script.Scripts[i].errorsInfo = t.Result;
                        LScriptTracker tr = new LScriptTracker();
                        tr.currenrecordentity = dMEEditor.ETL.script.Scripts[i].entityname;
                        tr.currentrecorddatasourcename = dMEEditor.ETL.script.Scripts[i].destinationdatasourcename;
                        tr.currenrecordindex = i;
                        tr.scriptType = dMEEditor.ETL.script.Scripts[i].scriptType;
                        tr.errorsInfo = dMEEditor.ETL.script.Scripts[i].errorsInfo;
                        dMEEditor.ETL.trackingHeader.trackingscript.Add(tr);
                        dMEEditor.ETL.script.Scripts[i].errormessage = DMEEditor.ErrorObject.Message;
                        // Report progress as a percentage of the total task.
                        int percentComplete = (int)((float)CurrentRecord / (float)numberToCompute * 100);
                        if (percentComplete > highestPercentageReached)
                        {
                            highestPercentageReached = percentComplete;

                        }
                        PassedArgs x = new PassedArgs();
                      
                        x.CurrentEntity = tr.currenrecordentity;
                        x.DatasourceName = destds.DatasourceName;
                        x.Objects.Add(new ObjectItem { obj = tr, Name = "TrackingHeader" });
                        x.ParameterInt1 = percentComplete;
                        DMEEditor.Passedarguments = x;
                        update();
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

                   
                
                    destds =DMEEditor.GetDataSource(dMEEditor.ETL.script.Scripts[i].destinationdatasourcename);
                    srcds = DMEEditor.GetDataSource(dMEEditor.ETL.script.Scripts[i].sourcedatasourcename);
                   
                if (destds != null)
                     {
                     if (dMEEditor.ETL.script.Scripts[i].scriptType == DDLScriptType.CopyData)
                     {

                        var t1 = Task.Run<IErrorsInfo>(() => { return DMEEditor.ETL.CopyEntityData(srcds, destds, dMEEditor.ETL.script.Scripts[i], true); } );
                      
                        t1.Wait();

                        dMEEditor.ETL.script.Scripts[i].errorsInfo =  t1.Result ;//DMEEditor.ETL.CopyEntityData(srcds, destds, ScriptHeader.Scripts[i], true);
                        dMEEditor.ETL.script.Scripts[i].errormessage = DMEEditor.ErrorObject.Message;
                        LScriptTracker tr = new LScriptTracker();
                        tr.currenrecordentity = dMEEditor.ETL.script.Scripts[i].entityname;
                        tr.currentrecorddatasourcename = dMEEditor.ETL.script.Scripts[i].destinationdatasourcename;
                        tr.currenrecordindex = i;
                        tr.scriptType = dMEEditor.ETL.script.Scripts[i].scriptType;
                        tr.errorsInfo = dMEEditor.ETL.script.Scripts[i].errorsInfo;
                        DMEEditor.ETL.trackingHeader.trackingscript.Add(tr);

                        // Report progress as a percentage of the total task.
                        int percentComplete = (int)((float)CurrentRecord / (float)numberToCompute * 100);
                        if (percentComplete > highestPercentageReached)
                        {
                            highestPercentageReached = percentComplete;

                        }
                        PassedArgs x = new PassedArgs();
                        x.CurrentEntity = tr.currenrecordentity;
                        x.DatasourceName = destds.DatasourceName;
                        x.Objects.Add(new ObjectItem { obj = tr, Name = "TrackingHeader" });
                        x.ParameterInt1 = percentComplete;
                        DMEEditor.Passedarguments = x;
                        update();
                        CurrentRecord += 1;
                    }
                  
                   
                   }
                    
               

                
            }
            for (int i = 0; i < dMEEditor.ETL.script.Scripts.Count(); i++)
            {


               // CurrentRecord = i;
                 destds = DMEEditor.GetDataSource(dMEEditor.ETL.script.Scripts[i].destinationdatasourcename);
                 srcds = DMEEditor.GetDataSource(dMEEditor.ETL.script.Scripts[i].sourcedatasourcename);
                if (destds != null)
                {
                    if (dMEEditor.ETL.script.Scripts[i].scriptType == DDLScriptType.AlterFor)
                    {
                       
                        var t = Task.Run<IErrorsInfo>(() => { return destds.ExecuteSql(dMEEditor.ETL.script.Scripts[i].ddl); });
                        t.Wait();
                        dMEEditor.ETL.script.Scripts[i].errorsInfo = t.Result;
                        dMEEditor.ETL.script.Scripts[i].errormessage = DMEEditor.ErrorObject.Message;
                        LScriptTracker tr = new LScriptTracker();
                        tr.currenrecordentity = dMEEditor.ETL.script.Scripts[i].entityname;
                        tr.currentrecorddatasourcename = dMEEditor.ETL.script.Scripts[i].destinationdatasourcename;
                        tr.currenrecordindex = i;
                        tr.scriptType = dMEEditor.ETL.script.Scripts[i].scriptType;
                        tr.errorsInfo = dMEEditor.ETL.script.Scripts[i].errorsInfo;
                        DMEEditor.ETL.trackingHeader.trackingscript.Add(tr);

                        // Report progress as a percentage of the total task.
                        int percentComplete = (int)((float)CurrentRecord / (float)numberToCompute * 100);
                        if (percentComplete > highestPercentageReached)
                        {
                            highestPercentageReached = percentComplete;

                        }
                        PassedArgs x = new PassedArgs();
                        x.CurrentEntity = tr.currenrecordentity;
                        x.DatasourceName = destds.DatasourceName;
                        x.Objects.Add(new ObjectItem { obj = tr, Name = "TrackingHeader" });
                        x.ParameterInt1 = percentComplete;
                        DMEEditor.Passedarguments = x;
                        update();
                          CurrentRecord += 1;
                    }
                 
                    
                }

             

            }
            return DMEEditor.ErrorObject;
        }

      
        private void update()
        {

            scriptBindingSource.DataSource = DMEEditor.ETL.script.Scripts;
            this.trackingscriptBindingSource.DataSource = DMEEditor.ETL.trackingHeader.trackingscript;
            this.scriptBindingSource.ResetBindings( true);
            this.scriptDataGridView.Refresh();
            this.trackingscriptBindingSource.ResetBindings(true);
            this.trackingscriptDataGridView.Refresh();
        }
        #region" background worker"
        BackgroundWorker backgroundWorker1 = new BackgroundWorker();
        private void BackgroundWorker_ReportProgress(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
            update();
        }

     
       
        #endregion
    }
}
