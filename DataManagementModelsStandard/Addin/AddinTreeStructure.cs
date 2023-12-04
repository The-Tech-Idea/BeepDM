using System;
using System.Collections.Generic;
using System.Text;

namespace TheTechIdea.Beep.Addin
{
    public class AddinTreeStructure
    {
        public AddinTreeStructure()
        {

        }
        public int ID { get; set; }
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public string NodeName { get; set; }
        public string Imagename { get; set; }
        public int Order { get; set; }
        public string RootName { get; set; }
        public string className { get; set; }
        public string dllname { get; set; }
        public string PackageName { get; set; }
        public string ObjectType { get; set; }
        public string misc { get; set; }
        public string menu { get; set; }
        public string iconimage { get; set; }
        public string addinType { get; set; }
        public string Showin { get; set; }
        public string Roles { get; set; }
    }


}
