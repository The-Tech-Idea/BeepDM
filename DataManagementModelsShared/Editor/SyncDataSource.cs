﻿using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Beep.Workflow.Mapping;

namespace TheTechIdea.Beep.Editor
{
    public class ETLScriptHDR
    {
        public ETLScriptHDR()
        {;
            id = 1;
        }
        public List<ETLScriptDet> ScriptDTL { get; set; } = new List<ETLScriptDet>();
        public string workflowFileId { get; set; }
        public string scriptSource { get; set; }
      
        public  int id { get; set; }


    }
}
