﻿using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.Editor
{
    public interface ISyncEntity
    {
        int id { get; set; }
        string ddl { get; set; }
        string sourceentityname { get; set; }
        string sourceDatasourceEntityName { get; set; }
        string destinationdatasourcename { get; set; }
        string sourcedatasourcename { get; set; }
         string destinationentityname { get; set; }
         string destinationDatasourceEntityName { get; set; }
        string errormessage { get; set; }
        bool Active { get; set; }
        IErrorsInfo errorsInfo { get; set; }
        DDLScriptType scriptType { get; set; }
        List<SyncErrorsandTracking> Tracking { get; set; }
        List<SyncEntity> CopyDataScripts { get; set; } 
    }
    public enum DDLScriptType
    {
        CopyEntities,SyncEntity,CompareEntity,CreateEntity,AlterPrimaryKey,AlterFor,AlterUni,DropTable,EnableCons,DisableCons,CopyData
    }
}
