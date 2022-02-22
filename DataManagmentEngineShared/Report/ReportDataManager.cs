using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.DataView;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.Report
{
    public class ReportDataManager
    {
        public ReportDataManager(IDMEEditor pDMEEditor)
        {
            DMEEditor = pDMEEditor;
        }
        public ReportDataManager(IDMEEditor pDMEEditor, IReportDefinition pDefinition)
        {
            DMEEditor = pDMEEditor;
            Definition = pDefinition;
        }
        public IReportDefinition Definition { get; set; }
        public IDMEEditor DMEEditor { get; set; }
        public DataSet Data { get; set; }
        IDataSource ds;
        #region "Data methods"
        private string GetSelectedFields(int blocknumber)
        {
            string selectedfields = "";

            foreach (ReportBlockColumns item in Definition.Blocks[0].BlockColumns.Where(x => x.Show).OrderBy(i => i.FieldDisplaySeq))
            {

                selectedfields += "," + item.ColumnName + " as " + item.DisplayName;
            }
            selectedfields = selectedfields.Remove(0, 1);
            return selectedfields;

        }
        private async Task<dynamic> GetOutputAsync(IDataSource ds, string CurrentEntity, List<ReportFilter> filter)
        {
            return await ds.GetEntityAsync(CurrentEntity, filter).ConfigureAwait(false);
        }
        private object GetData(IDataSource ds, string EntityName, EntityStructure entityStructure)
        {
            object retval = null;

            if (ds != null && ds.ConnectionStatus == ConnectionState.Open)
            {
                if (ds.Category == DatasourceCategory.WEBAPI)
                {
                    try
                    {
                        Task<dynamic> output = GetOutputAsync(ds, EntityName, entityStructure.Filters);
                        output.Wait();
                        dynamic t = output.Result;
                        Type tp = t.GetType();
                        //if (!tp.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IList)))

                        //{
                        //    retval = DMEEditor.ConfigEditor.JsonLoader.JsonToDataTable(t.ToString());
                        //}
                        //else
                        //{
                        retval = t;
                        // }


                    }
                    catch (Exception ex)
                    {
                        DMEEditor.AddLogMessage("Error", $"{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                    }
                }
                else
                {
                    try
                    {
                        retval = ds.GetEntity(EntityName, entityStructure.Filters);

                    }
                    catch (Exception ex)
                    {
                        DMEEditor.AddLogMessage("Error", $"{ex.Message}", DateTime.Now, 0, null, Errors.Failed);

                    }

                }
                if (retval != null)
                {
                    try
                    {
                        GetFields(retval, ds, EntityName, entityStructure);
                    }
                    catch (Exception ex)
                    {
                        retval = null;

                    }


                }
                else
                {
                    retval = null;
                }

            }
            return retval;
        }
        private void GetFields(dynamic retval, IDataSource ds, string EntityName, EntityStructure entityStructure)
        {
            Type tp = retval.GetType();
            DataTable dt;
            if (entityStructure.Fields.Count == 0)
            {
                if (tp.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IList)))
                {
                    dt = DMEEditor.Utilfunction.ToDataTable((IList)retval, DMEEditor.Utilfunction.GetListType(tp));
                }
                else
                {
                    dt = (DataTable)retval;

                }
                foreach (DataColumn item in dt.Columns)
                {
                    EntityField x = new EntityField();
                    try
                    {

                        x.fieldname = item.ColumnName;
                        x.fieldtype = item.DataType.ToString(); //"ColumnSize"
                        x.Size1 = item.MaxLength;
                        try
                        {
                            x.IsAutoIncrement = item.AutoIncrement;
                        }
                        catch (Exception)
                        {

                        }
                        try
                        {
                            x.AllowDBNull = item.AllowDBNull;
                        }
                        catch (Exception)
                        {


                        }



                        try
                        {
                            x.IsUnique = item.Unique;
                        }
                        catch (Exception)
                        {

                        }
                    }
                    catch (Exception ex)
                    {
                        DMEEditor.AddLogMessage("Error", $"{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                    }

                    if (x.IsKey)
                    {
                        entityStructure.PrimaryKeys.Add(x);
                    }


                    entityStructure.Fields.Add(x);
                }




            }
            ds.Entities[ds.Entities.FindIndex(p => p.EntityName == entityStructure.EntityName)] = entityStructure;
            DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new DatasourceEntities { datasourcename = entityStructure.DataSourceID, Entities = ds.Entities });
        }
        private object GetData(int blocknumber)
        {
            try
            {
                object PrintData = null; ;
                EntityStructure ent = null;
                string QueryString = "select " + GetSelectedFields(blocknumber) + " from " + Definition.Blocks[blocknumber].EntityID;
                ds = DMEEditor.GetDataSource(Definition.Blocks[blocknumber].ViewID);
                if (ds != null)
                {
                    if (ds.ConnectionStatus != ConnectionState.Open)
                    {
                        // ds.Dataconnection.OpenConnection();
                        DMEEditor.OpenDataSource(ds.DatasourceName);
                        ds.ConnectionStatus = ds.Dataconnection.ConnectionStatus;
                    }
                    if (ds.ConnectionStatus == ConnectionState.Open)
                    {
                        ent = (EntityStructure)ds.GetEntityStructure(Definition.Blocks[blocknumber].EntityID, true).Clone();
                        if (ent != null)
                        {
                            ent.CustomBuildQuery = QueryString;
                            ent.Viewtype = ViewType.Query;
                            PrintData = GetData(ds, Definition.Blocks[blocknumber].EntityID, ent);
                            if (PrintData != null)
                            {
                                DMEEditor.AddLogMessage("Success", $"Got Data for {Definition.Blocks[blocknumber].EntityID}", DateTime.Now, 0, null, Errors.Ok);
                            }
                            else
                            {
                                DMEEditor.AddLogMessage("Fail", $"Could not get Data for {Definition.Blocks[blocknumber].EntityID}", DateTime.Now, 0, null, Errors.Ok);
                            }
                        }
                        else
                        {
                            DMEEditor.AddLogMessage("Fail", $"Could not entity {Definition.Blocks[blocknumber].EntityID}", DateTime.Now, 0, null, Errors.Ok);
                        }
                    }
                    else
                    {
                        DMEEditor.AddLogMessage("Fail", $"Could not Connect to DataSource {Definition.Blocks[blocknumber].ViewID} for entity  {Definition.Blocks[blocknumber].EntityID}", DateTime.Now, 0, null, Errors.Ok);
                    }
                }
                else
                {
                    DMEEditor.AddLogMessage("Fail", $"Could not get DataSource for {Definition.Blocks[blocknumber].EntityID}", DateTime.Now, 0, null, Errors.Ok);
                }
                return PrintData;
            }
            catch (Exception ex)
            {
                string errmsg = $"Error getting DataSource for {Definition.Blocks[blocknumber].ViewID} ";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return null;
            }
        }
        private object GetData(ReportBlock blk)
        {
           return GetData(Definition.Blocks.IndexOf(blk));
               
        }
        private void CreateConstraint(DataSet dataSet, string childtable, string parenttable, string childcol, string parentcol)
        {
            // Declare parent column and child column variables.
            DataColumn parentColumn = new DataColumn();
            DataColumn childColumn = new DataColumn();
            ForeignKeyConstraint foreignKeyConstraint;
            if (dataSet.Tables.Contains(parenttable))
            {
                if(  parenttable != childtable)
                {
                    parentColumn = dataSet.Tables[parenttable].Columns[parentcol];
                    childColumn = dataSet.Tables[childtable].Columns[childcol];
                    if (parentColumn != null && childColumn != null)
                    {
                        foreignKeyConstraint = new ForeignKeyConstraint
                      (parenttable + "_" + parentcol, parentColumn, childColumn);

                        // Set null values when a value is deleted.
                        foreignKeyConstraint.DeleteRule = Rule.SetNull;
                        foreignKeyConstraint.UpdateRule = Rule.Cascade;
                        foreignKeyConstraint.AcceptRejectRule = AcceptRejectRule.None;

                        // Add the constraint, and set EnforceConstraints to true.
                        dataSet.Tables[childtable].Constraints.Add(foreignKeyConstraint);
                    }
                }
               


            }
            // Set parent and child column variables.

            // dataSet.EnforceConstraints = true;
        }
        private void CreateRelations()
        {
            try
            {
                for (int i = 0; i < Definition.Blocks.Where(p => p.ViewID != null).Count(); i++)
                {
                    ds = DMEEditor.GetDataSource(Definition.Blocks[i].ViewID);

                    if (ds != null)
                    {
                        if (ds.ConnectionStatus != ConnectionState.Open)
                        {
                            //ds.Dataconnection.OpenConnection();
                            DMEEditor.OpenDataSource(ds.DatasourceName);
                            ds.ConnectionStatus = ds.Dataconnection.ConnectionStatus;
                        }
                        if (ds.ConnectionStatus == ConnectionState.Open)
                        {
                            ds.GetEntitesList();
                            EntityStructure ent = (EntityStructure)ds.GetEntityStructure(Definition.Blocks[i].EntityID, true).Clone();
                            if (ent != null)
                            {
                                if (ent.Relations != null)
                                {
                                    if (ent.Relations.Count > 0)
                                    {
                                        foreach (RelationShipKeys relkey in ent.Relations.Where(o => o.RelatedEntityID != null).ToList())
                                        {

                                            CreateConstraint(Data, ent.DatasourceEntityName, relkey.RelatedEntityID, relkey.EntityColumnID, relkey.RelatedEntityColumnID);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                DMEEditor.AddLogMessage("Fail", $"Could not entity {Definition.Blocks[i].EntityID}", DateTime.Now, 0, null, Errors.Ok);
                            }


                        }


                    }


                }
                DMEEditor.AddLogMessage("Success", $"Getting Relations for DataSet", DateTime.Now, 0, null, Errors.Ok);

            }
            catch (Exception ex)
            {
                string errmsg = "Getting Relations for DataSet";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);

            }

        }
        #endregion
        #region "Files Management"
        public string GetReportFilePath(IReportDefinition reportDefinition)
        {
            string retval = null;
            try
            {
                CreateReportsFolder();
                if (DMEEditor.ErrorObject.Flag == Errors.Ok)
                {
                    retval = Path.Combine(DMEEditor.ConfigEditor.Config.ExePath, @"Reports\" + reportDefinition.Name);
                }
                return retval;
            }
            catch (Exception ex)
            {
                string errmsg = $"Error getting File for {reportDefinition.Name} ";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return null;
            }

        }
        private IErrorsInfo CreateReportsFolder()
        {
            try
            {
                if (DMEEditor.ConfigEditor.Config.Folders == null)
                {
                    DMEEditor.ConfigEditor.Config.Folders.Add(new StorageFolders(Path.Combine(DMEEditor.ConfigEditor.Config.ExePath, @"Reports"), FolderFileTypes.Reports));
                }
                if (!DMEEditor.ConfigEditor.Config.Folders.Where(i => i.FolderFilesType == FolderFileTypes.Reports).Any())
                {
                    DMEEditor.ConfigEditor.Config.Folders.Add(new StorageFolders(Path.Combine(DMEEditor.ConfigEditor.Config.ExePath, @"Reports"), FolderFileTypes.Reports));
                }
                DMEEditor.ConfigEditor.CreateDirConfig(Path.Combine(DMEEditor.ConfigEditor.Config.ExePath, @"Reports"), FolderFileTypes.Reports);

                DMEEditor.ConfigEditor.SaveConfigValues();

            }
            catch (Exception ex)
            {

                string errmsg = $"Error Creating Reports Folder";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }


        #endregion
        public DataSet GetDataSet()
        {

            Data = new DataSet();
            try
            {
                if (Definition == null)
                {
                    DMEEditor.AddLogMessage("Beep", $"Failed no Definition Defind", DateTime.Now, 0, null, Errors.Failed);
                    return null;
                }
                if (Definition.Blocks == null)
                {
                    DMEEditor.AddLogMessage("Beep", $"Failed null Blocks", DateTime.Now, 0, null, Errors.Failed);
                    return null;
                }
                if (Definition.Blocks.Count == 0)
                {
                    DMEEditor.AddLogMessage("Beep", $"Failed no Blocks", DateTime.Now, 0, null, Errors.Failed);
                    return null;
                }


                for (int i = 0; i < Definition.Blocks.Where(p => p.ViewID != null).Count(); i++)
                {
                    DataTable tb = new DataTable(Definition.Blocks[i].EntityID);
                    DataTable tmptb = new DataTable(Definition.Blocks[i].EntityID);
                    ds = DMEEditor.GetDataSource(Definition.Blocks[i].ViewID);

                    if (ds != null)
                    {
                        ds.Openconnection();

                        if (ds.ConnectionStatus == ConnectionState.Open)
                        {
                            //  ds.GetEntitesList();
                            //object tmptb = GetData(i);
                            // Type tp = tmptb.GetType();
                            //if (!tp.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IList)))
                            //{

                            //  tb = DMEEditor.Utilfunction.CreateDataTableVer2(tmptb);
                            //}
                            tmptb = (DataTable)GetData(i);
                            if (tmptb != null)
                            {
                                tb = tmptb.Clone();
                                tb.TableName = Definition.Blocks[i].EntityID;
                                foreach (DataRow item in tmptb.Rows)
                                {
                                    tb.ImportRow(item);
                                }
                            }
                         


                        }
                    }

                    Data.Tables.Add(tb);
                }
                CreateRelations();
                DMEEditor.AddLogMessage("Beep", $"Getting Data into DataSet", DateTime.Now, 0, null, Errors.Ok);
                return Data;
            }
            catch (Exception ex)
            {
                string errmsg = "Getting Data into DataSet";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return null;
            }


        }

        public object GetList(string EntityID)
        {

            ReportBlock blk = Definition.Blocks.Where(p => p.ViewID != null && p.EntityID.Equals(EntityID, StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();
            if (blk != null)
            {
              return  GetList(blk);
            }else
                return null;
            
        }
        public object GetList(ReportBlock blk)
        {
            object retval=null;
            try
            {
                if (Definition == null)
                {
                    DMEEditor.AddLogMessage("Beep", $"Failed no Definition Defind", DateTime.Now, 0, null, Errors.Failed);
                    return null;
                }
                if (Definition.Blocks == null)
                {
                    DMEEditor.AddLogMessage("Beep", $"Failed null Blocks", DateTime.Now, 0, null, Errors.Failed);
                    return null;
                }
                if (Definition.Blocks.Count == 0)
                {
                    DMEEditor.AddLogMessage("Beep", $"Failed no Blocks", DateTime.Now, 0, null, Errors.Failed);
                    return null;
                }

              
                ds = DMEEditor.GetDataSource(blk.ViewID);

                if (ds != null)
                {
                    ds.Openconnection();

                    if (ds.ConnectionStatus == ConnectionState.Open)
                    {
                        retval = GetData(blk);
                    }

                }

                DMEEditor.AddLogMessage("Beep", $"Getting Data into List", DateTime.Now, 0, null, Errors.Ok);
               
            }
            catch (Exception ex)
            {
                string errmsg = "Getting Data into List";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return null;
            }
            return retval;


        }

    }   
}
