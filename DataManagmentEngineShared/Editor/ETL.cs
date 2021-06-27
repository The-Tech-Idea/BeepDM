using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.Editor
{

    public class ETL : IETL
    {
        public ETL()
        {

        }
        public event EventHandler<PassedArgs> PassEvent;
        public IDMEEditor DMEEditor { get; set; }
        public PassedArgs Passedargs { get; set; }
        public int ScriptCount { get; set; }
        public int CurrentScriptRecord { get; set; }
        public decimal StopErrorCount { get; set; } = 10;
        public SyncDataSource script { get; set; } = new SyncDataSource();
        private bool stoprun = false;
      //  public LScriptTracking Tracker { get; set; } = new LScriptTracking();
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();
        public List<string> EntitiesNames { get; set; } = new List<string>();
        private List<SyncEntity> GenerateCopyScripts(List<SyncEntity> rt, EntityStructure item,string destSource)
        {
            List<SyncEntity> retval = new List<SyncEntity>();
            foreach (SyncEntity sc in rt)
            {
                SyncEntity upscript = new SyncEntity();
                upscript.sourcedatasourcename = item.DataSourceID;
                upscript.sourceentityname = item.EntityName;
                upscript.sourceDatasourceEntityName = item.DatasourceEntityName;

                upscript.destinationDatasourceEntityName = item.EntityName;
                upscript.destinationentityname = item.EntityName;
                upscript.destinationdatasourcename = destSource;
                upscript.scriptType = DDLScriptType.CopyData;
                retval.Add(upscript);
            }
           
            return retval;
        }
        private SyncEntity GenerateCopyScript(SyncEntity rt, EntityStructure item, string destSource)
        {
               
            
                SyncEntity upscript = new SyncEntity();
                upscript.sourcedatasourcename = item.DataSourceID;
                upscript.sourceentityname = item.EntityName;
                upscript.sourceDatasourceEntityName = item.DatasourceEntityName;

                upscript.destinationDatasourceEntityName = item.EntityName;
                upscript.destinationentityname = item.EntityName;
                upscript.destinationdatasourcename = destSource;
                upscript.scriptType = DDLScriptType.CopyData;
           

            return upscript;
        }
        public List<SyncEntity> GetCreateEntityScript(IDataSource Dest, List<EntityStructure> entities,IProgress<PassedArgs> progress, CancellationToken token)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;

            int i = 0;

            List<SyncEntity> retval = new List<SyncEntity>();

            try
            {
                // Generate Create Table First
                foreach (EntityStructure item in entities)
                {
                    // ds = DMEEditor.GetDataSource(item.DataSourceID);
                    List<EntityStructure> ls = new List<EntityStructure>();
                    List<SyncEntity> rt = new List<SyncEntity>();
                    ls.Add(item);
                    rt = Dest.GetCreateEntityScript(ls);
                    foreach (SyncEntity sc in rt)
                    {
                        sc.sourcedatasourcename = item.DataSourceID;
                        sc.sourceDatasourceEntityName = item.DatasourceEntityName;
                        sc.destinationentityname = item.EntityName;
                        sc.sourceentityname = item.DatasourceEntityName;
                        sc.destinationentityname = item.EntityName;
                        sc.destinationdatasourcename = Dest.DatasourceName;
                        sc.CopyDataScripts.Add(GenerateCopyScript(sc, item, Dest.DatasourceName));
                    }
                    //  rt.AddRange(Dest.GetCreateEntityScript(ls));
                    script.Entities.AddRange(rt);
                    retval.AddRange(rt);
                    i += 1;
                }

                //CreateForKeyRelationScripts(Dest, entities);
                // CreateForKeyRelationScripts

                DMEEditor.AddLogMessage("Success", $"Generated Script", DateTime.Now, 0, null, Errors.Ok);

            }
            catch (Exception ex)
            {
                string errmsg = "Error in Generating Script";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);

            }
            return retval;

        }
        public void CreateScriptHeader(IDataSource Srcds, IProgress<PassedArgs> progress, CancellationToken token)
        {
            int i = 0;
            script = new SyncDataSource();
            script.scriptSource = Srcds.DatasourceName;
            List<EntityStructure> ls = new List<EntityStructure>();
            Srcds.GetEntitesList();
            foreach (string item in Srcds.EntitiesNames)
            {
                ls.Add(Srcds.GetEntityStructure(item, true));
            }
            DMEEditor.ETL.GetCreateEntityScript(Srcds, ls, progress, token);
            foreach (var item in ls)
            {

                SyncEntity upscript = new SyncEntity();
                upscript.sourcedatasourcename = item.DataSourceID;
                upscript.sourceentityname = item.EntityName;
                upscript.sourceDatasourceEntityName = item.DatasourceEntityName;

                upscript.destinationDatasourceEntityName = item.EntityName;
                upscript.destinationentityname = item.EntityName;
                upscript.destinationdatasourcename = Srcds.DatasourceName;
                upscript.scriptType = DDLScriptType.CopyData;
                script.Entities.Add(upscript);
                i += 1;
            }
        }
        public List<SyncEntity> GetCreateEntityScript(IDataSource ds, List<string> entities, IProgress<PassedArgs> progress, CancellationToken token)
        {
            List<SyncEntity> rt = new List<SyncEntity>();
            try
            {


                foreach (string item in entities)
                {
                    var t = Task.Run<EntityStructure>(() => { return ds.GetEntityStructure(item, true); });
                    t.Wait();
                    EntityStructure t1 = t.Result;

                    t1.Created = false;


                }

                if (ds.Entities.Count > 0)
                {

                    var t = Task.Run<List<SyncEntity>>(() => { return GetCreateEntityScript(ds, ds.Entities,progress,token); });
                    t.Wait();

                    rt.AddRange(t.Result);

                }
            }
            catch (System.Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error in getting entities from Database ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);

            }
            return rt;
        }
        public IErrorsInfo CopyEntitiesStructure(IDataSource sourceds, IDataSource destds, List<string> entities,IProgress<PassedArgs> progress, CancellationToken token, bool CreateMissingEntity = true)
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

                    CopyEntityData(sourceds, destds, item.EntityName, item.EntityName,progress, token, CreateMissingEntity);

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
        public IErrorsInfo CopyDatasourceData(IDataSource sourceds, IDataSource destds, IProgress<PassedArgs> progress, CancellationToken token, bool CreateMissingEntity = true)
        {
            try
            {

                foreach (EntityStructure item in sourceds.Entities)
                {
                    CopyEntityData(sourceds, destds, item.EntityName, item.EntityName,progress, token, CreateMissingEntity);
                }

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", $"Error copying Data ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);

            }
            return DMEEditor.ErrorObject;
        }
        public IErrorsInfo CopyEntitiesData(IDataSource sourceds, IDataSource destds, List<string> entities, IProgress<PassedArgs> progress, CancellationToken token, bool CreateMissingEntity = true)
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
                        CopyEntityData(sourceds, destds, item.DatasourceEntityName, item.EntityName,progress, token, CreateMissingEntity);
                    }
                    else
                        CopyEntityData(sourceds, destds, item.EntityName, item.EntityName,progress, token,  CreateMissingEntity);

                }

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", $"Error copying Data ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);

            }
            return DMEEditor.ErrorObject;
        }
        public IErrorsInfo CopyEntityData(IDataSource sourceds, IDataSource destds, string srcentity, string destentity, IProgress<PassedArgs> progress, CancellationToken token, bool CreateMissingEntity = true)
        {
            try
            {
                int errorcount = 0;


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

                        object srcTb;
                        string entname;
                        var src = Task.Run(() => { return sourceds.GetEntity(item.EntityName, null); });
                        src.Wait();
                        srcTb = src.Result;
                        List<object> srcList=new List<object>();
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
                            //if (progress != null)
                            //{
                            //    PassedArgs ps = new PassedArgs { ParameterInt1 = CurrentScriptRecord, ParameterInt2 = ScriptCount };
                            //    progress.Report(ps);

                            //}
                            foreach (var r in srcList)
                            {
                                CurrentScriptRecord += 1;
                               DMEEditor.ErrorObject=destds.InsertEntity(item.EntityName, r);
                                
                                token.ThrowIfCancellationRequested();
                                if (progress != null)
                                {
                                    PassedArgs ps = new PassedArgs {  ParameterInt1 = CurrentScriptRecord, ParameterInt2 = ScriptCount, ParameterString3 = DMEEditor.ErrorObject.Message };
                                    progress.Report(ps);

                                }
                               
                            }
                        }


                        //var dst = Task.Run<IErrorsInfo>(() => { return destds.UpdateEntities(destentity, srcTb,progress); });
                        //dst.Wait();
                        if (progress != null)
                        {
                            PassedArgs ps = new PassedArgs { ParameterString1 = $"Ended Copying Data from {srcentity} on {sourceds.DatasourceName} to {srcentity} on {destds.DatasourceName} ", ParameterInt1 = CurrentScriptRecord, ParameterInt2 = ScriptCount };
                            progress.Report(ps);

                        }
                      //  DMEEditor.AddLogMessage("Copy Data", $"Ended Copying Data from {srcentity} on {sourceds.DatasourceName} to {srcentity} on {destds.DatasourceName} ", DateTime.Now, 0, null, Errors.Ok);

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
        private IErrorsInfo RunCopyEntityScript(ref SyncEntity sc,IDataSource sourceds, IDataSource destds, string srcentity, string destentity, IProgress<PassedArgs> progress, CancellationToken token, bool CreateMissingEntity = true)
        {
            try
            {
                int errorcount = 0;


                EntityStructure item = sourceds.GetEntityStructure(srcentity, true);

                if (item != null)
                {
                    if (destds.Category == DatasourceCategory.RDBMS)
                    {
                        IRDBSource rDB = (IRDBSource)destds;
                        rDB.DisableFKConstraints(item);
                    }
                    if (destds.CheckEntityExist(item.EntityName))
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
                            //if (progress != null)
                            //{
                            //    PassedArgs ps = new PassedArgs { ParameterInt1 = CurrentScriptRecord, ParameterInt2 = ScriptCount };
                            //    progress.Report(ps);

                            //}
                            foreach (var r in srcList)
                            {
                                CurrentScriptRecord += 1;
                                DMEEditor.ErrorObject = destds.InsertEntity(item.EntityName, r);
                                token.ThrowIfCancellationRequested();
                                if (DMEEditor.ErrorObject.Flag== Errors.Failed)
                                {
                                    SyncErrorsandTracking tr = new SyncErrorsandTracking();
                                    errorcount++;
                                    tr.errormessage = DMEEditor.ErrorObject.Message;
                                    tr.errorsInfo = DMEEditor.ErrorObject;
                                    tr.rundate = DateTime.Now;
                                    tr.sourceEntityName = item.EntityName;
                                    tr.currenrecordindex = CurrentScriptRecord;
                                    tr.sourceDataSourceName = item.DataSourceID;
                                    tr.parentscriptid = sc.id;
                                    sc.Tracking.Add(tr);
                                    if (progress != null)
                                    {
                                        PassedArgs ps = new PassedArgs {EventType="Update", ParameterInt1 = CurrentScriptRecord, ParameterInt2 = ScriptCount, ParameterString3 = DMEEditor.ErrorObject.Message };
                                        progress.Report(ps);

                                    }
                                    if (errorcount >= StopErrorCount)
                                    {
                                        stoprun = true;
                                        PassedArgs ps = new PassedArgs { EventType = "Stop", ParameterInt1 = CurrentScriptRecord, ParameterInt2 = ScriptCount, ParameterString3 = DMEEditor.ErrorObject.Message };
                                        progress.Report(ps);
                                    }

                                }
                                else
                                {
                                    if (progress != null)
                                    {
                                        PassedArgs ps = new PassedArgs {EventType="NA", ParameterInt1 = CurrentScriptRecord, ParameterInt2 = ScriptCount, ParameterString3 = DMEEditor.ErrorObject.Message };
                                        progress.Report(ps);

                                    }
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
        public IErrorsInfo CopyEntitiesData(IDataSource sourceds, IDataSource destds, List<SyncEntity> scripts, IProgress<PassedArgs> progress, CancellationToken token, bool CreateMissingEntity = true)
        {
            try
            {

                string srcentityname = "";
                foreach (SyncEntity s in scripts.Where(i => i.scriptType == DDLScriptType.CopyData))
                {
                    if ((s.sourceentityname != s.sourceDatasourceEntityName) && (!string.IsNullOrEmpty(s.sourceDatasourceEntityName)))
                    {
                        srcentityname = s.sourceDatasourceEntityName;
                    }
                    else
                        srcentityname = s.sourceentityname;
                    CopyEntityData(sourceds, destds, srcentityname, s.sourceentityname,progress, token,CreateMissingEntity);

                }

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", $"Error copying Data ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);

            }
            return DMEEditor.ErrorObject;
        }

        //-----------------------
         public async Task<IErrorsInfo> RunChildScriptAsync(SyncEntity ParentScript,IDataSource srcds,IDataSource destds, IProgress<PassedArgs> progress, CancellationToken token)
        {
            if (ParentScript.CopyDataScripts.Count > 0)
            {

                //foreach (LScript sc in ParentScript.CopyDataScripts.Where(p=>p.scriptType==DDLScriptType.CopyData).ToList())
                for (int i = 0; i < ParentScript.CopyDataScripts.Count; i++)
                {
                    SyncEntity sc = ParentScript.CopyDataScripts[i];
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

                                await Task.Run(() =>
                                {
                                    sc.errorsInfo = RunCopyEntityScript(ref sc,srcds, destds, sc.sourceDatasourceEntityName, sc.destinationentityname, progress, token, true);  //t1.Result;//DMEEditor.ETL.CopyEntityData(srcds, destds, ScriptHeader.Scripts[i], true);
                                    
                                });
                                if (DMEEditor.ErrorObject.Flag == Errors.Failed)
                                {
                                  

                                    sc.errormessage = DMEEditor.ErrorObject.Message;
                                    sc.errorsInfo = DMEEditor.ErrorObject;
                                    sc.Active = false;
                                    
                                    if (progress != null)
                                    {
                                        PassedArgs ps = new PassedArgs { EventType = "Update", ParameterInt1 = CurrentScriptRecord, ParameterInt2 = ScriptCount, ParameterString3 = DMEEditor.ErrorObject.Message };
                                        progress.Report(ps);

                                    }

                                }
                            }
                            else
                            {
                                DMEEditor.ErrorObject.Flag = Errors.Failed;
                                DMEEditor.ErrorObject.Message = $" Could not Connect to on the Data Dources {sc.destinationdatasourcename} or {sc.sourcedatasourcename}";
                            }
                            // Report progress as a percentage of the total task.
                            //    UpdateEvents(sc, highestPercentageReached, CurrentRecord, numberToCompute, destds);

                        }
                    }
                }
            }
            return DMEEditor.ErrorObject;
        }
        public async Task<IErrorsInfo> RunScriptAsync(IProgress<PassedArgs> progress, CancellationToken token)
        {
            #region "Update Data code "


            int highestPercentageReached = 0;
            int numberToCompute = 0;
          
            IDataSource destds = null;
            IDataSource srcds = null;
            //DMEEditor.ETL.Tracker = new LScriptTracking();
            //DMEEditor.ETL.Tracker.rundate = DateTime.Now;
            numberToCompute = DMEEditor.ETL.script.Entities.Count();
            List<SyncEntity> crls = DMEEditor.ETL.script.Entities.Where(i => i.scriptType == DDLScriptType.CreateEntity).ToList();
            List<SyncEntity> copudatals = DMEEditor.ETL.script.Entities.Where(i => i.scriptType == DDLScriptType.CopyData).ToList();
            List<SyncEntity> AlterForls = DMEEditor.ETL.script.Entities.Where(i => i.scriptType == DDLScriptType.AlterFor).ToList();
            // Run Scripts-----------------
            int o = 0;
            numberToCompute = DMEEditor.ETL.script.Entities.Count;
            int p1 = DMEEditor.ETL.script.Entities.Where(u => u.scriptType == DDLScriptType.CreateEntity).Count();
            ScriptCount = p1;
            CurrentScriptRecord = 0;
            foreach (SyncEntity sc in DMEEditor.ETL.script.Entities.Where(u => u.scriptType == DDLScriptType.CreateEntity))
            {
                destds = DMEEditor.GetDataSource(sc.destinationdatasourcename);
                srcds = DMEEditor.GetDataSource(sc.sourcedatasourcename);
                CurrentScriptRecord += 1;

                // destds.PassEvent += (sender, e) => { PassEvent?.Invoke(sender, e); };
                if (destds != null)
                {
                    DMEEditor.OpenDataSource(sc.destinationdatasourcename);
                    if (stoprun == false)
                    {
                        if (destds.ConnectionStatus == System.Data.ConnectionState.Open)
                        {
                            if (sc.scriptType == DDLScriptType.CreateEntity)
                            {
                                EntityStructure entitystr = (EntityStructure)srcds.GetEntityStructure(sc.sourceDatasourceEntityName, false).Clone();
                                bool retval = destds.CreateEntityAs(entitystr); // t.Result;
                                sc.errorsInfo = DMEEditor.ErrorObject;
                                sc.errormessage = DMEEditor.ErrorObject.Message;
                                if (sc.errorsInfo.Flag == Errors.Ok)
                                {
                                    sc.Active = true;
                                    sc.errormessage = "Entity Creation is Successful";
                                    if (progress != null)
                                    {
                                        PassedArgs ps = new PassedArgs { EventType = "Update", ParameterInt1 = CurrentScriptRecord, ParameterInt2 = ScriptCount, ParameterString3 = DMEEditor.ErrorObject.Message };
                                        progress.Report(ps);


                                    }
                                    if (sc.CopyDataScripts.Count > 0)
                                    {
                                        await RunChildScriptAsync(sc, srcds, destds, progress, token);
                                    }
                                }
                                else
                                {
                                    sc.errormessage = DMEEditor.ErrorObject.Message;
                                    sc.errorsInfo = DMEEditor.ErrorObject;
                                    sc.Active = false;
                                    if (progress != null)
                                    {
                                        PassedArgs ps = new PassedArgs { EventType = "Update", ParameterInt1 = CurrentScriptRecord, ParameterInt2 = ScriptCount, ParameterString3 = DMEEditor.ErrorObject.Message };
                                        progress.Report(ps);

                                    }
                                }

                            }
                            else
                            {
                                DMEEditor.ErrorObject.Flag = Errors.Failed;
                                DMEEditor.ErrorObject.Message = $" Could not Connect to on the Data Dources {sc.destinationdatasourcename} or {sc.sourcedatasourcename}";
                            }
                            //  UpdateEvents(sc, highestPercentageReached, CurrentRecord, numberToCompute, destds);
                            if (progress != null)
                            {
                                PassedArgs ps = new PassedArgs { ParameterInt1 = CurrentScriptRecord, ParameterInt2 = ScriptCount };
                                progress.Report(ps);

                            }

                        }
                    }
                   

                }
            }
          
            #endregion


            //    //-----------------------------


            //}

            return DMEEditor.ErrorObject;
        }
    }
    
}
