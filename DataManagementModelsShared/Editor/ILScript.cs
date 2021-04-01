using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.Editor
{
    public interface ILScript
    {
        string ddl { get; set; }
        string entityname { get; set; }
        string destinationdatasourcename { get; set; }
        string sourcedatasourcename { get; set; }
        string errormessage { get; set; }
        bool Active { get; set; }
        IErrorsInfo errorsInfo { get; set; }
        DDLScriptType scriptType { get; set; }
    }
    public enum DDLScriptType
    {
        CreateTable,AlterPrimaryKey,AlterFor,AlterUni,DropTable,EnableCons,DisableCons,CopyData
    }
}
