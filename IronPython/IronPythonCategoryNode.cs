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
    public class IronPythonCategoryNode : IBranch, ITreeView, IOrder
    {
        public IronPythonCategoryNode()
        {


        }
        public IronPythonCategoryNode(ITree pTreeEditor, IDMEEditor pDMEEditor, IBranch pParentNode, string pBranchText, int pID, EnumBranchType pBranchType, string pimagename)
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
                BranchID = pID;
            }
        }

        #region "Properties"
        public int ID { get; set; }
        public EntityStructure EntityStructure { get; set; }
        public int Order { get; set; } = 6;
        public string Name { get; set; }
        public string BranchText { get; set; } = "IRONPYTHON";
        public IDMEEditor DMEEditor { get; set; }
        public IDataSource DataSource { get; set; }
        public string DataSourceName { get; set; }
        public int Level { get; set; }
        public EnumBranchType BranchType { get; set; } = EnumBranchType.Category;
        public int BranchID { get; set; }
        public string IconImageName { get; set; } = "category.ico";
        public string BranchStatus { get; set; }
        public int ParentBranchID { get; set; }
        public string BranchDescription { get; set; }
        public string BranchClass { get; set; } = "AI";
        public List<IBranch> ChildBranchs { get; set; } = new List<IBranch>();
        public ITree TreeEditor { get; set; }
        public List<string> BranchActions { get; set; }
        public object TreeStrucure { get; set; }
        public IVisUtil Visutil { get; set; }
        public int MiscID { get; set; }
        //public AddinTreeStructure AddinTreeStructure { get; set; }

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

                foreach (CategoryFolder p in DMEEditor.ConfigEditor.CategoryFolders.Where(x => x.RootName == "IRONPYTHON" && x.FolderName == BranchText))
                {
                    foreach (string item in p.items)
                    {
                     //   ReportTemplate i = DMEEditor.ConfigEditor.Reports.Where(x => x.Name == item).FirstOrDefault();

                        //if (i != null)
                        //{
                        // //   CreateReportNode(i.ID, i.Name); //Path.Combine(i.FilePath, i.FileName)
                        //}
                    }

                }

            }
            catch (Exception ex)
            {
                string mes = "Could not Add Category";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;

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
        [BranchDelegate(Caption = "Create")]
        public IErrorsInfo Create()
        {

            try
            {

            }
            catch (Exception ex)
            {
                string mes = "Could not Added Category ";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        [BranchDelegate(Caption = "DoubleClick", Hidden = true, DoubleClick = true)]
        public IErrorsInfo DoubleClick()
        {

            try
            {


                DMEEditor.AddLogMessage("Success", "Added View", DateTime.Now, 0, null, Errors.Ok);


            }
            catch (Exception ex)
            {
                string mes = "Could not Added View ";
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
                //foreach (DataWorkFlow item in DMEEditor.WorkFlowEditor.WorkFlows)
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
        #endregion"Other Methods"

    }
}
