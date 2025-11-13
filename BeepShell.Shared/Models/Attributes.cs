using System;

namespace BeepShell.Shared.Models
{
    /// <summary>
    /// Extension metadata attribute for discovery and validation
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ShellExtensionAttribute : Attribute
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string[] Dependencies { get; set; } = Array.Empty<string>();
        public string MinShellVersion { get; set; } = string.Empty;
        public string ConfigFileName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Command metadata attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ShellCommandAttribute : Attribute
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string[] Aliases { get; set; } = Array.Empty<string>();
        public bool RequiresConnection { get; set; }
    }
}
