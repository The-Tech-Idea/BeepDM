using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Vis;
using TheTechIdea.Util;

namespace TheTechIdea.Winforms.VIS.WebAPI
{
    public class WebApiEntities : IBranch, ITreeView
    {
        public WebApiEntities(ITree pTreeEditor, IDMEEditor pDMEEditor, IBranch pParentNode, string pBranchText, int pID, EnumPointType pBranchType, string pimagename,string pDatasourcename)
        {
            TreeEditor = pTreeEditor;
            DMEEditor = pDMEEditor;
            ParentBranchID = pParentNode.ID;
            BranchText = pBranchText;
            BranchType = pBranchType;
            DataSourceName = pDatasourcename;
           IconImageName = pimagename;
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
        public EnumPointType BranchType { get; set; } 
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

        public IErrorsInfo SetConfig(ITree pTreeEditor, IDMEEditor pDMEEditor, IBranch pParentNode, string pBranchText, int pID, EnumPointType pBranchType, string pimagename)
        {
            try
            {
                TreeEditor = pTreeEditor;
                DMEEditor = pDMEEditor;
                ParentBranchID = pParentNode.ID;
                BranchText = pBranchText;
                BranchType = pBranchType;
                DataSourceName = pParentNode.DataSourceName;
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
        [CommandAttribute(Caption = "Get Series(s)/Data")]
        public IErrorsInfo GetDataAsync()
        {

            try
            {
                if (BranchType== EnumPointType.Entity)
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
                    IBranch RootWEBAPIBranch = TreeEditor.Branches[TreeEditor.Branches.FindIndex(x => x.BranchClass == "WEBAPI" && x.BranchType == EnumPointType.Root)];
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
                        ObjectName = DataSourceName,

                        Objects = ob,

                        DatasourceName = DataSourceName,
                        EventType = "GETDATAPOINT"

                    };
                    // ActionNeeded?.Invoke(this, Passedarguments);
                    Visutil.ShowUserControlInContainer("uc_webapiGetQuery", Visutil.DisplayPanel, DMEEditor, args, Passedarguments);
                }else
                {
                    DMEEditor.ErrorObject.Flag = Errors.Ok;
                    DMEEditor.Logger.WriteLog($"Filling View Entities Web Api");
                    try
                    {
                        bool loadv = false;
                        if (ChildBranchs.Count > 0)
                        {
                            if (Visutil.controlEditor.InputBoxYesNo("Beep", "Do you want to over write th existing View Structure?") == DialogResult.Yes)
                            {
                                TreeEditor.RemoveChildBranchs(this);
                                loadv = true;
                            }
                        }
                        else
                        {
                            loadv = true;
                        }
                        if (loadv)
                        {
                             CreateWebApiEntitiesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        DMEEditor.Logger.WriteLog($"Error in Filling Web Api Entites ({ex.Message}) ");
                        DMEEditor.ErrorObject.Flag = Errors.Failed;
                        DMEEditor.ErrorObject.Ex = ex;
                    }
                    return DMEEditor.ErrorObject;
                }
              

            }
            catch (Exception ex)
            {
                string mes = "Could not Create Query Entity";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        [CommandAttribute(Caption = "Edit", Hidden = false)]
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
        [CommandAttribute(Caption = "Copy Entities")]
        public IErrorsInfo CopyEntities()
        {

            try
            {
                if (BranchType == EnumPointType.Entity)
                {
                    List<string> ents = new List<string>();
                    if (TreeEditor.SelectedBranchs.Count > 0)
                    {
                        if (DataSource == null)
                        {
                            DataSource = DMEEditor.GetDataSource(DataSourceName);
                        }
                        if (DataSource != null)
                        {
                            foreach (int item in TreeEditor.SelectedBranchs)
                            {
                                IBranch br = TreeEditor.GetBranch(item);
                                ents.Add(br.BranchText);
                                // EntityStructure = DataSource.GetEntityStructure(br.BranchText, true);

                            }
                            IBranch pbr = TreeEditor.GetBranch(ParentBranchID);
                            List<ObjectItem> ob = new List<ObjectItem>(); ;
                            ObjectItem it = new ObjectItem();
                            it.obj = pbr;
                            it.Name = "ParentBranch";
                            ob.Add(it);

                            PassedArgs args = new PassedArgs
                            {
                                ObjectName = "DATABASE",
                                ObjectType = "TABLE",
                                EventType = "COPYENTITIES",
                                ParameterString1 = "COPYENTITIES",
                                DataSource = DataSource,
                                DatasourceName = DataSource.DatasourceName,
                                CurrentEntity = BranchText,
                                EntitiesNames = ents,
                                Objects = ob
                            };
                            TreeEditor.args = args;
                            DMEEditor.Passedarguments = args;
                        }
                        else
                        {
                            DMEEditor.AddLogMessage("Fail", "Could not get DataSource", DateTime.Now, -1, null, Errors.Failed);
                        }

                    }
                }
                 

                // TreeEditor.SendActionFromBranchToBranch(pbr, this, "Create View using Table");

            }
            catch (Exception ex)
            {
                string mes = "Could not Copy Entites";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        #endregion Exposed Interface"
        #region "Other Methods"
       
        public async Task<bool> CreateWebApiEntitiesAsync()
        {
            try

            {
                TreeEditor.RemoveChildBranchs(this);
                WebApiEntities webent;
               
                DataSource = DMEEditor.GetDataSource(DataSourceName);

                if (DataSource != null)
                {
                    DataSource.GetEntitesList();
                        
                    if (DataSource.Entities != null)
                    {
                        if (DataSource.Entities.Count > 0)
                        {
                            EntityStructure = DataSource.GetEntityStructure(BranchText, false);
                            List<EntityStructure> rootent = DataSource.Entities.Where(i => i.ParentId == EntityStructure.Id).ToList();

                            if (rootent.Count == 0)
                            {
                                DataSource.GetChildTablesList(EntityStructure.EntityName, EntityStructure.Id.ToString(),null);
                                rootent = DataSource.Entities.Where(i => i.ParentId == EntityStructure.Id).ToList();
                            }
                           
                            TreeEditor.ShowWaiting();
                            CreateEntitiesJob(rootent);
                            TreeEditor.HideWaiting();
                            
                         
                            
                        }
                    }

                }






                DMEEditor.AddLogMessage("Success", $"Generated WebApi node", DateTime.Now, 0, null, Errors.Ok);
                return true;
            }
            catch (Exception ex)
            {
                string errmsg = "Error in Generating App Version";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return false;
            }


        }
        private void CreateEntitiesJob(List<EntityStructure> rootent)
        {
            int cnt = rootent.Count;
            int startcnt = 1;
            TreeEditor.ChangeWaitingCaption($"Getting Web Api Entities/Categories Total:{cnt}");
            foreach (EntityStructure item in rootent)
            {
                string iconimage = "webapi.ico";
                EnumPointType branchType = EnumPointType.Entity;
                if (item.Category == "Category")
                {
                    iconimage = "webapicategory.ico";
                    branchType = EnumPointType.DataPoint;
                }
                WebApiEntities webentmain = new WebApiEntities(TreeEditor, DMEEditor, this, item.EntityName, TreeEditor.SeqID, branchType, iconimage, DataSourceName);
                webentmain.DataSource = DataSource;
                webentmain.DataSourceName = DataSource.DatasourceName;

                TreeEditor.AddBranch(this, webentmain);
                ChildBranchs.Add(webentmain);
                TreeEditor.AddCommentsWaiting($"{startcnt} - Added {item.EntityName} to WebAPI DataSource");
                startcnt += 1;
                CreateNode(DataSource.Entities, item, webentmain);

            }
        }
        private void CreateNode(List<EntityStructure> entities, EntityStructure parententity, IBranch br)
        {
            try
            {

                List<EntityStructure> ls = entities.Where(i => i.ParentId == parententity.Id).ToList();
                WebApiEntities webent;
                foreach (var item in ls)
                {
                    string iconimage = "webapi.ico";
                    EnumPointType branchType = EnumPointType.Entity;
                    if (item.Category == "Category")
                    {
                        iconimage = "webapicategory.ico";
                        branchType = EnumPointType.DataPoint;
                    }
                    webent = new WebApiEntities(TreeEditor, DMEEditor, br, item.EntityName, TreeEditor.SeqID, branchType, iconimage, BranchText);
                    webent.DataSource = DataSource;
                    webent.DataSourceName = DataSource.DatasourceName;
                    TreeEditor.AddBranch(br, webent);
                    ChildBranchs.Add(webent);

                    if (entities.Where(i => i.ParentId == item.Id && i.Id != 0).Any())
                    {
                        CreateNode(entities, item, webent);
                    }
                }

            }
            catch (Exception ex)
            {

                string errmsg = "Error in creating nodes for WebAPI";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
        }
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
