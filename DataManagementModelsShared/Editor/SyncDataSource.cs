using System;
using System.Collections.Generic;
using System.Text;

namespace TheTechIdea.Beep.Editor
{
    public class SyncDataSource
    {
        public SyncDataSource()
        {
            id = Guid.NewGuid().ToString();
        }
        public List<SyncEntity> Entities { get; set; } = new List<SyncEntity>();
        public string workflowFileId { get; set; }
        public string scriptSource { get; set; }
        public string mappingschemaFileId { get; set; }
        public string id { get; set; }


    }
}
