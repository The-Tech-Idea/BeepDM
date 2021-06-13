using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Editor;
using TheTechIdea.DataManagment_Engine.Vis;
using TheTechIdea.Util;

namespace TheTechIdea.Winforms.VIS
{
    public class DatabaseNode  : IBranch, ITreeView
    {
        public DatabaseNode()
        {

        }
        public DatabaseNode(ITree pTreeEditor, IDMEEditor pDMEEditor, IBranch pParentNode, string pBranchText, int pID, EnumBranchType pBranchType, string pimagename)
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
                    BranchID = ID;
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
        public string Name { get; set; }
        public EntityStructure EntityStructure { get; set; }
        public string BranchText { get; set; }
        public IDMEEditor DMEEditor { get; set; }
        public IDataSource DataSource { get; set; }
        public string DataSourceName { get; set; }
        public int Level { get; set; }
        public EnumBranchType BranchType { get; set; } = EnumBranchType.DataPoint;
        public int BranchID { get; set; }
        public string IconImageName { get; set; }
        public string BranchStatus { get; set; }
        public int ParentBranchID { get; set; }
        public string BranchDescription { get; set; }
        public string BranchClass { get; set; } = "RDBMS";
        public List<IBranch> ChildBranchs { get; set; } = new List<IBranch>();
        public ITree TreeEditor { get; set; }
        public List<string> BranchActions { get; set; }
        public List<Delegate> Delegates { get; set; }
        public int ID { get; set; }
        public object TreeStrucure { get ; set ; }
        public IVisUtil Visutil { get ; set ; }
        public int MiscID { get; set; }
        public IErrorsInfo CreateChildNodes()
        {
            throw new NotImplementedException();
        }
        public IErrorsInfo CreateDelegateMenu()
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
        [BranchDelegate(Caption = "Get Entities")]
        public IErrorsInfo CreateDatabaseEntites()
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
       //     DMEEditor.Logger.WriteLog($"Filling Database Entites ) ");
            try
            {
                EntityStructure ent;
                string iconimage;
                DataSource = (IRDBSource)DMEEditor.GetDataSource(BranchText);
                if (DataSource != null)
                {
                    DMEEditor.OpenDataSource(BranchText);
                   // DataSource.Dataconnection.OpenConnection();
                    if (DataSource.ConnectionStatus == System.Data.ConnectionState.Open)
                    {
                        DataSource.GetEntitesList();
                        List<string> ename = DataSource.EntitiesNames.ToList();
                        List<string> existing = DataSource.Entities.Select(o => o.EntityName.ToUpper()).ToList();
                        List<string> diffnames = ename.Except(existing).ToList();
                        TreeEditor.RemoveChildBranchs(this);
                        int i = 0;
                       
                            TreeEditor.ShowWaiting();
                            TreeEditor.ChangeWaitingCaption($"Getting  RDBMS Entities Total:{ diffnames.Count}");
                            foreach (string tb in diffnames) //
                            {
                                TreeEditor.AddCommentsWaiting($"{i} - Added {tb} to {DataSourceName}");
                                ent = DataSource.GetEntityStructure(tb, true);
                               

                                if (ent.Created == false)
                                {
                                    iconimage = "entitynotcreated.ico";
                                }
                                else
                                {
                                    iconimage = "databaseentities.ico";
                                }
                                DatabaseEntitesNode dbent = new DatabaseEntitesNode(TreeEditor, DMEEditor, this, tb, TreeEditor.SeqID, EnumBranchType.Entity, iconimage, DataSource);
                                dbent.DataSourceName = DataSource.DatasourceName;
                                dbent.DataSource = DataSource;
                                ChildBranchs.Add(dbent);
                                TreeEditor.AddBranch(this, dbent);
                                i += 1;
                            }
                            TreeEditor.HideWaiting();
                        //------------------------------- Draw Existing Entities
                         foreach (string tb in existing) //
                        {
                           
                         
                            ent = DataSource.GetEntityStructure(tb, false);
                          

                            if (ent.Created == false)
                            {
                                iconimage = "entitynotcreated.ico";
                            }
                            else
                            {
                                iconimage = "databaseentities.ico";
                            }
                            DatabaseEntitesNode dbent = new DatabaseEntitesNode(TreeEditor, DMEEditor, this, tb, TreeEditor.SeqID, EnumBranchType.Entity, iconimage, DataSource);
                            dbent.DataSourceName = DataSource.DatasourceName;
                            dbent.DataSource = DataSource;
                            ChildBranchs.Add(dbent);
                            TreeEditor.AddBranch(this, dbent);
                            i += 1;
                        }
                        //------------------------------------------------------
                   
                      
                        DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new DataManagment_Engine.ConfigUtil.DatasourceEntities { datasourcename = DataSourceName, Entities = DataSource.Entities });
                    }

                }
            }
            catch (Exception ex)
            {
                DMEEditor.Logger.WriteLog($"Error in Filling Database Entites ({ex.Message}) ");
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Ex = ex;
            }
            return DMEEditor.ErrorObject;

        }
        [BranchDelegate(Caption = "Copy Defaults")]
        public IErrorsInfo CopyDefaults()
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
          //  DMEEditor.Logger.WriteLog($"Filling Database Entites ) ");
            try
            {
                 List<DefaultValue> defaults  =DMEEditor.ConfigEditor.DataConnections[DMEEditor.ConfigEditor.DataConnections.FindIndex(i => i.ConnectionName == BranchText)].DatasourceDefaults;
                    if (defaults != null)
                    {
                        string[] args = { "CopyDefaults", null, null };
                        List<ObjectItem> ob = new List<ObjectItem>(); ;
                        ObjectItem it = new ObjectItem();
                        it.obj = defaults;
                        it.Name = "Defaults";
                        ob.Add(it);


                        PassedArgs Passedarguments = new PassedArgs
                        {
                            Addin = null,
                            AddinName = null,
                            AddinType = "",
                            DMView = null,
                            CurrentEntity = BranchText,
                            Id = 0,
                            ObjectType = "COPYDEFAULTS",
                            DataSource = null,
                            ObjectName = BranchText,

                            Objects = ob,

                            DatasourceName = BranchText,
                            EventType = "COPYDEFAULTS"

                        };
                        TreeEditor.args = Passedarguments;
                    }
                    else
                    {
                        string mes = "Could not get Defaults";
                        DMEEditor.AddLogMessage("Failed", mes, DateTime.Now, -1, mes, Errors.Failed);
                    }
                 
            }
            catch (Exception ex)
            {
                DMEEditor.Logger.WriteLog($"Error getting defaults ({ex.Message}) ");
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Ex = ex;
            }
            return DMEEditor.ErrorObject;

        }
        [BranchDelegate(Caption = "Paste Defaults")]
        public IErrorsInfo PasteDefaults()
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
          //  DMEEditor.Logger.WriteLog($"Filling Database Entites ) ");
            try
            {
                        if (TreeEditor.args != null)
                        {
                            if(TreeEditor.args.ObjectType=="COPYDEFAULTS")
                            {
                                List<DefaultValue> defaults =(List < DefaultValue >) TreeEditor.args.Objects[0].obj;
                                DMEEditor.ConfigEditor.DataConnections[DMEEditor.ConfigEditor.DataConnections.FindIndex(i => i.ConnectionName == BranchText)].DatasourceDefaults= defaults;
                                DMEEditor.ConfigEditor.SaveDataconnectionsValues();
                            }
                        }
            }
            catch (Exception ex)
            {
                DMEEditor.Logger.WriteLog($"Error in  pasting Defaults ({ex.Message}) ");
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Ex = ex;
            }
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
                    CurrentEntity =BranchText,
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
        [BranchDelegate(Caption = "Copy Entity(s)")]
        public IErrorsInfo CopyEntities()
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            //   DMEEditor.Logger.WriteLog($"Filling Database Entites ) ");
            try
            {
                List<EntityStructure> ents = new List<EntityStructure>();
                if (DataSource == null)
                {
                    DataSource = DMEEditor.GetDataSource(DataSourceName);
                }
                string[] args = new string[] { BranchText, DataSource.Dataconnection.ConnectionProp.SchemaName, null };
               
                    
                    if (DataSource != null)
                    {
                        DataSource.GetEntitesList();
                       
                      
                        IBranch pbr = TreeEditor.GetBranch(ParentBranchID);
                        List<ObjectItem> ob = new List<ObjectItem>(); ;
                        ObjectItem it = new ObjectItem();
                        it.obj = this;
                        it.Name = "ParentBranch";
                        ob.Add(it);

                        PassedArgs Passedarguments = new PassedArgs
                        {
                            ObjectName = "DATABASE",
                            ObjectType = "TABLE",
                            EventType = "COPYENTITIES",
                            ParameterString1 = "COPYENTITIES",
                            DataSource = DataSource,
                            DatasourceName = DataSource.DatasourceName,
                            CurrentEntity = BranchText,
                            EntitiesNames = DataSource.EntitiesNames,
                            Objects = ob
                        };
                        TreeEditor.args = Passedarguments;
                        DMEEditor.Passedarguments = Passedarguments;
                //        Visutil.ShowUserControlPopUp("uc_datasourceDefaults", DMEEditor, args, Passedarguments);
                    }
                    else
                    {
                        DMEEditor.AddLogMessage("Fail", "Could not get DataSource", DateTime.Now, -1, null, Errors.Failed);
                    }
                  

                

            }
            catch (Exception ex)
            {
                DMEEditor.Logger.WriteLog($"Error in Filling Database Entites ({ex.Message}) ");
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Ex = ex;
            }
            return DMEEditor.ErrorObject;

        }
        [BranchDelegate(Caption = "Paste Entity(s)")]
        public IErrorsInfo PasteEntity()
        {

            try
            {
                string iconimage = "";
                int cnt = 0;
                List<EntityStructure> ls = new List<EntityStructure>();
                DataSource = DMEEditor.GetDataSource(DataSourceName);
                IBranch RootBranch = TreeEditor.Branches[TreeEditor.Branches.FindIndex(x => x.BranchClass == "RDBMS" && x.BranchType == EnumBranchType.Root)];
                if (DataSource.Entities != null)
                {
                    if (DataSource.Entities.Count == 0)
                    {
                        cnt = 1;
                    }
                }else
                {
                    cnt = DataSource.Entities.Count;
                }
                if (TreeEditor.args != null)
                {
                    //   TreeEditor.args.Objects.Add(new ObjectItem { Name = "ParentBranch", obj = this });
                    if (TreeEditor.args.EventType == "COPYENTITY")
                    {

                        //  IBranch pbr = (IBranch)TreeEditor.args.Objects.Where(x => x.Name == "Branch").FirstOrDefault().obj;
                        EntityStructure entity = (EntityStructure)TreeEditor.args.Objects.Where(x => x.Name == "Entity").FirstOrDefault().obj;
                        if (DataSource.CheckEntityExist(entity.EntityName))
                        {
                            DMEEditor.AddLogMessage("Fail", $"Could Not Paste Entity {entity.EntityName}, it already exist", DateTime.Now, -1, null, Errors.Failed);
                        }
                        else
                        {
                            IDataSource srcds = DMEEditor.GetDataSource(entity.DataSourceID);
                            entity = (EntityStructure)srcds.GetEntityStructure(entity, true).Clone();
                            entity.Caption = entity.EntityName;
                            entity.DatasourceEntityName = entity.DatasourceEntityName;
                            entity.EntityName = entity.EntityName;
                            entity.Created = false;
                            entity.DataSourceID = srcds.DatasourceName;
                            entity.Id = cnt+1;
                            entity.ParentId = 0;
                            entity.ViewID = 0;
                            entity.DatabaseType = srcds.DatasourceType;
                            entity.Viewtype = ViewType.Table;
                            if (DataSource.CreateEntityAs(entity))
                            {
                                entity.Created = true;

                                DMEEditor.AddLogMessage("Success", $"Pasted Entity {entity.EntityName}", DateTime.Now, -1, null, Errors.Ok);
                            }
                            else
                            {
                                entity.Created = false;
                                DMEEditor.AddLogMessage("Fail", $"Error Copying Entity {entity.EntityName} - {DMEEditor.ErrorObject.Message}", DateTime.Now, -1, null, Errors.Failed);
                            }
                            if (entity.Created == false)
                            {
                                iconimage = "entitynotcreated.ico";
                            }
                            else
                            {
                                iconimage = "databaseentities.ico";
                            }
                            DatabaseEntitesNode dbent = new DatabaseEntitesNode(TreeEditor, DMEEditor, this, entity.EntityName, TreeEditor.SeqID, EnumBranchType.Entity, iconimage, DataSource);
                            TreeEditor.AddBranch(this, dbent);
                            dbent.DataSourceName = DataSource.DatasourceName;
                            dbent.DataSource = DataSource;
                            ChildBranchs.Add(dbent);
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
                                if (DataSource.CheckEntityExist(entity.EntityName))
                                {
                                    DMEEditor.AddLogMessage("Fail", $"Could Not Paste Entity {entity.EntityName}, it already exist", DateTime.Now, -1, null, Errors.Failed);
                                }else
                                {
                                    entity.Caption = entity.EntityName;
                                    entity.DatasourceEntityName = entity.DatasourceEntityName;
                                    entity.EntityName = entity.EntityName;
                                    entity.Created = false;
                                    entity.DataSourceID = srcds.DatasourceName;
                                    entity.Id = cnt + 1;
                                    cnt += 1;
                                    entity.ParentId = 0;
                                    entity.ViewID = 0;
                                    entity.DatabaseType = srcds.DatasourceType;
                                    entity.Viewtype = ViewType.Table;
                                    ls.Add(entity);
                                   
                                    
                                    
                                }
                              
                            }
                        }
                        LScriptHeader scriptHeader = TreeEditor.CreateScriptToCopyEntities(DataSource, ls, true);
                        if (scriptHeader != null)
                        {
                            TreeEditor.ShowRunScriptGUI(RootBranch,this, DataSource, scriptHeader);
                        }
                        foreach (var entity in ls)
                        {
                            if (DataSource.CreateEntityAs(entity))
                            {
                                entity.Created = true;

                                DMEEditor.AddLogMessage("Success", $"Pasted Entity {entity.EntityName}", DateTime.Now, -1, null, Errors.Ok);
                            }
                            else
                            {
                                entity.Created = false;
                                DMEEditor.AddLogMessage("Fail", $"Error Copying Entity {entity.EntityName} - {DMEEditor.ErrorObject.Message}", DateTime.Now, -1, null, Errors.Failed);
                            }
                            if (entity.Created == false)
                            {
                                iconimage = "entitynotcreated.ico";
                            }
                            else
                            {
                                iconimage = "databaseentities.ico";
                            }

                            DatabaseEntitesNode dbent = new DatabaseEntitesNode(TreeEditor, DMEEditor, this, entity.EntityName, TreeEditor.SeqID, EnumBranchType.Entity, iconimage, DataSource);
                            TreeEditor.AddBranch(this, dbent);
                            dbent.DataSourceName = DataSource.DatasourceName;
                            dbent.DataSource = DataSource;
                            ChildBranchs.Add(dbent);
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
        [BranchDelegate(Caption = "Scan for New Entities")]
        public IErrorsInfo ScanDatabaseEntites()
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            //     DMEEditor.Logger.WriteLog($"Filling Database Entites ) ");
            try
            {
                string iconimage;
                DataSource = (IRDBSource)DMEEditor.GetDataSource(BranchText);
                if (DataSource != null)
                {
                    DMEEditor.OpenDataSource(BranchText);
                  //  DataSource.Dataconnection.OpenConnection();
                    if (DataSource.ConnectionStatus == System.Data.ConnectionState.Open)
                    {
                        if (Visutil.controlEditor.InputBoxYesNo("Beep DM", "Are you sure, this might take some time?") == System.Windows.Forms.DialogResult.Yes)
                        {
                            DataSource.GetEntitesList();
                            List<string> ename = DataSource.Entities.Select(p => p.EntityName.ToUpper()).ToList();
                            List<string> diffnames = ename.Except(DataSource.EntitiesNames).ToList();
                          //  DataSource.Entities.Clear();
                           // TreeEditor.RemoveChildBranchs(this);
                            int i = 0;
                            TreeEditor.ShowWaiting();
                            TreeEditor.ChangeWaitingCaption($"Getting  RDBMS Entities Total:{DataSource.EntitiesNames.Count}");
                            foreach (string tb in diffnames)
                            {

                                TreeEditor.AddCommentsWaiting($"{i} - Added {tb} to {DataSourceName}");
                                EntityStructure ent = DataSource.GetEntityStructure(tb, true);
                                if (ent.Created == false)
                                {
                                    iconimage = "entitynotcreated.ico";
                                }
                                else
                                {
                                    iconimage = "databaseentities.ico";
                                }
                                DatabaseEntitesNode dbent = new DatabaseEntitesNode(TreeEditor, DMEEditor, this, tb, TreeEditor.SeqID, EnumBranchType.Entity, iconimage, DataSource);
                                TreeEditor.AddBranch(this, dbent);
                                dbent.DataSourceName = DataSource.DatasourceName;
                                dbent.DataSource = DataSource;
                                ChildBranchs.Add(dbent);
                                i += 1;


                            }
                            TreeEditor.HideWaiting();
                        }

                    }

                }



            }
            catch (Exception ex)
            {
                DMEEditor.Logger.WriteLog($"Error in Filling Database Entites ({ex.Message}) ");
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Ex = ex;
            }
            return DMEEditor.ErrorObject;

        }
        [BranchDelegate(Caption = "Refresh Entities")]
        public IErrorsInfo RefreshDatabaseEntites()
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            //     DMEEditor.Logger.WriteLog($"Filling Database Entites ) ");
            try
            {
                string iconimage;
                DataSource = (IRDBSource)DMEEditor.GetDataSource(BranchText);
                if (DataSource != null)
                {
                    DMEEditor.OpenDataSource(BranchText);
                  //  DataSource.Dataconnection.OpenConnection();
                    if (DataSource.ConnectionStatus == System.Data.ConnectionState.Open)
                    {
                        if (Visutil.controlEditor.InputBoxYesNo("Beep DM", "Are you sure, this might take some time?") == System.Windows.Forms.DialogResult.Yes)
                        {
                            DataSource.Entities.Clear();
                            DataSource.GetEntitesList();
                            TreeEditor.RemoveChildBranchs(this);
                            int i = 0;
                            TreeEditor.ShowWaiting();
                            TreeEditor.ChangeWaitingCaption($"Getting  RDBMS Entities Total:{DataSource.EntitiesNames.Count}");
                            foreach (string tb in DataSource.EntitiesNames)
                            {
                                TreeEditor.AddCommentsWaiting($"{i} - Added {tb} to {DataSourceName}");
                                EntityStructure ent = DataSource.GetEntityStructure(tb, true);
                                if (ent.Created == false)
                                {
                                    iconimage = "entitynotcreated.ico";
                                }
                                else
                                {
                                    iconimage = "databaseentities.ico";
                                }
                                DatabaseEntitesNode dbent = new DatabaseEntitesNode(TreeEditor, DMEEditor, this, tb, TreeEditor.SeqID, EnumBranchType.Entity, iconimage, DataSource);
                                TreeEditor.AddBranch(this, dbent);
                                dbent.DataSourceName = DataSource.DatasourceName;
                                dbent.DataSource = DataSource;
                                ChildBranchs.Add(dbent);
                                i += 1;
                            }
                            TreeEditor.HideWaiting();
                        }

                    }

                }



            }
            catch (Exception ex)
            {
                DMEEditor.Logger.WriteLog($"Error in Filling Database Entites ({ex.Message}) ");
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Ex = ex;
            }
            return DMEEditor.ErrorObject;

        }
        [BranchDelegate(Caption = "Drop Entities")]
        public IErrorsInfo DropEntities()
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                if (Visutil.controlEditor.InputBoxYesNo("Beep DM", "Are you sure you ?") == DialogResult.Yes)
                {
                    if (TreeEditor.SelectedBranchs.Count > 0)
                    {
                        foreach (int item in TreeEditor.SelectedBranchs)
                        {
                            IBranch br = TreeEditor.GetBranch(item);
                            if (br.DataSourceName == DataSourceName)
                            {
                                IDataSource srcds = DMEEditor.GetDataSource(br.DataSourceName);
                                EntityStructure ent = DataSource.GetEntityStructure(br.BranchText, false);
                                DataSource.ExecuteSql($"Drop Table {ent.DatasourceEntityName}");
                                if (DMEEditor.ErrorObject.Flag == Errors.Ok)
                                {
                                    TreeEditor.RemoveBranch(br);
                                    DataSource.Entities.RemoveAt(DataSource.Entities.FindIndex(p => p.DatasourceEntityName == ent.DatasourceEntityName));
                                    DMEEditor.AddLogMessage("Success", $"Droped Entity {ent.EntityName}", DateTime.Now, -1, null, Errors.Ok);
                                }
                                else
                                {
                                    DMEEditor.AddLogMessage("Fail", $"Error Drpping Entity {ent.EntityName} - {DMEEditor.ErrorObject.Message}", DateTime.Now, -1, null, Errors.Failed);
                                }
                            }
                        }
                        DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new DataManagment_Engine.ConfigUtil.DatasourceEntities { datasourcename = DataSourceName, Entities = DataSource.Entities });
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Ex = ex;
                DMEEditor.AddLogMessage("Fail", $"Error Drpping Entity {EntityStructure.EntityName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        //[BranchDelegate(Caption = "Copy DataSource")]
        //public async Task<IErrorsInfo> CopyDataSourceAsync()
        //{
        //    DMEEditor.ErrorObject.Flag = Errors.Ok;
        //    try
        //    {
        //        if (Visutil.controlEditor.InputBoxYesNo("Beep DM", "Are you sure you ?") == DialogResult.Yes)
        //        {

        //            await GetScriptAsync();

        //        }
        //     DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new DataManagment_Engine.ConfigUtil.DatasourceEntities { datasourcename = DataSourceName, Entities = DataSource.Entities });
        //    }
        //    catch (Exception ex)
        //    {
        //        DMEEditor.ErrorObject.Flag = Errors.Failed;
        //        DMEEditor.ErrorObject.Ex = ex;
        //        DMEEditor.AddLogMessage("Fail", $"Error Drpping Entity {EntityStructure.EntityName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
        //    }
        //    return DMEEditor.ErrorObject;
        //}
        //private void update()
        //{

        //}
        //private async Task GetScriptAsync()
        //{
        //    IDataSource srcds = DMEEditor.GetDataSource(BranchText);
        //    var progress = new Progress<int>(percent =>
        //    {

        //        update();
        //    });
        //    await Task.Run(() =>
        //    {
        //        DMEEditor.ETL.CreateScriptHeader(progress, srcds);

        //    });
        //}
    }
}
