
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.DataManagment_Engine.ConfigUtil;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.DataBase
{
    [ClassProperties(Category = DatasourceCategory.RDBMS, DatasourceType = DataSourceType.Mysql)]
    public class MySQLDataSource : RDBSource
    {

       
        public MySQLDataSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per) : base(datasourcename, logger, DMEEditor, databasetype, per)
        {
        }
        public override string DisableFKConstraints( EntityStructure t1)
        {
            try
            {
                this.ExecuteSql($"SET FOREIGN_KEY_CHECKS=0;");
                DMEEditor.ErrorObject.Message = "successfull Disabled Mysql FK Constraints";
                DMEEditor.ErrorObject.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", "Diabling Mysql FK Constraints" + ex.Message, DateTime.Now, 0, t1.EntityName, Errors.Failed);
            }
            return DMEEditor.ErrorObject.Message;
        }

        public override string EnableFKConstraints( EntityStructure t1)
        {
            try
            {
                this.ExecuteSql($"SET FOREIGN_KEY_CHECKS=1;");
                DMEEditor.ErrorObject.Message = "successfull Enabled Mysql FK Constraints";
                DMEEditor.ErrorObject.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", "Enabing Mysql FK Constraints" + ex.Message, DateTime.Now, 0, t1.EntityName, Errors.Failed);
            }
            return DMEEditor.ErrorObject.Message;
        }
    }
}
