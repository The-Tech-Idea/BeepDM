using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TheTechIdea.DataManagment_Engine.Addin;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Editor;
using TheTechIdea.Util;
using TheTechIdea.Winforms.VIS;

namespace TheTechIdea.DataManagment_Engine.Vis
{
    public  class FunntionExtensions : IFunctionExtension
    {
        public IDataSource DataSource { get; set; }
        public IVisUtil Visutil { get; set; }
        public ITree TreeEditor { get; set; }
        IBranch pbr;
        IBranch RootBranch;
        public FunntionExtensions(IDMEEditor pDMEEditor, IVisUtil pVisutil, ITree pTreeEditor)
        {
            DMEEditor = pDMEEditor ;
            Visutil = pVisutil;
            TreeEditor = pTreeEditor;
        }
        private void GetValues(IPassedArgs Passedarguments)
        {
            DataSource = DMEEditor.GetDataSource(Passedarguments.DatasourceName);
            DMEEditor.OpenDataSource(Passedarguments.DatasourceName);
            pbr = TreeEditor.GetBranch(Passedarguments.Id);
            RootBranch = TreeEditor.Branches[TreeEditor.Branches.FindIndex(x => x.BranchClass == pbr.BranchClass && x.BranchType == EnumPointType.Root)];
        }
        public  IDMEEditor DMEEditor { get; set; }
        [CommandAttribute(Name="CopyEntities",Caption = "Copy Entities", Click =true,iconimage ="copyentities.ico",PointType= EnumPointType.DataPoint)]
        public IErrorsInfo CopyEntities(IPassedArgs Passedarguments)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            //   DMEEditor.Logger.WriteLog($"Filling Database Entites ) ");
            try
            {
                List<EntityStructure> ents = new List<EntityStructure>();
                GetValues(Passedarguments);
                string[] args = new string[] { pbr.BranchText, DataSource.Dataconnection.ConnectionProp.SchemaName, null };
              
                Passedarguments.EventType = "COPYENTITIES";
                Passedarguments.ParameterString1 = "COPYENTITIES";
                TreeEditor.args = (PassedArgs)Passedarguments;
                DMEEditor.Passedarguments = Passedarguments;

            }
            catch (Exception ex)
            {
                DMEEditor.Logger.WriteLog($"Error in Filling Database Entites ({ex.Message}) ");
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Ex = ex;
            }
            return DMEEditor.ErrorObject;
        }
        [CommandAttribute(Name = "PasteEntities",Caption = "Paste Entities", Click = true, iconimage = "pasteentities.ico", PointType = EnumPointType.DataPoint)]
        public void PasteEntities(IPassedArgs Passedarguments)
        {
            try
            {
                GetValues(Passedarguments);
                var progress = new Progress<PassedArgs>(percent =>
                {

                  
                });
                string iconimage = "";
                int cnt = 0;
                List<EntityStructure> ls = new List<EntityStructure>();
                if (TreeEditor.args != null)
                {
                 
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
                                }
                                else
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
                       
                        SyncDataSource scriptHeader = TreeEditor.CreateScriptToCopyEntities(DataSource, ls, progress, true);
                        if (scriptHeader != null)
                        {
                            TreeEditor.ShowRunScriptGUI(RootBranch, pbr, DataSource, scriptHeader);
                        }
                        // RefreshDatabaseEntites();
                        foreach (var entity in ls)
                        {
                            if (DataSource.CheckEntityExist(entity.EntityName))
                            {
                                entity.Created = true;
                                if (entity.Created == false)
                                {
                                    iconimage = "entitynotcreated.ico";
                                }
                                else
                                {
                                    iconimage = "databaseentities.ico";
                                }

                                DatabaseEntitesNode dbent = new DatabaseEntitesNode(TreeEditor, DMEEditor, pbr, entity.EntityName.ToUpper(), TreeEditor.SeqID, EnumPointType.Entity, iconimage, DataSource);
                                TreeEditor.AddBranch(pbr, dbent);
                                dbent.DataSourceName = DataSource.DatasourceName;
                                dbent.DataSource = DataSource;
                                pbr.ChildBranchs.Add(dbent);
                                DMEEditor.AddLogMessage("Success", $"Pasted Entity {entity.EntityName}", DateTime.Now, -1, null, Errors.Ok);
                            }
                            else
                            {
                                entity.Created = false;
                                DMEEditor.AddLogMessage("Fail", $"Error Copying Entity {entity.EntityName} - {DMEEditor.ErrorObject.Message}", DateTime.Now, -1, null, Errors.Failed);
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
         
        }
        [CommandAttribute(Name = "CopyDefaults", Caption = "Copy Default", Click = true, iconimage = "copydefaults.ico", PointType = EnumPointType.DataPoint)]
        public void CopyDefault(IPassedArgs Passedarguments)
        {
            GetValues(Passedarguments);
        }
        [CommandAttribute(Name = "PasteDefaults", Caption = "Paste Default", Click = true, iconimage = "pastedefaults.ico", PointType = EnumPointType.DataPoint)]
        public void PasteDefault(IPassedArgs Passedarguments)
        {
            GetValues(Passedarguments);

        }
        [CommandAttribute(Name = "Refresh", Caption = "Refresh", Click = true, iconimage = "refresh.ico", PointType = EnumPointType.DataPoint)]
        public void Refresh(IPassedArgs Passedarguments)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            //     DMEEditor.Logger.WriteLog($"Filling Database Entites ) ");
            try
            {
                string iconimage;
                GetValues(Passedarguments);
               
                if (DataSource != null)
                {
                    //  DataSource.Dataconnection.OpenConnection();
                    if (DataSource.ConnectionStatus == System.Data.ConnectionState.Open)
                    {
                        if (Visutil.controlEditor.InputBoxYesNo("Beep DM", "Are you sure, this might take some time?") == System.Windows.Forms.DialogResult.Yes)
                        {
                            DataSource.Entities.Clear();
                            DataSource.GetEntitesList();
                            IBranch br = TreeEditor.GetBranch(Passedarguments.Id);
                            TreeEditor.RemoveChildBranchs(br);
                            int i = 0;
                            TreeEditor.ShowWaiting();
                            TreeEditor.ChangeWaitingCaption($"Getting Entities for {Passedarguments.DatasourceName}  Total:{DataSource.EntitiesNames.Count}");
                            foreach (string tb in DataSource.EntitiesNames)
                            {
                                TreeEditor.AddCommentsWaiting($"{i} - Added {tb} to {Passedarguments.DatasourceName}");
                                EntityStructure ent = DataSource.GetEntityStructure(tb, true);
                                if (ent.Created == false)
                                {
                                    iconimage = "entitynotcreated.ico";
                                }
                                else
                                {
                                    iconimage = "databaseentities.ico";
                                }
                                DatabaseEntitesNode dbent = new DatabaseEntitesNode(TreeEditor, DMEEditor, br, tb, TreeEditor.SeqID, EnumPointType.Entity, iconimage, DataSource);
                                TreeEditor.AddBranch(br, dbent);
                                dbent.DataSourceName = DataSource.DatasourceName;
                                dbent.DataSource = DataSource;
                                br.ChildBranchs.Add(dbent);
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
           
        }
        [CommandAttribute(Caption = "Drop Entities", Name = "dropentities", Click = true, iconimage = "dropentities.ico", PointType = EnumPointType.DataPoint)]
        public IErrorsInfo DropEntities(IPassedArgs Passedarguments)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            EntityStructure ent = new EntityStructure() ;
            try
            {
                GetValues(Passedarguments);
                if (Visutil.controlEditor.InputBoxYesNo("Beep DM", "Are you sure you ?") == DialogResult.Yes)
                {
                    if (TreeEditor.SelectedBranchs.Count > 0)
                    {
                        foreach (int item in TreeEditor.SelectedBranchs)
                        {
                            IBranch br = TreeEditor.GetBranch(item);
                            if (br.DataSourceName == Passedarguments.DatasourceName)
                            {
                                IDataSource srcds = DMEEditor.GetDataSource(br.DataSourceName);
                                ent = DataSource.GetEntityStructure(br.BranchText, false);
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
                        DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new DataManagment_Engine.ConfigUtil.DatasourceEntities { datasourcename = Passedarguments.DatasourceName, Entities = DataSource.Entities });
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Ex = ex;
                DMEEditor.AddLogMessage("Fail", $"Error Drpping Entity {ent.EntityName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        [CommandAttribute(Caption = "Create POCO Classes", Name = "createpoco", Click = true, iconimage = "createpoco.ico", PointType = EnumPointType.DataPoint)]
        public IErrorsInfo CreatePOCOlasses(IPassedArgs Passedarguments)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                string iconimage;
                GetValues(Passedarguments);
                if (DataSource != null)
                {
                  
                    if (DataSource.ConnectionStatus == System.Data.ConnectionState.Open)
                    {
                        if (Visutil.controlEditor.InputBoxYesNo("Beep DM", "Are you sure, this might take some time?") == System.Windows.Forms.DialogResult.Yes)
                        {
                         
                            int i = 0;
                            TreeEditor.ShowWaiting();
                            TreeEditor.ChangeWaitingCaption($"Creating POCO Entities for total:{DataSource.EntitiesNames.Count}");
                            foreach (string tb in DataSource.EntitiesNames)
                            {
                                TreeEditor.AddCommentsWaiting($"{i} - Added {tb} to {Passedarguments.DatasourceName}");
                                EntityStructure ent = DataSource.GetEntityStructure(tb, true);
                                DMEEditor.classCreator.CreateClass(ent.EntityName, ent.Fields, DMEEditor.ConfigEditor.ExePath);
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
    }
}
