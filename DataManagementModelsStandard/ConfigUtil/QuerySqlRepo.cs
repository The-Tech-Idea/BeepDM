using System;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.ConfigUtil
{
    public class QuerySqlRepo : Entity
    {

        private int _id;
        public int ID
        {
            get { return _id; }
            set { SetProperty(ref _id, value); }
        }

        private string _guidid;
        public string GuidID
        {
            get { return _guidid; }
            set { SetProperty(ref _guidid, value); }
        }

        private DataSourceType _databasetype;
        public DataSourceType DatabaseType
        {
            get { return _databasetype; }
            set { SetProperty(ref _databasetype, value); }
        }

        private Sqlcommandtype _sqltype;
        public Sqlcommandtype Sqltype
        {
            get { return _sqltype; }
            set { SetProperty(ref _sqltype, value); }
        }

        private string _sql;
        public string Sql
        {
            get { return _sql; }
            set { SetProperty(ref _sql, value); }
        }

        public QuerySqlRepo()
        {
            GuidID = Guid.NewGuid().ToString();
        }
        public QuerySqlRepo(DataSourceType dataSourceType, string sql, Sqlcommandtype sqltype)

        {
            GuidID = Guid.NewGuid().ToString();
            DatabaseType = dataSourceType;
            Sql = sql;
            Sqltype = sqltype;
        }


    }

}
