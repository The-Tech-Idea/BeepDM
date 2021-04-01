using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.DataBase
{

    public interface IQuerySqlRepo
    {
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
