using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Vis;
using TheTechIdea.Logger;
using TheTechIdea.Util;
using TheTechIdea.Winforms.VIS;

namespace TheTechIdea.ETL
{
    public partial class uc_workflowParameters : UserControl, IDM_Addin
    {
        public uc_workflowParameters()
        {
            InitializeComponent();
        }

        public string ParentName { get; set; }
        public string ObjectName { get; set; }
        public string ObjectType { get; set; } = "UserControl";
        public string AddinName { get; set; } = "Parameters";
        public string Description { get; set; } = "Parameters";
        public bool DefaultCreate { get; set; } = true;
        public string DllPath { get ; set ; }
        public string DllName { get ; set ; }
        public string NameSpace { get ; set ; }
        public DataSet Dset { get ; set ; }
        public IErrorsInfo ErrorObject { get ; set ; }
        public IDMLogger Logger { get ; set ; }
        public IDMEEditor DMEEditor { get ; set ; }
        public EntityStructure EntityStructure { get ; set ; }
        public string EntityName { get ; set ; }
        public PassedArgs Args { get ; set ; }
        public string RootNodeName { get ; set ; }
        public string CatgoryName { get ; set ; }
        public int Order { get ; set ; }
        public int ID { get ; set ; }
      
        public IVisUtil Vis { get; set; }
      
        public void Run(string param1)
        {
            throw new NotImplementedException();
        }

        public void SetConfig(IDMEEditor pbl, IDMLogger plogger, IUtil putil, string[] args, PassedArgs e, IErrorsInfo per)
        {
            Args = e;
            Logger = plogger;
            ErrorObject = per;
            DMEEditor = pbl;
            Vis = (IVisUtil)e.Objects.Where(c => c.Name == "VISUTIL").FirstOrDefault().obj;
            if (e.CurrentEntity != null)
            {
                if (DMEEditor.ConfigEditor.WorkFlows.Any(x => x.DataWorkFlowName == e.CurrentEntity))
                {
                    if (e.EventType == "INPARAMETER")
                    {
                      
                        if (e.Id != -1)
                        {
                            ID = e.Id;
                            EntityName = e.CurrentEntity;
                            inParametersBindingSource.DataSource = DMEEditor.ConfigEditor.WorkFlows[DMEEditor.ConfigEditor.WorkFlows.FindIndex(x => x.DataWorkFlowName == e.CurrentEntity)].Datasteps[e.Id].InParameters;
                        }
                       
                    }
                    else
                    {
                     
                        if (e.Id != -1)
                        {
                            ID = e.Id;
                            EntityName = e.CurrentEntity;
                            if (DMEEditor.ConfigEditor.WorkFlows[DMEEditor.ConfigEditor.WorkFlows.FindIndex(x => x.DataWorkFlowName == e.CurrentEntity)].Datasteps[e.Id].OutParameters == null)
                            {
                                DMEEditor.ConfigEditor.WorkFlows[DMEEditor.ConfigEditor.WorkFlows.FindIndex(x => x.DataWorkFlowName == e.CurrentEntity)].Datasteps[e.Id].OutParameters = new List<PassedArgs>();
                            }
                            inParametersBindingSource.DataSource = DMEEditor.ConfigEditor.WorkFlows[DMEEditor.ConfigEditor.WorkFlows.FindIndex(x => x.DataWorkFlowName == e.CurrentEntity)].Datasteps[e.Id].OutParameters;
                        }
                       
                    }
                   
                }
            }
            inParametersBindingNavigator.BindingSource = inParametersBindingSource;
            this.inParametersBindingNavigatorSaveItem.Click += InParametersBindingNavigatorSaveItem_Click;
         //   this.inParametersBindingSource.AddingNew += InParametersBindingSource_AddingNew;
            this.inParametersDataGridView.DataError += InParametersDataGridView_DataError;
        }

        private void InParametersDataGridView_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.Cancel = true;
        }

        //private void InParametersBindingSource_AddingNew(object sender, AddingNewEventArgs e)
        //{
        //    throw new NotImplementedException();
        //}

        private void InParametersBindingNavigatorSaveItem_Click(object sender, EventArgs e)
        {
            try

            {
               // DMEEditor.WorkFlowEditor.WorkFlows[DMEEditor.WorkFlowEditor.WorkFlows.FindIndex(x => x.DataWorkFlowName == EntityName)].Datasteps=
                // DMEEditor.ConfigEditor.DataConnections[DMEEditor.ConfigEditor.DataConnections.FindIndex(x => x.ConnectionName == EntityName)].Headers = (List<WebApiHeader>)this.headersBindingSource.List;
                DMEEditor.ConfigEditor.SaveWork();
                MessageBox.Show("Saved Successfully");

            }
            catch (Exception ex)
            {
                string errmsg = "Error in saving defaults";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                MessageBox.Show($"{errmsg}:{ex.Message}");
            }
        }
    }
}
