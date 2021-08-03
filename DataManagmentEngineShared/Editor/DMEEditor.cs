
using System.Collections.Generic;
using System.Data;
using TheTechIdea.Util;
using System.Linq;
using System;
using System.Reflection;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Logger;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Workflow;
using System.ComponentModel;
using TheTechIdea.Beep.Logger;
using System.IO;
using TheTechIdea.Tools;
using static TheTechIdea.Beep.Util;
using TheTechIdea.Beep.Report;
using System.Threading.Tasks;
using System.Collections;

namespace TheTechIdea.Beep
{
    public class DMEEditor : IDMEEditor,IDisposable
    {
        private bool disposedValue;
        public bool ContainerMode { get; set; } = false;
        public string ContainerName { get; set; } = null;
        public List<IDataSource> DataSources { get; set; } = new List<IDataSource>();
        public IETL ETL { get; set; }
        public IConfigEditor ConfigEditor { get; set; }
        public IDataTypesHelper typesHelper { get; set; }
        public IUtil Utilfunction { get; set; }
        public IAssemblyHandler assemblyHandler { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public IDMLogger Logger { get; set; }
        public IWorkFlowEditor WorkFlowEditor { get; set; }
        public IClassCreator classCreator { get; set; }
        public SyncDataSource Script { get; set; } = new SyncDataSource();
        public BindingList<ILogAndError> Loganderrors { get; set; } = new BindingList<ILogAndError>();
        public IPassedArgs Passedarguments { get; set; }
        public event EventHandler<PassedArgs> PassEvent;
 
        public void AddLogMessage(string pLogType ,string pLogMessage ,DateTime pLogData , int pRecordID , string pMiscData,Errors pFlag)
        {
            if (Logger != null)
            {
                LogAndError log = new LogAndError(pLogType, pLogMessage, pLogData, pRecordID, pMiscData);
                Loganderrors.Add(log);
                string errmsg = pLogType + "," + pLogMessage;
                ErrorObject.Flag = pFlag;
                ErrorObject.Message = errmsg;
                Logger.WriteLog(errmsg);
            }
           

        }
        public void AddLogMessage( string pLogMessage)
        {
            if (Logger != null)
            {
                LogAndError log = new LogAndError("Beep", pLogMessage,DateTime.Now, 0, null);
                Loganderrors.Add(log);
                string errmsg = "Beep" + "," + pLogMessage;
                ErrorObject.Flag =  Errors.Ok;
                ErrorObject.Message = errmsg;
                Logger.WriteLog(errmsg);
            }


        }
        public ConnectionState OpenDataSource(string pdatasourcename)
        {
            try
            {
                IDataSource ds1 = null;
                ds1 = DataSources.Where(f => f.DatasourceName.Equals(pdatasourcename, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                if (ds1 == null)
                {
                    GetDataSource(pdatasourcename);
                  
                }
                if (ds1 != null)
                {
                    return ds1.Openconnection();
                }
                else
                    return ConnectionState.Broken; 

            }
            catch (Exception ex)
            {

                AddLogMessage("Fail", $"Could not Open DataSource Connection {ex.Message}", DateTime.Now, 0, pdatasourcename, Errors.Failed);
                return ConnectionState.Broken; 
            }

        }
        public bool CloseDataSource(string pdatasourcename)
        {
            try
            {
                ConnectionState st= ConnectionState.Closed;
                IDataSource ds1 = GetDataSource(pdatasourcename);
                if (ds1 != null)
                {
                     st= ds1.Dataconnection.CloseConn();
                }
                else
                {
                    return false;
                }
                if (st == ConnectionState.Open)
                {
                    return true;
                }
                else
                    return false;
               
            }
            catch (Exception ex)
            {

                AddLogMessage("Fail", $"Could not Open DataSource Connection {ex.Message}", DateTime.Now, 0, pdatasourcename, Errors.Failed);
                return false;
            }
        }
        public IDataSource GetDataSource(string pdatasourcename)
        {
            IDataSource ds1=null;
            if (pdatasourcename == null)
            {
                return null;
            }
            else {

                try
                {
                    ds1 = DataSources.Where(f => f.DatasourceName.Equals(pdatasourcename, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                }
                catch (Exception ex)
                {
              
                    AddLogMessage(ex.Message, "Could not Open Datasource " , DateTime.Now, -1, "", Errors.Failed);
                };
            

                if (ds1 == null) //|| ds1.ConnectionStatus==ConnectionState.Closed

                {

                    try
                    {
                        ds1 = CreateNewDataSourceConnection(pdatasourcename);
                        if (ds1 != null)
                        {
                            if (ds1.Entities.Count == 0)
                            {

                                if (ConfigEditor.LoadDataSourceEntitiesValues(ds1.DatasourceName) != null)
                                {
                                    ds1.Entities = ConfigEditor.LoadDataSourceEntitiesValues(ds1.DatasourceName).Entities;
                                }

                                //if (ds1.Entities.Count == 0)
                                //{
                                //    ds1.Entities = ds1.Dataconnection.ConnectionProp.Entities;

                                //    ConfigEditor.SaveDataSourceEntitiesValues(new DatasourceEntities { datasourcename = pdatasourcename, Entities = ds1.Entities });
                                //}
                            }
                            
                        }else
                        {
                           
                            AddLogMessage("Fail", $"Error in Opening Connection ({ErrorObject.Message})", DateTime.Now, -1, "", Errors.Failed);
                        }
                       
                      
                    }
                    catch (Exception ex)
                    {

                        AddLogMessage("Fail", $"Error in Opening Connection ({ex.Message})", DateTime.Now, -1, "", Errors.Failed);

                        return null;
                    }
         

                }

            }



            return ds1 ;
        }
        public AssemblyClassDefinition GetDataSourceClass(string DatasourceName)
        {
            AssemblyClassDefinition retval = null;
            try
            {
                ConnectionProperties cn = ConfigEditor.DataConnections.Where(f => f.ConnectionName == DatasourceName).FirstOrDefault();
                ConnectionDriversConfig driversConfig = Utilfunction.LinkConnection2Drivers(cn);
                if (cn==null || driversConfig == null)
                {
                    AddLogMessage("Fail", "Could not get Datasource class " , DateTime.Now, -1, "", Errors.Failed);
                }else
                {
                    retval = ConfigEditor.DataSourcesClasses.Where(x => x.className == driversConfig.classHandler).FirstOrDefault();
                }
               
               

            }
            catch (Exception ex)
            {
                string mes = "";
                AddLogMessage(ex.Message, "Could not get Datasource class " + mes, DateTime.Now, -1, mes, Errors.Failed);
                return null;
            };
            return retval;
        }
        public IDataSource CreateNewDataSourceConnection(string pdatasourcename)
        {

            ConnectionProperties cn = ConfigEditor.DataConnections.Where(f => f.ConnectionName.Equals(pdatasourcename,StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            ErrorObject.Flag = Errors.Ok;
            IDataSource ds=null;
           if (cn != null)
            {
                ConnectionDriversConfig driversConfig = Utilfunction.LinkConnection2Drivers(cn);
                if (ConfigEditor.DataSourcesClasses.Where(x => x.className == driversConfig.classHandler).Any())
                {
                    string packagename = ConfigEditor.DataSourcesClasses.Where(x => x.className == driversConfig.classHandler).FirstOrDefault().PackageName;
                    //  Type adc = Type.GetType(packagename);
                    Type adc = assemblyHandler.GetType(packagename);
                    ConstructorInfo ctor = adc.GetConstructors().First();
                    ObjectActivator<IDataSource> createdActivator = GetActivator<IDataSource>(ctor);
                    //create an instance:
                    ds = createdActivator(cn.ConnectionName, Logger, this, cn.DatabaseType, ErrorObject);
                }
                
                try
                {
                    if (ds != null)
                    {
                        ds.Dataconnection.ConnectionProp = cn;
                        ds.Dataconnection.DataSourceDriver = driversConfig;
                        //  ds.ConnectionStatus = ds.Dataconnection.OpenConnection();
                        DataSources.Add(ds);
                        return ds;
                    }
                    else
                    {
                        AddLogMessage("Fail", "Could Find DataSource Drivers", DateTime.Now, 0, pdatasourcename, Errors.Failed);
                        return null;
                    }
                   
                    //if (ds.ConnectionStatus == ConnectionState.Open)
                    //{

                    //    AddLogMessage("Success", "Create DataSource Success" + pdatasourcename, DateTime.Now, 0, null, Errors.Ok);
                    //    return ds;
                    //}
                    //else
                    //{
                    //    AddLogMessage("Failure", "Error occured in  DataSource Creation " + pdatasourcename, DateTime.Now, 0, null, Errors.Ok);
                    //    return null;

                    //}
                }
                catch (Exception ex)
                {


                    AddLogMessage("Fail", $"Error in Opening Connection (Check DLL for Connection drivers,connect string, Datasource down,Firewall, .. etc)({ex.Message})", DateTime.Now, -1, "", Errors.Failed);
                    return null;

                }

            }
            else
            {
                AddLogMessage("Failure", "Error occured in  DataSource Creation " + pdatasourcename, DateTime.Now, 0, null, Errors.Ok);
                return null;
            }




        }
        public IDataSource CreateNewDataSourceConnection(ConnectionProperties cn, string pdatasourcename)
        {

            //ConnectionProperties cn = ConfigEditor.DataConnections.Where(f => f.ConnectionName == pdatasourcename).FirstOrDefault();
            ErrorObject.Flag = Errors.Ok;
            IDataSource ds = null;
            ConnectionDriversConfig driversConfig = Utilfunction.LinkConnection2Drivers(cn);
            if (ConfigEditor.DataSourcesClasses.Where(x => x.className == driversConfig.classHandler).Any())
            {
                string packagename = ConfigEditor.DataSourcesClasses.Where(x => x.className == driversConfig.classHandler).FirstOrDefault().PackageName;
                //  Type adc = Type.GetType(packagename);
                Type adc = assemblyHandler.GetType(packagename);
                ConstructorInfo ctor = adc.GetConstructors().First();
                ObjectActivator<IDataSource> createdActivator = GetActivator<IDataSource>(ctor);
                //create an instance:
                ds = createdActivator(cn.ConnectionName, Logger, this, cn.DatabaseType, ErrorObject);
            }


            try
            {

               
                if (ds == null)
                {
                    AddLogMessage("Fail", "Could Find DataSource Drivers", DateTime.Now, 0, pdatasourcename, Errors.Failed);
                    return null;
                }
                else
                {
                    ds.Dataconnection.ConnectionProp = cn;
                    ds.Dataconnection.DataSourceDriver = driversConfig;
                    DataSources.Add(ds);
                    return ds;
                }
            }
            catch (Exception ex)
            {


              
                AddLogMessage("Fail", "Error in Opening Connection (Check DLL for Connection drivers,connect string, Datasource down,Firewall, .. etc)({ex.Message})", DateTime.Now, 0, pdatasourcename, Errors.Failed);
                return null;

            }




        }
        public IDataSource CreateLocalDataSourceConnection(ConnectionProperties dataConnection, string pdatasourcename,string ClassDBHandlerName)
        {

            ErrorObject.Flag = Errors.Ok;
            IDataSource ds = null;
            ConnectionDriversConfig package=null;
            if (ConfigEditor.DataDriversClasses.Where(x => x.classHandler == ClassDBHandlerName).Any())
            {
                 package = ConfigEditor.DataDriversClasses.Where(x => x.classHandler == ClassDBHandlerName).FirstOrDefault();
                string packagename = ConfigEditor.DataSourcesClasses.Where(x => x.className == package.classHandler).FirstOrDefault().PackageName;

                Type adc = assemblyHandler.GetType(packagename);
                ConstructorInfo ctor = adc.GetConstructors().First();
                ObjectActivator<IDataSource> createdActivator = GetActivator<IDataSource>(ctor);

                //create an instance:
                ds = createdActivator(dataConnection.ConnectionName, Logger, this, dataConnection.DatabaseType, ErrorObject);
            }
       

            try
            {
                if (ds != null)
                {
                    ds.Dataconnection.ConnectionProp = dataConnection;
                    ds.Dataconnection.DataSourceDriver = package;
                    ds.Dataconnection.ReplaceValueFromConnectionString();
                    ILocalDB dB = (ILocalDB)ds;
                    DataSources.Add(ds);

                    AddLogMessage("Fail", $"Success Created Local Database  {pdatasourcename}", DateTime.Now, -1, "", Errors.Failed);
                    return ds;
                }else
                {
                    AddLogMessage("Fail", "Could Find DataSource Drivers", DateTime.Now, 0, pdatasourcename, Errors.Failed);
                    return null;
                }
              
           //     bool ok= dB.CreateDB();
          //      if (ok)
          //      {
                  //  ds.ConnectionStatus = ds.Dataconnection.OpenConnection();
                //if (ds.ConnectionStatus == ConnectionState.Open)
                //{
                    // ConfigEditor.DataConnections.Add(dataConnection);
                    
                   
                //}else
                //{
                //    return null;
                //}
                
          
               
            }
            catch (Exception ex)
            {


                
                AddLogMessage("Fail", $"Error in Opening Connection (Check DLL for Connection drivers,connect string, Datasource down,Firewall, .. etc)({ex.Message})", DateTime.Now, -1, "", Errors.Failed);
                return null;

            }




        }
        public bool RemoveDataDource(string pdatasourcename)
        {

            try
            {
                IDataSource ds = DataSources.Where(x => x.DatasourceName.ToLower() == pdatasourcename.ToLower()).FirstOrDefault();
             
                
                if (ds != null)
                {
                    //if (ds.Dataconnection.DbConn != null)
                    //{
                    //    ds.Dataconnection.DbConn.Close();
                    //    ds.Dataconnection.DbConn.Dispose();
                    //}
                    if (ds.Dataconnection.DataSourceDriver.CreateLocal)
                    {
                        ConfigEditor.RemoveDataSourceEntitiesValues(ds.DatasourceName);
                    }
                        DataSources.Remove(ds);
                   
                }
                else
                {
                    AddLogMessage("Error", "Could not Find data source " + pdatasourcename, DateTime.Now, -1, pdatasourcename, Errors.Failed);
                }
                
                return true;
            }
            catch (Exception ex)
            {
                string mes = ex.Message;
                AddLogMessage(ex.Message, "Could not Remove data source " + mes, DateTime.Now, -1, mes, Errors.Failed);
                return false;
            };
        }
        public EntityStructure GetEntityStructure(string entityname,string datasourcename)
        {
            IDataSource ds = null;
            EntityStructure entity = null;
            try
            {
                ds = GetDataSource(datasourcename);
                if (ds != null)
                {
                    entity= ds.GetEntityStructure(entityname,true);
                }
                return entity;
            }
            catch (Exception ex)
            {

                return entity;
            }
        }
        public bool CheckDataSourceExist(string pdatasourcename)
        {

            try
            {
                if (DataSources.Count > 0)
                {
                    return DataSources.Where(x => x.DatasourceName.ToLower() == pdatasourcename.ToLower()).Any();
                }
                else
                {
                    return true;
                }

             
                // AddLogMessage("Success", "Added Database Connection", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not check Datasource Exist";
                AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return false;
        }
        public void RaiseEvent(object sender, PassedArgs args)
        {
            PassEvent?.Invoke(sender, args);
        }
        private async Task<dynamic> GetOutputAsync(IDataSource ds,string CurrentEntity, List<ReportFilter> filter)
        {
            return await ds.GetEntityAsync(CurrentEntity, filter).ConfigureAwait(false);
        }

        public object GetData(IDataSource ds,EntityStructure entity)
        {
            object retval = null;

            if (ds != null && ds.ConnectionStatus == ConnectionState.Open)
            {
                if (ds.Category == DatasourceCategory.WEBAPI)
                {
                    try
                    {
                        Task<dynamic> output = GetOutputAsync(ds,entity.EntityName, entity.Filters);
                        output.Wait();
                        dynamic t = output.Result;
                        Type tp = t.GetType();
                        if (!tp.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IList)))

                        {
                            retval = ConfigEditor.JsonLoader.JsonToDataTable(t.ToString());
                        }
                        else
                        {
                            retval = t;
                        }


                    }
                    catch (Exception ex)
                    {
                        AddLogMessage($"{ex.Message}");
                    }
                }
                else
                {
                    try
                    {
                        retval = ds.GetEntity(entity.EntityName, entity.Filters);

                    }
                    catch (Exception ex)
                    {

                        AddLogMessage($"{ex.Message}");
                    }

                }
                if (retval != null)
                {
                    try
                    {
                        Utilfunction.GetEntityStructureFromListorTable(ref entity,retval);
                    }
                    catch (Exception ex)
                    {

                        throw;
                    }

                    //if (retval.GetType().FullName == "System.Data.DataTable")
                    //{
                    //    retval = DMEEditor.Utilfunction.ConvertTableToList((DataTable)retval, EntityStructure, DMEEditor.Utilfunction.GetEntityType(EntityStructure.EntityName,EntityStructure.Fields));
                    //}

                }
                else
                {
                    retval = null;
                }
               
           //     RefreshData(retval);
            }
            return retval;
        }
        public IErrorsInfo AskQuestion(IPassedArgs args)
        {
            try
            {

            }
            catch (Exception ex)
            {

                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
                ErrorObject.Message = ex.Message;
            }
            return ErrorObject;
        }

        //----------------- ------------------------------ -----
        public DMEEditor(IDMLogger logger, IUtil utilfunctions,IErrorsInfo per, IConfigEditor configEditor,IWorkFlowEditor pworkFlowEditor, IClassCreator pclasscreator, IETL pETL, IAssemblyHandler passemblyHandler, IDataTypesHelper dataTypesHelper)
        {
          
            logger.WriteLog("init all variables");
            Logger = logger;
            Utilfunction = utilfunctions;
            Utilfunction.DME = this;
            ConfigEditor = configEditor;
            ErrorObject = per;
            classCreator = pclasscreator;
            WorkFlowEditor = pworkFlowEditor;
            WorkFlowEditor.DMEEditor = this;
            ETL = pETL;
            ETL.DMEEditor = this;
            assemblyHandler = passemblyHandler;
            typesHelper = dataTypesHelper;
            typesHelper.DMEEditor = this;


        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                foreach (var item in DataSources)
                {
                    item.Closeconnection();
                    item.Dispose();
                }
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~DMEEditor()
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
