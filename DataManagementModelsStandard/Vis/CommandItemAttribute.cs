using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.Vis
{
    public sealed class CommandAttribute : Attribute
    {
        public string Name { get; set; } = null;
        public  string Caption { get; set; }=null;
        public bool Hidden { get; set; } = false;
        public bool Click { get; set; } = false;
        public bool DoubleClick { get; set; } = false;
        public string iconimage { get; set; } = null;
        public EnumPointType PointType { get; set; }
        public string ObjectType { get; set; } = null;
        public string ClassType { get; set; } = null;
        public string misc { get; set; } = null;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.NONE; 
        public DataSourceType DatasourceType { get; set; } = DataSourceType.NONE;
        public ShowinType Showin { get; set; } = ShowinType.Both;
        public bool IsLeftAligned { get; set; } = true;
    }
}
