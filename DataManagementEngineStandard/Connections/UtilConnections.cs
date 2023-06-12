using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.Connections
{
    public static class UtilConnections
    {
        public static string ReplaceValueFromConnectionString(ConnectionDriversConfig DataSourceDriver , IConnectionProperties ConnectionProp,IDMEEditor DMEEditor)
        {
            string rep = "";
            if (!string.IsNullOrEmpty(DataSourceDriver.ConnectionString) && string.IsNullOrWhiteSpace(ConnectionProp.ConnectionString))
            {
                ConnectionProp.ConnectionString = DataSourceDriver.ConnectionString;
            }
            if (string.IsNullOrWhiteSpace(ConnectionProp.ConnectionString) == false)
            {

                rep = DataSourceDriver.ConnectionString.Replace("{Host}", ConnectionProp.Host);
                rep = rep.Replace("{UserID}", ConnectionProp.UserID);

                rep = rep.Replace("{Password}", ConnectionProp.Password);
                rep = rep.Replace("{DataBase}", ConnectionProp.Database);
                rep = rep.Replace("{Port}", ConnectionProp.Port.ToString());

            }
            if (ConnectionProp.FilePath.StartsWith(".") || ConnectionProp.FilePath.Equals("/") || ConnectionProp.FilePath.Equals("\\"))
            {
                ConnectionProp.FilePath = ConnectionProp.FilePath.Replace(".", DMEEditor.ConfigEditor.ExePath);
            }
            if (!string.IsNullOrWhiteSpace(ConnectionProp.ConnectionString))
            {
                rep = DataSourceDriver.ConnectionString.Replace("{File}", Path.Combine(ConnectionProp.FilePath, ConnectionProp.FileName));
            }
            if (!string.IsNullOrWhiteSpace(ConnectionProp.Url))
            {
                rep = DataSourceDriver.ConnectionString.Replace("{Url}", ConnectionProp.Url);

            }
            if (string.IsNullOrWhiteSpace(ConnectionProp.ConnectionString) && !string.IsNullOrEmpty(ConnectionProp.FilePath) && !string.IsNullOrEmpty(ConnectionProp.FileName))
            {
                rep = Path.Combine(ConnectionProp.FilePath, ConnectionProp.FileName);
            }

            return rep;
        }
    }
}
