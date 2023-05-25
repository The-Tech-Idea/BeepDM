using System.Collections.Generic;

namespace TheTechIdea.Beep.FileManager
{
    public interface IProjectFolder


    {
        int ID { get; set; }
        string GuidID { get; set; }
        string Author { get; set; }
        string Description { get; set; }
        List<IFolderFiles> Folders { get; }
        string Name { get; set; }
        List<string> Tags { get; set; }
        string Url { get; set; }
        string Version { get; set; }
    }
    public interface IFolderFiles
    {
        string Name { get; set; }
        List<string> Files { get; }

    }
}