using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.SetUp.Steps
{
    public class DefaultsSetupStepOptions
    {
        public bool ApplyDefaults { get; set; } = true;
    }

    public class DefaultsSetupStep : ISetupStep
    {
        private readonly DefaultsSetupStepOptions _options;

        public DefaultsSetupStep(DefaultsSetupStepOptions options = null)
        {
            _options = options ?? new DefaultsSetupStepOptions();
        }

        public string StepId => "defaults-setup";
        public string StepName => "Apply Entity Defaults";
        public string Description => "Configures default values (audit timestamps) for the datasource.";
        public IReadOnlyList<string> DependsOn => new[] { "schema-setup" };

        public bool CanSkip(SetupContext context)
            => !_options.ApplyDefaults || context?.Editor?.ConfigEditor == null;

        public IErrorsInfo Validate(SetupContext context)
        {
            if (context?.Editor == null)
                return new ErrorsInfo { Flag = Errors.Failed, Message = "Editor is not available." };
            return new ErrorsInfo { Flag = Errors.Ok };
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
                    return new ErrorsInfo { Flag = Errors.Failed, Message = "No datasource name available." };

                progress?.Report(new PassedArgs { Messege = "Configuring entity defaults...", ParameterInt1 = 0 });

                var existing = configEditor.Getdefaults(editor, dsName) ?? new List<DefaultValue>();

                var defaultFields = new[] { "DateEntered", "DateModified", "CreatedAt", "UpdatedAt" };
                var added = 0;

                foreach (var fieldName in defaultFields)
                {
                    if (existing.Any(d => string.Equals(d.PropertyName, fieldName, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    existing.Add(new DefaultValue
                    {
                        PropertyName = fieldName,
                        PropertyValue = DateTime.UtcNow,
                        IsEnabled = true
                    });
                    added++;
                }

                if (added > 0)
                {
                    var result = configEditor.Savedefaults(editor, existing, dsName);
                    progress?.Report(new PassedArgs { Messege = $"Added {added} default values for '{dsName}'.", ParameterInt1 = 100 });
                    return result;
                }

                progress?.Report(new PassedArgs { Messege = "Defaults already configured.", ParameterInt1 = 100 });
                return new ErrorsInfo { Flag = Errors.Ok, Message = "Defaults already configured." };
            }
            catch (Exception ex)
            {
                return new ErrorsInfo { Flag = Errors.Failed, Message = $"Defaults setup failed: {ex.Message}" };
            }
        }
    }
}
