using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Workflow.Mapping;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.Workflow
{
    public class Map_Schema : IMap_Schema
    {

        public string Id { get; set; }
        public string SchemaName { get; set; }
        public string Description { get; set; }
        public List<EntityDataMap> Maps { get; set; } = new List<EntityDataMap>();
        public Map_Schema()
        {
            Id = Guid.NewGuid().ToString();
        }
    }
   
}
