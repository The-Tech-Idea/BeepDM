using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTechIdea.Util
{
    public class QuerySqlRepo
    {
        public DataSourceType DatabaseType { get; set; }
        public Sqlcommandtype Sqltype { get; set; }
        public string Sql { get; set; }

        public QuerySqlRepo()
        {

        }
        public QuerySqlRepo(DataSourceType dataSourceType, string sql, Sqlcommandtype sqltype)

        {
            DatabaseType = dataSourceType;
            Sql = sql;
            Sqltype = sqltype;
        }


    }
}
