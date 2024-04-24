using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Util;

namespace DataManagementModels.Addin
{
    public interface IImporter
    {
        void Import(string path);
        string ImporterName { get; }
        AssemblyClassDefinition AssemblyClass { get; set; }
        string methodname { get; set; }
        FileTypes DataSourceType { get; set; }
        string ImporterType { get; }
        string ImporterDescription { get; }
        string ImporterVersion { get; }
        string ImporterAuthor { get; }
        string ImporterAuthorContact { get; }
        string ImporterAuthorUrl { get; }
    }
    public interface IExporter
    {
        void Export(string path);
        string ExporterName { get; }
        AssemblyClassDefinition AssemblyClass { get; set; }
        string methodname { get; set; }
        FileTypes DataSourceType { get; set; }
        string ExporterType { get; }
        string ExporterDescription { get; }
        string ExporterVersion { get; }
        string ExporterAuthor { get; }
        string ExporterAuthorContact { get; }
        string ExporterAuthorUrl { get; }
    }
    
      
}
