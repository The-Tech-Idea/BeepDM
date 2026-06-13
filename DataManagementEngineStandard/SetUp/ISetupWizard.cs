using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.SetUp
{
    public interface ISetupWizard
    {
        IReadOnlyList<ISetupStep> Steps { get; }
        SetupState State { get; }
        SetupOptions Options { get; }

        IErrorsInfo Run(SetupContext context, System.IProgress<PassedArgs> progress = null);

        IErrorsInfo Resume(SetupContext context, System.IProgress<PassedArgs> progress = null);

        SetupReport GetReport();

        Task<IErrorsInfo> RunAsync(
            SetupContext context,
            System.IProgress<PassedArgs>? progress = null,
            CancellationToken token = default);
    }
}
