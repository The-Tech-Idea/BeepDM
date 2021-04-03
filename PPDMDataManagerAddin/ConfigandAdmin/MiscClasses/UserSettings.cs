using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleODM.systemconfigandutil.MiscClasses
{
   public  class UserSettings
    {

       
            public string CODE_ID { get; set; }
            public string USERNAME { get; set; }
            public string LOGINID { get; set; }
            public string PASSWORD { get; set; }
            public string TEAM { get; set; }
            public string GROUP { get; set; }
            public string DIVISION { get; set; }
            public string DATABASENAME { get; set; }
            public string HOSTNAME { get; set; }
           public DbTypeEnum DATABASETYPE { get; set; }
            public string DBCONNTYPE { get; set; }
            public string SCHEMA_NAME { get; set; }
            public PPDMVERSION VERSION { get; set; }
            public bool LocalDb { get; set; } = false;

            public void LOADVALUESFROMDB()
            {
            }

            public UserSettings()
            {
            }
        
    }
}
