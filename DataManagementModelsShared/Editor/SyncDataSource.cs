using System;
using System.Collections.Generic;
using System.Text;

namespace TheTechIdea.DataManagment_Engine.Editor
{
    public class SyncDataSource
    {
        public SyncDataSource()
        {
            id = Guid.NewGuid().ToString();
        }
        public string id { get; set; }
        public string datasourcename { get; set; }
        public List<SyncEntity> syncedentities {get;set;}
    }
    public class SyncEntity
    {
        public SyncEntity()
        {
            id = Guid.NewGuid().ToString();
        }
        public string id { get; set; }
        public string sourcedatasourcename { get; set; }
        public string sourceentityname { get; set; }
        public string sourceDatasourceEntityName { get; set; }

        public string entityname { get; set; }
        public string originalentityName { get; set; }
        public DateTime lastupdate { get; set; }
        public List<SyncErrors> syncErrors { get; set; }
      
    }
    public class SyncErrors
    {
        public SyncErrors(string pentityname, DateTime dateTime,string pscript)
        {
            id = Guid.NewGuid().ToString();
            entityname = pentityname;
            errorupdate = dateTime;
            script = pscript;
        }
        public string id { get; set; }
        public string entityname { get; set; }
        public DateTime errorupdate { get; set; }
        public string script { get; set; }
    }
}
