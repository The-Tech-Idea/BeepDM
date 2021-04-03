
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.Logger;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using TheTechIdea.Util;
using System.IO;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.FileManager;
using TheTechIdea.DataManagment_Engine.Workflow;
using TheTechIdea.DataManagment_Engine.WebAPI;
using System.Runtime.CompilerServices;

namespace TheTechIdea.Winforms.VIS
{
    public class TreeEditor_Notused : ITreeEditor

    {
        public TreeEditor_Notused(IDMLogger logger, IUtil putil, IErrorsInfo per)
        {
            Logger = logger;
            Util = putil;
            Erinfo = per;
        }
       
        public void SetConfig(IDMEEditor pDME_editor, TreeView ptree, string ptreetype, IDM_Addin psender)
        {
            Tree = ptree;
            DME_Editor = pDME_editor;
            Treetype = ptreetype;
            sender = psender;
            Erinfo = DME_Editor.ErrorObject;
            controlEditor.DME_Editor = pDME_editor;
            controlEditor.Erinfo = Erinfo;
            controlEditor.Logger = Logger;
            images = new ImageList();
            images.ImageSize= new Size(32, 32);
            images.ColorDepth = ColorDepth.Depth32Bit;
            foreach (string filename_w_path in Directory.GetFiles(DME_Editor.ConfigEditor.Config.Folders.Where(x => x.FolderFilesType == FolderFileTypes.GFX).FirstOrDefault().FolderPath, "*.ico", SearchOption.AllDirectories))
            {
                try
                {
                    string filename = Path.GetFileName(filename_w_path);

                    images.Images.Add(filename,Image.FromFile(filename_w_path) );
                

                }
                catch (FileLoadException ex)
                {
                    Erinfo.Flag = Errors.Failed;
                    Erinfo.Ex = ex;
                    Logger.WriteLog($"Error Loading icons ({ex.Message})");
                }
               
            }
            Tree.CheckBoxes = false;
            Tree.ImageList = images;
            Tree.BorderStyle = BorderStyle.None;
            Tree.ImageIndex = GetImageIndex("database.ico");
            Tree.SelectedImageIndex = GetImageIndex(SelectIcon);
            //Tree.images = GetImageIndex("select.ico");
            CreateMenusAndMainRootNodes();
            CreateDelagates();

          



        }

        private void Tree_DrawNode(object sender, DrawTreeNodeEventArgs e)
        {
            if (e.Node.ImageIndex >= e.Node.TreeView.ImageList.Images.Count) // if there is no image
            {
                int imagewidths = e.Node.TreeView.ImageList.ImageSize.Width;
                int textheight = TextRenderer.MeasureText(e.Node.Text, e.Node.NodeFont).Height;
                int x = e.Node.Bounds.Left - 3 - imagewidths / 2;
                int y = (e.Bounds.Top + e.Bounds.Bottom) / 2 + 1;

                Point point = new Point(x - imagewidths / 2, y - textheight / 2); // the new location for the text to be drawn

                TextRenderer.DrawText(e.Graphics, e.Node.Text, e.Node.NodeFont, point, e.Node.ForeColor);
            }
            else // drawn at the default location
                TextRenderer.DrawText(e.Graphics, e.Node.Text, e.Node.TreeView.Font, e.Bounds, e.Node.ForeColor);
        }

        public void CreateMenusAndMainRootNodes()
        {
            CreateMenuItems();
            CreateRootMenuNodes();
        }
        public void CreateRootMenuNodes()
        {
            if (Treetype == "Editor")
            {
                CreateTreeRootItemsForEditor();
            }
            else
            {
                CreateTreeRootItemsForMainDisplay();
            }
        }
        public void CreateDelagates()
        {
            Tree.AllowDrop = true;
            Tree.DrawMode = TreeViewDrawMode.OwnerDrawText;
            Tree.DrawNode += Tree_DrawNode;
            Tree.NodeMouseClick += TreeView1_NodeMouseClick;
            Tree.NodeMouseDoubleClick += TreeView1_NodeMouseDoubleClick;
            Tree.AfterCheck += TreeView1_AfterCheck;
            Tree.DragDrop += Tree_DragDrop;
            Tree.DragEnter += Tree_DragEnter;
            Tree.DragLeave += Tree_DragLeave;
            Tree.ItemDrag += Tree_ItemDrag;
            Tree.DragOver += Tree_DragOver;
        }
       
        public IDM_Addin sender { get; set; }
        public TreeView Tree { get; set; }
        public string Treetype { get; set; }
        public TreeNode _RDBMSNode { get; set; } //_RDBMSNode
        public TreeNode _NOSQLNode { get; set; }
        public TreeNode _DataViewNode { get; set; }
        public TreeNode _WorkFlowNode { get; set; }
        public TreeNode _FilesNode { get; set; }
        public TreeNode _MappingNode { get; set; }
        public TreeNode _WebApiNode { get; set; }
        public IUtil Util { get; set; }
        public IDMLogger Logger { get; set; }
        public IErrorsInfo Erinfo { get; set; }
       
        public string SelectedViewName { get; set; }
        public IDMEEditor DME_Editor { get; set; }
        public NodeID CurrentNode { get; set; }
        public IControlEditor controlEditor { get; set; } 
        public ImageList images { get; set; }
        public string SelectIcon { get; set; } = "cursor.ico";
        public Control DisplayPanel { get { return Visutil.DisplayPanel; } set { mdis = value; } }
        public event EventHandler<PassedArgs> OnObjectSelected;
        IDM_Addin addin { get; set; }

        public PassedArgs Passedarguments { get; set; }
        public IVisUtil Visutil { get; set; }
        private Control mdis = new Control();
        //--------------------------------------
        readonly ContextMenuStrip dataviewmenu = new ContextMenuStrip();
        readonly ContextMenuStrip dataviewhdrmenu = new ContextMenuStrip();
        readonly ContextMenuStrip datasourcemenu = new ContextMenuStrip();
        readonly ContextMenuStrip datasourceConnmenu = new ContextMenuStrip();
        readonly ContextMenuStrip datatablemenu = new ContextMenuStrip();
        readonly ContextMenuStrip dataEntitymenu = new ContextMenuStrip();
        readonly ContextMenuStrip fileManagermenu = new ContextMenuStrip();
        readonly ContextMenuStrip filemenu = new ContextMenuStrip();
        readonly ContextMenuStrip MappingManagermenu = new ContextMenuStrip();
        readonly ContextMenuStrip Mappingmenu = new ContextMenuStrip();
        readonly ContextMenuStrip WebAPiManagermenu = new ContextMenuStrip();
        readonly ContextMenuStrip WebApimenu = new ContextMenuStrip();
        readonly ContextMenuStrip NOSQLManagermenu = new ContextMenuStrip();
        readonly ContextMenuStrip NOSQLmenu = new ContextMenuStrip();
        bool busy = false;
        string cl = "";
        #region "Node Handling Functions"

        /// <summary>
        /// This Function Handles all click events in the tree
        /// </summary>
        /// <param name="e">The <see cref="TreeNodeMouseClickEventArgs"/> instance containing the event data.</param>
        /// <param name="v">The v is the Value represent the item in the tree node</param>
        /// <param name="t">The t is the Type of the node </param>
        public void NodeEvent(TreeNodeMouseClickEventArgs e, int v, string t)
        {
            // PassedArgs  = new PassedArgs();

            IDM_Addin s = sender;
            DME_Editor.viewEditor.CurrentEntity = null;
            DME_Editor.viewEditor.CurrentView = null;
            IDataSource ds = null;


            switch (t)
            {
                case "DV":
                    if (v == 0)
                    {
                        Passedarguments = new PassedArgs
                        {  // Obj= obj,
                            Addin = (IDM_Addin)sender,
                            AddinName = s.AddinName,
                            AddinType = "",
                            DMView = null,
                            CurrentEntity = e.Node.Text,

                            DataSource = null,
                            EventType = cl

                        };
                    }


                    break;
                case "DM":
                    IDMDataView view = DME_Editor.viewEditor.Views.Where(c => c.id == CurrentNode.NodeIndex).FirstOrDefault();

                    DME_Editor.viewEditor.CurrentEntity = view.Entity.Where(c => c.Id == CurrentNode.id).FirstOrDefault();
                    DME_Editor.viewEditor.CurrentEntity.ViewID = view.id;
                    DME_Editor.viewEditor.CurrentView = view;
                    string dsname = DME_Editor.viewEditor.CurrentEntity.DataSourceID;
                    if (dsname == null)
                    {
                        dsname = view.MainDataSourceID;
                        DME_Editor.viewEditor.CurrentEntity.DataSourceID = dsname;
                    }
                    ds = (IRDBSource)DME_Editor.GetDataSource(dsname);

                    //if (v == 0)
                    //{
                    Passedarguments = new PassedArgs
                    {  // Obj= obj,
                        Addin = (IDM_Addin)sender,
                        AddinName = s.AddinName,
                        AddinType = "",
                        DMView = view,
                        CurrentEntity = e.Node.Text,
                        Id = v,
                        ObjectType = "DATAVIEWTABLE",
                        DataSource = (IRDBSource)ds,
                        EventType = cl

                    };

                    break;

                case "FE":
                    ds = (IDataSource)DME_Editor.GetDataSource(e.Node.Text);

                    Passedarguments = new PassedArgs
                    {  // Obj= obj,
                        Addin = (IDM_Addin)sender,
                        AddinName = s.AddinName,
                        AddinType = "",
                        DMView = null,
                        CurrentEntity = e.Node.Text,
                        Id = v,
                        ObjectType = "FILE",
                        DataSource = ds,
                        EventType = cl

                    };

                    break;
                case "NS":
                    ds = (IDataSource)DME_Editor.GetDataSource(e.Node.Text);

                    Passedarguments = new PassedArgs
                    {  // Obj= obj,
                        Addin = (IDM_Addin)sender,
                        AddinName = s.AddinName,
                        AddinType = "",
                        DMView = null,
                        CurrentEntity = e.Node.Text,
                        Id = v,
                        ObjectType = "NOSQLMENU",
                        DataSource = ds,
                        EventType = cl

                    };

                    break;
                case "NQ":
                    ds = (IDataSource)DME_Editor.GetDataSource(e.Node.Text);

                    Passedarguments = new PassedArgs
                    {  // Obj= obj,
                        Addin = (IDM_Addin)sender,
                        AddinName = s.AddinName,
                        AddinType = "",
                        DMView = null,
                        CurrentEntity = e.Node.Text,
                        Id = v,
                        ObjectType = "NOSQLTABLE",
                        DataSource = ds,
                        EventType = cl

                    };

                    break;
                case "EN":


                    DME_Editor.viewEditor.CurrentEntity = new EntityStructure { DataSourceID = CurrentNode.misc, EntityName = CurrentNode.description };

                    dsname = DME_Editor.viewEditor.CurrentEntity.DataSourceID;

                    ds = (IRDBSource)DME_Editor.GetDataSource(dsname);

                    //if (v == 0)
                    //{
                    Passedarguments = new PassedArgs
                    {  // Obj= obj,
                        Addin = (IDM_Addin)sender,
                        AddinName = s.AddinName,
                        AddinType = "",
                        DMView = null,
                        CurrentEntity = e.Node.Text,
                        Id = v,
                        ObjectType = "DATABASETABLE",
                        DataSource = (IRDBSource)ds,
                        EventType = cl

                    };

                    break;

                case "WE":
                    ds = (IWebAPIDataSource)DME_Editor.GetDataSource(e.Node.Text);

                    Passedarguments = new PassedArgs
                    {  // Obj= obj,
                        Addin = (IDM_Addin)sender,
                        AddinName = s.AddinName,
                        AddinType = "",
                        DMView = null,
                        CurrentEntity = e.Node.Text,
                        Id = v,
                        ObjectType = "WEBAPITABLE",
                        DataSource = ds,
                        EventType = cl

                    };

                    break;
                case "DS":

                    if (v == 0)
                    {
                        Passedarguments = new PassedArgs
                        {  // Obj= obj,
                            Addin = (IDM_Addin)sender,
                            AddinName = s.AddinName,
                            AddinType = "",
                            DMView = null,
                            CurrentEntity = e.Node.Text,
                            DataSource = null,
                            EventType = cl

                        };

                    }
                    else
                    {
                        ds = DME_Editor.GetDataSource(e.Node.Text);
                        Passedarguments = new PassedArgs
                        {  // Obj= obj,
                            Addin = (IDM_Addin)sender,
                            AddinName = s.AddinName,
                            AddinType = "",
                            DMView = Passedarguments.DMView,
                            CurrentEntity = e.Node.Text,
                            Id = v,
                            DataSource = ds,
                            EventType = cl

                        };
                    }

                    break;

            }
            if (e.Button == MouseButtons.Right)
            {
                switch (t)
                {
                    case "VI":
                        e.Node.ContextMenuStrip = dataviewhdrmenu;
                        dataviewhdrmenu.Show();
                        break;
                    case "DV":

                        e.Node.ContextMenuStrip = dataviewmenu;
                        dataviewmenu.Show();
                        break;
                    case "DM":

                        e.Node.ContextMenuStrip = datatablemenu;
                        datatablemenu.Show();
                        break;
                    case "DS":
                        e.Node.ContextMenuStrip = datasourceConnmenu;
                        datasourceConnmenu.Show();
                        break;
                    case "DB":
                        e.Node.ContextMenuStrip = datasourcemenu;
                        datasourcemenu.Show();
                        break;
                    case "EN":
                        e.Node.ContextMenuStrip = dataEntitymenu;
                        dataEntitymenu.Show();
                        break;
                    case "FM":
                        e.Node.ContextMenuStrip = fileManagermenu;
                        fileManagermenu.Show();
                        break;
                    case "FE":
                        e.Node.ContextMenuStrip = filemenu;
                        filemenu.Show();
                        break;
                    case "MM":
                        e.Node.ContextMenuStrip = MappingManagermenu;
                        MappingManagermenu.Show();
                        break;
                    case "ME":
                        e.Node.ContextMenuStrip = Mappingmenu;
                        Mappingmenu.Show();
                        break;
                    case "NS":
                        e.Node.ContextMenuStrip = NOSQLManagermenu;
                        NOSQLManagermenu.Show();
                        break;
                    case "NQ":
                        e.Node.ContextMenuStrip = NOSQLmenu;
                        NOSQLmenu.Show();
                        break;
                    case "WM":
                        e.Node.ContextMenuStrip = WebAPiManagermenu;
                        WebAPiManagermenu.Show();
                        break;
                    case "WE":
                        e.Node.ContextMenuStrip = WebApimenu;
                        WebApimenu.Show();
                        break;


                }
                CurrentNode = GetNodeID(e.Node);

            }


        }
        private void TreeView1_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (busy) return;
            busy = true;
            try
            {
                //   CheckNodes(e.Node, e.Node.Checked);
            }
            catch (Exception ex)
            {

                Logger.WriteLog($"Error in Showing View on Tree ({ex.Message}) ");
                Erinfo.Flag = Errors.Failed;
                Erinfo.Ex = ex;
            }
            finally
            {
                busy = false;
            }

        }
        private void CheckNodes(TreeNode node, bool check)
        {
            try
            {
                CurrentNode = GetNodeID(node);
                NodeID nid = CurrentNode;
                switch (nid.nodeType)
                {
                    case "DV":

                        foreach (TreeNode child in node.Nodes)
                        {
                            child.Checked = check;
                            GetViewFromNode(node).Entity.Where(m => m.Id == nid.id).FirstOrDefault().Show = check;
                            CheckNodes(child, check);
                        }



                        break;
                    case "DM":

                        foreach (TreeNode child in node.Nodes)
                        {
                            child.Checked = check;
                            GetViewFromNode(node).Entity.Where(m => m.Id == nid.id).FirstOrDefault().Show = check;
                            CheckNodes(child, check);
                        }

                        break;
                    case "DS":
                        break;


                }
            }
            catch (Exception ex)
            {

                Logger.WriteLog($"Error in Showing View on Tree ({ex.Message}) ");
                Erinfo.Flag = Errors.Failed;
                Erinfo.Ex = ex;
            }



        }
        private void SetChildrenChecked(TreeNode treeNode, bool checkedState)
        {
            foreach (TreeNode item in treeNode.Nodes)
            {
                if (item.Checked != checkedState)
                {

                    // int vitem = Convert.ToInt32(item.Tag.ToString().Substring(item.Tag.ToString().IndexOf('-') + 1));
                    item.Checked = checkedState;
                    GetViewFromNode(item).Editable = checkedState;
                }
                SetChildrenChecked(item, item.Checked);
            }
        }
        private void TreeView1_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {

            if (e.Clicks == 1)
            {
                cl = "MouseClick";
            }
            else
            {
                cl = "MouseDoubleClick";
            }
            Nodeclickhandler(e);

        }
        private void TreeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Clicks == 1)
            {
                cl = "MouseClick";
            }
            else
            {
                cl = "MouseDoubleClick";
            }
            Nodeclickhandler(e);

        }
        private void Nodeclickhandler(TreeNodeMouseClickEventArgs e)
        {

            CurrentNode = GetNodeID(e.Node);

            Passedarguments = new PassedArgs();
            NodeEvent(e, CurrentNode.id, CurrentNode.nodeType);


            OnObjectSelected?.Invoke(this, Passedarguments);


        }
        #endregion
        #region "Menu Handling"
        public IDMDataView GetViewFromNode(TreeNode node)
        {
            NodeID nid = GetNodeID(node.Tag.ToString());
            IDMDataView v = DME_Editor.viewEditor.Views.Where(c => c.id == nid.NodeIndex).FirstOrDefault();
            return v;
        }
        public TreeNode GetViewMainNode(string viewname)
        {

            return _DataViewNode.Nodes.Cast<TreeNode>().Where(n => n.Text == viewname).FirstOrDefault();
        }
        public NodeID CreateNodeID(string pnodeType, int pNodeIndex, int pid, string pdescription, string pmisc)
        {
            return new NodeID { nodeType = pnodeType, NodeIndex = pNodeIndex, id = pid, description = pdescription, misc = pmisc };
        }
        public string CreateNodeIDString(string pnodeType, int pNodeIndex, int pid, string pdescription, string pmisc)
        {
            return pnodeType + ";" + pNodeIndex + ";" + pid + ";" + pdescription + ";" + pmisc;
        }
        public NodeID GetNodeID(string NodeTag)
        {
            NodeID retval;
            string[] s = NodeTag.Split(';');
            retval = new NodeID { nodeType = s[0], NodeIndex = Convert.ToInt32(s[1]), id = Convert.ToInt32(s[2]), description = s[3], misc = s[4] };
            return retval;
        }
        public NodeID GetNodeID(TreeNode node)
        {
            NodeID retval;
            string[] s = node.Tag.ToString().Split(';');
            retval = new NodeID { nodeType = s[0], NodeIndex = Convert.ToInt32(s[1]), id = Convert.ToInt32(s[2]), description = s[3], misc = s[4] };
            retval.CurrNode = node;
            return retval;
        }
        void LookupChecks(TreeNodeCollection nodes, List<TreeNode> list)
        {
            foreach (TreeNode node in nodes)
            {
                if ((node.Checked == true) && (GetNodeID(node).nodeType == "EN"))
                {
                    list.Add(node);
                }



                LookupChecks(node.Nodes, list);
            }
        }

        public void CreateTreeRootItemsForMainDisplay()
        {
            int x;

          _RDBMSNode = new TreeNode("RDBMS");
            // n.ImageIndex = 0;
            Tree.Nodes.Add(_RDBMSNode);
            _RDBMSNode.Tag = "DS;0;0;RDBMS Data Sources;RDBSource Needed for DataViews";
            x = GetImageIndex(_RDBMSNode,"database.ico");
            FillDataBaseNode(_RDBMSNode);


          
            _NOSQLNode = new TreeNode("NOSQL");
            // n.ImageIndex = 0;
            Tree.Nodes.Add(_NOSQLNode);
            x = GetImageIndex(_NOSQLNode,"bigdata.ico");
            _NOSQLNode.Tag = "NQ;0;0;NOSQL Data Sources;NOSQL Needed for DataViews";
            FillNOSQLNode(_NOSQLNode);




            _FilesNode = new TreeNode("Files");
            // nDV.ImageIndex = 1;
            Tree.Nodes.Add(_FilesNode);
            _FilesNode.Tag = "FM;0;0;FILES;Files Data Sources";
            x = GetImageIndex(_FilesNode,"file.ico");
            _FilesNode.Tag = "FM;0;0;FILES;Files Data Sources";
            FillFileNode(_FilesNode);


            _WebApiNode = new TreeNode("WebAPI(Services)");
            // nDV.ImageIndex = 1;
            Tree.Nodes.Add(_WebApiNode);
            x = GetImageIndex(_WebApiNode,"webapi.ico");
            _WebApiNode.Tag = "WM;0;0;WEBAPI;WebServices Data Sources";

            _WorkFlowNode = new TreeNode("WorkFlows");
            // nDV.ImageIndex = 1;
            Tree.Nodes.Add(_WorkFlowNode);
            x = GetImageIndex(_WorkFlowNode,"workflow.ico");
            _WorkFlowNode.Tag = "WK;0;0;WorkFlows;WorkFlows To Automate any Transaction between Data Sources";

            _MappingNode = new TreeNode("Mapping");
            // nDV.ImageIndex = 1;
            Tree.Nodes.Add(_MappingNode);
             x  = GetImageIndex(_MappingNode,"mapping.ico");
            _MappingNode.Tag = "MM;0;0;Mapping;Mapping Used for Data Loading and WorkFlows To Automate any Transaction between Data Sources";
            FillMappingNode(_MappingNode);


            //----------------------------------


            _DataViewNode = new TreeNode("Views");
            // nDV.ImageIndex = 1;
            Tree.Nodes.Add(_DataViewNode);
             x = GetImageIndex(_DataViewNode,"dataview.ico");
            _DataViewNode.Tag = "VI;0;0;Data Views;DataViews to Tables in the Data Sources";


        }
        public void CreateTreeRootItemsForEditor()
        {

            _DataViewNode = new TreeNode("Data Views");
            // nDV.ImageIndex = 1;
            Tree.Nodes.Add(_DataViewNode);
            _DataViewNode.Tag = "VI;0;0;Data Views;DataViews to Tables in the Data Sources";
        }
        public void CreateMenuItems()
        {
            dataviewhdrmenu.Items.Add("New View");
            dataviewhdrmenu.Items.Add("Load View");
            dataviewhdrmenu.ItemClicked += Dataviewhdrmenu_ItemClicked;

            dataviewmenu.Items.Add("Save View");
            dataviewmenu.Items.Add("Remove View");
            dataviewmenu.Items.Add("Remove Checked Tables");

            dataviewmenu.ItemClicked += Dataviewmenu_ItemClicked;

            datatablemenu.Items.Add("New Table");
            datatablemenu.Items.Add("Edit Table");
            datatablemenu.Items.Add("Remove Table");
            datatablemenu.Items.Add("Get Child Tables");
            datatablemenu.Items.Add("Remove Child Tables");
            datatablemenu.ItemClicked += DataTablemenu_ItemClicked;


            datasourceConnmenu.Items.Add("New/Edit Data Source");
            datasourceConnmenu.ItemClicked += DatasourceConnmenu_ItemClicked;

            datasourcemenu.Items.Add("New View");
            datasourcemenu.Items.Add("Delete");
            datasourcemenu.Items.Add("Get Entities/Tables");
            datasourcemenu.Items.Add("Add Checked Entites/Tables to a View");
            datasourcemenu.ItemClicked += Datasourcemenu_ItemClicked;

            dataEntitymenu.Items.Add("Creat View");

            dataEntitymenu.Items.Add("Link Entity to View");
            dataEntitymenu.ItemClicked += DataEntitymenu_ItemClicked;

            filemenu.Items.Add("Show");
            filemenu.ItemClicked += Filemenu_ItemClicked;
            fileManagermenu.Items.Add("Add File");
            fileManagermenu.ItemClicked += FileManagermenu_ItemClicked;

            Mappingmenu.Items.Add("Show");
            Mappingmenu.ItemClicked += Mappingmenu_ItemClicked; 
            MappingManagermenu.Items.Add("Add Mapping");
            MappingManagermenu.ItemClicked += MappingManagermenu_ItemClicked;


            WebApimenu.Items.Add("Show");
            WebApimenu.ItemClicked += WebApimenu_ItemClicked;
            WebAPiManagermenu.Items.Add("Add WebApi");
            WebAPiManagermenu.ItemClicked += WebAPiManagermenu_ItemClicked;

        }

        #endregion
        #region "DataView Node Handling"
        public IErrorsInfo ShowViewonTree(int viewIndex, bool showall = false)
        {
            IDMDataView v = DME_Editor.viewEditor.Views.Where(c => c.id == viewIndex).FirstOrDefault();
            TreeNode vnode = CheckifDataViewExitinTree(v);

            if (vnode == null)
            {
                vnode = new TreeNode(v.ViewName)
                {
                    //  Checked = true,
                    Tag = CreateNodeIDString("DV", v.id, v.id, v.ViewName, v.ViewName)
                };
                _DataViewNode.Nodes.Add(vnode);
            }

            Erinfo.Flag = Errors.Ok;
            try
            {
                //     v.DataHierarchy.ForEach(c => c.Drawn = false);

                //foreach(DataHierarchySet tr in v.DataHierarchy.Where(c=>c.ParentId==0).ToList())
                //   {
                //       FillChildNode(vnode, v.DataHierarchy[0], v.DataHierarchy, v.id, showall);
                //   }


                DrawViewOnTree(ref vnode, v.Entity[0], v.Entity, v.id, showall);
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error in Showing View on Tree ({ex.Message}) ");
                Erinfo.Flag = Errors.Failed;
                Erinfo.Ex = ex;
            }

            return Erinfo;

        }
        private IErrorsInfo DrawViewOnTree(ref TreeNode node, EntityStructure p, List<EntityStructure> c, int viewindex, bool showall = false)
        {
            Erinfo.Flag = Errors.Ok;
            List<EntityStructure> cr = c.Where(cx => (cx.Id != p.Id) && (cx.ParentId == p.Id) && (cx.ViewID == p.ViewID)).ToList();

            try
            {
                foreach (EntityStructure i in cr)
                {


                    TreeNode n = new TreeNode(i.EntityName)
                    {
                        // Checked = false,
                        Tag = CreateNodeIDString("DM", viewindex, i.Id, i.EntityName, i.EntityName)
                    };
                    node.Nodes.Add(n);



                    i.Drawn = true;
                    p.Drawn = true;

                    List<EntityStructure> tr = c.Where(tc => (tc.ParentId == i.Id) && (tc.ViewID == i.ViewID)).ToList();
                    if (tr.Count > 0)
                    {

                        DrawViewOnTree(ref n, i, c, viewindex, true);
                    }



                }

            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error in Filling Child Node View ({ex.Message}) ");
                Erinfo.Flag = Errors.Failed;
                Erinfo.Ex = ex;
            }
            return Erinfo;

        }
        public IErrorsInfo ShowTableonTree(int viewIndex, int Tableindex, bool showall = false)
        {
            IDMDataView v = DME_Editor.viewEditor.Views.Where(c => c.id == viewIndex).FirstOrDefault();

            TreeNode vnode = CheckifDataViewExitinTree(v);
            EntityStructure tablev = v.Entity.Where(c => (c.Id == Tableindex) && (c.ParentId == v.Entity[0].Id)).FirstOrDefault();
            List<EntityStructure> tablevChilds = v.Entity.Where(c => (c.ParentId == Tableindex)).ToList();
            TreeNode newtab;
            Erinfo.Flag = Errors.Ok;
            try
            {


                newtab = new TreeNode(tablev.EntityName)
                {
                    // Checked = true,
                    Tag = CreateNodeIDString("DM", v.id, tablev.Id, tablev.EntityName, tablev.EntityName)
                };
                vnode.Nodes.Add(newtab);


                FillChildNode(newtab, tablev, tablevChilds, v.id, showall);
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error in Showing View on Tree ({ex.Message}) ");
                Erinfo.Flag = Errors.Failed;
                Erinfo.Ex = ex;
            }

            return Erinfo;

        }
        private IErrorsInfo AddNodes(List<EntityStructure> cr, TreeNode node, List<EntityStructure> c, int viewindex, bool showall = false)
        {
            Erinfo.Flag = Errors.Ok;
            try
            {
                foreach (EntityStructure i in cr)
                {

                    TreeNode n1 = new TreeNode(i.EntityName)
                    {
                        //Checked = i.Show,
                        Tag = CreateNodeIDString("DM", viewindex, i.Id, i.EntityName, i.EntityName)
                    };
                    node.Nodes.Add(n1);


                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error in Filling Child Node View ({ex.Message}) ");
                Erinfo.Flag = Errors.Failed;
                Erinfo.Ex = ex;
            }
            return Erinfo;

        }
        private IErrorsInfo FillChildNode(TreeNode node, EntityStructure p, List<EntityStructure> c, int viewindex, bool showall = false)
        {
            Erinfo.Flag = Errors.Ok;
            List<EntityStructure> cr = c.Where(cx => (cx.Id != p.Id) && (cx.ParentId == p.Id)).ToList();

            try
            {
                AddNodes(cr, node, c, viewindex, true);
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error in Filling Child Node View ({ex.Message}) ");
                Erinfo.Flag = Errors.Failed;
                Erinfo.Ex = ex;
            }
            return Erinfo;

        }
        private IErrorsInfo ClearChilds()
        {

            Erinfo.Flag = Errors.Ok;
            IDMDataView v = DME_Editor.viewEditor.GetView(CurrentNode.NodeIndex);
            var n = CurrentNode.CurrNode;
            EntityStructure current = v.Entity.Where(m => m.Id == CurrentNode.id).FirstOrDefault();
            try
            {
                n.Nodes.Clear();


                current.Relations.Clear();
                v.Entity.RemoveAll(m => m.ParentId == current.Id);

            }
            catch (Exception ex)
            {

                Erinfo.Ex = ex;
                Erinfo.Message = $" Error Clearing Data Childs ({ex.Message}";
                Erinfo.Flag = Errors.Failed;
            }
            return Erinfo;
        }
        public IErrorsInfo ClearChildsfromTree(TreeNode n, int viewIndex)
        {
            //IDMDataView v = DME_Editor.viewEditor.Views.Where(c => c.id == viewIndex).FirstOrDefault();
            Erinfo.Flag = Errors.Ok;
            try
            {
                foreach (TreeNode child in n.Nodes)
                {

                    NodeID nid = GetNodeID(child.Tag.ToString());
                    //  DME_Editor.viewEditor.RemoveFromDataView( ref v, nid.id);



                }
                n.Nodes.Clear();
            }
            catch (Exception ex)
            {
                Erinfo.Ex = ex;

                Erinfo.Flag = Errors.Failed;
            }

            return Erinfo;
        }
        private IErrorsInfo GenerateChildsNodes()
        {
            Erinfo.Flag = Errors.Ok;

            IDMDataView v = DME_Editor.viewEditor.GetView(CurrentNode.NodeIndex);
            var n = CurrentNode.CurrNode;
            List<EntityStructure> newchilds = new List<EntityStructure>();
            EntityStructure current = v.Entity.Where(m => m.Id == CurrentNode.id).FirstOrDefault();
            try
            {
                ClearChilds();

            }
            catch (Exception ex)
            {

                Erinfo.Ex = ex;
                Erinfo.Message = $" Error Clearing Data Childs ({ex.Message}";
                Erinfo.Flag = Errors.Failed;
            }
            try
            {

                IRDBSource ds = (IRDBSource)DME_Editor.GetDataSource(v.MainDataSourceID);

                Erinfo = DME_Editor.viewEditor.GenerateDataViewForChildNode(ds, CurrentNode.id, n.Text, ds.Dataconnection.ConnectionProp.SchemaName, null, v.id, ref newchilds);

                // v.DataHierarchy.AddRange(newchilds);
                Erinfo = FillChildNode(n, current, v.Entity, v.id, true);


            }
            catch (Exception ex)
            {

                Erinfo.Ex = ex;
                Erinfo.Message = $" Error getting new Data Childs ({ex.Message}";
                Erinfo.Flag = Errors.Failed;
            }

            return Erinfo;
        }
        private IErrorsInfo RemoveTableFromTree()
        {
            Erinfo.Flag = Errors.Ok;

            IDMDataView v = DME_Editor.viewEditor.GetView(CurrentNode.NodeIndex);
            var n = CurrentNode.CurrNode;
            EntityStructure current = v.Entity.Where(m => m.Id == CurrentNode.id).FirstOrDefault();
            try
            {
                ClearChilds();
                v.Entity.Remove(current);
                Tree.Nodes.Remove(n);

            }
            catch (Exception ex)
            {

                Erinfo.Ex = ex;
                Erinfo.Message = $" Error removing Table From View ({ex.Message}";
                Erinfo.Flag = Errors.Failed;
            }

            return Erinfo;
        }
        private TreeNode CheckifDataViewExitinTree(IDMDataView v)
        {
            // return _DataViewNode.Nodes.Find(v.ViewName, true).FirstOrDefault();
            return _DataViewNode.Nodes.Cast<TreeNode>().Where(n => n.Text == v.ViewName).FirstOrDefault();

        }
        private IErrorsInfo FillEntites(string dsname)
        {
            Erinfo.Flag = Errors.Ok;
            Logger.WriteLog($"Filling Database Entites ) ");
            try
            {
                IRDBSource ds = (IRDBSource)DME_Editor.GetDataSource(dsname);
                if (ds != null)
                {
                   
                    if (ds.ConnectionStatus == System.Data.ConnectionState.Open)
                    {
                        ds.GetEntitesList();
                        TreeNode enti = CurrentNode.CurrNode.Nodes[0];
                        int i = 0;
                        foreach (string tb in ds.Entities)
                        {

                            TreeNode ent = new TreeNode(tb)
                            {
                                Tag = CreateNodeIDString("EN", 0, i, tb, dsname),
                                Checked = false
                            };
                            i += 1;
                            ent.SelectedImageIndex=99;
                            ent.ImageIndex = 99;

                            enti.Nodes.Add(ent);



                        }
                    }
                   

                }
                else
                {
                    MessageBox.Show(Erinfo.Ex.Message);
                }


            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error in Filling Database Entites ({ex.Message}) ");
                Erinfo.Flag = Errors.Failed;
                Erinfo.Ex = ex;
            }
            return Erinfo;

        }
        public IErrorsInfo RemoveDataView(string ViewName)
        {
            Erinfo.Flag = Errors.Ok;
            try
            {
                if (MessageBox.Show("Do you want to remove " + ViewName + "  View ?", "DB Engine", MessageBoxButtons.OKCancel) == DialogResult.OK)
                {
                    int vid = DME_Editor.viewEditor.Views.Where(c => c.ViewName == ViewName).FirstOrDefault().id;
                    DME_Editor.viewEditor.Views.Remove(DME_Editor.viewEditor.Views.Where(c => c.ViewName == ViewName).FirstOrDefault());
                    Tree.Nodes.Remove(GetViewMainNode(ViewName));
                    ClearChildsfromTree(GetViewMainNode(ViewName), vid);
                }
            }
            catch (Exception ex)
            {

                Erinfo.Flag = Errors.Failed;
                Erinfo.Ex = ex;
                MessageBox.Show("Error removing View", "DB Engine");
                Logger.WriteLog($"Error in removing View ({ex.Message}) ");

            }


            return Erinfo;
        }
        public IErrorsInfo SaveViewToFile()
        {
            Erinfo.Flag = Errors.Ok;
            try
            {

                DME_Editor.viewEditor.WriteDataViewFile(CurrentNode.NodeIndex, CurrentNode.description);
                MessageBox.Show("Changes Saved Successfuly", "DB Engine");
            }
            catch (Exception ex)
            {

                Erinfo.Flag = Errors.Failed;
                Erinfo.Ex = ex;
                MessageBox.Show("Error Saving View", "DB Engine");
                Logger.WriteLog($"Error in saving View ({ex.Message}) ");

            }


            return Erinfo;

        }
        public IErrorsInfo LoadViewFromFile(string viewfilename)
        {
            Erinfo.Flag = Errors.Ok;
            IDMDataView MyDataView = new DMDataView(viewfilename, ViewType.Table);
            try
            {
                MyDataView = DME_Editor.viewEditor.ReadDataViewFile(viewfilename);
                if (DME_Editor.viewEditor.Views.Where(c => c.ViewName == MyDataView.ViewName).Count() == 0)
                {
                    DME_Editor.viewEditor.UpdateDataViewIndex(ref MyDataView);

                    DME_Editor.viewEditor.Views.Add(MyDataView);
                }
                else
                {
                    if (MessageBox.Show("Do you want to over write th existing View?", "DB Engine", MessageBoxButtons.OKCancel) == DialogResult.OK)
                    {
                        DME_Editor.viewEditor.Views.Remove(DME_Editor.viewEditor.Views.Where(c => c.ViewName == MyDataView.ViewName).FirstOrDefault());
                        DME_Editor.viewEditor.UpdateDataViewIndex(ref MyDataView);
                        ClearChildsfromTree(GetViewMainNode(MyDataView.ViewName), MyDataView.id);
                        DME_Editor.viewEditor.Views.Add(MyDataView);
                    }
                }



                Logger.WriteLog($"Created Table Object");



                Logger.WriteLog($"Reset Datasource");

                ShowViewonTree(MyDataView.id, false);

            }
            catch (Exception ex)
            {

                Erinfo.Flag = Errors.Failed;
                Erinfo.Ex = ex;
                Logger.WriteLog($"Error in Loading View from file ({ex.Message}) ");

            }


            return Erinfo;

        }
        #endregion
        #region"NOSQL MEnu Handling"
        public IErrorsInfo FillNOSQLNode(TreeNode node)
        {
            Erinfo.Flag = Errors.Ok;
            Logger.WriteLog($"Filling Database Node View) ");
            try
            {
                foreach (ConnectionProperties i in DME_Editor.ConfigEditor.DataConnections.Where(c => c.Category == DatasourceCategory.NOSQL))
                {
                    TreeNode n = new TreeNode(i.ConnectionName)
                    {
                        Tag = CreateNodeIDString("NS", 0, i.ID, i.ConnectionName, i.ConnectionName) // "DS;" + i.ConnectionName
                    };
                    node.Nodes.Add(n);
                    TreeNode dbn = new TreeNode("Entity");
                    dbn.Tag = CreateNodeIDString("NQ", 0, 0, "ENTITY", i.ConnectionName);
                    n.Nodes.Add(dbn);
                    // foreach(string t in )

                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error in Filling NOSQL Node View ({ex.Message}) ");
                Erinfo.Flag = Errors.Failed;
                Erinfo.Ex = ex;
            }
            return Erinfo;
        }
        #endregion
        #region"Database Menu Handling"
        public IErrorsInfo FillDataBaseNode(TreeNode node)
        {
            Erinfo.Flag = Errors.Ok;
            Logger.WriteLog($"Filling Database Node View) ");
            try
            {
                foreach (ConnectionProperties i in DME_Editor.ConfigEditor.DataConnections.Where(c => c.Category == DatasourceCategory.RDBMS && c.Drawn==false) )
                {

                    CreateDBNode(node, i.ID, i.ConnectionName);
                    i.Drawn = true;

                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error in Filling Database Node View ({ex.Message}) ");
                Erinfo.Flag = Errors.Failed;
                Erinfo.Ex = ex;
            }
            return Erinfo;
        }
        public  TreeNode CreateDBNode(TreeNode parentnode,int id,string ConnectionName)
        {
            TreeNode n = new TreeNode(ConnectionName)
            {
                Tag = CreateNodeIDString("DB", 0, id, ConnectionName, ConnectionName) // "DS;" + i.ConnectionName
            };
            parentnode.Nodes.Add(n);
            TreeNode dbn = new TreeNode("Entity");
             var a= GetImageIndexFromConnectioName(n, ConnectionName);
            var x = GetImageIndex(dbn, "databaseentities.ico");
            dbn.Tag = CreateNodeIDString("DE", 0, 0, "ENTITY", ConnectionName);
            n.Nodes.Add(dbn);
            return n;
        }
        private void DataEntitymenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            ToolStripItem item = e.ClickedItem;
            string[] args = { "New View", CurrentNode.description, null };
            dataEntitymenu.Hide();
            string tbname = CurrentNode.description;
            IRDBSource ds = (IRDBSource)DME_Editor.GetDataSource(CurrentNode.misc);
            string vname = "";
            List<string> itvalues = new List<string>();
            foreach (IDMDataView c in DME_Editor.viewEditor.Views)
            {
                itvalues.Add(c.ViewName);

            }
            switch (item.Text)
            {
                case "Creat View":
                    // Get View Name

                    if (controlEditor.InputBox("New View", "Please enter View Name", ref vname) == DialogResult.OK)
                    {
                        if (vname != null)
                        {
                            IDMDataView v = DME_Editor.viewEditor.GenerateView(vname, ds);
                            int x=DME_Editor.viewEditor.AddEntitytoDataView(ds, tbname, ds.Dataconnection.ConnectionProp.SchemaName, null, v.id);
                            if (x > -1)
                            {
                                ShowViewonTree(v.id, true);
                            }
                            else
                            {
                                MessageBox.Show("Could not find Child Entities/Tables Found for this", "DB engine");
                            }
                          
                        }
                    }

                    // Generate vi 
                    //

                    //addin = Visutil.ShowFormFromAddin(Util.AddIns.Where(x => x.ParentName == Util.Config.DSEntryFormName).FirstOrDefault().DllPath, Util.Config.DSEntryFormName, DME_Editor, args, null);
                    break;

                case "Link Entity to View":

                    if (controlEditor.InputComboBox("Link Entity/Table to View", "Please Select View Name", itvalues, ref vname) == DialogResult.OK)
                    {
                        if (vname != null)
                        {
                            IDMDataView v = DME_Editor.viewEditor.GetView(vname);
                            int i = DME_Editor.viewEditor.AddEntitytoDataView(ds, tbname, ds.Dataconnection.ConnectionProp.SchemaName, null, v.id);

                            ShowTableonTree(v.id, i);
                        }
                    }
                    break;
               


            }
        }
        private void DatasourceConnmenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            ToolStripItem item = e.ClickedItem;
            string[] args = { "New View", CurrentNode.description, null };
            datasourceConnmenu.Hide();
            switch (item.Text)
            {
                case "New/Edit Data Source":
                    addin = Visutil.ShowFormFromAddin(Visutil.LLoader.AddIns.Where(x => x.ObjectName == DME_Editor.ConfigEditor.Config.DSEntryFormName).FirstOrDefault().DllPath, DME_Editor.ConfigEditor.Config.DSEntryFormName, DME_Editor, args, null);
                    break;
               



            }
        }
        private void Datasourcemenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            ToolStripItem item = e.ClickedItem;
            IRDBSource ds = (IRDBSource)DME_Editor.GetDataSource(CurrentNode.CurrNode.Text);
            string[] args = { null };
            datasourcemenu.Hide();
            string vname = "";
            List<string> itvalues = new List<string>();
            foreach (IDMDataView c in DME_Editor.viewEditor.Views)
            {
                itvalues.Add(c.ViewName);

            }
            switch (item.Text)
            {
                case "Delete":
                    string retval = "";
                    if (controlEditor.InputBox("Confirmation", "Are you sure you want to Delete this Data Source?", ref retval) == DialogResult.OK)
                    {
                        DME_Editor.DataSources.Remove(ds);
                        Tree.Nodes.Remove(CurrentNode.CurrNode);
                        ConnectionProperties cn = DME_Editor.ConfigEditor.DataConnections.Where(x => x.ConnectionName == CurrentNode.CurrNode.Text).FirstOrDefault();
                        DME_Editor.ConfigEditor.DataConnections.Remove(cn);
                        DME_Editor.ConfigEditor.SaveDataconnectionsValues();
                    }
                    break;
                case "New View":
                    addin = Visutil.ShowUserControlFromAddin("Uc_DataViewEditor", DME_Editor, args, null);

                    break;
                case "Get Entities/Tables":
                    FillEntites(CurrentNode.CurrNode.Text);

                    break;
                case "Add Checked Entites/Tables to a View":
                    var list = new List<TreeNode>();
                    LookupChecks(CurrentNode.CurrNode.Nodes, list);

                    if (list.Count > 0)
                    {
                        if (controlEditor.InputComboBox("Link Checked Entities/Tables to View", "Please Select View Name", itvalues, ref vname) == DialogResult.OK)
                        {
                            if (vname != null)
                            {
                                IDMDataView v = DME_Editor.viewEditor.GetView(vname);
                                foreach (TreeNode n in list)
                                {
                                    NodeID nd = GetNodeID(n);
                                    IRDBSource ds1 = (IRDBSource)DME_Editor.GetDataSource(nd.misc);
                                    if (DME_Editor.viewEditor.GetEntityFromView(nd.description, v.id) == null)
                                    {
                                        int i = DME_Editor.viewEditor.AddEntitytoDataView(ds1, nd.description, ds1.Dataconnection.ConnectionProp.SchemaName, null, v.id);
                                        ShowTableonTree(v.id, i);
                                    }

                                }



                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("No Entities/Tables Selected !!!", "DB engine");
                    }
                    break;


            }
        }
        #endregion
        #region "DataView Menu Handling"
        private void Dataviewhdrmenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            ToolStripItem item = e.ClickedItem;
            string[] args = { "New View", CurrentNode.description, null };
            dataviewhdrmenu.Hide();
            switch (item.Text)
            {
                case "New View":
                    addin = Visutil.ShowUserControlFromAddin("Uc_DataViewEditor", DME_Editor, args, null);

                    break;
                case "Load View":
                    OpenFileDialog openFileDialog1 = new OpenFileDialog();
                    openFileDialog1.InitialDirectory = DME_Editor.ConfigEditor.ExePath;
                    openFileDialog1.CheckFileExists = true;
                    DialogResult rs = openFileDialog1.ShowDialog();
                    if (rs == DialogResult.OK)
                    {
                        LoadViewFromFile(openFileDialog1.FileName);

                    }
                    break;

            }
        }
        private void Dataviewmenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            ToolStripItem item = e.ClickedItem;
            string[] args = { "New View", CurrentNode.description, null };
            dataviewmenu.Hide();
            switch (item.Text)
            {
                //case "New View":
                //    addin = Visutil.ShowUserControlFromAddin("Uc_DataViewEditor", DME_Editor, args, null);

                //    break;
                //case "Load View":
                //    OpenFileDialog openFileDialog1 = new OpenFileDialog();
                //    //openFileDialog1.InitialDirectory = Visutil.FileLocation;
                //    //openFileDialog1.CheckFileExists = true;
                //    DialogResult rs = openFileDialog1.ShowDialog();
                //    if (rs == DialogResult.OK)
                //    {
                //        LoadViewFromFile(openFileDialog1.FileName);

                //    }
                //    break;
                case "Save View":
                    SaveViewToFile();
                    break;
                case "Remove View":
                    RemoveDataView(CurrentNode.description);
                    break;
                case "Remove Checked Tables":
                    break;
            }
        }
        private void DataTablemenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            ToolStripItem item = e.ClickedItem;
            datatablemenu.Hide();
            switch (item.Text)
            {

                case "New Table":
                    break;
                case "Edit Table":
                    break;
                case "Remove Table":
                    RemoveTableFromTree();
                    break;
                case "Get Child Tables":

                    GenerateChildsNodes();
                    break;
                case "Remove Child Tables":
                    ClearChilds();
                    break;
                case "Save View":
                    SaveViewToFile();
                    break;
            }
        }

        #endregion
        #region "File Menu Handling"
        private void FileManagermenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            ToolStripItem item = e.ClickedItem;
            fileManagermenu.Hide();
            switch (item.Text)
            {
                case "Add File":
                    OpenFileDialog openFileDialog1 = new System.Windows.Forms.OpenFileDialog()
                    {

                        Title = "Browse Text Files",

                        CheckFileExists = true,
                        CheckPathExists = true,

                        DefaultExt = "txt",
                        Filter = "txt files (*.txt)|*.txt|csv files (*.csv)|*.csv|xls files (*.xls)|*.xls|All files (*.*)|*.*",
                        FilterIndex = 2,
                        RestoreDirectory = true

                        //ReadOnlyChecked = true,
                        //ShowReadOnly = true
                    };
                    openFileDialog1.InitialDirectory = DME_Editor.ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.DataFiles).FirstOrDefault().FolderPath ;
                    openFileDialog1.Multiselect = true;
                    DialogResult result = openFileDialog1.ShowDialog();
                    
                    if (result == DialogResult.OK) // Test result.
                    {
                        foreach (String file in openFileDialog1.FileNames)
                        {
                           
                            ConnectionProperties f = new ConnectionProperties
                            {
                                FileName = Path.GetFileName(file),
                                FilePath = Path.GetDirectoryName(file),
                                Ext = Path.GetExtension(file),
                                ConnectionName = Path.GetFileName(file)



                                //Fields = new List<EntityField>()
                            };
                            switch (f.Ext.ToLower())
                            {
                                case "txt":
                                    f.DatabaseType = DataSourceType.Text;
                                    break;
                                case "csv":
                                    f.DatabaseType = DataSourceType.CVS;
                                    break;
                                case "xls":
                                case "xlsx":
                                    f.DatabaseType = DataSourceType.Xls;
                                    break;
                                default:
                                    f.DatabaseType = DataSourceType.Text;
                                    break;
                            }
                            f.Category = DatasourceCategory.FILE;

                            CreateFileNode(_FilesNode, f);
                            DME_Editor.ConfigEditor.DataConnections.Add(f);
                            DME_Editor.GetDataSource(f.FileName);
                        }
                    
                        DME_Editor.ConfigEditor.SaveDataconnectionsValues();

                    }
                    break;
            }
        }
        public IErrorsInfo AddNewFileDataSourceGUI()
        {
            try
            {
                OpenFileDialog openFileDialog1 = new System.Windows.Forms.OpenFileDialog()
                {

                    Title = "Browse Text Files",

                    CheckFileExists = true,
                    CheckPathExists = true,

                    DefaultExt = "txt",
                    Filter = "txt files (*.txt)|*.txt|csv files (*.csv)|*.csv",
                    FilterIndex = 2,
                    RestoreDirectory = true

                    //ReadOnlyChecked = true,
                    //ShowReadOnly = true
                };
                openFileDialog1.InitialDirectory = DME_Editor.ConfigEditor.ConfigPath;
                DialogResult result = openFileDialog1.ShowDialog();
                if (result == DialogResult.OK) // Test result.
                {

                    ConnectionProperties f = new ConnectionProperties
                    {
                        FileName = Path.GetFileName(openFileDialog1.FileName),
                        FilePath = Path.GetDirectoryName(openFileDialog1.FileName),
                        Ext = Path.GetExtension(openFileDialog1.FileName),
                        ConnectionName = Path.GetFileName(openFileDialog1.FileName)



                        //Fields = new List<EntityField>()
                    };
                    switch (f.Ext.ToLower())
                    {
                        case "txt":
                            f.DatabaseType = DataSourceType.Text;
                            break;
                        case "csv":
                            f.DatabaseType = DataSourceType.CVS;
                            break;
                        case "xls":
                            f.DatabaseType = DataSourceType.Xls;
                            break;
                        default:
                            f.DatabaseType = DataSourceType.Text;
                            break;
                    }
                    f.Category = DatasourceCategory.FILE;

                    CreateFileNode(_FilesNode, f);
                    DME_Editor.ConfigEditor.DataConnections.Add(f);
                    DME_Editor.GetDataSource(f.FileName);
                    DME_Editor.ConfigEditor.SaveDataconnectionsValues();

                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error in Adding File  ({ex.Message}) ");
                Erinfo.Flag = Errors.Failed;
                Erinfo.Ex = ex;
            }
            return Erinfo;

        }
        private void Filemenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            ToolStripItem item = e.ClickedItem;
            filemenu.Hide();
            string[] args = { "Show File", CurrentNode.description, null };
            switch (item.Text)
            {

                case "Show":
                    IDM_Addin addin = Visutil.ShowUserControlFromAddinOnControl("uc_txtfileManager",  Visutil.DisplayPanel, DME_Editor, args, Passedarguments);
                    break;
            }
        }
        private void CreateFileNode(TreeNode node, IConnectionProperties i)
        {

            TreeNode n = new TreeNode(i.FileName)
            {
                Tag = CreateNodeIDString("FE", 0, i.ID, i.FileName, i.FilePath) // "DS;" + i.ConnectionName
            };
            node.Nodes.Add(n);

        }
        public IErrorsInfo FillFileNode(TreeNode node)
        {
            Erinfo.Flag = Errors.Ok;
            Logger.WriteLog($"Filling File Node View) ");
            try
            {
                foreach (IConnectionProperties i in DME_Editor.ConfigEditor.DataConnections.Where(c => c.Category == DatasourceCategory.FILE))
                {
                    TreeNode n = new TreeNode(i.FileName)
                    {
                        Tag = CreateNodeIDString("FE", 0, i.ID, i.FileName, i.FilePath) // "DS;" + i.ConnectionName
                    };
                    node.Nodes.Add(n);
                    // ds = new DataSource();

                    //DME_Editor.DataSources.Add(ds);


                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error in Filling File Node View ({ex.Message}) ");
                Erinfo.Flag = Errors.Failed;
                Erinfo.Ex = ex;
            }
            return Erinfo;
        }
        #endregion
        #region "WepApi Menu Handling"
        private void WebAPiManagermenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            ToolStripItem item = e.ClickedItem;
            MappingManagermenu.Hide();
            string[] args = { "Add WebAPI", null, null };
            switch (item.Text)
            {
                case "Add WebAPI":
                    IDM_Addin addin = Visutil.ShowUserControlFromAddinOnControl("uc_AddWebAPI", Visutil.DisplayPanel, DME_Editor, args, Passedarguments);
                    break;
            }
        }

        private void WebApimenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            throw new NotImplementedException();
        }
        public IErrorsInfo FillWebAPINode(TreeNode node)
        {
            Erinfo.Flag = Errors.Ok;
            Logger.WriteLog($"Filling WebAPI Node View) ");
            try
            {
                foreach (IConnectionProperties i in DME_Editor.ConfigEditor.DataConnections.Where(c => c.Category == DatasourceCategory.WEBAPI))
                {
                    TreeNode n = new TreeNode(i.FileName)
                    {
                        Tag = CreateNodeIDString("WE", 0, i.ID, i.ConnectionName, i.ConnectionString) // "DS;" + i.ConnectionName
                    };
                    node.Nodes.Add(n);
                  


                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error in WebAPI  Node View ({ex.Message}) ");
                Erinfo.Flag = Errors.Failed;
                Erinfo.Ex = ex;
            }
            return Erinfo;
        }
        #endregion
        #region"Mapping Handling"
        private void MappingManagermenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            ToolStripItem item = e.ClickedItem;
            MappingManagermenu.Hide();
            string[] args = { "Add Mapping", null, null };
            switch (item.Text)
            {
                case "Add Mapping":
                    IDM_Addin addin = Visutil.ShowUserControlFromAddinOnControl("uc_MappingEntities",  Visutil.DisplayPanel, DME_Editor, args, Passedarguments);
                    break;
            }
        }
        private void Mappingmenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            ToolStripItem item = e.ClickedItem;
            MappingManagermenu.Hide();
            string[] args = { "Show Mapping", CurrentNode.description, null };
            switch (item.Text)
            {
                case "Show":
                    IDM_Addin addin = Visutil.ShowUserControlFromAddinOnControl("uc_MappingEntities", Visutil.DisplayPanel, DME_Editor, args, Passedarguments);
                    break;
            }
        }
        public IErrorsInfo FillMappingNode(TreeNode node)
        {
            Erinfo.Flag = Errors.Ok;
            Logger.WriteLog($"Filling File Node View) ");
            try
            {
                foreach (IMapping_rep i in DME_Editor.ConfigEditor.Mappings)
                {
                    TreeNode n = new TreeNode(i.MappingName)
                    {
                        Tag = CreateNodeIDString("ME", 0, i.id, i.MappingName, i.MappingName) // "DS;" + i.ConnectionName
                    };
                    node.Nodes.Add(n);

                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error in Filling Mapping Node View ({ex.Message}) ");
                Erinfo.Flag = Errors.Failed;
                Erinfo.Ex = ex;
            }
            return Erinfo;
        }
        #endregion
        #region "Drag and Drop"
        // Determine whether one node is a parent 
        // or ancestor of a second node.
        private bool ContainsNode(TreeNode node1, TreeNode node2)
        {
            // Check the parent node of the second node.
            if (node2.Parent == null) return false;
            if (node2.Parent.Equals(node1)) return true;

            // If the parent node is not null or equal to the first node, 
            // call the ContainsNode method recursively using the parent of 
            // the second node.
            return ContainsNode(node1, node2.Parent);
        }
        //------------ Drag and Drop -----------------
        private void Tree_DragLeave(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void Tree_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.AllowedEffect;
        }

        private void Tree_DragDrop(object sender, DragEventArgs e)
        {
            // Retrieve the client coordinates of the drop location.
            Point targetPoint = Tree.PointToClient(new Point(e.X, e.Y));

            // Retrieve the node at the drop location.
            TreeNode targetNode = Tree.GetNodeAt(targetPoint);

            // Retrieve the node that was dragged.
            TreeNode draggedNode = (TreeNode)e.Data.GetData(typeof(TreeNode));
            if (GetNodeID(targetNode).nodeType == "DV")
            {
                // Confirm that the node at the drop location is not 
                // the dragged node or a descendant of the dragged node.
                IDMDataView v = DME_Editor.viewEditor.GetView(GetNodeID(targetNode).NodeIndex);
                IRDBSource ds = (IRDBSource)DME_Editor.GetDataSource(v.MainDataSourceID);
                if (!draggedNode.Equals(targetNode) && !ContainsNode(draggedNode, targetNode))
                {
                    // If it is a move operation, remove the node from its current 
                    // location and add it to the node at the drop location.
                    if (e.Effect == DragDropEffects.Move)
                    {

                        int tabid = DME_Editor.viewEditor.AddEntitytoDataView(ds, draggedNode.Text.ToUpper(), ds.GetSchemaName(), "", v.id);
                        ShowTableonTree(v.id, tabid, true);

                        //draggedNode.Remove();
                        //targetNode.Nodes.Add(draggedNode);
                    }

                    // If it is a copy operation, clone the dragged node 
                    // and add it to the node at the drop location.
                    //else if (e.Effect == DragDropEffects.Copy)
                    //{
                    //    targetNode.Nodes.Add((TreeNode)draggedNode.Clone());
                    //}

                    // Expand the node at the location 
                    // to show the dropped node.
                    targetNode.Expand();
                }
            }

        }
        private void Tree_ItemDrag(object sender, ItemDragEventArgs e)
          
        {
            if (CurrentNode != null)
            {
                if (CurrentNode.nodeType == "EN")
                {
                    // Move the dragged node when the left mouse button is used.
                    if (e.Button == MouseButtons.Left)
                    {
                        Tree.DoDragDrop(e.Item, DragDropEffects.Move);
                    }

                    // Copy the dragged node when the right mouse button is used.
                    else if (e.Button == MouseButtons.Right)
                    {
                        Tree.DoDragDrop(e.Item, DragDropEffects.Copy);
                    }
                }

            }
            

        }

        private void Tree_DragOver(object sender, DragEventArgs e)
        {
            // Retrieve the client coordinates of the mouse position.
            Point targetPoint = Tree.PointToClient(new Point(e.X, e.Y));

            // Select the node at the mouse position.
            Tree.SelectedNode = Tree.GetNodeAt(targetPoint);
        }
        #endregion
        private int GetImageIndexFromConnectioName(string Connectioname)
        {
            try
            {
                string drname = DME_Editor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == Connectioname).FirstOrDefault().DriverName;
                if (drname != null)
                {
                    string drversion = DME_Editor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == Connectioname).FirstOrDefault().DriverVersion;
                    string iconname = DME_Editor.ConfigEditor.DataDrivers.Where(c => c.version == drversion && c.DriverClass == drname).FirstOrDefault().iconname;
                    int imgindx = Tree.ImageList.Images.IndexOfKey(iconname);
                    return imgindx;
                }
                else
                    return -1;

              
            }
            catch (Exception)
            {

                return -1;
            }
           
        }
        private int GetImageIndexFromConnectioName(TreeNode n, string Connectioname)
        {
            try
            {
                string drname = DME_Editor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == Connectioname).FirstOrDefault().DriverName;
                if (drname != null)
                {
                    string drversion = DME_Editor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == Connectioname).FirstOrDefault().DriverVersion;
                    string iconname = DME_Editor.ConfigEditor.DataDrivers.Where(c => c.version == drversion && c.DriverClass == drname).FirstOrDefault().iconname;
                    int imgindx = Tree.ImageList.Images.IndexOfKey(iconname);
                    n.ImageIndex = imgindx;
                    n.SelectedImageIndex = imgindx;//GetImageIndex(SelectIcon);
                    return imgindx;
                }
                else
                    return -1;
             
            }
            catch (Exception)
            {

                return -1;
            }

        }
        private int GetImageIndex(string imagename)
        {
            try
            {
                  int imgindx = Tree.ImageList.Images.IndexOfKey(imagename);
                return imgindx;
               // Tree.SelectedImageIndex = GetImageIndex("select.ico");
            }
            catch (Exception)
            {

                return -1;
            }

        }
        private int GetImageIndex(TreeNode n,string imagename)
        {
            try
            {
                int imgindx = Tree.ImageList.Images.IndexOfKey(imagename);
                n.ImageIndex = imgindx;
                n.SelectedImageIndex = imgindx;// GetImageIndex(SelectIcon);
                return imgindx;
            }
            catch (Exception)
            {

                return -1;
            }

        }
      
      

    }
    
    
}