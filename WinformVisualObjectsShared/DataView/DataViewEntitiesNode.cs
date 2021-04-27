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

namespace TheTechIdea.Winforms.VIS
{
    public class DataViewEntitiesNode : IBranch, ITreeView
    {
        public DataViewEntitiesNode()
        {

        }
        public DataViewEntitiesNode(ITree pTreeEditor, IDMEEditor pDMEEditor, IBranch pParentNode, string pBranchText, int pID, EnumBranchType pBranchType, string pimagename, string pDSName, EntityStructure entityStructure)
        {



            TreeEditor = pTreeEditor;
            DMEEditor = pDMEEditor;
            ParentBranchID = pParentNode.BranchID;

            BranchType = pBranchType;
            IconImageName = pimagename;

            ds = (DataViewDataSource)DMEEditor.GetDataSource(pDSName);
            DataView = ds.DataView;
            EntityStructure = entityStructure;
            if (string.IsNullOrEmpty(entityStructure.Caption) || string.IsNullOrWhiteSpace(entityStructure.Caption))
            {
                entityStructure.Caption = entityStructure.EntityName;
            }
            if (string.IsNullOrEmpty(entityStructure.DatasourceEntityName) || string.IsNullOrWhiteSpace(entityStructure.DatasourceEntityName))
            {
                entityStructure.DatasourceEntityName = entityStructure.EntityName;
            }
            BranchText = entityStructure.Caption;
            MiscID = entityStructure.Id;
            DataSourceName = entityStructure.DataSourceID;
            ID = MiscID;
            if (pID != 0)
            {
               
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
        public EnumBranchType BranchType { get; set; } = EnumBranchType.Entity;
        public int BranchID { get; set; }
        public string IconImageName { get; set; }
        public string BranchStatus { get; set; }
        public int ParentBranchID { get; set; }
        public string BranchDescription { get; set; }
        public string BranchClass { get; set; } = "VIEW";
        public List<IBranch> ChildBranchs { get; set; } = new List<IBranch>();
        public ITree TreeEditor { get; set; }
        public List<string> BranchActions { get; set; }
        public object TreeStrucure { get; set; }
        public IVisUtil Visutil { get; set; }
        public int MiscID { get; set; }
        DataViewDataSource ds;
        public IDMDataView DataView
        {
            get
            {
                return ds.DataView;
            }
            set
            {
                ds.DataView = value;
            }
        }
        int DataViewID
        {
            get
            {
                return ds.DataView.ViewID;
            }
            set
            {
                ds.DataView.ViewID = value;
            }
        }
      
        #endregion "Properties"
        #region "Interface Methods"
     
        private void GetChildNodes(List<EntityStructure> Childs,EntityStructure Parent, IBranch ParentBranch)
        {

            DataViewEntitiesNode dbent = null ;
           
            foreach (EntityStructure i in Childs)
            {
                IBranch branch = TreeEditor.GetBranchByMiscID(i.Id);
                if (branch == null)
                {
                   
                    dbent = new DataViewEntitiesNode(TreeEditor, DMEEditor, ParentBranch, i.EntityName, TreeEditor.SeqID, EnumBranchType.Entity, ds.GeticonForViewType(i.Viewtype), DataView.DataViewDataSourceID, i);
                    TreeEditor.AddBranch(ParentBranch, dbent);
                    dbent.CreateChildNodes();
                    ChildBranchs.Add(dbent);
                }
                else
                {
                    if (!ChildBranchs.Where(x => x.BranchText == i.EntityName).Any())
                    {
                       
                        dbent = new DataViewEntitiesNode(TreeEditor, DMEEditor, ParentBranch, i.EntityName, TreeEditor.SeqID, EnumBranchType.Entity, ds.GeticonForViewType(i.Viewtype), DataView.DataViewDataSourceID, i);
                        TreeEditor.AddBranch(ParentBranch, dbent);
                        dbent.CreateChildNodes();
                        ChildBranchs.Add(dbent);
                    }
                    else
                    {
                        dbent =(DataViewEntitiesNode) branch;
                    }

                }
                List<EntityStructure> otherchilds = DataView.Entities.Where(cx => (cx.Id != i.Id) && (cx.ParentId == i.Id)).ToList();
                if (otherchilds != null)
                {
                    if (otherchilds.Count > 0)
                    {
                        GetChildNodes(otherchilds, i, dbent);
                    }
                }

            }
        }
        public IErrorsInfo CreateChildNodes()
        {

            try
            {
                IBranch dbent;
               
                List<EntityStructure> cr = DataView.Entities.Where(cx => (cx.Id != EntityStructure.Id) && (cx.ParentId == EntityStructure.Id) ).ToList();
                foreach (EntityStructure i in cr)
                {
                    if (ChildBranchs.Where(x=>x.BranchText== i.EntityName).Any()==false)
                    {
                        
                        dbent = new DataViewEntitiesNode(TreeEditor, DMEEditor, this, i.EntityName, TreeEditor.SeqID, EnumBranchType.Entity, ds.GeticonForViewType(i.Viewtype), DataView.DataViewDataSourceID, i);
                        TreeEditor.AddBranch(this, dbent);
                        dbent.CreateChildNodes();
                        ChildBranchs.Add(dbent);
                    }
                    else
                    {
                        dbent = ChildBranchs.Where(x => x.BranchText == i.EntityName).FirstOrDefault();
                        dbent.CreateChildNodes();
                    }

                    List<EntityStructure> childs = DataView.Entities.Where(cx => (cx.Id != i.Id) && (cx.ParentId == i.Id)).ToList();
                    if (childs != null)
                    {
                        if (childs.Count > 0)
                        {
                            GetChildNodes(childs, i, this);
                        }
                    }



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
                        ds.RemoveEntity(EntityStructure.Id);
                        // DMEEditor.viewEditor.Views.Where(x => x.ViewName == DataView.ViewName).FirstOrDefault().Entity.Remove(EntityStructure);
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
        #endregion "Interface Methods"
        #region "Exposed Interface"
        private List<ObjectItem> Createlistofitems()
        {
            List<ObjectItem> ob = new List<ObjectItem>(); 
            ObjectItem it = new ObjectItem();
            it.obj = this;
            it.Name = "Branch";
            ob.Add(it);
            ObjectItem EntityStructureit = new ObjectItem();
            EntityStructureit.obj = EntityStructure;
            EntityStructureit.Name = "EntityStructure";
            ob.Add(EntityStructureit);
            return ob;
        }
        [BranchDelegate(Caption = "Edit", Hidden = false, iconimage = "edit_entity.ico")]
        public IErrorsInfo EditEntity()
        {

            try
            {
                EntityStructure = ds.Entities[ds.Entities.FindIndex(o => o.Id == EntityStructure.Id)];
                string[] args = { "New View", null, null };
              
                PassedArgs Passedarguments = new PassedArgs
                {
                    Addin = null,
                    AddinName = null,
                    AddinType = "",
                    DMView = DataView,
                    CurrentEntity = EntityStructure.DatasourceEntityName,
                    Id = BranchID,
                    ObjectType = "VIEWENTITY",
                    DataSource = DataSource,
                    ObjectName = DataView.ViewName,
                    Objects = Createlistofitems(),
                    DatasourceName = EntityStructure.DataSourceID,
                    EventType = "VIEWENTITY"

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
        [BranchDelegate(Caption = "Remove", Hidden = false, iconimage = "remove.ico")]
        public IErrorsInfo RemoveEntity()
        {

            try
            {
                string[] args = { "New View", null, null };
               
                PassedArgs Passedarguments = new PassedArgs
                {
                    Addin = null,
                    AddinName = null,
                    AddinType = "",
                    DMView = DataView,
                    CurrentEntity = EntityStructure.DatasourceEntityName,
                    Id = BranchID,
                    ObjectType = "VIEWENTITY",
                    DataSource = DataSource,
                    ObjectName = DataView.ViewName,
                    Objects = Createlistofitems(),
                    EventType = "REMOVEENTITY"

                };
                if (Visutil.controlEditor.InputBoxYesNo("DM Engine","Are you sure you want to remove Entity?") == System.Windows.Forms.DialogResult.Yes)
                {
                    TreeEditor.RemoveBranch(this);
                    //---- Remove From View ---- //
                    ds.RemoveEntity(EntityStructure.Id);
                    DMEEditor.AddLogMessage("Success", "Removed Entity Node", DateTime.Now, 0, null, Errors.Ok);
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
        [BranchDelegate(Caption = "Get Childs", Hidden = false, iconimage = "get_childs.ico")]
        public IErrorsInfo GetChilds()
        {

            try
            {
                DataSource = DMEEditor.GetDataSource(EntityStructure.DataSourceID);
                if (DataSource!=null)
                {

                    ds.GenerateDataViewForChildNode(DataSource, EntityStructure.Id, EntityStructure.DatasourceEntityName, EntityStructure.SchemaOrOwnerOrDatabase, "");
                    CreateChildNodes();
                    DMEEditor.AddLogMessage("Success", "Got child Nodes", DateTime.Now, 0, null, Errors.Ok);
                }else
                {
                    Visutil.controlEditor.MsgBox("DM Engine", "Couldnot Get DataSource For Entity");
                }
             
            }
            catch (Exception ex)
            {
                string mes = "Could not get child nodes";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        [BranchDelegate(Caption = "Remove Childs", Hidden = false, iconimage = "remove_childs.ico")]
        public IErrorsInfo RemoveChilds()
        {

            try
            {
                if (Visutil.controlEditor.InputBoxYesNo("DM Engine","Are you sure you want to remove child  Entities?")==System.Windows.Forms.DialogResult.Yes)
                {
                    TreeEditor.RemoveChildBranchs(this);
                    ds.RemoveChildEntities(EntityStructure.Id);
                    DMEEditor.AddLogMessage("Success", "Removed Child Entites", DateTime.Now, 0, null, Errors.Ok);
                }
              
            }
            catch (Exception ex)
            {
                string mes = "Could not Remove Child Entites";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        //[BranchDelegate(Caption = "I/U Single Record", Hidden = false)]
        //public IErrorsInfo RecordEdit()
        //{

        //    try
        //    {

        //        string[] args = { "New View", null, null };
        //        List<ObjectItem> ob = new List<ObjectItem>(); ;
        //        ObjectItem it = new ObjectItem();
        //        it.obj = this;
        //        it.Name = "Branch";
        //        ob.Add(it);
        //        PassedArgs Passedarguments = new PassedArgs
        //        {
        //            Addin = null,
        //            AddinName = null,
        //            AddinType = "",
        //            DMView = DataView,
        //            CurrentEntity = BranchText,
        //            Id = BranchID,
        //            ObjectType = "CRUDENTITY",
        //            DataSource = DataSource,
        //            ObjectName = DataView.ViewName,
        //            Objects = ob,
        //            DatasourceName = EntityStructure.DataSourceID,
        //            EventType = "CRUDENTITY"

        //        };
        //        Visutil.ShowUserControlPopUp("Uc_DataTableSingleRecordEdit", DMEEditor, args, Passedarguments);
        //        DMEEditor.AddLogMessage("Success", "Show Entity Record I/U", DateTime.Now, 0, null, Errors.Ok);
        //    }
        //    catch (Exception ex)
        //    {
        //        string mes = "Could not Show Entity Record I/U";
        //        DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
        //    };
        //    return DMEEditor.ErrorObject;
        //}
        [BranchDelegate(Caption = "Data Edit", Hidden = false,iconimage ="data_edit.ico")]
        public IErrorsInfo DataEdit()
        {

            try
            {

                EntityStructure = ds.Entities[ds.Entities.FindIndex(o => o.Id == EntityStructure.Id)];
               
                    string[] args = { "New View", null, null };
                   
                    PassedArgs Passedarguments = new PassedArgs
                    {
                        Addin = null,
                        AddinName = null,
                        AddinType = "",
                        DMView = DataView,
                        CurrentEntity = EntityStructure.DatasourceEntityName,
                        Id = BranchID,
                        ObjectType = "VIEWENTITY",
                        DataSource = DataSource,
                        ObjectName = DataView.ViewName,
                        Objects = Createlistofitems(),
                        DatasourceName = EntityStructure.DataSourceID,
                        EventType = "CRUDENTITY"

                    };

                    Visutil.ShowUserControlInContainer("uc_getentities", Visutil.DisplayPanel, DMEEditor, args, Passedarguments);

                

                //  DMEEditor.AddLogMessage("Success", "Added Database Connection", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {                string mes = "Could not Add Database Connection";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        //[BranchDelegate(Caption = "Add Child Entity", Hidden = false)]
        //public IErrorsInfo AddChildEntity()
        //{

        //    try
        //    {
        //        string[] args = { "New Query Entity", null, null };
        //        List<ObjectItem> ob = new List<ObjectItem>(); ;
               
        //        if (TreeEditor.args.Objects != null)
        //        {
        //            ob.AddRange(TreeEditor.args.Objects);

        //        }
        //        ObjectItem it = new ObjectItem();
        //        it.obj = this;
        //        it.Name = "Branch";
        //        ob.Add(it);
        //        ObjectItem it1 = new ObjectItem();
        //        it1.obj = this;
        //        it1.Name = "ParentBranch";
        //        ob.Add(it1);
        //        PassedArgs Passedarguments = new PassedArgs
        //        {
        //            Addin = null,
        //            AddinName = null,
        //            AddinType = "",
        //            DMView = DataView,
        //            CurrentEntity = BranchText,
        //            Id = ID,
        //            ObjectType = "NEWECHILDNTITY",
        //            DataSource = DataSource,
        //            ObjectName = DataView.ViewName,

        //            Objects = ob,

        //            DatasourceName = DataView.DataViewDataSourceID,
        //            EventType = "NEWECHILDNTITY"

        //        };
        //        //ActionNeeded?.Invoke(this, Passedarguments);
        //        Visutil.ShowUserControlPopUp("Uc_DataViewEntityEditor", DMEEditor, args, Passedarguments);

        //        DMEEditor.AddLogMessage("Success", "Created Query Entity", DateTime.Now, 0, null, Errors.Ok);
        //    }
        //    catch (Exception ex)
        //    {
        //        string mes = "Could not Create Query Entity";
        //        DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
        //    };
        //    return DMEEditor.ErrorObject;
        //}
        [BranchDelegate(Caption = "Print Data", Hidden = false,iconimage ="print.ico")]
        public IErrorsInfo PrintData()
        {
            try
            {
               

            }
            catch (Exception ex)
            {
                string mes = "Could Print Data";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
           
        }
        //[BranchDelegate(Caption = "Get Child Entities From DataSource", Hidden = true)]
        //public IErrorsInfo AddChildEntityFromIDataSource()
        //{

        //    try
        //    {
        //        IDataSource childds = DMEEditor.GetDataSource(EntityStructure.DataSourceID);
        //        if (childds != null)
        //            {
        //                EntityStructure entity = childds.GetEntityStructure(BranchText, true);

        //                if (entity != null)
        //                {
        //                    EntityStructure CurEntity = ds.GetEntityStructure(BranchText, true);
        //                    EntityStructure newentity = new EntityStructure();
        //                    newentity.ParentId = CurEntity.Id;
        //                    newentity.ViewID = CurEntity.ViewID;
        //                    newentity.Viewtype = entity.Viewtype;
        //                    newentity.Relations = entity.Relations;
        //                    newentity.PrimaryKeys = entity.PrimaryKeys;
        //                    newentity.EntityName = entity.EntityName;
        //                    newentity.Fields = entity.Fields;
        //                    newentity.DataSourceID = entity.DataSourceID;
        //                    newentity.DatabaseType = entity.DatabaseType;
        //                    newentity.SchemaOrOwnerOrDatabase = entity.SchemaOrOwnerOrDatabase;
        //                    newentity.Id = ds.NextHearId();
        //                    ds.CreateEntityAs(newentity);

        //                    DataViewEntitiesNode dbent = new DataViewEntitiesNode(TreeEditor, DMEEditor, this, newentity.EntityName, TreeEditor.SeqID, EnumBranchType.Entity, "entity.ico", DataView.DataViewDataSourceID, newentity);

        //                    TreeEditor.AddBranch(this, dbent);
        //                    dbent.CreateChildNodes();
        //                    ChildBranchs.Add(dbent);
        //                }
        //            }
                 
        //        DMEEditor.AddLogMessage("Success", "Get Childs for Entity", DateTime.Now, 0, null, Errors.Ok);
        //    }
        //    catch (Exception ex)
        //    {
        //        string mes = "Could not Create Query Entity";
        //        DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
        //    };
        //    return DMEEditor.ErrorObject;
        //}
        [BranchDelegate(Caption = "Field Properties")]
        public IErrorsInfo FieldProperties()
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            //   DMEEditor.Logger.WriteLog($"Filling Database Entites ) ");
            try
            {
                string[] args = { "New Query Entity", null, null };
               


                PassedArgs Passedarguments = new PassedArgs
                {
                    Addin = null,
                    AddinName = null,
                    AddinType = "",
                    DMView = DataView,
                    CurrentEntity = EntityStructure.EntityName,
                    Id = ID,
                    ObjectType = "ENTITY",
                    DataSource = DataSource,
                    ObjectName = DataView.ViewName,

                    Objects = Createlistofitems(),

                    DatasourceName = DataView.DataViewDataSourceID,
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
        [BranchDelegate(Caption = "Paste Entity(s)",iconimage ="paste.ico")]
        public IErrorsInfo PasteEntity()
        {

            try
            {
                   ds = (DataViewDataSource)DMEEditor.GetDataSource(DataView.DataViewDataSourceID);
                if (TreeEditor.args != null)
                {
                    if (TreeEditor.args.EventType == "COPYENTITY" || TreeEditor.args.EventType == "DragandDropEntity")
                    {
                        if (TreeEditor.args.Objects != null)
                        {
                           
                            IBranch pbr = (IBranch)TreeEditor.args.Objects.Where(x => x.Name == "Branch").FirstOrDefault().obj;
                            EntityStructure entity = (EntityStructure)TreeEditor.args.Objects.Where(x => x.Name == "EntityStructure").FirstOrDefault().obj;
                            if (ds.CheckEntityExist(entity.EntityName))
                            {
                                DMEEditor.AddLogMessage("Fail", $"Could Not Paste Entity {entity.EntityName}, it already exist", DateTime.Now, -1, null, Errors.Failed);
                            }
                            else
                            {
                                IDataSource srcds = DMEEditor.GetDataSource(entity.DataSourceID);
                                entity = (EntityStructure)srcds.GetEntityStructure(entity, true).Clone();
                                entity.Caption = entity.EntityName;
                                entity.DatasourceEntityName = entity.EntityName;
                                entity.Created = false;
                                entity.Id = ds.NextHearId();
                                entity.DataSourceID = srcds.DatasourceName;
                                entity.ParentId = ID;
                                entity.ViewID = DataView.ViewID;
                                ds.CreateEntityAs(entity);
                                DataViewEntitiesNode dbent = new DataViewEntitiesNode(TreeEditor, DMEEditor, this, entity.EntityName, TreeEditor.SeqID, EnumBranchType.Entity, ds.GeticonForViewType(entity.Viewtype), DataView.DataViewDataSourceID, entity);
                                TreeEditor.AddBranch(this, dbent);
                                dbent.CreateChildNodes();
                                ChildBranchs.Add(dbent);
                                DMEEditor.AddLogMessage("Success", $"Pasted Entity {entity.EntityName}", DateTime.Now, -1, null, Errors.Ok);
                            }

                        }
                    }
                    else
                    if (TreeEditor.SelectedBranchs.Count > 0 && TreeEditor.args.EventType == "COPYENTITIES")
                    {
                        foreach (int item in TreeEditor.SelectedBranchs)
                        {
                            IBranch br = TreeEditor.GetBranch(item);
                            IDataSource srcds = DMEEditor.GetDataSource(br.DataSourceName);
                            if (srcds != null)
                            {
                                EntityStructure entity = (EntityStructure)srcds.GetEntityStructure(br.BranchText, true).Clone();
                                entity.Caption = entity.EntityName;
                                entity.DatasourceEntityName = entity.EntityName;
                                entity.Created = false;
                                entity.Id = ds.NextHearId();
                                entity.DataSourceID = srcds.DatasourceName;
                                entity.ParentId = ID;
                                entity.ViewID = DataView.ViewID;
                                ds.CreateEntityAs(entity);
                                DataViewEntitiesNode dbent = new DataViewEntitiesNode(TreeEditor, DMEEditor, this, entity.EntityName, TreeEditor.SeqID, EnumBranchType.Entity, ds.GeticonForViewType(entity.Viewtype), DataView.DataViewDataSourceID, entity);
                                TreeEditor.AddBranch(this, dbent);
                                dbent.CreateChildNodes();
                                ChildBranchs.Add(dbent);
                                DMEEditor.AddLogMessage("Success", $"Pasted Entity {entity.EntityName}", DateTime.Now, -1, null, Errors.Ok);
                            }


                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string mes = "Could not Added Entity ";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        #endregion Exposed Interface"
        #region "Other Methods"

        #endregion"Other Methods"
    }
}
