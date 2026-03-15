using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Pipelines.Attributes;
using TheTechIdea.Beep.Pipelines.Interfaces;
using TheTechIdea.Beep.Pipelines.Models;

namespace TheTechIdea.Beep.Pipelines.Engine.BuiltIn.Transformers
{
    /// <summary>
    /// Inline C# script transformer — stub implementation.
    /// Currently acts as a pass-through; Roslyn-based compilation will be added in a later phase.
    /// Configure the <c>Script</c> parameter with the C# snippet to be compiled and applied per-record.
    /// </summary>
    [PipelinePlugin(
        "beep.transform.script",
        "C# Script",
        PipelinePluginType.Transformer,
        Category = "Transform",
        Version  = "1.0-stub",
        Author   = "The Tech Idea")]
    public class ScriptTransformer : IPipelineTransformer
    {
        public string PluginId    => "beep.transform.script";
        public string DisplayName => "C# Script";
        public string Description => "Applies an inline C# script to each record (stub — pass-through).";

        private string _script = string.Empty;

        public IReadOnlyList<PipelineParameterDef> GetParameterDefinitions() => new[]
        {
            new PipelineParameterDef
            {
                Name        = "Script",
                Type        = ParamType.String,
                IsRequired  = false,
                Description = "Inline C# body. The variable 'record' (PipelineRecord) is available. " +
                              "Roslyn compilation not yet enabled — transformer currently passes records through unchanged."
            }
        };

        public void Configure(IReadOnlyDictionary<string, object> parameters)
        {
            if (parameters.TryGetValue("Script", out var s))
                _script = s?.ToString() ?? "";
        }

        public PipelineSchema GetOutputSchema(PipelineSchema inputSchema) => inputSchema;

        public async IAsyncEnumerable<PipelineRecord> TransformAsync(
            IAsyncEnumerable<PipelineRecord> input,
            PipelineRunContext ctx,
            [EnumeratorCancellation] CancellationToken token)
        {
            // Stub: pass-through until Roslyn integration is complete.
            await foreach (var record in input.WithCancellation(token))
                yield return record;
        }
    }
}
