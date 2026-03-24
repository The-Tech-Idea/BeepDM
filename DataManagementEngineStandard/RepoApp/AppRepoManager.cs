using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Container.Services;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Environments;

using TheTechIdea.Beep.Services;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.RepoApp
{
    public  class AppRepoManager : IAppRepoManager, IDisposable
    {
        public AppRepoManager()
        {

            
        }
        private IServiceCollection services;
        private bool disposedValue;

        public IBeepAppRepo CurrentRepoApp { get; set; }
        public bool IsRepoAppActive { get; set; } = false;
        public bool IsRepoAppLoaded { get; set; } = false;
        public bool IsRepoAppCreated { get; set; } = false;

        public bool IsLogOn { get; set; } = false;
        public bool IsDataModified { get; set; } = false;
        public bool IsAssembliesLoaded { get; set; } = false;
        public bool IsBeepDataOn { get; set; } = false;
        public bool IsAppOn { get; set; } = false;
        public bool IsDevModeOn { get; set; } = false;
        public string filename { get; set; }="containers.json";
        public AppRepoManager(IServiceCollection pservices)
        {
            services = pservices;
            RepoApps = new List<IBeepAppRepo>();
            ErrorsandMesseges = new ErrorsInfo();
            CreateMainRepoAppsfolder();
        }

        private List<IBeepAppRepo> _containers;
        public List<IBeepAppRepo> RepoApps
        {
            get
            {
                if (_containers == null)
                {
                    _containers = new List<IBeepAppRepo>();
                }
                return _containers;
            }
            set
            {
                _containers = value;
            }
        }

        public ErrorsInfo ErrorsandMesseges { get; set; } = new ErrorsInfo();
        string _containerFolderPath = string.Empty;
        public string RepoAppFolderPath 
        { get { 
                if (string.IsNullOrEmpty(_containerFolderPath))
                {
                   CreateMainRepoAppsfolder(); 
                 } 
                 return _containerFolderPath;
              }
          set 
              {
                _containerFolderPath = value;
              } 
        }
        private  void CreateMainRepoAppsfolder()
        {
                if (!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "TheTechIdea", "Beep")))
                {
                    Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "TheTechIdea", "Beep"));

                }
                string BeepDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "TheTechIdea", "Beep");
            if (!Directory.Exists(Path.Combine(BeepDataPath, "RepoApps")))
            {
                Directory.CreateDirectory(Path.Combine(BeepDataPath, "RepoApps"));

            }
            _containerFolderPath = Path.Combine(BeepDataPath, "RepoApps");
            filename= Path.Combine(BeepDataPath, "RepoApps", "containers.json");
        }
        public  Task<ErrorsInfo> LoadRepoApps()
        {
            // load containers from file using system.text.json
            // load using System.Text.Json json file into RepoApps = new List<IBeepRepoApp>();
            try
            {
                if (File.Exists(filename))
                {
                    String JSONtxt = File.ReadAllText(filename);
                    RepoApps = JsonConvert.DeserializeObject<List<IBeepAppRepo>>(JSONtxt, GetSettings());
                }
                else
                {
                    RepoApps = new List<IBeepAppRepo>();
                }
            }
            catch (Exception ex)
            {
                ErrorsandMesseges.Flag = Errors.Failed;
                ErrorsandMesseges.Message = ex.Message;
                ErrorsandMesseges.Ex = ex;
                ErrorsandMesseges.Fucntion = "Load RepoApps ";
                ErrorsandMesseges.Module = "RepoApp  Management";
            }
            
            return Task.FromResult(ErrorsandMesseges);
        }
        public Task<ErrorsInfo> SaveRepoApps()
        {
            // save containers to file using System.Text.Json
            try
            {
                using (StreamWriter file = File.CreateText(filename))
                {
                   
                        Newtonsoft.Json.JsonSerializer serializer = new JsonSerializer();
                        serializer.NullValueHandling = NullValueHandling.Include;
                        serializer.MissingMemberHandling = MissingMemberHandling.Ignore;
                        serializer.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                        serializer.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
                }
            }
            catch (Exception ex)
            {
                ErrorsandMesseges.Flag = Errors.Failed;
                ErrorsandMesseges.Message = ex.Message;
                ErrorsandMesseges.Ex = ex;
                ErrorsandMesseges.Fucntion = "Load RepoApps ";
                ErrorsandMesseges.Module = "RepoApp  Management";
            }
            return Task.FromResult(ErrorsandMesseges);
        }
        public List<IBeepAppRepo> GetUserRepoApps(string owner)
        {
            // get all conatiners for a user
            if(RepoApps==null)
            {
                RepoApps = new List<IBeepAppRepo>();
            }
            return RepoApps.Where(p => p.Owner.Equals(owner, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        public List<IBeepAppRepo> GetUserRepoAppsByGuiID(string guidid)
        {
            // get all conatiners for a user
            if (RepoApps == null)
            {
                RepoApps = new List<IBeepAppRepo>();
            }
            return RepoApps.Where(p => p.OwnerGuidID.Equals(guidid, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        public List<IBeepAppRepo> GetUserRepoApps(int id)
        {
            if (RepoApps == null)
            {
                RepoApps = new List<IBeepAppRepo>();
            }
            // get all conatiners for a user
            return RepoApps.Where(p => p.OwnerID == id).ToList();
        }
        public IBeepAppRepo GetUserPrimaryRepoApp(string owner)
        {
            if (RepoApps == null)
            {
                RepoApps = new List<IBeepAppRepo>();
            }
            // get primary container for a user
            if (RepoApps.Where(p => p.Owner.Equals(owner, StringComparison.OrdinalIgnoreCase) && p.IsPrimary).Any())
            {
                CurrentRepoApp = RepoApps.Where(p => p.Owner.Equals(owner, StringComparison.OrdinalIgnoreCase) && p.IsPrimary).FirstOrDefault();
                IsRepoAppActive = true;
                IsRepoAppCreated = true;
                IsRepoAppLoaded = true;
                return CurrentRepoApp;
            }else
                if(RepoApps.Where(p => p.Owner.Equals(owner, StringComparison.OrdinalIgnoreCase)).Any())
            {
                IsRepoAppActive = true;
                IsRepoAppCreated = true;
                IsRepoAppLoaded = true;
                CurrentRepoApp = RepoApps.Where(p => p.Owner.Equals(owner, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                return CurrentRepoApp;
              
            }
            CurrentRepoApp= null;
            IsRepoAppActive = false;
            IsRepoAppCreated = false;
            IsRepoAppLoaded = false;

            return null;
           
        }
        public async Task<ErrorsInfo> RemoveRepoApp(string RepoAppGuidID)
        {
            try
            {
                IErrorsInfo ErrorsandMesseges = new ErrorsInfo();
                // -- check if user already Exist
                var t = Task.Run<IBeepAppRepo>(() => RepoApps.Where(p => p.GuidID.Equals(RepoAppGuidID, StringComparison.OrdinalIgnoreCase)).FirstOrDefault());
                t.Wait();

                if (t.Result == null)
                {
                    ErrorsandMesseges.Flag = Errors.Failed;
                    ErrorsandMesseges.Message = $"RepoApp not Exists";
                }
                else
                {
                    t.Dispose();
                    RepoApps.Remove(t.Result);
                    ErrorsandMesseges.Flag = Errors.Ok;
                    ErrorsandMesseges.Message = $"RepoApp Added";
                }

            }
            catch (Exception ex)
            {
                ErrorsandMesseges.Flag = Errors.Failed;
                ErrorsandMesseges.Message = ex.Message;
                ErrorsandMesseges.Ex = ex;
                ErrorsandMesseges.Fucntion = "Add RepoApp";
                ErrorsandMesseges.Module = "RepoApp  Management";

            }
            return await Task.FromResult(ErrorsandMesseges);
        }
        public async Task<ErrorsInfo> AddUpdateRepoApp(IBeepAppRepo pRepoApp)
        {
            try
            {
                IErrorsInfo ErrorsandMesseges = new ErrorsInfo();
                // -- check if RepoApp already Exist

                if (!RepoApps.Where(p => p.BeepAppRepoName.Equals(pRepoApp.BeepAppRepoName, StringComparison.OrdinalIgnoreCase)).Any())
                {
                    RepoApps.Add(pRepoApp);
                    ErrorsandMesseges.Flag = Errors.Ok;
                    ErrorsandMesseges.Message = $"RepoApp Added";
                }
                else
                {
                    int idx = RepoApps.FindIndex(p => p.BeepAppRepoName.Equals(pRepoApp.BeepAppRepoName, StringComparison.OrdinalIgnoreCase));
                    RepoApps[idx] = pRepoApp;
                    ErrorsandMesseges.Flag = Errors.Ok;
                    ErrorsandMesseges.Message = $"RepoApp Updated";
                }

            }
            catch (Exception ex)
            {
                ErrorsandMesseges.Flag = Errors.Failed;
                ErrorsandMesseges.Message = ex.Message;
                ErrorsandMesseges.Ex = ex;
                ErrorsandMesseges.Fucntion = "Update/Add RepoApp";
                ErrorsandMesseges.Module = "RepoApp  Management";

            }
            return await Task.FromResult(ErrorsandMesseges);
        }
        public async Task<ErrorsInfo> CreateRepoApp(string RepoAppGuidID,string owner, string ownerEmail, int ownerID, string ownerGuid, string pRepoAppName,  string pRepoAppFolderPath=null)
        {
            try
            {
                ErrorsInfo ErrorsandMesseges = new ErrorsInfo();
                // -- check if RepoApp already Exist
                if(string.IsNullOrEmpty(pRepoAppName))
                {
                    ErrorsandMesseges.Flag = Errors.Failed;
                    ErrorsandMesseges.Message = $"RepoApp Name is Empty";
                    return await Task.FromResult(ErrorsandMesseges);
                }
                if(string.IsNullOrEmpty(pRepoAppFolderPath))
                {
                    pRepoAppFolderPath = RepoAppFolderPath;
                }
                IBeepAppRepo x = GetRepoApp( RepoAppGuidID, owner, ownerEmail, ownerID, ownerGuid, pRepoAppName);
                if (x==null)
                {
                    x = new BeepAppRepo() { BeepAppRepoName = pRepoAppName, BeepAppRepoFolderPath = pRepoAppFolderPath };
                    try
                    {
                        IBeepService beepservice = new BeepService(services);
                        beepservice.Configure(pRepoAppFolderPath, pRepoAppName, BeepConfigType.Application);
                        x.BeepService = beepservice;
                        x.GuidID = Guid.NewGuid().ToString();
                        x.BeepAppRepoName = pRepoAppName;
                        x.AdminUserID = owner;
                        x.Owner = owner;
                        x.OwnerEmail = ownerEmail;
                        x.OwnerID = ownerID;
                        x.OwnerGuidID = ownerGuid;
                        x.BeepAppRepoFolderPath = pRepoAppFolderPath;
                        x.IsPrimary = true;
                        x.isActive = true;
                        x.GuidID = RepoAppGuidID;
                        RepoApps.Add(x);
                        await CreateRepoAppFileSystem(x);
                        ErrorsandMesseges.Flag = Errors.Ok;
                        ErrorsandMesseges.Message = $"RepoApp Added";
                    }
                    catch (Exception ex)
                    {
                        ErrorsandMesseges.Flag = Errors.Failed;
                        ErrorsandMesseges.Message = $"RepoApp Failed : {ex.Message}";
                        
                    }
                 
                }
                else
                {
                    ErrorsandMesseges.Flag = Errors.Failed;
                    ErrorsandMesseges.Message = $"RepoApp Exist";
                }

            }
            catch (Exception ex)
            {
                ErrorsandMesseges.Flag = Errors.Failed;
                ErrorsandMesseges.Message = ex.Message;
                ErrorsandMesseges.Ex = ex;
                ErrorsandMesseges.Fucntion = "Update/Add RepoApp";
                ErrorsandMesseges.Module = "RepoApp  Management";

            }
            return await Task.FromResult(ErrorsandMesseges);
        }
        private IBeepAppRepo GetRepoApp(string RepoAppGuidID, string owner, string ownerEmail, int ownerID, string ownerGuid, string pRepoAppName)
        {
            // Get RepoApp by all parameters owner, ownerEmail, ownerID, ownerGuid, pRepoAppName
            return RepoApps.Where(p => p.BeepAppRepoName.Equals(pRepoAppName, StringComparison.OrdinalIgnoreCase) && p.Owner.Equals(owner, StringComparison.OrdinalIgnoreCase) && p.OwnerEmail.Equals(ownerEmail, StringComparison.OrdinalIgnoreCase) && p.OwnerID == ownerID && p.OwnerGuidID.Equals(ownerGuid, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

            
        }
        public async Task<ErrorsInfo> CreateRepoApp(string RepoAppGuidID, string owner, string ownerEmail, int ownerID, string ownerGuid, string pRepoAppName,  string pRepoAppFolderPath, string pSecretKey, string pTokenKey)
        {
            try
            {
                IErrorsInfo ErrorsandMesseges = new ErrorsInfo();
                // -- check if RepoApp already Exist

                ErrorsandMesseges= await CreateRepoApp( RepoAppGuidID, owner,  ownerEmail,  ownerID,  ownerGuid, pRepoAppName,  pRepoAppFolderPath);
                if(ErrorsandMesseges.Flag==Errors.Ok)
                {
                    IBeepAppRepo x = GetRepoApp(RepoAppGuidID, owner, ownerEmail, ownerID, ownerGuid, pRepoAppName); 
                    x.SecretKey = pSecretKey;
                    x.TokenKey = pTokenKey;
                   

                    ErrorsandMesseges = await AddUpdateRepoApp(x);
                }

            }
            catch (Exception ex)
            {
                ErrorsandMesseges.Flag = Errors.Failed;
                ErrorsandMesseges.Message = ex.Message;
                ErrorsandMesseges.Ex = ex;
                ErrorsandMesseges.Fucntion = "Update/Add RepoApp";
                ErrorsandMesseges.Module = "RepoApp  Management";

            }
            return await Task.FromResult(ErrorsandMesseges);
        }
        public async Task<ErrorsInfo> CreateRepoAppFileSystem(IBeepAppRepo pRepoApp)
        {
            try
            {
                IErrorsInfo ErrorsandMesseges = new ErrorsInfo();
                // -- check if RepoApp already Exist

              //  ErrorsandMesseges = (IErrorsInfo)AddUpdateRepoApp(pRepoApp);
                //--------------------- Create File System -------------
                if (ErrorsandMesseges.Flag == Errors.Ok)
                {
                    if (pRepoApp.BeepAppRepoFolderPath != null)
                    {
                        try
                        {
                           string ExePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
                         //   string ConatinerPath = Path.Combine(ExePath, pRepoApp.RepoAppFolderPath);
                         if(File.Exists(Path.Combine(ExePath, "BeepRepoAppFiles.zip")))
                         {
                                await Task.Run(() => ZipFile.ExtractToDirectory(Path.Combine(ExePath, "BeepRepoAppFiles.zip"), Path.Combine(RepoAppFolderPath,pRepoApp.BeepAppRepoName)));
                         }
                        }
                        #region "Catch for Zip file Extract"
                        catch (ArgumentNullException)
                        {
                            ErrorsandMesseges.Message = $"destinationDirectoryName or sourceArchiveFileName is null.";
                            ErrorsandMesseges.Flag = Errors.Failed;
                        }
                        catch (PathTooLongException)
                        {
                            ErrorsandMesseges.Flag = Errors.Failed;
                            ErrorsandMesseges.Message = $"The specified path in destinationDirectoryName or sourceArchiveFileName exceeds the system-defined maximum length.";
                        }
                        catch (DirectoryNotFoundException)
                        {
                            ErrorsandMesseges.Flag = Errors.Failed;
                            ErrorsandMesseges.Message = $"The specified path is invalid(for example, it is on an unmapped drive).";
                        }
                        catch (UnauthorizedAccessException)
                        {
                            ErrorsandMesseges.Message = $"The caller does not have the required permission to access the archive or the destination directory.";
                            ErrorsandMesseges.Flag = Errors.Failed;
                        }
                        catch (NotSupportedException)
                        {
                            ErrorsandMesseges.Message = $"destinationDirectoryName or sourceArchiveFileName contains an invalid format.";
                            ErrorsandMesseges.Flag = Errors.Failed;
                        }

                        catch (FileNotFoundException)
                        {
                            ErrorsandMesseges.Message = $"sourceArchiveFileName was not found.";
                            ErrorsandMesseges.Flag = Errors.Failed;
                        }
                        catch (InvalidDataException)
                        {
                            ErrorsandMesseges.Flag = Errors.Failed;
                            ErrorsandMesseges.Message = $"The archive specified by sourceArchiveFileName is not a valid zip archive.";
                            ErrorsandMesseges.Message += $"An archive entry was not found or was corrupt.";
                            ErrorsandMesseges.Message += $"An archive entry was compressed by using a compression method that is not supported.";
                        }
                        #endregion
                        //if (ErrorsandMesseges.Flag == Errors.Ok)
                        //{
                        //    //-------- Create DMService -----
                        //    try
                        //    {
                        //        services.AddScoped<IBeepService>(s => new BeepService(services, AppContext.BaseDirectory, pRepoApp.RepoAppName, BeepConfigType.RepoApp));
                        //        var provider = services.BuildServiceProvider();
                        //        var ContianerService = provider.GetService<IBeepService>();

                        //    }
                        //    catch (Exception dmex)
                        //    {
                        //        ErrorsandMesseges.Flag = Errors.Failed;
                        //        ErrorsandMesseges.Message = $"Failed to Create DMBeep Service - {dmex.Message}";
                        //    }
                        //}

                    }
                }

            }
            catch (Exception ex)
            {
                ErrorsandMesseges.Flag = Errors.Failed;
                ErrorsandMesseges.Message = ex.Message;
                ErrorsandMesseges.Ex = ex;
                ErrorsandMesseges.Fucntion = "Update/Add RepoApp";
                ErrorsandMesseges.Module = "RepoApp  Management";

            }
            return await Task.FromResult(ErrorsandMesseges);
        }
        public IBeepAppRepo GetBeepRepoApp(string RepoAppName)
        {
            return RepoApps.Where(p => p.BeepAppRepoName.Equals(RepoAppName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
        }
        public IBeepAppRepo GetBeepRepoAppByID(int RepoAppID)
        {
            return RepoApps.Where(p => p.BeepAppRepoID == RepoAppID).FirstOrDefault();
        }
        public IBeepAppRepo GetBeepRepoAppByGuidID(string RepoAppGuidID)
        {
            return RepoApps.Where(p => p.GuidID == RepoAppGuidID).FirstOrDefault();
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~CantainerManager()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        private JsonSerializerSettings GetSettings()

        {
            return new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Include,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                //CheckAdditionalContent=true,
                //TypeNameHandling = TypeNameHandling.All,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = new List<JsonConverter> { new Newtonsoft.Json.Converters.StringEnumConverter() }


            };
        }
    }
}
