using System;
using System.Collections.Generic;
using System.Text;

namespace TheTechIdea.Beep.Vis
{
    public sealed class CommandAttribute : Attribute
    {
        public string Name { get; set; }
        public  string Caption { get; set; }
        public bool Hidden { get; set; } = false;
        public bool Click { get; set; } = false;
        public bool DoubleClick { get; set; } = false;
        public string iconimage { get; set; } = null;
        public EnumPointType PointType { get; set; }
        public string ObjectType { get; set; }
    }
}
