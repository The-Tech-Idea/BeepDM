
using System.Data;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.DataBase
{
    public interface IDataConnection
    {
        IConnectionProperties ConnectionProp { get; set; }
        ConnectionDriversConfig DataSourceDriver { get; set; }
        ConnectionState ConnectionStatus { get; set; }
        IDMEEditor DMEEditor { get; set; }
         int ID { get; set; }
         string GuidID { get; set; } 
        IDMLogger Logger { get; set; }
        IErrorsInfo ErrorObject { get; set; }
        bool InMemory { get; set; }
        ConnectionState OpenConnection();
      //  IDbConnection DbConn { get; set; }
        string ReplaceValueFromConnectionString();
        ConnectionState OpenConnection(DataSourceType dbtype, string host, int port, string database, string userid, string password, string parameters);
        ConnectionState OpenConnection(DataSourceType dbtype, string connectionstring);
        ConnectionState CloseConn();

    }
}
