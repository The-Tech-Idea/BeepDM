
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Environments
{
    public interface IBeepFeature
    {
        AssemblyClassDefinition AssemblyDefinition { get; set; }
        string AssemblyDescription { get; set; }
        string AssemblyName { get; set; }
        string AssemblyVersion { get; set; }
        string Description { get; set; }
        string FeatureID { get; set; }
        string GuidID { get; set; }
        bool IsConfigured { get; set; }
        bool IsEnabled { get; set; }
        bool IsLoaded { get; set; }
        bool IsScoped { get; set; }
        bool IsSingleton { get; set; }
        bool IsTransient { get; set; }
        string Name { get; set; }
        string Version { get; set; }

     
        IBeepContainer Container { get; set; }
        IErrorsInfo Configure();
        IErrorsInfo Run();
    }
}