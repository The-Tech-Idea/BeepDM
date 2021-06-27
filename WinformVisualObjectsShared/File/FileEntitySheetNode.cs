using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Vis;
using TheTechIdea.Util;

namespace TheTechIdea.Winforms.VIS
{
    public class FileEntitySheetNode : IBranch, ITreeView
    {
        public FileEntitySheetNode()
        {

        }
        public FileEntitySheetNode(ITree pTreeEditor, IDMEEditor pDMEEditor, IBranch pParentNode, string pBranchText, int pID, EnumPointType pBranchType, string pimagename, string ConnectionName)
        {



            TreeEditor = pTreeEditor;
            DMEEditor = pDMEEditor;
            ParentBranchID = pParentNode.ID;
            BranchText = pBranchText;
            BranchType = pBranchType;
            DataSourceName = pParentNode.DataSourceName;
            IconImageName = "sheet.ico";

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
        public string BranchText { get; set; } = "Files";
        public IDMEEditor DMEEditor { get; set; }
        public IDataSource DataSource { get; set; }
        public string DataSourceName { get; set; }
        public int Level { get; set; }
        public EnumPointType BranchType { get; set; } = EnumPointType.Entity;
        public int BranchID { get; set; }
        public string IconImageName { get; set; } = "sheet.ico";
        public string BranchStatus { get; set; }
        public int ParentBranchID { get; set; }
        public string BranchDescription { get; set; }
        public string BranchClass { get; set; } = "FILE";
        public List<IBranch> ChildBranchs { get; set; } = new List<IBranch>();
        public ITree TreeEditor { get; set; }
        public List<string> BranchActions { get; set; }
        public object TreeStrucure { get; set; }
        public IVisUtil Visutil { get; set; }
        public int MiscID { get; set; }

     
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
                CreateNodes();

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
        [CommandAttribute(Caption = "View Structure", Hidden = false, DoubleClick = true)]
        public IErrorsInfo ViewStructure()
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
                    ObjectType = "FILEENTITY",
                    DataSource = DataSource,
                    ObjectName = DataSource.DatasourceName,
                    Objects = ob,
                    DatasourceName = DataSource.DatasourceName,
                    EventType = "FILEENTITY"

                };
                //ActionNeeded?.Invoke(this, Passedarguments);
                Visutil.ShowUserControlPopUp("uc_DataEntityStructureViewer", DMEEditor, args, Passedarguments);



                //  DMEEditor.AddLogMessage("Success", "Edit Control Shown", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not show Edit Control";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        [CommandAttribute(Caption = "Show", Hidden = false,  DoubleClick =true)]
        public IErrorsInfo Show()
        {

            try
            {
                List<ObjectItem> ob = new List<ObjectItem>(); ;
                ObjectItem it = new ObjectItem();
                it.obj = this;
                it.Name = "Branch";
                ob.Add(it);
                string[] args = new string[] { BranchText, DataSource.Dataconnection.ConnectionProp.SchemaName, null };
                PassedArgs Passedarguments = new PassedArgs
                
                {  
                    
                    CurrentEntity = BranchText,
                    Id = BranchID,
                    ObjectType = "FILE",
                    DataSource = DataSource,
                    ObjectName = BranchText,
                    Objects = ob,
                    DatasourceName = DataSource.DatasourceName,
                    EventType = "FILEENTITY"


                };
                //  Visutil.ShowUserControlInContainer("uc_txtfileManager", Visutil.DisplayPanel, DMEEditor, args, Passedarguments);
                Visutil.ShowUserControlInContainer("uc_getentities", Visutil.DisplayPanel, DMEEditor, args, Passedarguments);
            //    DMEEditor.AddLogMessage("Success", "Show File", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Show File";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        [CommandAttribute(Caption = "Copy Entities")]
        public IErrorsInfo CopyEntities()
        {

            try
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
                            ObjectName = "FILE",
                            ObjectType = "FILE",
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
        public IErrorsInfo CreateNodes()
        {

            try
            {


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
