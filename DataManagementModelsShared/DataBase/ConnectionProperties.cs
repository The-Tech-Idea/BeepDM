using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Util;
namespace TheTechIdea.DataManagment_Engine.DataBase
{
    public class ConnectionProperties
    {
        public int ID { get; set; }
     
        public string ConnectionName { get; set; }
        public string UserID { get; set; }
        public string Password { get; set; }
        public string ConnectionString { get; set; }
        public string Host { get; set; }
        public int Port { get; set; } = 0;
        public string Database { get; set; }
        public string Parameters { get; set; }
        public DataSourceType DatabaseType { get; set; }
        public string SchemaName { get; set; }
        public ConnectionProperties()
        {

        }
    }
}
