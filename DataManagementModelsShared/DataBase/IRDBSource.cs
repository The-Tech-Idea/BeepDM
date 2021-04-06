
using TheTechIdea.Logger;
using System.Data;
using TheTechIdea.Util;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Dynamic;
using TheTechIdea.DataManagment_Engine.Workflow;

namespace TheTechIdea.DataManagment_Engine.DataBase
{
    public interface IRDBSource: IDataSource
    {
       // object GetEntity(string EntityName, string QueryString);
       // DataTable RunQuery(string qrystr);
       // IErrorsInfo ExecuteSql(string sql);
        DataTable GetTableSchema(string TableName);
        IDataReader GetDataReader(string querystring);
        List<ChildRelation> GetTablesFKColumnList(string tablename, string SchemaName, string Filterparamters);

       
        string DisableFKConstraints(EntityStructure t1);
        string EnableFKConstraints( EntityStructure t1);
        List<T> GetData<T>(string sql);
        Task SaveData<T>(string sql, T parameters);
        string GetSchemaName();


    }
}