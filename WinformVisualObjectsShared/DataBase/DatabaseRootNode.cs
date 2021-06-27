using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Vis;
using TheTechIdea.Util;

namespace TheTechIdea.Winforms.VIS
{
    public class DatabaseRootNode : IBranch, ITreeView,IOrder,IBranchRootCategory
    {

        public DatabaseRootNode()
        {

        }
        public DatabaseRootNode(ITree pTreeEditor, IDMEEditor pDMEEditor, IBranch pParentNode, string pBranchText, int pID, EnumPointType pBranchType, string pimagename)
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
      
        public string Name { get; set; }
        public EntityStructure EntityStructure { get; set; }
        public string BranchText { get; set; } = "RDBMS";
        public IDMEEditor DMEEditor { get ; set ; }
        public IDataSource DataSource { get ; set ; }
        public string DataSourceName { get; set; }
        public int Level { get; set; } = 0;
        public  EnumPointType BranchType { get; set; } = EnumPointType.Root;
        public int BranchID { get ; set ; }
        public string IconImageName { get ; set ; }= "database.ico";
        public string BranchStatus { get ; set ; }
        public int ParentBranchID { get ; set ; }
        public string BranchDescription { get ; set ; }
      
        public string BranchClass { get; set; } = "RDBMS";
        public List<IBranch> ChildBranchs { get ; set ; } = new List<IBranch>();
        public ITree TreeEditor { get ; set ; }
        public List<string> BranchActions { get ; set ; }
        public List<Delegate> Delegates { get ; set ; }
        public int ID { get ; set ; }
        public int Order { get; set; } = 3;
        public int MiscID { get; set; }
        public IVisUtil Visutil { get; set; }
        PassedArgs Passedarguments { get; set; } = new PassedArgs();
        public object TreeStrucure { get ; set ; }

       // public event EventHandler<PassedArgs> BranchSelected;
       // public event EventHandler<PassedArgs> BranchDragEnter;
       // public event EventHandler<PassedArgs> BranchDragDrop;
       // public event EventHandler<PassedArgs> BranchDragLeave;
       // public event EventHandler<PassedArgs> BranchDragClick;
       // public event EventHandler<PassedArgs> BranchDragDoubleClick;
       // public event EventHandler<PassedArgs> ActionNeeded;
       // public event EventHandler<PassedArgs> OnObjectSelected;

       
        public IErrorsInfo CreateChildNodes()
        {

            try
            {
                TreeEditor.RemoveChildBranchs(this);
                foreach (ConnectionProperties i in DMEEditor.ConfigEditor.DataConnections.Where(c => c.Category == DatasourceCategory.RDBMS && c.CompositeLayer==false))
                {
                    if (TreeEditor.CheckifBranchExistinCategory(i.ConnectionName, "RDBMS")==null)
                    {
                        CreateDBNode(i.ID, i.ConnectionName);
                        i.Drawn = true;
                    }


                }
                foreach (CategoryFolder i in DMEEditor.ConfigEditor.CategoryFolders.Where(x => x.RootName == "RDBMS"))
                {

                    CreateCategoryNode(i);


                }

            //    DMEEditor.AddLogMessage("Success", "Added Database Connection", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Add Database Connection";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
            
        }

        private IErrorsInfo CreateDBNode(int id, string ConnectionName)
        {

            try
            {

                DatabaseNode database = new DatabaseNode(TreeEditor, DMEEditor, this, ConnectionName, TreeEditor.SeqID, EnumPointType.DataPoint, ConnectionName);
                TreeEditor.AddBranch(this,database);
             
                ChildBranchs.Add(database);
               
             //   DMEEditor.AddLogMessage("Success", "Added Database Connection", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Add Database Connection";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
           
            return DMEEditor.ErrorObject;
        }
        public IErrorsInfo CreateCategoryNode(CategoryFolder p)
        {
            try
            {
                DatabaseCategoryNode Category = new DatabaseCategoryNode(TreeEditor, DMEEditor, this,p.FolderName, TreeEditor.SeqID, EnumPointType.Category,TreeEditor.CategoryIcon);
                TreeEditor.AddBranch(this, Category);
                ChildBranchs.Add(Category);
                Category.CreateChildNodes();
               
            }
            catch (Exception ex)
            {
                DMEEditor.Logger.WriteLog($"Error Creating Category Node File Node ({ex.Message}) ");
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

        public IErrorsInfo OnBranchSelected(PassedArgs ActionDef)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo RemoveChildNodes()
        {
            throw new NotImplementedException();
        }
        #region "Delegate for Database Root Node"
        [CommandAttribute(Caption = "Create Local Database")]
        public IErrorsInfo CreateNewLocalDatabase()
        {

            try
            {
                string[] args = { "New Query Entity", null, null };
                List<ObjectItem> ob = new List<ObjectItem>(); ;
                ObjectItem it = new ObjectItem();
                it.obj = this;
                it.Name = "Branch";
                ob.Add(it);
               
                PassedArgs Passedarguments = new PassedArgs
                {
                    Addin = null,
                    AddinName = null,
                    AddinType = "",
                    DMView = null,
                    CurrentEntity = null,

                    ObjectType = "LOCALDB",
                    DataSource = null,
                    ObjectName = null,

                    Objects = ob,

                    DatasourceName = null,
                    EventType = "NEW"

                };
                Visutil.ShowUserControlPopUp("uc_CreateLocalDatabase",  DMEEditor, args, Passedarguments);
              //  Visutil.ShowFormFromAddin(Visutil.LLoader.AddIns.Where(x => x.ObjectName == "uc_CreateLocalDatabase").FirstOrDefault().DllPath, DMEEditor.ConfigEditor.Config.DSEntryFormName, DMEEditor, args, null);

                DMEEditor.AddLogMessage("Success", "create Local Database", DateTime.Now, 0, null, Errors.Failed);
            }
            catch (Exception ex)
            {
                string mes = "Could not create Local Database";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }

        [CommandAttribute(Caption = "New/Edit Data Source")]
        public  IErrorsInfo AddDataBaseConnection()
        {

            try
            {
                string[] args = { "New DataSource", "", null };
                Visutil.ShowUserControlInContainer("uc_DataConnection",Visutil.DisplayPanel,DMEEditor, args, null);

             //   DMEEditor.AddLogMessage("Success", "Added Database Connection", DateTime.Now, 0,null, Errors.Failed);
            }
            catch (Exception ex)
            {
                string mes = "Could not Add Database Connection";
                DMEEditor.AddLogMessage(ex.Message,  mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        [CommandAttribute(Caption = "Add Category")]
        public  IErrorsInfo AddCategory()
        {

            try
            {
                 Passedarguments  = new PassedArgs();
                string foldername = "";
                Visutil.controlEditor.InputBox("Enter Category Name", "What Category you want to Add", ref foldername);
                if (foldername != null)
                {
                    if (foldername.Length > 0)
                    {
                        CategoryFolder x = DMEEditor.ConfigEditor.AddFolderCategory(foldername, "RDBMS", foldername);
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

        #endregion
    }
}
