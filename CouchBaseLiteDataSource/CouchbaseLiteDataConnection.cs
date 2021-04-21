using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.NOSQL
{
    public class CouchbaseLiteDataConnection : IDataConnection
    {
        public IConnectionProperties ConnectionProp { get ; set ; }
        public ConnectionDriversConfig DataSourceDriver { get ; set ; }
        public ConnectionState ConnectionStatus { get ; set ; }
        public IDMEEditor DMEEditor { get; set; }
        public int ID { get ; set ; }
        public IDMLogger Logger { get ; set ; }
        public IErrorsInfo ErrorObject { get ; set ; }
        public IDbConnection DbConn { get ; set ; }

        public ConnectionState OpenConnection()
        {
            throw new NotImplementedException();
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
