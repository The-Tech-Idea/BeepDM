using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.FileManager
{
    public class Project: IProject
    {
        public Project()
        {
            GuidID = Guid.NewGuid().ToString();
        }
        public Project(string url)
        {
            Url = url;
            GuidID = Guid.NewGuid().ToString();
        }
        public Project(string name,string url)
        {
            Name = name;
            Url = url;
            GuidID = Guid.NewGuid().ToString();
        }
        public Project(string author, string description, List<IFolder> folders, string name, string tags, string url, string version)
        {
            Author = author;
            Description = description;
            Folders = folders;
            Name = name;
            Tags = tags;
            Url = url;
            Version = version;
            GuidID = Guid.NewGuid().ToString();

        }

        public List<IFolder> Folders { get  ; set  ; }
        public int ID { get  ; set  ; }
        public string GuidID { get  ; set  ; }
        public string Name { get  ; set  ; }
        public string Ext { get  ; set  ; }
        public string Tags { get  ; set  ; }
        public string Url { get  ; set  ; }
        public string Version { get  ; set  ; }
        public bool IsActive { get  ; set  ; }
        public string Icon { get  ; set  ; }
        public string Description { get  ; set  ; }
        public string VersionDescription { get  ; set  ; }
        public string AuthorDescription { get  ; set  ; }
        public string Author { get  ; set  ; }
        public DateTime AddedDate { get  ; set  ; }
        public DateTime LastModifiedDate { get  ; set  ; }
        public string ModificationAuther { get  ; set  ; }
        public bool IsPrivate { get  ; set  ; }
    }
    public class Folder : IFolder
    {
        public Folder()
        {
            AddedDate = DateTime.Now;
            LastModifiedDate = DateTime.Now;
            IsActive = true;
            IsPrivate = true;
            GuidID = Guid.NewGuid().ToString();
        }
        public Folder(string url)
        {
            if (string.IsNullOrEmpty(url)) throw new ArgumentNullException("url");

            Url = url;
         
            AddedDate = DateTime.Now;
            LastModifiedDate = DateTime.Now;
            IsActive = true;
            IsPrivate = true;
            GuidID = Guid.NewGuid().ToString();
        }
        public int ID { get; set; }
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public string Name { get ; set ; }
        public List<IFolder> Folders { get; set; }=new List<IFolder>();
        public List<IFile> Files { get; set; } = new List<IFile>();
        public string Ext { get ; set ; }
        public string Tags { get ; set ; }
        public string Url { get ; set ; }
        public string Version { get ; set ; }
        public bool IsActive { get ; set ; }
        public string Icon { get ; set ; }
        public string Description { get ; set ; }
        public string VersionDescription { get ; set ; }
        public string AuthorDescription { get ; set ; }
        public string Author { get ; set ; }
        public DateTime AddedDate { get ; set ; }
        public DateTime LastModifiedDate { get ; set ; }
        public string ModificationAuther { get ; set ; }
        public bool IsPrivate { get ; set ; }
    }
    public class FFile : IFile
    {
        public int ID { get ; set ; }
        public string GuidID { get ; set ; }
        public string Name { get ; set ; }
        public string Tags { get ; set ; }
        public string Url { get ; set ; }
        public string Version { get ; set ; }
        public bool IsActive { get ; set ; }
        public string Icon { get ; set ; }
        public string Description { get ; set ; }
        public string VersionDescription { get ; set ; }
        public string AuthorDescription { get ; set ; }
        public string Author { get ; set ; }
        public DateTime AddedDate { get ; set ; }
        public DateTime LastModifiedDate { get ; set ; }
        public string ModificationAuther { get ; set ; }
        public bool IsPrivate { get ; set ; }=true;
        public string Ext { get; set; }
        public FFile()
        {
            AddedDate = DateTime.Now; 
            LastModifiedDate=DateTime.Now;
            IsActive = true;
            IsPrivate = true;
            GuidID = Guid.NewGuid().ToString();
        }
        public FFile(string url)
        {
            if(string.IsNullOrEmpty(url)) throw new ArgumentNullException("url");
            
            Url = url;
            Name = Path.GetFileName(Url);
            Ext=Path.GetExtension(Url);
            AddedDate = DateTime.Now;
            LastModifiedDate = DateTime.Now;
            IsActive = true;
            IsPrivate = true;
            GuidID = Guid.NewGuid().ToString();
        }
        
    }
}
