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

using DataManagementModels.DriversConfigurations;
using TheTechIdea.Beep.Helpers;

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
            
            return ConnectionHelper.ReplaceValueFromConnectionString(DataSourceDriver,ConnectionProp,DMEEditor);
        }
        private ConnectionState OpenConn()
        {
            DataSourceDriver= ConnectionHelper.LinkConnection2Drivers(ConnectionProp, DMEEditor.ConfigEditor );
            if (DataSourceDriver== null)
            {
                DataSourceDriver = DMEEditor.ConfigEditor.DataDriversClasses.Where(c => c.classHandler ==ConnectionProp.DriverName).FirstOrDefault();
            }
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
            return ConnectionStatus;
        }

        public ConnectionState OpenConnection(DataSourceType dbtype, string connectionstring)
        {
            return ConnectionStatus;
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
