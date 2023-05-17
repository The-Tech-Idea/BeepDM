using System;
using System.Collections.Generic;
using System.Text;

namespace TheTechIdea.Beep.FileManager
{
    public partial class ProjectManager : IProjectManager
    {
        public ProjectManager()
        {

        }
        public ProjectManager(string name)
        {
            Name = name;
        }
        public ProjectManager(string name, string url)
        {
            Name = name;
            Url = url;
        }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Version { get; set; }
        public string Author { get; set; }
        public string Url { get; set; }
        public List<string> Tags { get; set; }
        public List<string> Folders { get; }
        public List<string> Files { get; }

    }
}
