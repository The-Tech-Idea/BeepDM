using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Text;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.WebAPI;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace DataManagmentEngineShared.WebAPI
{
    public class WebAPIDataConnection : IDataConnection

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
        private static bool isValidURL(string url)
        {
            //WebRequest webRequest = WebRequest.Create(url);
            //foreach (WebApiHeader item in ConnectionProp.Headers)
            //{
            //    webRequest.Headers.Add(item.headername, item.headervalue);
            //}
         
            //WebResponse webResponse;
            //try
            //{
            //    webResponse = webRequest.GetResponse();
            //}
            //catch //If exception thrown then couldn't get response from address
            //{
            //    return false;
            //}
            return true;
        }
        private ConnectionState OpenConn()
        {
            if (isValidURL(ConnectionProp.Url) )
            {
                 string str = $" Found WebApi  : {ConnectionProp.Url}";
                    ErrorObject.Message = str;
                    Logger.WriteLog(str);
                    ErrorObject.Flag = Errors.Ok;
                    ConnectionStatus = ConnectionState.Open;

                }
                else
                {
                    string str = $"Error in finding WebApi  ,{ConnectionProp.Url}";
                    Logger.WriteLog(str);
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = str;
                    ConnectionStatus = ConnectionState.Broken;
                    //ErrorObject.Ex = e;
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
