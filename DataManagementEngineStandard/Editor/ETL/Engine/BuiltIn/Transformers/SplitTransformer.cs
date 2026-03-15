using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Tags each record with a route label written to <c>Meta["__route_tag"]</c>,
    /// enabling downstream fan-out to different sinks based on data content.
    /// Routes are evaluated in declaration order; the first matching route wins.
    /// Unmatched records receive an empty route tag and are passed through.
    /// Parameters:
    /// <list type="bullet">
    ///   <item><c>Routes</c> — JSON array of <c>{ "Tag": "...", "Filter": "expr" }</c>.</item>
    ///   <item><c>DefaultTag</c> — Tag assigned to unmatched records. Default is empty string.</item>
    /// </list>
    /// </summary>
    [PipelinePlugin(
        "beep.transform.split",
        "Split / Route",
        PipelinePluginType.Transformer,
        Category = "Transform",
        Version  = "1.0",
        Author   = "The Tech Idea")]
    public class SplitTransformer : IPipelineTransformer
    {
        public string PluginId    => "beep.transform.split";
        public string DisplayName => "Split / Route";
        public string Description => "Tags records with a route label for downstream fan-out.";

        public const string RouteTagKey = "__route_tag";

        private readonly List<(string Tag, SimpleExpressionEvaluator? Eval)> _routes = new();
        private string _defaultTag = string.Empty;

        public IReadOnlyList<PipelineParameterDef> GetParameterDefinitions() => new[]
        {
            new PipelineParameterDef
            {
                Name        = "Routes",
                Type        = ParamType.Json,
                IsRequired  = true,
                Description = "JSON array: [ { \"Tag\": \"...\", \"Filter\": \"expr\" }, ... ]"
            },
            new PipelineParameterDef
            {
                Name         = "DefaultTag",
                Type         = ParamType.String,
                IsRequired   = false,
                DefaultValue = "",
                Description  = "Tag applied to records that match no route. Default is empty string."
            }
        };

        public void Configure(IReadOnlyDictionary<string, object> parameters)
        {
            _routes.Clear();

            if (parameters.TryGetValue("DefaultTag", out var dt))
                _defaultTag = dt?.ToString() ?? "";

            if (!parameters.TryGetValue("Routes", out var raw)) return;

            List<Dictionary<string, object>>? routeList = raw switch
            {
                List<Dictionary<string, object>> l => l,
                string json => System.Text.Json.JsonSerializer
                    .Deserialize<List<Dictionary<string, object>>>(json),
                _ => null
            };

            if (routeList == null) return;

            foreach (var entry in routeList)
            {
                string tag    = entry.TryGetValue("Tag",    out var t) ? t?.ToString() ?? "" : "";
                string filter = entry.TryGetValue("Filter", out var f) ? f?.ToString() ?? "" : "";

                SimpleExpressionEvaluator? eval = string.IsNullOrWhiteSpace(filter)
                    ? null
                    : new SimpleExpressionEvaluator(filter);

                _routes.Add((tag, eval));
            }
        }

        // Schema pass-through: route-tagging is done via Meta, not schema fields.
        public PipelineSchema GetOutputSchema(PipelineSchema inputSchema) => inputSchema;

        public async IAsyncEnumerable<PipelineRecord> TransformAsync(
            IAsyncEnumerable<PipelineRecord> input,
            PipelineRunContext ctx,
            [EnumeratorCancellation] CancellationToken token)
        {
            await foreach (var record in input.WithCancellation(token))
            {
                string tag = _defaultTag;

                foreach (var (routeTag, eval) in _routes)
                {
                    if (eval == null || eval.EvaluateBool(record))
                    {
                        tag = routeTag;
                        break;
                    }
                }

                record.Meta[RouteTagKey] = tag;
                yield return record;
            }
        }
    }
}
