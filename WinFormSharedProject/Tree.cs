using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.DataView;
using TheTechIdea.DataManagment_Engine.Editor;
using TheTechIdea.DataManagment_Engine.Vis;
using TheTechIdea.Util;

namespace TheTechIdea.Winforms.VIS
{
    public class Tree : ITree, ITreeView
    {
       
        public delegate T ObjectActivator<T>(params object[] args);
        public string CategoryIcon { get; set; } = "category.ico";
        public string SelectIcon { get; set; } = "cursor.ico";
        public IBranch CurrentBranch { get; set; }
        public IDMEEditor DMEEditor { get; set; }
        public PassedArgs args { get; set; } = new PassedArgs();
        static int pSeqID=0;
        public int SeqID
        {
            get
            {
                pSeqID += 1;
                return pSeqID;
            }
        }
        public List<IBranch> Branches { get; set; }
        ImageList images { get; set; } = new ImageList();
        public object TreeStrucure { get
            {
                return TreeV;
            }
            set 
            {
                TreeV = (TreeView)value;
                
                SetupTreeView();
            } }
        private TreeView TreeV;
        public TreeNode SelectedNode { get; set; }
        public TreeNode LastSelectedNode { get; set; }
        public int SelectedBranchID { get; set; } = 0;
        public List<int> SelectedBranchs { get; set; } = new List<int>();
        public Color SelectBackColor { get; set; } = Color.Red;
      
        public int StartselectBranchID { get; set; } = 0;
        public string CurrentNode { get; set; }
        public IVisUtil Visutil { get; set; }
        PassedArgs Passedarguments { get; set; } = new PassedArgs();
        string TreeEvent { get; set; }
        string TreeOP { get; set; }
        public Font tagFont { get; set; } = new Font("Helvetica", 8, FontStyle.Bold);
        private bool busy = false;
        IDM_Addin sender;
        WaitWndFun waitForm = new WaitWndFun();
        Thread loadthread;
        #region "Branch Handling"
        public string CheckifBranchExistinCategory(string BranchName, string pRootName)
        {
            //bool retval = false;
            List<CategoryFolder> ls = DMEEditor.ConfigEditor.CategoryFolders.Where(x => x.RootName == pRootName).ToList();
            foreach (CategoryFolder item in ls)
            {
                foreach (string f in item.items)
                {
                    if (f == BranchName)
                    {
                        return item.FolderName;
                    }
                }
            }
            return null;
        }
        public IErrorsInfo CreateBranch(IBranch Branch)
        {
            throw new NotImplementedException();
        }
        public IErrorsInfo AddBranch(IBranch ParentBranch, IBranch Branch)
        {
            try
            {
                AssemblyClassDefinition cls = DMEEditor.ConfigEditor.BranchesClasses.Where(x => x.PackageName == Branch.ToString()).FirstOrDefault();
                Branch.Name = cls.PackageName;
                TreeNode p = GetTreeNodeByTag(ParentBranch.BranchID.ToString(), TreeV.Nodes);
                TreeNode n = p.Nodes.Add(Branch.BranchText);
                if (GetImageIndex(Branch.IconImageName) == -1)
                {
                    n.ImageIndex = GetImageIndexFromConnectioName(Branch.BranchText);
                    n.SelectedImageIndex = GetImageIndexFromConnectioName(Branch.BranchText);
                }
                else
                {
                    n.ImageKey = Branch.IconImageName;
                    n.SelectedImageKey = Branch.IconImageName;
                }

                Branch.TreeEditor = this;
                n.Tag = Branch.BranchID;
                n.ContextMenuStrip = CreateMenuMethods(Branch);
                Branch.DMEEditor = DMEEditor;
                ITreeView treeView = (ITreeView)Branch;
                treeView.Visutil = Visutil;
                Branches.Add(Branch);
                if (!DMEEditor.ConfigEditor.objectTypes.Any(i => i.ObjectType == Branch.BranchClass && i.ObjectName == Branch.BranchType.ToString() + "_" + Branch.BranchClass))
                {
                    DMEEditor.ConfigEditor.objectTypes.Add(new DataManagment_Engine.Workflow.ObjectTypes { ObjectType = Branch.BranchClass, ObjectName = Branch.BranchType.ToString() + "_" + Branch.BranchClass });
                }
                if (Branch.BranchType == EnumBranchType.Entity)
                {
                    if (Branch.BranchClass == "VIEW")
                    {
                        //   DataViewDataSource dataViewDatasource = (DataViewDataSource)DMEEditor.GetDataSource(Branch.DataSourceName);
                        EntityStructure e = Branch.EntityStructure;
                        EntityStructure parententity = GetBranch(Branch.ParentBranchID).EntityStructure;
                        if (e != null && parententity != null)
                        {
                            switch (e.Viewtype)
                            {
                                case ViewType.Table:
                                    if (e.DataSourceID != parententity.DataSourceID)
                                    {
                                        n.ForeColor = Color.Black;
                                    }
                                    else
                                    {
                                        n.ForeColor = Color.Black;
                                        n.BackColor = Color.LightYellow;
                                    }

                                    break;
                                case ViewType.Query:
                                    if (e.DataSourceID != parententity.DataSourceID)
                                    {
                                        n.ForeColor = Color.Red;
                                    }
                                    else
                                    {
                                        n.ForeColor = Color.Red;
                                        n.BackColor = Color.LightYellow;
                                    }
                                    break;
                                case ViewType.Code:
                                    break;
                                case ViewType.File:
                                    if (e.DataSourceID != parententity.DataSourceID)
                                    {
                                        n.ForeColor = Color.Blue;
                                    }
                                    else
                                    {
                                        n.ForeColor = Color.Blue;
                                        n.BackColor = Color.LightYellow;
                                    }
                                    break;
                                case ViewType.Url:
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                   
                    if (Branch.EntityStructure != null)
                    {
                        if (Branch.EntityStructure.Created == false && Branch.BranchClass != "VIEW")
                        {
                            n.ForeColor = Color.Red;
                            n.BackColor = Color.LightYellow;
                        }
                    }

                }

            }
            catch (Exception ex)
            {
                string mes = "Could not Add Branch to " + ParentBranch.BranchText;
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;

        }
        public bool RemoveEntityFromCategory(string root, string foldername, string entityname)
        {

            try
            {
                CategoryFolder f = DMEEditor.ConfigEditor.CategoryFolders.Where(x => x.RootName == root && x.FolderName == foldername).FirstOrDefault();
                f.items.Remove(entityname);
                return true;
            }
            catch (Exception ex)
            {
                string mes = "";
                DMEEditor.AddLogMessage(ex.Message, "Could not remove entity from category" + mes, DateTime.Now, -1, mes, Errors.Failed);
                return false;
            };
        }
        public IErrorsInfo RemoveBranch(IBranch Branch)
        {

            try
            {

                TreeNode n = GetTreeNodeByTag(Branch.BranchID.ToString(), TreeV.Nodes);
                RemoveChildBranchs(Branch);
                Branches.Remove(Branch);
                if (n != null)
                {
                    n.Remove();
                }


                // DMEEditor.AddLogMessage("Success", "removed node and childs", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not  remove node and childs";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;

        }
        public IErrorsInfo RemoveChildBranchs(IBranch branch)
        {
            try
            {
                if (branch.ChildBranchs != null)
                {
                    if (branch.ChildBranchs.Count > 0)
                    {
                        foreach (IBranch item in branch.ChildBranchs)
                        {
                            if (branch.ChildBranchs.Count > 0)
                            {
                                RemoveBranch(item);
                            }

                            Branches.Remove(item);
                        }

                        branch.ChildBranchs.Clear();
                        TreeNode n = GetTreeNodeByTag(branch.BranchID.ToString(), TreeV.Nodes);
                        if (n != null)
                        {
                            n.Nodes.Clear();

                        }
                    }
                }




                //  DMEEditor.AddLogMessage("Success", "removed childs", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not  remove   childs";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        public IBranch GetBranch(int pID)
        {
            return Branches.Where(c => c.BranchID == pID).FirstOrDefault();
        }
        public void ShowWaiting()
        {

            waitForm.Show(Visutil.ParentForm);

        }
        public void HideWaiting()
        {
            waitForm.Close();
        }
        public void ChangeWaitingCaption(string Caption)
        {
            if (waitForm != null)
            {
                waitForm.ChangeCaption(Caption);


            }
        }
        public void AddCommentsWaiting(string comment)
        {
            if (waitForm != null)
            {
                waitForm.AddComment(comment);
            }
        }
        public IBranch GetBranchByMiscID(int pID)
        {
            return Branches.Where(c => c.MiscID == pID).FirstOrDefault();
        }
        public IErrorsInfo MoveBranchToParent(IBranch ParentBranch, IBranch CurrentBranch)
        {

            try
            {
                TreeNode ParentBranchNode = GetTreeNodeByTag(ParentBranch.BranchID.ToString(), TreeV.Nodes);
                TreeNode CurrentBranchNode = GetTreeNodeByTag(CurrentBranch.BranchID.ToString(), TreeV.Nodes);
                string foldername = CheckifBranchExistinCategory(CurrentBranch.BranchText, CurrentBranch.BranchClass);
                if (foldername != null)
                {
                    RemoveEntityFromCategory(ParentBranch.BranchClass, foldername, CurrentBranch.BranchText);
                }
                TreeV.Nodes.Remove(CurrentBranchNode);
             

                CategoryFolder CurFodler = DMEEditor.ConfigEditor.CategoryFolders.Where(y => y.RootName == ParentBranch.BranchClass).FirstOrDefault();
                if (CurFodler != null)
                {
                    if (CurFodler.items.Contains(CurrentBranch.BranchText) == false)
                    {
                        CurFodler.items.Remove(CurrentBranch.BranchText);
                    }
                }

                CategoryFolder NewFolder = DMEEditor.ConfigEditor.CategoryFolders.Where(y => y.FolderName == ParentBranch.BranchText && y.RootName == ParentBranch.BranchClass).FirstOrDefault();
                if (NewFolder != null)
                {
                    if (NewFolder.items.Contains(CurrentBranch.BranchText) == false)
                    {
                        NewFolder.items.Add(CurrentBranch.BranchText);
                    }
                }
                if ((ParentBranch.BranchType == EnumBranchType.Entity) && (ParentBranch.BranchClass == "VIEW" && CurrentBranch.BranchClass == "VIEW") && (ParentBranch.DataSourceName == CurrentBranch.DataSourceName))
                {
                    DataViewDataSource vds = (DataViewDataSource)DMEEditor.GetDataSource(CurrentBranch.DataSourceName);
                    if (vds.Entities[vds.EntityListIndex(ParentBranch.MiscID)].Id == vds.Entities[vds.EntityListIndex(CurrentBranch.MiscID)].ParentId)
                    {

                    } else
                    {
                        vds.Entities[vds.EntityListIndex(CurrentBranch.MiscID)].ParentId = vds.Entities[vds.EntityListIndex(ParentBranch.MiscID)].Id;
                    }
                  

                }
                
                    ParentBranchNode.Nodes.Add(CurrentBranchNode);
               
                DMEEditor.ConfigEditor.SaveCategoryFoldersValues();

                DMEEditor.AddLogMessage("Success", "Moved Branch successfully", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Moved Branch";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        public IErrorsInfo RemoveBranch(int id)
        {

            try
            {
                RemoveBranch(Branches.Where(x => x.BranchID == id).FirstOrDefault());
            }
            catch (Exception ex)
            {
                string mes = "Could not  remove node and childs";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;

        }
        public IErrorsInfo AddCategory(IBranch Rootbr)
        {

            try
            {
                Passedarguments = new PassedArgs();
                string foldername = "";
                Visutil.controlEditor.InputBox("Enter Category Name", "What Category you want to Add", ref foldername);
                if (foldername != null)
                {
                    if (foldername.Length > 0)
                    {
                        CategoryFolder x = DMEEditor.ConfigEditor.AddFolderCategory(foldername, Rootbr.BranchClass, foldername);
                       IBranchRootCategory f=(IBranchRootCategory) Rootbr;
                        f.CreateCategoryNode(x);
                        DMEEditor.ConfigEditor.SaveCategoryFoldersValues();

                    }
                }
                DMEEditor.AddLogMessage("Success", "Added Category", DateTime.Now, 0, null, Errors.Failed);
            }
            catch (Exception ex)
            {
                string mes = "Could not Add Category";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        public IErrorsInfo RemoveCategoryBranch(int id)
        {

            try
            {
                IBranch CategoryBranch = GetBranch(id);
                IBranch RootBranch = GetBranch(CategoryBranch.ParentBranchID);
                TreeNode CategoryBranchNode = GetTreeNodeByTag(CategoryBranch.BranchID.ToString(), TreeV.Nodes);
                var ls = Branches.Where(x => x.ParentBranchID == id).ToList();
                if (ls.Count() > 0)
                {
                    foreach (IBranch f in ls)
                    {
                        MoveBranchToParent(RootBranch, f);
                    }
                }

                TreeV.Nodes.Remove(CategoryBranchNode);
                CategoryFolder Folder = DMEEditor.ConfigEditor.CategoryFolders.Where(y => y.FolderName == CategoryBranch.BranchText && y.RootName == CategoryBranch.BranchClass).FirstOrDefault();
                DMEEditor.ConfigEditor.CategoryFolders.Remove(Folder);

                DMEEditor.ConfigEditor.SaveCategoryFoldersValues();
                DMEEditor.AddLogMessage("Success", "Removed Branch successfully", DateTime.Now, 0, null, Errors.Ok);

            }
            catch (Exception ex)
            {
                string mes = "";
                DMEEditor.AddLogMessage(ex.Message, "Could not remove category" + mes, DateTime.Now, -1, mes, Errors.Failed);

            };
            return DMEEditor.ErrorObject;

        }
        public IErrorsInfo SendActionFromBranchToBranch(IBranch ToBranch, IBranch CurrentBranch, string ActionType)
        {
            string targetBranchClass = ToBranch.GetType().Name;
            string dragedBranchClass = CurrentBranch.GetType().Name;


            try
            {

                Function2FunctionAction functionAction = DMEEditor.ConfigEditor.Function2Functions.Where(x => x.FromClass == dragedBranchClass && x.ToClass == targetBranchClass && x.ToMethod == ActionType).FirstOrDefault();
                if (functionAction != null)
                {
                    RunMethod(ToBranch, ActionType);
                }
                //   DMEEditor.AddLogMessage("Success", "Added Database Connection", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not send action to branch";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;

        }

        #endregion
        #region "Tree Creation and Method Calling"
        public IErrorsInfo RunMethod(Object branch, string MethodName)
        {

            try
            {
                Type t = branch.GetType();
                AssemblyClassDefinition cls = DMEEditor.ConfigEditor.BranchesClasses.Where(x => x.className == t.Name).FirstOrDefault();
                MethodInfo method = null;
                MethodsClass methodsClass;
                try
                {
                    methodsClass = cls.Methods.Where(x => x.Caption == MethodName).FirstOrDefault();
                }
                catch (Exception)
                {

                    methodsClass = null;
                }
                if (methodsClass != null)
                {
                    method = methodsClass.Info;
                    if (method.GetParameters().Length > 0)
                    {
                        method.Invoke(branch, new object[] { args.Objects[0].obj });
                    }
                    else
                        method.Invoke(branch, null);


                    //  DMEEditor.AddLogMessage("Success", "Running method", DateTime.Now, 0, null, Errors.Ok);
                }

            }
            catch (Exception ex)
            {
                string mes = "Could not Run Method " + MethodName;
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        public IErrorsInfo CreateRootTree()
        {
            Branches = new List<IBranch>();
            try
            {
                foreach (AssemblyClassDefinition cls in DMEEditor.ConfigEditor.BranchesClasses.OrderBy(x => x.Order))
                {
                    Type adc = DMEEditor.assemblyHandler.GetType(cls.PackageName);
                    ConstructorInfo ctor = adc.GetConstructors().First();
                    ObjectActivator<IBranch> createdActivator = GetActivator<IBranch>(ctor);
                    try
                    {
                        IBranch br = createdActivator();
                        if (br.BranchType == EnumBranchType.Root)
                        {

                            int id = SeqID;
                            br.Name = cls.PackageName;
                            TreeNode n = TreeV.Nodes.Add(br.BranchText);
                            n.Tag = id;

                            br.TreeEditor = this;
                            br.BranchID = id;
                            br.ID = id;
                            if (GetImageIndex(br.IconImageName) == -1)
                            {
                                n.ImageIndex = GetImageIndexFromConnectioName(br.BranchText);
                                n.SelectedImageIndex = GetImageIndexFromConnectioName(br.BranchText);
                            }
                            else
                            {
                                n.ImageKey = br.IconImageName;
                                n.SelectedImageKey = br.IconImageName;
                            }

                            ITreeView treeView = (ITreeView)br;
                            treeView.Visutil = Visutil;
                            n.ContextMenuStrip = CreateMenuMethods(br);

                            br.DMEEditor = DMEEditor;
                            if (!DMEEditor.ConfigEditor.objectTypes.Any(i => i.ObjectType == br.BranchClass && i.ObjectName == br.BranchType.ToString() + "_" + br.BranchClass))
                            {
                                DMEEditor.ConfigEditor.objectTypes.Add(new DataManagment_Engine.Workflow.ObjectTypes { ObjectType = br.BranchClass, ObjectName = br.BranchType.ToString() +"_"+ br.BranchClass });
                            }
                            Branches.Add(br);
                            br.CreateChildNodes();
                        }
                    }
                    catch (Exception ex)
                    {
                        DMEEditor.AddLogMessage("Error", $"Creating Tree Root Node {cls.PackageName} {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);

                    }

                }
                // DMEEditor.AddLogMessage("Success", "Create Tree", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Create Tree";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        public ContextMenuStrip CreateMenuMethods(IBranch branch)
        {

            ContextMenuStrip nodemenu = new ContextMenuStrip();
            try
            {
                nodemenu.ImageList = images;
                AssemblyClassDefinition cls = DMEEditor.ConfigEditor.BranchesClasses.Where(x => x.PackageName == branch.ToString()).FirstOrDefault();
                foreach (var item in cls.Methods.Where(y => y.Hidden == false))
                {
                    ToolStripItem  st= nodemenu.Items.Add(item.Caption);
                    nodemenu.Name = branch.ToString();
                    if (item.iconimage != null)
                    {
                        st.ImageIndex = GetImageIndex(item.iconimage);
                    }


                }
                nodemenu.ItemClicked += Nodemenu_ItemClicked;
                nodemenu.Tag = branch;
               
            }
            catch (Exception ex)
            {
                string mes = "Could not add method to menu " + branch.BranchText;
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return nodemenu;
        }
        private void Nodemenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            ContextMenuStrip n = (ContextMenuStrip)sender;
            ToolStripItem item = e.ClickedItem;
            //IBranch br  = (IBranch)n.Tag;
            n.Hide();
            IBranch br = GetBranch(SelectedBranchID);
            AssemblyClassDefinition cls = DMEEditor.ConfigEditor.BranchesClasses.Where(x => x.PackageName == item.Name).FirstOrDefault();
            RunMethod(br, item.Text);
        }
        private void Nodemenu_MouseClick(TreeNodeMouseClickEventArgs e)
        {
            ContextMenuStrip n = (ContextMenuStrip)sender;
            IBranch br = GetBranch(SelectedBranchID);
            string clicks = "";
            switch (e.Clicks)
            {
                case 1:
                    clicks = "SingleClick";
                    break;
                case 2:
                    clicks = "DoubleClick";
                    break;

                default:
                    break;
            }
            AssemblyClassDefinition cls = DMEEditor.ConfigEditor.BranchesClasses.Where(x => x.PackageName == br.Name && x.Methods.Where(y => y.DoubleClick == true || y.Click == true).Any()).FirstOrDefault();
            if (cls != null)
            {
                RunMethod(br, clicks);
            }
        }
        #endregion
        #region "Winform TreeView Setup"
        private void SetupTreeView()
        {
            sender = null;
            images = new ImageList();
            images.ImageSize = new Size(32, 32);
            images.ColorDepth = ColorDepth.Depth32Bit;
            foreach (string filename_w_path in Directory.GetFiles(DMEEditor.ConfigEditor.Config.Folders.Where(x => x.FolderFilesType == FolderFileTypes.GFX).FirstOrDefault().FolderPath, "*.ico", SearchOption.AllDirectories))
            {
                try
                {
                    string filename = Path.GetFileName(filename_w_path);

                    images.Images.Add(filename, Image.FromFile(filename_w_path));


                }
                catch (FileLoadException ex)
                {
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    DMEEditor.ErrorObject.Ex = ex;
                    DMEEditor.Logger.WriteLog($"Error Loading icons ({ex.Message})");
                }

            }
            TreeV.CheckBoxes = false;
            TreeV.ImageList = images;
            TreeV.ItemHeight = 32;
            TreeV.SelectedImageKey = SelectIcon;
            //TreeV.Dock = DockStyle.Fill;
            //TreeV.SendToBack();
            CreateDelagates();
        }
        #endregion
        #region "Misc Functions"
        public IErrorsInfo PasteEntityToDataView(DataViewDataSource ds)
        {

            try
            {
                //ds = (DataViewDataSource)DMEEditor.GetDataSource(DataView.DataViewDataSourceID);
                if (args != null)
                {
                    IBranch pbr = (IBranch)args.Objects.Where(x => x.Name == "Branch").FirstOrDefault().obj;
                    if (args.EventType == "COPYENTITY" || args.EventType == "DragandDropEntity")
                    {
                        if (args.Objects != null)
                        {
                           // IBranch pbr = (IBranch)args.Objects.Where(x => x.Name == "Branch").FirstOrDefault().obj;
                            EntityStructure entity = (EntityStructure)args.Objects.Where(x => x.Name == "Entity").FirstOrDefault().obj;
                            if (ds.CheckEntityExist(entity.EntityName))
                            {
                                DMEEditor.AddLogMessage("Fail", $"Could Not Paste Entity {entity.EntityName}, it already exist", DateTime.Now, -1, null, Errors.Failed);
                            }
                            else
                            {
                                IDataSource srcds = DMEEditor.GetDataSource(entity.DataSourceID);
                                entity = srcds.GetEntityStructure(entity, true);
                                entity.Caption = entity.EntityName;
                                entity.DatasourceEntityName = entity.EntityName;
                                entity.Created = false;
                                entity.Id = ds.NextHearId();
                                entity.ParentId = pbr.BranchID;
                                entity.ViewID = ds.DataView.ViewID;
                                ds.CreateEntityAs(entity);
                                //DataViewEntitiesNode dbent = new DataViewEntitiesNode(TreeEditor, DMEEditor, this, entity.EntityName, TreeEditor.SeqID, EnumBranchType.Entity, "entity.ico", DataView.DataViewDataSourceID, entity);
                                //TreeEditor.AddBranch(this, dbent);
                                //dbent.CreateChildNodes();
                                //ChildBranchs.Add(dbent);
                                DMEEditor.AddLogMessage("Success", $"Pasted Entity {entity.EntityName}", DateTime.Now, -1, null, Errors.Ok);
                            }

                        }
                    }
                    else
                    if (SelectedBranchs.Count > 0 && args.EventType == "COPYENTITIES")
                    {
                        foreach (int item in SelectedBranchs)
                        {
                           // IBranch pbr = (IBranch)args.Objects.Where(x => x.Name == "Branch").FirstOrDefault().obj;
                           IBranch br = GetBranch(item);
                            IDataSource srcds = DMEEditor.GetDataSource(pbr.DataSourceName);
                            if (srcds != null)
                            {
                                EntityStructure entity = srcds.GetEntityStructure(br.BranchText, true);
                                entity.Caption = entity.EntityName;
                                entity.DatasourceEntityName = entity.EntityName;
                                entity.Created = false;
                                entity.Id = ds.NextHearId();
                                entity.ParentId = pbr.ID;
                                entity.ViewID = ds.DataView.ViewID;
                                ds.CreateEntityAs(entity);
                                //DataViewEntitiesNode dbent = new DataViewEntitiesNode(TreeEditor, DMEEditor, this, entity.EntityName, TreeEditor.SeqID, EnumBranchType.Entity, "entity.ico", DataView.DataViewDataSourceID, entity);
                                //TreeEditor.AddBranch(this, dbent);
                                //dbent.CreateChildNodes();
                                //ChildBranchs.Add(dbent);
                                DMEEditor.AddLogMessage("Success", $"Pasted Entity {entity.EntityName}", DateTime.Now, -1, null, Errors.Ok);
                            }


                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string mes = "Could not Added Entity ";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        public IErrorsInfo CopySelectedEntities()
        {
            List<string> ents = new List<string>();
            if (SelectedBranchs.Count > 0)
            {

                foreach (int item in SelectedBranchs)
                {
                    IBranch br = GetBranch(item);
                    ents.Add(br.BranchText);
                    // EntityStructure = DataSource.GetEntityStructure(br.BranchText, true);

                }


                args = new PassedArgs
                {
                    ObjectName = "DATABASE",
                    ObjectType = "TABLE",
                    EventType = "COPYENTITIES",
                    ParameterString1 = "COPYENTITIES",

                };

                DMEEditor.Passedarguments = args;
            }
            else

                DMEEditor.AddLogMessage("Fail", "Could not get DataSource", DateTime.Now, -1, null, Errors.Failed);

            return DMEEditor.ErrorObject;
        
        
        }
        public int GetImageIndex(TreeNode n, string imagename)
        {
            try
            {
                int imgindx = TreeV.ImageList.Images.IndexOfKey(imagename);
                n.ImageIndex = imgindx;
                n.SelectedImageIndex = imgindx;// GetImageIndex(SelectIcon);
                return imgindx;
            }
            catch (Exception)
            {

                return -1;
            }

        }
        public TreeNode GetTreeNodeByTag(string tag, TreeNodeCollection p_Nodes)
        {
            try
            {
                foreach (TreeNode node in p_Nodes)
                {
                    if (node.Tag.ToString() == tag)
                    {
                        return node;
                    }

                    if (node.Nodes.Count > 0)
                    {
                        var result = GetTreeNodeByTag(tag, node.Nodes);
                        if (result != null)
                        {
                            return result;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return null;


            }
           

            return null;// TreeV.Nodes.Cast<TreeNode>().Where(n => n.Tag.ToString() == tag).FirstOrDefault();
        }
        public TreeNode GetTreeNodeByCaption(string Caption, TreeNodeCollection p_Nodes)
        {
            foreach (TreeNode node in p_Nodes)
            {
                if (node.Text == Caption)
                {
                    return node;
                }

                if (node.Nodes.Count > 0)
                {
                    var result = GetTreeNodeByCaption(Caption, node.Nodes);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }
            return null;
        }
        public int GetImageIndexFromConnectioName( string Connectioname)
        {
            try
            {
                string drname = null ;
                string iconname=null;
                ConnectionDriversConfig connectionDrivers;
                if (DMEEditor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == Connectioname).Any())
                {
                     drname = DMEEditor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == Connectioname).FirstOrDefault().DriverName;
                }
                
                if (drname != null)
                {
                    string drversion = DMEEditor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == Connectioname).FirstOrDefault().DriverVersion;
                    if (DMEEditor.ConfigEditor.DataDriversClasses.Where(c => c.version == drversion && c.DriverClass == drname).Any())
                    {

                         connectionDrivers = DMEEditor.ConfigEditor.DataDriversClasses.Where(c => c.version == drversion && c.DriverClass == drname).FirstOrDefault();
                        if (connectionDrivers != null)
                        {
                            iconname = connectionDrivers.iconname;
                        }                            
                    }
                    else
                    {
                        connectionDrivers = DMEEditor.ConfigEditor.DataDriversClasses.Where(c => c.DriverClass == drname).FirstOrDefault();
                        if (connectionDrivers != null)
                        {
                            iconname = connectionDrivers.iconname;
                        }
                        
                    }
                    
                    int imgindx = TreeV.ImageList.Images.IndexOfKey(iconname);
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
        public int GetImageIndex( string imagename)
        {
            try
            {
                int imgindx = TreeV.ImageList.Images.IndexOfKey(imagename);
                return imgindx;
                // Tree.SelectedImageIndex = GetImageIndex("select.ico");
            }
            catch (Exception)
            {

                return -1;
            }

        }
        #endregion "Misc Functions"
        #region "Util Functions"
        public static ObjectActivator<T> GetActivator<T>(ConstructorInfo ctor)
        {
            Type type = ctor.DeclaringType;
            ParameterInfo[] paramsInfo = ctor.GetParameters();

            //create a single param of type object[]
            ParameterExpression param =
                Expression.Parameter(typeof(object[]), "args");

            Expression[] argsExp =
                new Expression[paramsInfo.Length];

            //pick each arg from the params array 
            //and create a typed expression of them
            for (int i = 0; i < paramsInfo.Length; i++)
            {
                Expression index = Expression.Constant(i);
                Type paramType = paramsInfo[i].ParameterType;

                Expression paramAccessorExp =
                    Expression.ArrayIndex(param, index);

                Expression paramCastExp =
                    Expression.Convert(paramAccessorExp, paramType);

                argsExp[i] = paramCastExp;
            }

            //make a NewExpression that calls the
            //ctor with the args we just created
            NewExpression newExp = Expression.New(ctor, argsExp);

            //create a lambda with the New
            //Expression as body and our param object[] as arg
            LambdaExpression lambda =
                Expression.Lambda(typeof(ObjectActivator<T>), newExp, param);

            //compile it
            ObjectActivator<T> compiled = (ObjectActivator<T>)lambda.Compile();
            return compiled;
        }
        #endregion "Util Functions"
        #region "Node Handling Functions"
        public void CreateDelagates()
        {
           // TreeV.DrawMode=TreeViewDrawMode.OwnerDrawText;
            TreeV.AllowDrop = true;
            TreeV.NodeMouseClick += TreeView1_NodeMouseClick;
            TreeV.NodeMouseDoubleClick += TreeView1_NodeMouseDoubleClick;
            TreeV.AfterCheck += TreeView1_AfterCheck;
            
            TreeV.DragDrop += Tree_DragDrop;
            TreeV.DragEnter += Tree_DragEnter;
            TreeV.DragLeave += Tree_DragLeave;
            TreeV.ItemDrag += Tree_ItemDrag;
            TreeV.DragOver += Tree_DragOver;
         //   TreeV.DrawNode += TreeV_DrawNode;
            TreeV.AfterSelect += TreeV_AfterSelect;
         //   TreeV.KeyDown += TreeV_KeyDown;
        }
        private void TreeV_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.LControlKey)
            {
                TreeOP = "UnSelect";
                StartselectBranchID = 0;
                int BranchID = Convert.ToInt32(TreeV.SelectedNode.Tag);
                TreeV.BeginUpdate();
                if (TreeV.SelectedNode.BackColor == SelectBackColor)
                {
                    TreeV.SelectedNode.BackColor = Color.White;
                    SelectedBranchs.Remove(BranchID);

                }
                else
                {
                    TreeV.SelectedNode.BackColor = SelectBackColor;
                    SelectedBranchs.Add(BranchID);
                }
                TreeV.EndUpdate();

            }
            if (e.KeyCode == Keys.LShiftKey) //|| !Startselect
            {

                TreeOP = "StartSelect";
                if (StartselectBranchID == 0)
                {
                    StartselectBranchID = SelectedBranchID;
                }
                if (SelectedBranchID != StartselectBranchID)
                {

                    IBranch startbr = Branches.Where(x => x.BranchID == StartselectBranchID).FirstOrDefault();
                    IBranch endbr = Branches.Where(x => x.BranchID == SelectedBranchID).FirstOrDefault();
                    if ((startbr != endbr) || (startbr.ParentBranchID == endbr.ParentBranchID) || (startbr.BranchClass == endbr.BranchClass))
                    {

                        TreeNode startnode;
                        TreeNode endnode;
                        bool found = false;

                        if (SelectedBranchID > StartselectBranchID)
                        {
                            startnode = GetTreeNodeByTag(StartselectBranchID.ToString(), TreeV.Nodes);
                            endnode = GetTreeNodeByTag(SelectedBranchID.ToString(), TreeV.Nodes);
                        }
                        else
                        {
                            startnode = GetTreeNodeByTag(SelectedBranchID.ToString(), TreeV.Nodes);
                            endnode = GetTreeNodeByTag(StartselectBranchID.ToString(), TreeV.Nodes);
                        }
                        TreeNode n = startnode;
                        while (!found)
                        {
                            TreeV.BeginUpdate();
                            n.BackColor = SelectBackColor;
                            SelectedBranchs.Add(Convert.ToInt32(n.Tag));
                            if (n == endnode)
                            {
                                found = true;
                            }
                            else
                            {
                                n = n.NextNode;
                            }
                            TreeV.EndUpdate();
                        }

                    }
                }


            }
            if (e.KeyCode == Keys.RShiftKey) //|| !Startselect
            {

                if (SelectedBranchID != StartselectBranchID)
                {
                    TreeOP = "StartSelect";
                    if (StartselectBranchID == 0)
                    {
                        StartselectBranchID = SelectedBranchID;
                    }
                    IBranch startbr = Branches.Where(x => x.BranchID == StartselectBranchID).FirstOrDefault();
                    IBranch endbr = Branches.Where(x => x.BranchID == SelectedBranchID).FirstOrDefault();
                    if ((startbr != endbr) || (startbr.ParentBranchID == endbr.ParentBranchID) || (startbr.BranchClass == endbr.BranchClass))
                    {

                        TreeNode startnode;
                        TreeNode endnode;
                        bool found = false;

                        if (SelectedBranchID > StartselectBranchID)
                        {
                            startnode = GetTreeNodeByTag(StartselectBranchID.ToString(), TreeV.Nodes);
                            endnode = GetTreeNodeByTag(SelectedBranchID.ToString(), TreeV.Nodes);
                        }
                        else
                        {
                            startnode = GetTreeNodeByTag(SelectedBranchID.ToString(), TreeV.Nodes);
                            endnode = GetTreeNodeByTag(StartselectBranchID.ToString(), TreeV.Nodes);
                        }
                        TreeNode n = startnode;
                        while (!found)
                        {
                            TreeV.BeginUpdate();
                            n.BackColor = Color.White;

                            SelectedBranchs.Remove(Convert.ToInt32(n.Tag));
                            if (n == endnode)
                            {
                                found = true;
                            }
                            else
                            {
                                n = n.NextNode;
                            }
                            TreeV.EndUpdate();
                        }

                    }
                }


            }
        }

        // Returns the bounds of the specified node, including the region 
        // occupied by the node label and any node tag displayed.

        private void TreeV_AfterSelect(object sender, TreeViewEventArgs e)
        {
            // Vary the response depending on which TreeViewAction
            // triggered the event. 
            //switch (e.Action)
            //{
            //    case TreeViewAction.ByKeyboard:
                    LastSelectedNode = e.Node;
                    if (TreeOP != "StartSelect")
                    {
                        StartselectBranchID = Convert.ToInt32(LastSelectedNode.Tag);
                    }
            //        break;
            //    case TreeViewAction.ByMouse:
            //        LastSelectedNode = e.Node;
            //        if (TreeOP != "StartSelect")
            //        {
            //            StartselectBranchID = Convert.ToInt32(LastSelectedNode.Tag);
            //        }
                  
                  
            //        break;
            //}
        }
        public void NodeEvent(TreeNodeMouseClickEventArgs e)
        {

            TreeV.SelectedNode = e.Node;
            IDM_Addin s = sender;

            int BranchID = Convert.ToInt32(e.Node.Tag);
            string BranchText = e.Node.Text;
            IBranch br = Branches.Where(x => x.BranchID.ToString() == e.Node.Tag.ToString()).FirstOrDefault();
            SelectedBranchID = BranchID;
           

            if (br != null)
            {
                if (e.Button == MouseButtons.Left)
                {
                  
                    Passedarguments = new PassedArgs
                    {
                        Addin = null,
                        AddinName = br.BranchText,
                        AddinType = "",
                        DMView = null,
                        Id = BranchID,
                        CurrentEntity = BranchText,
                        DataSource = null,
                        EventType = TreeEvent
                    };
                    //if (Control.ModifierKeys == Keys.LControlKey)
                    //{
                    //    TreeOP = "UnSelect";
                    //    StartselectBranchID = 0;
                      
                    //    TreeV.BeginUpdate();
                    //    if (TreeV.SelectedNode.BackColor == SelectBackColor)
                    //    {
                    //        TreeV.SelectedNode.BackColor = Color.White;
                    //        SelectedBranchs.Remove(BranchID);

                    //    }
                    //    else
                    //    {
                    //        TreeV.SelectedNode.BackColor = SelectBackColor;
                    //        SelectedBranchs.Add(BranchID);
                    //    }
                    //    TreeV.EndUpdate();

                    //}
                    //if (Control.ModifierKeys == Keys.LShiftKey) //|| !Startselect
                    //{

                    //    TreeOP = "StartSelect";
                    //    if (StartselectBranchID == 0)
                    //    {
                    //        StartselectBranchID = SelectedBranchID;
                    //    }
                    //    if (SelectedBranchID != StartselectBranchID)
                    //    {

                    //        IBranch startbr = Branches.Where(x => x.BranchID == StartselectBranchID).FirstOrDefault();
                    //        IBranch endbr = Branches.Where(x => x.BranchID == SelectedBranchID).FirstOrDefault();
                    //        if ((startbr != endbr) || (startbr.ParentBranchID == endbr.ParentBranchID) || (startbr.BranchClass == endbr.BranchClass))
                    //        {

                    //            TreeNode startnode;
                    //            TreeNode endnode;
                    //            bool found = false;

                    //            if (SelectedBranchID > StartselectBranchID)
                    //            {
                    //                startnode = GetTreeNodeByTag(StartselectBranchID.ToString(), TreeV.Nodes);
                    //                endnode = GetTreeNodeByTag(SelectedBranchID.ToString(), TreeV.Nodes);
                    //            }
                    //            else
                    //            {
                    //                startnode = GetTreeNodeByTag(SelectedBranchID.ToString(), TreeV.Nodes);
                    //                endnode = GetTreeNodeByTag(StartselectBranchID.ToString(), TreeV.Nodes);
                    //            }
                    //            TreeNode n = startnode;
                    //            while (!found)
                    //            {
                    //                TreeV.BeginUpdate();
                    //                n.BackColor = SelectBackColor;
                    //                SelectedBranchs.Add(Convert.ToInt32(n.Tag));
                    //                if (n == endnode)
                    //                {
                    //                    found = true;
                    //                }
                    //                else
                    //                {
                    //                    n = n.NextNode;
                    //                }
                    //                TreeV.EndUpdate();
                    //            }

                    //        }
                    //    }


                    //}
                    //if (Control.ModifierKeys == Keys.RShiftKey) //|| !Startselect
                    //{

                    //    if (SelectedBranchID != StartselectBranchID)
                    //    {
                    //        TreeOP = "StartSelect";
                    //        if (StartselectBranchID == 0)
                    //        {
                    //            StartselectBranchID = SelectedBranchID;
                    //        }
                    //        IBranch startbr = Branches.Where(x => x.BranchID == StartselectBranchID).FirstOrDefault();
                    //        IBranch endbr = Branches.Where(x => x.BranchID == SelectedBranchID).FirstOrDefault();
                    //        if ((startbr != endbr) || (startbr.ParentBranchID == endbr.ParentBranchID) || (startbr.BranchClass == endbr.BranchClass))
                    //        {

                    //            TreeNode startnode;
                    //            TreeNode endnode;
                    //            bool found = false;

                    //            if (SelectedBranchID > StartselectBranchID)
                    //            {
                    //                startnode = GetTreeNodeByTag(StartselectBranchID.ToString(), TreeV.Nodes);
                    //                endnode = GetTreeNodeByTag(SelectedBranchID.ToString(), TreeV.Nodes);
                    //            }
                    //            else
                    //            {
                    //                startnode = GetTreeNodeByTag(SelectedBranchID.ToString(), TreeV.Nodes);
                    //                endnode = GetTreeNodeByTag(StartselectBranchID.ToString(), TreeV.Nodes);
                    //            }
                    //            TreeNode n = startnode;
                    //            while (!found)
                    //            {
                    //                TreeV.BeginUpdate();
                    //                n.BackColor = Color.White;

                    //                SelectedBranchs.Remove(Convert.ToInt32(n.Tag));
                    //                if (n == endnode)
                    //                {
                    //                    found = true;
                    //                }
                    //                else
                    //                {
                    //                    n = n.NextNode;
                    //                }
                    //                TreeV.EndUpdate();
                    //            }

                    //        }
                    //    }


                    //}
                }
                else
                {


                    StartselectBranchID = 0;
                    TreeOP = "NONE";
                }
                 
                if(e.Button== MouseButtons.Right)
                {
                    Nodemenu_MouseClick(e);
                }
               
            }
           

        }
        private void TreeView1_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (busy) return;
            busy = true;
            try
            {
                IBranch br = GetBranch(Convert.ToInt32(e.Node.Tag));

                if(br.BranchType== EnumBranchType.Entity && br.BranchClass!="VIEW")
                {
                    CheckNodes(e.Node, e.Node.Checked);
                    if (e.Node.Checked)
                    {
                        SelectedBranchs.Add(br.BranchID);
                    }else
                        SelectedBranchs.Remove(br.BranchID);

                }
                else
                {
                    e.Node.Checked = false;
                }
                  
            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail",$"Error in Showing View on Tree ({ex.Message}) ",DateTime.Now,0,null,Errors.Failed);
              
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
                SetChildrenChecked(node, node.Checked);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error in Setting Check for Node Tree ({ex.Message}) ", DateTime.Now, 0, null, Errors.Failed);
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
                    if (item.Checked)
                    {
                        SelectedBranchs.Add(Convert.ToInt32(item.Tag));
                    }
                    else
                        SelectedBranchs.Remove(Convert.ToInt32(item.Tag));
                }
                SetChildrenChecked(item, item.Checked);
            }
        }
        private void TreeView1_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            TreeEvent = "MouseDoubleClick";
            Nodeclickhandler(e);

        }
        private void TreeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
           TreeEvent = "MouseClick";
            Nodeclickhandler(e);
        }
        private void Nodeclickhandler(TreeNodeMouseClickEventArgs e)
        {
            SelectedNode = e.Node;
            SelectedBranchID = Convert.ToInt32(e.Node.Tag); 
            NodeEvent(e);
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
            // null;  //throw new NotImplementedException();
           
        }

        private void Tree_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.AllowedEffect;
        }

        private void Tree_DragDrop(object sender, DragEventArgs e)
        {
            // Retrieve the client coordinates of the drop location.
            Point targetPoint = TreeV.PointToClient(new Point(e.X, e.Y));

            // Retrieve the node at the drop location.
            TreeNode targetNode = TreeV.GetNodeAt(targetPoint);
            if (targetNode != null)
            {
                IBranch targetBranch = GetBranch(Convert.ToInt32(targetNode.Tag));
                // Retrieve the node that was dragged.
                IBranch dragedBranch = (IBranch)e.Data.GetData(e.Data.GetFormats()[0]);
                TreeNode dragedNode = GetTreeNodeByTag(dragedBranch.BranchID.ToString(), TreeV.Nodes);
                string targetBranchClass = targetBranch.GetType().Name;
                string dragedBranchClass = dragedBranch.GetType().Name;
                Function2FunctionAction functionAction = DMEEditor.ConfigEditor.Function2Functions.Where(x => x.FromClass == dragedBranchClass && x.ToClass == targetBranchClass && x.Event == "DragandDrop").FirstOrDefault();
                //---------------------------------------------------------
                if (targetBranch.BranchClass == dragedBranch.BranchClass)
                {
                    switch (targetBranch.BranchType)
                    {
                        case EnumBranchType.Root:
                            if (CheckifBranchExistinCategory(dragedBranch.BranchText, dragedBranch.BranchClass) != null)
                            {
                                if (dragedBranch.BranchType == EnumBranchType.DataPoint)
                                {
                                    MoveBranchToParent(targetBranch, dragedBranch);
                                }
                            }

                            break;
                     
                            break;
                        case EnumBranchType.Category:
                        case EnumBranchType.DataPoint:
                        case EnumBranchType.Entity:
                            if (dragedBranch.BranchClass == "VIEW")
                            {
                                if (dragedBranch.BranchType == EnumBranchType.Entity  && dragedBranch.DataSourceName==targetBranch.DataSourceName)
                                {
                                    MoveBranchToParent(targetBranch, dragedBranch);
                                }
                            }
                          

                            break;
                        default:
                            break;
                    }

                }
                if (functionAction != null) //functionAction
                {
                    switch (targetBranch.BranchType)
                    {
                        case EnumBranchType.Root:
                            args = new PassedArgs
                            {
                                ObjectName = "DATABASE",
                                ObjectType = "TABLE",
                                EventType = "DragandDrop",
                                ParameterString1 = "Create View using Table",
                                Objects = new List<ObjectItem> { new ObjectItem { Name = "Branch", obj = dragedBranch } }
                            };



                            SendActionFromBranchToBranch(targetBranch, dragedBranch, functionAction.ToMethod);
                            break;
                        case EnumBranchType.DataPoint:
                            args = new PassedArgs
                            {
                                ObjectName = "DATABASE",
                                ObjectType = "TABLE",
                                EventType = "DragandDrop",
                                ParameterString1 = "Add Entity Child",
                                DataSource = dragedBranch.DataSource,
                                DatasourceName = dragedBranch.DataSourceName,
                                CurrentEntity = dragedBranch.BranchText,
                                Id = dragedBranch.BranchID,
                                Objects = new List<ObjectItem> { new ObjectItem { Name = "ChildBranch", obj = dragedBranch } }
                            };
                            SendActionFromBranchToBranch(targetBranch, dragedBranch, functionAction.ToMethod);
                            break;
                        case EnumBranchType.Category:
                            if (dragedBranch.BranchType == EnumBranchType.DataPoint)
                            {
                                MoveBranchToParent(targetBranch, dragedBranch);
                            }
                             
                            break;
                        case EnumBranchType.Entity:
                            IDataSource ds = DMEEditor.GetDataSource(dragedBranch.DataSourceName);
                            EntityStructure  ent= ds.GetEntityStructure(dragedBranch.BranchText, true);
                            args = new PassedArgs
                            {
                                ObjectName = "DATABASE",
                                ObjectType = "TABLE",
                                EventType = "COPYENTITY",
                                ParameterString1 = "COPYENTITY",
                              
                                DataSource = dragedBranch.DataSource,
                                DatasourceName = dragedBranch.DataSourceName,
                                CurrentEntity = dragedBranch.BranchText,
                                 Id=dragedBranch.BranchID,
                                Objects = new List<ObjectItem> { new ObjectItem { Name = "Branch", obj = dragedBranch }, new ObjectItem { Name = "Entity", obj = ent } }
                            };



                            SendActionFromBranchToBranch(targetBranch, dragedBranch, functionAction.ToMethod);


                            break;
                        default:
                            break;
                    }
                }
            }
          

            //if (targetBranch.BranchType == EnumBranchType.Root)
            //{
            //    // Confirm that the node at the drop location is not 
            //    // the dragged node or a descendant of the dragged node.
            //    IDMDataView v = DME.viewEditor.GetView(Visutil.GetNodeID(targetNode).NodeIndex);
            //    IRDBSource ds = (IRDBSource)DME.GetDataSource(v.MainDataSourceID);
            //    if (!draggedNode.Equals(targetNode) && !ContainsNode(draggedNode, targetNode))
            //    {
            //        // If it is a move operation, remove the node from its current 
            //        // location and add it to the node at the drop location.
            //        if (e.Effect == DragDropEffects.Move)
            //        {

            //            int tabid = DME.viewEditor.AddEntitytoDataView(ds, draggedNode.Text.ToUpper(),ds.GetSchemaName(), "", v.id);
            //            //  Visutil.ShowTableonTree(MainNode, v.id, tabid, true);

            //            //draggedNode.Remove();
            //            //targetNode.Nodes.Add(draggedNode);
            //        }

            //        // If it is a copy operation, clone the dragged node 
            //        // and add it to the node at the drop location.
            //        //else if (e.Effect == DragDropEffects.Copy)
            //        //{
            //        //    targetNode.Nodes.Add((TreeNode)draggedNode.Clone());
            //        //}

            //        // Expand the node at the location 
            //        // to show the dropped node.
            //        targetNode.Expand();
            //    }
            //}

        }
        private void Tree_ItemDrag(object sender, ItemDragEventArgs e)

        {
            //if (CurrentNode != null)
            //{counties.xls,counties
            //    if (CurrentNode.nodeType == "EN")
            //    {
            // Move the dragged node when the left mouse button is used.
            IDataObject x = new DataObject();

            TreeNode n = (TreeNode)e.Item;
            if (e.Button == MouseButtons.Left)
            {
                IBranch branch = GetBranch(Convert.ToInt32(n.Tag));
                x.SetData(branch);
                switch (branch.BranchType)
                {
                    case EnumBranchType.Root:
                        break;
                    case EnumBranchType.DataPoint:

                        TreeV.DoDragDrop(x, DragDropEffects.Move);
                        break;
                    case EnumBranchType.Category:
                        break;
                    case EnumBranchType.Entity:



                        TreeV.DoDragDrop(x, DragDropEffects.Move);


                        break;
                    default:
                        break;
                }

            }




            // Copy the dragged node when the right mouse button is used.
            //else if (e.Button == MouseButtons.Right)
            //{
            //    Tree.DoDragDrop(e.Item, DragDropEffects.Copy);
            //}
            //  }

            //}


        }

        private void Tree_DragOver(object sender, DragEventArgs e)
        {
            // Retrieve the client coordinates of the mouse position.
            Point targetPoint = TreeV.PointToClient(new Point(e.X, e.Y));

            // Select the node at the mouse position.
            TreeV.SelectedNode = TreeV.GetNodeAt(targetPoint);
        }
        #endregion
        #region "Data Managemet Shared"
        public LScriptHeader CreateScriptToCopyEntities(IDataSource dest,List<EntityStructure> entities,bool copydata=true)
        {
            if (dest.Openconnection() == ConnectionState.Open)
            {
                List<EntityStructure> ls = new List<EntityStructure>();
                ShowWaiting();
                ChangeWaitingCaption($"Generating Scripts for Entities Total:{entities.Count}");
                ls = entities;
                int i = 0;
                if (ls.Count > 0)
                {
                    DMEEditor.ETL.script = new LScriptHeader();
                    DMEEditor.ETL.script.scriptSource = dest.DatasourceName;
                    DMEEditor.ETL.GetCreateEntityScript(dest, ls);
                    if (copydata)
                    {
                        foreach (var item in ls)
                        {
                            AddCommentsWaiting($"{i} - Creating script for Copy Data for Entity {item.EntityName} ");
                            LScript upscript = new LScript();
                            upscript.sourcedatasourcename = item.DataSourceID;
                            upscript.sourceentityname = item.EntityName;
                            upscript.sourceDatasourceEntityName = item.DatasourceEntityName;

                            upscript.destinationDatasourceEntityName = item.DatasourceEntityName;
                            upscript.destinationentityname = item.EntityName;

                            upscript.destinationdatasourcename = dest.DatasourceName;
                            upscript.scriptType = DDLScriptType.CopyData;
                            DMEEditor.ETL.script.Scripts.Add(upscript);
                            i += 1;
                        }
                    }
                    
                }
                HideWaiting();
                DMEEditor.AddLogMessage("Success", "Copy Entities Script Generated", DateTime.Now, 0, "", Errors.Failed);
                return DMEEditor.ETL.script;
            }
            else
            {
                DMEEditor.AddLogMessage("Fail", " Could not Open the desitination Datasource", DateTime.Now, 0, "", Errors.Failed);
                return null;
            }
               


        }
        public IErrorsInfo ShowRunScriptGUI(IBranch RootBranch, IBranch Branch ,IDataSource ds, LScriptHeader script)
        {
            string[] args = { "New Query Entity", null, null };
            List<ObjectItem> ob = new List<ObjectItem>(); ;
            ObjectItem it = new ObjectItem();
            it.obj = Branch;
            it.Name = "Branch";
            ob.Add(it);
          
            it = new ObjectItem();
            it.obj = RootBranch;
            it.Name = "RootBranch";
            ob.Add(it);
            it = new ObjectItem();
            it.obj = DMEEditor;
            it.Name = "DMEEDITOR";
            ob.Add(it);
            it = new ObjectItem();
            it.obj = ds;
            it.Name = "DATASOURCE";
            ob.Add(it);
            PassedArgs Passedarguments = new PassedArgs
            {
                Addin = null,
                AddinName = null,
                AddinType = "",
                DMView = null,
                CurrentEntity = null,
                ObjectType = "SCRIPT",
                DataSource = ds,
                ObjectName = ds.DatasourceName,
                Objects = ob,
                DatasourceName = null,
                EventType = "RUNSCRIPT"

            };
         
            DMEEditor.ETL.script = script;
            Visutil.ShowUserControlPopUp("uc_ScriptRun",  DMEEditor, args, Passedarguments);
            return DMEEditor.ErrorObject;
        }
        #endregion
    }
}
