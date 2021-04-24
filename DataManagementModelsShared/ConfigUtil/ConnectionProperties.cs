using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using TheTechIdea.DataManagment_Engine.DataBase;

namespace TheTechIdea.Util
{
    public class ConnectionProperties : IConnectionProperties
    {
        public int ID { get; set; }
        public string ConnectionName { get; set; } = "";
        public string UserID { get; set; } = "";
        public string Password { get; set; } = "";
        public string ConnectionString { get; set; } = "";
        public string Host { get; set; } = "";
        public int Port { get; set; } = 0;
        public string Database { get; set; } = "";
        public string Parameters { get; set; } = "";
        public string SchemaName { get; set; } = "";
        public string OracleSIDorService { get; set; } = "";
        public char Delimiter { get; set; }
        public string Ext { get; set; } = "";
        public DataSourceType DatabaseType { get; set; }
        public DatasourceCategory Category { get; set; }
        public string DriverName { get; set; } = "";
        public string DriverVersion { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string FileName { get; set; } = "";
        public bool Drawn { get; set; } = false;
        public string CertificatePath { get; set; } = "";
        public string Url { get; set; } = "";
        public List<string> Databases { get; set; } = new List<string>();
        public  string ApiKey { get; set; }
        public List<EntityStructure> Entities  {get ;  set ;} = new List<EntityStructure>();
        public string KeyToken { get; set; } = "";
        public List<WebApiHeader>  Headers { get; set; } = new List<WebApiHeader>();
        public bool CompositeLayer { get; set; } = false;
        public string CompositeLayerName { get; set; }
        public List<DefaultValue> DatasourceDefaults { get; set; } = new List<DefaultValue>();
        public ConnectionProperties()
        {
            
        }

     
    }
    public class WebApiHeader
    {
        public WebApiHeader()
        {

        }
        public string headername { get; set; }
        public string headervalue { get; set; }
    }

    public class ConnectionList
    {
        public ConnectionList()
        {

        }
        public List<ConnectionProperties> Connections { get; set; } = new List<ConnectionProperties>();
        public DatasourceCategory DataSourceCategory { get; set; }

    }
}
