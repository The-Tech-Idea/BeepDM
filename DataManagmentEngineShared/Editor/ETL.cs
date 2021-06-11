using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

        public LScriptHeader script { get; set; } = new LScriptHeader();
        public LScriptTrackHeader trackingHeader { get; set; } = new LScriptTrackHeader();
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();
        public List<string> EntitiesNames { get; set; } = new List<string>();
        public List<LScript> GetCreateEntityScript(IDataSource Dest, List<EntityStructure> entities)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;

            int i = 0;

            List<LScript> retval = new List<LScript>();

            try
            {
                // Generate Create Table First
                foreach (EntityStructure item in entities)
                {
                    // ds = DMEEditor.GetDataSource(item.DataSourceID);
                    List<EntityStructure> ls = new List<EntityStructure>();
                    List<LScript> rt = new List<LScript>();
                    ls.Add(item);
                    rt = Dest.GetCreateEntityScript(ls);
                    foreach (LScript sc in rt)
                    {
                        sc.sourcedatasourcename = item.DataSourceID;
                        sc.sourceDatasourceEntityName = item.DatasourceEntityName;
                        sc.destinationentityname = item.EntityName;
                        sc.sourceentityname = item.DatasourceEntityName;
                        sc.destinationentityname = item.EntityName;
                        sc.destinationdatasourcename = Dest.DatasourceName;
                    }
                    //  rt.AddRange(Dest.GetCreateEntityScript(ls));
                    script.Scripts.AddRange(rt);
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
        public List<LScript> GetCreateEntityScript(IDataSource ds, List<string> entities)
        {
            List<LScript> rt = new List<LScript>();
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

                    var t = Task.Run<List<LScript>>(() => { return GetCreateEntityScript(ds, ds.Entities); });
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
        public IErrorsInfo CopyEntitiesStructure(IDataSource sourceds, IDataSource destds, List<string> entities, bool CreateMissingEntity = true)
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

                    CopyEntityData(sourceds, destds, item.EntityName, item.EntityName, CreateMissingEntity);

                }

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", $"Error copying Data ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);

            }
            return DMEEditor.ErrorObject;
        }
        public IErrorsInfo CopyEntityStructure(IDataSource sourceds, IDataSource destds, string srcentity, string destentity, bool CreateMissingEntity = true)
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
        public IErrorsInfo CopyDatasourceData(IDataSource sourceds, IDataSource destds, bool CreateMissingEntity = true)
        {
            try
            {

                foreach (EntityStructure item in sourceds.Entities)
                {
                    CopyEntityData(sourceds, destds, item.EntityName, item.EntityName, CreateMissingEntity);
                }

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", $"Error copying Data ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);

            }
            return DMEEditor.ErrorObject;
        }
        public IErrorsInfo CopyEntitiesData(IDataSource sourceds, IDataSource destds, List<string> entities, bool CreateMissingEntity = true)
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
                        CopyEntityData(sourceds, destds, item.DatasourceEntityName, item.EntityName, CreateMissingEntity);
                    }
                    else
                        CopyEntityData(sourceds, destds, item.EntityName, item.EntityName, CreateMissingEntity);

                }

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", $"Error copying Data ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);

            }
            return DMEEditor.ErrorObject;
        }
        public IErrorsInfo CopyEntityData(IDataSource sourceds, IDataSource destds, string srcentity, string destentity, bool CreateMissingEntity = true)
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
                        object srcTb;
                        string entname;
                        var src = Task.Run(() => { return sourceds.GetEntity(item.EntityName, null); });
                        src.Wait();
                        srcTb = src.Result;
                        var dst = Task.Run<IErrorsInfo>(() => { return destds.UpdateEntities(destentity, srcTb); });
                        dst.Wait();
                        DMEEditor.AddLogMessage("Copy Data", $"Ended Copying Data from {srcentity} on {sourceds.DatasourceName} to {srcentity} on {destds.DatasourceName} ", DateTime.Now, 0, null, Errors.Ok);

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
        public IErrorsInfo CopyEntitiesData(IDataSource sourceds, IDataSource destds, List<LScript> scripts, bool CreateMissingEntity = true)
        {
            try
            {

                string srcentityname = "";
                foreach (LScript s in scripts.Where(i => i.scriptType == DDLScriptType.CopyData))
                {
                    if ((s.sourceentityname != s.sourceDatasourceEntityName) && (!string.IsNullOrEmpty(s.sourceDatasourceEntityName)))
                    {
                        srcentityname = s.sourceDatasourceEntityName;
                    }
                    else
                        srcentityname = s.sourceentityname;
                    CopyEntityData(sourceds, destds, srcentityname, s.sourceentityname, CreateMissingEntity);

                }

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", $"Error copying Data ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);

            }
            return DMEEditor.ErrorObject;
        }

        //-----------------------
        private void UpdateEvents(LScript sc, int highestPercentageReached, int CurrentRecord, int numberToCompute, IDataSource destds)
        {
            LScriptTracker tr = new LScriptTracker();
            sc.errorsInfo = DMEEditor.ErrorObject;
            sc.errormessage = DMEEditor.ErrorObject.Message;
            tr.currenrecordentity = sc.sourceentityname;
            tr.currentrecorddatasourcename = sc.destinationdatasourcename;
            //  tr.currenrecordindex = i;
            tr.scriptType = sc.scriptType;
            tr.errorsInfo = sc.errorsInfo;
            DMEEditor.ETL.trackingHeader.trackingscript.Add(tr);
            int percentComplete = (int)((float)CurrentRecord / (float)numberToCompute * 100);
            if (percentComplete > highestPercentageReached)
            {
                highestPercentageReached = percentComplete;
                PassedArgs x = new PassedArgs();
                x.CurrentEntity = tr.currenrecordentity;
                x.DatasourceName = destds.DatasourceName;
                x.Objects.Add(new ObjectItem { obj = tr, Name = "TrackingHeader" });
                x.ParameterInt1 = percentComplete;
                DMEEditor.Passedarguments = x;
                CurrentRecord += 1;

               // PassEvent?.Invoke(this, x);
            }
           
        }
        public IErrorsInfo RunScript(IProgress<int> progress)
        {
            #region "Update Data code "

            int CurrentRecord = 0;
            int highestPercentageReached = 0;
            int numberToCompute = 0;
            LScriptTracker tr;
            IDataSource destds = null;
        
            IDataSource srcds = null;
            DMEEditor.ETL.trackingHeader = new LScriptTrackHeader();
            DMEEditor.ETL.trackingHeader.parentscriptHeaderid = DMEEditor.ETL.script.id;
            DMEEditor.ETL.trackingHeader.rundate = DateTime.Now;
            numberToCompute = DMEEditor.ETL.script.Scripts.Count();
            List<LScript> crls = DMEEditor.ETL.script.Scripts.Where(i => i.scriptType == DDLScriptType.CreateTable).ToList();
            List<LScript> copudatals = DMEEditor.ETL.script.Scripts.Where(i => i.scriptType == DDLScriptType.CopyData).ToList();
            List<LScript> AlterForls = DMEEditor.ETL.script.Scripts.Where(i => i.scriptType == DDLScriptType.AlterFor).ToList();
            // Run Scripts-----------------
            int o = 0;
            numberToCompute = DMEEditor.ETL.script.Scripts.Count;
            int p1 = DMEEditor.ETL.script.Scripts.Where(u => u.scriptType == DDLScriptType.CreateTable).Count();
            foreach (LScript sc in DMEEditor.ETL.script.Scripts.Where(u => u.scriptType == DDLScriptType.CreateTable))
            {
                destds = DMEEditor.GetDataSource(sc.destinationdatasourcename);
                srcds = DMEEditor.GetDataSource(sc.sourcedatasourcename);
                destds.PassEvent += (sender, e) => { PassEvent?.Invoke(sender, e); };
                if (destds != null)
                {
                    DMEEditor.OpenDataSource(sc.destinationdatasourcename);
                    //  srcds.Dataconnection.OpenConnection();
                    if (destds.ConnectionStatus == System.Data.ConnectionState.Open)
                    {
                        if (sc.scriptType == DDLScriptType.CreateTable)
                        {
                            sc.errorsInfo = destds.ExecuteSql(sc.ddl); // t.Result;

                        }
                        else
                        {
                            DMEEditor.ErrorObject.Flag = Errors.Failed;
                            DMEEditor.ErrorObject.Message = $" Could not Connect to on the Data Dources {sc.destinationdatasourcename} or {sc.sourcedatasourcename}";
                        }
                        UpdateEvents(sc, highestPercentageReached, CurrentRecord, numberToCompute, destds);
                        if (progress != null)
                            progress.Report(CurrentRecord);
                    }

                }
            }
            //------------Update Entity structure
            int p2 = DMEEditor.ETL.script.Scripts.Where(u => u.scriptType == DDLScriptType.CopyData).Count();
            foreach (LScript sc in DMEEditor.ETL.script.Scripts.Where(u => u.scriptType == DDLScriptType.CopyData))
            {
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
                            sc.errorsInfo = DMEEditor.ETL.CopyEntityData(srcds, destds, sc.sourceDatasourceEntityName, sc.destinationentityname, true);  //t1.Result;//DMEEditor.ETL.CopyEntityData(srcds, destds, ScriptHeader.Scripts[i], true);
                        }
                        else
                        {
                            DMEEditor.ErrorObject.Flag = Errors.Failed;
                            DMEEditor.ErrorObject.Message = $" Could not Connect to on the Data Dources {sc.destinationdatasourcename} or {sc.sourcedatasourcename}";
                        }
                        // Report progress as a percentage of the total task.
                        UpdateEvents(sc, highestPercentageReached, CurrentRecord, numberToCompute, destds);
                        if (progress != null)
                            progress.Report(CurrentRecord);
                    }
                }
            }
            int p3 = DMEEditor.ETL.script.Scripts.Where(u => u.scriptType == DDLScriptType.AlterFor).Count();
            foreach (LScript sc in DMEEditor.ETL.script.Scripts.Where(u => u.scriptType == DDLScriptType.AlterFor))
            {

                destds = DMEEditor.GetDataSource(sc.destinationdatasourcename);
                srcds = DMEEditor.GetDataSource(sc.sourcedatasourcename);
                if (destds != null)
                {
                    destds.Dataconnection.OpenConnection();
                    DMEEditor.OpenDataSource(sc.destinationdatasourcename);
                    //      srcds.Dataconnection.OpenConnection();
                    if (destds.ConnectionStatus == System.Data.ConnectionState.Open)
                    {
                        if (sc.scriptType == DDLScriptType.AlterFor)
                        {
                            sc.errorsInfo = destds.ExecuteSql(sc.ddl);
                       
                        }
                        else
                        {

                            DMEEditor.ErrorObject.Flag = Errors.Failed;
                            DMEEditor.ErrorObject.Message = $" Could not Connect to on the Data Dources {sc.destinationdatasourcename} or {sc.sourcedatasourcename}";
                            sc.errorsInfo = DMEEditor.ErrorObject;
                            sc.errormessage = DMEEditor.ErrorObject.Message;
                           
                           

                        }
                        UpdateEvents(sc, highestPercentageReached, CurrentRecord, numberToCompute, destds);
                        if (progress != null)
                            progress.Report(CurrentRecord);

                    }

                }

                #endregion

                //-----------------------------

               
            }

            return DMEEditor.ErrorObject;
        }
    }
    
}
