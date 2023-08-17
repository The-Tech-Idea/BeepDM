using DataManagementModels.ConfigUtil;
using DataManagementModels.Editor;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Mapping;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Workflow.Mapping;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.Editor
{
    public partial class DataImportManager
    {
       
        public string SourceEntityName { get; set; } = string.Empty;
        public string DestEntityName { get; set; } = string.Empty;
        public string SourceDataSourceName { get; set; } = string.Empty;
        public string DestDataSourceName { get; set; } = string.Empty;
        public EntityStructure SourceEntityStructure { get; set; }
        public EntityStructure DestEntityStructure { get; set; }
        public IDataSource SourceData { get; set; }
        public IDataSource DestData { get; set; }
        public DefaultValueType DefaultValueType { get; set; }
        public UnitofWork<Entity> SrcunitofWork { get; set; }
        public UnitofWork<Entity> DstunitofWork { get; set; }
        public UnitofWork<EntityDataMap_DTL> MappingunitofWork { get; set; }
        public EntityDataMap_DTL CurrentMappingDTL { get; set; }
        public EntityDataMap Mapping { get; set; }
        public IDMEEditor DMEEditor { get; }
        public List<DefaultValue> SourceDefaults { get; set; } = new List<DefaultValue>();
        public List<DefaultValue> DestDefaults { get; set; } = new List<DefaultValue>();
        public List<IWorkFlowRule> Rules { get; set; }=new List<IWorkFlowRule>();
        bool IsEntitychanged = false;
        public DataImportManager(IDMEEditor dMEEditor)
        {
            DMEEditor = dMEEditor;

        }
        public DataImportManager(IDMEEditor dMEEditor, string destEntityName, string destDataSourceName)
        {
            DMEEditor = dMEEditor;
            LoadMapping();
            if (Mapping == null)
            {
                LoadDestEntityStructure(destEntityName, destDataSourceName);
                Mapping.MappedEntities.Add( MappingManager.AddEntitytoMappedEntities(dMEEditor, SourceEntityStructure, DestEntityStructure));
            }
        }
        public DataImportManager(IDMEEditor dMEEditor, string destEntityName, string destDataSourceName, string srcEntityName, string srcDataSourceName)
        {
            DMEEditor = dMEEditor;
            LoadMapping();
            if (Mapping == null)
            {
                LoadDestEntityStructure(destEntityName, destDataSourceName);
                LoadSourceEntityStructure(srcEntityName, srcDataSourceName);
                Mapping.MappedEntities.Add(MappingManager.AddEntitytoMappedEntities(dMEEditor, SourceEntityStructure, DestEntityStructure));
            }

        }
        public IErrorsInfo LoadDestEntityStructure(string destEntityName, string destDataSourceName)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                DestData = DMEEditor.GetDataSource(destDataSourceName);
                if (DestData != null)
                {
                    DestDataSourceName = destDataSourceName; DestEntityName = destEntityName;
                    if (DestData.ConnectionStatus == ConnectionState.Open)
                    {
                        DestDefaults = DMEEditor.Getdefaults(destDataSourceName);
                        DestEntityStructure = (EntityStructure)DestData.GetEntityStructure(destEntityName, false).Clone();
                    }
                    else DMEEditor.AddLogMessage("Beep", $"Error Could open  Destination Datasource {destDataSourceName} ", DateTime.Now, 0, null, Errors.Failed);
                }
                else DMEEditor.AddLogMessage("Beep", $"Error Could Get  Destination Datasource {destDataSourceName} ", DateTime.Now, 0, null, Errors.Failed);

            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error Loading Destination Data for  {destDataSourceName} -{destEntityName} - {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        public IErrorsInfo LoadSourceEntityStructure(string srcEntityName, string srcDataSourceName)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                SourceData = DMEEditor.GetDataSource(srcDataSourceName);
                if (DestData != null)
                {
                    SourceDataSourceName = srcDataSourceName;
                    SourceEntityName = srcEntityName;
                    if (SourceData.ConnectionStatus == ConnectionState.Open)
                    {
                        DestEntityStructure = (EntityStructure)SourceData.GetEntityStructure(srcEntityName, false).Clone();
                    }
                    else DMEEditor.AddLogMessage("Beep", $"Error Could open  Destination Datasource {srcDataSourceName} ", DateTime.Now, 0, null, Errors.Failed);
                }
                else DMEEditor.AddLogMessage("Beep", $"Error Could Get  Destination Datasource {srcDataSourceName} ", DateTime.Now, 0, null, Errors.Failed);

            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error Loading Destination Data for  {srcDataSourceName} -{srcEntityName} - {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        public IErrorsInfo LoadMapping(string destEntityName, string destDataSourceName)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                if (!destEntityName.Equals(DestEntityName, StringComparison.InvariantCultureIgnoreCase) || !destDataSourceName.Equals(DestDataSourceName, StringComparison.InvariantCultureIgnoreCase))
                {
                    IsEntitychanged = true;
                    DestDataSourceName = destDataSourceName; DestEntityName = destEntityName;
                }
                Mapping = DMEEditor.ConfigEditor.LoadMappingValues(destEntityName, destDataSourceName);
                if (DestEntityStructure == null || IsEntitychanged)
                {
                    LoadDestEntityStructure(destEntityName, destDataSourceName);
                }
                MappingunitofWork = new UnitofWork<EntityDataMap_DTL>(DMEEditor, true,new ObservableBindingList<EntityDataMap_DTL>(Mapping.MappedEntities), "GuidID");
                MappingunitofWork.PrimaryKey = "GuidID";
                IsEntitychanged = false;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error Loading Mapping File Data {destDataSourceName} -{destEntityName} - {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;

        }
        public IErrorsInfo LoadMapping()
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {

                Mapping = DMEEditor.ConfigEditor.LoadMappingValues(DestEntityName, DestDataSourceName);
                if (DestEntityStructure == null || IsEntitychanged)
                {
                    LoadDestEntityStructure(DestEntityName, DestDataSourceName);
                }
                if (SourceEntityName == null || IsEntitychanged)
                {
                    LoadSourceEntityStructure(SourceEntityName,SourceDataSourceName);
                }
                MappingunitofWork = new UnitofWork<EntityDataMap_DTL>(DMEEditor, true, new ObservableBindingList<EntityDataMap_DTL>(Mapping.MappedEntities), "GuidID");
                MappingunitofWork.PrimaryKey = "GuidID";
              

            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error Loading Mapping File Data {DestDataSourceName} -{DestEntityName} - {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;

        }
        public IErrorsInfo SaveMapping()
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                Mapping.MappedEntities = MappingunitofWork.Units.ToList();
                 DMEEditor.ConfigEditor.SaveMappingValues(DestEntityName, DestDataSourceName,Mapping);

            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error Loading Mapping File Data {DestDataSourceName} -{DestEntityName} - {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;

        }
        private bool GetIsEntityChanges(string sourceentityname, string destentityname)
        {
            if (!SourceEntityName.Equals(sourceentityname, StringComparison.InvariantCultureIgnoreCase) ||
                (!DestEntityName.Equals(destentityname, StringComparison.InvariantCultureIgnoreCase)))
            {
                IsEntitychanged = true;
                return true;
            }
            else
            {
                IsEntitychanged = false;
                return false;
            }

        }
        public IErrorsInfo RunImport(IProgress<IPassedArgs> progress, CancellationToken token )
        {
            try
            {
                      
                var ScriptRun = Task.Run(() => {
                     token.Register(() => StopTask());
                    DMEEditor.ETL.CreateImportScript(Mapping, CurrentMappingDTL);
                    DMEEditor.ETL.RunImportScript(progress, token).Wait();
                }).ContinueWith(t => { Update(); });
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error Running Import Data {DestDataSourceName} -{DestEntityName} - {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        public void StopTask()
        {
            DMEEditor.AddLogMessage("Beep", $"Error Running Import Data {DestDataSourceName} -{DestEntityName} Stopped by user", DateTime.Now, 0, null, Errors.Failed);
        }
        public void Update()
        {
            DMEEditor.AddLogMessage("Beep", $"Running Import Data {DestDataSourceName} -{DestEntityName} Finished", DateTime.Now, 0, null, Errors.Ok);
        }
    }
}
