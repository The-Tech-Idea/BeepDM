using System;
using System.Collections.Generic;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.FileManager
{
    public interface IProject: IObjectCommon
    {
        List<IFolder> Folders { get; set; }
    }
    public interface IFolder: IObjectCommon
    {
        List<IFile> Files { get; set; }
        List<IFolder> Folders { get; set; }
    }
    public interface IFile: IObjectCommon
    {
      
    }
    public interface IObjectCommon
    {
        int ID { get; set; }
        string GuidID { get; set; }
        string Name { get; set; }
        string Ext { get; set; }
        string Tags { get; set; }
        string Url { get; set; }
        string Version { get; set; }
        bool IsActive { get; set; }
        string Icon { get; set; }
        string Description { get; set; }
        string VersionDescription { get; set; }
        string AuthorDescription { get; set; }
        string Author { get; set; }
        DateTime AddedDate { get; set; }
        DateTime LastModifiedDate { get; set; }
        string ModificationAuther { get; set; }
        bool IsPrivate { get; set; }
    }
        
}