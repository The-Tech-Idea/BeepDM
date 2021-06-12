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
           
            this.dataConnectionsBindingSource.DataSource = DMEEditor.ConfigEditor.DataConnections;
            scriptBindingSource.DataSource = DMEEditor.ETL.script.Scripts;

            this.RunScriptbutton.Click += RunScriptbutton_Click;
            this.StopButton.Click += StopButton_Click;
            // this.CreateScriptButton.Click += CreateScriptButton_Click;
            this.trackingscriptBindingSource.DataSource = DMEEditor.ETL.trackingHeader.trackingscript;
           
            
           
        }

      

        private void StopButton_Click(object sender, EventArgs e)
        {
            
            backgroundWorker.RequestCancel();
        }
        private  void RunScriptbutton_Click(object sender, EventArgs e)
        {
            RunScripts();

        }
        private async Task RunScripts()
        {
            progressBar1.Step = 2;
            var progress = new Progress<int>(percent =>
            {
                progressBar1.Text = percent + "%";
                progressBar1.Value = percent;
                update();
            });
           
            await Task.Run(() =>
            {
                DMEEditor.ETL.RunScript(progress);

            });
            MessageBox.Show("Done");
        }
        private void update()
        {
            scriptBindingSource.DataSource = DMEEditor.ETL.script.Scripts;
            trackingscriptBindingSource.DataSource = DMEEditor.ETL.trackingHeader.trackingscript;
            this.trackingscriptBindingSource.ResetBindings(true);
            this.scriptBindingSource. ResetBindings( true); 
            this.scriptDataGridView.Update();
            this.scriptDataGridView.Refresh();
            this.trackingscriptDataGridView.Update();
            this.trackingscriptDataGridView.Refresh();
            
           
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
