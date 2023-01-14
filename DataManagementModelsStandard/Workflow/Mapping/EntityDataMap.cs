using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Workflow;

namespace TheTechIdea.Beep.Workflow.Mapping
{
    public class EntityDataMap
    {
        public int id { get; set; }
        public string MappingName { get; set; }
        public string Description { get; set; }
        public string EntityName { get; set; }     
        public string EntityDataSource { get; set; }
        public List<EntityField> EntityFields { get; set; } = new List<EntityField>();
        public List<EntityDataMap_DTL> MappedEntities { get; set; } = new List<EntityDataMap_DTL>();
     
        public EntityDataMap()
        {

        }
    }
    public class EntityDataMap_DTL
    {
        public EntityDataMap_DTL()
        {

        }
        public string EntityDataSource { get; set; }
        public List<AppFilter> Filter { get; set; } = new List<AppFilter>();
        public string EntityName { get; set; }
        public List<EntityField> EntityFields { get; set; } = new List<EntityField>();
        public List<EntityField> SelectedDestFields { get; set; } = new List<EntityField>();
        public List<Mapping_rep_fields> FieldMapping { get; set; } = new List<Mapping_rep_fields>();
    }
}
