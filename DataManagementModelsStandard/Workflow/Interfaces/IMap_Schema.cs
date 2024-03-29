﻿using System.Collections.Generic;
using System.ComponentModel;
using TheTechIdea.Beep.Workflow.Mapping;

namespace TheTechIdea.Beep.Workflow
{
    public interface IMap_Schema
    {
        string Description { get; set; }
        int Id { get; set; }
        List<EntityDataMap> Maps { get; set; }
        string SchemaName { get; set; }
        string GuidID { get; set; }
    }
}