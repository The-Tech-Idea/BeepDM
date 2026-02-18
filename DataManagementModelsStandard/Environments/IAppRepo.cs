

using System.Collections.Generic;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.Environments
{
    public interface IBeepAppRepo
    {
        
        IBeepService BeepService { get; set; }
        string AdminUserID { get; set; }
        string BeepAppRepoFolderPath { get; set; }
        int BeepAppRepoID { get; set; }
        string BeepAppRepoName { get; set; }
        string BeepAppRepoUrlPath { get; set; }
        List<string> Groups { get; set; }
        List<string> Products { get; set; }
        string GuidID { get; set; }
        List<string> Privileges { get; set; }
        List<string> Roles { get; set; }
        string SecretKey { get; set; }
        string TokenKey { get; set; }
        List<string> Users { get; set; }
         bool IsAdmin { get; set; }
         bool isActive { get;set; }
            bool IsPrimary { get;set; }
          List<string> Modules { get; set; }  
          List<string> Services { get; set; }  
          List<string> Assemblies { get; set; }  
          List<string> Configurations { get; set; }  
          List<string> DataSources { get; set; }  
          List<string> DataModels { get; set; }  
          List<string> DataEntities { get; set; }  
          List<string> DataViews { get; set; }  
          List<string> DataTransformations { get; set; }  
          List<string> DataExports { get; set; }  
          List<string> DataImports { get; set; }  
          List<string> DataConnections { get; set; }  
          List<string> DataConnectionsConfigurations { get; set; }  
          string Owner { get; set; }
          string OwnerEmail { get; set; }
          string OwnerGuidID { get; set; }
          int OwnerID { get; set; }
        bool IsBeepAppRepoActive { get; set; }
        bool IsBeepAppRepoLoaded { get; set; }
        bool IsBeepAppRepoCreated { get; set; }
    }
}
