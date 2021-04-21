using System;
using System.Collections.Generic;
using System.Text;

namespace TheTechIdea.DataManagment_Engine.Vis
{
    public sealed class BranchDelegate : Attribute
    {
        public  string Caption { get; set; }
        public bool Hidden { get; set; } = false;
        public bool Click { get; set; } = false;
        public bool DoubleClick { get; set; } = false;
        public string iconimage { get; set; } = null;
    }
}
