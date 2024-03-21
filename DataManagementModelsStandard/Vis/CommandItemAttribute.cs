using System;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.Vis
{
    /// <summary>
    /// Attribute class to define metadata for commands within the system.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class CommandAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the unique name of the command.
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// Gets or sets the caption or title for the command, usually for display purposes.
        /// </summary>
        public string Caption { get; set; } = null;

        /// <summary>
        /// Gets or sets a value indicating whether the command is hidden in the UI. Defaults to false.
        /// </summary>
        public bool Hidden { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the command is triggered on a single click. Defaults to false.
        /// </summary>
        public bool Click { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the command is triggered on a double click. Defaults to false.
        /// </summary>
        public bool DoubleClick { get; set; } = false;

        /// <summary>
        /// Gets or sets the path or identifier for the icon image of the command. Defaults to null.
        /// </summary>
        public string iconimage { get; set; } = null;

        /// <summary>   
        /// Gets or sets the point type for the command, defined by the EnumPointType enum.
        /// </summary>
        public EnumPointType PointType { get; set; } = EnumPointType.Global;

        /// <summary>
        /// Gets or sets the type of object the command is associated with.
        /// </summary>
        public string ObjectType { get; set; } = null;

        /// <summary>
        /// Gets or sets the class type of the command.
        /// </summary>
        public string ClassType { get; set; } = null;

        /// <summary>
        /// Gets or sets additional miscellaneous data associated with the command.
        /// </summary>
        public string misc { get; set; } = null;

        /// <summary>
        /// Gets or sets the data source category, defined by the DatasourceCategory enum. Defaults to NONE.
        /// </summary>
        public DatasourceCategory Category { get; set; } = DatasourceCategory.NONE;

        /// <summary>
        /// Gets or sets the data source type, defined by the DataSourceType enum. Defaults to NONE.
        /// </summary>
        public DataSourceType DatasourceType { get; set; } = DataSourceType.NONE;

        /// <summary>
        /// Gets or sets where the command should be shown, defined by the ShowinType enum. Defaults to Both.
        /// </summary>
        public ShowinType Showin { get; set; } = ShowinType.Both;

        /// <summary>
        /// Gets or sets a value indicating whether the command is left-aligned in the UI. Defaults to true.
        /// </summary>
        public bool IsLeftAligned { get; set; } = true;

        /// <summary>
        /// Gets or sets the display order of the command. Defaults to 0.
        /// </summary>
        public int Order { get; set; } = 0;
    }

}
