using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TheTechIdea.Beep.SetUp.Definition;

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
    public class SetupWizardBuilder : ISetupWizardBuilder
    {
        private readonly List<ISetupStep> _steps = new();
        private SetupOptions _options = new SetupOptions();
        private string _wizardId = "default-setup";
        private ILogger<SetupWizard>? _logger;
        private IServiceProvider? _services;
        private ISetupWizardAdapter? _adapter;
        private State.ISetupStateStore? _stateStore;
        private Rollback.IRollbackOrchestrator? _rollback;
        private Security.ISetupPrincipal? _principal;
        private Security.ISetupAuthorizer? _authorizer;
        private Audit.ISetupAuditSink? _audit;
        private string? _definitionHash;
        private string? _appId;

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

        /// <summary>
        /// Sets the state store. When unset, the wizard falls back to a local file store keyed by
        /// <see cref="SetupOptions.StateFilePath"/>, or disables checkpointing if that's unset too.
        /// Inject a <c>RemoteSetupStateStore</c> for shared/enterprise state.
        /// </summary>
        public SetupWizardBuilder WithStateStore(State.ISetupStateStore? stateStore)
        {
            _stateStore = stateStore;
            return this;
        }

        public SetupWizardBuilder WithAdapter(ISetupWizardAdapter? adapter)
        {
            _adapter = adapter;
            return this;
        }

        /// <summary>
        /// Sets the rollback orchestrator. When unset, a default <c>RollbackOrchestrator</c> is used;
        /// rollback only runs when <see cref="SetupOptions.AutoRollbackOnFailure"/> is set.
        /// </summary>
        public SetupWizardBuilder WithRollbackOrchestrator(Rollback.IRollbackOrchestrator? rollback)
        {
            _rollback = rollback;
            return this;
        }

        /// <summary>
        /// Sets who is running the setup and how permissions are checked. Both default to the solo
        /// no-op providers (anonymous principal, allow-all authorizer) when unset.
        /// </summary>
        public SetupWizardBuilder WithSecurity(
            Security.ISetupPrincipal? principal, Security.ISetupAuthorizer? authorizer = null)
        {
            _principal = principal;
            _authorizer = authorizer;
            return this;
        }

        /// <summary>
        /// Sets the audit sink. When unset, an append-only JSONL sink is derived from
        /// <see cref="SetupOptions.ReportOutputPath"/>; if that's unset too, auditing is a no-op.
        /// Inject a <c>BeepAuditSetupSink</c> for the tamper-evident enterprise chain.
        /// </summary>
        public SetupWizardBuilder WithAudit(Audit.ISetupAuditSink? audit)
        {
            _audit = audit;
            return this;
        }

        /// <summary>
        /// Scopes this wizard's state to an application, so several apps in one solution can share a
        /// state store without colliding (the <c>SetupStateKey.AppId</c> slot). Null for single-app.
        /// </summary>
        public SetupWizardBuilder WithAppId(string? appId)
        {
            _appId = appId;
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
                ReportOutputPath = _options.ReportOutputPath,
                AutoRollbackOnFailure = _options.AutoRollbackOnFailure
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
                ReportOutputPath = _options.ReportOutputPath,
                AutoRollbackOnFailure = _options.AutoRollbackOnFailure
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
                ReportOutputPath = _options.ReportOutputPath,
                AutoRollbackOnFailure = _options.AutoRollbackOnFailure
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
                ReportOutputPath = path,
                AutoRollbackOnFailure = _options.AutoRollbackOnFailure
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
            return new SetupWizard(_wizardId, _steps, _options, _logger, _adapter, _stateStore, _rollback,
                _principal, _authorizer, ResolveAudit(), _definitionHash, _appId);
        }

        /// <summary>
        /// Injected sink wins; else a JSONL sink under <see cref="SetupOptions.ReportOutputPath"/>;
        /// else no-op (matching the "no path = no persistence" pattern used for state and reports).
        /// </summary>
        private Audit.ISetupAuditSink ResolveAudit()
        {
            if (_audit != null) return _audit;
            if (!string.IsNullOrWhiteSpace(_options.ReportOutputPath))
                return new Audit.JsonlSetupAuditSink(
                    System.IO.Path.Combine(_options.ReportOutputPath, $"{_wizardId}.audit.jsonl"), _logger);
            return Audit.NullSetupAuditSink.Instance;
        }

        // ── SetupDefinition interop (Phase 2) ────────────────────────────────

        /// <summary>
        /// Builds a builder from a serialized <see cref="SetupDefinition"/>.
        /// </summary>
        /// <remarks>
        /// Steps are created through <paramref name="factory"/>, which is the allow-list: a
        /// definition names a registered type key, never an arbitrary type.
        /// Disabled steps are omitted entirely.
        /// </remarks>
        public static SetupWizardBuilder FromDefinition(
            SetupDefinition definition,
            ISetupStepFactory factory,
            ILogger<SetupWizard> logger = null)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            if (definition.SchemaVersion > SetupDefinition.CurrentSchemaVersion)
                throw new InvalidOperationException(
                    $"Definition '{definition.Id}' declares schemaVersion {definition.SchemaVersion}, " +
                    $"newer than this build supports ({SetupDefinition.CurrentSchemaVersion}). Upgrade BeepDM.");

            var builder = new SetupWizardBuilder()
                .WithId(definition.Id)
                .WithLogger(logger);

            // Carry the definition's content hash so audit events can bind "what was applied".
            builder._definitionHash = definition.ContentHash
                ?? new Definition.JsonSetupDefinitionSerializer().ComputeContentHash(definition);

            if (!string.IsNullOrWhiteSpace(definition.Environment))
                builder = builder.WithEnvironment(definition.Environment);

            foreach (var stepDef in (definition.Steps ?? new List<SetupStepDefinition>())
                     .Where(s => s != null && s.Enabled))
            {
                builder.AddStep(factory.Create(stepDef));
            }

            // Build() re-validates ids/order/cycles, so a malformed definition fails here with the
            // same message a hand-written wizard would produce.
            return builder;
        }

        /// <summary>
        /// Projects the configured steps back into a serializable <see cref="SetupDefinition"/>.
        /// </summary>
        /// <remarks>
        /// Steps that don't override <c>ISetupStep.SerializeOptions</c> contribute no options and
        /// are logged — the result would otherwise be a definition that looks complete but rebuilds
        /// into a differently-configured wizard.
        /// </remarks>
        public SetupDefinition ToDefinition()
        {
            var def = new SetupDefinition
            {
                Id = _wizardId,
                Environment = _options?.Environment ?? "Development",
                Steps = new List<SetupStepDefinition>(_steps.Count)
            };

            foreach (var step in _steps)
            {
                var options = step.SerializeOptions();
                if (options == null)
                    _logger?.LogWarning(
                        "Step '{StepId}' does not implement SerializeOptions(); its options are omitted " +
                        "from the definition and will not round-trip.", step.StepId);

                def.Steps.Add(new SetupStepDefinition
                {
                    StepId = step.StepId,
                    Type = step.TypeKey,
                    DependsOn = (step.DependsOn ?? Array.Empty<string>()).ToList(),
                    Enabled = true,
                    Options = options
                });
            }

            return def;
        }

        // ── ISetupWizardBuilder explicit implementations ─────────────────────

        ISetupWizardBuilder ISetupWizardBuilder.WithId(string wizardId) => WithId(wizardId);
        ISetupWizardBuilder ISetupWizardBuilder.WithEnvironment(string env) => WithEnvironment(env);
        ISetupWizardBuilder ISetupWizardBuilder.AddStep(ISetupStep step) => AddStep(step);

        // ── Helpers ──────────────────────────────────────────────────────────

        private void ValidateDependencyOrder()
        {
            // Explicit duplicate check: ToDictionary would throw "An item with the same key has
            // already been added", which names neither the step nor the builder.
            var duplicateId = _steps.GroupBy(s => s.StepId, StringComparer.Ordinal)
                                    .FirstOrDefault(g => g.Count() > 1)?.Key;
            if (duplicateId != null)
                throw new InvalidOperationException(
                    $"Duplicate step id '{duplicateId}'. Every step in a wizard must have a unique " +
                    "StepId — steps of the same type must qualify their id (e.g. " +
                    "\"driver-provision:SQLite\").");

            var stepById = _steps.ToDictionary(s => s.StepId, StringComparer.Ordinal);
            var stepIndexById = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < _steps.Count; i++)
                stepIndexById[_steps[i].StepId] = i;

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

                    // Registration-order check. Kahn's below detects cycles but is
                    // order-independent by construction, so it can never catch "declared after
                    // its dependent". SetupWizard.ValidateStepDefinitions enforces the same rule
                    // at Run(); enforcing it here too fails at author time instead.
                    if (stepIndexById[dep] > stepIndexById[step.StepId])
                        throw new InvalidOperationException(
                            $"Step '{step.StepId}' depends on '{dep}', but '{dep}' is registered " +
                            $"after it. Reorder steps so dependencies appear first.");
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
