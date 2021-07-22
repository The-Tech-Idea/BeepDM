using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Beep.Vis;

namespace TheTechIdea.Beep.Addin
{
   public  class AddinVisSchema: IAddinVisSchema
    {
        public AddinVisSchema()
        {

        }

        public string RootNodeName { get; set; }
        public string CatgoryName { get; set; }
        public string AddinName { get; set; }
        public int Order { get; set; }
        public int ID { get; set; }
        public string Name { get; set; }
        public string BranchText { get; set; }
        public int Level { get; set; }
        public EnumPointType BranchType { get; set; }
        public int BranchID { get; set; }
        public string IconImageName { get; set; }
        public string BranchStatus { get; set; }
        public int ParentBranchID { get; set; }
        public string BranchDescription { get; set; }
        public string BranchClass { get; set; } = "ADDIN";
    }
}
