using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Pipelines.Attributes;
using TheTechIdea.Beep.Pipelines.Engine.Expressions;
using TheTechIdea.Beep.Pipelines.Interfaces;
using TheTechIdea.Beep.Pipelines.Models;

namespace TheTechIdea.Beep.Pipelines.Engine.BuiltIn.Transformers
{
    /// <summary>
    /// Rows-level filter: retains or discards records based on a boolean expression.
    /// Parameters:
    /// <list type="bullet">
    ///   <item><c>Expression</c> — predicate string, e.g. <c>"Country == 'US' AND Amount > 100"</c>.</item>
    ///   <item><c>Mode</c> — <c>"Keep"</c> (default) passes matching rows; <c>"Reject"</c> drops them.</item>
    /// </list>
    /// </summary>
    [PipelinePlugin(
        "beep.transform.filter",
        "Filter",
        PipelinePluginType.Transformer,
        Category = "Transform",
        Version  = "1.0",
        Author   = "The Tech Idea")]
    public class FilterTransformer : IPipelineTransformer
    {
        public string PluginId    => "beep.transform.filter";
        public string DisplayName => "Filter";
        public string Description => "Retains or drops rows based on a boolean expression.";

        private SimpleExpressionEvaluator? _eval;
        private bool _keepMatching = true;   // true = Keep mode, false = Reject mode

        public IReadOnlyList<PipelineParameterDef> GetParameterDefinitions() => new[]
        {
            new PipelineParameterDef
            {
                Name        = "Expression",
                Type        = ParamType.String,
                IsRequired  = true,
                Description = "Boolean expression applied to each row."
            },
            new PipelineParameterDef
            {
                Name         = "Mode",
                Type         = ParamType.String,
                IsRequired   = false,
                DefaultValue = "Keep",
                Description  = "Keep (pass matching rows) or Reject (drop matching rows)."
            }
        };

        public void Configure(IReadOnlyDictionary<string, object> parameters)
        {
            _eval = null;
            _keepMatching = true;

            if (parameters.TryGetValue("Expression", out var expr) && expr is string raw && !string.IsNullOrWhiteSpace(raw))
                _eval = new SimpleExpressionEvaluator(raw);

            if (parameters.TryGetValue("Mode", out var mode))
                _keepMatching = mode?.ToString()?.Trim().ToUpperInvariant() != "REJECT";
        }

        // Schema pass-through: filter never changes column layout
        public PipelineSchema GetOutputSchema(PipelineSchema inputSchema) => inputSchema;

        public async IAsyncEnumerable<PipelineRecord> TransformAsync(
            IAsyncEnumerable<PipelineRecord> input,
            PipelineRunContext ctx,
            [EnumeratorCancellation] CancellationToken token)
        {
            await foreach (var record in input.WithCancellation(token))
            {
                bool match = _eval == null || _eval.EvaluateBool(record);

                if (match == _keepMatching)
                    yield return record;
                else
                    ctx.TotalRecordsRejected++;
            }
        }
    }
}
