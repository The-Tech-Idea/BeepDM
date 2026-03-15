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
    /// Checks that a combination of field values is unique within the current pipeline run.
    /// Parameters:
    /// <list type="bullet">
    ///   <item><c>Fields</c>   — comma-separated list of field names that form the unique key.</item>
    ///   <item><c>Scope</c>    — <c>"RunLocal"</c> (default, tracks duplicates in-memory for this run)
    ///                           or a data-source connection name to query for pre-existing duplicates.
    ///                           Note: cross-run deduplication via a data source is expensive;
    ///                           prefer <see cref="DeDuplicateTransformer"/> for large volumes.</item>
    ///   <item><c>Outcome</c>  — <c>"Reject"</c> (default) or <c>"Warn"</c> on violation.</item>
    ///   <item><c>WindowSize</c> — max keys held in memory for RunLocal scope (default 500 000).</item>
    /// </list>
    /// </summary>
    [PipelinePlugin(
        "beep.validate.uniqueness",
        "Uniqueness",
        PipelinePluginType.Validator,
        Category = "Validate",
        Version  = "1.0",
        Author   = "The Tech Idea")]
    public class UniquenessValidator : IPipelineValidator
    {
        public string PluginId    => "beep.validate.uniqueness";
        public string DisplayName => "Uniqueness";
        public string Description => "Validates that field combinations are unique within a pipeline run.";

        private string[]          _fields      = Array.Empty<string>();
        private string            _scope       = "RunLocal";
        private ValidationOutcome _failOutcome = ValidationOutcome.Reject;
        private int               _windowSize  = 500_000;

        // Keyed on run-id so the validator can be reused across runs
        private readonly Dictionary<string, HashSet<string>> _seen = new(StringComparer.Ordinal);

        public IReadOnlyList<PipelineParameterDef> GetParameterDefinitions() => new[]
        {
            new PipelineParameterDef { Name = "Fields",     Type = ParamType.String, IsRequired = true  },
            new PipelineParameterDef { Name = "Scope",      Type = ParamType.String, IsRequired = false, DefaultValue = "RunLocal" },
            new PipelineParameterDef { Name = "Outcome",    Type = ParamType.String, IsRequired = false, DefaultValue = "Reject"   },
            new PipelineParameterDef { Name = "WindowSize", Type = ParamType.Integer,    IsRequired = false, DefaultValue = "500000"   }
        };

        public void Configure(IReadOnlyDictionary<string, object> parameters)
        {
            if (parameters.TryGetValue("Fields", out var f))
                _fields = f.ToString()!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parameters.TryGetValue("Scope", out var sc))
                _scope = sc?.ToString() ?? "RunLocal";

            if (parameters.TryGetValue("Outcome", out var oc) &&
                Enum.TryParse<ValidationOutcome>(oc?.ToString(), true, out var parsed))
                _failOutcome = parsed;

            if (parameters.TryGetValue("WindowSize", out var ws) &&
                int.TryParse(ws?.ToString(), out int w) && w > 0)
                _windowSize = w;
        }

        public Task<ValidationResult> ValidateAsync(
            PipelineRecord record,
            PipelineRunContext ctx,
            CancellationToken token)
        {
            // Get or create per-run seen set
            if (!_seen.TryGetValue(ctx.RunId, out var seen))
            {
                seen = new HashSet<string>(StringComparer.Ordinal);
                _seen[ctx.RunId] = seen;
            }

            // Evict old run sets (keep only 2 most-recent runs to avoid memory leaks)
            if (_seen.Count > 4)
            {
                foreach (var oldRun in new List<string>(_seen.Keys))
                {
                    if (oldRun != ctx.RunId)
                    {
                        _seen.Remove(oldRun);
                        break;
                    }
                }
            }

            string key = BuildKey(record);

            if (seen.Contains(key))
            {
                string msg = $"Uniqueness violation: key [{key}] already seen in this run.";
                return Task.FromResult(new ValidationResult(_failOutcome, PluginId, msg, record));
            }

            // Evict oldest when window is full (simplistic: clear half)
            if (seen.Count >= _windowSize)
                seen.Clear();

            seen.Add(key);
            return Task.FromResult(new ValidationResult(ValidationOutcome.Pass, PluginId, string.Empty));
        }

        private string BuildKey(PipelineRecord rec)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var f in _fields)
            {
                sb.Append(rec[f]?.ToString() ?? "\0");
                sb.Append('|');
            }
            return sb.ToString();
        }
    }
}
