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
    public class WebApiNode : IBranch, ITreeView
    {
        public WebApiNode(ITree pTreeEditor, IDMEEditor pDMEEditor, IBranch pParentNode, string pBranchText, int pID, EnumBranchType pBranchType, string pimagename)
        {
            TreeEditor = pTreeEditor;
            DMEEditor = pDMEEditor;
            ParentBranchID = pParentNode.ID;
            BranchText = pBranchText;
            BranchType = pBranchType;
            IconImageName = pimagename;
            DataSourceName = pBranchText;
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
        public EnumBranchType BranchType { get; set; } = EnumBranchType.DataPoint;
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

                CreateWebApiEntities();

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

       
        [BranchDelegate(Caption = "Get DataPoint")]
        public IErrorsInfo GetDataPoint()
        {

            try
            {
                string[] args = { "New Query Entity", null, null };
                List<ObjectItem> ob = new List<ObjectItem>(); ;
                ObjectItem it = new ObjectItem();
                it.obj = this;
                it.Name = "Branch";
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
                    Id = 0,
                    ObjectType = "WEBAPI",
                    DataSource = null,
                    ObjectName = TreeEditor.Branches[this.ParentBranchID].BranchText,

                    Objects = ob,

                    DatasourceName = TreeEditor.Branches[this.ParentBranchID].BranchText,
                    EventType = "GETDATAPOINT"

                };
                // ActionNeeded?.Invoke(this, Passedarguments);
                Visutil.ShowUserControlPopUp("uc_AppCreateDefinition", DMEEditor, args, Passedarguments);

                //  DMEEditor.AddLogMessage("Success", "Created Query Entity", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Open App Generator";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
       
        #endregion Exposed Interface"
        #region "Other Methods"
      
        public bool CreateWebApiEntities()
        {
            try

            {
                TreeEditor.RemoveChildBranchs(this);
                WebApiEntities webent;
                List<EntityStructure> ls = new List<EntityStructure>();
                ls = DMEEditor.ConfigEditor.DataConnections.Where(i => i.ConnectionName == BranchText).FirstOrDefault().Entities;
                if (ls != null)
                {
                    foreach (EntityStructure item in ls)
                    {

                        webent = new WebApiEntities(TreeEditor, DMEEditor, this, item.EntityName, TreeEditor.SeqID, EnumBranchType.Entity, "app.ico",BranchText);
                        TreeEditor.AddBranch(this, webent);
                        ChildBranchs.Add(webent);
                        //  webent.CreateChildNodes();
                    }
                }
                
                    IDataSource ds = DMEEditor.GetDataSource(BranchText);
                    ds.GetEntitesList();
                if (ds.Entities != null)
                {
                    if (ds.Entities.Count > 0)
                    {
                        ls = ds.Entities;
                        List<EntityStructure> rootent = ls.Where(i => i.ParentId == 0 ).ToList();
                        foreach (EntityStructure item in rootent)
                        {
                            CreateNode(ls, item, this);
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
        private void CreateNode(List<EntityStructure> entities,EntityStructure parententity,IBranch br)
        {
            try
            {
                List<EntityStructure> ls = entities.Where(i => i.ParentId == parententity.Id).ToList();
                WebApiEntities webent;
                foreach (var item in ls)
                {
                    webent = new WebApiEntities(TreeEditor, DMEEditor, br, item.EntityName, TreeEditor.SeqID, EnumBranchType.Entity, "app.ico",BranchText);
                    TreeEditor.AddBranch(br, webent);
                    ChildBranchs.Add(webent);
                   
                    if (entities.Where(i => i.ParentId == item.Id && i.Id != 0).Any())
                    {
                        CreateNode(entities, item,webent);
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
        [BranchDelegate(Caption = "Defaults")]
        public IErrorsInfo EditDefaults()
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            //   DMEEditor.Logger.WriteLog($"Filling Database Entites ) ");
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
                    CurrentEntity = BranchText,
                    Id = 0,
                    ObjectType = "DEFAULTS",
                    DataSource = null,
                    ObjectName = BranchText,

                    Objects = ob,

                    DatasourceName = BranchText,
                    EventType = "DEFAULTS"

                };
                // ActionNeeded?.Invoke(this, Passedarguments);
                Visutil.ShowUserControlPopUp("uc_datasourceDefaults", DMEEditor, args, Passedarguments);



            }
            catch (Exception ex)
            {
                DMEEditor.Logger.WriteLog($"Error in Filling Database Entites ({ex.Message}) ");
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Ex = ex;
            }
            return DMEEditor.ErrorObject;

        }
        #endregion"Other Methods"

    }
}
