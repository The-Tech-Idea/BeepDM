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
    // DefaultsSetupStepOptions moved to DataManagementModelsStandard/SetUp/Steps/ so a
    // SetupDefinition's option shapes live with the contracts. Same namespace; a TypeForwardedTo
    // in Engine keeps already-compiled consumers resolving.

    public class DefaultsSetupStep : IDefaultsSetupStep
    {
        private readonly DefaultsSetupStepOptions _options;
        private readonly ILogger<DefaultsSetupStep>? _logger;

        public DefaultsSetupStep(DefaultsSetupStepOptions options = null, ILogger<DefaultsSetupStep>? logger = null)
        {
            _options = options ?? new DefaultsSetupStepOptions();
            _logger = logger;
        }

        public string StepId => "defaults-setup";

        /// <inheritdoc/>
        public System.Text.Json.JsonElement? SerializeOptions()
            => System.Text.Json.JsonSerializer.SerializeToElement(_options, Definition.SetupJson.Options);
        public string StepName => "Apply Entity Defaults";
        public string Description => "Configures default values (audit timestamps) for the datasource.";
        public IReadOnlyList<string> DependsOn => new[] { "schema-setup" };

        public bool CanSkip(SetupContext context)
        {
            if (context?.Options?.DryRun == true) return true;
            if (!_options.ApplyDefaults) return true;
            if (context?.Options?.SkipSchema == true) return true;
            if (context?.Editor?.ConfigEditor == null) return true;
            return false;
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
                var editor = context.Editor;
                var configEditor = editor.ConfigEditor;
                var dsName = context.DataSource?.DatasourceName
                    ?? configEditor?.DataConnections?.FirstOrDefault()?.ConnectionName;

                if (string.IsNullOrWhiteSpace(dsName))
                    return Fail("No datasource name available.");

                StepErrorHelpers.Report(progress, 0, "Configuring entity defaults...");

                var existing = configEditor.Getdefaults(editor, dsName) ?? new List<DefaultValue>();

                var defaultFields = new[] { "DateEntered", "DateModified", "CreatedAt", "UpdatedAt" };
                var added = 0;

                foreach (var fieldName in defaultFields)
                {
                    if (existing.Any(d => string.Equals(d.PropertyName, fieldName, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    // Rule-based, not Static: a Static DefaultValue captures the value once, here,
                    // so every row inserted for the life of the app would carry the setup run's
                    // timestamp. DateTimeResolver resolves "UTCNOW" per insert.
                    existing.Add(new DefaultValue
                    {
                        PropertyName = fieldName,
                        propertyType = DefaultValueType.Rule,
                        Rule = "UTCNOW",
                        IsEnabled = true
                    });
                    added++;
                }

                if (added > 0)
                {
                    var result = configEditor.Savedefaults(editor, existing, dsName);
                    StepErrorHelpers.Report(progress, 100, $"Added {added} default values for '{dsName}'.");
                    return result;
                }

                StepErrorHelpers.Report(progress, 100, "Defaults already configured.");
                return Ok("Defaults already configured.");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Defaults setup failed");
                return Fail($"Defaults setup failed: {ex.Message}");
            }
        }
    }
}
