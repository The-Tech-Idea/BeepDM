using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.SetUp
{
    public interface ISetupStep
    {
        string StepId { get; }
        string StepName { get; }
        string Description { get; }
        IReadOnlyList<string> DependsOn { get; }

        bool CanSkip(SetupContext context);

        IErrorsInfo Validate(SetupContext context);

        IErrorsInfo Execute(SetupContext context, System.IProgress<PassedArgs> progress = null);

        Task<IErrorsInfo> ValidateAsync(SetupContext context, CancellationToken token = default) =>
            Task.FromResult(Validate(context));

        Task<IErrorsInfo> ExecuteAsync(
            SetupContext context,
            System.IProgress<PassedArgs>? progress = null,
            CancellationToken token = default) =>
            Task.Run(() => Execute(context, progress), token);
    }
}
