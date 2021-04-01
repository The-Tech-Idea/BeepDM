using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.DataView
{
    public class DataViewConnection : IDataConnection
    {
        public IConnectionProperties ConnectionProp { get ; set ; }
        public ConnectionDriversConfig DataSourceDriver { get ; set ; }
        public ConnectionState ConnectionStatus { get ; set ; }
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
            string filen = Path.Combine(ConnectionProp.FilePath, ConnectionProp.FileName);
            if (filen != null)
            {
                if (File.Exists(filen))
                {
                    string str = $"Found File  ,{filen}";
                 
                    ConnectionStatus = ConnectionState.Open;





                }
                else
                {
                    string str = $"Error in finding File  ,{filen}";
                   
                    ConnectionStatus = ConnectionState.Broken;
                    //ErrorObject.Ex = e;
                }

            }
            else
            {
                string str = $"Error No Path Exist  ,{filen}";
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
