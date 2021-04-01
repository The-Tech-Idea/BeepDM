using System.Collections.Generic;
using System.ComponentModel;

namespace TheTechIdea.DataManagment_Engine.Workflow
{
    public interface IMap_Schema
    {
        string Description { get; set; }
        int Id { get; set; }
        List<Mapping_rep> Maps { get; set; }
        string SchemaName { get; set; }
    }
}