using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheTechIdea;
using TheTechIdea.Util;
using TheTechIdea.Logger;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.Winforms.VIS;
using TheTechIdea.DataManagment_Engine.Workflow;
using TheTechIdea.DataManagment_Engine.Vis;
using static System.Windows.Forms.DataGridView;

namespace TheTechIdea.ETL
{
    public partial class uc_WorkFlowManagerMainScreen : UserControl, IDM_Addin, IAddinVisSchema
    {
        public uc_WorkFlowManagerMainScreen()
        {
            InitializeComponent();
        }

        public string ParentName { get ; set ; }
        public string ObjectName { get ; set ; }
        public string ObjectType { get ; set ; } = "UserControl";
        public string AddinName { get; set; } = "WorkFlow Manager";
        public string Description { get; set; } = "Editor for WorkFlows";
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
        public IPassedArgs Passedarg { get ; set ; }
        public IVisUtil Vis { get; set; }
        IBranch branch { get; set; }
        IBranch dragedBranch { get; set; }
   #region "IAddinVisSchema"
        public string RootNodeName { get; set; } = "WorkFlows";
        public string CatgoryName { get; set; }
        public int Order { get; set; } = 2;
        public int ID { get; set; } = 2;
        public string BranchText { get; set; } = "Folders";
        public int Level { get; set; }
        public EnumPointType BranchType { get; set; } = EnumPointType.Entity;
        public int BranchID { get; set; } = 2;
        public string IconImageName { get; set; } = "workflowentity.ico";
        public string BranchStatus { get; set; }
        public int ParentBranchID { get; set; }
        public string BranchDescription { get; set; } = "";
        public string BranchClass { get; set; } = "WORKFLOW";
        #endregion "IAddinVisSchema"
        // public event EventHandler<PassedArgs> OnObjectSelected;
        string storagefolder;
        public void RaiseObjectSelected()
        {
            throw new NotImplementedException();
        }

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
            Vis = (IVisUtil)e.Objects.Where(c => c.Name == "VISUTIL").FirstOrDefault().obj;
            branch = (IBranch)e.Objects.Where(c => c.Name == "Branch").FirstOrDefault().obj;
            storagefolder = DMEEditor.ConfigEditor.Config.Folders.Where(o => o.FolderFilesType == FolderFileTypes.WorkFlows).FirstOrDefault().FolderPath;
            //foreach (var item in Enum.GetValues(typeof(EnumParameterType)))
            //{
            //    this.parameterTypeDataGridViewTextBoxColumn.Items.Add(item);
            //    this.parameterTypeDataGridViewTextBoxColumn1.Items.Add(item);
            //}
            workFlowsBindingSource.DataSource = DMEEditor.ConfigEditor.WorkFlows;
            foreach (IMap_Schema i in DMEEditor.ConfigEditor.MappingSchema)
            {
                this.Mapping.Items.Add(i.SchemaName);
            }
            if (e.CurrentEntity != null)
            {
                if (DMEEditor.ConfigEditor.WorkFlows.Any(x => x.DataWorkFlowName == e.CurrentEntity))
                {
                    FindRecord(e.CurrentEntity);
                }
              
            }
          
            
            dataConnectionsBindingSource.DataSource = DMEEditor.ConfigEditor.DataConnections;
            workFlowActionsBindingSource.DataSource = DMEEditor.ConfigEditor.WorkFlowActions;
            inTableParametersBindingSource.DataSource = datastepsBindingSource;
            datastepsBindingSource.AddingNew += DatastepsBindingSource_AddingNew;
          
            outTableParametersBindingSource.DataSource = datastepsBindingSource;
            rulesBindingSource.DataSource=datastepsBindingSource;
            rulesBindingSource.AddingNew += RulesBindingSource_AddingNew;
            if (workFlowsBindingSource.Count == 0)
            {
                workFlowsBindingSource.AddNew();
            }
         
            this.workFlowsBindingNavigatorSaveItem.Click += WorkFlowsBindingNavigatorSaveItem_Click;

            this.workFlowsBindingSource.ListChanged += workFlowsBindingSource_ListChanged;
        
            this.workFlowActionsDataGridView.CellContentClick += WorkFlowActionsDataGridView_CellContentClick;
            inTableParametersBindingSource.AddingNew += InTableParametersBindingSource_AddingNew;
            outTableParametersBindingSource.AddingNew += OutTableParametersBindingSource_AddingNew;
            this.workFlowsBindingSource.AddingNew += WorkFlowsBindingSource_AddingNew;
            this.InParametersentitiesBindingSource.AddingNew += InParametersentitiesBindingSource_AddingNew;
            this.OutParamtersentitiesBindingSource.AddingNew += OutParamtersentitiesBindingSource_AddingNew;
            this.objectTypesBindingSource.DataSource = DMEEditor.ConfigEditor.objectTypes;

            this.inParametersDataGridView.DragEnter += InParametersDataGridView_DragEnter;
            this.inParametersDataGridView.DragDrop += InParametersDataGridView_DragDrop;
            this.OutParameterdataGridView.DragEnter += OutParameterdataGridView_DragEnter;
            this.OutParameterdataGridView.DragDrop += OutParameterdataGridView_DragDrop;
            this.inParametersDataGridView.DataError += InParametersDataGridView_DataError;
            this.OutParameterdataGridView.DataError += OutParameterdataGridView_DataError;
            workFlowActionsDataGridView.DataError += WorkFlowActionsDataGridView_DataError;
        }

      

        private void WorkFlowActionsDataGridView_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.Cancel = false;
        }

        private void OutParameterdataGridView_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.Cancel = false;
        }

        private void InParametersDataGridView_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.Cancel = false;
        }


        #region "Drag and Drop"
        private void OutParameterdataGridView_DragDrop(object sender, DragEventArgs e)
        {
           
            dragedBranch = (IBranch)e.Data.GetData(e.Data.GetFormats()[0]);
            Point p = inParametersDataGridView.PointToClient(new Point(e.X, e.Y));
            PassedArgs inpar = new PassedArgs();
          
            HitTestInfo myHitTest = OutParameterdataGridView.HitTest(p.X, p.Y);
            if (myHitTest.RowIndex == -1)
            {
           ///     this.outTableParametersBindingSource.AddNew();
                inpar = (PassedArgs)this.inTableParametersBindingSource.Current;
            }
            else
            {
                if (myHitTest.RowIndex == this.outTableParametersBindingSource.Count)
                {
                    this.outTableParametersBindingSource.AddNew();
                    inpar = (PassedArgs)this.outTableParametersBindingSource.Current;
                }
                else
                {
                    inpar = (PassedArgs)this.OutParameterdataGridView.Rows[myHitTest.RowIndex].DataBoundItem;
                    // 
                }

            }
            if (this.outTableParametersBindingSource.Count <= 1)
            {
                this.outTableParametersBindingSource.AddNew();
                inpar = (PassedArgs)this.outTableParametersBindingSource.Current;
            }
            if (this.outTableParametersBindingSource.Current == null)
            {
                this.outTableParametersBindingSource.AddNew();
                inpar = (PassedArgs)this.outTableParametersBindingSource.Current;
            }
            inpar.Category = dragedBranch.BranchClass;
            inpar.CurrentEntity = dragedBranch.BranchText;
            inpar.DatasourceName = dragedBranch.DataSourceName;
            inpar.EventType = dragedBranch.BranchType.ToString();
        }

        private void OutParameterdataGridView_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.AllowedEffect;
        }

        private void InParametersDataGridView_DragDrop(object sender, DragEventArgs e)
        {
            dragedBranch = (IBranch)e.Data.GetData(e.Data.GetFormats()[0]);
            Point p = inParametersDataGridView.PointToClient(new Point(e.X, e.Y));
            PassedArgs inpar = new PassedArgs();
            HitTestInfo myHitTest = inParametersDataGridView.HitTest(p.X, p.Y);
            if (myHitTest.RowIndex == -1)
            {
           //     this.inTableParametersBindingSource.AddNew();
                inpar = (PassedArgs)this.inTableParametersBindingSource.Current;
            }
            else
            {
                if (myHitTest.RowIndex== this.inTableParametersBindingSource.Count)
                {
                    this.inTableParametersBindingSource.AddNew();
                    inpar = (PassedArgs)this.inTableParametersBindingSource.Current;
                }
                else
                {
                     inpar = (PassedArgs)  this.inParametersDataGridView.Rows[myHitTest.RowIndex].DataBoundItem;
                   // 
                }
               
            }
            if (this.inTableParametersBindingSource.Count <= 1)
            {
                this.inTableParametersBindingSource.AddNew();
                inpar = (PassedArgs)this.inTableParametersBindingSource.Current;
            }
            if (this.inTableParametersBindingSource.Current == null)
            {
                this.inTableParametersBindingSource.AddNew();
                inpar = (PassedArgs)this.inTableParametersBindingSource.Current;
            }
           
            inpar.Category = dragedBranch.BranchClass;
            inpar.CurrentEntity = dragedBranch.BranchText;
            inpar.DatasourceName = dragedBranch.DataSourceName;
            inpar.EventType = dragedBranch.BranchType.ToString();

            this.inParametersDataGridView.Refresh();


        }

        private void InParametersDataGridView_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.AllowedEffect;

        }
        #endregion


        private bool FindRecord(string name)
        {
            workFlowsBindingSource.MoveFirst();
            bool found = false;
            while (!found)
            {
                IDataWorkFlow w =(IDataWorkFlow) workFlowsBindingSource.Current;
                if (w.DataWorkFlowName == name)
                {
                    found = true;
                }else
                {
                    workFlowsBindingSource.MoveNext();
                }
            }
            return found;
           // workFlowsBindingSource.DataSource = DMEEditor.ConfigEditor.WorkFlows[DMEEditor.ConfigEditor.WorkFlows.FindIndex(x => x.DataWorkFlowName == e.CurrentEntity)];
        }
        private void workFlowsBindingSource_ListChanged(object sender, ListChangedEventArgs e)
        {
           if (e.ListChangedType== ListChangedType.ItemAdded)
            {
                this.dataWorkFlowNameTextBox.Enabled = true;
            }else
            {
                this.dataWorkFlowNameTextBox.Enabled = false;
            }
        }

        private void WorkFlowActionsDataGridView_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 6) //make sure button index here
            {
                ShowParameters("INPARAMETER");
            }
            if (e.ColumnIndex == 7) //make sure button index here
            {
                ShowParameters("OUTPARAMETER");
            }
        }
        private void ShowParameters(string parametertype)
        {
            if (datastepsBindingSource.Count > 0)
            {
                string[] args = { "New Query Entity", null, null };
                List<ObjectItem> ob = new List<ObjectItem>(); ;
                ObjectItem it = new ObjectItem();
                it.obj = this;
                it.Name = "Addin";
                ob.Add(it);
                IDataWorkFlowStep step = (IDataWorkFlowStep)datastepsBindingSource.Current;
                IDataWorkFlow wk = (IDataWorkFlow)workFlowsBindingSource.Current;
                if (!string.IsNullOrEmpty(step.StepName) && !string.IsNullOrWhiteSpace(step.StepName))
                {
                    if (!string.IsNullOrEmpty(this.dataWorkFlowNameTextBox.Text) && !string.IsNullOrWhiteSpace(this.dataWorkFlowNameTextBox.Text))
                    {
                        if (wk.Datasteps != null)
                        {
                            int stepidx = wk.Datasteps.FindIndex(i => i.StepName == step.StepName);
                            PassedArgs Passedarguments = new PassedArgs
                            {
                                Addin = null,
                                AddinName = null,
                                AddinType = "",
                                DMView = null,
                                CurrentEntity = this.dataWorkFlowNameTextBox.Text,
                                Id = stepidx,
                                ObjectType = "PARAMETERS",
                                DataSource = null,
                                ObjectName = this.dataWorkFlowNameTextBox.Text,

                                Objects = ob,

                                DatasourceName = BranchText,
                                EventType = parametertype

                            };
                            Vis.ShowUserControlPopUp("uc_workflowParameters", DMEEditor, args, Passedarguments);
                        }
                        else
                        {
                            MessageBox.Show("Please Save Data.");
                        }



                    }
                    else
                    {
                        MessageBox.Show("Please Enter the name for the schema and Save.");
                    }


                }
                else
                {
                    MessageBox.Show("Please Enter the name for Step and Save.");
                }


            }
        }
        private void InTableParametersDataGridView1_DataError(object sender, DataGridViewDataErrorEventArgs anError)
        {
            MessageBox.Show("Error happened " + anError.Context.ToString());

            if (anError.Context == DataGridViewDataErrorContexts.Commit)
            {
                MessageBox.Show("Commit error");
            }
            if (anError.Context == DataGridViewDataErrorContexts.CurrentCellChange)
            {
                MessageBox.Show("Cell change");
            }
            if (anError.Context == DataGridViewDataErrorContexts.Parsing)
            {
                MessageBox.Show("parsing error");
            }
            if (anError.Context == DataGridViewDataErrorContexts.LeaveControl)
            {
                MessageBox.Show("leave control error");
            }

            if ((anError.Exception) is ConstraintException)
            {
                DataGridView view = (DataGridView)sender;
                view.Rows[anError.RowIndex].ErrorText = "an error";
                view.Rows[anError.RowIndex].Cells[anError.ColumnIndex].ErrorText = "an error";

                anError.ThrowException = false;
            }
        }
        private void InTableParametersDataGridView_DataError(object sender, DataGridViewDataErrorEventArgs anError)
        {
            MessageBox.Show("Error happened " + anError.Context.ToString());

            if (anError.Context == DataGridViewDataErrorContexts.Commit)
            {
                MessageBox.Show("Commit error");
            }
            if (anError.Context == DataGridViewDataErrorContexts.CurrentCellChange)
            {
                MessageBox.Show("Cell change");
            }
            if (anError.Context == DataGridViewDataErrorContexts.Parsing)
            {
                MessageBox.Show("parsing error");
            }
            if (anError.Context == DataGridViewDataErrorContexts.LeaveControl)
            {
                MessageBox.Show("leave control error");
            }

            if ((anError.Exception) is ConstraintException)
            {
                DataGridView view = (DataGridView)sender;
                view.Rows[anError.RowIndex].ErrorText = "an error";
                view.Rows[anError.RowIndex].Cells[anError.ColumnIndex].ErrorText = "an error";

                anError.ThrowException = false;
            }
        }
        #region "Add new "
        private void OutParamtersentitiesBindingSource_AddingNew(object sender, AddingNewEventArgs e)
        {
            EntityStructure x = new EntityStructure();
            
            e.NewObject = x;
        }

        private void InParametersentitiesBindingSource_AddingNew(object sender, AddingNewEventArgs e)
        {
            EntityStructure x = new EntityStructure();

            e.NewObject = x;
        }
        private void WorkFlowsBindingSource_AddingNew(object sender, AddingNewEventArgs e)
        {
            IDataWorkFlow x = new DataWorkFlow();
            x.WorkSpaceFolder = storagefolder;
            x.Id = Guid.NewGuid().ToString();
            e.NewObject = x;
        }
        private void OutTableParametersBindingSource_AddingNew(object sender, AddingNewEventArgs e)
        {
            WorkFlowStep s = (WorkFlowStep)datastepsBindingSource.Current;
            PassedArgs x = new PassedArgs();
            //if (s.OutParameters == null)
            //{
            //    s.OutParameters = new List<PassedArgs>();
            //}
            //s.OutParameters.Add(x);
            e.NewObject = x;
        }
        private void InTableParametersBindingSource_AddingNew(object sender, AddingNewEventArgs e)
        {
            WorkFlowStep s = (WorkFlowStep)datastepsBindingSource.Current;
            PassedArgs x = new PassedArgs();
            //if (s.InParameters == null)
            //{
            //    s.InParameters = new List<PassedArgs>();
            //}
            //s.InParameters.Add(x);
            e.NewObject = x;
        }
        private void RulesBindingSource_AddingNew(object sender, AddingNewEventArgs e)
        {
            WorkFlowStepRules r = new WorkFlowStepRules();
            WorkFlowStep s = (WorkFlowStep)datastepsBindingSource.Current;
            //if (s.Rules == null)
            //{
            //    s.Rules = new List<WorkFlowStepRules>();
            //}
            e.NewObject = r;

        }
        private void DatastepsBindingSource_AddingNew(object sender, AddingNewEventArgs e)
        {
            IDataWorkFlow w = (IDataWorkFlow)workFlowsBindingSource.Current;
            WorkFlowStep ws = new WorkFlowStep();
            ws.ID = Guid.NewGuid().ToString();
            //if (w.Datasteps == null)
            //{
            //    w.Datasteps = new List<WorkFlowStep>();
            //}
            //w.Datasteps.Add(ws);
            e.NewObject = ws;
        }
        #endregion
        private void WorkFlowsBindingNavigatorSaveItem_Click(object sender, EventArgs e)
        {
            try
            {
                SaveData();
                DMEEditor.AddLogMessage("Succcess", "Saved Workflow ", DateTime.Now, -1, "", Errors.Ok);
                MessageBox.Show("Succcess Saving WorkFlow");
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Error", $"Could not save Workflow {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                MessageBox.Show("Error Saving WorkFlow");
            }
           
            
        }
        private IErrorsInfo SaveData()
        {
            try
            {
               

                inTableParametersBindingSource.EndEdit();
                outTableParametersBindingSource.EndEdit();
                dataSourcesBindingSource.EndEdit();
                rulesBindingSource.EndEdit();
                datastepsBindingSource.EndEdit();
                dataConnectionsBindingSource.EndEdit();
                workFlowsBindingSource.EndEdit();
                //   workFlowsBindingSource.ResumeBinding();
                //     DMEEditor.WorkFlowEditor.WorkFlows = (BindingList<TheTechIdea.DataManagment_Engine.Workflow.DataWorkFlow>)workFlowsBindingSource.List;
                DMEEditor.ConfigEditor.SaveWork();
                branch.CreateChildNodes();
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Error",$"Could not save Workflow {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                MessageBox.Show("Error Saving WorkFlow");
            }
            return DMEEditor.ErrorObject;
        }

        private void RunWorkFlow_Click(object sender, EventArgs e)
        {
            
            try
            {
                SaveData();
                DMEEditor.WorkFlowEditor.RunWorkFlow(this.dataWorkFlowNameTextBox.Text);
            }
            catch (Exception ex)
            {
                string mes = this.dataWorkFlowNameTextBox.Text;
                DMEEditor.AddLogMessage(ex.Message, "Could not run WorkFlow " + mes, DateTime.Now, -1, mes, Errors.Failed);
            };
           
        }

        private void StopWorkFlowbutton_Click(object sender, EventArgs e)
        {
            DMEEditor.WorkFlowEditor.StopWorkFlow();
        }

       
    }
}
