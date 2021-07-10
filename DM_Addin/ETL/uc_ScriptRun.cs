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
        public IPassedArgs Passedarg { get; set; }
        IBranch RootAppBranch;
        public IVisUtil Visutil { get; set; }
        IBranch branch; 
       // BackgroundWorkerThread backgroundWorker;
        CancellationTokenSource tokenSource;
        CancellationToken token;
        bool RequestCancle = false;
      
        int errorcount = 0;
        public void Run(string param1)
        {
            throw new NotImplementedException();
        }
        public void SetConfig(IDMEEditor pbl, IDMLogger plogger, IUtil putil, string[] args, IPassedArgs e, IErrorsInfo per)
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
           
            this.dataConnectionsBindingSource.DataSource = DMEEditor.ConfigEditor.DataConnections;
            scriptBindingSource.DataSource = DMEEditor.ETL.script.Entities;

            this.RunScriptbutton.Click += RunScriptbutton_Click;
            this.StopButton.Click += StopButton_Click;
            this.ErrorsAllowdnumericUpDown.Value = 10;
            // this.CreateScriptButton.Click += CreateScriptButton_Click;
            // this.scriptBindingSource.DataSource = DMEEditor.ETL.trackingHeader;



        }
        private void StopButton_Click(object sender, EventArgs e)
        {
            tokenSource.Cancel();
            //backgroundWorker.RequestCancel();
        }
        private  void RunScriptbutton_Click(object sender, EventArgs e)
        {
            RunScripts();

        }
        private async Task RunScripts()
        {
            DMEEditor.ETL.StopErrorCount = this.ErrorsAllowdnumericUpDown.Value;
            errorcount = 0;
            progressBar1.Step = 1;
            progressBar1.Maximum = 3;
            tokenSource = new CancellationTokenSource();
            token = tokenSource.Token;
            var progress = new Progress<PassedArgs>(percent =>
            {
                progressBar1.CustomText = percent.ParameterInt1 + " out of " + percent.ParameterInt2;

                if (percent.ParameterInt2 > 0)
                {
                    progressBar1.Maximum = percent.ParameterInt2;

                }
                progressBar1.Value = percent.ParameterInt1;
                //this.Log_panel.BeginInvoke(new Action(() =>
                if (percent.EventType == "Update")
                {
                    update();
                }
               
                //if (DMEEditor.ErrorObject.Flag == Errors.Failed)
                //{
                   if (!string.IsNullOrEmpty(percent.EventType))
                   {
                        if (percent.EventType == "Stop")
                        {
                            tokenSource.Cancel();
                        }
                    }
              //  }
               

            });
            Action action =
           () =>
               MessageBox.Show("Done");
            var ScriptRun = Task.Run(() =>
            {
                CancellationTokenRegistration ctr = token.Register(() => StopTask());
                 DMEEditor.ETL.RunScriptAsync(progress, token).Wait();
                
                MessageBox.Show("Done");
            });
           
        }
     
        void StopTask()
        {
            // Attempt to cancel the task politely
            tokenSource.Cancel();
            MessageBox.Show("Job Stopped");

        }
        private void update()
        {
            scriptBindingSource.DataSource = DMEEditor.ETL.script.Entities;
            childScriptsBindingSource.DataSource = scriptBindingSource;
            trackingBindingSource.DataSource = childScriptsBindingSource;
           
            scriptDataGridView.DataSource = scriptBindingSource;
            DataCopyScripts.DataSource = childScriptsBindingSource;
            TrackingdataGridView.DataSource = trackingBindingSource;

            scriptBindingSource.ResetBindings(false);
            childScriptsBindingSource.ResetBindings(false);
            trackingBindingSource.ResetBindings(false);
            //scriptDataGridView.Invoke(new MethodInvoker(() => { scriptDataGridView.Refresh(); }));
            //DataCopyScripts.Invoke(new MethodInvoker(() => { DataCopyScripts.Refresh(); }));
            //TrackingdataGridView.Invoke(new MethodInvoker(() => { TrackingdataGridView.Refresh(); }));

            scriptDataGridView.Invoke(new MethodInvoker(() => { scriptDataGridView.Update(); }));
            DataCopyScripts.Invoke(new MethodInvoker(() => { DataCopyScripts.Update(); }));
            TrackingdataGridView.Invoke(new MethodInvoker(() => { TrackingdataGridView.Update(); }));

        }
        #region" background worker"
        BackgroundWorker backgroundWorker1 = new BackgroundWorker();
        private void BackgroundWorker_ReportProgress(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
            update();
        }
        private void BackgroundWorker_JobCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            MessageBox.Show("Job Completed");
        }



        #endregion
    }
}
