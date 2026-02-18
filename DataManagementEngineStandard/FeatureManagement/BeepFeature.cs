using Microsoft.Extensions.DependencyInjection;

using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Environments;


namespace TheTechIdea.Beep.Container.FeatureManagement
{
    public class BeepFeature : IBeepFeature
    {
        public bool IsEnabled { get; set; } = false;
        public bool IsLoaded { get; set; } = false;
        public bool IsConfigured { get; set; } = false;
        public bool IsSingleton { get; set; } = false;
        public bool IsScoped { get; set; } = false;
        public bool IsTransient { get; set; } = false;

        public string Name { get; set; }
        public string Description { get; set; }
        public string GuidID { get; set; }
        public string FeatureID { get; set; }
        public string Version { get; set; }
        public AssemblyClassDefinition AssemblyDefinition { get; set; }
        public string AssemblyName { get; set; }
        public string AssemblyDescription { get; set; }
        public string AssemblyVersion { get; set; } = string.Empty;
        public IBeepAppRepo Container { get  ; set  ; }

        public BeepFeature() { }
        public BeepFeature(string name)
        {
            Name = name;
        }
        public BeepFeature(string name, string description)
        {
            Name = name;
            Description = description;
        }
        public BeepFeature(string name, string description, string guidID)
        {
            Name = name;
            Description = description;
            GuidID = guidID;
        }
        public BeepFeature(string name, string description, string guidID, string featureID)
        {
            Name = name;
            Description = description;
            GuidID = guidID;
            FeatureID = featureID;
        }
        public BeepFeature(string name, string description, string guidID, string featureID, string version)
        {
            Name = name;
            Description = description;
            GuidID = guidID;
            FeatureID = featureID;
            Version = version;
        }
        public BeepFeature(string name, string description, string guidID, string featureID, string version, AssemblyClassDefinition assemblyDefinition)
        {
            Name = name;
            Description = description;
            GuidID = guidID;
            FeatureID = featureID;
            Version = version;
            AssemblyDefinition = assemblyDefinition;
        }
        public BeepFeature(string name, string description, string guidID, string featureID, string version, AssemblyClassDefinition assemblyDefinition, string assemblyName)
        {
            Name = name;
            Description = description;
            GuidID = guidID;
            FeatureID = featureID;
            Version = version;
            AssemblyDefinition = assemblyDefinition;
            AssemblyName = assemblyName;
        }
        public BeepFeature(string name, string description, string guidID, string featureID, string version, AssemblyClassDefinition assemblyDefinition, string assemblyName, string assemblyDescription)
        {
            Name = name;
            Description = description;
            GuidID = guidID;
            FeatureID = featureID;
            Version = version;
            AssemblyDefinition = assemblyDefinition;
            AssemblyName = assemblyName;
            AssemblyDescription = assemblyDescription;
        }
        public BeepFeature(string name, string description, string guidID, string featureID, string version, AssemblyClassDefinition assemblyDefinition, string assemblyName, string assemblyDescription, string assemblyVersion)
        {
            Name = name;
            Description = description;
            GuidID = guidID;
            FeatureID = featureID;
            Version = version;
            AssemblyDefinition = assemblyDefinition;
            AssemblyName = assemblyName;
            AssemblyDescription = assemblyDescription;
            AssemblyVersion = assemblyVersion;
        }
        public IErrorsInfo Configure()
        {
            return null;
        }
        public IErrorsInfo AddAsService(IServiceCollection services, ServiceScope scope = ServiceScope.Singleton)
        {

            return null;
        }
        public IErrorsInfo AddAsService(IServiceCollection services, string ServiceName, ServiceScope scope = ServiceScope.Singleton)
        {
            return null;
        }
        public IErrorsInfo AddAsService(IServiceCollection services, string ServiceName, int key, ServiceScope scope = ServiceScope.Singleton)
        {
            return null;
        }

        public IErrorsInfo Run()
        {
            return new ErrorsInfo();
        }
    }
}
