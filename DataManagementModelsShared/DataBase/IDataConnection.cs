
using System.Data;

using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.DataBase
{
    public interface IDataConnection
    {
        IConnectionProperties ConnectionProp { get; set; }
        ConnectionDriversConfig DataSourceDriver { get; set; }
        ConnectionState ConnectionStatus { get; set; }
        IDMEEditor DMEEditor { get; set; }
        int ID { get; set; }
        IDMLogger Logger { get; set; }
        IErrorsInfo ErrorObject { get; set; }
        ConnectionState OpenConnection();
      //  IDbConnection DbConn { get; set; }
        string ReplaceValueFromConnectionString();
        ConnectionState OpenConnection(DataSourceType dbtype, string host, int port, string database, string userid, string password, string parameters);
        ConnectionState OpenConnection(DataSourceType dbtype, string connectionstring);
        ConnectionState CloseConn();

    }
}
