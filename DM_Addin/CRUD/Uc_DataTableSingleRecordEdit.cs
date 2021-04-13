using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.Logger;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.Winforms.VIS;
using TheTechIdea.Util;
using TheTechIdea;

namespace TheTechIdea.CRUD
{
    public partial class Uc_DataTableSingleRecordEdit : UserControl,IDM_Addin,IWinFormAddin
    {
        public Uc_DataTableSingleRecordEdit()
        {
            InitializeComponent();
        }

        public string AddinName { get; set; } = "Data Table Single Record Edit";
        public string Description { get; set; } = "Data Table Single Record Edit";
        public string ObjectName { get; set; } = "Data Table Single Record Edit";
        public string ObjectType { get; set; } = "UserControl";
        public string DllPath { get; set; }
        public string NameSpace { get; set; }
        public string DllName { get; set; }
        public string ParentName { get; set; }
        public Boolean DefaultCreate { get; set; } = false;
        public IDataSource DestConnection { get; set; }
        public IDMLogger Logger { get; set; }
        public IDataSource SourceConnection { get; set; }
        public IDMEEditor DMEEditor { get; set; }
        public string EntityName { get; set; }
        public EntityStructure EntityStructure { get; set; }
        public DataSet Dset { get; set; }
        private IDMDataView MyDataView { get; set; } = null;
        public IVisUtil Visutil { get; set; }
        public IErrorsInfo ErrorObject  { get; set; }
        public PassedArgs Passedarg { get; set; }
       // public event EventHandler<PassedArgs> OnObjectSelected;
        public System.Windows.Forms.BindingSource bindingSource1 = new BindingSource();
        public System.Windows.Forms.PropertyGrid propertyGrid1 = new PropertyGrid();
        public System.Windows.Forms.Panel panel1 = new Panel();
        DataTable t;
        RDBSource rdb;
        public void Run(string param1)
        {
            // LoadView(param1.ToUpper());

        }

        public void   SetConfig(IDMEEditor pDMEEditor, IDMLogger plogger, IUtil putil, string[] args, PassedArgs obj, IErrorsInfo per)
        {
            Passedarg=  obj;
          
            Logger = plogger;
            this.Width = 430;
           ErrorObject  = per;
            DMEEditor = pDMEEditor;
            Visutil = (IVisUtil)obj.Objects.Where(c => c.Name == "VISUTIL").FirstOrDefault().obj;
             SourceConnection = Passedarg.DataSource;
            if (SourceConnection.Category == DatasourceCategory.RDBMS)
            {
                rdb = (RDBSource)SourceConnection;
                string schemaname = rdb.GetSchemaName();

            }
            switch (obj.ObjectType)
            {
                case "RDBMSTABLE":
                    EntityName = obj.CurrentEntity;
                   
                   
                    t = (DataTable) rdb.GetEntity(EntityName, null) ;
                    break;
                case "CRUDENTITY":
                    EntityName = obj.CurrentEntity;
                    
                   
                    t = (DataTable) rdb.GetEntity(EntityName, null);
                    
                    break;
                default:
                    break;
            }

           

         
           
               bindingSource1.DataSource = t;
                bindingSource1.ResetBindings(true);
                Visutil.controlEditor.GenerateTableViewOnControl(EntityName, ref panel1, t, ref bindingSource1, 200, SourceConnection.DatasourceName);    
                this.Controls.Add(panel1);
                panel1.Dock = DockStyle.Fill;
                panel1.AutoScroll = true;
            //Form a = (Form)Parent;
            //a.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            EntityNameLabel.Text = EntityName;
            bindingNavigator1.BindingSource = bindingSource1;
            bindingNavigator1.SendToBack();
               // Savebutton.SendToBack();
               
                
         
         
          
            this.SavetoolStripButton.Click += Savebutton_Click;
        }

        private void Savebutton_Click(object sender, EventArgs e)
        {
            try
            {
                bindingSource1.EndEdit();
                rdb.UpdateEntities(EntityName, t.GetChanges());
                Logger.WriteLog($"Data Saved");
                MessageBox.Show("Data Saved");
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
                Logger.WriteLog($"Error in saving Data ({ex.Message}) ");

            }
        }

        private void TreeView1_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            //var args = new PassedArgs
            //{
            //    Addin = (IDM_Addin)this,
            //    AddinName = this.Name,
            //    AddinType = "",
            //    CurrentEntity = e.Node.Text,
            //    DataSource = this.SourceConnection
            //};
            //OnObjectSelected?.Invoke(this, args);
        }

        public void RaiseObjectSelected()
        {
           
        }
    }
}
