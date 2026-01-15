
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.ConfigUtil;
using System.Collections.Generic;
using TheTechIdea.Beep.Environments;
using System;
using System.Threading.Tasks;


namespace TheTechIdea.Beep.Services
{
    public interface IBeepService
    {
      
        IConfigEditor Config_editor { get; set; }
        IDMEEditor DMEEditor { get; set; }
        IErrorsInfo Erinfo { get; set; }
        IJsonLoader jsonLoader { get; set; }
        IDMLogger lg { get; set; }
        IAssemblyHandler LLoader { get; set; }
        void LoadConfigurations(string containername);
      //  IServiceCollection Services { get; }
        void LoadServicesScoped();
        void LoadServicesSingleton();
        IUtil util { get; set; }
        string  Containername { get; }
        BeepConfigType ConfigureationType { get; }
        string BeepDirectory { get; }
        void Configure(string directorypath, string containername, BeepConfigType configType,bool AddasSingleton=false);
       
        void LoadAssemblies(Progress<PassedArgs> progress);
        Task LoadAssembliesAsync(Progress<PassedArgs> progress);
        void LoadAssemblies();
        void ConfigureForDesignTime();
        Dictionary<EnvironmentType, IBeepEnvironment> Environments { get; set; }
        void LoadEnvironments();
        void SaveEnvironments();
        void Dispose();

    }
}