using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.Vis
{
    public sealed class AddinAttribute : Attribute
    {
        public string Name { get; set; }
        public string Caption { get; set; }
        public bool Hidden { get; set; } = false;
        public string iconimage { get; set; } = null;
        public string ObjectType { get; set; }
        public string ClassType { get; set; }
        public int key { get; set; } = -1;
        public string misc {get;set;}
        public int parentkey { get; set; } = -1;
        public string menu { get; set; }
        public int order { get; set; }
        public DisplayType displayType { get; set; } = DisplayType.InControl;
        public AddinType addinType { get; set; } = AddinType.Form;
        public ShowinType Showin { get; set; } = ShowinType.Both;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.NONE;
        public DataSourceType DatasourceType { get; set; } = DataSourceType.NONE;
        public string FileType { get; set; }
        public string returndataTypename { get; set; }
        public bool DefaultCreate { get; set; }
        public string Description { get; set; }


    }
    
}
