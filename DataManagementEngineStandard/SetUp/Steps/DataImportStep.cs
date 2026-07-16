using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.ConfigUtil;
using static TheTechIdea.Beep.SetUp.StepErrorHelpers;

namespace TheTechIdea.Beep.SetUp.Steps
{
    // DataImportStepOptions moved to DataManagementModelsStandard/SetUp/Steps/ so a
    // SetupDefinition's option shapes live with the contracts. Same namespace; a TypeForwardedTo
    // in Engine keeps already-compiled consumers resolving.

    public class DataImportStep : IDataImportStep
    {
        private readonly DataImportStepOptions _options;
        private readonly ILogger<DataImportStep>? _logger;

        public DataImportStep(DataImportStepOptions options = null, ILogger<DataImportStep>? logger = null)
        {
            _options = options ?? new DataImportStepOptions();
            _logger = logger;
        }

        public string StepId => "data-import";

        /// <inheritdoc/>
        public System.Text.Json.JsonElement? SerializeOptions()
            => System.Text.Json.JsonSerializer.SerializeToElement(_options, Definition.SetupJson.Options);
        public string StepName => "Import Initial Data";
        public string Description => "Verifies that key entities exist and reports their record counts. Use DataImportManager separately for actual data import.";
        public IReadOnlyList<string> DependsOn =>
            _options.DependsOnStepIds ?? new[] { "defaults-setup", "seeding" };

        public bool CanSkip(SetupContext context)
        {
            if (context?.Options?.DryRun == true) return true;
            if (context?.Options?.SkipSchema == true) return true;
            return _options.EntityNames == null || _options.EntityNames.Count == 0;
        }

        public IErrorsInfo Validate(SetupContext context)
        {
            if (context?.Editor == null)
                return Fail("Editor is not available.");
            return Ok();
        }

        public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs> progress = null)
        {
            try
            {
                var ds = context.DataSource;
                if (ds == null)
                    return Ok("No datasource available for import verification.");

                var total = _options.EntityNames.Count;
                var verified = 0;
                var messages = new List<string>();

                foreach (var entityName in _options.EntityNames)
                {
                    var pct = total > 0 ? (int)(verified * 100.0 / total) : 0;
                    StepErrorHelpers.Report(progress, pct, $"Verifying '{entityName}'...");

                    var entity = ds.GetEntityStructure(entityName, false);
                    if (entity == null)
                    {
                        messages.Add($"Entity '{entityName}' does not exist yet.");
                    }
                    else if (_options.SkipIfTargetHasData)
                    {
                        var rows = ds.GetEntity(entityName, new List<AppFilter>());
                        int count = (rows as System.Collections.IList)?.Count ?? 0;
                        messages.Add($"Entity '{entityName}' exists with {count} records.");
                    }
                    else
                    {
                        messages.Add($"Entity '{entityName}' verified.");
                    }

                    verified++;
                }

                StepErrorHelpers.Report(progress, 100, $"Verified {verified} entities.");
                return Ok(string.Join("; ", messages));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Data import verification failed");
                return Fail($"Data import verification failed: {ex.Message}");
            }
        }
    }
}
