using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.FileManager
{
    /// <summary>
    /// Represents a root folder for a project, containing subfolders and metadata.
    /// </summary>
    [Serializable]
    public class RootFolder : IProject, IObjectCommon, IEquatable<RootFolder>
    {
        /// <summary>
        /// Gets or sets the type of project folder (e.g., Files, Project).
        /// </summary>
        public ProjectFolderType FolderType { get; set; }

        /// <summary>
        /// Gets or sets the list of subfolders within the project.
        /// </summary>
        public List<Folder> Folders { get; set; } = new List<Folder>();

        /// <summary>
        /// Gets or sets the unique identifier for the project.
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// Gets the globally unique identifier for the project.
        /// </summary>
        public string GuidID { get; private set; }

        /// <summary>
        /// Gets or sets the name of the project.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the file extension associated with the project.
        /// </summary>
        public string Ext { get; set; }

        /// <summary>
        /// Gets or sets the tags associated with the project.
        /// </summary>
        public string Tags { get; set; }

        /// <summary>
        /// Gets or sets the URL or path of the project folder.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the version of the project.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the project is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the icon associated with the project.
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// Gets or sets the description of the project.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the description of the project version.
        /// </summary>
        public string VersionDescription { get; set; }

        /// <summary>
        /// Gets or sets the description of the project author.
        /// </summary>
        public string AuthorDescription { get; set; }

        /// <summary>
        /// Gets or sets the author of the project.
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// Gets or sets the date the project was added.
        /// </summary>
        public DateTime AddedDate { get; set; }

        /// <summary>
        /// Gets or sets the date the project was last modified.
        /// </summary>
        public DateTime LastModifiedDate { get; set; }

        /// <summary>
        /// Gets or sets the author who last modified the project.
        /// </summary>
        public string ModificationAuther { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the project is private.
        /// </summary>
        public bool IsPrivate { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RootFolder"/> class.
        /// </summary>
        public RootFolder()
        {
            GuidID = Guid.NewGuid().ToString();
            AddedDate = DateTime.Now;
            IsActive = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RootFolder"/> class with a specified URL.
        /// </summary>
        /// <param name="url">The URL or path of the project folder.</param>
        /// <exception cref="ArgumentException">Thrown if URL is null or empty.</exception>
        public RootFolder(string url)
            : this()
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException("Url cannot be null or empty", nameof(url));
            Url = url;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RootFolder"/> class with a specified name and URL.
        /// </summary>
        /// <param name="name">The name of the project.</param>
        /// <param name="url">The URL or path of the project folder.</param>
        /// <exception cref="ArgumentException">Thrown if name or URL is null or empty.</exception>
        public RootFolder(string name, string url)
            : this()
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name cannot be null or empty", nameof(name));
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException("Url cannot be null or empty", nameof(url));
            Name = name;
            Url = url;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RootFolder"/> class with detailed properties.
        /// </summary>
        /// <param name="author">The author of the project.</param>
        /// <param name="description">The description of the project.</param>
        /// <param name="folders">The list of subfolders.</param>
        /// <param name="name">The name of the project.</param>
        /// <param name="tags">The tags associated with the project.</param>
        /// <param name="url">The URL or path of the project folder.</param>
        /// <param name="version">The version of the project.</param>
        /// <exception cref="ArgumentException">Thrown if name or URL is null or empty.</exception>
        public RootFolder(string author, string description, List<Folder> folders, string name, string tags, string url, string version)
            : this()
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name cannot be null or empty", nameof(name));
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException("Url cannot be null or empty", nameof(url));
            Author = author;
            Description = description;
            Folders = folders ?? new List<Folder>();
            Name = name;
            Tags = tags;
            Url = url;
            Version = version;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current <see cref="RootFolder"/>.
        /// </summary>
        /// <param name="other">The object to compare with the current object.</param>
        /// <returns>True if the specified object is equal to the current object; otherwise, false.</returns>
        public bool Equals(RootFolder other)
        {
            if (other == null)
                return false;
            return GuidID == other.GuidID;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current <see cref="RootFolder"/>.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>True if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as RootFolder);
        }

        /// <summary>
        /// Returns a hash code for the current <see cref="RootFolder"/>.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return GuidID?.GetHashCode() ?? 0;
        }

        /// <summary>
        /// Sets the author of the project and returns the current instance.
        /// </summary>
        /// <param name="author">The author to set.</param>
        /// <returns>The current <see cref="RootFolder"/> instance.</returns>
        public RootFolder WithAuthor(string author)
        {
            Author = author;
            return this;
        }

        /// <summary>
        /// Sets the description of the project and returns the current instance.
        /// </summary>
        /// <param name="description">The description to set.</param>
        /// <returns>The current <see cref="RootFolder"/> instance.</returns>
        public RootFolder WithDescription(string description)
        {
            Description = description;
            return this;
        }
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
        public List<Folder> Folders { get; set; }=new List<Folder>();
        public List<FFile> Files { get; set; } = new List<FFile>();
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
