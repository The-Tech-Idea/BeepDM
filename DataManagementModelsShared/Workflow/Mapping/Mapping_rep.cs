using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.Workflow
{
    public class Mapping_rep : IMapping_rep
    {
        public int    id { get; set; }
        public string MappingName { get; set; }
        public string Description { get; set; }
        public string EntityName1 { get; set; }
        public string EntityName2 { get; set; }
        public string Entity1DataSource { get; set; }
        public string Entity2DataSource { get; set; }
        public List<EntityField> Entity1Fields { get; set; } = new List<EntityField>();
        public List<EntityField> Entity2Fields { get; set; } = new List<EntityField>();
        public List<Mapping_rep_fields> FldMapping { get; set; } = new List<Mapping_rep_fields>();
        public Mapping_rep()
        {

        }
    }
    public class Map_Schema : IMap_Schema
    {

        public string Id { get; set; }
        public string SchemaName { get; set; }
        public string Description { get; set; }
        public List<Mapping_rep> Maps { get; set; } = new List<Mapping_rep>();
        public Map_Schema()
        {
            Id = Guid.NewGuid().ToString();
        }
    }
   
}
