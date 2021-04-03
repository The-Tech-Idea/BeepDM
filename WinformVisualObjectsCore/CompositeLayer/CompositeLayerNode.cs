using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.CompositeLayer;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Editor;
using TheTechIdea.DataManagment_Engine.Vis;
using TheTechIdea.Util;

namespace TheTechIdea.Winforms.VIS
{
    public class CompositeLayerNode : IBranch, ITreeView
    {
        public CompositeLayerNode()
        {

        }
        public CompositeLayerNode(ITree pTreeEditor, IDMEEditor pDMEEditor, IBranch pParentNode, string pBranchText, int pID, EnumBranchType pBranchType, CompositeLayer pClayer)
        {
            TreeEditor = pTreeEditor;
            DMEEditor = pDMEEditor;
            ParentBranchID = pParentNode.ID;
            BranchText = pBranchText;
            DataSourceName = pBranchText;
            BranchType = pBranchType;
            CLayer = CLayer;
            ConnectionProperties cn = DMEEditor.ConfigEditor.DataConnections.Where(x => x.ConnectionName == pClayer.DataSourceName).FirstOrDefault();
           if (cn != null)
            {
                ConnectionDriversConfig driversConfig = DMEEditor.Utilfunction.LinkConnection2Drivers(cn);
                try
                {
                    compositeLayerDataSource = new CompositeLayerDataSource(cn.ConnectionName, DMEEditor.Logger, DMEEditor, cn.DatabaseType, DMEEditor.ErrorObject);
                    //  compositeLayerDataSource.LayerInfo = DMEEditor.ConfigEditor.CompositeQueryLayers[DMEEditor.ConfigEditor.CompositeQueryLayers.FindIndex(x => x.LayerName == cn.CompositeLayerName)];
                    compositeLayerDataSource.Dataconnection.ConnectionProp = cn;
                    compositeLayerDataSource.Dataconnection.DataSourceDriver = driversConfig;
                    IDataSource ds = DMEEditor.CreateLocalDataSourceConnection(cn, cn.ConnectionName, driversConfig.classHandler);
                    compositeLayerDataSource.LocalDB = (ILocalDB)ds;
                    compositeLayerDataSource.Dataconnection.OpenConnection();
                }
                catch (Exception )
                {

                   
                }
                
            }else
            {
                MessageBox.Show("Error: Could not Find Local Database for Composite Layer");
            }
           
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
             //   IconImageName = pimagename;
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
        public string IconImageName { get; set; } = "clayer.ico";
        public string BranchStatus { get; set; }
        public int ParentBranchID { get; set; }
        public string BranchDescription { get; set; }
        public string BranchClass { get; set; } = "CLAYER";
        public List<IBranch> ChildBranchs { get; set; } = new List<IBranch>();
        public ITree TreeEditor { get; set; }
        public List<string> BranchActions { get; set; }
        public List<Delegate> Delegates { get; set; }
        public int ID { get; set; }
        public object TreeStrucure { get ; set ; }
        public IVisUtil Visutil { get ; set ; }
        public int MiscID { get; set; }
        public CompositeLayerDataSource compositeLayerDataSource { get; set; }
        CompositeLayer CLayer = new CompositeLayer();
       // public event EventHandler<PassedArgs> BranchSelected;
       // public event EventHandler<PassedArgs> BranchDragEnter;
       // public event EventHandler<PassedArgs> BranchDragDrop;
       // public event EventHandler<PassedArgs> BranchDragLeave;
       // public event EventHandler<PassedArgs> BranchDragClick;
       // public event EventHandler<PassedArgs> BranchDragDoubleClick;
       // public event EventHandler<PassedArgs> ActionNeeded;

        public IErrorsInfo CreateChildNodes()
        {
           return GetEntites();
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
        private void CheckCreatedentities()
        {
            List<EntityStructure> ls = new List<EntityStructure>();
            ls = compositeLayerDataSource.LayerInfo.Entities.Where(x => x.Created == false).ToList();
            foreach (EntityStructure item in ls)
            {
                compositeLayerDataSource.LayerInfo.Entities[compositeLayerDataSource.LayerInfo.Entities.FindIndex(x => x.EntityName.ToLower() == item.EntityName.ToLower())].Created = compositeLayerDataSource.CheckEntityExist(item.EntityName);

                if (compositeLayerDataSource.LayerInfo.Entities[compositeLayerDataSource.LayerInfo.Entities.FindIndex(x => x.EntityName.ToLower() == item.EntityName.ToLower())].Created)
                {
                    compositeLayerDataSource.LayerInfo.Entities[compositeLayerDataSource.LayerInfo.Entities.FindIndex(x => x.EntityName.ToLower() == item.EntityName.ToLower())].DataSourceID = compositeLayerDataSource.DatasourceName;
                }

            }
            DMEEditor.ConfigEditor.SaveCompositeLayersValues();
        }
        #region "Exposed Methods"
        [BranchDelegate(Caption = "Get Entities")]
        public IErrorsInfo GetEntites()
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
          
            try
            {
              if (compositeLayerDataSource != null)
              {
                    
                    if (compositeLayerDataSource.ConnectionStatus == System.Data.ConnectionState.Open)
                    {
                        CheckCreatedentities();
                        
                        TreeEditor.RemoveChildBranchs(this);
                    
                        foreach (EntityStructure tb in compositeLayerDataSource.LayerInfo.Entities)
                        {
                            
                            CompositeLayerEntitesNode dbent = new CompositeLayerEntitesNode(TreeEditor, DMEEditor, this, tb, TreeEditor.SeqID, EnumBranchType.Entity, "entity.ico", compositeLayerDataSource);
                            TreeEditor.AddBranch(this, dbent);
                            dbent.DataSourceName = compositeLayerDataSource.DatasourceName;
                            dbent.compositeLayerDataSource = compositeLayerDataSource;
                            dbent.DataSource = compositeLayerDataSource;
                            ChildBranchs.Add(dbent);
                         
                          


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
        [BranchDelegate(Caption = "Sync. Layer w. View")]
        public IErrorsInfo SyncMissingEntites()
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
           
            try
            {
               if (compositeLayerDataSource != null)
                {
                    if (compositeLayerDataSource.ConnectionStatus != System.Data.ConnectionState.Open)
                    {
                        compositeLayerDataSource.Dataconnection.OpenConnection();
                    }
                    if (compositeLayerDataSource.ConnectionStatus == System.Data.ConnectionState.Open)
                    {
                       
                        compositeLayerDataSource.GetAllEntitiesFromDataView();
                        DMEEditor.ErrorObject = GetEntites();
                    }
                  //  CheckCreatedentities();
                    
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
        [BranchDelegate(Caption = "Create Entites")]
        public IErrorsInfo CreateMissingEntites()
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            DMEEditor.Logger.WriteLog($"Filling Database Entites ) ");
            try
            {
                //IDataSource layerds;
               // IDataSource ds;
              //  layerds = DMEEditor.GetDataSource(compositeLayerDataSource.LayerInfo.DataSourceName);
               // List<EntityStructure> ls = new List<EntityStructure>();
               // ls = compositeLayerDataSource.LayerInfo.Entities.Where(x => x.Created == false).ToList();
               
                if (compositeLayerDataSource != null)
                {
                   
                    if (compositeLayerDataSource.ConnectionStatus == System.Data.ConnectionState.Open)
                    {
                        CheckCreatedentities();

                            string[] args = { "New Query Entity", null, null };
                            List<ObjectItem> ob = new List<ObjectItem>(); ;
                            ObjectItem it = new ObjectItem();
                            it.obj = this;
                            it.Name = "Branch";
                            ob.Add(it);
                            IBranch RootBranch = TreeEditor.Branches[TreeEditor.Branches.FindIndex(x => x.BranchClass == "CLAYER" && x.BranchType == EnumBranchType.Root)];
                            it = new ObjectItem();
                            it.obj = RootBranch;
                            it.Name = "RootBranch";
                            ob.Add(it);
                            it = new ObjectItem();
                            it.obj = DMEEditor;
                            it.Name = "DMEEDITOR";
                            ob.Add(it);
                             it = new ObjectItem();
                             it.obj = compositeLayerDataSource;
                             it.Name = "Compositeds";
                             ob.Add(it);
                        PassedArgs Passedarguments = new PassedArgs
                            {
                                Addin = null,
                                AddinName = null,
                                AddinType = "",
                                DMView = null,
                                CurrentEntity = null,

                                ObjectType = "SCRIPT",
                                DataSource = compositeLayerDataSource,
                                ObjectName = compositeLayerDataSource.DatasourceName,

                                Objects = ob,

                                DatasourceName = null,
                                EventType = "RUNSCRIPT"

                            };
                        DMEEditor.ETL.script= CreateScript();
                        Visutil.ShowUserControlInContainer("uc_ScriptRun", Visutil.DisplayPanel, DMEEditor, args, Passedarguments);


                        //     DMEEditor.ErrorObject = GetEntites();
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
        [BranchDelegate(Caption = "Paste Entity")]
        public IErrorsInfo CopyEntity()
        {

            try
            {
                // IBranch pbr = TreeEditor.Branches.Where(x => x.BranchType == EnumBranchType.Root && x.BranchClass == "VIEW").FirstOrDefault();
                //PassedArgs args = new PassedArgs
                //{
                //    ObjectName = "DATABASE",
                //    ObjectType = "TABLE",
                //    EventType = "COPYENTITY",
                //    Parameter = "COPYENTITY",
                //    DataSource = DataSource,
                //    DatasourceName = DataSource.DatasourceName,
                //    CurrentEntity = BranchText,
                //    Objects = new List<ObjectItem> { new ObjectItem { Name = "Branch", obj = this } }
                //};
                if (TreeEditor.args != null)
                {
                    if (TreeEditor.args.EventType == "COPYENTITY")
                    {
                        if (TreeEditor.args.Objects!=null)
                        {
                            IBranch pbr =(IBranch) TreeEditor.args.Objects.Where(x => x.Name == "Branch").FirstOrDefault().obj;
                            EntityStructure entity=(EntityStructure)TreeEditor.args.Objects.Where(x => x.Name == "Entity").FirstOrDefault().obj;
                            if (compositeLayerDataSource.CheckEntityExist(entity.EntityName))
                            {
                                DMEEditor.AddLogMessage("Fail" , $"Could Not Paste Entity {entity.EntityName}, it already exist", DateTime.Now, -1, null, Errors.Failed);
                            }
                            else
                            {
                                entity.Created = false;
                                compositeLayerDataSource.LayerInfo.Entities.Add(entity);
                                DMEEditor.ConfigEditor.SaveCompositeLayersValues();
                                GetEntites();
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
        [BranchDelegate(Caption = "Remove Layer")]
        public IErrorsInfo RemoveLayer()
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;


            try
            {
                if (Visutil.controlEditor.InputBoxYesNo("Remove Layer", "Area you Sure ? you want to remove Layer???") == System.Windows.Forms.DialogResult.Yes)
                {
                    ConnectionProperties cn = DMEEditor.ConfigEditor.DataConnections.Where(x => string.Equals(Path.GetFileName(BranchText), x.ConnectionName, StringComparison.OrdinalIgnoreCase) ).FirstOrDefault();
                    string file = Path.Combine(cn.FilePath, cn.FileName);
                    bool ok=false;
                    try
                    {
                        if (cn != null)
                        {
                            if (DMEEditor.ConfigEditor.CompositeQueryLayers.Any(p=> string.Equals(p.LayerName, cn.ConnectionName, StringComparison.OrdinalIgnoreCase) ))
                            {
                                 ok = DMEEditor.ConfigEditor.RemoveLayerByName(cn.ConnectionName);
                            }
                            if (DMEEditor.DataSources.Any(p => p.DatasourceName.ToLower() == cn.ConnectionName.ToLower()))
                            {
                                ok = DMEEditor.RemoveDataDource(cn.ConnectionName);
                            }
                           
                                if (ok)
                                {
                                    if (Visutil.controlEditor.InputBoxYesNo("Remove Layer", "Do you want to Delete the Layer File ???") == System.Windows.Forms.DialogResult.Yes)
                                    {

                                        if (compositeLayerDataSource.DropDatabase())
                                        {
                                            DMEEditor.ConfigEditor.SaveCompositeLayersValues();
                                        if (DMEEditor.ConfigEditor.DataConnections.Any(p => p.ConnectionName.ToLower() == cn.ConnectionName.ToLower()))
                                        {
                                            ok = DMEEditor.ConfigEditor.RemoveDataConnection(cn.ConnectionName);
                                            DMEEditor.ConfigEditor.SaveDataconnectionsValues();
                                        }
                                      
                                            TreeEditor.RemoveBranch(this);
                                            DMEEditor.AddLogMessage("Success", "Removed Data file", DateTime.Now, 0, null, Errors.Ok);
                                        }
                                        else
                                        {
                                            DMEEditor.AddLogMessage("Fail", "Couldnot Remove Data File", DateTime.Now, 0, null, Errors.Failed);
                                        }
                                    }
                                }
                               
                        }
                      
                    }
                    catch (Exception ex)
                    {
                        string mes = "Could not Remove View from Layer List";
                        DMEEditor.AddLogMessage("Fail",ex.Message+ mes, DateTime.Now, -1, mes, Errors.Failed);
                    };

                    //try
                    //{

                    //    DMEEditor.DataSources.Remove(DMEEditor.DataSources.Where(x => x.DatasourceName == BranchText).FirstOrDefault());
                    //    DMEEditor.AddLogMessage("Success", "Removed View from DataSource List", DateTime.Now, 0, null, Errors.Ok);
                    //}
                    //catch (Exception ex)
                    //{
                    //    string mes = "Could not Removed View from DataSource List";
                    //    DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
                    //};

                  
                   

                }
              
            }
            catch (Exception ex)
            {
                string mes = "Could not Remove Layer";
                DMEEditor.AddLogMessage("Fail",ex.Message+ mes, DateTime.Now, -1, mes, Errors.Failed);
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
        [BranchDelegate(Caption = "Copy Defaults")]
        public IErrorsInfo CopyDefaults()
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            //  DMEEditor.Logger.WriteLog($"Filling Database Entites ) ");
            try
            {
                List<DefaultValue> defaults = DMEEditor.ConfigEditor.DataConnections[DMEEditor.ConfigEditor.DataConnections.FindIndex(i => i.ConnectionName == BranchText)].DatasourceDefaults;
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
                    if (TreeEditor.args.ObjectType == "COPYDEFAULTS")
                    {
                        List<DefaultValue> defaults = (List<DefaultValue>)TreeEditor.args.Objects[0].obj;
                        DMEEditor.ConfigEditor.DataConnections[DMEEditor.ConfigEditor.DataConnections.FindIndex(i => i.ConnectionName == BranchText)].DatasourceDefaults = defaults;
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
      
        #endregion "Exposed Methods"
        #region "Util"
        private LScriptHeader CreateScript()
        {
            List<EntityStructure> ls = new List<EntityStructure>();
          //  IDataSource ds;

           // ls = compositeLayerDataSource.LayerInfo.Entities.Where(x => x.Created == false).ToList();
            //foreach (EntityStructure item in ls)
            //{
            //    ds = DMEEditor.GetDataSource(item.DataSourceID);
            //    EntityStructure t1 = ds.GetEntityStructure(item.EntityName, true);
                
            //    t1.Created = false;
            //    if (DMEEditor.ErrorObject.Flag == Errors.Ok)
            //    {
            //        compositeLayerDataSource.LayerInfo.Entities[compositeLayerDataSource.LayerInfo.Entities.FindIndex(x => x.EntityName.ToLower() == item.EntityName.ToLower())] = t1;
            //    }
            //    else
            //    {
            //        DMEEditor.AddLogMessage("Fail", $"Error getting entity structure for {item.EntityName}", DateTime.Now, item.Id, item.DataSourceID, Errors.Failed);
            //    }

            //}
            ls = compositeLayerDataSource.LayerInfo.Entities.Where(x => x.Created == false).ToList();
            if (ls.Count > 0)
            {
                DMEEditor.ETL.script = new LScriptHeader();
                DMEEditor.ETL.script.scriptSource = compositeLayerDataSource.DatasourceName;
                DMEEditor.ETL.GetCreateEntityScript(compositeLayerDataSource, ls);
                foreach (var item in ls)
                {
                    LScript upscript = new LScript();
                    upscript.sourcedatasourcename = item.DataSourceID;
                    upscript.entityname = item.EntityName;
                    upscript.destinationdatasourcename = compositeLayerDataSource.DatasourceName;
                    upscript.scriptType = DDLScriptType.CopyData;
                    DMEEditor.ETL.script.Scripts.Add(upscript);
                }
            }

            return DMEEditor.ETL.script;
            
        }
        #endregion
    }
}
