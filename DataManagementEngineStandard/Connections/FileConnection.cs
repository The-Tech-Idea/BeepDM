using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Logger;
using TheTechIdea.Util;

using TheTechIdea.Beep.DataBase;
using System.IO;
using System.Data;
using TheTechIdea.Beep.Connections;

namespace TheTechIdea.Beep.FileManager
{
    public class FileConnection : IDataConnection
    {
        public FileConnection(IDMEEditor pDMEEditor)
        {
            DMEEditor = pDMEEditor;
        }
        public bool InMemory { get; set; } = false;
        public IConnectionProperties ConnectionProp { get; set; } = new ConnectionProperties();
        public ConnectionDriversConfig DataSourceDriver { get ; set ; }
        public ConnectionState ConnectionStatus { get; set; } = ConnectionState.Closed;
        public int ID { get; set; }
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
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
            //string rep = "";
            //if (!string.IsNullOrEmpty(DataSourceDriver.ConnectionString) && string.IsNullOrWhiteSpace(ConnectionProp.ConnectionString))
            //{
            //    ConnectionProp.ConnectionString = DataSourceDriver.ConnectionString;
            //} 
            //if (string.IsNullOrWhiteSpace(ConnectionProp.ConnectionString) == false)
            //{

            //    rep = DataSourceDriver.ConnectionString.Replace("{Host}", ConnectionProp.Host);
            //    rep = rep.Replace("{UserID}", ConnectionProp.UserID);

            //    rep = rep.Replace("{Password}", ConnectionProp.Password);
            //    rep = rep.Replace("{DataBase}", ConnectionProp.Database);
            //    rep = rep.Replace("{Port}", ConnectionProp.Port.ToString());

            //}
            //if (ConnectionProp.FilePath.StartsWith(".") || ConnectionProp.FilePath.Equals("/") || ConnectionProp.FilePath.Equals("\\"))
            //{
            //    ConnectionProp.FilePath = ConnectionProp.FilePath.Replace(".", DMEEditor.ConfigEditor.ExePath);
            //}
            //if (!string.IsNullOrWhiteSpace(ConnectionProp.ConnectionString))
            //{
            //    rep = DataSourceDriver.ConnectionString.Replace("{File}", Path.Combine(ConnectionProp.FilePath,ConnectionProp.FileName));
            //}
            //if (!string.IsNullOrWhiteSpace(ConnectionProp.Url))
            //{
            //    rep = DataSourceDriver.ConnectionString.Replace("{Url}", ConnectionProp.Url);
            
            //}
            //if (string.IsNullOrWhiteSpace(ConnectionProp.ConnectionString) && !string.IsNullOrEmpty(ConnectionProp.FilePath) && !string.IsNullOrEmpty(ConnectionProp.FileName))
            //{
            //    rep = Path.Combine(ConnectionProp.FilePath, ConnectionProp.FileName);
            //}
            
            return UtilConnections.ReplaceValueFromConnectionString(DataSourceDriver,ConnectionProp,DMEEditor);
        }
        private ConnectionState OpenConn()
        {
            string r= ReplaceValueFromConnectionString();
            if (!string.IsNullOrEmpty(r))
            {
                if (File.Exists(r))
                {
                    string str = $"Found File  ,{Path.Combine(r, ConnectionProp.FileName)}";
                   
                  //  DMEEditor.AddLogMessage("Success", str, DateTime.Now, -1, "", Errors.Ok);
                    ConnectionStatus = ConnectionState.Open;
                }
                else
                {
                    string str = $"Error in finding File  ,{Path.Combine(r, ConnectionProp.FileName)}";
                
                    DMEEditor.AddLogMessage("Fail", str, DateTime.Now, -1, "", Errors.Failed);
                    ConnectionStatus = ConnectionState.Broken;
                    //ErrorObject.Ex = e;
                }
            }
            else
            {
                string str = $"Error No Path Exist  ,{r}";
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
            string r = ReplaceValueFromConnectionString();
            if (File.Exists(Path.Combine(r, ConnectionProp.FileName)))
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
