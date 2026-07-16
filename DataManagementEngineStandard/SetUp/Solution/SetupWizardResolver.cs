using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TheTechIdea.Beep.AppMap;
using TheTechIdea.Beep.SetUp.Definition;
using TheTechIdea.Beep.SetUp.State;

namespace TheTechIdea.Beep.SetUp.Solution
{
    /// <summary>
    /// Default <see cref="ISetupWizardResolver"/>: reads an app's <c>SetupDefinition</c> JSON and
    /// builds a per-app wizard from it.
    /// </summary>
    public sealed class SetupWizardResolver : ISetupWizardResolver
    {
        private readonly ISetupStepFactory _factory;
        private readonly ISetupDefinitionSerializer _serializer;
        private readonly ISetupStateStore _stateStore;
        private readonly ILogger<SetupWizard> _logger;

        public SetupWizardResolver(
            ISetupStepFactory factory,
            ISetupStateStore stateStore = null,
            ISetupDefinitionSerializer serializer = null,
            ILogger<SetupWizard> logger = null)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _stateStore = stateStore;
            _serializer = serializer ?? new JsonSetupDefinitionSerializer();
            _logger = logger;
        }

        public Task<ISetupWizard> ResolveAsync(AppDefinition app, string environmentId,
            CancellationToken token = default)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));

            var path = ResolvePath(app);
            if (path == null || !File.Exists(path))
            {
                _logger?.LogInformation("App '{App}' has no setup definition ({Path}); skipping.",
                    app.Id, path ?? "<none>");
                return Task.FromResult<ISetupWizard>(null);
            }

            var definition = _serializer.Deserialize(File.ReadAllText(path));

            var wizard = SetupWizardBuilder
                .FromDefinition(definition, _factory, _logger)
                .WithEnvironment(environmentId)
                .WithAppId(app.Id)
                .WithStateStore(_stateStore)
                .Build();

            return Task.FromResult(wizard);
        }

        public Task<SetupStateKey> GetStateKeyAsync(AppDefinition app, string environmentId,
            CancellationToken token = default)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));

            var path = ResolvePath(app);
            if (path == null || !File.Exists(path))
                return Task.FromResult<SetupStateKey>(null);

            // wizardId == the definition's Id, matching what ResolveAsync/FromDefinition uses.
            var definition = _serializer.Deserialize(File.ReadAllText(path));
            return Task.FromResult(new SetupStateKey(definition.Id, environmentId, app.Id));
        }

        /// <summary>Absolute path as-is; a relative path is resolved against the solution directory.</summary>
        private static string ResolvePath(AppDefinition app)
        {
            var p = app.SetupDefinitionPath;
            if (string.IsNullOrWhiteSpace(p)) return null;
            if (Path.IsPathRooted(p)) return p;

            var slnDir = string.IsNullOrWhiteSpace(app.SolutionPath)
                ? null
                : Path.GetDirectoryName(app.SolutionPath);
            return slnDir == null ? p : Path.Combine(slnDir, p);
        }
    }
}
