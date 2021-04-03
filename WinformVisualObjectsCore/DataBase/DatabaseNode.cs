using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                DataSource = (IRDBSource)DMEEditor.GetDataSource(BranchText);
                if (DataSource != null)
                {

                    if (DataSource.ConnectionStatus == System.Data.ConnectionState.Open)
                    {
                        DataSource.GetEntitesList();

                        int i = 0;
                        foreach (string tb in DataSource.EntitiesNames)
                        {

                            DatabaseEntitesNode dbent = new DatabaseEntitesNode(TreeEditor, DMEEditor, this,tb , TreeEditor.SeqID, EnumBranchType.Entity, "entity.ico", DataSource);
                            TreeEditor.AddBranch(this, dbent);
                            dbent.DataSourceName = DataSource.DatasourceName;
                            dbent.DataSource = DataSource;
                            ChildBranchs.Add(dbent);
                            i += 1;


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
        [BranchDelegate(Caption = "Copy Entities")]
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
        [BranchDelegate(Caption = "Paste Entities")]
        public IErrorsInfo PasteEntities()
        {

            try
            {

                if (TreeEditor.args != null)
                {
                    if (TreeEditor.args.EventType == "COPYENTITY" || TreeEditor.args.EntitiesNames.Count>0)
                    {
                        if (TreeEditor.args.Objects != null)
                        {
                            List<LScript> scripts = new List<LScript>();
                            IBranch pbr = (IBranch)TreeEditor.args.Objects.Where(x => x.Name == "ParentBranch").FirstOrDefault().obj;
                            pbr.DataSource = DMEEditor.GetDataSource(pbr.BranchText);
                            DataSource = DMEEditor.GetDataSource(BranchText);
                         //   scripts.AddRange(pbr.DataSource.GetCreateEntityScript(TreeEditor.args.EntitiesNames));

                          //  DMEEditor.RDBMSHelper.CopyEntities(pbr.DataSource, DataSource, TreeEditor.args.EntitiesNames, true);
  
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
    }
}
