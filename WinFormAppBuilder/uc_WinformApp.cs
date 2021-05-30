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
using TheTechIdea.DataManagment_Engine.AppBuilder;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.DataView;
using TheTechIdea.DataManagment_Engine.Vis;
using TheTechIdea.Logger;
using TheTechIdea.Util;
using TheTechIdea.Winforms.VIS;

namespace TheTechIdea.ETL
{
    public partial class Frm_WinformApp : Form, IDM_Addin
    {
        public Frm_WinformApp()
        {
            InitializeComponent();
        }

        public string ParentName { get; set; }
        public string AddinName { get; set; } = "Beep Applications";
        public string Description { get; set; } = "WinForm Application";
        public string ObjectName { get; set; }
        public string ObjectType { get; set; } = "Form";
        public Boolean DefaultCreate { get; set; } = true;
        public string DllPath { get ; set ; }
        public string DllName { get ; set ; }
        public string NameSpace { get ; set ; }
        public DataSet Dset { get ; set ; }
        public IErrorsInfo ErrorObject { get ; set ; }
        public IDMLogger Logger { get ; set ; }
        public IDMEEditor DMEEditor { get ; set ; }
        public EntityStructure EntityStructure { get ; set ; }
        public string EntityName { get ; set ; }
        public PassedArgs Passedarg { get ; set ; }

        //public event EventHandler<PassedArgs> OnObjectSelected;
     
        AutoCompleteStringCollection data { get; set; } = new AutoCompleteStringCollection();
        public IVisUtil Visutil { get; set; }
       // private IDMDataView MyDataView;
        DataViewDataSource ds;
        IBranch RootAppBranch;
        IBranch branch;
        App app;
        TreeView _fieldsTreeCache = new TreeView();
     
        public void RaiseObjectSelected()
        {
            throw new NotImplementedException();
        }

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

            branch = (IBranch)e.Objects.Where(c => c.Name == "Branch").FirstOrDefault().obj;
            RootAppBranch = (IBranch)e.Objects.Where(c => c.Name == "RootAppBranch").FirstOrDefault().obj;
            appsBindingSource.DataSource = DMEEditor.ConfigEditor.Apps[DMEEditor.ConfigEditor.Apps.FindIndex(x => x.AppName == e.CurrentEntity)];
            app = DMEEditor.ConfigEditor.Apps[DMEEditor.ConfigEditor.Apps.FindIndex(x => x.AppName == e.CurrentEntity)];
            ds = (DataViewDataSource)DMEEditor.GetDataSource(app.DataViewDataSourceName);
            // AutoCompleteStringCollection
            FillTree(1,DataTreeView,null);
            FillTree(1, _fieldsTreeCache, null);
            this.Searchbutton.Click += Searchbutton_Click;
            this.SearchtextBox1.KeyDown += input_KeyDown;
            this.DataTreeView.NodeMouseDoubleClick += DataTreeView_NodeMouseDoubleClick;
            SearchtextBox1.AutoCompleteCustomSource = data;
            CloseBoxButton.Click += CloseBoxButton_Click;
            MinimizeBoxButton.Click += MinimizeBoxButton_Click;
            MaximizeBoxButton.Click += MaximizeBoxButton_Click;
        }

        private void MaximizeBoxButton_Click(object sender, EventArgs e)
        {
           
        }

        private void MinimizeBoxButton_Click(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void CloseBoxButton_Click(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void DataTreeView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {

            EntityStructure = ds.GetEntityStructure(e.Node.Text, true);
            IDataSource entds = DMEEditor.GetDataSource(EntityStructure.DataSourceID);
         //   entds.Dataconnection.OpenConnection();
            DMEEditor.OpenDataSource(EntityStructure.DataSourceID);
            if (entds.ConnectionStatus == ConnectionState.Open)
            {
                
                ShowCRUD();
            }
            else
            {
                Visutil.controlEditor.MsgBox("Error", $"Failed to Open Entity DataSource {EntityStructure.DataSourceID}");
            }

          

            
        }

        private void input_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
            {
          
                FilterTree();
            //    this.SearchtextBox1.Enabled = true;
            }
        }
        private void Searchbutton_Click(object sender, EventArgs e)
        {
            FilterTree();
        }
        #region "Tree Handling"
        private void FilterTree()
        {
            //blocks repainting tree till all objects loaded
            this.DataTreeView.BeginUpdate();
            this.DataTreeView.Nodes.Clear();
            if (this.SearchtextBox1.Text != string.Empty)
            {
                FillTreeFilterd(1, DataTreeView, null);
            }
            else
            {
                foreach (TreeNode _node in this._fieldsTreeCache.Nodes)
                {
                    DataTreeView.Nodes.Add((TreeNode)_node.Clone());
                }
            }
            //enables redrawing tree after all objects have been added
            this.DataTreeView.EndUpdate();
        }
        private void FillTree(int startid, TreeView v, TreeNode parentnode = null)
        {
            if (ds != null)
            {
                List<EntityStructure> cr = ds.DataView.Entities.Where(cx => (cx.Id > 1) && (cx.ParentId == startid)).ToList();
                int i = 0;
                foreach (EntityStructure tb in cr)
                {
                    TreeNode node = new TreeNode(tb.EntityName);
                    data.Add(tb.EntityName);

                    node.Tag = tb.Id;
                    if (parentnode == null)
                    {
                        v.Nodes.Add(node);
                    }
                    else
                    {
                        parentnode.Nodes.Add(node);
                    }
                    if (ds.DataView.Entities.Any(o => o.ParentId == tb.Id))
                    {
                        FillTree(tb.Id, v, node);
                    }

                    i += 1;
                }
            }
            else
            {
                DMEEditor.Logger.WriteLog($"Could not Find DataView File " + ds.DataView.DataViewDataSourceID);
            }
        }
        private void FillTreeFilterd(int startid, TreeView v, TreeNode parentnode = null)
        {
            if (ds != null)
            {
                List<EntityStructure> cr = ds.DataView.Entities.Where(cx => (cx.Id > 1) && (cx.ParentId == startid) && cx.EntityName.ToLower().Contains(SearchtextBox1.Text.ToLower())).ToList();
                int i = 0;
                foreach (EntityStructure tb in cr)
                {
                    TreeNode node = new TreeNode(tb.EntityName);
                    data.Add(tb.EntityName);
                    node.Tag = tb.Id;
                    if (parentnode == null)
                    {
                        v.Nodes.Add(node);
                    }
                    else
                    {
                        parentnode.Nodes.Add(node);
                    }
                    if (ds.DataView.Entities.Any(o => o.ParentId == tb.Id && o.EntityName.ToLower().Contains(SearchtextBox1.Text.ToLower())))
                    {
                        FillTree(tb.Id, v, node);
                    }

                    i += 1;
                }
            }
            else
            {
                DMEEditor.Logger.WriteLog($"Could not Find DataView File " + ds.DataView.DataViewDataSourceID);
            }
        }
        #endregion
        #region "CRUD Control"
        private void ShowCRUD()
        {
            if (EntityStructure != null)
            {
                string[] args = { "New View", null, null };
                List<ObjectItem> ob = new List<ObjectItem>(); ;
                ObjectItem it = new ObjectItem();
                it.obj = this;
                it.Name = "Entity";
                ob.Add(it);
                PassedArgs Passedarguments = new PassedArgs
                {
                    Addin = null,
                    AddinName = null,
                    AddinType = "",
                    DMView = ds.DataView,
                    CurrentEntity = EntityStructure.EntityName,
                    Id = EntityStructure.Id,
                    ObjectType = "CRUDENTITY",
                    DataSource = ds,
                    ObjectName = ds.DataView.ViewName,
                    Objects = ob,
                    DatasourceName = EntityStructure.DataSourceID,
                    EventType = "CRUDENTITY"

                };
                Visutil.ShowUserControlInContainer("uc_getentities", ControlPanel, DMEEditor, args, Passedarguments);
            }
          
        }
        #endregion

    }
}
