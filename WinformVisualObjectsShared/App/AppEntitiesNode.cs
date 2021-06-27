using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.DataView;
using TheTechIdea.DataManagment_Engine.Vis;
using TheTechIdea.Util;
using TheTechIdea.Winforms.VIS;

namespace TheTechIdea.DataManagment_Engine.AppBuilder
{
    public class AppEntitiesNode : IBranch, ITreeView
    {
        public AppEntitiesNode()
        {

        }
        public AppEntitiesNode(ITree pTreeEditor, IDMEEditor pDMEEditor, IBranch pParentNode, string pBranchText, int pID, EnumPointType pBranchType, string pimagename,IAppVersion pappVersion)
        {
            TreeEditor = pTreeEditor;
            DMEEditor = pDMEEditor;
            ParentBranchID = pParentNode.BranchID;
            BranchType = pBranchType;
            AppVersion = (AppVersion) pappVersion;
            APP = DMEEditor.ConfigEditor.Apps[DMEEditor.ConfigEditor.Apps.FindIndex(x => x.AppName == pBranchText)];
            DataSourceName = APP.DataViewDataSourceName;
            ID = AppVersion.Ver;
            switch (AppVersion.Apptype)
            {
                case AppType.Winform:
                    IconImageName = "windows_client.ico";
                    break;
                case AppType.Andriod:
                    IconImageName = "andriod.ico";
                    break;
                case AppType.Web:
                    IconImageName = "web.ico";
                    break;
                case AppType.IOS:
                    IconImageName = "ios.ico";
                    break;
                case AppType.Linux:
                    IconImageName = "linux.ico";
                    break;
                case AppType.WPF:
                    IconImageName = "wpf.ico";
                    break;
                default:
                    IconImageName = "category.ico";
                    break;
            }
            BranchText = "ver:"+ AppVersion.Ver.ToString();

            if (pID != 0)
            {
               
                BranchID = pID;
            }
            DataViewDS = (DataViewDataSource)DMEEditor.GetDataSource(APP.DataViewDataSourceName);
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
        public EnumPointType BranchType { get; set; } = EnumPointType.Entity;
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
        AppVersion AppVersion;
        App APP;
        DataViewDataSource DataViewDS;
        public IDMDataView DataView
        {
            get
            {
                return DataViewDS.DataView;
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
       // public EntityStructure EntityStructure { get; set; }

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

               

                //    DMEEditor.AddLogMessage("Success", "Added Database Connection", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Add Database Connection";
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

            try
            {
                if (Visutil.controlEditor.InputBoxYesNo("DM Engine","Are you sure you want to remove Entities?")==System.Windows.Forms.DialogResult.Yes)
                {
                    foreach (IBranch item in ChildBranchs)
                    {
                        TreeEditor.RemoveBranch(item);
                        DataViewDS.RemoveEntity(EntityStructure.Id);
                       //  DMEEditor.viewEditor.Views.Where(x => x.ViewName == DataView.ViewName).FirstOrDefault().Entity.Remove(EntityStructure);
                    }


                    DMEEditor.AddLogMessage("Success", "Removed Branch Successfully", DateTime.Now, 0, null, Errors.Ok);
                }
               
            }
            catch (Exception ex)
            {
                string mes = "Could not Removed Branch Successfully";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
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
                }

                DMEEditor.AddLogMessage("Success", "Set Config OK", DateTime.Now, 0, null, Errors.Ok);
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
        [CommandAttribute(Caption = "Edit", Hidden = false)]
        public IErrorsInfo EditAPP()
        {

            try
            {
                int ver;
                string[] args = { "New Query Entity", null, null };
                List<ObjectItem> ob = new List<ObjectItem>(); ;
                ObjectItem it = new ObjectItem();
                it.obj = this;
                it.Name = "Branch";
                ob.Add(it);
                IBranch RootAppBranch = TreeEditor.Branches[TreeEditor.Branches.FindIndex(x => x.BranchID == ParentBranchID)];
                it = new ObjectItem();
                it.obj = RootAppBranch;
                it.Name = "RootAppBranch";
                ob.Add(it);
                if (APP.AppVersions != null)
                {
                    ver = APP.AppVersions.Max(b => b.Ver) + 1;
                }else
                {
                    ver = 1;
                }
                PassedArgs Passedarguments = new PassedArgs
                {
                    Addin = null,
                    AddinName = null,
                    AddinType = "",
                    DMView = DataView,
                    CurrentEntity = RootAppBranch.BranchText,
                    Id = ver,
                    ObjectType = "QUERYENTITY",
                    DataSource = null,
                    ObjectName = DataView.ViewName,

                    Objects = ob,

                    DatasourceName = null,
                    EventType = "CREATAPP"

                };
                // ActionNeeded?.Invoke(this, Passedarguments);
                Visutil.ShowUserControlPopUp("uc_AppCreateDefinition", DMEEditor, args, Passedarguments);



                DMEEditor.AddLogMessage("Success", "Edit Control Shown", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not show Edit Control";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        [CommandAttribute(Caption = "Generate", Hidden = false)]
        public IErrorsInfo GenerateAPP()
        {

            try
            {
                int ver;
                string[] args = { "New Query Entity", null, null };
                List<ObjectItem> ob = new List<ObjectItem>(); ;
                ObjectItem it = new ObjectItem();
                it.obj = this;
                it.Name = "Branch";
                ob.Add(it);
                IBranch RootAppBranch = TreeEditor.Branches[TreeEditor.Branches.FindIndex(x=>x.BranchID==ParentBranchID)];
                ObjectItem it1 = new ObjectItem();
                it1.obj = RootAppBranch;
                it1.Name = "RootAppBranch";
                ob.Add(it1);
                if (APP.AppVersions != null)
                {
                    ver = APP.AppVersions.Max(b => b.Ver) + 1;
                }
                else
                {
                    ver = 1;
                }
                ObjectItem v = new ObjectItem { Name = "VISUTIL", obj =Visutil };
                ob.Add(v);
                PassedArgs Passedarguments = new PassedArgs
                {
                    Addin = null,
                    AddinName = null,
                    AddinType = "",
                    DMView = DataView,
                    CurrentEntity = APP.AppName,
                    Id = ver,
                    ObjectType = "QUERYENTITY",
                    DataSource = null,
                    ObjectName = DataView.ViewName,

                    Objects = ob,

                    DatasourceName = null,
                    EventType = "CREATAPP"

                };
                DMEEditor.Passedarguments = Passedarguments;
                if (DMEEditor.ConfigEditor.AppWritersClasses != null)
                {
                    if (DMEEditor.ConfigEditor.AppWritersClasses.Where(o => o.className == AppVersion.GeneratorName).Any())
                    {
                        string pkname = DMEEditor.ConfigEditor.AppWritersClasses.Where(o => o.className == AppVersion.GeneratorName).FirstOrDefault().PackageName;
                        IAppBuilder appBuilder = (IAppBuilder)DMEEditor.assemblyHandler.GetInstance(pkname);
                        appBuilder.BuildApp(DMEEditor, Passedarguments);
                        DMEEditor.AddLogMessage("Success", "Edit Control Shown", DateTime.Now, 0, null, Errors.Ok);
                    }
                    else
                    {
                        DMEEditor.AddLogMessage("Fail", "Cannot Find App Generator Class", DateTime.Now, 0, null, Errors.Ok);
                        Visutil.controlEditor.MsgBox("Beep", "Cannot Find App Generator Class");
                    }

                }
                else
                {
                    DMEEditor.AddLogMessage("Fail", "Cannot Find any App Generators", DateTime.Now, 0, null, Errors.Ok);
                    Visutil.controlEditor.MsgBox("Beep", "Cannot Find any App Generators");
                }
               
                
            }
            catch (Exception ex)
            {
                string mes = "Could not show Edit Control";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        [CommandAttribute(Caption = "Remove", Hidden = false)]
        public IErrorsInfo RemoveEntity()
        {

            try
            {
                
                if (Visutil.controlEditor.InputBoxYesNo("DM Engine", "Are you sure you want to remove Deployed Application?") == System.Windows.Forms.DialogResult.Yes)
                {
                  
                    //---- Remove From View ---- //
                    APP.AppVersions.Remove(AppVersion);
                    DMEEditor.ConfigEditor.SaveAppValues();
                    TreeEditor.RemoveBranch(this);
                    DMEEditor.AddLogMessage("Success", "Removed Deployed Application", DateTime.Now, 0, null, Errors.Ok);
                }


                //   ActionNeeded?.Invoke(this, Passedarguments);

            }
            catch (Exception ex)
            {
                string mes = "Could not Entity Node";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        #endregion Exposed Interface"
        #region "Other Methods"

        #endregion"Other Methods"
    }
}
