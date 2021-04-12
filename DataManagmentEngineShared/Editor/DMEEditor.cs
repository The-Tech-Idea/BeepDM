
using System.Collections.Generic;
using System.Data;
using TheTechIdea.Util;
using System.Linq;
using System;
using System.Reflection;

using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.Logger;

using TheTechIdea.DataManagment_Engine.Editor;

using TheTechIdea.DataManagment_Engine.Workflow;
using TheTechIdea.DataManagment_Engine.Report;

using static TheTechIdea.DataManagment_Engine.Util;
using System.ComponentModel;
using TheTechIdea.DataManagment_Engine.Logger;
using System.IO;
using TheTechIdea.Tools;
using TheTechIdea.DataManagment_Engine.ConfigUtil;

namespace TheTechIdea.DataManagment_Engine
{
    public class DMEEditor : IDMEEditor
    {
       
        public List<IDataSource> DataSources { get; set; } = new List<IDataSource>();
        public IETL ETL { get; set; }
        public IConfigEditor ConfigEditor { get; set; }
        public IDataTypesHelper typesHelper { get; set; }
        public IUtil Utilfunction { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public IDMLogger Logger { get; set; }
        public IWorkFlowEditor WorkFlowEditor { get; set; }
        public IClassCreator classCreator { get; set; }
        public LScriptHeader Script { get; set; } = new LScriptHeader();
        public BindingList<ILogAndError> Loganderrors { get; set; } = new BindingList<ILogAndError>();
        public PassedArgs Passedarguments { get; set; }
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
                    ds1 = DataSources.Where(f => string.Equals( f.DatasourceName, pdatasourcename, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
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

                                if (ds1.Entities.Count == 0)
                                {
                                    ds1.Entities = ds1.Dataconnection.ConnectionProp.Entities;

                                    ConfigEditor.SaveDataSourceEntitiesValues(new DatasourceEntities { datasourcename = pdatasourcename, Entities = ds1.Entities });
                                }
                            }
                            
                        }else
                        {
                            ErrorObject.Flag = Errors.Failed;
                            ErrorObject.Message = "Error Datasource Not found";
                            Logger.WriteLog($"Error in Opening Connection ({ErrorObject.Message})");
                        }
                       
                      
                    }
                    catch (Exception ex)
                    {

                        ErrorObject.Flag = Errors.Failed;
                        ErrorObject.Ex = ex;
                        Logger.WriteLog($"Error in Opening Connection ({ex.Message})");

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
                    retval = ConfigEditor.DataSources.Where(x => x.className == driversConfig.classHandler).FirstOrDefault();
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

            ConnectionProperties cn = ConfigEditor.DataConnections.Where(f => f.ConnectionName == pdatasourcename).FirstOrDefault();
            ErrorObject.Flag = Errors.Ok;
            IDataSource ds=null;
           if (cn != null)
            {
                ConnectionDriversConfig driversConfig = Utilfunction.LinkConnection2Drivers(cn);
                if (ConfigEditor.DataSources.Where(x => x.className == driversConfig.classHandler).Any())
                {
                    string packagename = ConfigEditor.DataSources.Where(x => x.className == driversConfig.classHandler).FirstOrDefault().PackageName;
                    //  Type adc = Type.GetType(packagename);
                    Type adc = Utilfunction.GetType(packagename);
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


                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Ex = ex;
                    Logger.WriteLog($"Error in Opening Connection (Check DLL for Connection drivers,connect string, Datasource down,Firewall, .. etc)({ex.Message})");

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
            if (ConfigEditor.DataSources.Where(x => x.className == driversConfig.classHandler).Any())
            {
                string packagename = ConfigEditor.DataSources.Where(x => x.className == driversConfig.classHandler).FirstOrDefault().PackageName;
                //  Type adc = Type.GetType(packagename);
                Type adc = Utilfunction.GetType(packagename);
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


                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
                Logger.WriteLog($"Error in Opening Connection (Check DLL for Connection drivers,connect string, Datasource down,Firewall, .. etc)({ex.Message})");

                return null;

            }




        }
        public IDataSource CreateLocalDataSourceConnection(ConnectionProperties dataConnection, string pdatasourcename,string ClassDBHandlerName)
        {

            ErrorObject.Flag = Errors.Ok;
            IDataSource ds = null;
            ConnectionDriversConfig package=null;
            if (ConfigEditor.DataDrivers.Where(x => x.classHandler == ClassDBHandlerName).Any())
            {
                 package = ConfigEditor.DataDrivers.Where(x => x.classHandler == ClassDBHandlerName).FirstOrDefault();
                string packagename = ConfigEditor.DataSources.Where(x => x.className == package.classHandler).FirstOrDefault().PackageName;

                Type adc = Utilfunction.GetType(packagename);
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
                    Logger.WriteLog($"Success Created Local Database " + pdatasourcename);

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


                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
                Logger.WriteLog($"Error in Opening Connection (Check DLL for Connection drivers,connect string, Datasource down,Firewall, .. etc)({ex.Message})");

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
                    if (ds.Dataconnection.DbConn != null)
                    {
                        ds.Dataconnection.DbConn.Close();
                        ds.Dataconnection.DbConn.Dispose();
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
        //----------------- ------------------------------ -----
        public DMEEditor(IDMLogger logger, IUtil utilfunctions,IErrorsInfo per, IConfigEditor configEditor,IWorkFlowEditor pworkFlowEditor, IClassCreator pclasscreator, IETL pETL)
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
            typesHelper = new DataTypesHelper(Logger, this, ErrorObject);
            
        }



    }
}
