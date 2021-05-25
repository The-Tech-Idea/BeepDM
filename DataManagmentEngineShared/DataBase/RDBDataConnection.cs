using TheTechIdea.Logger;
using System;
using TheTechIdea.Util;
using System.Data;
using System.Data.Common;
using System.IO;

namespace TheTechIdea.DataManagment_Engine.DataBase
{
    public class RDBDataConnection : IDataConnection
    {
        public int ID { get; set; }
        public IDbConnection DbConn { get; set; }
        public IDMEEditor DMEEditor { get; set; }
        public ConnectionState ConnectionStatus { get; set; } = ConnectionState.Closed;
        public ConnectionDriversConfig DataSourceDriver { get; set; }
        public IDMLogger Logger { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public IConnectionProperties ConnectionProp { get; set; } = new ConnectionProperties();
        public RDBDataConnection()
        {


        }
        public virtual ConnectionState OpenConnection(DataSourceType dbtype, string connectionstring) {
            ConnectionProp.DatabaseType = dbtype;
            ConnectionProp.ConnectionString = connectionstring;
            return OpenConn();
        }
        public virtual ConnectionState OpenConnection(DataSourceType dbtype, string host, int port, string database, string userid, string password, string parameters)
        {
            ConnectionProp.DatabaseType = dbtype;
            ConnectionProp.Host = host;
            ConnectionProp.Port = port;
            ConnectionProp.Database = database;
            ConnectionProp.UserID = userid;
            ConnectionProp.Password = password;
            ConnectionProp.Parameters = parameters;
            return OpenConn();
        }
        public string ReplaceValueFromConnectionString()
        {
            string rep="";
            if (string.IsNullOrWhiteSpace(DataSourceDriver.ConnectionString) == false )
            {
                rep = DataSourceDriver.ConnectionString.Replace("{Host}", ConnectionProp.Host);
                rep = rep.Replace("{UserID}", ConnectionProp.UserID);
                rep = rep.Replace("{Password}", ConnectionProp.Password);
                rep = rep.Replace("{Database}", ConnectionProp.Database);
                rep = rep.Replace("{Port}", ConnectionProp.Port.ToString());
              
                if (rep.Contains("{File}"))
                {
                    rep = rep.Replace("{File}", Path.Combine(ConnectionProp.FilePath, ConnectionProp.FileName));
                }
                if (rep.Contains("{Url}"))
                {
                    rep = rep.Replace("{Url}", ConnectionProp.Url);
                }

                   
            }
           
             return rep;             
        }
        public virtual ConnectionState OpenConnection()
        {

            ConnectionStatus = OpenConn(); 
            return ConnectionStatus;
        }
        public virtual ConnectionState OpenConn()
        {
            if (DbConn != null)
            {
                if (DbConn.State == ConnectionState.Open)
                {
                    ErrorObject.Flag = Errors.Ok;
                    ConnectionStatus = DbConn.State;
                 //   Logger.WriteLog("Database Already Open");
                    ErrorObject.Flag = Errors.Ok;
                    return DbConn.State;
                }


            }
            else
            {
                try
                {

                    DbConn = (IDbConnection) DMEEditor.assemblyHandler.GetInstance(DataSourceDriver.DbConnectionType);
                    if (DbConn != null)
                    {
                        DbConn.ConnectionString = ReplaceValueFromConnectionString(); //ConnectionProp.ConnectionString;
                    }
                    else
                    {
                        ConnectionStatus = ConnectionState.Broken;
                        DMEEditor.AddLogMessage("Fail", $"Could Find DataSource Drivers {DataSourceDriver.classHandler}", DateTime.Now, 0, null, Errors.Failed);
                        return ConnectionState.Broken;
                    }
                        

                    
                 //   Logger.WriteLog("Init  DbConn for  Server");
                }
                catch (Exception e)
                {
                    Logger.WriteLog($"Error in  Init for Sql Database ,{e.Message}");
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = e.Message;
                    ErrorObject.Ex = e;
                }




            }
                try
            {
                if (DbConn != null)
                {
                    if (ConnectionProp.FilePath!=null || ConnectionProp.FileName!=null)
                    {

                        if (System.IO.File.Exists(Path.Combine(ConnectionProp.FilePath, ConnectionProp.FileName)))
                        {
                            DbConn.Open();
                            Logger.WriteLog("Success in open Database");
                            ConnectionStatus = DbConn.State;

                        }
                        else
                        {
                            ConnectionStatus = ConnectionState.Broken;
                        }
                    }
                    else
                    {
                        DbConn.Open();
                        Logger.WriteLog("Success in open Database");
                        ConnectionStatus = DbConn.State;
                    }
                 
                    // Check if need to change schema name
                    if (ConnectionProp.DatabaseType == DataSourceType.Oracle || ConnectionProp.DatabaseType == DataSourceType.SqlServer)
                    {

                        if (ConnectionProp.SchemaName != null)
                        {
                            IDbCommand cmd = DbConn.CreateCommand();
                            switch (ConnectionProp.DatabaseType)
                            {
                                case DataSourceType.Oracle:

                                    cmd.CommandText = $"ALTER SESSION SET CURRENT_SCHEMA = {ConnectionProp.SchemaName}";

                                    break;
                                case DataSourceType.SqlServer:
                                    cmd.CommandText = $"ALTER LOGIN {ConnectionProp.UserID} with DEFAULT_DATABASE = {ConnectionProp.Database}";
                                    break;

                            }
                            try
                            {
                                var x = cmd.ExecuteNonQuery();
                                Logger.WriteLog("Success in Alter Schema");
                                ConnectionStatus = DbConn.State;
                            }

                            catch (Exception e)
                            {
                                Logger.WriteLog("Error in Alter Schema");
                                ErrorObject.Flag = Errors.Failed;
                                ErrorObject.Message = e.Message;
                                ErrorObject.Ex = e;
                            }

                        }
                    }


                }else
                {
                    Logger.WriteLog($"Could not get datasource drivers Database ");
                    ConnectionStatus = ConnectionState.Closed;
                    ErrorObject.Message = "Could not get datasource drivers Database ";
                    ErrorObject.Flag = Errors.Failed;
                }
            }

            catch (Exception e)
            {
                Logger.WriteLog($"Couldnot open Database ,{e.Message}");
                ConnectionStatus = DbConn.State;
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = e.Message;
                ErrorObject.Ex = e;
                //    throw;
            }
            


            

            Logger.WriteLog("Open Database Function End");
            return ConnectionStatus;
        }
        public object GetInstance(string strFullyQualifiedName)
        {
            Type type = Type.GetType(strFullyQualifiedName);
            if (type != null)
                return Activator.CreateInstance(type);
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = asm.GetType(strFullyQualifiedName);
                if (type != null)
                    return Activator.CreateInstance(type);
            }
            return null;
        }

    }
}
