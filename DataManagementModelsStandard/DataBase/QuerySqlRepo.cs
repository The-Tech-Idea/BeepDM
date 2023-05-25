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
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public DataSourceType DatabaseType { get; set; }
        public Sqlcommandtype Sqltype { get; set; }
        public string Sql { get; set; }

        public QuerySqlRepo()
        {

        }
        public QuerySqlRepo(DataSourceType dataSourceType,string sql, Sqlcommandtype sqltype)

        {
            DatabaseType = dataSourceType;
            Sql = sql;
            Sqltype = sqltype;
        }


    }
    //public class Sqlcommandtype
    //{
    //    public Sqlcommandtype()
    //    {

    //    }
    //    public string CommandType { get; set; }
       
    //}
    //public class DataSourceType
    //{
    //    public DataSourceType()
    //    {

    //    }
    //    public string Datasourcetype { get; set; }

    //}
}
