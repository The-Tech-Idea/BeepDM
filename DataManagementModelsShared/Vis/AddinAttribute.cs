using System;
using System.Collections.Generic;
using System.Text;

namespace TheTechIdea.DataManagment_Engine.Vis
{
    public sealed class AddinAttribute : Attribute
    {
        public string Name { get; set; }
        public string Caption { get; set; }
        public bool Hidden { get; set; } = false;
        public string iconimage { get; set; } = null;
        public string ObjectType { get; set; }
        public int key { get; set; } = -1;
        public string misc {get;set;}

      
    }
}
