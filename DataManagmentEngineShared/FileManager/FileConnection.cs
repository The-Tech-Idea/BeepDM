using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Logger;
using TheTechIdea.Util;

using TheTechIdea.DataManagment_Engine.DataBase;
using System.IO;
using System.Data;

namespace TheTechIdea.DataManagment_Engine.FileManager
{
    public class FileConnection : IDataConnection
    {
        public IConnectionProperties ConnectionProp { get; set; } = new ConnectionProperties();
        public ConnectionDriversConfig DataSourceDriver { get ; set ; }
        public ConnectionState ConnectionStatus { get; set; } = ConnectionState.Closed;
        public int ID { get ; set ; }
        public IDMLogger Logger { get ; set ; }
        public IErrorsInfo ErrorObject { get ; set ; }
        public IDbConnection DbConn { get ; set ; }
        public ConnectionState OpenConnection()
        {


            return OpenConn();
        }
        public string ReplaceValueFromConnectionString()
        {
            string rep = "";
            if (string.IsNullOrWhiteSpace(DataSourceDriver.ConnectionString) == false)
            {

                rep = DataSourceDriver.ConnectionString.Replace("{Host}", ConnectionProp.Host);
                rep = rep.Replace("{UserID}", ConnectionProp.UserID);

                rep = rep.Replace("{Password}", ConnectionProp.Password);
                rep = rep.Replace("{DataBase}", ConnectionProp.Database);
                rep = rep.Replace("{Port}", ConnectionProp.Port.ToString());

            }
            if (string.IsNullOrWhiteSpace(ConnectionProp.ConnectionString) == false)
            {
                rep = DataSourceDriver.ConnectionString.Replace("{File}", ConnectionProp.ConnectionString);


            }
            if (string.IsNullOrWhiteSpace(ConnectionProp.Url) == false)
            {
                rep = DataSourceDriver.ConnectionString.Replace("{Url}", ConnectionProp.Url);
                // rep =ConnectionProp.Url;

            }


            return rep;



        }
        private ConnectionState OpenConn()
        {
            if (Path.Combine(ConnectionProp.FilePath, ConnectionProp.FileName) != null)
            {
                if (File.Exists(Path.Combine(ConnectionProp.FilePath,ConnectionProp.FileName)))
                {
                    string str = $"Found File  ,{Path.Combine(ConnectionProp.FilePath, ConnectionProp.FileName)}";
                    ErrorObject.Message = str;
                    Logger.WriteLog(str);
                    ErrorObject.Flag = Errors.Ok;
                    ConnectionStatus = ConnectionState.Open;
                 
                   



                }
                else
                {
                    string str = $"Error in finding File  ,{Path.Combine(ConnectionProp.FilePath, ConnectionProp.FileName)}";
                    Logger.WriteLog(str);
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = str;
                    ConnectionStatus = ConnectionState.Broken;
                    //ErrorObject.Ex = e;
                }

            }
            else
            {
                string str = $"Error No Path Exist  ,{ConnectionProp.FilePath}";
                Logger.WriteLog(str);
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = str;
                ConnectionStatus = ConnectionState.Closed;
            }
            

           

            Logger.WriteLog("Open File Function End");
            return ConnectionStatus;
        }

        public ConnectionState OpenConnection(DataSourceType dbtype, string host, int port, string database, string userid, string password, string parameters)
        {
            throw new NotImplementedException();
        }

        public ConnectionState OpenConnection(DataSourceType dbtype, string connectionstring)
        {
            throw new NotImplementedException();
        }
    }
}
