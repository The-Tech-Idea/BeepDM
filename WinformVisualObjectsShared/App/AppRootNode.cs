using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.DataView;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Util;
using TheTechIdea.Winforms.VIS;

namespace TheTechIdea.Beep.AppBuilder
{
    public class AppRootNode : IBranch, ITreeView, IOrder
    {
        public AppRootNode()
        {

        }
        public AppRootNode(ITree pTreeEditor, IDMEEditor pDMEEditor, IBranch pParentNode, string pBranchText, int pID, EnumPointType pBranchType, string pimagename)
        {
            TreeEditor = pTreeEditor;
            DMEEditor = pDMEEditor;
            ParentBranchID = pParentNode.ID;
            BranchText = pBranchText;
            BranchType = pBranchType;
           // IconImageName = pimagename;
            if (pID != 0)
            {
                ID = pID;
                BranchID = ID;
            }
        }
        #region "Properties"
        public int Order { get; set; } = 8;
        public int ID { get ; set ; }
        public IDMEEditor DMEEditor { get ; set ; }
        public IDataSource DataSource { get ; set ; }
        public string DataSourceName { get ; set ; }
        public List<IBranch> ChildBranchs { get; set; } = new List<IBranch>();
        public ITree TreeEditor { get ; set ; }
        public List<string> BranchActions { get; set; } = new List<string>();
        public EntityStructure EntityStructure { get; set; } = new EntityStructure();
        public int MiscID { get ; set ; }
        public string Name { get ; set ; }
        public string BranchText { get; set; } = "Apps";
        public int Level { get ; set ; }
        public EnumPointType BranchType { get; set; } = EnumPointType.Root;
        public int BranchID { get ; set ; }
        public string IconImageName { get; set; } = "designer.ico";
        public string BranchStatus { get ; set ; }
        public int ParentBranchID { get ; set ; }
        public string BranchDescription { get ; set ; }
        public string BranchClass { get; set; } = "APP";
        public object TreeStrucure { get ; set ; }
        public IVisUtil Visutil { get ; set ; }

       // public event EventHandler<PassedArgs> BranchSelected;
       // public event EventHandler<PassedArgs> BranchDragEnter;
       // public event EventHandler<PassedArgs> BranchDragDrop;
       // public event EventHandler<PassedArgs> BranchDragLeave;
       // public event EventHandler<PassedArgs> BranchDragClick;
       // public event EventHandler<PassedArgs> BranchDragDoubleClick;
       // public event EventHandler<PassedArgs> ActionNeeded;
        #endregion "Properties"
        #region "Interface Methods"
        public IErrorsInfo CreateChildNodes()
        {
            try
            {
                TreeEditor.RemoveChildBranchs(this);
                foreach (App i in DMEEditor.ConfigEditor.Apps)
                {

                    if (TreeEditor.CheckifBranchExistinCategory(i.AppName, "APP") == null)
                    {
                        // ObjectDataSourcetemp = i.FileName;

                        CreateAppNode(i.ID,i.AppName);

                       
                    }
                }
                foreach (CategoryFolder i in DMEEditor.ConfigEditor.CategoryFolders.Where(y => y.RootName == "APP"))
                {

                    CreateCategoryNode(i);


                }
              
            }
            catch (Exception ex)
            {
                string mes = "Could not Add Childs";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        public IErrorsInfo ExecuteBranchAction(string ActionName)
        {
            throw new NotImplementedException();
        }
        public IErrorsInfo MenuItemClicked(string ActionNam)
        {
            throw new NotImplementedException();
        }
        public IErrorsInfo RemoveChildNodes()
        {
            throw new NotImplementedException();
        }
        public IErrorsInfo SetConfig(ITree pTreeEditor, IDMEEditor pDMEEditor, IBranch pParentNode, string pBranchText, int pID, EnumPointType pBranchType, string pimagename)
        {
            try
            {
                TreeEditor = pTreeEditor;
                DMEEditor = pDMEEditor;
                ParentBranchID = pParentNode.ID;
                BranchText = pBranchText;
                BranchType = pBranchType;
                IconImageName = pimagename;
                if (pID != 0)
                {
                    ID = pID;
                    BranchID = ID;
                }

                //   DMEEditor.AddLogMessage("Success", "Set Config OK", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Set Config";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        #endregion "Interface Methods"
        #region "Exposed Interface"
        [CommandAttribute(Caption = "Add Category")]
        public IErrorsInfo AddCategory()
        {

            try
            {
                PassedArgs Passedarguments = new PassedArgs();
                string foldername = "";
                Visutil.controlEditor.InputBox("Enter Category Name", "What Category you want to Add", ref foldername);
                if (foldername != null)
                {
                    if (foldername.Length > 0)
                    {
                        CategoryFolder x = DMEEditor.ConfigEditor.AddFolderCategory(foldername, "APP", foldername);
                        CreateCategoryNode(x);
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
        [CommandAttribute(Caption = "Create App")]
        public IErrorsInfo CreateApp()
        {

            try
            {
                string[] args = { "New Query Entity", null, null };
                List<ObjectItem> ob = new List<ObjectItem>(); ;
                ObjectItem it = new ObjectItem();
                it.obj = this;
                it.Name = "Branch";
                ob.Add(it);
                IBranch RootBranch = TreeEditor.Branches[TreeEditor.Branches.FindIndex(x => x.BranchClass == "APP" && x.BranchType == EnumPointType.Root)];
                it = new ObjectItem();
                it.obj = RootBranch;
                it.Name = "RootAppBranch";
                ob.Add(it);
                PassedArgs Passedarguments = new PassedArgs
                {
                    Addin = null,
                    AddinName = null,
                    AddinType = "",
                    DMView = null,
                    CurrentEntity = null,

                    ObjectType = "NEWAPP",
                    DataSource = null,
                    ObjectName = null,

                    Objects = ob,

                    DatasourceName = null,
                    EventType = "NEWAPP"

                };
             //   ActionNeeded?.Invoke(this, Passedarguments);
                Visutil.ShowUserControlPopUp("uc_App", DMEEditor, args, Passedarguments);

               // DMEEditor.AddLogMessage("Success", "Created Query Entity", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Create App";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        #endregion Exposed Interface"
        #region "Other Methods"
        private IBranch CreateAppNode(string id, string AppName)
        {
            AppNode viewbr = null;
            try
            {

                viewbr = new AppNode(TreeEditor, DMEEditor, this, AppName, TreeEditor.SeqID, EnumPointType.DataPoint, "app.ico", AppName);
                TreeEditor.AddBranch(this, viewbr);
                viewbr.CreateChildNodes();
                ChildBranchs.Add(viewbr);

                DMEEditor.AddLogMessage("Success", "Added App", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Add App";
                viewbr = null;
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };

            return viewbr;
        }
        private IErrorsInfo CreateCategoryNode(CategoryFolder p)
        {
            try
            {
               
               
                AppCategoryNode categoryBranch = new AppCategoryNode(TreeEditor, DMEEditor, this, p.FolderName, TreeEditor.SeqID, EnumPointType.Category, IconImageName);
                TreeEditor.AddBranch(this, categoryBranch);
                ChildBranchs.Add(categoryBranch);
                categoryBranch.CreateChildNodes();


            }
            catch (Exception ex)
            {
                DMEEditor.Logger.WriteLog($"Error Creating Category  App Node ({ex.Message}) ");
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Ex = ex;
            }
            return DMEEditor.ErrorObject;

        }
        #endregion
    }
}
