using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        public IErrorsInfo CopyDefault(IPassedArgs Passedarguments)
        {
            GetValues(Passedarguments);
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            //  DMEEditor.Logger.WriteLog($"Filling Database Entites ) ");
            try
            {
                List<DefaultValue> defaults = DMEEditor.ConfigEditor.DataConnections[DMEEditor.ConfigEditor.DataConnections.FindIndex(i => i.ConnectionName == Passedarguments.DatasourceName)].DatasourceDefaults;
                if (defaults != null)
                {
                    string[] args = { "CopyDefaults", null, null };
                    List<ObjectItem> ob = new List<ObjectItem>(); ;
                    ObjectItem it = new ObjectItem();
                    it.obj = defaults;
                    it.Name = "Defaults";
                    ob.Add(it);
                    Passedarguments.CurrentEntity = Passedarguments.DatasourceName;
                    Passedarguments.Id = 0;
                    Passedarguments.ObjectType = "COPYDEFAULTS";
                    Passedarguments.ObjectName = Passedarguments.DatasourceName;
                    Passedarguments.Objects = ob;
                    Passedarguments.DatasourceName = Passedarguments.DatasourceName;
                    Passedarguments.EventType = "COPYDEFAULTS";
                  
                    TreeEditor.args = (PassedArgs)Passedarguments;
                    DMEEditor.Passedarguments = Passedarguments;
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
        [CommandAttribute(Name = "PasteDefaults", Caption = "Paste Default", Click = true, iconimage = "pastedefaults.ico", PointType = EnumPointType.DataPoint)]
        public IErrorsInfo PasteDefault(IPassedArgs Passedarguments)
        {
            GetValues(Passedarguments);

            DMEEditor.ErrorObject.Flag = Errors.Ok;
            //  DMEEditor.Logger.WriteLog($"Filling Database Entites ) ");
            try
            {
                if (TreeEditor.args != null)
                {
                    if (TreeEditor.args.ObjectType == "COPYDEFAULTS")
                    {
                        if (Passedarguments.Objects.Where(o => o.Name == "Defaults").Any())
                        {
                            List<DefaultValue> defaults = (List<DefaultValue>)Passedarguments.Objects.Where(o => o.Name == "Defaults").FirstOrDefault().obj;
                            DMEEditor.ConfigEditor.DataConnections[DMEEditor.ConfigEditor.DataConnections.FindIndex(i => i.ConnectionName == DataSource.DatasourceName)].DatasourceDefaults = defaults;
                            DMEEditor.ConfigEditor.SaveDataconnectionsValues();
                        }
                       
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
                            try
                            {
                                if (!Directory.Exists(Path.Combine(DMEEditor.ConfigEditor.Config.ScriptsPath, Passedarguments.DatasourceName)))
                                {
                                    Directory.CreateDirectory(Path.Combine(DMEEditor.ConfigEditor.Config.ScriptsPath, Passedarguments.DatasourceName));
                                };
                                foreach (string tb in DataSource.EntitiesNames)
                                {
                                    TreeEditor.AddCommentsWaiting($"{i} - Added {tb} to {Passedarguments.DatasourceName}");
                                    EntityStructure ent = DataSource.GetEntityStructure(tb, true);

                                    DMEEditor.classCreator.CreateClass(ent.EntityName, ent.Fields, Path.Combine(DMEEditor.ConfigEditor.Config.ScriptsPath, Passedarguments.DatasourceName));
                                    i += 1;
                                }

                            }
                            catch (Exception ex1)
                            {

                                DMEEditor.AddLogMessage("Fail", $"Could not Create Directory or error in Generating Class {ex1.Message}", DateTime.Now, 0, Passedarguments.DatasourceName, Errors.Failed);
                            }
                           
                            TreeEditor.HideWaiting();
                        }

                    }

                }



            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $" error in Generating Class {ex.Message}", DateTime.Now, 0, Passedarguments.DatasourceName, Errors.Failed);
            }
            return DMEEditor.ErrorObject;

        }
        [CommandAttribute(Caption = "Create DLL Classes", Name = "createdll", Click = true, iconimage = "dllgen.ico", PointType = EnumPointType.DataPoint)]
        public IErrorsInfo CreateDLLclasses(IPassedArgs Passedarguments)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                string iconimage;
                GetValues(Passedarguments);
                List<EntityStructure> ls = new List<EntityStructure>();
                if (DataSource != null)
                {

                    if (DataSource.ConnectionStatus == System.Data.ConnectionState.Open)
                    {
                        if (Visutil.controlEditor.InputBoxYesNo("Beep DM", "Are you sure, this might take some time?") == System.Windows.Forms.DialogResult.Yes)
                        {

                            int i = 0;
                            TreeEditor.ShowWaiting();
                            TreeEditor.ChangeWaitingCaption($"Creating POCO Entities for total:{DataSource.EntitiesNames.Count}");
                            try
                            {
                                if (!Directory.Exists(Path.Combine(DMEEditor.ConfigEditor.Config.ScriptsPath, Passedarguments.DatasourceName)))
                                {
                                    Directory.CreateDirectory(Path.Combine(DMEEditor.ConfigEditor.Config.ScriptsPath, Passedarguments.DatasourceName));
                                };
                                foreach (string tb in DataSource.EntitiesNames)
                                {
                                    TreeEditor.AddCommentsWaiting($"{i} - Added {tb} to {Passedarguments.DatasourceName}");
                                    EntityStructure ent = DataSource.GetEntityStructure(tb, true);
                                    ls.Add(ent);
                               
                                    i += 1;
                                }
                                if (ls.Count > 0)
                                {
                                    DMEEditor.classCreator.CreateDLL(Regex.Replace(Passedarguments.DatasourceName, @"\s+", "_") ,ls, Path.Combine(DMEEditor.ConfigEditor.Config.ScriptsPath, Passedarguments.DatasourceName),"TheTechIdea."+ Regex.Replace(Passedarguments.DatasourceName, @"\s+", "_"));
                                }

                            }
                            catch (Exception ex1)
                            {

                                DMEEditor.AddLogMessage("Fail", $"Could not Create Directory or error in Generating DLL {ex1.Message}", DateTime.Now, 0, Passedarguments.DatasourceName, Errors.Failed);
                            }

                            TreeEditor.HideWaiting();
                        }

                    }

                }



            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $" error in Generating DLL {ex.Message}", DateTime.Now, 0, Passedarguments.DatasourceName, Errors.Failed);
            }
            return DMEEditor.ErrorObject;

        }
        [CommandAttribute(Caption = "Turnon/Off CheckBox's", Name = "Turnon/Off CheckBox", Click = true, iconimage = "checkbox.ico", PointType = EnumPointType.DataPoint)]
        public IErrorsInfo TurnonOffCheckBox(IPassedArgs Passedarguments)
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
                        TreeView trv = (TreeView)TreeEditor.TreeStrucure;
                        trv.CheckBoxes = !trv.CheckBoxes;
                        TreeEditor.SelectedBranchs.Clear();
                    }

                }



            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Could not select entities {ex.Message}", DateTime.Now, 0, Passedarguments.DatasourceName, Errors.Failed);
            }
            return DMEEditor.ErrorObject;

        }
        [CommandAttribute(Caption = "Data Connection", Name = "dataconnection", Click = true, iconimage = "dataconnection.ico", PointType = EnumPointType.Global)]
        public IErrorsInfo dataconnection(IPassedArgs Passedarguments)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                string iconimage;
                //GetValues(Passedarguments);
                Visutil.ShowUserControlInContainer("uc_DataConnection", Visutil.DisplayPanel, DMEEditor, null, null);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Could not show data connection {ex.Message}", DateTime.Now, 0, Passedarguments.DatasourceName, Errors.Failed);
            }
            return DMEEditor.ErrorObject;

        }

        //
    }
}
