using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.DataView;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Util;
using TheTechIdea.Winforms.VIS;

namespace TheTechIdea.Beep.AppBuilder
{
    public class AppNode : IBranch, ITreeView
    {
        public AppNode()
        {

        }
        public AppNode(ITree pTreeEditor, IDMEEditor pDMEEditor, IBranch pParentNode, string pBranchText, int pID, EnumPointType pBranchType, string pimagename,string ConnectionName)
        {

           

            TreeEditor = pTreeEditor;
            DMEEditor = pDMEEditor;
            ParentBranchID = pParentNode.ID;
            BranchText = pBranchText;
            BranchType = pBranchType;
            IconImageName = pimagename;
            APP = DMEEditor.ConfigEditor.Apps[DMEEditor.ConfigEditor.Apps.FindIndex(x => x.AppName == BranchText)];
            DataViewDS = (DataViewDataSource)DMEEditor.GetDataSource(APP.DataViewDataSourceName);
            DataSourceName = APP.DataViewDataSourceName;
            if (pID != 0)

            {
               ID = pID;
                BranchID = pID;
            }
        }
        #region "Properties"
        public int ID { get; set; }
        public EntityStructure EntityStructure { get; set; }
        public string Name { get; set; }
        public string BranchText { get; set; }
        public IDMEEditor DMEEditor { get; set; }
        public IDataSource DataSource { get; set; }
        public string DataSourceName { get; set; }
        public int Level { get; set; }
        public EnumPointType BranchType { get; set; } = EnumPointType.DataPoint;
        public int BranchID { get; set; }
        public string IconImageName { get; set; }
        public string BranchStatus { get; set; }
        public int ParentBranchID { get; set; }
        public string BranchDescription { get; set; }
        public string BranchClass { get; set; } = "APP";
        public List<IBranch> ChildBranchs { get; set; } = new List<IBranch>();
        public ITree TreeEditor { get; set; }
        public List<string> BranchActions { get; set; }
        public object TreeStrucure { get; set; }
        public IVisUtil Visutil { get; set; }
        public int MiscID { get; set; }
        DataViewDataSource DataViewDS;
        App APP;
        public IDMDataView DataView
        {
            get
            {
                if (DataViewDS != null)
                {
                    return DataViewDS.DataView;
                }else
                {
                    return null;
                }
            
            }
            set
            {
                DataViewDS.DataView = value;
            }
        }
        int DataViewID
        {
            get
            {
                return DataViewDS.DataView.ViewID;
            }
            set
            {
                DataViewDS.DataView.ViewID = value;
            }
        }
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
                APP = DMEEditor.ConfigEditor.Apps[DMEEditor.ConfigEditor.Apps.FindIndex(x => x.AppName == BranchText)];
                DataViewDS = (DataViewDataSource)DMEEditor.GetDataSource(APP.DataViewDataSourceName);
                DataSourceName = APP.DataViewDataSourceName;
                CreateAppEntities();

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

        //[BranchDelegate(Caption = "Save")]
        //public IErrorsInfo SaveApp()
        //{

        //    try
        //    {
        //       // DMEEditor.ConfigEditor.Apps[DMEEditor.ConfigEditor.Apps.FindIndex(x => x.AppName == BranchText)] = APP;
        //        DMEEditor.ConfigEditor.SaveAppValues();

        //        DMEEditor.AddLogMessage("Success", "Saved View", DateTime.Now, 0, null, Errors.Ok);
        //    }
        //    catch (Exception ex)
        //    {
        //        string mes = "Could not Save View";
        //        DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
        //    };
        //    return DMEEditor.ErrorObject;

        //}
        [CommandAttribute(Caption = "Remove")]
        public IErrorsInfo RemoveApp()
        {

            try
            {
                if (Visutil.controlEditor.InputBoxYesNo("Remove App", "Area you Sure ? you want to remove View???") == System.Windows.Forms.DialogResult.Yes)
                {
                   // ConnectionProperties cn = DMEEditor.ConfigEditor.DataConnections.Where(x => x.ConnectionName.ToUpper() == Path.GetFileName(DataView.DataViewDataSourceID).ToUpper()).FirstOrDefault();
                  //  string file = Path.Combine(cn.FilePath, cn.FileName);
                    try
                    {

                        //ds.ViewReader.re.RemoveDataViewByVID(DataView.VID);
                        //   DMEEditor.ConfigEditor.RemoveDataConnection(DataView.DataViewDataSourceID);
                        //  DMEEditor.RemoveDataDource(DataView.DataViewDataSourceID);
                        DMEEditor.ConfigEditor.RemoveAppByID( APP.ID);
                        DMEEditor.ConfigEditor.SaveAppValues();
                        DMEEditor.AddLogMessage("Success", "Removed App", DateTime.Now, 0, null, Errors.Ok);
                    }
                    catch (Exception ex)
                    {
                        string mes = "Could not Remove App";
                        DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
                    };

                   
                    TreeEditor.RemoveBranch(this);

                }

                
                DMEEditor.AddLogMessage("Success", "Remove App", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Remove App";
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
                IBranch RootAppBranch = TreeEditor.Branches[TreeEditor.Branches.FindIndex(x => x.BranchClass == "APP" && x.BranchType == EnumPointType.Root)];
                it = new ObjectItem();
                it.obj = RootAppBranch;
                it.Name = "RootAppBranch";
                ob.Add(it);
                if (DataView != null)
                {
                    PassedArgs Passedarguments = new PassedArgs
                    {
                        Addin = null,
                        AddinName = null,
                        AddinType = "",
                        DMView = DataView,
                        CurrentEntity = BranchText,
                        Id = 0,
                        ObjectType = "QUERYENTITY",
                        DataSource = null,
                        ObjectName = DataView.ViewName,

                        Objects = ob,
                        ParameterString1 = APP.ID,
                        DatasourceName = null,
                        EventType = "CREATAPP"

                    };
                    // ActionNeeded?.Invoke(this, Passedarguments);
                    Visutil.ShowUserControlPopUp("uc_AppCreateDefinition", DMEEditor, args, Passedarguments);

                }
                else
                {
                    DMEEditor.AddLogMessage("Fail", "Missing DataView", DateTime.Now, 0, null, Errors.Ok);
                }
               
              //  DMEEditor.AddLogMessage("Success", "Created Query Entity", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Open App Generator";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        [CommandAttribute(Caption = "Edit")]
        public IErrorsInfo UpdateApp()
        {

            try
            {
                string[] args = { "New Query Entity", null, null };
                List<ObjectItem> ob = new List<ObjectItem>(); ;
                ObjectItem it = new ObjectItem();
                it.obj = this;
                it.Name = "Branch";
                ob.Add(it);
                IBranch RootCompositeLayerBranch = TreeEditor.Branches[TreeEditor.Branches.FindIndex(x => x.BranchClass == "APP" && x.BranchType == EnumPointType.Root)];
                it = new ObjectItem();
                it.obj = RootCompositeLayerBranch;
                it.Name = "RootAppBranch";
                
                ob.Add(it);
                if (DataView != null)
                {
                    PassedArgs Passedarguments = new PassedArgs
                    {
                        Addin = null,
                        AddinName = null,
                        AddinType = "",
                        DMView = DataView,
                        CurrentEntity = BranchText,
                        Id = DataView.Entities[0].Id,
                        ObjectType = "QUERYENTITY",
                        DataSource = null,
                        ObjectName = DataView.ViewName,
                        ParameterString1 = APP.ID,
                        Objects = ob,

                        DatasourceName = null,
                        EventType = "CREATAPP"

                    };
                    // ActionNeeded?.Invoke(this, Passedarguments);
                    Visutil.ShowUserControlPopUp("uc_App", DMEEditor, args, Passedarguments);

                    DMEEditor.AddLogMessage("Success", "Created Query Entity", DateTime.Now, 0, null, Errors.Ok);
                }else
                {
                    PassedArgs Passedarguments = new PassedArgs
                    {
                        Addin = null,
                        AddinName = null,
                        AddinType = "",
                        DMView = null,
                        CurrentEntity = BranchText,
                        Id = 0,
                        ObjectType = "MISSINGVIEW",
                        DataSource = null,
                        ObjectName = null,

                        Objects = ob,
                        ParameterString1 = APP.ID,
                        DatasourceName = null,
                        EventType = "EDITAPP"

                    };
                    // ActionNeeded?.Invoke(this, Passedarguments);
                    Visutil.ShowUserControlPopUp("uc_App", DMEEditor, args, Passedarguments);
                    DMEEditor.AddLogMessage("Fail", "Missing DataView", DateTime.Now, 0, null, Errors.Ok);
                }
               
            }
            catch (Exception ex)
            {
                string mes = "Could not Create Query Entity";
                DMEEditor.AddLogMessage("Fail", mes + ex.Message, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        #endregion Exposed Interface"
        #region "Other Methods"
        private IBranch CreateAppversionNode(IAppVersion item)
        {
            AppEntitiesNode appVersion = null; 
            try
            {

                appVersion = new AppEntitiesNode(TreeEditor, DMEEditor, this, BranchText, TreeEditor.SeqID, EnumPointType.DataPoint, "app.ico", item);
                TreeEditor.AddBranch(this, appVersion);
                appVersion.CreateChildNodes();
                ChildBranchs.Add(appVersion);

                DMEEditor.AddLogMessage("Success", "Added App Version", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Add App Version";
               
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
                return null;
            };

            return appVersion;
        }
        public bool CreateAppEntities()
        {
            try

            {
                TreeEditor.RemoveChildBranchs(this);
                AppEntitiesNode appVersion;
                foreach (IAppVersion item in APP.AppVersions)
                {
                    appVersion = new AppEntitiesNode(TreeEditor, DMEEditor, this, BranchText, TreeEditor.SeqID, EnumPointType.Entity, "app.ico",item);
                    TreeEditor.AddBranch(this, appVersion);
                    appVersion.CreateChildNodes();
                    ChildBranchs.Add(appVersion);
                }

                DMEEditor.AddLogMessage("Success", $"Generated App Version", DateTime.Now, 0, null, Errors.Ok);
                return true;
            }
            catch (Exception ex)
            {
                string errmsg = "Error in Generating App Version";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return false;
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
