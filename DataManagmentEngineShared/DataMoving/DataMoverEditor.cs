using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Workflow;

namespace TheTechIdea.DataManagment_Engine.DataMoving
{
    public class DataMoverEditor
    {
        public IDMEEditor dMEEditor { get; set; }
        public DataMoverEditor(IDMEEditor pdMEEditor)
        {
            dMEEditor = pdMEEditor;

        }
        public IMapping_rep MappingData { get; set; }
     


    }
}
