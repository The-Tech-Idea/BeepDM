using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.CompositeLayer;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Vis;
using TheTechIdea.Util;

namespace TheTechIdea.Winforms.VIS
{
    public class CompositeLayerEntitesNode : IBranch, ITreeView
    {
        public CompositeLayerEntitesNode()
        {

        }
        public CompositeLayerEntitesNode(ITree pTreeEditor, IDMEEditor pDMEEditor, IBranch pParentNode, EntityStructure entity, int pID, EnumBranchType pBranchType, string pimagename,IDataSource ds)
        {
            DataSource = ds;
            TreeEditor = pTreeEditor;
            DMEEditor = pDMEEditor;
            ParentBranchID = pParentNode.ID;
            BranchText = entity.EntityName;
            BranchType =  EnumBranchType.Entity;
            IconImageName = "databaseentities.ico";
            DataSource = ds;
            compositeLayerDataSource = (CompositeLayerDataSource)ds;
            DataSourceName = ds.DatasourceName;
            EntityStructure = entity;
            
            if (pID != 0)
            {
                ID = pID;
                BranchID = ID;
            }
        }
        public int ID { get ; set ; }
        public EntityStructure EntityStructure { get; set; }
        public int Order { get; set; }
        public string Name { get; set; } = "";
        public string BranchText { get ; set ; }
        public IDMEEditor DMEEditor { get ; set ; }
        public IDataSource DataSource { get ; set ; }
        public string DataSourceName { get; set; }
        public int Level { get ; set ; }
        public EnumBranchType BranchType { get; set; } = EnumBranchType.Entity;
        public int BranchID { get ; set ; }
        public string IconImageName { get ; set ; }
        public string BranchStatus { get ; set ; }
        public int ParentBranchID { get ; set ; }
        public string BranchDescription { get ; set ; }
        public string BranchClass { get; set; } = "CLAYER";
        public List<IBranch> ChildBranchs { get ; set ; }
        public ITree TreeEditor { get ; set ; }
        public List<string> BranchActions { get ; set ; }
        public object TreeStrucure { get ; set ; }
        public IVisUtil Visutil { get ; set ; }
        public int MiscID { get; set; }
        public CompositeLayerDataSource compositeLayerDataSource { get; set; }
       // public event EventHandler<PassedArgs> BranchSelected;
       // public event EventHandler<PassedArgs> BranchDragEnter;
       // public event EventHandler<PassedArgs> BranchDragDrop;
       // public event EventHandler<PassedArgs> BranchDragLeave;
       // public event EventHandler<PassedArgs> BranchDragClick;
       // public event EventHandler<PassedArgs> BranchDragDoubleClick;
       // public event EventHandler<PassedArgs> ActionNeeded;

        public IErrorsInfo CreateChildNodes()
        {
            throw new NotImplementedException();
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

                DMEEditor.AddLogMessage("Success", "Set Config OK", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Set Config";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;

        }

        #region "Methods"
        [BranchDelegate(Caption = "Single Record CRUD")]
        public IErrorsInfo CRUDSingleRecord()
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
                    Addin = null,
                    AddinName = null,
                    AddinType = "",
                    DMView = null,
                    CurrentEntity = BranchText,
                    Id = BranchID,
                    ObjectType = "RDBMSTABLE",
                    DataSource = DataSource,
                    ObjectName = BranchText,
                    Objects = ob,
                    DatasourceName = DataSource.DatasourceName,
                    EventType = "CRUDENTITY"

                };
                //ActionNeeded?.Invoke(this, Passedarguments);

                 Visutil.ShowUserControlInContainer("Uc_DataTableSingleRecordEdit", Visutil.DisplayPanel, DMEEditor, args, Passedarguments);


                DMEEditor.AddLogMessage("Success", "Created Crud Single Record", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Create Crud Single Record";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        [BranchDelegate(Caption = "Grid CRUD")]
        public IErrorsInfo CRUDGrid()
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
                    Addin = null,
                    AddinName = null,
                    AddinType = "",
                    DMView = null,
                    CurrentEntity = BranchText,
                    Id = BranchID,
                    ObjectType = "RDBMSTABLE",
                    DataSource = DataSource,
                    ObjectName = BranchText,
                    Objects = ob,
                    DatasourceName = DataSource.DatasourceName,
                    EventType = "CRUDENTITY"

                };
             

                 Visutil.ShowUserControlInContainer("Uc_DataTableGridEdit", Visutil.DisplayPanel, DMEEditor, args, Passedarguments);



              //  DMEEditor.AddLogMessage("Success", "Added Database Connection", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Create CRUD GRID";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        [BranchDelegate(Caption = "Create View")]
        public IErrorsInfo CreateView()
        {
           
            try
            {
                PassedArgs args = new PassedArgs
                {
                    ObjectName = "DATABASE",
                    ObjectType = "TABLE",
                    EventType="CREATEVIEWBASEDONTABLE",
                     ParameterString1= "Create View using Table",
                    Objects = new List<ObjectItem> { new ObjectItem { Name = "Branch", obj = this } }
                };
                TreeEditor.args = args;
                IBranch pbr = TreeEditor.Branches.Where(x => x.BranchType == EnumBranchType.Root && x.BranchClass == "VIEW").FirstOrDefault();
                TreeEditor.SendActionFromBranchToBranch(pbr,this, "Create View using Table");

            }
            catch (Exception ex)
            {
                string mes = "Could not Added View ";
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
                 it = new ObjectItem();
                it.obj = compositeLayerDataSource;
                it.Name = "Clayer";
                ob.Add(it);
                PassedArgs Passedarguments = new PassedArgs
                {
                    Addin = null,
                    AddinName = null,
                    AddinType = "",
                    DMView = null,
                    CurrentEntity = BranchText,
                    Id = BranchID,
                    ObjectType = "RDBMSENTITY",
                    DataSource = DataSource,
                    ObjectName =compositeLayerDataSource.LayerInfo.LayerName,
                    Objects = ob,
                    DatasourceName = EntityStructure.DataSourceID,
                    EventType = "CLAYERENTITY"

                };
                //ActionNeeded?.Invoke(this, Passedarguments);
                Visutil.ShowUserControlPopUp("Uc_DataViewEntityEditor", DMEEditor, args, Passedarguments);



                DMEEditor.AddLogMessage("Success", "Edit Control Shown", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not show Edit Control";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        [BranchDelegate(Caption = "Field Properties")]
        public IErrorsInfo FieldProperties()
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
          
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
                    Id = ID,
                    ObjectType = "ENTITY",
                    DataSource = DataSource,
                    ObjectName = DataSourceName,

                    Objects = ob,

                    DatasourceName = DataSourceName,
                    EventType = "ENTITY"

                };
                // ActionNeeded?.Invoke(this, Passedarguments);
                Visutil.ShowUserControlPopUp("uc_fieldproperty", DMEEditor, args, Passedarguments);
            }
            catch (Exception ex)
            {
                DMEEditor.Logger.WriteLog($"Error in Filling Database Entites ({ex.Message}) ");
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Ex = ex;
            }
            return DMEEditor.ErrorObject;

        }
        #endregion
    }
}
