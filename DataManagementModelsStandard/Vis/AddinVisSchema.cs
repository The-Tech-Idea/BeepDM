using System;
using TheTechIdea.Beep.Vis;

namespace TheTechIdea.Beep.Addin
{

    /// <summary>
    /// Attribute class to define the visualization schema for add-ins within the system.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class AddinVisSchema : Attribute, IAddinVisSchema
    {
        /// <summary>
        /// Gets or sets the root node name in the visualization schema.
        /// </summary>
        public string RootNodeName { get; set; }

        /// <summary>
        /// Gets or sets the category name for the add-in.
        /// </summary>
        public string CatgoryName { get; set; }

        /// <summary>
        /// Gets or sets the name of the add-in.
        /// </summary>
        public string AddinName { get; set; }

        /// <summary>
        /// Gets or sets the display order of the add-in in the visualization schema.
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Gets or sets the identifier for the add-in.
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// Gets or sets the name of the add-in's visualization branch.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the text displayed on the branch.
        /// </summary>
        public string BranchText { get; set; }

        /// <summary>
        /// Gets or sets the level of the branch in the visualization schema.
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Gets or sets the type of the branch, defined by the EnumPointType enum.
        /// </summary>
        public EnumPointType BranchType { get; set; }

        /// <summary>
        /// Gets or sets the identifier for the branch.
        /// </summary>
        public int BranchID { get; set; }

        /// <summary>
        /// Gets or sets the name of the icon image associated with the branch.
        /// </summary>
        public string IconImageName { get; set; }

        /// <summary>
        /// Gets or sets the status of the branch.
        /// </summary>
        public string BranchStatus { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the parent branch, if applicable.
        /// </summary>
        public int ParentBranchID { get; set; }

        /// <summary>
        /// Gets or sets a description for the branch.
        /// </summary>
        public string BranchDescription { get; set; }

        /// <summary>
        /// Gets or sets the class associated with the branch. Defaults to "ADDIN".
        /// </summary>
        public string BranchClass { get; set; } = "ADDIN";
    }

}
