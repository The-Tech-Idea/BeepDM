
using TheTechIdea.Beep.Logger;
using System;
using TheTechIdea.Beep.Editor;

using TheTechIdea.Beep.ConfigUtil;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Addin
{
    /// <summary>
    /// Represents an add-in in the Beep system.
    /// </summary>
    public interface IDM_Addin : INavigable, IErrorView
    {
        // Lifecycle Management
        void Initialize();
        void Dispose();
        void Suspend();
        void Resume();

        // Addin Details
        AddinDetails Details { get; set; }
        Dependencies Dependencies { get; set; }

        // Error Management
        string GetErrorDetails();

        // Execution
        void Run(IPassedArgs pPassedarg);
        void Run(params object[] args);
        Task<IErrorsInfo> RunAsync(IPassedArgs pPassedarg);
        Task<IErrorsInfo> RunAsync(params object[] args);

        string GuidID { get; set; }

        // Optional Configuration (if runtime config is needed)
        void Configure(Dictionary<string, object> settings);
        // Apply theme if applicable
        void ApplyTheme();
        // Events
        event EventHandler OnStart;
        event EventHandler OnStop;
        event EventHandler<ErrorEventArgs> OnError;
    }

    public interface INavigable
    {
        void OnNavigatedTo(Dictionary<string, object> parameters);
    }
    public interface IErrorView
    {
        void SetError(string message);
    }
    public class AddinDetails
    {
        public string ParentName { get; set; }
        public string ObjectName { get; set; }
        public string ObjectType { get; set; }
        public string AddinName { get; set; }
        public string Description { get; set; }
        public string GuidID { get; set; }
    }

    public class Dependencies
    {
        public IErrorsInfo ErrorObject { get; set; }
        public IDMLogger Logger { get; set; }
        public IDMEEditor DMEEditor { get; set; }
    }
}
