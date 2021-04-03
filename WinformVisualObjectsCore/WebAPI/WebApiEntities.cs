using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Vis;
using TheTechIdea.Util;

namespace TheTechIdea.Winforms.VIS.WebAPI
{
    public class WebApiEntities : IBranch, ITreeView
    {
        public WebApiEntities(ITree pTreeEditor, IDMEEditor pDMEEditor, IBranch pParentNode, string pBranchText, int pID, EnumBranchType pBranchType, string pimagename,string pDatasourcename)
        {
            TreeEditor = pTreeEditor;
            DMEEditor = pDMEEditor;
            ParentBranchID = pParentNode.ID;
            BranchText = pBranchText;
            BranchType = pBranchType;
            DataSourceName = pDatasourcename;
            IconImageName = "datapoint.ico";
            if (pID != 0)
            {
                ID = pID;
                BranchID = ID;
            }
        }
        public int ID { get ; set ; }
        public IDMEEditor DMEEditor { get ; set ; }
        public IDataSource DataSource { get ; set ; }
        public string DataSourceName { get ; set ; }
        public List<IBranch> ChildBranchs { get; set; } = new List<IBranch>();
        public ITree TreeEditor { get; set; }
        public List<string> BranchActions { get; set; } = new List<string>();
        public EntityStructure EntityStructure { get; set; }
        public int MiscID { get; set; }
        public string Name { get; set; }
        public string BranchText { get; set; }
        public int Level { get; set; }
        public EnumBranchType BranchType { get; set; } = EnumBranchType.Entity;
        public int BranchID { get; set; }
        public string IconImageName { get; set; }
        public string BranchStatus { get; set; }
        public int ParentBranchID { get; set; }
        public string BranchDescription { get; set; }
        public string BranchClass { get; set; } = "WEBAPI";
        public object TreeStrucure { get ; set ; }
        public IVisUtil Visutil { get ; set ; }

       // public event EventHandler<PassedArgs> BranchSelected;
       // public event EventHandler<PassedArgs> BranchDragEnter;
       // public event EventHandler<PassedArgs> BranchDragDrop;
       // public event EventHandler<PassedArgs> BranchDragLeave;
       // public event EventHandler<PassedArgs> BranchDragClick;
       // public event EventHandler<PassedArgs> BranchDragDoubleClick;
       // public event EventHandler<PassedArgs> ActionNeeded;

        #region "Interface Methods"
        public IErrorsInfo CreateChildNodes()
        {

            try
            {
             

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
                //DataSourceName = pDatasourcename;
                IconImageName = pimagename;
                if (pID != 0)
                {
                    BranchID = pID;

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

        [BranchDelegate(Caption = "Get")]
        public IErrorsInfo GetData()
        {

            try
            {
                string[] args = { "New Query Entity", null, null };
                List<ObjectItem> ob = new List<ObjectItem>(); ;
                ObjectItem it = new ObjectItem();
                it.obj = this;
                it.Name = "Branch";
                ob.Add(it);
                IBranch DataSourceBr = TreeEditor.GetBranch(this.ParentBranchID);
                it = new ObjectItem();
                it.obj = DataSourceBr;
                it.Name = "ParentBranch";
                ob.Add(it);
                IBranch RootWEBAPIBranch = TreeEditor.Branches[TreeEditor.Branches.FindIndex(x => x.BranchClass == "WEBAPI" && x.BranchType == EnumBranchType.Root)];
                it = new ObjectItem();
                it.obj = RootWEBAPIBranch;
                it.Name = "RootWebApiBranch";
                ob.Add(it);
                PassedArgs Passedarguments = new PassedArgs
                {
                    Addin = null,
                    AddinName = null,
                    AddinType = "",
                    DMView = null,
                    CurrentEntity = BranchText,
                    Id = ID,
                    ObjectType = "WEBAPI",
                    DataSource = null,
                    ObjectName = DataSourceBr.BranchText,

                    Objects = ob,

                    DatasourceName = DataSourceName,
                    EventType = "GETDATAPOINT"

                };
                // ActionNeeded?.Invoke(this, Passedarguments);
                Visutil.ShowUserControlInContainer("uc_webapiGetQuery", Visutil.DisplayPanel, DMEEditor, args, Passedarguments);

            }
            catch (Exception ex)
            {
                string mes = "Could not Create Query Entity";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        [BranchDelegate(Caption = "Edit", Hidden = false)]
        public IErrorsInfo EditEntity()
        {

            try
            {
                string[] args = { "New View", null, null };
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
                    CurrentEntity = BranchText,
                    Id = BranchID,
                    ObjectType = "VIEWENTITY",
                    DataSource = DataSource,
                    ObjectName = BranchText,
                    Objects = ob,
                    DatasourceName =DataSourceName,
                    EventType = "VIEWENTITY"

                };
                //ActionNeeded?.Invoke(this, Passedarguments);
                Visutil.ShowUserControlPopUp("uc_updateEntity", DMEEditor, args, Passedarguments);



                DMEEditor.AddLogMessage("Success", "Edit Control Shown", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not show Edit Control";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        #endregion Exposed Interface"
        #region "Other Methods"


        public IErrorsInfo GetFile()
        {

            try
            {


                DMEEditor.AddLogMessage("Success", "Loaded File", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Load File";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        #endregion"Other Methods"


    }
}
