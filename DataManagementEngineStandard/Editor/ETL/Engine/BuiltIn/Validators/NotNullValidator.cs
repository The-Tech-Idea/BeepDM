using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Pipelines.Attributes;
using TheTechIdea.Beep.Pipelines.Interfaces;
using TheTechIdea.Beep.Pipelines.Models;
using ValidationResult = TheTechIdea.Beep.Pipelines.Interfaces.ValidationResult;

namespace TheTechIdea.Beep.Pipelines.Engine.BuiltIn.Validators
{
    /// <summary>
    /// Rejects records that contain null (or whitespace-only string) values in the specified fields.
    /// Parameters:
    /// <list type="bullet">
    ///   <item><c>Fields</c>   — comma-separated list of field names to check.</item>
    ///   <item><c>Message</c>  — rejection message template (default: "Field '{field}' must not be null.").</item>
    /// </list>
    /// </summary>
    [PipelinePlugin(
        "beep.validate.notnull",
        "Not-Null",
        PipelinePluginType.Validator,
        Category = "Validate",
        Version  = "1.0",
        Author   = "The Tech Idea")]
    public class NotNullValidator : IPipelineValidator
    {
        public string PluginId    => "beep.validate.notnull";
        public string DisplayName => "Not-Null";
        public string Description => "Rejects records that have null values in specified fields.";

        private string[] _fields  = Array.Empty<string>();
        private string   _message = "Field '{field}' must not be null.";

        public IReadOnlyList<PipelineParameterDef> GetParameterDefinitions() => new[]
        {
            new PipelineParameterDef { Name = "Fields",  Type = ParamType.String, IsRequired = true  },
            new PipelineParameterDef { Name = "Message", Type = ParamType.String, IsRequired = false,
                DefaultValue = "Field '{field}' must not be null." }
        };

        public void Configure(IReadOnlyDictionary<string, object> parameters)
        {
            if (parameters.TryGetValue("Fields", out var f))
                _fields = f.ToString()!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parameters.TryGetValue("Message", out var m) && m != null)
                _message = m.ToString()!;
        }

        public Task<ValidationResult> ValidateAsync(
            PipelineRecord record,
            PipelineRunContext ctx,
            CancellationToken token)
        {
            foreach (var field in _fields)
            {
                var val = record[field];
                bool isNull = val == null || (val is string s && string.IsNullOrWhiteSpace(s));

                if (isNull)
                {
                    string msg = _message.Replace("{field}", field);
                    return Task.FromResult(new ValidationResult(
                        ValidationOutcome.Reject, PluginId, msg, record));
                }
            }

            return Task.FromResult(new ValidationResult(ValidationOutcome.Pass, PluginId, string.Empty));
        }
    }
}
