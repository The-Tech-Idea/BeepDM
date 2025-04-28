using System;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Vis
{
    /// <summary>
    /// Attribute class to define metadata for add-ins within the system.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class AddinAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets URl of the add-in.
        /// </summary>
        public string Url { get; set; }
        /// <summary>
        /// Gets or sets the icon image of the add-in.
        /// </summary>
        public string Icon { get; set; }
        /// <summary>
        /// Gets or sets the unique identifier of the add-in.
        /// </summary>
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        /// <summary>
        /// Gets or sets the unique name of the add-in.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the caption or title for the add-in, usually for display purposes.
        /// </summary>
        public string Caption { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the add-in is hidden in the UI. Defaults to false.
        /// </summary>
        public bool Hidden { get; set; } = false;

        /// <summary>
        /// Gets or sets the path or identifier for the icon image of the add-in. Defaults to null.
        /// </summary>
        public string iconimage { get; set; } = null;

        /// <summary>
        /// Gets or sets the type of object the add-in is associated with.
        /// </summary>
        public string ObjectType { get; set; }

        /// <summary>
        /// Gets or sets the class type of the add-in.
        /// </summary>
        public string ClassType { get; set; }

        /// <summary>
        /// Gets or sets a unique integer key to identify the add-in. Defaults to -1.
        /// </summary>
        public int key { get; set; } = -1;

        /// <summary>
        /// Gets or sets additional miscellaneous data associated with the add-in.
        /// </summary>
        public string misc { get; set; }

        /// <summary>
        /// Gets or sets an integer key identifying the parent of this add-in, if applicable. Defaults to -1.
        /// </summary>
        public int parentkey { get; set; } = -1;

        /// <summary>
        /// Gets or sets the name of the menu or menu item the add-in is associated with.
        /// </summary>
        public string menu { get; set; }

        /// <summary>
        /// Gets or sets the display order of the add-in.
        /// </summary>
        public int order { get; set; }

        /// <summary>
        /// Gets or sets the display type of the add-in, defined by the DisplayType enum.
        /// </summary>
        public DisplayType displayType { get; set; } = DisplayType.InControl;

        /// <summary>
        /// Gets or sets the add-in type, defined by the AddinType enum.
        /// </summary>
        public AddinType addinType { get; set; } = AddinType.Form;

        /// <summary>
        /// Gets or sets where the add-in should be shown, defined by the ShowinType enum.
        /// </summary>
        public ShowinType Showin { get; set; } = ShowinType.Both;

        /// <summary>
        /// Gets or sets the data source category, defined by the DatasourceCategory enum. Defaults to NONE.
        /// </summary>
        public DatasourceCategory Category { get; set; } = DatasourceCategory.NONE;

        /// <summary>
        /// Gets or sets the data source type, defined by the DataSourceType enum. Defaults to NONE.
        /// </summary>
        public DataSourceType DatasourceType { get; set; } = DataSourceType.NONE;

        /// <summary>
        /// Gets or sets the file type or data format the add-in is associated with.
        /// </summary>
        public string FileType { get; set; }

        /// <summary>
        /// Gets or sets the type name of the data returned by the add-in.
        /// </summary>
        public string returndataTypename { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the add-in should be created by default.
        /// </summary>
        public bool DefaultCreate { get; set; }

        /// <summary>
        /// Gets or sets a detailed description of the add-in.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the add-in represents a home page or primary interface component.
        /// </summary>
        public bool IsHomePage { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public EnumPointType BranchType { get; set; }= EnumPointType.Global;
        public AddinScopeCreateType ScopeCreateType { get; set; } = AddinScopeCreateType.Single;
        public string ClassID { get; set; }  // Class ID from the addin usual guid generated
        public string ClassName { get; set; }  // Class Name from the addin
        public bool IsClassDistinct { get; set; } = false; // if true the class is distinct and not a part of the addin
    }

}
