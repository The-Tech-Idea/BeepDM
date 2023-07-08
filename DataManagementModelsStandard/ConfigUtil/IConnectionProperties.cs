using DataManagementModels.ConfigUtil;
using System;
using System.Collections.Generic;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Util
{
    public interface IConnectionProperties
    {
         int ID { get; set; }
         string GuidID { get; set; }
        string ConnectionName { get; set; }
        string ConnectionString { get; set; }
        string Database { get; set; }
        string OracleSIDorService { get; set; }
        DataSourceType DatabaseType { get; set; }
        DatasourceCategory Category { get; set; }
         string DriverName { get; set; }
         string DriverVersion { get; set; }
        string Host { get; set; }
      
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
        bool Favourite { get; set; }

       
         bool IsLocal { get; set; } 


         bool IsRemote { get; set; } 


         bool IsWebApi { get; set; } 


         bool IsFile { get; set; }


         bool IsDatabase { get; set; }


         bool IsComposite { get; set; }


         bool IsCloud { get; set; } 


         bool IsFavourite { get; set; }


         bool IsDefault { get; set; } 


         bool IsInMemory { get; set; } 
    }
}