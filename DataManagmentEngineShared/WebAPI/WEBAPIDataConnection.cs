using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Text;
using TheTechIdea.DataManagment_Engine;
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
        public IDMEEditor DMEEditor { get; set; }
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
                     DMEEditor.AddLogMessage("Success", str, DateTime.Now, -1, "", Errors.Ok);
                     ErrorObject.Flag = Errors.Ok;
                    ConnectionStatus = ConnectionState.Open;

                }
                else
                {
                    string str = $"Error in finding WebApi  ,{ConnectionProp.Url}";
                  
                    DMEEditor.AddLogMessage("Error", str, DateTime.Now, -1,"", Errors.Failed);
                ConnectionStatus = ConnectionState.Broken;
                    //ErrorObject.Ex = e;
                }
            DMEEditor.AddLogMessage("Success", $"WebAPI {ConnectionProp.ConnectionName} Found", DateTime.Now, -1, "", Errors.Ok);
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
            if (DbConn != null)
            {
                if (DbConn.State == ConnectionState.Open)
                {
                    ErrorObject.Flag = Errors.Ok;

                    try
                    {
                      //  DbConn.Close();
                        ConnectionStatus = ConnectionState.Closed;
                    }
                    catch (Exception ex)
                    {
                        DMEEditor.AddLogMessage("Fail", $"Could not close Connetion Database Function End {ex.Message}", DateTime.Now, 0, null, Errors.Failed);

                    }

                    return DbConn.State;
                }
                else
                {
                    ConnectionStatus = ConnectionState.Closed;
                    return ConnectionStatus;
                }


            }
            else
            {
                ConnectionStatus = ConnectionState.Closed;
                DMEEditor.AddLogMessage("Sucess", $"Closed WebAPI {ConnectionProp.ConnectionName} ", DateTime.Now, -1, "", Errors.Failed);
                return ConnectionStatus;

            }

          
            

        }
    }
}
