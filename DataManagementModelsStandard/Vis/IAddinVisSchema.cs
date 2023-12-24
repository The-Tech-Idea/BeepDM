

namespace TheTechIdea.Beep.Vis
{
    public interface IAddinVisSchema 
    {
        string RootNodeName { get; set; }
        string CatgoryName { get; set; }
         string AddinName { get; set; }
         int Order { get; set; }
         int ID { get; set; }
         string Name { get; set; }
         string BranchText { get; set; }
         int Level { get; set; }
         EnumPointType BranchType { get; set; }
         int BranchID { get; set; }
         string IconImageName { get; set; }
         string BranchStatus { get; set; }
         int ParentBranchID { get; set; }
         string BranchDescription { get; set; }
         string BranchClass { get; set; } 
    }
}
