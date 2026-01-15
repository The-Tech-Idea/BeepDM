
using System.Collections.Generic;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Environments
{
    public interface ICantainerManager
    {
        IBeepContainer CurrentContainer { get; set; }
        bool IsContainerActive { get; set; }
        bool IsContainerLoaded { get; set; } 
        bool IsContainerCreated { get; set; } 
        List<IBeepContainer> Containers { get; set; }
        ErrorsInfo ErrorsandMesseges { get; set; }
        string ContainerFolderPath { get; set; }
        bool IsLogOn { get; set; }
        bool IsDataModified { get; set; }
        bool IsAssembliesLoaded { get; set; }
        bool IsBeepDataOn { get; set; }
        bool IsAppOn { get; set; }
        bool IsDevModeOn { get; set; }
        Task<ErrorsInfo> LoadContainers();
        Task<ErrorsInfo> SaveContainers();
        List<IBeepContainer> GetUserContainers(string owner);
        List<IBeepContainer> GetUserContainers(int id);
        List<IBeepContainer> GetUserContainersByGuiID(string guidid);
        IBeepContainer GetUserPrimaryContainer(string owner);
        IBeepContainer GetBeepContainer(string ContainerName);
        IBeepContainer GetBeepContainerByID(int ContainerID);
        IBeepContainer GetBeepContainerByGuidID(string ContainerGuidID);
        Task<ErrorsInfo> AddUpdateContainer(IBeepContainer pContainer);
        Task<ErrorsInfo> CreateContainer(string ContainerGuidID, string owner,string ownerEmail,int ownerID,string ownerGuid,string pContainerName,  string pContainerFolderPath);
        Task<ErrorsInfo> CreateContainer(string ContainerGuidID, string owner, string ownerEmail, int ownerID, string ownerGuid, string pContainerName,  string pContainerFolderPath, string pSecretKey, string pTokenKey);
        Task<ErrorsInfo> CreateContainerFileSystem(IBeepContainer pContainer);
        Task<ErrorsInfo> RemoveContainer(string ContainerGuidID);
        void Dispose();
    }
}