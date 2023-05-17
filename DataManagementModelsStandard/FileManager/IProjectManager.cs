using System.Collections.Generic;

namespace TheTechIdea.Beep.FileManager
{
    public interface IProjectManager
    {
        string Author { get; set; }
        string Description { get; set; }
        List<string> Files { get; }
        List<string> Folders { get; }
        string Name { get; set; }
        List<string> Tags { get; set; }
        string Url { get; set; }
        string Version { get; set; }
    }
}