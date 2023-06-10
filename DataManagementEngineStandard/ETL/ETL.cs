using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Workflow.Mapping;
using TheTechIdea.Util;
using System.IO;

namespace TheTechIdea.Beep.Editor
{

    public class ETL : IETL
    {
        
        public ETL(IDMEEditor _DMEEditor)
        {
           DMEEditor= _DMEEditor;
            RulesEditor = new RulesEditor(DMEEditor);
        }
        public event EventHandler<PassedArgs> PassEvent;
        private IDMEEditor _DMEEditor;
        public IDMEEditor DMEEditor { get { return _DMEEditor; } set { _DMEEditor = value;MoveValidator = new EntityDataMoveValidator(value); } } //;RulesEditor = new RulesEditor(value);MoveValidator = new EntityDataMoveValidator(DMEEditor);
        public IRulesEditor RulesEditor { get; set; }
        public EntityDataMoveValidator MoveValidator { get; set; } 
        public PassedArgs Passedargs { get; set; }
        public int ScriptCount { get; set; }
        public int CurrentScriptRecord { get; set; }
        public decimal StopErrorCount { get; set; } = 10;
        public List<LoadDataLogResult> LoadDataLogs { get; set; } = new List<LoadDataLogResult>();
        public ETLScriptHDR Script { get; set; } = new ETLScriptHDR();
        private List<DefaultValue> CurrrentDBDefaults = new List<DefaultValue>();
        #region "Local Variables"
        private bool stoprun = false;
        private int errorcount = 0;
        #endregion
        #region "Create Scripts"
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
            Script.ScriptDTL= DMEEditor.ETL.GetCreateEntityScript(Srcds, ls, progress, token);
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
        public List<ETLScriptDet> GetCreateEntityScript(IDataSource ds, List<string> entities, IProgress<PassedArgs> progress, CancellationToken token)
        {
            List<ETLScriptDet> rt = new List<ETLScriptDet>();
           
            try
            {  
                //List<EntityStructure> ls = new List<EntityStructure>();
                //foreach (string item in entities)
                //{
                //    EntityStructure t1 = ds.GetEntityStructure(item, true); ;// t.Result;
                //    ls.Add(t1);
                //}
                rt.AddRange(GetCreateEntityScript(ds, entities, progress, token));
                
            }
            catch (System.Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error in getting entities from Database ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);

            }
           // Script.ScriptDTL.AddRange(rt);
            return rt;
        }
        private ETLScriptDet GenerateScript( EntityStructure item, string destSource, DDLScriptType scriptType)
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
        public List<ETLScriptDet> GetCreateEntityScript(IDataSource Dest, List<EntityStructure> entities, IProgress<PassedArgs> progress, CancellationToken token)
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
                            ETLScriptDet copyscript = GenerateScript( sc, Dest.DatasourceName,  DDLScriptType.CopyData);
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
                            DMTypeBuilder.CreateNewObject(item.EntityName, item.EntityName, item.Fields);
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
                                    sc.errorsInfo = RunCopyEntityScript(sc, srcds, destds, sc.sourceDatasourceEntityName, sc.destinationentityname, progress, token, true);  //t1.Result;//DMEEditor.ETL.CopyEntityData(srcds, destds, ScriptHeader.Scripts[i], true);

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
        public async Task<IErrorsInfo> RunCreateScript(IProgress<PassedArgs> progress, CancellationToken token)
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
            foreach (ETLScriptDet sc in DMEEditor.ETL.Script.ScriptDTL.OrderBy(p=>p.ID))
            {
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
                                        EntityStructure entitystr = (EntityStructure)srcds.GetEntityStructure(sc.sourceDatasourceEntityName, false).Clone();
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
                                            if (sc.CopyDataScripts.Count > 0)
                                            {
                                                SendMessege(progress, token, entitystr, sc, $"Started  Coping Data From {entitystr.EntityName} ");
                                                await RunChildScriptAsync(sc, srcds, destds, progress, token);
                                            }
                                        }
                                        else
                                        {
                                            DMEEditor.ErrorObject.Flag = Errors.Failed;
                                            DMEEditor.ErrorObject.Message = $"Failed in Creating Entity   {entitystr.EntityName} ";
                                            SendMessege(progress, token, entitystr, sc, $"Failed in Creating Entity   {entitystr.EntityName} ");
                                            sc.Active = false;
                                            sc.Failed = true;
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
                                        SendMessege(progress, token, null, sc, $"Started Coping Data for Entity  {sc.destinationentityname}  in {sc.destinationdatasourcename}");

                                        await Task.Run(() =>
                                        {
                                            sc.errorsInfo = RunCopyEntityScript(sc, srcds, destds, sc.sourceDatasourceEntityName, sc.destinationentityname, progress, token, true);  //t1.Result;//DMEEditor.ETL.CopyEntityData(srcds, destds, ScriptHeader.Scripts[i], true);

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
                    if (destds.CheckEntityExist(destentity))
                    {
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
                            DMTypeBuilder.CreateNewObject(destEntitystructure.EntityName, srcentitystructure.EntityName, SourceFields);
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

                            foreach (var r in srcList)
                            {

                                DMEEditor.ErrorObject = InsertEntity(destds, destEntitystructure, destentity, map_DTL, r, progress, token); ;
                                token.ThrowIfCancellationRequested();

                            }
                        }

                    }
                    else
                    {
                        DMEEditor.AddLogMessage("Copy Data", $"Error Could not Copy Entity Date {srcentity} on {sourceds.DatasourceName} to {srcentity} on {destds.DatasourceName} ", DateTime.Now, 0, null, Errors.Failed);
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
        public IErrorsInfo SaveETL(string DatasourceName)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                string   dbpath = DMEEditor.ConfigEditor.ExePath + "\\Scripts\\" + DatasourceName;
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
        public IErrorsInfo CreateImportScript(EntityDataMap mapping, EntityDataMap_DTL SelectedMapping)
        {
            try
            {
                Script = new ETLScriptHDR();
                Script.scriptSource = SelectedMapping.EntityDataSource;
                errorcount = 0;
                ScriptCount = 0;
                LoadDataLogs.Clear();
                Script.ScriptDTL.Add(new ETLScriptDet() { Active = true, destinationdatasourcename = mapping.EntityDataSource, destinationDatasourceEntityName = mapping.EntityName, destinationentityname = mapping.EntityName, scriptType = DDLScriptType.CopyData, Mapping = SelectedMapping, sourcedatasourcename = SelectedMapping.EntityDataSource,sourceDatasourceEntityName= SelectedMapping.EntityName, sourceentityname = SelectedMapping.EntityName });
                DMEEditor.AddLogMessage("OK", $"Generated Copy Data script", DateTime.Now, -1, "CopyDatabase", Errors.Ok);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error Generating Copy Data script ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        public async Task<IErrorsInfo> RunImportScript(IProgress<PassedArgs> progress, CancellationToken token)
        {
            IDataSource destds = null;
            IDataSource srcds = null;
            ScriptCount = 1;
            CurrentScriptRecord = 0;
            errorcount = 0;
            stoprun = false;
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

                                EntityStructure entitystr = (EntityStructure)srcds.GetEntityStructure(sc.sourceDatasourceEntityName, false).Clone();

                                sc.errormessage = DMEEditor.ErrorObject.Message;
                                sc.errorsInfo = DMEEditor.ErrorObject;
                                sc.Active = false;
                                SendMessege(progress, token,null,sc,"Starting Import Entities Script");
                                
                                if (errorcount == StopErrorCount)
                                {
                                    return DMEEditor.ErrorObject;
                                }
                                var src = await Task.Run(() => { return RunCopyEntityScript( sc, srcds, destds, sc.sourceentityname, sc.destinationentityname, progress, token, false, sc.Mapping); });
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
        public IErrorsInfo InsertEntity(IDataSource destds,  EntityStructure destEntitystructure,string destentity,EntityDataMap_DTL map_DTL,object r, IProgress<PassedArgs> progress, CancellationToken token)
        {
            object retval = r;
            try
            {
                if (map_DTL != null)
                {
                    retval = DMEEditor.Utilfunction.MapObjectToAnother(destentity, map_DTL, r);
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
                                if (MoveValidator.TrueifParentExist(destEntitystructure.EntityName, destEntitystructure.DataSourceID, retval, item.EntityColumnID, DMEEditor.Utilfunction.GetFieldValueFromObject(item.EntityColumnID, retval)) == EntityValidatorMesseges.MissingRefernceValue)
                                {
                                    LoadDataLogs.Add(new LoadDataLogResult() { InputLine = $"Inserting Parent for  Record {CurrentScriptRecord}  in {item.RelatedEntityID}" });
                                    //---- insert Parent Key ----
                                    object parentob = DMEEditor.Utilfunction.GetEntityObject(item.RelatedEntityID, refentity.Fields);
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
                SendMessege(progress, token, destEntitystructure, null, $"Inserting Record {CurrentScriptRecord} ");
                DMEEditor.ErrorObject = destds.InsertEntity(destEntitystructure.EntityName, retval);
                token.ThrowIfCancellationRequested();
                
            }
            catch (Exception ex )
            {
                DMEEditor.AddLogMessage("ETL", $"Failed to Insert Entity {destEntitystructure.EntityName} :{ex.Message}", DateTime.Now, CurrentScriptRecord, ex.Message, Errors.Failed);
                
            }


            return DMEEditor.ErrorObject;
        }
        private void SendMessege(IProgress<PassedArgs> progress, CancellationToken token, EntityStructure refentity = null, ETLScriptDet sc=null,string messege=null)
        {
            if (DMEEditor.ErrorObject.Flag == Errors.Failed)
            {

                SyncErrorsandTracking tr = new SyncErrorsandTracking();
                errorcount++;
                tr.errormessage = DMEEditor.ErrorObject.Message;
                tr.errorsInfo = DMEEditor.ErrorObject;
                tr.rundate = DateTime.Now;
                tr.sourceEntityName = refentity == null ? null : refentity.EntityName; 
                tr.currenrecordindex = CurrentScriptRecord;
                tr.sourceDataSourceName = refentity == null ? null : refentity.DataSourceID;
                if (sc != null)
                {
                    tr.parentscriptid =  sc.ID;
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
    }
    
}
