using System;
using System.Collections.Generic;
using System.Linq;

namespace TheTechIdea.Beep.SetUp
{
    /// <summary>
    /// Fluent builder for composing a <see cref="SetupWizard"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// var wizard = new SetupWizardBuilder()
    ///     .WithId("my-app-setup")
    ///     .WithEnvironment("Production")
    ///     .AddStep(new DriverProvisionStep(driverOpts))
    ///     .AddStep(new ConnectionConfigStep(connOpts))
    ///     .Build();
    /// </code>
    /// </example>
    public class SetupWizardBuilder
    {
        private readonly List<ISetupStep> _steps = new();
        private SetupOptions _options = new SetupOptions();
        private string _wizardId = "default-setup";

        /// <summary>Sets the wizard identifier used in reports.</summary>
        public SetupWizardBuilder WithId(string wizardId)
        {
            _wizardId = wizardId;
            return this;
        }

        /// <summary>Appends a step to the wizard execution sequence.</summary>
        public SetupWizardBuilder AddStep(ISetupStep step)
        {
            if (step == null) throw new ArgumentNullException(nameof(step));
            _steps.Add(step);
            return this;
        }

        /// <summary>Replaces the entire options object.</summary>
        public SetupWizardBuilder WithOptions(SetupOptions options)
        {
            _options = options;
            return this;
        }

        /// <summary>Toggles dry-run mode.</summary>
        public SetupWizardBuilder WithDryRun(bool dryRun = true)
        {
            _options.DryRun = dryRun;
            return this;
        }

        /// <summary>Sets the target environment label.</summary>
        public SetupWizardBuilder WithEnvironment(string env)
        {
            _options.Environment = env;
            return this;
        }

        /// <summary>Sets the checkpoint state file path.</summary>
        public SetupWizardBuilder WithStateFile(string path)
        {
            _options.StateFilePath = path;
            return this;
        }

        /// <summary>Sets the report output directory.</summary>
        public SetupWizardBuilder WithReportOutput(string path)
        {
            _options.ReportOutputPath = path;
            return this;
        }

        /// <summary>
        /// Sets the checkpoint state file path and configures the wizard to resume
        /// from the persisted checkpoint when <see cref="ISetupWizard.Resume"/> is called.
        /// Equivalent to <see cref="WithStateFile"/> but communicates intent clearly.
        /// </summary>
        public SetupWizardBuilder WithResumeFromFile(string path)
        {
            _options.StateFilePath = path;
            return this;
        }

        /// <summary>
        /// Builds and returns a configured <see cref="ISetupWizard"/>.
        /// Validates that each step's <c>DependsOn</c> constraints are satisfied by
        /// earlier steps in the sequence; throws <see cref="InvalidOperationException"/>
        /// if a dependency is missing or declared out of order.
        /// </summary>
        public ISetupWizard Build()
        {
            ValidateDependencyOrder();
            return new SetupWizard(_wizardId, _steps, _options);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private void ValidateDependencyOrder()
        {
            var seenIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var step in _steps)
            {
                foreach (var dep in step.DependsOn)
                {
                    if (!seenIds.Contains(dep))
                        throw new InvalidOperationException(
                            $"Step '{step.StepId}' declares dependency on '{dep}', " +
                            $"but '{dep}' has not been added before it. " +
                            $"Add the dependency step first or verify the StepId spelling.");
                }
                seenIds.Add(step.StepId);
            }
        }
    }
}
