using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.ConfigUtil
{
    public class ClassProperties : Attribute
    {
        public int ID { get; set; }
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public string Caption { get; set; }
        public DatasourceCategory Category { get; set; }
        public DataSourceType DatasourceType { get; set; }
        public string FileType { get; set; }
        public string iconimage { get; set; } = null;
        public string returndataTypename { get; set; }
        
    }
}
