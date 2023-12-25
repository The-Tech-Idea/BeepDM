using DataManagementModels.DriversConfigurations;
using System;
using System.Data;
using System.IO;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.DataView
{
    /// <summary>
    /// Represents a connection to a data source.
    /// </summary>
    public class DataViewConnection : IDataConnection
    {
        /// <summary>
        /// Gets or sets a value indicating whether the connection is in memory.
        /// </summary>
        public bool InMemory { get; set; } = false;
        /// <summary>Gets or sets the connection properties.</summary>
        /// <value>The connection properties.</value>
        public IConnectionProperties ConnectionProp { get; set; }
        /// <summary>Gets or sets the configuration for the data source driver.</summary>
        /// <value>The configuration for the data source driver.</value>
        public ConnectionDriversConfig DataSourceDriver { get; set; }
        /// <summary>Gets or sets the current connection status.</summary>
        /// <value>The current connection status.</value>
        public ConnectionState ConnectionStatus { get; set; }
        /// <summary>Gets or sets the DME editor.</summary>
        /// <value>The DME editor.</value>
        public IDMEEditor DMEEditor { get; set; }
        /// <summary>Gets or sets the ID.</summary>
        /// <value>The ID.</value>
        public int ID { get; set; }
        /// <summary>Gets or sets the GUID ID.</summary>
        /// <value>The GUID ID.</value>
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        /// <summary>Gets or sets the logger for the current object.</summary>
        /// <value>The logger.</value>
        public IDMLogger Logger { get; set; }
        /// <summary>Gets or sets the error object.</summary>
        /// <value>The error object.</value>
        public IErrorsInfo ErrorObject { get; set; }
        /// <summary>Gets or sets the database connection.</summary>
        /// <value>The database connection.</value>
        public IDbConnection DbConn { get; set; }

        /// <summary>Opens a connection to a database.</summary>
        /// <returns>The state of the connection after opening.</returns>
        public ConnectionState OpenConnection()
        {
            return OpenConn();
        }
        /// <summary>Replaces a specific value in a connection string.</summary>
        /// <returns>The modified connection string.</returns>
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
        /// <summary>Opens a connection to the database.</summary>
        /// <returns>The connection state after opening the connection.</returns>
        private ConnectionState OpenConn()
        {
            ReplaceValueFromConnectionString();
            if (Path.Combine(ConnectionProp.FilePath, ConnectionProp.FileName) != null)
            {
                if (File.Exists(Path.Combine(ConnectionProp.FilePath, ConnectionProp.FileName)))
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


        /// <summary>Opens a connection to a database.</summary>
        /// <param name="dbtype">The type of the database.</param>
        /// <param name="host">The host name or IP address of the database server.</param>
        /// <param name="port">The port number of the database server.</param>
        /// <param name="database">The name of the database.</param>
        /// <param name="userid">The user ID for authentication.</param>
        /// <param name="password">The password for authentication.</param>
        /// <param name="parameters">Additional parameters for the connection.</param>
        /// <returns>The connection state after opening the connection.</returns>
        public ConnectionState OpenConnection(DataSourceType dbtype, string host, int port, string database, string userid, string password, string parameters)
        {
            return ConnectionStatus;
        }

        /// <summary>Opens a connection to a database.</summary>
        /// <param name="dbtype">The type of the database.</param>
        /// <param name="connectionstring">The connection string for the database.</param>
        /// <returns>The state of the connection.</returns>
        public ConnectionState OpenConnection(DataSourceType dbtype, string connectionstring)
        {
            return ConnectionStatus;
        }
        /// <summary>Closes the connection to the database.</summary>
        /// <returns>The current state of the connection after closing.</returns>
        public virtual ConnectionState CloseConn()
        {
            if (File.Exists(Path.Combine(ConnectionProp.FilePath, ConnectionProp.FileName)))
            {
                DMEEditor.AddLogMessage("Success", $"Closed Connection for File {ConnectionProp.FileName}", DateTime.Now, 0, ConnectionProp.FileName, Errors.Ok);
                ConnectionStatus = ConnectionState.Closed;
            }
            else
            {
                DMEEditor.AddLogMessage("Success", $"Could not find File {ConnectionProp.FileName} to close", DateTime.Now, 0, ConnectionProp.FileName, Errors.Failed);
                ConnectionStatus = ConnectionState.Broken;
            }
            return ConnectionStatus;
        }
    }
}
