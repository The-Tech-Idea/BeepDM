using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.Logger;

using TheTechIdea.DataManagment_Engine;
using TheTechIdea.Winforms.VIS;
using System;

using System.Data;

using System.Linq;


using System.Windows.Forms;
using TheTechIdea.Util;


namespace TheTechIdea.Hidden
{
    public partial class Frm_MainDisplayForm : Form,IDM_Addin
    {
        public Frm_MainDisplayForm()
        {
            InitializeComponent();
        }

        public string AddinName { get; set; } = "Beep";
        public string Description { get; set; } = "Data Management Main Display";
        public string DllName { get; set; }
        public string ObjectName { get; set; }
        public string ObjectType { get; set; } = "Form";
        public string DllPath { get; set; }
        public string NameSpace { get; set; }
        public string ParentName { get; set; }
        public IRDBSource RdbmsDs { get; set; }
        public IDataSource FileDs { get; set; }
        public Boolean DefaultCreate { get; set; } = false;
        public IDMLogger Logger { get; set; }
       
        public IDMEEditor DMEEditor { get; set; }
        public string EntityName { get; set; }
        public EntityStructure EntityStructure { get; set; }
        public DataSet Dset { get; set; }
       
        public IVisUtil Visutil { get; set; }
        public IErrorsInfo ErrorObject  { get; set; }
        public IUtil Util { get; set; }
        public PassedArgs Args { get; set; }
        public PassedArgs PassedArgsFromFunctionTree { get; set; }
        public PassedArgs PassedArgsFromDataTree { get; set; }
        public IDMDataView MyDataView { get; set; } 
       // public event EventHandler<PassedArgs> OnObjectSelected;
        public void Run(string param1)
        {
           
        }

        public void SetConfig(IDMEEditor pDMEEditor, IDMLogger plogger, IUtil putil, string[] args, PassedArgs obj, IErrorsInfo per)
        {
            Args=  obj;
            //SourceConnection = pdataSource;

           ErrorObject  = per;
            Logger = plogger;
            DMEEditor = pDMEEditor;
         
            Util = putil;
            obj.AddinName = AddinName;
            string[] args1 = { AddinName, null, null };
            var o= obj.Objects.Where(c => c.Name == "VISUTIL").FirstOrDefault();
            Visutil = (IVisUtil)o.obj;
        
            Visutil.DisplayPanel=(Control)this.splitContainer1.Panel1;
            //Visutil.treeEditor.DisplayPanel = Visutil.DisplayPanel;
            this.uc_logpanel1.SetConfig(pDMEEditor, Logger, putil, args, obj, per);
           // this.uc_AddinFunctionTree1.SetConfig(pDMEEditor, Logger,putil, args1,obj,per);
            //this.uc_DataViewTree1.SetConfig(pDMEEditor, Logger,putil, args,obj,per);
        //    this.uc_ControlTree1.SetConfig(pDMEEditor, Logger, putil, args, obj, per);
            this.uc_DynamicTree1.SetConfig(pDMEEditor, Logger, putil, args, obj, per);
            //this.uc_DataViewTree1.OnObjectSelected += Uc_DataViewTree1_OnObjectSelected;
          //  this.uc_AddinFunctionTree1.OnObjectSelected += Uc_AddinFunctionTree1_OnObjectSelected;
            this.Shown += Frm_MainDisplayForm_Shown;
        }

        private void Frm_MainDisplayForm_Shown(object sender, EventArgs e)
        {
            uc_logpanel1.startLoggin = true;
        }

        private void Uc_AddinFunctionTree1_OnObjectSelected(object sender, PassedArgs e)
        {
         //   OnObjectSelected?.Invoke(this, e);
            //if (DMEEditor.viewEditor.CurrentEntity != null)
            //{
            //    EntityName = DMEEditor.viewEditor.CurrentEntity.EntityName;
            //}
            
            string[] args = { EntityName, null, null };

          
            if (e.DMView != null)
            {
                MyDataView = e.DMView;
            }
            else
            {
                e.DMView = MyDataView;
            }
          
          
            if (e.AddinType == "Form")
            {
                IDM_Addin addin = Visutil.ShowFormFromAddin(e.ObjectName, DMEEditor, args, PassedArgsFromDataTree);

            }
            else
            {
                if (e.AddinType == "UserControl")
                {
                    IDM_Addin addin = Visutil.ShowUserControlInContainer(e.ObjectName, this.splitContainer1.Panel1,DMEEditor, args, PassedArgsFromDataTree);
                }
            }
          

        }

        //private void Uc_DataViewTree1_OnObjectSelected(object sender, PassedArgs e)

        //{
        //    if (DMEEditor.viewEditor.CurrentTable != null)
        //    {
        //        TableName = DMEEditor.viewEditor.CurrentTable.Name;
        //    }
        //    else { TableName = e.CurrentTable;  }

        //    if (e.DMView != null)
        //    {
        //        MyDataView = e.DMView;
        //    }
        //    else
        //    {
        //        e.DMView = MyDataView;
        //    }

        //    PassedArgsFromDataTree = e;


        //    if (e.EventType== "MouseDoubleClick")
        //    {
        //        if (uc_DataViewTree1.Visutil.treeEditor.CurrentNode.nodeType == "DM")
        //        {
        //            IRDBSource Ds = (IRDBSource)e.DataSource;
        //            string[] args = { TableName, Ds.Dataconnection.ConnectionProp.SchemaName, null };
        //            IDM_Addin addin = Visutil.ShowUserControlFromAddinOnControl("Uc_DataTableGridEdit", (IRDBSource)e.DataSource, Visutil.DisplayPanel, DMEEditor, args, PassedArgsFromDataTree);
        //        }
               
        //    }

        //    OnObjectSelected?.Invoke(this, e);
        //}

        public void RaiseObjectSelected()
        {
           
        }

        private void uc_DataViewTree1_Load(object sender, EventArgs e)
        {

        }
    }
}
