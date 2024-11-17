using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
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
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Addin;

namespace TheTechIdea.Beep.Editor
{
    /// <summary>
    /// Represents a data import manager that handles importing data from a source entity to a destination entity.
    /// </summary>
    public partial class DataImportManager
    {
        /// <summary>
        /// Gets or sets the name of the source entity.
        /// </summary>

        /// <summary>Gets or sets the name of the source entity.</summary>
        /// <value>The name of the source entity.</value>
        public string SourceEntityName { get; set; } = string.Empty;
        /// <summary>Gets or sets the destination entity name.</summary>
        /// <value>The destination entity name.</value>
        public string DestEntityName { get; set; } = string.Empty;
        /// <summary>Gets or sets the name of the data source for the source.</summary>
        /// <value>The name of the data source.</value>
        public string SourceDataSourceName { get; set; } = string.Empty;
        /// <summary>Gets or sets the destination data source name.</summary>
        /// <value>The destination data source name.</value>
        public string DestDataSourceName { get; set; } = string.Empty;
        /// <summary>Gets or sets the source entity structure.</summary>
        /// <value>The source entity structure.</value>
        public EntityStructure SourceEntityStructure { get; set; }
        /// <summary>Gets or sets the destination entity structure.</summary>
        /// <value>The destination entity structure.</value>
        public EntityStructure DestEntityStructure { get; set; }
        /// <summary>Gets or sets the source data for the object.</summary>
        /// <value>The source data.</value>
        public IDataSource SourceData { get; set; }
        /// <summary>Gets or sets the destination data source.</summary>
        /// <value>The destination data source.</value>
        public IDataSource DestData { get; set; }
        /// <summary>Gets or sets the default value type.</summary>
        /// <value>The default value type.</value>
        public DefaultValueType DefaultValueType { get; set; }
        /// <summary>Gets or sets the unit of work for the source entity.</summary>
        /// <value>The unit of work for the source entity.</value>
        public IUnitofWork SrcunitofWork { get; set; }
        /// <summary>Gets or sets the unit of work for the destination entity.</summary>
        /// <value>The unit of work for the destination entity.</value>
        public IUnitofWork DstunitofWork { get; set; }
        /// <summary>Gets or sets the unit of work for mapping entity data.</summary>
        /// <value>The unit of work for mapping entity data.</value>
        public UnitofWork<EntityDataMap_DTL> MappingunitofWork { get; set; }
        /// <summary>Gets or sets the current mapping data transfer layer (DTL) entity data map.</summary>
        /// <value>The current mapping DTL entity data map.</value>
        public EntityDataMap_DTL CurrentMappingDTL { get; set; }
        /// <summary>Gets or sets the mapping of entity data.</summary>
        /// <value>The mapping of entity data.</value>
        /// <summary>Gets or sets the mapping of entity data.</summary>
        /// <value>The mapping of entity data.</value>
        public EntityDataMap Mapping { get; set; }
        /// <summary>Gets the DME editor.</summary>
        /// <value>The DME editor.</value>
        public IDMEEditor DMEEditor { get; }
        bool IsEntitychanged = false;
        /// <summary>Gets or sets the list of default values for the source.</summary>
        /// <value>The list of default values for the source.</value>
        public ObservableBindingList<DefaultValue> SourceDefaults { get; set; } = new ObservableBindingList<DefaultValue>();
        /// <summary>Gets or sets the list of default values for the destination.</summary>
        /// <value>The list of default values for the destination.</value>
        public ObservableBindingList<DefaultValue> DestDefaults { get; set; } = new ObservableBindingList<DefaultValue>();
        /// <summary>Gets or sets the list of workflow rules.</summary>
        /// <value>The list of workflow rules.</value>
        public List<IWorkFlowRule> Rules { get; set; } = new List<IWorkFlowRule>();
        /// <summary>Initializes a new instance of the DataImportManager class.</summary>
        /// <param name="dMEEditor">The IDMEEditor instance used for data import.</param>
        public DataImportManager(IDMEEditor dMEEditor)
        {
            DMEEditor = dMEEditor;

        }
        /// <summary>Initializes a new instance of the DataImportManager class.</summary>
        /// <param name="dMEEditor">The IDMEEditor instance used for data import.</param>
        /// <param name="destEntityName">The name of the destination entity.</param>
        /// <param name="destDataSourceName">The name of the destination data source.</param>
        public DataImportManager(IDMEEditor dMEEditor, string destEntityName, string destDataSourceName)
        {
            DMEEditor = dMEEditor;
            LoadMapping();
            if (Mapping == null)
            {
                LoadDestEntityStructure(destEntityName, destDataSourceName);
                Mapping.MappedEntities.Add(MappingManager.AddEntitytoMappedEntities(dMEEditor, SourceEntityStructure, DestEntityStructure));
            }
        }
        /// <summary>Initializes a new instance of the DataImportManager class.</summary>
        /// <param name="dMEEditor">The IDMEEditor instance used for data import.</param>
        /// <param name="destEntityName">The name of the destination entity.</param>
        /// <param name="destDataSourceName">The name of the destination data source.</param>
        /// <param name="srcEntityName">The name of the source entity.</param>
        /// <param name="srcDataSourceName">The name of the source data source.</param>
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
        /// <summary>Loads the structure of the destination entity.</summary>
        /// <param name="destEntityName">The name of the destination entity.</param>
        /// <param name="destDataSourceName">The name of the destination data source.</param>
        /// <returns>An object containing information about any errors that occurred during the loading process.</returns>
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
                        DestDefaults = new ObservableBindingList<DefaultValue>(DMEEditor.Getdefaults(destDataSourceName));
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
        /// <summary>Loads the structure of a source entity from a data source.</summary>
        /// <param name="srcEntityName">The name of the source entity.</param>
        /// <param name="srcDataSourceName">The name of the data source.</param>
        /// <returns>An object containing information about the structure of the source entity.</returns>
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
        /// <summary>Loads the mapping for a destination entity and data source.</summary>
        /// <param name="destEntityName">The name of the destination entity.</param>
        /// <param name="destDataSourceName">The name of the destination data source.</param>
        /// <returns>An object containing information about any errors that occurred during the loading process.</returns>
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
                MappingunitofWork = new UnitofWork<EntityDataMap_DTL>(DMEEditor, true, new ObservableBindingList<EntityDataMap_DTL>(Mapping.MappedEntities), "GuidID");
                MappingunitofWork.PrimaryKey = "GuidID";
                IsEntitychanged = false;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error Loading Mapping File Data {destDataSourceName} -{destEntityName} - {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;

        }
        /// <summary>Loads the mapping information.</summary>
        /// <returns>An object that contains information about any errors that occurred during the loading process.</returns>
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
                    LoadSourceEntityStructure(SourceEntityName, SourceDataSourceName);
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
        /// <summary>Saves the mapping and returns information about any errors that occurred.</summary>
        /// <returns>An object containing information about any errors that occurred during the mapping save process.</returns>
        public IErrorsInfo SaveMapping()
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                Mapping.MappedEntities = MappingunitofWork.Units.ToList();
                DMEEditor.ConfigEditor.SaveMappingValues(DestEntityName, DestDataSourceName, Mapping);

            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error Loading Mapping File Data {DestDataSourceName} -{DestEntityName} - {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;

        }
        /// <summary>Determines if there are any changes between two entity names.</summary>
        /// <param name="sourceentityname">The name of the source entity.</param>
        /// <param name="destentityname">The name of the destination entity.</param>
        /// <returns>True if there are any changes between the two entity names, false otherwise.</returns>
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
        /// <summary>Runs the import process.</summary>
        /// <param name="progress">An object that reports the progress of the import process.</param>
        /// <param name="token">A cancellation token that can be used to cancel the import process.</param>
        /// <returns>An object that contains information about any errors that occurred during the import process.</returns>
        public IErrorsInfo RunImport(IProgress<IPassedArgs> progress, CancellationToken token)
        {
            try
            {

                var ScriptRun = Task.Run(() =>
                {
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
        /// <summary>Stops the execution of the task.</summary>
        public void StopTask()
        {
            DMEEditor.AddLogMessage("Beep", $"Error Running Import Data {DestDataSourceName} -{DestEntityName} Stopped by user", DateTime.Now, 0, null, Errors.Failed);
        }
        /// <summary>Updates the state of the object.</summary>
        public void Update()
        {
            DMEEditor.AddLogMessage("Beep", $"Running Import Data {DestDataSourceName} -{DestEntityName} Finished", DateTime.Now, 0, null, Errors.Ok);
        }
    }
}
