using System;
using System.Collections.Generic;
using System.Text;

namespace TheTechIdea.Beep.FileManager
{
    public class ProjectFolder : IProjectFolder
    {
        public ProjectFolder()
        {
            
        }
        public ProjectFolder(string url)
        {
            Url = url;
        }
        public ProjectFolder(string name,string url)
        {
            Name = name;
            Url = url;
        }
        public ProjectFolder(string author, string description, List<IFolderFiles> folders, string name, List<string> tags, string url, string version)
        {
            Author = author;
            Description = description;
            Folders = folders;
            Name = name;
            Tags = tags;
            Url = url;
            Version = version;
        }

        public string Author { get  ; set  ; }
        public string Description { get  ; set  ; }

        public List<IFolderFiles> Folders { get; set; }=new List<IFolderFiles>();
        public List<string> Files { get; set; } = new List<string>();
        public string Name { get  ; set  ; }
        public List<string> Tags { get  ; set  ; }
        public string Url { get  ; set  ; }
        public string Version { get  ; set  ; }
    }
    public class FolderFiles : IFolderFiles
    {
        public string Name { get ; set ; }

        public List<string> Files { get; set; } = new List<string>();
    }
}
