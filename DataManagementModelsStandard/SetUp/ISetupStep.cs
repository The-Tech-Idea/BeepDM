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

        /// <summary>
        /// Registered step-type key used when this step is written into a <c>SetupDefinition</c>.
        /// Defaults to <see cref="StepId"/>; steps whose id is qualified (e.g.
        /// <c>"driver-provision:SQLite"</c>) must override this with the bare type key.
        /// </summary>
        string TypeKey => StepId;

        /// <summary>
        /// This step's options as JSON, for <c>SetupWizardBuilder.ToDefinition()</c>.
        /// Returns null by default — a step opts in by overriding.
        /// </summary>
        /// <remarks>
        /// A step that returns null round-trips <b>lossily</b> (its options are dropped);
        /// <c>ToDefinition()</c> warns rather than silently emitting an incomplete definition.
        /// </remarks>
        System.Text.Json.JsonElement? SerializeOptions() => null;

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

        /// <summary>
        /// True when <see cref="RollbackAsync"/> actually undoes something. Lets the rollback
        /// orchestrator report a step as <em>skipped</em> (nothing to undo) rather than a clean
        /// undo that never happened. Default false — most steps are forward-only.
        /// </summary>
        bool SupportsRollback => false;

        /// <summary>
        /// Permission the running principal must hold to execute this step. Checked by the wizard
        /// before <see cref="Execute"/>. Default <see cref="Security.SetupPermission.RunSetup"/>.
        /// </summary>
        Security.SetupPermission RequiredPermission => Security.SetupPermission.RunSetup;

        /// <summary>
        /// Undoes this step's effects. Called in reverse order by the rollback orchestrator after a
        /// failed run. Default: a no-op success — the step declares itself non-compensating via
        /// <see cref="SupportsRollback"/>.
        /// </summary>
        Task<IErrorsInfo> RollbackAsync(
            SetupContext context,
            System.IProgress<PassedArgs>? progress = null,
            CancellationToken token = default) =>
            Task.FromResult<IErrorsInfo>(new ErrorsInfo { Flag = Errors.Ok, Message = "No rollback defined." });
    }
}
