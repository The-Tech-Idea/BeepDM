using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.SetUp;

namespace TheTechIdea.Beep.Installer.Steps
{
    /// <summary>
    /// Applies <see cref="InstallConfig.EnvironmentVariables"/>.
    ///
    /// Until this step existed nothing consumed that collection: an author could configure
    /// environment variables, the installer would report success, and nothing was ever set.
    /// <see cref="VerifyInstallStep"/> already expected the <c>EnvVarsSet</c> context key this
    /// step writes, so the uninstall manifest recorded an empty list too.
    /// </summary>
    public class EnvironmentVariableStep : ISetupStep
    {
        public string StepId => "installer.envvars.write";
        public string StepName => "Set environment variables";
        public string Description => "Applies the configured environment variables.";
        public IReadOnlyList<string> DependsOn { get; }

        public EnvironmentVariableStep(string? dependsOn = null)
        {
            DependsOn = dependsOn != null ? new List<string> { dependsOn } : Array.Empty<string>();
        }

        public bool CanSkip(SetupContext context)
            => (context.TryGetProperty<InstallConfig>("InstallConfig")?.EnvironmentVariables?.Count ?? 0) == 0;

        public IErrorsInfo Validate(SetupContext context)
        {
            var config = context.TryGetProperty<InstallConfig>("InstallConfig");
            if (config?.EnvironmentVariables == null) return StepErrorHelpers.Ok("Nothing to set.");

            var unnamed = config.EnvironmentVariables.Count(v => string.IsNullOrWhiteSpace(v.Name));
            return unnamed > 0
                ? StepErrorHelpers.Fail($"{unnamed} environment variable(s) have no name.")
                : StepErrorHelpers.Ok("Validated.");
        }

        public bool SupportsRollback => true;

        public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs>? progress = null)
        {
            var config = context.TryGetProperty<InstallConfig>("InstallConfig");
            var variables = config?.EnvironmentVariables;
            if (variables == null || variables.Count == 0)
                return StepErrorHelpers.Ok("No environment variables to set.");

            if (context.Options?.DryRun == true)
                return StepErrorHelpers.Ok(
                    $"Dry run: {variables.Count} environment variable(s) would be set " +
                    $"({string.Join(", ", variables.Select(v => v.Name))}). Nothing was changed.");

            var installPath = context.TryGetProperty<string>("InstallPath") ?? "";
            var perUser = InstallScope.IsPerUser(context);

            var applied = new List<EnvironmentVariableOp>();
            var errors = new List<string>();

            for (int i = 0; i < variables.Count; i++)
            {
                var variable = variables[i];
                if (string.IsNullOrWhiteSpace(variable.Name)) continue;

                var scope = ResolveScope(variable.Scope, perUser);
                var value = (variable.Value ?? "").Replace("{InstallPath}", installPath);

                try
                {
                    Environment.SetEnvironmentVariable(variable.Name, value, scope);
                    applied.Add(new EnvironmentVariableOp { Name = variable.Name, Value = value, Scope = scope });
                    progress?.Report(new PassedArgs
                    {
                        Messege = $"Set {variable.Name} ({scope})",
                        ParameterInt1 = (int)((i + 1) * 100.0 / variables.Count)
                    });
                }
                catch (Exception ex)
                {
                    // A per-machine variable needs elevation; report rather than fail silently.
                    errors.Add($"{variable.Name}: {ex.Message}");
                }
            }

            context.Properties["EnvVarsSet"] = applied;

            if (applied.Count > 0)
            {
                // Without this, already-running processes (Explorer, shells) keep the old
                // environment until the next sign-in.
                InstallHelpers.BroadcastEnvironmentChange();
            }

            return errors.Count > 0
                ? StepErrorHelpers.Fail($"{errors.Count} environment variable(s) failed: {string.Join("; ", errors)}")
                : StepErrorHelpers.Ok($"{applied.Count} environment variable(s) set.");
        }

        public Task<IErrorsInfo> ExecuteAsync(SetupContext context, IProgress<PassedArgs>? progress = null, CancellationToken token = default)
            => Task.FromResult(Execute(context, progress));

        public Task<IErrorsInfo> RollbackAsync(SetupContext context, IProgress<PassedArgs>? progress = null, CancellationToken token = default)
        {
            var applied = context.TryGetProperty<List<EnvironmentVariableOp>>("EnvVarsSet");
            if (applied == null || applied.Count == 0)
                return Task.FromResult(StepErrorHelpers.Ok("No environment variables to undo."));

            var removed = Remove(applied);
            context.Properties["EnvVarsSet"] = new List<EnvironmentVariableOp>();
            return Task.FromResult(StepErrorHelpers.Ok($"{removed} environment variable(s) removed."));
        }

        /// <summary>
        /// Removes previously applied variables and returns how many were cleared. Shared with
        /// the uninstall path, which replays them from the install manifest.
        /// </summary>
        public static int Remove(IEnumerable<EnvironmentVariableOp> variables)
        {
            int removed = 0;
            foreach (var variable in variables)
            {
                if (string.IsNullOrWhiteSpace(variable.Name)) continue;
                try
                {
                    Environment.SetEnvironmentVariable(variable.Name, null, variable.Scope);
                    removed++;
                }
                catch { /* best-effort: a variable we cannot clear must not fail an uninstall */ }
            }
            if (removed > 0) InstallHelpers.BroadcastEnvironmentChange();
            return removed;
        }

        /// <summary>
        /// A per-user install cannot write machine-wide variables (that needs elevation), so a
        /// Machine-scoped request is downgraded rather than throwing.
        /// </summary>
        private static EnvironmentVariableTarget ResolveScope(EnvironmentVariableTarget requested, bool perUser)
            => perUser && requested == EnvironmentVariableTarget.Machine
                ? EnvironmentVariableTarget.User
                : requested;
    }
}
