using System.Collections.Generic;
using TheTechIdea.DataManagment_Engine.DataBase;

namespace TheTechIdea.Util
{
    public interface IConnectionProperties
    {
        string ConnectionName { get; set; }
        string ConnectionString { get; set; }
        string Database { get; set; }
        string OracleSIDorService { get; set; }
        DataSourceType DatabaseType { get; set; }
        DatasourceCategory Category { get; set; }
         string DriverName { get; set; }
         string DriverVersion { get; set; }
        string Host { get; set; }
        int ID { get; set; }
        string Parameters { get; set; }
        string Password { get; set; }
        int Port { get; set; }
        string SchemaName { get; set; }
        string UserID { get; set; }

         string FilePath { get; set; }
         string FileName { get; set; }
         string Ext { get; set; }
          bool Drawn { get; set; }
        string CertificatePath { get; set; }
         string Url { get; set; }
         string KeyToken { get; set; }
        string ApiKey { get; set; }
        List<string> Databases { get; set; }
        List<EntityStructure> Entities { get; set; }
         List<WebApiHeader> Headers { get; set; }
        List<DefaultValue> DatasourceDefaults { get; set; }
        char Delimiter { get; set; }
        bool CompositeLayer { get; set; }
    }
}