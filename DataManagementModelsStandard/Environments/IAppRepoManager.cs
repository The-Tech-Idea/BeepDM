
using System.Collections.Generic;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Environments
{
    public interface IAppRepoManager
    {
        IBeepAppRepo CurrentRepoApp { get; set; }
        bool IsRepoAppActive { get; set; }
        bool IsRepoAppLoaded { get; set; } 
        bool IsRepoAppCreated { get; set; } 
        List<IBeepAppRepo> RepoApps { get; set; }
        ErrorsInfo ErrorsandMesseges { get; set; }
        string RepoAppFolderPath { get; set; }
        bool IsLogOn { get; set; }
        bool IsDataModified { get; set; }
        bool IsAssembliesLoaded { get; set; }
        bool IsBeepDataOn { get; set; }
        bool IsAppOn { get; set; }
        bool IsDevModeOn { get; set; }
        Task<ErrorsInfo> LoadRepoApps();
        Task<ErrorsInfo> SaveRepoApps();
        List<IBeepAppRepo> GetUserRepoApps(string owner);
        List<IBeepAppRepo> GetUserRepoApps(int id);
        List<IBeepAppRepo> GetUserRepoAppsByGuiID(string guidid);
        IBeepAppRepo GetUserPrimaryRepoApp(string owner);
        IBeepAppRepo GetBeepRepoApp(string RepoAppName);
        IBeepAppRepo GetBeepRepoAppByID(int RepoAppID);
        IBeepAppRepo GetBeepRepoAppByGuidID(string RepoAppGuidID);
        Task<ErrorsInfo> AddUpdateRepoApp(IBeepAppRepo pRepoApp);
        Task<ErrorsInfo> CreateRepoApp(string RepoAppGuidID, string owner,string ownerEmail,int ownerID,string ownerGuid,string pRepoAppName,  string pRepoAppFolderPath);
        Task<ErrorsInfo> CreateRepoApp(string RepoAppGuidID, string owner, string ownerEmail, int ownerID, string ownerGuid, string pRepoAppName,  string pRepoAppFolderPath, string pSecretKey, string pTokenKey);
        Task<ErrorsInfo> CreateRepoAppFileSystem(IBeepAppRepo pRepoApp);
        Task<ErrorsInfo> RemoveRepoApp(string RepoAppGuidID);
        void Dispose();
    }
}