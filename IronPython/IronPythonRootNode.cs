using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Vis;
using TheTechIdea.Util;
using TheTechIdea.Winforms.VIS;

namespace IronPython
{
    public  class IronPythonRootNode : IBranch, ITreeView, IOrder
    {
      
        public IronPythonRootNode()
        {
            BranchText = "IronPython";
            BranchClass = "AI";
            IconImageName = "ironpython.ico";
           // BranchType = EnumBranchType.Root;
        }
       
        public int Order { get; set; } = 11;
        public object TreeStrucure { get ; set ; }
        public IVisUtil Visutil { get ; set ; }
        public int ID { get ; set ; }
        public IDMEEditor DMEEditor { get ; set ; }
        public IDataSource DataSource { get ; set ; }
        public string DataSourceName { get ; set ; }
        public List<IBranch> ChildBranchs { get; set; } = new List<IBranch>();
        public ITree TreeEditor { get ; set ; }
        public List<string> BranchActions { get; set; } = new List<string>();
        public EntityStructure EntityStructure { get ; set ; }
        public int MiscID { get ; set ; }
        public string Name { get ; set ; }
        public string BranchText { get; set; } = "IronPython";
        public int Level { get; set; } 
        public EnumBranchType BranchType { get; set; } = EnumBranchType.DataPoint;
        public int BranchID { get ; set ; }
        public string IconImageName { get; set; } = "ironpython.ico";
        public string BranchStatus { get ; set ; }
        public int ParentBranchID { get ; set ; }
        public string BranchDescription { get ; set ; }
        public string BranchClass { get; set; } = "AI";
        #region "Interface Methods"
        public IErrorsInfo CreateChildNodes()
        {

            try
            {
                CreateNodes();
                foreach (CategoryFolder i in DMEEditor.ConfigEditor.CategoryFolders.Where(x => x.RootName == "IRONPYTHON"))
                {

                    CreateCategoryNode(i);


                }
                //  CreateNodes();

                DMEEditor.AddLogMessage("Success", "Added Child Nodes", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Child Nodes";
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
        public IErrorsInfo CreateCategoryNode(IBranch Rootbr, CategoryFolder p)
        {
            try
            {
                IronPythonCategoryNode Category = new IronPythonCategoryNode(TreeEditor, DMEEditor, Rootbr, p.FolderName, TreeEditor.SeqID, EnumBranchType.Category, TreeEditor.CategoryIcon);
                TreeEditor.AddBranch(Rootbr, Category);
                Rootbr.ChildBranchs.Add(Category);
                Category.CreateChildNodes();

            }
            catch (Exception ex)
            {
                DMEEditor.Logger.WriteLog($"Error Creating Category Node ({ex.Message}) ");
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Ex = ex;
            }
            return DMEEditor.ErrorObject;

        }
        public IErrorsInfo RemoveChildNodes()
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo SetConfig(ITree pTreeEditor, IDMEEditor pDMEEditor, IBranch pParentNode, string pBranchText, int pID, EnumBranchType pBranchType, string pimagename)
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

        [BranchDelegate(Caption = "Add Category")]
        public IErrorsInfo AddCategory()
        {

            try
            {
                TreeEditor.AddCategory(this);
            }
            catch (Exception ex)
            {
                string mes = "Could not Add Category";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        public IErrorsInfo DoubleClick()
        {

            try
            {
                string[] args = { BranchText };
                PassedArgs Passedarguments = new PassedArgs
                {  // Obj= obj,
                    Addin = null,
                    AddinName = null,
                    AddinType = null,
                    DMView = null,
                    CurrentEntity = null,
                    ObjectName = null,
                    Id = BranchID,
                    ObjectType = "UserControl",
                    DataSource = DataSource,
                    EventType = "Run"

                };


                Visutil.ShowUserControlInContainer("uc_WorkFlowManagerMainScreen", Visutil.DisplayPanel, DMEEditor, args, Passedarguments);

                DMEEditor.AddLogMessage("Success", "Shown Module " + BranchText, DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Show Module " + BranchText;
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        #endregion Exposed Interface"
        #region "Other Methods"
        public IErrorsInfo CreateNodes()
        {

            try
            {
                TreeEditor.RemoveChildBranchs(this);
                //foreach (DataWorkFlow item in DMEEditor.ConfigEditor.WorkFlows)
                //{
                //    WorkFlowEntityNode en = new WorkFlowEntityNode(TreeEditor, DMEEditor, this, item.DataWorkFlowName, TreeEditor.SeqID, EnumBranchType.DataPoint, "workflowentity.ico");
                //    en.DataSource = DataSource;
                //    TreeEditor.AddBranch(this, en);
                //    ChildBranchs.Add(en);
                //}


                DMEEditor.AddLogMessage("Success", "Created child Nodes", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Create child Nodes";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;

        }
        private IErrorsInfo CreateCategoryNode(CategoryFolder p)
        {
            try
            {
                IronPythonCategoryNode categoryBranch = new IronPythonCategoryNode(TreeEditor, DMEEditor, this, p.FolderName, TreeEditor.SeqID, EnumBranchType.Category, "category.ico");
                TreeEditor.AddBranch(this, categoryBranch);
                ChildBranchs.Add(categoryBranch);
                categoryBranch.CreateChildNodes();


            }
            catch (Exception ex)
            {
                DMEEditor.Logger.WriteLog($"Error Creating Category  View Node ({ex.Message}) ");
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Ex = ex;
            }
            return DMEEditor.ErrorObject;

        }
        #endregion"Other Methods"
    }
}
