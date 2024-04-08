using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Workflow.Mapping;
using TheTechIdea.Util;
using System.IO;
using DataManagementModels.ConfigUtil;

namespace TheTechIdea.Beep.Editor
{
    /// <summary>
    /// Represents an Extract, Transform, Load (ETL) process.
    /// </summary>
    public class ETL : IETL
    {
        /// <summary>
        /// Initializes a new instance of the ETL class.
        /// </summary>
        /// <param name="_DMEEditor">The DME editor to use for the ETL process.</param>
        public ETL(IDMEEditor _DMEEditor)
        {
            DMEEditor = _DMEEditor;
            RulesEditor = new RulesEditor(DMEEditor);
        }
        /// <summary>
        /// Event that is raised when a process is passed.
        /// </summary>
        /// 
        public event EventHandler<PassedArgs> PassEvent;
        /// <summary>Gets or sets the DMEEditor instance.</summary>
        /// <value>The DMEEditor instance.</value>
        public IDMEEditor DMEEditor { get { return _DMEEditor; } set { _DMEEditor = value; } } //;RulesEditor = new RulesEditor(value);MoveValidator = new EntityDataMoveValidator(DMEEditor);
        /// <summary>Gets or sets the rules editor.</summary>
        /// <value>The rules editor.</value>
        public IRulesEditor RulesEditor { get; set; }
        /// <summary>Gets or sets the PassedArgs object.</summary>
        /// <value>The PassedArgs object.</value>
        public PassedArgs Passedargs { get; set; }
        /// <summary>Gets or sets the count of scripts.</summary>
        /// <value>The count of scripts.</value>
        public int ScriptCount { get; set; }
        /// <summary>Gets or sets the current script record.</summary>
        /// <value>The current script record.</value>
        public int CurrentScriptRecord { get; set; }
        /// <summary>Gets or sets the stop error count.</summary>
        /// <value>The stop error count.</value>
        /// <remarks>
        /// The stop error count determines the maximum number of errors allowed before a process is stopped.
        /// The default value is 10.
        /// </remarks>
        public decimal StopErrorCount { get; set; } = 10;
        /// <summary>Gets or sets the list of loaded data logs.</summary>
        /// <value>The list of loaded data logs.</value>
        public List<LoadDataLogResult> LoadDataLogs { get; set; } = new List<LoadDataLogResult>();
        /// <summary>Gets or sets the ETL script for HDR processing.</summary>
        /// <value>The ETL script for HDR processing.</value>
        public ETLScriptHDR Script { get; set; } = new ETLScriptHDR();

        #region "Local Variables"
        private bool stoprun = false;
        private IDMEEditor _DMEEditor;
        private int errorcount = 0;
        private List<DefaultValue> CurrrentDBDefaults = new List<DefaultValue>();
        private bool disposedValue;
        #endregion
        #region "Create Scripts"
        /// <summary>Creates the header of an ETL script.</summary>
        /// <param name="Srcds">The data source object.</param>
        /// <param name="progress">The progress object to report progress.</param>
        /// <param name="token">The cancellation token to cancel the operation.</param>
        /// <exception cref="ArgumentNullException">Thrown when Srcds is null.</exception>
        public void CreateScriptHeader(IDataSource Srcds, IProgress<PassedArgs> progress, CancellationToken token)
        {
            int i = 0;
            Script = new ETLScriptHDR();
            Script.scriptSource = Srcds.DatasourceName;
            List<EntityStructure> ls = new List<EntityStructure>();
            Srcds.GetEntitesList();
            foreach (string item in Srcds.EntitiesNames)
            {
                ls.Add(Srcds.GetEntityStructure(item, true));
            }
            Script.ScriptDTL = DMEEditor.ETL.GetCreateEntityScript(Srcds, ls, progress, token);
            foreach (var item in ls)
            {

                ETLScriptDet upscript = new ETLScriptDet();
                upscript.sourcedatasourcename = item.DataSourceID;
                upscript.sourceentityname = item.EntityName;
                upscript.sourceDatasourceEntityName = item.EntityName;
                upscript.destinationDatasourceEntityName = item.EntityName;
                upscript.destinationentityname = item.EntityName;
                upscript.destinationdatasourcename = Srcds.DatasourceName;
                upscript.scriptType = DDLScriptType.CopyData;
                Script.ScriptDTL.Add(upscript);
                i += 1;
            }
        }
        /// <summary>Generates a list of ETL script details for creating entities from a data source.</summary>
        /// <param name="ds">The data source to retrieve entities from.</param>
        /// <param name="entities">The list of entities to create scripts for.</param>
        /// <param name="progress">An object to report progress during the script generation.</param>
        /// <param name="token">A cancellation token to cancel the script generation.</param>
        /// <returns>A list of ETL script details for creating entities.</returns>
        /// <remarks>If an error occurs during the process, a log message will be added and an empty list will be returned.</remarks>
        public List<ETLScriptDet> GetCreateEntityScript(IDataSource ds, List<string> entities, IProgress<PassedArgs> progress, CancellationToken token, bool copydata = false)
        {
            List<ETLScriptDet> rt = new List<ETLScriptDet>();

            try
            {
                List<EntityStructure> ls = new List<EntityStructure>();
                foreach (string item in entities)
                {
                    EntityStructure t1 = ds.GetEntityStructure(item, true); ;// t.Result;
                    ls.Add(t1);
                }
                rt.AddRange(GetCreateEntityScript(ds, ls, progress, token,copydata));

            }
            catch (System.Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error in getting entities from Database ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);

            }
            // Script.ScriptDTL.AddRange(rt);
            return rt;
        }
        /// <summary>Generates an ETL script detail object based on the provided parameters.</summary>
        /// <param name="item">The entity structure object representing the source entity.</param>
        /// <param name="destSource">The name of the destination data source.</param>
        /// <param name="scriptType">The type of DDL script.</param>
        /// <returns>An ETLScriptDet object representing the generated script.</returns>
        private ETLScriptDet GenerateScript(EntityStructure item, string destSource, DDLScriptType scriptType)
        {
            ETLScriptDet upscript = new ETLScriptDet();
            upscript.sourcedatasourcename = item.DataSourceID;
            upscript.sourceentityname = item.EntityName;
            upscript.sourceDatasourceEntityName = item.DatasourceEntityName;
            upscript.destinationDatasourceEntityName = item.EntityName;
            upscript.destinationentityname = item.EntityName;
            upscript.destinationdatasourcename = destSource;
            upscript.SourceEntity = item;
            upscript.scriptType = scriptType;
            return upscript;
        }
        /// <summary>Generates a list of ETL script details for creating entities.</summary>
        /// <param name="Dest">The destination data source.</param>
        /// <param name="entities">The list of entity structures.</param>
        /// <param name="progress">An object for reporting progress.</param>
        /// <param name="token">A cancellation token to cancel the operation.</param>
        /// <returns>A list of ETL script details for creating entities.</returns>
        /// <remarks>
        /// This method generates ETL script details for creating entities based on the provided destination data source and entity structures.
        /// It reports progress using the provided progress object and can be cancelled using the cancellation token.
        /// </remarks>
        public List<ETLScriptDet> GetCreateEntityScript(IDataSource Dest, List<EntityStructure> entities, IProgress<PassedArgs> progress, CancellationToken token,bool copydata=false)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            int i = 0;
            List<ETLScriptDet> retval = new List<ETLScriptDet>();

            try
            {
                //rt = Dest.GetCreateEntityScript(entities);
                foreach (EntityStructure item in entities)
                {
                    ETLScriptDet copyscript = GenerateScript(item, Dest.DatasourceName, DDLScriptType.CreateEntity);
                    copyscript.ID = i;
                    copyscript.CopyData = copydata;
                    copyscript.IsCreated = false;
                    copyscript.IsModified = false;
                    copyscript.IsDataCopied = false;
                    copyscript.Failed = false;
                    copyscript.errormessage = "";
                  
                    copyscript.Active = true;
                    copyscript.Mapping = new EntityDataMap_DTL();
                    copyscript.Tracking = new List<SyncErrorsandTracking>();

                    retval.Add(copyscript);
                    i++;
                }

                DMEEditor.AddLogMessage("Success", $"Generated Script", DateTime.Now, 0, null, Errors.Ok);

            }
            catch (Exception ex)
            {
                string errmsg = "Error in Generating Script";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);

            }
            return retval;

        }
        /// <summary>Generates a script for copying data entities.</summary>
        /// <param name="Dest">The destination data source.</param>
        /// <param name="entities">The list of entity structures.</param>
        /// <param name="progress">An object to report progress.</param>
        /// <param name="token">A cancellation token to cancel the operation.</param>
        /// <returns>A list of ETLScriptDet objects representing the generated script.</returns>
        public List<ETLScriptDet> GetCopyDataEntityScript(IDataSource Dest, List<EntityStructure> entities, IProgress<PassedArgs> progress, CancellationToken token)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;

            int i = 0;

            List<ETLScriptDet> retval = new List<ETLScriptDet>();

            try
            {
                // Generate Create Table First
                foreach (EntityStructure sc in entities)
                {
                    ETLScriptDet copyscript = GenerateScript(sc, Dest.DatasourceName, DDLScriptType.CopyData);
                    copyscript.ID = i;
                    i++;
                    //Script.ScriptDTL.Add(copyscript);
                    retval.Add(copyscript);
                }
                i += 1;
                DMEEditor.AddLogMessage("Success", $"Generated Script", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string errmsg = "Error in Generating Script";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);

            }
            return retval;

        }
        #endregion "Create Scripts"
        #region "Copy Data"
        /// <summary>Copies the structure of specified entities from a source data source to a destination data source.</summary>
        /// <param name="sourceds">The source data source.</param>
        /// <param name="destds">The destination data source.</param>
        /// <param name="entities">A list of entity names to copy.</param>
        /// <param name="progress">An object to report progress during the copy operation.</param>
        /// <param name="token">A cancellation token to cancel the copy operation.</param>
        /// <param name="CreateMissingEntity">A flag indicating whether to create missing entities in the destination data source.</param>
        /// <returns>An object containing information about any errors that occurred during the copy operation
        public IErrorsInfo CopyEntitiesStructure(IDataSource sourceds, IDataSource destds, List<string> entities, IProgress<PassedArgs> progress, CancellationToken token, bool CreateMissingEntity = true)
        {
            try
            {
                var ls = from e in sourceds.Entities
                         from r in entities
                         where e.EntityName == r
                         select e;
                string entname = "";
                foreach (EntityStructure item in ls)
                {
                    CopyEntityStructure(sourceds, destds, item.EntityName, item.EntityName, progress, token, CreateMissingEntity);
                }

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", $"Error copying Data ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);

            }
            return DMEEditor.ErrorObject;
        }
        /// <summary>Copies the structure of an entity from a source data source to a destination data source.</summary>
        /// <param name="sourceds">The source data source.</param>
        /// <param name="destds">The destination data source.</param>
        /// <param name="srcentity">The name of the source entity.</param>
        /// <param name="destentity">The name of the destination entity.</param>
        /// <param name="progress">An object to report progress during the copy operation.</param>
        /// <param name="token">A cancellation token to cancel the copy operation.</param>
        /// <param name="CreateMissingEntity">A flag indicating whether to create the destination entity if it doesn't exist
        public IErrorsInfo CopyEntityStructure(IDataSource sourceds, IDataSource destds, string srcentity, string destentity, IProgress<PassedArgs> progress, CancellationToken token, bool CreateMissingEntity = true)
        {
            try
            {
                EntityStructure item = sourceds.GetEntityStructure(srcentity, true);
                if (item != null)
                {
                    if (destds.Category == DatasourceCategory.RDBMS)
                    {
                        IRDBSource rDB = (IRDBSource)destds;
                        rDB.DisableFKConstraints(item);
                    }
                    if (destds.CreateEntityAs(item))
                    {
                        DMEEditor.AddLogMessage("Success", $"Creating Entity  {item.EntityName} on {destds.DatasourceName}", DateTime.Now, 0, null, Errors.Ok);
                    }
                    else
                    {
                        DMEEditor.AddLogMessage("Fail", $"Error : Could not Create  Entity {item.EntityName} on {destds.DatasourceName}", DateTime.Now, 0, null, Errors.Failed);
                    }
                    if (destds.Category == DatasourceCategory.RDBMS)
                    {

                        IRDBSource rDB = (IRDBSource)destds;
                        rDB.EnableFKConstraints(item);
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error Could not Create  Entity {srcentity} on {destds.DatasourceName} ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        /// <summary>Copies data from a source data source to a destination data source.</summary>
        /// <param name="sourceds">The source data source.</param>
        /// <param name="destds">The destination data source.</param>
        /// <param name="progress">An object to report progress during the copy operation.</param>
        /// <param name="token">A cancellation token to cancel the copy operation.</param>
        /// <param name="CreateMissingEntity">A flag indicating whether to create missing entities in the destination data source.</param>
        /// <param name="map_DTL">An optional mapping object to map entity data between the source and destination data sources.</param>
        /// <returns>An object containing information about any errors
        public IErrorsInfo CopyDatasourceData(IDataSource sourceds, IDataSource destds, IProgress<PassedArgs> progress, CancellationToken token, bool CreateMissingEntity = true, EntityDataMap_DTL map_DTL = null)
        {
            try
            {
                foreach (EntityStructure item in sourceds.Entities)
                {
                    CopyEntityData(sourceds, destds, item.EntityName, item.EntityName, progress, token, CreateMissingEntity);
                }

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", $"Error copying Data ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);

            }
            return DMEEditor.ErrorObject;
        }
        /// <summary>Copies data from source data source to destination data source for specified entities.</summary>
        /// <param name="sourceds">The source data source.</param>
        /// <param name="destds">The destination data source.</param>
        /// <param name="entities">The list of entities to copy.</param>
        /// <param name="progress">The progress object to report progress.</param>
        /// <param name="token">The cancellation token to cancel the operation.</param>
        /// <param name="CreateMissingEntity">Flag indicating whether to create missing entities in the destination data source.</param>
        /// <param name="map_DTL">The mapping object for entity data transfer.</param>
        /// <returns>An object containing
        public IErrorsInfo CopyEntitiesData(IDataSource sourceds, IDataSource destds, List<string> entities, IProgress<PassedArgs> progress, CancellationToken token, bool CreateMissingEntity = true, EntityDataMap_DTL map_DTL = null)
        {
            try
            {
                var ls = from e in sourceds.Entities
                         from r in entities
                         where e.EntityName == r
                         select e;

                foreach (EntityStructure item in ls)
                {
                    if ((item.EntityName != item.DatasourceEntityName) && (!string.IsNullOrEmpty(item.DatasourceEntityName)))
                    {
                        CopyEntityData(sourceds, destds, item.DatasourceEntityName, item.EntityName, progress, token, CreateMissingEntity);
                    }
                    else
                        CopyEntityData(sourceds, destds, item.EntityName, item.EntityName, progress, token, CreateMissingEntity);

                }

            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error copying Data ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        /// <summary>Copies entity data from a source data source to a destination data source.</summary>
        /// <param name="sourceds">The source data source.</param>
        /// <param name="destds">The destination data source.</param>
        /// <param name="srcentity">The name of the source entity.</param>
        /// <param name="destentity">The name of the destination entity.</param>
        /// <param name="progress">An object to report progress during the copy operation.</param>
        /// <param name="token">A cancellation token to cancel the copy operation.</param>
        /// <param name="CreateMissingEntity">A flag indicating whether to create the destination entity if it doesn't exist.</param>
        public IErrorsInfo CopyEntityData(IDataSource sourceds, IDataSource destds, string srcentity, string destentity, IProgress<PassedArgs> progress, CancellationToken token, bool CreateMissingEntity = true, EntityDataMap_DTL map_DTL = null)
        {
            try
            {
                errorcount = 0;
                EntityStructure item = sourceds.GetEntityStructure(srcentity, true);
                if (item != null)
                {
                    if (destds.Category == DatasourceCategory.RDBMS)
                    {
                        IRDBSource rDB = (IRDBSource)destds;
                        rDB.DisableFKConstraints(item);
                    }
                    if (destds.CheckEntityExist(destentity))
                    {
                        object srcTb;
                        string entname;
                        var src = Task.Run(() => { return sourceds.GetEntity(item.EntityName, null); });
                        src.Wait();
                        srcTb = src.Result;
                        List<object> srcList = new List<object>();
                        if (src.Result != null)
                        {
                            DMTypeBuilder.CreateNewObject(DMEEditor, item.EntityName, item.EntityName, item.Fields);
                            if (srcTb.GetType().FullName.Contains("DataTable"))
                            {
                                srcList = DMEEditor.Utilfunction.GetListByDataTable((DataTable)srcTb, DMTypeBuilder.myType, item);
                            }
                            if (srcTb.GetType().FullName.Contains("List"))
                            {
                                srcList = (List<object>)srcTb;
                            }

                            if (srcTb.GetType().FullName.Contains("IEnumerable"))
                            {
                                srcList = (List<object>)srcTb;
                            }
                            ScriptCount += srcList.Count();
                            foreach (var r in srcList)
                            {
                                CurrentScriptRecord += 1;
                                // DMEEditor.ErrorObject=destds.InsertEntity(item.EntityName, r);
                                InsertEntity(destds, item, item.EntityName, null, r, progress, token);
                                token.ThrowIfCancellationRequested();
                                if (progress != null)
                                {
                                    PassedArgs ps = new PassedArgs { ParameterInt1 = CurrentScriptRecord, ParameterInt2 = ScriptCount, Messege = DMEEditor.ErrorObject.Message };
                                    progress.Report(ps);

                                }

                            }
                        }
                        if (progress != null)
                        {
                            PassedArgs ps = new PassedArgs { ParameterString1 = $"Ended Copying Data from {srcentity} on {sourceds.DatasourceName} to {srcentity} on {destds.DatasourceName} ", ParameterInt1 = CurrentScriptRecord, ParameterInt2 = ScriptCount };
                            progress.Report(ps);

                        }
                    }
                    else
                    {
                        DMEEditor.AddLogMessage("Copy Data", $"Error Could not Copy Entity Date {srcentity} on {sourceds.DatasourceName} to {srcentity} on {destds.DatasourceName} ", DateTime.Now, 0, null, Errors.Failed);
                    }
                    if (destds.Category == DatasourceCategory.RDBMS)
                    {

                        IRDBSource rDB = (IRDBSource)destds;
                        rDB.EnableFKConstraints(item);
                    }
                }
                else
                    DMEEditor.AddLogMessage("Copy Data", $"Error Could not Find Entity  {srcentity} on {sourceds.DatasourceName} to {srcentity} on {destds.DatasourceName} ", DateTime.Now, 0, null, Errors.Failed);

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", $"Error copying Data {srcentity} on {sourceds.DatasourceName} to {srcentity} on {destds.DatasourceName} ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);

            }
            return DMEEditor.ErrorObject;
        }
        /// <summary>Copies data from source to destination entities based on provided scripts.</summary>
        /// <param name="sourceds">The source data source.</param>
        /// <param name="destds">The destination data source.</param>
        /// <param name="scripts">The list of ETL scripts.</param>
        /// <param name="progress">The progress object to report progress.</param>
        /// <param name="token">The cancellation token to cancel the operation.</param>
        /// <param name="CreateMissingEntity">Flag indicating whether to create missing entities.</param>
        /// <param name="map_DTL">The entity data map for data transformation and mapping.</param>
        /// <returns>An object containing information about any errors that
        public IErrorsInfo CopyEntitiesData(IDataSource sourceds, IDataSource destds, List<ETLScriptDet> scripts, IProgress<PassedArgs> progress, CancellationToken token, bool CreateMissingEntity = true, EntityDataMap_DTL map_DTL = null)
        {
            try
            {
                string srcentityname = "";
                foreach (ETLScriptDet s in scripts.Where(i => i.scriptType == DDLScriptType.CopyData))
                {
                    if ((s.sourceentityname != s.sourceDatasourceEntityName) && (!string.IsNullOrEmpty(s.sourceDatasourceEntityName)))
                    {
                        srcentityname = s.sourceDatasourceEntityName;
                    }
                    else
                        srcentityname = s.sourceentityname;
                    CopyEntityData(sourceds, destds, srcentityname, s.sourceentityname, progress, token, CreateMissingEntity);
                }

            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error copying Data ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        #endregion "Copy Data"
        #region "Run Scripts"
        /// <summary>Runs a child script asynchronously.</summary>
        /// <param name="ParentScript">The parent script.</param>
        /// <param name="srcds">The data source for the source.</param>
        /// <param name="destds">The data source for the destination.</param>
        /// <param name="progress">The progress object to report progress.</param>
        /// <param name="token">The cancellation token to cancel the operation.</param>
        /// <returns>An object containing information about any errors that occurred during the execution of the child script.</returns>
        public async Task<IErrorsInfo> RunChildScriptAsync(ETLScriptDet ParentScript, IDataSource srcds, IDataSource destds, IProgress<PassedArgs> progress, CancellationToken token)
        {

            if (ParentScript.CopyDataScripts.Count > 0)
            {
                for (int i = 0; i < ParentScript.CopyDataScripts.Count; i++)
                {
                    ETLScriptDet sc = ParentScript.CopyDataScripts[i];
                    destds = DMEEditor.GetDataSource(sc.destinationdatasourcename);
                    srcds = DMEEditor.GetDataSource(sc.sourcedatasourcename);
                    if (destds != null && srcds != null)
                    {
                        DMEEditor.OpenDataSource(sc.destinationdatasourcename);
                        DMEEditor.OpenDataSource(sc.sourcedatasourcename);
                        if (destds.ConnectionStatus == System.Data.ConnectionState.Open)
                        {
                            if (sc.scriptType == DDLScriptType.CopyData)
                            {
                                SendMessege(progress, token, null, sc, $"Started Coping Data for Entity  {sc.destinationentityname}  in {sc.destinationdatasourcename}");

                                await Task.Run(() =>
                                {
                                    DMEEditor.ErrorObject = RunCopyEntityScript(sc, srcds, destds, sc.sourceDatasourceEntityName, sc.destinationentityname, progress, token, true);  //t1.Result;//DMEEditor.ETL.CopyEntityData(srcds, destds, ScriptHeader.Scripts[i], true);

                                });
                                SendMessege(progress, token, null, sc, $"Error in Coping Data for Entity  {sc.destinationentityname}"); ;
                            }
                        }
                        else
                        {
                            DMEEditor.ErrorObject.Flag = Errors.Failed;
                            DMEEditor.ErrorObject.Message = $" Could not Connect to on the Data Dource  {sc.sourcedatasourcename}";
                            errorcount = (int)StopErrorCount;
                            SendMessege(progress, token, null, sc);
                        }
                    }
                }
            }
            return DMEEditor.ErrorObject;
        }
        /// <summary>Runs a create script and updates data.</summary>
        /// <param name="progress">An object that reports the progress of the operation.</param>
        /// <param name="token">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>An object containing information about any errors that occurred during the operation.</returns>
        /// <remarks>
        /// This method runs a create script and updates data. It connects to the specified data sources, performs the necessary operations, and reports progress using the provided progress object. If the operation is cancelled using the provided cancellation token, the method will stop and return the current error information.
        /// </remarks>
        public async Task<IErrorsInfo> RunCreateScript(IProgress<PassedArgs> progress, CancellationToken token, bool copydata = true, bool useEntityStructure = true)
        {
            #region "Update Data code "



            int numberToCompute = 0;

            IDataSource destds = null;
            IDataSource srcds = null;
            LoadDataLogs = new List<LoadDataLogResult>();
            numberToCompute = DMEEditor.ETL.Script.ScriptDTL.Count();
            List<ETLScriptDet> crls = DMEEditor.ETL.Script.ScriptDTL.Where(i => i.scriptType == DDLScriptType.CreateEntity).ToList();
            List<ETLScriptDet> copudatals = DMEEditor.ETL.Script.ScriptDTL.Where(i => i.scriptType == DDLScriptType.CopyData).ToList();
            List<ETLScriptDet> AlterForls = DMEEditor.ETL.Script.ScriptDTL.Where(i => i.scriptType == DDLScriptType.AlterFor).ToList();
            // Run Scripts-----------------

            numberToCompute = DMEEditor.ETL.Script.ScriptDTL.Count;
            int p1 = DMEEditor.ETL.Script.ScriptDTL.Where(u => u.scriptType == DDLScriptType.CreateEntity).Count();
            ScriptCount = p1;
            CurrentScriptRecord = 0;
            errorcount = 0;
            stoprun = false;
            bool CreateSuccess;
            EntityStructure entitystr;
            foreach (ETLScriptDet sc in DMEEditor.ETL.Script.ScriptDTL.OrderBy(p => p.ID))
            {
                CreateSuccess = true;
                destds = DMEEditor.GetDataSource(sc.destinationdatasourcename);
                srcds = DMEEditor.GetDataSource(sc.sourcedatasourcename);
                CurrentScriptRecord += 1;
                if (errorcount == StopErrorCount)
                {
                    return DMEEditor.ErrorObject;
                }
                if (destds != null)
                {
                    DMEEditor.OpenDataSource(sc.destinationdatasourcename);
                    if (stoprun == false)
                    {
                        if (destds.ConnectionStatus == System.Data.ConnectionState.Open)
                        {
                            switch (sc.scriptType)
                            {
                                case DDLScriptType.CopyEntities:
                                    break;
                                case DDLScriptType.SyncEntity:
                                    break;
                                case DDLScriptType.CompareEntity:
                                    break;
                                case DDLScriptType.CreateEntity:
                                    if (sc.scriptType == DDLScriptType.CreateEntity)
                                    {
                                        if (!useEntityStructure || sc.SourceEntity == null)
                                        {
                                            entitystr = (EntityStructure)srcds.GetEntityStructure(sc.sourceDatasourceEntityName, false).Clone();
                                        }
                                        else
                                        {
                                            entitystr=sc.SourceEntity;
                                        }
                                        
                                        if (sc.sourceDatasourceEntityName != sc.destinationentityname)
                                        {
                                            entitystr.EntityName = sc.destinationentityname;
                                            entitystr.DatasourceEntityName = sc.destinationentityname;
                                            entitystr.OriginalEntityName = sc.destinationentityname;
                                        }
                                       
                                        SendMessege(progress, token, entitystr, sc, $"Creating Entity  {entitystr.EntityName} ");
                                        bool retval = destds.CreateEntityAs(entitystr); // t.Result;
                                        if (retval)
                                        {
                                            SendMessege(progress, token, entitystr, sc, $"Successfully Created Entity  {entitystr.EntityName} ");
                                            sc.Active = true;
                                            sc.IsCreated = true;
                                            sc.Active = true;
                                            if (sc.CopyDataScripts.Count > 0 && sc.CopyData && sc.IsCreated)
                                            {
                                                SendMessege(progress, token, entitystr, sc, $"Started  Coping Data From {entitystr.EntityName} ");
                                                var t=await RunChildScriptAsync(sc, srcds, destds, progress, token);
                                                CreateSuccess = true;
                                            }
                                        }
                                        else
                                        {
                                            DMEEditor.ErrorObject.Flag = Errors.Failed;
                                            DMEEditor.ErrorObject.Message = $"Failed in Creating Entity   {entitystr.EntityName} ";
                                            SendMessege(progress, token, entitystr, sc, $"Failed in Creating Entity   {entitystr.EntityName} ");
                                            sc.Active = false;
                                            sc.Failed = true;
                                            CreateSuccess= false;
                                        }
                                    }
                                    break;
                                case DDLScriptType.AlterPrimaryKey:
                                    break;
                                case DDLScriptType.AlterFor:
                                    break;
                                case DDLScriptType.AlterUni:
                                    break;
                                case DDLScriptType.DropTable:
                                    break;
                                case DDLScriptType.EnableCons:
                                    break;
                                case DDLScriptType.DisableCons:
                                    break;
                                case DDLScriptType.CopyData:
                                    if (sc.scriptType == DDLScriptType.CopyData)
                                    {
                                        if(CreateSuccess==false)
                                        {
                                            SendMessege(progress, token, null, sc, $"Cannot Copy Data for Failed  Entity   {sc.destinationentityname} ");
                                            break;
                                        }
                                        SendMessege(progress, token, null, sc, $"Started Coping Data for Entity  {sc.destinationentityname}  in {sc.destinationdatasourcename}");

                                        await Task.Run(() =>
                                        {
                                            DMEEditor.ErrorObject = RunCopyEntityScript(sc, srcds, destds, sc.sourceDatasourceEntityName, sc.destinationentityname, progress, token, true);  //t1.Result;//DMEEditor.ETL.CopyEntityData(srcds, destds, ScriptHeader.Scripts[i], true);

                                        });
                                        SendMessege(progress, token, null, sc, $"Finished in Coping Data for Entity  {sc.destinationentityname}"); ;
                                    }
                                    break;
                                default:
                                    break;
                            }

                        }
                        else
                        {
                            DMEEditor.ErrorObject.Flag = Errors.Failed;
                            DMEEditor.ErrorObject.Message = $" Could not Connect to on the Data Dources {sc.destinationdatasourcename} or {sc.sourcedatasourcename}";
                            SendMessege(progress, token, null, sc);
                        }
                    }
                }
            }
            // SaveETL(destds.DatasourceName);
            #endregion
            return DMEEditor.ErrorObject;
        }
        /// <summary>Runs a script to copy an entity from a source data source to a destination data source.</summary>
        /// <param name="sc">The ETL script details.</param>
        /// <param name="sourceds">The source data source.</param>
        /// <param name="destds">The destination data source.</param>
        /// <param name="srcentity">The name of the source entity.</param>
        /// <param name="destentity">The name of the destination entity.</param>
        /// <param name="progress">An object to report progress.</param>
        /// <param name="token">A cancellation token to cancel the operation.</param>
        /// <param name="CreateMissingEntity">A flag indicating
        private IErrorsInfo RunCopyEntityScript(ETLScriptDet sc, IDataSource sourceds, IDataSource destds, string srcentity, string destentity, IProgress<PassedArgs> progress, CancellationToken token, bool CreateMissingEntity = true, EntityDataMap_DTL map_DTL = null)
        {
            try
            {
                errorcount = 0;
                EntityStructure srcentitystructure = sourceds.GetEntityStructure(srcentity, true);
                EntityStructure destEntitystructure = destds.GetEntityStructure(destentity, true);
                if (srcentitystructure != null)
                {
                    if (destds.Category == DatasourceCategory.RDBMS)
                    {
                        IRDBSource rDB = (IRDBSource)destds;
                        rDB.DisableFKConstraints(destEntitystructure);
                    }
                   
                        object srcTb;
                        string querystring = null;
                        List<AppFilter> filters = null;
                        List<EntityField> SelectedFields = null;
                        List<EntityField> SourceFields = null;
                        if (map_DTL != null)
                        {
                            SelectedFields = map_DTL.SelectedDestFields;
                            SourceFields = map_DTL.EntityFields;
                            //querystring = "Select ";
                            //foreach (Mapping_rep_fields mp in map_DTL.FieldMapping)
                            //{
                            //    querystring += mp.FromFieldName + " ,";
                            //}
                            //querystring = querystring.Remove(querystring.Length - 1);
                            //querystring += $" from {map_DTL.EntityName} ";
                            querystring = srcentitystructure.EntityName;
                        }
                        else
                        {
                            querystring = srcentitystructure.EntityName;
                            filters = null;
                            SelectedFields = srcentitystructure.Fields;
                            SourceFields = srcentitystructure.Fields;
                        }
                        SendMessege(progress, token, null, sc, $"Getting Data for  {srcentity}"); ;
                        var src = Task.Run(() => { return sourceds.GetEntity(querystring, filters); });
                        src.Wait();
                        SendMessege(progress, token, null, sc, $"Finish Getting Data for  {srcentity}"); ;

                        srcTb = src.Result;

                        List<object> srcList = new List<object>();
                        if (src.Result != null)
                        {
                            DMTypeBuilder.CreateNewObject(DMEEditor, destEntitystructure.EntityName, srcentitystructure.EntityName, SourceFields);
                            if (srcTb.GetType().FullName.Contains("DataTable"))
                            {
                                srcList = DMEEditor.Utilfunction.GetListByDataTable((DataTable)srcTb, DMTypeBuilder.myType, srcentitystructure);
                            }
                            if (srcTb.GetType().FullName.Contains("List"))
                            {
                                srcList = (List<object>)srcTb;
                            }

                            if (srcTb.GetType().FullName.Contains("IEnumerable"))
                            {
                                srcList = (List<object>)srcTb;
                            }
                            ScriptCount += srcList.Count();
                            SendMessege(progress, token, null, sc, $"Data fetched {ScriptCount} Record"); ;
                            int i=0;
                            foreach (var r in srcList)
                            {
                                i++;
                                DMEEditor.ErrorObject = InsertEntity(destds, destEntitystructure, destentity, map_DTL, r, progress, token); ;
                               // SendMessege(progress, token, null, sc, $"Data Inserted for {destEntitystructure.EntityName} Record {i}"); ;
                                token.ThrowIfCancellationRequested();

                            }
                        }

                   
                
                    if (destds.Category == DatasourceCategory.RDBMS)
                    {
                        IRDBSource rDB = (IRDBSource)destds;
                        rDB.EnableFKConstraints(srcentitystructure);
                    }
                }
                else
                {
                    DMEEditor.AddLogMessage("Copy Data", $"Error Could not Find Entity  {srcentity} on {sourceds.DatasourceName} to {srcentity} on {destds.DatasourceName} ", DateTime.Now, 0, null, Errors.Failed);
                    errorcount = (int)StopErrorCount;
                    SendMessege(progress, token, null, sc, $"Error Could not Find Entity  {srcentity} on {sourceds.DatasourceName} to {srcentity} on {destds.DatasourceName} "); ;
                }

            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error copying Data {srcentity} on {sourceds.DatasourceName} to {srcentity} on {destds.DatasourceName} ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        /// <summary>Loads an ETL (Extract, Transform, Load) script from a specified data source.</summary>
        /// <param name="DatasourceName">The name of the data source.</param>
        /// <returns>An object containing information about any errors that occurred during the loading process.</returns>
        /// <remarks>
        /// This method loads an ETL script from the specified data source. It first creates a directory for the script if it doesn't already exist.
        /// Then, it constructs the file path for the script and checks if the file exists. If the file exists, it deserializes the script object from the file.
        /// If any errors occur during the loading process, a log message is added to the DMEEditor and the
        public IErrorsInfo LoadETL(string DatasourceName)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                string dbpath = DMEEditor.ConfigEditor.ExePath + "\\Scripts\\" + DatasourceName;
                Directory.CreateDirectory(dbpath);
                string filepath = Path.Combine(dbpath, "createscripts.json");
                //string InMemoryStructuresfilepath = Path.Combine(dbpath, "InMemoryStructures.json");

                if (File.Exists(filepath))
                {
                    Script = DMEEditor.ConfigEditor.JsonLoader.DeserializeSingleObject<ETLScriptHDR>(filepath);
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Could not Load InMemory Structure for {DatasourceName}- {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        /// <summary>Saves the ETL (Extract, Transform, Load) configuration for a given datasource.</summary>
        /// <param name="DatasourceName">The name of the datasource.</param>
        /// <returns>An object containing information about any errors that occurred during the save operation.</returns>
        /// <remarks>
        /// This method creates a directory for the specified datasource if it doesn't already exist.
        /// It then saves the ETL configuration as a JSON file in the created directory.
        /// If any errors occur during the save operation, a log message is added and the error object is returned.
        /// </remarks>
        public IErrorsInfo SaveETL(string DatasourceName)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                string dbpath = DMEEditor.ConfigEditor.ExePath + "\\Scripts\\" + DatasourceName;
                Directory.CreateDirectory(dbpath);
                string filepath = Path.Combine(dbpath, "createscripts.json");
                string InMemoryStructuresfilepath = Path.Combine(dbpath, "InMemoryStructures.json");
                DMEEditor.ConfigEditor.JsonLoader.Serialize(filepath, Script);
                //DMEEditor.ConfigEditor.JsonLoader.Serialize(InMemoryStructuresfilepath, InMemoryStructures);


            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Could not save InMemory Structure for {DatasourceName}- {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        #endregion "Run Scripts"
        #region "Import Methods"
        /// <summary>Creates an import script based on the provided entity mappings.</summary>
        /// <param name="mapping">The main entity data map.</param>
        /// <param name="SelectedMapping">The selected entity data map.</param>
        /// <returns>An object containing information about any errors that occurred during the script creation.</returns>
        /// <remarks>
        /// This method generates an import script by populating the necessary properties of the ETLScriptHDR and ETLScriptDet objects.
        /// It sets the script source to the entity data source of the selected mapping, initializes error and script count variables,
        /// clears the load data logs, and adds a new ETLScriptDet object to the ScriptDTL list with the necessary properties.
        /// If any
        public IErrorsInfo CreateImportScript(EntityDataMap mapping, EntityDataMap_DTL SelectedMapping)
        {
            try
            {
                Script = new ETLScriptHDR();
                Script.scriptSource = SelectedMapping.EntityDataSource;
                errorcount = 0;
                ScriptCount = 0;
                LoadDataLogs.Clear();
                Script.ScriptDTL.Add(new ETLScriptDet() { Active = true, destinationdatasourcename = mapping.EntityDataSource, destinationDatasourceEntityName = mapping.EntityName, destinationentityname = mapping.EntityName, scriptType = DDLScriptType.CopyData, Mapping = SelectedMapping, sourcedatasourcename = SelectedMapping.EntityDataSource, sourceDatasourceEntityName = SelectedMapping.EntityName, sourceentityname = SelectedMapping.EntityName });
                DMEEditor.AddLogMessage("OK", $"Generated Copy Data script", DateTime.Now, -1, "CopyDatabase", Errors.Ok);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error Generating Copy Data script ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        /// <summary>Runs an import script and returns information about any errors that occurred.</summary>
        /// <param name="progress">An object that reports the progress of the import script.</param>
        /// <param name="token">A cancellation token that can be used to cancel the import script.</param>
        /// <returns>An object containing information about any errors that occurred during the import script.</returns>
        public async Task<IErrorsInfo> RunImportScript(IProgress<PassedArgs> progress, CancellationToken token,bool useEntityStructure = true)
        {
            IDataSource destds = null;
            IDataSource srcds = null;
            ScriptCount = 1;
            CurrentScriptRecord = 0;
            errorcount = 0;
            stoprun = false;
            EntityStructure entitystr;
            CurrentScriptRecord += 1;
            LoadDataLogs = new List<LoadDataLogResult>();
            ETLScriptDet sc = DMEEditor.ETL.Script.ScriptDTL.First();
            if (sc != null)
            {
                destds = DMEEditor.GetDataSource(sc.destinationdatasourcename);
                srcds = DMEEditor.GetDataSource(sc.sourcedatasourcename);
                if (errorcount == StopErrorCount)
                {
                    return DMEEditor.ErrorObject;
                }
                if (destds != null)
                {

                    DMEEditor.OpenDataSource(sc.destinationdatasourcename);
                    if (stoprun == false)
                    {
                        if (destds.ConnectionStatus == System.Data.ConnectionState.Open)
                        {
                            if (sc.scriptType == DDLScriptType.CopyData)
                            {
                                CurrrentDBDefaults = DMEEditor.ConfigEditor.DataConnections[DMEEditor.ConfigEditor.DataConnections.FindIndex(i => i.ConnectionName == destds.DatasourceName)].DatasourceDefaults;
                                if (!useEntityStructure || sc.SourceEntity == null)
                                {
                                    entitystr = (EntityStructure)srcds.GetEntityStructure(sc.sourceDatasourceEntityName, false).Clone();
                                }
                                else
                                {
                                    entitystr = sc.SourceEntity;
                                }

                              

                                sc.errormessage = DMEEditor.ErrorObject.Message;
                               
                                sc.Active = false;
                                SendMessege(progress, token, null, sc, "Starting Import Entities Script");

                                if (errorcount == StopErrorCount)
                                {
                                    return DMEEditor.ErrorObject;
                                }
                                var src = await Task.Run(() => { return RunCopyEntityScript(sc, srcds, destds, sc.sourceentityname, sc.destinationentityname, progress, token, false, sc.Mapping); });
                            }
                        }
                        else
                        {
                            DMEEditor.ErrorObject.Flag = Errors.Failed;
                            DMEEditor.ErrorObject.Message = $" Could not Connect to on the Data Dources {sc.destinationdatasourcename} or {sc.sourcedatasourcename}";
                            SendMessege(progress, token);
                        }

                    }
                }
            }
            return DMEEditor.ErrorObject;

        }
        #endregion
        /// <summary>Inserts an entity into a destination data source.</summary>
        /// <param name="destds">The destination data source.</param>
        /// <param name="destEntitystructure">The structure of the destination entity.</param>
        /// <param name="destentity">The name of the destination entity.</param>
        /// <param name="map_DTL">The mapping details for the entity.</param>
        /// <param name="r">The object representing the entity to be inserted.</param>
        /// <param name="progress">An object to report progress during the insertion process.</param>
        /// <param name="token">A cancellation token to cancel the insertion process.</param>
        /// <returns>An object containing information about any errors
        public IErrorsInfo InsertEntity(IDataSource destds, EntityStructure destEntitystructure, string destentity, EntityDataMap_DTL map_DTL, object r, IProgress<PassedArgs> progress, CancellationToken token)
        {
            object retval = r;
            try
            {
                if (map_DTL != null)
                {
                    retval = DMEEditor.Utilfunction.MapObjectToAnother(DMEEditor, destentity, map_DTL, r);
                }
                if (CurrrentDBDefaults.Count > 0)
                {
                    foreach (DefaultValue _defaultValue in CurrrentDBDefaults.Where(p => p.propertyType == DefaultValueType.Rule))
                    {
                        if (destEntitystructure.Fields.Any(p => p.fieldname.Equals(_defaultValue.propertyName, StringComparison.InvariantCultureIgnoreCase)))
                        {
                            string fieldname = _defaultValue.propertyName;
                            DMEEditor.Passedarguments.DatasourceName = destds.DatasourceName;
                            DMEEditor.Passedarguments.CurrentEntity = destentity;
                            ObjectItem ob = DMEEditor.Passedarguments.Objects.Find(p => p.Name == destentity);
                            if (ob != null)
                            {
                                DMEEditor.Passedarguments.Objects.Remove(ob);
                            }
                            DMEEditor.Passedarguments.Objects.Add(new ObjectItem() { Name = destentity, obj = retval });
                            DMEEditor.Passedarguments.ParameterString1 = $":{_defaultValue.Rule}.{fieldname}.{_defaultValue.propoertValue}";
                            var value = RulesEditor.SolveRule(DMEEditor.Passedarguments);
                            if (value != null)
                            {
                                DMEEditor.Utilfunction.SetFieldValueFromObject(fieldname, retval, value);
                            }
                        }
                    }
                }
                if (destEntitystructure.Relations.Any())
                {
                    foreach (RelationShipKeys item in destEntitystructure.Relations) // .Where(p => !p.RelatedEntityID.Equals(destEntitystructure.EntityName, StringComparison.InvariantCultureIgnoreCase)
                    {
                        //if (destEntitystructure.Fields.Any(p => p.fieldname.Equals(item.EntityColumnID, StringComparison.InvariantCultureIgnoreCase)))
                        //{
                        if (!string.IsNullOrEmpty(item.RelatedEntityID))
                        {
                            EntityStructure refentity = (EntityStructure)destds.GetEntityStructure(item.RelatedEntityID, true).Clone();
                            if (DMEEditor.Utilfunction.GetFieldValueFromObject(item.EntityColumnID, retval) != null)
                            {
                                if (EntityDataMoveValidator.TrueifParentExist(DMEEditor, destds, destEntitystructure, retval, item.EntityColumnID, DMEEditor.Utilfunction.GetFieldValueFromObject(item.EntityColumnID, retval)) == EntityValidatorMesseges.MissingRefernceValue)
                                {
                                    LoadDataLogs.Add(new LoadDataLogResult() { InputLine = $"Inserting Parent for  Record {CurrentScriptRecord}  in {item.RelatedEntityID}" });
                                    //---- insert Parent Key ----
                                    object parentob = DMEEditor.Utilfunction.GetEntityObject(DMEEditor, item.RelatedEntityID, refentity.Fields);
                                    if (parentob != null)
                                    {
                                        var refval = DMEEditor.Utilfunction.GetFieldValueFromObject(item.EntityColumnID, retval);
                                        DMEEditor.Utilfunction.SetFieldValueFromObject(item.RelatedEntityColumnID, parentob, refval);
                                        //DMEEditor.ErrorObject = destds.InsertEntity(item.ParentEntityID, parentob);
                                        DMEEditor.ErrorObject = InsertEntity(destds, refentity, item.RelatedEntityID, null, parentob, progress, token); ;
                                        token.ThrowIfCancellationRequested();
                                        SendMessege(progress, token, refentity);
                                    }
                                }
                                //};
                            }

                        }
                    }
                }
                CurrentScriptRecord += 1;
              //  SendMessege(progress, token, destEntitystructure, null, $"Inserting Record {CurrentScriptRecord} ");
                DMEEditor.ErrorObject = destds.InsertEntity(destEntitystructure.EntityName, retval);
                token.ThrowIfCancellationRequested();

            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("ETL", $"Failed to Insert Entity {destEntitystructure.EntityName} :{ex.Message}", DateTime.Now, CurrentScriptRecord, ex.Message, Errors.Failed);

            }


            return DMEEditor.ErrorObject;
        }
        /// <summary>Sends a message and updates progress based on the result.</summary>
        /// <param name="progress">An object that reports progress updates.</param>
        /// <param name="token">A cancellation token that can be used to cancel the operation.</param>
        /// <param name="refentity">An optional reference to an entity structure.</param>
        /// <param name="sc">An optional ETL script detail.</param>
        /// <param name="messege">An optional message to send.</param>
        /// <remarks>
        /// If the error flag is set to "Failed" in the DMEEditor.ErrorObject, a SyncErrorsandTracking object is created and the error count is incremented.
        /// If the error flag is not
        private void SendMessege(IProgress<PassedArgs> progress, CancellationToken token, EntityStructure refentity = null, ETLScriptDet sc = null, string messege = null)
        {
            if (DMEEditor.ErrorObject.Flag == Errors.Failed)
            {

                SyncErrorsandTracking tr = new SyncErrorsandTracking();
                errorcount++;
                tr.errormessage = DMEEditor.ErrorObject.Message;
                
                tr.rundate = DateTime.Now;
                tr.sourceEntityName = refentity == null ? null : refentity.EntityName;
                tr.currenrecordindex = CurrentScriptRecord;
                tr.sourceDataSourceName = refentity == null ? null : refentity.DataSourceID;
                if (sc != null)
                {
                    tr.parentscriptid = sc.ID;
                    sc.Tracking.Add(tr);
                }

                LoadDataLogs.Add(new LoadDataLogResult() { InputLine = $"Failed   {CurrentScriptRecord} -{messege} : {tr.errormessage}" });
                if (progress != null)
                {
                    PassedArgs ps = new PassedArgs { EventType = "Update", ParameterInt1 = CurrentScriptRecord, ParameterInt2 = ScriptCount, Messege = DMEEditor.ErrorObject.Message };
                    progress.Report(ps);

                }
                if (errorcount > StopErrorCount)
                {
                    stoprun = true;
                    PassedArgs ps = new PassedArgs { EventType = "Stop", ParameterInt1 = CurrentScriptRecord, ParameterInt2 = ScriptCount, Messege = DMEEditor.ErrorObject.Message };
                    progress.Report(ps);

                }
            }
            else
            {
                LoadDataLogs.Add(new LoadDataLogResult() { InputLine = $"{messege} " });
                if (progress != null)
                {
                    PassedArgs ps = new PassedArgs { EventType = "Update", ParameterInt1 = CurrentScriptRecord, ParameterInt2 = ScriptCount, Messege = DMEEditor.ErrorObject.Message };
                    progress.Report(ps);
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    LoadDataLogs = null;
                    Script = null;
                    CurrrentDBDefaults = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ETL()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

}
