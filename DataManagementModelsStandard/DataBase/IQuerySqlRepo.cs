using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.DataBase
{

    public interface IQuerySqlRepo
    {
         int ID { get; set; }
         string GuidID { get; set; } 
        DataSourceType DatabaseType { get; set; }
        string Sql { get; set; }
        Sqlcommandtype Sqltype { get; set; }
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
