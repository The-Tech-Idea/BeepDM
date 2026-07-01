using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
        private ILogger<SetupWizard>? _logger;
        private IServiceProvider? _services;
        private ISetupWizardAdapter? _adapter;

        public SetupWizardBuilder WithId(string wizardId)
        {
            _wizardId = wizardId;
            return this;
        }

        public SetupWizardBuilder WithLogger(ILogger<SetupWizard>? logger)
        {
            _logger = logger;
            return this;
        }

        public SetupWizardBuilder WithAdapter(ISetupWizardAdapter? adapter)
        {
            _adapter = adapter;
            return this;
        }

        public SetupWizardBuilder WithOptions(SetupOptions options)
        {
            _options = options ?? new SetupOptions();
            return this;
        }

        public SetupWizardBuilder UseServiceProvider(IServiceProvider services)
        {
            _services = services;
            return this;
        }

        public SetupWizardBuilder AddStep(ISetupStep step)
        {
            if (step == null) throw new ArgumentNullException(nameof(step));
            _steps.Add(step);
            return this;
        }

        public SetupWizardBuilder AddStep<T>(Action<T>? configure = null) where T : class, ISetupStep
        {
            T step;
            if (_services != null)
                step = ActivatorUtilities.CreateInstance<T>(_services);
            else
                step = Activator.CreateInstance<T>();

            configure?.Invoke(step);
            _steps.Add(step);
            return this;
        }

        /// <summary>Toggles dry-run mode.</summary>
        public SetupWizardBuilder WithDryRun(bool dryRun = true)
        {
            _options = new SetupOptions
            {
                DryRun = dryRun,
                SkipSeeding = _options.SkipSeeding,
                SkipSchema = _options.SkipSchema,
                Environment = _options.Environment,
                StrictPolicyMode = _options.StrictPolicyMode,
                StateFilePath = _options.StateFilePath,
                ReportOutputPath = _options.ReportOutputPath
            };
            return this;
        }

        /// <summary>Sets the target environment label.</summary>
        public SetupWizardBuilder WithEnvironment(string env)
        {
            _options = new SetupOptions
            {
                DryRun = _options.DryRun,
                SkipSeeding = _options.SkipSeeding,
                SkipSchema = _options.SkipSchema,
                Environment = env,
                StrictPolicyMode = _options.StrictPolicyMode,
                StateFilePath = _options.StateFilePath,
                ReportOutputPath = _options.ReportOutputPath
            };
            return this;
        }

        /// <summary>Sets the checkpoint state file path.</summary>
        public SetupWizardBuilder WithStateFile(string path)
        {
            _options = new SetupOptions
            {
                DryRun = _options.DryRun,
                SkipSeeding = _options.SkipSeeding,
                SkipSchema = _options.SkipSchema,
                Environment = _options.Environment,
                StrictPolicyMode = _options.StrictPolicyMode,
                StateFilePath = path,
                ReportOutputPath = _options.ReportOutputPath
            };
            return this;
        }

        /// <summary>Sets the report output directory.</summary>
        public SetupWizardBuilder WithReportOutput(string path)
        {
            _options = new SetupOptions
            {
                DryRun = _options.DryRun,
                SkipSeeding = _options.SkipSeeding,
                SkipSchema = _options.SkipSchema,
                Environment = _options.Environment,
                StrictPolicyMode = _options.StrictPolicyMode,
                StateFilePath = _options.StateFilePath,
                ReportOutputPath = path
            };
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
            return new SetupWizard(_wizardId, _steps, _options, _logger, _adapter);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private void ValidateDependencyOrder()
        {
            var stepById = _steps.ToDictionary(s => s.StepId, StringComparer.Ordinal);
            var inDegree = new Dictionary<string, int>(StringComparer.Ordinal);
            var adjacency = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            foreach (var step in _steps)
            {
                var deps = (step.DependsOn ?? Array.Empty<string>())
                    .Where(d => !string.IsNullOrWhiteSpace(d))
                    .ToList();

                foreach (var dep in deps)
                {
                    if (!stepById.ContainsKey(dep))
                        throw new InvalidOperationException(
                            $"Step '{step.StepId}' depends on unknown step '{dep}'.");

                    if (string.Equals(dep, step.StepId, StringComparison.Ordinal))
                        throw new InvalidOperationException(
                            $"Step '{step.StepId}' cannot depend on itself.");
                }

                adjacency[step.StepId] = deps;
                if (!inDegree.ContainsKey(step.StepId))
                    inDegree[step.StepId] = 0;

                foreach (var dep in deps)
                {
                    inDegree[dep] = inDegree.GetValueOrDefault(dep, 0) + 1;
                }
            }

            // Kahn's algorithm: detect cycles
            var queue = new Queue<string>(_steps
                .Select(s => s.StepId)
                .Where(id => inDegree.GetValueOrDefault(id, 0) == 0));

            var sorted = new List<string>();
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                sorted.Add(current);

                if (adjacency.TryGetValue(current, out var neighbors))
                {
                    foreach (var neighbor in neighbors)
                    {
                        inDegree[neighbor]--;
                        if (inDegree[neighbor] == 0)
                            queue.Enqueue(neighbor);
                    }
                }
            }

            if (sorted.Count != _steps.Count)
            {
                var cycleSteps = string.Join(" → ",
                    _steps.Where(s => !sorted.Contains(s.StepId))
                        .Select(s => $"'{s.StepId}'"));
                throw new InvalidOperationException(
                    $"Cycle detected in step dependencies: {cycleSteps}. " +
                    "Remove circular dependencies between steps.");
            }
        }
    }
}
