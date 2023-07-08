using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.DataBase
{
    public class QuerySqlRepo
    {
        public int ID { get; set; }
        public string GuidID { get; set; } 
        public DataSourceType DatabaseType { get; set; }
        public Sqlcommandtype Sqltype { get; set; }
        public string Sql { get; set; }

        public QuerySqlRepo()
        {
            GuidID = Guid.NewGuid().ToString();
        }
        public QuerySqlRepo(DataSourceType dataSourceType,string sql, Sqlcommandtype sqltype)

        {
            GuidID = Guid.NewGuid().ToString();
            DatabaseType = dataSourceType;
            Sql = sql;
            Sqltype = sqltype;
        }


    }
  
}
