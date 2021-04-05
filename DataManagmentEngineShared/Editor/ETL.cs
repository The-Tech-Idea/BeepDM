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
            IDataSource ds;
            List<LScript> rt = new List<LScript>();

            try
            {
                // Generate Create Table First
                foreach (EntityStructure item in entities)
                {
                   // ds = DMEEditor.GetDataSource(item.DataSourceID);
                    List<EntityStructure> ls = new List<EntityStructure>();
                    ls.Add(item);
                    rt.AddRange(Dest.GetCreateEntityScript(ls));
                  
                    i += 1;
                }
                script.Scripts.AddRange(rt);
                //CreateForKeyRelationScripts(Dest, entities);
                // CreateForKeyRelationScripts

                DMEEditor.AddLogMessage("Success", $"Generated Script", DateTime.Now, 0, null, Errors.Ok);

            }
            catch (Exception ex)
            {
                string errmsg = "Error in Generating Script";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);

            }
            return rt;

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

                foreach (EntityStructure item in ls)
                {
                    CopyEntityData(sourceds, destds, item.EntityName, CreateMissingEntity);

                }

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", $"Error copying Data ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);

            }
            return DMEEditor.ErrorObject;
        }
        public IErrorsInfo CopyEntityStructure(IDataSource sourceds, IDataSource destds, string entity, bool CreateMissingEntity = true)
        {
            try
            {



                EntityStructure item = sourceds.GetEntityStructure(entity, true);
                  if (destds.CreateEntityAs(item))
                    {
                     //   DMEEditor.AddLogMessage("Copy Data", $"Started Copying Strcuture for {item.EntityName}", DateTime.Now, 0, null, Errors.Ok);
                        if (destds.Category == DatasourceCategory.RDBMS)
                        {

                            IRDBSource rDB = (IRDBSource)destds;
                            rDB.DisableFKConstraints(item);
                        }
                        //DataTable srcTb = new DataTable();
                        //var src = Task.Run<DataTable>(() => { return sourceds.GetEntity(item.EntityName, ""); });
                        //src.Wait();
                        //srcTb = src.Result;
                        //var dst = Task.Run<IErrorsInfo>(() => { return destds.UpdateEntities(item.EntityName, srcTb, null); });
                        //dst.Wait();
                        DMEEditor.AddLogMessage("Copy Data", $"Ended Copying Strcuture for {item.EntityName}", DateTime.Now, 0, null, Errors.Ok);
                        if (destds.Category == DatasourceCategory.RDBMS)
                        {

                            IRDBSource rDB = (IRDBSource)destds;
                            rDB.EnableFKConstraints(item);
                        }

                    }
                    else
                    {
                        DMEEditor.AddLogMessage("Copy Data", $"Error : Could not Create missing Entity {item.EntityName}", DateTime.Now, 0, null, Errors.Failed);
                    }
                



            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", $"Error copying Data ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);

            }
            return DMEEditor.ErrorObject;
        }
        public IErrorsInfo CopyDatasourceData(IDataSource sourceds, IDataSource destds, bool CreateMissingEntity = true)
        {
            try
            {

                foreach (EntityStructure item in sourceds.Entities)
                {
                    CopyEntityData(sourceds, destds, item.EntityName, CreateMissingEntity);
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
                    CopyEntityData(sourceds, destds,item.EntityName, CreateMissingEntity);

                }

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", $"Error copying Data ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);

            }
            return DMEEditor.ErrorObject;
        }
        public IErrorsInfo CopyEntityData(IDataSource sourceds, IDataSource destds, string entity, bool CreateMissingEntity = true)
        {
            try
            {



                EntityStructure item = sourceds.GetEntityStructure(entity, true);
             
                    if (destds.CreateEntityAs(item))
                    {
                   //     DMEEditor.AddLogMessage("Copy Data", $"Started Copying Data for {item.EntityName}", DateTime.Now, 0, null, Errors.Ok);
                        if (destds.Category == DatasourceCategory.RDBMS)
                        {

                            IRDBSource rDB = (IRDBSource)destds;
                            rDB.DisableFKConstraints(item);
                        }
                        DataTable srcTb = new DataTable();
                        var src = Task.Run<DataTable>(() => { return (DataTable)sourceds.GetEntity(item.EntityName, ""); });
                        src.Wait();
                        srcTb = src.Result;
                        var dst = Task.Run<IErrorsInfo>(() => { return destds.UpdateEntities(item.EntityName, srcTb); });
                        dst.Wait();
                        DMEEditor.AddLogMessage("Copy Data", $"Ended Copying Data for {item.EntityName}", DateTime.Now, 0, null, Errors.Ok);
                        if (destds.Category == DatasourceCategory.RDBMS)
                        {

                            IRDBSource rDB = (IRDBSource)destds;
                            rDB.EnableFKConstraints(item);
                        }

                    }
                    else
                    {
                        DMEEditor.AddLogMessage("Copy Data", $"Error : Could not Create missing Entity {item.EntityName}", DateTime.Now, 0, null, Errors.Failed);
                    }
                
            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", $"Error copying Data ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);

            }
            return DMEEditor.ErrorObject;
        }
        public IErrorsInfo CopyEntitiesData(IDataSource sourceds, IDataSource destds,List<LScript> scripts, bool CreateMissingEntity = true)
        {
            try
            {
              

                foreach (LScript s in scripts.Where(i=>i.scriptType==DDLScriptType.CopyData))
                {
                    CopyEntityData(sourceds, destds, s, CreateMissingEntity);

                }

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", $"Error copying Data ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);

            }
            return DMEEditor.ErrorObject;
        }
        public IErrorsInfo CopyEntityData(IDataSource sourceds, IDataSource destds, LScript scripts, bool CreateMissingEntity = true)
        {
            try
            {


                
                    EntityStructure item = sourceds.GetEntityStructure(scripts.entityname, true);
                  
                        if (destds.CreateEntityAs(item))
                        {
                           // DMEEditor.AddLogMessage("Copy Data", $"Started Copying Data for {item.EntityName}", DateTime.Now, 0, null, Errors.Ok);
                            if (destds.Category == DatasourceCategory.RDBMS)
                            {

                                IRDBSource rDB = (IRDBSource)destds;
                                rDB.DisableFKConstraints(item);
                            }
                            DataTable srcTb = new DataTable();
                            var src = Task.Run<DataTable>(() => { return (DataTable)sourceds.GetEntity(item.EntityName, ""); });
                            src.Wait();
                            srcTb = src.Result;
                            //var dst = Task.Run<IErrorsInfo>(() => { return destds.UpdateEntities(item.EntityName, srcTb, null); });
                            //dst.Wait();
                           destds.UpdateEntities(item.EntityName, srcTb);
                           DMEEditor.AddLogMessage("Copy Data", $"Ended Copying Data for {item.EntityName}", DateTime.Now, 0, null, Errors.Ok);
                            if (destds.Category == DatasourceCategory.RDBMS)
                            {

                                IRDBSource rDB = (IRDBSource)destds;
                                rDB.EnableFKConstraints(item);
                            }

                        }
                        else
                        {
                            DMEEditor.AddLogMessage("Copy Data", $"Error : Could not Create missing Entity {item.EntityName}", DateTime.Now, 0, null, Errors.Failed);
                        }
                    

               

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", $"Error copying Data ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);

            }
            return DMEEditor.ErrorObject;
        }
        //-----------------------
        public IErrorsInfo RunScript(  )
        {
            int CurrentRecord = 1;
            int highestPercentageReached = 0;
            int numberToCompute = 0;
            var dDLScripts = script;
            DMEEditor.ETL.trackingHeader = new LScriptTrackHeader();
            DMEEditor.ETL.trackingHeader.parentscriptHeaderid = script.id;
            DMEEditor.ETL.trackingHeader.rundate = DateTime.Now;
            numberToCompute = script.Scripts.Count();

            numberToCompute = dDLScripts.Scripts.Count();
                // Run Scripts-----------------
                for (int i = 0; i < dDLScripts.Scripts.Count(); i++)
                {

                    IDataSource destds = DMEEditor.GetDataSource(script.Scripts[i].destinationdatasourcename);
                    IDataSource srcds = DMEEditor.GetDataSource(script.Scripts[i].sourcedatasourcename);
                    CurrentRecord = i;
                    if (script.Scripts[i].scriptType != DDLScriptType.CopyData)
                    {
                        var t = Task.Run<IErrorsInfo>(() => { return destds.ExecuteSql(script.Scripts[i].ddl); });
                        t.Wait();
                        script.Scripts[i].errorsInfo = t.Result;
                    }
                    else
                    {
                        var t1 = Task.Run<IErrorsInfo>(() => { return CopyEntityData(srcds, destds, script.Scripts[i], true); });
                        t1.Wait();
                        script.Scripts[i].errorsInfo = t1.Result;
                    }

                  //  script.Scripts[i].errorsInfo = t.Result;  //destds.ExecuteSql(DMEEditor.DDLEditor.script[i].ddl);
                    script.Scripts[i].errormessage = DMEEditor.ErrorObject.Message;
                LScriptTracker tr = new LScriptTracker();
                tr.currenrecordentity = script.Scripts[i].entityname;
                tr.currentrecorddatasourcename = script.Scripts[i].destinationdatasourcename;
                tr.currenrecordindex = i;
                tr.scriptType = script.Scripts[i].scriptType;
                tr.errorsInfo = script.Scripts[i].errorsInfo;
                DMEEditor.ETL.trackingHeader.trackingscript.Add(tr);

                // Report progress as a percentage of the total task.
                int percentComplete = (int)((float)CurrentRecord / (float)numberToCompute * 100);
                    if (percentComplete > highestPercentageReached)
                    {
                        highestPercentageReached = percentComplete;

                    }
                    PassedArgs x = new PassedArgs();
                    x.CurrentEntity = script.Scripts[i].entityname;
                    x.DatasourceName = destds.DatasourceName;
                    x.Objects.Add(new ObjectItem { obj = trackingHeader, Name = "TrackingHeader" });
                    x.ParameterInt1 = percentComplete;
                    //  ReportProgress?.Invoke(this, x);

                }
           
            return DMEEditor.ErrorObject;
        }

        #region "RDBMS"

        #endregion

    }
}
