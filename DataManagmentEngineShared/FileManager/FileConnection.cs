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
        public FileConnection(IDMEEditor pDMEEditor)
        {
            DMEEditor = pDMEEditor;
        }
        public IConnectionProperties ConnectionProp { get; set; } = new ConnectionProperties();
        public ConnectionDriversConfig DataSourceDriver { get ; set ; }
        public ConnectionState ConnectionStatus { get; set; } = ConnectionState.Closed;
        public int ID { get ; set ; }
        public IDMEEditor DMEEditor { get; set; }
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
            if (ConnectionProp.FilePath.StartsWith(".") || ConnectionProp.FilePath.Equals("/") || ConnectionProp.FilePath.Equals("\\"))
            {
                ConnectionProp.FilePath = ConnectionProp.FilePath.Replace(".", DMEEditor.ConfigEditor.ExePath);
            }
            return rep;
        }
        private ConnectionState OpenConn()
        {
            ReplaceValueFromConnectionString();
            if (Path.Combine(ConnectionProp.FilePath, ConnectionProp.FileName) != null)
            {
                if (File.Exists(Path.Combine(ConnectionProp.FilePath,ConnectionProp.FileName)))
                {
                    string str = $"Found File  ,{Path.Combine(ConnectionProp.FilePath, ConnectionProp.FileName)}";
                   
                  //  DMEEditor.AddLogMessage("Success", str, DateTime.Now, -1, "", Errors.Ok);
                    ConnectionStatus = ConnectionState.Open;
                }
                else
                {
                    string str = $"Error in finding File  ,{Path.Combine(ConnectionProp.FilePath, ConnectionProp.FileName)}";
                
                    DMEEditor.AddLogMessage("Fail", str, DateTime.Now, -1, "", Errors.Failed);
                    ConnectionStatus = ConnectionState.Broken;
                    //ErrorObject.Ex = e;
                }
            }
            else
            {
                string str = $"Error No Path Exist  ,{ConnectionProp.FilePath}";
                DMEEditor.AddLogMessage("Fail", str, DateTime.Now, -1, "", Errors.Failed);
                ConnectionStatus = ConnectionState.Closed;
            }
            DMEEditor.AddLogMessage("Success", $"File Found {ConnectionProp.FileName}", DateTime.Now, -1, "", Errors.Ok);
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
        public virtual ConnectionState CloseConn()
        {
            if (File.Exists(Path.Combine(ConnectionProp.FilePath, ConnectionProp.FileName)))
            {
                DMEEditor.AddLogMessage("Success", $"Closed Connection for File { ConnectionProp.FileName}", DateTime.Now, 0, ConnectionProp.FileName, Errors.Ok);
                ConnectionStatus = ConnectionState.Closed;
            }else
            {
                DMEEditor.AddLogMessage("Success", $"Could not find File { ConnectionProp.FileName} to close", DateTime.Now, 0, ConnectionProp.FileName, Errors.Failed);
                ConnectionStatus = ConnectionState.Broken;
            }
            return ConnectionStatus;
        }
    }
}
