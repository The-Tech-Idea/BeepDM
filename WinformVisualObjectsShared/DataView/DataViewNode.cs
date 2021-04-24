using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.DataView;
using TheTechIdea.DataManagment_Engine.Vis;
using TheTechIdea.Util;

namespace TheTechIdea.Winforms.VIS
{
    public class DataViewNode  : IBranch, ITreeView
    {
        public DataViewNode()
        {

        }
        public DataViewNode(ITree pTreeEditor, IDMEEditor pDMEEditor, IBranch pParentNode, string pBranchText, int pID, EnumBranchType pBranchType, string pimagename,string ConnectionName)
        {

           

            TreeEditor = pTreeEditor;
            DMEEditor = pDMEEditor;
            ParentBranchID = pParentNode.ID;
            BranchText = pBranchText;
            BranchType = pBranchType;
        //   IconImageName = pimagename;

            ds = (DataViewDataSource)DMEEditor.GetDataSource(ConnectionName);
            //if (ds.Entities.Count > 0)
            //{
            //    ds.Entities.Clear();
            //    DMEEditor.ConfigEditor.SaveDataconnectionsValues();
            //}
            if(ds!= null)  MiscID = ds.ViewID; 
            DataSourceName = ConnectionName;
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
        public EnumBranchType BranchType { get; set; } = EnumBranchType.DataPoint;
        public int BranchID { get; set; }
        public string IconImageName { get; set; } = "dataviewnode.ico";
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
                if (ds != null)
                {
                    return ds.DataView;
                }
                else
                    return null;
               
            }
            set
            {
                ds.DataView = value;
            }
        }
     
        #endregion "Properties"
        #region "Interface Methods"
        public IErrorsInfo CreateChildNodes()
        {

            try
            {
                CreateViewEntites();

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
        [BranchDelegate(Caption = "Edit")]
        public IErrorsInfo Edit()
        {

            try
            {
                string[] args = { "New Query Entity", null, null };
                List<ObjectItem> ob = new List<ObjectItem>(); ;
                ObjectItem it = new ObjectItem();
                it.obj = this;
                it.Name = "Branch";
                ob.Add(it);
                IBranch RootCompositeLayerBranch = TreeEditor.Branches[TreeEditor.Branches.FindIndex(x => x.BranchClass == "APP" && x.BranchType == EnumBranchType.Root)];
                it = new ObjectItem();
                it.obj = RootCompositeLayerBranch;
                it.Name = "RootAppBranch";
                ob.Add(it);
                PassedArgs Passedarguments = new PassedArgs
                {
                    Addin = null,
                    AddinName = null,
                    AddinType = "",
                    DMView = DataView,
                    CurrentEntity = BranchText,
                    Id = DataView.Entities[0].Id,
                    ObjectType = "EDITVIEW",
                    DataSource = ds,
                    ObjectName = DataView.ViewName,

                    Objects = ob,

                    DatasourceName = null,
                    EventType = "EDITVIEW"

                };
                // ActionNeeded?.Invoke(this, Passedarguments);
                Visutil.ShowUserControlPopUp("uc_ViewEditor", DMEEditor, args, Passedarguments);

              //  DMEEditor.AddLogMessage("Success", "Show", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not show edit view control";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        [BranchDelegate(Caption = "Get Entities")]
        public IErrorsInfo CreateViewEntites()
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            DMEEditor.Logger.WriteLog($"Filling View Entites ) ");
            string iconname;
            try
            {
               
                ds = (DataViewDataSource)DMEEditor.GetDataSource(DataView.DataViewDataSourceID);
             
                if (ds != null)
                {
                 
                    bool loadv = false;
                    if (ChildBranchs.Count > 0)
                    {
                        if (Visutil.controlEditor.InputBoxYesNo("Beep", "Do you want to over write th existing View Structure?") == DialogResult.Yes)
                        {
                            TreeEditor.RemoveChildBranchs(this);
                            ds.LoadView();
                            loadv = true;
                        }
                    }
                    else
                    {
                        ds.LoadView();
                        loadv = true;
                    }
                    if (loadv)
                    {
                        // ds.Dataview;// DMEEditor.viewEditor.Views[DMEEditor.viewEditor.ViewListIndex(DataView.id)];
                        if (ds != null)
                        {
                            if (DataView != null)
                            {
                                TreeEditor.RemoveChildBranchs(this);
                                List<EntityStructure> cr = DataView.Entities.Where(cx => (cx.Id != 0) && (cx.ParentId == 1)).ToList();
                                int i = 0;
                                foreach (EntityStructure tb in cr)
                                {

                                    DataViewEntitiesNode dbent = new DataViewEntitiesNode(TreeEditor, DMEEditor, this, tb.EntityName, TreeEditor.SeqID, EnumBranchType.Entity, ds.GeticonForViewType(tb.Viewtype), DataView.DataViewDataSourceID, tb);
                                    dbent.ID = tb.Id;
                                    dbent.DataSourceName = tb.DataSourceID;
                                    TreeEditor.AddBranch(this, dbent);
                                    dbent.CreateChildNodes();
                                    ChildBranchs.Add(dbent);
                                    i += 1;

                                }

                            }
                           
                        }
                        else
                        {
                            DMEEditor.Logger.WriteLog($"Could not Find Datasource File " + DataView.DataViewDataSourceID);
                        }

                    }
                }else
                {
                    DMEEditor.Logger.WriteLog($"Could not Find DataView File " + DataView.DataViewDataSourceID);
                }
                SaveView();


            }
            catch (Exception ex)
            {
                DMEEditor.Logger.WriteLog($"Error in Filling Database Entites ({ex.Message}) ");
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Ex = ex;
            }
            return DMEEditor.ErrorObject;

        }
        [BranchDelegate(Caption = "Save")]
        public IErrorsInfo SaveView()
        {

            try
            {
                DMEEditor.ConfigEditor.SaveDataconnectionsValues();
               // ds.Dataview=DataView;
                ds.WriteDataViewFile(DataView.DataViewDataSourceID);
           
                DMEEditor.AddLogMessage("Success", "Saved View", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Save View";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;

        }
        [BranchDelegate(Caption = "Remove")]
        public IErrorsInfo RemoveView()
        {

            try
            {
                if (Visutil.controlEditor.InputBoxYesNo("Remove View", "Area you Sure ? you want to remove View???") == System.Windows.Forms.DialogResult.Yes)
                {
                    ConnectionProperties cn = DMEEditor.ConfigEditor.DataConnections.Where(x => x.ConnectionName.ToUpper() == Path.GetFileName(DataView.DataViewDataSourceID).ToUpper()).FirstOrDefault();
                    string file = Path.Combine(cn.FilePath, cn.FileName);
                    try
                    {

                        //ds.ViewReader.re.RemoveDataViewByVID(DataView.VID);
                        DMEEditor.ConfigEditor.RemoveDataConnection(DataView.DataViewDataSourceID);
                        DMEEditor.RemoveDataDource(DataView.DataViewDataSourceID);
                      
                        DMEEditor.AddLogMessage("Success", "Removed View from Views List", DateTime.Now, 0, null, Errors.Ok);
                    }
                    catch (Exception ex)
                    {
                        string mes = "Could not Remove View from Views List";
                        DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
                    };

                    try
                    {

                        DMEEditor.DataSources.Remove(DMEEditor.DataSources.Where(x => x.DatasourceName == DataView.ViewName).FirstOrDefault());
                        DMEEditor.AddLogMessage("Success", "Removed View from DataSource List", DateTime.Now, 0, null, Errors.Ok);
                    }
                    catch (Exception ex)
                    {
                        string mes = "Could not Removed View from DataSource List";
                        DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
                    };

                    if (Visutil.controlEditor.InputBoxYesNo("Remove View", "Do you want to Delete the View File ???") == System.Windows.Forms.DialogResult.Yes)
                    {
                      
                        File.Delete(file);
                    }
                    TreeEditor.RemoveBranch(this);

                }

                DMEEditor.ConfigEditor.SaveDataconnectionsValues();
                DMEEditor.AddLogMessage("Success", "Remove View", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Remove View";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        [BranchDelegate(Caption = "Create Entity")]
        public IErrorsInfo CreateEntity()
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
                    DMView = DataView,
                    CurrentEntity = null,
                    Id = DataView.Entities[0].Id,
                    ObjectType = "NEWENTITY",
                    DataSource = ds,
                    ObjectName = DataView.ViewName,
                    
                    Objects = ob,
                   
                    DatasourceName = ds.DatasourceName,
                    EventType = "NEWENTITY"

                };
                //ActionNeeded?.Invoke(this, Passedarguments);
                Visutil.ShowUserControlPopUp("Uc_DataViewEntityEditor", DMEEditor, args, Passedarguments);

                DMEEditor.AddLogMessage("Success", "Created Query Entity", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Create Query Entity";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        [BranchDelegate(Caption = "Create Composed Layer")]
        public IErrorsInfo CreateComposedLayer()
        {

            try
            {
                string[] args = { "New Query Entity", null, null };
                List<ObjectItem> ob = new List<ObjectItem>(); ;
                ObjectItem it = new ObjectItem();
                it.obj = this;
                it.Name = "Branch";
                ob.Add(it);
                IBranch RootCompositeLayerBranch = TreeEditor.Branches[TreeEditor.Branches.FindIndex(x => x.BranchClass == "CLAYER" && x.BranchType == EnumBranchType.Root)];
                it = new ObjectItem();
                it.obj = RootCompositeLayerBranch;
                it.Name = "RootCompositeLayerBranch";
                ob.Add(it);
                PassedArgs Passedarguments = new PassedArgs
                {
                    Addin = null,
                    AddinName = null,
                    AddinType = "",
                    DMView = DataView,
                    CurrentEntity = null,
                    Id = DataView.Entities[0].Id,
                    ObjectType = "QUERYENTITY",
                    DataSource = ds,
                    ObjectName = DataView.ViewName,

                    Objects = ob,

                    DatasourceName = null,
                    EventType = "CLEARECOMPOSITELAYER"

                };
               // ActionNeeded?.Invoke(this, Passedarguments);
                Visutil.ShowUserControlPopUp("uc_ComposedLayer", DMEEditor, args, Passedarguments);

                DMEEditor.AddLogMessage("Success", "Created Query Entity", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Create Query Entity";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        [BranchDelegate(Caption = "Create App")]
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
                IBranch RootCompositeLayerBranch = TreeEditor.Branches[TreeEditor.Branches.FindIndex(x => x.BranchClass == "APP" && x.BranchType == EnumBranchType.Root)];
                it = new ObjectItem();
                it.obj = RootCompositeLayerBranch;
                it.Name = "RootAppBranch";
                ob.Add(it);
                PassedArgs Passedarguments = new PassedArgs
                {
                    Addin = null,
                    AddinName = null,
                    AddinType = "",
                    DMView = DataView,
                    CurrentEntity = null,
                    Id = DataView.Entities[0].Id,
                    ObjectType = "QUERYENTITY",
                    DataSource = ds,
                    ObjectName = DataView.ViewName,

                    Objects = ob,

                    DatasourceName = null,
                    EventType = "CREATAPP"

                };
                // ActionNeeded?.Invoke(this, Passedarguments);
                Visutil.ShowUserControlPopUp("uc_App", DMEEditor, args, Passedarguments);

                DMEEditor.AddLogMessage("Success", "Created Query Entity", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Create Query Entity";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        //[BranchDelegate(Caption = "Add Child Entity From IDataSource", Hidden = true)]
        //public IErrorsInfo AddChildEntityFromIDataSource()
        //{

        //    try
        //    {

        //        if (TreeEditor.args.Objects != null)
        //        {
        //            IBranch branchentity = (IBranch)TreeEditor.args.Objects.Where(x => x.Name == "ChildBranch").FirstOrDefault().obj;
        //            IDataSource childds = DMEEditor.GetDataSource(branchentity.DataSource.DatasourceName);
        //            if (childds != null)
        //            {
        //                EntityStructure entity = childds.GetEntityStructure(branchentity.BranchText, true);

        //                if (entity != null)
        //                {
        //                   // EntityStructure CurEntity = childds.GetEntityStructure(BranchText, true);
        //                    EntityStructure newentity = new EntityStructure();
        //                    newentity.Id = ds.NextHearId();
        //                    newentity.ParentId = 1;
        //                    newentity.ViewID = DataView.ViewID;
        //                    newentity.Viewtype = entity.Viewtype;
        //                    newentity.Relations = entity.Relations;
        //                    newentity.PrimaryKeys = entity.PrimaryKeys;
        //                    newentity.EntityName = entity.EntityName;
        //                    newentity.Fields = entity.Fields;
        //                    newentity.DataSourceID = entity.DataSourceID;
        //                    newentity.DatabaseType = entity.DatabaseType;
        //                    newentity.SchemaOrOwnerOrDatabase = entity.SchemaOrOwnerOrDatabase;
        //                    newentity.Created = false;
                          
        //                    ds.CreateEntityAs(newentity);

        //                    DataViewEntitiesNode dbent = new DataViewEntitiesNode(TreeEditor, DMEEditor, this, newentity.EntityName, TreeEditor.SeqID, EnumBranchType.Entity, "entity.ico", DataView.DataViewDataSourceID, newentity);

        //                    TreeEditor.AddBranch(this, dbent);
        //                    dbent.CreateChildNodes();
        //                    ChildBranchs.Add(dbent);
        //                }
        //            }

        //        }



        //        DMEEditor.AddLogMessage("Success", "Created Query Entity", DateTime.Now, 0, null, Errors.Ok);
        //    }
        //    catch (Exception ex)
        //    {
        //        string mes = "Could not Create Query Entity";
        //        DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
        //    };
        //    return DMEEditor.ErrorObject;
        //}
        [BranchDelegate(Caption = "Paste Entity(s)")]
        public IErrorsInfo PasteEntity()
        {

            try
            {
               // IBranch pbr = TreeEditor.Branches.Where(x => x.BranchType == EnumBranchType.Root && x.BranchClass == "VIEW").FirstOrDefault();
               //
                ds = (DataViewDataSource)DMEEditor.GetDataSource(DataView.DataViewDataSourceID);
              
                
                if (TreeEditor.args != null)
                {
                 //   TreeEditor.args.Objects.Add(new ObjectItem { Name = "ParentBranch", obj = this });
                    if (TreeEditor.args.EventType == "COPYENTITY")
                    {
                      
                          //  IBranch pbr = (IBranch)TreeEditor.args.Objects.Where(x => x.Name == "Branch").FirstOrDefault().obj;
                            EntityStructure entity = (EntityStructure)TreeEditor.args.Objects.Where(x => x.Name == "Entity").FirstOrDefault().obj;
                            if (ds.CheckEntityExist(entity.EntityName))
                            {
                                DMEEditor.AddLogMessage("Fail", $"Could Not Paste Entity {entity.EntityName}, it already exist", DateTime.Now, -1, null, Errors.Failed);
                            }
                            else
                            {
                                IDataSource srcds = DMEEditor.GetDataSource(entity.DataSourceID);
                                entity = srcds.GetEntityStructure(entity, true);
                                entity.Caption = entity.EntityName;
                                entity.DatasourceEntityName = entity.EntityName;
                                entity.Created = false;
                                entity.Id = ds.NextHearId();
                                entity.ParentId = 1;
                                entity.ViewID = DataView.ViewID;
                                ds.CreateEntityAs(entity);
                                DataViewEntitiesNode dbent = new DataViewEntitiesNode(TreeEditor, DMEEditor, this, entity.EntityName, TreeEditor.SeqID, EnumBranchType.Entity, ds.GeticonForViewType(entity.Viewtype), DataView.DataViewDataSourceID, entity);
                                TreeEditor.AddBranch(this, dbent);
                                dbent.CreateChildNodes();
                                ChildBranchs.Add(dbent);
                                DMEEditor.AddLogMessage("Success", $"Pasted Entity {entity.EntityName}", DateTime.Now, -1, null, Errors.Ok);
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
                                    EntityStructure entity = srcds.GetEntityStructure(br.BranchText, true);
                                    entity.Caption = entity.EntityName;
                                    entity.DatasourceEntityName = entity.EntityName;
                                    entity.Created = false;
                                    entity.Id = ds.NextHearId();
                                    entity.ParentId = 1;
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
